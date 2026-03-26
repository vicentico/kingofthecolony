using System.ComponentModel;
using System.Runtime.CompilerServices;
using GGPOLauncher.Core.Models;

namespace GGPOLauncher.Core.Services;

public sealed class OverlayViewModel : INotifyPropertyChanged
{
    private string _roomTitle = "Sala";
    private string _kingLabel = "Rey pendiente";
    private string _challengerLabel = "Retador pendiente";
    private string _statusText = "Preparando partida";
    private string _queueSummary = string.Empty;
    private string _spectatorSummary = string.Empty;
    private MatchOverlayState _overlayState = MatchOverlayState.Hidden;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string RoomTitle
    {
        get => _roomTitle;
        set => SetProperty(ref _roomTitle, value);
    }

    public string KingLabel
    {
        get => _kingLabel;
        set => SetProperty(ref _kingLabel, value);
    }

    public string ChallengerLabel
    {
        get => _challengerLabel;
        set => SetProperty(ref _challengerLabel, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string QueueSummary
    {
        get => _queueSummary;
        set => SetProperty(ref _queueSummary, value);
    }

    public string SpectatorSummary
    {
        get => _spectatorSummary;
        set => SetProperty(ref _spectatorSummary, value);
    }

    public MatchOverlayState OverlayState
    {
        get => _overlayState;
        set => SetProperty(ref _overlayState, value);
    }

    public void UpdateFromRoom(RoomStateDto room)
    {
        RoomTitle = $"Sala #{room.RoomId} | {room.Status}";
        KingLabel = room.King is null
            ? "Rey pendiente"
            : $"Rey: {room.King.DisplayName} | {room.King.Credits} cr | W:{room.King.Wins} L:{room.King.Losses}";
        ChallengerLabel = room.Challenger is null
            ? "Retador pendiente"
            : $"Retador: {room.Challenger.DisplayName} | {room.Challenger.Credits} cr | W:{room.Challenger.Wins} L:{room.Challenger.Losses}";
        QueueSummary = room.Queue.Count > 0
            ? $"Siguiente: {room.Queue[0].User.DisplayName} ({room.Queue[0].User.Credits} cr) | Cola: {room.Queue.Count}"
            : "Sin jugadores en cola";
        SpectatorSummary = $"Espectadores: {room.SpectatorCount}";
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}