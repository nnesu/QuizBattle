using System;

namespace QuizBattle.Models
{
    public static class GameSettings
    {
        // current difficulty name
        public static string CurrentDifficulty { get; set; } = "Medium";

        // difficulty rules
        public static bool IsZenMode { get; set; } = false;
        public static int PlayerLives { get; set; } = 4;
        public static int CorrectAnswersRequired { get; set; } = 2;
        public static int TimeLimitSeconds { get; set; } = 15;
        public static int MaxQuestions { get; set; } = 20;

        // deck selection
        public static string SelectedDeckName { get; set; } = "";
    }
}