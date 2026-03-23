namespace KingOfTheColonyApi.Models;

public class QueueEntry
{
    public int Id { get; set; }
    public int GameRoomId { get; set; }
    public GameRoom GameRoom { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int Position { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
