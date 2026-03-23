using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using GGPOLauncher.Core.Interfaces;
using GGPOLauncher.Core.Models;
using GGPOLauncher.Views;

namespace GGPOLauncher.Core.Services;

public sealed class MatchSessionCoordinator : IAsyncDisposable
{
    private readonly Window _owner;
    private readonly IApiClient _apiClient;
    private readonly IEmulatorLauncher _launcher;
    private readonly Action<string> _setStatus;
    private readonly Func<Task> _refreshDashboardAsync;
    private readonly Action<RoomStateDto> _updateRoomState;
    private readonly OverlayViewModel _overlayViewModel = new();

    private OverlayWindow? _overlayWindow;
    private EmulatorWindowTracker? _windowTracker;
    private System.Diagnostics.Process? _emulatorProcess;
    private bool _handlingExit;

    public ActiveMatchSession? ActiveSession { get; private set; }

    public MatchSessionCoordinator(
        Window owner,
        IApiClient apiClient,
        IEmulatorLauncher launcher,
        Action<string> setStatus,
        Func<Task> refreshDashboardAsync,
        Action<RoomStateDto> updateRoomState)
    {
        _owner = owner;
        _apiClient = apiClient;
        _launcher = launcher;
        _setStatus = setStatus;
        _refreshDashboardAsync = refreshDashboardAsync;
        _updateRoomState = updateRoomState;
    }

    public async Task StartMatchAsync(HandshakePayload payload, RoomStateDto? room, UserProfile? currentUser, bool isHost)
    {
        if (room?.King is null || room.Challenger is null || currentUser is null)
        {
            _setStatus("La sala aún no tiene rey y retador listos. Se abrirá el emulador sin seguimiento.");
            LaunchFallback(payload, isHost);
            return;
        }

        if (ActiveSession is not null && ActiveSession.State is MatchLifecycleState.Running or MatchLifecycleState.AwaitingManualResult or MatchLifecycleState.ReportingResult)
        {
            _setStatus("Ya existe una partida en seguimiento. Cierra la sesión actual antes de iniciar otra.");
            return;
        }

        try
        {
            var createResponse = await _apiClient.CreateMatchSessionAsync(room.RoomId, new CreateMatchSessionRequest(
                room.King.Id,
                room.Challenger.Id,
                payload.GameRom,
                "LauncherWpf",
                Guid.NewGuid().ToString("D")));

            EnsureOverlayWindow();
            _overlayViewModel.UpdateFromRoom(room);
            _overlayViewModel.OverlayState = MatchOverlayState.Loading;
            _overlayViewModel.StatusText = "Conectando partida...";
            _overlayWindow?.ShowOverlay();

            _emulatorProcess = isHost
                ? _launcher.LaunchAsHost(payload.UdpPort, payload.GameRom)
                : _launcher.LaunchAsClient(payload.HostIp!, payload.UdpPort, payload.GameRom);

            ActiveSession = new ActiveMatchSession
            {
                MatchSessionId = createResponse.MatchSessionId,
                RoomId = room.RoomId,
                KingUserId = room.King.Id,
                KingDisplayName = room.King.DisplayName,
                ChallengerUserId = room.Challenger.Id,
                ChallengerDisplayName = room.Challenger.DisplayName,
                GameRom = payload.GameRom,
                LocalReporterUserId = currentUser.Id,
                EmulatorProcessId = _emulatorProcess.Id,
                EmulatorProcessPath = _emulatorProcess.StartInfo.FileName ?? string.Empty,
                StartedAtUtc = DateTime.UtcNow,
                State = MatchLifecycleState.PendingLaunch
            };

            AttachWindowTracker(_emulatorProcess);

            await _apiClient.MarkMatchStartedAsync(room.RoomId, createResponse.MatchSessionId,
                new StartMatchSessionRequest(_emulatorProcess.Id, DateTime.UtcNow));

            ActiveSession.State = MatchLifecycleState.Running;
            _overlayViewModel.OverlayState = MatchOverlayState.InMatch;
            _overlayViewModel.StatusText = "Partida en curso";
            _setStatus($"Partida en seguimiento para la sala #{room.RoomId}.");
        }
        catch (Exception ex)
        {
            ActiveSession = null;
            _overlayWindow?.HideOverlay();
            _setStatus($"No se pudo iniciar el seguimiento de la partida: {ex.Message}");
            LaunchFallback(payload, isHost);
        }
    }

    public void UpdateRoom(RoomStateDto room)
    {
        if (ActiveSession?.RoomId != room.RoomId)
            return;

        _overlayViewModel.UpdateFromRoom(room);
    }

    private void LaunchFallback(HandshakePayload payload, bool isHost)
    {
        if (isHost)
            _launcher.LaunchAsHost(payload.UdpPort, payload.GameRom);
        else
            _launcher.LaunchAsClient(payload.HostIp!, payload.UdpPort, payload.GameRom);
    }

    private void EnsureOverlayWindow()
    {
        if (_overlayWindow is not null)
            return;

        _overlayWindow = new OverlayWindow(_overlayViewModel)
        {
            Owner = _owner
        };
    }

    private void AttachWindowTracker(System.Diagnostics.Process process)
    {
        _windowTracker?.Dispose();
        _windowTracker = new EmulatorWindowTracker(process);
        _windowTracker.BoundsChanged += bounds =>
        {
            Application.Current.Dispatcher.Invoke(() => _overlayWindow?.UpdateBounds(bounds));
        };
        _windowTracker.Exited += () =>
        {
            Application.Current.Dispatcher.InvokeAsync(HandleEmulatorExitedAsync);
        };
    }

    private async Task HandleEmulatorExitedAsync()
    {
        if (_handlingExit || ActiveSession is null)
            return;

        _handlingExit = true;

        try
        {
            ActiveSession.State = MatchLifecycleState.AwaitingManualResult;
            _overlayViewModel.OverlayState = MatchOverlayState.AwaitingResult;
            _overlayViewModel.StatusText = "Confirma el ganador";

            var dialog = new MatchResultDialog(ActiveSession.KingDisplayName, ActiveSession.ChallengerDisplayName)
            {
                Owner = _owner
            };

            dialog.ShowDialog();

            switch (dialog.Decision)
            {
                case MatchResultDialogDecision.KingWon:
                    await SubmitResultAsync(ActiveSession.KingUserId, ActiveSession.ChallengerUserId, MatchResultSource.ManualSelection);
                    break;
                case MatchResultDialogDecision.ChallengerWon:
                    await SubmitResultAsync(ActiveSession.ChallengerUserId, ActiveSession.KingUserId, MatchResultSource.ManualSelection);
                    break;
                case MatchResultDialogDecision.Cancelled:
                    await CancelAsync("PlayersClosedEmulator");
                    break;
                case MatchResultDialogDecision.NeedsReview:
                default:
                    await ReviewAsync("Resultado no confirmado por el usuario.");
                    break;
            }
        }
        finally
        {
            _handlingExit = false;
        }
    }

    private async Task SubmitResultAsync(int winnerUserId, int loserUserId, MatchResultSource source)
    {
        if (ActiveSession is null)
            return;

        ActiveSession.State = MatchLifecycleState.ReportingResult;
        ActiveSession.ResultSource = source;
        ActiveSession.WinnerUserId = winnerUserId;
        ActiveSession.LoserUserId = loserUserId;

        var evidence = JsonSerializer.SerializeToElement(new
        {
            type = "manual-dialog",
            emulatorExitDetected = true,
            emulatorProcessId = ActiveSession.EmulatorProcessId,
            notes = "Resultado confirmado desde el launcher WPF"
        });

        var response = await _apiClient.CompleteMatchSessionAsync(ActiveSession.RoomId, ActiveSession.MatchSessionId,
            new CompleteMatchSessionRequest(
                winnerUserId,
                loserUserId,
                source.ToString(),
                ActiveSession.LocalReporterUserId,
                DateTime.UtcNow,
                evidence,
                $"{ActiveSession.RoomId}-{ActiveSession.MatchSessionId}-complete-v1"));

        ActiveSession.State = MatchLifecycleState.Completed;
        _overlayViewModel.UpdateFromRoom(response.RoomState);
        _overlayViewModel.OverlayState = MatchOverlayState.ResultSubmitted;
        _overlayViewModel.StatusText = response.Message;
        _updateRoomState(response.RoomState);
        _setStatus(response.Message);
        await _refreshDashboardAsync();
        await Task.Delay(2000);
        _overlayWindow?.HideOverlay();
    }

    private async Task CancelAsync(string reason)
    {
        if (ActiveSession is null)
            return;

        await _apiClient.CancelMatchSessionAsync(ActiveSession.RoomId, ActiveSession.MatchSessionId,
            new CancelMatchSessionRequest(reason, ActiveSession.LocalReporterUserId, DateTime.UtcNow));

        ActiveSession.State = MatchLifecycleState.Cancelled;
        _overlayViewModel.OverlayState = MatchOverlayState.ResultSubmitted;
        _overlayViewModel.StatusText = "Partida cancelada";
        _setStatus("La partida fue cancelada y no afecta el ranking.");
        await _refreshDashboardAsync();
        await Task.Delay(1200);
        _overlayWindow?.HideOverlay();
    }

    private async Task ReviewAsync(string reason)
    {
        if (ActiveSession is null)
            return;

        await _apiClient.ReviewMatchSessionAsync(ActiveSession.RoomId, ActiveSession.MatchSessionId,
            new ReviewMatchSessionRequest(reason, ActiveSession.LocalReporterUserId, DateTime.UtcNow));

        ActiveSession.State = MatchLifecycleState.PendingReview;
        _overlayViewModel.OverlayState = MatchOverlayState.ResultSubmitted;
        _overlayViewModel.StatusText = "Pendiente de revisión manual";
        _setStatus("La partida quedó pendiente de revisión manual.");
        await _refreshDashboardAsync();
        await Task.Delay(1200);
        _overlayWindow?.HideOverlay();
    }

    public async ValueTask DisposeAsync()
    {
        _windowTracker?.Dispose();
        _windowTracker = null;

        if (_overlayWindow is not null)
        {
            _overlayWindow.Close();
            _overlayWindow = null;
        }

        if (_emulatorProcess is not null)
        {
            await _emulatorProcess.WaitForExitAsync().WaitAsync(TimeSpan.FromMilliseconds(50)).ContinueWith(_ => { });
            _emulatorProcess.Dispose();
            _emulatorProcess = null;
        }
    }
}