using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using GGPOLauncher.Core.Diagnostics;
using GGPOLauncher.Core.Interfaces;
using GGPOLauncher.Core.Models;
using GGPOLauncher.Core.Network;
using GGPOLauncher.Core.Process;
using GGPOLauncher.Utils;

namespace GGPOLauncher;

public partial class MainWindow : Window
{
    private readonly IEmulatorLauncher _launcher = new FbNeoLauncher();
    private INetworkManager? _networkManager;
    private CancellationTokenSource? _cts;
    private string _publicIp = "";
    private bool _isHost;

    public MainWindow()
    {
        InitializeComponent();
    }

    // --- Role Selection ---

    private async void BtnHost_Click(object sender, RoutedEventArgs e)
    {
        PanelRoleSelection.Visibility = Visibility.Collapsed;
        PanelHost.Visibility = Visibility.Visible;

        _publicIp = await IpHelper.GetPublicIpAsync();
        TxtHostInstruction.Text =
            $"Para crear una partida, asegúrate de haber abierto el puerto 6000 (UDP y TCP) en la configuración de tu router (Port Forwarding). Tu IP pública actual es: {_publicIp}. Compártela con tu amigo.";
    }

    private void BtnClient_Click(object sender, RoutedEventArgs e)
    {
        PanelRoleSelection.Visibility = Visibility.Collapsed;
        PanelClient.Visibility = Visibility.Visible;
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        CleanupNetwork();
        PanelHost.Visibility = Visibility.Collapsed;
        PanelClient.Visibility = Visibility.Collapsed;
        PanelDiagnostics.Visibility = Visibility.Collapsed;
        PanelRoleSelection.Visibility = Visibility.Visible;
        TxtHostStatus.Text = "";
        TxtClientStatus.Text = "";
        BtnStartHost.IsEnabled = true;
        BtnConnect.IsEnabled = true;
    }

    // --- Diagnostics ---

    private async void BtnDiagnostic_Click(object sender, RoutedEventArgs e)
    {
        PanelRoleSelection.Visibility = Visibility.Collapsed;
        PanelDiagnostics.Visibility = Visibility.Visible;
        BtnRerunDiagnostic.IsEnabled = false;
        DiagnosticsResults.Children.Clear();

        var loading = new TextBlock
        {
            Text = "Ejecutando validaciones...",
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A6ADC8")),
            FontSize = 14,
            Margin = new Thickness(0, 8, 0, 8)
        };
        DiagnosticsResults.Children.Add(loading);

        var results = await EnvironmentValidator.RunAllChecksAsync();

        DiagnosticsResults.Children.Clear();

        foreach (var result in results)
        {
            DiagnosticsResults.Children.Add(BuildResultCard(result));
        }

        var allPassed = results.TrueForAll(r => r.Passed);
        var summary = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(allPassed ? "#313244" : "#45475A")),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 12, 0, 0),
            Child = new TextBlock
            {
                Text = allPassed
                    ? "Todas las validaciones pasaron correctamente. ¡Estás listo para jugar!"
                    : "Algunas validaciones fallaron. Revisa los pasos indicados arriba para solucionar los problemas.",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(allPassed ? "#A6E3A1" : "#F38BA8")),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap
            }
        };
        DiagnosticsResults.Children.Add(summary);

        BtnRerunDiagnostic.IsEnabled = true;
    }

    private void BtnBackFromDiag_Click(object sender, RoutedEventArgs e)
    {
        PanelDiagnostics.Visibility = Visibility.Collapsed;
        PanelRoleSelection.Visibility = Visibility.Visible;
    }

    private static Border BuildResultCard(DiagnosticResult result)
    {
        var icon = result.Passed ? "✅" : "❌";
        var statusColor = result.Passed ? "#A6E3A1" : "#F38BA8";

        var stack = new StackPanel();

        // Header
        var header = new TextBlock
        {
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        };
        header.Inlines.Add(new Run(icon + " ") { FontSize = 16 });
        header.Inlines.Add(new Run(result.Name)
        {
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(statusColor))
        });
        stack.Children.Add(header);

        // Message
        stack.Children.Add(new TextBlock
        {
            Text = result.Message,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CDD6F4")),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(24, 0, 0, 0)
        });

        // Fix steps if failed
        if (!result.Passed && result.FixSteps is not null)
        {
            var fixBlock = new TextBlock
            {
                Text = result.FixSteps.Trim(),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9E2AF")),
                FontSize = 12,
                FontFamily = new FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(24, 6, 0, 0)
            };
            stack.Children.Add(fixBlock);
        }

        return new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#313244")),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 6, 0, 0),
            Child = stack
        };
    }

    // --- Host Flow ---

    private async void BtnStartHost_Click(object sender, RoutedEventArgs e)
    {
        _isHost = true;
        BtnStartHost.IsEnabled = false;
        _cts = new CancellationTokenSource();

        _networkManager = new TcpHost();
        WireEvents(_networkManager, TxtHostStatus);

        await _networkManager.StartHostAsync(_publicIp, _cts.Token);
    }

    // --- Client Flow ---

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

    // --- Shared Wiring ---

    private void WireEvents(INetworkManager manager, System.Windows.Controls.TextBlock statusBlock)
    {
        manager.StatusChanged += msg =>
            Dispatcher.Invoke(() => statusBlock.Text = msg);

        manager.ErrorOccurred += msg =>
            Dispatcher.Invoke(() =>
            {
                statusBlock.Text = msg;
                BtnStartHost.IsEnabled = true;
                BtnConnect.IsEnabled = true;
            });

        manager.GameReady += payload =>
            Dispatcher.Invoke(() => OnGameReady(payload));
    }

    private async void OnGameReady(HandshakePayload payload)
    {
        await Task.Delay(3000);

        try
        {
            if (_isHost)
                _launcher.LaunchAsHost(payload.UdpPort, payload.GameRom);
            else
                _launcher.LaunchAsClient(payload.HostIp!, payload.UdpPort, payload.GameRom);
        }
        catch (FileNotFoundException)
        {
            // MessageBox is already shown by FbNeoLauncher
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

    protected override void OnClosed(EventArgs e)
    {
        CleanupNetwork();
        base.OnClosed(e);
    }
}
