using System.Collections.Generic;
using KatKat.Core;
using KatKat.Levels;
using KatKat.UI;
using KatKat.View;

namespace KatKat.Game
{
    /// <summary>
    /// Konsept pilotu (kullanıcı, 2026-11-15: "10 bölüm yap pilota koy kontrol edeyim"). 10 konseptten birer bölüm
    /// (Resources/Concept, LevelLab concept). Deneme seti akışıyla oynanır: ana ilerlemeye, yıldızlara, ZPE'ye ve
    /// sıralamaya yazılmaz. Haritada Pilot düğmesinin altındaki "Konsept" düğmesi (pilotla aynı koşul: deneme derlemesi).
    /// Tehdit her hamlede bir adım ilerler; durum satırı (hedef kartının altı) ve tahtadaki işaretler önceden gösterir.
    /// </summary>
    public sealed partial class GameController
    {
        public const int ConceptCount = 10;
        private readonly LevelData[] _concepts = new LevelData[ConceptCount];
        private bool _concept;
        public bool InConcept => _concept;

        /// <summary>Konsept listesi (haritadaki Konsept düğmesi).</summary>
        public void ShowConcepts() { _pilot = false; _concept = true; ShowSet(); }

        private string ConceptRuleText(LevelData d)
        {
            var h = d.Hazard;
            switch (h.Kind)
            {
                case "rot": return Loc.F("concept.rule.rot", h.Every);
                case "cycle":
                {
                    var names = new List<string>();
                    foreach (var col in h.Colors) names.Add(ColorIds.TryParse(col, out var lc) ? Paint(lc) : col);
                    return Loc.F("concept.rule.cycle", h.Every, string.Join(" → ", names));
                }
                case "rain": return Loc.F("concept.rule.rain", h.Every, h.Max);
                case "rival": return Loc.F("concept.rule.rival", h.Max);
                case "moss": return Loc.F("concept.rule.moss", h.Every, h.Max);
                case "balloon": return Loc.F("concept.rule.balloon", h.Every);
                case "fire": return Loc.F("concept.rule.fire", h.Max);
                default: return Loc.T("concept.rule." + h.Kind);
            }
        }

        /// <summary>Durum satırı: tehdidin şu anki sayaçları (her hamleden, geri almadan ve yeniden başlatmadan sonra).</summary>
        private void ConceptHud()
        {
            var s = _session?.State;
            var h = s?.Hazard;
            if (h == null) { _hud.SetHazardLine(null); _hud.SetGoalFocus(-1); return; }
            // Kullanıcı 2026-11-15: görev görsellerle anlatılsın (taş şekli, kuzgun, yuva, balon, saat).
            var items = new List<HudView.StripItem>();
            string hint = null;
            string B(int n) => "<b>" + n + "</b>";
            switch (h.Kind)
            {
                case HazardKind.Cycle:
                {
                    items.Add(HudView.StripItem.Of(Chip(h.ActiveColor), 72f));
                    items.Add(HudView.StripItem.Say(Loc.F("concept.strip.cycle.now", B(h.Counter))));
                    items.Add(HudView.StripItem.Say("›", true));
                    items.Add(HudView.StripItem.Of(Chip(h.NextColor), 52f, true));
                    hint = Loc.T("concept.strip.cycle.hint");
                    int focus = -1;
                    for (int i = 0; i < s.Goals.Count; i++) if (s.Goals[i].Color == h.ActiveColor && !s.Goals[i].IsComplete) { focus = i; break; }
                    _hud.SetGoalFocus(focus);
                    break;
                }
                case HazardKind.Raven:
                    items.Add(HudView.StripItem.Of(HazardPaint.Raven, 80f));
                    items.Add(HudView.StripItem.Say(Loc.F("concept.strip.raven", B(h.Path.Length - 1 - h.Index))));
                    items.Add(HudView.StripItem.Of(HazardPaint.Nest, 80f));
                    hint = Loc.T("concept.strip.raven.hint");
                    break;
                case HazardKind.Balloon:
                {
                    int n = 0;
                    foreach (var bl in h.Balloon) if (bl) n++;
                    items.Add(HudView.StripItem.Of(HazardPaint.Balloon, 76f));
                    items.Add(HudView.StripItem.Say(Loc.F("concept.strip.balloon", B(n), B(h.Counter))));
                    hint = Loc.T("concept.strip.balloon.hint");
                    break;
                }
                case HazardKind.Orders:
                {
                    items.Add(HudView.StripItem.Of(HazardPaint.Clock, 64f));
                    items.Add(HudView.StripItem.Say(Loc.F("concept.strip.orders", s.ActiveStage + 1, s.StageCount)));
                    foreach (var g in s.Goals)
                    {
                        if (g.Stage != s.ActiveStage || g.IsComplete) continue;
                        items.Add(HudView.StripItem.Of(Chip(g.Color), 64f));
                        items.Add(HudView.StripItem.Say(B(g.Amount - g.DisplayProgress)));
                    }
                    items.Add(HudView.StripItem.Say(Loc.F("concept.strip.orders.moves", B(h.Value))));
                    hint = Loc.T("concept.strip.orders.hint");
                    break;
                }
            }
            if (items.Count > 0) { _hud.SetHazardStrip(items, hint); return; }
            _hud.SetGoalFocus(-1);
            _hud.SetHazardLine(HazardLine(s));
        }

        private static UnityEngine.Sprite Chip(LayerColor c) => PremiumArt.Chip(c) ?? UiArt.Chip(c);

        public static string HazardLine(LevelState s)
        {
            var h = s.Hazard;
            string Num(int n) => "<b>" + n + "</b>";
            switch (h.Kind)
            {
                case HazardKind.Rot:
                    return h.Row < 0 ? Loc.T("concept.hud.rot.done") : Loc.F("concept.hud.rot", Num(h.Counter));
                case HazardKind.Fuse:
                {
                    int min = int.MaxValue, n = 0;
                    foreach (var f in h.Fuse) if (f > 0) { n++; if (f < min) min = f; }
                    return n == 0 ? Loc.T("concept.hud.fuse.done") : Loc.F("concept.hud.fuse", n, Num(min));
                }
                case HazardKind.Cycle:
                    return Loc.F("concept.hud.cycle", Paint(h.ActiveColor), Num(h.Counter), Paint(h.NextColor));
                case HazardKind.Raven:
                {
                    int left = h.Path.Length - 1 - h.Index;
                    return Loc.F("concept.hud.raven", Num(left));
                }
                case HazardKind.Rain:
                    return Loc.F("concept.hud.rain", Num(h.Counter), h.Max);
                case HazardKind.Rival:
                    return Loc.F("concept.hud.rival", Num(h.Value), h.Max);
                case HazardKind.Moss:
                    return Loc.F("concept.hud.moss", Num(HazardRules.MossCount(s.Board)), h.Max, h.Counter);
                case HazardKind.Balloon:
                {
                    int n = 0;
                    foreach (var b in h.Balloon) if (b) n++;
                    return Loc.F("concept.hud.balloon", n, Num(h.Counter));
                }
                case HazardKind.Orders:
                    return Loc.F("concept.hud.orders", s.ActiveStage + 1, s.StageCount, Num(h.Value));
                case HazardKind.Fire:
                    return Loc.F("concept.hud.fire", Num(h.Value), h.Max);
            }
            return null;
        }

        private static string Paint(LayerColor c)
        {
            var col = LayerPalette.FrontColor(c);
            return $"<color=#{col.r:X2}{col.g:X2}{col.b:X2}><b>{LayerPalette.ColorName(c)}</b></color>";
        }

        /// <summary>Hamle bitince: tehdit olayını bildir, etkilenen hücreleri parlat, durum satırını tazele.</summary>
        private void ConceptAfterPlay(MoveResult r)
        {
            var s = _session.State;
            if (s.Hazard == null) return;
            ConceptHud();
            if (r == null || r.HazardNote == HazardNote.None || s.Status != GameStatus.Idle) return;
            _board.Hazards?.Pulse(r.HazardCells);
            string key = "concept.note." + r.HazardNote;
            if (Loc.Has(key)) _hud.ShowToast(r.HazardNote == HazardNote.CycleSwitch ? Loc.F(key, Paint(s.Hazard.ActiveColor)) : Loc.T(key), 1.8f);
            if (r.HazardNote == HazardNote.RotCrumble || r.HazardNote == HazardNote.RainFell || r.HazardNote == HazardNote.MossSpread) Haptics.Light();
        }

        /// <summary>Test ve yakalama: kayıtlı çözümün ilk <paramref name="steps"/> adımı animasyonsuz uygulanır, ekran tazelenir.</summary>
        public void ApplySolutionForTest(int steps)
        {
            if (_session == null || _level == null) return;
            for (int i = 0; i < steps && i < _level.Solution.Count; i++)
            {
                var r = _session.Submit(LevelLoader.ToCommand(_level.Solution[i]));
                _lastResult = r;
            }
            ResyncAll();
            if (_lastResult != null && _lastResult.HazardNote != HazardNote.None) _board.Hazards?.Pulse(_lastResult.HazardCells);
        }

        /// <summary>Tehdit kaybı: nedeni söyleyen kart; geri al (son hamleyi), yeniden dene, liste.</summary>
        private void ShowConceptLost()
        {
            var loss = _session.State.Hazard?.Loss ?? HazardLoss.None;
            string title = Loc.T("concept.lose." + loss + ".title", Loc.T("lose.stuck.title"));
            string body = Loc.T("concept.lose." + loss + ".body", Loc.T("lose.stuck.body"));
            var buttons = new List<HudView.PanelButton>
            {
                new HudView.PanelButton(Loc.T("btn.undo"), UiIcon.Undo, true, Undo),
                new HudView.PanelButton(Loc.T("btn.retry"), UiIcon.Restart, false, () => { _trialHint = false; Restart(); }),
            };
            if (_trialLosses >= 2)
                buttons.Add(new HudView.PanelButton(Loc.T("trial.hint"), UiIcon.Play, false, () => { _trialHint = true; Restart(); OutcomeAssist(KatKat.Meta.AssistFlags.Guidance); ShowTrialHint(); }));
            buttons.Add(new HudView.PanelButton(Loc.T("trial.list"), UiIcon.Menu, false, () => { ShowLevels(); ShowSet(); }));
            _hud.ShowPanel(title, body, buttons.ToArray());
        }
    }
}
