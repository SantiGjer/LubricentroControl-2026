# Estado actual del sistema

**Proyecto:** LubricentroControl 2026 · Programación Avanzada — USAL
**Última actualización:** 7 de septiembre de 2026 (tarde)

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

**Fase 1 completa. Fase 2 en curso: Clientes, Vehículos y Proveedores terminados, faltan
Insumos y Servicios.**

| Fase | Contenido | Estado |
|---|---|:---:|
| 1 | Login, roles, menú dinámico, ABM de usuarios, capa de datos | ✅ Completa |
| 2 | ABM de Clientes, Vehículos, Proveedores, Insumos, Servicios | 🟨 En curso (3/5) |
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
- **ABM de Clientes**: alta/baja lógica/edición, búsqueda por nombre/apellido/DNI, validación de
  formato de DNI. Layout de dos columnas (buscador+grilla a la izquierda, formulario siempre
  visible a la derecha).
- **ABM de Vehículos**: alta/baja lógica/edición, búsqueda por patente/marca/modelo, selector de
  dueño con buscador desplegable (reutiliza el buscador de Clientes, no un `DropDownList` con
  todos los clientes), validación de formato de patente, dropdown fijo de tipo de combustible.
- **"Nuevo cliente" desde Vehículos**: si al cargar un vehículo el dueño todavía no existe como
  cliente, se puede crear sin perder los datos del vehículo ya tipeados; al crearlo se vuelve
  automáticamente con ese cliente ya seleccionado como dueño.
- **ABM de Proveedores**: alta/baja lógica/edición, búsqueda por razón social o CUIT (tolera
  guiones en ambos sentidos), CUIT formateado en la grilla. Primera pantalla real en modo
  **solo consulta para el rol Empleado**: se le esconde el formulario entero y la columna de
  acciones de la grilla, no solo los botones — solo ve el buscador y los resultados.

### Qué NO funciona todavía

Quedan 12 pantallas de negocio como **cascarones** (Insumos, Servicios, Turnos, Órdenes, Compras,
Ventas, Pagos, las dos cuentas corrientes y los tres reportes): existen, están enlazadas desde el
menú y respetan los permisos por rol, pero no tienen funcionalidad.

---

## 2. Historial de sesiones

### 2026-09-07 (tarde) — Proveedores implementado; primer modo solo-consulta real

`BIZ/Modelo/Proveedor.cs` + `BIZ/Data/ProveedorDAL.cs` (patrón `UsuarioDAL`) y `Proveedores.aspx`
con el mismo layout de dos columnas de Clientes/Vehículos.

- **CUIT tolerante a guiones en los dos sentidos**: se puede cargar y buscar con o sin guiones
  (`ProveedorDAL` separa los dígitos del texto de búsqueda antes de armar el `LIKE`); se guarda
  siempre sin guiones y se muestra formateado (`Proveedor.FormatearCuit`) en la grilla.
- **Primera pantalla real en modo solo-consulta** (Empleado, según la matriz de permisos §5:
  Insumos/Proveedores/Compras son solo consulta para ese rol). Decisión explícita del usuario:
  no alcanza con deshabilitar los botones — se esconde el `Panel` del formulario entero
  (`pnlFormulario.Visible = false`) y la columna "Acciones" de la grilla
  (`gvProveedores.Columns[5].Visible = false`); el Empleado ve únicamente el buscador y los
  resultados. Los métodos de escritura del DAL (`Guardar`/`Borrar`/`RowCommand`) además chequean
  `EsSoloLectura` y rechazan la operación aunque alguien fuerce el request — mismo criterio que ya
  usa `PaginaSegura` para el menú. **Este es el patrón a repetir en Insumos y Servicios**, que
  tienen la misma restricción para Empleado.
- Se agregó `Proveedor.EsEmailValido` con su propio `CustomValidator` en el formulario — a
  diferencia de `Usuario`/`Cliente`, que validan el formato de mail en `Validar()` pero nunca
  tuvieron el `CustomValidator` correspondiente en el `.aspx` (gap identificado, no corregido
  retroactivamente ahí por quedar fuera de alcance de esa sesión).

**Verificación:** rebuild limpio + `aspnet_compiler`. Probado contra IIS Express: alta con CUIT
con guiones, búsqueda con y sin guiones, búsqueda por razón social, CUIT duplicado y formato
inválido rechazados, editar/borrar como Admin, acceso completo confirmado para Encargado, y el
modo solo-consulta confirmado para Empleado (sin formulario, sin columna de acciones, grilla
visible).

### 2026-09-07 — Clientes y Vehículos implementados (Fase 2)

Se escribieron las dos primeras pantallas de Fase 2 sobre el diseño confirmado el 2026-09-06.

**Capa BIZ:** `Modelo/Cliente.cs` y `Modelo/Vehiculo.cs` (con `Validar()`, `EsDniValido()`/
`EsPatenteValida()` estáticos, y `Vehiculo.TiposCombustible` como fuente única para el
`DropDownList` y su validación). `Data/ClienteDAL.cs` y `Data/VehiculoDAL.cs` siguiendo el
patrón de `UsuarioDAL`: `Listar`, `Buscar`, `ObtenerPorId`, `Existe*` (unicidad), `Crear`,
`Actualizar`, `Desactivar` (baja lógica). `VehiculoDAL` suma `ListarPorCliente` (para un futuro
botón "Ver vehículos" en Clientes, todavía no construido).

**Pantallas**, layout de dos columnas (buscador+grilla izquierda, formulario siempre visible
derecha, sin mostrar/ocultar):

- `Clientes.aspx`: sin checkbox "Activo" en el formulario — un botón **"Borrar"** aparece solo
  al seleccionar un cliente activo existente y hace la baja lógica directo desde ahí. Buscador
  con checkbox "Incluir inactivos" (`AutoPostBack`, sin necesitar tocar "Buscar").
- `Vehiculos.aspx`: mismo patrón. El campo "Dueño" es un buscador desplegable dentro de un
  `UpdatePanel` (busca clientes activos por nombre/apellido/DNI, lista resultados, seleccionar
  uno cierra la lista) en vez de un `DropDownList` con todos los clientes — decisión del
  2026-09-06, no escala igual que el de roles de `Usuarios`.

**Bug encontrado y corregido en el camino:** al reemplazar el checkbox "Activo" por un
`HiddenField`, `bool.Parse(hdnActivo.Value)` (y el `int.Parse` equivalente sobre IDs ocultos)
rompía con un error 500 si ese campo llegaba vacío o manipulado (ej. un POST armado a mano). Se
resolvió con `TryParse` + valor por defecto seguro, aplicado de entrada en `Vehiculos.aspx.cs` y
retroactivamente en `Clientes.aspx.cs`.

**"Nuevo cliente" desde Vehículos, y el problema real que apareció:** el botón (ubicado al lado
del buscador de clientes dentro del formulario, no del buscador de vehículos) manda a
`Clientes.aspx` con los datos del vehículo en curso; al crear el cliente ahí, se vuelve
automático a `Vehiculos.aspx` con esos datos repuestos y el cliente nuevo ya seleccionado como
dueño (o, si se cancela con "Volver sin crear", con el dueño que ya estaba elegido antes).

Se intentó primero con el mecanismo clásico de ASP.NET para esto — `PostBackUrl` +
`PreviousPage`, que es la forma estándar de pasar datos entre páginas vía ViewState —, pero
**no funciona en este proyecto**: `PreviousPage` reconstruye la página de origen a partir de la
ruta con la que se accedió, y como acá todo se navega con FriendlyUrls (`/Vehiculos`, sin
extensión), `BuildManager` no encuentra ningún archivo en esa ruta y tira
`HttpException: El archivo '/Vehiculos' no existe`. No hay manera de arreglarlo solo cambiando
el `RedirectMode` de FriendlyUrls: el error es por cómo `PreviousPage` resuelve la ruta, no por
la redirección en sí. Se resolvió con `Response.Redirect` pasando los datos por query string en
las dos direcciones — mismo resultado para el usuario, sin pelearse con el ruteo. Ver
`CLAUDE.md` («Cross-page posting no funciona con FriendlyUrls»).

Como el botón vive dentro de un `UpdatePanel` (es parte del buscador de dueño), hizo falta
declararlo como `PostBackTrigger` explícito: un trigger async normal no deja que
`Response.Redirect` navegue de verdad.

**Verificación:** rebuild limpio + `aspnet_compiler` sin errores. Probado a mano contra IIS
Express con requests HTTP (sin navegador disponible en la sesión, así que la experiencia real
del `UpdatePanel`/desplegable sin recargar página no se confirmó visualmente, solo la lógica de
servidor): alta/edición/baja de Cliente y Vehículo, unicidad de DNI y patente, formato de DNI y
patente rechazado correctamente, acceso completo del rol Empleado, y el circuito completo de
"Nuevo cliente" (ida con datos preservados, alta y vuelta automática con dueño seleccionado,
"volver sin crear" preservando el dueño original, guardado final del vehículo).

### 2026-09-06 — Diseño de Clientes y Vehículos para arrancar Fase 2

Antes de escribir código se cerró el diseño de las dos primeras pantallas de Fase 2. Decisiones
completas en `Docs/Lubricentro_Requerimientos.md` §9.2:

- `Clientes` y `Vehiculos` quedan como **dos pantallas separadas** (no maestro-detalle), tal como
  ya estaban en el menú.
- La búsqueda rápida del §6.2 se resuelve **partida en dos**: nombre/apellido/DNI en `Clientes`,
  patente/marca/modelo en `Vehiculos`. Cada pantalla tiene un botón por fila para saltar a la
  otra ya filtrada.
- El buscador de clientes se reutiliza como **selector de dueño** al dar de alta un Vehículo
  (no un `DropDownList` con todos los clientes — no escala).
- Tipo de combustible: `DropDownList` fijo — Nafta, Diésel, GNC, Eléctrico, Híbrido.

Según la matriz de permisos (§5), estas dos pantallas tienen acceso completo para los 3 roles:
a diferencia de Insumos/Proveedores/Compras/Ctas. ctes., acá `EsSoloLectura` nunca da `true`, no
hace falta lógica de deshabilitado de campos.

**Sigue:** escribir `BIZ/Modelo/Cliente.cs` y `Vehiculo.cs`, `BIZ/Data/ClienteDAL.cs` y
`VehiculoDAL.cs` (patrón `UsuarioDAL`), y recién después las pantallas.

### 2026-09-06 — Patrón de validación de formularios confirmado

Se resolvió el pendiente sobre criterio de validaciones de formulario (sección 4). Se evaluaron
tres opciones: (1) dejar el patrón actual, donde formato/unicidad solo se valida server-side y se
muestra con el banner genérico; (2) `RegularExpressionValidator` client-side, duplicando la regex
entre el `.aspx` y el `Modelo`; (3) `CustomValidator` con `OnServerValidate` en el code-behind,
que llama a un método estático del `Modelo` (sin duplicar la regex, pero sin feedback antes del
postback). **Se eligió la opción 3.** Detalle y ejemplo de código en `CLAUDE.md` («Patrón de
validación de formularios»).

Con esto, DNI/CUIT/patente/mail se validan igual en todos los ABM nuevos, con error inline junto
al campo (mismo estilo visual que `RequiredFieldValidator`) en vez del banner genérico que usa
hoy `Usuario.Validar()`. Las reglas que necesitan ir a la base (unicidad, no quedarse sin admins)
quedan fuera de este patrón y se siguen resolviendo como hasta ahora.

### 2026-09-06 — Formato de DNI, CUIT y patente confirmado

Se resolvió el supuesto pendiente sobre formato de estos tres campos, necesario para escribir
las validaciones de Cliente, Proveedor y Vehiculo en Fase 2 (quedaba anotado sin decidir en la
sección 3 de este documento). Decisión completa y regex de referencia en
`Docs/Lubricentro_Requerimientos.md` §9.1 y `CLAUDE.md` («Formato de DNI, CUIT y patente»):

- DNI: 7 u 8 dígitos, sin puntos.
- CUIT: se guarda sin guiones, se muestra en pantalla con guiones (`NN-NNNNNNNN-N`).
- Patente: acepta los dos formatos vigentes (viejo y Mercosur).

### 2026-09-06 — Se desarma la capa `Negocio/` como capa separada

El diseño de 3 capas dentro de `BIZ` (`Modelo`/`Data`/`Negocio`) venía generando una clase
`XNegocio` casi en espejo de cada clase `XDAL`, sin aportar una separación real: las reglas de
negocio (validar antes de guardar, no quedarse sin ningún admin activo, mail duplicado, mensajes
genéricos anti-enumeración de cuentas) terminaban llamando a un método del DAL casi homónimo. Se
decidió fusionar ambas responsabilidades en una sola clase por entidad, dentro de `Data/`.

Cambios:

- Se eliminaron `BIZ/Negocio/UsuarioNegocio.cs`, `SeguridadNegocio.cs` y `MenuNegocio.cs`. Su
  lógica pasó a `UsuarioDAL`, `RecuperacionClaveDAL` y `MenuDAL` respectivamente (incluye
  `Autenticar`, `Crear`, `Actualizar`, `Desactivar`, `BlanquearPassword`, `CambiarPassword`,
  `SolicitarRecuperacion`, `ValidarToken`, `RestablecerPassword`, `ObtenerArbol`).
- `ResultadoOperacion.cs` se movió de `Negocio/` a `Modelo/` (es un tipo de dato, no una regla).
- `ServicioMail.cs` se movió de `Negocio/` a `Data/` (es I/O externo — envío de mail —, no una
  regla de negocio).
- Se agregaron validaciones nuevas que antes no existían separadas: `Usuario.Validar()` y
  `Usuario.ValidarPassword()` (Modelo), y `ItemMenu.ArmarArbol()` (Modelo, antes vivía en
  `MenuNegocio`).
- `BIZ/Negocio/` quedó con una sola clase: `PasswordHasher.cs`.
- Se reescribieron los comentarios XML-doc (`/// <summary>`) como comentarios de línea (`//`) en
  todos los archivos tocados — cambio de estilo, sin efecto funcional.

Todos los code-behind que llamaban a las clases `*Negocio` se actualizaron para llamar a las
clases `Data` correspondientes. No queda ninguna referencia a `UsuarioNegocio`, `SeguridadNegocio`
ni `MenuNegocio` en el código (verificado con búsqueda en todo el repo).

**Verificación:** rebuild limpio de la solución (`MSBuild /p:Configuration=Debug`) y
`aspnet_compiler` sobre el markup, ambos sin errores. Además, se levantó IIS Express standalone
contra LocalDB y se probó a mano contra el código ya reorganizado:

- Login válido (`admin@lubricentro.com`) y login inválido (mensaje de error genérico, sin
  excepción).
- Acceso anónimo a `/Usuarios` redirige a `/Login?ReturnUrl=...` (guarda de `PaginaConSesion`).
- Menú armado por rol vía `MenuDAL.ObtenerArbol` → `ItemMenu.ArmarArbol`: como Admin aparecen
  `Usuarios` y `Reportes`; logueado como el Empleado de prueba (`empleado@lubricentro.com` /
  `Empleado123!`) esas opciones no están, y entrar a `/Usuarios` escribiendo la URL a mano
  redirige a `/AccesoDenegado` (la guarda de `PaginaSegura` corre server-side, no alcanza con
  esconder el link).
- ABM de Usuarios: la grilla carga y lista al admin sembrado.
- `CambiarClave`: el formulario carga.
- Circuito de recuperación de contraseña completo: `RecuperarClave` genera el `.eml` en
  `App_Data\MailsEnviados` con el enlace y token; `RestablecerClave?token=...` valida un token
  real (muestra el formulario) y rechaza uno inventado (muestra el error). No se llegó a
  confirmar el submit final del cambio de contraseña para no invalidar la clave sembrada del
  admin que se usa a diario en desarrollo.
- Sin mojibake en ninguna respuesta (`Administrador del Sistema`, acentos del menú, etc.).

No se re-ejecutaron las 52 verificaciones automatizadas de la sesión 2026-08-17 (no hay arnés de
tests en la solución, eran manuales); lo de arriba las reemplaza como evidencia de que el
refactor no rompió nada observable.

**Regla para Fase 2 en adelante:** no recrear `Negocio/`. El patrón es una clase por entidad en
`Data/` que hace acceso a datos y valida sus propias reglas, devolviendo `ResultadoOperacion`.

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

- Clientes (pantalla propia; el vínculo con sus vehículos es por navegación cruzada, ver diseño
  confirmado arriba, no maestro-detalle)
- Vehículos (pantalla propia; usa el buscador de Clientes como selector de dueño)
- Proveedores
- Insumos (catálogo y stock inicial)
- Servicios (catálogo y precio base)

Cada una necesita su entidad en `BIZ/Modelo`, y su DAL en `BIZ/Data` siguiendo el patrón de
`UsuarioDAL` (acceso a datos y reglas de negocio juntos, devolviendo `ResultadoOperacion` — ver
sesión 2026-09-06), y la pantalla heredando de `PaginaSegura`. Las que tienen modo consulta para el rol Empleado (Insumos,
Proveedores, Servicios) deben deshabilitar sus acciones de escritura cuando `EsSoloLectura`
es verdadero.

Además, validaciones de formulario: campos obligatorios, formato de mail, y formato de DNI, CUIT
y patente según lo confirmado arriba.

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
