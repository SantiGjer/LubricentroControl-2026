# Estado actual del sistema

**Proyecto:** LubricentroControl 2026 · Programación Avanzada — USAL
**Última actualización:** 17 de agosto de 2026

Documento vivo: se actualiza al cerrar cada sesión de trabajo. Registra hasta dónde está
completo el sistema, qué se hizo, y qué queda planificado para adelante.

---

## Cómo actualizar este documento

- Actualizar **"Última actualización"** (encabezado) con la fecha del día en que se edita,
  formato `17 de agosto de 2026`.
- La tabla de fases (sección 1) es **estado actual, no histórico**: se edita in place, no se
  agregan filas nuevas.
- Cada sesión de trabajo agrega una entrada nueva al principio de la sección 2, con encabezado
  `### AAAA-MM-DD — <resumen corto de la sesión>`. Las entradas anteriores no se tocan ni se
  borran: es una bitácora acumulativa. La fecha es la de la sesión real, no la de una eventual
  reescritura posterior.
- Qué va en una entrada de sesión: lo que **no** se puede derivar leyendo el código o
  `git log` — decisiones de diseño, bugs no obvios y su causa raíz, qué quedó verificado y cómo.
  No listar cada archivo tocado (para eso está git).
- Las secciones 3 ("Planificado para la próxima sesión") y 4 ("Pendientes conocidos, sin fecha")
  son **vivas**: se editan, tachan o mueven a una entrada de sesión cuando se resuelven, no se
  versionan por fecha.

---

## 1. Hasta dónde estamos

**Fase 1 completa y verificada.** Las fases 2 a 6 no están empezadas.

| Fase | Contenido | Estado |
|---|---|:---:|
| 1 | Login, roles, menú dinámico, ABM de usuarios, capa de datos | ✅ Completa |
| 2 | ABM de Clientes, Vehículos, Proveedores, Insumos, Servicios | ⬜ No empezada |
| 3 | Turnos y Órdenes de trabajo | ⬜ No empezada |
| 4 | Compras, Ventas, Pagos, Cuentas corrientes | ⬜ No empezada |
| 5 | Reportes | ⬜ No empezada |
| 6 | Integración, pruebas y pulido | ⬜ No empezada |

### Qué funciona hoy

- Login por mail y contraseña hasheada, con cierre de sesión.
- Recuperación de contraseña por mail, con token de un solo uso y vencimiento a 60 minutos.
- Cambio de contraseña propia.
- ABM de usuarios con asignación de rol, alta con contraseña temporal enviada por mail,
  blanqueo de clave y baja lógica.
- Menú principal armado dinámicamente desde la base según el rol del usuario.
- Control de acceso por pantalla verificado del lado del servidor: esconder la opción del menú
  no alcanza, la guarda corre en cada request.
- Las 21 tablas del diagrama E/R creadas, con los datos semilla de seguridad.
- Capa `BIZ/Data` funcionando de punta a punta contra SQL Server.

### Qué NO funciona todavía

Las 15 pantallas de negocio (Clientes, Vehículos, Turnos, Órdenes, Servicios, Proveedores,
Insumos, Compras, Ventas, Pagos, las dos cuentas corrientes y los tres reportes) son
**cascarones**: existen, están enlazadas desde el menú y respetan los permisos por rol, pero
no tienen funcionalidad.

---

## 2. Historial de sesiones

### 2026-08-17 — Simplificación de estilos en pantallas reales

Se comparó el estilo de este proyecto contra `ViewState` (otro TP de la materia, scaffold de
Visual Studio sin modificar) y salieron 9 diferencias. Se decidió revertir 3 de ellas — controles
sin `CssClass` de Bootstrap, layout de formulario sin `card`/centrado, y la clase del navbar
(`navbar-expand-sm navbar-toggleable-sm` en vez de `navbar-expand-lg`) — para **no anticipar
estilos "fuera de lo común" antes de que la lógica de negocio esté confirmada**. La idea es
volver a estilos más elaborados (cards, badges, tablas con clases, layout centrado) más adelante,
una vez validado que cada pantalla funciona bien — ver pendiente en la sección 4.

Alcance del cambio: `Login`, `RecuperarClave`, `RestablecerClave`, `CambiarClave`, `Usuarios`
(incluida su grilla: sin badges, sin clases de tabla, botones de acción sin estilo),
`AccesoDenegado`, `Default` (incluida la lista de accesos generada en `Default.aspx.cs`, que
también usaba `badge`/`list-unstyled`) y `Site.Master` (solo la clase del `<nav>`).

**Deliberadamente fuera de este cambio:**

- El armado dinámico del menú por rol (`MenuNegocio.ObtenerArbol`) y el dropdown de cuenta en el
  navbar: es funcionalidad de seguridad ya probada (52 verificaciones e2e) y exigida por
  `CLAUDE.md`, no es un tema de estilo.
- La asignación dinámica de `CssClass = "alert alert-success/alert-danger"` en el code-behind de
  `CambiarClave`, `RecuperarClave`, `RestablecerClave` y `Usuarios`: es la señal de éxito/error de
  una operación, no decoración, y tocarla es cambiar lógica en `.cs`.
- Los validadores de formulario (`RequiredFieldValidator`, `CompareValidator`): quedan tal cual
  están — ver pendiente en la sección 4.

### 2026-08-17 — Fase 1: login, roles, menú, ABM de usuarios y capa de datos

#### Corrección previa al desarrollo

La referencia entre proyectos estaba invertida: `BIZ.csproj` referenciaba al proyecto web.
Con eso el web no podía usar `BIZ` sin generar una referencia circular, es decir, no se podía
arrancar. Se invirtió a **Web → BIZ**, que es lo que piden los requerimientos.

#### Base de datos (`Database/`)

| Script | Contenido |
|---|---|
| `01_Esquema.sql` | Las 21 entidades del E/R + `MenuNivel` (tabla de relación menú↔rol). Idempotente: borra y recrea, **pierde los datos**. |
| `02_DatosIniciales.sql` | 3 roles, árbol de menú con permisos por rol, usuario administrador. |
| `03_UsuariosDePrueba.sql` | Opcional: un Encargado y un Empleado para probar permisos a mano. |

Corriendo sobre **LocalDB** (`(localdb)\MSSQLLocalDB`, base `LubricentroControl`). Pasar al
SQL Server del lubricentro por VPN Radmin es solo cambiar la cadena `LubricentroDB` en
`Web.config`; los scripts corren igual.

#### Capa BIZ

- `Modelo/` — `Usuario`, `Nivel`, `Url`, `ItemMenu`, `RecuperacionClave`.
- `Data/` — `AccesoDatos` (cadena de conexión centralizada, `Consultar`/`Ejecutar`/`Escalar`,
  helpers de mapeo de `DataRow`) más un DAL por entidad. Todo el SQL va parametrizado.
- `Negocio/` — `PasswordHasher` (PBKDF2-SHA256, 25.000 iteraciones), `SeguridadNegocio`,
  `UsuarioNegocio`, `MenuNegocio`, `ServicioMail`, `ResultadoOperacion`.

Se eliminó el `Class1.cs` del template.

#### Capa web

Pantallas reales: `Login`, `RecuperarClave`, `RestablecerClave`, `CambiarClave`, `Usuarios`,
`AccesoDenegado`, `Default`.
Pantallas cascarón: las 15 de negocio.

Infraestructura de seguridad en `Seguridad/`: `SesionUsuario` (único punto que toca `Session`),
`PaginaConSesion` (exige login) y `PaginaSegura` (exige además permiso de menú sobre la ruta).

Se eliminó el andamiaje del template: `About.aspx`, `Contact.aspx`, `Site.Mobile.Master` y
`ViewSwitcher.ascx`. Todo cuelga de `Site.Master`.

#### Bug corregido durante el desarrollo

`Response.Redirect(url, false)` seguido de `CompleteRequest()` en `OnPreInit` **no corta el
ciclo de vida de la página**: `Page_Load` se ejecutaba igual, sin usuario en sesión, y reventaba
con `NullReferenceException`. Se pasó a `Response.Redirect(url, true)`.

#### Mensajes de fase quitados de la interfaz

La aplicación ya no le cuenta al usuario en qué fase de desarrollo está el proyecto.

- Las 15 pantallas cascarón dicen ahora simplemente **«Pendiente»**.
- Se quitaron los 15 comentarios `/// Cascarón de la Fase N…` de los code-behind.
- Se eliminó de `Default.aspx` la tarjeta «Estado del sistema» completa, junto con el
  diagnóstico de conexión que vivía adentro (opción 1 de las tres que estaban planteadas).

El método `AccesoDatos.ProbarConexion(out mensaje)` **se conservó** en la capa de datos aunque
ya no lo llame ninguna pantalla: es el diagnóstico que va a hacer falta el día que se conmute a
la VPN Radmin. Volver a exponerlo es agregar una pantalla que lo invoque.

#### Textos mal codificados corregidos

Eran dos causas independientes con el mismo síntoma. Las dos quedaron arregladas.

**Causa A — datos corruptos en la base.** `sqlcmd -i` leía los `.sql` (guardados UTF-8 sin BOM)
con el codepage ANSI del sistema, así que «Vehículos» entró a la base ya corrompido como
`VehÃ­culos`. Se guardaron los tres scripts con BOM y se volvió a sembrar con `sqlcmd -f 65001`.
Verificado leyendo los codepoints de la columna: ahora `í=237` y `Ó=211`, un carácter cada uno,
donde antes había `195,173`.

**Causa B — markup parseado con el codepage equivocado.** ASP.NET lee los `.aspx`/`.master` con
el codepage del sistema si el archivo no tiene BOM y no hay `<globalization fileEncoding>`. Ocho
archivos estaban así, y el pie de página mostraba `ProgramaciÃ³n Avanzada`. Se agregó
`<globalization fileEncoding="utf-8" requestEncoding="utf-8" responseEncoding="utf-8" />` a
`Web.config` **y** se les puso BOM a los ocho. Ahora renderiza `Programación Avanzada 2026`.

Los `.cs` nunca estuvieron afectados: el compilador de C# asume UTF-8 cuando no hay BOM.

#### Verificación

- Rebuild limpio de la solución.
- `aspnet_compiler` sobre todo el proyecto (MSBuild **no** valida el markup `.aspx`).
- Dos suites end-to-end contra IIS Express, **52 verificaciones, todas en verde**: redirección
  de anónimos, login válido e inválido, menú por rol, las 17 pantallas respondiendo, bloqueo del
  Empleado en Usuarios y Reportes, modo solo-consulta, circuito completo de recuperación de
  contraseña, y el ABM con sus validaciones. Las suites incluyen ahora una comprobación de que
  Inicio no expone la fase de desarrollo y otra de que los acentos del menú renderizan bien.
- Comprobado que ya no queda ninguna mención a «Fase N», «roadmap» ni «Cascarón» en el código
  de la capa web, y que todos los `.aspx`, `.master` y `.sql` tienen BOM.

---

## 3. Planificado para la próxima sesión

**Fase 2 — ABM de entidades maestras.** Las cinco pantallas son independientes entre sí, ya
tienen su tabla creada y su cascarón enlazado en el menú, así que se pueden encarar en paralelo:

- Clientes (con sus vehículos asociados)
- Vehículos
- Proveedores
- Insumos (catálogo y stock inicial)
- Servicios (catálogo y precio base)

Cada una necesita su entidad en `BIZ/Modelo`, su DAL en `BIZ/Data` siguiendo el patrón de
`UsuarioDAL`, sus reglas en `BIZ/Negocio` devolviendo `ResultadoOperacion`, y la pantalla
heredando de `PaginaSegura`. Las que tienen modo consulta para el rol Empleado (Insumos,
Proveedores, Servicios) deben deshabilitar sus acciones de escritura cuando `EsSoloLectura`
es verdadero.

Además, validaciones de formulario: campos obligatorios y formato de mail, DNI, CUIT y patente.

### Repaso de redacción, pendiente

Con la codificación ya arreglada, queda revisar los textos visibles de punta a punta:
consistencia del voseo (hoy se mezcla «Ingresá» con formas neutras) y los rótulos abreviados del
menú frente a los títulos de cada pantalla («Cta. cte. clientes» vs. «Cuenta corriente de
clientes»). La abreviatura en el menú es deliberada por espacio en la barra de navegación; lo que
falta es decidir si se unifica el criterio.

---

## 4. Pendientes conocidos, sin fecha

Cosas que hay que resolver antes de la entrega, anotadas para no perderlas:

- **Cambiar la contraseña del administrador.** Hoy es la sembrada por el script (`Admin123!`).
- **Borrar los usuarios de prueba** (`encargado@lubricentro.com`, `empleado@lubricentro.com`)
  y el script `03_UsuariosDePrueba.sql` de la entrega final.
- **Conmutar a la VPN Radmin:** cambiar la cadena `LubricentroDB` en `Web.config`.
- **Salida real de mails:** hoy `MailModoDesarrollo=true` escribe los mails como archivos `.eml`
  en `App_Data\MailsEnviados` en vez de enviarlos. Para producción hay que ponerlo en `false` y
  configurar `<system.net>/<mailSettings>`.
- **`customErrors`:** con `debug="true"` y sin `customErrors`, un error muestra el stack trace
  completo en pantalla. Antes de entregar conviene una página de error propia.
- **El esquema de negocio nunca se ejerció.** Las 16 tablas de negocio están creadas pero
  ninguna se usó todavía: es esperable que en la Fase 2 aparezcan ajustes de tipos o de
  restricciones al escribir los primeros ABM.
- **Reevaluar el uso de estilos Bootstrap más elaborados** (cards, badges, tablas con clases,
  `form-control`/`form-select`, layout centrado). Se simplificaron a propósito todas las
  pantallas reales a HTML sin esas clases (ver sesión 2026-08-17 — Simplificación de estilos)
  para no anticipar estilo antes de confirmar la lógica de negocio. Evaluar si reintroducirlos
  una vez que cada pantalla esté probada — candidato natural: al cerrar la Fase 2.
- **Definir tratamiento uniforme de las validaciones de formulario.** Hoy cada pantalla usa
  `RequiredFieldValidator`/`CompareValidator` de ASP.NET tal cual, sin unificar mensajes ni
  estilo, y sin decidir si conviene sumar validación adicional del lado del servidor en
  `BIZ/Negocio`. Quedan como están por ahora; evaluar el criterio antes de escribir los ABMs de
  Fase 2.
