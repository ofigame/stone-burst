using System.Collections.Generic;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Efekt malzemeleri (kullanıcı 2026-11-18: "efektleri daha profesyonel yapmanı istiyorum. Patlama efekti, yıldırım, özel
    /// patlatma taşlarının efektleri … bu tarz oyunlara uygun olsun"): eklemeli malzeme ve koddan üretilen yumuşak dokular —
    /// ışık topu, dört kollu yıldız, şok dalgası halkası, duman/alev pufu, ince çizgi. Hepsi beyaz; renk köşe renginden.
    /// </summary>
    public static class FxKit
    {
        private static Material _add;
        private static bool _addTried;

        /// <summary>Eklemeli malzeme (Resources/Shaders/KatKatAdditive). Yoksa null (varsayılan sprite malzemesi kalır).</summary>
        public static Material Additive
        {
            get
            {
                if (_add != null || _addTried) return _add;
                _addTried = true;
                var sh = KatKat.View.Res.Load<Shader>("Shaders/KatKatAdditive");
                if (sh != null && sh.isSupported) _add = new Material(sh) { name = "KatKatAdditive", hideFlags = HideFlags.DontSave };
                return _add;
            }
        }

        public static void MakeAdditive(SpriteRenderer sr) { var m = Additive; if (m != null) sr.sharedMaterial = m; }

        private static Sprite _glow, _star, _ring, _puff, _streak, _dot;

        /// <summary>Gauss benzeri yumuşak ışık topu.</summary>
        public static Sprite Glow => _glow != null ? _glow : (_glow = Make("fx_glow", 64, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            return Mathf.Exp(-r * r * 5.5f) * Mathf.Clamp01((1f - r) * 6f);
        }));

        /// <summary>Küçük, keskin merkezli parlak nokta (kıvılcım, köz).</summary>
        public static Sprite Dot => _dot != null ? _dot : (_dot = Make("fx_dot", 32, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            return Mathf.Clamp01(Mathf.Exp(-r * r * 14f) + Mathf.Clamp01(1f - r / 0.18f));
        }));

        /// <summary>Dört kollu ince ışıltı + yumuşak hale.</summary>
        public static Sprite Star => _star != null ? _star : (_star = Make("fx_star", 96, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
            float arms = Mathf.Max(Mathf.Exp(-ay * ay * 900f) * Mathf.Pow(Mathf.Clamp01(1f - ax), 2f),
                                   Mathf.Exp(-ax * ax * 900f) * Mathf.Pow(Mathf.Clamp01(1f - ay), 2f));
            float glow = Mathf.Exp(-r * r * 9f) * 0.55f;
            float core = Mathf.Exp(-r * r * 120f);
            return Mathf.Clamp01(arms + glow + core);
        }));

        /// <summary>Şok dalgası halkası: ince parlak kenar, içe doğru sönen.</summary>
        public static Sprite Ring => _ring != null ? _ring : (_ring = Make("fx_ring", 128, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float edge = Mathf.Exp(-Mathf.Pow((r - 0.86f) / 0.06f, 2f));
            float inner = Mathf.Clamp01((r - 0.35f) / 0.5f) * 0.25f * Mathf.Clamp01((0.92f - r) * 12f);
            return Mathf.Clamp01(edge + inner);
        }));

        /// <summary>Düzensiz kenarlı yumuşak puf (alev dili, duman, toz); gürültüyle bozulmuş daire.</summary>
        public static Sprite Puff => _puff != null ? _puff : (_puff = Make("fx_puff", 96, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float ang = Mathf.Atan2(y, x);
            float wob = 0.08f * Mathf.Sin(ang * 3f + 1.3f) + 0.05f * Mathf.Sin(ang * 7f + 0.4f) + 0.03f * Mathf.Sin(ang * 13f);
            float rr = r / (0.82f + wob);
            float a = Mathf.Clamp01(1f - rr);
            return a * a * (3f - 2f * a) * (0.75f + 0.25f * Mathf.Sin(x * 9f + y * 7f));
        }));

        /// <summary>Uzun ince çizgi (hız izi): yatayda uzun, uçlara ve kenarlara sönen.</summary>
        public static Sprite Streak => _streak != null ? _streak : (_streak = MakeRect("fx_streak", 128, 16, (x, y) =>
        {
            float along = Mathf.Clamp01(1f - Mathf.Abs(x));
            return Mathf.Exp(-y * y * 6f) * along * along;
        }));

        private delegate float Field(float x, float y);

        private static Sprite Make(string name, int n, Field f) => MakeRect(name, n, n, f);

        private static Sprite MakeRect(string name, int w, int h, Field f)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var p = new Color32[w * h];
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float x = (i + 0.5f) / w * 2f - 1f, y = (j + 0.5f) / h * 2f - 1f;
                float a = Mathf.Clamp01(f(x, y));
                p[j * w + i] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            t.SetPixels32(p);
            t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Mathf.Max(w, h));
        }
    }

    /// <summary>
    /// Hafif parçacık sistemi (SpriteRenderer havuzu). Tahta zamanıyla ilerler (<see cref="BoardView.Advance"/> çağırır), böylece
    /// oyun hızı ve kayıt araçlarıyla uyumludur. Konumlar dünya uzayında. Her parçacık: hız, yerçekimi, sürtünme, boyut ve renk
    /// eğrisi (başlangıç → bitiş), dönüş, gecikme.
    /// </summary>
    public sealed class FxParticles : MonoBehaviour
    {
        /// <summary>Etkin tahtanın parçacık sistemi (taş patlamaları buraya yayar).</summary>
        public static FxParticles Active;

        private struct P
        {
            public SpriteRenderer Sr;
            public Vector3 Pos, Vel;
            public float Age, Life, Delay, Size0, Size1, Rot, RotVel, Drag, Gravity, Stretch;
            public Color C0, C1;
        }

        private readonly List<P> _live = new List<P>();
        private readonly Stack<SpriteRenderer> _poolAdd = new Stack<SpriteRenderer>(), _poolAlpha = new Stack<SpriteRenderer>();

        public int Count => _live.Count;

        /// <param name="stretch">0: yuvarlak; &gt;0: hız yönünde uzar (kıvılcım izi), değer = hız başına ek uzama.</param>
        public void Emit(Sprite sprite, bool additive, int order, Vector3 pos, Vector3 vel, float life, float size0, float size1,
            Color c0, Color c1, float gravity = 0f, float drag = 0f, float delay = 0f, float rot = 0f, float rotVel = 0f, float stretch = 0f)
        {
            var pool = additive ? _poolAdd : _poolAlpha;
            SpriteRenderer sr = null;
            while (pool.Count > 0 && sr == null) sr = pool.Pop();
            if (sr == null)
            {
                sr = new GameObject(additive ? "FxAdd" : "Fx").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(transform, false);
                if (additive) FxKit.MakeAdditive(sr);
            }
            sr.gameObject.SetActive(true);
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.enabled = delay <= 0f;
            var p = new P { Sr = sr, Pos = pos, Vel = vel, Life = Mathf.Max(0.01f, life), Delay = delay, Size0 = size0, Size1 = size1, C0 = c0, C1 = c1, Gravity = gravity, Drag = drag, Rot = rot, RotVel = rotVel, Stretch = stretch };
            Apply(ref p, 0f);
            _live.Add(p);
        }

        /// <summary>Süre (ms) kadar ilerletir.</summary>
        public void Tick(float ms)
        {
            float dt = ms / 1000f;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var p = _live[i];
                if (p.Delay > 0f)
                {
                    p.Delay -= dt;
                    if (p.Delay > 0f) { _live[i] = p; continue; }
                    p.Sr.enabled = true;
                }
                p.Age += dt;
                if (p.Age >= p.Life)
                {
                    Release(p.Sr);
                    _live.RemoveAt(i);
                    continue;
                }
                p.Vel += Vector3.up * (p.Gravity * dt);
                if (p.Drag > 0f) p.Vel *= Mathf.Exp(-p.Drag * dt);
                p.Pos += p.Vel * dt;
                p.Rot += p.RotVel * dt;
                Apply(ref p, p.Age / p.Life);
                _live[i] = p;
            }
        }

        private static void Apply(ref P p, float t)
        {
            var tr = p.Sr.transform;
            tr.position = p.Pos;
            float s = Mathf.Lerp(p.Size0, p.Size1, t);
            if (p.Stretch > 0f && p.Vel.sqrMagnitude > 1e-6f)
            {
                float sp = p.Vel.magnitude;
                tr.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(p.Vel.y, p.Vel.x) * Mathf.Rad2Deg);
                tr.localScale = new Vector3(s * (1f + sp * p.Stretch), s, 1f);
            }
            else
            {
                tr.rotation = Quaternion.Euler(0f, 0f, p.Rot);
                tr.localScale = new Vector3(s, s, 1f);
            }
            // Renk: başlangıç → bitiş; saydamlık başta hızlı açılır (ilk %8), sonra eğriyle söner.
            var c = Color.Lerp(p.C0, p.C1, t);
            c.a *= Mathf.Clamp01(t / 0.08f);
            p.Sr.color = c;
        }

        private void Release(SpriteRenderer sr)
        {
            if (sr == null) return;
            sr.gameObject.SetActive(false);
            (sr.sharedMaterial == FxKit.Additive && FxKit.Additive != null ? _poolAdd : _poolAlpha).Push(sr);
        }

        /// <summary>2026-11-27 ön ısıtma: havuza nesne hazırlar (görünmez, kısa ömürlü parçacık olarak bir kez çizilir).</summary>
        public void Warm(Vector3 at, int alpha, int additive)
        {
            at.z -= 0.05f;
            var clear = new Color(1f, 1f, 1f, 0.004f);
            for (int i = 0; i < alpha; i++) Emit(i % 2 == 0 ? FxKit.Dot : StoneShatter.Chip(i), false, 0, at, Vector3.zero, 0.05f, 0.01f, 0.01f, clear, clear);
            for (int i = 0; i < additive; i++) Emit(i % 3 == 0 ? FxKit.Streak : i % 3 == 1 ? FxKit.Ring : FxKit.Glow, true, 0, at, Vector3.zero, 0.05f, 0.01f, 0.01f, clear, clear);
        }

        /// <summary>Bütün parçacıkları hemen bitirir (bölüm değişimi).</summary>
        public void Clear()
        {
            foreach (var p in _live) Release(p.Sr);
            _live.Clear();
        }
    }
}
