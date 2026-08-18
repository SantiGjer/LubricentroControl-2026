/* ============================================================================
   LubricentroControl 2026 — Datos iniciales

   Carga los 3 roles, el árbol de menú con sus permisos por rol, y el usuario
   administrador inicial. Correr DESPUÉS de 01_Esquema.sql.

     sqlcmd -S "(localdb)\MSSQLLocalDB" -i Database\02_DatosIniciales.sql

   Usuario inicial:  admin@lubricentro.com  /  Admin123!
   >>> Cambiar esa contraseña después del primer login. <<<
   ============================================================================ */

USE LubricentroControl;
GO

SET NOCOUNT ON;

/* --- Roles --------------------------------------------------------------- */
INSERT INTO Nivel (nombre, jerarquia) VALUES
    ('Admin', 1),
    ('Encargado', 2),
    ('Empleado', 3);

DECLARE @admin INT = (SELECT idNivel FROM Nivel WHERE nombre = 'Admin');
DECLARE @encargado INT = (SELECT idNivel FROM Nivel WHERE nombre = 'Encargado');
DECLARE @empleado INT = (SELECT idNivel FROM Nivel WHERE nombre = 'Empleado');

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
    ('Inicio',                      '~/Default'),
    ('Clientes',                    '~/Clientes'),
    ('Vehículos',                   '~/Vehiculos'),
    ('Turnos',                      '~/Turnos'),
    ('Órdenes de trabajo',          '~/OrdenesDeTrabajo'),
    ('Servicios',                   '~/Servicios'),
    ('Proveedores',                 '~/Proveedores'),
    ('Insumos',                     '~/Insumos'),
    ('Compras',                     '~/Compras'),
    ('Ventas',                      '~/Ventas'),
    ('Pagos',                       '~/Pagos'),
    ('Cuenta corriente clientes',   '~/CuentaCorrienteClientes'),
    ('Cuenta corriente proveedores','~/CuentaCorrienteProveedores'),
    ('Reporte de stock bajo',       '~/Reportes/StockBajo'),
    ('Reporte de ventas por período','~/Reportes/VentasPorPeriodo'),
    ('Reporte de cuentas corrientes','~/Reportes/CuentasCorrientes'),
    ('Usuarios',                    '~/Usuarios');

/* --- Árbol de menú -------------------------------------------------------
   idUrl NULL = grupo desplegable.                                          */
DECLARE @idMenu INT;

/* Nivel raíz: Inicio (link directo) */
INSERT INTO Menu (texto, idUrl, idMenuPadre, orden)
    VALUES ('Inicio', (SELECT idUrl FROM Url WHERE path = '~/Default'), NULL, 1);
DECLARE @mInicio INT = SCOPE_IDENTITY();

/* Grupos */
INSERT INTO Menu (texto, idUrl, idMenuPadre, orden) VALUES ('Clientes', NULL, NULL, 2);
DECLARE @gClientes INT = SCOPE_IDENTITY();
INSERT INTO Menu (texto, idUrl, idMenuPadre, orden) VALUES ('Operación', NULL, NULL, 3);
DECLARE @gOperacion INT = SCOPE_IDENTITY();
INSERT INTO Menu (texto, idUrl, idMenuPadre, orden) VALUES ('Compras', NULL, NULL, 4);
DECLARE @gCompras INT = SCOPE_IDENTITY();
INSERT INTO Menu (texto, idUrl, idMenuPadre, orden) VALUES ('Ventas y cobros', NULL, NULL, 5);
DECLARE @gVentas INT = SCOPE_IDENTITY();
INSERT INTO Menu (texto, idUrl, idMenuPadre, orden) VALUES ('Reportes', NULL, NULL, 6);
DECLARE @gReportes INT = SCOPE_IDENTITY();
INSERT INTO Menu (texto, idUrl, idMenuPadre, orden) VALUES ('Administración', NULL, NULL, 7);
DECLARE @gAdmin INT = SCOPE_IDENTITY();

/* Hojas */
INSERT INTO Menu (texto, idUrl, idMenuPadre, orden)
SELECT v.texto, u.idUrl, v.padre, v.orden
FROM (VALUES
    ('Clientes',                     '~/Clientes',                     @gClientes,  1),
    ('Vehículos',                    '~/Vehiculos',                    @gClientes,  2),
    ('Turnos',                       '~/Turnos',                       @gOperacion, 1),
    ('Órdenes de trabajo',           '~/OrdenesDeTrabajo',             @gOperacion, 2),
    ('Servicios',                    '~/Servicios',                    @gOperacion, 3),
    ('Proveedores',                  '~/Proveedores',                  @gCompras,   1),
    ('Insumos',                      '~/Insumos',                      @gCompras,   2),
    ('Compras',                      '~/Compras',                      @gCompras,   3),
    ('Ventas',                       '~/Ventas',                       @gVentas,    1),
    ('Pagos',                        '~/Pagos',                        @gVentas,    2),
    ('Cta. cte. clientes',           '~/CuentaCorrienteClientes',      @gVentas,    3),
    ('Cta. cte. proveedores',        '~/CuentaCorrienteProveedores',   @gVentas,    4),
    ('Stock bajo',                   '~/Reportes/StockBajo',           @gReportes,  1),
    ('Ventas por período',           '~/Reportes/VentasPorPeriodo',    @gReportes,  2),
    ('Cuentas corrientes',           '~/Reportes/CuentasCorrientes',   @gReportes,  3),
    ('Usuarios',                     '~/Usuarios',                     @gAdmin,     1)
) AS v(texto, path, padre, orden)
JOIN Url u ON u.path = v.path;

/* --- Permisos de menú por rol -------------------------------------------
   Refleja la matriz de permisos de los requerimientos (§5).
   soloLectura = 1 son los casos "👁️ Solo consulta" del rol Empleado.       */

/* Admin ve absolutamente todo, con permiso completo. */
INSERT INTO MenuNivel (idMenu, idNivel, soloLectura)
SELECT idMenu, @admin, 0 FROM Menu;

/* Encargado: todo menos Administración (gestión de usuarios). */
INSERT INTO MenuNivel (idMenu, idNivel, soloLectura)
SELECT idMenu, @encargado, 0
FROM Menu
WHERE idMenu <> @gAdmin AND ISNULL(idMenuPadre, 0) <> @gAdmin;

/* Empleado: sin Administración ni Reportes; consulta en compras y ctas. ctes. */
INSERT INTO MenuNivel (idMenu, idNivel, soloLectura)
SELECT m.idMenu, @empleado,
       CASE WHEN u.path IN ('~/Proveedores', '~/Insumos', '~/Compras', '~/Servicios',
                            '~/CuentaCorrienteClientes', '~/CuentaCorrienteProveedores')
            THEN 1 ELSE 0 END
FROM Menu m
LEFT JOIN Url u ON u.idUrl = m.idUrl
WHERE m.idMenu NOT IN (@gAdmin, @gReportes)
  AND ISNULL(m.idMenuPadre, 0) NOT IN (@gAdmin, @gReportes);

GO

PRINT 'Datos iniciales cargados.';
PRINT 'Usuario: admin@lubricentro.com / Admin123!';
GO
