using GGPOLauncher.Core.Models;

namespace GGPOLauncher.Core.Interfaces;

public interface IApiClient
{
    string? Token { get; }
    UserProfile? CurrentUser { get; }

    Task<AuthResponse> LoginWithGoogleAsync(string idToken);
    Task<AuthResponse> LoginWithEmailAsync(string email, string password);
    Task<AuthResponse> RegisterWithEmailAsync(string email, string password, string displayName);
    Task<UserProfile> GetMyProfileAsync();
    Task<List<RankingEntry>> GetRankingAsync(int page = 1, int pageSize = 20);
    Task<List<RoomStateDto>> GetRoomsAsync();
    Task<RoomStateDto> GetRoomAsync(int roomId);
    Task<RoomStateDto> CreateRoomAsync(string name, string hostIp);
    Task JoinSpectatorAsync(int roomId);
    Task JoinQueueAsync(int roomId);
    Task<MatchSessionDto> CreateMatchSessionAsync(int roomId, CreateMatchSessionRequest request);
    Task<MatchSessionDto> MarkMatchStartedAsync(int roomId, Guid matchSessionId, StartMatchSessionRequest request);
    Task<MatchSessionCompletionResponse> CompleteMatchSessionAsync(int roomId, Guid matchSessionId, CompleteMatchSessionRequest request);
    Task<MatchSessionDto> CancelMatchSessionAsync(int roomId, Guid matchSessionId, CancelMatchSessionRequest request);
    Task<MatchSessionDto> ReviewMatchSessionAsync(int roomId, Guid matchSessionId, ReviewMatchSessionRequest request);
    Task<int> AddCreditsAsync(int amount);
    Task<List<CreditTransactionDto>> GetCreditHistoryAsync();
}
