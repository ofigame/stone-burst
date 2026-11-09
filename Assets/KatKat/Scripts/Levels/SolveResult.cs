using System.Collections.Generic;
using KatKat.Core;

namespace KatKat.Levels
{
    public sealed class SolveResult
    {
        public SolveOutcome Outcome;
        public List<Command> Solution;
        public int NodesExpanded;
    }
}
