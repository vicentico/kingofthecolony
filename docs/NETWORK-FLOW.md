# Flujo de Red y Protocolo de Handshake

## Visión general

La aplicación utiliza **TCP en el puerto 6000** exclusivamente para el handshake inicial entre ambos jugadores. Una vez completado, el emulador FBNeo toma el control y usa **UDP en el puerto 6000** para el netcode GGPO. La conexión TCP se cierra inmediatamente después del handshake.

```
┌──────────────────────────────────────────────────────────────────┐
│  FASE 1: Handshake TCP (esta aplicación)                        │
│  Propósito: Acordar parámetros y sincronizar lanzamiento        │
├──────────────────────────────────────────────────────────────────┤
│  FASE 2: Juego UDP (FBNeo + GGPO)                               │
│  Propósito: Netcode del juego (rollback, inputs, sincronización)│
└──────────────────────────────────────────────────────────────────┘
```

---

## Diagrama de secuencia completo

```
  HOST (Jugador 1)                              CLIENTE (Jugador 2)
       │                                              │
       │  1. Click "Crear Partida"                    │
       │  ───────────────────────►                    │
       │  Obtiene IP pública via api.ipify.org        │
       │                                              │
       │  2. Click "Iniciar Host"                     │
       │  Crea TcpListener(Any:6000)                  │
       │  Estado: "Esperando jugador 2..."            │
       │                                              │
       │                                              │  3. Click "Unirse a Partida"
       │                                              │  Ingresa IP del Host
       │                                              │  Click "Conectar"
       │                                              │
       │              TCP Connect                      │
       │  ◄──────────────────────────────────────────  │
       │                                              │
       │  4. AcceptTcpClient() → conexión aceptada    │
       │  Estado: "¡Jugador conectado!"                │
       │                                              │
       │  5. Envía payload JSON ────────────────────► │
       │     {                                        │
       │       "Type": "StartGame",                   │
       │       "HostIp": "190.x.x.x",                │
       │       "UdpPort": 6000,                       │
       │       "GameRom": "kof2002",                  │
       │       "DelayFrames": 2                       │
       │     }                                        │
       │                                              │
       │                                              │  6. Recibe y parsea JSON
       │                                              │  Valida Type == "StartGame"
       │                                              │
       │  ◄──────────────────────────── Envía Ack ──  │
       │     {                                        │
       │       "Type": "Ack"                          │
       │     }                                        │
       │                                              │
       │  7. Recibe Ack                               │  7. Estado: "¡Conectado!"
       │  Dispara evento GameReady                    │  Dispara evento GameReady
       │                                              │
       │  8. Espera 3 segundos                        │  8. Espera 3 segundos
       │                                              │
       │  9. Cierra conexión TCP                      │  9. Cierra conexión TCP
       │                                              │
       │  10. Lanza emulador:                         │  10. Lanza emulador:
       │  fbneo.exe kof2002 -net 6000              │  fbneo.exe kof2002 -net 190.x.x.x:6000
       │                                              │
       │  ═══════════════  GGPO UDP ═══════════════   │
       │  ◄═══════════════════════════════════════►   │
       │         El juego se conecta via UDP          │
       └──────────────────────────────────────────────┘
```

---

## Detalle de cada paso

### Paso 1-2: Preparación del Host

```csharp
// IpHelper.cs - Obtiene la IP pública
var ip = await Http.GetStringAsync("https://api.ipify.org");

// TcpHost.cs - Inicia el listener
_listener = new TcpListener(IPAddress.Any, Port);  // Port = 6000
_listener.Start();
```

El Host muestra su IP pública en pantalla para que se la comparta al Cliente (por chat, mensaje, etc.).

### Paso 3: Conexión del Cliente

```csharp
// TcpClientManager.cs - Conecta al Host
_client = new TcpClient();
await _client.ConnectAsync(hostIp, Port, timeoutCts.Token);
```

- Timeout de **10 segundos** para evitar esperas infinitas.
- Si la conexión falla, muestra un error amigable con instrucciones.

### Paso 4-5: Host envía payload

```csharp
var payload = new HandshakePayload(
    Type: "StartGame",
    HostIp: publicIp,
    UdpPort: 6000,
    GameRom: "kof2002",
    DelayFrames: 2);

var json = JsonSerializer.Serialize(payload);
await stream.WriteAsync(Encoding.UTF8.GetBytes(json + "\n"), ct);
```

### Paso 6: Cliente responde con Ack

```csharp
var ack = new HandshakePayload(Type: "Ack");
await stream.WriteAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ack) + "\n"), ct);
```

### Paso 7-10: Lanzamiento simultáneo

Ambos lados disparan el evento `GameReady`, esperan 3 segundos y luego lanzan el emulador:

```csharp
// Host
launcher.LaunchAsHost(payload.UdpPort, payload.GameRom);
// → fbneo.exe kof2002 -net 6000

// Cliente
launcher.LaunchAsClient(payload.HostIp, payload.UdpPort, payload.GameRom);
// → fbneo.exe kof2002 -net 190.x.x.x:6000
```

---

## Payload JSON — Especificación

### StartGame (Host → Cliente)

```json
{
  "Type": "StartGame",
  "HostIp": "190.x.x.x",
  "UdpPort": 6000,
  "GameRom": "kof2002",
  "DelayFrames": 2
}
```

| Campo         | Tipo      | Requerido               | Descripción           |
| ------------- | --------- | ----------------------- | --------------------- |
| `Type`        | `string`  | Sí                      | Siempre `"StartGame"` |
| `HostIp`      | `string?` | Sí                      | IP pública del Host   |
| `UdpPort`     | `int`     | No (default: 6000)      | Puerto UDP para GGPO  |
| `GameRom`     | `string`  | No (default: "kof2002") | Nombre de la ROM      |
| `DelayFrames` | `int`     | No (default: 2)         | Frames de delay GGPO  |

### Ack (Cliente → Host)

```json
{
  "Type": "Ack"
}
```

| Campo  | Tipo     | Requerido | Descripción     |
| ------ | -------- | --------- | --------------- |
| `Type` | `string` | Sí        | Siempre `"Ack"` |

---

## Puertos utilizados

| Puerto | Protocolo | Usado por     | Fase      | Dirección                           |
| ------ | --------- | ------------- | --------- | ----------------------------------- |
| 6000   | TCP       | GGPO Launcher | Handshake | Host escucha, Cliente conecta       |
| 6000   | UDP       | FBNeo (GGPO)  | Juego     | Bidireccional entre ambos jugadores |

> **Nota:** El TCP se cierra después del handshake. El UDP es manejado enteramente por FBNeo/GGPO.

---

## Manejo de errores de red

| Escenario                                 | Quién lo detecta    | Acción                             |
| ----------------------------------------- | ------------------- | ---------------------------------- |
| Cliente no puede conectar (IP incorrecta) | `TcpClientManager`  | Muestra error + instrucciones      |
| Timeout de conexión (10s)                 | `TcpClientManager`  | Muestra error de timeout           |
| Host no recibe Ack válido                 | `TcpHost`           | Muestra error de handshake         |
| Puerto 6000 ya en uso                     | `TcpHost`           | Exception capturada, muestra error |
| Sin conexión a Internet                   | `IpHelper`          | Muestra "No se pudo obtener IP"    |
| Cancelación por el usuario (botón Volver) | `CancellationToken` | Limpia recursos, vuelve al menú    |

---

## Diagrama de estados de la UI

```
                    ┌──────────────┐
                    │  Selección   │
          ┌─────── │  de Rol      │ ───────┐
          │         └──────┬───────┘        │
          │                │                │
          ▼                ▼                ▼
   ┌──────────┐    ┌──────────┐    ┌──────────────┐
   │  Panel   │    │  Panel   │    │  Panel       │
   │  Host    │    │  Cliente │    │  Diagnóstico │
   └─────┬────┘    └─────┬────┘    └──────┬───────┘
         │               │               │
         │  Volver        │  Volver       │  Volver
         └───────►  ◄─────┘     ◄─────────┘
                Selección de Rol
```
