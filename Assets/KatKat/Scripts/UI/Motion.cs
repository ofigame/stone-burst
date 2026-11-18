namespace KatKat.UI
{
    /// <summary>
    /// Azaltılmış hareket (vr1 04 "Reduced motion"; Ayarlar'da anahtar, kayıtta <c>SaveData.reducedMotion</c>): tahta
    /// sarsıntısı, sürekli büyüyüp küçülme ve buz kırılmasının titreme/parçacıkları kapanır; kazanım 180 ms geçişle
    /// gösterilir. Oyun kuralı, ödül ve süreler (sonuç) değişmez.
    /// </summary>
    public static class Motion
    {
        public static bool Reduced;

        /// <summary>
        /// Tahta animasyonlarının temposu (kullanıcı 2026-11-18: "animasyonlar güzel oldu fakat çok hızlı olup bitiyor
        /// … ağır çekim gibi olmayacak doğal akışı gibi görünecek"; "animasyonların tamamını doğal yavaşlat").
        /// Olay planı (dalgalar, soyulma, özel taşlar, yıldırım, sarsıntı) bu oranla ilerler; 0,72 → her şey ~%39 uzun.
        /// Parçacıklar (kıvılcım, alev, duman) gerçek zamanla akar: fizikleri ağır çekim gibi görünmez, yalnız daha
        /// uzun bir gösteriye yayılır. 1 = eski hız.
        /// </summary>
        public static float Pace = 0.72f;
    }
}
