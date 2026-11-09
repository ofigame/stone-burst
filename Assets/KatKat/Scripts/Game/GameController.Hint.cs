using System.Collections.Generic;
using KatKat.Core;
using KatKat.Levels;
using KatKat.Meta;
using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Reklamlı ipucu (kullanıcı, 2026-11-08: "1 ipucu hakkı alma özelliği koyalım ama reklam izleyerek olsun. ipucu
    /// alındığında o bölümün yıldız ilerlemesinden 1 yıldız düşürsün").
    /// - Yalnız normal bölümde; deneme başına 1 ipucu. Ödüllü reklam tamamlanınca sıradaki iyi dokunuş halkayla gösterilir.
    /// - Bedel: o bölümün kazanılan yıldızı 1 azalır (en az 1). Oturum reklam yardımlı sayılır (sıralama yıldızı ve ZPE
    ///   mevcut kuralla: reklamlı kazanım sıralamaya yazılmaz).
    /// - Hamle seçimi: önce kısa bütçeli çözücü (ilk hamle dokunuşsa); bulunamazsa hedefe en çok katkı yapan dokunuş.
    /// </summary>
    public sealed partial class GameController
    {
        private bool _paidHintUsed;
        private Coord? _paidHint;

        /// <summary>Bu denemede reklamlı ipucu alındı mı (testler ve sonuç kartı).</summary>
        public bool PaidHintUsed => _paidHintUsed;
        /// <summary>Gösterilen reklamlı ipucu hücresi (önerilen taş oynanınca söner; o zamana kadar düğmeyle yeniden gösterilir).</summary>
        public Coord? PaidHintCell => _paidHint;
        /// <summary>Testler: ipucu halkası ekranda mı.</summary>
        public bool HintRingVisible => _hint != null && _hint.Visible;

        /// <summary>Kazanılan yıldız: puan yıldızı − ipucu bedeli, en az 1.</summary>
        private int EarnedStars => Mathf.Max(1, _session.State.Stars - (_paidHintUsed ? 1 : 0));

        private bool HintAllowed => _session != null && _sessionKind == SessionKind.Normal && !_session.State.IsTerminal;

        private void ResetPaidHint()
        {
            _paidHintUsed = false;
            _paidHint = null;
            _hud.SetHintButton(HintAllowed, false);
        }

        private void OnHintPressed()
        {
            if (!HintAllowed || CurrentPhase != Phase.Idle) return;
            if (_paidHintUsed)
            {
                // Kullanıcı (2026-11-09): "ipucunu aldığımda o ipucuna tekrar tekrar bakabilmem lazım". Önerilen taş
                // oynanana kadar düğme reklamsız aynı ipucunu yeniden gösterir; oynandıysa hak bitti bildirimi.
                if (_paidHint.HasValue) { RefreshHint(); _hud.ShowToast(Loc.T("hint.look"), 2.4f); }
                else _hud.ShowToast(Loc.T("hint.used"), 2f);
                return;
            }
            Ads.Show(AdPlacements.Hint, (o, token) =>
            {
                if (o == AdOutcome.Rewarded && HintAllowed) { GrantHint(); return; }
                if (o == AdOutcome.Failed) _hud.ShowToast(Loc.T("ad.nofill"), 2.4f);
            });
        }

        /// <summary>Reklam ödülü: ipucu hakkı işlenir, sıradaki dokunuş gösterilir, bedel bildirilir.</summary>
        private void GrantHint()
        {
            _paidHintUsed = true;
            OutcomeAssist(AssistFlags.RewardedAd);
            var move = FindHintMove(_session.State);
            _paidHint = move.HasValue ? new Coord(move.Value.X, move.Value.Y) : (Coord?)null;
            _hud.SetHintButton(true, !_paidHint.HasValue);
            RefreshHint();
            _hud.ShowToast(move.HasValue ? Loc.T("hint.got") : Loc.T("hint.none"), 2.6f);
        }

        /// <summary>
        /// Kabul edilen her hamleden sonra: önerilen taşa dokunulduysa ipucu tükenir. Başka hamle yapıldıysa ipucu
        /// kalır; önerilen dokunuş artık geçersizse (taş değişti) yeni duruma göre bir kez yeniden hesaplanır.
        /// Halka oynatma süresince gizlidir, tahta durunca (<see cref="Phase.Idle"/>) yeniden görünür.
        /// </summary>
        private void PaidHintAfterMove(Coord? tapped)
        {
            if (!_paidHint.HasValue) return;
            if (tapped.HasValue && tapped.Value.X == _paidHint.Value.X && tapped.Value.Y == _paidHint.Value.Y)
            {
                _paidHint = null;
                _hud.SetHintButton(HintAllowed, true);
                return;
            }
            var s = _session.State;
            if (s.IsTerminal) { _paidHint = null; return; }
            var probe = s.Clone();
            if (!RulesEngine.TryApply(probe, Command.Tap(_paidHint.Value.X, _paidHint.Value.Y)).Accepted)
            {
                var move = FindHintMove(s);
                _paidHint = move.HasValue ? new Coord(move.Value.X, move.Value.Y) : (Coord?)null;
                _hud.SetHintButton(HintAllowed, !_paidHint.HasValue);
            }
        }

        /// <summary>Test: reklam ödülünü doğrudan uygula.</summary>
        public void PressHintForTest() => OnHintPressed();

        /// <summary>
        /// Sıradaki önerilen dokunuş. Çözücü (sınırlı bütçe) ilk hamlesi dokunuşsa o; değilse her grubun temsilci
        /// dokunuşu denenir ve hedef ilerlemesi (tamamlanan hedef ağır), zincir dalgası ve puanla en iyisi seçilir.
        /// </summary>
        public static Command? FindHintMove(LevelState s, int budget = 40000)
        {
            if (s == null || s.IsTerminal) return null;
            var solved = LevelSolver.Solve(s, budget);
            if (solved.Outcome == SolveOutcome.Solved && solved.Solution.Count > 0 && solved.Solution[0].Type == CommandType.Tap)
                return solved.Solution[0];
            Command? best = null;
            float bestScore = float.MinValue;
            foreach (var c in LevelSolver.Moves(s))
            {
                if (c.Type != CommandType.Tap) continue;
                var next = s.Clone();
                var r = RulesEngine.TryApply(next, c);
                if (!r.Accepted) continue;
                float score = GoalValue(next) - GoalValue(s);
                if (next.Status == GameStatus.Won) score += 10000f;
                score += r.Waves.Count * 3f + (next.Score - s.Score) * 0.01f;
                if (score > bestScore) { bestScore = score; best = c; }
            }
            return best;
        }

        private static float GoalValue(LevelState s)
        {
            float v = 0f;
            foreach (var g in s.Goals)
            {
                v += Mathf.Min(g.Progress, g.Amount) * 10f;
                if (g.IsComplete) v += 40f;
            }
            return v;
        }
    }
}
