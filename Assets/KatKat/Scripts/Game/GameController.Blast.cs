using System.Collections.Generic;
using KatKat.Core;
using KatKat.View;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// 2026-11-26 (kullanıcı: "Normal Taş Patlatma ve Hedefe Taş Toplama Animasyonu"): ortak taş patlamasının oyun tarafı — hedef
    /// kotası (görevin hamledeki gerçek ilerlemesi kadar kopya; fazla taş yalnız patlar, tamamlanmış göreve hiç uçmaz), varışta
    /// HUD sayacı ve toplama sesi (gruplanmış, aralıklı, ses sayısı sınırlı, hafif perde değişimli), görev dolunca tek seferlik ses
    /// ve titreşim. Mantık RulesEngine'de olmuştur; burası yalnız gösterim.
    /// </summary>
    public sealed partial class GameController
    {
        /// <summary>Son oynatılan hamleden sonraki mantıksal görev ilerlemesi (bir sonraki hamlenin "önce" değeri).</summary>
        private int[] _goalBase;

        private void GoalBaseReset()
        {
            var goals = _session?.State.Goals;
            if (goals == null) { _goalBase = null; return; }
            _goalBase = new int[goals.Count];
            for (int g = 0; g < goals.Count; g++) _goalBase[g] = goals[g].Progress;
        }

        /// <summary>
        /// Bu hamlenin dalga dalga hedef kotası: her renk görevi için dalgadaki görünen ilerleme farkı (tavan görev miktarı).
        /// Torba bölümünde görev teslimle ilerler: kopya yok.
        /// </summary>
        private System.Func<PeelWave, List<(int Goal, LayerColor Color, int Count)>> GoalQuotaFor()
        {
            var goals = _session.State.Goals;
            if (_goalBase == null || _goalBase.Length != goals.Count) GoalBaseReset();
            var prev = (int[])_goalBase.Clone();
            for (int g = 0; g < goals.Count; g++) _goalBase[g] = goals[g].Progress;
            if (BucketLevel) return null;
            return w =>
            {
                var list = new List<(int, LayerColor, int)>();
                if (w.GoalProgressAfter == null) return list;
                for (int g = 0; g < goals.Count && g < w.GoalProgressAfter.Length; g++)
                {
                    int after = w.GoalProgressAfter[g];
                    int add = Mathf.Min(after, goals[g].Amount) - Mathf.Min(prev[g], goals[g].Amount);
                    prev[g] = after;
                    if (goals[g].Type == GoalType.CollectColor && add > 0) list.Add((g, goals[g].Color, add));
                }
                return list;
            };
        }

        /// <summary>Yakalama/test: görev ilerlemesini verir (mantık, HUD ve kota birlikte); ör. "görev bu hamlede dolsun".</summary>
        public void AuditSetGoalProgress(int goal, int progress)
        {
            var goals = _session?.State.Goals;
            if (goals == null || goal < 0 || goal >= goals.Count) return;
            goals[goal].Progress = progress;
            _hud.SetGoalsImmediate(goals);
            GoalBaseReset();
        }

        private float _collectLast = -10f;
        private int _collectRun;
        private readonly Queue<float> _collectVoices = new Queue<float>();

        /// <summary>Kopya görev simgesine vardı: sayaç (mantıksal değeri aşmaz), ses; görev dolduysa true.</summary>
        private bool OnGoalCopyArrived(int goal)
        {
            var goals = _session?.State.Goals;
            if (goals == null || goal < 0 || goal >= goals.Count) return false;
            bool complete = _hud.GoalArrive(goal, goals[goal].Progress);
            // 2026-11-27 harman: tek tek varış sesi eski akıştan (BoardView → StreamTick); burada yalnız görev dolunca.
            if (complete || !BoosterFxConfig.Current.Blast.OldStreamSound) CollectSound(complete);
            return complete;
        }

        private void CollectSound(bool complete)
        {
            var c = BoosterFxConfig.Current.Blast;
            float now = Time.unscaledTime;
            if (complete)
            {
                // Görev tamamlandı: kısa mutlu ses ve titreşim (tek sefer).
                if (!_sound.PlayV17("star-gain", 3f, c.CompleteVolume)) _sound.Play(Sfx.Star, 0, c.CompleteVolume);
                Haptics.Medium();
                _collectLast = now;
                return;
            }
            // Çok taş aynı anda varırsa sesler birleşir: en az CollectGapMs aralık, 250 ms içinde en çok CollectVoices ses.
            if ((now - _collectLast) * 1000f < c.CollectGapMs) return;
            while (_collectVoices.Count > 0 && now - _collectVoices.Peek() > 0.25f) _collectVoices.Dequeue();
            if (_collectVoices.Count >= Mathf.Max(1, c.CollectVoices)) return;
            if (now - _collectLast > 0.45f) _collectRun = 0;
            float pitch = Mathf.Min(7f, c.CollectPitch + _collectRun * c.CollectPitchStep) + Random.Range(-c.CollectPitchJitter, c.CollectPitchJitter);
            _collectRun++;
            _collectLast = now;
            _collectVoices.Enqueue(now);
            if (!_sound.PlayV17("goal-collect", pitch, c.CollectVolume)) _sound.Play(Sfx.Coin, 0, c.CollectVolume);
        }
    }
}
