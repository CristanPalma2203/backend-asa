# Scripts

`migrations.sql` es el esquema idempotente. `limpieza-datos-prueba.sql` no es una migración: borra libros, ejemplares, lectores, usuarios y reservas creados a mano en QA.

Solo toca filas que no están en el seed de `DatosIniciales` y cuyo título, autor, nombre, correo, identificador o código de inventario contiene `QA`, `Test`, `Prueba` o `Live`. No borra `admin@demo.local`, `ana.lopez@demo.local`, `carlos.mendez@demo.local` ni los ISBN y códigos `BIB-001` a `BIB-051`.

El script abre una transacción, lista lo que borraría y termina en `ROLLBACK`. Para aplicarlo, pon `@Confirmar` en `1`.

## Local con Docker Compose

El servicio `db` no publica el puerto. El SQL entra por stdin. Déjalo primero en modo de prueba (`@Confirmar = 0`):

```powershell
Get-Content scripts/limpieza-datos-prueba.sql -Raw | docker compose exec -T db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $env:MSSQL_SA_PASSWORD -d BibliotecaAsa -C -b
```

Revisa los `SELECT`. Si los conteos cuadran, cambia `@Confirmar` a `1` y vuelve a correr el mismo comando.

## Azure SQL

No lo dejes apuntando a Azure hasta haber visto el `ROLLBACK` en local. Con `@Confirmar` todavía en `0`:

```bash
sqlcmd -S <servidor>.database.windows.net -d <base> -U <usuario> -P <password> -i scripts/limpieza-datos-prueba.sql
```

Solo después de revisar esa salida, pon `@Confirmar = 1` y repite el comando.
