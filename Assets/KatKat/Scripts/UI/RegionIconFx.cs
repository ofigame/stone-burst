using System.Collections.Generic;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Anasayfa bölge plakasındaki simgenin canlılığı (kullanıcı, 2026-11-20: "anasayfada üst kısımdaki fenere ışık efekti
    /// yapalım ve geniş bir çevreyi fener ışığıyla gezsin. Yeşil vadi objesine su akma efekti olsun, kum vadisinde de ağaç
    /// yapraklarına hareket katalım ve su hareketli olsun"):
    /// <list type="bullet">
    /// <item>Simge <c>KatKat/RegionIconMotion</c> malzemesiyle çizilir: maskedeki su akar ve parlar, palmiye tacı salınır
    /// (maske tools/region_icon_masks.ps1).</item>
    /// <item>Fener (coast): lambada nabız atan ışık ve dönen fener ışığı — lambadan çıkan geniş ışık konisi plakanın üstünde
    /// sağa sola süpürür (dönüşün izdüşümü: koni boyu |cos|, yön işaret değiştirir; ışık izleyiciye dönünce lamba parlar).</item>
    /// </list>
    /// Dokunma yakalamaz. Azaltılmış harekette yalnız durağan simge.
    /// </summary>
    public sealed class RegionIconFx : MonoBehaviour
    {
        private static Shader _shader;
        private static bool _tried;
        private static readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();
        private static Sprite _cone;

        private RectTransform _beam, _flare;
        private Image _beamImg, _flareImg;
        private float _beamLen;

        /// <summary>Lamba konumu (simge dikdörtgeninde 0–1) ve ışık konisinin boyu (referans px).</summary>
        public const float LampX = 0.5f, LampY = 0.76f, BeamLength = 980f, BeamPeriod = 6.5f;

        public static void Attach(Image icon, string themeId)
        {
            if (icon == null || Motion.Reduced) return;
            var mat = MaterialFor(themeId);
            if (mat != null) icon.material = mat;
            if (themeId == "coast") icon.gameObject.AddComponent<RegionIconFx>().BuildBeam(icon.rectTransform);
        }

        private static Material MaterialFor(string id)
        {
            if (_mats.TryGetValue(id, out var m)) return m;
            if (!_tried)
            {
                _tried = true;
                _shader = Resources.Load<Shader>("Shaders/KatKatRegionIcon");
                if (_shader != null && !_shader.isSupported) _shader = null;
            }
            var mask = _shader != null ? Resources.Load<Texture2D>("PremiumV6/ui/region-" + id + "-motion") : null;
            if (mask != null)
            {
                m = new Material(_shader) { name = "RegionIcon_" + id, hideFlags = HideFlags.DontSave };
                m.SetTexture("_Motion", mask);
            }
            _mats[id] = m;
            return m;
        }

        private void BuildBeam(RectTransform icon)
        {
            var r = icon.rect;
            var lamp = new Vector2(r.xMin + r.width * LampX, r.yMin + r.height * LampY);
            // Koni simgenin arkasında değil önünde: ışık plakanın üstünden geçer (yarı saydam, sıcak sarı).
            _beam = UiFactory.Rect("Beam", icon);
            _beam.anchorMin = _beam.anchorMax = new Vector2(0.5f, 0.5f);
            _beam.pivot = new Vector2(0f, 0.5f);
            _beam.anchoredPosition = lamp - r.center;
            _beamImg = _beam.gameObject.AddComponent<Image>();
            _beamImg.sprite = Cone();
            _beamImg.raycastTarget = false;
            _beamImg.color = new Color(1f, 0.93f, 0.6f, 0.5f);
            _beamLen = BeamLength;
            _beam.sizeDelta = new Vector2(_beamLen, 190f);
            _flare = UiFactory.Rect("Flare", icon);
            _flare.anchorMin = _flare.anchorMax = new Vector2(0.5f, 0.5f);
            _flare.anchoredPosition = lamp - r.center;
            _flareImg = _flare.gameObject.AddComponent<Image>();
            _flareImg.sprite = SpecialArt.Glow;
            _flareImg.raycastTarget = false;
            _flare.sizeDelta = new Vector2(120f, 120f);
            Advance(0f);
        }

        private void Update() => Advance(Time.unscaledTime);

        public void Advance(float t)
        {
            if (_beam == null) return;
            float a = t / BeamPeriod * Mathf.PI * 2f;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            // Izdüşüm: yan tarafa bakarken uzun ve belirgin, izleyiciye/arkaya dönerken kısalır.
            float len = Mathf.Max(0.04f, Mathf.Abs(c));
            _beam.localScale = new Vector3(c >= 0f ? len : -len, 1f, 1f);
            // Arkaya dönükken (sin < 0) soluk; önde parlak.
            float front = Mathf.Clamp01(0.55f + 0.45f * s);
            _beamImg.color = new Color(1f, 0.93f, 0.6f, 0.72f * front * Mathf.Clamp01(len * 1.6f));
            // Işık izleyiciye dönünce (|cos| küçük, sin > 0) lamba parlar.
            float face = Mathf.Clamp01(1f - Mathf.Abs(c) * 2.2f) * Mathf.Clamp01(s * 2f);

            _flareImg.color = new Color(1f, 0.92f, 0.55f, Mathf.Clamp01(0.55f + 0.45f * face));
            _flare.sizeDelta = Vector2.one * (90f + 200f * face) * (1f + 0.1f * Mathf.Sin(t * 5f));
        }

        /// <summary>Işık konisi: solda dar (lamba), sağa doğru açılan ve sönen yumuşak kenarlı koni (256 × 128).</summary>
        private static Sprite Cone()
        {
            if (_cone != null) return _cone;
            const int W = 256, H = 128;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { name = "lighthouse_cone", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W, v = (y + 0.5f) / H - 0.5f;
                float half = Mathf.Lerp(0.06f, 0.5f, u); // açılan koni
                float edge = 1f - Mathf.Clamp01((Mathf.Abs(v) - half * 0.45f) / (half * 0.55f));
                float along = Mathf.Pow(1f - u, 1.3f) * Mathf.Clamp01(u * 12f); // lambadan uzaklaştıkça söner
                float core = Mathf.Exp(-(v * v) / (half * half * 0.08f)) * 0.5f;
                float a = Mathf.Clamp01(edge * edge * along + core * along);
                px[y * W + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _cone = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0f, 0.5f), 100f);
            _cone.name = "lighthouse_cone";
            return _cone;
        }
    }
}
