namespace LeaderboardAPI.Data.DTOs
{
    public class PlayerLeaderboardDto
    {
        public int PlayerId { get; set; }
        public int TotalScore { get; set; }
        public DateTime RegistrationDate { get; set; }  
        public int PlayerLevel { get; set; }            
        public int TrophyCount { get; set; }            
    }

}
