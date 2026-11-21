namespace KatKat.Core
{
    /// <summary>
    /// v14 (kullanıcı 2026-11-16: zincir tamamen kaldırılsın): övgü, maskot coşkusu, zincir sesi ve "MUHTEŞEM" kutlaması eskiden
    /// zincir dalgalarına bağlıydı. Artık oyuncunun kendi büyük hamlesi ölçülür: 6+ taşlık dokunuş grubu ya da toplamda 8+ katman
    /// soyan özel taş hamlesi. Seviye 0 = sıradan hamle, 1–4 = büyüyen övgü.
    /// </summary>
    public static class BigMove
    {
        public const int TapGroup = 6;
        public const int SpecialLayers = 8;

        /// <summary>Dokunulan grubun övgü seviyesi: 6–7 → 1, 8–9 → 2, 10–12 → 3, 13+ → 4.</summary>
        public static int TapLevel(MoveResult r)
        {
            if (r == null || !r.Accepted || (r.Command.Type != CommandType.Tap && r.Command.Type != CommandType.Swap) || r.Waves.Count == 0 || r.Waves[0].Kind != WaveKind.Tap) return 0;
            int n = r.TappedGroup.Count;
            return n >= 13 ? 4 : n >= 10 ? 3 : n >= 8 ? 2 : n >= TapGroup ? 1 : 0;
        }

        /// <summary>Özel taş dalgalarında soyulan katman: 8–11 → 1, 12–17 → 2, 18–25 → 3, 26+ → 4.</summary>
        public static int SpecialLevel(MoveResult r)
        {
            if (r == null || !r.Accepted) return 0;
            int n = 0;
            foreach (var w in r.Waves) if (w.Kind == WaveKind.Special) n += w.Cells.Count;
            return n >= 26 ? 4 : n >= 18 ? 3 : n >= 12 ? 2 : n >= SpecialLayers ? 1 : 0;
        }

        /// <summary>Hamlenin övgü seviyesi (0–4).</summary>
        public static int Level(MoveResult r)
        {
            int a = TapLevel(r), b = SpecialLevel(r);
            return a > b ? a : b;
        }

        /// <summary>Övgünün gösterileceği dalga: büyük dokunuşta dokunuş dalgası, büyük özel taş hamlesinde son dalga.</summary>
        public static bool IsMomentWave(MoveResult r, PeelWave wave)
        {
            if (r == null || wave == null) return false;
            if (wave.Kind == WaveKind.Tap && TapLevel(r) > 0) return true;
            return wave.Kind == WaveKind.Special && r.Waves.Count > 0 && r.Waves[r.Waves.Count - 1] == wave && SpecialLevel(r) > 0;
        }
    }
}
