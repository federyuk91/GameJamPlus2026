namespace GameJamPlus.Gameplay
{
    public enum EndGameOutcome
    {
        NoDeaths,
        SingleDeath,
        MultipleDeaths
    }

    public static class EndGameOutcomeSelector
    {
        public static EndGameOutcome Select(int deceasedSubjects)
        {
            if (deceasedSubjects <= 0) return EndGameOutcome.NoDeaths;
            return deceasedSubjects == 1 ? EndGameOutcome.SingleDeath : EndGameOutcome.MultipleDeaths;
        }
    }
}
