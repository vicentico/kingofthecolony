using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using GGPOLauncher.Core.Models;

namespace GGPOLauncher.Core.Diagnostics;

public static class EnvironmentValidator
{
    private const int Port = 6000;

    public static async Task<List<DiagnosticResult>> RunAllChecksAsync()
    {
        var results = new List<DiagnosticResult>
        {
            CheckWindowsCompatibility(),
            CheckEmulatorExists(),
            CheckRomExists(),
            CheckPortAvailable(),
            await CheckFirewallRuleAsync()
        };

        results.Add(await CheckInternetConnectivityAsync());

        return results;
    }

    private static DiagnosticResult CheckWindowsCompatibility()
    {
        var osVersion = Environment.OSVersion.Version;
        var isWin10OrLater = osVersion.Major >= 10;

        if (!isWin10OrLater)
        {
            return new DiagnosticResult(
                "Compatibilidad de Windows",
                false,
                $"Se detectó Windows {osVersion}. Esta aplicación requiere Windows 10 o superior.",
                """
                Paso a paso para solucionar:
                1. Esta aplicación requiere Windows 10 (versión 1809) o Windows 11.
                2. Actualiza tu sistema operativo a una versión compatible.
                """);
        }

        var buildNumber = osVersion.Build;
        var versionName = buildNumber switch
        {
            >= 22000 => "Windows 11",
            >= 10240 => "Windows 10",
            _ => $"Windows (build {buildNumber})"
        };

        return new DiagnosticResult(
            "Compatibilidad de Windows",
            true,
            $"{versionName} (build {buildNumber}) — Compatible.");
    }

    private static DiagnosticResult CheckEmulatorExists()
    {
        string[] searchPaths =
        [
            Path.Combine(AppContext.BaseDirectory, "fbneo.exe"),
            Path.Combine(AppContext.BaseDirectory, "emulator", "fbneo.exe")
        ];

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
                return new DiagnosticResult(
                    "Emulador (fbneo.exe)",
                    true,
                    $"Encontrado en: {path}");
        }

        var basePath = AppContext.BaseDirectory;
        return new DiagnosticResult(
            "Emulador (fbneo.exe)",
            false,
            "No se encontró fbneo.exe.",
            $"Paso a paso para solucionar:\n" +
            $"1. Descarga FBNeo desde: https://github.com/finalburnneo/FBNeo/releases\n" +
            $"2. Extrae el archivo descargado.\n" +
            $"3. Copia \"fbneo.exe\" en la misma carpeta donde está esta aplicación,\n" +
            $"   o dentro de una subcarpeta llamada \"emulator\".\n" +
            $"   Ruta esperada: {basePath}\n" +
            $"4. Vuelve a ejecutar esta validación.");
    }

    private static DiagnosticResult CheckRomExists()
    {
        string[] searchPaths =
        [
            Path.Combine(AppContext.BaseDirectory, "kof2002.zip"),
            Path.Combine(AppContext.BaseDirectory, "emulator", "kof2002.zip"),
            Path.Combine(AppContext.BaseDirectory, "roms", "kof2002.zip"),
            Path.Combine(AppContext.BaseDirectory, "emulator", "roms", "kof2002.zip")
        ];

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
                return new DiagnosticResult(
                    "ROM (kof2002.zip)",
                    true,
                    $"Encontrada en: {path}");
        }

        var basePath = AppContext.BaseDirectory;
        return new DiagnosticResult(
            "ROM (kof2002.zip)",
            false,
            "No se encontró kof2002.zip.",
            $"Paso a paso para solucionar:\n" +
            $"1. Consigue el archivo ROM \"kof2002.zip\" (debe ser compatible con FBNeo).\n" +
            $"2. Coloca \"kof2002.zip\" en alguna de estas ubicaciones:\n" +
            $"   - {basePath}\n" +
            $"   - {Path.Combine(basePath, "emulator")}\n" +
            $"   - {Path.Combine(basePath, "roms")}\n" +
            $"   - {Path.Combine(basePath, "emulator", "roms")}\n" +
            $"3. Vuelve a ejecutar esta validación.");
    }

    private static DiagnosticResult CheckPortAvailable()
    {
        try
        {
            // Check if port 6000 is already in use
            var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpListeners = ipProperties.GetActiveTcpListeners();

            foreach (var endpoint in tcpListeners)
            {
                if (endpoint.Port == Port)
                {
                    return new DiagnosticResult(
                        $"Puerto TCP {Port}",
                        false,
                        $"El puerto {Port} ya está en uso por otro programa.",
                        $"""
                        Paso a paso para solucionar:
                        1. Abre una terminal (CMD o PowerShell) como Administrador.
                        2. Ejecuta: netstat -aon | findstr :{Port}
                        3. Identifica el PID (último número de la línea).
                        4. Ejecuta: taskkill /PID <número_PID> /F
                        5. Si no puedes cerrar el proceso, reinicia el equipo.
                        6. Vuelve a ejecutar esta validación.
                        """);
                }
            }

            // Try to bind briefly to confirm it's truly available
            using var testSocket = new TcpListener(IPAddress.Any, Port);
            testSocket.Start();
            testSocket.Stop();

            return new DiagnosticResult(
                $"Puerto TCP {Port}",
                true,
                $"El puerto {Port} está disponible.");
        }
        catch (SocketException)
        {
            return new DiagnosticResult(
                $"Puerto TCP {Port}",
                false,
                $"No se pudo acceder al puerto {Port}.",
                $"""
                Paso a paso para solucionar:
                1. Asegúrate de no tener otra instancia de esta aplicación abierta.
                2. Cierra cualquier programa que use el puerto {Port}.
                3. Si el problema persiste, reinicia el equipo.
                4. Vuelve a ejecutar esta validación.
                """);
        }
    }

    private static async Task<DiagnosticResult> CheckFirewallRuleAsync()
    {
        try
        {
            // Check Windows Firewall for inbound rules on port 6000
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "advfirewall firewall show rule name=all dir=in",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process is null)
                return new DiagnosticResult("Firewall de Windows", false, "No se pudo verificar el firewall.");

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Look for a rule that allows port 6000
            bool hasRule = output.Contains("6000", StringComparison.OrdinalIgnoreCase)
                        || output.Contains("fbneo", StringComparison.OrdinalIgnoreCase)
                        || output.Contains("FBNeo", StringComparison.OrdinalIgnoreCase);

            if (hasRule)
            {
                return new DiagnosticResult(
                    "Firewall de Windows",
                    true,
                    "Se detectó una regla de firewall que podría permitir el tráfico en el puerto 6000.");
            }

            return new DiagnosticResult(
                "Firewall de Windows",
                false,
                "No se encontró una regla de firewall para el puerto 6000.",
                $"""
                Paso a paso para solucionar:
                1. Abre una terminal (PowerShell) como Administrador.
                2. Ejecuta estos dos comandos para crear las reglas:

                   netsh advfirewall firewall add rule name="KOF2002 GGPO TCP" dir=in action=allow protocol=TCP localport=6000
                   netsh advfirewall firewall add rule name="KOF2002 GGPO UDP" dir=in action=allow protocol=UDP localport=6000

                3. Alternativamente, en Windows 11:
                   a. Abre "Configuración" > "Privacidad y seguridad" > "Seguridad de Windows".
                   b. Haz clic en "Firewall y protección de red" > "Configuración avanzada".
                   c. Clic en "Reglas de entrada" > "Nueva regla...".
                   d. Selecciona "Puerto" > Siguiente.
                   e. Selecciona "TCP", escribe "6000" > Siguiente.
                   f. Selecciona "Permitir la conexión" > Siguiente > Siguiente.
                   g. Nombre: "KOF2002 GGPO TCP" > Finalizar.
                   h. Repite los pasos c-g pero seleccionando "UDP" en el paso e.
                4. Vuelve a ejecutar esta validación.
                """);
        }
        catch
        {
            return new DiagnosticResult(
                "Firewall de Windows",
                false,
                "No se pudo verificar la configuración del firewall.",
                """
                Paso a paso para solucionar:
                1. Ejecuta esta aplicación como Administrador para permitir la verificación.
                2. Si el problema persiste, abre manualmente las reglas de firewall
                   para el puerto 6000 (TCP y UDP) como se indica arriba.
                """);
        }
    }

    private static async Task<DiagnosticResult> CheckInternetConnectivityAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var ip = await http.GetStringAsync("https://api.ipify.org");

            return new DiagnosticResult(
                "Conexión a Internet",
                true,
                $"Conectado. IP pública: {ip.Trim()}");
        }
        catch
        {
            return new DiagnosticResult(
                "Conexión a Internet",
                false,
                "No se pudo obtener la IP pública. Sin conexión a Internet o el servicio no responde.",
                """
                Paso a paso para solucionar:
                1. Verifica que tu equipo tenga conexión a Internet.
                2. Abre un navegador y visita https://api.ipify.org para confirmarlo.
                3. Si usas VPN, intenta desconectarla temporalmente.
                4. Revisa la configuración de tu adaptador de red.
                5. Reinicia tu router si es necesario.
                6. Vuelve a ejecutar esta validación.
                """);
        }
    }
}
