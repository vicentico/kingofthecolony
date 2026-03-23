namespace GGPOLauncher.Core.Interfaces;

public interface IEmulatorLauncher
{
    void LaunchAsHost(int udpPort, string gameRom);
    void LaunchAsClient(string hostIp, int udpPort, string gameRom);
}
