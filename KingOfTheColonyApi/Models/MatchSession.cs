namespace KingOfTheColonyApi.Models;

public class MatchSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int GameRoomId { get; set; }
    public GameRoom GameRoom { get; set; } = null!;

    public int KingUserId { get; set; }
    public User KingUser { get; set; } = null!;

    public int ChallengerUserId { get; set; }
    public User ChallengerUser { get; set; } = null!;

    public int? WinnerUserId { get; set; }
    public User? WinnerUser { get; set; }

    public int? LoserUserId { get; set; }
    public User? LoserUser { get; set; }

    public string Status { get; set; } = "Created";
    public string GameRom { get; set; } = "kof2002";
    public string ResultSource { get; set; } = "Unknown";
    public string LaunchSource { get; set; } = "LauncherWpf";
    public string ClientInstanceId { get; set; } = "";
    public string? EvidencePayload { get; set; }
    public string? ResultReason { get; set; }
    public string? LastIdempotencyKey { get; set; }
    public int? EmulatorProcessId { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public int? ReportedByUserId { get; set; }
    public User? ReportedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public DateTime? ReportedAtUtc { get; set; }
}