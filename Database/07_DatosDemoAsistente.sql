/* ============================================================================
   LubricentroControl 2026 — Datos de ejemplo para el asistente de consultas

   Suma a los datos de 04_DatosDemo.sql lo que hace falta para que las 12
   consultas del asistente (~/Reportes/Asistente) tengan algo que contestar:

   - 24 órdenes de trabajo cerradas en el mes actual y los dos anteriores, con
     sus ventas, repartidas entre los 10 vehículos de ejemplo (unos vuelven más
     que otros: marca y modelo más atendidos).
   - Facturas emitidas por los tres usuarios con escritura (ventas por cajero),
     pagos con distintos medios, dos ventas anuladas con nota de crédito, y
     deuda nueva en las cuentas corrientes.
   - Consumo de insumos en el kardex (más vendidos, stock y reposición).
   - Dos meses de inicios de sesión y pantallas abiertas de los cuatro usuarios
     de prueba (tiempo en sesión, páginas más visitadas, usuarios que más ingresan).

   Todo respeta las reglas de BIZ/Data, igual que 04: el stock baja en relativo
   y queda en el kardex (una línea de insumo sin stock no se carga), el saldo de
   la cuenta corriente se encadena con el movimiento anterior, la venta nueva
   usa el saldo a favor del cliente, un pago cancela primero la deuda más vieja,
   y la factura y la nota de crédito toman la letra y el número que les daría la
   aplicación. Las fechas salen de GETDATE(): el mes actual siempre tiene datos.

   Correr DESPUÉS de 01 a 04 (03 es obligatorio acá: usa a los usuarios de
   prueba). Se corre una sola vez: si ya se corrió, avisa y no hace nada.

     sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i "Database\07_DatosDemoAsistente.sql"
   ============================================================================ */

SET QUOTED_IDENTIFIER ON;
GO

USE LubricentroControl;
GO

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM Cliente WHERE numeroDocumento = '30111222')
   OR NOT EXISTS (SELECT 1 FROM Usuario WHERE email = 'empleado@lubricentro.com')
   OR OBJECT_ID('dbo.RegistroSesion', 'U') IS NULL
BEGIN
    RAISERROR(N'Faltan datos previos: correr antes 01 a 04 (con 03) y, en una base anterior al 2026-11-02, Actualizar_2026-11-02_RegistroDeUso.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

IF EXISTS (SELECT 1 FROM OrdenDeTrabajo WHERE observaciones = N'Service completo, cliente frecuente')
BEGIN
    PRINT 'Estos datos ya estaban cargados: no se cambió nada.';
    SET NOEXEC ON;
END
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @admin INT = (SELECT idUsuario FROM Usuario WHERE email = 'admin@lubricentro.com');
DECLARE @encargado INT = (SELECT idUsuario FROM Usuario WHERE email = 'encargado@lubricentro.com');
DECLARE @empleado INT = (SELECT idUsuario FROM Usuario WHERE email = 'empleado@lubricentro.com');

DECLARE @ahora DATETIME = GETDATE();
DECLARE @hoy DATETIME = CAST(CAST(@ahora AS DATE) AS DATETIME);
DECLARE @inicioMes DATETIME = DATEADD(DAY, 1 - DAY(@hoy), @hoy);

/* --- 1. Las órdenes ---------------------------------------------------------
   mesesAtras 0 = mes actual (el día se recorta a hoy si todavía no llegó),
   1 = mes anterior, 2 = el anterior a ese. cajero NULL = venta sin facturar;
   medioPago NULL = no se cobra al cerrar (queda en la cuenta corriente). */
DECLARE @Orden TABLE (
    n INT PRIMARY KEY, mesesAtras INT, dia INT, minuto INT,
    documento NVARCHAR(20), patente NVARCHAR(10), kilometraje INT, observaciones NVARCHAR(300),
    cajero NVARCHAR(150) NULL, medioPago NVARCHAR(30) NULL, anular BIT,
    fecha DATETIME NULL, secuencia INT NULL);

INSERT INTO @Orden (n, mesesAtras, dia, minuto, documento, patente, kilometraje, observaciones, cajero, medioPago, anular) VALUES
    -- Hace dos meses
    ( 1, 2,  3, 540, '30111222',    'ABC123',  52000, N'Service completo, cliente frecuente', 'encargado@lubricentro.com', NULL,            0),
    ( 2, 2,  6, 600, '30714598216', 'PQR987', 140000, N'Service de flota',                    'admin@lubricentro.com',     'Transferencia', 0),
    ( 3, 2, 10, 660, '28222333',    'AD456EF', 47000, N'Alineación y balanceo',               'empleado@lubricentro.com',  'Efectivo',      0),
    ( 4, 2, 14, 570, '27555666',    'JKL321',  98000, N'Cambio de correa',                    'encargado@lubricentro.com', 'Tarjeta',       0),
    ( 5, 2, 18, 630, '30111222',    'ABC123',  54500, N'Rotación de neumáticos',              NULL,                        NULL,            0),
    ( 6, 2, 22, 720, '27338889998', 'AG567DE', 61000, N'Frenos',                              'empleado@lubricentro.com',  'Efectivo',      0),
    ( 7, 2, 26, 540, '30714598216', 'PQR987', 143000, N'Batería',                             'admin@lubricentro.com',     'Transferencia', 0),
    -- Mes anterior
    ( 8, 1,  2, 560, '32444555',    'AE789GH', 27000, N'Service de aceite',                   'empleado@lubricentro.com',  'Tarjeta',       0),
    ( 9, 1,  5, 600, '30111222',    'ABC123',  57000, N'Service completo',                    'encargado@lubricentro.com', NULL,            0),
    (10, 1,  8, 690, '20253334445', 'XYZ789',  91000, N'Amortiguadores delanteros',           'admin@lubricentro.com',     NULL,            0),
    (11, 1, 12, 540, '31666777',    'AF234BC', 33000, N'Service de aceite',                   'empleado@lubricentro.com',  NULL,            1),
    (12, 1, 15, 620, '30714598216', 'PQR987', 146500, N'Service de flota',                    'encargado@lubricentro.com', 'Transferencia', 0),
    (13, 1, 19, 580, '34000111',    'AH890FG', 12000, N'Primer service',                      'empleado@lubricentro.com',  NULL,            0),
    (14, 1, 22, 650, '29777888',    'MNO654',  76000, N'Filtros y bujías',                    'encargado@lubricentro.com', 'Efectivo',      0),
    (15, 1, 25, 700, '30111222',    'ABC123',  59000, N'Lámparas y escobillas',               'empleado@lubricentro.com',  NULL,            0),
    (16, 1, 27, 545, '27338889998', 'AG567DE', 64000, N'Service de aceite',                   'admin@lubricentro.com',     'Tarjeta',       0),
    -- Mes actual
    (17, 0,  1, 545, '30714598216', 'PQR987', 149000, N'Service de flota',                    'encargado@lubricentro.com', 'Transferencia', 0),
    (18, 0,  1, 600, '30111222',    'ABC123',  61000, N'Service completo',                    'empleado@lubricentro.com',  NULL,            0),
    (19, 0,  2, 570, '28222333',    'AD456EF', 50000, N'Service de aceite',                   'empleado@lubricentro.com',  'Efectivo',      0),
    (20, 0,  2, 630, '29777888',    'MNO654',  78000, N'Frenos y refrigerante',               'encargado@lubricentro.com', NULL,            1),
    (21, 0,  3, 660, '32444555',    'AE789GH', 29000, N'Filtro de habitáculo',                'admin@lubricentro.com',     'Tarjeta',       0),
    (22, 0,  5, 690, '34000111',    'AH890FG', 14000, N'Alineación y balanceo',               NULL,                        NULL,            0),
    (23, 0,  8, 720, '20253334445', 'XYZ789',  93000, N'Service de aceite',                   'encargado@lubricentro.com', NULL,            0),
    (24, 0, 12, 540, '27555666',    'JKL321', 101000, N'Service completo',                    'empleado@lubricentro.com',  'Efectivo',      0);

DECLARE @Linea TABLE (n INT, producto NVARCHAR(150), cantidad DECIMAL(12,2));
INSERT INTO @Linea (n, producto, cantidad) VALUES
    ( 1, N'Cambio de aceite y filtro', 1), ( 1, N'Aceite 15W40', 4), ( 1, N'Filtro de aceite', 1), ( 1, N'Cambio de filtro de aire', 1), ( 1, N'Filtro de aire', 1),
    ( 2, N'Cambio de aceite y filtro', 1), ( 2, N'Aceite 15W40', 6), ( 2, N'Filtro de aceite', 1), ( 2, N'Cambio de filtro de combustible', 1), ( 2, N'Filtro de combustible', 1),
    ( 3, N'Alineación', 1), ( 3, N'Balanceo', 1),
    ( 4, N'Cambio de correa de distribución', 1), ( 4, N'Correa de distribución', 1), ( 4, N'Refrigerante', 2),
    ( 5, N'Rotación de neumáticos', 1), ( 5, N'Balanceo', 1),
    ( 6, N'Cambio de líquido de frenos', 1), ( 6, N'Líquido de frenos DOT4', 1),
    ( 7, N'Revisión y carga de batería', 1), ( 7, N'Batería 12V 65Ah', 1),
    ( 8, N'Cambio de aceite y filtro', 1), ( 8, N'Aceite 5W30 sintético', 4), ( 8, N'Filtro de aceite', 1),
    ( 9, N'Cambio de aceite y filtro', 1), ( 9, N'Aceite 15W40', 4), ( 9, N'Filtro de aceite', 1), ( 9, N'Bujías (caja x4)', 1),
    (10, N'Cambio de amortiguadores', 1), (10, N'Alineación', 1),
    (11, N'Cambio de aceite y filtro', 1), (11, N'Aceite 5W30 sintético', 4), (11, N'Filtro de aceite', 1),
    (12, N'Cambio de aceite y filtro', 1), (12, N'Aceite 15W40', 6), (12, N'Filtro de aceite', 1), (12, N'Grasa multipropósito', 1),
    (13, N'Cambio de aceite y filtro', 1), (13, N'Aceite 5W30 sintético', 4), (13, N'Filtro de aceite', 1),
    (14, N'Cambio de filtro de aire', 1), (14, N'Filtro de aire', 1), (14, N'Cambio de filtro de combustible', 1), (14, N'Filtro de combustible', 1), (14, N'Bujías (caja x4)', 1),
    (15, N'Revisión y carga de batería', 1), (15, N'Lámpara H4 12V', 2), (15, N'Escobillas limpiaparabrisas (par)', 1),
    (16, N'Cambio de aceite y filtro', 1), (16, N'Aceite 5W30 sintético', 4), (16, N'Filtro de aceite', 1), (16, N'Aditivo limpia inyectores', 1),
    (17, N'Cambio de aceite y filtro', 1), (17, N'Aceite 15W40', 6), (17, N'Filtro de aceite', 1), (17, N'Rotación de neumáticos', 1),
    (18, N'Cambio de aceite y filtro', 1), (18, N'Aceite 15W40', 4), (18, N'Filtro de aceite', 1), (18, N'Filtro de habitáculo', 1),
    (19, N'Cambio de aceite y filtro', 1), (19, N'Aceite 15W40', 4), (19, N'Filtro de aceite', 1),
    (20, N'Cambio de líquido de frenos', 1), (20, N'Líquido de frenos DOT4', 1), (20, N'Refrigerante', 2),
    (21, N'Cambio de filtro de aire', 1), (21, N'Filtro de habitáculo', 1), (21, N'Lámpara H4 12V', 1),
    (22, N'Alineación', 1), (22, N'Balanceo', 1), (22, N'Aditivo limpia inyectores', 1),
    (23, N'Cambio de aceite y filtro', 1), (23, N'Aceite 15W40', 5), (23, N'Filtro de aceite', 1), (23, N'Grasa multipropósito', 1),
    (24, N'Cambio de aceite y filtro', 1), (24, N'Aceite 5W30 sintético', 4), (24, N'Filtro de aceite', 1), (24, N'Bujías (caja x4)', 1);

/* Fecha de cada orden: el día del mes que le toca (recortado a hoy en el mes
   actual), corrido al lunes si cae domingo, y nunca en el futuro. El 7 de enero
   de 1900 fue domingo. */
UPDATE @Orden SET fecha = DATEADD(MINUTE, minuto, DATEADD(DAY,
    CASE WHEN mesesAtras = 0 AND dia > DAY(@hoy) THEN DAY(@hoy) ELSE dia END - 1,
    DATEADD(MONTH, -mesesAtras, @inicioMes)));
UPDATE @Orden SET fecha = DATEADD(DAY, 1, fecha) WHERE DATEDIFF(DAY, '19000107', fecha) % 7 = 0;
UPDATE @Orden SET fecha = DATEADD(MINUTE, -(25 - n) * 20 - 120, @ahora) WHERE fecha > DATEADD(HOUR, -3, @ahora);

UPDATE o SET secuencia = r.rn
FROM @Orden o
JOIN (SELECT n, ROW_NUMBER() OVER (ORDER BY fecha, n) AS rn FROM @Orden) r ON r.n = o.n;

/* --- 2. Cada orden, en orden de fecha: detalle, stock, venta, factura, pago
   y nota de crédito ----------------------------------------------------------- */
DECLARE @Venta TABLE (tipoItem CHAR(1), idServicio INT NULL, idInsumo INT NULL, descripcion NVARCHAR(200),
                      cantidad DECIMAL(12,2), precio DECIMAL(12,2), tipoIva NVARCHAR(20), alicuotaIva DECIMAL(5,2));

DECLARE @i INT = 1, @total INT = (SELECT COUNT(*) FROM @Orden);
DECLARE @n INT, @fecha DATETIME, @documento NVARCHAR(20), @patente NVARCHAR(10), @kilometraje INT,
        @observaciones NVARCHAR(300), @cajero NVARCHAR(150), @medioPago NVARCHAR(30), @anular BIT;
DECLARE @idCliente INT, @idVehiculo INT, @idOrden INT, @idVenta INT, @idFactura INT, @idCajero INT,
        @idInsumo INT, @cantidad DECIMAL(12,2), @precio DECIMAL(12,2), @lineaInsumo INT,
        @importe DECIMAL(12,2), @iva DECIMAL(12,2), @saldoPrevio DECIMAL(12,2), @credito DECIMAL(12,2),
        @fechaVenta DATETIME, @fechaPago DATETIME, @fechaNota DATETIME, @tipo CHAR(1), @puntoVenta INT,
        @numero INT, @condicionVenta NVARCHAR(30), @domicilio NVARCHAR(400),
        @direccion NVARCHAR(200), @localidad NVARCHAR(100), @provincia NVARCHAR(100), @codigoPostal NVARCHAR(20),
        @restante DECIMAL(12,2), @idVentaPagar INT, @saldoVenta DECIMAL(12,2), @monto DECIMAL(12,2),
        @idPago INT, @numeroVenta NVARCHAR(50);

DECLARE @Insumos TABLE (linea INT IDENTITY(1,1), idInsumo INT, cantidad DECIMAL(12,2), precio DECIMAL(12,2));

SELECT @puntoVenta = puntoVenta FROM Emisor WHERE idEmisor = 1;

WHILE @i <= @total
BEGIN
    SELECT @n = n, @fecha = fecha, @documento = documento, @patente = patente, @kilometraje = kilometraje,
           @observaciones = observaciones, @cajero = cajero, @medioPago = medioPago, @anular = anular
    FROM @Orden WHERE secuencia = @i;

    SELECT @idCliente = idCliente, @condicionVenta = CASE WHEN cuentaCorriente = 1 THEN N'Cuenta corriente' ELSE N'Contado' END,
           @direccion = NULLIF(LTRIM(RTRIM(direccion)), ''), @localidad = NULLIF(LTRIM(RTRIM(localidad)), ''),
           @provincia = NULLIF(LTRIM(RTRIM(provincia)), ''), @codigoPostal = NULLIF(LTRIM(RTRIM(codigoPostal)), '')
    FROM Cliente WHERE numeroDocumento = @documento;
    SELECT @idVehiculo = idVehiculo FROM Vehiculo WHERE patente = @patente;

    INSERT INTO OrdenDeTrabajo (idTurno, idCliente, idVehiculo, idUsuario, fecha, kilometraje, observaciones, estado)
    VALUES (NULL, @idCliente, @idVehiculo, @empleado, @fecha, @kilometraje, @observaciones, 'Abierta');
    SET @idOrden = SCOPE_IDENTITY();

    INSERT INTO DetalleOrdenServicio (idOrden, idServicio, cantidad, precioAplicado)
    SELECT @idOrden, p.idProducto, l.cantidad, p.precio
    FROM @Linea l JOIN Producto p ON p.nombre = l.producto AND p.tipo = 'Servicio'
    WHERE l.n = @n;

    -- Insumos: el stock baja en relativo y solo si alcanza (DetalleOrdenInsumoDAL.Agregar)
    DELETE FROM @Insumos;
    INSERT INTO @Insumos (idInsumo, cantidad, precio)
    SELECT p.idProducto, l.cantidad, p.precio
    FROM @Linea l JOIN Producto p ON p.nombre = l.producto AND p.tipo = 'Insumo'
    WHERE l.n = @n;

    SET @lineaInsumo = (SELECT MIN(linea) FROM @Insumos);
    WHILE @lineaInsumo IS NOT NULL
    BEGIN
        SELECT @idInsumo = idInsumo, @cantidad = cantidad, @precio = precio FROM @Insumos WHERE linea = @lineaInsumo;

        UPDATE Insumo SET stockActual = stockActual - @cantidad
        WHERE idInsumo = @idInsumo AND stockActual - @cantidad >= 0;

        IF @@ROWCOUNT = 1
        BEGIN
            INSERT INTO DetalleOrdenInsumo (idOrden, idInsumo, cantidad, precioUnitario)
            VALUES (@idOrden, @idInsumo, @cantidad, @precio);

            INSERT INTO MovimientoStock (idInsumo, fecha, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
            VALUES (@idInsumo, @fecha, 'Orden', NULL, @idOrden, @empleado, 0, @cantidad,
                    (SELECT stockActual FROM Insumo WHERE idInsumo = @idInsumo),
                    'Orden de trabajo #' + CAST(@idOrden AS VARCHAR(10)));
        END

        SET @lineaInsumo = (SELECT MIN(linea) FROM @Insumos WHERE linea > @lineaInsumo);
    END

    -- Venta (ComprobanteVentaDAL.GenerarDesdeOrden): líneas con el IVA contenido,
    -- saldo a favor aplicado y movimiento en la cuenta corriente
    SET @fechaVenta = DATEADD(MINUTE, 90, @fecha);
    IF @fechaVenta > DATEADD(MINUTE, -30, @ahora) SET @fechaVenta = DATEADD(MINUTE, -30, @ahora);

    DELETE FROM @Venta;
    INSERT INTO @Venta (tipoItem, idServicio, idInsumo, descripcion, cantidad, precio, tipoIva, alicuotaIva)
    SELECT 'S', d.idServicio, NULL, p.nombre, d.cantidad, d.precioAplicado, p.tipoIva, p.alicuotaIva
    FROM DetalleOrdenServicio d JOIN Producto p ON p.idProducto = d.idServicio WHERE d.idOrden = @idOrden
    UNION ALL
    SELECT 'I', NULL, d.idInsumo, p.nombre, d.cantidad, d.precioUnitario, p.tipoIva, p.alicuotaIva
    FROM DetalleOrdenInsumo d JOIN Producto p ON p.idProducto = d.idInsumo WHERE d.idOrden = @idOrden;

    SELECT @importe = SUM(cantidad * precio),
           @iva = SUM(CASE WHEN tipoIva = 'Gravado'
                           THEN cantidad * precio - ROUND(cantidad * precio / (1 + alicuotaIva / 100), 2) ELSE 0 END)
    FROM @Venta;

    SET @saldoPrevio = ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente
                               WHERE idCliente = @idCliente ORDER BY idMovimiento DESC), 0);
    SET @credito = CASE WHEN @saldoPrevio >= 0 THEN 0 WHEN -@saldoPrevio < @importe THEN -@saldoPrevio ELSE @importe END;

    UPDATE OrdenDeTrabajo SET estado = 'Cerrada' WHERE idOrden = @idOrden;

    INSERT INTO ComprobanteVenta (idOrden, idCliente, numeroComprobante, fecha, subtotal, impuestos, total, saldoPendiente)
    VALUES (@idOrden, @idCliente, '(pendiente)', @fechaVenta, @importe - @iva, @iva, @importe, @importe - @credito);
    SET @idVenta = SCOPE_IDENTITY();

    INSERT INTO DetalleComprobanteVenta (idVenta, tipoItem, idServicio, idInsumo, descripcion, cantidad, precioUnitario, subtotal,
                                         tipoIva, alicuotaIva, importeIva)
    SELECT @idVenta, tipoItem, idServicio, idInsumo, descripcion, cantidad, precio, cantidad * precio, tipoIva, alicuotaIva,
           CASE WHEN tipoIva = 'Gravado'
                THEN cantidad * precio - ROUND(cantidad * precio / (1 + alicuotaIva / 100), 2) ELSE 0 END
    FROM @Venta;

    INSERT INTO CuentaCorrienteCliente (idCliente, fecha, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
    VALUES (@idCliente, @fechaVenta, 'Venta', @idVenta, NULL, @importe, 0, @saldoPrevio + @importe,
            'Venta #' + CAST(@idVenta AS VARCHAR(10)), NULL);

    SET @numeroVenta = 'V-' + RIGHT('000000' + CAST(@idVenta AS VARCHAR(10)), 6);
    UPDATE ComprobanteVenta SET numeroComprobante = @numeroVenta WHERE idVenta = @idVenta;

    -- Letra (Factura.DeterminarTipo) y domicilio del cliente (Domicilio.Armar), para factura y nota
    SELECT @tipo = CASE WHEN e.condicionIva <> N'Responsable Inscripto' THEN 'C'
                        WHEN c.condicionIva IN (N'Responsable Inscripto', N'Monotributista') THEN 'A'
                        ELSE 'B' END
    FROM Emisor e CROSS JOIN Cliente c WHERE e.idEmisor = 1 AND c.idCliente = @idCliente;

    IF @localidad = @provincia SET @provincia = NULL;
    SET @domicilio = ISNULL(@direccion, '');
    IF @localidad IS NOT NULL SET @domicilio = @domicilio + CASE WHEN @domicilio = '' THEN '' ELSE ', ' END + @localidad;
    IF @provincia IS NOT NULL SET @domicilio = @domicilio + CASE WHEN @domicilio = '' THEN '' ELSE ', ' END + @provincia;
    IF @codigoPostal IS NOT NULL SET @domicilio = @domicilio + CASE WHEN @domicilio = '' THEN '(CP ' ELSE ' (CP ' END + @codigoPostal + ')';
    IF @domicilio = '' SET @domicilio = NULL;

    -- Factura (FacturaDAL.Emitir): la emite el cajero de la fila
    SET @idFactura = NULL;
    SET @idCajero = (SELECT idUsuario FROM Usuario WHERE email = @cajero);
    IF @idCajero IS NOT NULL
    BEGIN
        SET @numero = ISNULL((SELECT MAX(numero) FROM Factura WHERE tipo = @tipo AND puntoVenta = @puntoVenta), 0) + 1;

        INSERT INTO Factura (idVenta, tipo, puntoVenta, numero, fecha, idUsuario, condicionVenta,
                             emisorRazonSocial, emisorCuit, emisorCondicionIva, emisorDomicilio, emisorIngresosBrutos,
                             emisorInicioActividades, receptorNombre, receptorTipoDocumento, receptorNumeroDocumento,
                             receptorCondicionIva, receptorDomicilio)
        SELECT @idVenta, @tipo, @puntoVenta, @numero, DATEADD(MINUTE, 5, @fechaVenta), @idCajero, @condicionVenta,
               e.razonSocial, e.cuit, e.condicionIva, e.domicilio, e.ingresosBrutos,
               CONVERT(VARCHAR(10), e.inicioActividades, 103), c.denominacion, c.tipoDocumento, c.numeroDocumento,
               c.condicionIva, @domicilio
        FROM Emisor e CROSS JOIN Cliente c WHERE e.idEmisor = 1 AND c.idCliente = @idCliente;
        SET @idFactura = SCOPE_IDENTITY();
    END

    -- Pago (PagoDAL.Registrar): el cliente paga lo que quedó de esta venta, y el monto
    -- cancela primero la deuda más vieja; una fila de Pago por venta que toca
    IF @medioPago IS NOT NULL
    BEGIN
        SET @restante = (SELECT saldoPendiente FROM ComprobanteVenta WHERE idVenta = @idVenta);
        SET @fechaPago = DATEADD(MINUTE, 10, @fechaVenta);

        WHILE @restante > 0
        BEGIN
            SET @idVentaPagar = NULL;
            SELECT TOP 1 @idVentaPagar = idVenta, @saldoVenta = saldoPendiente, @numeroVenta = numeroComprobante
            FROM ComprobanteVenta
            WHERE idCliente = @idCliente AND saldoPendiente > 0
            ORDER BY fecha, idVenta;

            IF @idVentaPagar IS NULL BREAK;

            SET @monto = CASE WHEN @saldoVenta < @restante THEN @saldoVenta ELSE @restante END;

            INSERT INTO Pago (tipo, idCliente, idProveedor, idVenta, idCompra, idUsuario, fecha, medioPago, monto, observaciones)
            VALUES ('C', @idCliente, NULL, @idVentaPagar, NULL, ISNULL(@idCajero, @encargado), @fechaPago, @medioPago, @monto, NULL);
            SET @idPago = SCOPE_IDENTITY();

            UPDATE ComprobanteVenta SET saldoPendiente = saldoPendiente - @monto WHERE idVenta = @idVentaPagar;

            SET @saldoPrevio = ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente
                                       WHERE idCliente = @idCliente ORDER BY idMovimiento DESC), 0);
            INSERT INTO CuentaCorrienteCliente (idCliente, fecha, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
            VALUES (@idCliente, @fechaPago, 'Pago', NULL, @idPago, 0, @monto, @saldoPrevio - @monto,
                    N'Pago de venta ' + @numeroVenta, NULL);

            SET @restante = @restante - @monto;
        END
    END

    -- Nota de crédito (NotaCreditoDAL.Emitir): anula la venta entera, la acredita en la
    -- cuenta corriente y la deja sin saldo. Las filas que se anulan no tienen pago.
    IF @anular = 1
    BEGIN
        SET @fechaNota = DATEADD(HOUR, 3, @fechaVenta);
        IF @fechaNota > @ahora SET @fechaNota = @ahora;
        SET @numeroVenta = 'V-' + RIGHT('000000' + CAST(@idVenta AS VARCHAR(10)), 6);
        SET @numero = ISNULL((SELECT MAX(numero) FROM NotaCredito WHERE tipo = @tipo AND puntoVenta = @puntoVenta), 0) + 1;

        INSERT INTO NotaCredito (idVenta, idFactura, tipo, puntoVenta, numero, fecha, idUsuario, motivo, condicionVenta,
                                 subtotal, impuestos, total,
                                 emisorRazonSocial, emisorCuit, emisorCondicionIva, emisorDomicilio, emisorIngresosBrutos,
                                 emisorInicioActividades, receptorNombre, receptorTipoDocumento, receptorNumeroDocumento,
                                 receptorCondicionIva, receptorDomicilio)
        SELECT @idVenta, @idFactura, @tipo, @puntoVenta, @numero, @fechaNota, @encargado,
               N'Se facturó un service que el cliente no aceptó', @condicionVenta,
               v.subtotal, v.impuestos, v.total,
               e.razonSocial, e.cuit, e.condicionIva, e.domicilio, e.ingresosBrutos,
               CONVERT(VARCHAR(10), e.inicioActividades, 103), c.denominacion, c.tipoDocumento, c.numeroDocumento,
               c.condicionIva, @domicilio
        FROM ComprobanteVenta v
        CROSS JOIN Emisor e
        JOIN Cliente c ON c.idCliente = v.idCliente
        WHERE v.idVenta = @idVenta AND e.idEmisor = 1;

        UPDATE ComprobanteVenta SET saldoPendiente = 0 WHERE idVenta = @idVenta;

        SET @saldoPrevio = ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente
                                   WHERE idCliente = @idCliente ORDER BY idMovimiento DESC), 0);
        INSERT INTO CuentaCorrienteCliente (idCliente, fecha, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
        VALUES (@idCliente, @fechaNota, 'NotaCredito', @idVenta, NULL, 0, @importe, @saldoPrevio - @importe,
                N'Nota de crédito ' + @tipo + ' ' + RIGHT('00000' + CAST(@puntoVenta AS VARCHAR(5)), 5) + '-' +
                    RIGHT('00000000' + CAST(@numero AS VARCHAR(8)), 8) + ' (anula ' + @numeroVenta + ')',
                @encargado);
    END

    SET @i = @i + 1;
END

/* --- 3. Uso del sistema: sesiones y pantallas abiertas -------------------------
   Dos meses hacia atrás (sin domingos), con un turno mañana y uno tarde. Cada
   usuario tiene su frecuencia, su duración y sus pantallas: el empleado vive en
   el taller, el encargado en compras y cobros, el admin en administración y
   reportes, y el de solo lectura entra poco. Las elecciones "al azar" salen de
   CHECKSUM, así que dos corridas en el mismo día dan lo mismo. */
DECLARE @lectura INT = (SELECT idUsuario FROM Usuario WHERE email = 'lectura@lubricentro.com');

DECLARE @Perfil TABLE (idUsuario INT, turno INT, probabilidad INT, minutoInicio INT, duracionMin INT, duracionMax INT);
INSERT INTO @Perfil (idUsuario, turno, probabilidad, minutoInicio, duracionMin, duracionMax) VALUES
    (@empleado,  1, 92, 495, 150, 240),
    (@empleado,  2, 80, 900, 120, 200),
    (@encargado, 1, 85, 525,  90, 200),
    (@encargado, 2, 45, 960,  40, 120),
    (@admin,     1, 55, 570,  30, 110),
    (@admin,     2, 20, 1020, 15,  60),
    (@lectura,   1, 22, 600,  10,  35);

DECLARE @Pantalla TABLE (idUsuario INT, path NVARCHAR(200), peso INT, desde INT NULL, hasta INT NULL);
INSERT INTO @Pantalla (idUsuario, path, peso) VALUES
    (@empleado, '~/OrdenesDeTrabajo', 8), (@empleado, '~/Turnos', 6), (@empleado, '~/Clientes', 4),
    (@empleado, '~/Vehiculos', 3), (@empleado, '~/Productos', 2), (@empleado, '~/Pagos', 2), (@empleado, '~/Ventas', 1),
    (@encargado, '~/Ventas', 4), (@encargado, '~/Pagos', 3), (@encargado, '~/Compras', 3), (@encargado, '~/Productos', 3),
    (@encargado, '~/OrdenesDeTrabajo', 2), (@encargado, '~/CuentaCorrienteClientes', 2),
    (@encargado, '~/CuentaCorrienteProveedores', 1), (@encargado, '~/Proveedores', 1), (@encargado, '~/Reportes/StockBajo', 2),
    (@admin, '~/Reportes/Asistente', 3), (@admin, '~/Reportes/VentasPorPeriodo', 2), (@admin, '~/Usuarios', 2),
    (@admin, '~/Roles', 1), (@admin, '~/Ventas', 2), (@admin, '~/Reportes/CuentasCorrientes', 1), (@admin, '~/DatosComercio', 1),
    (@lectura, '~/Clientes', 2), (@lectura, '~/Turnos', 2), (@lectura, '~/Productos', 2), (@lectura, '~/Ventas', 1);

UPDATE p SET desde = (SELECT ISNULL(SUM(q.peso), 0) FROM @Pantalla q
                      WHERE q.idUsuario = p.idUsuario AND q.path < p.path),
             hasta = (SELECT SUM(q.peso) FROM @Pantalla q
                      WHERE q.idUsuario = p.idUsuario AND q.path <= p.path)
FROM @Pantalla p;

DECLARE @Sesion TABLE (idRegistroSesion INT, idUsuario INT, inicio DATETIME, ultimaActividad DATETIME);

;WITH dias AS (
    SELECT TOP (62) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS atras
    FROM sys.all_objects
),
candidatas AS (
    SELECT p.idUsuario, p.turno, d.atras,
           DATEADD(MINUTE, p.minutoInicio + ABS(CHECKSUM(p.idUsuario, p.turno, d.atras, 7)) % 50, DATEADD(DAY, -d.atras, @hoy)) AS inicio,
           p.duracionMin + ABS(CHECKSUM(p.idUsuario, p.turno, d.atras, 11)) % (p.duracionMax - p.duracionMin + 1) AS duracion,
           ABS(CHECKSUM(p.idUsuario, p.turno, d.atras, 13)) % 100 AS sorteo,
           p.probabilidad
    FROM @Perfil p CROSS JOIN dias d
    WHERE p.idUsuario IS NOT NULL
      AND DATEDIFF(DAY, '19000107', DATEADD(DAY, -d.atras, @hoy)) % 7 <> 0
)
INSERT INTO RegistroSesion (idUsuario, inicio, ultimaActividad, fin)
OUTPUT inserted.idRegistroSesion, inserted.idUsuario, inserted.inicio, inserted.ultimaActividad INTO @Sesion
SELECT idUsuario, inicio, DATEADD(MINUTE, duracion, inicio),
       -- 1 de cada 6 sesiones vence sola, sin "Cerrar sesión"
       CASE WHEN sorteo % 6 = 0 THEN NULL ELSE DATEADD(MINUTE, duracion, inicio) END
FROM candidatas
WHERE sorteo < probabilidad AND DATEADD(MINUTE, duracion, inicio) < @ahora;

-- La primera pantalla de cada sesión es el Inicio; después, entre 2 y 11 más según el perfil
;WITH pasos AS (
    SELECT TOP (12) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS paso FROM sys.all_objects
),
visitas AS (
    SELECT s.idRegistroSesion, s.idUsuario, s.inicio, s.ultimaActividad, x.paso,
           2 + ABS(CHECKSUM(s.idRegistroSesion, 3)) % 10 AS cantidad,
           ABS(CHECKSUM(s.idRegistroSesion, x.paso, 5)) AS sorteo
    FROM @Sesion s CROSS JOIN pasos x
)
INSERT INTO VisitaPagina (idRegistroSesion, idUsuario, path, fecha)
SELECT v.idRegistroSesion, v.idUsuario,
       CASE WHEN v.paso = 1 THEN '~/Default' ELSE p.path END,
       DATEADD(SECOND, DATEDIFF(SECOND, v.inicio, v.ultimaActividad) * (v.paso - 1) / (v.cantidad + 1), v.inicio)
FROM visitas v
JOIN @Pantalla p ON p.idUsuario = v.idUsuario
    AND v.sorteo % (SELECT MAX(q.hasta) FROM @Pantalla q WHERE q.idUsuario = v.idUsuario) >= p.desde
    AND v.sorteo % (SELECT MAX(q.hasta) FROM @Pantalla q WHERE q.idUsuario = v.idUsuario) < p.hasta
WHERE v.paso <= v.cantidad + 1;

COMMIT TRANSACTION;

DECLARE @sesiones INT = (SELECT COUNT(*) FROM @Sesion);
PRINT 'Listo: ' + CAST(@total AS VARCHAR(10)) + ' órdenes con sus ventas y '
    + CAST(@sesiones AS VARCHAR(10)) + ' sesiones de uso cargadas.';
GO

SET NOEXEC OFF;
GO
