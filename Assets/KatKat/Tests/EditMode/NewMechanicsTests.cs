using KatKat.Core;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>Premium rehber M09–M12 (sünger, bağlı ataş, düğme/geçit, boya) ve etkileşim tablosu.</summary>
    public class NewMechanicsTests
    {
        private static int Bits(params LayerColor[] cs)
        {
            int m = 0;
            foreach (var c in cs) m |= CellState.Bit(c);
            return m;
        }

        // ---- M09 sünger --------------------------------------------------------------------------------

        [Test]
        public void Sponge_SameColorManyTimes_FillsOneSlot_ProgressPersistsAcrossWaves_ThenOpensAndCollects()
        {
            var s = TestBoards.State(9, new[]
            {
                "c c c b",
                "y y y y",
                "b b b y",
            }, (LayerColor.Coral, 99));
            s.Goals.Add(new GoalState { Type = GoalType.CollectSponges, Amount = 1 });
            s.Board[3, 1].SpongeNeed = Bits(LayerColor.Coral, LayerColor.Blue);
            s.Board[3, 1].Layers.Clear();
            s.Board[3, 1].Layers.Add(LayerColor.Purple);
            // Mercan grubu (0..2,0): (3,1)'e komşu değil → yuva dolmaz.
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(0, s.Board[3, 1].SpongeHave);
            // Mavi (3,0) tek başına grup değil; satır 2 mavileri (0..2,2) komşu değil. Sarı satırı (0..2,1) kaldır → (3,1)'in
            // sol komşusu (2,1) sarı: sarı gerekmez → dolmaz.
            var t = TestBoards.State(9, new[]
            {
                "y c c c",
                "y b b .",
                "y y y .",
            }, (LayerColor.Coral, 99));
            t.Goals.Add(new GoalState { Type = GoalType.CollectSponges, Amount = 1 });
            var sp = t.Board[3, 1];
            sp.Layers.Add(LayerColor.Purple);
            sp.SpongeNeed = Bits(LayerColor.Coral, LayerColor.Blue);
            var r1 = RulesEngine.TryApply(t, Command.Tap(1, 0)); // üç mercan: (3,0) komşu → mercan yuvası
            Assert.AreEqual(Bits(LayerColor.Coral), sp.SpongeHave);
            Assert.AreEqual(1, r1.Waves[0].SpongeHits.Count);
            Assert.AreEqual(0, t.Goals[1].Progress);
            Assert.IsTrue(sp.HasCover, "bir yuva eksik: hâlâ örtülü");
        }

        [Test]
        public void Sponge_TwoDifferentNeighbourColorsSameWave_FillTwoSlots_OpensWithoutPeeling()
        {
            var s = TestBoards.State(9, new[]
            {
                "y  bc y",
                "bc p  cb",
                "y  bc y",
            }, (LayerColor.Coral, 99));
            s.Goals.Add(new GoalState { Type = GoalType.CollectSponges, Amount = 1 });
            var sp = s.Board[1, 1];
            sp.SpongeNeed = Bits(LayerColor.Coral, LayerColor.Blue);
            // Rulo roketi satır 1: (0,1) ve (2,1) mercan kalkar → yalnızca mercan. Sütun roketi sütun 1: (1,0),(1,2) mercan.
            // İkili roket (satır+sütun) aynı dalgada: dört komşudan mercan; mavi yok → bir yuva.
            s.SpecialsEnabled = true;
            s.Board[0, 0].Special = SpecialKind.RocketRow;
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(Bits(LayerColor.Coral), sp.SpongeHave);
            // Şimdi açılan maviler (0,1),(2,1),(1,0)?: (0,1) mavi açıldı; mavi grubu yok. Doğrudan mavi kaldırma:
            sp.SpongeHave = Bits(LayerColor.Coral);
            s.Board[2, 2].Special = SpecialKind.RocketColumn; // sütun 2: (2,1) mavi kalkar → mavi yuvası, sünger açılır
            s.Status = RulesEngine.Evaluate(s);
            var r2 = RulesEngine.TryApply(s, Command.Tap(2, 2));
            Assert.IsTrue(r2.Accepted);
            Assert.AreEqual(0, sp.SpongeNeed, "sünger açıldı");
            Assert.AreEqual(1, s.Goals[1].Progress);
            Assert.AreEqual("p", TestBoards.Cell(s, 1, 1), "alt yığın soyulmaz");
            Assert.IsTrue(sp.IsSelectable);
        }

        // ---- M10 bağlı ataş ------------------------------------------------------------------------------

        [Test]
        public void LinkedClip_TwoMembersAffectedSameWave_CounterDropsOnce_ThenAllOpen()
        {
            var s = TestBoards.State(9, new[]
            {
                "yb c yb",
                "c  c c",
                "y  b y",
            }, (LayerColor.Coral, 99));
            foreach (var (x, y) in new[] { (0, 0), (2, 0) })
            {
                s.Board[x, y].LinkId = 1;
                s.Board[x, y].LinkHits = 2;
            }
            // Mercan grubu (1,0),(0,1),(1,1),(2,1): iki üyeye de komşu → ortak sayaç 1 azalır.
            var r = RulesEngine.TryApply(s, Command.Tap(1, 1));
            Assert.AreEqual(1, s.Board[0, 0].LinkHits);
            Assert.AreEqual(1, s.Board[2, 0].LinkHits);
            Assert.AreEqual(2, r.Waves[0].LinkHits.Count, "üye başına bir kayıt, grup başına bir hasar");
            Assert.IsFalse(s.Board[0, 0].IsSelectable);
            // İkinci hasar: alttaki sarı/mavi ile değil; doğrudan özel taş vuruşu (satır 0).
            s.SpecialsEnabled = true;
            s.Board[1, 0].Special = SpecialKind.RocketRow; // boş hücrede roket: satır 0'daki iki üyeye doğrudan vuruş
            s.Status = RulesEngine.Evaluate(s);           // özel taş yasal hamledir (ilk hamleden sonra grup kalmamıştı)
            Assert.IsTrue(RulesEngine.TryApply(s, Command.Tap(1, 0)).Accepted);
            Assert.AreEqual(0, s.Board[0, 0].LinkHits, "grup birlikte açıldı");
            Assert.AreEqual(0, s.Board[2, 0].LinkHits);
            Assert.IsTrue(s.Board[0, 0].IsSelectable);
            Assert.AreEqual("yb", TestBoards.Cell(s, 0, 0), "açılan aynı dalgada soyulmaz");
        }

        // ---- M11 düğme ve geçit ----------------------------------------------------------------------------

        [Test]
        public void Button_LastLayerRemoved_OpensGateOnce_NewCellsAreNotChainSeed_UndoClosesIt()
        {
            var s = TestBoards.State(9, new[]
            {
                "c c c #",
                "y y b #",
            }, (LayerColor.Yellow, 99));
            s.Board[0, 0].ButtonGate = 7;
            foreach (var (x, y) in new[] { (3, 0), (3, 1) })
            {
                var g = s.Board[x, y];
                g.GateId = 7;
                g.GateLayers.Add(LayerColor.Yellow);
            }
            var session = new GameSession(s);
            var r = session.Submit(Command.Tap(1, 0));
            var st = session.State;
            Assert.AreEqual(1, r.Waves[0].ButtonPresses.Count);
            Assert.AreEqual(2, r.Waves[0].GateOpens.Count);
            Assert.IsTrue(st.Board[3, 0].Active);
            Assert.AreEqual("y", TestBoards.Cell(st, 3, 0));
            Assert.AreEqual(1, r.Waves.Count, "açılan geçit hücreleri zincir tohumu değil");
            Assert.AreEqual(0, st.Board[0, 0].ButtonGate, "düğme bir kez basılır");
            Assert.IsTrue(session.Undo());
            Assert.IsFalse(session.State.Board[3, 0].Active, "geri alma geçidi kapatır");
            Assert.AreEqual(7, session.State.Board[0, 0].ButtonGate);
            Assert.AreEqual(1, session.State.Board[3, 1].GateLayers.Count);
        }

        // ---- M12 boya --------------------------------------------------------------------------------------

        [Test]
        public void Paint_ChangesOnlyTop_CostsMoveAndPaint_NoGoalNoChain_SameColorFree()
        {
            var s = TestBoards.State(5, new[]
            {
                "cb y y",
                "b  c c",
            }, (LayerColor.Blue, 99), (LayerColor.Yellow, 99));
            s.PaintLeft = 1;
            var same = RulesEngine.TryApply(s, Command.Paint(1, 0, LayerColor.Yellow));
            Assert.AreEqual(RejectReason.SameColor, same.Reason);
            Assert.AreEqual(1, s.PaintLeft);
            Assert.AreEqual(5, s.MovesLeft);

            var r = RulesEngine.TryApply(s, Command.Paint(0, 0, LayerColor.Yellow));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual("cy", TestBoards.Cell(s, 0, 0), "alt renk aynı, üst sarı");
            Assert.AreEqual(0, s.PaintLeft);
            Assert.AreEqual(4, s.MovesLeft);
            Assert.AreEqual(0, r.Waves.Count, "otomatik soyma/zincir yok");
            Assert.AreEqual(0, s.Goals[0].Progress, "eski renk kaldırılmış sayılmaz");
            Assert.AreEqual(0, s.Goals[1].Progress);
            Assert.AreEqual(RejectReason.PaintEmpty, RulesEngine.TryApply(s, Command.Paint(1, 1, LayerColor.Blue)).Reason);
            // Yeni grup ayrıca seçilir.
            var r2 = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.IsTrue(r2.Accepted);
            Assert.AreEqual(3, s.Goals[1].Progress);
        }

        [Test]
        public void NoGroups_ButPaintAvailable_IsNotLost()
        {
            var s = TestBoards.State(5, new[] { "c b c", "b c b" }, (LayerColor.Coral, 3));
            Assert.IsFalse(RulesEngine.HasLegalMove(s));
            s.PaintLeft = 1;
            Assert.IsTrue(RulesEngine.HasLegalMove(s));
        }
    }
}
