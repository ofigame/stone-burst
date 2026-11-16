using System.Collections.Generic;
using KatKat.Core;
using KatKat.Levels;
using KatKat.View;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>
    /// Yeni renkler (2026-11-16): green = yeşil yaprak, navy = lacivert yıldız. Seviye dosyası bu kimlikleri yazınca
    /// ayrıştırma, kurallar ve palet sorunsuz çalışmalı; ilk dört rengin kimliği ve atlas hücresi değişmemeli.
    /// </summary>
    public class NewColorsTests
    {
        // LevelDataTests.ChainFixture ile aynı düzen; mercan/turkuaz yerine yeşil/lacivert. v14: zincir yok, iki dokunuş.
        private const string Fixture = @"{
  ""schemaVersion"": 1,
  ""id"": ""fixture_green_navy"",
  ""width"": 4,
  ""height"": 4,
  ""moveLimit"": 2,
  ""goals"": [
    { ""type"": ""collectColor"", ""color"": ""green"", ""amount"": 3 },
    { ""type"": ""collectColor"", ""color"": ""navy"", ""amount"": 3 }
  ],
  ""cells"": [
    { ""x"": 0, ""y"": 0, ""active"": true, ""layers"": [""navy"", ""green""] },
    { ""x"": 1, ""y"": 0, ""active"": true, ""layers"": [""navy"", ""green""] },
    { ""x"": 2, ""y"": 0, ""active"": true, ""layers"": [""navy"", ""green""] }
  ],
  ""solution"": [ { ""type"": ""tap"", ""x"": 0, ""y"": 0 }, { ""type"": ""tap"", ""x"": 0, ""y"": 0 } ]
}";

        [Test]
        public void GreenAndNavy_ParseRoundTrip_AndHaveDistinctPalette()
        {
            var d = LevelParser.Parse(Fixture);
            var state = LevelLoader.ToState(d);
            Assert.AreEqual(LayerColor.Green, state.Board[0, 0].Top);
            Assert.AreEqual(LayerColor.Navy, state.Board[0, 0].Layers[0]);
            Assert.AreEqual(LayerColor.Green, state.Goals[0].Color);
            Assert.AreEqual(LayerColor.Navy, state.Goals[1].Color);

            // Kimlik gidiş-dönüşü: bütün renkler, eski dördü dahil.
            foreach (LayerColor c in System.Enum.GetValues(typeof(LayerColor)))
            {
                Assert.IsTrue(ColorIds.TryParse(ColorIds.ToId(c), out var back), c.ToString());
                Assert.AreEqual(c, back);
            }
            Assert.AreEqual("green", ColorIds.ToId(LayerColor.Green));
            Assert.AreEqual("navy", ColorIds.ToId(LayerColor.Navy));
            Assert.AreEqual(System.Enum.GetValues(typeof(LayerColor)).Length, LayerColors.Count);

            // Palet: her rengin yüz, şerit rengi ve atlas hücresi ayrı; ilk dört hücre eski 2x2 düzende.
            var fronts = new HashSet<UnityEngine.Color32>();
            var rims = new HashSet<UnityEngine.Color32>();
            var cells = new HashSet<UnityEngine.Vector2>();
            for (int i = 0; i < LayerColors.Count; i++)
            {
                var c = (LayerColor)i;
                Assert.IsTrue(fronts.Add(LayerPalette.FrontColor(c)), "yüz rengi tekrar: " + c);
                Assert.IsTrue(rims.Add(LayerPalette.RimColor(c)), "şerit rengi tekrar: " + c);
                var cell = LayerPalette.IconCell(c);
                Assert.IsTrue(cells.Add(cell), "atlas hücresi tekrar: " + c);
                Assert.That(cell.x, Is.InRange(0f, 3f));
                Assert.That(cell.y, Is.InRange(0f, 1f));
                if (i < 4) Assert.AreEqual(new UnityEngine.Vector2(i % 2, i / 2), cell, "eski hücre değişti: " + c);
            }
            Assert.AreEqual("yaprak", LayerPalette.ShapeName(LayerColor.Green));
            Assert.AreEqual("yıldız", LayerPalette.ShapeName(LayerColor.Navy));

            // Kurallar: yeni renkler eski renkler gibi soyulur ve hedefe sayılır.
            CollectionAssert.IsEmpty(LevelValidator.ValidateStatic(d, LevelValidator.FirstRelease));
            var rep = LevelValidator.ReplaySolution(d);
            Assert.IsTrue(rep.Success, rep.Error);
            Assert.AreEqual(6, rep.LayersPeeled);
            Assert.AreEqual(GameStatus.Won, rep.FinalStatus);
        }
    }
}
