using System.Collections.Generic;
using System.IO;
using System.Linq;
using KatKat.Core;
using KatKat.Core.Live;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>Canlı maç kabul kontrolleri (docs/vr1/CLAUDE_CANLI_MAC.md §6).</summary>
    public sealed class LiveMatchTests
    {
        /// <summary>Unity ve tools/CoreTests (dotnet test) için aynı: Resources/Live havuzu.</summary>
        private static string PoolPath => Path.Combine(Path.GetDirectoryName(ProductionLevelTests.LevelsDir()), "Live", "live_pool_v1.txt");

        private static LayerColor Col(char c) => c switch { 'C' => LayerColor.Coral, 'B' => LayerColor.Blue, 'Y' => LayerColor.Yellow, _ => LayerColor.Purple };

        /// <summary>Elle dizilim: satırlar üst/alt renk (C B Y P, . etkin değil), el harfleri.</summary>
        private static LiveLayout Make(string[] top, string[] bottom, string hand)
        {
            int h = top.Length, w = top[0].Length;
            var l = new LiveLayout { Id = "test", Width = w, Height = h, Mask = new bool[w * h], Top = new LayerColor[w * h], Bottom = new LayerColor[w * h] };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    l.Mask[i] = top[y][x] != '.';
                    if (!l.Mask[i]) continue;
                    l.Top[i] = Col(top[y][x]);
                    l.Bottom[i] = Col(bottom[y][x]);
                }
            l.Hand = hand.Select(Col).ToArray();
            return l;
        }

        // Başlangıçta hazır 3'lü yok (elle denetlendi; Validate testi de doğrular).
        private static readonly string[] Top = { "CCBY", "BYPB", "YPY." };
        private static readonly string[] Bottom = { "PPPC", "CCCC", "BBB." };
        private const string Hand = "CBPYCBPY";

        private static LiveRules Rules(bool tap = false) => new LiveRules { Width = 4, Height = 3, TapReadyGroups = tap };

        private static string Snapshot(LiveMatch m)
        {
            var s = new System.Text.StringBuilder();
            for (int p = 0; p < 2; p++)
            {
                var b = m.Boards[p];
                for (int i = 0; i < b.Count; i++) { s.Append(b[i].Layers.Count); foreach (var c in b[i].Layers) s.Append((int)c); s.Append(','); }
                s.Append('|').Append(string.Join("", m.Hands[p].Select(c => (int)c))).Append('|').Append(m.Scores[p]).Append('|').Append(m.TurnsUsed[p]).Append(';');
            }
            return s.Append(m.Current).ToString();
        }

        private static int Slot(LiveMatch m, int p, LayerColor c) => m.Hands[p].IndexOf(c);

        private static LiveResult Swap(LiveMatch m, int p, LayerColor c, LiveSide side, int x, int y) =>
            m.Submit(LiveCommand.Swap(p, m.NextSeq(p), Slot(m, p, c), side, x, y));

        [Test]
        public void Opening_BothPlayers_SameMask_TwoLayers_SameHand()
        {
            var pool = LiveLayout.ParsePool(File.ReadAllText(PoolPath));
            Assert.Greater(pool.Count, 0);
            foreach (var l in pool.Take(20))
            {
                var m = new LiveMatch(l, new LiveRules());
                for (int i = 0; i < l.Count; i++)
                {
                    Assert.AreEqual(m.Boards[0][i].Active, m.Boards[1][i].Active);
                    if (!m.Boards[0][i].Active) continue;
                    Assert.AreEqual(2, m.Boards[0][i].Layers.Count);
                    CollectionAssert.AreEqual(m.Boards[0][i].Layers, m.Boards[1][i].Layers);
                }
                CollectionAssert.AreEqual(m.Hands[0], m.Hands[1]);
                Assert.AreNotSame(m.Boards[0], m.Boards[1]);
                Assert.AreNotSame(m.Hands[0], m.Hands[1]);
            }
        }

        [Test]
        public void Generator_RejectsReadyTriple()
        {
            var l = Make(new[] { "CCCY", "BYPB", "YPY." }, Bottom, Hand);
            Assert.AreEqual("hazır 3'lü", LiveGenerator.Validate(l, Rules()));
            Assert.IsNull(LiveGenerator.Validate(Make(Top, Bottom, Hand), Rules()), "örnek dizilim geçerli olmalı");
        }

        [Test]
        public void Generator_RejectsWhenOneAttackLeavesDefenderNoScore()
        {
            // Tek fırsat bölgesi: mercan ikilisi yanında tek hedef; başka renkte fırsat yok → filtre 2 ya da 3 reddeder.
            var l = Make(new[] { "CCBY", "BYPB", "YPYC" }, new[] { "PPPC", "CCCC", "BBBB" }, "CCCCCCCC");
            Assert.IsNotNull(LiveGenerator.Validate(l, Rules()));
        }

        [Test]
        public void Pool_EveryOpeningAttack_LeavesDefenderAScoringMove()
        {
            var rules = new LiveRules();
            foreach (var l in LiveLayout.ParsePool(File.ReadAllText(PoolPath)))
            {
                Assert.IsNull(LiveGenerator.Validate(l, rules), l.Id);
                var b = l.CreateBoard();
                foreach (var c in LiveMoves.DistinctColors(l.Hand))
                    for (int i = 0; i < b.Count; i++)
                    {
                        if (LiveMoves.CheckAttack(b, i, c, rules) != LiveReject.None) continue;
                        var m = new LiveMatch(l, rules);
                        var r = m.Submit(LiveCommand.Swap(0, 1, m.Hands[0].IndexOf(c), LiveSide.Opponent, b.X(i), b.Y(i)));
                        Assert.IsTrue(r.Accepted);
                        Assert.Greater(LiveMoves.Opportunities(m.Boards[1], m.Hands[1]).Count, 0, l.Id + " saldırı " + c + "@" + i);
                    }
            }
        }

        [Test]
        public void OwnTriple_PeelsGroupOnce_ScoresLayers()
        {
            var m = new LiveMatch(Make(Top, Bottom, Hand), Rules());
            var r = Swap(m, 0, LayerColor.Coral, LiveSide.Own, 2, 0);
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(LiveMoveKind.Score, r.Kind);
            Assert.AreEqual(3, r.Gained);
            Assert.AreEqual(3, m.Scores[0]);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, r.Peeled);
            for (int x = 0; x < 3; x++) Assert.AreEqual(1, m.Boards[0][x, 0].Layers.Count);
            Assert.AreEqual(2, m.Boards[0][3, 0].Layers.Count);
            Assert.AreEqual(7, m.Hands[0].Count);
            Assert.AreEqual(0, m.Scores[1]);
            Assert.AreEqual(2, m.Boards[1][2, 0].Layers.Count, "rakibin tahtası değişmez");
        }

        [Test]
        public void BottomLayerMakesNewTriple_NoChain_StonesStay()
        {
            var m = new LiveMatch(Make(Top, Bottom, Hand), Rules());
            var r = Swap(m, 0, LayerColor.Coral, LiveSide.Own, 2, 0);
            Assert.AreEqual(3, r.Gained);
            // Alttan mor-mor-mor çıktı (yanında (2,1) mor): hazır grup var ama patlamadı.
            for (int x = 0; x < 3; x++)
            {
                Assert.AreEqual(1, m.Boards[0][x, 0].Layers.Count);
                Assert.AreEqual(LayerColor.Purple, m.Boards[0][x, 0].Top);
            }
            Assert.IsTrue(LiveMoves.HasReadyGroup(m.Boards[0]));
            Assert.AreEqual(3, m.Scores[0]);
        }

        [Test]
        public void TapReadyGroup_Off_Rejected_On_PeelsWithoutHand_UsesTurn_LastLayerEmpties()
        {
            foreach (bool tap in new[] { false, true })
            {
                var m = new LiveMatch(Make(Top, Bottom, Hand), Rules(tap));
                Swap(m, 0, LayerColor.Coral, LiveSide.Own, 2, 0); // alttan mor 3'lü hazır
                Assert.IsTrue(Swap(m, 1, LayerColor.Purple, LiveSide.Own, 3, 0).Accepted); // P2 hazırlık
                var before = Snapshot(m);
                var r = m.Submit(LiveCommand.Tap(0, m.NextSeq(0), 0, 0));
                if (!tap)
                {
                    Assert.IsFalse(r.Accepted);
                    Assert.AreEqual(LiveReject.TapDisabled, r.Reject);
                    Assert.AreEqual(before, Snapshot(m));
                    continue;
                }
                Assert.IsTrue(r.Accepted);
                Assert.AreEqual(LiveMoveKind.Tap, r.Kind);
                Assert.AreEqual(4, r.Gained, "mor 3 + (2,1) mor = 4");
                Assert.AreEqual(7, m.Scores[0]);
                Assert.AreEqual(7, m.Hands[0].Count, "dokunma el taşı harcamaz");
                Assert.AreEqual(2, m.TurnsUsed[0]);
                Assert.AreEqual(1, m.Current);
                // Son katman soyuldu: hücre boş, artık değiştirilemez.
                Assert.IsTrue(m.Boards[0][0, 0].IsEmpty);
                Assert.IsTrue(Swap(m, 1, LayerColor.Coral, LiveSide.Own, 0, 1).Accepted); // P2 sırası
                var snap = Snapshot(m);
                var bad = Swap(m, 0, LayerColor.Blue, LiveSide.Own, 0, 0);
                Assert.AreEqual(LiveReject.EmptyCell, bad.Reject);
                Assert.AreEqual(snap, Snapshot(m));
                // Rakip de boş hücreye saldıramaz.
                Assert.AreEqual(LiveReject.EmptyCell, LiveMoves.CheckAttack(m.Boards[0], 0, LayerColor.Blue, m.Rules));
            }
        }

        [Test]
        public void OpponentSwap_OnlyTopChanges_NoScore_TurnAdvances()
        {
            var m = new LiveMatch(Make(Top, Bottom, Hand), Rules());
            var r = Swap(m, 0, LayerColor.Purple, LiveSide.Opponent, 3, 0);
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(LiveMoveKind.Attack, r.Kind);
            Assert.AreEqual(0, r.Gained);
            Assert.AreEqual(0, m.Scores[0] + m.Scores[1]);
            var c = m.Boards[1][3, 0];
            Assert.AreEqual(2, c.Layers.Count);
            Assert.AreEqual(LayerColor.Purple, c.Top);
            Assert.AreEqual(LayerColor.Coral, c.Layers[0], "alt katman aynı kalır");
            Assert.AreEqual(LayerColor.Yellow, m.Boards[0][3, 0].Top, "kendi tahtası değişmez");
            Assert.AreEqual(7, m.Hands[0].Count);
            Assert.AreEqual(1, m.Current);
        }

        [Test]
        public void Rejects_SameColor_EmptyCell_AttackTriple_NotYourTurn_Duplicate_StateUnchanged()
        {
            var m = new LiveMatch(Make(Top, Bottom, Hand), Rules());
            var before = Snapshot(m);
            Assert.AreEqual(LiveReject.SameColor, Swap(m, 0, LayerColor.Coral, LiveSide.Own, 0, 0).Reject);
            Assert.AreEqual(LiveReject.SameColor, Swap(m, 0, LayerColor.Coral, LiveSide.Opponent, 0, 0).Reject);
            Assert.AreEqual(LiveReject.EmptyCell, Swap(m, 0, LayerColor.Coral, LiveSide.Own, 3, 2).Reject, "etkin olmayan hücre");
            Assert.AreEqual(LiveReject.AttackMakesGroup, Swap(m, 0, LayerColor.Blue, LiveSide.Opponent, 3, 0).Reject);
            Assert.AreEqual(LiveReject.NotYourTurn, Swap(m, 1, LayerColor.Coral, LiveSide.Own, 2, 0).Reject);
            Assert.AreEqual(LiveReject.OutOfBounds, Swap(m, 0, LayerColor.Coral, LiveSide.Own, 9, 0).Reject);
            Assert.AreEqual(LiveReject.BadHandSlot, m.Submit(LiveCommand.Swap(0, 1, 8, LiveSide.Own, 2, 0)).Reject);
            Assert.AreEqual(LiveReject.PassWithLegalMove, m.Submit(LiveCommand.Pass(0, 1)).Reject);
            Assert.AreEqual(before, Snapshot(m));
            // Yinelenen komut ikinci kez işlenmez.
            var cmd = LiveCommand.Swap(0, 1, Slot(m, 0, LayerColor.Coral), LiveSide.Own, 2, 0);
            Assert.IsTrue(m.Submit(cmd).Accepted);
            Assert.IsTrue(Swap(m, 1, LayerColor.Purple, LiveSide.Own, 3, 0).Accepted);
            var mid = Snapshot(m);
            Assert.AreEqual(LiveReject.Duplicate, m.Submit(cmd).Reject);
            Assert.AreEqual(mid, Snapshot(m));
        }

        [Test]
        public void AttackElsewhereReadyTriple_DoesNotInvalidateAttack()
        {
            var m = new LiveMatch(Make(Top, Bottom, Hand), Rules());
            Swap(m, 0, LayerColor.Coral, LiveSide.Own, 2, 0); // P1 tahtasında mor 3'lü hazır kaldı
            var r = Swap(m, 1, LayerColor.Coral, LiveSide.Opponent, 3, 0); // P2, P1 tahtasında başka yere saldırır
            Assert.IsTrue(r.Accepted, r.Reject.ToString());
        }

        [Test]
        public void PrepMove_ChangesColor_NoScore_TurnAdvances()
        {
            var m = new LiveMatch(Make(Top, Bottom, Hand), Rules());
            var r = Swap(m, 0, LayerColor.Purple, LiveSide.Own, 3, 0);
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(LiveMoveKind.Prep, r.Kind);
            Assert.AreEqual(0, m.Scores[0]);
            Assert.AreEqual(LayerColor.Purple, m.Boards[0][3, 0].Top);
            Assert.AreEqual(2, m.Boards[0][3, 0].Layers.Count);
            Assert.AreEqual(1, m.Current);
            Assert.AreEqual(1, m.TurnsUsed[0]);
        }

        [Test]
        public void MatchResult_EqualTurns_ScoreIsPeeledLayers()
        {
            var pool = LiveLayout.ParsePool(File.ReadAllText(PoolPath));
            foreach (bool tap in new[] { false, true })
                foreach (var kind in new[] { LiveBotKind.Random, LiveBotKind.Greedy, LiveBotKind.Aggressive, LiveBotKind.Balanced })
                    for (int k = 0; k < 10; k++)
                    {
                        var l = pool[k];
                        var rules = new LiveRules { TapReadyGroups = tap };
                        int initial = l.CreateBoard().TotalLayers();
                        var m = LiveSim.Play(l, rules, k % 2, new[] { kind, kind }, 77 + k);
                        Assert.IsTrue(m.IsOver);
                        Assert.AreEqual(rules.TurnsPerPlayer, m.TurnsUsed[0]);
                        Assert.AreEqual(rules.TurnsPerPlayer, m.TurnsUsed[1]);
                        for (int p = 0; p < 2; p++) Assert.AreEqual(initial - m.Boards[p].TotalLayers(), m.Scores[p], "puan = soyulan katman");
                        int expect = m.Scores[0] == m.Scores[1] ? -1 : (m.Scores[0] > m.Scores[1] ? 0 : 1);
                        Assert.AreEqual(expect, m.WinnerBy(LiveTieBreak.None));
                        // Varsayılan eşitlik bozma: beraberlik kalmaz; puan farkı varsa kural etkisiz.
                        Assert.AreNotEqual(-1, m.Winner);
                        if (expect != -1) Assert.AreEqual(expect, m.Winner);
                        Assert.AreEqual(m.Winner, m.Record.Winner);
                        // En büyük tek soyma, kayıttaki en büyük kazançtır.
                        for (int p = 0; p < 2; p++)
                            Assert.AreEqual(m.Record.Moves.Where(e => e.Command.Player == p).Select(e => e.Gained).DefaultIfEmpty(0).Max(), m.BestMove[p]);
                        Assert.AreEqual(16, m.Record.Moves.Count);
                        Assert.AreEqual(l.Id, m.Record.LayoutId);
                        Assert.AreEqual(k % 2, m.Record.FirstPlayer);
                        Assert.AreEqual(m.Scores[0], m.Record.FinalScores[0]);
                        // Alternation: ilk oyuncudan başlayarak sırayla.
                        for (int i = 0; i < 16; i++) Assert.AreEqual((k % 2 + i) % 2, m.Record.Moves[i].Command.Player);
                    }
        }

        [Test]
        public void TieBreak_Rules()
        {
            // Puan farkı varsa her kuralda puan kazanır.
            foreach (var rule in LiveSim.TieRules)
                Assert.AreEqual(1, LiveMatch.Decide(new[] { 20, 21 }, new[] { 9, 3 }, 0, rule));
            // Eşit puan, farklı en büyük grup.
            var s = new[] { 20, 20 };
            var b = new[] { 5, 4 };
            Assert.AreEqual(-1, LiveMatch.Decide(s, b, 0, LiveTieBreak.None));
            Assert.AreEqual(0, LiveMatch.Decide(s, b, 0, LiveTieBreak.BiggestGroup));
            Assert.AreEqual(1, LiveMatch.Decide(s, b, 0, LiveTieBreak.SecondMover));
            Assert.AreEqual(0, LiveMatch.Decide(s, b, 0, LiveTieBreak.BiggestGroupThenSecond));
            // Eşit puan, eşit en büyük grup: ikinci oynayan (ilk P2 ise P1).
            var e = new[] { 4, 4 };
            Assert.AreEqual(-1, LiveMatch.Decide(s, e, 0, LiveTieBreak.BiggestGroup));
            Assert.AreEqual(1, LiveMatch.Decide(s, e, 0, LiveTieBreak.BiggestGroupThenSecond));
            Assert.AreEqual(0, LiveMatch.Decide(s, e, 1, LiveTieBreak.BiggestGroupThenSecond));
            Assert.AreEqual(0, LiveMatch.Decide(s, e, 1, LiveTieBreak.SecondMover));
        }

        [Test]
        public void TieBreak_InRealMatch_UsesBiggestSingleMove()
        {
            // P1 bir hamlede 3 (mercan), P2 aynı tahtada aynı hamle: 3–3, en büyük grup 3–3 → varsayılan kuralda ikinci oynayan (P2).
            var rules = Rules();
            rules.TurnsPerPlayer = 1;
            var m = new LiveMatch(Make(Top, Bottom, Hand), rules);
            Swap(m, 0, LayerColor.Coral, LiveSide.Own, 2, 0);
            Swap(m, 1, LayerColor.Coral, LiveSide.Own, 2, 0);
            Assert.IsTrue(m.IsOver);
            Assert.AreEqual(3, m.Scores[0]);
            Assert.AreEqual(3, m.Scores[1]);
            Assert.AreEqual(-1, m.WinnerBy(LiveTieBreak.None));
            Assert.AreEqual(-1, m.WinnerBy(LiveTieBreak.BiggestGroup));
            Assert.AreEqual(1, m.Winner);
            Assert.IsNotNull(m.TieReason());
            StringAssert.Contains("kazanan=P2", m.Record.ToText());
        }

        [Test]
        public void NoLegalMove_PassAccepted_UsesTurn()
        {
            // Tek hücreli tahta + tek renk el: kendi tahtada aynı renk, rakipte aynı renk → yasal hamle yok.
            var l = Make(new[] { "C" }, new[] { "B" }, "C");
            var rules = new LiveRules { Width = 1, Height = 1, HandSize = 1, TurnsPerPlayer = 1 };
            var m = new LiveMatch(l, rules);
            Assert.IsFalse(m.HasLegalMove(0));
            var r = m.Submit(LiveCommand.Pass(0, 1));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(LiveMoveKind.Pass, r.Kind);
            Assert.AreEqual(1, m.TurnsUsed[0]);
            Assert.AreEqual(1, m.Current);
        }

        [Test]
        public void CanonicalKey_ColorRenamedMirroredRotated_SameFamily()
        {
            var pool = LiveLayout.ParsePool(File.ReadAllText(PoolPath));
            foreach (var l in pool.Take(15))
            {
                string key = LiveGenerator.CanonicalKey(l);
                // Renk adları değiştirilmiş kopya (döngüsel kaydırma).
                var renamed = Clone(l, i => i, c => (LayerColor)(((int)c + 1) % 4));
                Assert.AreEqual(key, LiveGenerator.CanonicalKey(renamed), l.Id + " renk");
                int w = l.Width, h = l.Height;
                // Yatay ayna ve 180° döndürme (maske uyuyorsa aynı aile; uymuyorsa farklı olabilir).
                var mirror = Clone(l, i => (i / w) * w + (w - 1 - i % w), c => c);
                if (SameMask(l, mirror)) Assert.AreEqual(key, LiveGenerator.CanonicalKey(mirror), l.Id + " ayna");
                var rot = Clone(l, i => l.Count - 1 - i, c => c);
                if (SameMask(l, rot)) Assert.AreEqual(key, LiveGenerator.CanonicalKey(rot), l.Id + " döndürme");
                // Alt katman farkı ayrı aile.
                var diffBottom = Clone(l, i => i, c => c);
                int a = System.Array.FindIndex(diffBottom.Mask, x => x);
                diffBottom.Bottom[a] = (LayerColor)(((int)diffBottom.Bottom[a] + 1) % 4);
                Assert.AreNotEqual(key, LiveGenerator.CanonicalKey(diffBottom));
            }
            // Havuzda aynı aileden iki dizilim yok.
            var keys = pool.Select(LiveGenerator.CanonicalKey).ToList();
            Assert.AreEqual(keys.Count, keys.Distinct().Count());
        }

        [Test]
        public void Pool_IsReproducible_FromRecordedSeed()
        {
            var text = File.ReadAllText(PoolPath);
            var header = text.Split('\n').First(s => s.StartsWith("# gen="));
            long seed = long.Parse(header.Split(' ').First(s => s.StartsWith("seed=")).Substring(5));
            var stored = LiveLayout.ParsePool(text);
            var again = LiveGenerator.BuildPool(12, seed, new LiveRules());
            for (int i = 0; i < again.Count; i++) Assert.AreEqual(stored[i].ToLine(), again[i].ToLine(), "dizilim " + i);
            foreach (var l in stored) Assert.AreEqual(l.ToLine(), LiveLayout.Parse(l.ToLine()).ToLine());
            // Türler karışık: her açılış türü havuzda var.
            foreach (var t in LiveOpening.All) Assert.IsTrue(stored.Any(l => l.HasTag(t)), "tür " + t);
        }

        private static string DiffPoolPath => Path.Combine(Path.GetDirectoryName(PoolPath), "live_pool_diffhands_v2.txt");

        [Test]
        public void DifferentHands_LayoutRoundTrip_EachPlayerGetsOwnHand()
        {
            var l = LiveLayout.ParsePool(File.ReadAllText(PoolPath))[0];
            Assert.IsFalse(l.DifferentHands);
            Assert.AreEqual(l.ToLine(), LiveLayout.Parse(l.ToLine()).ToLine(), "aynı elli satır değişmez");
            l.Hand2 = l.Hand.Select(c => (LayerColor)(((int)c + 1) % 4)).ToArray();
            var back = LiveLayout.Parse(l.ToLine());
            Assert.IsTrue(back.DifferentHands);
            CollectionAssert.AreEqual(l.Hand2, back.Hand2);
            var m = new LiveMatch(back, new LiveRules { DifferentHands = true });
            CollectionAssert.AreEqual(l.Hand, m.Hands[0]);
            CollectionAssert.AreEqual(l.Hand2, m.Hands[1]);
            CollectionAssert.AreEqual(l.Hand2, m.Record.Hand2);
            StringAssert.Contains(" el2=", m.Record.ToText());
            // İkinci el kanonik anahtara girer.
            var same = LiveLayout.Parse(l.ToLine());
            same.Hand2 = null;
            Assert.AreNotEqual(LiveGenerator.CanonicalKey(same), LiveGenerator.CanonicalKey(back));
        }

        [Test]
        public void DifferentHands_Validate_RejectsIdenticalOrUnequalHands()
        {
            var rules = new LiveRules { DifferentHands = true };
            var l = LiveLayout.ParsePool(File.ReadAllText(DiffPoolPath))[0];
            Assert.IsNull(LiveGenerator.Validate(l, rules));
            var copy = LiveLayout.Parse(l.ToLine());
            copy.Hand2 = (LayerColor[])copy.Hand.Clone();
            Assert.AreEqual("eller aynı", LiveGenerator.Validate(copy, rules));
            copy.Hand2 = Enumerable.Repeat(LayerColor.Coral, copy.Hand.Length).ToArray();
            Assert.IsNotNull(LiveGenerator.Validate(copy, rules));
            Assert.AreEqual("beklenmeyen ikinci el", LiveGenerator.Validate(l, new LiveRules()));
        }

        [Test]
        public void DifferentHandsPool_AllValid_Equivalent_Distinct_Reproducible()
        {
            var rules = new LiveRules { DifferentHands = true };
            var text = File.ReadAllText(DiffPoolPath);
            var pool = LiveLayout.ParsePool(text);
            Assert.Greater(pool.Count, 0);
            foreach (var l in pool)
            {
                Assert.IsTrue(l.DifferentHands, l.Id);
                Assert.AreEqual(LiveGenerator.VersionDifferentHands, l.GenVersion);
                StringAssert.StartsWith("LM2-", l.Id);
                Assert.IsNull(LiveGenerator.Validate(l, rules), l.Id);
                Assert.IsFalse(LiveGenerator.SameMultiset(l.Hand, l.Hand2), l.Id);
                var b = l.CreateBoard();
                Assert.AreEqual(LiveGenerator.HandValue(b, l.Hand).Best, LiveGenerator.HandValue(b, l.Hand2).Best, l.Id);
            }
            Assert.AreEqual(pool.Count, pool.Select(LiveGenerator.CanonicalKey).Distinct().Count());
            var header = text.Split('\n').First(s => s.StartsWith("# gen="));
            long seed = long.Parse(header.Split(' ').First(s => s.StartsWith("seed=")).Substring(5));
            var again = LiveGenerator.BuildPool(8, seed, rules);
            for (int i = 0; i < again.Count; i++) Assert.AreEqual(pool[i].ToLine(), again[i].ToLine(), "dizilim " + i);
            // Botlar farklı ellerle tam maç oynar.
            var m = LiveSim.Play(pool[0], rules, 0, new[] { LiveBotKind.Balanced, LiveBotKind.Balanced }, 3);
            Assert.IsTrue(m.IsOver);
        }

        private static bool SameMask(LiveLayout a, LiveLayout b)
        {
            for (int i = 0; i < a.Count; i++) if (a.Mask[i] != b.Mask[i]) return false;
            return true;
        }

        /// <summary>Hücre eşlemesi (yeni i ← eski map(i)) ve renk eşlemesiyle kopya.</summary>
        private static LiveLayout Clone(LiveLayout l, System.Func<int, int> map, System.Func<LayerColor, LayerColor> col)
        {
            int n = l.Count;
            var c = new LiveLayout { Id = l.Id, Width = l.Width, Height = l.Height, Mask = new bool[n], Top = new LayerColor[n], Bottom = new LayerColor[n] };
            for (int i = 0; i < n; i++)
            {
                int j = map(i);
                c.Mask[i] = l.Mask[j];
                c.Top[i] = col(l.Top[j]);
                c.Bottom[i] = col(l.Bottom[j]);
            }
            c.Hand = l.Hand.Select(col).ToArray();
            return c;
        }
    }
}
