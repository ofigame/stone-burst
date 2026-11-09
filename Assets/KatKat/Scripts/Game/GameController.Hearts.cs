using System;
using KatKat.Meta;
using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Kalp sistemi (kullanıcı, 2026-11-09; kurallar <see cref="Hearts"/>). Ana sayfada logo ile yıldız arasında
    /// kalp + sayı + "Dolu"/geri sayım; dokununca kalp kartı (reklamla 5 kalp). Kalp, ana bölümde (21+) deneme
    /// kaybedilip bırakıldığında gider: ilk geçerli hamleden sonra yeniden başlatma, başka bölüme geçme ya da
    /// menüye dönme. Kazanınca, geri alıp sürdürünce, reklamla devam edince gitmez.
    /// </summary>
    public sealed partial class GameController
    {
        /// <summary>Testler için saat (Unix sn); null ise gerçek saat.</summary>
        public static Func<long> HeartClock;
        private static long HeartNow => HeartClock?.Invoke() ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>Bu denemede bir hamle yapıldı; bırakılırsa kalp gider.</summary>
        private bool _heartAtStake;

        public int HeartCount => Hearts.Count(_save.Data.meta, HeartNow);
        public bool HeartAtStake => _heartAtStake;

        private void SetupHearts()
        {
            _menu.HeartsInfo = () =>
            {
                var m = _save.Data.meta;
                long now = HeartNow;
                return new MenuView.HeartsModel { Count = Hearts.Count(m, now), Max = Hearts.Max, SecondsToNext = Hearts.SecondsToNext(m, now) };
            };
            _menu.HeartsPressed = () => ShowHeartsCard(null);
        }

        /// <summary>Normal oturumda kabul edilen ilk hamle denemeyi kalbe bağlar.</summary>
        private void HeartOnMove()
        {
            if (_sessionKind == SessionKind.Normal && Hearts.LevelUses(_levelIndex)) _heartAtStake = true;
        }

        /// <summary>Deneme kazanılmadan bırakıldı: kalp gider.</summary>
        private void HeartForfeit()
        {
            if (!_heartAtStake) return;
            _heartAtStake = false;
            if (_session != null && _session.State.Status == Core.GameStatus.Won) return;
            Hearts.Spend(_save.Data.meta, HeartNow);
            _save.Save();
        }

        private void HeartOnWon() => _heartAtStake = false;

        /// <summary>Bölüme giriş kapısı: kalp gerekmiyorsa ya da varsa true; yoksa haritaya dönüp kalp kartını açar.</summary>
        private bool HeartGate(int index, Action retry)
        {
            if (!Hearts.LevelUses(index) || HeartCount > 0) return true;
            GameAnalytics.Log("hearts_empty", ("level_number", index + 1));
            ShowLevels();
            ShowHeartsCard(retry);
            return false;
        }

        /// <summary>Kalp kartı: sayı, sonraki kalbe kalan süre, reklamla doldurma. Doldurulunca <paramref name="after"/>.</summary>
        private void ShowHeartsCard(Action after)
        {
            _menu.ShowHeartsCard(() =>
            {
                Ads.Show(AdPlacements.ExtraLife, (o, token) =>
                {
                    if (o == AdOutcome.Rewarded)
                    {
                        Hearts.Refill(_save.Data.meta);
                        _save.Save();
                        _menu.CloseHeartsCard();
                        _menu.ShowToast(Loc.T("hearts.got"), 2f);
                        after?.Invoke();
                        return;
                    }
                    if (o == AdOutcome.Failed) _menu.ShowToast(Loc.T("ad.nofill"), 2.4f);
                });
            });
        }

        /// <summary>Test: kalp kartındaki reklam düğmesi (sahte reklamla ödül).</summary>
        public void RefillHeartsForTest() => Ads.Show(AdPlacements.ExtraLife, (o, t) => { if (o == AdOutcome.Rewarded) { Hearts.Refill(_save.Data.meta); _save.Save(); } });
    }
}
