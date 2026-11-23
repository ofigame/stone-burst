using System;
using System.Collections.Generic;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Süreli bölüm efektleri (kullanıcı, 2026-11-21): son 10 saniyede tahta kalp atışı gibi atar (taşlar "lüp-düp" büyür, her
    /// atışta çerçeveden kırmızı-turuncu kıvılcımlı ışık dalgası; atış gittikçe hızlanır); süre bitince büyük patlama, sarsıntı ve
    /// bütün taşlar dönerek aşağı düşer, ardından kayıp kartı.
    /// </summary>
    public sealed partial class BoardView
    {
        private float _beat, _beatClock, _beatLast = -1f;
        private bool _beating;

        /// <summary>Her kalp atışında (son 10 sn): oyun akışı ses ve titreşim çalar (kullanıcı, 2026-11-22: "geri sayım efektini titreşimli").</summary>
        public Action OnBeat;

        private struct Faller { public Transform T; public Vector3 V; public float Spin; }
        private readonly List<Faller> _fallers = new List<Faller>();
        private float _fallT = -1f;
        private Action _fallDone;
        private bool _fell; // taşlar düştü: yeniden başlatmada yerlerine döner (2026-11-23 hata: tahta boş görünüyor, taşlar görünmeden patlıyordu)

        /// <summary>Süre bitişi düşüşünü durdurur ve düşen taşları hücrelerine geri koyar (geri al / yeniden başla).</summary>
        private void ResetFall()
        {
            _fallT = -1f;
            _fallers.Clear();
            _fallDone = null;
            if (!_fell) return;
            _fell = false;
            for (int y = 0; y < _height; y++)
            for (int x = 0; x < _width; x++)
            {
                var s = StackAt(x, y);
                if (s == null) continue;
                s.transform.localPosition = CellCenter(x, y);
                s.transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>Kalp atışı şiddeti: 0 yok, 1 en hızlı (son saniye).</summary>
        public void SetHeartbeat(float u)
        {
            _beat = Mathf.Clamp01(u);
            if (_beat <= 0f && _beating)
            {
                _beating = false;
                foreach (var s in _stacks) if (s != null && s.gameObject.activeSelf) s.transform.localScale = Vector3.one;
            }
        }

        public bool HeartbeatOn => _beating;

        private void TickHeartbeat(float dt)
        {
            if (_beat <= 0f) return;
            _beating = true;
            // Periyot 1,0 sn → 0,45 sn; lüp (büyük) + düp (küçük).
            float period = Mathf.Lerp(1.0f, 0.45f, _beat);
            _beatClock += dt / period;
            float ph = _beatClock % 1f;
            float lub = Mathf.Exp(-Mathf.Pow((ph - 0.08f) / 0.07f, 2f));
            float dub = 0.6f * Mathf.Exp(-Mathf.Pow((ph - 0.3f) / 0.07f, 2f));
            float k = KatKat.UI.Motion.Reduced ? 0f : (lub + dub) * (0.05f + 0.05f * _beat);
            foreach (var s in _stacks) if (s != null && s.gameObject.activeSelf && s.Layers.Count > 0) s.transform.localScale = Vector3.one * (1f + k);
            int beatNo = Mathf.FloorToInt(_beatClock);
            if (beatNo != (int)_beatLast)
            {
                _beatLast = beatNo;
                OnBeat?.Invoke();
                RimSweep(new Color(1f, 0.3f, 0.15f), new Color(1f, 0.75f, 0.3f), 0.6f + 0.6f * _beat, fire: true);
            }
        }

        /// <summary>Süre bitti: patlama, sarsıntı, taşlar düşer; ~1,5 sn sonra <paramref name="done"/>.</summary>
        public void TimeUpBlast(Action done)
        {
            _fallDone = done;
            _fallT = 0f;
            _fell = true;
            _fallers.Clear();
            var center = _floorWorld.center;
            ShakeBoard(0.12f, 700f);
            if (!KatKat.UI.Motion.Reduced)
            {
                var pos = transform.TransformPoint(new Vector3(center.x, center.y, 0f));
                float sz = WorldPitch * 6f;
                int order = PeelDirector.PeelSortingOrder + 1200;
                Particles.Emit(FxKit.Glow, true, order, pos, Vector3.zero, 0.45f, sz * 0.3f, sz * 1.4f, new Color(1f, 0.95f, 0.8f, 1f), new Color(1f, 0.4f, 0.1f, 0f));
                Particles.Emit(FxKit.Ring, true, order + 1, pos, Vector3.zero, 0.6f, sz * 0.1f, sz * 1.6f, new Color(1f, 0.8f, 0.4f, 1f), new Color(1f, 0.3f, 0.1f, 0f));
                for (int i = 0; i < 60; i++)
                {
                    float a = i / 60f * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    var c = new Color(1f, UnityEngine.Random.Range(0.5f, 0.9f), 0.2f, 1f);
                    Particles.Emit(FxKit.Star, true, order + 2, pos, dir * sz * UnityEngine.Random.Range(0.6f, 1.3f), UnityEngine.Random.Range(0.5f, 0.9f),
                        WorldPitch * 0.5f, WorldPitch * 0.05f, c, new Color(c.r, c.g, c.b, 0f), drag: 1.6f);
                }
            }
            RimSweep(new Color(1f, 0.3f, 0.1f), new Color(1f, 0.8f, 0.3f), 1.6f, fire: true);
            foreach (var s in _stacks)
            {
                if (s == null || !s.gameObject.activeSelf) continue;
                var p = s.transform.localPosition;
                var away = new Vector3(p.x - center.x, p.y - center.y, 0f).normalized;
                _fallers.Add(new Faller
                {
                    T = s.transform,
                    V = (away * UnityEngine.Random.Range(1.5f, 3.5f) + Vector3.up * UnityEngine.Random.Range(2f, 5f)) * _pitch,
                    Spin = UnityEngine.Random.Range(-360f, 360f),
                });
            }
        }

        private void TickFall(float dt)
        {
            if (_fallT < 0f) return;
            _fallT += dt;
            for (int i = 0; i < _fallers.Count; i++)
            {
                var f = _fallers[i];
                if (f.T == null) continue;
                f.V.y -= 30f * _pitch * dt;
                f.T.localPosition += f.V * dt;
                f.T.localRotation = Quaternion.Euler(0f, 0f, f.T.localEulerAngles.z + f.Spin * dt);
                _fallers[i] = f;
            }
            if (_fallT >= 1.5f)
            {
                _fallT = -1f;
                _fallers.Clear();
                var d = _fallDone;
                _fallDone = null;
                d?.Invoke();
            }
        }

        /// <summary>
        /// Patlatılması yasak yığın soyuldu (kullanıcı, 2026-11-22: "yasaklı bomba grubunda yasaklı taşlara basarsam bomba da
        /// patlasın"): o yığınlardaki bombalar patlar (işaret kalkar, alev/kıvılcım, sarsıntı); ~0,9 sn sonra <paramref name="done"/>.
        /// </summary>
        public void ExplodeBombs(IList<KatKat.Core.Coord> cells, Action done)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                var st = StackAt(cells[i].X, cells[i].Y);
                if (st == null) continue;
                st.SetProtected(false);
                int k = i;
                if (k == 0) BombBlast(st);
                else At(k * 120f, () => BombBlast(st));
            }
            At(900f + cells.Count * 120f, () => done?.Invoke());
        }

        /// <summary>Testler/yakalama: patlama ve düşüşü ilerletir.</summary>
        public void AdvanceTimedFx(float sec)
        {
            for (float t = 0f; t < sec; t += 0.02f) { TickHeartbeat(0.02f); TickFall(0.02f); Advance(20f); }
        }
    }
}
