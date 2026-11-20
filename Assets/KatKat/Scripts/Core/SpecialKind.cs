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
        /// <summary>2026-11-20 güçlendirici bombası: tahtaya konmaz, yalnız <see cref="CommandType.Bomb"/> ile 3×3 alanda ateşlenir.</summary>
        Bomb = 4,
        /// <summary>2026-11-21 Kürek: çakıllı tahtada 4 taşlık grup patlatınca tepsiye girer; seçilen hücre merkezli 3×3 alandaki
        /// çakılı temizler (taş soymaz). Hamle harcamaz.</summary>
        Shovel = 5,
    }

    public static class SpecialKindExt
    {
        public static bool IsRocket(this SpecialKind k) => k == SpecialKind.RocketRow || k == SpecialKind.RocketColumn;
    }
}
