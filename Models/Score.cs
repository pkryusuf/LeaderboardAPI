namespace LeaderboardAPI.Models
{
    public class Score
    {
        public int ScoreId { get; set; }
        public int PlayerId { get; set; }
        public Player Player { get; set; }
        public int MatchScore { get; set; }
        public DateTime ScoreTime { get; set; }
    }
}
