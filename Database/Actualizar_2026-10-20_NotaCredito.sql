/* ============================================================================
   LubricentroControl 2026 — Actualización del 2026-10-20: nota de crédito

   Para una base creada antes del 20/10, sin perder sus datos. Una base nueva no
   lo necesita: 01_Esquema.sql ya lo trae.

   - Crea la tabla NotaCredito (Requerimientos §9.17).
   - Deja CK_CCCli_origen admitiendo el movimiento NotaCredito de la cuenta
     corriente de clientes: si la restricción ya existe con la definición
     anterior (la del 2026-10-18), la reemplaza; si no existe, la agrega.

   Antes de cambiar nada cuenta los movimientos de cuenta corriente que no
   cumplirían esa restricción: si hay alguno, no toca la base y avisa. Todo va
   en una sola transacción y se puede correr más de una vez.

   Uso:
     sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i Database\Actualizar_2026-10-20_NotaCredito.sql
   ============================================================================ */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

USE LubricentroControl;
GO

/* Todo en un solo batch: un RETURN corta el batch, no el script entero. */
DECLARE @filas INT = (
    SELECT COUNT(*) FROM CuentaCorrienteCliente
    WHERE NOT ((tipoMovimiento = 'Venta'       AND idVenta IS NOT NULL AND idPago IS NULL) OR
               (tipoMovimiento = 'Pago'        AND idPago IS NOT NULL AND idVenta IS NULL) OR
               (tipoMovimiento = 'NotaCredito' AND idVenta IS NOT NULL AND idPago IS NULL AND idUsuario IS NOT NULL) OR
               (tipoMovimiento = 'Ajuste'      AND idVenta IS NULL AND idPago IS NULL AND idUsuario IS NOT NULL)));

IF @filas > 0
BEGIN
    RAISERROR('Hay %d movimientos de cuenta corriente de clientes que no cumplen CK_CCCli_origen. No se cambió nada: corregirlos y volver a correr el script.', 16, 1, @filas);
    RETURN;
END

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.NotaCredito', 'U') IS NULL
    CREATE TABLE NotaCredito (
        idNotaCredito            INT IDENTITY(1,1) NOT NULL,
        idVenta                  INT               NOT NULL,
        idFactura                INT               NULL,
        tipo                     CHAR(1)           NOT NULL,
        puntoVenta               INT               NOT NULL,
        numero                   INT               NOT NULL,
        fecha                    DATETIME          NOT NULL CONSTRAINT DF_NotaCredito_fecha DEFAULT (GETDATE()),
        idUsuario                INT               NOT NULL,
        motivo                   NVARCHAR(300)     NOT NULL,
        condicionVenta           NVARCHAR(30)      NOT NULL,
        subtotal                 DECIMAL(12,2)     NOT NULL,
        impuestos                DECIMAL(12,2)     NOT NULL,
        total                    DECIMAL(12,2)     NOT NULL,
        emisorRazonSocial        NVARCHAR(150)     NOT NULL,
        emisorCuit               NVARCHAR(20)      NOT NULL,
        emisorCondicionIva       NVARCHAR(30)      NOT NULL,
        emisorDomicilio          NVARCHAR(300)     NULL,
        emisorIngresosBrutos     NVARCHAR(30)      NULL,
        emisorInicioActividades  NVARCHAR(20)      NULL,
        receptorNombre           NVARCHAR(150)     NOT NULL,
        receptorTipoDocumento    NVARCHAR(10)      NOT NULL,
        receptorNumeroDocumento  NVARCHAR(20)      NOT NULL,
        receptorCondicionIva     NVARCHAR(30)      NOT NULL,
        receptorDomicilio        NVARCHAR(400)     NULL,
        CONSTRAINT PK_NotaCredito PRIMARY KEY (idNotaCredito),
        CONSTRAINT UQ_NotaCredito_venta UNIQUE (idVenta),
        CONSTRAINT UQ_NotaCredito_numero UNIQUE (tipo, puntoVenta, numero),
        CONSTRAINT FK_NotaCredito_Venta FOREIGN KEY (idVenta) REFERENCES ComprobanteVenta(idVenta),
        CONSTRAINT FK_NotaCredito_Factura FOREIGN KEY (idFactura) REFERENCES Factura(idFactura),
        CONSTRAINT FK_NotaCredito_Usuario FOREIGN KEY (idUsuario) REFERENCES Usuario(idUsuario),
        CONSTRAINT CK_NotaCredito_tipo CHECK (tipo IN ('A','B','C')),
        CONSTRAINT CK_NotaCredito_puntoVenta CHECK (puntoVenta BETWEEN 1 AND 99999),
        CONSTRAINT CK_NotaCredito_numero CHECK (numero > 0),
        CONSTRAINT CK_NotaCredito_importes CHECK (subtotal >= 0 AND impuestos >= 0 AND total >= 0)
    );

IF EXISTS (SELECT 1 FROM sys.check_constraints
           WHERE name = 'CK_CCCli_origen' AND definition NOT LIKE '%NotaCredito%')
    ALTER TABLE CuentaCorrienteCliente DROP CONSTRAINT CK_CCCli_origen;

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CCCli_origen')
    ALTER TABLE CuentaCorrienteCliente ADD CONSTRAINT CK_CCCli_origen CHECK (
        (tipoMovimiento = 'Venta'       AND idVenta IS NOT NULL AND idPago IS NULL) OR
        (tipoMovimiento = 'Pago'        AND idPago IS NOT NULL AND idVenta IS NULL) OR
        (tipoMovimiento = 'NotaCredito' AND idVenta IS NOT NULL AND idPago IS NULL AND idUsuario IS NOT NULL) OR
        (tipoMovimiento = 'Ajuste'      AND idVenta IS NULL AND idPago IS NULL AND idUsuario IS NOT NULL));

COMMIT TRANSACTION;

PRINT 'Listo: la base tiene la nota de crédito del 2026-10-20.';
GO
