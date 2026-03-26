using System.Text.Json;

namespace KingOfTheColonyApi.Models.Dto;

public record GoogleLoginRequest(string IdToken);

public record EmailRegisterRequest(string Email, string Password, string DisplayName);

public record EmailLoginRequest(string Email, string Password);

public record AuthResponse(string Token, UserProfile Profile);

public record UserProfile(
    int Id,
    string DisplayName,
    string AvatarUrl,
    string Email,
    int Credits,
    int Wins,
    int Losses,
    int BestStreak,
    int CurrentStreak,
    double WinRate,
    double Score);

public record RankingEntry(
    int Position,
    int UserId,
    string DisplayName,
    string AvatarUrl,
    int Wins,
    int Losses,
    double WinRate,
    int BestStreak,
    int CurrentStreak,
    double Score);

public record RoomStateDto(
    int RoomId,
    string Status,
    UserSummary? King,
    UserSummary? Challenger,
    List<QueueEntryDto> Queue,
    int SpectatorCount);

public record UserSummary(int Id, string DisplayName, string AvatarUrl, int Credits, int Wins, int Losses);

public record QueueEntryDto(int Position, UserSummary User, DateTime JoinedAt);

public record AddCreditsRequest(int Amount);

public record ReportMatchRequest(int RoomId, int WinnerId, int LoserId);

public record CreateMatchSessionRequest(
    int KingUserId,
    int ChallengerUserId,
    string GameRom,
    string LaunchSource,
    string ClientInstanceId);

public record MatchSessionDto(
    Guid MatchSessionId,
    int RoomId,
    string Status,
    string GameRom,
    UserSummary King,
    UserSummary Challenger,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? EndedAtUtc);

public record StartMatchSessionRequest(int EmulatorProcessId, DateTime? StartedAtUtc);

public record CompleteMatchSessionRequest(
    int WinnerUserId,
    int LoserUserId,
    string ResultSource,
    int ReportedByUserId,
    DateTime? EndedAtUtc,
    JsonElement? Evidence,
    string IdempotencyKey);

public record CancelMatchSessionRequest(string Reason, int ReportedByUserId, DateTime? EndedAtUtc);

public record ReviewMatchSessionRequest(string Reason, int ReportedByUserId, DateTime? EndedAtUtc);

public record MatchSessionCompletionResponse(
    Guid MatchSessionId,
    string Status,
    string Message,
    int? NextChallengerId,
    string? NextChallengerDisplayName,
    RoomStateDto RoomState);
