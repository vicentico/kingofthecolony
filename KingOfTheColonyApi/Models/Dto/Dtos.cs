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

public record UserSummary(int Id, string DisplayName, string AvatarUrl, int Wins, int Losses);

public record QueueEntryDto(int Position, UserSummary User, DateTime JoinedAt);

public record AddCreditsRequest(int Amount);

public record ReportMatchRequest(int RoomId, int WinnerId, int LoserId);
