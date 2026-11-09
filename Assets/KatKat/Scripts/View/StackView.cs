using System.Collections.Generic;
using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Bir hücredeki katman yığınının statik görünümü. Bütün katmanlar tek ağda çizilir:
    /// her katman için koyu kenar şeridi + işaretli üst yüz; alt katmanlar dar şerit olarak görünür.
    /// Kök = hücre merkezi; "body" alt kenardan esnetilir (sıkışma/sekme).
    /// </summary>
    public sealed class StackView : MonoBehaviour
    {
        // v10: 0.084 → 0.10 (alt katman şeritleri daha geniş görünsün; tahta düzeni yığın yüksekliğinden hesaplanır).
        public const float ThicknessRatio = 0.10f;
        /// <summary>Tahta tepsisindeki boş hücre yuvası ve yığın altı temas gölgesi: temadan (BoardView.SetTheme).</summary>
        private Color32 _slot = ThemeDefinition.Coast.Slot, _contact = ThemeDefinition.Coast.ContactShadow;
        private Mesh _shadowMesh;

        public void SetPalette(Color32 slot, Color32 contact)
        {
            _slot = slot;
            _contact = contact;
            if (_shadowMesh != null) ColorShadow();
            Rebuild();
        }

        private void ColorShadow()
        {
            var b = new LayerMeshBuilder();
            b.AddQuad(Vector2.zero, Vector2.one, _contact, Vector2.zero, 0f, 1f, LayerMeshBuilder.Flat);
            b.ApplyTo(_shadowMesh);
        }
        private const float RimDrop = 0.8f;

        private readonly List<LayerColor> _layers = new List<LayerColor>(4);
        private readonly LayerMeshBuilder _builder = new LayerMeshBuilder();
        private Transform _body;
        private Mesh _mesh;
        private MeshRenderer _renderer;
        private MeshRenderer _shadow;
        private LayerColor? _detachedRim;
        private object _rimOwner;
        private object _bodyOwner;
        private bool _showBase;
        private SpriteRenderer _clip;
        private readonly SpriteRenderer[] _dots = new SpriteRenderer[2];
        private SpriteRenderer _star;
        private int _clipHits;
        private bool _hasStar;
        // Premium-v2 örtüleri: buz (cam çerçeve + soluk dolgu), renk kilidi (asma kilit + hedef renk taşı + sayaç).
        private SpriteRenderer _iceFill, _ice, _lock, _lockRibbon, _lockChip, _envelope, _envelopeSeal, _envelopeDisc;
        private bool _hasEnvelope;
        private SpriteRenderer _rotor;
        private bool _hasRotor, _rotorActive;
        private LayerColor _envelopeColor;
        private TextMesh _lockCount, _lockCountShadow;
        private int _iceHits, _lockRemaining;
        private LayerColor _lockColor;

        public float Size { get; private set; }
        public int ClipHits => _clipHits;
        public bool HasStar => _hasStar;
        public SpriteRenderer ClipRenderer => _clip;
        public SpriteRenderer StarRenderer => _star;
        /// <summary>Dört katmanlı tahtada toplam yükseklik üç katmanı geçmesin diye katman inceltilir.</summary>
        public float Thickness => Size * ThicknessRatio * _thicknessScale;
        private float _thicknessScale = 1f;

        /// <param name="maxLayers">Tahtadaki en yüksek yığın; 4 ise katmanlar 2/3 kalınlıkta.</param>
        /// <summary>Katman kalınlığı çarpanı: dört katmanlı tahtada 2/3.</summary>
        public static float ThicknessScale(int maxLayers) => maxLayers > 3 ? 2f / (maxLayers - 1) : 1f;

        public void SetMaxLayers(int maxLayers)
        {
            float s = ThicknessScale(maxLayers);
            if (Mathf.Approximately(s, _thicknessScale)) return;
            _thicknessScale = s;
            Rebuild();
        }
        public IReadOnlyList<LayerColor> Layers => _layers;
        public int SortingOrder => _renderer.sortingOrder;

        public void Init(float size, int sortingOrder, bool showBase = true)
        {
            Size = size;
            _showBase = showBase;
            if (_body == null)
            {
                _body = new GameObject("Body").transform;
                _body.SetParent(transform, false);
                _mesh = new Mesh { name = "StackMesh" };
                _mesh.MarkDynamic();
                _body.gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
                _renderer = _body.gameObject.AddComponent<MeshRenderer>();
                _renderer.sharedMaterial = ProceduralArt.LayerMaterial;

                var sh = new GameObject("Shadow");
                sh.transform.SetParent(transform, false);
                _shadowMesh = new Mesh { name = "StackShadow" };
                ColorShadow();
                sh.AddComponent<MeshFilter>().sharedMesh = _shadowMesh;
                _shadow = sh.AddComponent<MeshRenderer>();
                _shadow.sharedMaterial = ProceduralArt.ShadowMaterial;
            }
            _body.localPosition = new Vector3(0f, -size * 0.5f, 0f);
            _body.localScale = Vector3.one;
            _renderer.sortingOrder = sortingOrder;
            _shadow.sortingOrder = sortingOrder - 1;
            Rebuild();
        }

        public void SetLayers(IEnumerable<LayerColor> bottomToTop)
        {
            _layers.Clear();
            _layers.AddRange(bottomToTop);
            _detachedRim = null;
            _rimOwner = null;
            _bodyOwner = null;
            if (_body != null) _body.localScale = Vector3.one;
            Rebuild();
        }

        /// <summary>Kök uzayında k. katmanın üst yüz merkezi (gövde ölçeği 1 iken).</summary>
        public Vector3 FaceCenterLocal(int k) => new Vector3(0f, (k - 1) * Thickness, 0f);

        /// <summary>Üst katmanı soyulma ağına devretmek için yığından ayırır; kenar şeridi bir süre kalır.</summary>
        public LayerColor DetachTop(object owner)
        {
            var top = _layers[_layers.Count - 1];
            _layers.RemoveAt(_layers.Count - 1);
            _detachedRim = top;
            _rimOwner = owner;
            _bodyOwner = owner;
            Rebuild();
            return top;
        }

        public void ReleaseDetachedRim(object owner)
        {
            if (_detachedRim == null || _rimOwner != owner) return;
            _detachedRim = null;
            _rimOwner = null;
            Rebuild();
        }

        /// <summary>Gövdeyi alt kenardan esnetir (x, y ölçek).</summary>
        /// <remarks>Aynı yığında iki soyulma örtüşürse (zincir) gövdeyi en son başlayan yönetir.</remarks>
        public void SetBodyScale(object owner, Vector2 s)
        {
            if (owner != _bodyOwner) return;
            _body.localScale = new Vector3(s.x, s.y, 1f);
        }

        public Vector3 BodyLocalToRoot(Vector3 bodyLocal)
        {
            return _body.localPosition + Vector3.Scale(bodyLocal, _body.localScale);
        }

        /// <summary>Ataş (kalan hasar noktalarıyla) ve dip yıldızı işaretleri; mantıksal durumdan.</summary>
        public void SetMarks(int clipHits, bool star)
        {
            _clipHits = clipHits;
            _hasStar = star;
            UpdateMarks();
        }

        public int IceHits => _iceHits;
        public int LockRemaining => _lockRemaining;
        public SpriteRenderer IceRenderer => _ice;
        public SpriteRenderer LockRenderer => _lock;

        public SpriteRenderer EnvelopeRenderer => _envelope;

        /// <summary>Buz, renk kilidi ve zarf örtüleri; mantıksal durumdan.</summary>
        public void SetCovers(int iceHits, LayerColor lockColor, int lockRemaining, bool envelope = false, LayerColor envelopeColor = default)
        {
            _iceHits = iceHits;
            _lockColor = lockColor;
            _lockRemaining = lockRemaining;
            _hasEnvelope = envelope;
            _envelopeColor = envelopeColor;
            UpdateMarks();
        }

        public bool HasRotor => _hasRotor;
        public SpriteRenderer RotorRenderer => _rotor;

        /// <summary>Döner taban düğmesinin kök uzayındaki yeri: yığının sağ alt köşesi.</summary>
        public Vector3 RotorLocal => new Vector3(Size * 0.36f, -Size * 0.38f, 0f);

        /// <param name="active">Döndürülebilir mi (iki+ katman, örtüsüz); değilse düğme soluk.</param>
        public void SetRotor(bool has, bool active)
        {
            _hasRotor = has;
            _rotorActive = active;
            UpdateMarks();
        }

        public void SetEnvelope(bool on)
        {
            _hasEnvelope = on;
            UpdateMarks();
        }

        public void SetIceHits(int hits)
        {
            _iceHits = hits;
            UpdateMarks();
        }

        public void SetLockRemaining(int remaining)
        {
            _lockRemaining = remaining;
            UpdateMarks();
        }

        public void SetClipHits(int clipHits)
        {
            _clipHits = clipHits;
            UpdateMarks();
        }

        public void SetStar(bool star)
        {
            _hasStar = star;
            UpdateMarks();
        }

        // ---- M09–M11 görselleri -----------------------------------------------------------------------

        private SpriteRenderer _sponge, _button, _ghost;
        private SpriteRenderer[] _spongeSlots;
        private int _spongeNeed, _spongeHave;
        private static readonly Color[] LinkTints = { new Color(0.55f, 0.9f, 0.95f), new Color(1f, 0.72f, 0.45f), new Color(0.95f, 0.6f, 0.85f), new Color(0.7f, 0.95f, 0.6f) };

        public SpriteRenderer SpongeRenderer => _sponge;
        public SpriteRenderer ButtonRenderer => _button;

        /// <summary>Sünger yastığı + gereken renk yuvaları (dolu = renkli disk, boş = soluk halka).</summary>
        public void SetSponge(int need, int have)
        {
            _spongeNeed = need;
            _spongeHave = have;
            if (need == 0)
            {
                if (_sponge != null) _sponge.gameObject.SetActive(false);
                if (_spongeSlots != null) foreach (var s in _spongeSlots) s.gameObject.SetActive(false);
                return;
            }
            if (_sponge == null)
            {
                _sponge = Mark("Sponge", SpecialArt.Sponge, 2);
                _spongeSlots = new SpriteRenderer[4];
                for (int i = 0; i < 4; i++) _spongeSlots[i] = Mark("SpongeSlot", SpecialArt.Disc, 3);
            }
            _sponge.gameObject.SetActive(true);
            _sponge.sortingOrder = _renderer.sortingOrder + 2;
            var face = TopFaceLocal;
            FitOnFace(_sponge, face, Vector2.zero, 1.0f);
            int count = 0;
            for (int c = 0; c < 4; c++) if ((need & (1 << c)) != 0) count++;
            int k = 0;
            for (int c = 0; c < 4; c++)
            {
                var slot = _spongeSlots[c];
                bool needed = (need & (1 << c)) != 0;
                slot.gameObject.SetActive(needed);
                if (!needed) continue;
                bool full = (have & (1 << c)) != 0;
                Color col = LayerPalette.FrontColor((LayerColor)c);
                slot.color = full ? col : new Color(col.r, col.g, col.b, 0.35f);
                slot.sortingOrder = _renderer.sortingOrder + 3;
                float x = (k - (count - 1) * 0.5f) * 0.16f;
                slot.transform.localPosition = face + new Vector3(Size * x, -Size * 0.37f, 0f);
                slot.transform.localScale = Vector3.one * (Size * (full ? 0.094f : 0.072f) / slot.sprite.bounds.size.x);
                k++;
            }
        }

        /// <summary>M10: bağlı ataş = ataş görseli, gruba göre renk tonu (aynı renk aynı grup).</summary>
        public void SetLinkTint(int linkId)
        {
            if (_clip == null) return;
            _clip.color = linkId > 0 ? LinkTints[(linkId - 1) % LinkTints.Length] : Color.white;
        }

        /// <summary>M11: dipteki düğme (yığının sol alt köşesi).</summary>
        public void SetButton(bool has)
        {
            if (!has) { if (_button != null) _button.gameObject.SetActive(false); return; }
            if (_button == null) _button = Mark("Button", SpecialArt.Button, 6);
            _button.gameObject.SetActive(true);
            _button.sortingOrder = _renderer.sortingOrder + 6;
            _button.transform.localPosition = new Vector3(-Size * 0.3f, -Size * 0.34f, 0f);
            _button.transform.localScale = Vector3.one * (Size * 0.5f / _button.sprite.bounds.size.x);
        }

        private SpriteRenderer _crystal, _crystalGlow;
        public bool IsProtected => _crystal != null && _crystal.gameObject.activeSelf;

        /// <summary>
        /// v12 deneme "koruma": kristal yığın (bir katmanı bile soyulursa bölüm kaybedilir). Üst yüzün sol üst köşesinde
        /// kristal rozeti ve yığının çevresinde buz mavisi parıltı. Katman çizimi değişmez.
        /// </summary>
        public void SetProtected(bool on)
        {
            if (!on)
            {
                if (_crystal != null) _crystal.gameObject.SetActive(false);
                if (_crystalGlow != null) _crystalGlow.gameObject.SetActive(false);
                return;
            }
            if (_crystalGlow == null) _crystalGlow = Mark("CrystalGlow", SpecialArt.Glow, -1);
            _crystalGlow.gameObject.SetActive(true);
            _crystalGlow.color = new Color(0.55f, 0.9f, 1f, 0.7f);
            _crystalGlow.sortingOrder = _renderer.sortingOrder - 1;
            _crystalGlow.transform.localPosition = TopFaceLocal * 0.5f;
            _crystalGlow.transform.localScale = Vector3.one * (Size * 1.5f / _crystalGlow.sprite.bounds.size.x);
            if (_crystal == null) _crystal = Mark("Crystal", KatKat.UI.RuleIcons.For("protect"), 9);
            _crystal.gameObject.SetActive(true);
            _crystal.sortingOrder = _renderer.sortingOrder + 9;
            _crystal.transform.localPosition = TopFaceLocal + new Vector3(-Size * 0.3f, Size * 0.3f, 0f);
            _crystal.transform.localScale = Vector3.one * (Size * 0.42f / _crystal.sprite.bounds.size.x);
        }

        /// <summary>M11 geçit hayaleti: henüz açılmamış hücrede gelecek üst rengin soluk karesi.</summary>
        public readonly List<LayerColor> GhostLayers = new List<LayerColor>();

        public void SetGhost(bool on, IList<LayerColor> layers = null)
        {
            GhostLayers.Clear();
            if (!on || layers == null || layers.Count == 0) { if (_ghost != null) _ghost.gameObject.SetActive(false); return; }
            GhostLayers.AddRange(layers);
            var top = layers[layers.Count - 1];
            if (_ghost == null) _ghost = Mark("Ghost", SpecialArt.Square, 1);
            _ghost.gameObject.SetActive(true);
            Color c = LayerPalette.FrontColor(top);
            _ghost.color = new Color(c.r, c.g, c.b, 0.32f);
            _ghost.transform.localPosition = Vector3.zero;
            _ghost.transform.localScale = Vector3.one * (Size * 0.92f / _ghost.sprite.bounds.size.x);
        }

        // ---- v5 özel taş simgesi ------------------------------------------------------------------------

        private SpriteRenderer _special;
        private SpecialKind _specialKind;
        private float _specialClock, _popT = 10f;

        /// <summary>Doğma animasyonunu başlatır (simge küçükten büyüyüp yaylanarak oturur).</summary>
        public void PopSpecial() => _popT = 0f;

        public SpecialKind Special => _specialKind;
        public SpriteRenderer SpecialRenderer => _special;

        /// <summary>Üst yüzde duran özel taş: roket (sütun roketi 90° döner) ya da gökkuşağı (göbek = grubun rengi).</summary>
        public void SetSpecial(SpecialKind kind, LayerColor color)
        {
            _specialKind = kind;
            if (kind == SpecialKind.None)
            {
                if (_special != null) _special.gameObject.SetActive(false);
                return;
            }
            if (_special == null) _special = Mark("Special", SpecialArt.Rocket, 8);
            _special.gameObject.SetActive(true);
            _special.sprite = SpecialArt.For(kind);
            _special.sortingOrder = _renderer.sortingOrder + 8;
            _special.color = Color.white;
            Vector2 offset = TileSkin.SpecialOffset(kind);
            _special.transform.localPosition = TopFaceLocal + (Vector3)(offset * Size);
            _special.transform.localRotation = Quaternion.Euler(0f, 0f, TileSkin.SpecialAngle(kind));
            _special.transform.localScale = Vector3.one * SpecialScale();
        }

        /// <summary>Boşta özel taş: hafif nefes (ölçek) ve gökkuşağında yavaş dönüş; oyuncunun gözü takılsın.</summary>
        private void LateUpdate()
        {
            if (_special == null || !_special.gameObject.activeSelf) return;
            _specialClock += Time.unscaledDeltaTime;
            _popT += Time.unscaledDeltaTime;
            float baseScale = SpecialScale();
            float k = 1f + 0.012f * Mathf.Sin(_specialClock * 2f * Mathf.PI / 1.6f);
            // Doğma: 0 → 1.25 → 1 (420 ms, yaylı).
            if (_popT < 0.42f)
            {
                float x = _popT / 0.42f;
                k *= x < 0.55f ? Mathf.Lerp(0f, 1.04f, Ease.OutCubic(x / 0.55f)) : Mathf.Lerp(1.04f, 1f, Ease.InOutQuad((x - 0.55f) / 0.45f));
            }
            _special.transform.localScale = Vector3.one * baseScale * k;
        }

        private float SpecialScale()
        {
            Vector2 box = TileSkin.SpecialBox(_specialKind);
            Vector3 bounds = _special.sprite.bounds.size;
            return Size * Mathf.Min(box.x / bounds.x, box.y / bounds.y);
        }

        /// <summary>Üst yüzün kök uzayındaki merkezi (yığın boşsa taban).</summary>
        public Vector3 TopFaceLocal => new Vector3(0f, (_layers.Count - 2) * Thickness, 0f);

        /// <summary>Yıldız rozetinin kök uzayındaki yeri: yığının ön alt kenarı, boşken taban ortası.</summary>
        public Vector3 StarLocal => _layers.Count == 0 ? TopFaceLocal : new Vector3(Size * 0.3f, -Size * 0.38f, 0f);

        private SpriteRenderer Mark(string name, Sprite sprite, int orderOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = _renderer.sortingOrder + orderOffset;
            return sr;
        }

        private void UpdateMarks()
        {
            if (_renderer == null) return;
            if (_clipHits > 0 && _clip == null)
            {
                _clip = Mark("Clip", SpecialArt.Clip(false), 1);
                for (int i = 0; i < _dots.Length; i++) _dots[i] = Mark("ClipDot", ProceduralArt.DotSprite, 2);
            }
            if (_hasStar && _star == null) _star = Mark("Star", ProceduralArt.StarSprite, 2);

            var face = TopFaceLocal;
            if (_clip != null)
            {
                _clip.gameObject.SetActive(_clipHits > 0);
                _clip.sortingOrder = _renderer.sortingOrder + 1;
                // v4 ataş kıskacı: renk kilidinin altın asma kilidiyle karışmasın diye büyük ve gümüş, yüzün üst yarısında.
                FitOnFace(_clip, face, TileSkin.ClipOffset, TileSkin.ClipWidth);
                _clip.color = Color.white;
                for (int i = 0; i < _dots.Length; i++)
                {
                    var d = _dots[i];
                    d.gameObject.SetActive(i < _clipHits);
                    d.sortingOrder = _renderer.sortingOrder + 2;
                    float off = _clipHits == 2 ? (i == 0 ? -0.11f : 0.11f) : 0f;
                    d.transform.localPosition = face + new Vector3(Size * off, -Size * 0.33f, 0f);
                    d.transform.localScale = Vector3.one * Size * 0.2f;
                }
            }
            UpdateIce(face);
            UpdateLock(face);
            UpdateEnvelope(face);
            UpdateRotor();
            if (_star != null)
            {
                _star.gameObject.SetActive(_hasStar);
                _star.sortingOrder = _renderer.sortingOrder + 5; // alttaki satırın yığını rozeti örtmesin
                _star.transform.localPosition = StarLocal;
                _star.transform.localScale = Vector3.one * Size * (_layers.Count == 0 ? 0.55f : 0.42f);
                _star.color = Color.white;
            }
        }

        /// <summary>Sprite'ı üst yüze, verilen genişlik oranında yerleştirir (sprite boyutundan bağımsız).</summary>
        private void FitOnFace(SpriteRenderer sr, Vector3 face, Vector2 offset, float widthRatio)
        {
            float w = sr.sprite.bounds.size.x;
            sr.transform.localPosition = face + new Vector3(Size * offset.x, Size * offset.y, 0f);
            sr.transform.localScale = Vector3.one * (Size * widthRatio / w);
        }

        private void UpdateIce(Vector3 face)
        {
            bool on = _iceHits > 0;
            // v5: buz kodla çizilen, taş yüzüyle aynı yuvarlatılmış kare blok (kullanıcı: "buz efekti tam oturmamış";
            // paketteki kabuk görselinin saydam kenarı yüzden küçük bir çıkartma gibi duruyordu). Taş rengi altından okunur;
            // iki vuruşlu buz daha opak, tek vuruşu kalan buz çatlaklı. Mantıksal kutu değişmedi.
            if (on && _ice == null) _ice = Mark("Ice", SpecialArt.Ice(false), 2);
            if (_iceFill != null) _iceFill.gameObject.SetActive(false);
            if (_ice == null) return;
            _ice.gameObject.SetActive(on);
            if (!on) return;
            _ice.sortingOrder = _renderer.sortingOrder + 2;
            _ice.sprite = SpecialArt.Ice(_iceHits < 2);
            _ice.color = Color.white;
            FitOnFace(_ice, face, Vector2.zero, TileSkin.IceWidth);
        }

        private void UpdateLock(Vector3 face)
        {
            bool on = _lockRemaining > 0;
            if (on && _lock == null)
            {
                // v4 montajı: ayrı kilit kayışı (arkada) + ayrı altın asma kilit (önde). Yoksa v2 birleşik görsel.
                var ribbon = TileSkin.Sprite("mechanics/lock-ribbon");
                var padlock = PremiumArt.V4("icons/lock");
                if (ribbon != null && padlock != null)
                {
                    _lockRibbon = Mark("LockRibbon", ribbon, 2);
                    _lock = Mark("Lock", padlock, 3);
                }
                else _lock = Mark("Lock", PremiumArt.Get("ov_lock") ?? ProceduralArt.ClipSprite, 2);
                _lockChip = Mark("LockColor", PremiumArt.Chip(_lockColor) ?? ProceduralArt.StarSprite, 4);
                _lockCountShadow = Count("LockCountShadow", new Color(0.29f, 0.21f, 0.18f, 1f), 5);
                _lockCount = Count("LockCount", Color.white, 6);
            }
            if (_lock == null) return;
            _lock.gameObject.SetActive(on);
            if (_lockRibbon != null) _lockRibbon.gameObject.SetActive(on);
            _lockChip.gameObject.SetActive(on);
            _lockCount.gameObject.SetActive(on);
            _lockCountShadow.gameObject.SetActive(on);
            if (!on) return;
            _lock.color = Color.white;
            if (_lockRibbon != null)
            {
                _lockRibbon.sortingOrder = _renderer.sortingOrder + 2;
                FitOnFace(_lockRibbon, face, TileSkin.RibbonOffset, TileSkin.RibbonWidth);
                _lock.sortingOrder = _renderer.sortingOrder + 3;
                FitOnFace(_lock, face, TileSkin.LockOffset, TileSkin.LockWidth);
            }
            else
            {
                _lock.sortingOrder = _renderer.sortingOrder + 2;
                FitOnFace(_lock, face, new Vector2(0f, 0.04f), 0.98f);
            }
            // Hedef renk taşı + kalan sayı sağ altta: kilit rengi şekil işaretiyle de okunur (rehber §7).
            _lockChip.sprite = PremiumArt.Chip(_lockColor) ?? _lockChip.sprite;
            _lockChip.sortingOrder = _renderer.sortingOrder + 4;
            FitOnFace(_lockChip, face, new Vector2(0.3f, -0.3f), 0.38f);
            string n = _lockRemaining.ToString();
            _lockCount.text = _lockCountShadow.text = n;
            var p = face + new Vector3(Size * 0.3f, -Size * 0.31f, 0f);
            float cs = Size * 0.3f;
            _lockCount.transform.localPosition = p;
            _lockCountShadow.transform.localPosition = p + new Vector3(Size * 0.012f, -Size * 0.016f, 0f);
            _lockCount.transform.localScale = _lockCountShadow.transform.localScale = new Vector3(cs, cs, 1f);
            _lockCount.GetComponent<MeshRenderer>().sortingOrder = _renderer.sortingOrder + 6;
            _lockCountShadow.GetComponent<MeshRenderer>().sortingOrder = _renderer.sortingOrder + 5;
        }

        /// <summary>
        /// Mühürlü zarf: paketteki zarf + mühür yerinde mühür renginin taşı (renk şekil işaretiyle de okunur, rehber §7).
        /// Zarf yığının üst yüzünü örter ama yan şeritler görünür: içerik rastgele değildir (rehber §10 M07).
        /// </summary>
        private void UpdateEnvelope(Vector3 face)
        {
            if (_hasEnvelope && _envelope == null)
            {
                _envelope = Mark("Envelope", TileSkin.Sprite("mechanics/envelope-closed") ?? PremiumArt.Get("ov_envelope") ?? ProceduralArt.StarSprite, 2);
                _envelopeDisc = Mark("SealDisc", ProceduralArt.DiscSprite, 3);
                _envelopeSeal = Mark("Seal", PremiumArt.Chip(_envelopeColor) ?? ProceduralArt.StarSprite, 4);
            }
            if (_envelope == null) return;
            _envelope.gameObject.SetActive(_hasEnvelope);
            _envelopeDisc.gameObject.SetActive(_hasEnvelope);
            _envelopeSeal.gameObject.SetActive(_hasEnvelope);
            if (!_hasEnvelope) return;
            _envelope.sortingOrder = _renderer.sortingOrder + 2;
            _envelope.color = Color.white;
            FitOnFace(_envelope, face, Vector2.zero, TileSkin.EnvelopeWidth);
            // Krem disk paketteki kırmızı mührü tamamen örter; her mühür rengi zarf zemininden ayrışır.
            _envelopeDisc.sortingOrder = _renderer.sortingOrder + 3;
            FitOnFace(_envelopeDisc, face, TileSkin.SealOffset, 0.28f);
            _envelopeSeal.sprite = PremiumArt.Chip(_envelopeColor) ?? _envelopeSeal.sprite;
            _envelopeSeal.sortingOrder = _renderer.sortingOrder + 4;
            FitOnFace(_envelopeSeal, face, TileSkin.SealOffset, 0.21f);
        }

        /// <summary>Döner taban düğmesi (paketteki altın dönüş tabanı, küçük). Dokunma alanı BoardView.TryRotorAt.</summary>
        private void UpdateRotor()
        {
            if (_hasRotor && _rotor == null) _rotor = Mark("Rotor", PremiumArt.V4("mechanics/rotor-base") ?? PremiumArt.Get("ov_rotor") ?? ProceduralArt.StarSprite, 6);
            if (_rotor == null) return;
            _rotor.gameObject.SetActive(_hasRotor);
            if (!_hasRotor) return;
            _rotor.sortingOrder = _renderer.sortingOrder + 6;
            _rotor.color = _rotorActive ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            float w = _rotor.sprite.bounds.size.x;
            _rotor.transform.localPosition = RotorLocal;
            _rotor.transform.localScale = Vector3.one * (Size * 0.38f / w);
        }

        /// <summary>Dünya uzayında gerçek metin (kilit sayacı). Yerleşik yazı tipi; resim değil.</summary>
        private TextMesh Count(string name, Color color, int orderOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var t = go.AddComponent<TextMesh>();
            // Nunito 800 (ağırlık fonttan; sahte kalınlık yok).
            var font = KatKat.UI.UiTypography.WorldBold;
            t.font = font;
            t.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            t.fontSize = 64;
            t.characterSize = 0.1f;
            t.fontStyle = FontStyle.Normal;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            t.color = color;
            t.GetComponent<MeshRenderer>().sortingOrder = _renderer.sortingOrder + orderOffset;
            return t;
        }

        private void Rebuild()
        {
            if (_mesh == null) return;
            _builder.Clear();
            float t = Thickness;
            var faceSize = new Vector2(Size, Size);
            if (_showBase)
            {
                // Aktif hücrenin silik taban yuvası.
                _builder.AddQuad(new Vector2(0f, Size * 0.5f - t * 1.1f), faceSize * 0.96f, _slot, Vector2.zero, 0f, 1f, LayerMeshBuilder.Flat);
            }
            int n = _layers.Count;
            for (int k = 0; k < n; k++) AddLayer(k, _layers[k], faceSize, t, true);
            if (_detachedRim.HasValue) AddLayer(n, _detachedRim.Value, faceSize, t, false);
            _builder.ApplyTo(_mesh);
            _shadow.transform.localPosition = new Vector3(0f, -t * 1.6f, 0f);
            _shadow.transform.localScale = new Vector3(Size * 1.12f, Size * 1.08f, 1f);
            _shadow.enabled = n > 0 || _detachedRim.HasValue;
            UpdateMarks();
        }

        private void AddLayer(int k, LayerColor c, Vector2 faceSize, float t, bool withFace)
        {
            float y = Size * 0.5f + (k - 1) * t;
            _builder.AddQuad(new Vector2(0f, y - t * RimDrop), faceSize, LayerPalette.RimColor(c), Vector2.zero, 0f, 1f, LayerMeshBuilder.Side);
            if (withFace) _builder.AddQuad(new Vector2(0f, y), faceSize, LayerPalette.FrontColor(c), LayerPalette.IconCell(c), 1f);
        }
    }
}
