SET NOCOUNT ON;

DECLARE @Confirmar bit = 0;

DECLARE @Patrones TABLE (Patron nvarchar(20) NOT NULL PRIMARY KEY);
INSERT INTO @Patrones (Patron) VALUES (N'%QA%'), (N'%Prueba%');

CREATE TABLE #IsbnSeed (Isbn varchar(20) NOT NULL PRIMARY KEY);
INSERT INTO #IsbnSeed (Isbn) VALUES
    ('9788498381498'), ('9780307474728'), ('9788420471839'), ('9788437604572'), ('9788401242359'),
    ('9780061120084'), ('9780451524935'), ('9781451673319'), ('9788420412146'), ('9780141439518'),
    ('9780143058144'), ('9780140268867'), ('9780743477123'), ('9780547928227'), ('9780062316097'),
    ('9780062464316'), ('9780393317558'), ('9780553380163'), ('9780345539434'), ('9780198788607'),
    ('9780142410318'), ('9788478884452'), ('9780132350884'), ('9780135957059'), ('9780201633610');

CREATE TABLE #CodigoSeed (Codigo varchar(30) NOT NULL PRIMARY KEY);
INSERT INTO #CodigoSeed (Codigo)
SELECT 'BIB-' + RIGHT('000' + CAST(n AS varchar(3)), 3)
FROM (
    SELECT TOP (51) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects
) AS numeros;

CREATE TABLE #CorreoLectorSeed (Correo varchar(150) NOT NULL PRIMARY KEY);
INSERT INTO #CorreoLectorSeed (Correo) VALUES
    ('ana.lopez@demo.local'),
    ('carlos.mendez@demo.local');

CREATE TABLE #CorreoUsuarioDemo (Correo varchar(150) NOT NULL PRIMARY KEY);
INSERT INTO #CorreoUsuarioDemo (Correo) VALUES
    ('admin@demo.local'),
    ('ana.lopez@demo.local');

SELECT l.IdLibro
INTO #LibrosPrueba
FROM Libros AS l
WHERE NOT EXISTS (SELECT 1 FROM #IsbnSeed AS s WHERE s.Isbn = l.Isbn)
  AND EXISTS (
      SELECT 1
      FROM @Patrones AS p
      WHERE l.Titulo COLLATE Latin1_General_CI_AI LIKE p.Patron
         OR l.Autor COLLATE Latin1_General_CI_AI LIKE p.Patron
  );

SELECT e.IdEjemplar
INTO #EjemplaresPrueba
FROM Ejemplares AS e
WHERE e.IdLibro IN (SELECT IdLibro FROM #LibrosPrueba)
   OR (
        NOT EXISTS (SELECT 1 FROM #CodigoSeed AS s WHERE s.Codigo = e.CodigoInventario)
        AND EXISTS (
            SELECT 1 FROM @Patrones AS p
            WHERE e.CodigoInventario COLLATE Latin1_General_CI_AI LIKE p.Patron
        )
   );

SELECT l.IdLector
INTO #LectoresPrueba
FROM Lectores AS l
WHERE NOT EXISTS (SELECT 1 FROM #CorreoLectorSeed AS s WHERE s.Correo = l.Correo)
  AND EXISTS (
      SELECT 1
      FROM @Patrones AS p
      WHERE l.Nombre COLLATE Latin1_General_CI_AI LIKE p.Patron
         OR l.Correo COLLATE Latin1_General_CI_AI LIKE p.Patron
         OR l.Identificador COLLATE Latin1_General_CI_AI LIKE p.Patron
  );

SELECT u.IdUsuario
INTO #UsuariosPrueba
FROM Usuarios AS u
WHERE NOT EXISTS (SELECT 1 FROM #CorreoUsuarioDemo AS s WHERE s.Correo = u.Correo)
  AND (
      u.IdLector IN (SELECT IdLector FROM #LectoresPrueba)
      OR EXISTS (
          SELECT 1
          FROM @Patrones AS p
          WHERE u.Nombre COLLATE Latin1_General_CI_AI LIKE p.Patron
             OR u.Correo COLLATE Latin1_General_CI_AI LIKE p.Patron
      )
  );

SELECT r.IdReserva
INTO #ReservasPrueba
FROM Reservas AS r
WHERE r.IdLector IN (SELECT IdLector FROM #LectoresPrueba)
   OR r.IdEjemplar IN (SELECT IdEjemplar FROM #EjemplaresPrueba)
   OR EXISTS (
        SELECT 1
        FROM Ejemplares AS e
        WHERE e.IdEjemplar = r.IdEjemplar
          AND e.IdLibro IN (SELECT IdLibro FROM #LibrosPrueba)
   );

SELECT Tabla, Registros
FROM (
    SELECT 'Reservas' AS Tabla, COUNT(*) AS Registros FROM #ReservasPrueba
    UNION ALL SELECT 'Ejemplares', COUNT(*) FROM #EjemplaresPrueba
    UNION ALL SELECT 'Libros', COUNT(*) FROM #LibrosPrueba
    UNION ALL SELECT 'Lectores', COUNT(*) FROM #LectoresPrueba
    UNION ALL SELECT 'Usuarios', COUNT(*) FROM #UsuariosPrueba
) AS preview
ORDER BY Tabla;

SELECT 'Reserva' AS Tipo, r.IdReserva, r.IdLector, r.IdEjemplar, r.Estado
FROM Reservas AS r
WHERE r.IdReserva IN (SELECT IdReserva FROM #ReservasPrueba);

SELECT 'Ejemplar' AS Tipo, e.IdEjemplar, e.CodigoInventario, e.IdLibro, e.Estado
FROM Ejemplares AS e
WHERE e.IdEjemplar IN (SELECT IdEjemplar FROM #EjemplaresPrueba);

SELECT 'Libro' AS Tipo, l.IdLibro, l.Isbn, l.Titulo
FROM Libros AS l
WHERE l.IdLibro IN (SELECT IdLibro FROM #LibrosPrueba);

SELECT 'Lector' AS Tipo, l.IdLector, l.Identificador, l.Correo, l.Nombre
FROM Lectores AS l
WHERE l.IdLector IN (SELECT IdLector FROM #LectoresPrueba);

SELECT 'Usuario' AS Tipo, u.IdUsuario, u.Correo, u.Nombre, u.Rol
FROM Usuarios AS u
WHERE u.IdUsuario IN (SELECT IdUsuario FROM #UsuariosPrueba);

BEGIN TRAN;

DELETE FROM Reservas
WHERE IdReserva IN (SELECT IdReserva FROM #ReservasPrueba);

UPDATE e
SET Estado = 'Disponible',
    UpdatedAt = SYSUTCDATETIME()
FROM Ejemplares AS e
WHERE e.Estado = 'Reservado'
  AND e.IdEjemplar NOT IN (SELECT IdEjemplar FROM #EjemplaresPrueba)
  AND NOT EXISTS (
      SELECT 1
      FROM Reservas AS r
      WHERE r.IdEjemplar = e.IdEjemplar
        AND r.Estado = 'Activa'
  );

DELETE FROM Ejemplares
WHERE IdEjemplar IN (SELECT IdEjemplar FROM #EjemplaresPrueba);

DELETE FROM Libros
WHERE IdLibro IN (SELECT IdLibro FROM #LibrosPrueba);

DELETE FROM Usuarios
WHERE IdUsuario IN (SELECT IdUsuario FROM #UsuariosPrueba);

DELETE FROM Lectores
WHERE IdLector IN (SELECT IdLector FROM #LectoresPrueba);

SELECT Tabla, Registros
FROM (
    SELECT 'Reservas' AS Tabla, COUNT(*) AS Registros FROM Reservas
    UNION ALL SELECT 'Ejemplares', COUNT(*) FROM Ejemplares
    UNION ALL SELECT 'Libros', COUNT(*) FROM Libros
    UNION ALL SELECT 'Lectores', COUNT(*) FROM Lectores
    UNION ALL SELECT 'Usuarios', COUNT(*) FROM Usuarios
) AS despues
ORDER BY Tabla;

IF @Confirmar = 1
    COMMIT TRAN;
ELSE
    ROLLBACK TRAN;
