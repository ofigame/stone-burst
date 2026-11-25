using System.Collections;
using System.Linq;
using KatKat.Core;
using KatKat.Core.Live;
using KatKat.Game;
using KatKat.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KatKat.Tests
{
    /// <summary>
    /// Canlı maç ekranı (kullanıcı 2026-11-13 tasarımı): anasayfa girişi, oyunun tahta görünümü, eşit iki tahta,
    /// öneri/önizleme yok, zincirsiz otomatik patlama, sıra bota geçer, bot oynar, çıkış haritaya döner.
    /// </summary>
    public sealed class LiveMatchViewTests
    {
        private GameController _gc;

        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            GameController.RequestedLevel = -1;
            GameController.SaveDirectoryOverride = System.IO.Path.Combine(Application.temporaryCachePath, "katkat_live_test");
            if (System.IO.Directory.Exists(GameController.SaveDirectoryOverride)) System.IO.Directory.Delete(GameController.SaveDirectoryOverride, true);
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            _gc = Object.FindAnyObjectByType<GameController>();
            Assert.IsNotNull(_gc);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameController.SaveDirectoryOverride = null;
            yield return null;
        }

        /// <summary>2026-11-23 (kullanıcı: "üst alandan iple sarkıyormuş gibi olsun … ipten sallanma efekti"): sol rozetler ve sağdaki
        /// koleksiyon kartı iki iple asılı, ipin üst ucundan küçük açıyla sallanır; azaltılmış harekette düz.</summary>
        [UnityTest]
        public IEnumerator MapColumns_HangOnRopes_SwingGently()
        {
            _gc.ShowLevels();
            yield return null;
            var swings = Object.FindObjectsByType<KatKat.UI.HangSwing>(FindObjectsInactive.Exclude);
            Assert.GreaterOrEqual(swings.Length, 3, "sol rozetler ve koleksiyon kartı asılı");
            Assert.IsNotNull(GameObject.Find("DailyButton").GetComponentInParent<KatKat.UI.HangSwing>(), "Günlük rozeti askıda");
            var chained = swings.Where(x => x.Chained).ToArray();
            Assert.IsNotEmpty(chained, "alt rozetler üsttekine bağlı (ip uçları sabit)");
            foreach (var c in chained) Assert.IsNotNull(c.transform.parent.GetComponentInParent<KatKat.UI.HangSwing>(), c.name + " üst askıya bağlı");
            foreach (var s in swings)
            {
                int ropes = 0;
                foreach (Transform c in s.transform) if (c.name == "Rope") ropes++;
                Assert.AreEqual(2, ropes, s.name + " iki iple asılı");
                Assert.Greater(((RectTransform)s.transform).pivot.y, 1f, s.name + ": dönme noktası ipin üst ucunda");
                s.Advance(1f);
                Assert.AreEqual(0f, s.Angle, 0.01f, s.name + ": sarsıntı yokken kıpırdamaz");
            }
            // 2026-11-23: telefon sarsılınca askı salınır, birkaç saniyede söner.
            var sw = swings[0];
            try
            {
                KatKat.UI.HangSwing.AccelOverride = new Vector3(0f, -1f, 0f);
                for (int i = 0; i < 100; i++) sw.Advance(0.02f);
                float calm = Mathf.Abs(sw.ShakeSwing);
                // Telefonu yavaşça çevirmek (0,5 sn'de 30°) sallamaz.
                for (int i = 0; i <= 25; i++) { float ang = 30f * Mathf.Deg2Rad * i / 25f; KatKat.UI.HangSwing.AccelOverride = new Vector3(Mathf.Sin(ang), -Mathf.Cos(ang), 0f); sw.Advance(0.02f); }
                for (int i = 0; i < 50; i++) sw.Advance(0.02f);
                Assert.Less(Mathf.Abs(sw.ShakeSwing), 1f, "çevirme sallamaz");
                KatKat.UI.HangSwing.AccelOverride = new Vector3(0f, -1f, 0f);
                for (int i = 0; i < 100; i++) sw.Advance(0.02f);
                calm = Mathf.Abs(sw.ShakeSwing);
                KatKat.UI.HangSwing.AccelOverride = new Vector3(1.6f, -1f, 0f); // sert yana sarsıntı
                for (int i = 0; i < 4; i++) sw.Advance(0.02f);
                KatKat.UI.HangSwing.AccelOverride = new Vector3(0f, -1f, 0f);
                float peak = 0f;
                for (int i = 0; i < 50; i++) { sw.Advance(0.02f); peak = Mathf.Max(peak, Mathf.Abs(sw.ShakeSwing)); }
                Assert.Greater(peak, calm + 3f, "sarsıntı askıyı salladı");
                Assert.LessOrEqual(Mathf.Abs(sw.Angle), KatKat.UI.HangSwing.MaxAngle, "açı sınırlı");
                for (int i = 0; i < 400; i++) sw.Advance(0.02f);
                Assert.Less(Mathf.Abs(sw.ShakeSwing), 1f, "salınım söndü");
            }
            finally { KatKat.UI.HangSwing.AccelOverride = null; }
            // 2026-11-26 (kullanıcı: "Butonlara basılı tutup parmağımızı oynatırsak butonlar ip desteği ile o yönde sallansın"):
            // sağa itilen askı sağa salınır (alt kenarı sağa: açı artı), sonra söner.
            for (int i = 0; i < 400; i++) sw.Advance(0.02f);
            sw.Push(0.05f);
            float maxAng = 0f;
            for (int i = 0; i < 15; i++) { sw.Advance(0.02f); maxAng = Mathf.Max(maxAng, sw.Angle); }
            Assert.Greater(maxAng, 2f, "parmağın yönünde salındı");
            for (int i = 0; i < 400; i++) sw.Advance(0.02f);
            Assert.Less(Mathf.Abs(sw.ShakeSwing), 1f, "salınım söndü");
            // 2026-11-26: alttaki askı sürüklenince üstündeki de sallanır.
            var low = chained[chained.Length - 1];
            var up = low.transform.parent.GetComponentInParent<KatKat.UI.HangSwing>();
            for (int i = 0; i < 400; i++) { low.Advance(0.02f); up.Advance(0.02f); }
            low.PushChain(0.05f);
            float upMax = 0f;
            for (int i = 0; i < 15; i++) { low.Advance(0.02f); up.Advance(0.02f); upMax = Mathf.Max(upMax, up.Angle); }
            Assert.Greater(upMax, 1f, "üstteki askı da sallandı");
            KatKat.UI.Motion.Reduced = true;
            try { swings[0].Advance(1f); Assert.AreEqual(0f, swings[0].Angle, "azaltılmış harekette sabit"); }
            finally { KatKat.UI.Motion.Reduced = false; }
        }

        [UnityTest]
        public IEnumerator HomeButton_OpensLive_MoveAppliesWithoutPreview_BotReplies_ExitReturnsHome()
        {
            _gc.ShowLevels();
            yield return null;
            Assert.IsNotNull(_gc.Menu.LivePressed, "anasayfada canlı maç girişi bağlı");
            Assert.IsNotNull(GameObject.Find(KatKat.UI.Loc.T("live.button")), "alt menünün ortasında canlı maç (2026-11-22)");
            Assert.IsNotNull(GameObject.Find("DailyButton"), "Günlük Bulmaca sol sütunda");
            _gc.Menu.LivePressed();
            yield return null;
            Assert.IsTrue(_gc.Menu.LiveIntroShown, "önce bilgi kartı (2026-11-22)");
            Assert.IsFalse(_gc.LiveOpen, "Hazırım’a basmadan maç açılmaz");
            Assert.IsTrue(_gc.Menu.PressForTest("Ready"), "Hazırım");
            yield return null;
            var v = _gc.Live;
            Assert.IsNotNull(v, "canlı maç açıldı");
            Assert.IsTrue(_gc.LiveOpen);
            var m = v.Match;
            Assert.IsNotNull(m, "havuz yüklendi, maç başladı");
            StringAssert.StartsWith("LM5-", m.Layout.Id, "varsayılan havuz v5");
            v.BotDelay = 0f;

            // İki tahta birebir aynı başlar; 7×6, tam dikdörtgen; 3 katman, her katman üstündekinden farklı; iki tahta aynı boyda.
            Assert.AreEqual(7, m.Boards[0].Width);
            Assert.AreEqual(6, m.Boards[0].Height);
            for (int i = 0; i < m.Boards[0].Count; i++)
            {
                CollectionAssert.AreEqual(m.Boards[0][i].Layers, m.Boards[1][i].Layers);
                Assert.IsTrue(m.Boards[0][i].Active, "tahtada boşluk yok");
                Assert.AreEqual(3, m.Boards[0][i].Layers.Count, "3 katman");
                Assert.AreNotEqual(m.Boards[0][i].Layers[0], m.Boards[0][i].Layers[1]);
                Assert.AreNotEqual(m.Boards[0][i].Layers[1], m.Boards[0][i].Layers[2]);
            }
            Assert.AreEqual(v.BoardOf(0).Pitch, v.BoardOf(1).Pitch, 1e-4f, "eşit tahta boyutu");
            Assert.AreEqual(LiveMatchView.Human, m.Current);

            // En iyi puan hamlesi: el taşı seç, hücreye bir kez dokun → hemen uygulanır (önizleme/onay yok).
            var best = m.LegalMoves(0).Where(x => x.Type == LiveCommandType.Swap && x.Side == LiveSide.Own).OrderByDescending(x => x.Gain).First();
            Assert.Greater(best.Gain, 0);
            v.SelectSlot(best.Slot);
            Assert.AreEqual(best.Slot, v.Selected);
            Assert.AreEqual(0, m.Scores[0], "seçim tek başına bir şey değiştirmez");
            var b0 = m.Boards[0];
            int handBefore = m.Hands[0].Count;
            v.TapCell(LiveSide.Own, b0.X(best.Target), b0.Y(best.Target));
            Assert.AreEqual(best.Gain, m.Scores[0], "tek dokunuşla puan");
            Assert.AreEqual(handBefore - 1, m.Hands[0].Count);
            Assert.AreEqual(-1, v.Selected);
            Assert.AreEqual(LiveMatchView.Bot, m.Current, "sıra rakipte");
            v.CompleteAnimations();

            // Bot sırası: Update bekleme süresinden sonra oynar.
            float limit = Time.realtimeSinceStartup + 10f;
            while (m.Current == LiveMatchView.Bot && Time.realtimeSinceStartup < limit) yield return null;
            Assert.AreEqual(LiveMatchView.Human, m.Current, "bot oynadı");
            Assert.AreEqual(1, m.TurnsUsed[1]);
            v.CompleteAnimations();

            // Geçersiz hamle: seçimsiz dokunuş ve rakipte aynı renk → durum değişmez, mesaj verilir.
            int score = m.Scores[0];
            var b1 = m.Boards[1];
            int cell = System.Array.FindIndex(m.Layout.Mask, x => x);
            v.TapCell(LiveSide.Opponent, b1.X(cell), b1.Y(cell));
            Assert.AreEqual(LiveMatchView.Human, m.Current, "seçimsiz dokunuş yok sayılır");
            int same = m.Hands[0].IndexOf(b1[cell].Top);
            if (same >= 0)
            {
                v.SelectSlot(same);
                v.TapCell(LiveSide.Opponent, b1.X(cell), b1.Y(cell));
                Assert.AreEqual(LiveMatchView.Human, m.Current, "aynı renge değiştirme reddedilir");
                Assert.AreEqual(score, m.Scores[0]);
                Assert.IsFalse(string.IsNullOrEmpty(v.Message));
            }

            // Çıkış: harita geri gelir.
            _gc.CloseLive();
            yield return null;
            Assert.IsFalse(_gc.LiveOpen);
            Assert.IsTrue(_gc.Menu.IsMapShown, "çıkınca harita");
        }

        /// <summary>Kullanıcı 2026-11-14: "sıra kimdeyse 5 sn süresi olsun" → "10 saniye yapalım". Süre dolunca sıra geçer; çubuk ve saniye görünür.</summary>
        [UnityTest]
        public IEnumerator TurnTimer_FifteenSeconds_ExpiresToPass()
        {
            _gc.ShowLive();
            yield return null;
            var v = _gc.Live;
            v.BotDelay = 99f; // bot beklesin
            var m = v.Match;
            Assert.AreEqual(LiveMatchView.Human, m.Current);
            Assert.LessOrEqual(v.TimeLeft, 15f); Assert.Greater(v.TimeLeft, 13.5f, "sıra başında 15 sn (kullanıcı 2026-11-25; ilk kare süresi düşülür)");
            float t0 = v.TimeLeft;
            var bar = GameObject.Find("TimerBar");
            Assert.IsNotNull(bar, "süre çubuğu");
            v.AdvanceTurnClock(3f);
            Assert.AreEqual(t0 - 3f, v.TimeLeft, 0.01f);
            Assert.AreEqual(LiveMatchView.Human, m.Current, "süre bitmeden sıra sende");
            v.AdvanceTurnClock(t0 - 3f + 0.1f);
            Assert.AreEqual(LiveMatchView.Bot, m.Current, "süre dolunca sıra rakibe geçti");
            Assert.AreEqual(1, m.TurnsUsed[0], "tur harcandı");
            Assert.AreEqual(0, m.Scores[0]);
            Assert.IsFalse(string.IsNullOrEmpty(v.Message), "süre doldu yazısı");
            yield return null;
            Assert.Greater(v.TimeLeft, 13.5f, "rakibin süresi yeniden başlar");
            _gc.CloseLive();
            yield return null;
        }

        private static void PlayOut(LiveMatchView v, LiveGenerator.Rng rng)
        {
            for (int k = 0; k < 60 && !v.Match.IsOver; k++)
            {
                v.CompleteAnimations();
                var m = v.Match;
                if (m.Current == LiveMatchView.Bot) { v.BotMove(); v.CompleteAnimations(); continue; }
                var cmd = LiveBots.Choose(m, LiveBotKind.Greedy, rng);
                if (cmd.Type == LiveCommandType.TapGroup) { v.TapCell(LiveSide.Own, cmd.X, cmd.Y); v.CompleteAnimations(); continue; }
                if (cmd.Type != LiveCommandType.Swap) { m.Submit(LiveCommand.Pass(0, m.NextSeq(0))); v.Refresh(); continue; }
                v.SelectSlot(cmd.HandSlot);
                v.TapCell(cmd.Side, cmd.X, cmd.Y);
                v.CompleteAnimations();
            }
        }

        /// <summary>
        /// Kullanıcı 2026-11-14: "rövanş olmayacak 2 raunt olacak ilk raunt bitince direkt 2. raunt başlayacak. Tabi ekranda bunu
        /// gösterelim". 1. raunt → düğmesiz "2. Raunt" gösterimi → kendiliğinden 2. raunt (eller ve sıra değişik) → toplamla sonuç.
        /// </summary>
        [UnityTest]
        public IEnumerator FullMatch_Round1_AutoRound2_SwapsHands_EndsWithTotalResult()
        {
            _gc.ShowLive();
            yield return null;
            var v = _gc.Live;
            v.BotDelay = 0f;
            v.RoundBreakSeconds = 0.4f;
            var rng = new LiveGenerator.Rng(3);
            var layout = v.Match.Layout;
            CollectionAssert.AreEqual(layout.Hand, v.Match.Hands[0], "1. rauntta sen A eli");
            PlayOut(v, rng);
            Assert.IsTrue(v.Match.IsOver, "8 tur sonunda 1. raunt biter");
            Assert.AreEqual(8, v.Match.TurnsUsed[0]);
            Assert.AreEqual(8, v.Match.TurnsUsed[1]);
            Assert.IsFalse(v.EndShown, "ara kart yok");
            Assert.IsTrue(v.RoundBreak, "ekranda 2. raunt gösterimi");
            Assert.IsNotNull(GameObject.Find("RoundCard"));
            Assert.IsFalse(v.SeriesOver, "maç 2. rauntla sürer");
            int s0 = v.Match.Scores[0], s1 = v.Match.Scores[1];
            int first = v.Match.FirstPlayer;

            // Dokunmadan, kısa süre sonra 2. raunt kendiliğinden başlar ve yazı kalkar.
            float limit = Time.realtimeSinceStartup + 5f;
            while ((v.Round1 == null || v.RoundBreak) && Time.realtimeSinceStartup < limit) yield return null;
            Assert.IsNotNull(v.Round1, "2. raunt kendiliğinden başladı");
            Assert.IsFalse(v.RoundBreak);
            Assert.AreSame(layout, v.Match.Layout, "aynı dizilim");
            Assert.AreEqual(1 - first, v.Match.FirstPlayer, "ilk oynayan değişti");
            if (v.Match.TurnsUsed[0] + v.Match.TurnsUsed[1] == 0)
            {
                CollectionAssert.AreEqual(layout.Hand2, v.Match.Hands[0], "2. rauntta sen B eli");
                CollectionAssert.AreEqual(layout.Hand, v.Match.Hands[1], "2. rauntta rakip A eli");
            }
            Assert.AreEqual(s0, v.ShownScore(0), "skor kutusu toplamı gösterir");
            Assert.AreEqual(s1, v.ShownScore(1));
            PlayOut(v, rng);
            // 2026-11-25 (kullanıcı: "3 raunta çıkartalım"): 2. raunttan sonra son raunt; başka dizilim, geride olan başlar.
            Assert.IsFalse(v.SeriesOver, "3. raunt var");
            Assert.IsFalse(v.EndShown);
            int r2a = v.Match.Scores[0], r2b = v.Match.Scores[1];
            int t0 = s0 + r2a, t1 = s1 + r2b;
            limit = Time.realtimeSinceStartup + 5f;
            while ((v.RoundNumber < 3 || v.RoundBreak) && Time.realtimeSinceStartup < limit) yield return null;
            Assert.AreEqual(3, v.RoundNumber, "3. raunt kendiliğinden başladı");
            Assert.AreNotSame(layout, v.Match.Layout, "son raunt başka dizilimde");
            if (t0 != t1) Assert.AreEqual(t0 < t1 ? 0 : 1, v.Match.FirstPlayer, "geride olan başlar");
            Assert.AreEqual(t0, v.ShownScore(0), "skor kutusu iki rauntun toplamı");
            PlayOut(v, rng);
            Assert.IsTrue(v.SeriesOver);
            Assert.IsTrue(v.EndShown, "sonuç kartı");
            Assert.AreEqual(t0 + v.Match.Scores[0], v.ShownScore(0));
            Assert.AreEqual(t1 + v.Match.Scores[1], v.ShownScore(1));
            _gc.CloseLive();
            yield return null;
        }

        /// <summary>
        /// Kullanıcı 2026-11-14: "Taş yerleştirmeden dokunarak da patlatma olsun". Hazır 3'lüye seçimsiz dokunuş grubu soyar,
        /// el taşı harcamaz, tur harcar; skor yazısı parçalar varınca artar.
        /// </summary>
        [UnityTest]
        public IEnumerator TapReadyGroup_WithoutStone_Peels_KeepsHand_ScoreShownAfterArrival()
        {
            _gc.ShowLive();
            yield return null;
            var v = _gc.Live;
            v.BotDelay = 0f;
            Assert.IsTrue(v.Match.Rules.TapReadyGroups, "varsayılan açık");
            var rng = new LiveGenerator.Rng(7);
            bool tapped = false;
            for (int game = 0; game < 30 && !tapped; game++)
            {
                v.NewMatch(game, 0);
                for (int k = 0; k < 40 && !v.Match.IsOver && !tapped; k++)
                {
                    var m = v.Match;
                    if (m.Current == LiveMatchView.Bot) { v.BotMove(); v.CompleteAnimations(); continue; }
                    var tap = m.LegalMoves(0).Where(x => x.Type == LiveCommandType.TapGroup).OrderByDescending(x => x.Gain).ToList();
                    if (tap.Count > 0)
                    {
                        var b = m.Boards[0];
                        int score = m.Scores[0], hand = m.Hands[0].Count, turns = m.TurnsUsed[0];
                        v.TapCell(LiveSide.Own, b.X(tap[0].Target), b.Y(tap[0].Target));
                        Assert.AreEqual(score + tap[0].Gain, m.Scores[0], "dokunuşla grup puanı");
                        Assert.AreEqual(hand, m.Hands[0].Count, "el taşı harcanmaz");
                        Assert.AreEqual(turns + 1, m.TurnsUsed[0], "tur harcar");
                        Assert.IsTrue(v.Animating, "soyulma animasyonu oynuyor");
                        Assert.AreEqual(score, v.ShownScore(0), "parçalar varmadan skor yazısı eski");
                        v.CompleteAnimations();
                        Assert.IsFalse(v.Animating);
                        Assert.AreEqual(m.Scores[0], v.ShownScore(0), "parçalar varınca skor yazısı güncel");
                        tapped = true;
                        break;
                    }
                    var cmd = LiveBots.Choose(m, LiveBotKind.Greedy, rng);
                    if (cmd.Type != LiveCommandType.Swap) { m.Submit(LiveCommand.Pass(0, m.NextSeq(0))); v.Refresh(); continue; }
                    v.SelectSlot(cmd.HandSlot);
                    v.TapCell(cmd.Side, cmd.X, cmd.Y);
                    v.CompleteAnimations();
                }
            }
            Assert.IsTrue(tapped, "hazır grup oluştu ve dokunuşla patlatıldı");
            _gc.CloseLive();
            yield return null;
        }
    }
}
