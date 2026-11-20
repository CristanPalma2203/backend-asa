# backend-asa

API de biblioteca para catálogo, ejemplares y reservas, sin asignar la misma copia dos veces.

## Stack

.NET 10, minimal APIs en Vertical Slice, EF Core 10 con SQL Server, FluentValidation, JWT, Scalar y xUnit con Testcontainers.

## Requisitos

.NET 10 SDK, SQL Server local o Docker, y Docker Desktop encendido para los tests.

## Cómo lo corro

Clono el repo y apunto `ConnectionStrings:database-asa` a mi SQL Server. Lo dejo en `appsettings.Development.json` o en la variable `ConnectionStrings__database-asa`. `dotnet user-secrets` también vale después de `dotnet user-secrets init`, porque el proyecto todavía no trae `UserSecretsId`.

```bash
dotnet run
```

En Development, `Program.cs` llama `MigrateAsync`. Eso aplica las migraciones y el seed (`UseSeeding` / `UseAsyncSeeding`). Fuera de Development no migro ni siembro al arrancar.

Scalar queda en `/scalar` (y `/` redirige ahí). El health check está en `/health`.

`Jwt:Key` va vacía en `appsettings.json`. Si arranco fuera de Development sin clave, el proceso se detiene.

## Usuarios demo

Viven en la tabla `Usuarios` y se siembran solo en Development, junto con el resto del seed. Las contraseñas salen de `Seed:AdminPassword` y `Seed:LectorPassword` en `appsettings.Development.json`. Si faltan, no creo usuarios. En la base guardo solo el hash.

| Correo | Contraseña | Rol |
| --- | --- | --- |
| admin@demo.local | Admin-demo-123 | Admin |
| ana.lopez@demo.local | Ana-demo-123 | Lector |

Ana queda ligada al lector `ana.lopez@demo.local`. El token lleva `sub`, `email`, `role` y, si tiene lector, `lector_id` tomado de `IdLector`. Un usuario inactivo no entra: el login responde 401 `credenciales-invalidas`.

`POST /api/auth/login` devuelve `accessToken`. En Scalar elijo el esquema Bearer y pego el token, sin la palabra `Bearer`.

## Base de datos

Cuatro entidades de catálogo más la cuenta: `Libro`, `Ejemplar`, `Lector`, `Reserva` y `Usuario`.

No guardo stock. Disponibles es el conteo de ejemplares en estado `Disponible` para ese libro.

`IX_Reservas_IdEjemplar_Activa` es único y filtrado: `[Estado] = 'Activa'`. Un ejemplar no puede tener dos reservas activas.

Checks: `CK_Libro_Estado`, `CK_Ejemplar_Estado`, `CK_Lector_Estado`, `CK_Reserva_Estado` y `CK_Reserva_FechaCancelacion` (si el estado es `Cancelada`, `FechaCancelacion` es obligatoria). `Ejemplar.RowVersion` es `rowversion` para concurrencia optimista al cambiar estado.

Aplico migraciones de tres maneras:

1. Automática en Development, con `MigrateAsync` al arrancar.
2. A mano: `dotnet ef database update --project backend-asa.csproj`.
3. Script idempotente. No dejé el archivo en el repo. Lo genero y lo ejecuto así:

```bash
dotnet ef migrations script --idempotent --project backend-asa.csproj -o scripts/migrations.sql
sqlcmd -S localhost -d database-asa -E -i scripts/migrations.sql
```

## Reserva y concurrencia

`POST /api/reservas` abre una transacción `ReadCommitted`.

1. Compruebo lector y libro (existen y están activos) y que ese lector no tenga ya una reserva activa del mismo libro.
2. Tomo una copia con:

```sql
SELECT TOP (1) e.*
FROM Ejemplares AS e WITH (UPDLOCK, READPAST, ROWLOCK)
WHERE e.IdLibro = {libroId} AND e.Estado = 'Disponible'
ORDER BY e.CodigoInventario
```

`UPDLOCK` y `ROWLOCK` bloquean esa fila hasta el commit. `READPAST` hace que la otra transacción salte la fila bloqueada y busque la siguiente. Si no hay otra disponible, hago rollback y respondo `sin-stock`. Por eso dos `POST` a la vez no se quedan con la misma copia.

3. Creo la reserva `Activa`, paso el ejemplar a `Reservado`, un `SaveChanges` y commit.
4. Si el índice filtrado salta (2601 o 2627), respondo `ejemplar-ya-reservado`. Es la segunda barrera si el candado no alcanza.

Cancelar va en la misma transacción: la reserva pasa a `Cancelada` con `FechaCancelacion` en UTC y el ejemplar vuelve a `Disponible`. Un solo commit. Si falla, el `await using` hace rollback.

## Errores

Respondo `ProblemDetails`. El código va en `code`, en kebab-case.

| Código | Status |
| --- | --- |
| validacion-fallida | 400 |
| no-autenticado | 401 |
| credenciales-invalidas | 401 |
| acceso-denegado | 403 |
| libro-no-encontrado | 404 |
| ejemplar-no-encontrado | 404 |
| lector-no-encontrado | 404 |
| reserva-no-encontrada | 404 |
| isbn-duplicado | 409 |
| codigo-inventario-duplicado | 409 |
| sin-stock | 409 |
| ejemplar-ya-reservado | 409 |
| conflicto-concurrencia | 409 |
| registro-duplicado | 409 |
| libro-inactivo | 422 |
| lector-inactivo | 422 |
| reserva-duplicada | 422 |
| reserva-no-activa | 422 |
| libro-con-reservas-activas | 422 |
| estado-no-permitido | 422 |
| ejemplar-reservado | 422 |
| error-interno | 500 |

## Endpoints

| Método | Ruta | Quién | Qué hace |
| --- | --- | --- | --- |
| POST | /api/auth/login | Público | Entrega el JWT |
| GET | /api/auth/me | Autenticado | Lee rol, nombre y lector del token |
| GET | /api/libros | Público | Lista libros activos, paginado |
| GET | /api/libros/{id} | Público | Detalle, disponibles y ejemplares |
| POST | /api/libros | Admin | Crea un libro activo |
| PUT | /api/libros/{id} | Admin | Edita ISBN, título, autor y categoría |
| DELETE | /api/libros/{id} | Admin | Baja lógica. 422 si hay reserva activa |
| GET | /api/libros/{idLibro}/ejemplares | Admin | Lista copias con rowVersion en base64 |
| POST | /api/libros/{idLibro}/ejemplares | Admin | Alta en Disponible |
| PUT | /api/libros/{idLibro}/ejemplares/{idEjemplar}/estado | Admin | Solo Disponible o Inactivo |
| POST | /api/reservas | Autenticado | Reserva una copia. El lector usa su `lector_id`. Admin puede indicar otro |
| POST | /api/reservas/{id}/cancelar | Dueño o Admin | Cancela y libera la copia |
| GET | /api/reservas/{id} | Dueño o Admin | Detalle de una reserva |
| GET | /api/lectores/{lectorId}/reservas | Ese lector o Admin | Lista paginada, la más reciente primero |
| GET | /health | Público | Comprueba el DbContext |

## Tests

Con Docker Desktop en marcha:

```bash
dotnet test tests/backend-asa.Tests/backend-asa.Tests.csproj
```

Levantan SQL Server en Testcontainers, migran, siembran y Respawn limpia entre tests.

Cubro listado y alta de libros, ejemplar duplicado, no inactivar una copia reservada, stock al reservar y al cancelar, lector inactivo, sin stock, reserva duplicada, 401 sin token, 403 si un lector reserva por otro, alta de libro como admin, y que el modelo no tenga cambios pendientes.

El de concurrencia deja un libro con una sola copia disponible, dispara dos `POST /api/reservas` a la vez y comprueba un 201, un 409 y una sola reserva `Activa` de ese ejemplar.

## Decisiones y lo que sigue

Dejé el stock fuera de la tabla para que no se desfase del estado real de cada copia. La reserva elige el ejemplar en el servidor, con candado de fila, y el índice filtrado cubre el hueco.

Con más tiempo haría esto:

- Commitear `scripts/migrations.sql` y dejar de depender de migrar al arrancar.
- Préstamo de verdad: hoy `Prestado` existe en el enum y nadie lo asigna.
- Refresh token y caducidad más corta que 60 minutos.
- `UserSecretsId` en el csproj para no dejar la cadena en `appsettings.Development.json`.
- Seguridad OpenAPI por operación, no un Bearer global que marca también los GET públicos.

## Autor

Alex Palma. [LinkedIn](https://www.linkedin.com/in/cristian-palma-07a407259) y [GitHub](https://github.com/CristanPalma2203).
