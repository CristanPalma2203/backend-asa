# backend-asa

API REST de una biblioteca: catálogo de libros, ejemplares físicos, lectores y reservas. La regla central es que dos personas nunca se queden con la misma copia, aunque reserven en el mismo instante.

El frontend vive en otro repo: [frontend-asa](https://github.com/CristanPalma2203/frontend-asa).

## Stack

- .NET 10 con minimal APIs.
- EF Core 10 sobre SQL Server 2022.
- FluentValidation para las entradas.
- JWT Bearer para autenticación, con roles `Admin` y `Lector`.
- OpenAPI más Scalar en Development.
- xUnit, FluentAssertions, Testcontainers (SQL Server) y Respawn para los tests.
- Docker con build multi-stage y Docker Compose (API + SQL Server).

## Arquitectura

Uso **Vertical Slice**. Cada caso de uso es una carpeta `Features/<Área>/<Operación>/` con un archivo por pieza: `Endpoint.cs` (`IEndpoint`), `Handler.cs`, `Request.cs`, `Response.cs` y `Validator.cs`; solo están los que la operación necesita. Lo que comparten varias operaciones de un área queda en la raíz del área (`LibroIsbn`, `CodigoInventario`, `RowVersionToken`, `ReservaDetalle`, `Acceso`). Los endpoints se registran solos por reflexión (`AddEndpoints` / `MapEndpoints`).

```
Common/          Endpoints, Result/Error, ProblemDetails, validación, paginación, OpenAPI
Domain/          Entidades y enums (Libro, Ejemplar, Lector, Reserva, Usuario)
Features/        Casos de uso: una carpeta por área y una subcarpeta por operación (detalle abajo)
Infrastructure/  DbContext, configuraciones EF, converters, migraciones y seed
scripts/         migrations.sql idempotente
tests/           Tests de integración contra SQL Server real
```

```
Features/
├── Auth/
│   ├── Login/            Endpoint.cs, Handler.cs, Request.cs, Response.cs, Validator.cs
│   ├── Me/               Endpoint.cs
│   ├── Acceso.cs
│   ├── AuthOptions.cs
│   └── BearerSecuritySchemeTransformer.cs
├── Ejemplar/
│   ├── CambiarEstado/    Endpoint.cs, Handler.cs, Request.cs, Validator.cs
│   ├── Crear/            Endpoint.cs, Handler.cs, Request.cs, Response.cs, Validator.cs
│   ├── Desactivar/       Endpoint.cs, Handler.cs, Request.cs, Validator.cs
│   ├── Editar/           Endpoint.cs, Handler.cs, Request.cs, Validator.cs
│   ├── Listar/           Endpoint.cs, Handler.cs, Request.cs, Response.cs, Validator.cs
│   ├── CodigoInventario.cs
│   └── RowVersionToken.cs
├── Lector/
│   ├── Crear/            Endpoint.cs, Handler.cs, Request.cs, Response.cs, Validator.cs
│   ├── Desactivar/       Endpoint.cs, Handler.cs, Request.cs, Validator.cs
│   ├── Editar/           Endpoint.cs, Handler.cs, Request.cs, Validator.cs
│   ├── Listar/           Endpoint.cs, Handler.cs, Request.cs, Response.cs, Validator.cs
│   └── Obtener/          Endpoint.cs, Handler.cs, Request.cs, Response.cs, Validator.cs
├── Libro/
│   ├── Crear/            Endpoint.cs, Handler.cs, Request.cs, Response.cs, Validator.cs
│   ├── Desactivar/       Endpoint.cs, Handler.cs, Request.cs, Validator.cs
│   ├── Editar/           Endpoint.cs, Handler.cs, Request.cs, Validator.cs
│   ├── Listar/           Endpoint.cs, Handler.cs, Request.cs, Response.cs, Validator.cs
│   ├── Obtener/          Endpoint.cs, Handler.cs, Request.cs, Response.cs, Validator.cs
│   └── LibroIsbn.cs
└── Reserva/
    ├── Cancelar/         Endpoint.cs, Handler.cs, Request.cs, Validator.cs
    ├── Crear/            Endpoint.cs, Handler.cs, Request.cs, Response.cs, Validator.cs
    ├── ListarPorLector/  Endpoint.cs, Handler.cs, Request.cs, Validator.cs
    ├── Obtener/          Endpoint.cs, Handler.cs, Request.cs, Validator.cs
    └── ReservaDetalle.cs
```

Los handlers devuelven `Result` / `Result<T>` y no lanzan excepciones para los errores de negocio. `ResultExtensions.ToHttpResult()` traduce cada `ErrorType` a su status HTTP.

### Decisiones

**Concurrencia de reservas.** `POST /api/reservas` abre una transacción `ReadCommitted`, valida lector, libro y reserva duplicada, y toma una copia así:

```sql
SELECT TOP (1) e.*
FROM Ejemplares AS e WITH (UPDLOCK, READPAST, ROWLOCK)
WHERE e.IdLibro = {libroId} AND e.Estado = 'Disponible'
ORDER BY e.CodigoInventario
```

`UPDLOCK` + `ROWLOCK` bloquean solo esa fila hasta el commit. `READPAST` hace que una transacción concurrente salte la fila bloqueada y tome la siguiente copia libre en vez de esperar. Si no queda ninguna, respondo `409 sin-stock`.

Antes de verificar la reserva duplicada, la misma transacción bloquea la fila del lector:

```sql
SELECT * FROM Lectores WITH (UPDLOCK, ROWLOCK) WHERE IdLector = {lectorId}
```

Sin este bloqueo había una carrera: si el mismo lector enviaba varias reservas del mismo libro a la vez, todas veían "no hay reserva activa" y, como `READPAST` les da copias distintas, el índice único no las frenaba y se creaban varias. Con `UPDLOCK` la segunda petición del mismo lector espera a que la primera haga commit y entonces ve su reserva, así que responde `422 reserva-duplicada`. Solo se serializan las reservas del mismo lector; las de lectores distintos no se esperan entre sí. Un test lanza cuatro reservas simultáneas de Ana sobre un libro con tres copias y comprueba un 201, tres `reserva-duplicada` y una sola reserva activa.

Como segunda barrera, `IX_Reservas_IdEjemplar_Activa` es un índice **único filtrado** (`[Estado] = 'Activa'`). Si por cualquier camino se intentara una segunda reserva activa del mismo ejemplar, SQL Server lo rechaza (2601/2627) y respondo `409 ejemplar-ya-reservado`. Un test dispara dos reservas simultáneas de la única copia y comprueba un 201, un 409 y una sola reserva activa.

**Stock calculado.** No guardo un contador. "Disponibles" es el conteo de ejemplares en estado `Disponible`, así nunca se desfasa del estado real de cada copia.

**Concurrencia optimista con `rowversion`.** `Ejemplar.RowVersion` es `rowversion`. Lo envío en base64 y el cliente lo devuelve al cambiar el estado de un ejemplar o al editarlo. Si otro usuario lo modificó antes, EF lanza `DbUpdateConcurrencyException` y respondo `409 conflicto-concurrencia`.

**Errores como ProblemDetails con `code`.** Todas las respuestas de error son `application/problem+json` con una extensión `code` en kebab-case (`sin-stock`, `lector-inactivo`…). El frontend decide qué mostrar por `code`, no por el texto. Las validaciones devuelven `400 validacion-fallida` con `errors` por campo. `GlobalExceptionHandler` cubre concurrencia, duplicados e internos.

**Contrato JSON.**

- Los enums viajan como texto (`"estado": "Activa"`). Uso `JsonStringEnumConverter` y el OpenAPI los declara `type: string`.
- Las fechas salen en UTC con `Z`. Un `ValueConverter` global marca como `Utc` todo `DateTime` leído de SQL Server, y los handlers guardan con `TimeProvider.GetUtcNow()`.
- Las reservas exponen `fechaSolicitud` y `fechaCancelacion`, y los ejemplares `idEjemplar`.

**Checks en la base.** `CK_Libro_Estado`, `CK_Ejemplar_Estado`, `CK_Lector_Estado`, `CK_Reserva_Estado`, `CK_Usuario_Rol`, `CK_Usuario_Estado` y `CK_Reserva_FechaCancelacion` (si el estado es `Cancelada`, la fecha es obligatoria).

## Base de datos

Sacado de `Infrastructure/Persistence/Configurations/*.cs`; los tipos son los que genera la migración. `PK` clave primaria (`int IDENTITY`), `FK` clave foránea, `UK` único.

```mermaid
erDiagram
    Libros ||--o{ Ejemplares : "IdLibro"
    Lectores ||--o{ Reservas : "IdLector"
    Ejemplares ||--o{ Reservas : "IdEjemplar"
    Lectores |o--o| Usuarios : "IdLector (único si no es NULL)"

    Libros {
        int IdLibro PK
        varchar(20) Isbn UK
        nvarchar(200) Titulo
        nvarchar(150) Autor
        nvarchar(80) Categoria
        varchar(20) Estado "Activo | Inactivo"
    }

    Ejemplares {
        int IdEjemplar PK
        int IdLibro FK
        varchar(30) CodigoInventario UK
        varchar(20) Estado "Disponible | Reservado | Prestado | Inactivo"
        datetime2 CreatedAt "default SYSUTCDATETIME()"
        datetime2 UpdatedAt "default SYSUTCDATETIME()"
        rowversion RowVersion "token de concurrencia optimista"
    }

    Lectores {
        int IdLector PK
        varchar(20) Identificador UK
        nvarchar(150) Nombre
        varchar(150) Correo UK
        varchar(20) Estado "Activo | Inactivo"
    }

    Reservas {
        int IdReserva PK
        int IdLector FK
        int IdEjemplar FK
        datetime2 FechaSolicitud "default SYSUTCDATETIME()"
        varchar(20) Estado "Activa | Cancelada | Completada"
        datetime2 FechaCancelacion "NULL"
    }

    Usuarios {
        int IdUsuario PK
        varchar(150) Correo UK
        nvarchar(150) Nombre
        nvarchar(500) PasswordHash
        varchar(20) Rol "Admin | Lector"
        varchar(20) Estado "Activo | Inactivo"
        int IdLector FK, UK "NULL para el admin"
        datetime2 CreatedAt "default SYSUTCDATETIME()"
    }
```

Todas las FK son `ON DELETE RESTRICT`: no hay borrado físico en cascada, las bajas son lógicas por `Estado`. Un lector tiene a lo sumo un usuario (`IX_Usuarios_IdLector`), y un usuario puede no tener lector (el admin).

**Índices**

| Índice | Tabla | Columnas | Tipo |
| --- | --- | --- | --- |
| `IX_Libros_Isbn` | Libros | `Isbn` | Único |
| `IX_Libros_Estado` | Libros | `Estado` | |
| `IX_Ejemplares_CodigoInventario` | Ejemplares | `CodigoInventario` | Único |
| `IX_Ejemplares_Estado` | Ejemplares | `Estado` | |
| `IX_Ejemplares_IdLibro_Estado` | Ejemplares | `IdLibro`, `Estado` | Compuesto (conteo de disponibles; por ser `IdLibro` la columna inicial, también sirve para buscar por libro y para la FK) |
| `IX_Lectores_Identificador` | Lectores | `Identificador` | Único |
| `IX_Lectores_Correo` | Lectores | `Correo` | Único |
| `IX_Reservas_IdLector` | Reservas | `IdLector` | |
| `IX_Reservas_IdEjemplar` | Reservas | `IdEjemplar` | |
| `IX_Reservas_IdEjemplar_Activa` | Reservas | `IdEjemplar` | **Único filtrado** `WHERE [Estado] = 'Activa'`: una sola reserva activa por ejemplar |
| `IX_Reservas_Estado` | Reservas | `Estado` | |
| `IX_Usuarios_Correo` | Usuarios | `Correo` | Único |
| `IX_Usuarios_IdLector` | Usuarios | `IdLector` | Único filtrado `WHERE [IdLector] IS NOT NULL` |

No hay un índice aparte sobre `Ejemplares.IdLibro`: era redundante con `IX_Ejemplares_IdLibro_Estado` y lo quita la migración `QuitarIndiceEjemplaresIdLibro`.

**CHECK**

| Restricción | Tabla | Condición |
| --- | --- | --- |
| `CK_Libro_Estado` | Libros | `[Estado] IN ('Activo', 'Inactivo')` |
| `CK_Ejemplar_Estado` | Ejemplares | `[Estado] IN ('Disponible', 'Reservado', 'Prestado', 'Inactivo')` |
| `CK_Lector_Estado` | Lectores | `[Estado] IN ('Activo', 'Inactivo')` |
| `CK_Reserva_Estado` | Reservas | `[Estado] IN ('Activa', 'Cancelada', 'Completada')` |
| `CK_Reserva_FechaCancelacion` | Reservas | `([Estado] <> 'Cancelada') OR ([FechaCancelacion] IS NOT NULL)` |
| `CK_Usuario_Rol` | Usuarios | `[Rol] IN ('Admin', 'Lector')` |
| `CK_Usuario_Estado` | Usuarios | `[Estado] IN ('Activo', 'Inactivo')` |

**Stock disponible.** No existe una columna de stock: `disponibles` es `COUNT(*)` de los `Ejemplares` del libro con `Estado = 'Disponible'`, calculado en cada consulta y apoyado en `IX_Ejemplares_IdLibro_Estado`.
Reservar, cancelar o desactivar solo cambian el `Estado` de un ejemplar, así que el número no puede desfasarse de las copias reales.

## Requisitos

- **.NET 10 SDK**, para compilar, correr en local y lanzar los tests.
- **Para correr en local:** SQL Server accesible desde la máquina (instancia local, LocalDB o Developer Edition).
- **Para Docker y para los tests:** Docker Desktop encendido. Compose debe ser **v2** (el comando `docker compose`, no el antiguo `docker-compose` v1), porque el archivo usa la Compose Specification: `name:` de nivel superior, sin `version:`, y `depends_on` con `condition: service_healthy`. Probado con Docker Compose v2.24.6.
- Opcional: `dotnet-ef` (`dotnet tool install --global dotnet-ef`) para crear migraciones o regenerar `scripts/migrations.sql`.

## Cómo correrlo

### Visual Studio / IIS Express (Development)

1. SQL Server local. La cadena por defecto (`appsettings.Development.json`) es `Server=localhost;Database=database-asa;Integrated Security=True;TrustServerCertificate=True`. Si la tuya es otra, cámbiala ahí o en la variable `ConnectionStrings__database-asa`.
2. Abro `backend-asa.slnx` y arranco el perfil **IIS Express** (`https://localhost:44358`) o **https** (`https://localhost:7212`). Con la CLI: `dotnet run --launch-profile https`.
3. En Development, `Program.cs` corre `MigrateAsync`, que aplica las migraciones y el seed. Scalar queda en `/scalar` (y `/` redirige ahí).

### Docker Compose

```bash
cp .env.example .env     # y cambio todos los valores marcados CAMBIAR
docker compose up --build
```

- `db`: SQL Server 2022 con volumen `mssql-data` y healthcheck con `sqlcmd`. No publico el 1433 al host.
- `api`: imagen local multi-stage (`sdk:10.0` → `aspnet:10.0`), corre como usuario no root `app` en el puerto 8080, con `HEALTHCHECK` a `/health`. Arranca cuando `db` está healthy y tiene `restart: on-failure`. Además, EF Core usa `EnableRetryOnFailure`, así que si SQL Server tarda en aceptar conexiones la API reintenta en vez de caerse.
- La API queda en `http://localhost:8080` y el health check en `http://localhost:8080/health`.

Todos los secretos (contraseña de `sa`, `JWT_KEY`, contraseñas del seed) salen de `.env`, que no se versiona.

Por defecto el contenedor corre en `Production`: sin Scalar ni OpenAPI. Con `ASPNETCORE_ENVIRONMENT=Development` en `.env` se publican.

**Migraciones al arrancar.** Fuera de Development no migro ni siembro, salvo que `Database:MigrateOnStartup=true` (variable `Database__MigrateOnStartup`). Compose la pone en `true` para que la base quede lista sola. En un despliegue real la dejaría apagada y aplicaría `scripts/migrations.sql`.

Para empezar de cero: `docker compose down -v` (borra el volumen de la base).

## Usuarios demo

El seed crea dos usuarios en la tabla `Usuarios`. En la base guardo solo el hash.

| Correo | Rol | Contraseña |
| --- | --- | --- |
| admin@demo.local | Admin | `Seed:AdminPassword` |
| ana.lopez@demo.local | Lector (ligada al lector Ana López) | `Seed:LectorPassword` |

Las contraseñas no están en este README. Dependen de cómo levantes la API:

- **Con Docker Compose:** salen de tu `.env`, que copias de `.env.example` (`cp .env.example .env`) y donde cambias los valores marcados `CAMBIAR`. Las variables son `SEED_ADMIN_PASSWORD` y `SEED_LECTOR_PASSWORD`. `.env` no se versiona, así que cada uno define las suyas.
- **En local (Visual Studio o `dotnet run`, entorno Development):** salen de la sección `Seed` de `appsettings.Development.json`. Ese archivo es **solo para desarrollo**: no se carga fuera de Development y sus valores no deben usarse en ningún otro entorno.

Si faltan, el seed no crea usuarios.

El token lleva `sub`, `email`, `role` y, si el usuario tiene lector, `lector_id`. En Scalar elijo el esquema Bearer y pego el `accessToken` de `POST /api/auth/login`, sin la palabra `Bearer`.

## Endpoints principales

| Método | Ruta | Quién | Qué hace |
| --- | --- | --- | --- |
| POST | /api/auth/login | Público | Entrega el JWT |
| GET | /api/auth/me | Autenticado | Rol, nombre y lector del token |
| GET | /api/libros | Público | Libros activos paginados. `buscar` (o su alias `search`): contiene, en título, autor o ISBN. `autor`: contiene, sin distinguir mayúsculas. `categoria`: exacta. `page`, `pageSize` |
| GET | /api/libros/{id} | Público | Detalle, disponibles y ejemplares |
| POST | /api/libros | Admin | Crea un libro |
| PUT | /api/libros/{id} | Admin | Edita ISBN, título, autor y categoría |
| DELETE | /api/libros/{id} | Admin | Baja lógica. 422 si hay reservas activas |
| GET | /api/libros/{idLibro}/ejemplares | Admin | Copias con `rowVersion` en base64 |
| POST | /api/libros/{idLibro}/ejemplares | Admin | Alta de copia en `Disponible` |
| PUT | /api/libros/{idLibro}/ejemplares/{idEjemplar}/estado | Admin | Solo `Disponible` ↔ `Inactivo`, con `rowVersion` |
| PUT | /api/ejemplares/{id} | Admin | Edita el `codigoInventario`, con `rowVersion`. 409 por código duplicado o por concurrencia |
| DELETE | /api/ejemplares/{id} | Admin | Baja lógica (pasa a `Inactivo`). 422 `ejemplar-reservado` si está reservado |
| POST | /api/reservas | Autenticado | Reserva una copia. El lector usa su `lector_id`; Admin puede indicar otro |
| GET | /api/reservas/{id} | Dueño o Admin | Detalle de una reserva |
| POST | /api/reservas/{id}/cancelar | Dueño o Admin | Cancela y libera la copia |
| GET | /api/lectores/{lectorId}/reservas | Ese lector o Admin | Paginado, la más reciente primero (`estado` opcional) |
| GET | /api/lectores | Admin | Lectores paginados e indica si tienen usuario |
| GET | /api/lectores/{id} | Admin | Detalle y reservas activas |
| POST | /api/lectores | Admin | Crea el lector; con `password` (8+) crea también su usuario |
| PUT | /api/lectores/{id} | Admin | Edita datos y el correo del usuario ligado |
| DELETE | /api/lectores/{id} | Admin | Baja lógica del lector y de su usuario |
| GET | /health | Público | Comprueba la conexión a la base |

## Códigos de error

| Código | Status |
| --- | --- |
| validacion-fallida | 400 |
| json-invalido | 400 |
| solicitud-invalida | 400 (o el status del fallo, p. ej. 415) |
| no-autenticado | 401 |
| credenciales-invalidas | 401 |
| acceso-denegado | 403 |
| libro-no-encontrado | 404 |
| ejemplar-no-encontrado | 404 |
| lector-no-encontrado | 404 |
| reserva-no-encontrada | 404 |
| isbn-duplicado | 409 |
| codigo-inventario-duplicado | 409 |
| correo-duplicado | 409 |
| identificador-duplicado | 409 |
| sin-stock | 409 |
| ejemplar-ya-reservado | 409 |
| conflicto-concurrencia | 409 |
| registro-duplicado | 409 |
| libro-inactivo | 422 |
| lector-inactivo | 422 |
| reserva-duplicada | 422 |
| reserva-no-activa | 422 |
| libro-con-reservas-activas | 422 |
| lector-con-reservas-activas | 422 |
| estado-no-permitido | 422 |
| ejemplar-reservado | 422 |
| error-interno | 500 |

`json-invalido` es un body con JSON mal formado o con un valor de tipo incorrecto (por ejemplo `"lectorId": "abc"`). `solicitud-invalida` cubre el resto de fallos de binding: un parámetro de ruta o query con formato inválido, un body ausente o un `Content-Type` no soportado.

## Tests

Necesitan **Docker encendido**: Testcontainers levanta un SQL Server 2022 desechable.

```bash
dotnet test tests/backend-asa.Tests/backend-asa.Tests.csproj
```

Son tests de integración de punta a punta (HTTP → API → SQL Server real). La fixture migra y siembra una vez, y Respawn limpia la base entre tests. Son 33 y cubren:

- **Libros:** listado con datos iniciales, `search` como alias de `buscar`, filtro `autor` sin distinguir mayúsculas, alta como admin, ISBN duplicado e ISBN inválido.
- **Ejemplares:** código duplicado, que no se pueda inactivar una copia reservada, editar con `rowVersion` vieja (`409 conflicto-concurrencia`) y desactivar una copia sin borrarla.
- **Reservas:**
  - stock al reservar y al cancelar;
  - cancelar dos veces: la segunda da `422 reserva-no-activa` y no cambia nada;
  - lector inactivo, sin stock y reserva duplicada;
  - 401 sin token y 403 si un lector reserva por otro;
  - **dos reservas simultáneas de la única copia**;
  - **cuatro reservas simultáneas del mismo lector y libro**: un 201 y tres `reserva-duplicada`.
- **Lectores:** correo duplicado, baja con reservas activas, 403 para un lector, alta con y sin contraseña, y contraseña corta.
- **Usuarios:** un usuario inactivo no entra.
- **Contrato:**
  - enums como texto en JSON y en OpenAPI;
  - `fechaSolicitud` y `fechaCancelacion` en UTC con `Z`;
  - `idEjemplar` en el detalle y en el listado;
  - un body JSON roto da `400 json-invalido` como ProblemDetails.
- **Migraciones:** que el modelo no tenga cambios pendientes (`HasPendingModelChanges`).

## Migraciones y `scripts/migrations.sql`

Las migraciones están en `Infrastructure/Persistence/Migrations`. Las aplico de cuatro maneras:

1. Automática en Development, con `MigrateAsync` al arrancar.
2. Automática en Docker, con `Database__MigrateOnStartup=true`.
3. A mano: `dotnet ef database update --project backend-asa.csproj`.
4. Con el script idempotente `scripts/migrations.sql`, que puede correrse varias veces sin romper nada:

   ```bash
   sqlcmd -S localhost -d database-asa -E -i scripts/migrations.sql
   ```

   Se regenera con `dotnet ef migrations script --idempotent -o scripts/migrations.sql`.

## Despliegue

### Imagen (GitHub Actions)

`.github/workflows/deploy.yml` construye y publica la imagen. No despliega nada en Azure. Se dispara con cada push a `main`, y a mano desde **Actions → Build y push de la imagen a GHCR → Run workflow**.

1. **Tests:** corre `dotnet test` (Testcontainers usa el Docker del runner). Si fallan, no se publica la imagen.
2. **Imagen:** construye el `Dockerfile` y la sube a GHCR con dos tags: `ghcr.io/cristanpalma2203/backend-asa:latest` y `:<sha del commit>`. Usa el `GITHUB_TOKEN` (`packages: write`): no hay que crear secrets.

La imagen solo se publica desde `main`. Si se lanza a mano desde otra rama, solo corren los tests. El tag con el SHA queda para saber qué commit hay en cada imagen y para volver a una versión anterior si hace falta.

### Container App en Azure

La Container App `ca-asa-backend-test` (resource group `asa-test`) usa la imagen `ghcr.io/cristanpalma2203/backend-asa:latest`. Escucha en el puerto `8080` (`ASPNETCORE_HTTP_PORTS=8080`), que es el *target port* del ingress. Las variables de entorno y los secrets se configuran en Azure, no en este repo.

Cuando el workflow publica una imagen nueva, la Container App no se actualiza sola. Para desplegarla, desde el portal de Azure:

- **Reiniciar la revisión activa:** **Container App → Revisions and replicas → revisión activa → Restart**.
- **Crear una nueva revisión:** **Container App → Application → Revisions and replicas → Create new revision** (o **Edit and deploy**), dejar la imagen `:latest` y **Create**.

Si después de reiniciar sigue respondiendo la versión anterior, crear una nueva revisión: así la Container App vuelve a descargar `:latest`.

Para comprobar qué versión quedó, se puede comparar el digest de la imagen de la revisión con el del último run del workflow, o probar `GET /health` y algún endpoint que haya cambiado.

### Antes del primer despliegue

- **Acceso de la Container App a GHCR.** Los paquetes nuevos de GHCR son privados. Hay dos opciones: hacer público el paquete (**Packages → backend-asa → Package settings → Change visibility**), o registrar `ghcr.io` en la Container App con un PAT de solo lectura (`read:packages`), en **Settings → Registries** del portal. Si no, la revisión no puede descargar la imagen.
- **Migraciones.** Ni el workflow ni el reinicio tocan la base. Los cambios de esquema se aplican con `scripts/migrations.sql` (idempotente), o con `Database__MigrateOnStartup=true` en la configuración de la Container App.

## Supuestos

- Una reserva asigna una copia concreta al momento. No hay cola de espera: sin copias libres, la respuesta es `sin-stock`.
- Un lector no puede tener dos reservas activas del mismo libro.
- Las bajas son lógicas (estado `Inactivo`). No borro filas con historial.
- Desde la API, un ejemplar solo pasa entre `Disponible` e `Inactivo`. `Reservado` lo gestionan las reservas y `Prestado` existe en el enum, pero aún no hay flujo de préstamo.
- Todas las fechas se guardan y se devuelven en UTC. Convertir a la zona del usuario es tarea del cliente.
- El contenedor solo sirve HTTP en 8080. TLS lo pondría un proxy o ingress delante. Por eso el log avisa que no encuentra puerto HTTPS para redirigir.
- Las claves de Data Protection viven dentro del contenedor. La API no las usa para datos persistentes (el JWT se firma con `Jwt:Key`), así que se pueden perder sin efecto.

Con más tiempo haría esto:

- Préstamo y devolución.
- Refresh token con caducidad más corta.
- `UserSecretsId` para sacar la cadena de `appsettings.Development.json`.
- Seguridad OpenAPI por operación en vez de un Bearer global.

## Autor

Alex Palma. [LinkedIn](https://www.linkedin.com/in/cristian-palma-07a407259) y [GitHub](https://github.com/CristanPalma2203).
