using System.Collections.Generic;
using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Boşta ipucu (kullanıcı, 2026-11-21: "oyuncu uzun süre işlem yapmaz ise ipucu olsun diye patlatılabilinecek taşları
    /// dikkat çekici yapalım yanıp sönme gibi olsun"; sonra "ışıltıdan ziyade büyüyebilir büyüyüp küçülsün"): grubun taşları
    /// ışıksız, yumuşak nabızla büyüyüp küçülür (en çok %15).
    /// </summary>
    public sealed partial class BoardView
    {
        private readonly List<SpriteRenderer> _idleGlows = new List<SpriteRenderer>(), _idleFrames = new List<SpriteRenderer>(), _idleCores = new List<SpriteRenderer>();
        private readonly List<StackView> _idleStacks = new List<StackView>();
        private float _idleT;
        /// <summary>Yanıp sönme periyodu (sn).</summary>
        public const float IdleBlink = 0.9f;
        /// <summary>Nabızdaki en büyük ek boy (taş boyu oranı).</summary>
        public const float IdleGrow = 0.15f;

        public bool IdleHintVisible => _idleStacks.Count > 0;

        /// <summary>Yakalama/testler: bütün yığınlar.</summary>
        public void ForEachStackForTest(System.Action<StackView> a) { foreach (var s in _stacks) if (s != null && s.gameObject.activeSelf) a(s); }

        /// <summary>Yakalama/testler: yanıp sönmeyi verilen süre (sn) ilerletir.</summary>
        public void AdvanceIdleHint(float sec) { if (_idleStacks.Count > 0) TickIdleHint(sec); }

        public void ShowIdleHint(IList<Coord> cells)
        {
            HideIdleHint();
            if (cells == null) return;
            _idleT = 0f;
            for (int i = 0; i < cells.Count; i++)
            {
                var s = StackAt(cells[i].X, cells[i].Y);
                if (s == null) continue;
                _idleStacks.Add(s);
                if (_idleGlows.Count < _idleStacks.Count)
                {
                    var g = new GameObject("IdleGlow").AddComponent<SpriteRenderer>();
                    g.transform.SetParent(transform, false);
                    g.sprite = SpecialArt.Glow;
                    _idleGlows.Add(g);
                    var f = new GameObject("IdleFrame").AddComponent<SpriteRenderer>();
                    f.transform.SetParent(transform, false);
                    f.sprite = HintFrame;
                    _idleFrames.Add(f);
                    var c = new GameObject("IdleCore").AddComponent<SpriteRenderer>();
                    c.transform.SetParent(transform, false);
                    c.sprite = SpecialArt.Glow;
                    _idleCores.Add(c);
                }
            }
            TickIdleHint(0f);
        }

        public void HideIdleHint()
        {
            foreach (var s in _idleStacks) if (s != null) s.transform.localScale = Vector3.one;
            _idleStacks.Clear();
            foreach (var g in _idleGlows) if (g != null) g.gameObject.SetActive(false);
            foreach (var f in _idleFrames) if (f != null) f.gameObject.SetActive(false);
            foreach (var c in _idleCores) if (c != null) c.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_idleStacks.Count > 0) TickIdleHint(Time.unscaledDeltaTime);
        }

        private void TickIdleHint(float dt)
        {
            _idleT += dt;
            // Kullanıcı (2026-11-21): "ışıltıdan ziyade büyüyebilir büyüyüp küçülsün" — ışık yok; grubun taşları yumuşak
            // nabızla büyüyüp küçülür. Azaltılmış harekette yalnız hafif büyük durur.
            bool still = KatKat.UI.Motion.Reduced;
            float pulse = still ? 0.4f : 0.5f - 0.5f * Mathf.Cos(_idleT / IdleBlink * 2f * Mathf.PI);
            foreach (var g in _idleGlows) if (g != null) g.gameObject.SetActive(false);
            foreach (var f in _idleFrames) if (f != null) f.gameObject.SetActive(false);
            foreach (var c in _idleCores) if (c != null) c.gameObject.SetActive(false);
            for (int i = 0; i < _idleStacks.Count; i++)
            {
                var s = _idleStacks[i];
                if (s == null) continue;
                s.transform.localScale = Vector3.one * (1f + IdleGrow * pulse);
            }
        }
    }
}
