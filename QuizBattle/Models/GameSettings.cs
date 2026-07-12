using System;

namespace QuizBattle.Models
{
    public static class GameSettings
    {
        // Combat & Game Mode settings
        public static bool IsZenMode { get; set; } = false;
        public static int PlayerLives { get; set; } = 3;
        public static int CorrectAnswersRequired { get; set; } = 1;
        private static int timeLimitSeconds = 300;
        public static int TimeLimitSeconds
        {
            get => timeLimitSeconds;
            set
            {
                if (value == -1)
                {
                    timeLimitSeconds = -1;
                    return;
                }
                timeLimitSeconds = Math.Clamp(value, 1, 600);
            }
        }
        public static int MaxQuestions { get; set; } = 20;

        // Deck & Difficulty Selection settings
        public static string SelectedDeckName { get; set; } = "QuestionList";
        public static string CurrentDifficulty { get; set; } = "Normal";
        public static bool IsTimerEnabled { get; set; } = true;
        private static int timerSeconds = 300;
        public static int TimerSeconds
        {
            get => timerSeconds;
            set => timerSeconds = Math.Clamp(value, 1, 600);
        }
    }
}