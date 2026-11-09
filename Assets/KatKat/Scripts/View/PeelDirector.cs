using System;
using System.Collections.Generic;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Etkin soyulmaları ilerletir ve soyulma ağlarını havuzda tutar.
    /// Etkin soyulma yokken Update hiçbir iş yapmaz.
    /// </summary>
    public sealed class PeelDirector : MonoBehaviour
    {
        public const int PeelSortingOrder = 1000;

        private readonly List<PeelSequence> _active = new List<PeelSequence>(32);
        private readonly List<float> _started = new List<float>(32);
        private readonly Stack<PeelSheet> _pool = new Stack<PeelSheet>();
        private float _clock;

        /// <summary>1 normal hız; yavaş çekim incelemesi için küçültülebilir.</summary>
        public float TimeScale { get; set; } = 1f;
        public int ActiveCount => _active.Count;
        public float Clock => _clock;

        public PeelSequence Peel(StackView stack, Vector3 goalWorld, Action<PeelSequence> onArrive, float delayMs = 0f)
            => Peel(stack, goalWorld, goalWorld, 1f, onArrive, delayMs);

        /// <summary>v10 taş kırılması: parçaların <paramref name="goalShare"/> oranı görev simgesine, kalanı yıldız çubuğuna.</summary>
        public PeelSequence Peel(StackView stack, Vector3 goalWorld, Vector3 starWorld, float goalShare, Action<PeelSequence> onArrive, float delayMs = 0f)
        {
            var sheet = _pool.Count > 0 ? _pool.Pop() : PeelSheet.Create(transform);
            var seq = new PeelSequence(stack, sheet, goalWorld, starWorld, goalShare, PeelSortingOrder + _active.Count * 12, s =>
            {
                s.Sheet.transform.SetParent(transform, false);
                _pool.Push(s.Sheet);
                onArrive?.Invoke(s);
            }, transform);
            _active.Add(seq);
            _started.Add(_clock + delayMs);
            return seq;
        }

        /// <summary>Bütün soyulmaları anında bitirir; görünüm mantıksal son durumdan yeniden çizilebilir.</summary>
        public void CompleteAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--) _active[i].Complete();
            _active.Clear();
            _started.Clear();
        }

        private void Update()
        {
            if (_active.Count > 0) Advance(Time.unscaledDeltaTime * 1000f * TimeScale);
        }

        /// <summary>Soyulmaları verilen süre (ms) kadar ilerletir. Update ve ekran yakalama aracı kullanır.</summary>
        public void Advance(float ms)
        {
            if (_active.Count == 0) return;
            _clock += ms;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                float t = _clock - _started[i];
                if (t < 0f) continue;
                _active[i].Evaluate(t);
                if (_active[i].IsDone)
                {
                    _active.RemoveAt(i);
                    _started.RemoveAt(i);
                }
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) CompleteAll();
        }
    }
}
