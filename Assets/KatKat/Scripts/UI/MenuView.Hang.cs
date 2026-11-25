using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Harita yan sütunları iple asılı (kullanıcı 2026-11-23: "Anasayfa harita üzerindeki sol ve sağda olan butonlar. Üst alandan
    /// iple sarkıyormuş gibi olsun çok fazla göz yormayacak şekilde ipten sallanma efekti olsun"). Sol sütunun ilk rozeti ve sağdaki
    /// koleksiyon kartı bölge plakasının altından, alttaki rozetler bir üsttekinin altından iki burgulu iple sarkar. Her askı ipin
    /// üst ucundan sarkaç gibi yavaşça sallanır (rozet kenarında en çok ~4 px; sütun boyunca küçük faz farkı, dalga gibi).
    /// Azaltılmış harekette sabit.
    /// </summary>
    public partial class MenuView
    {
        internal const string HangSuffix = "Hang";


        /// <summary>İpler kartın üst kenarının bu kadar içine bağlanır (düğüm kartın üstünde görünür).</summary>
        private const float RopeTuck = 10f;
        /// <summary>İpin karttaki yatay yeri (kart genişliği oranı): simge çerçevesinin (~%30) dışı.</summary>
        private const float RopeX = 0.40f;

        /// <summary>Sol sütun askıları ve sağdaki koleksiyon kartını iplere bağlar, sallanmayı başlatır.</summary>
        private void HangColumns(RectTransform shell)
        {
            var left = new List<RectTransform>();
            RectTransform right = null;
            for (int i = 0; i < shell.childCount; i++)
            {
                var c = shell.GetChild(i) as RectTransform;
                if (c == null) continue;
                if (c.name.EndsWith(HangSuffix) && c.anchoredPosition.x < 0f) left.Add(c);
                else if (c.name == "StarGoal") right = c;
            }
            // 2026-11-23 (kullanıcı: "ipleri güzel oturt konduğu alana aitmiş gibi dursun"): en üstteki askılar üst panelin
            // arkasından iner (ip panelin içinden çıkıyormuş gibi; askı panelden önce çizilir), alttakiler üstteki kartın alt
            // kenarındaki pirinç halkalardan sarkar; ipler kartlara pirinç halkalarla bağlanır.
            var plate = shell.Find("RegionShell");
            int behind = plate != null ? plate.GetSiblingIndex() : -1;
            left.Sort((a, b) => b.anchoredPosition.y.CompareTo(a.anchoredPosition.y)); // üstten alta
            float above = HangBehindY;
            for (int i = 0; i < left.Count; i++)
            {
                var h = left[i];
                bool first = i == 0 && behind >= 0;
                // 2026-11-26 (kullanıcı: "ipli menülerin ip bağlantıları iconun altında olan arkaplanına sabitlensin"): halkalar simge
                // çerçevesinin dışında, kartın krem arka planında (köşelere yakın).
                Hang(h, first ? HangBehindY : above, first ? PlateBottomY : above, BadgeW * RopeX, i, RopeTuck, !first);
                if (first) h.SetSiblingIndex(behind++);
                above = h.anchoredPosition.y - h.sizeDelta.y * h.pivot.y + EyeletInset; // üst kartın alt kenarındaki halka
            }
            // 2026-11-23 (kullanıcı: "sallanmada ipler konumlarını korumuyor. sabit olmalı uç noktaları"): alttaki askı üsttekinin
            // içine bağlanır; ipin üst ucu üstteki kartın halkasında kalır, zincir gibi birlikte salınır.
            for (int i = left.Count - 1; i >= 1; i--)
            {
                left[i].SetParent(left[i - 1], true);
                var sw = left[i].GetComponent<HangSwing>();
                if (sw != null) sw.Chained = true;
            }
            if (right != null)
            {
                Hang(right, HangBehindY, PlateBottomY, right.sizeDelta.x * 0.3f, 1, 56f, behind < 0); // buz taşının eğik kenarına bağlanır
                if (behind >= 0) right.SetSiblingIndex(behind);
            }
        }

        /// <summary>İlk askıların ipinin üst ucu: bölge plakasının içinde (plakanın arkasında kalır, alt kenarından çıkar).</summary>
        private const float HangBehindY = -250f;

        /// <summary>Bölge plakasının alt kenarı: ilk askıların dönme noktası (ipin göründüğü yer kaymaz).</summary>
        private const float PlateBottomY = -296f;

        /// <summary>Halkanın kart kenarından içerisi (px).</summary>
        private const float EyeletInset = 9f;

        /// <summary>
        /// <paramref name="rt"/> (anchor üst orta) <paramref name="ropeTop"/> çizgisinden iki iple asılır; pivot ipin üst ucuna
        /// alınır (görünür konum değişmez) ve sallanma eklenir. <paramref name="topEyelet"/>: ipin üst ucunda da halka (üstteki
        /// kartın alt kenarı); yoksa ip panelin arkasından çıkar.
        /// </summary>
        private static void Hang(RectTransform rt, float ropeTop, float pivotTop, float ropeX, int index, float tuck, bool topEyelet)
        {
            float w = rt.sizeDelta.x, hgt = rt.sizeDelta.y;
            float cardTop = rt.anchoredPosition.y + hgt * (1f - rt.pivot.y);
            float len = ropeTop - cardTop + tuck;
            if (len < tuck + 4f) return; // ip için yer yok

            foreach (var sx in new[] { -1f, 1f })
            {
                var bottom = new Vector2(sx * ropeX, -tuck);
                Eyelet(rt, bottom);
                if (topEyelet) Eyelet(rt, bottom + new Vector2(0f, len));
                var rope = Decor("Rope", rt, RopeSprite);
                rope.type = Image.Type.Tiled;
                var rr = rope.rectTransform;
                rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 1f);
                rr.pivot = new Vector2(0.5f, 0f);
                rr.sizeDelta = new Vector2(8f, len);
                rr.anchoredPosition = bottom;
                rr.SetAsLastSibling();
                // İpin ucu halkanın deliğinden geçer: uçta küçük sarım (düğüm).
                Wrap(rt, bottom + new Vector2(0f, 7f));
                if (topEyelet) Wrap(rt, bottom + new Vector2(0f, len - 7f));
            }

            // Pivot ipin üst ucuna (sarkaç noktası); konum korunur.
            var oldPivot = rt.pivot;
            var pivot = new Vector2(0.5f, 1f + (pivotTop - cardTop) / hgt);
            rt.pivot = pivot;
            rt.anchoredPosition += new Vector2((pivot.x - oldPivot.x) * w, (pivot.y - oldPivot.y) * hgt);

            var swing = rt.gameObject.AddComponent<HangSwing>();
            float radius = Mathf.Max(40f, pivotTop - (cardTop - hgt));
            swing.Amp = Mathf.Rad2Deg * (HangSwing.EdgePx / radius);
            swing.Phase = index * 0.7f;
            swing.Radius = radius;
        }

        /// <summary>Kartın kenarına çakılı pirinç halka (koyu delikli); ip deliğe girer.</summary>
        private static void Eyelet(RectTransform parent, Vector2 pos)
        {
            Dot(parent, "RopeEyelet", pos, 20f, Ui3.Rounded(10f, Ui3.Hex("#F3DCA6"), Ui3.Hex("#A87C40"), 2f, Ui3.Hex("#6E4E2A")));
            Dot(parent, "RopeEyeletHole", pos, 9f, Ui3.Rounded(4.5f, Ui3.Hex("#3A2614"), Ui3.Hex("#5A3E22")));
        }

        /// <summary>İp ucundaki sarım: ipten biraz geniş, koyu kenarlı kısa bant.</summary>
        private static void Wrap(RectTransform parent, Vector2 pos)
        {
            var k = Decor("RopeWrap", parent, Ui3.Rounded(3f, Ui3.Hex("#C9A46A"), Ui3.Hex("#8A6236"), 1.5f, Ui3.Hex("#5E4022")));
            var kr = k.rectTransform;
            kr.anchorMin = kr.anchorMax = new Vector2(0.5f, 1f);
            kr.pivot = new Vector2(0.5f, 0.5f);
            kr.sizeDelta = new Vector2(12f, 7f);
            kr.anchoredPosition = pos;
            kr.SetAsLastSibling();
        }

        private static void Dot(RectTransform parent, string name, Vector2 pos, float size, Sprite sprite)
        {
            var k = Decor(name, parent, sprite);
            var kr = k.rectTransform;
            kr.anchorMin = kr.anchorMax = new Vector2(0.5f, 1f);
            kr.pivot = new Vector2(0.5f, 0.5f);
            kr.sizeDelta = new Vector2(size, size);
            kr.anchoredPosition = pos;
            kr.SetAsLastSibling();
        }

        private static Sprite _ropeSprite;

        /// <summary>Burgulu ip dokusu (10×12, koddan; dikey döşenir): eğik açık/koyu hasır şeritleri, kenarlar gölgeli.</summary>
        private static Sprite RopeSprite
        {
            get
            {
                if (_ropeSprite) return _ropeSprite;
                const int w = 10, h = 12;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "map_rope", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
                var px = new Color32[w * h];
                var light = new Color(0.93f, 0.78f, 0.52f);
                var dark = new Color(0.55f, 0.37f, 0.18f);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float band = 0.5f + 0.5f * Mathf.Cos(((y + x * 0.8f) / 6f) * Mathf.PI * 2f);
                    float edge = Mathf.Abs((x + 0.5f) / w - 0.5f) * 2f; // 0 orta, 1 kenar
                    var c = Color.Lerp(dark, light, band * (1f - 0.55f * edge * edge));
                    c.a = edge > 0.85f ? 0.75f : 1f;
                    px[y * w + x] = c;
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                _ropeSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                _ropeSprite.name = "map_rope";
                return _ropeSprite;
            }
        }
    }

    /// <summary>
    /// İpten sarkan askının sarkaç salınımı (gerçek zamanlı; azaltılmış harekette düz). Durgunken yavaş, hafif salınım.
    /// 2026-11-23: ivmeölçer okunur; yalnız sert sarsıntıda (kullanıcı: "en ufak telefonu çevirmede bile sallanıyor. telefonu biraz
    /// sert sallayınca sallanacaklar") eşiği aşan yanal ivme askıyı iter, yaylı sarkaç gibi salınıp birkaç saniyede söner. Telefonu
    /// çevirmek/eğmek etkisiz. Zincirdeki askı üstündekine bağlı (göreli açı) ve daha az itilir. Açı en çok <see cref="MaxAngle"/>.
    /// </summary>
    public sealed class HangSwing : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>Durgun salınımda askının alt kenarındaki en büyük yanal kayma (px) ve periyodu (sn).</summary>
        public const float EdgePx = 4f, Period = 4.2f;
        /// <summary>En büyük açı (derece), sarsıntı eşiği (g), eşiği aşan ivmenin kazancı (derece/sn² / g) ve sönüm oranı.</summary>
        public const float MaxAngle = 16f, ShakeThreshold = 0.55f, ShakeGain = 1500f, Damping = 0.18f;
        /// <summary>Parmakla itme kazancı (derece/sn, ekran genişliği oranında sürükleme başına).</summary>
        public const float DragGain = 900f;
        public float Amp = 1f, Phase;
        /// <summary>Sarkaç boyu (px, ipin üstünden askının altına): uzun askı daha yavaş salınır.</summary>
        public float Radius = 200f;
        /// <summary>Zincirdeki askı (üstteki rozete bağlı): açı üstündekine göre.</summary>
        public bool Chained;
        private float _t, _swing, _vel;

        /// <summary>Testler: ivmeölçer yerine bu değer (g biriminde; null → cihaz).</summary>
        public static Vector3? AccelOverride;
        private static int _frame = -1;
        private static Vector3 _gravity, _shake;
        private static bool _primed;

        public float Angle { get; private set; }
        /// <summary>Sarsıntıdan gelen salınım (testler).</summary>
        public float ShakeSwing => _swing;

        private void Update() => Advance(Time.unscaledDeltaTime);

        // 2026-11-26 (kullanıcı: "Butonlara basılı tutup parmağımızı oynatırsak butonlar ip desteği ile o yönde sallansın"): sürükleme
        // askıyı parmağın yönünde iter (yatay hız kadar), bırakınca yaylı sarkaç gibi salınıp söner. Sürükleme başlayınca düğmenin
        // tıklaması düşer (sürükleyen askı, basılan düğme değil).
        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = true;
        public void OnBeginDrag(PointerEventData e) { }
        public void OnDrag(PointerEventData e)
        {
            if (Motion.Reduced) return;
            float dx = e.delta.x / Mathf.Max(1f, Screen.width);
            PushChain(dx);
        }
        public void OnEndDrag(PointerEventData e) { }

        /// <summary>Testler: parmakla itmeyi taklit eder (ekran genişliği oranında yatay kayma).</summary>
        public void Push(float dxScreenFraction) => _vel += dxScreenFraction * DragGain;

        /// <summary>
        /// 2026-11-26 (kullanıcı: "ipli butonu sürüklediğimde üstündeki butonlar da sallanmalı. ip bağlantı noktasından konumunu korumalı"):
        /// itme sürüklenen askıya tam, üstündeki askılara azalarak (her basamakta %70) aktarılır; alttaki askı üsttekinin halkalarına
        /// bağlı (çocuğu) olduğundan ip uçları ayrılmaz, sütun birlikte sallanır.
        /// </summary>
        public void PushChain(float dxScreenFraction)
        {
            float f = 1f;
            for (var sw = this; sw != null; sw = sw.transform.parent != null ? sw.transform.parent.GetComponentInParent<HangSwing>() : null)
            {
                sw._vel += dxScreenFraction * DragGain * f;
                f *= 0.7f;
            }
        }

        /// <summary>İvmeölçer kare başına bir kez okunur: hızla izlenen kısım yerçekimi (çevirme), kalan ani kısım sarsıntı.</summary>
        private static void Sample(float dt)
        {
            if (_frame == Time.frameCount && AccelOverride == null) return;
            _frame = Time.frameCount;
            var a = AccelOverride ?? (SystemInfo.supportsAccelerometer ? Input.acceleration : Vector3.zero);
            if (!_primed) { _gravity = a; _primed = true; }
            _gravity = Vector3.Lerp(_gravity, a, 1f - Mathf.Exp(-dt * 10f));
            _shake = a - _gravity;
        }

        public void Advance(float dt)
        {
            if (Motion.Reduced) { Angle = 0f; _swing = _vel = 0f; transform.localRotation = Quaternion.identity; return; }
            if (dt <= 0f) return;
            dt = Mathf.Min(dt, 0.05f);
            _t += dt;
            Sample(dt);
            float sx = _shake.x, mag = Mathf.Abs(sx);
            float drive = mag > ShakeThreshold ? Mathf.Sign(sx) * (mag - ShakeThreshold) : 0f;
            float w0 = Mathf.Clamp(Mathf.Sqrt(9.81f / Mathf.Max(0.05f, Radius / 1000f)) * 0.55f, 3.2f, 7f);
            float acc = -w0 * w0 * _swing - 2f * Damping * w0 * _vel - drive * ShakeGain * (Chained ? 0.6f : 1f);
            _vel += acc * dt;
            _swing += _vel * dt;
            if (Mathf.Abs(_swing) > MaxAngle) { _swing = Mathf.Sign(_swing) * MaxAngle; _vel *= -0.3f; }
            // 2026-11-23 (kullanıcı: "durduğu yerden sallanmayacak ancak sarsılırsa"): durgunken kıpırtı yok; yalnız sarsıntı salınımı.
            Angle = Mathf.Clamp(_swing, -MaxAngle, MaxAngle);
            transform.localRotation = Quaternion.Euler(0f, 0f, Angle);
        }
    }
}
