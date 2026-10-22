/* ============================================================================
   LubricentroControl 2026 — Actualización del 2026-10-22: vínculo de Factura y
   NotaCredito con Emisor

   Para una base creada ANTES de este cambio con 01_Esquema.sql, sin perder sus
   datos. Una base nueva no lo necesita: 01_Esquema.sql ya trae la columna.

   Agrega a Factura y a NotaCredito la columna idEmisor (el comercio que la emite;
   Emisor tiene una sola fila, así que vale 1, también en los comprobantes que ya
   existen) y su clave externa: FK_Factura_Emisor y FK_NotaCredito_Emisor. Los
   datos del comercio se siguen copiando en cada comprobante.

   Antes de cambiar nada comprueba que exista el comercio (Emisor con idEmisor 1):
   si falta, no toca la base. Todo en una sola transacción y se puede correr más
   de una vez: lo que ya existe no se vuelve a agregar.

   Uso:
     sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i Database\Actualizar_2026-10-22_Emisor.sql
   ============================================================================ */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

USE LubricentroControl;
GO

/* Todo en un solo batch: un RETURN corta el batch, no el script entero. */
IF NOT EXISTS (SELECT 1 FROM Emisor WHERE idEmisor = 1)
BEGIN
    RAISERROR('Falta el comercio (la fila de Emisor con idEmisor 1, que carga 02_DatosIniciales.sql). No se cambió nada.', 16, 1);
    RETURN;
END

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Factura', 'idEmisor') IS NULL
    ALTER TABLE Factura ADD idEmisor INT NOT NULL CONSTRAINT DF_Factura_emisor DEFAULT (1) WITH VALUES;

IF COL_LENGTH('dbo.NotaCredito', 'idEmisor') IS NULL
    ALTER TABLE NotaCredito ADD idEmisor INT NOT NULL CONSTRAINT DF_NotaCredito_emisor DEFAULT (1) WITH VALUES;

COMMIT TRANSACTION;
GO

/* Las claves externas en otro batch: en el anterior la columna todavía no existía
   cuando se compiló. */
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Factura', 'idEmisor') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Factura_Emisor')
    ALTER TABLE Factura ADD CONSTRAINT FK_Factura_Emisor FOREIGN KEY (idEmisor) REFERENCES Emisor(idEmisor);

IF COL_LENGTH('dbo.NotaCredito', 'idEmisor') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_NotaCredito_Emisor')
    ALTER TABLE NotaCredito ADD CONSTRAINT FK_NotaCredito_Emisor FOREIGN KEY (idEmisor) REFERENCES Emisor(idEmisor);

COMMIT TRANSACTION;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Factura_Emisor')
   AND EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_NotaCredito_Emisor')
    PRINT 'Listo: Factura y NotaCredito están vinculadas con Emisor.';
GO
