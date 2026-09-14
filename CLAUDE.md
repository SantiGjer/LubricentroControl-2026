# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Qué es este proyecto

Sistema de gestión para un Lubricentro (clientes, vehículos, turnos, órdenes de trabajo,
proveedores, insumos, compras, ventas, pagos, cuentas corrientes y reportes), con control de
acceso por roles. TP de Programación Avanzada 2026 — USAL.

**Estado real del código: Fase 1, Fase 2 y Fase 3 terminadas.** Andan el login, la recuperación de
contraseña por mail, el ABM de usuarios, el menú dinámico por rol, la capa `BIZ/Data` de punta a
punta contra SQL Server, los 5 ABM de Fase 2 (**Clientes**, **Vehículos**, **Proveedores**,
**Insumos** con kardex de stock, y **Servicios**), y las 2 pantallas de Fase 3 (**Turnos** y
**Órdenes de trabajo**, esta última con descuento/reposición automática de stock). Las 21 tablas
del diagrama original ya existen (`Database\01_Esquema.sql`), más `MovimientoStock` (kardex de
stock, agregada en Fase 2 — ver §9.3 de los Requerimientos). **El resto de las pantallas de
negocio (Compras, Ventas, Pagos, cuentas corrientes, reportes) siguen siendo cascarones vacíos**:
solo muestran su título y "Pendiente".

Documentos de referencia (leer antes de diseñar algo del dominio):

- `Docs/Lubricentro_Requerimientos.md` — alcance, matriz de permisos por rol, las 21 entidades
  (+ `MovimientoStock`), reglas de negocio, qué quedó explícitamente fuera de alcance, y §9 con
  los supuestos/formatos ya confirmados en Fase 2/3 (DNI/CUIT/patente, diseño de
  Clientes/Vehículos, kardex de stock, estados de Turno/Orden).
- `Docs/Lubricentro_Roadmap.md` — 6 fases de ejecución. **Sigue la Fase 4** (Compras, Ventas,
  Pagos, Cuentas corrientes). Los ABM de Fase 2 y las pantallas de Fase 3 quedan como referencia
  de patrón — Proveedores/Insumos/Servicios para el modo solo-consulta, Clientes/Vehículos para
  el layout de dos columnas y el buscador desplegable, Turnos/Órdenes para pantallas con
  cliente/vehículo fijo post-alta y (en Órdenes) franja de detalle con líneas.

## Restricciones del stack (no negociables)

Vienen impuestas por los requerimientos de la materia:

- **ASP.NET Web Forms** sobre **.NET Framework 4.7.2** — no .NET Core / .NET 5+, no MVC, no Razor,
  no Blazor.
- **ADO.NET puro** (`SqlConnection` / `SqlCommand`, SQL parametrizado) — **sin ORM**, sin Entity
  Framework, sin Dapper.
- SQL Server, accedido por VPN Radmin.
- NuGet con `packages.config` (no `PackageReference`). Las dependencias ya están commiteadas en
  `packages/`.
- Bootstrap 5.2.3 + jQuery 3.7.0, con bundling vía `System.Web.Optimization`.

## Comandos

Compilar la solución:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "LubricentroControl-2026.sln" /p:Configuration=Debug
```

Compilar un solo proyecto: mismo comando apuntando a `BIZ\BIZ.csproj` o a
`LubricentroControl-2026\LubricentroControl-2026.csproj`.

Validar el markup `.aspx` (MSBuild solo compila el code-behind, **no** detecta errores en el
markup ni en los `.designer.cs` desincronizados):

```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\aspnet_compiler.exe" -v / -p "LubricentroControl-2026" <carpeta-salida>
```

Ejecutar: F5 desde Visual Studio 2022 (IIS Express en `https://localhost:44356/`, configurado en
`LubricentroControl-2026.csproj.user`). Sin Visual Studio:

```powershell
& "C:\Program Files\IIS Express\iisexpress.exe" /path:"<ruta-absoluta>\LubricentroControl-2026" /port:8123
```

Crear/recrear la base (LocalDB, en orden; el paso 1 **borra los datos**). El `-f 65001` no es
opcional — ver «Codificación» más abajo:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i "Database\01_Esquema.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i "Database\02_DatosIniciales.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i "Database\03_UsuariosDePrueba.sql"   # opcional
```

Restore de paquetes: lo hace Visual Studio al abrir la solución. `dotnet restore` **no aplica**
acá (es `packages.config`) y `nuget.exe` no está instalado en esta máquina.

**No hay proyecto de tests** ni framework de testing configurado en la solución. No inventar un
comando de tests: la verificación es compilar, correr `aspnet_compiler` y probar la pantalla
levantando IIS Express.

## Arquitectura

Dos proyectos en la solución:

| Proyecto | Rol |
|---|---|
| `LubricentroControl-2026` | Capa web: páginas `.aspx`. RootNamespace `LubricentroControl_2026` (guion **bajo**), assembly `LubricentroControl-2026` (guion medio) |
| `BIZ` | Biblioteca de clases: lógica de negocio **y** acceso a datos, juntos en el mismo proyecto |

Dentro de `BIZ` hay tres carpetas, **no proyectos aparte** (decisión explícita de los
requerimientos §4 — no partir `BIZ`):

- `Modelo/` — entidades (`Usuario`, `Nivel`, `Url`, `ItemMenu`, `RecuperacionClave`,
  `ResultadoOperacion`, `Cliente`, `Vehiculo`, `Proveedor`, `Insumo`, `MovimientoStock`, `Servicio`,
  `Turno`, `OrdenDeTrabajo`, `DetalleOrdenServicio`, `DetalleOrdenInsumo`).
- `Data/` — el DAL **y las reglas de negocio**, juntos en la misma clase por entidad (ej.
  `UsuarioDAL`, `RecuperacionClaveDAL`, `MenuDAL`, `ClienteDAL`, `VehiculoDAL`, `ProveedorDAL`,
  `InsumoDAL`, `MovimientoStockDAL`, `ServicioDAL`, `TurnoDAL`, `OrdenDeTrabajoDAL`,
  `DetalleOrdenServicioDAL`, `DetalleOrdenInsumoDAL`). Todo pasa por
  `AccesoDatos.cs`, que centraliza
  la cadena de conexión y expone `Consultar` / `Ejecutar` / `Escalar` + los helpers `LeerString`,
  `LeerInt`, etc. para mapear `DataRow`. **Nunca concatenar SQL**: siempre
  `AccesoDatos.Param("@x", valor)`. Las operaciones devuelven `ResultadoOperacion` (`Ok`/`Error`) en
  vez de tirar excepciones para validaciones esperables (mail duplicado, sin admins activos,
  credenciales inválidas, etc.).
- `Negocio/` — hoy solo `PasswordHasher.cs`. Se desarmó como capa separada de reglas de negocio
  (ver «Historial de decisiones» abajo): `UsuarioNegocio`, `SeguridadNegocio` y `MenuNegocio` se
  fusionaron dentro de `Data/` (`UsuarioDAL`, `RecuperacionClaveDAL`, `MenuDAL`), y
  `ResultadoOperacion`/`ServicioMail` se reubicaron en `Modelo/`/`Data/` respectivamente.
  **No recrear esa capa** al agregar módulos nuevos — seguir el patrón: una clase por entidad en
  `Data/` que hace acceso a datos y valida sus propias reglas.

Reglas transversales de la capa web:

- **La dependencia va Web → BIZ, nunca al revés.** Si algo en `BIZ` parece necesitar algo del web,
  el diseño está mal.
- **Toda pantalla del menú hereda de `PaginaSegura`** (`Seguridad/PaginaSegura.cs`), que verifica
  contra la base que el rol tenga permiso sobre esa ruta. Esconder la opción del menú **no** es
  suficiente: sin esa guarda alcanza con escribir la URL a mano. Las pantallas fuera del menú que
  igual exigen login heredan de `PaginaConSesion`; `Login`, `RecuperarClave` y `RestablecerClave`
  son `Page` común.
- `PaginaSegura` expone `EsSoloLectura` para los casos "👁️ Solo consulta" de la matriz de permisos.
  **Una pantalla nueva debe deshabilitar sus acciones de escritura cuando vale true.** Patrón ya
  implementado en `Proveedores.aspx` (primera pantalla real que lo necesita, aplica igual a
  Insumos y Servicios): esconder el `Panel` del formulario entero
  (`pnlFormulario.Visible = !EsSoloLectura`) y la columna "Acciones" de la grilla
  (`gvX.Columns[n].Visible = false`), no solo deshabilitar botones — y además cada método de
  escritura (`Guardar`/`Borrar`/`RowCommand`) chequea `EsSoloLectura` y corta al principio, por si
  alguien fuerza el request aunque el control esté escondido.
- La sesión se toca solo a través de `Seguridad/SesionUsuario.cs`, nunca `Session["..."]` directo.
- El menú se arma en `Site.Master.cs` desde `MenuDAL.ObtenerArbol(idNivel)` (que a su vez arma el
  árbol con `ItemMenu.ArmarArbol`, en `Modelo/`, a partir de la lista plana de
  `MenuDAL.ListarPorNivel`). Para agregar una
  pantalla al menú hay que insertar filas en `Url`, `Menu` y `MenuNivel` — ver el patrón en
  `Database\02_DatosIniciales.sql`. Una pantalla sin fila en `MenuNivel` es inaccesible para ese rol.
- **FriendlyUrls está activo** (`App_Start/RouteConfig.cs`): los links y los `path` de la tabla
  `Url` van sin extensión — `~/Clientes`, no `~/Clientes.aspx`. Rompe el cross-page posting
  clásico de Web Forms (`PostBackUrl`/`PreviousPage`) — ver «Cross-page posting no funciona con
  FriendlyUrls» más abajo antes de usar esa técnica.
- Cada página `.aspx` tiene su code-behind `.aspx.cs` y un `.aspx.designer.cs` que declara los
  controles. Editando fuera de Visual Studio **hay que actualizar el designer a mano**; si falta un
  control, MSBuild compila igual y el error recién aparece con `aspnet_compiler` o en runtime.
- `Global.asax.cs` registra rutas y bundles al arrancar.
- Todas las páginas cuelgan de `Site.Master` (el `Site.Mobile.Master` y el `ViewSwitcher.ascx` del
  template se eliminaron, igual que `About.aspx` y `Contact.aspx`).

## Reglas de negocio que cruzan módulos

Estas no se ven leyendo un solo archivo:

- **Stock automático en los dos sentidos:** baja al agregar una línea de insumo a una orden de
  trabajo (`DetalleOrdenInsumoDAL.Agregar`, Fase 3, ya implementado), sube al registrar una compra
  a proveedor (Fase 4, todavía sin implementar). **Cancelar una orden de trabajo repone el stock
  de los insumos cargados** (`OrdenDeTrabajoDAL.Cancelar`, ya implementado). Cada cambio de stock
  (orden, cancelación de orden, ajuste manual, y compra cuando exista) queda registrado en
  `MovimientoStock` (kardex) con quién y por qué. `MovimientoStockDAL.Registrar` es el camino para
  tocar `Insumo.stockActual` **excepto** cuando la escritura de stock tiene que ir atómicamente
  junto con una tercera tabla (`DetalleOrdenInsumoDAL.Agregar` arma su propio batch
  `XACT_ABORT`/`BEGIN TRAN`/`COMMIT` en vez de llamar a `Registrar`, para no dejar un movimiento de
  kardex sin su línea de detalle si esa tercera escritura fallara — ver «Historial de decisiones»
  para el detalle y el mecanismo de atomicidad).
- **La venta no se carga a mano:** el comprobante de venta se genera automáticamente al cerrar la
  orden de trabajo. Es un comprobante interno, sin validez fiscal.
- **El vínculo Turno–Orden es opcional:** una orden puede nacer de un turno previo o de un walk-in
  (cliente que llega sin turno, se da de alta en el momento).
- Clientes **y** proveedores pueden quedar con saldo pendiente; la cuenta corriente funciona en
  ambos sentidos (a favor o en contra).
- Roles jerárquicos **Admin > Encargado > Empleado**. El menú se arma dinámicamente según el nivel
  del usuario logueado (entidades `Menu`, `Url`, `Nivel`). El rol Empleado tiene acceso restringido
  a compras y cuentas corrientes (solo consulta) y ninguno a reportes financieros ni a gestión de
  usuarios.
- **Fuera de alcance por decisión explícita:** facturación fiscal / AFIP, portal público de turnos
  para el cliente, notificaciones automáticas por mail o SMS, multi-sucursal.

## Base de datos

`Database\01_Esquema.sql` crea las 21 entidades del diagrama E/R más `MenuNivel` (tabla de
relación menú↔rol, no es una entidad) y `MovimientoStock` (kardex de stock, agregada en Fase 2 —
no estaba en el diagrama original, ver Requerimientos §8 y §9.3). Los estados de `Turno` y
`OrdenDeTrabajo` están fijados por `CHECK` — usar exactamente esos literales. `MovimientoStock`
usa un patrón distinto para su `tipoMovimiento`: sin `CHECK` sobre los valores literales (igual
que `CuentaCorrienteCliente`/`Proveedor`), pero con `CK_MovStock_origen`, que ata cada tipo a qué
FK debe estar poblada (mismo criterio que `CK_Pago_titular`) — entre los dos, cualquier valor
fuera de los 4 esperados ya queda rechazado sin necesitar un CHECK aparte.

Hoy apunta a **LocalDB** (`(localdb)\MSSQLLocalDB`, base `LubricentroControl`). Para pasar al
SQL Server del lubricentro por VPN Radmin alcanza con cambiar la cadena `LubricentroDB` en
`Web.config`; los scripts corren igual.

Usuario inicial que siembra `02_DatosIniciales.sql`: **admin@lubricentro.com / Admin123!**

## Contraseñas y mails

- El hash es **PBKDF2-SHA256, 25.000 iteraciones, 32 bytes**, salt por usuario, en
  `BIZ\Negocio\PasswordHasher.cs`. Cambiar cualquiera de esas constantes invalida todos los hashes
  existentes, incluido el del admin sembrado por SQL.
- Con `MailModoDesarrollo=true` en `Web.config` los mails **no salen por SMTP**: se escriben como
  `.eml` en `App_Data\MailsEnviados`. Así se prueba el circuito de recuperación de clave sin
  servidor de correo. Ese archivo `.eml` tiene el cuerpo en base64.
- La recuperación responde **el mismo mensaje genérico exista o no el mail**, y el login usa un
  único mensaje de error para usuario inexistente y contraseña incorrecta. Es a propósito: evita
  que el formulario sirva para averiguar qué cuentas existen. No "mejorar" esos mensajes.

## Codificación (ya mordió una vez)

El proyecto es todo en español y acentuado, y en Windows hay dos trampas distintas. Las dos ya
pasaron y están arregladas; lo que sigue es para no repetirlas.

- **Guardar todo `.aspx`, `.master`, `.ascx` y `.sql` como UTF-8 CON BOM.** Sin BOM, ASP.NET
  parsea el markup con el codepage ANSI del sistema y los acentos salen como `ProgramaciÃ³n`.
  `Web.config` ya trae `<globalization fileEncoding="utf-8" …/>` que cubre el caso, pero el BOM
  es lo que espera Visual Studio — poner los dos.
- **Correr siempre `sqlcmd` con `-f 65001`.** Sin eso lee el `.sql` como CP1252 y **guarda el
  texto ya corrompido en la base**: es corrupción de datos, no de presentación, y no se arregla
  tocando el HTML. Fue lo que dejó `VehÃ­culos` dentro de `Menu.texto`.
- Para verificar que un texto de la base está sano, mirar los codepoints, no el texto:
  `í` tiene que ser `237`, no la pareja `195,173`.
- Los `.cs` **no** están afectados: el compilador de C# asume UTF-8 cuando no hay BOM.

## Cross-page posting no funciona con FriendlyUrls (ya mordió una vez)

El mecanismo clásico de ASP.NET Web Forms para pasar datos de una página a otra vía ViewState —
`<asp:Button PostBackUrl="~/Otra.aspx">` + `Page.PreviousPage` (opcionalmente con
`<%@ PreviousPageType %>` para tiparlo) — **no anda en este proyecto**. `PreviousPage`
reconstruye la página de origen a partir de la ruta con la que se accedió, y como acá todo se
navega con FriendlyUrls (`~/Vehiculos`, sin extensión — ver más abajo), `BuildManager` no
encuentra ningún archivo físico en esa ruta y tira `HttpException: El archivo '/Vehiculos' no
existe`. Pasa apenas se lee `PreviousPage` en la página destino, incluso si el `PostBackUrl`
apunta a la ruta amigable en vez de al `.aspx` (que además tiene su propio problema: el
`AutoRedirectMode = RedirectMode.Permanent` de `RouteConfig.cs` hace un 301 de `Algo.aspx` a
`Algo`, y ese redirect **pierde el POST** — se vuelve GET).

**Para llevar datos de una pantalla a otra, usar `Response.Redirect` con query string** (ver
`Vehiculos.aspx.cs` → `btnNuevoCliente_Click` y `Clientes.aspx.cs` → `ArmarUrlVuelta`/
`btnGuardar_Click`, el flujo de "Nuevo cliente" desde Vehículos). Si el control que dispara la
navegación vive dentro de un `UpdatePanel`, declararlo como `<asp:PostBackTrigger>` explícito en
`<Triggers>` — un trigger async normal no deja que `Response.Redirect` navegue de verdad.

## Convenciones

Código, comentarios, nombres de entidades y textos de UI **en español**, siguiendo la
nomenclatura de `Docs/` (`Cliente`, `Vehiculo`, `OrdenDeTrabajo`, `DetalleOrdenInsumo`, etc.).

La aplicación **no debe mencionar fases de desarrollo, el roadmap ni el estado del proyecto** en
la interfaz. Las pantallas sin implementar dicen solo «Pendiente». El seguimiento del avance vive
en `Docs/EstadoActual.md`, no en la UI.

### Formato de DNI, CUIT y patente (Fase 2)

Decisión de negocio en `Docs/Lubricentro_Requerimientos.md` §9.1. Regex de referencia para los
validadores de Cliente, Proveedor y Vehiculo:

| Campo | Guardado | Regex | Ejemplo |
|---|---|---|---|
| `Cliente.dni` | tal cual, sin puntos | `^\d{7,8}$` | `12345678` |
| `Proveedor.cuit` | sin guiones | `^\d{11}$` | guarda `20123456786`, muestra `20-12345678-6` |
| `Vehiculo.patente` | mayúsculas | `^([A-Z]{3}\d{3}|[A-Z]{2}\d{3}[A-Z]{2})$` | `ABC123` o `AB123CD` |

El CUIT es el único de los tres que necesita una función de formateo para mostrar (insertar los
guiones en las posiciones 2 y 10 sobre los 11 dígitos guardados); DNI y patente se muestran igual
que se guardan.

### Patrón de validación de formularios (Fase 2)

Las reglas de formato (DNI, CUIT, patente, mail) se validan con **`CustomValidator` +
`OnServerValidate`**, no con `RegularExpressionValidator`: la regex vive una sola vez, como
método estático en la entidad de `Modelo`, y el validador del `.aspx` solo lo llama. Evita
duplicar cada regex entre el markup y el modelo (que fue el problema con
`Usuario.Validar()`/`FormatoEmail`, que hoy no tiene ningún validator equivalente en
`Usuarios.aspx`).

Patrón a seguir:

```csharp
// BIZ/Modelo/Cliente.cs
public static bool EsDniValido(string dni)
{
    return DniRegex.IsMatch(dni ?? "");
}
```

```html
<!-- Clientes.aspx -->
<asp:CustomValidator runat="server" ControlToValidate="txtDni" OnServerValidate="valDni_ServerValidate"
    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cliente"
    ErrorMessage="El DNI debe tener 7 u 8 números, sin puntos." />
```

```csharp
// Clientes.aspx.cs
protected void valDni_ServerValidate(object source, ServerValidateEventArgs args)
{
    args.IsValid = Cliente.EsDniValido(args.Value);
}
```

Consecuencias de esta elección, para tenerlas presentes al escribir los ABM:

- **No hay chequeo de formato en el navegador** (no hay `ClientValidationFunction`): el error
  aparece recién después del postback, igual que `RequiredFieldValidator` tarda 0 viajes al
  servidor pero esto tarda 1. Sí queda con el mismo estilo visual inline (`text-danger small`,
  pegado al campo) que los validators declarativos — no es el banner genérico de arriba.
- **Alcance: solo formato, no reglas que necesitan la base.** Unicidad (DNI/CUIT/patente
  repetidos) y reglas cruzadas (ej. "no dejar el sistema sin admins") siguen resolviéndose en
  `Validar()` del DAL correspondiente y se siguen mostrando con el banner genérico
  (`ResultadoOperacion.Error(...)` → `MostrarMensaje`), tal como hoy: no son responsabilidad de
  `CustomValidator`, que valida campo por campo sin ir a la base.

La entidad `Menu` del diagrama E/R se llama `ItemMenu` en C# (`BIZ\Modelo\ItemMenu.cs`) para no
chocar con `System.Web.UI.WebControls.Menu` en los code-behind. La tabla sigue llamándose `Menu`.

## Historial de decisiones

- **Se desarmó la capa `Negocio/` como capa separada (sesión 2026-09-06).** El diseño original de
  3 capas (`Modelo`/`Data`/`Negocio`) generaba una clase `Negocio` por cada clase `DAL` casi en
  espejo, sin aportar separación real: las reglas de negocio (validar antes de guardar, no
  quedarse sin admins, mensajes anti-enumeración de cuentas) quedaron fusionadas dentro de la
  misma clase `Data` que hace el acceso a datos. Se eliminaron `UsuarioNegocio.cs`,
  `SeguridadNegocio.cs` y `MenuNegocio.cs`; su lógica pasó a `UsuarioDAL`, `RecuperacionClaveDAL` y
  `MenuDAL` respectivamente. `ResultadoOperacion.cs` se movió a `Modelo/` (es un tipo de dato, no
  una regla) y `ServicioMail.cs` a `Data/` (es I/O externo, no una regla de negocio).
  `BIZ\Negocio\` quedó solo con `PasswordHasher.cs`. Los módulos de Fase 2 en adelante deben seguir
  este patrón de 2 capas, no recrear `Negocio/`.

  Esto además **alinea el código con `Docs/Lubricentro_Requerimientos.md` §4**, que siempre
  describió `BIZ` como `Modelo` + `Data` (2 carpetas) con la lógica de negocio *dentro* de `Data`
  ("acceso a datos... y reglas de validación... todo dentro del mismo proyecto"). La carpeta
  `Negocio/` como capa separada nunca estuvo en el requerimiento original: fue una interpretación
  de la Fase 1 que se revierte con este cambio.

- **`MovimientoStock` (kardex de stock, sesión 2026-09-07).** Entidad nueva, agregada más allá de
  las 21 del diagrama E/R original, para poder trazar cada cambio de stock (compra, orden,
  cancelación, ajuste manual) con quién, cuándo y por qué — ver
  `Docs/Lubricentro_Requerimientos.md` §9.3. A diferencia de `CuentaCorrienteCliente`/`Proveedor`
  (el patrón de "tabla de movimientos" ya existente, que no llevan `idUsuario`), `MovimientoStock`
  **sí** lo lleva como `NOT NULL`: hay precedente directo en `Pago` (que tampoco es una
  `CuentaCorriente*` y sí guarda `idUsuario`) — el criterio es "si el 'quién' importa como dato
  operativo, se guarda", y acá se pidió explícitamente poder saber quién ajustó el stock.

  **Atomicidad sin tocar `AccesoDatos.cs`:** actualizar `Insumo.stockActual` e insertar la fila en
  `MovimientoStock` tienen que pasar juntos o ninguno. Como `AccesoDatos.cs` no expone
  transacciones (cada método abre su propia conexión), `MovimientoStockDAL.Registrar` arma un solo
  batch de texto SQL con `SET XACT_ABORT ON; BEGIN TRANSACTION; ...; COMMIT TRANSACTION;` — el
  `XACT_ABORT` es imprescindible, sin él un `BEGIN TRAN`/`COMMIT` no revierte automáticamente el
  `UPDATE` si el `INSERT` falla a mitad de camino. Verificado a mano con `sqlcmd`: sin
  `TRY/CATCH` alrededor (igual que en el código real, que no envuelve `AccesoDatos.Ejecutar` en
  ningún `try/catch`), un `INSERT` que viola un `CHECK` revierte el `UPDATE` anterior — confirmado
  leyendo el valor desde una conexión separada.

  Fase 4 (`CompraDAL`, todavía sin implementar) va a llamar directo a
  `MovimientoStockDAL.Registrar(...)` con `MovimientoStock.TipoCompra` — a propósito **no** se
  agregaron wrappers (`RegistrarEntradaPorCompra`, etc.) sin caller todavía: hubiera sido diseñar
  para un requerimiento hipotético futuro. En Fase 3, `OrdenDeTrabajoDAL.Cancelar` sí terminó
  llamando directo a `Registrar` con `TipoCancelacionOrden` como estaba previsto acá, pero
  **agregar una línea de insumo a una orden no** — ver la entrada de Fase 3 más abajo para la
  única excepción real a "`Registrar` es el único camino".

- **Turnos y Órdenes de trabajo (Fase 3, sesión 2026-09-11/2026-09-14).** Las dos pantallas de
  agenda/taller, sobre `Turno`/`TurnoDAL` y `OrdenDeTrabajo`+`DetalleOrdenServicio`+
  `DetalleOrdenInsumo` (con sus DAL). Detalle completo de sesión en `Docs/EstadoActual.md`; acá
  solo lo que deja precedente para módulos futuros:

  **Cliente/vehículo (y en Órdenes, turno) quedan fijos una vez creada la fila.** Se eligen al dar
  de alta, nunca se reasignan después — evita el problema de qué hacer cuando un `DropDownList`
  poblado en el momento de la creación ya no refleja la realidad al editar (un vehículo dado de
  baja después, un turno que pasó a `Completado`). Editando una fila existente, esos datos se
  muestran como texto de solo lectura, no como controles editables. Los módulos de Fase 4 que
  referencien una entidad ya creada (`ComprobanteCompra.idProveedor`, `Pago.idCliente`/
  `idProveedor`, etc.) deberían seguir el mismo criterio salvo que el requerimiento pida
  explícitamente poder reasignar.

  **Bug real encontrado dos veces, mismo patrón: un `DropDownList` sin ningún `<option>`
  renderizado rechaza cualquier valor posteado — incluso `""` — con `HttpUnhandledException:
  Argumento de postback no válido`.** Pasó primero con `ddlVehiculo` en Turnos (se poblaba recién
  al elegir cliente, nunca en el primer `Page_Load`) y de nuevo con `ddlEstado` en Órdenes (vive
  dentro de un `Panel` que arranca invisible, así que nunca se renderiza en el alta). La solución
  en los dos casos: asegurar que el control tenga **al menos su placeholder** cargado ya en el
  primer `Page_Load`, no solo en el flujo que lo repuebla más tarde. Cualquier `DropDownList`
  poblado dinámicamente en una pantalla nueva tiene que revisarse contra este mismo problema.

  **`DetalleOrdenInsumoDAL.Agregar` es la única excepción a "`MovimientoStockDAL.Registrar` es el
  único camino para tocar `Insumo.stockActual`".** Agregar una línea de insumo son tres escrituras
  atómicas (`UPDATE Insumo`, `INSERT MovimientoStock`, `INSERT DetalleOrdenInsumo`), y `Registrar`
  solo cubre las primeras dos. Se replicó el mismo patrón `XACT_ABORT`/`BEGIN TRAN`/`COMMIT`
  directo ahí (duplicando ~15 líneas de SQL) en vez de acoplar `Data/MovimientoStockDAL` a
  `Data/DetalleOrdenInsumoDAL` con un parámetro extra especulativo. La reposición de stock al
  cancelar una orden no tiene este problema (no inserta detalle nuevo) y sí llama a `Registrar`
  directo, tal como se había anticipado en la entrada anterior.

  **Walk-in con cliente y vehículo nuevos, sin salir de la pantalla.** Como `OrdenDeTrabajo.
  idVehiculo` es `NOT NULL`, se extendió el mecanismo de `Response.Redirect` + query string que ya
  conectaba `Vehiculos.aspx` ↔ `Clientes.aspx` (ver «Cross-page posting no funciona con
  FriendlyUrls» más abajo) agregando un tercer origen `"orden"` en paralelo al `"vehiculo"`
  existente — sin tocar esa lógica. Cualquier pantalla futura que necesite el mismo atajo de alta
  en cascada sigue este patrón: un origen nuevo, hidden fields propios (prefijo distinto, acá
  `hdnOr*`), nunca reescribir la rama existente.
