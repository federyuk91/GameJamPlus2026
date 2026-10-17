namespace GameJamPlus.Gameplay
{
    public enum EndGameOutcome
    {
        NoDeaths,
        PartialLoss,
        AllSubjectsLost
    }

    public static class EndGameOutcomeSelector
    {
        public static EndGameOutcome Select(int deceasedSubjects)
        {
            if (deceasedSubjects <= 0) return EndGameOutcome.NoDeaths;
            return deceasedSubjects >= 10 ? EndGameOutcome.AllSubjectsLost : EndGameOutcome.PartialLoss;
        }
    }
}
