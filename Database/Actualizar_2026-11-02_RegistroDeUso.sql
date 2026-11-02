/* ============================================================================
   LubricentroControl 2026 — Actualización del 2026-11-02: registro de uso

   Para una base creada ANTES de este cambio con 01_Esquema.sql, sin perder sus
   datos. Una base nueva no lo necesita: 01_Esquema.sql ya trae las tablas.

   Crea las dos tablas que llena la aplicación y lee el asistente de consultas
   (tiempo en sesión, páginas más visitadas y usuarios que más ingresan):
   - RegistroSesion: una fila por cada inicio de sesión (inicio, última
     actividad y fin, NULL si la sesión venció sin "Cerrar sesión").
   - VisitaPagina: una fila por cada pantalla abierta dentro de una sesión.
   Arrancan vacías: el uso se empieza a registrar desde que se corre esto.
   lubricentro_app ya las puede leer y escribir (db_datareader y db_datawriter).

   Todo en una sola transacción y se puede correr más de una vez: lo que ya
   existe no se vuelve a crear.

   Uso:
     sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i Database\Actualizar_2026-11-02_RegistroDeUso.sql
   ============================================================================ */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

USE LubricentroControl;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.RegistroSesion', 'U') IS NULL
    CREATE TABLE RegistroSesion (
        idRegistroSesion INT IDENTITY(1,1) NOT NULL,
        idUsuario        INT               NOT NULL,
        inicio           DATETIME          NOT NULL CONSTRAINT DF_RegistroSesion_inicio DEFAULT (GETDATE()),
        ultimaActividad  DATETIME          NOT NULL CONSTRAINT DF_RegistroSesion_ultimaActividad DEFAULT (GETDATE()),
        fin              DATETIME          NULL,
        CONSTRAINT PK_RegistroSesion PRIMARY KEY (idRegistroSesion),
        CONSTRAINT FK_RegistroSesion_Usuario FOREIGN KEY (idUsuario) REFERENCES Usuario(idUsuario)
    );

IF OBJECT_ID('dbo.VisitaPagina', 'U') IS NULL
    CREATE TABLE VisitaPagina (
        idVisitaPagina   INT IDENTITY(1,1) NOT NULL,
        idRegistroSesion INT               NOT NULL,
        idUsuario        INT               NOT NULL,
        path             NVARCHAR(200)     NOT NULL,
        fecha            DATETIME          NOT NULL CONSTRAINT DF_VisitaPagina_fecha DEFAULT (GETDATE()),
        CONSTRAINT PK_VisitaPagina PRIMARY KEY (idVisitaPagina),
        CONSTRAINT FK_VisitaPagina_RegistroSesion FOREIGN KEY (idRegistroSesion) REFERENCES RegistroSesion(idRegistroSesion),
        CONSTRAINT FK_VisitaPagina_Usuario FOREIGN KEY (idUsuario) REFERENCES Usuario(idUsuario)
    );

COMMIT TRANSACTION;
GO

/* Los índices en otro batch: en el anterior las tablas todavía no existían
   cuando se compiló. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RegistroSesion_inicio')
    CREATE INDEX IX_RegistroSesion_inicio ON RegistroSesion(inicio);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VisitaPagina_fecha')
    CREATE INDEX IX_VisitaPagina_fecha ON VisitaPagina(fecha);

IF OBJECT_ID('dbo.RegistroSesion', 'U') IS NOT NULL AND OBJECT_ID('dbo.VisitaPagina', 'U') IS NOT NULL
    PRINT 'Listo: RegistroSesion y VisitaPagina creadas. El uso se registra desde ahora.';
GO
