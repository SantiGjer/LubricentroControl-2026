/* ============================================================================
   LubricentroControl 2026 — Actualización del 2026-10-18: restricciones nuevas

   Para una base creada ANTES del 18/10 con 01_Esquema.sql, sin perder sus datos.
   Una base nueva no lo necesita: 01_Esquema.sql ya trae estas restricciones.

   Agrega:
     - UQ_ComprobanteVenta_orden: una sola venta por orden de trabajo.
     - CK_Venta_importes y CK_Venta_saldo: importes no negativos y saldo
       pendiente entre 0 y el total.
     - CK_DetVenta_producto, CK_DetVenta_cantidad y CK_DetVenta_importes: cada
       línea apunta al producto de su tipo, con cantidad mayor a cero e importes
       no negativos.
     - CK_CCCli_debeHaber y CK_CCCli_origen: en la cuenta corriente de clientes,
       un movimiento va al debe o al haber, y cada tipo (Venta, Pago, Ajuste y,
       desde el 2026-10-20, NotaCredito) con su origen. La tabla NotaCredito la
       crea Actualizar_2026-10-20_NotaCredito.sql.

   Antes de cambiar nada cuenta las filas que no cumplirían cada restricción: si
   hay alguna, no toca la base y muestra cuáles son, para corregirlas a mano.
   Agrega todas juntas o ninguna (una sola transacción) y se puede correr más de
   una vez: lo que ya existe no se vuelve a agregar.

   Uso:
     sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i Database\Actualizar_2026-10-18_Restricciones.sql
   ============================================================================ */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

USE LubricentroControl;
GO

/* Todo en un solo batch: un RETURN corta el batch, no el script entero. */
DECLARE @problemas TABLE (restriccion SYSNAME, filas INT);

INSERT INTO @problemas (restriccion, filas)
SELECT 'UQ_ComprobanteVenta_orden', COUNT(*)
FROM (SELECT idOrden FROM ComprobanteVenta GROUP BY idOrden HAVING COUNT(*) > 1) repetidas
UNION ALL
SELECT 'CK_Venta_importes', COUNT(*) FROM ComprobanteVenta
WHERE NOT (subtotal >= 0 AND impuestos >= 0 AND total >= 0)
UNION ALL
SELECT 'CK_Venta_saldo', COUNT(*) FROM ComprobanteVenta
WHERE NOT (saldoPendiente >= 0 AND saldoPendiente <= total)
UNION ALL
SELECT 'CK_DetVenta_producto', COUNT(*) FROM DetalleComprobanteVenta
WHERE NOT ((tipoItem = 'S' AND idServicio IS NOT NULL AND idInsumo IS NULL) OR
           (tipoItem = 'I' AND idInsumo IS NOT NULL AND idServicio IS NULL))
UNION ALL
SELECT 'CK_DetVenta_cantidad', COUNT(*) FROM DetalleComprobanteVenta
WHERE NOT (cantidad > 0)
UNION ALL
SELECT 'CK_DetVenta_importes', COUNT(*) FROM DetalleComprobanteVenta
WHERE NOT (precioUnitario >= 0 AND subtotal >= 0 AND importeIva >= 0)
UNION ALL
SELECT 'CK_CCCli_debeHaber', COUNT(*) FROM CuentaCorrienteCliente
WHERE NOT (debe >= 0 AND haber >= 0 AND (debe = 0 OR haber = 0))
UNION ALL
SELECT 'CK_CCCli_origen', COUNT(*) FROM CuentaCorrienteCliente
WHERE NOT ((tipoMovimiento = 'Venta'       AND idVenta IS NOT NULL AND idPago IS NULL) OR
           (tipoMovimiento = 'Pago'        AND idPago IS NOT NULL AND idVenta IS NULL) OR
           (tipoMovimiento = 'NotaCredito' AND idVenta IS NOT NULL AND idPago IS NULL AND idUsuario IS NOT NULL) OR
           (tipoMovimiento = 'Ajuste'      AND idVenta IS NULL AND idPago IS NULL AND idUsuario IS NOT NULL));

IF EXISTS (SELECT 1 FROM @problemas WHERE filas > 0)
BEGIN
    SELECT restriccion AS [Restricción que no se cumpliría], filas AS [Filas]
    FROM @problemas WHERE filas > 0;

    RAISERROR('Hay datos que no cumplen las restricciones nuevas (arriba, cuántas filas por cada una). No se cambió nada: corregirlos y volver a correr el script.', 16, 1);
    RETURN;
END

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_ComprobanteVenta_orden')
    ALTER TABLE ComprobanteVenta ADD CONSTRAINT UQ_ComprobanteVenta_orden UNIQUE (idOrden);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Venta_importes')
    ALTER TABLE ComprobanteVenta ADD CONSTRAINT CK_Venta_importes
        CHECK (subtotal >= 0 AND impuestos >= 0 AND total >= 0);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Venta_saldo')
    ALTER TABLE ComprobanteVenta ADD CONSTRAINT CK_Venta_saldo
        CHECK (saldoPendiente >= 0 AND saldoPendiente <= total);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_DetVenta_producto')
    ALTER TABLE DetalleComprobanteVenta ADD CONSTRAINT CK_DetVenta_producto CHECK (
        (tipoItem = 'S' AND idServicio IS NOT NULL AND idInsumo IS NULL) OR
        (tipoItem = 'I' AND idInsumo IS NOT NULL AND idServicio IS NULL));

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_DetVenta_cantidad')
    ALTER TABLE DetalleComprobanteVenta ADD CONSTRAINT CK_DetVenta_cantidad CHECK (cantidad > 0);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_DetVenta_importes')
    ALTER TABLE DetalleComprobanteVenta ADD CONSTRAINT CK_DetVenta_importes
        CHECK (precioUnitario >= 0 AND subtotal >= 0 AND importeIva >= 0);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CCCli_debeHaber')
    ALTER TABLE CuentaCorrienteCliente ADD CONSTRAINT CK_CCCli_debeHaber
        CHECK (debe >= 0 AND haber >= 0 AND (debe = 0 OR haber = 0));

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CCCli_origen')
    ALTER TABLE CuentaCorrienteCliente ADD CONSTRAINT CK_CCCli_origen CHECK (
        (tipoMovimiento = 'Venta'       AND idVenta IS NOT NULL AND idPago IS NULL) OR
        (tipoMovimiento = 'Pago'        AND idPago IS NOT NULL AND idVenta IS NULL) OR
        (tipoMovimiento = 'NotaCredito' AND idVenta IS NOT NULL AND idPago IS NULL AND idUsuario IS NOT NULL) OR
        (tipoMovimiento = 'Ajuste'      AND idVenta IS NULL AND idPago IS NULL AND idUsuario IS NOT NULL));

COMMIT TRANSACTION;

PRINT 'Listo: las restricciones del 2026-10-18 están en la base.';
GO
