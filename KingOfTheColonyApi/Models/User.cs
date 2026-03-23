namespace KingOfTheColonyApi.Models;

public class User
{
    public int Id { get; set; }
    public string GoogleId { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public int Credits { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int BestStreak { get; set; }
    public int CurrentStreak { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
