using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GGPOLauncher.Core.Interfaces;
using GGPOLauncher.Core.Models;

namespace GGPOLauncher.Core.Network;

public sealed class TcpHost : INetworkManager
{
    private const int Port = 6000;
    private TcpListener? _listener;
    private TcpClient? _client;
    private bool _disposed;

    public event Action<string>? StatusChanged;
    public event Action<HandshakePayload>? GameReady;
    public event Action<string>? ErrorOccurred;

    public async Task StartHostAsync(string publicIp, CancellationToken ct = default)
    {
        try
        {
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();
            StatusChanged?.Invoke("Esperando a que el jugador 2 se conecte...");

            _client = await _listener.AcceptTcpClientAsync(ct);
            StatusChanged?.Invoke("¡Jugador conectado! Lanzando KOF 2002 en 3 segundos...");

            var payload = new HandshakePayload(
                Type: "StartGame",
                HostIp: publicIp,
                UdpPort: Port,
                GameRom: "kof2002",
                DelayFrames: 2);

            var stream = _client.GetStream();
            var json = JsonSerializer.Serialize(payload);
            var data = Encoding.UTF8.GetBytes(json + "\n");
            await stream.WriteAsync(data, ct);
            await stream.FlushAsync(ct);

            // Wait for Ack
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer, ct);
            var response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            var ack = JsonSerializer.Deserialize<HandshakePayload>(response);

            if (ack?.Type == "Ack")
            {
                GameReady?.Invoke(payload);
            }
            else
            {
                ErrorOccurred?.Invoke("El cliente no respondió correctamente al handshake.");
            }
        }
        catch (OperationCanceledException)
        {
            StatusChanged?.Invoke("Conexión cancelada.");
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

    public Task ConnectToHostAsync(string hostIp, CancellationToken ct = default)
    {
        throw new NotSupportedException("TcpHost no soporta ConnectToHostAsync. Usa TcpClientManager.");
    }

    public void Stop()
    {
        _client?.Close();
        _client = null;
        _listener?.Stop();
        _listener = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
