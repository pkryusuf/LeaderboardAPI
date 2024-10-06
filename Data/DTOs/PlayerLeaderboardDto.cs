namespace LeaderboardAPI.Data.DTOs
{
    public class PlayerLeaderboardDto
    {
        public int PlayerId { get; set; }
        public int TotalScore { get; set; }
        public DateTime RegistrationDate { get; set; }

        // Currently, PlayerLevel and TrophyCount are not used in this API.
        // These values may be provided by other APIs or might be added to this API in future updates.
        public int PlayerLevel { get; set; }            
        public int TrophyCount { get; set; }            
    }

}
