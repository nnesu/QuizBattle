using QuizBattle.Models;
using System.Collections.Generic;

namespace QuizBattle.Models
{
    public class Question
    {
        public QuestionType Type { get; set; }

        public string Text { get; set; } = string.Empty;

        public List<string> Options { get; set; } = new List<string>();

        public List<string> CorrectAnswers { get; set; } = new List<string>();

        public int TimesAsked { get; set; }

        public int TimesCorrect { get; set; }

        public int TimesIncorrect { get; set; }

        public int CorrectProgress { get; set; }

        public bool IsCompleted { get; set; }
    }
}