using System.Collections.Generic;
using KatKat.Core;

namespace KatKat.Levels
{
    public sealed class ReplayReport
    {
        public string LevelId;
        public bool Success;
        public string Error;
        public int Steps;
        public int MoveLimit;
        public int MovesLeft;
        public int Waves;
        public int LayersPeeled;
        public int RollsAwarded;
        public int RollsUsed;
        public GameStatus FinalStatus;
        public readonly List<string> Goals = new List<string>();
    }
}
