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

        // ---- Çok raunt (kullanıcı 2026-11-25: "3 raunta çıkartalım") ----------------------------------------------

        public static int Total(System.Collections.Generic.IList<LiveMatch> rounds, int player)
        {
            int t = 0;
            foreach (var r in rounds) t += r.Scores[player];
            return t;
        }

        public static int Best(System.Collections.Generic.IList<LiveMatch> rounds, int player)
        {
            int b = 0;
            foreach (var r in rounds) b = System.Math.Max(b, r.BestMove[player]);
            return b;
        }

        /// <summary>-1 berabere, yoksa kazanan: rauntların toplamı; eşitse en büyük tek grup.</summary>
        public static int Winner(System.Collections.Generic.IList<LiveMatch> rounds)
        {
            int t0 = Total(rounds, 0), t1 = Total(rounds, 1);
            if (t0 != t1) return t0 > t1 ? 0 : 1;
            int b0 = Best(rounds, 0), b1 = Best(rounds, 1);
            if (b0 != b1) return b0 > b1 ? 0 : 1;
            return -1;
        }

        public static bool DecidedByBestGroup(System.Collections.Generic.IList<LiveMatch> rounds) =>
            Total(rounds, 0) == Total(rounds, 1) && Best(rounds, 0) != Best(rounds, 1);

        /// <summary>
        /// Son raunt: havuzdaki başka dizilim, onun denk iki eli (<see cref="LiveLayout.Hand2"/>); toplamda geride olan başlar,
        /// eşitse ilk rauntta ikinci oynayan.
        /// </summary>
        public static LiveMatch Decider(LiveLayout layout, LiveRules rules, System.Collections.Generic.IList<LiveMatch> done)
        {
            int t0 = Total(done, 0), t1 = Total(done, 1);
            int first = t0 < t1 ? 0 : t1 < t0 ? 1 : 1 - done[0].FirstPlayer;
            return new LiveMatch(layout, rules, first, first == 1);
        }

        /// <summary>Eşit toplamda kazananı belirleyen gerekçe (ekran); toplam farklıysa ya da tam beraberlikte null.</summary>
        public static bool DecidedByBestGroup(LiveMatch a, LiveMatch b) =>
            Total(a, b, 0) == Total(a, b, 1) && Best(a, b, 0) != Best(a, b, 1);
    }
}
