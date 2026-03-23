namespace KingOfTheColonyApi.Models;

public class MatchHistory
{
    public int Id { get; set; }
    public int GameRoomId { get; set; }
    public int WinnerId { get; set; }
    public User Winner { get; set; } = null!;
    public int LoserId { get; set; }
    public User Loser { get; set; } = null!;
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
}
