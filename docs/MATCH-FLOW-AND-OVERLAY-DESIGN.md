# Flujo de cierre, overlay y contrato Launcher/API

## Objetivo

Este documento baja a diseño concreto tres piezas que faltan para cerrar el ciclo completo de juego:

1. cierre manual de partida y reporte de ganador;
2. overlay WPF por encima de FBNeo para mostrar contexto de la sala;
3. contrato exacto entre launcher y API para evolucionar desde reporte manual a registro automatico del resultado.

La recomendacion es implementar esto en dos fases:

- Fase 1: cierre manual confiable con trazabilidad completa.
- Fase 2: deteccion automatica del ganador reutilizando el mismo contrato.

Asi se evita bloquear la entrega por depender de una integracion incierta con FBNeo.

## Estado actual del sistema

Hoy existen estas capacidades:

- el launcher abre FBNeo cuando se completa el handshake TCP;
- la API ya expone `POST /api/rooms/{id}/report-result`;
- `GameRoomService.ReportMatchResultAsync()` ya actualiza rey, cola, victorias, derrotas y ranking;
- el cliente WPF ya conoce la sala seleccionada y el usuario autenticado.

Hoy faltan estas capacidades:

- representar una partida concreta como sesion con identidad propia;
- saber que el proceso de FBNeo asociado a la partida termino;
- capturar o confirmar quien gano;
- mostrar overlay persistente sobre la ventana del emulador;
- tener un contrato robusto para idempotencia, auditoria y futura automatizacion.

## Decision de diseño

No usar `report-result` como contrato principal del launcher para produccion. Se debe mantener por compatibilidad o administracion, pero el flujo real del cliente debe operar sobre una entidad nueva: `match session`.

### Por que

- `report-result` no distingue intentos repetidos;
- no modela evidencia ni origen del resultado;
- no permite saber si una partida fue lanzada pero nunca terminada;
- no separa `launcher inicio partida` de `resultado confirmado`;
- dificulta evolucionar a deteccion automatica sin romper clientes.

## Flujo manual de cierre de partida

### Resumen operativo

1. El rey y el retador estan listos.
2. El launcher inicia la partida y registra una `match session` en la API.
3. Se abre FBNeo.
4. El launcher monitorea el proceso del emulador.
5. Cuando FBNeo termina, el launcher abre un dialogo modal de cierre.
6. El usuario elige ganador o marca la partida como cancelada.
7. El launcher envia el resultado a la API.
8. La API actualiza sala, rey, cola, ranking e historial.
9. SignalR emite la actualizacion de sala.
10. El launcher refresca dashboard y overlay.

### Regla principal

El resultado solo se considera oficial cuando la API cambia la `match session` a `Completed`.

### Estado local minimo en el launcher

Se recomienda introducir un coordinador local de partida:

```csharp
public sealed record ActiveMatchSession(
    Guid MatchSessionId,
    int RoomId,
    int KingUserId,
    string KingDisplayName,
    int ChallengerUserId,
    string ChallengerDisplayName,
    string GameRom,
    DateTime StartedAtUtc,
    int LocalReporterUserId,
    string EmulatorProcessPath,
    int? EmulatorProcessId,
    MatchLifecycleState State,
    MatchResultSource ResultSource,
    int? WinnerUserId,
    int? LoserUserId);

public enum MatchLifecycleState
{
    PendingLaunch,
    Running,
    AwaitingManualResult,
    ReportingResult,
    Completed,
    Cancelled,
    Failed
}

public enum MatchResultSource
{
    Unknown,
    ManualSelection,
    AutoDetection,
    AdminCorrection
}
```

### Flujo detallado

#### 1. Antes de abrir FBNeo

Cuando `OnGameReady(...)` tenga confirmados `roomId`, `king`, `challenger` y `gameRom`, el launcher debe:

- crear la `match session` en la API;
- guardar el `matchSessionId` retornado;
- inicializar el overlay en modo `Loading`;
- lanzar FBNeo y conservar el `Process`.

#### 2. Mientras FBNeo esta abierto

El launcher debe:

- fijar `State = Running`;
- mover el overlay junto a la ventana del emulador;
- mostrar rey, retador, numero de sala y siguiente jugador de cola;
- opcionalmente enviar heartbeat cada 10 segundos.

#### 3. Cuando FBNeo se cierra

El launcher debe:

- detectar `Process.Exited`;
- esconder el overlay principal;
- cambiar a `State = AwaitingManualResult`;
- abrir una ventana modal `MatchResultDialog`.

#### 4. Dialogo de cierre manual

La ventana debe ofrecer exactamente estas acciones:

- `Gano el rey`;
- `Gano el retador`;
- `Partida cancelada`;
- `No estoy seguro`.

Comportamiento:

- `Gano el rey`: completa con `winnerUserId = kingUserId`.
- `Gano el retador`: completa con `winnerUserId = challengerUserId`.
- `Partida cancelada`: marca sesion `Cancelled`; no altera ranking.
- `No estoy seguro`: deja sesion en `PendingReview`; no altera ranking hasta resolucion manual.

### Reglas UX

- si el reporter es solo espectador, no puede cerrar resultado;
- si el reporter es uno de los dos jugadores, se le permite reportar;
- si ambos clientes reportan manualmente y discrepan, la API debe dejar la sesion en `Conflict`;
- si solo un cliente reporta dentro del tiempo limite, se acepta como `SingleReporterConfirmed`.

### Recomendacion de entrega inicial

Para la primera iteracion, aceptar resultado desde un solo reporter autenticado si coincide con rey o retador, y registrar `ReportedByUserId`. La doble confirmacion puede llegar despues sin romper el contrato.

## Diseño del overlay WPF encima de FBNeo

## Objetivo visual

Mostrar informacion competitiva sin modificar FBNeo:

- sala actual;
- rey actual;
- retador actual;
- racha o score resumido;
- estado de la partida;
- siguiente jugador en cola;
- opcionalmente un sello `Tu turno`.

## Ventana recomendada

Crear una ventana independiente, no integrada a `MainWindow`:

- `OverlayWindow.xaml`
- `OverlayWindow.xaml.cs`
- `OverlayViewModel.cs`
- `EmulatorWindowTracker.cs`
- `MatchSessionCoordinator.cs`

## Propiedades de la ventana

La ventana debe ser:

- `WindowStyle = None`;
- `AllowsTransparency = true`;
- `Background = Transparent`;
- `Topmost = true`;
- `ShowInTaskbar = false`;
- no activable al mostrarse.

Adicionalmente se recomienda volverla click-through cuando este en modo solo lectura, usando estilos extendidos Win32.

## Posicionamiento

El overlay no debe ir fullscreen. Debe acoplarse a la ventana de FBNeo.

### Estrategia

1. localizar el `MainWindowHandle` del proceso FBNeo;
2. leer su rectangulo con `GetWindowRect`;
3. ubicar la ventana overlay relativa a ese rectangulo;
4. re-sincronizar cada 100-150 ms mientras el proceso siga vivo.

### Layout recomendado

- franja superior de 72 px alineada con el borde superior del juego;
- panel izquierdo: rey actual;
- panel central: `Sala #12 | KOF 2002 | FT1` o formato que definas;
- panel derecho: retador actual;
- franja inferior opcional para `Siguiente: nombre` y `Espectadores: N`.

## Estados visuales

El overlay debe tener cuatro estados:

### 1. `Loading`

Se usa entre lanzamiento y aparicion de la ventana del emulador.

Contenido:

- `Conectando partida...`
- nombres de jugadores ya asignados.

### 2. `InMatch`

Se usa con FBNeo activo.

Contenido:

- rey y retador;
- `Rey de la colina`;
- identificador de sala;
- siguiente en cola.

### 3. `AwaitingResult`

Se usa cuando el proceso se cerro pero el resultado aun no fue enviado.

Contenido:

- fondo mas marcado;
- mensaje `Confirma el ganador`.

### 4. `ResultSubmitted`

Visible 2-3 segundos despues de enviar resultado.

Contenido:

- `Resultado registrado`;
- `Siguiente retador: nombre` o `Esperando challenger`.

## Jerarquia de componentes

```text
MainWindow
  -> MatchSessionCoordinator
      -> ApiClient
      -> RealtimeClient
      -> FbNeoLauncher
      -> OverlayWindow
      -> MatchResultDialog
      -> EmulatorWindowTracker
```

## Responsabilidades por clase

### `MatchSessionCoordinator`

- crear y cerrar `match session`;
- orquestar overlay;
- abrir y observar proceso FBNeo;
- disparar cierre manual o automatico;
- evitar dobles reportes.

### `EmulatorWindowTracker`

- vincularse al `Process`;
- encontrar `HWND`;
- exponer evento `BoundsChanged`;
- exponer evento `Exited`.

### `OverlayWindow`

- renderizar datos ya preparados;
- no contener logica de negocio.

### `MatchResultDialog`

- recoger decision humana al terminar la partida;
- devolver `WinnerUserId?`, `Cancelled`, `NeedsReview`.

## Contrato exacto entre launcher y API

## Modelo recomendado

Agregar una entidad `MatchSession`.

```csharp
public class MatchSession
{
    public Guid Id { get; set; }
    public int GameRoomId { get; set; }
    public int KingUserId { get; set; }
    public int ChallengerUserId { get; set; }
    public int? WinnerUserId { get; set; }
    public int? LoserUserId { get; set; }
    public string Status { get; set; } = "Created";
    public string GameRom { get; set; } = "kof2002";
    public string ResultSource { get; set; } = "Unknown";
    public string? EvidencePayload { get; set; }
    public int CreatedByUserId { get; set; }
    public int? ReportedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public DateTime? ReportedAtUtc { get; set; }
}
```

## Estados de sesion

- `Created`
- `Running`
- `AwaitingResult`
- `Completed`
- `Cancelled`
- `PendingReview`
- `Conflict`

## Endpoints nuevos

### 1. Crear sesion de partida

`POST /api/rooms/{roomId}/matches`

Request:

```json
{
  "kingUserId": 15,
  "challengerUserId": 27,
  "gameRom": "kof2002",
  "launchSource": "LauncherWpf",
  "clientInstanceId": "a54a7b72-9b24-4355-b211-5a983ba4d42d"
}
```

Response:

```json
{
  "matchSessionId": "ef8aaf65-6dbe-4d1d-b9d9-4a32bdf4b4cb",
  "roomId": 12,
  "status": "Created",
  "king": {
    "id": 15,
    "displayName": "ReyActual"
  },
  "challenger": {
    "id": 27,
    "displayName": "RetadorActual"
  },
  "createdAtUtc": "2026-03-23T20:15:00Z"
}
```

Reglas:

- solo rey, retador o host logico de la sala pueden crearla;
- solo puede existir una sesion `Created` o `Running` por sala;
- el backend valida que `kingUserId` y `challengerUserId` coincidan con el estado real de la sala.

### 2. Marcar sesion como iniciada

`POST /api/rooms/{roomId}/matches/{matchSessionId}/started`

Request:

```json
{
  "emulatorProcessId": 38420,
  "startedAtUtc": "2026-03-23T20:15:07Z"
}
```

Response:

```json
{
  "status": "Running"
}
```

### 3. Heartbeat opcional

`POST /api/rooms/{roomId}/matches/{matchSessionId}/heartbeat`

Request:

```json
{
  "sentAtUtc": "2026-03-23T20:16:00Z"
}
```

Uso:

- opcional en Fase 1;
- util para detectar sesiones abandonadas.

### 4. Reportar resultado final

`POST /api/rooms/{roomId}/matches/{matchSessionId}/complete`

Request manual:

```json
{
  "winnerUserId": 15,
  "loserUserId": 27,
  "resultSource": "ManualSelection",
  "reportedByUserId": 15,
  "endedAtUtc": "2026-03-23T20:22:41Z",
  "evidence": {
    "type": "manual-dialog",
    "emulatorExitDetected": true,
    "emulatorProcessId": 38420,
    "notes": "Resultado confirmado por el rey"
  },
  "idempotencyKey": "12-ef8aaf65-6dbe-4d1d-b9d9-4a32bdf4b4cb-complete-v1"
}
```

Request automatico:

```json
{
  "winnerUserId": 27,
  "loserUserId": 15,
  "resultSource": "AutoDetection",
  "reportedByUserId": 27,
  "endedAtUtc": "2026-03-23T20:22:41Z",
  "evidence": {
    "type": "overlay-parser",
    "confidence": 0.98,
    "rawWinnerText": "PLAYER 2 WIN",
    "frameTimestampUtc": "2026-03-23T20:22:39Z"
  },
  "idempotencyKey": "12-ef8aaf65-6dbe-4d1d-b9d9-4a32bdf4b4cb-complete-v1"
}
```

Response:

```json
{
  "matchSessionId": "ef8aaf65-6dbe-4d1d-b9d9-4a32bdf4b4cb",
  "status": "Completed",
  "message": "Resultado registrado.",
  "nextChallengerId": 34,
  "nextChallengerDisplayName": "JugadorEnCola",
  "roomState": {
    "roomId": 12,
    "status": "Playing",
    "king": {
      "id": 15,
      "displayName": "ReyActual",
      "wins": 18,
      "losses": 4
    },
    "challenger": {
      "id": 34,
      "displayName": "JugadorEnCola",
      "wins": 7,
      "losses": 3
    },
    "queue": [],
    "spectatorCount": 5
  }
}
```

### 5. Cancelar partida

`POST /api/rooms/{roomId}/matches/{matchSessionId}/cancel`

Request:

```json
{
  "reason": "PlayersClosedEmulator",
  "reportedByUserId": 15,
  "endedAtUtc": "2026-03-23T20:22:41Z"
}
```

Response:

```json
{
  "matchSessionId": "ef8aaf65-6dbe-4d1d-b9d9-4a32bdf4b4cb",
  "status": "Cancelled",
  "message": "La partida fue cancelada y no afecta el ranking."
}
```

## Compatibilidad con `report-result`

`POST /api/rooms/{id}/report-result` puede mantenerse como adaptador interno:

- uso administrativo;
- migracion temporal;
- tests rapidos.

Pero el launcher no deberia llamarlo directamente una vez exista `MatchSession`.

## Eventos SignalR recomendados

Ademas de `RoomUpdated` y `YourTurn`, conviene emitir:

- `MatchCreated`
- `MatchStarted`
- `MatchAwaitingResult`
- `MatchCompleted`
- `MatchCancelled`
- `MatchConflict`

Ejemplo de payload para `MatchCompleted`:

```json
{
  "roomId": 12,
  "matchSessionId": "ef8aaf65-6dbe-4d1d-b9d9-4a32bdf4b4cb",
  "winnerUserId": 15,
  "nextChallengerId": 34,
  "reportedAtUtc": "2026-03-23T20:22:43Z"
}
```

## Integracion concreta en el launcher WPF

## Cambios de codigo recomendados

### En `IEmulatorLauncher`

Pasar de metodos `void` a un retorno que permita observar el proceso:

```csharp
public interface IEmulatorLauncher
{
    Process LaunchAsHost(int udpPort, string gameRom);
    Process LaunchAsClient(string hostIp, int udpPort, string gameRom);
}
```

Si se quiere mantener compatibilidad, crear una sobrecarga nueva:

```csharp
public interface IEmulatorLauncher
{
    void LaunchAsHost(int udpPort, string gameRom);
    void LaunchAsClient(string hostIp, int udpPort, string gameRom);
    Process LaunchAsHostTracked(int udpPort, string gameRom);
    Process LaunchAsClientTracked(string hostIp, int udpPort, string gameRom);
}
```

### En `ApiModels.cs`

Agregar DTOs:

- `CreateMatchSessionRequest`
- `CreateMatchSessionResponse`
- `StartMatchSessionRequest`
- `CompleteMatchSessionRequest`
- `CancelMatchSessionRequest`
- `MatchSessionDto`

### En `IApiClient` y `ApiClient`

Agregar metodos:

- `CreateMatchSessionAsync(...)`
- `MarkMatchStartedAsync(...)`
- `CompleteMatchSessionAsync(...)`
- `CancelMatchSessionAsync(...)`

### En `MainWindow.xaml.cs`

Sacar la responsabilidad de `OnGameReady(...)` a un coordinador:

- hoy `OnGameReady(...)` solo espera 3 segundos y abre FBNeo;
- debe pasar a `MatchSessionCoordinator.StartMatchAsync(...)`.

### En UI

Agregar:

- `OverlayWindow`
- `MatchResultDialog`
- indicador visible de `partida en curso` dentro del dashboard.

## Secuencia exacta recomendada

```text
1. SignalR/estado sala confirma rey + challenger
2. Launcher llama POST /api/rooms/{roomId}/matches
3. Launcher abre FBNeo y obtiene ProcessId
4. Launcher llama POST /started
5. Overlay entra en estado InMatch
6. FBNeo termina
7. Launcher abre MatchResultDialog
8. Usuario elige ganador o cancelar
9. Launcher llama POST /complete o /cancel
10. API actualiza match history, rey, cola, ranking
11. API emite RoomUpdated + MatchCompleted
12. Launcher actualiza dashboard y overlay final
```

## Estrategia para automatizar despues

Cuando quieras automatizar la deteccion del ganador, no cambias la UI ni el backend principal. Solo agregas un proveedor de resultado:

```csharp
public interface IMatchResultDetector
{
    Task<DetectedMatchResult?> TryDetectAsync(ActiveMatchSession session, CancellationToken ct);
}
```

Implementaciones posibles:

- lectura de archivo producido por overlay externo;
- OCR sobre captura final;
- parser de texto del overlay Fightcade si realmente expone datos utilizables;
- confirmacion dual entre ambos clientes.

Todas esas opciones terminan llamando el mismo endpoint `POST /complete` con `resultSource = AutoDetection`.

## Recomendacion final de implementacion

Orden recomendado:

1. introducir `MatchSession` en backend;
2. adaptar launcher para obtener y observar `Process` de FBNeo;
3. crear `MatchSessionCoordinator`;
4. crear `MatchResultDialog` manual;
5. crear `OverlayWindow` alineado a la ventana del emulador;
6. conectar `POST /complete` y `POST /cancel`;
7. despues evaluar deteccion automatica del ganador.

Ese orden te da una version operativa rapido y deja lista la base para automatizar sin rehacer el flujo.
