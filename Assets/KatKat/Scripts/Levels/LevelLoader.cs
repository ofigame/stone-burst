using System;
using KatKat.Core;

namespace KatKat.Levels
{
    /// <summary>LevelData'dan derin kopya çalışma durumu üretir. Kaynak veri değişmez.</summary>
    public static class LevelLoader
    {
        public static LevelState ToState(LevelData d)
        {
            var board = new BoardState(d.Width, d.Height);
            foreach (var c in d.Cells)
            {
                if (!board.InBounds(c.X, c.Y)) throw new FormatException($"{d.Id}: ({c.X},{c.Y}) tahta dışında");
                var cell = board[c.X, c.Y];
                cell.Active = c.Active;
                cell.ClipHitsRemaining = c.ClipHitsRemaining;
                cell.HasBottomStar = c.HasBottomStar;
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
                MovesLeft = d.MoveLimit,
                RollEnabled = d.RollEnabled,
                RollSlotFull = d.RollEnabled && d.StartingRoll,
                SpecialsEnabled = d.SpecialsEnabled,
                StarThresholds = d.StarThresholds,
                PaintLeft = d.Paint,
                Status = GameStatus.Idle,
            };
            foreach (var g in d.Goals)
            {
                var gs = new GoalState { Amount = g.Amount };
                if (g.Type == "collectColor")
                {
                    gs.Type = GoalType.CollectColor;
                    if (!ColorIds.TryParse(g.Color, out gs.Color)) throw new FormatException($"{d.Id}: hedefte bilinmeyen renk '{g.Color}'");
                }
                else if (g.Type == "collectChain")
                {
                    gs.Type = GoalType.CollectChain;
                    if (!ColorIds.TryParse(g.Color, out gs.Color)) throw new FormatException($"{d.Id}: zincir hedefinde bilinmeyen renk '{g.Color}'");
                }
                else if (g.Type == "collectStars") gs.Type = GoalType.CollectStars;
                else if (g.Type == "collectToken" && g.Kind == "envelope") gs.Type = GoalType.CollectEnvelopes;
                else if (g.Type == "collectToken" && g.Kind == "sponge") gs.Type = GoalType.CollectSponges;
                else throw new FormatException($"{d.Id}: bilinmeyen hedef tipi '{g.Type}'");
                s.Goals.Add(gs);
            }
            return s;
        }

        public static Command ToCommand(StepData step)
        {
            if (step.Type == "tap") return Command.Tap(step.X, step.Y);
            if (step.Type == "roll") return Command.Roll(step.Row);
            if (step.Type == "rotate") return Command.Rotate(step.X, step.Y);
            if (step.Type == "use") return Command.UseSpecial(step.Row, step.X, step.Y);
            if (step.Type == "paint")
            {
                if (!ColorIds.TryParse(step.Color, out var color)) throw new FormatException($"Boya adımında bilinmeyen renk '{step.Color}'");
                return Command.Paint(step.X, step.Y, color);
            }
            throw new FormatException($"Bilinmeyen çözüm adımı '{step.Type}'");
        }
    }
}
