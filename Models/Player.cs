namespace LeaderboardAPI.Models
{
    public class Player
{
    public int PlayerId { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string DeviceId { get; set; }
    public DateTime RegistrationDate { get; set; }

    // Currently, PlayerLevel and TrophyCount are not used in this API.
    // These values may be provided by other APIs or might be added to this API in future updates.
    public int PlayerLevel { get; set; }
    public int TrophyCount { get; set; }
    public ICollection<Score> Scores { get; set; }
    }
}