using System;
using System.Collections.Generic;
using KatKat.Core;

namespace KatKat.Levels
{
    /// <summary>
    /// Statik doğrulama (belge Bölüm 14) ve kayıtlı çözüm tekrarı (Bölüm 15). Statik kontrol gerekli fakat
    /// çözüm garantisi için yeterli değildir; çözüm tekrarı gerçek RulesEngine ile oynanır ve yalnızca
    /// çözüm varlığını kanıtlar.
    /// </summary>
    public static class LevelValidator
    {
        /// <summary>İlk sürüm sınırları: 4x4/5x5, en çok 3 katman, ataş/yıldız yok.</summary>
        public static readonly ValidationScope FirstRelease = new ValidationScope { AllowedSizes = new[] { 4, 5 }, MaxLayers = 3, AllowClips = false, AllowStars = false };

        /// <summary>İkinci aşama (Faz 5, bölüm 21+): en çok 6x6, 4 katman, ataş ve yıldız.</summary>
        public static readonly ValidationScope SecondStage = new ValidationScope { AllowedSizes = new[] { 4, 5, 6 }, MaxLayers = 4, AllowClips = true, AllowStars = true };

        /// <summary>schemaVersion 2'de geçerli yetenek adları (Premium-v2 rehberi §12).</summary>
        public static readonly string[] KnownCapabilities = { "clip", "star", "ice", "colorLock", "envelope", "rotor", "specials", "sponge", "linkedClip", "gate", "paint", "stages", "seal", "areas", "gravel", "buckets", "refill", "water" }; // vr1 P3: stages/seal/areas yalnız pilot katalog (v14: seal tanınır ama reddedilir)

        /// <summary>
        /// schemaVersion 1: belge Bölüm 15 — ilk 20 bölümde ataş, yıldız, 6x6 ve dört katman yok (kimlik "level_###").
        /// schemaVersion 2: kapsam bölüm numarasından değil, verideki yetenek listesinden.
        /// </summary>
        public static ValidationScope ScopeFor(LevelData d)
        {
            if (d.SchemaVersion >= 2)
                return new ValidationScope
                {
                    AllowedSizes = new[] { 4, 5, 6 }, MaxLayers = 4,
                    AllowClips = d.Capabilities.Contains("clip"), AllowStars = d.Capabilities.Contains("star"),
                    AllowIce = d.Capabilities.Contains("ice"), AllowColorLock = d.Capabilities.Contains("colorLock"),
                    AllowEnvelope = d.Capabilities.Contains("envelope"), AllowRotor = d.Capabilities.Contains("rotor"),
                };
            return LevelNumber(d.Id) > 20 ? SecondStage : FirstRelease;
        }

        public static int LevelNumber(string id)
        {
            if (id == null) return 0;
            int u = id.LastIndexOf('_');
            return u >= 0 && int.TryParse(id.Substring(u + 1), out var n) ? n : 0;
        }

        public static List<string> ValidateStatic(LevelData d, ValidationScope scope)
        {
            var e = new List<string>();
            string id = string.IsNullOrEmpty(d.Id) ? "?" : d.Id;
            if (d.SchemaVersion != 1 && d.SchemaVersion != 2) e.Add($"{id}: schemaVersion 1 veya 2 olmalı ({d.SchemaVersion})");
            if (d.SchemaVersion == 1 && d.Capabilities.Count > 0) e.Add($"{id}: capabilities için schemaVersion 2 gerekli");
            foreach (var cap in d.Capabilities)
                if (Array.IndexOf(KnownCapabilities, cap) < 0) e.Add($"{id}: bilinmeyen yetenek '{cap}'");
            if (string.IsNullOrEmpty(d.Id)) e.Add("id boş");
            // vr1 P3 yenilikleri yalnız yetenekle (özellik anahtarı) ve tutarlı veriyle.
            int maxStage = 0;
            foreach (var g in d.Goals) { if (g.Stage < 0) e.Add($"{id}: hedef aşaması negatif"); if (g.Stage > maxStage) maxStage = g.Stage; }
            if (maxStage > 0 && !d.Capabilities.Contains("stages")) e.Add($"{id}: aşamalı hedef için stages yeteneği gerekli");
            for (int st = 0; st <= maxStage; st++) if (!d.Goals.Exists(g => g.Stage == st)) e.Add($"{id}: aşama {st} için hedef yok");
            // v14 (kullanıcı 2026-11-16: zincir tamamen kaldırılsın): zincire bağlı veri artık desteklenmez; eski kayıt yakalanır.
            if (d.SealWave != 0 || d.Capabilities.Contains("seal")) e.Add($"{id}: zincir mührü (seal) v14'te kaldırıldı, desteklenmiyor");
            if (d.Rules.Contains("chainOnly")) e.Add($"{id}: chainOnly kuralı v14'te kaldırıldı, desteklenmiyor");
            if (d.TrialType == "chain" || d.TrialType == "aha") e.Add($"{id}: '{d.TrialType}' türü zincire bağlıydı, v14'te desteklenmiyor");
            if (d.Cells.Exists(c => c.Area != 0) && !d.Capabilities.Contains("areas")) e.Add($"{id}: iki alan için areas yeteneği gerekli");
            if (d.HasRefill)
            {
                if (!d.Capabilities.Contains("refill")) e.Add($"{id}: yukarıdan dolma için refill yeteneği gerekli");
                if (d.RefillCount < 0 || d.RefillMinLayers < 1 || d.RefillMaxLayers < d.RefillMinLayers || d.RefillMaxLayers > 4) e.Add($"{id}: refill değerleri geçersiz");
            }
            // 2026-11-23 kova kipi: süreli; her renk hedefi o rengin kova toplamına eşit.
            if (d.Buckets.Count > 0)
            {
                if (!d.Capabilities.Contains("buckets")) e.Add($"{id}: kova için buckets yeteneği gerekli");
                if (d.TimeLimit <= 0) e.Add($"{id}: kovalı bölüm süreli olmalı");
                var sums = new Dictionary<string, int>();
                foreach (var kv in d.Buckets)
                {
                    if (kv.Value <= 0) e.Add($"{id}: kova miktarı pozitif olmalı");
                    sums[kv.Key] = (sums.TryGetValue(kv.Key, out var v) ? v : 0) + kv.Value;
                }
                foreach (var g in d.Goals)
                    if (g.Type != "collectColor" || !sums.TryGetValue(g.Color, out var want) || want != g.Amount) e.Add($"{id}: kovalı bölümde hedef yalnız kova renkleri ve toplamları olmalı ({g.Type} {g.Color} {g.Amount})");
                if (d.Goals.Count != sums.Count) e.Add($"{id}: her kova rengi için bir hedef olmalı");
            }
            if (d.Cells.Exists(c => c.Area < 0 || c.Area > 1)) e.Add($"{id}: alan 0 ya da 1 olmalı");
            // v5 (özel taşlı bölümler): dikdörtgen tahta 5–8 genişlik, 5–9 yükseklik (taşlar küçülür, tahta alanı aynı).
            bool v5Board = (d.SpecialsEnabled || !string.IsNullOrEmpty(d.TrialType)) && d.Width >= 5 && d.Width <= 8 && d.Height >= 5 && d.Height <= 9; // v12 deneme: dikdörtgen
            if (!v5Board && (d.Width != d.Height || Array.IndexOf(scope.AllowedSizes, d.Width) < 0))
                e.Add($"{id}: boyut {d.Width}x{d.Height} kapsam dışında");
            if (d.MoveLimit <= 0) e.Add($"{id}: moveLimit pozitif olmalı");
            if (d.StartingRoll && !d.RollEnabled) e.Add($"{id}: startingRoll için rollEnabled gerekli");

            var seen = new HashSet<long>();
            var colorTotals = new int[LayerColors.Count];
            int stars = 0, envelopes = 0, water = 0;
            foreach (var c in d.Cells)
            {
                string at = $"{id}: ({c.X},{c.Y})";
                if (c.X < 0 || c.Y < 0 || c.X >= d.Width || c.Y >= d.Height) e.Add($"{at} tahta dışında");
                if (!seen.Add(((long)c.X << 32) | (uint)c.Y)) e.Add($"{at} iki kez yazılmış");
                if (!c.Active)
                {
                    if (c.Layers.Count > 0 || c.CoverCount > 0 || c.HasBottomStar || c.Water != 0) e.Add($"{at} inactive hücre boş, örtüsüz, yıldızsız ve susuz olmalı");
                    // M11: geçit hücresinin potansiyel katmanları hedef üst sınırına sayılır (rehber §12).
                    if (c.GateId > 0)
                    {
                        if (c.GateLayers.Count == 0) e.Add($"{at} geçit hücresi boş olamaz");
                        if (!d.Cells.Exists(b => b.ButtonGate == c.GateId)) e.Add($"{at} geçit {c.GateId} için düğme yok");
                        foreach (var l in c.GateLayers)
                            if (ColorIds.TryParse(l, out var gc)) colorTotals[(int)gc]++;
                    }
                    continue;
                }
                if (c.ButtonGate > 0 && c.Layers.Count == 0) e.Add($"{at} düğmeli yığın boş olamaz");
                if (c.LinkHits < 0 || c.LinkHits > 2 || (c.LinkHits > 0 && c.LinkId <= 0)) e.Add($"{at} bağlı ataş 1-2 vuruş ve linkId ister");
                if (c.LinkHits > 0 && d.Cells.Exists(o => o != c && o.LinkId == c.LinkId && o.LinkHits != c.LinkHits))
                    e.Add($"{at} bağlı ataş grubunda sayaçlar farklı");
                if ((c.SpongeColors.Count > 0 || c.LinkHits > 0) && c.Layers.Count == 0) e.Add($"{at} boş hücrede sünger/bağlı ataş olamaz");
                if (c.CoverCount > 1) e.Add($"{at} hücrede birden fazla örtü (ataş/buz/kilit/zarf)");
                if (c.HasRotor)
                {
                    if (d.SchemaVersion < 2) e.Add($"{at} döner taban için schemaVersion 2 gerekli");
                    if (c.HasBottomStar) e.Add($"{at} hücrede birden fazla taban etkisi (yıldız + döner taban)");
                    if (c.Layers.Count < 2) e.Add($"{at} döner taban en az iki katman ister");
                    if (!scope.AllowRotor) e.Add($"{at} döner taban bu bölümün kapsamı dışında");
                }
                if ((c.IceHits != 0 || !string.IsNullOrEmpty(c.LockColor) || c.LockRemaining != 0 || !string.IsNullOrEmpty(c.EnvelopeColor)) && d.SchemaVersion < 2)
                    e.Add($"{at} buz/renk kilidi/zarf için schemaVersion 2 gerekli");
                if (!string.IsNullOrEmpty(c.EnvelopeColor))
                {
                    envelopes++;
                    if (!ColorIds.TryParse(c.EnvelopeColor, out _)) e.Add($"{at} zarfta bilinmeyen mühür rengi '{c.EnvelopeColor}'");
                    if (c.Layers.Count == 0) e.Add($"{at} boş hücrede zarf olamaz");
                    if (!scope.AllowEnvelope) e.Add($"{at} zarf bu bölümün kapsamı dışında");
                }
                if (c.IceHits < 0 || c.IceHits > 2) e.Add($"{at} buz 0-2 olmalı");
                if (c.IceHits > 0 && c.Layers.Count == 0) e.Add($"{at} boş hücrede buz olamaz");
                if (c.IceHits > 0 && !scope.AllowIce) e.Add($"{at} buz bu bölümün kapsamı dışında");
                if (c.Gravel && c.Layers.Count == 0) e.Add($"{at} boş hücrede çakıl olamaz");
                if (c.Gravel && !d.Capabilities.Contains("gravel")) e.Add($"{at} çakıl için gravel yeteneği gerekli");
                if (!string.IsNullOrEmpty(c.LockColor) || c.LockRemaining != 0)
                {
                    if (!ColorIds.TryParse(c.LockColor, out _)) e.Add($"{at} kilitte bilinmeyen renk '{c.LockColor}'");
                    if (c.LockRemaining < 1 || c.LockRemaining > 30) e.Add($"{at} kilit sayacı 1-30 olmalı");
                    if (c.Layers.Count == 0) e.Add($"{at} boş hücrede kilit olamaz");
                    if (!scope.AllowColorLock) e.Add($"{at} renk kilidi bu bölümün kapsamı dışında");
                }
                if (c.Layers.Count > scope.MaxLayers) e.Add($"{at} {c.Layers.Count} katman; sınır {scope.MaxLayers}");
                foreach (var l in c.Layers)
                {
                    if (ColorIds.TryParse(l, out var col)) colorTotals[(int)col]++;
                    else e.Add($"{at} bilinmeyen renk '{l}'");
                }
                if (c.ClipHitsRemaining < 0 || c.ClipHitsRemaining > 2) e.Add($"{at} ataş 0-2 olmalı");
                if (c.ClipHitsRemaining > 0 && c.Layers.Count == 0) e.Add($"{at} boş hücrede ataş olamaz");
                if (c.ClipHitsRemaining > 0 && !scope.AllowClips) e.Add($"{at} ataş bu bölümün kapsamı dışında");
                if (c.Water != 0)
                {
                    water += c.Water;
                    if (c.Water < 0 || c.Water > 3) e.Add($"{at} su 1-3 birim olmalı");
                    if (c.Layers.Count == 0) e.Add($"{at} sulu hücre en az bir katman içermeli");
                    if (!d.Capabilities.Contains("water")) e.Add($"{at} su için water yeteneği gerekli");
                }
                if (c.HasBottomStar)
                {
                    stars++;
                    if (c.Layers.Count == 0) e.Add($"{at} yıldızlı hücre en az bir katman içermeli");
                    if (!scope.AllowStars) e.Add($"{at} yıldız bu bölümün kapsamı dışında");
                }
            }

            // Kilit üst sınırı: kendi hücresi dışındaki o renkteki katmanlar sayacı karşılamalı.
            foreach (var c in d.Cells)
            {
                if (!c.Active || c.LockRemaining <= 0 || !ColorIds.TryParse(c.LockColor, out var lc)) continue;
                int own = 0;
                foreach (var l in c.Layers) if (ColorIds.TryParse(l, out var col) && col == lc) own++;
                if (colorTotals[(int)lc] - own < c.LockRemaining)
                    e.Add($"{id}: ({c.X},{c.Y}) kilit {c.LockRemaining} {c.LockColor} istiyor, tahtanın geri kalanında {colorTotals[(int)lc] - own} var");
            }

            if (d.Goals.Count == 0) e.Add($"{id}: goals boş");
            var goalKeys = new HashSet<string>();
            foreach (var g in d.Goals)
            {
                if (g.Amount <= 0) e.Add($"{id}: hedef miktarı pozitif olmalı ({g.Type} {g.Color})");
                if (!goalKeys.Add(g.Type + ":" + g.Color + ":" + g.Kind + ":" + g.Stage)) e.Add($"{id}: yinelenen hedef {g.Type} {g.Color}{g.Kind}");
                if (g.Type == "collectColor")
                {
                    if (!ColorIds.TryParse(g.Color, out var col)) e.Add($"{id}: hedefte bilinmeyen renk '{g.Color}'");
                    else if (!d.HasRefill && g.Amount > colorTotals[(int)col]) e.Add($"{id}: {g.Color} hedefi {g.Amount}, tahtada {colorTotals[(int)col]} katman var");
                }
                else if (g.Type == "fillWater")
                {
                    if (d.WaterCap < 0 || (d.WaterCap > 0 && d.WaterCap * 3 > g.Amount)) e.Add($"{id}: su kovası kapasitesi {d.WaterCap}, görevin üçte birini aşamaz (en az 3 gönderim)");
                    if (!d.Capabilities.Contains("water")) e.Add($"{id}: su hedefi için water yeteneği gerekli");
                    if (g.Amount > water) e.Add($"{id}: su hedefi {g.Amount}, tahtada {water} birim su var");
                }
                else if (g.Type == "collectChain") e.Add($"{id}: zincir hedefi (collectChain) v14'te kaldırıldı, desteklenmiyor");
                else if (g.Type == "collectStars")
                {
                    if (!scope.AllowStars) e.Add($"{id}: yıldız hedefi bu bölümün kapsamı dışında");
                    if (g.Amount > stars) e.Add($"{id}: yıldız hedefi {g.Amount}, tahtada {stars} yıldız var");
                }
                else if (g.Type == "collectToken")
                {
                    if (d.SchemaVersion < 2) e.Add($"{id}: collectToken için schemaVersion 2 gerekli");
                    if (g.Kind == "sponge") { if (g.Amount > d.Cells.FindAll(c => c.SpongeColors.Count > 0).Count) e.Add($"{id}: sünger hedefi tahtadaki süngerden fazla"); }
                    else if (g.Kind == "balloon") { if (d.Hazard == null || d.Hazard.Kind != "balloon" || g.Amount > d.Hazard.Cells.Count) e.Add($"{id}: balon hedefi tahtadaki balondan fazla"); } // konsept pilotu
                    else if (g.Amount > envelopes) e.Add($"{id}: zarf hedefi {g.Amount}, tahtada {envelopes} zarf var");
                }
                else e.Add($"{id}: bilinmeyen hedef tipi '{g.Type}'");
            }

            foreach (var s in d.Solution)
                if (s.Type != "tap" && s.Type != "roll" && s.Type != "rotate" && s.Type != "paint" && s.Type != "use" && s.Type != "swap" && !(s.Type == "send" && (d.Buckets.Count > 0 || d.WaterCap > 0))) e.Add($"{id}: bilinmeyen çözüm adımı '{s.Type}'");

            if (e.Count == 0)
            {
                // İçerik hatası: başlangıçta yasal hamle yoksa bölüm oynatılamaz.
                var state = LevelLoader.ToState(d);
                if (!RulesEngine.HasLegalMove(state)) e.Add($"{id}: başlangıçta yasal hamle yok");
            }
            return e;
        }

        /// <summary>Kayıtlı çözümü baştan sona gerçek kural motoruyla oynatır.</summary>
        public static ReplayReport ReplaySolution(LevelData d)
        {
            // v6: tepsi kullanımı ("use") hamle harcamaz; adım sayısı yalnız hamle harcayan adımlar.
            var report = new ReplayReport { LevelId = d.Id, MoveLimit = d.MoveLimit, Steps = 0 /* 2026-11-27: yeniden oynatma sonunda harcanan hamle (roket/rulo bedava, Kürek 1 hamle) */ };
            if (d.Solution.Count == 0) { report.Error = "kayıtlı çözüm yok"; return report; }
            var session = new GameSession(LevelLoader.ToState(d));
            for (int i = 0; i < d.Solution.Count; i++)
            {
                var cmd = LevelLoader.ToCommand(d.Solution[i]);
                if (session.State.IsTerminal)
                {
                    report.Error = $"adım {i + 1} ({cmd}) öncesi bölüm bitmiş: {session.State.Status}";
                    break;
                }
                var r = session.Submit(cmd);
                if (!r.Accepted)
                {
                    report.Error = $"adım {i + 1} ({cmd}) geçersiz: {r.Reason}";
                    break;
                }
                report.Waves += r.Waves.Count;
                report.LayersPeeled += r.TotalPeeled;
                if (r.RollAwarded) report.RollsAwarded++;
                if (r.RollConsumed) report.RollsUsed++;
            }
            var st = session.State;
            report.FinalStatus = st.Status;
            report.MovesLeft = st.MovesLeft;
            report.Steps = d.MoveLimit - st.MovesLeft;
            foreach (var g in st.Goals)
                report.Goals.Add($"{(g.Type == GoalType.CollectColor ? ColorIds.ToId(g.Color) : g.Type == GoalType.CollectStars ? "star" : g.Type == GoalType.FillWater ? "water" : g.Type == GoalType.CollectSponges ? "sponge" : "envelope")} {g.Progress}/{g.Amount}");
            if (report.Error == null && st.Status != GameStatus.Won) report.Error = $"çözüm sonunda durum {st.Status}";
            report.Success = report.Error == null;
            return report;
        }
    }
}
