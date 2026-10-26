/* ============================================================================
   LubricentroControl 2026 — Actualización del 2026-10-26: pantalla "Asistente IA"

   Para una base creada ANTES de este cambio, sin perder sus datos. Una base nueva
   no lo necesita: 02_DatosIniciales.sql ya trae la pantalla.

   Agrega la pantalla ~/Reportes/Asistente al menú, dentro del grupo Reportes (orden 4,
   texto "Asistente IA"), y se la da a los mismos roles que ya ven los reportes: copia
   el permiso (incluido soloLectura) de cada rol que tiene "Ventas por período". Como
   en 02_DatosIniciales.sql, también asegura la fila del grupo Reportes en esos roles:
   sin ella la pantalla no aparece en el menú. Sin tablas nuevas.

   Antes de cambiar nada comprueba que exista el grupo Reportes: si falta, no toca la
   base. Todo en una sola transacción y se puede correr más de una vez: lo que ya
   existe no se vuelve a agregar.

   Uso:
     sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i Database\Actualizar_2026-10-26_Asistente.sql
   ============================================================================ */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

USE LubricentroControl;
GO

/* Todo en un solo batch: un RETURN corta el batch, no el script entero. */
DECLARE @gReportes INT = (SELECT TOP 1 idMenu FROM Menu
                          WHERE texto = N'Reportes' AND idUrl IS NULL AND idMenuPadre IS NULL);
IF @gReportes IS NULL
BEGIN
    RAISERROR('Falta el grupo Reportes del menú (que carga 02_DatosIniciales.sql). No se cambió nada.', 16, 1);
    RETURN;
END

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM Url WHERE path = '~/Reportes/Asistente')
    INSERT INTO Url (descripcion, path) VALUES (N'Asistente de consultas con IA', '~/Reportes/Asistente');

DECLARE @idUrl INT = (SELECT idUrl FROM Url WHERE path = '~/Reportes/Asistente');

IF NOT EXISTS (SELECT 1 FROM Menu WHERE idUrl = @idUrl)
    INSERT INTO Menu (texto, idUrl, idMenuPadre, orden)
    VALUES (N'Asistente IA', @idUrl, @gReportes, 4);

DECLARE @idMenu INT = (SELECT TOP 1 idMenu FROM Menu WHERE idUrl = @idUrl);

/* Los roles que hoy ven los reportes (Admin y Encargado de base): la pantalla del
   asistente y, si hiciera falta, el grupo Reportes que la contiene. */
INSERT INTO MenuNivel (idMenu, idNivel, soloLectura)
SELECT @idMenu, r.idNivel, r.soloLectura
FROM MenuNivel r
JOIN Menu m ON m.idMenu = r.idMenu
JOIN Url u ON u.idUrl = m.idUrl
WHERE u.path = '~/Reportes/VentasPorPeriodo'
  AND NOT EXISTS (SELECT 1 FROM MenuNivel x WHERE x.idMenu = @idMenu AND x.idNivel = r.idNivel);

INSERT INTO MenuNivel (idMenu, idNivel, soloLectura)
SELECT @gReportes, a.idNivel, a.soloLectura
FROM MenuNivel a
WHERE a.idMenu = @idMenu
  AND NOT EXISTS (SELECT 1 FROM MenuNivel x WHERE x.idMenu = @gReportes AND x.idNivel = a.idNivel);

COMMIT TRANSACTION;

PRINT 'Listo: la pantalla Asistente IA está en el menú Reportes.';
GO
