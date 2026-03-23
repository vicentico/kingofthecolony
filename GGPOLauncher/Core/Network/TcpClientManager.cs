using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GGPOLauncher.Core.Interfaces;
using GGPOLauncher.Core.Models;

namespace GGPOLauncher.Core.Network;

public sealed class TcpClientManager : INetworkManager
{
    private const int Port = 6000;
    private TcpClient? _client;
    private bool _disposed;

    public event Action<string>? StatusChanged;
    public event Action<HandshakePayload>? GameReady;
    public event Action<string>? ErrorOccurred;

    public Task StartHostAsync(string publicIp, CancellationToken ct = default)
    {
        throw new NotSupportedException("TcpClientManager no soporta StartHostAsync. Usa TcpHost.");
    }

    public async Task ConnectToHostAsync(string hostIp, CancellationToken ct = default)
    {
        try
        {
            _client = new TcpClient();
            StatusChanged?.Invoke("Conectando con el Host...");

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

            await _client.ConnectAsync(hostIp, Port, timeoutCts.Token);

            var stream = _client.GetStream();

            // Read the handshake payload from Host
            var buffer = new byte[4096];
            var bytesRead = await stream.ReadAsync(buffer, timeoutCts.Token);
            var json = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            var payload = JsonSerializer.Deserialize<HandshakePayload>(json);

            if (payload?.Type != "StartGame")
            {
                ErrorOccurred?.Invoke("Se recibió un mensaje inesperado del Host.");
                return;
            }

            // Send Ack
            var ack = new HandshakePayload(Type: "Ack");
            var ackJson = JsonSerializer.Serialize(ack);
            var ackData = Encoding.UTF8.GetBytes(ackJson + "\n");
            await stream.WriteAsync(ackData, timeoutCts.Token);
            await stream.FlushAsync(timeoutCts.Token);

            StatusChanged?.Invoke("¡Conectado! Lanzando KOF 2002 en 3 segundos...");
            GameReady?.Invoke(payload);
        }
        catch (OperationCanceledException)
        {
            ErrorOccurred?.Invoke("No se pudo conectar. Verifica que la IP sea correcta y que tu amigo haya abierto el puerto 6000 en su router.");
        }
        catch (SocketException)
        {
            ErrorOccurred?.Invoke("No se pudo conectar. Verifica que la IP sea correcta y que tu amigo haya abierto el puerto 6000 en su router.");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Error de conexión: {ex.Message}");
        }
        finally
        {
            Stop();
        }
    }

    public void Stop()
    {
        _client?.Close();
        _client = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
