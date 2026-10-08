/* ============================================================================
   LubricentroControl 2026 — Datos iniciales

   Carga los 4 roles, el árbol de menú con sus permisos por rol, el usuario
   administrador inicial y unos datos de ejemplo del comercio para las facturas.
   Correr DESPUÉS de 01_Esquema.sql.

     sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i Database\02_DatosIniciales.sql

   Usuario inicial:  admin@lubricentro.com  /  Admin123!
   >>> Cambiar esa contraseña después del primer login. <<<
   ============================================================================ */

SET QUOTED_IDENTIFIER ON;
GO

USE LubricentroControl;
GO

SET NOCOUNT ON;

/* --- Roles ----------------------------------------------------------------
   Admin (1) y Lectura (4) quedan fijos: el código los busca por id (Nivel.Admin,
   Nivel.Lectura). Encargado y Empleado se pueden editar o borrar desde Roles. */
INSERT INTO Nivel (nombre, jerarquia) VALUES
    ('Admin', 1),
    ('Encargado', 2),
    ('Empleado', 3),
    ('Lectura', 4);

DECLARE @admin INT = (SELECT idNivel FROM Nivel WHERE nombre = 'Admin');
DECLARE @encargado INT = (SELECT idNivel FROM Nivel WHERE nombre = 'Encargado');
DECLARE @empleado INT = (SELECT idNivel FROM Nivel WHERE nombre = 'Empleado');
DECLARE @lectura INT = (SELECT idNivel FROM Nivel WHERE nombre = 'Lectura');

/* --- Usuario administrador inicial --------------------------------------
   Hash PBKDF2-SHA256, 25.000 iteraciones, 32 bytes — mismo algoritmo que
   BIZ\Negocio\PasswordHasher.cs. Contraseña en claro: Admin123!            */
INSERT INTO Usuario (nombre, apellido, email, passwordHash, passwordSalt, idNivel, activo)
VALUES ('Administrador', 'del Sistema', 'admin@lubricentro.com',
        'W8/jv9TYetjWiitFgEi844FGrsFgmALCR+NFlf55u9U=',
        'Vz952IpLxGaW1fdiskBl8Q==',
        @admin, 1);

/* --- Pantallas del sistema ----------------------------------------------
   El path va sin extensión: FriendlyUrls está activo.                      */
INSERT INTO Url (descripcion, path) VALUES
    (N'Inicio',                      '~/Default'),
    (N'Clientes',                    '~/Clientes'),
    (N'Vehículos',                   '~/Vehiculos'),
    (N'Turnos',                      '~/Turnos'),
    (N'Órdenes de trabajo',          '~/OrdenesDeTrabajo'),
    (N'Productos',                   '~/Productos'),
    (N'Proveedores',                 '~/Proveedores'),
    (N'Compras',                     '~/Compras'),
    (N'Ventas',                      '~/Ventas'),
    (N'Pagos',                       '~/Pagos'),
    (N'Cuenta corriente clientes',   '~/CuentaCorrienteClientes'),
    (N'Cuenta corriente proveedores','~/CuentaCorrienteProveedores'),
    (N'Reporte de stock bajo',       '~/Reportes/StockBajo'),
    (N'Reporte de ventas por período','~/Reportes/VentasPorPeriodo'),
    (N'Reporte de cuentas corrientes','~/Reportes/CuentasCorrientes'),
    (N'Usuarios',                    '~/Usuarios'),
    (N'Roles y permisos',            '~/Roles'),
    (N'Datos del comercio',          '~/DatosComercio');

/* --- Árbol de menú -------------------------------------------------------
   idUrl NULL = grupo desplegable. El ícono solo lo usan las opciones de
   primer nivel de la barra lateral (ver Site.Master.cs, Iconos).           */
DECLARE @idMenu INT;

/* Nivel raíz: Inicio (link directo) */
INSERT INTO Menu (texto, idUrl, idMenuPadre, orden, icono)
    VALUES (N'Inicio', (SELECT idUrl FROM Url WHERE path = '~/Default'), NULL, 1, 'inicio');
DECLARE @mInicio INT = SCOPE_IDENTITY();

/* Grupos */
INSERT INTO Menu (texto, idUrl, idMenuPadre, orden, icono) VALUES (N'Clientes', NULL, NULL, 2, 'clientes');
DECLARE @gClientes INT = SCOPE_IDENTITY();
INSERT INTO Menu (texto, idUrl, idMenuPadre, orden, icono) VALUES (N'Operación', NULL, NULL, 3, 'operacion');
DECLARE @gOperacion INT = SCOPE_IDENTITY();
INSERT INTO Menu (texto, idUrl, idMenuPadre, orden, icono) VALUES (N'Compras', NULL, NULL, 4, 'compras');
DECLARE @gCompras INT = SCOPE_IDENTITY();
INSERT INTO Menu (texto, idUrl, idMenuPadre, orden, icono) VALUES (N'Ventas y cobros', NULL, NULL, 5, 'ventas');
DECLARE @gVentas INT = SCOPE_IDENTITY();
INSERT INTO Menu (texto, idUrl, idMenuPadre, orden, icono) VALUES (N'Reportes', NULL, NULL, 6, 'reportes');
DECLARE @gReportes INT = SCOPE_IDENTITY();
INSERT INTO Menu (texto, idUrl, idMenuPadre, orden, icono) VALUES (N'Administración', NULL, NULL, 7, 'administracion');
DECLARE @gAdmin INT = SCOPE_IDENTITY();

/* Hojas */
INSERT INTO Menu (texto, idUrl, idMenuPadre, orden)
SELECT v.texto, u.idUrl, v.padre, v.orden
FROM (VALUES
    (N'Clientes',                     '~/Clientes',                     @gClientes,  1),
    (N'Vehículos',                    '~/Vehiculos',                    @gClientes,  2),
    (N'Turnos',                       '~/Turnos',                       @gOperacion, 1),
    (N'Órdenes de trabajo',           '~/OrdenesDeTrabajo',             @gOperacion, 2),
    (N'Productos',                    '~/Productos',                    @gOperacion, 3),
    (N'Proveedores',                  '~/Proveedores',                  @gCompras,   1),
    (N'Compras',                      '~/Compras',                      @gCompras,   2),
    (N'Ventas',                       '~/Ventas',                       @gVentas,    1),
    (N'Pagos',                        '~/Pagos',                        @gVentas,    2),
    (N'Cta. cte. clientes',           '~/CuentaCorrienteClientes',      @gVentas,    3),
    (N'Cta. cte. proveedores',        '~/CuentaCorrienteProveedores',   @gVentas,    4),
    (N'Stock bajo',                   '~/Reportes/StockBajo',           @gReportes,  1),
    (N'Ventas por período',           '~/Reportes/VentasPorPeriodo',    @gReportes,  2),
    (N'Cuentas corrientes',           '~/Reportes/CuentasCorrientes',   @gReportes,  3),
    (N'Usuarios',                     '~/Usuarios',                     @gAdmin,     1),
    (N'Roles y permisos',             '~/Roles',                        @gAdmin,     2),
    (N'Datos del comercio',           '~/DatosComercio',                @gAdmin,     3)
) AS v(texto, path, padre, orden)
JOIN Url u ON u.path = v.path;

/* --- Permisos de menú por rol -------------------------------------------
   Refleja la matriz de permisos de los requerimientos (§5). Después se edita
   desde la pantalla de Roles (MenuNivel guarda los grupos además de las hojas).
   soloLectura = 1 son los casos "👁️ Solo consulta" del rol Empleado, y
   absolutamente todo para el rol Lectura (ver bloque más abajo).          */

/* Admin ve absolutamente todo, con permiso completo. */
INSERT INTO MenuNivel (idMenu, idNivel, soloLectura)
SELECT idMenu, @admin, 0 FROM Menu;

/* Encargado: todo menos Administración (usuarios, roles y datos del comercio). */
INSERT INTO MenuNivel (idMenu, idNivel, soloLectura)
SELECT idMenu, @encargado, 0
FROM Menu
WHERE idMenu <> @gAdmin AND ISNULL(idMenuPadre, 0) <> @gAdmin;

/* Empleado: sin Administración ni Reportes; consulta en productos, compras y ctas. ctes. */
INSERT INTO MenuNivel (idMenu, idNivel, soloLectura)
SELECT m.idMenu, @empleado,
       CASE WHEN u.path IN ('~/Proveedores', '~/Productos', '~/Compras',
                            '~/CuentaCorrienteClientes', '~/CuentaCorrienteProveedores')
            THEN 1 ELSE 0 END
FROM Menu m
LEFT JOIN Url u ON u.idUrl = m.idUrl
WHERE m.idMenu NOT IN (@gAdmin, @gReportes)
  AND ISNULL(m.idMenuPadre, 0) NOT IN (@gAdmin, @gReportes);

/* Lectura: mismas pantallas que ve Empleado (sin Administración ni Reportes),
   pero solo consulta en absolutamente todas — incluidas las que a Empleado
   todavía le dan alta/edición (Clientes, Vehículos, Turnos, Órdenes, Ventas, Pagos). */
INSERT INTO MenuNivel (idMenu, idNivel, soloLectura)
SELECT m.idMenu, @lectura, 1
FROM Menu m
WHERE m.idMenu NOT IN (@gAdmin, @gReportes)
  AND ISNULL(m.idMenuPadre, 0) NOT IN (@gAdmin, @gReportes);

/* --- Datos del comercio para las facturas ---------------------------------
   De ejemplo: se reemplazan por los reales desde Administración > Datos del
   comercio antes de imprimir una factura. */
INSERT INTO Emisor (idEmisor, razonSocial, cuit, condicionIva, domicilio, ingresosBrutos,
                    inicioActividades, puntoVenta)
VALUES (1, N'Lubricentro Control S.R.L.', '30716543214', 'Responsable Inscripto',
        N'Av. San Martín 1500, Ciudad Autónoma de Buenos Aires', '901-716543-2', '2020-03-01', 1);

GO

PRINT 'Datos iniciales cargados.';
PRINT 'Usuario: admin@lubricentro.com / Admin123!';
GO
