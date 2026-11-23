using System.Collections.Generic;
using UnityEngine;
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
                Hang(h, first ? HangBehindY : above, BadgeW * 0.3f, i, RopeTuck, !first);
                if (first) h.SetSiblingIndex(behind++);
                above = h.anchoredPosition.y - h.sizeDelta.y * h.pivot.y + EyeletInset; // üst kartın alt kenarındaki halka
            }
            if (right != null)
            {
                Hang(right, HangBehindY, right.sizeDelta.x * 0.3f, 1, 56f, behind < 0); // buz taşının eğik kenarına bağlanır
                if (behind >= 0) right.SetSiblingIndex(behind);
            }
        }

        /// <summary>İlk askıların ipinin üst ucu: bölge plakasının içinde (plakanın arkasında kalır, alt kenarından çıkar).</summary>
        private const float HangBehindY = -250f;

        /// <summary>Halkanın kart kenarından içerisi (px).</summary>
        private const float EyeletInset = 9f;

        /// <summary>
        /// <paramref name="rt"/> (anchor üst orta) <paramref name="ropeTop"/> çizgisinden iki iple asılır; pivot ipin üst ucuna
        /// alınır (görünür konum değişmez) ve sallanma eklenir. <paramref name="topEyelet"/>: ipin üst ucunda da halka (üstteki
        /// kartın alt kenarı); yoksa ip panelin arkasından çıkar.
        /// </summary>
        private static void Hang(RectTransform rt, float ropeTop, float ropeX, int index, float tuck, bool topEyelet)
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
            var pivot = new Vector2(0.5f, 1f + (ropeTop - cardTop) / hgt);
            rt.pivot = pivot;
            rt.anchoredPosition += new Vector2((pivot.x - oldPivot.x) * w, (pivot.y - oldPivot.y) * hgt);

            var swing = rt.gameObject.AddComponent<HangSwing>();
            float radius = Mathf.Max(40f, ropeTop - (cardTop - hgt));
            swing.Amp = Mathf.Rad2Deg * (HangSwing.EdgePx / radius);
            swing.Phase = index * 0.7f;
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

    /// <summary>İpten sarkan askının yavaş sarkaç salınımı (gerçek zamanlı; azaltılmış harekette düz).</summary>
    public sealed class HangSwing : MonoBehaviour
    {
        /// <summary>Askının alt kenarındaki en büyük yanal kayma (px) ve salınım periyodu (sn).</summary>
        public const float EdgePx = 4f, Period = 4.2f;
        public float Amp = 1f, Phase;
        private float _t;

        public float Angle { get; private set; }

        private void Update() => Advance(Time.unscaledDeltaTime);

        public void Advance(float dt)
        {
            if (Motion.Reduced) { Angle = 0f; transform.localRotation = Quaternion.identity; return; }
            _t += dt;
            Angle = Amp * Mathf.Sin(_t * Mathf.PI * 2f / Period + Phase);
            transform.localRotation = Quaternion.Euler(0f, 0f, Angle);
        }
    }
}
