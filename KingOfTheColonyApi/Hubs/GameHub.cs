using System.Security.Claims;
using KingOfTheColonyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KingOfTheColonyApi.Hubs;

[Authorize]
public class GameHub : Hub
{
    private readonly GameRoomService _rooms;

    public GameHub(GameRoomService rooms) => _rooms = rooms;

    /// <summary>Client joins a room group to receive real-time updates</summary>
    public async Task JoinRoom(int roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room-{roomId}");

        var state = await _rooms.GetRoomStateAsync(roomId);
        if (state is not null)
            await Clients.Caller.SendAsync("RoomUpdated", state);
    }

    /// <summary>Client leaves a room group</summary>
    public async Task LeaveRoom(int roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room-{roomId}");
    }

    /// <summary>Notify the room that a player is ready for the handshake</summary>
    public async Task PlayerReady(int roomId, string publicIp)
    {
        var userId = GetUserId();
        if (userId is null) return;

        await Clients.Group($"room-{roomId}")
            .SendAsync("PlayerReady", new { UserId = userId.Value, PublicIp = publicIp });
    }

    /// <summary>Send a chat-like message in the room (optional)</summary>
    public async Task SendMessage(int roomId, string message)
    {
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Anónimo";
        await Clients.Group($"room-{roomId}")
            .SendAsync("ReceiveMessage", new { User = userName, Message = message, Timestamp = DateTime.UtcNow });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    private int? GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null ? int.Parse(claim) : null;
    }
}
