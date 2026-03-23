namespace GGPOLauncher.Core.Models;

public sealed class ActiveMatchSession
{
    public Guid MatchSessionId { get; init; }
    public int RoomId { get; init; }
    public int KingUserId { get; init; }
    public string KingDisplayName { get; init; } = string.Empty;
    public int ChallengerUserId { get; init; }
    public string ChallengerDisplayName { get; init; } = string.Empty;
    public string GameRom { get; init; } = "kof2002";
    public DateTime StartedAtUtc { get; set; }
    public int LocalReporterUserId { get; init; }
    public string EmulatorProcessPath { get; init; } = string.Empty;
    public int? EmulatorProcessId { get; set; }
    public MatchLifecycleState State { get; set; } = MatchLifecycleState.PendingLaunch;
    public MatchResultSource ResultSource { get; set; } = MatchResultSource.Unknown;
    public int? WinnerUserId { get; set; }
    public int? LoserUserId { get; set; }
}

public enum MatchLifecycleState
{
    PendingLaunch,
    Running,
    AwaitingManualResult,
    ReportingResult,
    Completed,
    Cancelled,
    PendingReview,
    Failed
}

public enum MatchResultSource
{
    Unknown,
    ManualSelection,
    AutoDetection,
    AdminCorrection
}

public enum MatchOverlayState
{
    Loading,
    InMatch,
    AwaitingResult,
    ResultSubmitted,
    Hidden
}

public enum MatchResultDialogDecision
{
    None,
    KingWon,
    ChallengerWon,
    Cancelled,
    NeedsReview
}