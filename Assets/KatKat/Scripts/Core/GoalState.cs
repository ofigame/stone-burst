namespace KatKat.Core
{
    /// <summary>Hedef ve ilerlemesi. Progress mantıkta sınırsız toplam tutar; ekran Amount ile sınırlar.</summary>
    public sealed class GoalState
    {
        public GoalType Type;
        public LayerColor Color;
        public int Amount;
        public int Progress;

        public bool IsComplete => Progress >= Amount;
        public int DisplayProgress => Progress < Amount ? Progress : Amount;

        public GoalState Clone() => new GoalState { Type = Type, Color = Color, Amount = Amount, Progress = Progress };

        public bool ContentEquals(GoalState o) => Type == o.Type && Color == o.Color && Amount == o.Amount && Progress == o.Progress;
    }
}
