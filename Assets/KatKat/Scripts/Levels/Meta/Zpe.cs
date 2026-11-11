using System;
using System.Collections.Generic;
using System.Globalization;
using KatKat.Levels;

namespace KatKat.Meta
{
    /// <summary>
    /// v10 P5: ZPE — Zekâ Performans Endeksi. TriKat'a özgü OYUN performans endeksidir; gerçek IQ ölçümü, tanı ya da
    /// bilimsel zekâ testi DEĞİLDİR ve öyle sunulmaz (ana plan §7).
    ///
    /// Bölüm başına beş bileşen (0–1), ağırlıklar yapılandırmada (Resources/Zpe/zpe_config.json, sürümlü):
    /// hamle verimi %35, zorluğa göre başarı %25, ilk denemede başarı %20, çözüm süresi %10, yardımsız bitirme %10.
    /// Referans: bölümün doğrulanmış kayıtlı çözüm uzunluğu ve plansız botun kazanma oranı (LevelLab zperef;
    /// uydurma "en iyi hamle" yok). v11 puanı: oynanan BÜTÜN normal bölümlerin düz ortalaması × 100 (0–100); toplam ve
    /// oyun sayısı kayıtta birikir (<see cref="MetaSave.zpeSum"/>, <see cref="MetaSave.zpeGames"/>). Ham girdiler son
    /// <see cref="Zpe.HistoryCap"/> bölüm için saklanır (bileşen dökümü ve eğilim).
    /// Yardımlar (günlük +1 hamle, taş gücü, reklam hamlesi): ek hamleyle geçilen bölümde verim 0, yardımsız bileşeni
    /// düşük; böylece sıralama avantajı yaratmaz. Ham girdiler kayıtta saklanır (algoritma değişince yeniden hesap).
    /// </summary>
    [Serializable]
    public sealed class ZpeRecord
    {
        public string levelId = "";
        public bool success;
        public bool firstAttempt;
        public bool assisted;
        public int movesUsed;
        public int moveLimit;
        public int moveBonus;
        /// <summary>Çözüm süresi (sn). <see cref="timeVer"/> 2: yalnız karar süresi; 0: eski kayıt (animasyon dahil).</summary>
        public float seconds;
        public int algo;
        /// <summary>Süre ölçüm sürümü (<see cref="SessionOutcome.TimeMeasureVersion"/>); eski kayıtta 0. Eski kayıtların
        /// süresi geriye dönük düzeltilemez (ayrılmış süre eskide yok).</summary>
        public int timeVer;
        /// <summary>Bilgi (ZPE hesabına girmez): animasyon ve zorunlu öğretici süresi.</summary>
        public float animSeconds, tutorialSeconds;
        /// <summary>UTC unix saniyesi.</summary>
        public long time;

        /// <summary>Oturum sonucundan ZPE kaydı: hız bileşeni karar süresini kullanır.</summary>
        public static ZpeRecord From(SessionOutcome o, long time) => new ZpeRecord
        {
            levelId = o.LevelId,
            success = o.Success,
            firstAttempt = o.FirstAttempt,
            assisted = o.Assisted,
            movesUsed = o.MovesUsed,
            moveLimit = o.MoveLimit,
            moveBonus = o.MoveBonus,
            seconds = (float)o.DecisionSeconds,
            timeVer = SessionOutcome.TimeMeasureVersion,
            animSeconds = (float)o.AnimationSeconds,
            tutorialSeconds = (float)o.TutorialSeconds,
            time = time,
        };
    }

    public sealed class ZpeLevelRef
    {
        public string Id;
        public int Steps, Limit;
        public double Bot;
    }

    public sealed class ZpeConfig
    {
        public int Version = 1;
        public double WEfficiency = 0.35, WDifficulty = 0.25, WFirstTry = 0.20, WTime = 0.10, WNoAssist = 0.10;
        /// <summary>Bileşen dökümü penceresi (son N kayıt). v11 puanı ise bütün oyunların düz ortalamasıdır.</summary>
        public int Window = 50, MinLevels = 1;
        /// <summary>v3 (vr1 02_ZPE deney B): puan son <see cref="Window"/> farklı bölümden (bölüm başına penceredeki en iyi sonuç).
        /// false: v2 (oynanan bütün oyunların düz ortalaması).</summary>
        public bool Distinct;
        public double Decay = 1.0;
        public int Scale = 100;
        /// <summary>Beklenen süre = TimeBase + TimePerStep × çözüm adımı (sn); bu sürede ya da daha hızlı = 1.</summary>
        public double TimeBase = 6, TimePerStep = 4;

        public static ZpeConfig Parse(string json)
        {
            var c = new ZpeConfig();
            if (string.IsNullOrEmpty(json)) return c;
            if (!(MiniJson.Parse(json) is Dictionary<string, object> o)) throw new FormatException("zpe_config: kök nesne olmalı");
            double D(string k, double d) => o.TryGetValue(k, out var v) && v != null ? Convert.ToDouble(v, CultureInfo.InvariantCulture) : d;
            c.Version = (int)D("version", 1);
            if (o.TryGetValue("weights", out var w) && w is Dictionary<string, object> wo)
            {
                double W(string k, double d) => wo.TryGetValue(k, out var v) && v != null ? Convert.ToDouble(v, CultureInfo.InvariantCulture) : d;
                c.WEfficiency = W("efficiency", c.WEfficiency);
                c.WDifficulty = W("difficulty", c.WDifficulty);
                c.WFirstTry = W("firstTry", c.WFirstTry);
                c.WTime = W("time", c.WTime);
                c.WNoAssist = W("noAssist", c.WNoAssist);
            }
            c.Window = (int)D("window", c.Window);
            c.Distinct = o.TryGetValue("distinct", out var dv) && dv is bool db && db;
            c.MinLevels = (int)D("minLevels", c.MinLevels);
            c.Decay = D("decay", c.Decay);
            c.Scale = (int)D("scale", c.Scale);
            c.TimeBase = D("timeBase", c.TimeBase);
            c.TimePerStep = D("timePerStep", c.TimePerStep);
            double sum = c.WEfficiency + c.WDifficulty + c.WFirstTry + c.WTime + c.WNoAssist;
            if (sum <= 0 || c.WTime < 0) throw new FormatException("zpe_config: ağırlık toplamı 0");
            return c;
        }
    }

    /// <summary>Bir bölümün bileşenleri (0–1) ve ağırlıklı toplamı.</summary>
    public struct ZpeParts
    {
        public double Efficiency, Difficulty, FirstTry, Time, NoAssist, Total;
    }

    public sealed class ZpeResult
    {
        public int Count;
        public bool Calibrating;
        public int MinLevels;
        /// <summary>0..Scale (kalibrasyonda da hesaplanır ama gösterilmez).</summary>
        public int Score;
        /// <summary>Son 10 bölüm ile önceki 10 bölümün puan farkı (ölçekli); veri azsa 0.</summary>
        public int Trend;
        public ZpeParts Average;
        public double WinRate, FirstTryRate;
        public int Algo;
        /// <summary>v3: v2 yöntemiyle (bütün oyunların ortalaması) dondurulmuş eski puan; yoksa -1. Yeni puana karışmaz.</summary>
        public int LegacyScore = -1;
    }

    public static class Zpe
    {
        /// <summary>2: v3 yapılandırması (hızsız, son 50 farklı bölüm). Kayıtlar ham girdi tuttuğu için yeniden hesaplanır.</summary>
        public const int AlgoVersion = 2;
        /// <summary>Ham geçmiş (son N kayıt): 50 farklı bölüm için tekrarlar dahil yeterli pay (eski 60).</summary>
        public const int HistoryCap = 250;

        public static Dictionary<string, ZpeLevelRef> ParseRefs(string json)
        {
            var map = new Dictionary<string, ZpeLevelRef>();
            if (string.IsNullOrEmpty(json)) return map;
            if (!(MiniJson.Parse(json) is Dictionary<string, object> root) || !(root.TryGetValue("levels", out var l) && l is List<object> arr))
                throw new FormatException("level_ref: \"levels\" yok");
            foreach (var it in arr)
                if (it is Dictionary<string, object> o && o.TryGetValue("id", out var id) && id is string s)
                    map[s] = new ZpeLevelRef
                    {
                        Id = s,
                        Steps = Convert.ToInt32(o["steps"], CultureInfo.InvariantCulture),
                        Limit = Convert.ToInt32(o["limit"], CultureInfo.InvariantCulture),
                        Bot = Convert.ToDouble(o["bot"], CultureInfo.InvariantCulture),
                    };
            return map;
        }

        private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;

        /// <summary>Tek bölümün bileşenleri. Referans yoksa orta değerler (bot %50, çözüm = sınırın yarısı).</summary>
        public static ZpeParts Score(ZpeRecord r, ZpeLevelRef rf, ZpeConfig c)
        {
            var p = new ZpeParts();
            if (r == null || !r.success) return p; // başarısız: bütün bileşenler 0
            int baseLimit = Math.Max(1, r.moveLimit - r.moveBonus);
            int steps = rf != null && rf.Steps > 0 ? rf.Steps : Math.Max(1, baseLimit / 2);
            double bot = rf != null ? rf.Bot : 0.5;
            // Hamle verimi: referans çözüm uzunluğu ya da daha az = 1; temel sınırda = 0; ek hamleyle (sınır aşımı) = 0.
            if (r.movesUsed <= steps) p.Efficiency = 1;
            else if (r.movesUsed > baseLimit) p.Efficiency = 0;
            else p.Efficiency = Clamp01(1.0 - (r.movesUsed - steps) / (double)Math.Max(1, baseLimit - steps + 1));
            // Zorluğa göre başarı: zor bölüm (botun az kazandığı) daha çok puan.
            p.Difficulty = Clamp01(0.35 + 0.65 * (1.0 - bot));
            p.FirstTry = r.firstAttempt ? 1 : 0.4;
            double expected = c.TimeBase + c.TimePerStep * steps;
            p.Time = r.seconds > 0.5 ? Clamp01(expected / r.seconds) : 0.5;
            p.NoAssist = r.assisted ? 0.3 : 1;
            double wsum = c.WEfficiency + c.WDifficulty + c.WFirstTry + c.WTime + c.WNoAssist;
            p.Total = (p.Efficiency * c.WEfficiency + p.Difficulty * c.WDifficulty + p.FirstTry * c.WFirstTry + p.Time * c.WTime + p.NoAssist * c.WNoAssist) / wsum;
            return p;
        }

        /// <summary>Geçmişten endeks (son Window kayıt, yeniden eskiye Decay^yaş ağırlık).</summary>
        public static ZpeResult Compute(IList<ZpeRecord> history, Func<string, ZpeLevelRef> refOf, ZpeConfig c)
        {
            var res = new ZpeResult { MinLevels = c.MinLevels, Algo = AlgoVersion };
            if (history == null || history.Count == 0) { res.Calibrating = true; return res; }
            int n = Math.Min(c.Window, history.Count);
            double ws = 0, acc = 0, wins = 0, first = 0;
            var avg = new ZpeParts();
            var totals = new double[n];
            for (int k = 0; k < n; k++)
            {
                var r = history[history.Count - 1 - k]; // k = yaş (0 en yeni)
                var p = Score(r, refOf?.Invoke(r.levelId), c);
                totals[k] = p.Total;
                double w = Math.Pow(c.Decay, k);
                ws += w;
                acc += w * p.Total;
                avg.Efficiency += w * p.Efficiency; avg.Difficulty += w * p.Difficulty; avg.FirstTry += w * p.FirstTry;
                avg.Time += w * p.Time; avg.NoAssist += w * p.NoAssist;
                if (r.success) wins++;
                if (r.success && r.firstAttempt) first++;
            }
            res.Count = history.Count;
            res.Calibrating = history.Count < c.MinLevels;
            res.Score = (int)Math.Round(acc / ws * c.Scale);
            avg.Efficiency /= ws; avg.Difficulty /= ws; avg.FirstTry /= ws; avg.Time /= ws; avg.NoAssist /= ws;
            avg.Total = acc / ws;
            res.Average = avg;
            res.WinRate = wins / n;
            res.FirstTryRate = first / n;
            if (n >= 20)
            {
                double a = 0, b = 0;
                for (int k = 0; k < 10; k++) { a += totals[k]; b += totals[k + 10]; }
                res.Trend = (int)Math.Round((a - b) / 10.0 * c.Scale);
            }
            return res;
        }

        /// <summary>
        /// Kayda ekler (ham girdi en çok <see cref="HistoryCap"/>; en eskiler düşer) ve bölüm puanını oynanan bütün
        /// oyunların toplamına katar (yapılandırma verilmezse varsayılan).
        /// </summary>
        public static void Add(MetaSave meta, ZpeRecord r, Func<string, ZpeLevelRef> refOf = null, ZpeConfig c = null)
        {
            if (meta == null || r == null) return;
            c ??= new ZpeConfig();
            meta.zpeHistory ??= new List<ZpeRecord>();
            // v3: eski toplam (zpeSum/zpeGames) dondurulur; yeni bileşenlerle karıştırılmaz (02_ZPE "Kayıt geçişi").
            if (!c.Distinct) Seed(meta, refOf, c);
            r.algo = AlgoVersion;
            meta.zpeHistory.Add(r);
            if (!c.Distinct)
            {
                meta.zpeSum += Score(r, refOf?.Invoke(r.levelId), c).Total;
                meta.zpeGames++;
            }
            while (meta.zpeHistory.Count > HistoryCap) meta.zpeHistory.RemoveAt(0);
        }

        /// <summary>
        /// v12: kayda geçmiş bir sonucu geri çeker (kayıp kartından geri alma ya da reklamla ek hamle: oturum sürer,
        /// sonucu bitince yeniden yazılır; çift sayım yok).
        /// </summary>
        public static void Retract(MetaSave meta, ZpeRecord r, Func<string, ZpeLevelRef> refOf = null, ZpeConfig c = null)
        {
            if (meta == null || r == null) return;
            c ??= new ZpeConfig();
            meta.zpeHistory?.Remove(r);
            if (c.Distinct || meta.zpeGames <= 0) return; // v3: eski toplam donuk
            meta.zpeSum = Math.Max(0, meta.zpeSum - Score(r, refOf?.Invoke(r.levelId), c).Total);
            meta.zpeGames--;
            if (meta.zpeGames == 0) meta.zpeSum = 0;
        }

        /// <summary>Eski kayıt (toplamı olmayan): toplam eldeki geçmişten kurulur.</summary>
        private static void Seed(MetaSave meta, Func<string, ZpeLevelRef> refOf, ZpeConfig c)
        {
            if (meta.zpeGames >= meta.zpeHistory.Count) return;
            double sum = 0;
            foreach (var h in meta.zpeHistory) sum += Score(h, refOf?.Invoke(h.levelId), c).Total;
            meta.zpeSum = sum;
            meta.zpeGames = meta.zpeHistory.Count;
        }

        /// <summary>v11 ZPE: oynanan bütün oyunların düz ortalaması (0–Scale); döküm ve eğilim son kayıtlardan.</summary>
        public static ZpeResult Compute(MetaSave meta, Func<string, ZpeLevelRef> refOf, ZpeConfig c)
        {
            if (meta == null) return Compute((IList<ZpeRecord>)null, refOf, c);
            meta.zpeHistory ??= new List<ZpeRecord>();
            if (c.Distinct)
            {
                // v3: en yeniden geriye son Window farklı bölüm; her bölüm için geçmişteki en iyi sonucu.
                var ids = new List<string>();
                for (int i = meta.zpeHistory.Count - 1; i >= 0 && ids.Count < c.Window; i--)
                    if (!ids.Contains(meta.zpeHistory[i].levelId)) ids.Add(meta.zpeHistory[i].levelId);
                var chosen = new List<ZpeRecord>();
                for (int k = ids.Count - 1; k >= 0; k--) // eskiden yeniye (Compute son kayıtları en yeni sayar)
                {
                    ZpeRecord best = null;
                    double bt = -1;
                    foreach (var r in meta.zpeHistory)
                    {
                        if (r.levelId != ids[k]) continue;
                        double t = Score(r, refOf?.Invoke(r.levelId), c).Total;
                        if (t > bt) { bt = t; best = r; }
                    }
                    if (best != null) chosen.Add(best);
                }
                var v3 = Compute(chosen, refOf, c);
                v3.Count = chosen.Count;
                v3.Calibrating = chosen.Count < c.MinLevels;
                v3.LegacyScore = meta.zpeGames > 0 ? (int)Math.Round(meta.zpeSum / meta.zpeGames * c.Scale) : -1;
                return v3;
            }
            Seed(meta, refOf, c);
            var res = Compute(meta.zpeHistory, refOf, c);
            if (meta.zpeGames > 0)
            {
                res.Count = meta.zpeGames;
                res.Calibrating = meta.zpeGames < c.MinLevels;
                res.Score = (int)Math.Round(meta.zpeSum / meta.zpeGames * c.Scale);
            }
            return res;
        }
    }
}
