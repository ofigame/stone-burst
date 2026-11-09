using System.Collections;
using KatKat.Core;
using KatKat.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KatKat.Tests
{
    public class PeelLabTests
    {
        private PeelLab _lab;

        [UnitySetUp]
        public IEnumerator LoadLab()
        {
            yield return SceneManager.LoadSceneAsync("PeelLab", LoadSceneMode.Single);
            yield return null;
            _lab = Object.FindAnyObjectByType<PeelLab>();
            Assert.IsNotNull(_lab);
        }

        [UnityTest]
        public IEnumerator Peel_RemovesExactlyTopLayer_AndCleansUp()
        {
            CollectionAssert.AreEqual(PeelLab.Presets[0], _lab.Stack.Layers);
            var seq = _lab.PeelTop();
            Assert.AreEqual(LayerColor.Coral, seq.Color);
            // Alt katmanlar soyulma sırasında yerinde kalır, sıra değişmez.
            CollectionAssert.AreEqual(new[] { LayerColor.Yellow, LayerColor.Blue }, _lab.Stack.Layers);

            yield return new WaitForSecondsRealtime(PeelSequence.Duration / 1000f + 0.2f);
            Assert.IsTrue(seq.IsDone);
            Assert.AreEqual(0, _lab.Director.ActiveCount);
            Assert.IsFalse(seq.Sheet.gameObject.activeSelf, "Soyulma ağı havuza dönmeli.");
            Assert.AreEqual(Vector3.one, _lab.Stack.transform.Find("Body").localScale);
        }

        [UnityTest]
        public IEnumerator CompleteAll_EndsMidAnimationCleanly()
        {
            var seq = _lab.PeelTop();
            yield return new WaitForSecondsRealtime(0.15f);
            _lab.Director.CompleteAll();
            Assert.IsTrue(seq.IsDone);
            Assert.AreEqual(0, _lab.Director.ActiveCount);
            Assert.AreEqual(2, _lab.Stack.Layers.Count);
        }
    }
}
