using System.Collections.Generic;
using KatKat.Core;
using KatKat.Levels;
using KatKat.Meta;
using KatKat.UI;

namespace KatKat.Game
{
    /// <summary>
    /// v8 (kullanıcı onayı 2026-11-08; docs/v12/PLAN.md §6.1): deneme setinin kural gösterimi ana bölümlerde.
    /// Kural verilir, strateji verilmez:
    ///   * kurallı bölümde kalıcı kural simgeleri (hedef kabuğunun sağında; dokununca kural cümlesi),
    ///   * kural kartı yalnız bir türün İLK görüldüğü bölümde (tür tanıtımı) ve patron bölümlerinde,
    ///   * aynı bölümde 2 kayıptan sonra kayıp kartında "İpucu": bölüm yeniden başlar, yalnız ilk doğru hamle
    ///     (kayıtlı çözümün ilk adımı) gösterilir; bölüm ZPE'de yardımlı sayılır.
    /// </summary>
    public sealed partial class GameController
    {
        private string _lossLevelId;
        private int _levelLosses;
        private bool _mainHint;

        /// <summary>Bölüm türü (v8): kurallı ya da türlü ana bölüm mü.</summary>
        private static bool HasRules(LevelData d) => d != null && (d.Rules.Count > 0 || !string.IsNullOrEmpty(d.TrialType));

        /// <summary>Ana bölümün başlığı (kural kartı): patron için genel ad, diğerleri tür adı.</summary>
        private static string MainTypeTitle(LevelData d) => d.TrialType == "boss" ? Loc.T("level.boss") : TrialTitle(d);

        /// <summary>Begin'den (normal oturum): kural simgeleri, gerekirse kural kartı, bekleyen ipucu.</summary>
        private void MainRulesBegin()
        {
            if (_level == null || _level.Id != _lossLevelId) { _levelLosses = 0; _mainHint = false; }
            if (!HasRules(_level)) return;
            var rules = RuleLines(_level);
            _hud.SetRuleIcons(rules.Count > 0 ? rules : null);
            var seen = _save.Data.meta.seenTypes;
            string type = _level.TrialType ?? "";
            bool first = type.Length > 0 && type != "boss" && !seen.Contains(type);
            if (first) { seen.Add(type); _save.Save(); }
            // Patron kartı her patron bölümünde yalnız ilk girişte (2026-11-09, kullanıcı: "tekrar tekrar çıkmasın");
            // yeniden denemede kurallar sağdaki simgelerde durur.
            string bossKey = "boss@" + _level.Id;
            bool bossFirst = type == "boss" && !seen.Contains(bossKey);
            if (bossFirst) { seen.Add(bossKey); _save.Save(); }
            if (first || bossFirst || _mainHint)
                _hud.ShowRuleCard(MainTypeTitle(_level), Loc.T("trial.intro." + type), rules, () => { if (_mainHint) ShowMainHint(); });
        }

        /// <summary>Kayıp kartına eklenecek ipucu düğmesi (aynı bölümde 2. kayıptan sonra); yoksa null.</summary>
        private HudView.PanelButton? MainHintButton()
        {
            if (_level == null || _level.Solution.Count == 0) return null;
            if (_lossLevelId != _level.Id) { _lossLevelId = _level.Id; _levelLosses = 0; }
            _levelLosses++;
            if (_levelLosses < 2) return null;
            return new HudView.PanelButton(Loc.T("trial.hint"), UiIcon.Play, false, () =>
            {
                _mainHint = true;
                Restart();
                OutcomeAssist(AssistFlags.Guidance);
                ShowMainHint();
            });
        }

        public bool MainHintOffered => _level != null && _lossLevelId == _level.Id && _levelLosses >= 2;

        /// <summary>İpucu: kayıtlı çözümün ilk adımı (bölüm baştan başladığı için geçerli); yalnız dokunuş gösterilir.</summary>
        private void ShowMainHint()
        {
            if (_session == null || _level.Solution.Count == 0) return;
            var step = _level.Solution[0];
            if (step.Type == "tap") _hint.Show(_board.CellCenter(step.X, step.Y), _board.Pitch);
            else _hint.Hide();
            _hud.ShowToast(Loc.T("trial.hint.toast"), 2.4f);
        }
    }
}
