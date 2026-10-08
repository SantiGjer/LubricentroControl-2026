/* ============================================================================
   LubricentroControl 2026 — Esquema de base de datos
   Las 21 entidades del diagrama E/R (16 de negocio + 5 de seguridad) más cinco
   agregadas después: MovimientoStock (kardex de stock, Fase 2 — ver
   Docs/Lubricentro_Requerimientos.md §8 y §9.3), Producto (supertipo de
   Servicio e Insumo, §9.9), Factura y Emisor (datos del comercio que factura,
   §9.10) e ImagenProducto (la imagen de cada producto, §9.13).

   Idempotente: se puede correr varias veces. Borra y recrea todas las tablas,
   por lo que PIERDE LOS DATOS. Correr 02_DatosIniciales.sql a continuación.

   Uso:
     sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i Database\01_Esquema.sql
   ============================================================================ */

/* Los índices filtrados de Producto (SKU y código de barras únicos cuando
   vienen cargados) exigen QUOTED_IDENTIFIER ON, y sqlcmd lo trae apagado. */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF DB_ID('LubricentroControl') IS NULL
    CREATE DATABASE LubricentroControl;
GO

USE LubricentroControl;
GO

/* --- Borrado en orden inverso a las dependencias ------------------------- */
DROP TABLE IF EXISTS MovimientoStock;
DROP TABLE IF EXISTS CuentaCorrienteProveedor;
DROP TABLE IF EXISTS CuentaCorrienteCliente;
DROP TABLE IF EXISTS Pago;
DROP TABLE IF EXISTS Factura;
DROP TABLE IF EXISTS Emisor;
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
DROP TABLE IF EXISTS ImagenProducto;
DROP TABLE IF EXISTS Insumo;
DROP TABLE IF EXISTS Servicio;
DROP TABLE IF EXISTS Producto;
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

/* Rol de usuario. Admin (1) y Lectura (4) son fijos: Admin tiene siempre acceso
   completo y Lectura es el rol de las cuentas creadas desde ~/Registro. El resto
   se crea, renombra y borra desde la pantalla de Roles (§9.11). */
CREATE TABLE Nivel (
    idNivel     INT IDENTITY(1,1) NOT NULL,
    nombre      NVARCHAR(50)      NOT NULL,
    /* Orden en que se listan los roles: menor = más permisos. */
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
    /* Ícono de la barra lateral (nombre que entiende Site.Master.cs). Solo lo usan
       las opciones de primer nivel; NULL = ícono genérico. */
    icono       NVARCHAR(30)      NULL,
    activo      BIT               NOT NULL CONSTRAINT DF_Menu_activo DEFAULT (1),
    CONSTRAINT PK_Menu PRIMARY KEY (idMenu),
    CONSTRAINT FK_Menu_Url FOREIGN KEY (idUrl) REFERENCES Url(idUrl),
    CONSTRAINT FK_Menu_MenuPadre FOREIGN KEY (idMenuPadre) REFERENCES Menu(idMenu)
);
GO

/* Qué opción de menú ve cada rol. soloLectura marca los casos "👁️ consulta"
   de la matriz de permisos (§5 de los requerimientos). Sin fila = sin acceso.
   Se edita desde la pantalla de Roles. */
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

/* Datos fiscales agregados el 2026-10-07 (§9.8): tipo de cliente, documento con
   su tipo, condición frente al IVA y domicilio completo. */
CREATE TABLE Cliente (
    idCliente       INT IDENTITY(1,1) NOT NULL,
    /* Persona física (se nombra por nombre y apellido) | Empresa (por razón social) */
    tipoCliente     NVARCHAR(20)      NOT NULL CONSTRAINT DF_Cliente_tipoCliente DEFAULT (N'Persona física'),
    nombre          NVARCHAR(50)      NULL,
    apellido        NVARCHAR(50)      NULL,
    razonSocial     NVARCHAR(150)     NULL,
    /* DNI | CUIT | CUIL | LE | LC | Pasaporte. Los numéricos se guardan sin puntos ni guiones. */
    tipoDocumento   NVARCHAR(10)      NOT NULL CONSTRAINT DF_Cliente_tipoDocumento DEFAULT ('DNI'),
    numeroDocumento NVARCHAR(20)      NOT NULL,
    /* Consumidor Final | Responsable Inscripto | Monotributista | Exento — define la letra de la factura */
    condicionIva    NVARCHAR(30)      NOT NULL CONSTRAINT DF_Cliente_condicionIva DEFAULT ('Consumidor Final'),
    telefono        NVARCHAR(30)      NULL,
    email           NVARCHAR(150)     NULL,
    direccion       NVARCHAR(200)     NULL,
    localidad       NVARCHAR(100)     NULL,
    provincia       NVARCHAR(60)      NULL,
    codigoPostal    NVARCHAR(10)      NULL,
    /* 1 = puede quedar debiendo (fiado). 0 = paga al cerrar la orden: la pantalla de Órdenes
       lo lleva directo a Pagos. Agregada el 2026-10-07, no estaba en el diagrama original. */
    cuentaCorriente BIT               NOT NULL CONSTRAINT DF_Cliente_cuentaCorriente DEFAULT (0),
    activo          BIT               NOT NULL CONSTRAINT DF_Cliente_activo DEFAULT (1),
    fechaAlta       DATETIME          NOT NULL CONSTRAINT DF_Cliente_fechaAlta DEFAULT (GETDATE()),
    /* Cómo se lo nombra en todas las pantallas: la razón social de una empresa, o
       "Nombre Apellido" de una persona. Calculada: los DAL la leen como nombreCliente. */
    denominacion    AS (CASE WHEN tipoCliente = N'Empresa' THEN razonSocial ELSE nombre + N' ' + apellido END),
    CONSTRAINT PK_Cliente PRIMARY KEY (idCliente),
    CONSTRAINT UQ_Cliente_documento UNIQUE (tipoDocumento, numeroDocumento),
    CONSTRAINT CK_Cliente_tipoCliente CHECK (tipoCliente IN (N'Persona física', N'Empresa')),
    CONSTRAINT CK_Cliente_nombre CHECK (
        (tipoCliente = N'Persona física' AND nombre IS NOT NULL AND apellido IS NOT NULL) OR
        (tipoCliente = N'Empresa' AND razonSocial IS NOT NULL)),
    CONSTRAINT CK_Cliente_tipoDocumento CHECK (tipoDocumento IN ('DNI','CUIT','CUIL','LE','LC','Pasaporte')),
    CONSTRAINT CK_Cliente_condicionIva CHECK (
        condicionIva IN ('Consumidor Final','Responsable Inscripto','Monotributista','Exento')),
    /* Una empresa, y cualquiera que no sea consumidor final, se identifica con CUIT. */
    CONSTRAINT CK_Cliente_cuit CHECK (
        tipoDocumento = 'CUIT' OR (tipoCliente = N'Persona física' AND condicionIva = 'Consumidor Final'))
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

/* Supertipo de Servicio e Insumo (§9.9): lo común a todo lo que se vende. Cada
   producto es exactamente una de las dos subcategorías — la fila hija vive en
   Servicio o en Insumo con el mismo id, y la FK compuesta (id, tipo) de cada
   subtipo impide colgar un servicio de un producto de tipo Insumo o al revés. */
CREATE TABLE Producto (
    idProducto   INT IDENTITY(1,1) NOT NULL,
    /* Servicio | Insumo — fijo desde el alta */
    tipo         NVARCHAR(20)      NOT NULL,
    nombre       NVARCHAR(100)     NOT NULL,
    descripcion  NVARCHAR(300)     NULL,
    sku          NVARCHAR(50)      NULL,
    codigoBarras NVARCHAR(50)      NULL,
    /* Precio final al público, con el IVA incluido. */
    precio       DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Producto_precio DEFAULT (0),
    /* Gravado | Exento | No gravado. Solo un producto gravado lleva alícuota. */
    tipoIva      NVARCHAR(20)      NOT NULL CONSTRAINT DF_Producto_tipoIva DEFAULT ('Gravado'),
    alicuotaIva  DECIMAL(5,2)      NOT NULL CONSTRAINT DF_Producto_alicuotaIva DEFAULT (21),
    activo       BIT               NOT NULL CONSTRAINT DF_Producto_activo DEFAULT (1),
    CONSTRAINT PK_Producto PRIMARY KEY (idProducto),
    CONSTRAINT UQ_Producto_idTipo UNIQUE (idProducto, tipo),
    CONSTRAINT CK_Producto_tipo CHECK (tipo IN ('Servicio','Insumo')),
    CONSTRAINT CK_Producto_precio CHECK (precio >= 0),
    CONSTRAINT CK_Producto_iva CHECK (
        (tipoIva = 'Gravado' AND alicuotaIva IN (2.5, 5, 10.5, 21, 27)) OR
        (tipoIva IN ('Exento','No gravado') AND alicuotaIva = 0))
);
GO

/* SKU y código de barras: opcionales, pero no se repiten entre productos. */
CREATE UNIQUE INDEX UX_Producto_sku ON Producto(sku) WHERE sku IS NOT NULL;
CREATE UNIQUE INDEX UX_Producto_codigoBarras ON Producto(codigoBarras) WHERE codigoBarras IS NOT NULL;
GO

/* Subtipo Servicio: sin atributos propios por ahora, pero es el que referencian
   las líneas de servicio de las órdenes y de las ventas. */
CREATE TABLE Servicio (
    idServicio  INT               NOT NULL,
    tipo        NVARCHAR(20)      NOT NULL CONSTRAINT DF_Servicio_tipo DEFAULT ('Servicio'),
    CONSTRAINT PK_Servicio PRIMARY KEY (idServicio),
    CONSTRAINT CK_Servicio_tipo CHECK (tipo = 'Servicio'),
    CONSTRAINT FK_Servicio_Producto FOREIGN KEY (idServicio, tipo) REFERENCES Producto(idProducto, tipo)
);
GO

/* Subtipo Insumo: lo que tiene stock. stockActual solo cambia a través del kardex
   (MovimientoStock). */
CREATE TABLE Insumo (
    idInsumo      INT               NOT NULL,
    tipo          NVARCHAR(20)      NOT NULL CONSTRAINT DF_Insumo_tipo DEFAULT ('Insumo'),
    marca         NVARCHAR(50)      NULL,
    unidadMedida  NVARCHAR(20)      NULL,
    stockActual   DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Insumo_stockActual DEFAULT (0),
    stockMinimo   DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Insumo_stockMinimo DEFAULT (0),
    CONSTRAINT PK_Insumo PRIMARY KEY (idInsumo),
    CONSTRAINT CK_Insumo_tipo CHECK (tipo = 'Insumo'),
    CONSTRAINT FK_Insumo_Producto FOREIGN KEY (idInsumo, tipo) REFERENCES Producto(idProducto, tipo),
    CONSTRAINT CK_Insumo_stockActual CHECK (stockActual >= 0),
    CONSTRAINT CK_Insumo_stockMinimo CHECK (stockMinimo >= 0)
);
GO

/* Imagen de un producto (§9.13, agregada el 2026-10-08): opcional, una por
   producto. En una tabla aparte para que las consultas de productos no
   arrastren los archivos: las pantallas la piden por separado
   (ImagenProducto.ashx). PNG o JPG, del mismo formato que el archivo subido. */
CREATE TABLE ImagenProducto (
    idProducto         INT               NOT NULL,
    /* image/png | image/jpeg */
    tipoContenido      NVARCHAR(20)      NOT NULL,
    /* A lo sumo 800 px de lado: el formulario y "Ver" no la muestran más grande. */
    contenido          VARBINARY(MAX)    NOT NULL,
    /* 120 px de lado, para la lista. */
    miniatura          VARBINARY(MAX)    NOT NULL,
    /* Va en la dirección de la imagen: al cambiarla, el navegador no sigue
       mostrando la anterior. */
    fechaActualizacion DATETIME          NOT NULL CONSTRAINT DF_ImagenProducto_fecha DEFAULT (GETDATE()),
    CONSTRAINT PK_ImagenProducto PRIMARY KEY (idProducto),
    CONSTRAINT FK_ImagenProducto_Producto FOREIGN KEY (idProducto) REFERENCES Producto(idProducto),
    CONSTRAINT CK_ImagenProducto_tipo CHECK (tipoContenido IN ('image/png','image/jpeg'))
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
    /* Solo poblado si condicionPago = Contado (CK_Compra_medioPago_condicion): una compra a
       cuenta corriente no tiene medio de pago porque todavía no se pagó nada. Agregada en
       Fase 4 — no estaba en el diagrama original, ver Requerimientos §9.5. */
    medioPago         NVARCHAR(30)      NULL,
    subtotal          DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Compra_subtotal DEFAULT (0),
    impuestos         DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Compra_impuestos DEFAULT (0),
    total             DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Compra_total DEFAULT (0),
    saldoPendiente    DECIMAL(12,2)     NOT NULL CONSTRAINT DF_Compra_saldo DEFAULT (0),
    CONSTRAINT PK_ComprobanteCompra PRIMARY KEY (idCompra),
    CONSTRAINT FK_Compra_Proveedor FOREIGN KEY (idProveedor) REFERENCES Proveedor(idProveedor),
    CONSTRAINT CK_Compra_medioPago_dominio CHECK (medioPago IN ('Efectivo','Transferencia','Tarjeta') OR medioPago IS NULL),
    CONSTRAINT CK_Compra_medioPago_condicion CHECK (
        (condicionPago = 'Contado' AND medioPago IS NOT NULL) OR
        (condicionPago = 'Cuenta corriente' AND medioPago IS NULL))
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
   Comprobante interno, sin validez fiscal. total = precios finales con IVA;
   subtotal = neto (sin IVA) e impuestos = IVA contenido, sumados de sus líneas. */
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
    /* Precio final (con IVA) y subtotal = cantidad × precio. */
    precioUnitario DECIMAL(12,2)     NOT NULL,
    subtotal       DECIMAL(12,2)     NOT NULL,
    /* IVA del producto al momento de la venta (§9.10): la venta no cambia si después se
       cambia el IVA del producto. importeIva es el IVA contenido en el subtotal. */
    tipoIva        NVARCHAR(20)      NOT NULL,
    alicuotaIva    DECIMAL(5,2)      NOT NULL,
    importeIva     DECIMAL(12,2)     NOT NULL,
    CONSTRAINT PK_DetalleComprobanteVenta PRIMARY KEY (idDetalle),
    CONSTRAINT FK_DetVenta_Venta FOREIGN KEY (idVenta) REFERENCES ComprobanteVenta(idVenta),
    CONSTRAINT FK_DetVenta_Servicio FOREIGN KEY (idServicio) REFERENCES Servicio(idServicio),
    CONSTRAINT FK_DetVenta_Insumo FOREIGN KEY (idInsumo) REFERENCES Insumo(idInsumo),
    CONSTRAINT CK_DetVenta_tipoItem CHECK (tipoItem IN ('S','I')),
    CONSTRAINT CK_DetVenta_iva CHECK (
        (tipoIva = 'Gravado' AND alicuotaIva > 0) OR
        (tipoIva IN ('Exento','No gravado') AND alicuotaIva = 0 AND importeIva = 0))
);
GO

/* Datos del comercio que emite las facturas (§9.10): una sola fila (idEmisor = 1),
   editable desde Administración > Datos del comercio. Cada factura los copia al
   emitirse, así que cambiarlos no toca las ya emitidas. La condición frente al
   IVA define la letra: Responsable Inscripto emite A o B; Monotributista o
   Exento, C. Cada letra numera por separado en cada punto de venta. */
CREATE TABLE Emisor (
    idEmisor          INT               NOT NULL,
    razonSocial       NVARCHAR(150)     NOT NULL,
    /* Sin guiones */
    cuit              NVARCHAR(20)      NOT NULL,
    condicionIva      NVARCHAR(30)      NOT NULL,
    domicilio         NVARCHAR(300)     NULL,
    ingresosBrutos    NVARCHAR(30)      NULL,
    inicioActividades DATE              NULL,
    puntoVenta        INT               NOT NULL,
    CONSTRAINT PK_Emisor PRIMARY KEY (idEmisor),
    CONSTRAINT CK_Emisor_unico CHECK (idEmisor = 1),
    CONSTRAINT CK_Emisor_condicionIva CHECK (condicionIva IN ('Responsable Inscripto','Monotributista','Exento')),
    CONSTRAINT CK_Emisor_puntoVenta CHECK (puntoVenta BETWEEN 1 AND 99999)
);
GO

/* Factura de una venta (§9.10), generada a pedido desde la pantalla de Ventas.
   Comprobante sin validez fiscal (no hay CAE de ARCA): la letra sale de la
   condición frente al IVA del comercio y del cliente, y la numeración es
   correlativa por letra y punto de venta. Guarda los datos del comercio y del
   cliente del momento en que se emitió: la factura no cambia si después se
   editan. Las líneas y los importes son los de la venta, que no se modifica. */
CREATE TABLE Factura (
    idFactura                INT IDENTITY(1,1) NOT NULL,
    idVenta                  INT               NOT NULL,
    /* A | B | C */
    tipo                     CHAR(1)           NOT NULL,
    puntoVenta               INT               NOT NULL,
    numero                   INT               NOT NULL,
    fecha                    DATETIME          NOT NULL CONSTRAINT DF_Factura_fecha DEFAULT (GETDATE()),
    idUsuario                INT               NOT NULL,
    /* Contado | Cuenta corriente */
    condicionVenta           NVARCHAR(30)      NOT NULL,
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
    CONSTRAINT PK_Factura PRIMARY KEY (idFactura),
    CONSTRAINT UQ_Factura_venta UNIQUE (idVenta),
    CONSTRAINT UQ_Factura_numero UNIQUE (tipo, puntoVenta, numero),
    CONSTRAINT FK_Factura_Venta FOREIGN KEY (idVenta) REFERENCES ComprobanteVenta(idVenta),
    CONSTRAINT FK_Factura_Usuario FOREIGN KEY (idUsuario) REFERENCES Usuario(idUsuario),
    CONSTRAINT CK_Factura_tipo CHECK (tipo IN ('A','B','C')),
    CONSTRAINT CK_Factura_puntoVenta CHECK (puntoVenta BETWEEN 1 AND 99999),
    CONSTRAINT CK_Factura_numero CHECK (numero > 0)
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
    /* Nullable a propósito: los movimientos automáticos de Venta/Pago ya son trazables por otro
       lado (Pago.idUsuario, o el usuario que cerró la orden) — solo Ajuste lo puebla. Agregada
       en Fase 4 junto con el ajuste manual, mismo criterio que MovimientoStock.idUsuario. */
    idUsuario      INT               NULL,
    CONSTRAINT PK_CuentaCorrienteCliente PRIMARY KEY (idMovimiento),
    CONSTRAINT FK_CCCli_Cliente FOREIGN KEY (idCliente) REFERENCES Cliente(idCliente),
    CONSTRAINT FK_CCCli_Venta FOREIGN KEY (idVenta) REFERENCES ComprobanteVenta(idVenta),
    CONSTRAINT FK_CCCli_Pago FOREIGN KEY (idPago) REFERENCES Pago(idPago),
    CONSTRAINT FK_CCCli_Usuario FOREIGN KEY (idUsuario) REFERENCES Usuario(idUsuario)
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
    /* Ídem CuentaCorrienteCliente.idUsuario: nullable, solo poblado en los ajustes manuales. */
    idUsuario      INT               NULL,
    CONSTRAINT PK_CuentaCorrienteProveedor PRIMARY KEY (idMovimiento),
    CONSTRAINT FK_CCProv_Proveedor FOREIGN KEY (idProveedor) REFERENCES Proveedor(idProveedor),
    CONSTRAINT FK_CCProv_Compra FOREIGN KEY (idCompra) REFERENCES ComprobanteCompra(idCompra),
    CONSTRAINT FK_CCProv_Pago FOREIGN KEY (idPago) REFERENCES Pago(idPago),
    CONSTRAINT FK_CCProv_Usuario FOREIGN KEY (idUsuario) REFERENCES Usuario(idUsuario)
);
GO

/* ==========================================================================
   CONTROL DE STOCK (KARDEX)
   Depende de Insumo (Maestros), OrdenDeTrabajo (Operación), ComprobanteCompra
   (Circuito de dinero) y Usuario (Seguridad) — por eso va al final del
   archivo, después de las cuatro secciones de las que depende.
   ========================================================================== */

CREATE TABLE MovimientoStock (
    idMovimiento    INT IDENTITY(1,1) NOT NULL,
    idInsumo        INT               NOT NULL,
    fecha           DATETIME          NOT NULL CONSTRAINT DF_MovStock_fecha DEFAULT (GETDATE()),
    /* Compra | Orden | CancelacionOrden | AjusteManual */
    tipoMovimiento  NVARCHAR(30)      NOT NULL,
    idCompra        INT               NULL,
    idOrden         INT               NULL,
    idUsuario       INT               NOT NULL,
    entrada         DECIMAL(12,2)     NOT NULL CONSTRAINT DF_MovStock_entrada DEFAULT (0),
    salida          DECIMAL(12,2)     NOT NULL CONSTRAINT DF_MovStock_salida DEFAULT (0),
    stockResultante DECIMAL(12,2)     NOT NULL,
    descripcion     NVARCHAR(300)     NULL,
    CONSTRAINT PK_MovimientoStock PRIMARY KEY (idMovimiento),
    CONSTRAINT FK_MovStock_Insumo FOREIGN KEY (idInsumo) REFERENCES Insumo(idInsumo),
    CONSTRAINT FK_MovStock_Compra FOREIGN KEY (idCompra) REFERENCES ComprobanteCompra(idCompra),
    CONSTRAINT FK_MovStock_Orden FOREIGN KEY (idOrden) REFERENCES OrdenDeTrabajo(idOrden),
    CONSTRAINT FK_MovStock_Usuario FOREIGN KEY (idUsuario) REFERENCES Usuario(idUsuario),
    CONSTRAINT CK_MovStock_entrada CHECK (entrada >= 0),
    CONSTRAINT CK_MovStock_salida CHECK (salida >= 0),
    CONSTRAINT CK_MovStock_unSentido CHECK (
        (entrada > 0 AND salida = 0) OR (salida > 0 AND entrada = 0)),
    CONSTRAINT CK_MovStock_origen CHECK (
        (tipoMovimiento = 'Compra' AND idCompra IS NOT NULL AND idOrden IS NULL) OR
        (tipoMovimiento IN ('Orden','CancelacionOrden') AND idOrden IS NOT NULL AND idCompra IS NULL) OR
        (tipoMovimiento = 'AjusteManual' AND idCompra IS NULL AND idOrden IS NULL))
);
GO

/* --- Índices de apoyo a las búsquedas más frecuentes --------------------- */
CREATE INDEX IX_Vehiculo_idCliente        ON Vehiculo(idCliente);
CREATE INDEX IX_Turno_fechaHoraAsignada   ON Turno(fechaHoraAsignada);
CREATE INDEX IX_Turno_idVehiculo          ON Turno(idVehiculo);
CREATE INDEX IX_Orden_idCliente           ON OrdenDeTrabajo(idCliente);
CREATE INDEX IX_Orden_idVehiculo          ON OrdenDeTrabajo(idVehiculo);
CREATE INDEX IX_Orden_fecha               ON OrdenDeTrabajo(fecha);
CREATE INDEX IX_Venta_fecha               ON ComprobanteVenta(fecha);
CREATE INDEX IX_CCCli_idCliente           ON CuentaCorrienteCliente(idCliente);
CREATE INDEX IX_CCProv_idProveedor        ON CuentaCorrienteProveedor(idProveedor);
CREATE INDEX IX_Menu_idMenuPadre          ON Menu(idMenuPadre);
CREATE INDEX IX_MovStock_idInsumo         ON MovimientoStock(idInsumo);
CREATE INDEX IX_MovStock_fecha            ON MovimientoStock(fecha);
GO

PRINT 'Esquema creado correctamente.';
GO
