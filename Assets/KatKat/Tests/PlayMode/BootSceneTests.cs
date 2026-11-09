using System.Collections;
using KatKat.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KatKat.Tests
{
    public class BootSceneTests
    {
        [UnityTest]
        public IEnumerator BootScene_LoadsAndRunsBootstrap()
        {
            yield return SceneManager.LoadSceneAsync("Boot", LoadSceneMode.Single);
            yield return null;
            Assert.IsTrue(Bootstrap.HasBooted);
            Assert.IsNotNull(Object.FindAnyObjectByType<Camera>());
        }
    }
}
