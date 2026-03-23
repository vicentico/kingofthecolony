namespace KingOfTheColonyApi.Models;

public class GameRoom
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    /// <summary>Current king (winner stays)</summary>
    public int? KingUserId { get; set; }
    public User? KingUser { get; set; }

    /// <summary>Current challenger fighting the king</summary>
    public int? ChallengerUserId { get; set; }
    public User? ChallengerUser { get; set; }

    /// <summary>Playing, WaitingChallenger, Idle</summary>
    public string Status { get; set; } = "Idle";

    public string? HostIp { get; set; }
    public int UdpPort { get; set; } = 6000;
    public string GameRom { get; set; } = "kof2002";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<QueueEntry> Queue { get; set; } = [];
    public List<Spectator> Spectators { get; set; } = [];
}
