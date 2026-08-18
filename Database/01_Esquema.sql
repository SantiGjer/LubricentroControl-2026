/* ============================================================================
   LubricentroControl 2026 — Esquema de base de datos
   Las 21 entidades del diagrama E/R (16 de negocio + 5 de seguridad).

   Idempotente: se puede correr varias veces. Borra y recrea todas las tablas,
   por lo que PIERDE LOS DATOS. Correr 02_DatosIniciales.sql a continuación.

   Uso:
     sqlcmd -S "(localdb)\MSSQLLocalDB" -i Database\01_Esquema.sql
   ============================================================================ */

IF DB_ID('LubricentroControl') IS NULL
    CREATE DATABASE LubricentroControl;
GO

USE LubricentroControl;
GO

/* --- Borrado en orden inverso a las dependencias ------------------------- */
DROP TABLE IF EXISTS CuentaCorrienteProveedor;
DROP TABLE IF EXISTS CuentaCorrienteCliente;
DROP TABLE IF EXISTS Pago;
DROP TABLE IF EXISTS DetalleComprobanteVenta;
DROP TABLE IF EXISTS ComprobanteVenta;
DROP TABLE IF EXISTS DetalleCompra;
DROP TABLE IF EXISTS ComprobanteCompra;
DROP TABLE IF EXISTS DetalleOrdenInsumo;
DROP TABLE IF EXISTS DetalleOrdenServicio;
DROP TABLE IF EXISTS OrdenDeTrabajo;
DROP TABLE IF EXISTS Turno;
DROP TABLE IF EXISTS Vehiculo;
DROP TABLE IF EXISTS Cliente;
DROP TABLE IF EXISTS Insumo;
DROP TABLE IF EXISTS Servicio;
DROP TABLE IF EXISTS Proveedor;
DROP TABLE IF EXISTS RecuperacionClave;
DROP TABLE IF EXISTS MenuNivel;
DROP TABLE IF EXISTS Menu;
DROP TABLE IF EXISTS Url;
DROP TABLE IF EXISTS Usuario;
DROP TABLE IF EXISTS Nivel;
GO

/* ==========================================================================
   SEGURIDAD / LOGIN / MENÚ
   ========================================================================== */

CREATE TABLE Nivel (
    idNivel     INT IDENTITY(1,1) NOT NULL,
    nombre      NVARCHAR(50)      NOT NULL,
    /* Menor jerarquía = más permisos. Admin=1 > Encargado=2 > Empleado=3 */
    jerarquia   INT               NOT NULL,
    CONSTRAINT PK_Nivel PRIMARY KEY (idNivel),
    CONSTRAINT UQ_Nivel_nombre UNIQUE (nombre)
);
GO

CREATE TABLE Usuario (
    idUsuario     INT IDENTITY(1,1) NOT NULL,
    nombre        NVARCHAR(50)      NOT NULL,
    apellido      NVARCHAR(50)      NOT NULL,
    email         NVARCHAR(150)     NOT NULL,
    passwordHash  NVARCHAR(200)     NOT NULL,
    passwordSalt  NVARCHAR(100)     NOT NULL,
    idNivel       INT               NOT NULL,
    activo        BIT               NOT NULL CONSTRAINT DF_Usuario_activo DEFAULT (1),
    fechaAlta     DATETIME          NOT NULL CONSTRAINT DF_Usuario_fechaAlta DEFAULT (GETDATE()),
    CONSTRAINT PK_Usuario PRIMARY KEY (idUsuario),
    CONSTRAINT UQ_Usuario_email UNIQUE (email),
    CONSTRAINT FK_Usuario_Nivel FOREIGN KEY (idNivel) REFERENCES Nivel(idNivel)
);
GO

CREATE TABLE Url (
    idUrl       INT IDENTITY(1,1) NOT NULL,
    descripcion NVARCHAR(100)     NOT NULL,
    /* Ruta relativa sin extensión — FriendlyUrls está activo. Ej: ~/Clientes */
    path        NVARCHAR(200)     NOT NULL,
    CONSTRAINT PK_Url PRIMARY KEY (idUrl),
    CONSTRAINT UQ_Url_path UNIQUE (path)
);
GO

CREATE TABLE Menu (
    idMenu      INT IDENTITY(1,1) NOT NULL,
    texto       NVARCHAR(100)     NOT NULL,
    /* NULL = es un grupo desplegable, no un link */
    idUrl       INT               NULL,
    idMenuPadre INT               NULL,
    orden       INT               NOT NULL,
    activo      BIT               NOT NULL CONSTRAINT DF_Menu_activo DEFAULT (1),
    CONSTRAINT PK_Menu PRIMARY KEY (idMenu),
    CONSTRAINT FK_Menu_Url FOREIGN KEY (idUrl) REFERENCES Url(idUrl),
    CONSTRAINT FK_Menu_MenuPadre FOREIGN KEY (idMenuPadre) REFERENCES Menu(idMenu)
);
GO

/* Qué opción de menú ve cada rol. soloLectura marca los casos "👁️ consulta"
   de la matriz de permisos (§5 de los requerimientos). */
CREATE TABLE MenuNivel (
    idMenu      INT NOT NULL,
    idNivel     INT NOT NULL,
    soloLectura BIT NOT NULL CONSTRAINT DF_MenuNivel_soloLectura DEFAULT (0),
    CONSTRAINT PK_MenuNivel PRIMARY KEY (idMenu, idNivel),
    CONSTRAINT FK_MenuNivel_Menu FOREIGN KEY (idMenu) REFERENCES Menu(idMenu),
    CONSTRAINT FK_MenuNivel_Nivel FOREIGN KEY (idNivel) REFERENCES Nivel(idNivel)
);
GO

CREATE TABLE RecuperacionClave (
    idRecuperacion   INT IDENTITY(1,1) NOT NULL,
    idUsuario        INT               NOT NULL,
    token            NVARCHAR(100)     NOT NULL,
    fechaSolicitud   DATETIME          NOT NULL CONSTRAINT DF_Recup_fechaSolicitud DEFAULT (GETDATE()),
    fechaVencimiento DATETIME          NOT NULL,
    usado            BIT               NOT NULL CONSTRAINT DF_Recup_usado DEFAULT (0),
    fechaUso         DATETIME          NULL,
    CONSTRAINT PK_RecuperacionClave PRIMARY KEY (idRecuperacion),
    CONSTRAINT UQ_RecuperacionClave_token UNIQUE (token),
    CONSTRAINT FK_RecuperacionClave_Usuario FOREIGN KEY (idUsuario) REFERENCES Usuario(idUsuario)
);
GO

/* ==========================================================================
   MAESTROS DE NEGOCIO
   ========================================================================== */

CREATE TABLE Cliente (
    idCliente INT IDENTITY(1,1) NOT NULL,
    nombre    NVARCHAR(50)      NOT NULL,
    apellido  NVARCHAR(50)      NOT NULL,
    dni       NVARCHAR(15)      NOT NULL,
    telefono  NVARCHAR(30)      NULL,
    email     NVARCHAR(150)     NULL,
    direccion NVARCHAR(200)     NULL,
    activo    BIT               NOT NULL CONSTRAINT DF_Cliente_activo DEFAULT (1),
    fechaAlta DATETIME          NOT NULL CONSTRAINT DF_Cliente_fechaAlta DEFAULT (GETDATE()),
    CONSTRAINT PK_Cliente PRIMARY KEY (idCliente),
    CONSTRAINT UQ_Cliente_dni UNIQUE (dni)
);
GO

CREATE TABLE Vehiculo (
    idVehiculo      INT IDENTITY(1,1) NOT NULL,
    idCliente       INT               NOT NULL,
    patente         NVARCHAR(15)      NOT NULL,
    marca           NVARCHAR(50)      NULL,
    modelo          NVARCHAR(50)      NULL,
    anio            INT               NULL,
    tipoCombustible NVARCHAR(30)      NULL,
    activo          BIT               NOT NULL CONSTRAINT DF_Vehiculo_activo DEFAULT (1),
    CONSTRAINT PK_Vehiculo PRIMARY KEY (idVehiculo),
    CONSTRAINT UQ_Vehiculo_patente UNIQUE (patente),
    CONSTRAINT FK_Vehiculo_Cliente FOREIGN KEY (idCliente) REFERENCES Cliente(idCliente)
);
GO

CREATE TABLE Proveedor (
    idProveedor INT IDENTITY(1,1) NOT NULL,
    razonSocial NVARCHAR(150)     NOT NULL,
    cuit        NVARCHAR(20)      NOT NULL,
    telefono    NVARCHAR(30)      NULL,
    email       NVARCHAR(150)     NULL,
    direccion   NVARCHAR(200)     NULL,
    activo      BIT               NOT NULL CONSTRAINT DF_Proveedor_activo DEFAULT (1),
    CONSTRAINT PK_Proveedor PRIMARY KEY (idProveedor),
    CONSTRAINT UQ_Proveedor_cuit UNIQUE (cuit)
);
GO

CREATE TABLE Servicio (
    idServicio  INT IDENTITY(1,1) NOT NULL,
    nombre      NVARCHAR(100)     NOT NULL,
    descripcion NVARCHAR(300)     NULL,
    precioBase  DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Servicio_precioBase DEFAULT (0),
    activo      BIT               NOT NULL CONSTRAINT DF_Servicio_activo DEFAULT (1),
    CONSTRAINT PK_Servicio PRIMARY KEY (idServicio),
    CONSTRAINT CK_Servicio_precioBase CHECK (precioBase >= 0)
);
GO

CREATE TABLE Insumo (
    idInsumo      INT IDENTITY(1,1) NOT NULL,
    nombre        NVARCHAR(100)     NOT NULL,
    marca         NVARCHAR(50)      NULL,
    unidadMedida  NVARCHAR(20)      NULL,
    stockActual   DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Insumo_stockActual DEFAULT (0),
    stockMinimo   DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Insumo_stockMinimo DEFAULT (0),
    precioVenta   DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Insumo_precioVenta DEFAULT (0),
    activo        BIT               NOT NULL CONSTRAINT DF_Insumo_activo DEFAULT (1),
    CONSTRAINT PK_Insumo PRIMARY KEY (idInsumo),
    CONSTRAINT CK_Insumo_precioVenta CHECK (precioVenta >= 0)
);
GO

/* ==========================================================================
   OPERACIÓN: TURNOS Y ÓRDENES DE TRABAJO
   ========================================================================== */

CREATE TABLE Turno (
    idTurno           INT IDENTITY(1,1) NOT NULL,
    idCliente         INT               NOT NULL,
    idVehiculo        INT               NULL,
    fechaSolicitud    DATETIME          NOT NULL CONSTRAINT DF_Turno_fechaSolicitud DEFAULT (GETDATE()),
    fechaHoraAsignada DATETIME          NOT NULL,
    /* Solicitado | Confirmado | Completado | Cancelado */
    estado            NVARCHAR(20)      NOT NULL CONSTRAINT DF_Turno_estado DEFAULT ('Solicitado'),
    observaciones     NVARCHAR(500)     NULL,
    CONSTRAINT PK_Turno PRIMARY KEY (idTurno),
    CONSTRAINT FK_Turno_Cliente FOREIGN KEY (idCliente) REFERENCES Cliente(idCliente),
    CONSTRAINT FK_Turno_Vehiculo FOREIGN KEY (idVehiculo) REFERENCES Vehiculo(idVehiculo),
    CONSTRAINT CK_Turno_estado CHECK (estado IN ('Solicitado','Confirmado','Completado','Cancelado'))
);
GO

CREATE TABLE OrdenDeTrabajo (
    idOrden       INT IDENTITY(1,1) NOT NULL,
    /* Opcional a propósito: una orden puede ser walk-in, sin turno previo */
    idTurno       INT               NULL,
    idCliente     INT               NOT NULL,
    idVehiculo    INT               NOT NULL,
    idUsuario     INT               NOT NULL,
    fecha         DATETIME          NOT NULL CONSTRAINT DF_Orden_fecha DEFAULT (GETDATE()),
    kilometraje   INT               NULL,
    observaciones NVARCHAR(500)     NULL,
    /* Abierta | En proceso | Cerrada | Cancelada */
    estado        NVARCHAR(20)      NOT NULL CONSTRAINT DF_Orden_estado DEFAULT ('Abierta'),
    CONSTRAINT PK_OrdenDeTrabajo PRIMARY KEY (idOrden),
    CONSTRAINT FK_Orden_Turno FOREIGN KEY (idTurno) REFERENCES Turno(idTurno),
    CONSTRAINT FK_Orden_Cliente FOREIGN KEY (idCliente) REFERENCES Cliente(idCliente),
    CONSTRAINT FK_Orden_Vehiculo FOREIGN KEY (idVehiculo) REFERENCES Vehiculo(idVehiculo),
    CONSTRAINT FK_Orden_Usuario FOREIGN KEY (idUsuario) REFERENCES Usuario(idUsuario),
    CONSTRAINT CK_Orden_estado CHECK (estado IN ('Abierta','En proceso','Cerrada','Cancelada'))
);
GO

CREATE TABLE DetalleOrdenServicio (
    idDetalle      INT IDENTITY(1,1) NOT NULL,
    idOrden        INT               NOT NULL,
    idServicio     INT               NOT NULL,
    cantidad       DECIMAL(12,2)     NOT NULL CONSTRAINT DF_DetOrdServ_cantidad DEFAULT (1),
    precioAplicado DECIMAL(12,2)     NOT NULL,
    CONSTRAINT PK_DetalleOrdenServicio PRIMARY KEY (idDetalle),
    CONSTRAINT FK_DetOrdServ_Orden FOREIGN KEY (idOrden) REFERENCES OrdenDeTrabajo(idOrden),
    CONSTRAINT FK_DetOrdServ_Servicio FOREIGN KEY (idServicio) REFERENCES Servicio(idServicio),
    CONSTRAINT CK_DetOrdServ_cantidad CHECK (cantidad > 0)
);
GO

CREATE TABLE DetalleOrdenInsumo (
    idDetalle      INT IDENTITY(1,1) NOT NULL,
    idOrden        INT               NOT NULL,
    idInsumo       INT               NOT NULL,
    cantidad       DECIMAL(12,2)     NOT NULL,
    precioUnitario DECIMAL(12,2)     NOT NULL,
    CONSTRAINT PK_DetalleOrdenInsumo PRIMARY KEY (idDetalle),
    CONSTRAINT FK_DetOrdIns_Orden FOREIGN KEY (idOrden) REFERENCES OrdenDeTrabajo(idOrden),
    CONSTRAINT FK_DetOrdIns_Insumo FOREIGN KEY (idInsumo) REFERENCES Insumo(idInsumo),
    CONSTRAINT CK_DetOrdIns_cantidad CHECK (cantidad > 0)
);
GO

/* ==========================================================================
   CIRCUITO DE DINERO: COMPRAS, VENTAS, PAGOS, CUENTAS CORRIENTES
   ========================================================================== */

CREATE TABLE ComprobanteCompra (
    idCompra          INT IDENTITY(1,1) NOT NULL,
    idProveedor       INT               NOT NULL,
    numeroComprobante NVARCHAR(50)      NOT NULL,
    fecha             DATETIME          NOT NULL CONSTRAINT DF_Compra_fecha DEFAULT (GETDATE()),
    /* Contado | Cuenta corriente */
    condicionPago     NVARCHAR(30)      NOT NULL CONSTRAINT DF_Compra_condicionPago DEFAULT ('Contado'),
    subtotal          DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Compra_subtotal DEFAULT (0),
    impuestos         DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Compra_impuestos DEFAULT (0),
    total             DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Compra_total DEFAULT (0),
    saldoPendiente    DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Compra_saldo DEFAULT (0),
    CONSTRAINT PK_ComprobanteCompra PRIMARY KEY (idCompra),
    CONSTRAINT FK_Compra_Proveedor FOREIGN KEY (idProveedor) REFERENCES Proveedor(idProveedor)
);
GO

CREATE TABLE DetalleCompra (
    idDetalle      INT IDENTITY(1,1) NOT NULL,
    idCompra       INT               NOT NULL,
    idInsumo       INT               NOT NULL,
    cantidad       DECIMAL(12,2)     NOT NULL,
    precioUnitario DECIMAL(12,2)     NOT NULL,
    CONSTRAINT PK_DetalleCompra PRIMARY KEY (idDetalle),
    CONSTRAINT FK_DetCompra_Compra FOREIGN KEY (idCompra) REFERENCES ComprobanteCompra(idCompra),
    CONSTRAINT FK_DetCompra_Insumo FOREIGN KEY (idInsumo) REFERENCES Insumo(idInsumo),
    CONSTRAINT CK_DetCompra_cantidad CHECK (cantidad > 0)
);
GO

/* Nace automáticamente al cerrar una orden de trabajo — no se carga a mano.
   Comprobante interno, sin validez fiscal. */
CREATE TABLE ComprobanteVenta (
    idVenta           INT IDENTITY(1,1) NOT NULL,
    idOrden           INT               NOT NULL,
    idCliente         INT               NOT NULL,
    numeroComprobante NVARCHAR(50)      NOT NULL,
    fecha             DATETIME          NOT NULL CONSTRAINT DF_Venta_fecha DEFAULT (GETDATE()),
    subtotal          DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Venta_subtotal DEFAULT (0),
    impuestos         DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Venta_impuestos DEFAULT (0),
    total             DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Venta_total DEFAULT (0),
    saldoPendiente    DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Venta_saldo DEFAULT (0),
    CONSTRAINT PK_ComprobanteVenta PRIMARY KEY (idVenta),
    CONSTRAINT UQ_ComprobanteVenta_numero UNIQUE (numeroComprobante),
    CONSTRAINT FK_Venta_Orden FOREIGN KEY (idOrden) REFERENCES OrdenDeTrabajo(idOrden),
    CONSTRAINT FK_Venta_Cliente FOREIGN KEY (idCliente) REFERENCES Cliente(idCliente)
);
GO

CREATE TABLE DetalleComprobanteVenta (
    idDetalle      INT IDENTITY(1,1) NOT NULL,
    idVenta        INT               NOT NULL,
    /* S = servicio, I = insumo */
    tipoItem       CHAR(1)           NOT NULL,
    idServicio     INT               NULL,
    idInsumo       INT               NULL,
    descripcion    NVARCHAR(200)     NOT NULL,
    cantidad       DECIMAL(12,2)     NOT NULL,
    precioUnitario DECIMAL(12,2)     NOT NULL,
    subtotal       DECIMAL(12,2)     NOT NULL,
    CONSTRAINT PK_DetalleComprobanteVenta PRIMARY KEY (idDetalle),
    CONSTRAINT FK_DetVenta_Venta FOREIGN KEY (idVenta) REFERENCES ComprobanteVenta(idVenta),
    CONSTRAINT FK_DetVenta_Servicio FOREIGN KEY (idServicio) REFERENCES Servicio(idServicio),
    CONSTRAINT FK_DetVenta_Insumo FOREIGN KEY (idInsumo) REFERENCES Insumo(idInsumo),
    CONSTRAINT CK_DetVenta_tipoItem CHECK (tipoItem IN ('S','I'))
);
GO

/* Un pago es de cliente (tipo C) o de proveedor (tipo P). Puede imputarse a un
   comprobante puntual o quedar a cuenta (los ids de comprobante en NULL). */
CREATE TABLE Pago (
    idPago        INT IDENTITY(1,1) NOT NULL,
    tipo          CHAR(1)           NOT NULL,
    idCliente     INT               NULL,
    idProveedor   INT               NULL,
    idVenta       INT               NULL,
    idCompra      INT               NULL,
    idUsuario     INT               NOT NULL,
    fecha         DATETIME          NOT NULL CONSTRAINT DF_Pago_fecha DEFAULT (GETDATE()),
    /* Efectivo | Transferencia | Tarjeta */
    medioPago     NVARCHAR(30)      NOT NULL,
    monto         DECIMAL(12,2)     NOT NULL,
    observaciones NVARCHAR(300)     NULL,
    CONSTRAINT PK_Pago PRIMARY KEY (idPago),
    CONSTRAINT FK_Pago_Cliente FOREIGN KEY (idCliente) REFERENCES Cliente(idCliente),
    CONSTRAINT FK_Pago_Proveedor FOREIGN KEY (idProveedor) REFERENCES Proveedor(idProveedor),
    CONSTRAINT FK_Pago_Venta FOREIGN KEY (idVenta) REFERENCES ComprobanteVenta(idVenta),
    CONSTRAINT FK_Pago_Compra FOREIGN KEY (idCompra) REFERENCES ComprobanteCompra(idCompra),
    CONSTRAINT FK_Pago_Usuario FOREIGN KEY (idUsuario) REFERENCES Usuario(idUsuario),
    CONSTRAINT CK_Pago_tipo CHECK (tipo IN ('C','P')),
    CONSTRAINT CK_Pago_medioPago CHECK (medioPago IN ('Efectivo','Transferencia','Tarjeta')),
    CONSTRAINT CK_Pago_monto CHECK (monto > 0),
    /* Un pago de cliente exige cliente y no proveedor, y viceversa */
    CONSTRAINT CK_Pago_titular CHECK (
        (tipo = 'C' AND idCliente IS NOT NULL AND idProveedor IS NULL) OR
        (tipo = 'P' AND idProveedor IS NOT NULL AND idCliente IS NULL))
);
GO

CREATE TABLE CuentaCorrienteCliente (
    idMovimiento   INT IDENTITY(1,1) NOT NULL,
    idCliente      INT               NOT NULL,
    fecha          DATETIME          NOT NULL CONSTRAINT DF_CCCli_fecha DEFAULT (GETDATE()),
    /* Venta | Pago | Ajuste */
    tipoMovimiento NVARCHAR(30)      NOT NULL,
    idVenta        INT               NULL,
    idPago         INT               NULL,
    debe           DECIMAL(12,2)     NOT NULL CONSTRAINT DF_CCCli_debe DEFAULT (0),
    haber          DECIMAL(12,2)     NOT NULL CONSTRAINT DF_CCCli_haber DEFAULT (0),
    saldo          DECIMAL(12,2)     NOT NULL,
    descripcion    NVARCHAR(300)     NULL,
    CONSTRAINT PK_CuentaCorrienteCliente PRIMARY KEY (idMovimiento),
    CONSTRAINT FK_CCCli_Cliente FOREIGN KEY (idCliente) REFERENCES Cliente(idCliente),
    CONSTRAINT FK_CCCli_Venta FOREIGN KEY (idVenta) REFERENCES ComprobanteVenta(idVenta),
    CONSTRAINT FK_CCCli_Pago FOREIGN KEY (idPago) REFERENCES Pago(idPago)
);
GO

CREATE TABLE CuentaCorrienteProveedor (
    idMovimiento   INT IDENTITY(1,1) NOT NULL,
    idProveedor    INT               NOT NULL,
    fecha          DATETIME          NOT NULL CONSTRAINT DF_CCProv_fecha DEFAULT (GETDATE()),
    /* Compra | Pago | Ajuste */
    tipoMovimiento NVARCHAR(30)      NOT NULL,
    idCompra       INT               NULL,
    idPago         INT               NULL,
    debe           DECIMAL(12,2)     NOT NULL CONSTRAINT DF_CCProv_debe DEFAULT (0),
    haber          DECIMAL(12,2)     NOT NULL CONSTRAINT DF_CCProv_haber DEFAULT (0),
    saldo          DECIMAL(12,2)     NOT NULL,
    descripcion    NVARCHAR(300)     NULL,
    CONSTRAINT PK_CuentaCorrienteProveedor PRIMARY KEY (idMovimiento),
    CONSTRAINT FK_CCProv_Proveedor FOREIGN KEY (idProveedor) REFERENCES Proveedor(idProveedor),
    CONSTRAINT FK_CCProv_Compra FOREIGN KEY (idCompra) REFERENCES ComprobanteCompra(idCompra),
    CONSTRAINT FK_CCProv_Pago FOREIGN KEY (idPago) REFERENCES Pago(idPago)
);
GO

/* --- Índices de apoyo a las búsquedas más frecuentes --------------------- */
CREATE INDEX IX_Vehiculo_idCliente        ON Vehiculo(idCliente);
CREATE INDEX IX_Turno_fechaHoraAsignada   ON Turno(fechaHoraAsignada);
CREATE INDEX IX_Orden_idCliente           ON OrdenDeTrabajo(idCliente);
CREATE INDEX IX_Orden_fecha               ON OrdenDeTrabajo(fecha);
CREATE INDEX IX_Venta_fecha               ON ComprobanteVenta(fecha);
CREATE INDEX IX_CCCli_idCliente           ON CuentaCorrienteCliente(idCliente);
CREATE INDEX IX_CCProv_idProveedor        ON CuentaCorrienteProveedor(idProveedor);
CREATE INDEX IX_Menu_idMenuPadre          ON Menu(idMenuPadre);
GO

PRINT 'Esquema creado correctamente.';
GO
