using System.Collections;
using System.IO;
using KatKat.Core;
using KatKat.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace KatKat.Tests
{
    /// <summary>
    /// 2026-11-27 KESİN KURAL (kullanıcı: "3 lü götürürken hiç hedefe gitmiyor. Kesin olarak kural koy ve taşlar hedefe gitsin"):
    /// patlayan her taş uçar — görev rengi görev simgesine, görevde olmayan renk yıldız çubuğuna. 244. bölümde (görevler lacivert,
    /// sarı, mor) görevde olmayan renkteki grup da uçuş başlatmalı.
    /// </summary>
    public class BlastFlightTests
    {
        private GameController _gc;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameController.RequestedLevel = -1;
            GameController.ToursOverride = false;
            GameController.SaveDirectoryOverride = Path.Combine(Application.temporaryCachePath, "katkat_blastfly_test");
            if (Directory.Exists(GameController.SaveDirectoryOverride)) Directory.Delete(GameController.SaveDirectoryOverride, true);
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            _gc = Object.FindAnyObjectByType<GameController>();
            Assert.IsNotNull(_gc);
        }

        [TearDown]
        public void TearDown() => GameController.ToursOverride = null;

        [UnityTest]
        public IEnumerator NonGoalColorGroup_StillFlies()
        {
            _gc.StartLevel(243);
            yield return null;
            _gc.Hud.HideRuleCard(false);
            Assert.AreEqual("level_244", _gc.CurrentLevel.Id);
            var st = _gc.Session.State;
            bool IsGoal(LayerColor c)
            {
                foreach (var g in st.Goals) if (g.Type == GoalType.CollectColor && g.Color == c) return true;
                return false;
            }
            int tx = -1, ty = -1, size = 0;
            for (int i = 0; i < st.Board.Count && tx < 0; i++)
            {
                var cell = st.Board[i];
                if (!cell.IsSelectable || cell.Layers.Count == 0 || IsGoal(cell.Top)) continue;
                var cmd = Command.Tap(st.Board.X(i), st.Board.Y(i));
                if (RulesEngine.Validate(st, cmd, out var wave0) != RejectReason.None) continue;
                tx = cmd.X; ty = cmd.Y; size = wave0.Count;
            }
            Assert.GreaterOrEqual(tx, 0, "görevde olmayan renkte geçerli grup yok");
            _gc.OnCellPressed(tx, ty);
            int most = 0;
            float limit = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < limit)
            {
                most = Mathf.Max(most, _gc.Board.BlastFlights);
                if (most >= size) break;
                yield return null;
            }
            Assert.GreaterOrEqual(most, size, $"görevde olmayan {size} taşlık grubun her taşı yıldız çubuğuna uçmalı");
        }
    }
}
