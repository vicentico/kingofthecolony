using GGPOLauncher.Core.Interfaces;
using GGPOLauncher.Core.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace GGPOLauncher.Core.Services;

public sealed class RealtimeClient : IRealtimeClient
{
    private HubConnection? _connection;

    public event Action<RoomStateDto>? RoomUpdated;
    public event Action<int>? YourTurn;
    public event Action<string>? StatusMessage;

    public async Task ConnectAsync(string baseUrl, string token)
    {
        if (_connection is not null)
            return;

        _connection = new HubConnectionBuilder()
            .WithUrl(baseUrl.TrimEnd('/') + "/hubs/game", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token)!;
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<RoomStateDto>("RoomUpdated", room => RoomUpdated?.Invoke(room));
        _connection.On<int>("YourTurn", userId => YourTurn?.Invoke(userId));
        _connection.On<object>("PlayerReady", _ => StatusMessage?.Invoke("Un jugador está listo para iniciar la partida."));
        _connection.On<object>("RoomCreated", _ => StatusMessage?.Invoke("Se creó una nueva sala."));

        await _connection.StartAsync();
        StatusMessage?.Invoke("Conectado al servidor en tiempo real.");
    }

    public Task JoinRoomAsync(int roomId) =>
        _connection?.InvokeAsync("JoinRoom", roomId) ?? Task.CompletedTask;

    public Task LeaveRoomAsync(int roomId) =>
        _connection?.InvokeAsync("LeaveRoom", roomId) ?? Task.CompletedTask;

    public Task NotifyPlayerReadyAsync(int roomId, string publicIp) =>
        _connection?.InvokeAsync("PlayerReady", roomId, publicIp) ?? Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
