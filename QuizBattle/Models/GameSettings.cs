namespace QuizBattle.Models
{
    public static class GameSettings
    {
        // Combat & Game Mode settings
        public static bool IsZenMode { get; set; } = false;
        public static int PlayerLives { get; set; } = 3;
        public static int CorrectAnswersRequired { get; set; } = 1;
        public static int TimeLimitSeconds { get; set; } = 15; // -1 means untimed
        public static int MaxQuestions { get; set; } = 20;

        // Deck & Difficulty Selection settings
        public static string SelectedDeckName { get; set; } = "QuestionList";
        public static string CurrentDifficulty { get; set; } = "Normal";
        public static bool IsTimerEnabled { get; set; } = true;
        public static int TimerSeconds { get; set; } = 15;
    }
}