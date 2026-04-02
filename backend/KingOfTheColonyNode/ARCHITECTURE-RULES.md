# Reglas de Arquitectura y Desarrollo

## Proyecto

KingOfTheColony Node Platform

## Objetivo

Definir las reglas obligatorias para construir una plataforma profesional basada en Node.js para el backend y Angular Material para el frontend, con sincronizacion en tiempo real, soporte para PostgreSQL o MongoDB y una arquitectura limpia preparada para crecer sin degradar mantenibilidad.

## Vision del sistema

La plataforma debe soportar:

- autenticacion segura
- perfil de usuario
- billetera y movimientos de creditos
- dashboard del Rey de la Colina
- salas y cola de retadores
- sincronizacion en tiempo real
- ranking e historial
- catalogo de juegos
- modos versus simples, incluyendo Pong

El backend es la fuente oficial de verdad del dominio, de la cola, de los resultados, de la billetera y del estado competitivo.

---

## 1. Principios no negociables

1. La logica de negocio no puede vivir en controladores, gateways websocket ni componentes Angular.
2. El dominio no puede depender de NestJS, Prisma, Mongoose, Angular ni de librerias de transporte.
3. Toda operacion critica debe ser consistente, auditable e idempotente cuando corresponda.
4. Los eventos en tiempo real solo reflejan cambios ya confirmados por el backend.
5. La persistencia debe estar desacoplada para permitir PostgreSQL o MongoDB con el menor impacto posible en application y domain.
6. Cada modulo nuevo debe tener contratos, casos de uso, pruebas y documentacion minima.
7. La seguridad, observabilidad y despliegue forman parte del diseño inicial.
8. La experiencia de usuario debe ser moderna y minimalista, con foco en claridad operativa.

---

## 2. Stack recomendado

### Backend

- Node.js LTS
- TypeScript estricto
- NestJS
- Socket.IO o WebSocket Gateway con NestJS
- JWT + refresh tokens
- Passport para estrategias de autenticacion
- Zod o class-validator para validacion de entrada
- Swagger / OpenAPI
- Prisma para PostgreSQL
- Mongoose o driver oficial para MongoDB
- Pino para logs estructurados
- Jest para unit e integration tests
- Supertest para pruebas HTTP

### Frontend

- Angular
- Angular Material
- Angular CDK
- RxJS
- SCSS
- Angular Router con lazy loading
- state layer coherente, preferiblemente Signals Store o equivalente del equipo

### Plataforma y DevOps

- Docker
- Docker Compose para desarrollo local
- CI/CD en GitHub Actions
- despliegue preferido en Cloud Run para backend y en Firebase Hosting, Cloud Run o Nginx para frontend
- almacenamiento de secretos en Secret Manager o proveedor equivalente

---

## 3. Arquitectura obligatoria

Se adopta Clean Architecture con una organizacion modular por bounded context.

### Capas

- presentation
- application
- domain
- infrastructure

### Reglas por capa

#### domain

- define entidades, value objects, reglas e interfaces
- no importa librerias de infraestructura
- expresa invariantes de negocio

#### application

- contiene casos de uso
- coordina repositorios, servicios de dominio y politicas
- no conoce detalles de HTTP, websockets ni ORM concretos

#### infrastructure

- implementa repositorios PostgreSQL y MongoDB
- implementa seguridad, sockets, cache, logs y servicios externos
- adapta el dominio a librerias reales

#### presentation

- expone REST, websocket gateways y DTOs publicos
- valida entrada y orquesta respuestas
- no decide reglas de negocio criticas

---

## 4. Estructura sugerida

```text
backend/
	KingOfTheColonyNode/
		src/
			main.ts
			app.module.ts
			config/
			shared/
				domain/
				application/
				infrastructure/
				presentation/
			modules/
				auth/
					domain/
					application/
					infrastructure/
					presentation/
				users/
				profile/
				wallet/
				leaderboard/
				king-of-the-hill/
				versus/
				games/
				realtime/
				notifications/
				health/
			database/
				postgres/
				mongodb/
				migrations/
			tests/
				unit/
				integration/
				contract/
				e2e/
		docs/
		scripts/
		Dockerfile
		docker-compose.yml
		package.json
		tsconfig.json
		.env.example
		README.MD
		ARCHITECTURE-RULES.md
```

---

## 5. Modulos obligatorios

### auth

- registro
- login
- refresh token
- logout
- recuperacion de acceso
- proveedores sociales opcionales

### profile

- datos publicos y privados del usuario
- avatar
- nickname
- preferencias
- resumen estadistico

### wallet

- saldo actual
- historial de creditos
- recargas
- consumo por partidas
- trazabilidad completa

### king-of-the-hill

- salas
- rey actual
- challenger actual
- cola
- espectadores
- sesiones de partida
- historial reciente
- actualizacion de resultados

### leaderboard

- ranking general
- ranking por juego
- rachas
- score calculado

### games

- catalogo de juegos soportados
- metadatos de UI
- reglas de juego configurables

### versus

- modos casuales o simples
- lobby basico
- partidas uno contra uno
- soporte inicial para Pong

### realtime

- eventos del dashboard
- actualizaciones de sala
- notificaciones de turno
- cambios de billetera
- cambios de ranking

---

## 6. Persistencia desacoplada

La primera version debe trabajar sobre interfaces de repositorio.

### Contratos minimos

- UserRepository
- ProfileRepository
- WalletRepository
- WalletTransactionRepository
- KingRoomRepository
- MatchSessionRepository
- MatchHistoryRepository
- RankingRepository
- GameCatalogRepository
- VersusMatchRepository

### Regla de interoperabilidad

Los casos de uso no pueden usar Prisma ni Mongoose de forma directa. Solo pueden hablar con interfaces definidas en domain o application.

### Criterio para PostgreSQL

Usar PostgreSQL cuando la prioridad sea:

- consistencia transaccional fuerte
- consultas complejas de ranking
- integridad referencial fuerte
- reporting estructurado

### Criterio para MongoDB

Usar MongoDB cuando la prioridad sea:

- flexibilidad documental
- agregados ricos como sesiones y eventos
- historiales y evidencia con esquema variable
- iteracion rapida sobre modelos de datos

---

## 7. Tiempo real y sincronizacion

### Reglas

1. El backend persiste primero y emite despues.
2. El frontend debe poder reconstruir estado con REST si pierde eventos realtime.
3. Los clientes no deben recalcular el estado oficial del rey, de la cola ni del saldo.
4. Cada evento realtime debe tener payload estable y versionable.
5. Las operaciones de cola, wallet y cierre de partida deben tratarse como flujos criticos.

### Eventos recomendados

- room.created
- room.updated
- queue.updated
- match.created
- match.started
- match.completed
- match.cancelled
- wallet.updated
- leaderboard.updated
- notification.created

---

## 8. Seguridad obligatoria

1. Access token de corta vida.
2. Refresh token con rotacion.
3. Hash de password con Argon2 o bcrypt bien configurado.
4. Rate limiting para auth y endpoints sensibles.
5. Variables de entorno para secretos.
6. Auditoria de cambios sensibles.
7. Validacion de payloads en todos los endpoints.
8. Politicas de autorizacion por roles y ownership.

### Roles minimos

- user
- moderator
- admin

---

## 9. Reglas del frontend Angular Material

La UI debe ser minimalista, moderna y orientada a operacion.

### Criterios visuales

- navegacion clara
- layout limpio y respirado
- tipografia jerarquica
- componentes Material personalizados, no apariencia por defecto sin criterio
- colores neutros con acentos controlados
- dashboard comprensible en menos de 3 segundos

### Navegacion requerida

- Home
- Dashboard Rey de la Colina
- Salas
- Cola y Retos
- Perfil
- Billetera
- Historial
- Ranking
- Juegos Versus
- Pong
- Configuracion
- Ayuda

### Comportamiento del menu

- menu lateral desplegable
- modo compacto y expandido
- indicadores visuales de seccion activa
- resumen rapido del usuario en la cabecera

---

## 10. Dashboard principal

El dashboard del Rey de la Colina es la vista prioritaria del producto.

### Debe mostrar

- rey actual
- challenger actual
- cola de jugadores
- posicion del usuario en la cola
- saldo actual
- racha del rey
- ultimos resultados
- acciones rapidas: retar, espectar, recargar

### Reglas UX

1. El rey actual debe tener prioridad visual.
2. El siguiente challenger debe verse sin hacer scroll.
3. Los cambios realtime deben ser notorios pero no invasivos.
4. El usuario debe saber en todo momento si esta jugando, esperando o espectando.

---

## 11. Calidad y pruebas

### Testing obligatorio

- unit tests para reglas de cola, wallet y ranking
- integration tests para repositorios, auth y sockets
- contract tests para payloads REST y realtime
- e2e para login, cola, match result, wallet y Pong

### Calidad de codigo

1. TypeScript estricto.
2. ESLint y Prettier obligatorios.
3. Prohibido acoplar presentation con ORM.
4. Toda decision relevante debe documentarse.
5. Todo PR debe incluir pruebas o justificacion tecnica.

---

## 12. Despliegue

### Entornos minimos

- local
- dev
- staging
- production

### Reglas de despliegue

1. Backend y frontend se despliegan de forma independiente.
2. El backend debe ser stateless.
3. Las conexiones a base de datos y secretos se inyectan por entorno.
4. Se deben publicar health checks y readiness checks.
5. Toda build debe generar artefactos versionados.

### Recomendacion de infraestructura

- Backend Node: Cloud Run
- Front Angular: Firebase Hosting o Cloud Run con Nginx
- PostgreSQL: Cloud SQL, Supabase o Neon
- MongoDB: Atlas
- Secretos: Secret Manager
- Logs y metricas: Cloud Logging + Error Reporting o equivalente

---

## 13. Hoja de ruta sugerida

### Fase 1

- bootstrap del monorepo o estructura de proyectos
- auth
- profile
- wallet base
- king-of-the-hill base
- realtime base
- dashboard inicial

### Fase 2

- ranking avanzado
- historial
- administracion basica
- notificaciones
- endurecimiento de seguridad

### Fase 3

- juegos versus
- Pong
- soporte dual de persistencia refinado
- observabilidad avanzada
- optimizacion de costos y rendimiento

---

## 14. Criterios de aceptacion de arquitectura

La arquitectura esta bien aplicada si:

1. El dominio puede probarse sin levantar NestJS.
2. Se puede cambiar la persistencia sin reescribir los casos de uso.
3. El frontend consume contratos estables.
4. El dashboard se mantiene sincronizado por REST + realtime.
5. El sistema puede crecer hacia nuevos juegos sin romper el modulo principal del Rey de la Colina.
