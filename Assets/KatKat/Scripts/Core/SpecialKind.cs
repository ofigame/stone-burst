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
        /// <summary>2026-11-26 (kullanıcı: "Roket ve altın rulolar tıkladığımız taşın olduğu yere konacak ve 4 dibinde olan taşlardan birine sürüklediğimiz zaman o renkteki taşları patlatacak altın rulo. roket ise zaten o taştan yatayda veya dikeyde patlayacak. çift rokette getirelim. Çift roket 2 satır veya sütunu yok edecek. roketler veya altın rulolar. Tuzaklı bombaları imha edecekler"): çift roket iki satırı (kendi satırı ve altındaki; en altta üstündeki) temizler. 7'lik gruptan ya da
        /// bir roket komşu rokete sürüklenince doğar.</summary>
        RocketRow2 = 6,
        /// <summary>Çift roket, iki sütun (kendi sütunu ve sağındaki; en sağda solundaki).</summary>
        RocketColumn2 = 7,
    }

    public static class SpecialKindExt
    {
        public static bool IsRocket(this SpecialKind k) => k == SpecialKind.RocketRow || k == SpecialKind.RocketColumn || k == SpecialKind.RocketRow2 || k == SpecialKind.RocketColumn2;
        /// <summary>Çift roketin tek karşılığı (görsel ve yön için); diğerleri aynen.</summary>
        public static SpecialKind Single(this SpecialKind k) => k == SpecialKind.RocketRow2 ? SpecialKind.RocketRow : k == SpecialKind.RocketColumn2 ? SpecialKind.RocketColumn : k;
        public static bool IsDoubleRocket(this SpecialKind k) => k == SpecialKind.RocketRow2 || k == SpecialKind.RocketColumn2;
        /// <summary>Satır (yatay) roketi mi (tek ya da çift).</summary>
        public static bool IsRowRocket(this SpecialKind k) => k == SpecialKind.RocketRow || k == SpecialKind.RocketRow2;
        /// <summary>Roketin çift karşılığı (aynı yön).</summary>
        public static SpecialKind Doubled(this SpecialKind k) => k.IsRowRocket() ? SpecialKind.RocketRow2 : SpecialKind.RocketColumn2;
    }
}
