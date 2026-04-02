# KingOfTheColony Node

Backend en Node.js y plataforma frontend en Angular Material para King of the Colony, diseñada para soportar autenticacion, billetera, dashboard del Rey de la Colina, ranking, sincronizacion en tiempo real y expansion a modos versus simples como Pong.

## Objetivo

Este proyecto define una base de trabajo profesional para construir una solucion escalable con:

- backend modular en Node.js
- frontend Angular Material moderno y minimalista
- soporte para PostgreSQL o MongoDB
- realtime para cola, dashboard y eventos de partida
- despliegue reproducible en cloud

La guia normativa del proyecto vive en [ARCHITECTURE-RULES.md](./ARCHITECTURE-RULES.md).

---

## Alcance funcional inicial

La primera etapa debe cubrir:

- autenticacion y autorizacion
- perfil de usuario
- billetera y creditos
- dashboard del Rey de la Colina
- salas, cola y ranking
- sincronizacion en tiempo real
- historial de partidas
- catalogo de juegos
- modulo versus inicial con Pong

### Endpoints base de auth

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`

### Endpoints base de profile

- `GET /api/profile/me`
- `GET /api/profile/:id`
- `PATCH /api/profile/me`

### Endpoints base de wallet

- `GET /api/wallet/me`
- `GET /api/wallet/me/transactions`
- `POST /api/wallet/me/top-up`
- `POST /api/wallet/me/consume`

### Endpoints base de king-of-the-hill

- `GET /api/king-of-the-hill/rooms`
- `GET /api/king-of-the-hill/rooms/:roomId`
- `POST /api/king-of-the-hill/rooms`
- `POST /api/king-of-the-hill/rooms/:roomId/join-queue`
- `POST /api/king-of-the-hill/rooms/:roomId/match-sessions`
- `POST /api/king-of-the-hill/rooms/:roomId/match-sessions/:matchSessionId/start`
- `POST /api/king-of-the-hill/rooms/:roomId/match-sessions/:matchSessionId/complete`

Los modulos actuales de `auth` y `profile` usan JWT de acceso, refresh token rotado y seleccionan persistencia segun configuracion:

- PostgreSQL real cuando `DATABASE_PROVIDER=postgres` y `POSTGRES_URL` esta definido
- repositorio en memoria como fallback para pruebas o bootstrap rapido

El modulo `profile` ya permite leer y actualizar `displayName`, `avatarUrl` y `bio` del usuario autenticado.

El modulo `wallet` ya permite:

- consultar saldo actual
- consultar movimientos
- agregar creditos
- consumir creditos

El modulo `king-of-the-hill` ya permite:

- crear salas con un rey inicial
- listar salas disponibles
- consultar el estado de una sala
- asignar challenger actual o cola de retadores
- consumir un credito al entrar a la cola o tomar el lugar de challenger
- crear sesiones de partida entre rey y challenger actual
- iniciar la sesion y marcar la sala como `Playing`
- cerrar la sesion con ganador y perdedor
- promover al ganador como nuevo rey y avanzar la cola automaticamente

Los usuarios nuevos arrancan con `3` creditos para alinearse con el comportamiento actual del backend .NET.

---

## Stack sugerido

### Backend

- Node.js LTS
- TypeScript
- NestJS
- Socket.IO o WebSocket Gateway
- Prisma para PostgreSQL
- Mongoose o driver oficial para MongoDB
- JWT + refresh tokens
- Pino
- Jest

### Frontend

- Angular
- Angular Material
- Angular CDK
- RxJS
- SCSS

### Infraestructura

- Docker
- Docker Compose
- GitHub Actions
- Cloud Run
- Firebase Hosting o Nginx para Angular
- PostgreSQL gestionado o MongoDB Atlas

---

## Estructura recomendada

```text
backend/
	KingOfTheColonyNode/
		src/
		docs/
		scripts/
		tests/
		Dockerfile
		docker-compose.yml
		package.json
		tsconfig.json
		tsconfig.build.json
		nest-cli.json
		eslint.config.mjs
		jest.config.cjs
		.prettierrc
		.env.example
		README.MD
		ARCHITECTURE-RULES.md

frontend/
	king-of-the-colony-web/
		src/
		angular.json
		package.json
```

Si se prefiere un monorepo, puede organizarse tambien como `apps/backend` y `apps/frontend`, pero la separacion de capas y modulos sigue siendo obligatoria.

---

## Modulos del producto

### Home

- resumen del usuario
- acceso rapido a jugar
- estado del rey actual
- actividad reciente

### Dashboard Rey de la Colina

- rey actual
- challenger actual
- cola en tiempo real
- ultimos resultados
- saldo del usuario
- acciones: retar, espectar, recargar

### Perfil

- avatar
- nickname
- estadisticas
- preferencias

### Billetera

- saldo actual
- historial de movimientos
- recargas
- consumo por partida

### Ranking

- ranking general
- ranking por juego
- rachas y score

### Juegos Versus

- lista de juegos disponibles
- acceso a Pong
- historial casual basico

### Pong

- lobby simple
- crear partida
- unirse a partida
- resultado basico

---

## Realtime

El backend debe sincronizar como minimo:

- actualizaciones de sala
- cambios en cola
- cambio de rey
- siguiente challenger
- movimientos de billetera relevantes
- notificaciones de turno

Todos los eventos realtime deben tener respaldo por endpoints REST de consulta para rehidratacion del estado.

---

## Persistencia

El sistema debe poder soportar una de estas estrategias:

### PostgreSQL

Adecuado para:

- operaciones con consistencia fuerte
- wallet y transacciones
- ranking relacional
- consultas estructuradas

### MongoDB

Adecuado para:

- sesiones y eventos con esquema flexible
- historiales enriquecidos
- agregados documentales

La seleccion concreta debe quedar escondida detras de repositorios. El dominio no debe acoplarse a la tecnologia de persistencia. crear script para la creación de cada una de las bases de datos.

### Scripts de bootstrap de base de datos

Este proyecto ya define dos scripts Node para crear o inicializar la base elegida:

- `scripts/create-postgres-database.mjs`
- `scripts/create-mongodb-database.mjs`
- `scripts/apply-postgres-auth-schema.mjs`

Ambos scripts estan pensados para ejecutarse desde variables de entorno y pueden usarse en local, CI o tareas administrativas de staging.

### Variables para PostgreSQL

```env
POSTGRES_ADMIN_URL=postgresql://postgres:postgres@localhost:5432/postgres
POSTGRES_DB_NAME=kingofthecolony_node
POSTGRES_APP_USER=kingofthecolony_app
POSTGRES_APP_PASSWORD=change_me
```

### Variables para MongoDB

```env
MONGODB_ADMIN_URL=mongodb://root:root@localhost:27017/admin
MONGODB_DB_NAME=kingofthecolony_node
MONGODB_BOOTSTRAP_COLLECTION=app_bootstrap
MONGODB_APP_USER=kingofthecolony_app
MONGODB_APP_PASSWORD=change_me
```

### Ejecucion de los scripts

PostgreSQL:

```bash
node ./scripts/create-postgres-database.mjs
node ./scripts/apply-postgres-auth-schema.mjs
```

MongoDB:

```bash
node ./scripts/create-mongodb-database.mjs
```

### Recomendacion operativa

- usar el script de PostgreSQL para crear la base y el usuario de aplicacion antes de correr Prisma
- aplicar `db:schema:auth:postgres` para crear la tabla `auth_users` antes de usar persistencia real del modulo auth
- usar el script de MongoDB para garantizar la creacion inicial de la base, la coleccion de bootstrap y el usuario con permisos `readWrite`
- no ejecutar estos scripts en cada arranque de produccion; usarlos como bootstrap controlado

---

## Variables de entorno sugeridas

```env
NODE_ENV=development
PORT=3000
APP_NAME=kingofthecolony-node
API_PREFIX=/api

JWT_ACCESS_SECRET=
JWT_REFRESH_SECRET=
JWT_ACCESS_TTL=15m
JWT_REFRESH_TTL=7d
JWT_ISSUER=kingofthecolony-node
JWT_AUDIENCE=kingofthecolony-clients

DATABASE_PROVIDER=postgres
POSTGRES_URL=
MONGODB_URL=

POSTGRES_ADMIN_URL=
POSTGRES_DB_NAME=
POSTGRES_APP_USER=
POSTGRES_APP_PASSWORD=

MONGODB_ADMIN_URL=
MONGODB_DB_NAME=
MONGODB_BOOTSTRAP_COLLECTION=app_bootstrap
MONGODB_APP_USER=
MONGODB_APP_PASSWORD=

REDIS_URL=

CORS_ORIGIN=http://localhost:4200
LOG_LEVEL=info
```

`DATABASE_PROVIDER` debe aceptar al menos `postgres` o `mongodb`.

---

## Desarrollo local

### Requisitos

- Node.js LTS
- npm o pnpm
- Docker Desktop

### Flujo sugerido

1. Crear `.env` a partir de `.env.example`.
2. Levantar dependencias con Docker Compose.
3. Ejecutar el script de bootstrap de la base elegida.
4. Instalar dependencias del backend.
5. Ejecutar migraciones o bootstrap de esquema.
6. Levantar backend en modo desarrollo.
7. Levantar frontend Angular.

### Comandos de referencia

```bash
docker compose up -d
npm install
npm run db:create:postgres
npm run db:schema:auth:postgres
npm run start:dev
```

Si se usa MongoDB en lugar de PostgreSQL:

```bash
docker compose up -d
npm install
npm run db:create:mongodb
npm run start:dev
```

Health check local esperado:

```text
GET http://localhost:3000/api/health
```

Para frontend Angular:

```bash
npm install
ng serve
```

---

## Despliegue recomendado

### Backend Node

Desplegar como contenedor stateless en Cloud Run.

Requisitos:

- Dockerfile multi-stage
- variables de entorno inyectadas por entorno
- health check publico o interno
- logs estructurados a stdout
- secretos desde Secret Manager

### Frontend Angular

Opciones recomendadas:

- Firebase Hosting
- Cloud Run sirviendo archivos estaticos con Nginx

### Base de datos

Opciones recomendadas:

- PostgreSQL: Cloud SQL, Supabase, Neon
- MongoDB: Atlas

### Bootstrap de base en despliegue

Los scripts de bootstrap deben ejecutarse de manera controlada, preferiblemente como paso administrativo o job independiente antes del primer despliegue funcional.

Reglas:

- no depender del arranque del contenedor para crear bases de datos
- en PostgreSQL, crear la base y el usuario antes de correr migraciones
- en MongoDB, crear la base, la coleccion de bootstrap y el usuario antes de habilitar trafico real
- guardar las credenciales administrativas fuera del runtime normal de la aplicacion

### CI/CD

El pipeline debe ejecutar:

1. lint
2. typecheck
3. unit tests
4. integration tests criticos
5. build
6. deploy a staging
7. aprobacion o merge para production

---

## Plan de desarrollo paso a paso

### Fase 0. Fundacion tecnica

1. Crear el proyecto backend con NestJS y TypeScript.
2. Definir estructura por modulos y capas.
3. Crear configuracion centralizada y manejo de entornos.
4. Crear scripts de bootstrap para PostgreSQL y MongoDB.
5. Configurar lint, format, test y CI basicos.
6. Crear Dockerfile y docker-compose local.

Entregable: esqueleto ejecutable con health check y pipeline minimo.

### Fase 1. Identidad y usuario

1. Implementar auth con JWT y refresh token.
2. Implementar registro, login, logout y perfil.
3. Agregar roles base.
4. Exponer documentacion OpenAPI.

Entregable: usuario autenticado puede iniciar sesion y consultar su perfil.

### Fase 2. Billetera

1. Crear modelo de wallet y transacciones.
2. Implementar saldo actual e historial.
3. Implementar recargas controladas.
4. Registrar auditoria de movimientos.

Entregable: usuario puede ver y gestionar creditos.

### Fase 3. Rey de la Colina base

1. Implementar salas y dashboard.
2. Implementar rey, challenger y cola.
3. Implementar reglas de ingreso a cola.
4. Implementar ranking y resultados base.

Entregable: dashboard funcional con estado persistente.

### Fase 4. Tiempo real

1. Implementar gateway websocket.
2. Emitir eventos de sala, cola y ranking.
3. Rehidratar estado desde REST.
4. Agregar notificaciones de turno.

Entregable: dashboard sincronizado sin refresh manual.

### Fase 5. Frontend Angular Material

1. Crear shell visual con menu lateral desplegable.
2. Crear Home y Dashboard Rey de la Colina.
3. Crear Perfil, Billetera, Ranking e Historial.
4. Integrar auth, realtime y estados de carga.

Entregable: experiencia web moderna, minimalista y operativa.

### Fase 6. Juegos versus y Pong

1. Crear modulo games.
2. Crear modulo versus.
3. Agregar Pong como juego simple inicial.
4. Integrar lobby y resultados basicos.

Entregable: plataforma extensible a juegos casuales.

### Fase 7. Hardening y produccion

1. Endurecer seguridad.
2. Agregar observabilidad y metricas.
3. Mejorar cobertura de pruebas.
4. Configurar staging y production.
5. Ejecutar pruebas de carga basicas.

Entregable: primera version apta para despliegue controlado.

---

## Definition of Done por historia

Una historia se considera terminada cuando:

1. Tiene caso de uso implementado.
2. Tiene validacion de entrada y salida.
3. Tiene pruebas apropiadas.
4. Tiene logging suficiente.
5. No rompe la separacion de capas.
6. Tiene contrato documentado si expone API o realtime.

---

## Riesgos principales

1. Mezclar logica de negocio con NestJS y perder separacion real.
2. Acoplar casos de uso a Prisma o Mongoose demasiado pronto.
3. Confiar en realtime sin estrategia de rehidratacion.
4. Hacer dashboards sobrecargados y poco operativos.
5. Introducir soporte dual de base de datos sin contratos claros.

---

## Siguiente paso recomendado

1. Crear el esqueleto NestJS del backend.
2. Crear el shell Angular Material con menu lateral.
3. Implementar auth y profile como primera entrega vertical.
