using SQLite;

namespace QuizBattle.Models
{
    [SQLite.Table("Decks")]
    public class DeckEntity
    {
        [SQLite.PrimaryKey, SQLite.AutoIncrement]
        public int Id { get; set; }

        [SQLite.Unique, SQLite.NotNull]
        public string Name { get; set; } = string.Empty;

        // Added this field for Firestore compatibility
        [SQLite.Ignore] // Prevents SQLite from trying to create a column for this in the DB
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [SQLite.Table("Questions")]
    public class QuestionEntity
    {
        [SQLite.PrimaryKey, SQLite.AutoIncrement]
        public int Id { get; set; }

        [SQLite.Indexed]
        public int DeckId { get; set; }

        [SQLite.NotNull]
        public string Type { get; set; } = "Identification";

        [SQLite.NotNull]
        public string Text { get; set; } = string.Empty;

        // Serialized option choices: "Opt1|Opt2|Opt3|Opt4"
        public string OptionsRaw { get; set; } = string.Empty;

        // Serialized answer text: "AnswerText"
        public string AnswersRaw { get; set; } = string.Empty;

        public int TimesAsked { get; set; } = 0;
        public int TimesCorrect { get; set; } = 0;
        public int TimesIncorrect { get; set; } = 0;
        public int CorrectProgress { get; set; } = 0;
    }
}