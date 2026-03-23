namespace GGPOLauncher.Core.Interfaces;

public interface IEmulatorLauncher
{
    System.Diagnostics.Process LaunchAsHost(int udpPort, string gameRom);
    System.Diagnostics.Process LaunchAsClient(string hostIp, int udpPort, string gameRom);
}
