# Despliegue y operación

Notas para quien despliega u opera backend-asa. Para evaluar el proyecto basta con el [README](../README.md).

## Configuración

| Variable | Qué hace |
| --- | --- |
| `ConnectionStrings__database-asa` | Cadena de conexión a SQL Server |
| `Jwt__Key` | Clave de firma del JWT (32+ caracteres). Sin ella la API no arranca |
| `Seed__AdminPassword`, `Seed__LectorPassword` | Contraseñas de los usuarios demo (ver [Seed](#seed-y-usuarios-demo)) |
| `Database__MigrateOnStartup` | `true`: aplica migraciones y seed al arrancar fuera de Development |
| `Docs__Enabled` | `true`: publica `/openapi/v1.json` y `/scalar` fuera de Development. Por defecto `false` |
| `Cors__AllowedOrigins__0` | Origen del frontend permitido por CORS |

**`Docs:Enabled`.** Siempre activo en Development. Fuera de Development solo se activa a propósito, por ejemplo para que un evaluador explore la API, y conviene apagarlo después. Los endpoints siguen exigiendo JWT; en Scalar se elige el esquema Bearer y se pega el `accessToken` de `POST /api/auth/login`, sin la palabra `Bearer`.

**`Database:MigrateOnStartup`.** En Development siempre se migra y se siembra. En otros entornos solo con este flag (`docker-compose.yml` lo pone en `true`). En un despliegue real lo dejaría apagado y aplicaría `scripts/migrations.sql`.

## Seed y usuarios demo

- El seed crea los libros, los ejemplares, los lectores y dos usuarios: `admin@demo.local` (Admin) y `ana.lopez@demo.local` (Lector, ligada a Ana López). En la base se guarda solo el hash de la contraseña.
- Va enganchado a `MigrateAsync` (`UseSeeding`/`UseAsyncSeeding`). Por eso corre en Development y, en otros entornos, solo con `Database:MigrateOnStartup=true`. Si la base se migra solo con `scripts/migrations.sql`, no se crean los usuarios.
- Si `Seed:AdminPassword` o `Seed:LectorPassword` están vacías, no crea ese usuario, en ningún entorno.
- De dónde salen las contraseñas: con Docker, de `.env` (`SEED_ADMIN_PASSWORD`, `SEED_LECTOR_PASSWORD`); en local, de la sección `Seed` de `appsettings.Development.json`, que es solo para desarrollo; en Azure, de las variables de la Container App.
- **Rotación.** El seed es idempotente: solo crea a un usuario si su correo no existe, así que **no cambia la contraseña de un usuario que ya existe**. Para rotarla hay que actualizar o borrar ese usuario en la base para que el seed lo vuelva a crear, y luego actualizar la tabla de acceso del README.

## Docker Compose

- Requiere Compose **v2** (`docker compose`, no el antiguo `docker-compose` v1). El archivo usa la Compose Specification: `name:` de nivel superior, sin `version:`, y `depends_on` con `condition: service_healthy`. Probado con v2.24.6.
- `db`: SQL Server 2022 con volumen `mssql-data` y healthcheck con `sqlcmd`. No publica el 1433 al host.
- `api`: imagen multi-stage (`sdk:10.0` → `aspnet:10.0`), usuario no root `app`, puerto 8080, `HEALTHCHECK` a `/health`. Arranca cuando `db` está healthy y tiene `restart: on-failure`. EF Core usa `EnableRetryOnFailure`, así que si SQL Server tarda en aceptar conexiones la API reintenta en vez de caerse.
- Todos los secretos salen de `.env`, que no se versiona. Por defecto el contenedor corre en `Production`.
- Para empezar de cero: `docker compose down -v` (borra el volumen de la base).

## Migraciones

Están en `Infrastructure/Persistence/Migrations`. Se aplican de cuatro maneras:

1. Automática en Development, con `MigrateAsync` al arrancar.
2. Automática con `Database__MigrateOnStartup=true` (Docker Compose).
3. A mano: `dotnet ef database update --project backend-asa.csproj` (requiere `dotnet tool install --global dotnet-ef`).
4. Con el script idempotente `scripts/migrations.sql`, que puede correrse varias veces:

   ```bash
   sqlcmd -S localhost -d database-asa -E -i scripts/migrations.sql
   ```

   Se regenera con `dotnet ef migrations script --idempotent -o scripts/migrations.sql`.

## Imagen (GitHub Actions)

`.github/workflows/deploy.yml` construye y publica la imagen; no despliega en Azure. Se dispara con cada push a `main` y a mano desde **Actions → Build y push de la imagen a GHCR → Run workflow**.

1. **Tests:** `dotnet test` (Testcontainers usa el Docker del runner). Si fallan, no se publica.
2. **Imagen:** construye el `Dockerfile` y la sube a GHCR como `ghcr.io/cristanpalma2203/backend-asa:latest` y `:<sha del commit>`, con el `GITHUB_TOKEN` (`packages: write`). No hay que crear secrets.

Solo se publica desde `main`; lanzado a mano desde otra rama, solo corren los tests. El tag con el SHA sirve para saber qué commit hay en cada imagen y para volver atrás.

## Container App en Azure

La Container App `ca-asa-backend-test` (resource group `asa-test`) usa `ghcr.io/cristanpalma2203/backend-asa:latest` y escucha en el puerto `8080` (`ASPNETCORE_HTTP_PORTS=8080`), que es el *target port* del ingress. Variables de entorno y secrets se configuran en Azure, no en este repo.

La Container App no se actualiza sola cuando hay una imagen nueva. Desde el portal:

- **Reiniciar la revisión activa:** **Container App → Revisions and replicas → revisión activa → Restart**.
- **Crear una nueva revisión:** **Container App → Application → Revisions and replicas → Create new revision** (o **Edit and deploy**), dejar la imagen `:latest` y **Create**.

Si después de reiniciar sigue respondiendo la versión anterior, crear una nueva revisión para que vuelva a descargar `:latest`. Para comprobar qué versión quedó, comparar el digest de la imagen de la revisión con el del último run del workflow.

**Antes del primer despliegue:**

- **Acceso a GHCR.** Los paquetes nuevos son privados. Hacer público el paquete (**Packages → backend-asa → Package settings → Change visibility**) o registrar `ghcr.io` en la Container App con un PAT de solo lectura (`read:packages`) en **Settings → Registries**.
- **Migraciones.** Ni el workflow ni el reinicio tocan la base: aplicar `scripts/migrations.sql` o usar `Database__MigrateOnStartup=true`.

## Notas de runtime

- El contenedor solo sirve HTTP en 8080; TLS lo pone el ingress. Por eso el log avisa que no encuentra puerto HTTPS para redirigir.
- Las claves de Data Protection viven dentro del contenedor. La API no las usa para datos persistentes (el JWT se firma con `Jwt:Key`), así que se pueden perder sin efecto.
