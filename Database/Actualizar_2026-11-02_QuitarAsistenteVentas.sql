/* ============================================================================
   LubricentroControl 2026 — Actualización del 2026-11-02: un solo asistente

   Para una base que corrió Actualizar_2026-10-27_AsistenteVentas.sql (ya borrado
   del repositorio). Las preguntas de ventas y stock pasaron a la pantalla
   ~/Reportes/Asistente, debajo de las de consultas (Requerimientos §9.21), y la
   pantalla ~/Reportes/AsistenteVentas dejó de existir.

   - Borra del menú la pantalla ~/Reportes/AsistenteVentas: sus permisos
     (MenuNivel), su opción de menú (Menu) y su dirección (Url).

   No toca nada más: el acceso a ~/Reportes/Asistente y al grupo Reportes queda
   como está. Una base nueva no lo necesita (02_DatosIniciales.sql ya no trae la
   pantalla). Todo va en una sola transacción y se puede correr más de una vez.

   Uso:
     sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i Database\Actualizar_2026-11-02_QuitarAsistenteVentas.sql
   ============================================================================ */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

USE LubricentroControl;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DELETE mn
FROM MenuNivel mn
JOIN Menu m ON m.idMenu = mn.idMenu
JOIN Url u ON u.idUrl = m.idUrl
WHERE u.path = '~/Reportes/AsistenteVentas';

DELETE m
FROM Menu m
JOIN Url u ON u.idUrl = m.idUrl
WHERE u.path = '~/Reportes/AsistenteVentas';

DELETE FROM Url WHERE path = '~/Reportes/AsistenteVentas';

COMMIT TRANSACTION;

PRINT 'Listo: la pantalla Asistente de ventas ya no está en el menú (ahora vive en Asistente IA).';
GO
