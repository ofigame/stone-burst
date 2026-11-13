using System.Collections;
using KatKat.Core.Live;
using KatKat.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KatKat.Tests
{
    /// <summary>Canlı maç prototip ekranı: seç → önizle → onayla akışı ve geçersiz hamlenin durumu değiştirmemesi.</summary>
    public sealed class LiveMatchScreenTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.Destroy(_go);
            foreach (var es in Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None)) Object.Destroy(es.gameObject);
        }

        [UnityTest]
        public IEnumerator SelectPreviewConfirm_Scores_ThenTurnPasses_InvalidTapChangesNothing()
        {
            _go = new GameObject("LiveMatchTest");
            var screen = _go.AddComponent<LiveMatchScreen>();
            yield return null;
            var m = screen.Match;
            Assert.IsNotNull(m, "havuz yüklenmeli ve maç başlamalı");
            Assert.AreEqual(0, m.Current);

            var opp = LiveMoves.Opportunities(m.Boards[0], m.Hands[0])[0];
            screen.SelectSlot(m.Hands[0].IndexOf(opp.Color));
            screen.TapCell(LiveSide.Own, opp.Target);
            Assert.AreEqual(0, m.Scores[0], "ilk dokunuş yalnız önizleme");
            Assert.AreEqual(0, m.Current);
            screen.TapCell(LiveSide.Own, opp.Target);
            Assert.AreEqual(opp.Group.Count, m.Scores[0]);
            Assert.AreEqual(1, m.Current);
            Assert.AreEqual(7, m.Hands[0].Count);

            // P2: aynı renge değiştirme reddedilir, hiçbir şey değişmez.
            var b = m.Boards[1];
            int cell = System.Array.FindIndex(m.Layout.Mask, x => x);
            int slot = m.Hands[1].IndexOf(b[cell].Top);
            if (slot >= 0)
            {
                screen.SelectSlot(slot);
                screen.TapCell(LiveSide.Own, cell);
                screen.TapCell(LiveSide.Own, cell);
                Assert.AreEqual(1, m.Current);
                Assert.AreEqual(8, m.Hands[1].Count);
            }
            yield return null;
        }
    }
}
