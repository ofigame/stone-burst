using System;
using System.Collections.Generic;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Kova çatlağı (2026-11-26, kullanıcı: "Bomba tuzaklarında kova yarılacak. Yarık doğal ve gerçekçi olmalı"; 2026-11-25: "eğer
    /// hemen telefonu eğerse yarığa denk gelmeyen su boşalmasın. Belli bir sürede yarık kendini onarsın").
    /// <para>Bombalı yığın patlayınca kovanın camında, alt yarısında bir darbe çatlağı açılır (dallanan kırık çizgiler; camda ince
    /// kıvılcım). Su çatlaktan dışarı fışkırıp aşağı akar. Sızıntı hızı çatlağın üstündeki su derinliğiyle orantılı: telefon
    /// çatlağın tersine eğilirse orada su alçalır, sızıntı yavaşlar ya da durur. Her birim sızınca <see cref="WaterLeaked"/>.
    /// <see cref="CrackHealSeconds"/> sonra çatlak kapanır (<see cref="CrackHealed"/>).</para>
    /// </summary>
    public sealed partial class BoardView
    {
        public const float CrackHealSeconds = 6f;
        /// <summary>Tam derinlikte sızıntı hızı (birim/sn).</summary>
        public const float LeakMax = 1.6f;

        public Action<int> WaterLeaked { get; set; }
        public Action CrackHealed { get; set; }

        private sealed class Crack
        {
            public Vector3 P; // tahta yerel
            public float Age, Acc, Flow, Dir;
            public SpriteRenderer R;
        }

        private readonly List<Crack> _cracks = new List<Crack>();
        public int OpenCracks => _cracks.Count;

        /// <summary>Kova çatlar (patlayan bombanın sütunu çatlağın yanını seçer). Açık çatlak varsa süresi yenilenir.</summary>
        public void OpenCrack(int col)
        {
            if (!_waterOn) return;
            if (_cracks.Count > 0) { _cracks[0].Age = 0f; return; }
            float side = CellCenter(Mathf.Clamp(col, 0, _width - 1), 0).x < _bucketX ? -1f : 1f;
            float half = _bucketS * WaterArt.BucketHalfBottom;
            var p = new Vector3(_bucketX + side * half * UnityEngine.Random.Range(0.25f, 0.6f), _bucketBottom + _bucketS * Mathf.Lerp(WaterArt.WaterInBottom, WaterArt.WaterInTop, UnityEngine.Random.Range(0.12f, 0.4f)), 0f);
            var go = new GameObject("BucketCrack");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WaterArt.GlassCrack;
            sr.sortingOrder = 9;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            _cracks.Add(new Crack { P = p, R = sr, Dir = side });
            if (!KatKat.UI.Motion.Reduced)
                for (int i = 0; i < 12; i++)
                {
                    float a = UnityEngine.Random.value * Mathf.PI * 2f;
                    SpawnFoam(p, new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * _pitch * UnityEngine.Random.Range(1f, 2.5f), 0.07f);
                }
        }

        private void TickCracks(float dt)
        {
            for (int i = _cracks.Count - 1; i >= 0; i--)
            {
                var c = _cracks[i];
                c.Age += dt;
                float depth = _waterShown > 0.001f ? SurfaceAt(c.P.x) - c.P.y : 0f;
                float k = Mathf.Clamp01(depth / Mathf.Max(0.01f, _bucketS * 0.3f));
                float heal = Mathf.Clamp01((CrackHealSeconds - c.Age) / 1.2f); // son 1,2 sn: çatlak kapanır
                float rate = LeakMax * k * heal;
                float leak = Mathf.Min(rate * dt, _waterHave);
                _waterHave -= leak;
                _waterShown = Mathf.Min(_waterShown, _waterHave + 0.5f);
                c.Acc += leak;
                c.Flow = Mathf.Lerp(c.Flow, rate / LeakMax, 1f - Mathf.Exp(-8f * dt));
                while (c.Acc >= 1f) { c.Acc -= 1f; WaterLeaked?.Invoke(1); }
                if (c.Flow > 0.05f && !KatKat.UI.Motion.Reduced && UnityEngine.Random.value < dt * 20f * c.Flow)
                    SpawnFoam(c.P + new Vector3(c.Dir * _pitch * 0.15f, 0f), new Vector3(c.Dir * UnityEngine.Random.Range(0.6f, 1.6f), UnityEngine.Random.Range(-0.2f, 0.8f)) * _pitch, 0.06f);
                if (c.R != null)
                {
                    c.R.transform.localPosition = c.P;
                    float s = _bucketS * 0.36f / Mathf.Max(0.01f, c.R.sprite.bounds.size.x);
                    float open = Mathf.Clamp01(c.Age / 0.12f); // çatlak bir anda yayılır
                    c.R.transform.localScale = Vector3.one * s * (0.3f + 0.7f * open);
                    c.R.color = new Color(1f, 1f, 1f, heal);
                }
                if (c.Age >= CrackHealSeconds)
                {
                    if (c.R != null) Destroy(c.R.gameObject);
                    if (!KatKat.UI.Motion.Reduced)
                        for (int j = 0; j < 8; j++)
                            SpawnFoam(c.P, new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(0.5f, 1.8f)) * _pitch, 0.07f);
                    _cracks.RemoveAt(i);
                    CrackHealed?.Invoke();
                }
            }
        }

        /// <summary>Çatlaktan fışkıran su: yana çıkar, yerçekimiyle kıvrılıp aşağı akar (akış hızıyla kalınlaşır, uzar).</summary>
        private void LeakRibbons()
        {
            if (_flyT >= 0f) return; // kova uçarken sızıntı şeridi oyukta kalmasın
            foreach (var c in _cracks)
            {
                if (c.Flow < 0.03f) continue;
                _rp.Clear();
                float v = _pitch * (1.2f + 2.2f * c.Flow), g = _pitch * 20f;
                for (int i = 0; i <= 14; i++)
                {
                    float t = i / 14f * 0.55f;
                    _rp.Add(c.P + new Vector3(c.Dir * v * t, -0.5f * g * t * t, 0f));
                }
                Ribbon(_rp, _pitch * (0.06f + 0.16f * c.Flow), i => v + g * i / 14f * 0.55f, false, false);
            }
        }

        private void ClearCracks()
        {
            foreach (var c in _cracks) if (c.R != null) Destroy(c.R.gameObject);
            _cracks.Clear();
        }
    }
}
