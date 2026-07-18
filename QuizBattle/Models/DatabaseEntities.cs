using SQLite;

namespace QuizBattle.Models
{
    [SQLite.Table("Decks")]
    public class DeckEntity
    {
        [SQLite.PrimaryKey, SQLite.AutoIncrement]
        public int Id { get; set; }

        [SQLite.NotNull]
        public string Name { get; set; } = string.Empty;

        [SQLite.Unique, SQLite.NotNull]
        public string Uid { get; set; } = string.Empty; // UNIQUE RANDOM RUNTIME TOKEN

        [SQLite.Ignore]
        public string Description { get; set; } = string.Empty;

        [SQLite.NotNull]
        public bool IsReadOnly { get; set; } = false;

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

        public string OptionsRaw { get; set; } = string.Empty;
        public string AnswersRaw { get; set; } = string.Empty;

        public int TimesAsked { get; set; } = 0;
        public int TimesCorrect { get; set; } = 0;
        public int TimesIncorrect { get; set; } = 0;
        public int CorrectProgress { get; set; } = 0;
    }
}