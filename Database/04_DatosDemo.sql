/* ============================================================================
   LubricentroControl 2026 — Datos de ejemplo (demo)

   Carga 10 filas de ejemplo en cada entidad de negocio (Clientes, Vehículos,
   Proveedores, Servicios, Insumos, Turnos, Órdenes de trabajo) más el circuito
   de dinero y stock derivado (Compras, Ventas generadas al cerrar una orden,
   Pagos, Cuentas corrientes de Cliente/Proveedor y kardex de MovimientoStock),
   armado a mano respetando las mismas reglas que aplican los DAL reales de
   BIZ/Data: no son filas sueltas, es un circuito consistente de punta a
   punta para poder probar cualquier pantalla ya implementada y los reportes
   de Fase 5 en cuanto existan.

   Correr DESPUÉS de 01_Esquema.sql y 02_DatosIniciales.sql. 03_UsuariosDePrueba.sql
   es opcional: este script funciona solo con el admin sembrado por 02, y usa
   encargado/empleado si 03 se corrió (fallback automático al admin si no).

     sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i "Database\04_DatosDemo.sql"

   No es idempotente: asume que las tablas de negocio están vacías. Para
   recargar, volver a correr 01_Esquema.sql primero (ya borra y recrea todo).
   ============================================================================ */

USE LubricentroControl;
GO

SET NOCOUNT ON;

/* --- Usuario para las FK idUsuario (con fallback si 03_ no se corrió) ---- */
DECLARE @admin INT = (SELECT idUsuario FROM Usuario WHERE email = 'admin@lubricentro.com');
DECLARE @encargado INT = ISNULL((SELECT idUsuario FROM Usuario WHERE email = 'encargado@lubricentro.com'), @admin);
DECLARE @empleado INT = ISNULL((SELECT idUsuario FROM Usuario WHERE email = 'empleado@lubricentro.com'), @admin);

/* --- 1. Clientes (10) ----------------------------------------------------- */
INSERT INTO Cliente (nombre, apellido, dni, telefono, email, direccion) VALUES
    ('Juan', 'Pérez', '30111222', '11-4321-5678', 'juan.perez@gmail.com', 'Av. Rivadavia 1234, CABA'),
    ('María', 'Gómez', '28222333', '11-4555-1122', 'maria.gomez@gmail.com', 'Calle San Martín 456, Vicente López'),
    ('Carlos', 'Rodríguez', '25333444', '11-4666-2233', 'carlos.rodriguez@gmail.com', 'Av. Cabildo 789, CABA'),
    ('Ana', 'López', '32444555', '11-4777-3344', 'ana.lopez@hotmail.com', 'Belgrano 234, San Isidro'),
    ('Luis', 'Fernández', '27555666', '11-4888-4455', 'luis.fernandez@gmail.com', 'Av. Mitre 1560, Avellaneda'),
    ('Laura', 'Martínez', '31666777', '11-4999-5566', 'laura.martinez@gmail.com', 'Sarmiento 890, Morón'),
    ('Diego', 'Sánchez', '29777888', '11-4111-6677', 'diego.sanchez@gmail.com', 'Av. Corrientes 3200, CABA'),
    ('Sofía', 'Romero', '33888999', '11-4222-7788', 'sofia.romero@hotmail.com', 'Moreno 550, Quilmes'),
    ('Martín', 'Díaz', '26999000', '11-4333-8899', 'martin.diaz@gmail.com', 'Av. Pueyrredón 1122, CABA'),
    ('Valentina', 'Torres', '34000111', '11-4444-9900', 'valentina.torres@gmail.com', 'Independencia 678, La Plata');

DECLARE @cliJuan INT = (SELECT idCliente FROM Cliente WHERE dni = '30111222');
DECLARE @cliMaria INT = (SELECT idCliente FROM Cliente WHERE dni = '28222333');
DECLARE @cliCarlos INT = (SELECT idCliente FROM Cliente WHERE dni = '25333444');
DECLARE @cliAna INT = (SELECT idCliente FROM Cliente WHERE dni = '32444555');
DECLARE @cliLuis INT = (SELECT idCliente FROM Cliente WHERE dni = '27555666');
DECLARE @cliLaura INT = (SELECT idCliente FROM Cliente WHERE dni = '31666777');
DECLARE @cliDiego INT = (SELECT idCliente FROM Cliente WHERE dni = '29777888');
DECLARE @cliSofia INT = (SELECT idCliente FROM Cliente WHERE dni = '33888999');
DECLARE @cliMartin INT = (SELECT idCliente FROM Cliente WHERE dni = '26999000');
DECLARE @cliValentina INT = (SELECT idCliente FROM Cliente WHERE dni = '34000111');

/* --- 2. Vehículos (10, uno por cliente) ----------------------------------- */
INSERT INTO Vehiculo (idCliente, patente, marca, modelo, anio, tipoCombustible) VALUES
    (@cliJuan, 'ABC123', 'Toyota', 'Corolla', 2018, 'Nafta'),
    (@cliMaria, 'AD456EF', 'Volkswagen', 'Gol', 2020, 'Nafta'),
    (@cliCarlos, 'XYZ789', 'Ford', 'Focus', 2016, 'Diésel'),
    (@cliAna, 'AE789GH', 'Chevrolet', 'Onix', 2021, 'Nafta'),
    (@cliLuis, 'JKL321', 'Renault', 'Clio', 2015, 'Nafta'),
    (@cliLaura, 'AF234BC', 'Fiat', 'Cronos', 2022, 'GNC'),
    (@cliDiego, 'MNO654', 'Peugeot', '208', 2019, 'Nafta'),
    (@cliSofia, 'AG567DE', 'Honda', 'Civic', 2017, 'Nafta'),
    (@cliMartin, 'PQR987', 'Toyota', 'Hilux', 2020, 'Diésel'),
    (@cliValentina, 'AH890FG', 'Chevrolet', 'Spark', 2023, 'Eléctrico');

DECLARE @vehJuan INT = (SELECT idVehiculo FROM Vehiculo WHERE patente = 'ABC123');
DECLARE @vehMaria INT = (SELECT idVehiculo FROM Vehiculo WHERE patente = 'AD456EF');
DECLARE @vehCarlos INT = (SELECT idVehiculo FROM Vehiculo WHERE patente = 'XYZ789');
DECLARE @vehAna INT = (SELECT idVehiculo FROM Vehiculo WHERE patente = 'AE789GH');
DECLARE @vehLuis INT = (SELECT idVehiculo FROM Vehiculo WHERE patente = 'JKL321');
DECLARE @vehLaura INT = (SELECT idVehiculo FROM Vehiculo WHERE patente = 'AF234BC');
DECLARE @vehDiego INT = (SELECT idVehiculo FROM Vehiculo WHERE patente = 'MNO654');
DECLARE @vehSofia INT = (SELECT idVehiculo FROM Vehiculo WHERE patente = 'AG567DE');
DECLARE @vehMartin INT = (SELECT idVehiculo FROM Vehiculo WHERE patente = 'PQR987');
DECLARE @vehValentina INT = (SELECT idVehiculo FROM Vehiculo WHERE patente = 'AH890FG');

/* --- 3. Proveedores (10) --------------------------------------------------- */
INSERT INTO Proveedor (razonSocial, cuit, telefono, email, direccion) VALUES
    ('YPF Lubricantes S.A.', '30711234561', '11-5000-1001', 'ventas@ypflubricantes.com.ar', 'Av. del Libertador 1000, CABA'),
    ('Shell Argentina S.R.L.', '30711234562', '11-5000-1002', 'contacto@shellargentina.com.ar', 'Av. Leandro N. Alem 2000, CABA'),
    ('Distribuidora Filtros del Sur S.A.', '30711234563', '11-5000-1003', 'ventas@filtrosdelsur.com.ar', 'Av. Hipólito Yrigoyen 3400, Lanús'),
    ('Repuestos Mercedes S.R.L.', '30711234564', '11-5000-1004', 'info@repuestosmercedes.com.ar', 'Av. Mitre 4500, San Justo'),
    ('Neumáticos del Plata S.A.', '30711234565', '11-5000-1005', 'ventas@neumaticosdelplata.com.ar', 'Camino Gral. Belgrano 5600, Quilmes'),
    ('Baterías Moura Argentina S.A.', '30711234566', '11-5000-1006', 'comercial@baterismoura.com.ar', 'Ruta 8 Km 45, Pilar'),
    ('Autopartes San Martín S.R.L.', '30711234567', '11-5000-1007', 'ventas@autopartessm.com.ar', 'Av. San Martín 6700, CABA'),
    ('Lubricantes Total Argentina S.A.', '30711234568', '11-5000-1008', 'info@totalargentina.com.ar', 'Av. Córdoba 7800, CABA'),
    ('Distribuidora Bosch Repuestos S.A.', '30711234569', '11-5000-1009', 'ventas@boschrepuestos.com.ar', 'Av. Boulogne Sur Mer 8900, San Isidro'),
    ('Correas y Filtros del Norte S.R.L.', '30711234570', '11-5000-1010', 'contacto@correasfiltrosnorte.com.ar', 'Panamericana Km 32, Tigre');

DECLARE @provYPF INT = (SELECT idProveedor FROM Proveedor WHERE cuit = '30711234561');
DECLARE @provShell INT = (SELECT idProveedor FROM Proveedor WHERE cuit = '30711234562');
DECLARE @provFiltrosSur INT = (SELECT idProveedor FROM Proveedor WHERE cuit = '30711234563');
DECLARE @provMercedes INT = (SELECT idProveedor FROM Proveedor WHERE cuit = '30711234564');
DECLARE @provNeumaticos INT = (SELECT idProveedor FROM Proveedor WHERE cuit = '30711234565');
DECLARE @provMoura INT = (SELECT idProveedor FROM Proveedor WHERE cuit = '30711234566');
DECLARE @provSanMartin INT = (SELECT idProveedor FROM Proveedor WHERE cuit = '30711234567');
DECLARE @provTotal INT = (SELECT idProveedor FROM Proveedor WHERE cuit = '30711234568');
DECLARE @provBosch INT = (SELECT idProveedor FROM Proveedor WHERE cuit = '30711234569');
DECLARE @provNorte INT = (SELECT idProveedor FROM Proveedor WHERE cuit = '30711234570');

/* --- 4. Servicios (10) ------------------------------------------------------ */
INSERT INTO Servicio (nombre, descripcion, precioBase) VALUES
    ('Cambio de aceite y filtro', 'Cambio de aceite de motor y filtro de aceite', 15000),
    ('Rotación de neumáticos', 'Rotación de las cuatro ruedas', 8000),
    ('Alineación', 'Alineación de dirección', 12000),
    ('Balanceo', 'Balanceo de las cuatro ruedas', 10000),
    ('Cambio de filtro de aire', 'Reemplazo del filtro de aire del motor', 6000),
    ('Cambio de filtro de combustible', 'Reemplazo del filtro de combustible', 7000),
    ('Cambio de líquido de frenos', 'Purga y reemplazo de líquido de frenos', 11000),
    ('Cambio de correa de distribución', 'Reemplazo de correa de distribución', 45000),
    ('Revisión y carga de batería', 'Diagnóstico y carga de batería', 5000),
    ('Cambio de amortiguadores', 'Reemplazo de amortiguadores delanteros o traseros', 60000);

DECLARE @svcCambioAceite INT = (SELECT idServicio FROM Servicio WHERE nombre = 'Cambio de aceite y filtro');
DECLARE @svcRotacion INT = (SELECT idServicio FROM Servicio WHERE nombre = 'Rotación de neumáticos');
DECLARE @svcAlineacion INT = (SELECT idServicio FROM Servicio WHERE nombre = 'Alineación');
DECLARE @svcBalanceo INT = (SELECT idServicio FROM Servicio WHERE nombre = 'Balanceo');
DECLARE @svcFiltroAire INT = (SELECT idServicio FROM Servicio WHERE nombre = 'Cambio de filtro de aire');
DECLARE @svcFiltroComb INT = (SELECT idServicio FROM Servicio WHERE nombre = 'Cambio de filtro de combustible');
DECLARE @svcLiquidoFrenos INT = (SELECT idServicio FROM Servicio WHERE nombre = 'Cambio de líquido de frenos');
DECLARE @svcCorrea INT = (SELECT idServicio FROM Servicio WHERE nombre = 'Cambio de correa de distribución');
DECLARE @svcBateria INT = (SELECT idServicio FROM Servicio WHERE nombre = 'Revisión y carga de batería');
DECLARE @svcAmortiguadores INT = (SELECT idServicio FROM Servicio WHERE nombre = 'Cambio de amortiguadores');

/* --- 5. Insumos (10) --------------------------------------------------------
   Se insertan con stockActual = 0 y el stock inicial se respalda con un
   movimiento AjusteManual en el kardex, igual que InsumoDAL.Crear +
   MovimientoStockDAL.RegistrarAjusteManual.                                 */
INSERT INTO Insumo (nombre, marca, unidadMedida, stockActual, stockMinimo, precioVenta) VALUES
    ('Aceite 15W40', 'YPF', 'Litro', 0, 20, 3500),
    ('Aceite 5W30 sintético', 'Shell', 'Litro', 0, 15, 5200),
    ('Filtro de aceite', 'Fram', 'Unidad', 0, 10, 4500),
    ('Filtro de aire', 'Fram', 'Unidad', 0, 10, 5000),
    ('Filtro de combustible', 'Bosch', 'Unidad', 0, 8, 6000),
    ('Líquido de frenos DOT4', 'Motul', 'Litro', 0, 5, 4200),
    ('Refrigerante', 'Genérico', 'Litro', 0, 5, 3800),
    ('Correa de distribución', 'Gates', 'Unidad', 0, 3, 18000),
    ('Batería 12V 65Ah', 'Moura', 'Unidad', 0, 2, 55000),
    ('Bujías (caja x4)', 'NGK', 'Caja', 0, 5, 9000);

DECLARE @insAceite15 INT = (SELECT idInsumo FROM Insumo WHERE nombre = 'Aceite 15W40');
DECLARE @insAceite5w30 INT = (SELECT idInsumo FROM Insumo WHERE nombre = 'Aceite 5W30 sintético');
DECLARE @insFiltroAceite INT = (SELECT idInsumo FROM Insumo WHERE nombre = 'Filtro de aceite');
DECLARE @insFiltroAire INT = (SELECT idInsumo FROM Insumo WHERE nombre = 'Filtro de aire');
DECLARE @insFiltroComb INT = (SELECT idInsumo FROM Insumo WHERE nombre = 'Filtro de combustible');
DECLARE @insLiquidoFrenos INT = (SELECT idInsumo FROM Insumo WHERE nombre = 'Líquido de frenos DOT4');
DECLARE @insRefrigerante INT = (SELECT idInsumo FROM Insumo WHERE nombre = 'Refrigerante');
DECLARE @insCorrea INT = (SELECT idInsumo FROM Insumo WHERE nombre = 'Correa de distribución');
DECLARE @insBateria INT = (SELECT idInsumo FROM Insumo WHERE nombre = 'Batería 12V 65Ah');
DECLARE @insBujias INT = (SELECT idInsumo FROM Insumo WHERE nombre = 'Bujías (caja x4)');

/* Stock corriente de cada insumo: arranca en el stock inicial y se va
   actualizando en línea con cada línea de Orden/Compra más abajo — con solo
   10 insumos alcanza con una variable por insumo, sin cursores. */
DECLARE @stockAceite15 DECIMAL(12,2) = 100;
DECLARE @stockAceite5w30 DECIMAL(12,2) = 80;
DECLARE @stockFiltroAceite DECIMAL(12,2) = 50;
DECLARE @stockFiltroAire DECIMAL(12,2) = 40;
DECLARE @stockFiltroComb DECIMAL(12,2) = 35;
DECLARE @stockLiquidoFrenos DECIMAL(12,2) = 30;
DECLARE @stockRefrigerante DECIMAL(12,2) = 25;
DECLARE @stockCorrea DECIMAL(12,2) = 15;
DECLARE @stockBateria DECIMAL(12,2) = 10;
DECLARE @stockBujias DECIMAL(12,2) = 20;

INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion) VALUES
    (@insAceite15, 'AjusteManual', NULL, NULL, @admin, @stockAceite15, 0, @stockAceite15, 'Alta de insumo — stock inicial'),
    (@insAceite5w30, 'AjusteManual', NULL, NULL, @admin, @stockAceite5w30, 0, @stockAceite5w30, 'Alta de insumo — stock inicial'),
    (@insFiltroAceite, 'AjusteManual', NULL, NULL, @admin, @stockFiltroAceite, 0, @stockFiltroAceite, 'Alta de insumo — stock inicial'),
    (@insFiltroAire, 'AjusteManual', NULL, NULL, @admin, @stockFiltroAire, 0, @stockFiltroAire, 'Alta de insumo — stock inicial'),
    (@insFiltroComb, 'AjusteManual', NULL, NULL, @admin, @stockFiltroComb, 0, @stockFiltroComb, 'Alta de insumo — stock inicial'),
    (@insLiquidoFrenos, 'AjusteManual', NULL, NULL, @admin, @stockLiquidoFrenos, 0, @stockLiquidoFrenos, 'Alta de insumo — stock inicial'),
    (@insRefrigerante, 'AjusteManual', NULL, NULL, @admin, @stockRefrigerante, 0, @stockRefrigerante, 'Alta de insumo — stock inicial'),
    (@insCorrea, 'AjusteManual', NULL, NULL, @admin, @stockCorrea, 0, @stockCorrea, 'Alta de insumo — stock inicial'),
    (@insBateria, 'AjusteManual', NULL, NULL, @admin, @stockBateria, 0, @stockBateria, 'Alta de insumo — stock inicial'),
    (@insBujias, 'AjusteManual', NULL, NULL, @admin, @stockBujias, 0, @stockBujias, 'Alta de insumo — stock inicial');

UPDATE Insumo SET stockActual = @stockAceite15 WHERE idInsumo = @insAceite15;
UPDATE Insumo SET stockActual = @stockAceite5w30 WHERE idInsumo = @insAceite5w30;
UPDATE Insumo SET stockActual = @stockFiltroAceite WHERE idInsumo = @insFiltroAceite;
UPDATE Insumo SET stockActual = @stockFiltroAire WHERE idInsumo = @insFiltroAire;
UPDATE Insumo SET stockActual = @stockFiltroComb WHERE idInsumo = @insFiltroComb;
UPDATE Insumo SET stockActual = @stockLiquidoFrenos WHERE idInsumo = @insLiquidoFrenos;
UPDATE Insumo SET stockActual = @stockRefrigerante WHERE idInsumo = @insRefrigerante;
UPDATE Insumo SET stockActual = @stockCorrea WHERE idInsumo = @insCorrea;
UPDATE Insumo SET stockActual = @stockBateria WHERE idInsumo = @insBateria;
UPDATE Insumo SET stockActual = @stockBujias WHERE idInsumo = @insBujias;

/* --- 6. Turnos (10, uno por cliente) ---------------------------------------
   Los que van a derivar en una Orden de trabajo (más abajo) quedan
   Completado; el resto son agenda futura, o un Cancelado que nunca genera
   orden (el cliente avisó que no podía venir).                              */
INSERT INTO Turno (idCliente, idVehiculo, fechaSolicitud, fechaHoraAsignada, estado, observaciones) VALUES
    (@cliJuan, @vehJuan, DATEADD(DAY, 3, GETDATE()), DATEADD(DAY, 5, GETDATE()), 'Confirmado', 'Rotación de neumáticos'),
    (@cliMaria, @vehMaria, DATEADD(DAY, -22, GETDATE()), DATEADD(DAY, -20, GETDATE()), 'Completado', 'Service de aceite'),
    (@cliCarlos, @vehCarlos, DATEADD(DAY, -20, GETDATE()), DATEADD(DAY, -18, GETDATE()), 'Completado', 'Alineación y balanceo'),
    (@cliAna, @vehAna, DATEADD(DAY, -17, GETDATE()), DATEADD(DAY, -15, GETDATE()), 'Completado', 'Cambio de filtro de aire'),
    (@cliLuis, @vehLuis, DATEADD(DAY, 1, GETDATE()), DATEADD(DAY, 3, GETDATE()), 'Solicitado', 'Consulta por ruido en frenos'),
    (@cliLaura, @vehLaura, DATEADD(DAY, -12, GETDATE()), DATEADD(DAY, -10, GETDATE()), 'Cancelado', 'El cliente avisó que no podía venir'),
    (@cliDiego, @vehDiego, DATEADD(DAY, -9, GETDATE()), DATEADD(DAY, -7, GETDATE()), 'Completado', 'Correa de distribución'),
    (@cliSofia, @vehSofia, DATEADD(DAY, 0, GETDATE()), DATEADD(DAY, 2, GETDATE()), 'Confirmado', 'Turno para revisión general'),
    (@cliMartin, @vehMartin, DATEADD(DAY, -7, GETDATE()), DATEADD(DAY, -5, GETDATE()), 'Completado', 'Batería descargada'),
    (@cliValentina, @vehValentina, DATEADD(DAY, 5, GETDATE()), DATEADD(DAY, 7, GETDATE()), 'Solicitado', 'Primer service');

DECLARE @turnoMaria INT = (SELECT idTurno FROM Turno WHERE idCliente = @cliMaria);
DECLARE @turnoCarlos INT = (SELECT idTurno FROM Turno WHERE idCliente = @cliCarlos);
DECLARE @turnoAna INT = (SELECT idTurno FROM Turno WHERE idCliente = @cliAna);
DECLARE @turnoDiego INT = (SELECT idTurno FROM Turno WHERE idCliente = @cliDiego);
DECLARE @turnoMartin INT = (SELECT idTurno FROM Turno WHERE idCliente = @cliMartin);

/* --- 7. Órdenes de trabajo (10) + detalle + kardex + ventas generadas -----
   4 Cerrada (generan venta), 3 En proceso, 2 Abierta, 1 Cancelada (repone
   el stock del insumo que llegó a cargarse).                                */

-- Orden 1: María Gómez / AD456EF — desde Turno — se cierra (genera venta)
INSERT INTO OrdenDeTrabajo (idTurno, idCliente, idVehiculo, idUsuario, fecha, kilometraje, observaciones, estado)
VALUES (@turnoMaria, @cliMaria, @vehMaria, @empleado, DATEADD(DAY, -20, GETDATE()), 45000, 'Cambio de aceite y filtro', 'Abierta');
DECLARE @orden1 INT = SCOPE_IDENTITY();

INSERT INTO DetalleOrdenServicio (idOrden, idServicio, cantidad, precioAplicado)
VALUES (@orden1, @svcCambioAceite, 1, 15000);

INSERT INTO DetalleOrdenInsumo (idOrden, idInsumo, cantidad, precioUnitario) VALUES (@orden1, @insAceite15, 4, 3500);
SET @stockAceite15 = @stockAceite15 - 4;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insAceite15, 'Orden', NULL, @orden1, @empleado, 0, 4, @stockAceite15, 'Orden de trabajo #' + CAST(@orden1 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockAceite15 WHERE idInsumo = @insAceite15;

INSERT INTO DetalleOrdenInsumo (idOrden, idInsumo, cantidad, precioUnitario) VALUES (@orden1, @insFiltroAceite, 1, 4500);
SET @stockFiltroAceite = @stockFiltroAceite - 1;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insFiltroAceite, 'Orden', NULL, @orden1, @empleado, 0, 1, @stockFiltroAceite, 'Orden de trabajo #' + CAST(@orden1 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockFiltroAceite WHERE idInsumo = @insFiltroAceite;

INSERT INTO ComprobanteVenta (idOrden, idCliente, numeroComprobante, fecha, subtotal, impuestos, total, saldoPendiente)
VALUES (@orden1, @cliMaria, '(pendiente)', DATEADD(DAY, -20, GETDATE()), 33500, 0, 33500, 33500);
DECLARE @venta1 INT = SCOPE_IDENTITY();

INSERT INTO DetalleComprobanteVenta (idVenta, tipoItem, idServicio, idInsumo, descripcion, cantidad, precioUnitario, subtotal) VALUES
    (@venta1, 'S', @svcCambioAceite, NULL, 'Cambio de aceite y filtro', 1, 15000, 15000),
    (@venta1, 'I', NULL, @insAceite15, 'Aceite 15W40', 4, 3500, 14000),
    (@venta1, 'I', NULL, @insFiltroAceite, 'Filtro de aceite', 1, 4500, 4500);

INSERT INTO CuentaCorrienteCliente (idCliente, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@cliMaria, 'Venta', @venta1, NULL, 33500, 0,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente WHERE idCliente = @cliMaria ORDER BY idMovimiento DESC), 0) + 33500,
    'Venta #' + CAST(@venta1 AS VARCHAR(10)), NULL);

UPDATE ComprobanteVenta SET numeroComprobante = 'V-' + RIGHT('000000' + CAST(@venta1 AS VARCHAR(10)), 6) WHERE idVenta = @venta1;
UPDATE OrdenDeTrabajo SET estado = 'Cerrada' WHERE idOrden = @orden1;

-- Orden 2: Carlos Rodríguez / XYZ789 — se cierra, sin insumos
INSERT INTO OrdenDeTrabajo (idTurno, idCliente, idVehiculo, idUsuario, fecha, kilometraje, observaciones, estado)
VALUES (@turnoCarlos, @cliCarlos, @vehCarlos, @empleado, DATEADD(DAY, -18, GETDATE()), 88000, 'Alineación y balanceo', 'Abierta');
DECLARE @orden2 INT = SCOPE_IDENTITY();

INSERT INTO DetalleOrdenServicio (idOrden, idServicio, cantidad, precioAplicado) VALUES
    (@orden2, @svcAlineacion, 1, 12000),
    (@orden2, @svcBalanceo, 1, 10000);

INSERT INTO ComprobanteVenta (idOrden, idCliente, numeroComprobante, fecha, subtotal, impuestos, total, saldoPendiente)
VALUES (@orden2, @cliCarlos, '(pendiente)', DATEADD(DAY, -18, GETDATE()), 22000, 0, 22000, 22000);
DECLARE @venta2 INT = SCOPE_IDENTITY();

INSERT INTO DetalleComprobanteVenta (idVenta, tipoItem, idServicio, idInsumo, descripcion, cantidad, precioUnitario, subtotal) VALUES
    (@venta2, 'S', @svcAlineacion, NULL, 'Alineación', 1, 12000, 12000),
    (@venta2, 'S', @svcBalanceo, NULL, 'Balanceo', 1, 10000, 10000);

INSERT INTO CuentaCorrienteCliente (idCliente, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@cliCarlos, 'Venta', @venta2, NULL, 22000, 0,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente WHERE idCliente = @cliCarlos ORDER BY idMovimiento DESC), 0) + 22000,
    'Venta #' + CAST(@venta2 AS VARCHAR(10)), NULL);

UPDATE ComprobanteVenta SET numeroComprobante = 'V-' + RIGHT('000000' + CAST(@venta2 AS VARCHAR(10)), 6) WHERE idVenta = @venta2;
UPDATE OrdenDeTrabajo SET estado = 'Cerrada' WHERE idOrden = @orden2;

-- Orden 3: Ana López / AE789GH — se cierra
INSERT INTO OrdenDeTrabajo (idTurno, idCliente, idVehiculo, idUsuario, fecha, kilometraje, observaciones, estado)
VALUES (@turnoAna, @cliAna, @vehAna, @empleado, DATEADD(DAY, -15, GETDATE()), 25000, 'Cambio de filtro de aire', 'Abierta');
DECLARE @orden3 INT = SCOPE_IDENTITY();

INSERT INTO DetalleOrdenServicio (idOrden, idServicio, cantidad, precioAplicado) VALUES (@orden3, @svcFiltroAire, 1, 6000);

INSERT INTO DetalleOrdenInsumo (idOrden, idInsumo, cantidad, precioUnitario) VALUES (@orden3, @insFiltroAire, 1, 5000);
SET @stockFiltroAire = @stockFiltroAire - 1;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insFiltroAire, 'Orden', NULL, @orden3, @empleado, 0, 1, @stockFiltroAire, 'Orden de trabajo #' + CAST(@orden3 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockFiltroAire WHERE idInsumo = @insFiltroAire;

INSERT INTO ComprobanteVenta (idOrden, idCliente, numeroComprobante, fecha, subtotal, impuestos, total, saldoPendiente)
VALUES (@orden3, @cliAna, '(pendiente)', DATEADD(DAY, -15, GETDATE()), 11000, 0, 11000, 11000);
DECLARE @venta3 INT = SCOPE_IDENTITY();

INSERT INTO DetalleComprobanteVenta (idVenta, tipoItem, idServicio, idInsumo, descripcion, cantidad, precioUnitario, subtotal) VALUES
    (@venta3, 'S', @svcFiltroAire, NULL, 'Cambio de filtro de aire', 1, 6000, 6000),
    (@venta3, 'I', NULL, @insFiltroAire, 'Filtro de aire', 1, 5000, 5000);

INSERT INTO CuentaCorrienteCliente (idCliente, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@cliAna, 'Venta', @venta3, NULL, 11000, 0,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente WHERE idCliente = @cliAna ORDER BY idMovimiento DESC), 0) + 11000,
    'Venta #' + CAST(@venta3 AS VARCHAR(10)), NULL);

UPDATE ComprobanteVenta SET numeroComprobante = 'V-' + RIGHT('000000' + CAST(@venta3 AS VARCHAR(10)), 6) WHERE idVenta = @venta3;
UPDATE OrdenDeTrabajo SET estado = 'Cerrada' WHERE idOrden = @orden3;

-- Orden 4: Diego Sánchez / MNO654 — En proceso (correa de distribución, sin cerrar)
INSERT INTO OrdenDeTrabajo (idTurno, idCliente, idVehiculo, idUsuario, fecha, kilometraje, observaciones, estado)
VALUES (@turnoDiego, @cliDiego, @vehDiego, @empleado, DATEADD(DAY, -7, GETDATE()), 95000, 'Cambio de correa de distribución', 'En proceso');
DECLARE @orden4 INT = SCOPE_IDENTITY();

INSERT INTO DetalleOrdenServicio (idOrden, idServicio, cantidad, precioAplicado) VALUES (@orden4, @svcCorrea, 1, 45000);

INSERT INTO DetalleOrdenInsumo (idOrden, idInsumo, cantidad, precioUnitario) VALUES (@orden4, @insCorrea, 1, 18000);
SET @stockCorrea = @stockCorrea - 1;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insCorrea, 'Orden', NULL, @orden4, @empleado, 0, 1, @stockCorrea, 'Orden de trabajo #' + CAST(@orden4 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockCorrea WHERE idInsumo = @insCorrea;

-- Orden 5: Martín Díaz / PQR987 — Abierta (batería, sin cerrar)
INSERT INTO OrdenDeTrabajo (idTurno, idCliente, idVehiculo, idUsuario, fecha, kilometraje, observaciones, estado)
VALUES (@turnoMartin, @cliMartin, @vehMartin, @empleado, DATEADD(DAY, -5, GETDATE()), 120000, 'Batería descargada', 'Abierta');
DECLARE @orden5 INT = SCOPE_IDENTITY();

INSERT INTO DetalleOrdenServicio (idOrden, idServicio, cantidad, precioAplicado) VALUES (@orden5, @svcBateria, 1, 5000);

INSERT INTO DetalleOrdenInsumo (idOrden, idInsumo, cantidad, precioUnitario) VALUES (@orden5, @insBateria, 1, 55000);
SET @stockBateria = @stockBateria - 1;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insBateria, 'Orden', NULL, @orden5, @empleado, 0, 1, @stockBateria, 'Orden de trabajo #' + CAST(@orden5 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockBateria WHERE idInsumo = @insBateria;

-- Orden 6: Juan Pérez / ABC123 — walk-in, Abierta (rotación, sin insumos)
INSERT INTO OrdenDeTrabajo (idTurno, idCliente, idVehiculo, idUsuario, fecha, kilometraje, observaciones, estado)
VALUES (NULL, @cliJuan, @vehJuan, @empleado, DATEADD(DAY, -3, GETDATE()), 60000, 'Walk-in: rotación de neumáticos', 'Abierta');
DECLARE @orden6 INT = SCOPE_IDENTITY();

INSERT INTO DetalleOrdenServicio (idOrden, idServicio, cantidad, precioAplicado) VALUES (@orden6, @svcRotacion, 1, 8000);

-- Orden 7: Luis Fernández / JKL321 — walk-in, En proceso (líquido de frenos)
INSERT INTO OrdenDeTrabajo (idTurno, idCliente, idVehiculo, idUsuario, fecha, kilometraje, observaciones, estado)
VALUES (NULL, @cliLuis, @vehLuis, @empleado, DATEADD(DAY, -6, GETDATE()), 150000, 'Walk-in: cambio de líquido de frenos', 'En proceso');
DECLARE @orden7 INT = SCOPE_IDENTITY();

INSERT INTO DetalleOrdenServicio (idOrden, idServicio, cantidad, precioAplicado) VALUES (@orden7, @svcLiquidoFrenos, 1, 11000);

INSERT INTO DetalleOrdenInsumo (idOrden, idInsumo, cantidad, precioUnitario) VALUES (@orden7, @insLiquidoFrenos, 2, 4200);
SET @stockLiquidoFrenos = @stockLiquidoFrenos - 2;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insLiquidoFrenos, 'Orden', NULL, @orden7, @empleado, 0, 2, @stockLiquidoFrenos, 'Orden de trabajo #' + CAST(@orden7 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockLiquidoFrenos WHERE idInsumo = @insLiquidoFrenos;

-- Orden 8: Laura Martínez / AF234BC — walk-in, se cancela (repone el stock cargado, OrdenDeTrabajoDAL.Cancelar)
INSERT INTO OrdenDeTrabajo (idTurno, idCliente, idVehiculo, idUsuario, fecha, kilometraje, observaciones, estado)
VALUES (NULL, @cliLaura, @vehLaura, @empleado, DATEADD(DAY, -12, GETDATE()), 15000, 'Walk-in: cambio de amortiguadores — el cliente canceló', 'Abierta');
DECLARE @orden8 INT = SCOPE_IDENTITY();

INSERT INTO DetalleOrdenServicio (idOrden, idServicio, cantidad, precioAplicado) VALUES (@orden8, @svcAmortiguadores, 1, 60000);

INSERT INTO DetalleOrdenInsumo (idOrden, idInsumo, cantidad, precioUnitario) VALUES (@orden8, @insFiltroComb, 1, 6000);
SET @stockFiltroComb = @stockFiltroComb - 1;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insFiltroComb, 'Orden', NULL, @orden8, @empleado, 0, 1, @stockFiltroComb, 'Orden de trabajo #' + CAST(@orden8 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockFiltroComb WHERE idInsumo = @insFiltroComb;

SET @stockFiltroComb = @stockFiltroComb + 1;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insFiltroComb, 'CancelacionOrden', NULL, @orden8, @empleado, 1, 0, @stockFiltroComb, 'Reposición por cancelación de orden #' + CAST(@orden8 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockFiltroComb WHERE idInsumo = @insFiltroComb;
UPDATE OrdenDeTrabajo SET estado = 'Cancelada' WHERE idOrden = @orden8;

-- Orden 9: Sofía Romero / AG567DE — walk-in, En proceso
INSERT INTO OrdenDeTrabajo (idTurno, idCliente, idVehiculo, idUsuario, fecha, kilometraje, observaciones, estado)
VALUES (NULL, @cliSofia, @vehSofia, @empleado, DATEADD(DAY, -4, GETDATE()), 40000, 'Walk-in: cambio de filtro de combustible', 'En proceso');
DECLARE @orden9 INT = SCOPE_IDENTITY();

INSERT INTO DetalleOrdenServicio (idOrden, idServicio, cantidad, precioAplicado) VALUES (@orden9, @svcFiltroComb, 1, 7000);

INSERT INTO DetalleOrdenInsumo (idOrden, idInsumo, cantidad, precioUnitario) VALUES (@orden9, @insFiltroComb, 1, 6000);
SET @stockFiltroComb = @stockFiltroComb - 1;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insFiltroComb, 'Orden', NULL, @orden9, @empleado, 0, 1, @stockFiltroComb, 'Orden de trabajo #' + CAST(@orden9 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockFiltroComb WHERE idInsumo = @insFiltroComb;

-- Orden 10: Valentina Torres / AH890FG — walk-in, se cierra
INSERT INTO OrdenDeTrabajo (idTurno, idCliente, idVehiculo, idUsuario, fecha, kilometraje, observaciones, estado)
VALUES (NULL, @cliValentina, @vehValentina, @empleado, DATEADD(DAY, -2, GETDATE()), 5000, 'Walk-in: primer service', 'Abierta');
DECLARE @orden10 INT = SCOPE_IDENTITY();

INSERT INTO DetalleOrdenServicio (idOrden, idServicio, cantidad, precioAplicado) VALUES
    (@orden10, @svcCambioAceite, 1, 15000),
    (@orden10, @svcBalanceo, 1, 10000);

INSERT INTO DetalleOrdenInsumo (idOrden, idInsumo, cantidad, precioUnitario) VALUES (@orden10, @insAceite5w30, 4, 5200);
SET @stockAceite5w30 = @stockAceite5w30 - 4;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insAceite5w30, 'Orden', NULL, @orden10, @empleado, 0, 4, @stockAceite5w30, 'Orden de trabajo #' + CAST(@orden10 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockAceite5w30 WHERE idInsumo = @insAceite5w30;

INSERT INTO DetalleOrdenInsumo (idOrden, idInsumo, cantidad, precioUnitario) VALUES (@orden10, @insFiltroAceite, 1, 4500);
SET @stockFiltroAceite = @stockFiltroAceite - 1;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insFiltroAceite, 'Orden', NULL, @orden10, @empleado, 0, 1, @stockFiltroAceite, 'Orden de trabajo #' + CAST(@orden10 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockFiltroAceite WHERE idInsumo = @insFiltroAceite;

INSERT INTO ComprobanteVenta (idOrden, idCliente, numeroComprobante, fecha, subtotal, impuestos, total, saldoPendiente)
VALUES (@orden10, @cliValentina, '(pendiente)', DATEADD(DAY, -2, GETDATE()), 50300, 0, 50300, 50300);
DECLARE @venta4 INT = SCOPE_IDENTITY();

INSERT INTO DetalleComprobanteVenta (idVenta, tipoItem, idServicio, idInsumo, descripcion, cantidad, precioUnitario, subtotal) VALUES
    (@venta4, 'S', @svcCambioAceite, NULL, 'Cambio de aceite y filtro', 1, 15000, 15000),
    (@venta4, 'S', @svcBalanceo, NULL, 'Balanceo', 1, 10000, 10000),
    (@venta4, 'I', NULL, @insAceite5w30, 'Aceite 5W30 sintético', 4, 5200, 20800),
    (@venta4, 'I', NULL, @insFiltroAceite, 'Filtro de aceite', 1, 4500, 4500);

INSERT INTO CuentaCorrienteCliente (idCliente, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@cliValentina, 'Venta', @venta4, NULL, 50300, 0,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente WHERE idCliente = @cliValentina ORDER BY idMovimiento DESC), 0) + 50300,
    'Venta #' + CAST(@venta4 AS VARCHAR(10)), NULL);

UPDATE ComprobanteVenta SET numeroComprobante = 'V-' + RIGHT('000000' + CAST(@venta4 AS VARCHAR(10)), 6) WHERE idVenta = @venta4;
UPDATE OrdenDeTrabajo SET estado = 'Cerrada' WHERE idOrden = @orden10;

/* --- 8. Compras (10) + detalle + kardex + cuenta corriente de proveedor --- */

-- Compra 1: YPF Lubricantes — Contado/Efectivo
INSERT INTO ComprobanteCompra (idProveedor, numeroComprobante, fecha, condicionPago, medioPago, subtotal, impuestos, total, saldoPendiente)
VALUES (@provYPF, '(pendiente)', DATEADD(DAY, -25, GETDATE()), 'Contado', 'Efectivo', 125000, 0, 125000, 0);
DECLARE @compra1 INT = SCOPE_IDENTITY();

INSERT INTO DetalleCompra (idCompra, idInsumo, cantidad, precioUnitario) VALUES (@compra1, @insAceite15, 50, 2500);
SET @stockAceite15 = @stockAceite15 + 50;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insAceite15, 'Compra', @compra1, NULL, @empleado, 50, 0, @stockAceite15, 'Compra #' + CAST(@compra1 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockAceite15 WHERE idInsumo = @insAceite15;

UPDATE ComprobanteCompra SET numeroComprobante = 'C-' + RIGHT('000000' + CAST(@compra1 AS VARCHAR(10)), 6) WHERE idCompra = @compra1;

-- Compra 2: Shell Argentina — Contado/Transferencia
INSERT INTO ComprobanteCompra (idProveedor, numeroComprobante, fecha, condicionPago, medioPago, subtotal, impuestos, total, saldoPendiente)
VALUES (@provShell, '(pendiente)', DATEADD(DAY, -22, GETDATE()), 'Contado', 'Transferencia', 152000, 0, 152000, 0);
DECLARE @compra2 INT = SCOPE_IDENTITY();

INSERT INTO DetalleCompra (idCompra, idInsumo, cantidad, precioUnitario) VALUES (@compra2, @insAceite5w30, 40, 3800);
SET @stockAceite5w30 = @stockAceite5w30 + 40;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insAceite5w30, 'Compra', @compra2, NULL, @empleado, 40, 0, @stockAceite5w30, 'Compra #' + CAST(@compra2 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockAceite5w30 WHERE idInsumo = @insAceite5w30;

UPDATE ComprobanteCompra SET numeroComprobante = 'C-' + RIGHT('000000' + CAST(@compra2 AS VARCHAR(10)), 6) WHERE idCompra = @compra2;

-- Compra 3: Distribuidora Filtros del Sur — Cuenta corriente (dos líneas)
INSERT INTO ComprobanteCompra (idProveedor, numeroComprobante, fecha, condicionPago, medioPago, subtotal, impuestos, total, saldoPendiente)
VALUES (@provFiltrosSur, '(pendiente)', DATEADD(DAY, -19, GETDATE()), 'Cuenta corriente', NULL, 160000, 0, 160000, 160000);
DECLARE @compra3 INT = SCOPE_IDENTITY();

INSERT INTO DetalleCompra (idCompra, idInsumo, cantidad, precioUnitario) VALUES (@compra3, @insFiltroAceite, 30, 3000);
SET @stockFiltroAceite = @stockFiltroAceite + 30;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insFiltroAceite, 'Compra', @compra3, NULL, @empleado, 30, 0, @stockFiltroAceite, 'Compra #' + CAST(@compra3 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockFiltroAceite WHERE idInsumo = @insFiltroAceite;

INSERT INTO DetalleCompra (idCompra, idInsumo, cantidad, precioUnitario) VALUES (@compra3, @insFiltroAire, 20, 3500);
SET @stockFiltroAire = @stockFiltroAire + 20;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insFiltroAire, 'Compra', @compra3, NULL, @empleado, 20, 0, @stockFiltroAire, 'Compra #' + CAST(@compra3 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockFiltroAire WHERE idInsumo = @insFiltroAire;

INSERT INTO CuentaCorrienteProveedor (idProveedor, tipoMovimiento, idCompra, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@provFiltrosSur, 'Compra', @compra3, NULL, 160000, 0,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteProveedor WHERE idProveedor = @provFiltrosSur ORDER BY idMovimiento DESC), 0) + 160000,
    'Compra #' + CAST(@compra3 AS VARCHAR(10)), NULL);

UPDATE ComprobanteCompra SET numeroComprobante = 'C-' + RIGHT('000000' + CAST(@compra3 AS VARCHAR(10)), 6) WHERE idCompra = @compra3;

-- Compra 4: Repuestos Mercedes — Cuenta corriente
INSERT INTO ComprobanteCompra (idProveedor, numeroComprobante, fecha, condicionPago, medioPago, subtotal, impuestos, total, saldoPendiente)
VALUES (@provMercedes, '(pendiente)', DATEADD(DAY, -16, GETDATE()), 'Cuenta corriente', NULL, 140000, 0, 140000, 140000);
DECLARE @compra4 INT = SCOPE_IDENTITY();

INSERT INTO DetalleCompra (idCompra, idInsumo, cantidad, precioUnitario) VALUES (@compra4, @insCorrea, 10, 14000);
SET @stockCorrea = @stockCorrea + 10;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insCorrea, 'Compra', @compra4, NULL, @empleado, 10, 0, @stockCorrea, 'Compra #' + CAST(@compra4 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockCorrea WHERE idInsumo = @insCorrea;

INSERT INTO CuentaCorrienteProveedor (idProveedor, tipoMovimiento, idCompra, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@provMercedes, 'Compra', @compra4, NULL, 140000, 0,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteProveedor WHERE idProveedor = @provMercedes ORDER BY idMovimiento DESC), 0) + 140000,
    'Compra #' + CAST(@compra4 AS VARCHAR(10)), NULL);

UPDATE ComprobanteCompra SET numeroComprobante = 'C-' + RIGHT('000000' + CAST(@compra4 AS VARCHAR(10)), 6) WHERE idCompra = @compra4;

-- Compra 5: Neumáticos del Plata — Contado/Tarjeta
INSERT INTO ComprobanteCompra (idProveedor, numeroComprobante, fecha, condicionPago, medioPago, subtotal, impuestos, total, saldoPendiente)
VALUES (@provNeumaticos, '(pendiente)', DATEADD(DAY, -14, GETDATE()), 'Contado', 'Tarjeta', 45000, 0, 45000, 0);
DECLARE @compra5 INT = SCOPE_IDENTITY();

INSERT INTO DetalleCompra (idCompra, idInsumo, cantidad, precioUnitario) VALUES (@compra5, @insLiquidoFrenos, 15, 3000);
SET @stockLiquidoFrenos = @stockLiquidoFrenos + 15;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insLiquidoFrenos, 'Compra', @compra5, NULL, @empleado, 15, 0, @stockLiquidoFrenos, 'Compra #' + CAST(@compra5 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockLiquidoFrenos WHERE idInsumo = @insLiquidoFrenos;

UPDATE ComprobanteCompra SET numeroComprobante = 'C-' + RIGHT('000000' + CAST(@compra5 AS VARCHAR(10)), 6) WHERE idCompra = @compra5;

-- Compra 6: Baterías Moura — Contado/Efectivo
INSERT INTO ComprobanteCompra (idProveedor, numeroComprobante, fecha, condicionPago, medioPago, subtotal, impuestos, total, saldoPendiente)
VALUES (@provMoura, '(pendiente)', DATEADD(DAY, -12, GETDATE()), 'Contado', 'Efectivo', 200000, 0, 200000, 0);
DECLARE @compra6 INT = SCOPE_IDENTITY();

INSERT INTO DetalleCompra (idCompra, idInsumo, cantidad, precioUnitario) VALUES (@compra6, @insBateria, 5, 40000);
SET @stockBateria = @stockBateria + 5;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insBateria, 'Compra', @compra6, NULL, @empleado, 5, 0, @stockBateria, 'Compra #' + CAST(@compra6 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockBateria WHERE idInsumo = @insBateria;

UPDATE ComprobanteCompra SET numeroComprobante = 'C-' + RIGHT('000000' + CAST(@compra6 AS VARCHAR(10)), 6) WHERE idCompra = @compra6;

-- Compra 7: Autopartes San Martín — Cuenta corriente
INSERT INTO ComprobanteCompra (idProveedor, numeroComprobante, fecha, condicionPago, medioPago, subtotal, impuestos, total, saldoPendiente)
VALUES (@provSanMartin, '(pendiente)', DATEADD(DAY, -9, GETDATE()), 'Cuenta corriente', NULL, 56000, 0, 56000, 56000);
DECLARE @compra7 INT = SCOPE_IDENTITY();

INSERT INTO DetalleCompra (idCompra, idInsumo, cantidad, precioUnitario) VALUES (@compra7, @insRefrigerante, 20, 2800);
SET @stockRefrigerante = @stockRefrigerante + 20;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insRefrigerante, 'Compra', @compra7, NULL, @empleado, 20, 0, @stockRefrigerante, 'Compra #' + CAST(@compra7 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockRefrigerante WHERE idInsumo = @insRefrigerante;

INSERT INTO CuentaCorrienteProveedor (idProveedor, tipoMovimiento, idCompra, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@provSanMartin, 'Compra', @compra7, NULL, 56000, 0,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteProveedor WHERE idProveedor = @provSanMartin ORDER BY idMovimiento DESC), 0) + 56000,
    'Compra #' + CAST(@compra7 AS VARCHAR(10)), NULL);

UPDATE ComprobanteCompra SET numeroComprobante = 'C-' + RIGHT('000000' + CAST(@compra7 AS VARCHAR(10)), 6) WHERE idCompra = @compra7;

-- Compra 8: Lubricantes Total — Contado/Transferencia
INSERT INTO ComprobanteCompra (idProveedor, numeroComprobante, fecha, condicionPago, medioPago, subtotal, impuestos, total, saldoPendiente)
VALUES (@provTotal, '(pendiente)', DATEADD(DAY, -6, GETDATE()), 'Contado', 'Transferencia', 72000, 0, 72000, 0);
DECLARE @compra8 INT = SCOPE_IDENTITY();

INSERT INTO DetalleCompra (idCompra, idInsumo, cantidad, precioUnitario) VALUES (@compra8, @insAceite15, 30, 2400);
SET @stockAceite15 = @stockAceite15 + 30;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insAceite15, 'Compra', @compra8, NULL, @empleado, 30, 0, @stockAceite15, 'Compra #' + CAST(@compra8 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockAceite15 WHERE idInsumo = @insAceite15;

UPDATE ComprobanteCompra SET numeroComprobante = 'C-' + RIGHT('000000' + CAST(@compra8 AS VARCHAR(10)), 6) WHERE idCompra = @compra8;

-- Compra 9: Distribuidora Bosch Repuestos — Cuenta corriente
INSERT INTO ComprobanteCompra (idProveedor, numeroComprobante, fecha, condicionPago, medioPago, subtotal, impuestos, total, saldoPendiente)
VALUES (@provBosch, '(pendiente)', DATEADD(DAY, -3, GETDATE()), 'Cuenta corriente', NULL, 60000, 0, 60000, 60000);
DECLARE @compra9 INT = SCOPE_IDENTITY();

INSERT INTO DetalleCompra (idCompra, idInsumo, cantidad, precioUnitario) VALUES (@compra9, @insBujias, 10, 6000);
SET @stockBujias = @stockBujias + 10;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insBujias, 'Compra', @compra9, NULL, @empleado, 10, 0, @stockBujias, 'Compra #' + CAST(@compra9 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockBujias WHERE idInsumo = @insBujias;

INSERT INTO CuentaCorrienteProveedor (idProveedor, tipoMovimiento, idCompra, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@provBosch, 'Compra', @compra9, NULL, 60000, 0,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteProveedor WHERE idProveedor = @provBosch ORDER BY idMovimiento DESC), 0) + 60000,
    'Compra #' + CAST(@compra9 AS VARCHAR(10)), NULL);

UPDATE ComprobanteCompra SET numeroComprobante = 'C-' + RIGHT('000000' + CAST(@compra9 AS VARCHAR(10)), 6) WHERE idCompra = @compra9;

-- Compra 10: Correas y Filtros del Norte — Contado/Efectivo
INSERT INTO ComprobanteCompra (idProveedor, numeroComprobante, fecha, condicionPago, medioPago, subtotal, impuestos, total, saldoPendiente)
VALUES (@provNorte, '(pendiente)', DATEADD(DAY, -1, GETDATE()), 'Contado', 'Efectivo', 63000, 0, 63000, 0);
DECLARE @compra10 INT = SCOPE_IDENTITY();

INSERT INTO DetalleCompra (idCompra, idInsumo, cantidad, precioUnitario) VALUES (@compra10, @insFiltroComb, 15, 4200);
SET @stockFiltroComb = @stockFiltroComb + 15;
INSERT INTO MovimientoStock (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
VALUES (@insFiltroComb, 'Compra', @compra10, NULL, @empleado, 15, 0, @stockFiltroComb, 'Compra #' + CAST(@compra10 AS VARCHAR(10)));
UPDATE Insumo SET stockActual = @stockFiltroComb WHERE idInsumo = @insFiltroComb;

UPDATE ComprobanteCompra SET numeroComprobante = 'C-' + RIGHT('000000' + CAST(@compra10 AS VARCHAR(10)), 6) WHERE idCompra = @compra10;

/* --- 9. Pagos (10) — 5 de cliente, 5 de proveedor -------------------------
   Mezcla de pago total, parcial y "a cuenta general" (sin comprobante
   puntual, sin techo de monto — PagoDAL.Registrar).                         */

-- Pago 1: María Gómez paga la Venta 1 completa
INSERT INTO Pago (tipo, idCliente, idProveedor, idVenta, idCompra, idUsuario, fecha, medioPago, monto, observaciones)
VALUES ('C', @cliMaria, NULL, @venta1, NULL, @encargado, DATEADD(DAY, -18, GETDATE()), 'Efectivo', 33500, 'Pago de venta V-000001');
DECLARE @pago1 INT = SCOPE_IDENTITY();
UPDATE ComprobanteVenta SET saldoPendiente = saldoPendiente - 33500 WHERE idVenta = @venta1;
INSERT INTO CuentaCorrienteCliente (idCliente, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@cliMaria, 'Pago', NULL, @pago1, 0, 33500,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente WHERE idCliente = @cliMaria ORDER BY idMovimiento DESC), 0) - 33500,
    'Pago de venta', NULL);

-- Pago 2: Carlos Rodríguez paga la mitad de la Venta 2
INSERT INTO Pago (tipo, idCliente, idProveedor, idVenta, idCompra, idUsuario, fecha, medioPago, monto, observaciones)
VALUES ('C', @cliCarlos, NULL, @venta2, NULL, @encargado, DATEADD(DAY, -10, GETDATE()), 'Transferencia', 11000, 'Pago parcial de venta V-000002');
DECLARE @pago2 INT = SCOPE_IDENTITY();
UPDATE ComprobanteVenta SET saldoPendiente = saldoPendiente - 11000 WHERE idVenta = @venta2;
INSERT INTO CuentaCorrienteCliente (idCliente, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@cliCarlos, 'Pago', NULL, @pago2, 0, 11000,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente WHERE idCliente = @cliCarlos ORDER BY idMovimiento DESC), 0) - 11000,
    'Pago de venta', NULL);

-- Pago 3: Ana López paga la Venta 3 completa
INSERT INTO Pago (tipo, idCliente, idProveedor, idVenta, idCompra, idUsuario, fecha, medioPago, monto, observaciones)
VALUES ('C', @cliAna, NULL, @venta3, NULL, @encargado, DATEADD(DAY, -13, GETDATE()), 'Tarjeta', 11000, 'Pago de venta V-000003');
DECLARE @pago3 INT = SCOPE_IDENTITY();
UPDATE ComprobanteVenta SET saldoPendiente = saldoPendiente - 11000 WHERE idVenta = @venta3;
INSERT INTO CuentaCorrienteCliente (idCliente, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@cliAna, 'Pago', NULL, @pago3, 0, 11000,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente WHERE idCliente = @cliAna ORDER BY idMovimiento DESC), 0) - 11000,
    'Pago de venta', NULL);

-- Pago 4: Valentina Torres paga parte de la Venta 4
INSERT INTO Pago (tipo, idCliente, idProveedor, idVenta, idCompra, idUsuario, fecha, medioPago, monto, observaciones)
VALUES ('C', @cliValentina, NULL, @venta4, NULL, @encargado, DATEADD(DAY, -1, GETDATE()), 'Efectivo', 25000, 'Pago parcial de venta V-000004');
DECLARE @pago4 INT = SCOPE_IDENTITY();
UPDATE ComprobanteVenta SET saldoPendiente = saldoPendiente - 25000 WHERE idVenta = @venta4;
INSERT INTO CuentaCorrienteCliente (idCliente, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@cliValentina, 'Pago', NULL, @pago4, 0, 25000,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente WHERE idCliente = @cliValentina ORDER BY idMovimiento DESC), 0) - 25000,
    'Pago de venta', NULL);

-- Pago 5: Juan Pérez, pago a cuenta general (sin comprobante puntual)
INSERT INTO Pago (tipo, idCliente, idProveedor, idVenta, idCompra, idUsuario, fecha, medioPago, monto, observaciones)
VALUES ('C', @cliJuan, NULL, NULL, NULL, @encargado, DATEADD(DAY, -6, GETDATE()), 'Efectivo', 5000, 'Pago a cuenta');
DECLARE @pago5 INT = SCOPE_IDENTITY();
INSERT INTO CuentaCorrienteCliente (idCliente, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@cliJuan, 'Pago', NULL, @pago5, 0, 5000,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente WHERE idCliente = @cliJuan ORDER BY idMovimiento DESC), 0) - 5000,
    'Pago a cuenta', NULL);

-- Pago 6: pago completo a Distribuidora Filtros del Sur (Compra 3)
INSERT INTO Pago (tipo, idCliente, idProveedor, idVenta, idCompra, idUsuario, fecha, medioPago, monto, observaciones)
VALUES ('P', NULL, @provFiltrosSur, NULL, @compra3, @encargado, DATEADD(DAY, -15, GETDATE()), 'Transferencia', 160000, 'Pago de compra C-000003');
DECLARE @pago6 INT = SCOPE_IDENTITY();
UPDATE ComprobanteCompra SET saldoPendiente = saldoPendiente - 160000 WHERE idCompra = @compra3;
INSERT INTO CuentaCorrienteProveedor (idProveedor, tipoMovimiento, idCompra, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@provFiltrosSur, 'Pago', NULL, @pago6, 0, 160000,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteProveedor WHERE idProveedor = @provFiltrosSur ORDER BY idMovimiento DESC), 0) - 160000,
    'Pago de compra', NULL);

-- Pago 7: pago parcial a Repuestos Mercedes (Compra 4)
INSERT INTO Pago (tipo, idCliente, idProveedor, idVenta, idCompra, idUsuario, fecha, medioPago, monto, observaciones)
VALUES ('P', NULL, @provMercedes, NULL, @compra4, @encargado, DATEADD(DAY, -8, GETDATE()), 'Efectivo', 70000, 'Pago parcial de compra C-000004');
DECLARE @pago7 INT = SCOPE_IDENTITY();
UPDATE ComprobanteCompra SET saldoPendiente = saldoPendiente - 70000 WHERE idCompra = @compra4;
INSERT INTO CuentaCorrienteProveedor (idProveedor, tipoMovimiento, idCompra, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@provMercedes, 'Pago', NULL, @pago7, 0, 70000,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteProveedor WHERE idProveedor = @provMercedes ORDER BY idMovimiento DESC), 0) - 70000,
    'Pago de compra', NULL);

-- Pago 8: pago completo a Autopartes San Martín (Compra 7)
INSERT INTO Pago (tipo, idCliente, idProveedor, idVenta, idCompra, idUsuario, fecha, medioPago, monto, observaciones)
VALUES ('P', NULL, @provSanMartin, NULL, @compra7, @encargado, DATEADD(DAY, -5, GETDATE()), 'Transferencia', 56000, 'Pago de compra C-000007');
DECLARE @pago8 INT = SCOPE_IDENTITY();
UPDATE ComprobanteCompra SET saldoPendiente = saldoPendiente - 56000 WHERE idCompra = @compra7;
INSERT INTO CuentaCorrienteProveedor (idProveedor, tipoMovimiento, idCompra, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@provSanMartin, 'Pago', NULL, @pago8, 0, 56000,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteProveedor WHERE idProveedor = @provSanMartin ORDER BY idMovimiento DESC), 0) - 56000,
    'Pago de compra', NULL);

-- Pago 9: pago parcial a Distribuidora Bosch Repuestos (Compra 9)
INSERT INTO Pago (tipo, idCliente, idProveedor, idVenta, idCompra, idUsuario, fecha, medioPago, monto, observaciones)
VALUES ('P', NULL, @provBosch, NULL, @compra9, @encargado, DATEADD(DAY, -2, GETDATE()), 'Tarjeta', 30000, 'Pago parcial de compra C-000009');
DECLARE @pago9 INT = SCOPE_IDENTITY();
UPDATE ComprobanteCompra SET saldoPendiente = saldoPendiente - 30000 WHERE idCompra = @compra9;
INSERT INTO CuentaCorrienteProveedor (idProveedor, tipoMovimiento, idCompra, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@provBosch, 'Pago', NULL, @pago9, 0, 30000,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteProveedor WHERE idProveedor = @provBosch ORDER BY idMovimiento DESC), 0) - 30000,
    'Pago de compra', NULL);

-- Pago 10: pago a cuenta general a YPF Lubricantes (sin comprobante puntual)
INSERT INTO Pago (tipo, idCliente, idProveedor, idVenta, idCompra, idUsuario, fecha, medioPago, monto, observaciones)
VALUES ('P', NULL, @provYPF, NULL, NULL, @encargado, DATEADD(DAY, -4, GETDATE()), 'Efectivo', 10000, 'Pago a cuenta');
DECLARE @pago10 INT = SCOPE_IDENTITY();
INSERT INTO CuentaCorrienteProveedor (idProveedor, tipoMovimiento, idCompra, idPago, debe, haber, saldo, descripcion, idUsuario)
VALUES (@provYPF, 'Pago', NULL, @pago10, 0, 10000,
    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteProveedor WHERE idProveedor = @provYPF ORDER BY idMovimiento DESC), 0) - 10000,
    'Pago a cuenta', NULL);

GO

PRINT 'Datos de ejemplo cargados: 10 Clientes, 10 Vehículos, 10 Proveedores, 10 Servicios, 10 Insumos, 10 Turnos, 10 Órdenes de trabajo (4 Cerrada con venta generada, 3 En proceso, 2 Abierta, 1 Cancelada), 10 Compras (6 Contado, 4 Cuenta corriente), 10 Pagos.';

SELECT 'Clientes' AS tabla, COUNT(*) AS filas FROM Cliente
UNION ALL SELECT 'Vehiculos', COUNT(*) FROM Vehiculo
UNION ALL SELECT 'Proveedores', COUNT(*) FROM Proveedor
UNION ALL SELECT 'Servicios', COUNT(*) FROM Servicio
UNION ALL SELECT 'Insumos', COUNT(*) FROM Insumo
UNION ALL SELECT 'Turnos', COUNT(*) FROM Turno
UNION ALL SELECT 'OrdenesDeTrabajo', COUNT(*) FROM OrdenDeTrabajo
UNION ALL SELECT 'ComprobanteCompra', COUNT(*) FROM ComprobanteCompra
UNION ALL SELECT 'ComprobanteVenta', COUNT(*) FROM ComprobanteVenta
UNION ALL SELECT 'Pago', COUNT(*) FROM Pago
UNION ALL SELECT 'MovimientoStock', COUNT(*) FROM MovimientoStock
UNION ALL SELECT 'CuentaCorrienteCliente', COUNT(*) FROM CuentaCorrienteCliente
UNION ALL SELECT 'CuentaCorrienteProveedor', COUNT(*) FROM CuentaCorrienteProveedor;

/* Invariante de kardex: el stock actual de cada insumo tiene que coincidir
   exactamente con la suma de sus movimientos (entrada - salida). No debería
   devolver ninguna fila. */
SELECT i.idInsumo, i.nombre, i.stockActual, SUM(m.entrada) - SUM(m.salida) AS calculado
FROM Insumo i
JOIN MovimientoStock m ON m.idInsumo = i.idInsumo
GROUP BY i.idInsumo, i.nombre, i.stockActual
HAVING i.stockActual <> SUM(m.entrada) - SUM(m.salida);

GO
