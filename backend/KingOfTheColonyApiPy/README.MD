# Contexto de Rediseño Backend

## Proyecto

King of the Colony

## Objetivo

Rediseñar el backend actual en .NET como una nueva API en Python, siguiendo buenas prácticas de arquitectura, testing, observabilidad y seguridad, usando MongoDB como persistencia principal.

## Motivación

El backend actual implementa autenticación, créditos, ranking, salas, cola de retadores, sesiones de partida y mensajería en tiempo real. La migración a Python busca:

- mejorar mantenibilidad
- separar mejor capas y responsabilidades
- facilitar evolución del dominio
- preparar una base más flexible para eventos, sesiones y auditoría
- reemplazar el modelo relacional actual por documentos adecuados al dominio

## Estado Actual

El backend actual está en KingOfTheColonyApi y usa:

- ASP.NET Core
- JWT Bearer auth
- SignalR para tiempo real
- Entity Framework Core
- Postgres en runtime actual y diseño previo documentado desde SQLite
- lógica de negocio concentrada en servicios, especialmente GameRoomService

## Capacidades actuales a preservar

1. Registro y login por email/password
2. Login con Google opcional
3. Perfil de usuario
4. Créditos y historial de créditos
5. Ranking paginado
6. Listado y detalle de salas
7. Crear sala
8. Entrar como espectador
9. Entrar a la cola
10. Reportar resultado simple
11. Flujo moderno de MatchSession:

- crear sesión
- marcar inicio
- completar
- cancelar
- enviar a revisión

12. Tiempo real por sala
13. Health check y documentación API

## Fuentes de verdad actuales

- docs/ARCHITECTURE.md
- docs/DATA-MODEL.md
- KingOfTheColonyApi/Program.cs
- KingOfTheColonyApi/Controllers
- KingOfTheColonyApi/Services
- KingOfTheColonyApi/Models
- KingOfTheColonyApi/Models/Dto/Dtos.cs

## Reglas de negocio críticas

1. Un usuario no puede estar dos veces en la cola de la misma sala
2. El rey no puede entrar a su propia cola
3. El retador actual no puede duplicarse
4. Entrar a cola consume 1 crédito
5. Si no hay retador, el usuario pasa a ser Challenger directamente
6. Si un espectador entra a cola, deja de ser espectador
7. Solo jugadores activos pueden iniciar, cancelar, revisar o completar una MatchSession
8. Una MatchSession cerrada no debe mutar salvo control de idempotencia
9. El ranking depende de victorias, derrotas y racha, no de un score persistido fijo
10. Una partida cancelada o en revisión no debe impactar MatchHistory ni ranking
11. Debe existir idempotencia al completar MatchSession

## Decisión tecnológica propuesta

- Framework API: FastAPI
- Validación y contratos: Pydantic
- ODM o capa de acceso: Beanie o PyMongo con repositorios explícitos
- Base de datos: MongoDB
- Autenticación: JWT
- Tiempo real:
  - opción A: WebSocket nativo con FastAPI
  - opción B: Socket.IO
  - opción C: mantener polling temporal si se busca compatibilidad rápida
- Background tasks: Celery o arq solo si luego aparece necesidad real
- Testing: pytest
- Quality: ruff, black, mypy
- Configuración: pydantic-settings
- Contenedores: Docker + docker-compose

## Arquitectura propuesta

### Capas

- api
- application
- domain
- infrastructure
- tests

### Responsabilidades

- api: routers, schemas, auth dependencies, websocket handlers
- application: casos de uso
- domain: entidades, value objects, reglas, servicios de dominio
- infrastructure: mongo repositories, jwt provider, password hasher, event bus adapters
- tests: unit, integration, contract

## Estructura sugerida

backend/
KingOfTheColonyApiPy/
app/
api/
routers/
dependencies/
websocket/
application/
auth/
credits/
rooms/
ranking/
match_sessions/
domain/
users/
rooms/
matches/
credits/
common/
infrastructure/
db/
repositories/
security/
realtime/
logging/
schemas/
main.py
config.py
tests/
unit/
integration/
contract/
scripts/
Dockerfile
docker-compose.yml
pyproject.toml
README.md

## Modelo documental propuesto en MongoDB

### users

- \_id
- email
- display_name
- avatar_url
- auth:
  - password_hash
  - google_id
  - providers
- credits
- stats:
  - wins
  - losses
  - best_streak
  - current_streak
- created_at

### rooms

- \_id
- name
- status
- king_user_id
- challenger_user_id
- host_ip
- udp_port
- game_rom
- queue:
  - user_id
  - position
  - joined_at
- spectators:
  - user_id
  - joined_at
- created_at
- updated_at

### match_sessions

- \_id
- room_id
- king_user_id
- challenger_user_id
- winner_user_id
- loser_user_id
- status
- game_rom
- result_source
- launch_source
- client_instance_id
- evidence_payload
- result_reason
- last_idempotency_key
- emulator_process_id
- created_by_user_id
- reported_by_user_id
- created_at_utc
- started_at_utc
- ended_at_utc
- reported_at_utc

### match_history

- \_id
- room_id
- winner_id
- loser_id
- played_at

### credit_transactions

- \_id
- user_id
- amount
- type
- description
- created_at

## Índices MongoDB recomendados

- users.email unique
- users.auth.google_id unique sparse
- rooms.status
- rooms.king_user_id
- rooms.challenger_user_id
- match_sessions.room_id + status
- match_sessions.last_idempotency_key
- credit_transactions.user_id + created_at desc
- match_history.winner_id + played_at desc
- match_history.loser_id + played_at desc

## Compatibilidad de API

La primera versión en Python debe mantener estos contratos funcionales:

- auth
- profile
- ranking
- credits
- rooms
- match sessions
  La compatibilidad puede ser:
- total a nivel payload, o
- versionada bajo /api/v2 si se decide mejorar contratos

## Estrategia de migración

- mantener .NET vivo mientras Python alcanza paridad
- migrar endpoint por endpoint
- validar respuestas contra contratos actuales
- cambiar el cliente WPF por configuración, no por hardcode
- activar tráfico parcial o entorno staging primero

## Requisitos no funcionales

- logs estructurados
- manejo consistente de errores
- tracing básico
- documentación OpenAPI
- validación estricta de entrada
- tests unitarios e integración
- CI con lint, tipos y tests
- configuración por variables de entorno
- secretos fuera del repositorio

## Riesgos

1. Pasar de relacional a documental puede esconder invariantes si no se diseñan bien las actualizaciones atómicas
2. Cola, créditos y cierre de partida requieren consistencia
3. Tiempo real y grupos por sala deben definirse antes de tocar el cliente
4. Ranking puede degradarse si se calcula sin estrategia de lectura
5. Idempotencia debe modelarse desde el inicio

## Criterios de éxito

1. El cliente puede autenticarse y listar salas
2. Puede crear sala y unirse a cola
3. Puede iniciar y completar MatchSession
4. El ranking y créditos siguen siendo correctos
5. Los eventos en tiempo real siguen funcionando
6. El backend tiene cobertura de pruebas de los casos críticos
