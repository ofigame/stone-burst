using KatKat.Core;
using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Su kipi akışı (2026-11-25, kullanıcı: "hücre dibindeki su tepsinin içine akacak ve tepsinin dibinden yukarıya doğru su
    /// akıttıkça artacak … Suyun doluluğuyla ilgili bir sayaç olmayacak. Gizli sayaç olacak ve o sayaca göre tepside seviye yükselecek
    /// ve tüm tepsi su dolunca bölüm bitecek"). Kural <see cref="RulesEngine"/>'de (hücre dibine kadar boşalınca su bir kez akar,
    /// <see cref="GoalType.FillWater"/>); tepsideki su <see cref="KatKat.View.BoardView"/>'de. Burada kurulum, eşitleme ve sesler:
    /// her dökülmede sıçrama (tepsi doldukça tizleşir), tepsi dolunca büyük sıçrama.
    /// </summary>
    public sealed partial class GameController
    {
        private GoalState WaterGoal()
        {
            if (_session == null) return null;
            foreach (var g in _session.State.Goals) if (g.Type == GoalType.FillWater) return g;
            return null;
        }

        public bool WaterLevel => WaterGoal() != null;

        private void WaterBegin()
        {
            var g = WaterGoal();
            _board.SetWater(g != null ? g.Amount : 0, g != null ? g.Progress : 0, _session.State.Board);
            _board.WaterArrived = g != null ? WaterArrived : (System.Action<int>)null;
            _hud.WaterPercent = g != null ? () => _board.WaterLevel01 : (System.Func<float>)null;
        }

        private void WaterArrived(int units)
        {
            var g = WaterGoal();
            if (g == null) return;
            float fill = g.Amount > 0 ? Mathf.Clamp01(_board.WaterShown / (float)g.Amount) : 0f;
            if (_board.WaterShown >= g.Amount)
            {
                if (!_sound.PlayV17("water-splash", -3f, 1f)) _sound.Play(Sfx.CoinFinal, 0, 0.85f);
                _sound.PlayV17("water-drop", 0f, 0.8f);
                Haptics.Medium();
                return;
            }
            if (_sound.PlayV17("water-splash", Mathf.Lerp(-2f, 5f, fill) + Random.Range(-0.5f, 0.5f), 0.75f + 0.08f * units)) Haptics.Tick();
            else { _sound.Play(Sfx.Coin, 0, 0.7f); Haptics.Tick(); }
            _sound.PlayV17("water-drop", Mathf.Lerp(-1f, 6f, fill), 0.5f);
        }

        private void WaterAfterPlay()
        {
            var g = WaterGoal();
            if (g != null) _board.WaterSettle(g.Progress);
        }

        private void WaterResync() => WaterBegin();
    }
}
