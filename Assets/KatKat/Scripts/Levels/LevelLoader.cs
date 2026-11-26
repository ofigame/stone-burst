using System;
using System.Collections.Generic;
using KatKat.Core;

namespace KatKat.Levels
{
    /// <summary>LevelData'dan derin kopya çalışma durumu üretir. Kaynak veri değişmez.</summary>
    public static class LevelLoader
    {
        /// <summary>Süreli bölümde hamle sayacı (pratikte sınırsız; HUD süreyi gösterir).</summary>
        public const int UnlimitedMoves = 9999;

        public static LevelState ToState(LevelData d)
        {
            var board = new BoardState(d.Width, d.Height);
            var startTray = new List<SpecialKind>();
            foreach (var c in d.Cells)
            {
                if (!board.InBounds(c.X, c.Y)) throw new FormatException($"{d.Id}: ({c.X},{c.Y}) tahta dışında");
                var cell = board[c.X, c.Y];
                cell.Active = c.Active;
                cell.ClipHitsRemaining = c.ClipHitsRemaining;
                cell.HasBottomStar = c.HasBottomStar;
                cell.Water = c.Water;
                cell.IceHitsRemaining = c.IceHits;
                cell.HasRotor = c.HasRotor;
                if (!string.IsNullOrEmpty(c.EnvelopeColor))
                {
                    if (!ColorIds.TryParse(c.EnvelopeColor, out cell.EnvelopeColor)) throw new FormatException($"{d.Id}: ({c.X},{c.Y}) zarfta bilinmeyen renk '{c.EnvelopeColor}'");
                    cell.HasEnvelope = true;
                }
                if (c.LockRemaining > 0)
                {
                    if (!ColorIds.TryParse(c.LockColor, out cell.LockColor)) throw new FormatException($"{d.Id}: ({c.X},{c.Y}) kilitte bilinmeyen renk '{c.LockColor}'");
                    cell.LockRemaining = c.LockRemaining;
                }
                foreach (var l in c.Layers)
                {
                    if (!ColorIds.TryParse(l, out var color)) throw new FormatException($"{d.Id}: ({c.X},{c.Y}) bilinmeyen renk '{l}'");
                    cell.Layers.Add(color);
                }
                foreach (var sc in c.SpongeColors)
                {
                    if (!ColorIds.TryParse(sc, out var color)) throw new FormatException($"{d.Id}: ({c.X},{c.Y}) süngerde bilinmeyen renk '{sc}'");
                    cell.SpongeNeed |= CellState.Bit(color);
                }
                cell.LinkId = c.LinkId;
                cell.LinkHits = c.LinkHits;
                cell.ButtonGate = c.ButtonGate;
                cell.Protected = c.Protected;
                // 2026-11-26 test bölümleri: hücredeki "special" açılışta tepsiye girer (2026-11-26: özel taşlar tepside).
                switch (c.Special)
                {
                    case "row": startTray.Add(SpecialKind.RocketRow); break;
                    case "col": startTray.Add(SpecialKind.RocketColumn); break;
                    case "row2": startTray.Add(SpecialKind.RocketRow2); break;
                    case "col2": startTray.Add(SpecialKind.RocketColumn2); break;
                    case "rainbow": startTray.Add(SpecialKind.Rainbow); break;
                    case "shovel": startTray.Add(SpecialKind.Shovel); break;
                }
                cell.Area = c.Area;
                cell.Gravel = c.Gravel;
                if (c.GateId > 0)
                {
                    cell.GateId = c.GateId;
                    cell.Active = false;
                    foreach (var gl in c.GateLayers)
                    {
                        if (!ColorIds.TryParse(gl, out var color)) throw new FormatException($"{d.Id}: ({c.X},{c.Y}) geçitte bilinmeyen renk '{gl}'");
                        cell.GateLayers.Add(color);
                    }
                }
            }

            var s = new LevelState
            {
                Board = board,
                MovesLeft = d.TimeLimit > 0 ? UnlimitedMoves : d.MoveLimit,
                RollEnabled = d.RollEnabled,
                RollSlotFull = d.RollEnabled && d.StartingRoll,
                SpecialsEnabled = d.SpecialsEnabled,
                StarThresholds = d.StarThresholds,
                PaintLeft = d.Paint,
                Status = GameStatus.Idle,
            };
            foreach (var g in d.Goals)
            {
                var gs = new GoalState { Amount = g.Amount, Stage = g.Stage };
                if (g.Type == "collectColor")
                {
                    gs.Type = GoalType.CollectColor;
                    if (!ColorIds.TryParse(g.Color, out gs.Color)) throw new FormatException($"{d.Id}: hedefte bilinmeyen renk '{g.Color}'");
                }
                // v14 (kullanıcı 2026-11-16: zincir tamamen kaldırılsın): collectChain desteklenmez; doğrulayıcı reddeder. Eski
                // veri yeniden üretilene kadar oyun çökmesin diye düz renk hedefi gibi okunur.
                else if (g.Type == "collectChain")
                {
                    gs.Type = GoalType.CollectColor;
                    if (!ColorIds.TryParse(g.Color, out gs.Color)) throw new FormatException($"{d.Id}: hedefte bilinmeyen renk '{g.Color}'");
                }
                else if (g.Type == "collectStars") gs.Type = GoalType.CollectStars;
                else if (g.Type == "fillWater") gs.Type = GoalType.FillWater;
                else if (g.Type == "collectToken" && g.Kind == "envelope") gs.Type = GoalType.CollectEnvelopes;
                else if (g.Type == "collectToken" && g.Kind == "sponge") gs.Type = GoalType.CollectSponges;
                else if (g.Type == "collectToken" && g.Kind == "balloon") gs.Type = GoalType.CollectBalloons;
                else throw new FormatException($"{d.Id}: bilinmeyen hedef tipi '{g.Type}'");
                s.Goals.Add(gs);
            }
            foreach (var k in startTray) if (s.Tray.Count < LevelState.TrayCapacity) s.Tray.Add(k);
            if (d.Hazard != null) s.Hazard = d.Hazard.ToState(board, d.Id); // konsept pilotu
            s.WaterCap = d.WaterCap; // 2026-11-26 su kovası
            if (d.Buckets.Count > 0) // 2026-11-23 kova kipi
            {
                var orders = new List<BucketOrder>();
                foreach (var kv in d.Buckets)
                {
                    if (!ColorIds.TryParse(kv.Key, out var bc)) throw new FormatException($"{d.Id}: kovada bilinmeyen renk '{kv.Key}'");
                    orders.Add(new BucketOrder(bc, kv.Value));
                }
                s.Bucket = new BucketRun();
                s.Bucket.Queue.AddRange(orders);
                RulesEngine.NextBucket(s);
            }
            if (d.HasRefill) // 2026-11-24 yukarıdan dolma: renkler tahtadakiler, tohum bölüm kimliğinden
            {
                var r = new RefillRun { Left = d.RefillCount, MinLayers = d.RefillMinLayers, MaxLayers = d.RefillMaxLayers };
                uint h = 2166136261u;
                foreach (char ch in d.Id ?? "") h = (h ^ ch) * 16777619u;
                r.Rng = h;
                var seen = new HashSet<LayerColor>();
                for (int i = 0; i < board.Count; i++) foreach (var c in board[i].Layers) seen.Add(c);
                var cols = new List<LayerColor>(seen);
                cols.Sort();
                r.Colors.AddRange(cols);
                s.Refill = r;
            }
            return s;
        }

        public static Command ToCommand(StepData step)
        {
            if (step.Type == "tap") return Command.Tap(step.X, step.Y);
            if (step.Type == "roll") return Command.Roll(step.Row);
            if (step.Type == "rotate") return Command.Rotate(step.X, step.Y);
            if (step.Type == "use") return Command.UseSpecial(step.Row, step.X, step.Y);
            if (step.Type == "swap") return Command.Swap(step.X, step.Y, step.Row); // 2026-11-26 özel taş sürüklemesi (row = yön 0 sağ, 1 aşağı, 2 sol, 3 yukarı)
            if (step.Type == "send") return Command.SendBucket(); // 2026-11-24 torba bölümleri
            if (step.Type == "paint")
            {
                if (!ColorIds.TryParse(step.Color, out var color)) throw new FormatException($"Boya adımında bilinmeyen renk '{step.Color}'");
                return Command.Paint(step.X, step.Y, color);
            }
            throw new FormatException($"Bilinmeyen çözüm adımı '{step.Type}'");
        }
    }
}
