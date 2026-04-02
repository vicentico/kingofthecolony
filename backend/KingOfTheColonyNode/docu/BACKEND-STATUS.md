# Estado Actual del Backend Node

## Resumen

El proyecto `KingOfTheColonyNode` implementa una API en NestJS con arquitectura por capas:

- `presentation`: controladores, DTOs y guards
- `application`: casos de uso y mapeadores de respuesta
- `domain`: modelos, contratos y errores de negocio
- `infrastructure`: repositorios en memoria y PostgreSQL

La aplicacion expone actualmente modulos funcionales para:

- autenticacion
- perfil
- wallet
- king-of-the-hill
- health check

## Persistencia

La seleccion de persistencia depende de configuracion:

- `DATABASE_PROVIDER=postgres` y `POSTGRES_URL` habilitan repositorios PostgreSQL
- cualquier otro caso usa repositorios en memoria

Esto permite:

- desarrollo rapido local
- pruebas e2e sin infraestructura externa
- validacion posterior sobre PostgreSQL real

## Modulos implementados

### Auth

Capacidades:

- registro por email y password
- login con JWT
- refresh token con rotacion
- logout
- consulta de usuario autenticado

Reglas relevantes:

- email normalizado a minusculas
- password hasheado con `bcryptjs`
- usuario nuevo inicia con `3` creditos

Endpoints:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`

### Profile

Capacidades:

- consultar perfil propio
- consultar perfil publico por id
- actualizar `displayName`, `avatarUrl` y `bio`

Endpoints:

- `GET /api/profile/me`
- `GET /api/profile/:id`
- `PATCH /api/profile/me`

### Wallet

Capacidades:

- consultar saldo actual
- listar transacciones
- agregar creditos
- consumir creditos

Tipos de transaccion usados hoy:

- `Purchase`
- `GameEntry`

Endpoints:

- `GET /api/wallet/me`
- `GET /api/wallet/me/transactions`
- `POST /api/wallet/me/top-up`
- `POST /api/wallet/me/consume`

### King Of The Hill

Capacidades:

- crear salas
- listar salas
- consultar estado de una sala
- entrar a cola
- asignar challenger actual
- crear sesiones de partida
- iniciar sesiones de partida
- completar sesiones con idempotencia
- promover ganador a rey
- avanzar cola automaticamente

Estados de sala actuales:

- `WaitingChallenger`
- `ReadyToPlay`
- `Playing`

Estados de match session:

- `Created`
- `Running`
- `Completed`

Endpoints:

- `GET /api/king-of-the-hill/rooms`
- `GET /api/king-of-the-hill/rooms/:roomId`
- `POST /api/king-of-the-hill/rooms`
- `POST /api/king-of-the-hill/rooms/:roomId/join-queue`
- `POST /api/king-of-the-hill/rooms/:roomId/match-sessions`
- `POST /api/king-of-the-hill/rooms/:roomId/match-sessions/:matchSessionId/start`
- `POST /api/king-of-the-hill/rooms/:roomId/match-sessions/:matchSessionId/complete`

## Base de datos PostgreSQL

El bootstrap SQL actual crea estas tablas principales:

- `auth_users`
- `wallet_transactions`
- `king_rooms`
- `king_room_queue_entries`
- `king_match_sessions`
- `king_match_history`

Script de aplicacion de esquema:

- `npm run db:schema:auth:postgres`

## Validacion actual

Se validaron correctamente:

- `npm run build`
- `npm run test:e2e`

Suites e2e cubiertas:

- `health`
- `auth`
- `profile`
- `wallet`
- `king-of-the-hill`

## Trabajo pendiente natural

Siguientes areas candidatas:

- realtime por eventos de sala y match
- ranking e historial competitivo derivado de resultados
- espectadores
- cancelacion y revision de match sessions
- validacion sobre PostgreSQL real