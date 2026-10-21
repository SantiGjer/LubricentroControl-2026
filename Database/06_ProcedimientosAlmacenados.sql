/* ============================================================================
   LubricentroControl 2026 — Procedimientos almacenados
   Opcional. Se corre después de 01 y 02 (03 a 05 son opcionales). No modifica tablas
   ni datos existentes: solo crea (o reemplaza) los procedimientos. Se puede correr
   varias veces, y 01 no los borra.

   La aplicación no los usa: sus consultas están en las clases de BIZ/Data. Se corren
   y se ejecutan con un usuario administrador; lubricentro_app no tiene permiso
   EXECUTE (solo lee y escribe datos).

   Uso:
     sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i "Database\06_ProcedimientosAlmacenados.sql"

   Operaciones (con transacción):
     sp_AjustarStock          — ajuste manual de stock de un insumo + su movimiento en el kardex
     sp_RegistrarPagoCliente  — pago de un cliente: cancela sus deudas de la más vieja a la más
                                nueva y deja el sobrante a cuenta, con su movimiento de cuenta corriente
   Consultas:
     sp_StockBajo             — insumos activos por debajo del stock mínimo
     sp_VentasPorPeriodo      — ventas entre dos fechas (detalle y totales)
     sp_SaldosCuentaCorriente — clientes con saldo distinto de cero
     sp_HistorialVehiculo     — órdenes de trabajo y ventas de un vehículo por patente

   Replican la lógica que hoy hace la aplicación en BIZ/Data (MovimientoStockDAL,
   PagoDAL, ProductoDAL, ComprobanteVentaDAL, CuentaCorrienteClienteDAL).
   ============================================================================ */

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

USE LubricentroControl;
GO

/* ---------------------------------------------------------------------------
   sp_AjustarStock
   Suma o resta stock a un insumo y lo deja registrado en MovimientoStock, todo en
   una sola transacción (si algo falla, no queda ni el cambio de stock ni el movimiento).
   Rechaza dejar el stock en negativo.
   --------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE sp_AjustarStock
    @idInsumo   INT,
    @cantidad   DECIMAL(12,2),
    @esEntrada  BIT,
    @motivo     NVARCHAR(300),
    @idUsuario  INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @cantidad IS NULL OR @cantidad <= 0
        THROW 50001, 'La cantidad a ajustar debe ser mayor a cero.', 1;
    IF @motivo IS NULL OR LTRIM(RTRIM(@motivo)) = ''
        THROW 50002, 'El motivo del ajuste es obligatorio.', 1;

    BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @stockActual DECIMAL(12,2);
    SELECT @stockActual = stockActual
    FROM Insumo WITH (UPDLOCK, HOLDLOCK)
    WHERE idInsumo = @idInsumo;

    IF @stockActual IS NULL
        THROW 50003, 'El insumo no existe.', 1;

    DECLARE @entrada DECIMAL(12,2) = CASE WHEN @esEntrada = 1 THEN @cantidad ELSE 0 END;
    DECLARE @salida  DECIMAL(12,2) = CASE WHEN @esEntrada = 1 THEN 0 ELSE @cantidad END;
    DECLARE @stockResultante DECIMAL(12,2) = @stockActual + @entrada - @salida;

    IF @stockResultante < 0
        THROW 50004, 'Stock insuficiente para el ajuste.', 1;

    UPDATE Insumo SET stockActual = @stockResultante WHERE idInsumo = @idInsumo;

    INSERT INTO MovimientoStock
        (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
    VALUES
        (@idInsumo, 'AjusteManual', NULL, NULL, @idUsuario, @entrada, @salida, @stockResultante, LTRIM(RTRIM(@motivo)));

    COMMIT TRANSACTION;

    SELECT @idInsumo AS idInsumo, @stockActual AS stockAnterior, @stockResultante AS stockNuevo;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;   -- deshace todo y devuelve el error a quien llamó
        THROW;
    END CATCH;
END;
GO

/* ---------------------------------------------------------------------------
   sp_RegistrarPagoCliente
   Registra un pago de un cliente repartiéndolo solo: primero cancela sus ventas con
   saldo pendiente, de la más vieja a la más nueva; lo que sobra queda "a cuenta"
   (saldo a favor). Por cada comprobante tocado se guarda una fila de Pago, se descuenta
   el saldo de la venta y se agrega el movimiento (haber) en CuentaCorrienteCliente.
   Todo en una sola transacción.
   --------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE sp_RegistrarPagoCliente
    @idCliente     INT,
    @monto         DECIMAL(12,2),
    @medioPago     NVARCHAR(30),
    @idUsuario     INT,
    @observaciones NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @monto IS NULL OR @monto <= 0
        THROW 50011, 'El monto del pago debe ser mayor a cero.', 1;
    IF NOT EXISTS (SELECT 1 FROM Cliente WHERE idCliente = @idCliente)
        THROW 50012, 'El cliente no existe.', 1;

    BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @restante DECIMAL(12,2) = @monto;
    DECLARE @idVenta INT, @numero NVARCHAR(50), @saldoVenta DECIMAL(12,2), @aplicado DECIMAL(12,2);
    DECLARE @idPago INT, @saldoCC DECIMAL(12,2);
    DECLARE @pagos TABLE (idPago INT, idVenta INT, numeroComprobante NVARCHAR(50), monto DECIMAL(12,2));

    DECLARE ventas CURSOR LOCAL FAST_FORWARD FOR
        SELECT idVenta, numeroComprobante, saldoPendiente
        FROM ComprobanteVenta WITH (UPDLOCK, HOLDLOCK)
        WHERE idCliente = @idCliente AND saldoPendiente > 0
        ORDER BY fecha, idVenta;

    OPEN ventas;
    FETCH NEXT FROM ventas INTO @idVenta, @numero, @saldoVenta;
    WHILE @@FETCH_STATUS = 0 AND @restante > 0
    BEGIN
        SET @aplicado = CASE WHEN @restante < @saldoVenta THEN @restante ELSE @saldoVenta END;

        INSERT INTO Pago (tipo, idCliente, idProveedor, idVenta, idCompra, idUsuario, medioPago, monto, observaciones)
        VALUES ('C', @idCliente, NULL, @idVenta, NULL, @idUsuario, @medioPago, @aplicado, @observaciones);
        SET @idPago = CAST(SCOPE_IDENTITY() AS INT);

        UPDATE ComprobanteVenta SET saldoPendiente = saldoPendiente - @aplicado WHERE idVenta = @idVenta;

        SELECT @saldoCC = ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente
                                  WHERE idCliente = @idCliente ORDER BY idMovimiento DESC), 0);
        INSERT INTO CuentaCorrienteCliente (idCliente, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
        VALUES (@idCliente, 'Pago', NULL, @idPago, 0, @aplicado, @saldoCC - @aplicado, 'Pago de venta ' + @numero, NULL);

        INSERT INTO @pagos VALUES (@idPago, @idVenta, @numero, @aplicado);
        SET @restante = @restante - @aplicado;
        FETCH NEXT FROM ventas INTO @idVenta, @numero, @saldoVenta;
    END;
    CLOSE ventas;
    DEALLOCATE ventas;

    IF @restante > 0
    BEGIN
        INSERT INTO Pago (tipo, idCliente, idProveedor, idVenta, idCompra, idUsuario, medioPago, monto, observaciones)
        VALUES ('C', @idCliente, NULL, NULL, NULL, @idUsuario, @medioPago, @restante, @observaciones);
        SET @idPago = CAST(SCOPE_IDENTITY() AS INT);

        SELECT @saldoCC = ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente
                                  WHERE idCliente = @idCliente ORDER BY idMovimiento DESC), 0);
        INSERT INTO CuentaCorrienteCliente (idCliente, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
        VALUES (@idCliente, 'Pago', NULL, @idPago, 0, @restante, @saldoCC - @restante, 'Pago a cuenta', NULL);

        INSERT INTO @pagos VALUES (@idPago, NULL, NULL, @restante);
    END;

    COMMIT TRANSACTION;

    SELECT idPago, idVenta, ISNULL(numeroComprobante, 'A cuenta') AS aplicadoA, monto FROM @pagos ORDER BY idPago;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        IF CURSOR_STATUS('local', 'ventas') >= -1 DEALLOCATE ventas;
        THROW;
    END CATCH;
END;
GO

/* ---------------------------------------------------------------------------
   sp_StockBajo — insumos activos con stock por debajo del mínimo
   --------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE sp_StockBajo
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.idProducto, p.nombre, i.marca, i.unidadMedida, i.stockActual, i.stockMinimo,
           i.stockMinimo - i.stockActual AS faltante
    FROM Producto p
    JOIN Insumo i ON i.idInsumo = p.idProducto
    WHERE p.tipo = 'Insumo' AND p.activo = 1 AND i.stockActual < i.stockMinimo
    ORDER BY p.nombre;
END;
GO

/* ---------------------------------------------------------------------------
   sp_VentasPorPeriodo — ventas entre dos fechas (inclusive del día completo de @hasta)
   Devuelve dos resultados: el detalle de las ventas y los totales del período.
   --------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE sp_VentasPorPeriodo
    @desde DATE,
    @hasta DATE
AS
BEGIN
    SET NOCOUNT ON;
    IF @hasta < @desde
        THROW 50021, 'La fecha hasta no puede ser anterior a la fecha desde.', 1;

    DECLARE @hastaExclusiva DATETIME = DATEADD(DAY, 1, CAST(@hasta AS DATETIME));

    SELECT v.idVenta, v.numeroComprobante, v.fecha, c.denominacion AS cliente,
           v.subtotal, v.impuestos, v.total, v.saldoPendiente
    FROM ComprobanteVenta v
    JOIN Cliente c ON c.idCliente = v.idCliente
    WHERE v.fecha >= @desde AND v.fecha < @hastaExclusiva
    ORDER BY v.fecha;

    SELECT COUNT(*) AS cantidadVentas,
           ISNULL(SUM(v.total), 0) AS totalVendido,
           ISNULL(SUM(v.total - v.saldoPendiente), 0) AS totalCobrado,
           ISNULL(SUM(v.saldoPendiente), 0) AS totalPendiente
    FROM ComprobanteVenta v
    WHERE v.fecha >= @desde AND v.fecha < @hastaExclusiva;
END;
GO

/* ---------------------------------------------------------------------------
   sp_SaldosCuentaCorriente — clientes cuyo último movimiento deja saldo distinto de 0
   (saldo positivo = el cliente debe; negativo = tiene saldo a favor)
   --------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE sp_SaldosCuentaCorriente
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.idCliente, c.denominacion AS cliente, u.saldo, u.fecha AS ultimoMovimiento
    FROM Cliente c
    CROSS APPLY (
        SELECT TOP 1 saldo, fecha FROM CuentaCorrienteCliente
        WHERE idCliente = c.idCliente ORDER BY idMovimiento DESC
    ) u
    WHERE u.saldo <> 0
    ORDER BY u.saldo DESC;
END;
GO

/* ---------------------------------------------------------------------------
   sp_HistorialVehiculo — órdenes de trabajo de un vehículo y la venta de cada una
   --------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE sp_HistorialVehiculo
    @patente NVARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM Vehiculo WHERE patente = @patente)
        THROW 50031, 'No existe un vehículo con esa patente.', 1;

    SELECT ve.patente, ve.marca, ve.modelo, c.denominacion AS titular,
           o.idOrden, o.fecha, o.kilometraje, o.estado AS estadoOrden,
           v.numeroComprobante, v.total, v.saldoPendiente
    FROM Vehiculo ve
    JOIN Cliente c ON c.idCliente = ve.idCliente
    LEFT JOIN OrdenDeTrabajo o ON o.idVehiculo = ve.idVehiculo
    LEFT JOIN ComprobanteVenta v ON v.idOrden = o.idOrden
    WHERE ve.patente = @patente
    ORDER BY o.fecha DESC;
END;
GO

PRINT 'Procedimientos almacenados creados: sp_AjustarStock, sp_RegistrarPagoCliente, sp_StockBajo, sp_VentasPorPeriodo, sp_SaldosCuentaCorriente, sp_HistorialVehiculo.';
GO
