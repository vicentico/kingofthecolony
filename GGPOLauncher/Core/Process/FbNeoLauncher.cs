using System.Diagnostics;
using System.IO;
using System.Windows;
using GGPOLauncher.Core.Interfaces;

namespace GGPOLauncher.Core.Process;

public sealed class FbNeoLauncher : IEmulatorLauncher
{
    private static readonly string[] SearchPaths =
    [
        Path.Combine(AppContext.BaseDirectory, "fbneo.exe"),
        Path.Combine(AppContext.BaseDirectory, "emulator", "fbneo.exe")
    ];

    public System.Diagnostics.Process LaunchAsHost(int udpPort, string gameRom)
    {
        var exePath = FindExecutable();
        var args = $"{gameRom} -net {udpPort}";
        return Launch(exePath, args);
    }

    public System.Diagnostics.Process LaunchAsClient(string hostIp, int udpPort, string gameRom)
    {
        var exePath = FindExecutable();
        var args = $"{gameRom} -net {hostIp}:{udpPort}";
        return Launch(exePath, args);
    }

    private static string FindExecutable()
    {
        foreach (var path in SearchPaths)
        {
            if (File.Exists(path))
                return path;
        }

        MessageBox.Show(
            "No se encontró el emulador. Asegúrate de que fbneo.exe esté en la misma carpeta.",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        throw new FileNotFoundException("fbneo.exe no encontrado.");
    }

    private static System.Diagnostics.Process Launch(string exePath, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(exePath)!
        };

        var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException("No se pudo iniciar el emulador.");

        process.EnableRaisingEvents = true;
        return process;
    }
}
