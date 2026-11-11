using System.Collections;
using KatKat.Core;
using KatKat.Game;
using KatKat.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KatKat.Tests
{
    /// <summary>v12 deneme seti: haritadaki Deneme düğmesi, liste, kural kartı/simgeleri, kazanma, kristal kaybı, ipucu, katman görünümü.</summary>
    public class TrialFlowTests
    {
        private GameController _gc;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameController.RequestedLevel = -1;
            GameController.ToursOverride = false;
            GameController.SaveDirectoryOverride = System.IO.Path.Combine(Application.temporaryCachePath, "katkat_trial_test");
            if (System.IO.Directory.Exists(GameController.SaveDirectoryOverride)) System.IO.Directory.Delete(GameController.SaveDirectoryOverride, true);
            System.IO.Directory.CreateDirectory(GameController.SaveDirectoryOverride);
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return null;
            _gc = Object.FindAnyObjectByType<GameController>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameController.ToursOverride = null;
            GameController.SaveDirectoryOverride = null;
            yield return null;
        }

        private IEnumerator WaitStable()
        {
            float limit = Time.realtimeSinceStartup + 20f;
            while ((_gc.CurrentPhase == GameController.Phase.Animating || _gc.IsCelebrating) && Time.realtimeSinceStartup < limit) yield return null;
        }

        [UnityTest]
        public IEnumerator MapButton_List_RuleCard_WinTrial1_NoMainProgress()
        {
            // v8 (kullanıcı onayı 2026-11-08): türler ana bölümlerde; haritadaki Deneme düğmesi kalktı, liste kodda duruyor.
            _gc.ShowLevels();
            Assert.IsNull(GameObject.Find("TrialButton"), "v8: haritada Deneme düğmesi yok");
            _gc.ShowTrials();
            yield return null;
            for (int i = 1; i <= 10; i++) Assert.IsNotNull(GameObject.Find("Trial" + i), $"listede {i}. deneme");
            int completedBefore = _gc.SaveService.Data.completed.Count;
            GameObject.Find("Trial1").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.IsTrue(_gc.InTrial);
            Assert.IsTrue(_gc.Hud.RuleCardOpen, "kural kartı açılır");
            Assert.IsNotNull(GameObject.Find("Rule_exact"), "kalıcı kural simgesi");
            _gc.Hud.HideRuleCard(true);
            _gc.Board.Director.TimeScale = 8f;
            foreach (var step in _gc.CurrentLevel.Solution) { _gc.PlayStep(step); yield return WaitStable(); }
            Assert.AreEqual(GameStatus.Won, _gc.Session.State.Status);
            Assert.IsTrue(_gc.TrialDone(0), "deneme bitti olarak kaydedildi");
            Assert.AreEqual(completedBefore, _gc.SaveService.Data.completed.Count, "ana ilerleme değişmez");
            Assert.AreEqual(0, (_gc.SaveService.Data.meta.zpeHistory?.Count ?? 0), "ZPE'ye yazılmaz");
            // Katman görünümü.
            _gc.StartTrial(1);
            _gc.Hud.HideRuleCard(false);
            _gc.PeekForTest(0, 0, true);
            Assert.IsTrue(_gc.Hud.PeekShown);
            _gc.PeekForTest(0, 0, false);
            Assert.IsFalse(_gc.Hud.PeekShown);
        }

        [UnityTest]
        public IEnumerator Crystal_BreakLoses_HintAfterTwoLosses()
        {
            int protectIndex = -1;
            for (int i = 0; i < GameController.TrialCount; i++) if (_gc.Trial(i).TrialType == "protect") protectIndex = i;
            Assert.GreaterOrEqual(protectIndex, 0);
            for (int round = 0; round < 2; round++)
            {
                _gc.StartTrial(protectIndex);
                _gc.Hud.HideRuleCard(false);
                Command breaker = default; bool found = false;
                foreach (var c in LevelSolver.Moves(_gc.Session.State))
                {
                    var probe = _gc.Session.State.Clone();
                    if (RulesEngine.TryApply(probe, c).Accepted && probe.Status == GameStatus.LostProtected) { breaker = c; found = true; break; }
                }
                Assert.IsTrue(found, "kristali kıran bir hamle var");
                _gc.Board.Director.TimeScale = 8f;
                _gc.OnCellPressed(breaker.X, breaker.Y);
                yield return WaitStable();
                Assert.AreEqual(GameStatus.LostProtected, _gc.Session.State.Status);
                Assert.IsTrue(_gc.Hud.PanelOpen, "kayıp kartı");
            }
            Assert.IsTrue(_gc.TrialHintOffered, "iki kayıptan sonra ipucu önerilir");
        }
    }
}
