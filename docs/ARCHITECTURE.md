# Arquitectura del Proyecto

## Principios de diseño

El proyecto sigue los principios **SOLID**, separando la UI de la lógica de negocio mediante interfaces. El código usa características modernas de C# 12 (records, primary constructors, collection expressions).

---

## Diagrama de capas

```
┌─────────────────────────────────────────────────────┐
│                    UI Layer (WPF)                    │
│              MainWindow.xaml / .xaml.cs              │
│  - Maneja eventos de botones                        │
│  - Actualiza mensajes en pantalla (Dispatcher)      │
│  - Orquesta el flujo Host / Cliente / Diagnóstico   │
└──────────┬──────────────┬───────────────┬───────────┘
           │              │               │
           ▼              ▼               ▼
┌──────────────┐ ┌────────────────┐ ┌─────────────────┐
│ Network Layer│ │ Launcher Layer │ │ Diagnostics     │
│ INetworkMgr  │ │ IEmulatorLnchr │ │ EnvironmentVal. │
│              │ │                │ │                 │
│ - TcpHost    │ │ - FbNeoLnchr   │ │ - 5 checks      │
│ - TcpClient  │ │                │ │                 │
│   Manager    │ │                │ │                 │
└──────┬───────┘ └───────┬────────┘ └────────┬────────┘
       │                 │                   │
       ▼                 ▼                   ▼
┌──────────────┐ ┌────────────────┐ ┌─────────────────┐
│   Models     │ │  System.Diag.  │ │   Utils         │
│ Handshake    │ │  Process       │ │   IpHelper      │
│ Payload      │ │                │ │                 │
└──────────────┘ └────────────────┘ └─────────────────┘
```

---

## Descripción de cada componente

### UI Layer — `MainWindow`

| Responsabilidad   | Detalle                                                                    |
| ----------------- | -------------------------------------------------------------------------- |
| Selección de rol  | Muestra 3 opciones: Host, Cliente, Validar Requisitos                      |
| Panel Host        | Muestra IP pública, inicia `TcpHost`, espera conexión                      |
| Panel Cliente     | Campo de texto para IP, inicia `TcpClientManager`                          |
| Panel Diagnóstico | Ejecuta `EnvironmentValidator`, muestra resultados con pasos de corrección |
| Dispatcher        | Todas las actualizaciones de UI se hacen via `Dispatcher.Invoke()`         |

La UI usa tres `StackPanel` superpuestos con `Visibility.Collapsed/Visible` para navegar entre paneles sin frames ni navegación.

### Network Layer — `INetworkManager`

Interfaz que define el contrato de red:

```csharp
public interface INetworkManager : IDisposable
{
    event Action<string>?             StatusChanged;
    event Action<HandshakePayload>?   GameReady;
    event Action<string>?             ErrorOccurred;

    Task StartHostAsync(string publicIp, CancellationToken ct);
    Task ConnectToHostAsync(string hostIp, CancellationToken ct);
    void Stop();
}
```

**Implementaciones:**

#### `TcpHost` (Host / Jugador 1)

- Crea un `TcpListener` en `IPAddress.Any:6000`.
- Espera conexión via `AcceptTcpClientAsync`.
- Envía payload `StartGame` (JSON) al cliente.
- Espera respuesta `Ack`.
- Dispara evento `GameReady` con los parámetros de la partida.

#### `TcpClientManager` (Cliente / Jugador 2)

- Crea un `TcpClient` y conecta a `hostIp:6000`.
- Timeout de 10 segundos para la conexión.
- Lee el payload `StartGame` del Host.
- Responde con `Ack`.
- Dispara evento `GameReady` con los parámetros recibidos.

### Launcher Layer — `IEmulatorLauncher`

```csharp
public interface IEmulatorLauncher
{
    void LaunchAsHost(int udpPort, string gameRom);
    void LaunchAsClient(string hostIp, int udpPort, string gameRom);
}
```

#### `FbNeoLauncher`

- Busca `fbneo.exe` en dos ubicaciones:
  1. `{AppDirectory}/fbneo.exe`
  2. `{AppDirectory}/emulator/fbneo.exe`
- **Host:** `fbneo.exe kof2002 -net 6000`
- **Cliente:** `fbneo.exe kof2002 -net {IP}:6000`
- Usa `ProcessStartInfo` con `UseShellExecute = false`.
- Si no encuentra el ejecutable, muestra `MessageBox` al usuario.

### Diagnostics — `EnvironmentValidator`

Clase estática que ejecuta 5 validaciones:

| #   | Validación                    | Método                             |
| --- | ----------------------------- | ---------------------------------- |
| 1   | Emulador `fbneo.exe` existe   | `CheckEmulatorExists()`            |
| 2   | ROM `kof2002.zip` existe      | `CheckRomExists()`                 |
| 3   | Puerto TCP 6000 disponible    | `CheckPortAvailable()`             |
| 4   | Regla de firewall configurada | `CheckFirewallRule()`              |
| 5   | Conectividad a Internet       | `CheckInternetConnectivityAsync()` |

Cada check devuelve un `DiagnosticResult` con:

- `Name`: Nombre descriptivo
- `Passed`: `true/false`
- `Message`: Descripción del resultado
- `FixSteps`: Instrucciones paso a paso si falló (nullable)

### Models

#### `HandshakePayload`

```csharp
public record HandshakePayload(
    string  Type,           // "StartGame" o "Ack"
    string? HostIp,         // IP pública del host
    int     UdpPort = 6000, // Puerto UDP para GGPO
    string  GameRom = "kof2002",
    int     DelayFrames = 2
);
```

#### `DiagnosticResult`

```csharp
public record DiagnosticResult(
    string  Name,
    bool    Passed,
    string  Message,
    string? FixSteps = null
);
```

### Utils — `IpHelper`

Clase estática que obtiene la IP pública del Host consultando `https://api.ipify.org` con `HttpClient`. Devuelve un string con la IP o un mensaje de error si no hay conectividad.

---

## Tecnologías y versiones

| Componente    | Versión                           |
| ------------- | --------------------------------- |
| .NET          | 10.0                              |
| C#            | 12                                |
| UI Framework  | WPF                               |
| Serialización | `System.Text.Json`                |
| Red           | `System.Net.Sockets` (TCP nativo) |
| Procesos      | `System.Diagnostics.Process`      |
