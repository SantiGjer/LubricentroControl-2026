/* ============================================================================
   LubricentroControl 2026 — Actualización del 2026-10-27: asistente de ventas y stock

   Para una base creada antes del 27/10, sin perder sus datos. Una base nueva no
   lo necesita: 02_DatosIniciales.sql ya trae la pantalla en el menú.

   - Agrega la pantalla ~/Reportes/AsistenteVentas (Requerimientos §9.21) al
     menú Reportes, con permiso para los mismos roles que hoy ven los reportes
     (Admin y Encargado de base).

   No crea tablas: el asistente solo lee. Todo va en una sola transacción y se
   puede correr más de una vez.

   Uso:
     sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i Database\Actualizar_2026-10-27_AsistenteVentas.sql
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
    RAISERROR('Falta el grupo Reportes del menú (lo carga 02_DatosIniciales.sql). No se cambió nada.', 16, 1);
    RETURN;
END

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM Url WHERE path = '~/Reportes/AsistenteVentas')
    INSERT INTO Url (descripcion, path) VALUES (N'Asistente de ventas y stock con IA', '~/Reportes/AsistenteVentas');

DECLARE @idUrl INT = (SELECT idUrl FROM Url WHERE path = '~/Reportes/AsistenteVentas');

IF NOT EXISTS (SELECT 1 FROM Menu WHERE idUrl = @idUrl)
    INSERT INTO Menu (texto, idUrl, idMenuPadre, orden)
    VALUES (N'Asistente de ventas', @idUrl, @gReportes, 5);

DECLARE @idMenu INT = (SELECT TOP 1 idMenu FROM Menu WHERE idUrl = @idUrl);

/* Los roles que hoy ven el reporte de ventas: la pantalla del asistente y, si hiciera
   falta, el grupo Reportes que la contiene. */
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

PRINT 'Listo: la pantalla Asistente de ventas está en el menú Reportes.';
GO
