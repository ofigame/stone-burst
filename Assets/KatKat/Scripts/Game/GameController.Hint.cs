using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KatKat.Core;
using KatKat.Levels;
using KatKat.Meta;
using KatKat.UI;
using KatKat.View;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Reklamlı ipucu (kullanıcı, 2026-11-08: "1 ipucu hakkı alma özelliği koyalım ama reklam izleyerek olsun. ipucu
    /// alındığında o bölümün yıldız ilerlemesinden 1 yıldız düşürsün").
    /// vr1 P0 (docs/vr1/claude_code_paketi_v1/belgeler/01_IPUCU.md, onaylı güvenilirlik düzeltmesi, 2026-11-11):
    /// - Öneri yalnız <see cref="HintPlanner"/>'ın motorla yeniden yürütüp kazanca ulaştığını doğruladığı yolun ilk
    ///   komutudur (dokunuş, tepsi, döndürme, boya, rulo). Tahmine (açgözlü seçim) geçilmez.
    /// - Akış: Hazırlanıyor (tahta kopyasında arka plan iş parçacığı, 40k düğüm + 750 ms) → plan yoksa reklam açılmaz,
    ///   hak/yıldız/yardım değişmez → plan varsa reklam → ödül gelince plan mevcut tahtada yeniden doğrulanır → teslim.
    /// - Bedel (−1 yıldız, en az 1) ve "reklam yardımlı" bayrağı yalnız geçerli öneri oyuncuya TESLİM edilince işlenir.
    ///   Reklam iptal/başarısız: hiçbir şey değişmez. Yinelenen ödül geri çağrısı tek teslim üretir.
    /// - Öneri uygulanana kadar düğme reklamsız aynı planı gösterir. Başka hamle/geri alma sonrası plan gizlenir ve aynı
    ///   ödenmiş hakla ücretsiz yeniden çözülür. Uygulanmış sayılması: hamleden sonraki durum planın beklediği durumla
    ///   aynı (aynı grubun başka hücresi dahil; sonucu farklı olan dokunuş sayılmaz).
    /// - Deneme başına bir hak: yeniden başlatma yeni denemedir (hak ve bedel sıfırlanır).
    /// </summary>
    public sealed partial class GameController
    {
        private enum HintStage { Idle, Preparing, AdPending }

        private HintStage _hintStage;
        /// <summary>Öneri teslim edildi: yıldız bedeli ve reklam yardımı işlendi.</summary>
        private bool _paidHintUsed;
        /// <summary>Bu denemede ödüllü reklam izlendi (hak oyuncuda; teslim bekliyor olabilir).</summary>
        private bool _hintCredit;
        /// <summary>Önerilen komut uygulandı: hak bitti.</summary>
        private bool _hintConsumed;
        /// <summary>Gösterilen ücretli plan (yalnız çıkarıldığı durumda geçerli).</summary>
        private HintPlan _hintPlan;
        /// <summary>Ücretsiz kayıp ipucu planı (2 kayıp sonrası; bedelsiz).</summary>
        private HintPlan _freePlan;
        /// <summary>Uygulanan planın kalan yolu (sonraki yeniden çözümde ilk aday).</summary>
        private List<Command> _hintRemaining;
        private int _hintAttempt, _hintPlanSeq, _hintAdToken;
        /// <summary>Son başarısız planın durumu: aynı tahtada otomatik yeniden deneme ve bildirim tekrarı yok.</summary>
        private string _hintFailedKey;

        private Task<HintPlan> _hintJob;
        private CancellationTokenSource _hintCts;
        private int _hintJobAttempt;
        private string _hintJobKey;
        private bool _hintJobOffer, _hintJobLoud;

        /// <summary>Bu denemede reklamlı ipucu teslim edildi mi (yıldız bedeli işlendi).</summary>
        public bool PaidHintUsed => _paidHintUsed;
        /// <summary>Gösterilen ücretli planın ilk komutu (geçerli değilse null).</summary>
        public Command? PaidHintCommand => PlanCurrent(_hintPlan) ? _hintPlan.Command : (Command?)null;
        /// <summary>Gösterilen ücretli plan dokunuşsa hücresi (testler ve eski çağrılar).</summary>
        public Coord? PaidHintCell => PaidHintCommand is Command c && c.Type == CommandType.Tap ? new Coord(c.X, c.Y) : (Coord?)null;
        /// <summary>Testler: ipucu halkası ekranda mı.</summary>
        public bool HintRingVisible => _hint != null && _hint.Visible;
        /// <summary>Testler: plan hazırlanıyor mu.</summary>
        public bool HintPreparing => _hintStage == HintStage.Preparing;
        /// <summary>Testler: ödenmiş/kredili hak var ama öneri uygulanmadı.</summary>
        public bool HintCreditOpen => _hintCredit && !_hintConsumed;
        public bool HintConsumed => _hintConsumed;
        /// <summary>Testler: ücretsiz kayıp ipucunun gösterdiği komut.</summary>
        public Command? FreeHintCommand => PlanCurrent(_freePlan) ? _freePlan.Command : (Command?)null;

        /// <summary>Kazanılan yıldız: puan yıldızı − ipucu bedeli, en az 1.</summary>
        private int EarnedStars => Mathf.Max(1, _session.State.Stars - (_paidHintUsed ? 1 : 0));

        private bool HintAllowed => _session != null && _sessionKind == SessionKind.Normal && !_session.State.IsTerminal;

        /// <summary>Plan arama bütçesi (01_IPUCU taslağı; cihaz ölçümüyle ayarlanacak).</summary>
        public static int HintNodeBudget = HintPlanner.DefaultNodeBudget;
        public static long HintMillisBudget = HintPlanner.DefaultMillisBudget;

        /// <summary>Yeni deneme (Begin, Restart): hak, bedel, plan ve bekleyen iş sıfırlanır.</summary>
        private void ResetPaidHint()
        {
            CancelHintJob();
            _hintAttempt++;
            _hintAdToken++; // önceki denemenin geç gelen reklam geri çağrısı yok sayılır
            _hintStage = HintStage.Idle;
            _paidHintUsed = _hintCredit = _hintConsumed = false;
            _hintPlan = _freePlan = null;
            _hintRemaining = null;
            _hintFailedKey = null;
            _hud.HideHintMark();
            _hud.SetHintButton(HintAllowed, false);
        }

        private void CancelHintJob()
        {
            _hintCts?.Cancel();
            _hintCts = null;
            _hintJob = null;
        }

        private bool PlanCurrent(HintPlan p) =>
            p != null && p.IsVerified && _session != null && !_session.State.IsTerminal && LevelSolver.StateKey(_session.State) == p.StateHash;

        private void OnHintPressed()
        {
            if (!HintAllowed || CurrentPhase != Phase.Idle) return;
            if (_hintStage != HintStage.Idle) { _hud.ShowToast(Loc.T("hint.preparing"), 1.6f); return; }
            if (_hintConsumed) { _hud.ShowToast(Loc.T("hint.used"), 2f); return; }
            // Kullanıcı (2026-11-09): "ipucunu aldığımda o ipucuna tekrar tekrar bakabilmem lazım".
            if (PlanCurrent(_hintPlan)) { RefreshHint(); _hud.ShowToast(LookText(_hintPlan.Command), 2.4f); return; }
            if (_hintFailedKey != null && _hintFailedKey == LevelSolver.StateKey(_session.State))
            {
                _hud.ShowToast(Loc.T(_hintCredit ? "hint.unavailable.kept" : "hint.unavailable"), 3f);
                return;
            }
            GameAnalytics.Log("hint_request", ("level_number", AnalyticsLevelNumber), ("owned", _hintCredit ? 1 : 0));
            StartHintJob(offer: !_hintCredit, loud: true);
        }

        /// <summary>Plan işini tahtanın sabit kopyasıyla başlatır (Unity nesnelerine dokunmaz).</summary>
        private void StartHintJob(bool offer, bool loud)
        {
            CancelHintJob();
            var snapshot = _session.State.Clone();
            var candidates = new List<IList<Command>>();
            if (_hintRemaining != null) candidates.Add(new List<Command>(_hintRemaining));
            if (_level != null && _level.Solution.Count > 0)
            {
                var saved = new List<Command>();
                try { foreach (var st in _level.Solution) saved.Add(LevelLoader.ToCommand(st)); candidates.Add(saved); }
                catch (System.FormatException) { }
            }
            int nodes = HintNodeBudget;
            long ms = HintMillisBudget;
            var cts = _hintCts = new CancellationTokenSource();
            _hintJobAttempt = _hintAttempt;
            _hintJobKey = LevelSolver.StateKey(snapshot);
            _hintJobOffer = offer;
            _hintJobLoud = loud;
            _hintStage = HintStage.Preparing;
            if (loud) _hud.ShowToast(Loc.T("hint.preparing"), 1.6f);
            _hintJob = Task.Run(() => HintPlanner.Plan(snapshot, nodes, ms, candidates, cts.Token));
        }

        /// <summary>Update'ten: biten plan işini ana iş parçacığında işler.</summary>
        private void HintTick()
        {
            var job = _hintJob;
            if (job == null || !job.IsCompleted) return;
            _hintJob = null;
            _hintCts = null;
            _hintStage = HintStage.Idle;
            if (_hintJobAttempt != _hintAttempt || _session == null || !HintAllowed) return; // eski deneme/sahne
            if (LevelSolver.StateKey(_session.State) != _hintJobKey)
            {
                // Hazırlanırken tahta değişti: eski plan uygulanmaz; gerekiyorsa yeni durum için yeniden çöz.
                if (_hintJobLoud || HintCreditOpen) StartHintJob(_hintJobOffer, _hintJobLoud);
                return;
            }
            var plan = job.Status == TaskStatus.RanToCompletion ? job.Result : null;
            if (plan == null) plan = new HintPlan { Status = HintPlanStatus.Cancelled };
            plan.PlanId = ++_hintPlanSeq;
            plan.LevelId = _level?.Id;
            plan.ContentVersion = ContentVersions.Levels;
            GameAnalytics.Log("hint_plan_result", ("level_number", AnalyticsLevelNumber), ("status", plan.Status.ToString().ToLowerInvariant()),
                ("nodes", plan.Nodes), ("ms", (int)plan.Millis), ("command", plan.IsVerified ? plan.Command.Type.ToString().ToLowerInvariant() : "none"));
            if (!plan.IsVerified)
            {
                GameAnalytics.Log("hint_unavailable", ("level_number", AnalyticsLevelNumber), ("status", plan.Status.ToString().ToLowerInvariant()),
                    ("owned", _hintCredit ? 1 : 0));
                // Oyunu çözümsüz ilan etme; hak ve yıldız değişmedi.
                _hintFailedKey = _hintJobKey;
                _hud.ShowToast(Loc.T(_hintCredit ? "hint.unavailable.kept" : "hint.unavailable"), 3f);
                RefreshHint();
                return;
            }
            if (_hintJobOffer) ShowHintAd(plan);
            else DeliverHint(plan);
        }

        /// <summary>Plan hazır: ödüllü reklam. Bedel (−1 ★) düğme rozetinde reklamdan önce görünür.</summary>
        private void ShowHintAd(HintPlan plan)
        {
            int token = ++_hintAdToken, attempt = _hintAttempt;
            bool handled = false;
            _hintStage = HintStage.AdPending;
            Ads.Show(AdPlacements.Hint, (o, t) =>
            {
                if (handled || token != _hintAdToken) return; // yinelenen ya da eskimiş geri çağrı
                handled = true;
                _hintStage = HintStage.Idle;
                if (o != AdOutcome.Rewarded)
                {
                    if (o == AdOutcome.Failed) _hud.ShowToast(Loc.T("ad.nofill"), 2.4f);
                    return; // iptal/başarısız: bayrak, yıldız, hak değişmez
                }
                if (attempt != _hintAttempt || !HintAllowed) return; // deneme/sahne değişti: ücret işlenmez
                _hintCredit = true;
                if (PlanCurrent(plan) && HintPlanner.Verify(_session.State, plan.Path)) DeliverHint(plan);
                else StartHintJob(offer: false, loud: true); // telafi: aynı denemede ücretsiz yeni plan
            });
        }

        /// <summary>Geçerli öneri oyuncuya teslim: ilk teslimde bedel ve reklam yardımı işlenir.</summary>
        private void DeliverHint(HintPlan plan)
        {
            bool first = !_paidHintUsed;
            _paidHintUsed = true;
            _hintCredit = true;
            OutcomeAssist(AssistFlags.RewardedAd);
            _hintPlan = plan;
            if (first) HintGrantsForTest++;
            GameAnalytics.Log(first ? "hint_granted" : "hint_revalidated", ("level_number", AnalyticsLevelNumber), ("plan_id", plan.PlanId),
                ("command", plan.Command.Type.ToString().ToLowerInvariant()));
            _hud.SetHintButton(true, false);
            RefreshHint();
            _hud.ShowToast(first ? GotText(plan.Command) : LookText(plan.Command), 2.6f);
        }

        private static string GotText(Command c) => c.Type == CommandType.Tap ? Loc.T("hint.got") : Loc.T("hint.got.other");
        private static string LookText(Command c) => c.Type == CommandType.Tap ? Loc.T("hint.look") : Loc.T("hint.look.other");

        /// <summary>
        /// Kabul edilen her oyuncu komutundan hemen sonra (oynatmadan önce): öneri uygulandıysa hak tükenir; başka hamle
        /// yapıldıysa plan düşer, tahta durunca aynı hakla ücretsiz yeniden çözülür. Ücretsiz kayıp ipucu her hamlede söner.
        /// </summary>
        private void HintAfterCommand()
        {
            _freePlan = null;
            _hud.HideHintMark();
            if (_hintPlan == null) return;
            if (HintPlanner.IsFollowed(_hintPlan, _session.State))
            {
                _hintRemaining = HintPlanner.Remaining(_hintPlan);
                _hintConsumed = true;
                GameAnalytics.Log("hint_consumed", ("level_number", AnalyticsLevelNumber), ("plan_id", _hintPlan.PlanId));
                _hintPlan = null;
                _hud.SetHintButton(HintAllowed, true);
                return;
            }
            _hintRemaining = null;
            _hintPlan = null;
        }

        /// <summary>SetPhase'ten: tahta durunca geçersiz planı yeniden çöz, görseli tazele; oynatmada gizle.</summary>
        private void HintOnPhase(Phase p)
        {
            if (p == Phase.Idle && HintAllowed && HintCreditOpen && _hintStage == HintStage.Idle && !PlanCurrent(_hintPlan)
                && _hintFailedKey != LevelSolver.StateKey(_session.State))
            {
                _hintPlan = null; // geri alma, ek hamle vb.: eski halka anında gizlenir
                StartHintJob(offer: false, loud: false);
            }
            if (p == Phase.Idle || p == Phase.Targeting) RefreshHint();
            else if (p == Phase.Animating || p == Phase.Ended) { if (!_hintActive) _hint.Hide(); _hud.HideHintMark(); }
        }

        /// <summary>Ücretli ya da ücretsiz planın ilk komutunu gösterir (tek görselleştirici; bütün komut türleri).</summary>
        private bool ShowPlanHint()
        {
            var plan = PlanCurrent(_hintPlan) ? _hintPlan : PlanCurrent(_freePlan) ? _freePlan : null;
            if (plan == null || CurrentPhase == Phase.Animating || CurrentPhase == Phase.Ended) return false;
            var c = plan.Command;
            var s = _session.State;
            switch (c.Type)
            {
                case CommandType.Tap:
                    _hint.Show(_board.CellCenter(c.X, c.Y), _board.Pitch, true);
                    return true;
                case CommandType.Rotate:
                    _hint.Show(_board.CellCenter(c.X, c.Y) + _board.StackAt(c.X, c.Y).RotorLocal, _board.Pitch * 0.45f, true);
                    return true;
                case CommandType.UseSpecial:
                    // Önce yuva; aynı türde bir yuva seçilince hedef hücre. Yuva seçimi hakkı bitirmez.
                    bool picked = _traySlot >= 0 && _traySlot < s.Tray.Count && c.Slot < s.Tray.Count && s.Tray[_traySlot] == s.Tray[c.Slot];
                    if (picked) _hint.Show(_board.CellCenter(c.X, c.Y), _board.Pitch, true);
                    else { _hint.Hide(); _hud.ShowHintMark(HudView.HintUi.TraySlot, c.Slot); }
                    return true;
                case CommandType.Paint:
                    if (_paintMode) _hint.Show(_board.CellCenter(c.X, c.Y), _board.Pitch, true);
                    else { _hint.Hide(); _hud.ShowHintMark(HudView.HintUi.Paint); }
                    return true;
                case CommandType.Roll:
                    if (CurrentPhase == Phase.Targeting)
                    {
                        var targets = RulesEngine.RollTargets(s.Board, c.Row);
                        if (targets.Count == 0) { _hint.Hide(); return true; }
                        int i = targets[targets.Count / 2];
                        _hint.Show(_board.CellCenter(s.Board.X(i), s.Board.Y(i)), _board.Pitch, true);
                    }
                    else { _hint.Hide(); _hud.ShowHintMark(HudView.HintUi.Roll); }
                    return true;
            }
            return false;
        }

        /// <summary>Boya ipucu: önerilen hücre seçilince önerilen renk yazılır.</summary>
        private void HintOnPaintCell(Coord cell)
        {
            var plan = PlanCurrent(_hintPlan) ? _hintPlan : PlanCurrent(_freePlan) ? _freePlan : null;
            if (plan == null || plan.Command.Type != CommandType.Paint || plan.Command.X != cell.X || plan.Command.Y != cell.Y) return;
            _hud.ShowToast(Loc.F("hint.paint", LayerPalette.ColorName(plan.Command.Color)), 2.6f);
        }

        /// <summary>Testler: ilk teslim sayısı (yinelenen reklam geri çağrısı tek teslim).</summary>
        public static int HintGrantsForTest;

        /// <summary>Test: ipucu düğmesine basış.</summary>
        public void PressHintForTest() => OnHintPressed();

        /// <summary>Test: bekleyen plan işini hemen bekleyip işler (gerçek akışta Update yoklar).</summary>
        public void CompleteHintJobForTest()
        {
            var job = _hintJob;
            if (job == null) return;
            try { job.Wait(10000); } catch (System.AggregateException) { }
            HintTick();
        }
    }
}
