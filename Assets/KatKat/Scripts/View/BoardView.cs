using System;
using System.Collections.Generic;
using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Tahtanın görünümü. Kural çalıştırmaz: mantıksal durumdan çizer (Sync) ve kural motorunun ürettiği
    /// olay planını (MoveResult) dalga dalga oynatır. Hücre nesneleri bölüm boyunca yeniden kullanılır.
    /// </summary>
    public sealed partial class BoardView : MonoBehaviour
    {
        public const float FaceRatio = 0.95f;
        // v5: dalgalar arası nefes uzadı (kullanıcı: animasyonları yavaşlat); domino yine hızlanır.
        // vr1 P1 prototipi (04 tablo: "zincir dalga arası 100 ms", taslak): dalga, önceki dalganın alt rengi okunur olduktan
        // 100 ms sonra (en az 80 ms); motor sırası aynı. Anahtar kapalıyken eski 420 / 300 ms.
        public static float FirstChainInterval => KatKat.UI.Vr1Proto.On ? PeelSequence.ReadableAt + 100f : 420f;
        public static float MinChainInterval => KatKat.UI.Vr1Proto.On ? PeelSequence.ReadableAt + 80f : 300f;
        /// <summary>Özel taş: ateşleme ile ilk soyulma arası ve hücre başına süpürme gecikmesi (ms).</summary>
        // Kullanıcı (2026-11-08): roket "kullanıldığı an daha büyük ve belirgin olsun, ikiye bölünüp uçtuğunda görülsün,
        // hızlarını düşürelim": öncü süre 140→260 ms (simge büyüyüp parlar), hücre başına 60→100 ms.
        // vr1 P1 prototipi (04 tablo: roket öncü 120 ms + uçuş 350–600 ms; rulo toplam 500–800 ms): öncü 120, roket 60 ms/hücre,
        // rulo 45 ms/hücre. Anahtar kapalıyken eski 260 / 100 / 55.
        public static float SpecialLead => KatKat.UI.Vr1Proto.On ? 120f : 260f;
        public static float RocketCellMs => KatKat.UI.Vr1Proto.On ? 60f : 100f;
        public static float RainbowCellMs => KatKat.UI.Vr1Proto.On ? 45f : 55f;
        /// <summary>Doğan özel taşın tepsiye uçuşu: prototipte 420 ms (tablo), eskisi 500 ms.</summary>
        public static float SpecialFlyMs => KatKat.UI.Vr1Proto.On ? 420f : StarFlight + 120f;

        private readonly List<StackView> _stacks = new List<StackView>();
        private readonly List<TimedAction> _timeline = new List<TimedAction>();
        private readonly List<Shake> _shakes = new List<Shake>();
        private readonly List<SpriteRenderer> _rowMarks = new List<SpriteRenderer>();
        private readonly List<Fx> _fx = new List<Fx>();
        private PeelDirector _director;
        private int _width, _height, _maxLayers;
        /// <summary>Kurulumdaki en yüksek yığın (reklam ödülü taş yerleştirme bu yüksekliği aşmaz).</summary>
        public int MaxLayers => _maxLayers;
        private float _pitch;
        private Vector2 _origin;
        private float _clock;
        private Action _onDone;

        public int Width => _width;
        public int Height => _height;
        public float Pitch => _pitch;
        public bool IsPlaying => _timeline.Count > 0 || (_director != null && _director.ActiveCount > 0);
        public PeelDirector Director => _director;
        /// <summary>Süren görsel efekt (uçuş, patlama) var mı; oynatma dışı yakalama bekler.</summary>
        public bool HasFx => _fx.Count > 0;

        /// <summary>Tahta alanı ölçeği (1: HUD arası tam alan) ve taş boyu sınırı (0: yok; n: en çok n sütunluk tahta boyu).</summary>
        // Kullanıcı seçimi (2026-11-06): B (alan %88) ve küçük tahtada taş en çok 5,5 sütunluk tahtadaki boy
        // ("B iyi; ilk bölümlerdeki örnek A ile B arasında olsun" — B'de 7 sütun idi).
        public static float AreaScale = 0.88f;
        public static float MinColumns = 5.5f;

        public void Init(PeelDirector director)
        {
            _director = director;
        }

        /// <summary>Tahtayı yeni bölüm için kurar; mevcut yığın nesneleri yeniden kullanılır.</summary>
        public void Build(LevelState state, Rect worldArea)
        {
            CompleteImmediately();
            _width = state.Board.Width;
            _height = state.Board.Height;
            int needed = _width * _height;
            while (_stacks.Count < needed)
            {
                var s = new GameObject("Stack").AddComponent<StackView>();
                s.transform.SetParent(transform, false);
                s.SetPalette(SlotColor, _theme.ContactShadow);
                _stacks.Add(s);
            }
            for (int i = 0; i < _stacks.Count; i++) _stacks[i].gameObject.SetActive(false);
            _maxLayers = 0;
            for (int i = 0; i < state.Board.Count; i++) _maxLayers = Mathf.Max(_maxLayers, state.Board[i].Layers.Count);
            // v6 şekilli tahta: etkin ya da geçit hücresi tahtanın parçasıdır; oyulmuş hücrede zemin çizilmez.
            _inShape = new bool[needed];
            for (int i = 0; i < needed; i++) _inShape[i] = state.Board[i].Active || state.Board[i].GateId > 0;
            Layout(worldArea);
            Sync(state);
            if (_hazards == null && state.Hazard != null) _hazards = HazardView.Attach(this);
            _hazards?.Begin(state);
        }

        /// <summary>Konsept pilotu: tehdit katmanı (tehditli bölümde ilk kurulumda eklenir).</summary>
        private HazardView _hazards;
        public HazardView Hazards => _hazards;

        /// <summary>Hücre ızgarası ile tepsi kenarı arası ve çerçeve borusu (hücre adımı oranı).</summary>
        public const float TrayPad = 0.075f, FrameTube = 0.12f;
        private SpriteRenderer _tray, _frame, _traySeal;
        private ThemeDefinition _theme = ThemeDefinition.Coast;

        public ThemeDefinition Theme => _theme;

        /// <summary>Boş hücre yuvası. VR2: iç alanın biraz koyu tonu (kullanıcı 2026-11-14: "taşlar tamamen kalkınca alt zemininde yerini belli eden hücresi olsun").</summary>
        // Tepside yuva görseli dokuya pişirilir (TrayArt) → düz renk kare çizilmez.
        private Color32 SlotColor => TrayArt.Ready ? new Color32(0, 0, 0, 0) : KatKat.UI.Vr2.Ready ? KatKat.UI.Vr2.EmptySlot(_vr2Skin) : _theme.Slot;

        /// <summary>
        /// Tahtanın temasını değiştirir: iç tepsi, renkli çerçeve, boş hücre yuvası ve temas gölgesi birlikte.
        /// Taş paleti değişmez. Tema varlığı yoksa nötr krem yedeğe düşer ve uyarı yazar (mor yedek yok).
        /// </summary>
        public void SetTheme(ThemeDefinition theme)
        {
            _theme = theme ?? ThemeDefinition.Coast;
            if (_tray != null) ApplyThemeSprites();
            foreach (var s in _stacks) s.SetPalette(SlotColor, _theme.ContactShadow);
        }

        private void ApplyThemeSprites()
        {
            var floor = _theme.Id == "coast" ? new Color32(44, 61, 72, 255) :
                _theme.Id == "garden" ? new Color32(51, 67, 48, 255) : new Color32(72, 59, 48, 255);
            _traySeal.color = floor;
            _tray.sprite = ProceduralArt.TraySprite;
            _tray.color = floor;
            _frame.sprite = ReferenceArt.Frame;
            _frame.color = ReferenceArt.FrameTint(_theme.Id);
            // v6 (kullanıcı: "oyun tahtasının borderını kaldır"): renkli çerçeve çizilmez; koyu tepsi kalır.
            _frame.enabled = false;
        }

        /// <summary>Satırlar arasında yığın yüksekliğinin ötesinde bırakılan boşluk (hücre adımı oranı).</summary>
        public const float RowGap = 0.025f;

        /// <summary>
        /// Yığının hücre merkezine göre üst ve alt sınırı (adım biriminde). Üst yüz (n-2)·t yukarıda, en alttaki
        /// katmanın yan şeridi -(S/2 + 1.8·t) aşağıda. Satır adımı bu yükseklikten küçük olursa alt satırın taşları
        /// üst satırın katman şeritlerini örter; alt renkler okunmaz.
        /// </summary>
        private void StackExtent(out float top, out float bottom)
        {
            float s = FaceRatio, t = s * StackView.ThicknessRatio * StackView.ThicknessScale(_maxLayers);
            int n = Mathf.Max(1, _maxLayers);
            top = (n - 2) * t + s * 0.5f;
            bottom = -(s * 0.5f + 1.35f * t);
        }

        public void Layout(Rect area)
        {
            // Satır adımı = yığın yüksekliği + küçük boşluk (en az sütun adımı); tepsi ve çerçeve payı dahil.
            StackExtent(out float top, out float bottom);
            float rowK = Mathf.Max(1f, top - bottom + RowGap);
            float margin = 2f * (TrayPad + FrameTube);
            // VR2: iç alan hücre kenarından Vr2.Inset taşar; tahta alanına bu pay da sığsın (hedef yuvasına/kenara değmesin).
            if (KatKat.UI.Vr2.Ready) margin = Mathf.Max(margin, 2f * KatKat.UI.Vr2.Inset + 0.06f);
            // Ahşap tepsi: kalın kenar tahta alanına sığsın.
            if (TrayArt.Ready) margin = Mathf.Max(margin, 2f * TrayArt.Reach + 0.04f);
            float contentK = (_height - 1) * rowK + (top - bottom);
            // Kullanıcı (Redmi): "taşlar çok büyük, tahtayı ve taşları küçültelim". Alan merkezden AreaScale kadar
            // daralır (her yandan); küçük tahtada taş en çok MinColumns sütunluk tahtadaki kadar büyür.
            if (AreaScale < 0.999f)
            {
                var c = area.center;
                area = new Rect(c.x - area.width * AreaScale * 0.5f, c.y - area.height * AreaScale * 0.5f, area.width * AreaScale, area.height * AreaScale);
            }
            _pitch = Mathf.Min(area.width / (_width + margin), area.height / (contentK + margin));
            if (MinColumns > 0) _pitch = Mathf.Min(_pitch, area.width / (Mathf.Max(_width, MinColumns) + margin));
            _rowPitch = rowK * _pitch;
            float w = _pitch * _width, h = contentK * _pitch;
            // İçerik kutusu alanın ortasına; ilk satırın merkezi kutunun üstünden "top" kadar aşağıda.
            _origin = new Vector2(area.center.x - w * 0.5f + _pitch * 0.5f, area.center.y + h * 0.5f - top * _pitch);
            LayoutFrame(new Vector2(area.center.x, area.center.y), w, h);
            for (int y = 0; y < _height; y++)
            for (int x = 0; x < _width; x++)
            {
                var s = _stacks[y * _width + x];
                s.transform.localPosition = CellCenter(x, y);
                s.SetMaxLayers(_maxLayers);
                s.Init(_pitch * FaceRatio, 10 + y * 4);
            }
        }

        /// <summary>
        /// Lavanta tepsi (kod üretimi, 9-dilim) ve paketteki altın kenarlı çerçeve (9-dilim). Çerçevenin içi
        /// saydamdır; taşlarla birlikte basılmaz. Boru kalınlığı hücre adımına orantılı, tahta oranı ne olursa olsun.
        /// </summary>
        private void LayoutFrame(Vector2 center, float gridW, float gridH)
        {
            if (_tray == null)
            {
                // Solid themed backing overlaps beneath the rim, sealing alpha margins in generated plates.
                _traySeal = new GameObject("TraySeal").AddComponent<SpriteRenderer>();
                _traySeal.transform.SetParent(transform, false);
                _traySeal.sprite = ProceduralArt.TraySprite;
                _traySeal.drawMode = SpriteDrawMode.Sliced;
                _traySeal.sortingOrder = 1;
                _tray = new GameObject("Tray").AddComponent<SpriteRenderer>();
                _tray.transform.SetParent(transform, false);
                _tray.drawMode = SpriteDrawMode.Sliced;
                _tray.sortingOrder = 2;
                _frame = new GameObject("Frame").AddComponent<SpriteRenderer>();
                _frame.transform.SetParent(transform, false);
                _frame.drawMode = SpriteDrawMode.Sliced;
                _frame.sortingOrder = 4;
                ApplyThemeSprites();
            }
            LayoutFloorTiles();
            float pad = TrayPad * _pitch;
            // Dikeyde içerik zaten yığın sınırlarından ölçülü; yatayda hücre ızgarası.
            var traySize = new Vector2(gridW + 2f * pad, gridH + 2f * pad * 0.75f);
            // Tepsi köşe bölgesi: hücrenin ~%45'i; 9-dilim kenarlığı ölçekle aynı oranda kalır.
            float trayBorder = Mathf.Max(1f, _tray.sprite.border.x);
            float trayScale = _pitch * 0.23f / (trayBorder / 100f);
            _tray.transform.localPosition = new Vector3(center.x, center.y, 0f);
            _tray.transform.localScale = new Vector3(trayScale, trayScale, 1f);
            _tray.size = traySize / trayScale;
            // Rim covers the backing edge; retain an opaque floor across all alpha seams.
            float sealScale = _pitch * 0.23f / (_traySeal.sprite.border.x / 100f);
            _traySeal.transform.localPosition = new Vector3(center.x, center.y, 0f);
            _traySeal.transform.localScale = new Vector3(sealScale, sealScale, 1f);
            _traySeal.size = (traySize + Vector2.one * (_pitch * 0.16f)) / sealScale;
            if (_frame == null || _frame.sprite == null) return;
            // Çerçeve kenarlığı ≈ boru + saydam iç pay; boru tepsi kenarının biraz içine biner.
            var b = _frame.sprite.border;
            float borderWorld = 0.27f * _pitch;
            float s = borderWorld / (b.x / 100f);
            _frame.transform.localPosition = new Vector3(center.x, center.y, 0f);
            _frame.transform.localScale = new Vector3(s, s, 1f);
            float inner = borderWorld - FrameTube * _pitch;
            _frame.size = new Vector2(traySize.x + 2f * (borderWorld - inner), traySize.y + 2f * (borderWorld - inner)) / s;
        }

        // ---- v6 şekilli zemin ve bölüm rengi ------------------------------------------------------------
        // Kullanıcı: "tahta farklı şekillerde olsun (kareler birleşimi), tahtanın renkleri de değişsin". Zemin artık
        // tek dikdörtgen değil: tahtaya ait her hücrenin altında yuvarlak köşeli bir karo (komşulara biner, birleşim
        // şeklin dış hattını verir) ve biraz büyük, koyu bir kenar karosu. Renk bölüm numarasından paletten seçilir.

        private bool[] _inShape = new bool[0];
        private readonly List<SpriteRenderer> _floorTiles = new List<SpriteRenderer>(), _rimTiles = new List<SpriteRenderer>();
        private Color _floor = new Color(0.75f, 0.89f, 0.93f, 0.94f);

        // Kullanıcı: "saydam çeşitli renklerde tahtalar olsun": renkli cam gibi yarı saydam zemin (bulanık arka plan
        // altından seçilir); kenar aynı rengin koyu ve daha opak tonu. Taşlar okunur kalsın diye renkler koyu-doygun.
        // Kullanıcı (görselle): "daha önceki bu tahta yapısını kullanalım" — açık renkli düz tepsi, ince açık kenar;
        // şekiller kalır. Renk açık pastel tonlarda bölümden bölüme değişir (hafif saydam: arka plan çok az seçilir).
        private static readonly Color[] FloorPalette =
        {
            new Color(0.75f, 0.89f, 0.93f, 0.94f), // açık mavi (eski tahta)
            new Color(0.80f, 0.91f, 0.84f, 0.94f), // nane
            new Color(0.86f, 0.83f, 0.94f, 0.94f), // lavanta
            new Color(0.96f, 0.86f, 0.80f, 0.94f), // şeftali
            new Color(0.95f, 0.92f, 0.78f, 0.94f), // açık sarı
            new Color(0.95f, 0.83f, 0.87f, 0.94f), // pembe
            new Color(0.79f, 0.86f, 0.95f, 0.94f), // gök
            new Color(0.88f, 0.90f, 0.82f, 0.94f), // adaçayı
        };

        /// <summary>Bölümün zemin rengi (numaradan; komşu bölümler farklı renkte).</summary>
        public void SetLevelPalette(int levelIndex)
        {
            _levelIndex = levelIndex;
            _floor = FloorPalette[((levelIndex * 5) + levelIndex / 10) % FloorPalette.Length];
            ApplyFloorColor();
        }

        // Kullanıcı: "tahta çizgilerinin bazı noktalarında sorun var" (ayrı şeritler iç köşelerde saydam zeminin
        // altında görünüyor, köşelerde üst üste biniyordu). Zemin + kenar artık tek dokuda: hücre maskesi pikselde
        // genişletilir (Öklid uzaklığı → dış köşeler yumuşak yuvarlak), iç bölge saydam renk, dış bant opak kenar.
        // Hiçbir parça üst üste binmez; kenar kalınlığı her yerde aynı.
        private const int FloorRes = 40; // hücre başına piksel
        private SpriteRenderer _floorSr, _floorShadow;
        private Texture2D _floorTex;
        private Rect _floorWorld;

        private void ApplyFloorColor()
        {
            if (_floorSr == null || _inShape.Length == 0) return;
            BakeFloor();
        }

        private void LayoutFloorTiles()
        {
            if (_tray != null) _tray.enabled = false;
            if (_traySeal != null) _traySeal.enabled = false;
            foreach (var t in _floorTiles) if (t != null) t.gameObject.SetActive(false);
            foreach (var t in _rimTiles) if (t != null) t.gameObject.SetActive(false);
            if (_floorSr == null)
            {
                _floorSr = new GameObject("FloorShape").AddComponent<SpriteRenderer>();
                _floorSr.transform.SetParent(transform, false);
                _floorSr.sortingOrder = 2;
            }
            StackExtent(out float top, out float bottom);
            float mid = (top + bottom) * 0.5f * _pitch;
            // Dünya kutusu: hücre ızgarası + her yana pay (kenar ve dış pay).
            var c00 = CellCenter(0, 0) + new Vector3(0f, mid, 0f);
            float padCells = 0.25f;
            float left = c00.x - _pitch * (0.5f + padCells), topY = c00.y + _rowPitch * (0.5f + padCells);
            float w = _pitch * (_width + 2f * padCells), h = _rowPitch * (_height + 2f * padCells);
            _floorWorld = new Rect(left, topY - h, w, h);
            BakeFloor();
            // İçi boş tahta: şeklin içindeki boşluklara hareketli süs (maskot / fener / kaya / çeşme).
            if (_decor == null)
            {
                _decor = new GameObject("Decor").AddComponent<BoardDecor>();
                _decor.transform.SetParent(transform, false);
            }
            // VR2: pasif hücreler ve iç boşluklar arka planı gösterir (belge) → boşluk süsü kurulmaz.
            if (KatKat.UI.Vr2.Ready) { _decor.Rebuild(new bool[_inShape.Length], _width, _height, (x, y) => default, _levelIndex, _theme != null ? _theme.Id : "coast", 10); return; }
            _decor.Rebuild(_inShape, _width, _height, (x, y) =>
            {
                var c = CellCenter(x, y) + new Vector3(0f, mid, 0f);
                return new Rect(c.x - _pitch * 0.5f, c.y - _rowPitch * 0.5f, _pitch, _rowPitch);
            }, _levelIndex, _theme != null ? _theme.Id : "coast", 10);
        }

        private BoardDecor _decor;
        private int _levelIndex;
        public BoardDecor Decor => _decor;

        /// <summary>ui3 kenar malzemesi (docs/ui3_onayli_tasarim/detaylar/03_ZEMIN_VE_BORDER.md); varsayılan B01.</summary>
        public static int BorderMaterial = 0;
        private static readonly string[] BorderSide = { "#D0B18D", "#DFD8BE", "#4F514D", "#8E9D7E", "#B3CCD7", "#C5A260", "#C58A70", "#477D75", "#706579", "#C8B28D" };
        private const int Res3 = 96; // ui3: hücre başına piksel (24 px profil için yeterli çözünürlük)
        private Texture2D _floorTex3;

        /// <summary>
        /// ui3 tahta zemini (yalnız görünüş): P0 = hücre birleşimi + 6 px iç pay; P0'dan dışa 0–4 açık dudak, 4–7 pirinç,
        /// 7–17 fildişi üst pah, 17–24 yan yüz (B01 #D0B18D). İç dolgu fildişi + 45° 1 px kum çizgi (periyot 16, alfa 0.07).
        /// Uzaklık komşu dokuz hücrenin karelerine gerçek Öklid; dış köşe yuvarlak, iç köşe keskin; delikler aynı
        /// profil. Hücre, dokunma alanı, pitch değişmez. Gölge aynı şekil (0, 12) alfa 0.26.
        /// </summary>
        private void BakeFloorUi3()
        {
            float refPerCell = Mathf.Max(40f, _pitch / Mathf.Max(1e-5f, KatKat.UI.HudView.WorldPerRef));
            float pxPerRef = Res3 / refPerCell;
            float clear = 6f * pxPerRef, lip = 4f * pxPerRef, brass = 3f * pxPerRef, bevel = 10f * pxPerRef, side = 7f * pxPerRef;
            float total = clear + lip + brass + bevel + side;
            int pad = Mathf.CeilToInt(total + 2f);
            int tw = _width * Res3 + 2 * pad, th = _height * Res3 + 2 * pad;
            if (_floorTex3 == null || _floorTex3.width != tw || _floorTex3.height != th)
            {
                if (_floorTex3 != null) DestroyImmediate(_floorTex3);
                _floorTex3 = new Texture2D(tw, th, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            }
            Color H(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
            Color ivory = H("#FFF3DE"), sand = H("#D8C09C"), rimHi = H("#FFF9ED"), brassC = H("#B99760"), sideC = H(BorderSide[Mathf.Clamp(BorderMaterial, 0, 9)]);
            Color bevelLo = Color.Lerp(ivory, sand, 0.35f);
            var px = new Color32[tw * th];
            float b1 = clear, b2 = b1 + lip, b3 = b2 + brass, b4 = b3 + bevel, b5 = b4 + side;
            for (int py = 0; py < th; py++)
            for (int pxx = 0; pxx < tw; pxx++)
            {
                float gx = pxx - pad + 0.5f, gy = (th - 1 - py) - pad + 0.5f; // hücre ızgarası px (y aşağı)
                float d = DistUnion(gx, gy);
                Color col;
                if (d <= 0f)
                {
                    col = ivory;
                    // 45° ince kum çizgi (periyot 16 ref px).
                    float u = (gx + gy) / pxPerRef;
                    float m = Mathf.Repeat(u, 16f);
                    if (m < 1f) col = Color.Lerp(col, sand, 0.07f);
                }
                else if (d <= b1) col = ivory;
                else if (d <= b2) col = Color.Lerp(ivory, rimHi, Mathf.Clamp01(d - b1 + 0.5f));
                else if (d <= b3) col = Color.Lerp(rimHi, brassC, Mathf.Clamp01(d - b2 + 0.5f));
                else if (d <= b4)
                {
                    // Üst pah: içte fildişi, dışa doğru hafif koyulaşır (ışık sol üstten).
                    float t = (d - b3) / Mathf.Max(1f, bevel);
                    col = Color.Lerp(brassC, Color.Lerp(ivory, bevelLo, t), Mathf.Clamp01(d - b3 + 0.5f));
                }
                else if (d <= b5) col = Color.Lerp(bevelLo, sideC, Mathf.Clamp01(d - b4 + 0.5f));
                else
                {
                    float a = Mathf.Clamp01(b5 + 1f - d);
                    col = new Color(sideC.r, sideC.g, sideC.b, a);
                }
                px[py * tw + pxx] = col;
            }
            _floorTex3.SetPixels32(px);
            _floorTex3.Apply(false, false);
            if (_floorSr.sprite != null) DestroyImmediate(_floorSr.sprite);
            var sp = Sprite.Create(_floorTex3, new Rect(0, 0, tw, th), new Vector2(0f, 0f), Res3);
            sp.hideFlags = HideFlags.DontSave;
            _floorSr.sprite = sp;
            // Izgara sol üstü (_floorWorld 0.25 hücre pay ile kuruldu).
            float gridLeft = _floorWorld.xMin + 0.25f * _pitch, gridTop = _floorWorld.yMax - 0.25f * _rowPitch;
            float padX = pad / (float)Res3 * _pitch, padY = pad / (float)Res3 * _rowPitch;
            _floorSr.transform.localPosition = new Vector3(gridLeft - padX, gridTop - _height * _rowPitch - padY, 0f);
            _floorSr.transform.localScale = new Vector3(_pitch, _rowPitch, 1f);
            if (_floorShadow == null)
            {
                _floorShadow = new GameObject("FloorShadow").AddComponent<SpriteRenderer>();
                _floorShadow.transform.SetParent(transform, false);
                _floorShadow.sortingOrder = 1;
            }
            _floorShadow.enabled = true;
            _floorShadow.sprite = sp;
            _floorShadow.color = new Color(0.188f, 0.173f, 0.165f, 0.26f);
            _floorShadow.transform.localPosition = _floorSr.transform.localPosition + new Vector3(0f, -12f * KatKat.UI.HudView.WorldPerRef, 0f);
            _floorShadow.transform.localScale = _floorSr.transform.localScale;
        }

        /// <summary>Izgara px noktasının (y aşağı) hücre birleşimine Öklid uzaklığı; içeride 0. Komşu 3×3 hücre yeter (kenar &lt; 1 hücre).</summary>
        private float DistUnion(float gx, float gy)
        {
            int cx = Mathf.FloorToInt(gx / Res3), cy = Mathf.FloorToInt(gy / Res3);
            if (InCell(cx, cy)) return 0f;
            float best = float.MaxValue;
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int x = cx + dx, y = cy + dy;
                if (!InCell(x, y)) continue;
                float ex = Mathf.Max(x * Res3 - gx, 0f, gx - (x + 1) * Res3);
                float ey = Mathf.Max(y * Res3 - gy, 0f, gy - (y + 1) * Res3);
                float dd = ex * ex + ey * ey;
                if (dd < best) best = dd;
            }
            return best == float.MaxValue ? 1e6f : Mathf.Sqrt(best);
        }

        private bool InCell(int x, int y) => x >= 0 && y >= 0 && x < _width && y < _height && _inShape[y * _width + x];

        // ---- VR2 tahta iç alanı (docs/vr2/CLAUDE_CODE.md; Vr2Floor) ----------------------------------------------------
        private int _vr2Skin;
        private const int ResVr2 = 64; // hücre başına piksel (0,085 hücre σ ≈ 5,4 px)
        private Texture2D _floorTexVr2;

        /// <summary>VR2 teması (0..5): iç alan rengi palet.json floorColor. Tahta kurulmadan önce çağrılır.</summary>
        public void SetVr2Skin(int skin)
        {
            _vr2Skin = Mathf.Clamp(skin, 0, KatKat.UI.Vr2.Skins.Length - 1);
            if (_floorSr != null && _inShape.Length > 0 && (KatKat.UI.Vr2.Ready || TrayArt.Ready)) BakeFloor();
            if (KatKat.UI.Vr2.Ready) foreach (var s in _stacks) s.SetPalette(SlotColor, _theme.ContactShadow);
        }

        /// <summary>Test/yakalama: tahta iç alanının rengi (VR2 açıkken temanın floorColor'ı).</summary>
        public Color Vr2FloorColor => KatKat.UI.Vr2.InnerFloor(_vr2Skin);

        /// <summary>Test: iç alan sprite'ı (VR2 dokusu).</summary>
        public Sprite FloorSprite => _floorSr != null ? _floorSr.sprite : null;

        private void BakeFloorVr2()
        {
            int pad = Mathf.CeilToInt(ResVr2 * (KatKat.UI.Vr2.Inset + 0.3f)); // pay + gölge bandı
            var px = Vr2Floor.Bake(_inShape, _width, _height, ResVr2, pad, KatKat.UI.Vr2.InnerFloor(_vr2Skin), KatKat.UI.Vr2.ShadowTint(_vr2Skin), out int tw, out int th);
            if (_floorTexVr2 == null || _floorTexVr2.width != tw || _floorTexVr2.height != th)
            {
                if (_floorTexVr2 != null) DestroyImmediate(_floorTexVr2);
                _floorTexVr2 = new Texture2D(tw, th, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "vr2_floor" };
            }
            _floorTexVr2.SetPixels32(px);
            _floorTexVr2.Apply(false, false);
            if (_floorSr.sprite != null) DestroyImmediate(_floorSr.sprite);
            var sp = Sprite.Create(_floorTexVr2, new Rect(0, 0, tw, th), new Vector2(0f, 0f), ResVr2);
            sp.hideFlags = HideFlags.DontSave;
            _floorSr.sprite = sp;
            _floorSr.color = Color.white;
            float gridLeft = _floorWorld.xMin + 0.25f * _pitch, gridTop = _floorWorld.yMax - 0.25f * _rowPitch;
            float padX = pad / (float)ResVr2 * _pitch, padY = pad / (float)ResVr2 * _rowPitch;
            _floorSr.transform.localPosition = new Vector3(gridLeft - padX, gridTop - _height * _rowPitch - padY, 0f);
            _floorSr.transform.localScale = new Vector3(_pitch, _rowPitch, 1f);
            // Dış gölge yok (belge).
            if (_floorShadow != null) _floorShadow.enabled = false;
        }

        // ---- Ahşap kenarlı krem tepsi (kullanıcı 2026-11-17, docs/gorseller/tepsi; TrayArt) -----------------------------
        private const int ResTray = 112; // hücre başına piksel (kenar ≈ 13 px)
        private Texture2D _floorTexTray;

        private void BakeFloorTray()
        {
            int resY = Mathf.Max(1, Mathf.RoundToInt(ResTray * _rowPitch / Mathf.Max(1e-5f, _pitch)));
            // Pay: dış hat + iç köşe kapamasının genişletme payı (doku kenarına değerse kapama orada geri daralamaz) + gölge.
            int pad = Mathf.CeilToInt(ResTray * (TrayArt.Reach + TrayArt.ConcaveR + 0.08f));
            // Yuva = eski boş hücre karesinin yeri (StackView: gövde -S/2, kare +S/2 − 1,1t → hücre merkezinden −1,1t).
            StackExtent(out float top, out float bottom);
            float t = FaceRatio * StackView.ThicknessRatio * StackView.ThicknessScale(_maxLayers) * _pitch;
            float mid = (top + bottom) * 0.5f * _pitch;
            float socketDy = (1.1f * t + mid) / _rowPitch;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            // Kenar tonu: bölgenin arka plan teması (VR2 paleti, ana ton); VR2 yoksa özgün ahşap.
            Color? tone = KatKat.UI.Vr2.Ready ? (Color)KatKat.UI.Vr2.Skins[Mathf.Clamp(_vr2Skin, 0, KatKat.UI.Vr2.Skins.Length - 1)].Base : (Color?)null;
            var px = TrayArt.Bake(_inShape, _width, _height, ResTray, resY, pad, socketDy, FaceRatio * TrayArt.SocketOverFace, tone, out int tw, out int th);
            if (_floorTexTray == null || _floorTexTray.width != tw || _floorTexTray.height != th)
            {
                if (_floorTexTray != null) DestroyImmediate(_floorTexTray);
                _floorTexTray = new Texture2D(tw, th, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "tray_floor" };
            }
            _floorTexTray.SetPixels32(px);
            _floorTexTray.Apply(false, false);
            LastTrayBakeMs = sw.ElapsedMilliseconds;
            if (_floorSr.sprite != null) DestroyImmediate(_floorSr.sprite);
            var sp = Sprite.Create(_floorTexTray, new Rect(0, 0, tw, th), new Vector2(0f, 0f), ResTray);
            sp.hideFlags = HideFlags.DontSave;
            _floorSr.sprite = sp;
            _floorSr.color = Color.white;
            float gridLeft = _floorWorld.xMin + 0.25f * _pitch, gridTop = _floorWorld.yMax - 0.25f * _rowPitch;
            float padW = pad / (float)ResTray * _pitch;
            _floorSr.transform.localPosition = new Vector3(gridLeft - padW, gridTop - _height * _rowPitch - padW, 0f);
            _floorSr.transform.localScale = new Vector3(_pitch, _pitch, 1f);
            // Hafif dış gölge: aynı şekil, koyu, biraz aşağıda (tepsi arka plandan ayrılsın).
            if (_floorShadow == null)
            {
                _floorShadow = new GameObject("FloorShadow").AddComponent<SpriteRenderer>();
                _floorShadow.transform.SetParent(transform, false);
                _floorShadow.sortingOrder = 1;
            }
            _floorShadow.enabled = true;
            _floorShadow.sprite = sp;
            _floorShadow.color = new Color(0.10f, 0.07f, 0.04f, 0.30f);
            _floorShadow.transform.localPosition = _floorSr.transform.localPosition + new Vector3(0f, -0.05f * _pitch, 0f);
            _floorShadow.transform.localScale = _floorSr.transform.localScale;
        }

        /// <summary>Test/ölçüm: son tepsi pişirme süresi (ms).</summary>
        public static long LastTrayBakeMs;

        private void BakeFloor()
        {
            if (TrayArt.Ready) { BakeFloorTray(); return; }
            if (KatKat.UI.Vr2.Ready) { BakeFloorVr2(); return; }
            if (KatKat.UI.Ui3.Ready) { BakeFloorUi3(); return; }
            int pad = Mathf.RoundToInt(FloorRes * 0.25f);
            int tw = _width * FloorRes + 2 * pad, th = _height * FloorRes + 2 * pad;
            if (_floorTex == null || _floorTex.width != tw || _floorTex.height != th)
            {
                if (_floorTex != null) DestroyImmediate(_floorTex);
                _floorTex = new Texture2D(tw, th, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            }
            // Ölçüler piksel (hücre = FloorRes): dış pay 0.06, kenar 0.07 hücre; yumuşatma 1 px.
            float margin = 0.07f * FloorRes, edge = (KatKat.View.GameShellArt.Enabled ? 0.11f : 0.05f) * FloorRes; // v10: belirgin altın kontur
            float reach = margin + edge + 1.5f;
            int r = Mathf.CeilToInt(reach);
            // Eski tahta: ince, neredeyse beyaz açık kenar.
            // v10 kabuk: krem iç tepsi + sarı-altın ince kontur (kontur mantıksal matristen; hücre merkezi değişmez).
            var floor = KatKat.View.GameShellArt.Enabled ? KatKat.View.GameShellArt.TrayFill : _floor;
            var rim = KatKat.View.GameShellArt.Enabled ? KatKat.View.GameShellArt.TrayRim : Color.Lerp(_floor, Color.white, 0.75f);
            rim.a = 1f;
            var px = new Color32[tw * th];
            for (int py = 0; py < th; py++)
            for (int pxx = 0; pxx < tw; pxx++)
            {
                // Doku y yukarı; hücre satırı y aşağı.
                int gx = pxx - pad, gy = (th - 1 - py) - pad;
                float d = Dist(gx, gy, r);
                Color col;
                if (d <= margin) col = floor;
                else if (d <= margin + edge)
                {
                    // İç tarafta 1 px saydam→opak yumuşak geçiş.
                    float t = Mathf.Clamp01(d - margin);
                    col = Color.Lerp(floor, rim, t);
                }
                else
                {
                    float a = Mathf.Clamp01(margin + edge + 1f - d);
                    col = new Color(rim.r, rim.g, rim.b, a);
                }
                px[py * tw + pxx] = col;
            }
            _floorTex.SetPixels32(px);
            _floorTex.Apply(false, false);
            if (_floorSr.sprite != null) DestroyImmediate(_floorSr.sprite);
            var sp = Sprite.Create(_floorTex, new Rect(0, 0, tw, th), new Vector2(0f, 0f), FloorRes);
            sp.hideFlags = HideFlags.DontSave;
            _floorSr.sprite = sp;
            _floorSr.transform.localPosition = new Vector3(_floorWorld.xMin, _floorWorld.yMin, 0f);
            _floorSr.transform.localScale = new Vector3(_floorWorld.width / (tw / (float)FloorRes), _floorWorld.height / (th / (float)FloorRes), 1f);
            // v10: aşağı yönlü yumuşak gölge (aynı şekil, koyu, biraz aşağıda).
            if (_floorShadow == null)
            {
                _floorShadow = new GameObject("FloorShadow").AddComponent<SpriteRenderer>();
                _floorShadow.transform.SetParent(transform, false);
                _floorShadow.sortingOrder = 1;
            }
            _floorShadow.enabled = KatKat.View.GameShellArt.Enabled;
            _floorShadow.sprite = sp;
            _floorShadow.color = KatKat.View.GameShellArt.TrayShadow;
            _floorShadow.transform.localPosition = _floorSr.transform.localPosition + new Vector3(0f, -0.09f * _pitch, 0f);
            _floorShadow.transform.localScale = _floorSr.transform.localScale;
        }

        /// <summary>Piksel (hücre ızgarası koordinatı, y aşağı) ile şekle ait en yakın piksel arası uzaklık (0: içeride).</summary>
        private float Dist(int gx, int gy, int r)
        {
            if (InShapePx(gx, gy)) return 0f;
            float best = 999f;
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                float dd = Mathf.Sqrt(dx * dx + dy * dy);
                if (dd >= best || dd > r + 0.5f) continue;
                if (InShapePx(gx + dx, gy + dy)) best = dd;
            }
            return best;
        }

        private bool InShapePx(int gx, int gy)
        {
            if (gx < 0 || gy < 0) return false;
            int cx = gx / FloorRes, cy = gy / FloorRes;
            return cx < _width && cy < _height && _inShape[cy * _width + cx];
        }

        public Vector3 CellCenter(int x, int y) => new Vector3(_origin.x + x * _pitch, _origin.y - y * _rowPitch, 0f);

        /// <summary>Satırlar arası dünya mesafesi (yığın yüksekliğine göre sütun adımından büyük olabilir).</summary>
        public float RowPitch => _rowPitch;
        private float _rowPitch;

        public StackView StackAt(int x, int y) => _stacks[y * _width + x];

        /// <summary>Bütün hücreleri mantıksal durumdan yeniden çizer.</summary>
        public void Sync(LevelState state)
        {
            for (int y = 0; y < _height; y++)
            for (int x = 0; x < _width; x++)
            {
                var cell = state.Board[x, y];
                var s = StackAt(x, y);
                // M11: açılmamış geçit hücresi soluk önizleme olarak görünür (oyuncu ne geleceğini bilir).
                bool ghost = !cell.Active && cell.GateId > 0;
                s.gameObject.SetActive(cell.Active || ghost);
                if (ghost)
                {
                    s.SetLayers(System.Array.Empty<LayerColor>());
                    s.SetMarks(0, false);
                    s.SetCovers(0, default, 0);
                    s.SetRotor(false, false);
                    s.SetSpecial(SpecialKind.None, default);
                    s.SetSponge(0, 0);
                    s.SetButton(false);
                    s.SetGhost(true, cell.GateLayers);
                    continue;
                }
                if (!cell.Active) continue;
                s.SetGhost(false);
                s.SetLayers(cell.Layers);
                s.SetMarks(cell.ClipHitsRemaining + cell.LinkHits, cell.HasBottomStar);
                s.SetLinkTint(cell.LinkHits > 0 ? cell.LinkId : 0);
                s.SetSponge(cell.SpongeNeed, cell.SpongeHave);
                s.SetButton(cell.ButtonGate > 0);
                s.SetCovers(cell.IceHitsRemaining, cell.LockColor, cell.LockRemaining, cell.HasEnvelope, cell.EnvelopeColor);
                s.SetRotor(cell.HasRotor, cell.IsRotatable);
                s.SetSpecial(cell.Special, cell.SpecialColor);
                s.SetProtected(cell.Protected);
                s.transform.localRotation = Quaternion.identity;
                s.transform.localScale = Vector3.one;
            }
            _hazards?.Show(state);
        }

        /// <summary>Döner taban düğmesi isabeti (düğme çevresinde ~0.26 adım). Hücre dokunuşundan önce denetlenir.</summary>
        public bool TryRotorAt(Vector2 world, out int x, out int y)
        {
            float r = _pitch * 0.26f;
            for (int cy = 0; cy < _height; cy++)
            for (int cx = 0; cx < _width; cx++)
            {
                var s = StackAt(cx, cy);
                if (!s.gameObject.activeSelf || !s.HasRotor) continue;
                var p = s.transform.position + s.RotorLocal;
                if (((Vector2)p - world).sqrMagnitude <= r * r) { x = cx; y = cy; return true; }
            }
            x = y = -1;
            return false;
        }

        /// <summary>Dünya noktasındaki hücre. Hücrenin tamamı hedeflenebilir (yalnızca görünen yüzey değil).</summary>
        public bool TryCellAt(Vector2 world, out int x, out int y)
        {
            float fx = (world.x - (_origin.x - _pitch * 0.5f)) / _pitch;
            float fy = ((_origin.y + _rowPitch * 0.5f) - world.y) / _rowPitch;
            x = Mathf.FloorToInt(fx);
            y = Mathf.FloorToInt(fy);
            return x >= 0 && y >= 0 && x < _width && y < _height;
        }

        /// <summary>Komşu soyulma kalkarken ataş hasarı görünür (köşe kalkışının sonu).</summary>
        public static float ClipHitAt => PeelSequence.LiftEnd + 20f;
        /// <summary>Yıldız, son katman okunur hâle gelince tabandan kalkar ve hedefe uçar.</summary>
        public const float StarRiseAt = 240f;
        public const float StarFlight = 380f;

        public void Play(MoveResult result, Func<LayerColor, Vector3> goalTarget, Action<PeelWave> onWaveStart, Action<PeelWave> onWaveArrived, Action onDone)
            => Play(result, goalTarget, null, onWaveStart, onWaveArrived, onDone);

        /// <param name="goalTarget">Rengin uçacağı dünya noktası (HUD hedef simgesi).</param>
        /// <param name="starTarget">Yıldızın uçacağı dünya noktası (HUD yıldız hedefi).</param>
        /// <param name="onWaveArrived">Dalganın parçaları hedefe vardığında; argüman dalga indeksi.</param>
        public void Play(MoveResult result, Func<LayerColor, Vector3> goalTarget, Func<Vector3> starTarget, Action<PeelWave> onWaveStart, Action<PeelWave> onWaveArrived, Action onDone)
        {
            _onDone = onDone;
            float t = 0f, interval = FirstChainInterval;
            float last = 0f;
            if (result.Command.Type == CommandType.Rotate)
            {
                PlayRotate(result);
                At(RotateMs + 30f, FinishPlay);
                return;
            }
            if (result.Command.Type == CommandType.Paint)
            {
                PlayPaint(result);
                At(460f, FinishPlay);
                return;
            }
            bool created = result.SpecialCreated != SpecialKind.None;
            foreach (var wave in result.Waves)
            {
                var w = wave;
                float start = t;
                bool special = w.Kind == WaveKind.Special;
                At(start, () => onWaveStart?.Invoke(w));
                if (special) At(start, () => FireFx(w));
                else if (w.Index >= 1) At(start, () => ShakeBoard(0.012f * Mathf.Min(4, w.Index), 260f));
                float spread = 0f;
                foreach (var c in w.Cells)
                {
                    var cp = c;
                    // Aynı dalga birlikte başlar; en çok 20 ms belirlenimli görsel sapma. Özel taşta soyulma, roket/ışık
                    // hücreye vardığında başlar (kaynaktan uzaklığa göre).
                    float delay = special ? SpecialDelay(w, cp.X, cp.Y) : ((cp.X + cp.Y) % 3) * 10f;
                    spread = Mathf.Max(spread, delay);
                    At(start + delay, () =>
                    {
                        var stack = StackAt(cp.X, cp.Y);
                        // v10: görev rengiyse parçaların yarısı görev simgesine, kalanı yıldız çubuğuna; değilse hepsi çubuğa.
                        bool isGoal = IsGoalColor == null || IsGoalColor(cp.Removed);
                        var star = ScoreTarget != null ? ScoreTarget() : goalTarget(cp.Removed);
                        if (stack.Layers.Count > 0) _director.Peel(stack, goalTarget(cp.Removed), star, ScoreTarget == null ? 1f : isGoal ? 0.5f : 0f, null);
                        else Debug.LogError($"[KatKat] Görünüm mantıktan ayrıştı: ({cp.X},{cp.Y}) boş");
                    });
                    if (cp.StarCollected)
                        At(start + delay + StarRiseAt, () => LaunchStar(StackAt(cp.X, cp.Y), starTarget != null ? starTarget() : transform.position + Vector3.up * 5f));
                }
                foreach (var h in w.ClipHits)
                {
                    var hit = h;
                    At(start + (special ? SpecialDelay(w, hit.X, hit.Y) : 0f) + ClipHitAt, () => HitClip(StackAt(hit.X, hit.Y), hit.RemainingAfter));
                }
                foreach (var h in w.IceHits)
                {
                    var hit = h;
                    At(start + (special ? SpecialDelay(w, hit.X, hit.Y) : 0f) + ClipHitAt, () => HitIce(StackAt(hit.X, hit.Y), hit.RemainingAfter));
                }
                foreach (var e in w.EnvelopeOpens)
                {
                    var env = e;
                    At(start + (special ? SpecialDelay(w, env.X, env.Y) : 0f) + ClipHitAt, () => OpenEnvelope(StackAt(env.X, env.Y)));
                }
                foreach (var h in w.LinkHits)
                {
                    var hit = h;
                    At(start + (special ? SpecialDelay(w, hit.X, hit.Y) : 0f) + ClipHitAt, () => HitClip(StackAt(hit.X, hit.Y), hit.RemainingAfter));
                }
                foreach (var h in w.SpongeHits)
                {
                    var hit = h;
                    At(start + (special ? SpecialDelay(w, hit.X, hit.Y) : 0f) + PeelSequence.ReadableAt, () =>
                    {
                        var st = StackAt(hit.X, hit.Y);
                        st.SetSponge(hit.Need, hit.HaveAfter);
                        Flash(st, new Color(1f, 0.95f, 0.6f, 0.8f), 1.2f);
                    });
                }
                foreach (var o in w.SpongeOpens)
                {
                    var op = o;
                    At(start + PeelSequence.ReadableAt + 160f, () => OpenSponge(StackAt(op.X, op.Y)));
                }
                foreach (var b in w.ButtonPresses)
                {
                    var bp = b;
                    At(start + PeelSequence.ReadableAt, () => PressButton(StackAt(bp.X, bp.Y)));
                }
                for (int g = 0; g < w.GateOpens.Count; g++)
                {
                    var gp = w.GateOpens[g];
                    At(start + PeelSequence.ReadableAt + 200f + g * 110f, () => OpenGate(StackAt(gp.X, gp.Y)));
                    spread = Mathf.Max(spread, 200f + g * 110f);
                }
                foreach (var d in w.DirectHits)
                {
                    var dh = d;
                    At(start + SpecialDelay(w, dh.X, dh.Y), () => Flash(StackAt(dh.X, dh.Y), Color.white, 0.9f));
                }
                if (w.Index == 0 && created && !special)
                {
                    var r = result;
                    At(start + PeelSequence.ReadableAt, () => CreateFx(r));
                }
                foreach (var h in w.LockHits)
                {
                    var hit = h;
                    // İlgili renkten kalkan katmanlar kilide kısa ışık izleri gönderir (en çok 4; rehber §10 M06).
                    int sent = 0;
                    foreach (var c in w.Cells)
                    {
                        if (c.Removed != hit.Color || sent >= 4) continue;
                        var from = c;
                        At(start + PeelSequence.ReadableAt - StreakMs + sent * 15f, () => Streak(StackAt(from.X, from.Y), StackAt(hit.X, hit.Y), hit.Color));
                        sent++;
                    }
                    // Sayaç, ilgili renk katmanları kalkıp okunur hâle geldiğinde düşer.
                    At(start + PeelSequence.ReadableAt, () => HitLock(StackAt(hit.X, hit.Y), hit.RemainingAfter));
                }
                At(start + spread + PeelSequence.ReleaseEnd, () => onWaveArrived?.Invoke(w));
                last = Mathf.Max(last, start + spread + PeelSequence.Duration + 30f);
                // Özel taş dalgasında sıradaki dalga süpürme bitince; dönüşüm dalgası (hücresiz) kısa.
                float step = special ? (w.Cells.Count == 0 ? 120f + 90f * w.Triggered.Count : spread + PeelSequence.ReadableAt) : interval;
                // M11: açılan geçit hücreleri sonraki dalgada soyulabilir; görünüm onları açmadan sıradaki dalga başlamasın.
                if (w.GateOpens.Count > 0) step = Mathf.Max(step, PeelSequence.ReadableAt + 200f + w.GateOpens.Count * 110f + 60f);
                t += step;
                interval = Mathf.Max(MinChainInterval, interval * 0.85f);
            }
            if (created) last = Mathf.Max(last, PeelSequence.ReadableAt + StarFlight + 160f); // tepsiye uçuş bitsin
            At(last, FinishPlay);
        }

        // ---- M09–M12 sunumu ------------------------------------------------------------------------

        /// <summary>Süngerin uçacağı nokta (HUD sünger hedefi); atanmamışsa yukarı uçar.</summary>
        public Func<Vector3> SpongeTarget { get; set; }

        /// <summary>Sünger dolunca sıkışıp kalkar ve hedefe uçar; alt yığın sağlam kalır.</summary>
        private void OpenSponge(StackView stack)
        {
            var sp = stack.SpongeRenderer;
            if (sp == null) { stack.SetSponge(0, 0); return; }
            var ghost = Ghost(sp, stack.transform);
            ghost.sortingOrder = 10 + _height * 4 + 8;
            stack.SetSponge(0, 0);
            var from = ghost.transform.position;
            var to = SpongeTarget != null ? SpongeTarget() : from + Vector3.up * 5f;
            _fx.Add(new Fx { Kind = FxKind.Fly, T = ghost.transform, R = ghost, Owned = true, From = from, To = to, BaseScale = ghost.transform.localScale * 0.6f, Dur = StarFlight });
            Flash(stack, new Color(1f, 0.9f, 0.5f, 0.9f), 1.6f);
        }

        /// <summary>Düğme basılır (çöker, söner), tahta hafif sarsılır.</summary>
        private void PressButton(StackView stack)
        {
            var b = stack.ButtonRenderer;
            if (b != null)
            {
                var ghost = Ghost(b, stack.transform);
                ghost.sortingOrder = 10 + _height * 4 + 8;
                _fx.Add(new Fx { Kind = FxKind.Burst, T = ghost.transform, R = ghost, Owned = true, BaseScale = ghost.transform.localScale, Dur = 320f });
            }
            stack.SetButton(false);
            Flash(stack, new Color(1f, 0.6f, 0.5f, 0.9f), 1.4f);
            ShakeBoard(0.02f, 240f);
        }

        /// <summary>Geçit hücresi ışık alıp yükselir: hayalet kalkar, gerçek yığın küçükten büyüyerek gelir.</summary>
        private void OpenGate(StackView stack)
        {
            var layers = new List<LayerColor>(stack.GhostLayers);
            stack.SetGhost(false);
            stack.SetLayers(layers);
            Flash(stack, new Color(1f, 1f, 0.85f, 0.9f), 1.6f);
            _fx.Add(new Fx { Kind = FxKind.Pop, T = stack.transform, BaseScale = Vector3.one, Dur = 300f });
        }

        /// <summary>M12 boya: üst renk yumuşak boya dalgasıyla değişir.</summary>
        private void PlayPaint(MoveResult r)
        {
            var stack = StackAt(r.Painted.X, r.Painted.Y);
            var color = LayerPalette.FrontColor(r.PaintTo);
            Flash(stack, new Color(color.r / 255f, color.g / 255f, color.b / 255f, 0.95f), 1.8f);
            At(140f, () =>
            {
                var layers = new List<LayerColor>(stack.Layers);
                if (layers.Count > 0) layers[layers.Count - 1] = r.PaintTo;
                stack.SetLayers(layers);
                _fx.Add(new Fx { Kind = FxKind.Pop, T = stack.transform, BaseScale = Vector3.one, Dur = 260f });
            });
        }

        // ---- v5 özel taş sunumu ---------------------------------------------------------------------

        /// <summary>Özel taş dalgasında hücrenin soyulma gecikmesi: roket satır/sütunda, ışık kaynaktan uzaklıkla yayılır.</summary>
        private static float SpecialDelay(PeelWave w, int x, int y)
        {
            float dx = x - w.FiredAt.X, dy = y - w.FiredAt.Y;
            bool rocket = w.Fired.IsRocket() && (w.ComboWith == SpecialKind.None || w.ComboWith.IsRocket());
            if (rocket) return SpecialLead + (Mathf.Abs(dx) + Mathf.Abs(dy)) * RocketCellMs;
            return SpecialLead + 40f + Mathf.Sqrt(dx * dx + dy * dy) * RainbowCellMs;
        }

        /// <summary>Doğan özel taşın uçacağı dünya noktası (HUD tepsi yuvası); atanmamışsa yerinde söner.</summary>
        public Func<Vector3> TrayTarget { get; set; }
        /// <summary>v10: yıldız (puan) çubuğu hedefi ve rengin görev olup olmadığı (kırılan taş parçalarının yönü).</summary>
        public Func<Vector3> ScoreTarget { get; set; }
        public Func<LayerColor, bool> IsGoalColor { get; set; }

        /// <summary>
        /// Doğma: v6'da özel taş tahtada kalmaz; dokunulan yığının üstünde belirip (parlama) tepsideki yuvasına uçar.
        /// Tepsi doluysa yerinde patlayıp söner (yalnız puan).
        /// </summary>
        private void CreateFx(MoveResult r)
        {
            var at = r.SpecialCreatedAt;
            var stack = StackAt(at.X, at.Y);
            var glowColor = r.SpecialCreated == SpecialKind.Rainbow ? new Color(1f, 0.95f, 1f, 0.9f) : new Color(1f, 0.85f, 0.4f, 0.9f);
            Flash(stack, glowColor, 1.8f);
            ShakeBoard(0.02f, 220f);
            var go = new GameObject("TrayToken");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpecialArt.For(r.SpecialCreated);
            sr.sortingOrder = 10 + _height * 4 + 10;
            var from = stack.transform.position + stack.TopFaceLocal;
            go.transform.position = from;
            go.transform.rotation = Quaternion.Euler(0f, 0f, SpecialArt.Angle(r.SpecialCreated));
            go.transform.localScale = Vector3.one * (_pitch * 0.9f / Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y));
            if (r.SpecialToTray && TrayTarget != null)
                _fx.Add(new Fx { Kind = FxKind.Fly, T = go.transform, R = sr, Owned = true, From = from, To = TrayTarget(), BaseScale = go.transform.localScale, Dur = SpecialFlyMs });
            else
                _fx.Add(new Fx { Kind = FxKind.Burst, T = go.transform, R = sr, Owned = true, BaseScale = go.transform.localScale, Dur = 360f });
        }

        /// <summary>Ateşleme: taş patlayarak kalkar; roket iki yöne yuvarlanan rulolar, gökkuşağı yayılan ışık.</summary>
        private void FireFx(PeelWave w)
        {
            var src = StackAt(w.FiredAt.X, w.FiredAt.Y);
            // v11 (kullanıcı, 2026-11-08): yatay/dikey simge ateşlenince ortadan ikiye bölünür ve yarılar zıt yönlere
            // gider; bu yüzden roket kaynaklarında büyüyüp sönen hayalet yok (simgenin kendisi ayrılır).
            bool split = SpecialArt.HasV11 && w.Fired.IsRocket() && (w.ComboWith == SpecialKind.None || w.ComboWith.IsRocket());
            BurstToken(src, !split);
            if (w.ComboWith != SpecialKind.None) BurstToken(StackAt(w.ComboAt.X, w.ComboAt.Y), !split);
            bool aRain = w.Fired == SpecialKind.Rainbow, bRain = w.ComboWith == SpecialKind.Rainbow;
            bool combo = w.ComboWith != SpecialKind.None;

            if (combo && (aRain != bRain))
            {
                // Rulo + gökkuşağı: renkteki yığınlar sırayla rokete dönüşür (mantıkla aynı sıra: satır, sütun, ...).
                Flash(src, new Color(1f, 0.95f, 1f, 1f), 3.2f);
                for (int i = 0; i < w.Triggered.Count; i++)
                {
                    var c = w.Triggered[i];
                    var kind = i % 2 == 0 ? SpecialKind.RocketRow : SpecialKind.RocketColumn;
                    At(80f + i * 90f, () =>
                    {
                        var s = StackAt(c.X, c.Y);
                        s.SetSpecial(kind, default);
                        s.PopSpecial();
                        Flash(s, new Color(1f, 0.9f, 0.5f, 0.9f), 1.4f);
                    });
                }
                ShakeBoard(0.03f, 360f);
                return;
            }
            if (aRain || bRain)
            {
                // Tek gökkuşağı ya da gökkuşağı + gökkuşağı: kaynakta büyüyen ışık halkası; ikilide bütün tahtaya yayılır.
                Expand(src, combo ? Mathf.Max(_width, _height) * 1.4f : 3.2f, combo ? 900f : 520f);
                ShakeBoard(combo ? 0.05f : 0.025f, combo ? 600f : 320f);
                // Kullanıcı (2026-11-08): "patlayacak olan tüm renklere yıldırım gibi ağ fırlatılsın". Kaynaktan her hedef
                // taşa titreşen bir yıldırım; o taş soyulana kadar yanar (ikili gökkuşağında bütün tahta ışıkla gider).
                if (!combo)
                {
                    var from = src.transform.position + src.TopFaceLocal;
                    var targets = new List<Coord>();
                    foreach (var c in w.Cells) targets.Add(new Coord(c.X, c.Y));
                    foreach (var c in w.Triggered) targets.Add(c);
                    foreach (var c in w.DirectHits) targets.Add(c);
                    // 2026-11-18: yıldırım hedef taşın (patlayan) renginde; hücre bilgisi yoksa dalganın ilk rengi.
                    var colorAt = new Dictionary<int, Color>();
                    foreach (var c in w.Cells) colorAt[c.Y * 4096 + c.X] = LayerPalette.FrontColor(c.Removed);
                    Color fallback = w.Cells.Count > 0 ? (Color)LayerPalette.FrontColor(w.Cells[0].Removed) : new Color(0.75f, 0.55f, 1f);
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var t = targets[i];
                        if (t.X == w.FiredAt.X && t.Y == w.FiredAt.Y) continue;
                        var dst = StackAt(t.X, t.Y);
                        float life = SpecialDelay(w, t.X, t.Y) + 160f;
                        var boltCol = colorAt.TryGetValue(t.Y * 4096 + t.X, out var bc) ? bc : fallback;
                        var bolt = LightningBolt.Create(transform, from, dst.transform.position + dst.TopFaceLocal, _pitch, PeelDirector.PeelSortingOrder + 1000, 1000 + i * 7919, boltCol);
                        _fx.Add(new Fx { Kind = FxKind.Bolt, T = bolt.transform, Bolt = bolt, Owned = true, Dur = life });
                    }
                }
                return;
            }
            Flash(src, new Color(1f, 0.86f, 0.4f, 0.95f), 2.6f);
            // Roket(lar): satır roketi sağa-sola, sütun roketi yukarı-aşağı; ikili roket hepsi.
            bool row = w.Fired == SpecialKind.RocketRow || combo;
            bool col = w.Fired == SpecialKind.RocketColumn || combo;
            if (row)
            {
                Travel(src, w.FiredAt.X, w.FiredAt.Y, -1, 0);
                Travel(src, w.FiredAt.X, w.FiredAt.Y, 1, 0);
            }
            if (col)
            {
                Travel(src, w.FiredAt.X, w.FiredAt.Y, 0, -1);
                Travel(src, w.FiredAt.X, w.FiredAt.Y, 0, 1);
            }
            ShakeBoard(combo ? 0.04f : 0.022f, combo ? 420f : 280f);
        }

        /// <summary>Simgenin hayaleti büyüyüp söner; gerçek simge hemen kalkar.</summary>
        private void BurstToken(StackView s, bool ghost = true)
        {
            var sr = s.SpecialRenderer;
            if (ghost && sr != null && sr.gameObject.activeSelf)
            {
                var g = Ghost(sr, s.transform);
                g.transform.rotation = sr.transform.rotation;
                g.sortingOrder = 10 + _height * 4 + 9;
                _fx.Add(new Fx { Kind = FxKind.Burst, T = g.transform, R = g, Owned = true, BaseScale = g.transform.localScale, Dur = 300f });
            }
            s.SetSpecial(SpecialKind.None, default);
        }

        /// <summary>Kaynak hücreden tahta kenarına yuvarlanan rulo (hücre başına <see cref="RocketCellMs"/>).</summary>
        private void Travel(StackView src, int x, int y, int dx, int dy)
        {
            int steps = 0;
            int cx = x, cy = y;
            while (true)
            {
                int nx = cx + dx, ny = cy + dy;
                if (nx < 0 || ny < 0 || nx >= _width || ny >= _height) break;
                cx = nx; cy = ny; steps++;
            }
            var from = src.transform.position + src.TopFaceLocal;
            var end = CellCenter(cx, cy) + new Vector3(dx * _pitch * 0.9f, -dy * _rowPitch * 0.9f, 0f);
            var to = transform.TransformPoint(end);
            var go = new GameObject("RocketRoll");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            // 2026-11-08: yarılar yığınların ve soyulan katman parçalarının (PeelDirector 1000+) önünde uçsun.
            sr.sortingOrder = PeelDirector.PeelSortingOrder + 1000;
            if (SpecialArt.HasV11)
            {
                // Yarım simge: yatay sol/sağ yarı, dikey alt/üst yarı (ekranda dy > 0 aşağı). Tam simgenin tahtadaki
                // ölçüsüyle başlar; yarı, bütün simgedeki yerinden (± çeyrek uzunluk) ayrılır, döndürülmez.
                var kind = dx != 0 ? SpecialKind.RocketRow : SpecialKind.RocketColumn;
                int side = dx != 0 ? (dx < 0 ? -1 : 1) : (dy > 0 ? -1 : 1);
                go.name = "RocketHalf";
                sr.sprite = SpecialArt.Half(kind, side);
                var full = SpecialArt.For(kind).bounds.size;
                float k = _pitch * 1.5f / Mathf.Max(full.x, full.y); // 2026-11-08: daha büyük (önce 0,85)
                go.transform.localScale = Vector3.one * k;
                var dir = (to - from).normalized;
                from += dir * (Mathf.Max(full.x, full.y) * k * 0.25f * transform.lossyScale.x);
                // Arkasında altın parıltı (uçan yarı uzaktan seçilsin).
                var glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
                glow.transform.SetParent(go.transform, false);
                glow.sprite = SpecialArt.Glow;
                glow.color = new Color(1f, 0.86f, 0.4f, 0.75f);
                glow.sortingOrder = sr.sortingOrder - 1;
                FxKit.MakeAdditive(glow);
                glow.transform.localScale = Vector3.one * (Mathf.Max(full.x, full.y) * 0.9f / SpecialArt.Glow.bounds.size.x);
            }
            else
            {
                sr.sprite = SpecialArt.Rocket;
                go.transform.rotation = Quaternion.Euler(0f, 0f, dx != 0 ? (dx > 0 ? 0f : 180f) : (dy > 0 ? -90f : 90f));
                go.transform.localScale = Vector3.one * (_pitch * 0.8f / sr.sprite.bounds.size.x);
            }
            go.transform.position = from;
            _fx.Add(new Fx { Kind = FxKind.Travel, T = go.transform, R = sr, Owned = true, From = from, To = to, BaseScale = go.transform.localScale, Lead = SpecialArt.HasV11 ? SpecialLead : 0f, Dur = SpecialLead + (steps + 1) * RocketCellMs });
        }

        /// <summary>Hücrede kısa parlama (beyaz ya da renkli ışık).</summary>
        private void Flash(StackView s, Color color, float sizeCells)
        {
            var go = new GameObject("Flash");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpecialArt.Glow;
            sr.color = color;
            sr.sortingOrder = 10 + _height * 4 + 6;
            FxKit.MakeAdditive(sr); // 2026-11-18: parlayan ışık
            go.transform.position = s.transform.position + s.TopFaceLocal;
            go.transform.localScale = Vector3.one * (_pitch * sizeCells / sr.sprite.bounds.size.x);
            _fx.Add(new Fx { Kind = FxKind.Burst, T = go.transform, R = sr, Owned = true, BaseScale = go.transform.localScale, Dur = 360f });
            // Özel taş vuruşu: halka + kıvılcım + yıldız patlaması.
            if (!KatKat.UI.Motion.Reduced) ImpactBurst(go.transform.position, color, Mathf.Max(0.8f, sizeCells * 0.6f), PeelDirector.PeelSortingOrder + 1100);
        }

        /// <summary>Kaynaktan büyüyen ışık halkası (gökkuşağı).</summary>
        private void Expand(StackView s, float sizeCells, float ms)
        {
            var go = new GameObject("Wave");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpecialArt.Glow;
            sr.color = new Color(1f, 0.97f, 1f, 0.85f);
            sr.sortingOrder = 10 + _height * 4 + 6;
            FxKit.MakeAdditive(sr);
            go.transform.position = s.transform.position + s.TopFaceLocal;
            go.transform.localScale = Vector3.one * (_pitch * sizeCells / sr.sprite.bounds.size.x);
            _fx.Add(new Fx { Kind = FxKind.Expand, T = go.transform, R = sr, Owned = true, BaseScale = go.transform.localScale, Dur = ms });
            if (KatKat.UI.Motion.Reduced) return;
            // 2026-11-18: Altın Rulo dalgası — altın şok halkası, gökkuşağı renkli kıvılcım çemberi.
            var pos = go.transform.position;
            float sz = WorldPitch * sizeCells;
            int order = PeelDirector.PeelSortingOrder + 1100;
            Particles.Emit(FxKit.Ring, true, order, pos, Vector3.zero, ms / 1000f, sz * 0.1f, sz * 1.1f, new Color(1f, 0.9f, 0.55f, 1f), new Color(1f, 0.6f, 0.9f, 0f));
            Particles.Emit(FxKit.Ring, true, order, pos, Vector3.zero, ms / 1000f * 1.2f, sz * 0.05f, sz * 0.8f, new Color(1f, 1f, 1f, 0.7f), new Color(0.6f, 0.8f, 1f, 0f), delay: 0.1f);
            int n = 24;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                var c = Color.HSVToRGB(i / (float)n, 0.55f, 1f);
                Particles.Emit(FxKit.Star, true, order + 1, pos, dir * sz * R(0.9f, 1.3f), R(0.55f, 0.8f), WorldPitch * 0.5f, WorldPitch * 0.1f,
                    new Color(c.r, c.g, c.b, 1f), new Color(c.r, c.g, c.b, 0f), drag: 1.5f, rot: R(0f, 45f), rotVel: R(-120f, 120f));
            }
            ImpactBurst(pos, new Color(1f, 0.85f, 0.4f), 1.2f, order + 2);
        }

        private float _shakeAmp, _shakeDur, _shakeTime;
        private Vector3 _shakeBase;
        private bool _shaking;

        /// <summary>Tahtanın kısa sarsıntısı (dünya birimi; sönümlü). Büyük anlar için.</summary>
        public void ShakeBoard(float amplitude, float ms)
        {
            if (KatKat.UI.Motion.Reduced) return; // azaltılmış hareket: sarsıntı yok
            if (!_shaking) _shakeBase = transform.localPosition;
            _shaking = true;
            _shakeAmp = Mathf.Max(_shakeAmp * (1f - Mathf.Clamp01(_shakeTime / Mathf.Max(1f, _shakeDur))), amplitude * _pitch * 4f);
            _shakeDur = ms;
            _shakeTime = 0f;
        }

        /// <summary>
        /// Büyük grup patlaması (kullanıcı 2026-11-18: "büyük grup patlama olduğunda tepsinin arkasından her bir tarafından çıkan
        /// patlayan taş renginde ışıltılar olsun"): tepsinin dış kenarı boyunca, tepsinin arkasından (sıra 0; tepsi 2, gölge 1)
        /// dışa fırlayan dört kollu ışıltılar. Renk patlayan taşlardan (her ışıltı dalgadaki rastgele bir taşın rengi).
        /// </summary>
        /// <param name="tier">
        /// Kullanıcı 2026-11-18: "3 lü grup patlamada da yapalım. 3 lüde daha az olsun. 5 lide biraz daha çok ve arada hafif ateşler.
        /// 5 üzerinde grup patlamasında ise taş rengi ve alevler çıksın." → 1: 3–4'lü grup (az, küçük, kısa), 2: 5'li (daha çok +
        /// küçük ateş kıvılcımları), 3: 6+ grup ya da büyük özel taş hamlesi (en çok + tepsinin arkasından yükselen alevler).
        /// </param>
        public void TrayBurst(PeelWave wave, int tier = 3)
        {
            if (wave == null || wave.Cells.Count == 0 || _inShape.Length == 0 || tier <= 0) return;
            if (KatKat.UI.Motion.Reduced) return;
            // Dış kenar noktaları: şekil hücresinin şekil dışı (ya da tahta dışı) komşusuna bakan kenar ortası + dış normal.
            var edges = new List<(Vector3 p, Vector3 n)>();
            for (int y = 0; y < _height; y++)
            for (int x = 0; x < _width; x++)
            {
                if (!_inShape[y * _width + x]) continue;
                var c = CellCenter(x, y);
                if (!InShape(x - 1, y)) edges.Add((c + new Vector3(-_pitch * 0.5f, 0f, 0f), Vector3.left));
                if (!InShape(x + 1, y)) edges.Add((c + new Vector3(_pitch * 0.5f, 0f, 0f), Vector3.right));
                if (!InShape(x, y - 1)) edges.Add((c + new Vector3(0f, _rowPitch * 0.5f, 0f), Vector3.up));
                if (!InShape(x, y + 1)) edges.Add((c + new Vector3(0f, -_rowPitch * 0.5f, 0f), Vector3.down));
            }
            if (edges.Count == 0) return;
            int count = tier == 1 ? Mathf.Clamp(edges.Count, 12, 24) : tier == 2 ? Mathf.Clamp(edges.Count * 2, 28, 56) : Mathf.Clamp(edges.Count * 3, 40, 96);
            float sizeK = tier == 1 ? 0.65f : tier == 2 ? 0.85f : 1f;
            float travelK = tier == 1 ? 0.65f : tier == 2 ? 0.85f : 1f;
            float reach = TrayArt.Ready ? TrayArt.Reach * _pitch : 0.2f * _pitch;
            float wp = WorldPitch;
            // Parçacıklar eklemeli (parlak), tepsinin arkasında (sıra 0); dünya uzayında.
            for (int i = 0; i < count; i++)
            {
                var e = edges[(i * 7 + UnityEngine.Random.Range(0, 3)) % edges.Count];
                var along = new Vector3(-e.n.y, e.n.x, 0f) * UnityEngine.Random.Range(-0.45f, 0.45f) * _pitch;
                var spread = (e.n + along.normalized * UnityEngine.Random.Range(-0.5f, 0.5f)).normalized;
                var from = transform.TransformPoint(e.p + along + e.n * reach * 0.4f);
                var vel = transform.TransformDirection(spread) * wp * UnityEngine.Random.Range(2.4f, 4.6f) * travelK;
                Color col = LayerPalette.FrontColor(wave.Cells[UnityEngine.Random.Range(0, wave.Cells.Count)].Removed);
                col = Color.Lerp(col, Color.white, 0.2f); col.a = 1f;
                // 5'li: aralarda küçük ateş kıvılcımları (közler, yukarı süzülür).
                if (tier == 2 && i % 3 == 0) { EmitEmber(from, e.n, 0, UnityEngine.Random.Range(0f, 0.15f)); continue; }
                float sizeR = UnityEngine.Random.Range(0.9f, 1.5f) * sizeK;
                EmitTraySpark(from, vel, col, wp * sizeR * 0.8f, UnityEngine.Random.Range(0.65f, 0.95f) * (tier == 1 ? 0.8f : 1f));
            }
            if (tier >= 3)
            {
                // 6+: tepsinin kenarlarından saydam, yaygın ateş (yükselen puflar + közler).
                int fires = Mathf.Clamp(edges.Count / 2, 10, 22);
                for (int i = 0; i < fires; i++)
                {
                    var e = edges[(i * 11 + UnityEngine.Random.Range(0, 5)) % edges.Count];
                    var along = new Vector3(-e.n.y, e.n.x, 0f) * UnityEngine.Random.Range(-0.45f, 0.45f) * _pitch;
                    var pos = transform.TransformPoint(e.p + along + e.n * reach * 0.95f);
                    EmitFire(pos, e.n, 0, UnityEngine.Random.Range(0f, 0.25f));
                }
            }
        }

        private static Sprite _flame;

        /// <summary>Alev dili: altı yuvarlak, üstü sivri damla; içi sarı-beyaz, dışı turuncu-kırmızı (kendi renkli; ton beyaz).</summary>
        private static Sprite FlameSprite()
        {
            if (_flame != null) return _flame;
            const int W = 64, H = 128;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "tray_flame" };
            var p = new Color32[W * H];
            Color inner = new Color(1f, 0.95f, 0.6f), mid = new Color(1f, 0.62f, 0.12f), outer = new Color(0.95f, 0.25f, 0.05f);
            for (int j = 0; j < H; j++)
            for (int i = 0; i < W; i++)
            {
                float v = (j + 0.5f) / H;                       // 0 alt, 1 uç
                float u = (i + 0.5f) / W * 2f - 1f;
                // Genişlik: altta yuvarlak (v<0,3), yukarı doğru incelen dil.
                float half = v < 0.3f ? Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((0.3f - v) / 0.3f, 2f))) * 0.85f : 0.85f * Mathf.Pow(1f - (v - 0.3f) / 0.7f, 1.4f);
                float d = half <= 0.001f ? 9f : Mathf.Abs(u) / half;
                float body = Mathf.Pow(Mathf.Clamp01(1f - d), 1.3f), halo = 0.35f * Mathf.Clamp01(1f - d / 1.8f);
                float a = Mathf.Clamp01(Mathf.Max(body, halo)) * Mathf.Clamp01((1f - v) * 3f) * 0.9f;
                float heat = Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - v * 1.1f);
                Color c = heat > 0.5f ? Color.Lerp(mid, inner, (heat - 0.5f) * 2f) : Color.Lerp(outer, mid, heat * 2f);
                c.a = a;
                p[j * W + i] = c;
            }
            t.SetPixels32(p); t.Apply(false, true);
            return _flame = Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.1f), H);
        }

        private bool InShape(int x, int y) => x >= 0 && y >= 0 && x < _width && y < _height && _inShape[y * _width + x];

        private static Sprite _spark;

        /// <summary>Dört kollu ışıltı: ince ışınlar + yumuşak merkez parıltısı (beyaz; renk SpriteRenderer'dan).</summary>
        private static Sprite SparkSprite()
        {
            if (_spark != null) return _spark;
            const int N = 96;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "tray_spark" };
            var p = new Color32[N * N];
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                float dx = (i + 0.5f) / N * 2f - 1f, dy = (j + 0.5f) / N * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float glow = Mathf.Pow(Mathf.Clamp01(1f - r / 0.7f), 1.6f);
                float arms = Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(dx) / 0.1f) * Mathf.Clamp01(1f - Mathf.Abs(dy)),
                                       Mathf.Clamp01(1f - Mathf.Abs(dy) / 0.1f) * Mathf.Clamp01(1f - Mathf.Abs(dx)));
                float core = Mathf.Clamp01(1f - r / 0.16f);
                float a = Mathf.Clamp01(Mathf.Max(arms * arms, glow * 0.75f) + core);
                byte w = (byte)(Mathf.Lerp(0.75f, 1f, Mathf.Clamp01(core + arms)) * 255);
                p[j * N + i] = new Color32(w, w, w, (byte)(a * 255));
            }
            t.SetPixels32(p); t.Apply(false, true);
            return _spark = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
        }

        private void TickBoardShake(float ms)
        {
            if (!_shaking) return;
            _shakeTime += ms;
            float x = _shakeTime / _shakeDur;
            if (x >= 1f)
            {
                transform.localPosition = _shakeBase;
                _shaking = false;
                _shakeAmp = 0f;
                return;
            }
            float a = _shakeAmp * (1f - x);
            transform.localPosition = _shakeBase + new Vector3(Mathf.Sin(_shakeTime * 0.09f) * a, Mathf.Cos(_shakeTime * 0.11f) * a * 0.6f, 0f);
        }

        // ---- Ataş ve yıldız efektleri ---------------------------------------------------------------

        private void HitClip(StackView stack, int remaining)
        {
            var clip = stack.ClipRenderer;
            if (clip == null) return;
            // Sarmaşık (2026-11-18): her vuruşta yapraklar kopup saçılır; kalın → ince, son vuruşta dal kalkıp kaybolur.
            LeafBurst(clip.transform.position, stack.Size * Mathf.Max(1e-4f, transform.lossyScale.x), remaining > 0 ? 6 : 10, clip.sortingOrder + 4);
            if (remaining > 0)
            {
                stack.SetClipHits(remaining);
                _fx.Add(new Fx { Kind = FxKind.Pop, T = clip.transform, BaseScale = clip.transform.localScale, Dur = 220f });
                return;
            }
            // Kilit açıldı: ince sarmaşık hafifçe kalkar, döner ve kaybolur; yığın serbest kalır.
            var ghost = Ghost(clip, stack.transform);
            SwapState(ghost, SpecialArt.Clip(true));
            stack.SetClipHits(0);
            var from = ghost.transform.position;
            _fx.Add(new Fx { Kind = FxKind.Away, T = ghost.transform, R = ghost, Owned = true, From = from, To = from + new Vector3(stack.Size * 0.35f, stack.Size * 0.6f, 0f), BaseScale = ghost.transform.localScale, Dur = 300f });
        }

        /// <summary>Buz: vuruşta kısa sekme; kırılınca 4 buz parçası dağılır, alttaki yığın sağlam kalır (rehber §8).</summary>
        private void HitIce(StackView stack, int remaining)
        {
            var ice = stack.IceRenderer;
            if (remaining > 0)
            {
                stack.SetIceHits(remaining);
                if (ice != null) _fx.Add(new Fx { Kind = FxKind.Pop, T = ice.transform, BaseScale = ice.transform.localScale, Dur = 200f });
                Shards(stack, 2);
                return;
            }
            stack.SetIceHits(0);
            Shards(stack, 5);
        }

        private void Shards(StackView stack, int count)
        {
            var sprite = PremiumArt.Get("fx_diamond");
            if (sprite == null) return;
            var c = stack.transform.position + stack.TopFaceLocal;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("IceShard");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = new Color(0.75f, 0.92f, 1f, 1f);
                sr.sortingOrder = 10 + _height * 4 + 6;
                float ang = (i + 0.5f) / count * Mathf.PI * 2f + 0.4f;
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                go.transform.position = c + dir * stack.Size * 0.25f;
                float s = stack.Size * 0.22f / sprite.bounds.size.x;
                go.transform.localScale = Vector3.one * s;
                _fx.Add(new Fx { Kind = FxKind.Away, T = go.transform, R = sr, Owned = true, From = go.transform.position, To = go.transform.position + dir * stack.Size * 0.55f + Vector3.down * stack.Size * 0.15f, BaseScale = go.transform.localScale, Dur = 320f });
            }
        }

        public const float RotateMs = 420f;

        /// <summary>
        /// Döner taban (rehber §10 M08): taban düğmesi bir tur döner; yığın sıkışıp yan döner (takla), ortada yeni
        /// sıra çizilir ve tek sekmeyle oturur. Gerçek fizik değil, kurgulanmış hareket. Soyulma yok.
        /// </summary>
        private void PlayRotate(MoveResult r)
        {
            var stack = StackAt(r.Rotated.X, r.Rotated.Y);
            var layers = new List<LayerColor>(r.RotatedLayers);
            _fx.Add(new Fx { Kind = FxKind.Tumble, T = stack.transform, BaseScale = stack.transform.localScale, Dur = RotateMs });
            if (stack.RotorRenderer != null)
                _fx.Add(new Fx { Kind = FxKind.Spin, T = stack.RotorRenderer.transform, BaseScale = stack.RotorRenderer.transform.localScale, Dur = RotateMs });
            At(RotateMs * 0.5f, () => stack.SetLayers(layers));
        }

        /// <summary>Zarfın uçacağı dünya noktası (HUD zarf hedefi); atanmamışsa yukarı uçup kaybolur.</summary>
        public Func<Vector3> EnvelopeTarget { get; set; }

        /// <summary>Zarf: mühür seker, zarf kalkıp HUD zarf hedefine uçar; alttaki yığın sağlam kalır (rehber §8).</summary>
        private void OpenEnvelope(StackView stack)
        {
            var env = stack.EnvelopeRenderer;
            if (env == null) { stack.SetEnvelope(false); return; }
            var ghost = Ghost(env, stack.transform);
            SwapState(ghost, TileSkin.Sprite("mechanics/envelope-open"));
            ghost.sortingOrder = 10 + _height * 4 + 8;
            stack.SetEnvelope(false);
            var from = ghost.transform.position;
            var to = EnvelopeTarget != null ? EnvelopeTarget() : from + Vector3.up * 5f;
            // Uçuşta küçülür: HUD hedef simgesi boyuna yakın varsın.
            _fx.Add(new Fx { Kind = FxKind.Fly, T = ghost.transform, R = ghost, Owned = true, From = from, To = to, BaseScale = ghost.transform.localScale * 0.8f, Dur = StarFlight });
        }

        public const float StreakMs = 240f;

        /// <summary>Soyulan hücreden kilide rengin parıltısı: yay çizerek uçar, varınca söner.</summary>
        private void Streak(StackView from, StackView to, LayerColor color)
        {
            var sprite = PremiumArt.Get("fx_sparkle");
            if (sprite == null) return;
            var go = new GameObject("LockStreak");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            Color c = LayerPalette.FrontColor(color);
            sr.color = Color.Lerp(c, Color.white, 0.35f);
            sr.sortingOrder = 10 + _height * 4 + 7;
            var a = from.transform.position + from.TopFaceLocal;
            var b = to.transform.position + to.TopFaceLocal;
            go.transform.position = a;
            go.transform.localScale = Vector3.one * (from.Size * 0.32f / sprite.bounds.size.x);
            _fx.Add(new Fx { Kind = FxKind.Streak, T = go.transform, R = sr, Owned = true, From = a, To = b, BaseScale = go.transform.localScale, Dur = StreakMs });
        }

        /// <summary>Renk kilidi: sayaç düşer ve kilit seker; sıfırda asma kilit kalkıp kaybolur.</summary>
        private void HitLock(StackView stack, int remaining)
        {
            var lk = stack.LockRenderer;
            if (lk == null) { stack.SetLockRemaining(remaining); return; }
            if (remaining > 0)
            {
                stack.SetLockRemaining(remaining);
                _fx.Add(new Fx { Kind = FxKind.Pop, T = lk.transform, BaseScale = lk.transform.localScale, Dur = 220f });
                return;
            }
            var ghost = Ghost(lk, stack.transform);
            stack.SetLockRemaining(0);
            var from = ghost.transform.position;
            _fx.Add(new Fx { Kind = FxKind.Away, T = ghost.transform, R = ghost, Owned = true, From = from, To = from + new Vector3(stack.Size * 0.3f, stack.Size * 0.55f, 0f), BaseScale = ghost.transform.localScale, Dur = 320f });
        }

        private void LaunchStar(StackView stack, Vector3 target)
        {
            var star = stack.StarRenderer;
            if (star == null) return;
            var ghost = Ghost(star, stack.transform);
            ghost.sortingOrder = 10 + _height * 4 + 8;
            stack.SetStar(false);
            var from = ghost.transform.position;
            _fx.Add(new Fx { Kind = FxKind.Fly, T = ghost.transform, R = ghost, Owned = true, From = from, To = target, BaseScale = ghost.transform.localScale, Dur = StarFlight });
        }

        /// <summary>v4 durum çifti (kapalı → açık): aynı genişliğe ölçeklenerek değişir; açık sürüm yoksa aynen kalır.</summary>
        private static void SwapState(SpriteRenderer ghost, Sprite open)
        {
            if (open == null || ghost.sprite == null) return;
            float bottom = ghost.sprite.bounds.min.y * ghost.transform.localScale.y;
            float k = ghost.sprite.bounds.size.x / open.bounds.size.x;
            ghost.sprite = open;
            ghost.transform.localScale *= k;
            ghost.transform.localPosition += Vector3.up * (bottom - open.bounds.min.y * ghost.transform.localScale.y);
        }

        private SpriteRenderer Ghost(SpriteRenderer src, Transform parent)
        {
            var go = new GameObject(src.name + "Fx");
            go.transform.SetParent(transform, false);
            go.transform.position = src.transform.position;
            go.transform.localScale = src.transform.lossyScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = src.sprite;
            sr.sortingOrder = src.sortingOrder + 1;
            return sr;
        }

        private void TickFx(float ms)
        {
            for (int i = _fx.Count - 1; i >= 0; i--)
            {
                var f = _fx[i];
                f.Time += ms;
                float x = Mathf.Clamp01(f.Time / f.Dur);
                if (f.T == null) { _fx.RemoveAt(i); continue; }
                switch (f.Kind)
                {
                    case FxKind.Pop:
                        f.T.localScale = f.BaseScale * (1f + 0.22f * Mathf.Sin(x * Mathf.PI));
                        break;
                    case FxKind.Tumble:
                    {
                        // Yan dönüş: genişlik ortada sıfıra iner (yeni sıra o anda çizilir), sonra küçük taşmayla oturur.
                        float sx = x < 0.5f ? Mathf.Cos(x * Mathf.PI) : Mathf.Sin((x - 0.5f) * Mathf.PI) * (1f + 0.08f * Mathf.Sin((x - 0.5f) * 2f * Mathf.PI));
                        float sy = 1f - 0.12f * Mathf.Sin(x * Mathf.PI);
                        f.T.localScale = new Vector3(f.BaseScale.x * Mathf.Max(0.02f, sx), f.BaseScale.y * sy, 1f);
                        break;
                    }
                    case FxKind.Spin:
                        f.T.localRotation = Quaternion.Euler(0f, 0f, -360f * Ease.InOutQuad(x));
                        f.T.localScale = f.BaseScale * (1f + 0.15f * Mathf.Sin(x * Mathf.PI));
                        break;
                    case FxKind.Streak:
                    {
                        var p = Vector3.Lerp(f.From, f.To, Ease.InOutQuad(x));
                        p.y += Mathf.Sin(x * Mathf.PI) * 0.35f;
                        f.T.position = p;
                        f.T.localScale = f.BaseScale * Mathf.Lerp(1f, 0.55f, x);
                        f.T.localRotation = Quaternion.Euler(0f, 0f, 180f * x);
                        var col = f.R.color;
                        col.a = x < 0.8f ? 1f : (1f - x) / 0.2f;
                        f.R.color = col;
                        break;
                    }
                    case FxKind.Burst:
                    {
                        f.T.localScale = f.BaseScale * (1f + 0.7f * Ease.OutCubic(x));
                        var col = f.R.color;
                        col.a = (1f - x) * (f.R.sprite == SpecialArt.Glow ? 0.9f : 1f);
                        f.R.color = col;
                        break;
                    }
                    case FxKind.Expand:
                    {
                        f.T.localScale = f.BaseScale * Mathf.Lerp(0.15f, 1f, Ease.OutCubic(x));
                        var col = f.R.color;
                        col.a = 0.85f * (1f - x * x);
                        f.R.color = col;
                        break;
                    }
                    case FxKind.Travel:
                    {
                        // Öncü: yarılar birleşik simge olarak yerinde büyür (belirgin an); sonra ayrılıp kenara uçar, sonda söner.
                        if (f.Lead > 0f && f.Time < f.Lead)
                        {
                            float g = Mathf.Clamp01(f.Time / f.Lead);
                            f.T.position = f.From;
                            f.T.localScale = f.BaseScale * (0.8f + 0.5f * Ease.OutCubic(g) - 0.08f * Mathf.Sin(g * Mathf.PI));
                            var c0 = f.R.color; c0.a = 1f; f.R.color = c0;
                            break;
                        }
                        float m = f.Lead > 0f ? Mathf.Clamp01((f.Time - f.Lead) / Mathf.Max(1f, f.Dur - f.Lead)) : x;
                        f.T.position = Vector3.Lerp(f.From, f.To, m);
                        // 2026-11-18: roket izi (sıcak ışık pufu + geriye savrulan kıvılcımlar).
                        if (m > 0f && m < 0.95f && !KatKat.UI.Motion.Reduced) RocketTrail(f.T.position, (f.To - f.From).normalized, f.R.sortingOrder - 1);
                        var col = f.R.color;
                        col.a = m < 0.85f ? 1f : (1f - m) / 0.15f;
                        f.R.color = col;
                        f.T.localScale = f.BaseScale * ((f.Lead > 0f ? 1.3f : 1f) + 0.06f * Mathf.Sin(m * Mathf.PI * 6f));
                        break;
                    }
                    case FxKind.Bolt:
                        if (f.Bolt != null) f.Bolt.Tick(f.Time, x);
                        break;
                    case FxKind.Flame:
                    {
                        // Alev: yükselir, titrer (genişlik salınımı), uzayıp incelerek söner.
                        f.T.localPosition = Vector3.LerpUnclamped(f.From, f.To, Ease.OutCubic(x));
                        float flick = 1f + 0.18f * Mathf.Sin(x * 40f + f.Lead) + 0.08f * Mathf.Sin(x * 73f + f.Lead * 2f);
                        float grow = Mathf.Clamp01(x / 0.2f);
                        f.T.localScale = new Vector3(f.BaseScale.x * (0.5f + 0.5f * grow) * flick * (1f - 0.5f * x), f.BaseScale.y * (0.4f + 0.8f * grow) * (1f + 0.3f * x), 1f);
                        var fc = f.Tint; fc.a = x < 0.45f ? 1f : 1f - (x - 0.45f) / 0.55f;
                        f.R.color = fc;
                        break;
                    }
                    case FxKind.Spark:
                    {
                        // Tepsi arkasından dışa fırlayan ışıltı: hızlı çıkış, yavaşlama; önce büyür, sonra söner ve küçülür.
                        f.T.localPosition = Vector3.LerpUnclamped(f.From, f.To, Ease.OutCubic(x));
                        float grow = Mathf.Clamp01(x / 0.18f);
                        f.T.localScale = f.BaseScale * (0.4f + 0.8f * grow) * (1f - 0.55f * x);
                        f.T.localRotation = Quaternion.Euler(0f, 0f, 120f * x + f.Lead);
                        var sc = f.Tint; sc.a = f.Tint.a * (x < 0.55f ? 1f : 1f - (x - 0.55f) / 0.45f);
                        f.R.color = sc;
                        break;
                    }
                    case FxKind.Away:
                        f.T.position = Vector3.Lerp(f.From, f.To, Ease.OutCubic(x));
                        f.T.localRotation = Quaternion.Euler(0f, 0f, -40f * x);
                        f.R.color = new Color(1f, 1f, 1f, 1f - x * x);
                        break;
                    default:
                    {
                        // Yıldız: önce yerinde büyür, sonra yay çizerek hedefe uçar ve küçülür.
                        float rise = Mathf.Clamp01(x / 0.25f), fly = Mathf.Clamp01((x - 0.2f) / 0.8f);
                        var p = Vector3.Lerp(f.From, f.To, Ease.InOutQuad(fly));
                        p.y += Mathf.Sin(fly * Mathf.PI) * 0.6f;
                        f.T.position = p;
                        f.T.localScale = f.BaseScale * (1f + 0.6f * rise - 0.7f * fly * fly);
                        f.T.localRotation = Quaternion.Euler(0f, 0f, 160f * fly);
                        break;
                    }
                }
                _fx[i] = f;
                if (x >= 1f) EndFx(i);
            }
        }

        private void EndFx(int i)
        {
            var f = _fx[i];
            _fx.RemoveAt(i);
            if (f.T == null) return;
            if (f.Owned)
            {
                // Oynatma dışında (ekran yakalama) Destroy ertelenir ve nesne sahnede kalır.
                if (Application.isPlaying) Destroy(f.T.gameObject);
                else DestroyImmediate(f.T.gameObject);
            }
            else
            {
                f.T.localScale = f.BaseScale;
                if (f.Kind == FxKind.Spin) f.T.localRotation = Quaternion.identity;
            }
        }

        private void FinishPlay()
        {
            var done = _onDone;
            _onDone = null;
            done?.Invoke();
        }

        /// <summary>Geçersiz seçim: kısa sallanma. Hamle harcanmaz.</summary>
        public void ShakeCells(IEnumerable<Coord> cells)
        {
            foreach (var c in cells) _shakes.Add(new Shake { Stack = StackAt(c.X, c.Y), Start = Time.unscaledTime });
        }

        /// <summary>Rulo önizlemesi: satırda soyulacak hücrelerin altına ince işaret.</summary>
        public void ShowRowPreview(int row, IList<int> columns, Sprite mark, Color color)
        {
            HideRowPreview();
            if (row < 0) return;
            for (int i = 0; i < columns.Count; i++)
            {
                if (_rowMarks.Count <= i)
                {
                    var go = new GameObject("RowMark");
                    go.transform.SetParent(transform, false);
                    var sr = go.AddComponent<SpriteRenderer>();
                    _rowMarks.Add(sr);
                }
                var m = _rowMarks[i];
                m.gameObject.SetActive(true);
                m.sprite = mark;
                m.color = color;
                m.sortingOrder = 10 + _height * 4 + 2;
                m.transform.localPosition = CellCenter(columns[i], row) + new Vector3(0f, _pitch * 0.05f, 0f);
                float size = _pitch * 0.98f / (mark.rect.width / mark.pixelsPerUnit);
                m.transform.localScale = new Vector3(size, size, 1f);
            }
        }

        public void HideRowPreview()
        {
            foreach (var m in _rowMarks) m.gameObject.SetActive(false);
        }

        // ---- vr1 P0: ipucu grubunun ince beyaz-altın sınırı (01_IPUCU "Gösterim": semboller kapanmaz) ----------

        private readonly List<SpriteRenderer> _hintMarks = new List<SpriteRenderer>();
        private static Sprite _hintFrame;

        /// <summary>Önerilen dokunuşun grubunun her hücresine ince sınır (iç saydam; taş sembolü görünür kalır).</summary>
        public void ShowHintGroup(IList<Coord> cells)
        {
            HideHintGroup();
            if (cells == null) return;
            var frame = HintFrame;
            for (int i = 0; i < cells.Count; i++)
            {
                if (_hintMarks.Count <= i)
                {
                    var go = new GameObject("HintGroupMark");
                    go.transform.SetParent(transform, false);
                    _hintMarks.Add(go.AddComponent<SpriteRenderer>());
                }
                var m = _hintMarks[i];
                m.gameObject.SetActive(true);
                m.sprite = frame;
                m.color = new Color(1f, 0.93f, 0.62f, 0.95f);
                m.sortingOrder = 895;
                m.transform.localPosition = CellCenter(cells[i].X, cells[i].Y) + new Vector3(0f, _pitch * 0.04f, 0f);
                float size = _pitch * 0.96f / (frame.rect.width / frame.pixelsPerUnit);
                m.transform.localScale = new Vector3(size, size, 1f);
            }
        }

        public void HideHintGroup()
        {
            foreach (var m in _hintMarks) if (m != null) m.gameObject.SetActive(false);
        }

        /// <summary>Testler: görünen ipucu grubu işaret sayısı.</summary>
        public int HintGroupMarks { get { int n = 0; foreach (var m in _hintMarks) if (m != null && m.gameObject.activeSelf) n++; return n; } }

        /// <summary>Yuvarlak köşeli ince çerçeve (64 px, 4 px kenar, yumuşak).</summary>
        private static Sprite HintFrame
        {
            get
            {
                if (_hintFrame) return _hintFrame;
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "hint-frame", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                var px = new Color32[n * n];
                const float r = 12f, w = 4f;
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float fx = Mathf.Abs(x + 0.5f - n * 0.5f) - (n * 0.5f - r - 1f), fy = Mathf.Abs(y + 0.5f - n * 0.5f) - (n * 0.5f - r - 1f);
                    float d = new Vector2(Mathf.Max(fx, 0f), Mathf.Max(fy, 0f)).magnitude + Mathf.Min(Mathf.Max(fx, fy), 0f) - r; // köşeli kutuya uzaklık
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d + w * 0.5f) / (w * 0.5f + 0.5f));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(255f * a));
                }
                tex.SetPixels32(px);
                tex.Apply();
                return _hintFrame = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            }
        }

        /// <summary>Animasyonu atlar; çağıran ardından Sync ile mantıksal son durumu çizer.</summary>
        public void CompleteImmediately()
        {
            _timeline.Clear();
            _director?.CompleteAll();
            foreach (var s in _shakes) if (s.Stack) s.Stack.transform.localRotation = Quaternion.identity;
            _shakes.Clear();
            while (_fx.Count > 0) EndFx(_fx.Count - 1);
            if (_shaking) { transform.localPosition = _shakeBase; _shaking = false; _shakeAmp = 0f; }
            if (_onDone != null) FinishPlay();
        }

        private void At(float ms, Action a)
        {
            _timeline.Add(new TimedAction { Time = _clock + ms, Action = a });
        }

        private void Update()
        {
            float scale = _director != null ? _director.TimeScale : 1f;
            Advance(Time.unscaledDeltaTime * 1000f * scale);
            TickShakes();
        }

        /// <summary>Olay planı zaman çizgisini verilen süre (ms) kadar ilerletir.</summary>
        public void Advance(float ms)
        {
            // ms gerçek süre; olay planı Motion.Pace ile yavaşlar, parçacıklar gerçek zamanla (2026-11-18).
            Particles.Tick(ms); // parçacıklar (FxKit); erişim Active'i de bu tahtaya ayarlar
            ms *= KatKat.UI.Motion.Pace;
            if (_fx.Count > 0) TickFx(ms);
            TickBoardShake(ms);
            if (_timeline.Count > 0)
            {
                _clock += ms;
                // Sıra korunur: aynı zamandakiler eklendikleri sırayla.
                for (int i = 0; i < _timeline.Count;)
                {
                    if (_timeline[i].Time <= _clock)
                    {
                        var a = _timeline[i].Action;
                        _timeline.RemoveAt(i);
                        a();
                    }
                    else i++;
                }
            }
            else _clock = 0f;
        }

        private void TickShakes()
        {
            for (int i = _shakes.Count - 1; i >= 0; i--)
            {
                var s = _shakes[i];
                float x = (Time.unscaledTime - s.Start) / 0.22f;
                if (!s.Stack || x >= 1f)
                {
                    if (s.Stack) s.Stack.transform.localRotation = Quaternion.identity;
                    _shakes.RemoveAt(i);
                    continue;
                }
                float a = Mathf.Sin(x * Mathf.PI * 4f) * (1f - x) * 7f;
                s.Stack.transform.localRotation = Quaternion.Euler(0f, 0f, a);
            }
        }

        private struct TimedAction
        {
            public float Time;
            public Action Action;
        }

        private struct Shake
        {
            public StackView Stack;
            public float Start;
        }

        private enum FxKind { Pop, Away, Fly, Streak, Tumble, Spin, Burst, Expand, Travel, Bolt, Spark, Flame }

        private struct Fx
        {
            public FxKind Kind;
            public Transform T;
            public SpriteRenderer R;
            public bool Owned;
            public Vector3 From, To, BaseScale;
            public float Time, Dur;
            /// <summary>Travel: hareketten önce yerinde büyüme süresi (ms). Bolt: yıldırım nesnesi.</summary>
            public float Lead;
            public LightningBolt Bolt;
            /// <summary>Spark: ışıltı rengi.</summary>
            public Color Tint;
        }
    }
}
