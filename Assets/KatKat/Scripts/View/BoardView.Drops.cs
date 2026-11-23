using System.Collections.Generic;
using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Yukarıdan dolma sunumu (kullanıcı 2026-11-24: "katlar boşaldıkça üstten alta dolsun … yeni taşlar da en üstten oyuna girsin").
    /// Sync son durumu çizdikten sonra kayan yığın eski hücresinden, yeni yığın tahtanın üstünden hedef hücresine düşer (hızlanarak,
    /// sonda küçük sekme). Mantık zaten sonuçta; atlamada ve yeniden başlatmada yığınlar yerine oturur.
    /// </summary>
    public sealed partial class BoardView
    {
        private struct Drop
        {
            public Transform T;
            public Vector3 From, To;
            public float Delay, Dur, Age;
            public bool Spawned;
        }

        private readonly List<Drop> _drops = new List<Drop>();

        /// <summary>Kayma sürüyor mu (testler).</summary>
        public bool Dropping => _drops.Count > 0;

        public void PlayDrops(IList<FallMove> falls)
        {
            FinishDrops();
            if (falls == null || falls.Count == 0) return;
            bool reduced = KatKat.UI.Motion.Reduced;
            foreach (var f in falls)
            {
                var s = StackAt(f.To.X, f.To.Y);
                if (s == null) continue;
                var to = CellCenter(f.To.X, f.To.Y);
                var from = CellCenter(f.From.X, f.From.Y);
                float rows = Mathf.Max(1f, f.To.Y - f.From.Y);
                var d = new Drop
                {
                    T = s.transform, From = from, To = to, Spawned = f.Spawned,
                    // Alttan üste: aşağıdakiler önce düşer; sütunlar hafif kaydırmalı.
                    Delay = reduced ? 0f : (_height - f.To.Y) * 0.025f + f.To.X * 0.012f,
                    Dur = reduced ? 0.01f : 0.14f + 0.06f * Mathf.Sqrt(rows),
                };
                s.transform.localPosition = from;
                if (f.Spawned) s.transform.localScale = Vector3.one * 0.6f;
                _drops.Add(d);
            }
        }

        private void TickDrops(float dt)
        {
            for (int i = _drops.Count - 1; i >= 0; i--)
            {
                var d = _drops[i];
                if (d.T == null) { _drops.RemoveAt(i); continue; }
                d.Age += dt;
                float t = Mathf.Clamp01((d.Age - d.Delay) / d.Dur);
                // Hızlanan düşüş, varışta küçük sekme.
                float e = t * t;
                var p = Vector3.LerpUnclamped(d.From, d.To, e);
                float settle = d.Age - d.Delay - d.Dur;
                if (settle > 0f) p = d.To + Vector3.up * (_rowPitch * 0.08f * Mathf.Sin(Mathf.Min(1f, settle / 0.12f) * Mathf.PI));
                d.T.localPosition = p;
                if (d.Spawned) d.T.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(t * 1.6f));
                _drops[i] = d;
                if (settle >= 0.12f)
                {
                    d.T.localPosition = d.To;
                    d.T.localScale = Vector3.one;
                    _drops.RemoveAt(i);
                }
            }
        }

        /// <summary>Kaymaları hemen bitirir (atlama, yeniden başlatma, yeni kayma).</summary>
        private void FinishDrops()
        {
            foreach (var d in _drops)
            {
                if (d.T == null) continue;
                d.T.localPosition = d.To;
                d.T.localScale = Vector3.one;
            }
            _drops.Clear();
        }
    }
}
