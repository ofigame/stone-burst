namespace KatKat.Levels
{
    public enum SolveOutcome
    {
        Unsolvable = 0,
        Solved = 1,
        /// <summary>Bütçe aşıldı; çözümsüz anlamına gelmez.</summary>
        BudgetExceeded = 2,
    }
}
