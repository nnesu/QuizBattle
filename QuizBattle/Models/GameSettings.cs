namespace QuizBattle.Models
{
    public static class GameSettings
    {
        public static bool IsZenMode { get; set; }

        public static int PlayerLives { get; set; }

        public static int CorrectAnswersRequired { get; set; }

        // -1 means untimed
        public static int TimeLimitSeconds { get; set; }

        // Max number of questions to generate (for Gemini integration)
        public static int MaxQuestions { get; set; } = 20;

    }
}