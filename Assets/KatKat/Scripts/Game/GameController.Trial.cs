using System;
using System.Collections.Generic;
using KatKat.Core;
using KatKat.Levels;
using KatKat.Meta;
using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// v12 deneme seti (kullanıcı, 2026-11-08: "haritada Deneme düğmesi olsun"; docs/v12/PLAN.md §6). 10 bölüm, her
    /// bölüm türünden bir örnek + patron (Resources/Trial, LevelLab trial). Ana 1000 bölüme, yıldızlara, ZPE'ye,
    /// sıralamaya yazılmaz; yalnız hangi denemenin bitirildiği kayıtta (meta.trialDone).
    /// Kural verilir, strateji verilmez: bölüm açılırken kural kartı, oyun boyunca kural simgeleri; uzun basınca yığının
    /// katmanları (tam bilgi); iki kayıptan sonra istenirse bir hamlelik ipucu.
    /// </summary>
    public sealed partial class GameController
    {
        public const int TrialCount = 10;
        private readonly LevelData[] _trials = new LevelData[TrialCount];
        private int _trialIndex = -1;
        private int _trialLosses;
        private bool _trialHint;

        public int TrialIndex => _trialIndex;
        public bool InTrial => _sessionKind == SessionKind.Trial;

        public LevelData Trial(int i)
        {
            if (i < 0 || i >= TrialCount) return null;
            if (_trials[i] != null) return _trials[i];
            var asset = Resources.Load<TextAsset>($"Trial/trial_{i + 1:00}");
            if (asset == null) return null;
            try { _trials[i] = LevelParser.Parse(asset.text, asset.name); }
            catch (FormatException e) { Debug.LogError("[KatKat] Deneme bölümü okunamadı: " + e.Message); }
            return _trials[i];
        }

        public bool TrialDone(int i) => Trial(i) is LevelData d && _save.Data.meta.trialDone.Contains(d.Id);

        /// <summary>Haritadaki Deneme düğmesi: deneme listesi (modal).</summary>
        public void ShowTrials()
        {
            _bgPick = -1; // ui3: menüden yeni girişte oyun zemini yeniden seçilir
            var items = new List<MenuView.TrialItem>();
            for (int i = 0; i < TrialCount; i++)
            {
                var d = Trial(i);
                if (d == null) continue;
                items.Add(new MenuView.TrialItem
                {
                    Index = i, Title = TrialTitle(d), Intro = Loc.T("trial.intro." + d.TrialType), Rules = new List<string>(d.Rules), Done = TrialDone(i), Boss = d.TrialType == "boss",
                });
            }
            _menu.ShowTrials(items, StartTrial);
        }

        public static string TrialTitle(LevelData d) => Loc.T("trial.type." + d.TrialType, d.Title);

        public void StartTrial(int i)
        {
            var d = Trial(i);
            if (d == null) { ShowContentError(Loc.T("err.notfound")); return; }
            if (_trialIndex != i) { _trialLosses = 0; _trialHint = false; }
            _trialIndex = i;
            _menu.Hide();
            _music.Leave();
            _hud.HidePanel();
            _levelIndex = i;
            _nextKind = SessionKind.Trial;
            Begin(d, 0); // tema: Mavi Liman (bölge 1)
        }

        /// <summary>AfterBegin'den: başlık, kural simgeleri ve kural kartı.</summary>
        private void TrialAfterBegin()
        {
            var d = _level;
            _hud.SetLevel(Loc.Upper(Loc.T("trial.tag")) + " " + (_trialIndex + 1), _session.State.MovesLeft, _session.State.Goals, _session.State.RollEnabled, _trialIndex + 1);
            var rules = RuleLines(d);
            _hud.SetRuleIcons(rules);
            _hud.ShowRuleCard(TrialTitle(d), Loc.T("trial.intro." + d.TrialType), rules, () => { if (_trialHint) ShowTrialHint(); });
            if (_trialHint) OutcomeAssist(AssistFlags.Guidance);
        }

        private List<HudView.RuleLine> RuleLines(LevelData d)
        {
            var list = new List<HudView.RuleLine>();
            foreach (var r in d.Rules)
            {
                string text = Loc.T("trial.rule." + r);
                if (r == "chainOnly")
                    foreach (var g in d.Goals) if (g.Type == "collectChain") { text = Loc.F("trial.rule.chainOnly.color", Loc.T("color." + g.Color, g.Color)); break; }
                if (r == "exact") text = Loc.F("trial.rule.exact", d.MoveLimit);
                list.Add(new HudView.RuleLine { Rule = r, Text = text });
            }
            return list;
        }

        /// <summary>İpucu: şu anki durumdan bir sonraki doğru hamle (çözücüyle); yalnız istenince.</summary>
        private void ShowTrialHint()
        {
            if (_session == null) return;
            var r = LevelSolver.Solve(_session.State, 300000);
            if (r.Outcome != SolveOutcome.Solved || r.Solution.Count == 0) { _hint.Hide(); return; }
            var c = r.Solution[0];
            if (c.Type == CommandType.Tap) _hint.Show(_board.CellCenter(c.X, c.Y), _board.Pitch);
            else _hint.Hide();
            _hud.ShowToast(Loc.T("trial.hint.toast"), 2.4f);
        }

        private void OnTrialWon()
        {
            _trialLosses = 0;
            _trialHint = false;
            if (!_save.Data.meta.trialDone.Contains(_level.Id)) { _save.Data.meta.trialDone.Add(_level.Id); _save.Save(); }
            _sound.PlayReward();
            Haptics.Medium();
            bool hasNext = _trialIndex + 1 < TrialCount && Trial(_trialIndex + 1) != null;
            _hud.ShowPanel(Loc.T("trial.won.title"), Loc.F("trial.won.body", _trialIndex + 1, TrialCount),
                new HudView.PanelButton(hasNext ? Loc.T("trial.next") : Loc.T("trial.list"), UiIcon.Play, true,
                    () => { if (hasNext) StartTrial(_trialIndex + 1); else { ShowLevels(); ShowTrials(); } }),
                new HudView.PanelButton(Loc.T("trial.list"), UiIcon.Menu, false, () => { ShowLevels(); ShowTrials(); }));
        }

        private void ShowTrialLost(GameStatus status)
        {
            _trialLosses++;
            string title = status == GameStatus.LostProtected ? Loc.T("lose.protect.title")
                : status == GameStatus.LostOutOfMoves ? Loc.T("lose.moves.title") : Loc.T("lose.stuck.title");
            string body = status == GameStatus.LostProtected ? Loc.T("lose.protect.body")
                : status == GameStatus.LostOutOfMoves ? Loc.T("lose.moves.body") : Loc.T("lose.stuck.body");
            var buttons = new List<HudView.PanelButton>
            {
                new HudView.PanelButton(Loc.T("btn.retry"), UiIcon.Restart, true, () => { _trialHint = false; Restart(); }),
            };
            // İki kayıptan sonra istenirse ipucu (yalnız bir sonraki hamle; çözüm söylenmez).
            if (_trialLosses >= 2)
                buttons.Add(new HudView.PanelButton(Loc.T("trial.hint"), UiIcon.Play, false, () => { _trialHint = true; Restart(); OutcomeAssist(AssistFlags.Guidance); ShowTrialHint(); }));
            buttons.Add(new HudView.PanelButton(Loc.T("trial.list"), UiIcon.Menu, false, () => { ShowLevels(); ShowTrials(); }));
            _hud.ShowPanel(title, body, buttons.ToArray());
        }

        public bool TrialHintOffered => _trialLosses >= 2;

        // ---- Uzun basış: katman görünümü (v8: bütün oturumlarda; dokunuş bırakınca) --------------------------

        public const float PeekHoldSeconds = 0.42f;
        private bool _pressPending;
        private int _pressX, _pressY;
        private float _pressAt;
        private bool _peeking;

        /// <summary>Deneme bölümünde dokunuş bırakınca çalışır; basılı tutulursa katmanlar görünür ve dokunuş iptal olur.</summary>
        private bool TrialHandlePress(Vector2 screenPos, int x, int y)
        {
            // v8 (kullanıcı onayı 2026-11-08): ana oyunda ve günlükte de (dokunuş bırakınca çalışır).
            if (_session == null || CurrentPhase == Phase.Targeting || _traySlot >= 0) return false;
            _pressPending = true;
            _pressX = x; _pressY = y;
            _pressAt = Time.unscaledTime;
            _peeking = false;
            return true;
        }

        private void TrialTickPress()
        {
            if (!_pressPending) return;
            bool down = Input.touchCount > 0 ? Input.GetTouch(0).phase != TouchPhase.Ended && Input.GetTouch(0).phase != TouchPhase.Canceled : Input.GetMouseButton(0);
            if (down)
            {
                if (!_peeking && Time.unscaledTime - _pressAt >= PeekHoldSeconds)
                {
                    _peeking = true;
                    var cell = _session.State.Board[_pressX, _pressY];
                    var screen = _cam.WorldToScreenPoint(_board.CellCenter(_pressX, _pressY));
                    _hud.ShowStackPeek(screen, cell.Layers);
                }
                return;
            }
            _pressPending = false;
            if (_peeking) { _peeking = false; _hud.HideStackPeek(); return; }
            OnCellPressed(_pressX, _pressY);
        }

        /// <summary>Test: yığın görünümünü doğrudan aç/kapat.</summary>
        public void PeekForTest(int x, int y, bool on)
        {
            if (!on) { _hud.HideStackPeek(); return; }
            var screen = _cam.WorldToScreenPoint(_board.CellCenter(x, y));
            _hud.ShowStackPeek(screen, _session.State.Board[x, y].Layers);
        }
    }
}
