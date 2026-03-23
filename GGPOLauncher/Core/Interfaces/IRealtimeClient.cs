using GGPOLauncher.Core.Models;

namespace GGPOLauncher.Core.Interfaces;

public interface IRealtimeClient : IAsyncDisposable
{
    event Action<RoomStateDto>? RoomUpdated;
    event Action<int>? YourTurn;
    event Action<string>? StatusMessage;

    Task ConnectAsync(string baseUrl, string token);
    Task JoinRoomAsync(int roomId);
    Task LeaveRoomAsync(int roomId);
    Task NotifyPlayerReadyAsync(int roomId, string publicIp);
}
