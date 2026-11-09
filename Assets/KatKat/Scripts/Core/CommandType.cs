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
    }
}
