# Flujo de Match Sessions

## Objetivo

Documentar el flujo actual de sesiones de partida dentro del modulo `king-of-the-hill`.

## Precondiciones

Para crear una sesion de partida se requiere:

- una sala existente
- un `king` asignado
- un `challenger` actual asignado
- que no exista otra sesion activa en la misma sala

Una sesion activa es cualquier sesion con estado:

- `Created`
- `Running`

## Flujo completo

### 1. Crear sala

`POST /api/king-of-the-hill/rooms`

Resultado:

- se crea una sala con el usuario autenticado como `king`
- el estado inicial es `WaitingChallenger`

### 2. Entrar a cola

`POST /api/king-of-the-hill/rooms/:roomId/join-queue`

Reglas:

- el rey no puede entrar a su propia cola
- el challenger actual no puede volver a entrar
- un mismo usuario no puede duplicarse en la cola
- entrar consume `1` credito y registra una transaccion `GameEntry`

Comportamiento:

- si no hay challenger, el usuario pasa directo a `challenger`
- si ya hay challenger, el usuario entra a la cola ordenada por posicion

### 3. Crear match session

`POST /api/king-of-the-hill/rooms/:roomId/match-sessions`

Reglas:

- solo el `king` o el `challenger` actual pueden crearla
- los ids enviados deben coincidir con el estado real de la sala
- no puede existir otra sesion activa

Resultado:

- se crea una sesion en estado `Created`

### 4. Iniciar match session

`POST /api/king-of-the-hill/rooms/:roomId/match-sessions/:matchSessionId/start`

Reglas:

- solo un jugador activo de esa sesion puede iniciarla
- una sesion `Completed` no puede reabrirse

Resultado:

- la sesion pasa a `Running`
- la sala pasa a `Playing`
- opcionalmente se guarda `emulatorProcessId`

### 5. Completar match session

`POST /api/king-of-the-hill/rooms/:roomId/match-sessions/:matchSessionId/complete`

Reglas:

- solo un jugador activo puede reportar resultado
- `winnerUserId` y `loserUserId` deben ser participantes de la sesion
- ganador y perdedor no pueden ser el mismo usuario

Resultado:

- la sesion pasa a `Completed`
- se guardan ganador, perdedor y origen del resultado
- el ganador se convierte en nuevo `king`
- el primer usuario de la cola pasa a ser nuevo `challenger`, si existe
- la cola restante se reindexa
- si no queda cola, la sala vuelve a `WaitingChallenger`
- si queda challenger nuevo, la sala queda en `ReadyToPlay`

## Idempotencia

La finalizacion soporta replay idempotente usando `idempotencyKey`.

Comportamiento:

- si una sesion ya fue completada con la misma clave, la API responde el mismo estado con `idempotentReplay=true`
- si la sesion ya fue completada con otra clave o sin coincidencia, se rechaza la mutacion

Esto protege contra:

- reintentos de cliente
- duplicacion por red
- reenvio accidental del cierre de partida

## Persistencia

### En memoria

Se usa para pruebas y desarrollo rapido.

Mantiene:

- mapa de salas
- mapa de sesiones
- promocion y avance de cola en memoria

### PostgreSQL

Se usa cuando `DATABASE_PROVIDER=postgres`.

Mantiene:

- `king_rooms`
- `king_room_queue_entries`
- `king_match_sessions`
- `king_match_history`

El repositorio PostgreSQL usa transacciones para:

- bloquear fila de sala
- bloquear sesion activa
- descontar creditos al entrar a cola
- actualizar estado de sala
- registrar historial de partida
- promover rey y avanzar cola de forma consistente

## Casos cubiertos por pruebas e2e

La suite actual valida:

- creacion de sala
- asignacion de challenger
- ingreso a cola
- consumo de creditos
- creacion de match session
- inicio de match session
- transicion de sala a `Playing`
- cierre con ganador y perdedor
- promocion del ganador a rey
- avance del primer jugador en cola a challenger
- replay idempotente de finalizacion
