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
    Task<int> AddCreditsAsync(int amount);
    Task<List<CreditTransactionDto>> GetCreditHistoryAsync();
}
