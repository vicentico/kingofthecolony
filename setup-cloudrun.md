## Despliegue de KingOfTheColonyApi en Google Cloud Run

Esta guía está adaptada a la estructura real de este repositorio:

- `GGPOLauncher/` es una app WPF para Windows y no participa en el despliegue a Cloud Run.
- `KingOfTheColonyApi/` es el único proyecto que se compila, containeriza y despliega.
- El Docker build usa contexto en la raíz del repo porque `nuget.config` vive en la raíz.

### Archivos agregados para este flujo

- `KingOfTheColonyApi/Dockerfile`
- `.dockerignore`
- `docker-compose.yml`
- `.env.example`
- `.github/workflows/deploy-api-cloudrun.yml`

### Variables de entorno que consume la API

Requeridas:

- `Jwt__Key`
- `ConnectionStrings__DefaultConnection`

Opcionales:

- `ConnectionStrings__MigrationConnection`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Google__Enabled`
- `Google__ClientId`
- `Google__ClientSecret`

### Ejecución local con Docker Compose

1. Crea un archivo `.env` en la raíz usando `.env.example` como base.
2. Define `Jwt__Key` y `ConnectionStrings__DefaultConnection`.
3. Si quieres aplicar migraciones al arrancar, define también `ConnectionStrings__MigrationConnection` con una conexión directa.
4. Levanta la API:

```bash
docker compose up --build api
```

La API quedará disponible en `http://localhost:8080`.

### Por qué el workflow solo compila la API

El repositorio contiene una solución con dos proyectos:

- `GGPOLauncher.sln` incluye una app WPF que requiere Windows.
- GitHub Actions para Cloud Run corre sobre `ubuntu-latest`.

Por eso el workflow compila únicamente `KingOfTheColonyApi/KingOfTheColonyApi.csproj`.

### Variables y secretos que debes configurar en GitHub

Repository variables:

- `GCP_PROJECT_ID`
- `GCP_REGION`
- `GAR_REPOSITORY`
- `CLOUD_RUN_SERVICE`

Repository secrets:

- `GCP_WORKLOAD_IDENTITY_PROVIDER`
- `GCP_SERVICE_ACCOUNT_EMAIL`

### Crear Artifact Registry

```bash
gcloud artifacts repositories create REPOSITORY_NAME \
   --repository-format=docker \
   --location=REGION \
   --description="Docker images for KingOfTheColonyApi"
```

### Crear secretos en Secret Manager

```bash
echo -n "TU_CLAVE_LARGA_Y_SECRETA" | gcloud secrets create jwt-key \
   --data-file=-

echo -n "postgresql://postgres.gtuwxzlzlbhanbcsbhny:TU_PASSWORD@aws-0-us-west-2.pooler.supabase.com:6543/postgres?sslmode=require" | gcloud secrets create default-connection \
   --data-file=-

echo -n "postgresql://postgres:TU_PASSWORD@db.gtuwxzlzlbhanbcsbhny.supabase.co:5432/postgres?sslmode=require" | gcloud secrets create migration-connection \
   --data-file=-
```

Si los secretos ya existen:

```bash
echo -n "TU_CLAVE_LARGA_Y_SECRETA" | gcloud secrets versions add jwt-key \
   --data-file=-

echo -n "postgresql://postgres.gtuwxzlzlbhanbcsbhny:TU_PASSWORD@aws-0-us-west-2.pooler.supabase.com:6543/postgres?sslmode=require" | gcloud secrets versions add default-connection \
   --data-file=-

echo -n "postgresql://postgres:TU_PASSWORD@db.gtuwxzlzlbhanbcsbhny.supabase.co:5432/postgres?sslmode=require" | gcloud secrets versions add migration-connection \
   --data-file=-
```

### Crear service account para GitHub Actions

```bash
gcloud iam service-accounts create github-cloud-run \
   --display-name="GitHub Cloud Run Deploy"
```

Asigna permisos mínimos:

```bash
gcloud projects add-iam-policy-binding PROJECT_ID \
   --member="serviceAccount:github-cloud-run@PROJECT_ID.iam.gserviceaccount.com" \
   --role="roles/run.admin"

gcloud projects add-iam-policy-binding PROJECT_ID \
   --member="serviceAccount:github-cloud-run@PROJECT_ID.iam.gserviceaccount.com" \
   --role="roles/artifactregistry.writer"

gcloud projects add-iam-policy-binding PROJECT_ID \
   --member="serviceAccount:github-cloud-run@PROJECT_ID.iam.gserviceaccount.com" \
   --role="roles/secretmanager.secretAccessor"

gcloud projects add-iam-policy-binding PROJECT_ID \
   --member="serviceAccount:github-cloud-run@PROJECT_ID.iam.gserviceaccount.com" \
   --role="roles/iam.serviceAccountUser"
```

### Habilitar Workload Identity Federation para GitHub

1. Crea un Workload Identity Pool y su Provider para GitHub.
2. Permite que el repositorio `vicentico/kingofthecolony` impersonifique la service account.
3. Guarda el provider completo en `GCP_WORKLOAD_IDENTITY_PROVIDER`.
4. Guarda el correo de la cuenta en `GCP_SERVICE_ACCOUNT_EMAIL`.

Referencia de member principal:

```text
principalSet://iam.googleapis.com/projects/PROJECT_NUMBER/locations/global/workloadIdentityPools/POOL_ID/attribute.repository/vicentico/kingofthecolony
```

### Despliegue a Cloud Run

El workflow publica imágenes con este formato:

```text
REGION-docker.pkg.dev/PROJECT_ID/GAR_REPOSITORY/kingofthecolony-api:GITHUB_SHA
```

Y despliega públicamente el servicio de Cloud Run con `--allow-unauthenticated`.

### Nota sobre el proveedor de base de datos

La API ya no queda configurada para SQLite. Ahora usa PostgreSQL mediante `Npgsql` y espera una cadena en `ConnectionStrings__DefaultConnection`.

La API acepta tanto el formato nativo de Npgsql como URLs `postgresql://...`, `postgres://...` y `jdbc:postgresql://...`. Si recibe una URL, la convierte internamente antes de crear la conexión.

Usa `ConnectionStrings__DefaultConnection` para el runtime de la app. En Supabase, esto puede apuntar al pooler `aws-0-us-west-2.pooler.supabase.com:6543`.

Usa `ConnectionStrings__MigrationConnection` para `db.Database.Migrate()`. En Supabase, esto debería apuntar a la conexión directa `db.gtuwxzlzlbhanbcsbhny.supabase.co:5432` si tu entorno soporta ese acceso.

Si no defines `ConnectionStrings__MigrationConnection`, la app arranca sin ejecutar migraciones automáticas.
