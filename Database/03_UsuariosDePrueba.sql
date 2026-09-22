/* ============================================================================
   LubricentroControl 2026 — Usuarios de prueba (OPCIONAL)

   Crea un Encargado, un Empleado y un Lectura para poder verificar a mano
   cómo cambia el menú y qué pantallas quedan bloqueadas según el rol.
   No correr en producción.

     sqlcmd -S "(localdb)\MSSQLLocalDB" -i Database\03_UsuariosDePrueba.sql

   encargado@lubricentro.com  /  Encargado123!
   empleado@lubricentro.com   /  Empleado123!
   lectura@lubricentro.com    /  Lectura123!
   ============================================================================ */

USE LubricentroControl;
GO

SET NOCOUNT ON;

DELETE FROM Usuario WHERE email IN
    ('encargado@lubricentro.com', 'empleado@lubricentro.com', 'lectura@lubricentro.com');

INSERT INTO Usuario (nombre, apellido, email, passwordHash, passwordSalt, idNivel, activo)
VALUES
    ('María', 'Encargada', 'encargado@lubricentro.com',
     'kiibiBe7kzjvqW8pJ61/yCmZOQI10ftrInTDprGxaFE=',
     'o+DUMYNLGZOEW1AflssdLQ==',
     (SELECT idNivel FROM Nivel WHERE nombre = 'Encargado'), 1),

    ('Juan', 'Empleado', 'empleado@lubricentro.com',
     'iUYR/mOBb1JCTfdZh9PvkOINRquIqPJSqbi2uw2j97s=',
     'QsIlr5AyR7iqExhbQNr8dw==',
     (SELECT idNivel FROM Nivel WHERE nombre = 'Empleado'), 1),

    ('Rosa', 'Lectora', 'lectura@lubricentro.com',
     'eeL2upJWQa7bVeKoAF3/p0m1DVMCV/zTdpf/3sccV4g=',
     'yCIclzl8CcwJxZm66rGBLw==',
     (SELECT idNivel FROM Nivel WHERE nombre = 'Lectura'), 1);

GO

PRINT 'Usuarios de prueba creados.';
PRINT '  encargado@lubricentro.com / Encargado123!';
PRINT '  empleado@lubricentro.com  / Empleado123!';
PRINT '  lectura@lubricentro.com   / Lectura123!';
GO
