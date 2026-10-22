/* ============================================================================
   LubricentroControl 2026 — Actualización del 2026-10-22: restricciones en la
   cuenta corriente de proveedores

   Para una base creada ANTES del 22/10 con 01_Esquema.sql, sin perder sus datos.
   Una base nueva no lo necesita: 01_Esquema.sql ya trae estas restricciones.

   Agrega a CuentaCorrienteProveedor las mismas que tiene desde el 2026-10-18 la
   de clientes:
     - CK_CCProv_debeHaber: un movimiento va al debe o al haber, nunca a los dos.
     - CK_CCProv_origen: cada tipo con su origen. Una Compra apunta a su compra,
       un Pago a su pago, y un Ajuste a ninguna de las dos pero guarda quién lo
       hizo.

   Antes de cambiar nada cuenta las filas que no cumplirían cada restricción: si
   hay alguna, no toca la base y muestra cuáles son, para corregirlas a mano.
   Agrega las dos juntas o ninguna (una sola transacción) y se puede correr más
   de una vez: lo que ya existe no se vuelve a agregar.

   Uso:
     sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i Database\Actualizar_2026-10-22_CuentaCorrienteProveedor.sql
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
SELECT 'CK_CCProv_debeHaber', COUNT(*) FROM CuentaCorrienteProveedor
WHERE NOT (debe >= 0 AND haber >= 0 AND (debe = 0 OR haber = 0))
UNION ALL
SELECT 'CK_CCProv_origen', COUNT(*) FROM CuentaCorrienteProveedor
WHERE NOT ((tipoMovimiento = 'Compra' AND idCompra IS NOT NULL AND idPago IS NULL) OR
           (tipoMovimiento = 'Pago'   AND idPago IS NOT NULL AND idCompra IS NULL) OR
           (tipoMovimiento = 'Ajuste' AND idCompra IS NULL AND idPago IS NULL AND idUsuario IS NOT NULL));

IF EXISTS (SELECT 1 FROM @problemas WHERE filas > 0)
BEGIN
    SELECT restriccion AS [Restricción que no se cumpliría], filas AS [Filas]
    FROM @problemas WHERE filas > 0;

    RAISERROR('Hay datos que no cumplen las restricciones nuevas (arriba, cuántas filas por cada una). No se cambió nada: corregirlos y volver a correr el script.', 16, 1);
    RETURN;
END

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CCProv_debeHaber')
    ALTER TABLE CuentaCorrienteProveedor ADD CONSTRAINT CK_CCProv_debeHaber
        CHECK (debe >= 0 AND haber >= 0 AND (debe = 0 OR haber = 0));

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CCProv_origen')
    ALTER TABLE CuentaCorrienteProveedor ADD CONSTRAINT CK_CCProv_origen CHECK (
        (tipoMovimiento = 'Compra' AND idCompra IS NOT NULL AND idPago IS NULL) OR
        (tipoMovimiento = 'Pago'   AND idPago IS NOT NULL AND idCompra IS NULL) OR
        (tipoMovimiento = 'Ajuste' AND idCompra IS NULL AND idPago IS NULL AND idUsuario IS NOT NULL));

COMMIT TRANSACTION;

PRINT 'Listo: las restricciones del 2026-10-22 están en la base.';
GO
