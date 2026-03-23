using GGPOLauncher.Core.Models;

namespace GGPOLauncher.Core.Interfaces;

public interface INetworkManager : IDisposable
{
    event Action<string>? StatusChanged;
    event Action<HandshakePayload>? GameReady;
    event Action<string>? ErrorOccurred;

    Task StartHostAsync(string publicIp, CancellationToken ct = default);
    Task ConnectToHostAsync(string hostIp, CancellationToken ct = default);
    void Stop();
}
