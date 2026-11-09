namespace KatKat.Core
{
    /// <summary>Hücrenin üstünde duran özel taş (v5 §2). Yığını değiştirmez; dokunulunca ateşlenir.</summary>
    public enum SpecialKind
    {
        None = 0,
        /// <summary>Rulo roketi, satırı sarar.</summary>
        RocketRow = 1,
        /// <summary>Rulo roketi, sütunu sarar.</summary>
        RocketColumn = 2,
        /// <summary>Gökkuşağı kıvrımı: kendi rengindeki bütün açık yığınlar.</summary>
        Rainbow = 3,
    }

    public static class SpecialKindExt
    {
        public static bool IsRocket(this SpecialKind k) => k == SpecialKind.RocketRow || k == SpecialKind.RocketColumn;
    }
}
