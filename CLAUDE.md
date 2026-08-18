# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Qué es este proyecto

Sistema de gestión para un Lubricentro (clientes, vehículos, turnos, órdenes de trabajo,
proveedores, insumos, compras, ventas, pagos, cuentas corrientes y reportes), con control de
acceso por roles. TP de Programación Avanzada 2026 — USAL.

**Estado real del código: Fase 1 terminada.** Andan el login, la recuperación de contraseña por
mail, el ABM de usuarios, el menú dinámico por rol y la capa `BIZ/Data` de punta a punta contra
SQL Server. Las 21 tablas ya existen (`Database\01_Esquema.sql`), pero **las pantallas de negocio
son cascarones vacíos**: solo muestran su título y en qué fase se implementan.

Documentos de referencia (leer antes de diseñar algo del dominio):

- `Docs/Lubricentro_Requerimientos.md` — alcance, matriz de permisos por rol, las 21 entidades,
  reglas de negocio, y qué quedó explícitamente fuera de alcance.
- `Docs/Lubricentro_Roadmap.md` — 6 fases de ejecución. **Sigue la Fase 2:** ABM de Clientes,
  Vehículos, Proveedores, Insumos y Servicios. Son independientes entre sí, se pueden hacer en
  paralelo, y todas tienen ya su tabla y su cascarón.

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

- `Modelo/` — entidades (`Usuario`, `Nivel`, `Url`, `ItemMenu`, `RecuperacionClave`).
- `Data/` — el DAL. Todo pasa por `AccesoDatos.cs`, que centraliza la cadena de conexión y expone
  `Consultar` / `Ejecutar` / `Escalar` + los helpers `LeerString`, `LeerInt`, etc. para mapear
  `DataRow`. **Nunca concatenar SQL**: siempre `AccesoDatos.Param("@x", valor)`.
- `Negocio/` — reglas y validaciones. Las operaciones devuelven `ResultadoOperacion` (`Ok`/`Error`)
  en vez de tirar excepciones para validaciones esperables.

Reglas transversales de la capa web:

- **La dependencia va Web → BIZ, nunca al revés.** Si algo en `BIZ` parece necesitar algo del web,
  el diseño está mal.
- **Toda pantalla del menú hereda de `PaginaSegura`** (`Seguridad/PaginaSegura.cs`), que verifica
  contra la base que el rol tenga permiso sobre esa ruta. Esconder la opción del menú **no** es
  suficiente: sin esa guarda alcanza con escribir la URL a mano. Las pantallas fuera del menú que
  igual exigen login heredan de `PaginaConSesion`; `Login`, `RecuperarClave` y `RestablecerClave`
  son `Page` común.
- `PaginaSegura` expone `EsSoloLectura` para los casos "👁️ Solo consulta" de la matriz de permisos.
  **Una pantalla nueva debe deshabilitar sus acciones de escritura cuando vale true.**
- La sesión se toca solo a través de `Seguridad/SesionUsuario.cs`, nunca `Session["..."]` directo.
- El menú se arma en `Site.Master.cs` desde `MenuNegocio.ObtenerArbol(idNivel)`. Para agregar una
  pantalla al menú hay que insertar filas en `Url`, `Menu` y `MenuNivel` — ver el patrón en
  `Database\02_DatosIniciales.sql`. Una pantalla sin fila en `MenuNivel` es inaccesible para ese rol.
- **FriendlyUrls está activo** (`App_Start/RouteConfig.cs`): los links y los `path` de la tabla
  `Url` van sin extensión — `~/Clientes`, no `~/Clientes.aspx`.
- Cada página `.aspx` tiene su code-behind `.aspx.cs` y un `.aspx.designer.cs` que declara los
  controles. Editando fuera de Visual Studio **hay que actualizar el designer a mano**; si falta un
  control, MSBuild compila igual y el error recién aparece con `aspnet_compiler` o en runtime.
- `Global.asax.cs` registra rutas y bundles al arrancar.
- Todas las páginas cuelgan de `Site.Master` (el `Site.Mobile.Master` y el `ViewSwitcher.ascx` del
  template se eliminaron, igual que `About.aspx` y `Contact.aspx`).

## Reglas de negocio que cruzan módulos

Estas no se ven leyendo un solo archivo:

- **Stock automático en los dos sentidos:** baja al cargar una orden de trabajo con insumos, sube
  al registrar una compra a proveedor.
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
relación menú↔rol, no es una entidad). Los estados de `Turno` y `OrdenDeTrabajo` están fijados por
`CHECK` — usar exactamente esos literales.

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

## Convenciones

Código, comentarios, nombres de entidades y textos de UI **en español**, siguiendo la
nomenclatura de `Docs/` (`Cliente`, `Vehiculo`, `OrdenDeTrabajo`, `DetalleOrdenInsumo`, etc.).

La aplicación **no debe mencionar fases de desarrollo, el roadmap ni el estado del proyecto** en
la interfaz. Las pantallas sin implementar dicen solo «Pendiente». El seguimiento del avance vive
en `Docs/EstadoActual.md`, no en la UI.

La entidad `Menu` del diagrama E/R se llama `ItemMenu` en C# (`BIZ\Modelo\ItemMenu.cs`) para no
chocar con `System.Web.UI.WebControls.Menu` en los code-behind. La tabla sigue llamándose `Menu`.
