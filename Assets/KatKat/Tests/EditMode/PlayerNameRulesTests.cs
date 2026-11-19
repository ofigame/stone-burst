using KatKat.Game;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>AppGallery'de oyuncunun yazdığı sıralama adı (kullanıcı 2026-11-19: "ismini kendi versin").</summary>
    public class PlayerNameRulesTests
    {
        [Test]
        public void Clean_TrimsSpaces_DropsEmojiAndSymbols_KeepsLetters()
        {
            Assert.AreEqual("Ayşe 42", PlayerNameRules.Clean("  Ayşe   42  "));
            Assert.AreEqual("Ali.Can_7-x", PlayerNameRules.Clean("Ali.Can_7-x"));
            Assert.AreEqual("Mehmet", PlayerNameRules.Clean("Meh😀met<>!"));
            Assert.AreEqual("محمد", PlayerNameRules.Clean("محمد"));
            Assert.AreEqual("", PlayerNameRules.Clean(null));
            Assert.AreEqual(PlayerNameRules.MaxLength, PlayerNameRules.Clean("abcdefghijklmnopqrstuvwxyz").Length, "en çok 16");
        }

        [Test]
        public void Problem_ShortAndBlocked_Rejected_NormalAccepted()
        {
            Assert.AreEqual("acct.name.short", PlayerNameRules.Problem("ab"));
            Assert.AreEqual("acct.name.short", PlayerNameRules.Problem(""));
            Assert.IsNull(PlayerNameRules.Problem("Ayşe 42"));
            Assert.IsNull(PlayerNameRules.Problem("Klasik"), "kısa yasak sözcük yalnız tam sözcükse");
            Assert.IsNull(PlayerNameRules.Problem("Iraq Star"));
            Assert.AreEqual("acct.name.bad", PlayerNameRules.Problem("sik"));
            Assert.AreEqual("acct.name.bad", PlayerNameRules.Problem("xFuckerx"));
            Assert.AreEqual("acct.name.bad", PlayerNameRules.Problem("Ofigame Admin"));
        }
    }
}
