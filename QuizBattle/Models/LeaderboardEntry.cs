namespace QuizBattle.Models
{
    public class LeaderboardEntry
    {
        public string LocalId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string DeckName { get; set; } = string.Empty;
        public int Score { get; set; }
    }
}