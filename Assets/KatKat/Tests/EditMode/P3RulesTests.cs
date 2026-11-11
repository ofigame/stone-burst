using System.Collections.Generic;
using System.Text;
using KatKat.Core;
using KatKat.Levels;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>
    /// vr1 P3 kuralları (05_TAHTA_YENILIKLER; yalnız pilot katalogda, yetenek anahtarıyla): aşamalı hedef, zincir mührü,
    /// iki alan. Eski bölümler bu alanları taşımaz; davranışları değişmez (ana katalog testleri ayrıca koşar).
    /// </summary>
    public class P3RulesTests
    {
        private static readonly Dictionary<char, string> Col = new Dictionary<char, string> { { 'c', "coral" }, { 'b', "blue" }, { 'y', "yellow" }, { 'p', "purple" } };

        /// <summary>
        /// Satır başına boşlukla ayrılmış hücreler: katmanlar alttan üste harflerle ("bc" = alt mavi, üst mercan); "." etkin
        /// değil; "g1:bc" geçit 1 (katmanlarıyla); "a1:" önekiyle alan 1.
        /// </summary>
        private static LevelState Build(string[] rows, string goals, int moves = 5, string caps = "", string extra = "")
        {
            int h = rows.Length, w = rows[0].Split(' ').Length;
            var sb = new StringBuilder();
            sb.Append("{\"schemaVersion\":2,\"id\":\"p3_test\",\"width\":").Append(w).Append(",\"height\":").Append(h)
              .Append(",\"moveLimit\":").Append(moves).Append(",\"trialType\":\"pilot\",\"capabilities\":[").Append(caps).Append("],\"goals\":[").Append(goals).Append("],\"cells\":[");
            bool first = true;
            for (int y = 0; y < h; y++)
            {
                var tok = rows[y].Split(' ');
                for (int x = 0; x < w; x++)
                {
                    string t = tok[x];
                    int area = 0;
                    if (t.StartsWith("a1:")) { area = 1; t = t.Substring(3); }
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append("{\"x\":").Append(x).Append(",\"y\":").Append(y);
                    if (area != 0) sb.Append(",\"area\":").Append(area);
                    if (t == ".") sb.Append(",\"active\":false");
                    else if (t.StartsWith("g"))
                    {
                        int colon = t.IndexOf(':');
                        sb.Append(",\"active\":false,\"gate\":").Append(t.Substring(1, colon - 1)).Append(",\"gateLayers\":[").Append(Layers(t.Substring(colon + 1))).Append(']');
                    }
                    else sb.Append(",\"layers\":[").Append(Layers(t)).Append(']');
                    sb.Append('}');
                }
            }
            sb.Append(']').Append(extra).Append('}');
            var d = LevelParser.Parse(sb.ToString(), "p3");
            var errors = LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d));
            Assert.IsEmpty(errors, string.Join(" | ", errors));
            return LevelLoader.ToState(d);
        }

        private static string Layers(string s)
        {
            var parts = new List<string>();
            foreach (var ch in s) parts.Add("\"" + Col[ch] + "\"");
            return string.Join(",", parts);
        }

        private static string Goal(string color, int amount, int stage) =>
            "{\"type\":\"collectColor\",\"color\":\"" + color + "\",\"amount\":" + amount + ",\"stage\":" + stage + "}";

        /// <summary>Aşama tamamlanınca dalga sonunda sonraki aşama; aynı hamlenin sonraki zincir dalgası yeni aşamaya sayılır.</summary>
        [Test]
        public void Stages_AdvanceAfterWave_NextWaveCountsForNextStage()
        {
            var s = Build(new[] { "bc bc bc y", "y p y p", "p y p y", "y p y p" }, Goal("coral", 3, 0) + "," + Goal("blue", 3, 1), caps: "\"stages\"");
            Assert.AreEqual(2, s.StageCount);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.IsTrue(r.Accepted);
            Assert.IsTrue(r.Waves[0].StageAdvanced, "mercan dalgası aşama 0'ı bitirir");
            Assert.AreEqual(GameStatus.Won, s.Status, "zincirdeki mavi dalga aşama 1'e sayılır");
        }

        /// <summary>Aynı dalganın fazlası sonraki aşamaya aktarılmaz; etkin olmayan aşamaya ilerleme sayılmaz.</summary>
        [Test]
        public void Stages_ExcessNotCarried_InactiveStageNotCounted()
        {
            var s = Build(new[] { "yc yc yc p", "y p y p", "p y p y", "p p p y" }, Goal("coral", 2, 0) + "," + Goal("coral", 1, 1), caps: "\"stages\"");
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(1, s.ActiveStage);
            Assert.AreEqual(3, s.Goals[0].Progress);
            Assert.AreEqual(0, s.Goals[1].Progress, "fazla mercan aktarılmaz");
            Assert.AreEqual(GameStatus.Idle, s.Status);

            var t = Build(new[] { "c c c p", "y y y p", "p p y y", "y p p y" }, Goal("coral", 3, 0) + "," + Goal("yellow", 3, 1), caps: "\"stages\"");
            RulesEngine.TryApply(t, Command.Tap(0, 1)); // önce sarı (aşama 1 etkin değil)
            Assert.AreEqual(0, t.Goals[1].Progress, "etkin olmayan aşama sayılmaz");
            Assert.AreEqual(0, t.ActiveStage);
        }

        /// <summary>Mühür: otomatik zincir derinliği ulaşınca geçit açılır; doğrudan dokunuş derinlik 0; yeterli değilse açılmaz.</summary>
        [Test]
        public void Seal_OpensGateAtChainDepth()
        {
            string[] rows = { "bc bc bc g1:yy", "p y p y", "y p y p", "p y p y" };
            string goals = Goal("yellow", 2, 0);
            var s = Build(rows, goals, caps: "\"seal\",\"gate\"", extra: ",\"seal\":{\"wave\":1,\"gate\":1}");
            Assert.IsFalse(s.Board[3, 0].Active);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0)); // mercan dokunuş, alttan çıkan mavi zinciri (derinlik 1)
            Assert.IsTrue(s.SealOpen);
            Assert.IsTrue(s.Board[3, 0].Active, "geçit açıldı");
            Assert.IsTrue(r.Waves.Exists(w => w.SealOpened));

            var deep = Build(rows, goals, caps: "\"seal\",\"gate\"", extra: ",\"seal\":{\"wave\":2,\"gate\":1}");
            RulesEngine.TryApply(deep, Command.Tap(0, 0));
            Assert.IsFalse(deep.SealOpen, "tek zincir dalgası 2. derinliğe ulaşmaz");
            Assert.IsFalse(deep.Board[3, 0].Active);
        }

        /// <summary>İki alan: tepsideki satır roketi yalnız hedefin alanında; diğer alandaki aynı satır soyulmaz.</summary>
        [Test]
        public void Areas_RocketStaysInChosenArea()
        {
            var s = Build(new[] { "c y c . a1:y a1:c a1:y", "p p p . a1:p a1:y a1:p", "y p y . a1:y a1:p a1:y", "p y p . a1:p a1:y a1:p", "y p y . a1:y a1:p a1:y" }, Goal("purple", 2, 0), moves: 3, caps: "\"areas\",\"specials\"");
            s.Tray.Add(SpecialKind.RocketRow);
            int beforeArea1 = s.Board[4, 0].Layers.Count + s.Board[5, 0].Layers.Count + s.Board[6, 0].Layers.Count;
            var r = RulesEngine.TryApply(s, Command.UseSpecial(0, 0, 0));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(0, s.Board[0, 0].Layers.Count, "alan 0 satırı soyuldu");
            Assert.AreEqual(beforeArea1, s.Board[4, 0].Layers.Count + s.Board[5, 0].Layers.Count + s.Board[6, 0].Layers.Count, "alan 1 dokunulmadı");
        }

        /// <summary>Yetenek anahtarı olmadan yeni veri reddedilir (ana katalog yanlışlıkla P3 kuralı taşıyamaz).</summary>
        [Test]
        public void NewRules_RequireCapability()
        {
            var d = LevelParser.Parse("{\"schemaVersion\":2,\"id\":\"x\",\"width\":4,\"height\":4,\"moveLimit\":3,\"trialType\":\"pilot\",\"goals\":[" + Goal("coral", 1, 0) + "," + Goal("blue", 1, 1)
                + "],\"cells\":[{\"x\":0,\"y\":0,\"layers\":[\"coral\"]}]}", "x");
            Assert.IsTrue(LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)).Exists(e => e.Contains("stages")));
        }
    }
}
