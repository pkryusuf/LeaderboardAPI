namespace LeaderboardAPI.Models
{
    public class Player
{
    public int PlayerId { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string DeviceId { get; set; }
    public DateTime RegistrationDate { get; set; }
    public int PlayerLevel { get; set; }
    public int TrophyCount { get; set; }
    public ICollection<Score> Scores { get; set; }
    }
}