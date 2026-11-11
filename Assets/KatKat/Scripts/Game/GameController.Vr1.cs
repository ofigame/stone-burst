using System;
using System.Collections.Generic;
using KatKat.Meta;
using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// vr1 P1 prototipi (docs/vr1/claude_code_paketi_v1/belgeler/03_KOLEKSIYON_ODUL.md, 04_ANIMASYON_GORSEL_SES.md).
    /// Yalnız <see cref="Vr1Proto.On"/> iken görünür; kapalıyken oyun önceki gibidir. Kayıt gerçekleri (yıldız, tamamlama,
    /// kazanılan taş) mevcut akışta yazılır; buradaki sunum veri yazmaz (yalnız haritanın "son gördüğü" işaretini).
    /// </summary>
    public sealed partial class GameController
    {
        /// <summary>Haritadan bu yana kazanılan bölüm sayısı (tekrar galibiyet özeti için; bellekte).</summary>
        private int _winsSinceMap;

        private void SetupVr1()
        {
            Vr1Proto.Sound = n => _sound?.PlayVr1(n);
        }

        /// <summary>Taş hedefinin harita kartı verisi (sprite, ışıltı, buz ilerlemesi).</summary>
        private MenuView.StarGoalInfo GoalInfo(StarGoal g)
        {
            var info = new MenuView.StarGoalInfo();
            if (!g.Has || _stones == null) return info;
            var s = _stones.Find(g.StoneId);
            info.Has = true;
            info.Stars = g.Stars;
            info.From = g.From;
            info.To = g.To;
            // Kullanıcı (2026-11-08): kilidi açılmayı bekleyen taş yoğun bulanık (koleksiyondaki kilitli kartla aynı kaynak).
            KatKat.View.ResourceUsage.Note("Collection/stones_blur/" + g.StoneId);
            info.StoneSprite = Resources.Load<Sprite>("Collection/stones_blur/" + g.StoneId) ?? StoneSprite(s);
            info.Glow = GlowColor(s);
            info.Ice = Vr1Proto.On;
            if (info.Ice)
            {
                info.SharpSprite = StoneSprite(s);
                info.P = CollectionProgress.Progress(g);
            }
            return info;
        }

        /// <summary>
        /// Harita açılırken: kayıt farkından dönüş özeti ve taş kartı. Taş bu dönüşte kazanıldıysa kart kazanılan taşı
        /// gösterir, kabuk kırılır, sonra vitrin açılır ve harita sıradaki hedefle yenilenir. Dönen: vitrin ertelendi mi.
        /// </summary>
        private bool PrepareMapGoal()
        {
            var meta = _save.Data.meta;
            int stars = _save.Data.TotalStars, levels = _save.Data.completed.Count;
            var ret = CollectionProgress.Compare(meta.mapSeenStars, meta.mapSeenLevels, stars, levels, _winsSinceMap);
            if (meta.mapSeenStars != stars || meta.mapSeenLevels != levels)
            {
                meta.mapSeenStars = stars;
                meta.mapSeenLevels = levels;
                _save.Save();
            }
            _winsSinceMap = 0;
            var now = CollectionProgress.NextStarGoal(_stones, meta.stones, stars);
            _menu.Vr1Return = default;
            if (!Vr1Proto.On)
            {
                _menu.NextStarGoal = GoalInfo(now);
                return false;
            }
            var unlocked = new List<string>();
            foreach (var s in _pendingStoneToasts)
                if (s.Category == StoneCategory.StarStone && s.UnlockType == StoneUnlock.StarsTotal) unlocked.Add(s.StoneId);
            var before = CollectionProgress.NextStarGoal(_stones, meta.stones, ret.StarsBefore, unlocked);
            bool unlocking = ret.HasChange && before.Has && unlocked.Contains(before.StoneId);
            var shown = now;
            if (unlocking) { shown = before; shown.Stars = Mathf.Min(stars, before.To); }
            _menu.NextStarGoal = GoalInfo(shown);
            float fromP = before.Has && shown.Has && before.StoneId == shown.StoneId ? CollectionProgress.Progress(before) : 0f;
            float toP = unlocking ? 1f : CollectionProgress.Progress(shown);
            string summary = ret.NewLevels > 0 ? Loc.F("vr1.summary", ret.NewLevels, ret.NetStars)
                : ret.NetStars > 0 ? Loc.F("vr1.summary.stars", ret.NetStars)
                : ret.ReplayOnly ? Loc.T("vr1.replay") : null;
            _menu.Vr1Return = new MenuView.Vr1ReturnInfo
            {
                Has = ret.HasChange || ret.ReplayOnly,
                StarsBefore = ret.StarsBefore,
                StarsAfter = stars,
                NetStars = ret.NetStars,
                NewLevels = ret.NewLevels,
                ReplayOnly = ret.ReplayOnly,
                FromP = ret.HasChange ? fromP : toP,
                ToP = toP,
                Unlocking = unlocking,
                AfterReveal = unlocking ? (Action)(() => FlushStoneToasts(false, ShowLevels)) : null,
                Summary = summary,
            };
            return unlocking;
        }

        /// <summary>Bölüm başı (03 "Bölüm başı"): bu denemenin en çok net yeni yıldızı eşiği geçirebiliyorsa 2 sn bilgi.</summary>
        private void Vr1LevelStart()
        {
            if (!Vr1Proto.On || _sessionKind != SessionKind.Normal || _level == null || _stones == null) return;
            var g = CollectionProgress.NextStarGoal(_stones, _save.Data.meta.stones, _save.Data.TotalStars);
            if (!CollectionProgress.CanUnlockThisAttempt(g, _save.Data.StarsFor(_level.Id))) return;
            var s = _stones.Find(g.StoneId);
            if (s != null) _hud.ShowToast(Loc.F("vr1.can.unlock", Loc.T(s.NameKey)), 2f);
        }
    }
}
