namespace KatKat.Core.Live
{
    /// <summary>
    /// Rövanşlı maç (kullanıcı 2026-11-14: "rövanşta el değişsin"). Aynı dizilimde iki el oynanır; rövanşta iki oyuncunun
    /// elleri (A/B taş seti) ve ilk oynayan yer değiştirir. Böylece iki oyuncu da her iki eli ve her iki sırayı birer kez
    /// oynar; el ya da sıra avantajı kendini götürür. Kazanan iki elin toplam puanıyla; eşitlikte iki eldeki en büyük tek
    /// soyma; o da eşitse berabere.
    /// </summary>
    public static class LiveSeries
    {
        /// <summary>İlk elin rövanşı: aynı dizilim, eller ve ilk oynayan değişmiş.</summary>
        public static LiveMatch Rematch(LiveMatch first) =>
            new LiveMatch(first.Layout, first.Rules, 1 - first.FirstPlayer, !first.SwappedHands);

        public static int Total(LiveMatch a, LiveMatch b, int player) =>
            a.Scores[player] + (b != null ? b.Scores[player] : 0);

        public static int Best(LiveMatch a, LiveMatch b, int player) =>
            System.Math.Max(a.BestMove[player], b != null ? b.BestMove[player] : 0);

        /// <summary>-1 berabere, yoksa kazanan oyuncu (iki el bittiğinde anlamlı).</summary>
        public static int Winner(LiveMatch a, LiveMatch b)
        {
            int t0 = Total(a, b, 0), t1 = Total(a, b, 1);
            if (t0 != t1) return t0 > t1 ? 0 : 1;
            int b0 = Best(a, b, 0), b1 = Best(a, b, 1);
            if (b0 != b1) return b0 > b1 ? 0 : 1;
            return -1;
        }

        /// <summary>Eşit toplamda kazananı belirleyen gerekçe (ekran); toplam farklıysa ya da tam beraberlikte null.</summary>
        public static bool DecidedByBestGroup(LiveMatch a, LiveMatch b) =>
            Total(a, b, 0) == Total(a, b, 1) && Best(a, b, 0) != Best(a, b, 1);
    }
}
