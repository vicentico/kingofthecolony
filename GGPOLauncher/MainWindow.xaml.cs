using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using GGPOLauncher.Core.Diagnostics;
using GGPOLauncher.Core.Interfaces;
using GGPOLauncher.Core.Models;
using GGPOLauncher.Core.Network;
using GGPOLauncher.Core.Process;
using GGPOLauncher.Core.Services;
using GGPOLauncher.Utils;

namespace GGPOLauncher;

public partial class MainWindow : Window
{
    private readonly IEmulatorLauncher _launcher = new FbNeoLauncher();
    private INetworkManager? _networkManager;
    private CancellationTokenSource? _cts;
    private string _publicIp = "";
    private bool _isHost;

    private IApiClient? _apiClient;
    private IRealtimeClient? _realtimeClient;
    private UserProfile? _currentUser;
    private RoomStateDto? _selectedRoom;
    private string _apiBaseUrl = "http://localhost:5153";

    public MainWindow()
    {
        InitializeComponent();
        ShowLoginMode();
    }

    private void BtnShowLoginMode_Click(object sender, RoutedEventArgs e)
    {
        ShowLoginMode();
    }

    private void BtnShowRegisterMode_Click(object sender, RoutedEventArgs e)
    {
        ShowRegisterMode();
    }

    private void ShowLoginMode()
    {
        CardLoginMode.Visibility = Visibility.Visible;
        CardRegisterMode.Visibility = Visibility.Collapsed;
        TxtAuthModeHint.Text = "Ingresa con tu cuenta existente para ver tu perfil y tu posición en la cola.";
        BtnShowLoginMode.Background = System.Windows.Media.Brushes.Teal;
        BtnShowRegisterMode.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#374151"));

        if (!string.IsNullOrWhiteSpace(TxtRegisterEmail.Text) && string.IsNullOrWhiteSpace(TxtEmailLogin.Text))
            TxtEmailLogin.Text = TxtRegisterEmail.Text.Trim();
    }

    private void ShowRegisterMode()
    {
        CardLoginMode.Visibility = Visibility.Collapsed;
        CardRegisterMode.Visibility = Visibility.Visible;
        TxtAuthModeHint.Text = "Crea tu cuenta una sola vez. Luego entrarás automáticamente al dashboard.";
        BtnShowRegisterMode.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2563EB"));
        BtnShowLoginMode.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#374151"));

        if (!string.IsNullOrWhiteSpace(TxtEmailLogin.Text) && string.IsNullOrWhiteSpace(TxtRegisterEmail.Text))
            TxtRegisterEmail.Text = TxtEmailLogin.Text.Trim();
    }

    private async void BtnGoogleLogin_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetAuthControlsEnabled(false);
            TxtLoginStatus.Text = "Inicializando cliente...";

            _apiBaseUrl = TxtApiBaseUrl.Text.Trim();
            if (!TryValidateApiBaseUrl(out var validationError))
            {
                TxtLoginStatus.Text = validationError;
                return;
            }

            _apiClient = new ApiClient(_apiBaseUrl);

            var idToken = Microsoft.VisualBasic.Interaction.InputBox(
                "Pega aquí el Google id_token emitido por tu flujo OAuth.",
                "Google Login",
                "");

            if (string.IsNullOrWhiteSpace(idToken))
            {
                TxtLoginStatus.Text = "Inicio de sesión cancelado.";
                return;
            }

            TxtLoginStatus.Text = "Validando token con el servidor...";
            var auth = await _apiClient.LoginWithGoogleAsync(idToken);
            await CompleteLoginAsync(auth);
        }
        catch (Exception ex)
        {
            TxtLoginStatus.Text = $"No se pudo iniciar sesión: {ex.Message}";
        }
        finally
        {
            SetAuthControlsEnabled(true);
        }
    }

    private async void BtnEmailLogin_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetAuthControlsEnabled(false);
            TxtLoginStatus.Text = "Validando correo y clave...";

            if (!TryValidateEmailLogin(out var validationError))
            {
                TxtLoginStatus.Text = validationError;
                return;
            }

            _apiBaseUrl = TxtApiBaseUrl.Text.Trim();
            _apiClient = new ApiClient(_apiBaseUrl);

            var auth = await _apiClient.LoginWithEmailAsync(TxtEmailLogin.Text.Trim(), PwdLogin.Password);
            await CompleteLoginAsync(auth);
        }
        catch (Exception ex)
        {
            TxtLoginStatus.Text = $"No se pudo iniciar sesión: {ex.Message}";
        }
        finally
        {
            SetAuthControlsEnabled(true);
        }
    }

    private async void BtnRegisterEmail_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetAuthControlsEnabled(false);

            if (!TryValidateEmailRegistration(out var validationError))
            {
                TxtLoginStatus.Text = validationError;
                return;
            }

            TxtLoginStatus.Text = "Creando cuenta...";
            _apiBaseUrl = TxtApiBaseUrl.Text.Trim();
            _apiClient = new ApiClient(_apiBaseUrl);

            var auth = await _apiClient.RegisterWithEmailAsync(
                TxtRegisterEmail.Text.Trim(),
                PwdRegister.Password,
                TxtRegisterDisplayName.Text.Trim());

            await CompleteLoginAsync(auth);
        }
        catch (Exception ex)
        {
            TxtLoginStatus.Text = $"No se pudo crear la cuenta: {ex.Message}";
        }
        finally
        {
            SetAuthControlsEnabled(true);
        }
    }

    private async Task CompleteLoginAsync(AuthResponse auth)
    {
        _currentUser = auth.Profile;

        if (_realtimeClient is not null)
        {
            await _realtimeClient.DisposeAsync();
        }

        _realtimeClient = new RealtimeClient();
        _realtimeClient.RoomUpdated += room => Dispatcher.Invoke(() => UpdateRoomState(room));
        _realtimeClient.YourTurn += userId => Dispatcher.Invoke(() => OnYourTurn(userId));
        _realtimeClient.StatusMessage += message => Dispatcher.Invoke(() => TxtRealtimeStatus.Text = message);
        await _realtimeClient.ConnectAsync(_apiBaseUrl, auth.Token);

        PanelLogin.Visibility = Visibility.Collapsed;
        PanelDashboard.Visibility = Visibility.Visible;

        await RefreshDashboardAsync();
        ClearAuthInputs();
        TxtLoginStatus.Text = string.Empty;
    }

    private bool TryValidateApiBaseUrl(out string error)
    {
        _apiBaseUrl = TxtApiBaseUrl.Text.Trim();

        if (string.IsNullOrWhiteSpace(_apiBaseUrl))
        {
            error = "Debes indicar la URL base del backend.";
            return false;
        }

        if (!Uri.TryCreate(_apiBaseUrl, UriKind.Absolute, out _))
        {
            error = "La URL del backend no es válida.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private bool TryValidateEmailLogin(out string error)
    {
        if (!TryValidateApiBaseUrl(out error))
            return false;

        if (string.IsNullOrWhiteSpace(TxtEmailLogin.Text))
        {
            error = "Ingresa tu correo para iniciar sesión.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(PwdLogin.Password))
        {
            error = "Ingresa tu clave para iniciar sesión.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private bool TryValidateEmailRegistration(out string error)
    {
        if (!TryValidateApiBaseUrl(out error))
            return false;

        if (string.IsNullOrWhiteSpace(TxtRegisterDisplayName.Text))
        {
            error = "Ingresa el nombre que quieres mostrar en la plataforma.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(TxtRegisterEmail.Text))
        {
            error = "Ingresa un correo para crear tu cuenta.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(PwdRegister.Password))
        {
            error = "Crea una clave para proteger tu cuenta.";
            return false;
        }

        if (PwdRegister.Password.Length < 8)
        {
            error = "La clave debe tener al menos 8 caracteres.";
            return false;
        }

        if (PwdRegister.Password != PwdRegisterConfirm.Password)
        {
            error = "La confirmación de clave no coincide.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void SetAuthControlsEnabled(bool isEnabled)
    {
        BtnGoogleLogin.IsEnabled = isEnabled;
        BtnEmailLogin.IsEnabled = isEnabled;
        BtnRegisterEmail.IsEnabled = isEnabled;
        BtnShowLoginMode.IsEnabled = isEnabled;
        BtnShowRegisterMode.IsEnabled = isEnabled;
    }

    private void ClearAuthInputs()
    {
        TxtEmailLogin.Text = string.Empty;
        PwdLogin.Password = string.Empty;
        TxtRegisterDisplayName.Text = string.Empty;
        TxtRegisterEmail.Text = string.Empty;
        PwdRegister.Password = string.Empty;
        PwdRegisterConfirm.Password = string.Empty;
    }

    private async Task RefreshDashboardAsync()
    {
        if (_apiClient is null)
            return;

        _currentUser = await _apiClient.GetMyProfileAsync();
        var rooms = await _apiClient.GetRoomsAsync();
        var ranking = await _apiClient.GetRankingAsync();
        var credits = await _apiClient.GetCreditHistoryAsync();

        TxtWelcome.Text = $"Bienvenido, {_currentUser.DisplayName}";
        TxtHeaderSummary.Text = $"Créditos: {_currentUser.Credits} | Victorias: {_currentUser.Wins} | Derrotas: {_currentUser.Losses} | Win Rate: {_currentUser.WinRate:P1}";
        TxtProfileName.Text = _currentUser.DisplayName;
        TxtProfileEmail.Text = _currentUser.Email;
        TxtProfileStats.Text = $"Victorias: {_currentUser.Wins} | Derrotas: {_currentUser.Losses}\nRacha actual: {_currentUser.CurrentStreak} | Mejor racha: {_currentUser.BestStreak}\nPuntaje histórico: {_currentUser.Score:F0}";
        TxtCredits.Text = $"Créditos disponibles: {_currentUser.Credits}";

        ListCreditHistory.ItemsSource = credits.Select(c =>
            $"{c.CreatedAt:yyyy-MM-dd HH:mm} | {c.Type} | {(c.Amount > 0 ? "+" : "")}{c.Amount} | {c.Description}");

        ListRooms.ItemsSource = rooms.Select(r =>
            $"Sala #{r.RoomId} | Estado: {r.Status} | Rey: {r.King?.DisplayName ?? "N/D"} | Cola: {r.Queue.Count} | Espectadores: {r.SpectatorCount}");

        ListRanking.ItemsSource = ranking.Select(r =>
            $"#{r.Position} {r.DisplayName} | W:{r.Wins} L:{r.Losses} | WR:{r.WinRate:P1} | Racha:{r.BestStreak} | Score:{r.Score:F0}");

        if (_selectedRoom is not null)
        {
            var refreshed = await _apiClient.GetRoomAsync(_selectedRoom.RoomId);
            UpdateRoomState(refreshed);
        }
        else if (rooms.Count > 0)
        {
            UpdateRoomState(rooms[0]);
        }
        else
        {
            TxtRoomStatus.Text = "No hay salas activas. Crea una nueva sala para empezar.";
            TxtKingInfo.Text = string.Empty;
            TxtChallengerInfo.Text = string.Empty;
            TxtSpectatorCount.Text = string.Empty;
            ListQueue.ItemsSource = null;
        }
    }

    private async void BtnRefreshDashboard_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RefreshDashboardAsync();
        }
        catch (Exception ex)
        {
            TxtRealtimeStatus.Text = $"No se pudo actualizar el dashboard: {ex.Message}";
        }
    }

    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        _currentUser = null;
        _selectedRoom = null;
        _apiClient = null;

        if (_realtimeClient is not null)
        {
            _ = _realtimeClient.DisposeAsync();
            _realtimeClient = null;
        }

        PanelDashboard.Visibility = Visibility.Collapsed;
        PanelLegacyLauncher.Visibility = Visibility.Collapsed;
        PanelLogin.Visibility = Visibility.Visible;
        TxtLoginStatus.Text = "Sesión cerrada.";
    }

    private async void BtnCreateRoom_Click(object sender, RoutedEventArgs e)
    {
        if (_apiClient is null)
            return;

        try
        {
            _publicIp = await IpHelper.GetPublicIpAsync();
            var room = await _apiClient.CreateRoomAsync($"Sala de {_currentUser?.DisplayName}", _publicIp);
            _selectedRoom = room;
            UpdateRoomState(room);

            if (_realtimeClient is not null)
            {
                await _realtimeClient.JoinRoomAsync(room.RoomId);
            }

            TxtRealtimeStatus.Text = $"Sala #{room.RoomId} creada correctamente.";
            await RefreshDashboardAsync();
        }
        catch (Exception ex)
        {
            TxtRealtimeStatus.Text = $"No se pudo crear la sala: {ex.Message}";
        }
    }

    private async void BtnSpectateSelectedRoom_Click(object sender, RoutedEventArgs e)
    {
        if (_apiClient is null || _selectedRoom is null)
            return;

        try
        {
            await _apiClient.JoinSpectatorAsync(_selectedRoom.RoomId);
            if (_realtimeClient is not null)
            {
                await _realtimeClient.JoinRoomAsync(_selectedRoom.RoomId);
            }
            TxtRealtimeStatus.Text = "Ahora estás como espectador en la sala.";
            await RefreshDashboardAsync();
        }
        catch (Exception ex)
        {
            TxtRealtimeStatus.Text = $"No se pudo entrar como espectador: {ex.Message}";
        }
    }

    private async void BtnJoinQueueSelectedRoom_Click(object sender, RoutedEventArgs e)
    {
        if (_apiClient is null || _selectedRoom is null)
            return;

        try
        {
            await _apiClient.JoinQueueAsync(_selectedRoom.RoomId);
            TxtRealtimeStatus.Text = "Entraste a la cola correctamente.";
            await RefreshDashboardAsync();
        }
        catch (Exception ex)
        {
            TxtRealtimeStatus.Text = $"No se pudo entrar a la cola: {ex.Message}";
        }
    }

    private async void BtnAddOneCredit_Click(object sender, RoutedEventArgs e)
    {
        await AddCreditsAsync(1);
    }

    private async void BtnAddFiveCredits_Click(object sender, RoutedEventArgs e)
    {
        await AddCreditsAsync(5);
    }

    private async Task AddCreditsAsync(int amount)
    {
        if (_apiClient is null)
            return;

        try
        {
            var newBalance = await _apiClient.AddCreditsAsync(amount);
            TxtRealtimeStatus.Text = $"Créditos agregados. Nuevo saldo: {newBalance}.";
            await RefreshDashboardAsync();
        }
        catch (Exception ex)
        {
            TxtRealtimeStatus.Text = $"No se pudieron agregar créditos: {ex.Message}";
        }
    }

    private async void ListRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_apiClient is null || ListRooms.SelectedIndex < 0)
            return;

        try
        {
            var rooms = await _apiClient.GetRoomsAsync();
            if (ListRooms.SelectedIndex >= rooms.Count)
                return;

            _selectedRoom = rooms[ListRooms.SelectedIndex];
            UpdateRoomState(_selectedRoom);

            if (_realtimeClient is not null)
            {
                await _realtimeClient.JoinRoomAsync(_selectedRoom.RoomId);
            }
        }
        catch (Exception ex)
        {
            TxtRealtimeStatus.Text = $"No se pudo cargar la sala seleccionada: {ex.Message}";
        }
    }

    private void UpdateRoomState(RoomStateDto room)
    {
        _selectedRoom = room;
        TxtRoomStatus.Text = $"Sala #{room.RoomId} | Estado: {room.Status}";
        TxtKingInfo.Text = room.King is null
            ? "Rey actual: pendiente"
            : $"Rey actual: {room.King.DisplayName} | W:{room.King.Wins} L:{room.King.Losses}";
        TxtChallengerInfo.Text = room.Challenger is null
            ? "Retador actual: esperando siguiente jugador"
            : $"Retador actual: {room.Challenger.DisplayName} | W:{room.Challenger.Wins} L:{room.Challenger.Losses}";
        TxtSpectatorCount.Text = $"Espectadores: {room.SpectatorCount}";
        ListQueue.ItemsSource = room.Queue.Select(q =>
            $"#{q.Position} {q.User.DisplayName} | W:{q.User.Wins} L:{q.User.Losses} | desde {q.JoinedAt:HH:mm}");
    }

    private void OnYourTurn(int userId)
    {
        if (_currentUser is null)
            return;

        if (_currentUser.Id == userId)
        {
            TxtRealtimeStatus.Text = "Es tu turno. Abre el launcher y prepárate para la partida.";
        }
    }

    private async void BtnOpenLauncher_Click(object sender, RoutedEventArgs e)
    {
        PanelDashboard.Visibility = Visibility.Collapsed;
        PanelLegacyLauncher.Visibility = Visibility.Visible;

        _publicIp = await IpHelper.GetPublicIpAsync();
        TxtHostInstruction.Text =
            $"Para crear una partida, asegúrate de haber abierto el puerto 6000 (UDP y TCP) en la configuración de tu router (Port Forwarding). Tu IP pública actual es: {_publicIp}. Compártela con tu amigo.";
    }

    private void BtnBackToDashboard_Click(object sender, RoutedEventArgs e)
    {
        CleanupNetwork();
        PanelLegacyLauncher.Visibility = Visibility.Collapsed;
        PanelDashboard.Visibility = Visibility.Visible;
        TxtHostStatus.Text = string.Empty;
        TxtClientStatus.Text = string.Empty;
        BtnStartHost.IsEnabled = true;
        BtnConnect.IsEnabled = true;
    }

    private async void BtnStartHost_Click(object sender, RoutedEventArgs e)
    {
        _isHost = true;
        BtnStartHost.IsEnabled = false;
        _cts = new CancellationTokenSource();

        _networkManager = new TcpHost();
        WireEvents(_networkManager, TxtHostStatus);

        if (_selectedRoom is not null && _realtimeClient is not null)
        {
            await _realtimeClient.NotifyPlayerReadyAsync(_selectedRoom.RoomId, _publicIp);
        }

        await _networkManager.StartHostAsync(_publicIp, _cts.Token);
    }

    private async void BtnConnect_Click(object sender, RoutedEventArgs e)
    {
        var hostIp = TxtHostIp.Text.Trim();

        if (string.IsNullOrEmpty(hostIp) || !IPAddress.TryParse(hostIp, out _))
        {
            TxtClientStatus.Text = "Por favor, ingresa una dirección IP válida.";
            return;
        }

        _isHost = false;
        BtnConnect.IsEnabled = false;
        _cts = new CancellationTokenSource();

        _networkManager = new TcpClientManager();
        WireEvents(_networkManager, TxtClientStatus);

        await _networkManager.ConnectToHostAsync(hostIp, _cts.Token);
    }

    private void WireEvents(INetworkManager manager, TextBlock statusBlock)
    {
        manager.StatusChanged += msg => Dispatcher.Invoke(() => statusBlock.Text = msg);

        manager.ErrorOccurred += msg =>
            Dispatcher.Invoke(() =>
            {
                statusBlock.Text = msg;
                BtnStartHost.IsEnabled = true;
                BtnConnect.IsEnabled = true;
            });

        manager.GameReady += payload => Dispatcher.Invoke(() => OnGameReady(payload));
    }

    private async void OnGameReady(HandshakePayload payload)
    {
        await Task.Delay(3000);

        try
        {
            if (_isHost)
            {
                _launcher.LaunchAsHost(payload.UdpPort, payload.GameRom);
            }
            else
            {
                _launcher.LaunchAsClient(payload.HostIp!, payload.UdpPort, payload.GameRom);
            }
        }
        catch (FileNotFoundException)
        {
        }
    }

    private void CleanupNetwork()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _networkManager?.Dispose();
        _networkManager = null;
    }

    protected override async void OnClosed(EventArgs e)
    {
        CleanupNetwork();

        if (_realtimeClient is not null)
        {
            await _realtimeClient.DisposeAsync();
        }

        base.OnClosed(e);
    }
}
