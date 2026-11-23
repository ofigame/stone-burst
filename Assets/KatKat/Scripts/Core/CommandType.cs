namespace KatKat.Core
{
    public enum CommandType
    {
        Tap = 0,
        Roll = 1,
        /// <summary>Döner taban (Premium-v2 M08): yığının katman sırasını döndürür, bir hamle harcar.</summary>
        Rotate = 2,
        /// <summary>M12 boya damlası: örtüsüz yığının yalnızca üst rengini değiştirir; bir hamle ve bir boya.</summary>
        Paint = 3,
        /// <summary>v6 özel taş tepsisi: tepsideki (Row = yuva) roket/Altın Rulo hedef hücrede ateşlenir; hamle harcamaz.</summary>
        UseSpecial = 4,
        /// <summary>2026-11-20 güçlendirici (külçeyle): hedefin 3×3 çevresindeki her yığından bir kat soyar, örtülere vurur; hamle harcamaz.</summary>
        Bomb = 5,
        /// <summary>2026-11-21 sürükle-değiştir (kullanıcı: "taşı sürükleyerek 3'lüyü tamamlama … kendi katındaki taşla yer değiştirir"):
        /// (X, Y) yığınının üst taşı, Row yönündeki komşu yığının aynı kattaki taşıyla yer değiştirir; 3+ grup oluşursa o grup
        /// dokunuş gibi patlar. Bir hamle harcar.</summary>
        Swap = 6,
        /// <summary>2026-11-23 kova kipi: etkin kovadaki taşlar teslim edilir (eksik kısım sıranın sonuna), sıradaki kova gelir; hamle harcamaz.</summary>
        SendBucket = 7,
    }
}
