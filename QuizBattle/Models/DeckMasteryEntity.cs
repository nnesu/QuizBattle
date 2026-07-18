using SQLite;
using System;

namespace QuizBattle.Models
{
    [Table("DeckMastery")]
    public class DeckMasteryEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int DeckId { get; set; }

        public int HighScore { get; set; }
        public DateTime LastPlayed { get; set; }
    }
}