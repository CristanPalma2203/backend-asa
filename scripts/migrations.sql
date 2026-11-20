IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    CREATE TABLE [Lectores] (
        [IdLector] int NOT NULL IDENTITY,
        [Identificador] varchar(20) NOT NULL,
        [Nombre] nvarchar(150) NOT NULL,
        [Correo] varchar(150) NOT NULL,
        [Estado] varchar(20) NOT NULL,
        CONSTRAINT [PK_Lectores] PRIMARY KEY ([IdLector]),
        CONSTRAINT [CK_Lector_Estado] CHECK ([Estado] IN ('Activo', 'Inactivo'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    CREATE TABLE [Libros] (
        [IdLibro] int NOT NULL IDENTITY,
        [Isbn] varchar(20) NOT NULL,
        [Titulo] nvarchar(200) NOT NULL,
        [Autor] nvarchar(150) NOT NULL,
        [Categoria] nvarchar(80) NOT NULL,
        [Estado] varchar(20) NOT NULL,
        CONSTRAINT [PK_Libros] PRIMARY KEY ([IdLibro]),
        CONSTRAINT [CK_Libro_Estado] CHECK ([Estado] IN ('Activo', 'Inactivo'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    CREATE TABLE [Ejemplares] (
        [IdEjemplar] int NOT NULL IDENTITY,
        [IdLibro] int NOT NULL,
        [CodigoInventario] varchar(30) NOT NULL,
        [Estado] varchar(20) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Ejemplares] PRIMARY KEY ([IdEjemplar]),
        CONSTRAINT [CK_Ejemplar_Estado] CHECK ([Estado] IN ('Disponible', 'Reservado', 'Prestado', 'Inactivo')),
        CONSTRAINT [FK_Ejemplares_Libros_IdLibro] FOREIGN KEY ([IdLibro]) REFERENCES [Libros] ([IdLibro]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    CREATE TABLE [Reservas] (
        [IdReserva] int NOT NULL IDENTITY,
        [IdLector] int NOT NULL,
        [IdEjemplar] int NOT NULL,
        [FechaSolicitud] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [Estado] varchar(20) NOT NULL,
        [FechaCancelacion] datetime2 NULL,
        CONSTRAINT [PK_Reservas] PRIMARY KEY ([IdReserva]),
        CONSTRAINT [CK_Reserva_Estado] CHECK ([Estado] IN ('Activa', 'Cancelada', 'Completada')),
        CONSTRAINT [CK_Reserva_FechaCancelacion] CHECK (([Estado] <> 'Cancelada') OR ([FechaCancelacion] IS NOT NULL)),
        CONSTRAINT [FK_Reservas_Ejemplares_IdEjemplar] FOREIGN KEY ([IdEjemplar]) REFERENCES [Ejemplares] ([IdEjemplar]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reservas_Lectores_IdLector] FOREIGN KEY ([IdLector]) REFERENCES [Lectores] ([IdLector]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Ejemplares_CodigoInventario] ON [Ejemplares] ([CodigoInventario]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    CREATE INDEX [IX_Ejemplares_Estado] ON [Ejemplares] ([Estado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    CREATE INDEX [IX_Ejemplares_IdLibro] ON [Ejemplares] ([IdLibro]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    CREATE INDEX [IX_Ejemplares_IdLibro_Estado] ON [Ejemplares] ([IdLibro], [Estado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Lectores_Correo] ON [Lectores] ([Correo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Lectores_Identificador] ON [Lectores] ([Identificador]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    CREATE INDEX [IX_Libros_Estado] ON [Libros] ([Estado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Libros_Isbn] ON [Libros] ([Isbn]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    CREATE INDEX [IX_Reservas_Estado] ON [Reservas] ([Estado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    CREATE INDEX [IX_Reservas_IdEjemplar] ON [Reservas] ([IdEjemplar]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Reservas_IdEjemplar_Activa] ON [Reservas] ([IdEjemplar]) WHERE [Estado] = ''Activa''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    CREATE INDEX [IX_Reservas_IdLector] ON [Reservas] ([IdLector]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261119211326_Inicial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261119211326_Inicial', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120045345_AgregarUsuarios'
)
BEGIN
    CREATE TABLE [Usuarios] (
        [IdUsuario] int NOT NULL IDENTITY,
        [Correo] varchar(150) NOT NULL,
        [Nombre] nvarchar(150) NOT NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [Rol] varchar(20) NOT NULL,
        [Estado] varchar(20) NOT NULL,
        [IdLector] int NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([IdUsuario]),
        CONSTRAINT [CK_Usuario_Estado] CHECK ([Estado] IN ('Activo', 'Inactivo')),
        CONSTRAINT [CK_Usuario_Rol] CHECK ([Rol] IN ('Admin', 'Lector')),
        CONSTRAINT [FK_Usuarios_Lectores_IdLector] FOREIGN KEY ([IdLector]) REFERENCES [Lectores] ([IdLector]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120045345_AgregarUsuarios'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuarios_Correo] ON [Usuarios] ([Correo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120045345_AgregarUsuarios'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Usuarios_IdLector] ON [Usuarios] ([IdLector]) WHERE [IdLector] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120045345_AgregarUsuarios'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261120045345_AgregarUsuarios', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191826_QuitarIndiceEjemplaresIdLibro'
)
BEGIN
    DROP INDEX [IX_Ejemplares_IdLibro] ON [Ejemplares];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120191826_QuitarIndiceEjemplaresIdLibro'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261120191826_QuitarIndiceEjemplaresIdLibro', N'10.0.15');
END;

COMMIT;
GO

