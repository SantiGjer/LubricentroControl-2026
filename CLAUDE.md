# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Qué es este proyecto

Sistema de gestión para un Lubricentro (clientes, vehículos, turnos, órdenes de trabajo,
proveedores, insumos, compras, ventas, pagos, cuentas corrientes y reportes), con control de
acceso por roles. TP de Programación Avanzada 2026 — USAL.

**Estado real del código: Fase 1 a Fase 5 terminadas, Fase 6 en curso** (integración, pruebas y
pulido — primer frente arrancado el 27/09: pruebas de flujo completo, ver Historial de decisiones
más abajo). Andan el login, la
recuperación de contraseña por mail, el ABM de usuarios, el menú dinámico por rol, la capa
`BIZ/Data` de punta a punta contra SQL Server, los 5 ABM de Fase 2 (**Clientes**, **Vehículos**,
**Proveedores**, **Insumos** con kardex de stock, y **Servicios** — estos dos, fusionados en
**Productos** desde el 2026-10-07), las 2 pantallas de Fase 3
(**Turnos** y **Órdenes de trabajo**), y las 5 de Fase 4: **Compras** (alta con líneas armadas en
memoria y guardadas todas juntas, suma stock automáticamente, condición de pago Contado/Cuenta
corriente), **Cuenta corriente de Proveedores** y **Cuenta corriente de Clientes** (historial +
saldo + ajuste manual, calcadas entre sí), **Ventas** (de solo lectura: el comprobante se genera
automáticamente al cerrar una orden de trabajo, botón nuevo `btnCerrarOrden` en Órdenes), y
**Pagos** (de cliente o de proveedor, imputado a un comprobante puntual o "a cuenta general").
Las 21 tablas del diagrama original ya existen (`Database\01_Esquema.sql`), más `MovimientoStock`
(kardex de stock, agregada en Fase 2 — ver §9.3 de los Requerimientos), `Producto` y `Factura`
(2026-10-07, §9.9 y §9.10), `Emisor` (datos del comercio que factura, 2026-10-08), `ImagenProducto`
(la imagen opcional de cada producto, 2026-10-08, §9.13) y dos columnas agregadas
en Fase 4 (`ComprobanteCompra.medioPago`, `CuentaCorrienteCliente`/`Proveedor.idUsuario` — ver
§9.5). **Los tres reportes de Fase 5 ya están hechos**: Stock bajo, Ventas por período (sobre
`ComprobanteVentaDAL.ListarPorPeriodo`) y Cuentas corrientes (sobre
`CuentaCorrienteCliente`/`ProveedorDAL.ListarSaldos`, dos métodos nuevos) — ninguno de los tres
necesitó cambio de esquema.

**Segundo pedido del 2026-10-07 (13 puntos, ver Requerimientos §9.7 a §9.12):** barra lateral
con el menú; opciones de filtro en botones, columnas elegibles y "Ver" en todas las listas;
**Servicios e Insumos pasaron a ser subcategorías de `Producto`** (una sola pantalla,
`~/Productos`), con SKU, código de barras e IVA; **datos fiscales del cliente** (persona o empresa,
tipo de identificación, condición frente al IVA, domicilio completo); **factura imprimible** desde
Ventas; **cambio de dueño** de un vehículo; los turnos de hoy primero; el cierre de una orden de un
cliente con cuenta corriente pregunta si el saldo va a la cuenta o se cobra; y **roles y permisos
editables** (`~/Roles`).

Fuera de las 6 fases del Roadmap se agregó un **cuarto rol, Lectura** (`Nivel.Lectura = 4`,
jerarquía por debajo de Empleado): solo consulta en absolutamente todas las pantallas de negocio
(incluidas las que a Empleado le dan alta/edición completa — Clientes, Vehículos, Turnos, Órdenes,
Ventas, Pagos), sin ningún acceso a Usuarios ni a Reportes. Se sumó también **`~/Registro`**, una
pantalla pública de alta de usuario (link "Crear cuenta nueva" desde `Login.aspx`) que crea la
cuenta con ese rol Lectura siempre — es la única forma de que un visitante sin cuenta entre al
sistema por su cuenta, sin pasar por el ABM de Usuarios de un Admin. Ver §9.6 de los
Requerimientos y la entrada "Rol Lectura y alta pública de usuarios" del Historial de decisiones
más abajo.

Documentos de referencia (leer antes de diseñar algo del dominio):

- `Docs/Lubricentro_Requerimientos.md` — alcance, matriz de permisos por rol, las 21 entidades
  (+ `MovimientoStock`, `Producto` y `Factura`), reglas de negocio, qué quedó explícitamente fuera de alcance, y §9 con
  los supuestos/formatos ya confirmados en Fase 2/3/4 (DNI/CUIT/patente, diseño de
  Clientes/Vehículos, kardex de stock, estados de Turno/Orden, medio de pago de Compras) más el
  rol Lectura y `~/Registro` en §9.6, fuera de las 6 fases, y §9.7 a §9.12 (cuenta corriente
  opcional, datos fiscales, productos, IVA y factura, roles editables, cambio de dueño).
- `Docs/Lubricentro_Roadmap.md` — 6 fases de ejecución. **Fase 5 terminada** (Reportes — §6.9: los tres hechos); **falta Fase 6** (integración, pruebas
  y pulido — sin empezar). Los ABM de Fase 2, las pantallas de Fase 3 y las
  de Fase 4 quedan como referencia de patrón — Proveedores/Productos para el modo
  solo-consulta, Clientes/Vehículos para la lista con formulario en modal y el selector con
  búsqueda (desde 2026-10-07, ver «Formularios en modales y listas en el navegador» más abajo),
  Turnos/Órdenes para pantallas con cliente/vehículo fijo post-alta y (en Órdenes) franja de
  detalle con líneas. Compras suma un patrón nuevo: líneas armadas en memoria (`ViewState`) y
  guardadas todas juntas en un solo batch atómico, en vez de la franja progresiva de Órdenes —
  usarlo cuando la entidad se carga completa de una vez (como una factura), no progresivamente.
  Las dos Cuentas corrientes suman otro matiz de permisos: "solo consulta" para Empleado no
  esconde toda la pantalla (a diferencia de Proveedores/Productos/Compras), solo la franja de
  escritura — porque la pantalla ya es de consulta para todos los roles, lo único que cambia es
  si además puede escribir un ajuste. Ventas fue la primera pantalla puramente de solo lectura del
  proyecto y la primera vez que una pantalla de Fase 4 modificó código de una fase ya entregada
  (Órdenes de trabajo); desde el 2026-10-07 tiene una escritura, "Facturar", que sí respeta
  `EsSoloLectura`. Pagos usa los mismos selectores
  con búsqueda de cliente y de proveedor que el resto (`Utilidades/Selectores.cs`).

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
sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i "Database\04_DatosDemo.sql"          # opcional, datos de ejemplo
sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i "Database\05_ImagenesDemo.sql"       # opcional, imágenes de los productos de 04
```

Restore de paquetes: lo hace Visual Studio al abrir la solución. `dotnet restore` **no aplica**
acá (es `packages.config`) y `nuget.exe` no está instalado en esta máquina.

**No hay proyecto de tests** ni framework de testing configurado en la solución. No inventar un
comando de tests: la verificación es compilar, correr `aspnet_compiler` y probar la pantalla
levantando IIS Express.

## Al levantar el proyecto en otra máquina

Lo que clonar el repo no resuelve solo (relevado el 2026-10-08). Las decisiones que siguen
abiertas están en `Docs/EstadoActual.md`, sección 4.

- **Recrear la base con `01` a `04`.** El esquema cambió el 2026-10-07 (ver «Base de datos»): con
  una base anterior la app falla apenas se inicia sesión, porque el menú lee `Menu.icono`. Volvió
  a cambiar el 2026-10-08 (tabla `Emisor`): sin ella fallan "Facturar" y Datos del comercio, y la
  pantalla no aparece en el menú. Y otra vez el mismo día (tabla `ImagenProducto`): sin ella falla
  todo lo que lista productos (Productos, Inicio, Órdenes, Compras, Stock bajo). `01` borra los
  datos; `03`, `04` y `05` son opcionales (`04` usa al admin si faltan los usuarios de `03`; `05`
  carga las imágenes de los productos de `04`).
- **Los datos de ejemplo se fechan al correr `04`.** Turnos, órdenes, compras y pagos salen de
  `GETDATE()`, y dos turnos quedan para ese mismo día (10:30 y 17:00). Con la base recreada otro
  día, "hoy" sale vacío en Turnos y en el Inicio: no es un bug, se vuelven a correr `01` a `04`.
- **Mails: el `Web.config` commiteado trae `MailModoDesarrollo=false`** (envío real por Gmail),
  pero las credenciales van en `Web.MailSettings.config`, que está gitignoreado y no viene con el
  repo. Sin ese archivo el envío falla sin romper la pantalla: `ServicioMail.Enviar` devuelve el
  error, pero quien lo llama no lo muestra. El alta y el blanqueo de usuarios igual muestran la
  clave temporal en pantalla; el mail de recuperación de clave, en cambio, no sale y nadie se
  entera. Para que ande, copiar `Web.MailSettings.config.example` como `Web.MailSettings.config`,
  completarlo y poner la misma cuenta en `MailRemitente`; para probar sin correo,
  `MailModoDesarrollo=true` (los mails quedan en `App_Data\MailsEnviados`) sin commitearlo.
- **Los datos del comercio para la factura son de ejemplo**: razón social, CUIT, domicilio,
  ingresos brutos e inicio de actividades inventados, sembrados por `02_DatosIniciales.sql` en la
  tabla `Emisor`. Reemplazarlos por los reales antes de imprimir una factura, desde Administración
  > Datos del comercio (`~/DatosComercio`, solo Admin de entrada). Hasta el 2026-10-08 estaban en
  `Web.config` (`Emisor.*`), que ya no se lee. La condición frente al IVA define la letra
  (Responsable Inscripto emite A o B; Monotributista o Exento, C), y cada letra numera por
  separado en cada punto de venta.
- **La factura no tiene validez fiscal:** no lleva CAE ni se conecta con AFIP/ARCA, que quedó
  fuera de alcance (ver «Reglas de negocio que cruzan módulos»). No es algo a medio hacer.
- **Los números dependen de la configuración regional de Windows.** `<globalization>` no fija
  `culture`, así que cada máquina muestra y lee los importes con la suya: en inglés `3,500.00`,
  en español de Argentina `3.500,00`. En una misma máquina es coherente (lo que se carga al
  editar se vuelve a leer bien, y `Lubricentro.js` ordena con los separadores que le pasa
  `Site.Master`). Lo que se tipea en el formato de la otra configuración lo rechaza el
  `CompareValidator Type="Currency"` de cada campo, salvo en las líneas de Compras (cantidad y
  precio unitario), que no tienen validador: en una máquina en inglés `1500,50` se guarda como
  `150050`, sin aviso. Fijar `culture="es-AR" uiCulture="es-AR"` lo dejaría igual en todas las
  máquinas; está pendiente de decidir.

## Arquitectura

Dos proyectos en la solución:

| Proyecto | Rol |
|---|---|
| `LubricentroControl-2026` | Capa web: páginas `.aspx`. RootNamespace `LubricentroControl_2026` (guion **bajo**), assembly `LubricentroControl-2026` (guion medio) |
| `BIZ` | Biblioteca de clases: lógica de negocio **y** acceso a datos, juntos en el mismo proyecto |

Dentro de `BIZ` hay tres carpetas, **no proyectos aparte** (decisión explícita de los
requerimientos §4 — no partir `BIZ`):

- `Modelo/` — entidades (`Usuario`, `Nivel` + `PermisoPantalla`, `Url`, `ItemMenu`,
  `RecuperacionClave`, `ResultadoOperacion`, `Cliente`, `Vehiculo`, `Proveedor`, `Producto`,
  `MovimientoStock`, `Turno`, `OrdenDeTrabajo`, `DetalleOrdenServicio`, `DetalleOrdenInsumo`,
  `ComprobanteVenta`, `Factura` + `TotalesFactura` + `DatosEmisor`, etc.) y dos clases estáticas de
  reglas de formato: `FormatoTelefono` e `Iva` (tipos, alícuotas, condiciones frente al IVA y el
  cálculo del IVA contenido). `Imagen` es la imagen de un producto con sus reglas: qué archivo se
  acepta y cómo se achica y se arma la miniatura (`Imagen.Preparar`, con `System.Drawing`).
- `Data/` — el DAL **y las reglas de negocio**, juntos en la misma clase por entidad (ej.
  `UsuarioDAL`, `RecuperacionClaveDAL`, `MenuDAL`, `NivelDAL`, `ClienteDAL`, `VehiculoDAL`,
  `ProveedorDAL`, `ProductoDAL`, `MovimientoStockDAL`, `TurnoDAL`, `OrdenDeTrabajoDAL`,
  `DetalleOrdenServicioDAL`, `DetalleOrdenInsumoDAL`, `ComprobanteVentaDAL`, `FacturaDAL`,
  `EmisorDAL`). Todo pasa por
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
  **Una pantalla nueva debe deshabilitar sus acciones de escritura cuando vale true** — y desde que
  los permisos se editan en Roles (2026-10-07), **cualquier pantalla** puede quedar en consulta
  para cualquier rol, también las que hoy solo ve Admin. Patrón (ej. `Proveedores.aspx`): esconder
  el botón "Nuevo …" de la barra y el `Panel` del formulario entero
  (`pnlFormulario.Visible = !EsSoloLectura`, que es el contenido del modal), y en la grilla solo
  los enlaces de escritura (`Visible='<%# PuedeEscribir %>'`, propiedad de `PaginaSegura`): la
  columna "Acciones" queda porque "Ver" es para todos. Además cada método de escritura
  (`Guardar`/`Borrar`/`Reactivar`/`RowCommand`) chequea `EsSoloLectura` y corta al principio, por
  si alguien fuerza el request aunque el control esté escondido.
- **Formularios en modales y listas en el navegador (desde 2026-10-07).** Cada pantalla de gestión
  es una lista a todo el ancho con una `barra-herramientas` arriba (filtro, opciones, botón
  "Nuevo …"); alta, edición y detalle van en un modal de Bootstrap. Reglas del patrón:
  - El modal se abre desde el servidor después del postback que lo necesita (Nuevo, Editar, error
    al guardar) con `Utilidades/Interfaz.AbrirModal(this, "idModal")`. Los errores del DAL se
    muestran **adentro** del modal (`pnlErrorFormulario`, o `pnlMensajeFormulario` donde también
    hay avisos de éxito sin cerrar el modal); `pnlMensaje` de la página queda para el resultado
    final, con el modal cerrado. Cada `Page_Load` oculta los dos al principio: el Literal guarda
    su texto en el ViewState y si no repetiría el último aviso en cada postback.
  - Si el formulario tiene idas y vueltas sin cerrarse (elegir un titular que carga datos,
    agregar/quitar líneas), su cuerpo va en un `UpdatePanel`; los botones del pie quedan afuera
    y hacen postback completo para refrescar la lista.
  - Orden, filtro y paginado de las tablas los hace `Scripts/Lubricentro.js` sobre toda
    `table.tabla-abm`: el servidor trae la lista completa (sin `Buscar` en el DAL ni
    `AllowPaging` en el GridView). Atributos: `data-filtro="idInput"` en el GridView para usar el
    filtro de la barra, `data-sin-filtro` para grillas chicas, `data-filas-por-pagina="N"`,
    `data-buscar` en la fila (desde `RowDataBound`) para lo que se busca pero no es columna,
    `HeaderStyle-CssClass="sin-orden"` en la columna de Acciones y `data-estatica` para una tabla
    con el estilo pero sin orden (la matriz de Roles). El estado sobrevive a los postbacks en
    `hdnEstadoTablas` (Site.Master).
  - **Opciones de filtro (desde 2026-10-07):** un `<div class="opciones-tabla" id="opcionesX">`
    justo debajo de la barra, con grupos `.grupo-opciones` de botones `.opcion data-valor="…"`
    (`""` = todos, `"A|B"` = cualquiera de los dos), y `data-opciones-tabla="opcionesX"` en el
    GridView. Cada grupo filtra por una columna (`data-columna="Estado"`, por el texto del
    encabezado) o por un atributo de la fila (`data-atributo="saldo"` lee `data-saldo`, puesto en
    `RowDataBound`); `data-inicial` elige con qué arranca. Reemplazan a las casillas que
    filtraban en el servidor: no agregar filtros nuevos al DAL.
  - **Columnas y "Ver":** cada grilla trae todas las columnas útiles; las secundarias van con
    `HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta"` (escondidas de entrada; el botón
    "Columnas" las muestra y se recuerda en el navegador). "Ver" es `<a href="#" class="accion-ver"
    data-ver-detalle>Ver</a>` en Acciones: abre una ventana con todas las columnas de la fila;
    los enlaces de la fila con `CssClass="accion-detalle"` (Editar…) salen en su pie, y
    `ItemStyle-CssClass="celda-titulo"` hace clicable el nombre. Si el detalle necesita más que la
    fila (líneas de una orden o una venta), "Ver" es un `LinkButton` con `CssClass="accion-ver"`
    que abre un modal del servidor.
  - **Campos que dependen de otra elección:** `data-mostrar-si="idControl=Valor"` (o
    `"Valor1|Valor2"`) muestra el elemento solo con ese valor en el desplegable o
    `RadioButtonList` `idControl` (con `ClientIDMode="Static"`). Solo esconde: el servidor valida
    igual (ej. `CustomValidator` con `ValidateEmptyText` que mira la otra elección).
  - Para elegir cliente, dueño o proveedor: `.selector-busqueda` (TextBox `selector-texto` +
    botón + HiddenField con el id), con las opciones en `data-opciones="<%: OpcionesClientes %>"`
    (`Utilidades/Selectores.cs`). Con `data-postback="true"` elegir dispara el `OnValueChanged`
    del HiddenField (va adentro de un `UpdatePanel`).
- **Imágenes (desde 2026-10-08, hoy solo de productos).** El archivo va en la base
  (`ImagenProducto`, con su miniatura), no en una carpeta del sitio: la base es compartida y cada
  máquina corre su propio IIS. Las pantallas lo piden aparte a `ImagenProducto.ashx` (exige
  sesión; `ImagenProducto.Url` arma la dirección con la fecha de la imagen, para que el navegador
  la guarde). En el formulario, `.campo-imagen` con un `FileUpload` (`Lubricentro.js` muestra la
  vista previa y descarta sin subirlo lo que no es PNG ni JPG o pesa de más); la imagen se valida
  con `Imagen.Preparar` **antes** de guardar la entidad y se guarda después. Un `FileUpload` no
  sube nada en un postback parcial: si el formulario va en un `UpdatePanel`, el botón que guarda
  tiene que ser `PostBackTrigger`. En una grilla, una miniatura con `data-imagen` (la imagen
  entera) hace que "Ver" la muestre grande.
- La sesión se toca solo a través de `Seguridad/SesionUsuario.cs`, nunca `Session["..."]` directo.
- El menú es una **barra lateral** (desde 2026-10-07) que arma `Site.Master.cs` desde
  `MenuDAL.ObtenerArbol(idNivel)` (que a su vez arma el árbol con `ItemMenu.ArmarArbol`, en
  `Modelo/`, a partir de la lista plana de `MenuDAL.ListarPorNivel`). Las opciones de primer nivel
  llevan ícono (`Menu.icono`, dibujado en `Site.Master.cs`). Para agregar una pantalla al menú hay
  que insertar filas en `Url`, `Menu` y `MenuNivel` — ver el patrón en
  `Database\02_DatosIniciales.sql`, con los permisos de los roles de base; después se ajustan
  desde `~/Roles`, que reescribe `MenuNivel` del rol (pantallas **y** sus grupos: sin la fila del
  grupo, la pantalla no aparece). Una pantalla sin fila en `MenuNivel` es inaccesible para ese rol.
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

- **Servicio e Insumo son subtipos de `Producto`** (desde 2026-10-07): el `idServicio`/`idInsumo`
  de cada tabla es el `idProducto`, y las órdenes, compras, ventas y el kardex apuntan al subtipo.
  Nombre, precio, SKU, código de barras e IVA se leen de `Producto` (`ProductoDAL`); marca, unidad y
  stock, de `Insumo`. El tipo de un producto no cambia después del alta.
- **Los precios son finales, con el IVA incluido.** Al generar la venta, cada línea copia el IVA
  de su producto y guarda el IVA que contiene (`Iva.Contenido`); la venta guarda el neto en
  `subtotal` y el IVA en `impuestos`, y el total no cambia. La factura (`FacturaDAL.Emitir`, desde
  Ventas) toma la letra de la condición frente al IVA del comercio (tabla `Emisor`, `EmisorDAL`,
  editable en `~/DatosComercio`) y del cliente, copia los datos de los dos al emitirse (editarlos
  después no la cambia) y es sin validez fiscal (sin CAE).
- **Cambiar el dueño de un vehículo es `VehiculoDAL.CambiarDueno`**, no la edición (`Actualizar`
  ya no toca `idCliente`): se rechaza con una orden en el taller o un turno pendiente del vehículo.
- **Stock automático en los dos sentidos:** baja al agregar una línea de insumo a una orden de
  trabajo (`DetalleOrdenInsumoDAL.Agregar`, Fase 3, ya implementado), sube al registrar una compra
  a proveedor (`ComprobanteCompraDAL.Crear`, Fase 4). **Cancelar una orden de trabajo repone el stock
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
- **La cuenta corriente del cliente es opcional (`Cliente.cuentaCorriente`, desde 2026-10-07).**
  Sin ella, cerrar su orden en `OrdenesDeTrabajo.aspx` redirige a `~/Pagos?idVenta=N`, que abre
  el cobro con el cliente y el saldo de esa venta cargados (el monto se lee de la base, no del
  query string). Con ella, el cierre pregunta si el saldo queda en la cuenta o se cobra ahora
  (mismo redirect). La venta y su movimiento de cuenta corriente se generan igual para todos: el
  flag no cambia el circuito de `BIZ`, solo adónde va la pantalla. El interruptor lo cambia quien
  escribe en `~/CuentaCorrienteClientes` (Admin/Encargado), con la guarda en el servidor
  (`Clientes.aspx.cs`, `CuentaCorrienteElegida`).
- **Una orden `Cerrada` o `Cancelada` no admite tocar su detalle.** Agregar o quitar una línea de
  servicio/insumo sólo vale mientras `orden.Estado` esté en `OrdenDeTrabajo.EstadosEditables`
  (`Abierta`/`En proceso`) — `DetalleOrdenServicioDAL.Agregar`/`Quitar` y
  `DetalleOrdenInsumoDAL.Agregar` lo chequean ellos mismos, no sólo la UI (ver «Historial de
  decisiones», entrada de Fase 6).
- Roles de base **Admin > Encargado > Empleado > Lectura**, editables desde `~/Roles` (desde
  2026-10-07: se crean roles y se cambia el acceso por pantalla; Admin queda siempre completo y no
  se edita, Lectura no se borra). El código solo conoce dos ids: `Nivel.Admin` y `Nivel.Lectura`.
  El menú se arma dinámicamente según el rol del usuario logueado (entidades `Menu`, `Url`, `Nivel`). El rol Empleado tiene acceso
  restringido a compras y cuentas corrientes (solo consulta) y ninguno a reportes financieros ni a
  gestión de usuarios. El rol Lectura (agregado fuera del alcance original, ver §9.6) es solo
  consulta en **todo** lo que ve — mismas pantallas que Empleado, pero sin escritura en ninguna —
  y es el rol que recibe cualquier alta hecha desde `~/Registro`.
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

Fase 4 sumó dos columnas que tampoco estaban en el diagrama original (ver Requerimientos §9.5):
`ComprobanteCompra.medioPago` (nullable, atada a `condicionPago = Contado` por
`CK_Compra_medioPago_condicion`, mismo criterio que `CK_Pago_titular`/`CK_MovStock_origen`) y
`CuentaCorrienteCliente`/`Proveedor.idUsuario` (nullable, poblado solo en movimientos `Ajuste`).

`Cliente.cuentaCorriente` (`BIT NOT NULL`, default 0) se agregó el 2026-10-07: una base creada
antes no la tiene y `ClienteDAL` falla hasta recrearla con `01` a `04`.

El mismo día (segundo pedido) cambió más el esquema, así que **toda base anterior se recrea con
`01` a `04`**: `Producto` (supertipo, con índices únicos filtrados de SKU y código de barras) y
`Servicio`/`Insumo` como subtipos con FK compuesta `(id, tipo)`; `Cliente` con `tipoCliente`,
`razonSocial`, `tipoDocumento` + `numeroDocumento` (reemplazan a `dni`), `condicionIva`,
`localidad`, `provincia`, `codigoPostal` y la columna calculada `denominacion`;
`DetalleComprobanteVenta` con `tipoIva`, `alicuotaIva` e `importeIva`; `Factura`; y `Menu.icono`.
Los índices filtrados exigen `SET QUOTED_IDENTIFIER ON` (sqlcmd lo trae apagado): los scripts lo
fijan al principio; un script nuevo, o un `sqlcmd -Q` suelto, que escriba en `Producto` (insertar,
actualizar o borrar) tiene que hacer lo mismo o correr con `-I`.

El 2026-10-08 se sumó `Emisor`, los datos del comercio que factura: una tabla de **una sola fila**
(`idEmisor = 1`, fijado por `CK_Emisor_unico`) en vez de claves de `Web.config`, para poder
editarla desde la aplicación. `EmisorDAL.Guardar` hace `UPDATE` y, si no había fila, `INSERT`.

El mismo día se sumó `ImagenProducto`, la imagen opcional de cada producto: el archivo (`contenido`,
hasta 800 px de lado) y su `miniatura` de 120 px en `VARBINARY(MAX)`, con el tipo (`image/png` o
`image/jpeg`) y la fecha. Va aparte de `Producto` para que sus consultas no arrastren los archivos
(`ProductoDAL` solo lee la fecha). `05_ImagenesDemo.sql` carga las de los productos de ejemplo,
con cada binario partido en renglones con `\` al final (T-SQL une las partes).

Hoy apunta a **LocalDB** (`(localdb)\MSSQLLocalDB`, base `LubricentroControl`). Para pasar al
SQL Server del lubricentro por VPN Radmin alcanza con cambiar la cadena `LubricentroDB` en
`Web.config`; los scripts corren igual.

Usuario inicial que siembra `02_DatosIniciales.sql`: **admin@lubricentro.com / Admin123!**

Nota (2026-09-27): `04_DatosDemo.sql` corrido contra la LocalDB de Alexis (los 4 scripts, `01` a
`04`) — el circuito de 10 clientes/vehículos/proveedores/etc de ejemplo está cargado ahí. Es
LocalDB, por-usuario: no afecta la base de ningún otro compañero, cada uno decide si lo corre
en la suya.

## Contraseñas y mails

- El hash es **PBKDF2-SHA256, 25.000 iteraciones, 32 bytes**, salt por usuario, en
  `BIZ\Negocio\PasswordHasher.cs`. Cambiar cualquiera de esas constantes invalida todos los hashes
  existentes, incluido el del admin sembrado por SQL.
- Con `MailModoDesarrollo=true` en `Web.config` los mails **no salen por SMTP**: se escriben como
  `.txt` planos en `App_Data\MailsEnviados` (destinatario, asunto y cuerpo, sin codificar). Así se
  prueba el circuito de recuperación de clave sin servidor de correo ni cliente de mail — alcanza
  con abrir el archivo con cualquier editor de texto. Ojo: el `Web.config` commiteado hoy lo trae
  en `false` (ver «Al levantar el proyecto en otra máquina»).
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

**Estilo de las pantallas (desde 2026-10-06, ampliado el 2026-10-07).** Todas las pantallas con menú
ponen su contenido dentro de `<div class="pantalla-abm">` y usan las clases de `Content\Site.css`
(bloque "Pantallas de gestión (ABM)"): `barra-herramientas` (+ `filtro-tabla-texto` y
`acciones-barra`) seguida de `opciones-tabla` si hay opciones de filtro, `tabla-abm` (+
`tabla-compacta` si tiene muchas columnas), el modal (`modal-content` con
`modal-header`/`modal-body`/`modal-footer` y `acciones-secundarias` a la izquierda del pie),
`campos-formulario` + `campo` para la grilla de campos (+ `subtitulo-formulario` para separar
grupos de campos y `opciones-radio` para un `RadioButtonList` con forma de botones),
`selector-busqueda`, `panel-gris`, `boton-rojo`/`boton-gris`/`boton-borde-rojo`. Sin bordes
`border border-1`, sin las clases `table table-striped...` de Bootstrap en las grillas. El fondo gris
es el del `body` y la barra lateral la pone el `Site.Master` (`body.con-menu`), así que una pantalla
nueva solo necesita el contenedor. Las pantallas de ingreso (Login, Registro, Recuperar/Restablecer
clave) tienen su tarjeta propia (`.login-card`, clara desde 2026-10-07) y van sin barra lateral
(`body.sin-menu`). Las filas que el código resalta con un fondo en línea (stock bajo, saldo
pendiente) conservan ese fondo. El nombre de la aplicación en pantalla es **"Lubricentro
Control"**, separado (el proyecto, la base y el namespace siguen siendo `LubricentroControl`).

### Formato de DNI, CUIT y patente (Fase 2)

Decisión de negocio en `Docs/Lubricentro_Requerimientos.md` §9.1. Regex de referencia para los
validadores de Cliente, Proveedor y Vehiculo:

| Campo | Guardado | Regex | Ejemplo |
|---|---|---|---|
| `Cliente.numeroDocumento` (DNI, LE, LC) | solo dígitos | `^\d{7,8}$` | `12345678` |
| `Cliente.numeroDocumento` (CUIT, CUIL) | solo dígitos | `^\d{11}$` | guarda `20253334445`, muestra `20-25333444-5` |
| `Cliente.numeroDocumento` (Pasaporte) | mayúsculas | `^[A-Z0-9]{6,12}$` | `AAB123456` |
| `Proveedor.cuit` | sin guiones | `^\d{11}$` | guarda `20123456786`, muestra `20-12345678-6` |
| `Vehiculo.patente` | mayúsculas | `^([A-Z]{3}\d{3}|[A-Z]{2}\d{3}[A-Z]{2})$` | `ABC123` o `AB123CD` |

Desde el 2026-10-07 el documento del cliente va con su tipo (`Cliente.tipoDocumento`) y se acepta
con puntos o guiones (`Cliente.NormalizarNumeroDocumento` los saca); se muestra como
`Cliente.FormatearDocumento` ("DNI 30111222", "CUIT 20-25333444-5"). El CUIT/CUIL es el único que
necesita guiones para mostrar (posiciones 2 y 10 sobre los 11 dígitos); DNI y patente se muestran
igual que se guardan.

Desde 2026-10-07, además:

| Campo | Regla | Dónde vive |
|---|---|---|
| `Cliente.telefono`, `Proveedor.telefono` | opcional; se escribe con números, espacios, guiones, puntos, paréntesis y "+" inicial, 6 a 15 dígitos; se guarda y se muestra solo con los dígitos (y el "+"), desde 2026-10-08 | `Modelo/FormatoTelefono.cs` (`EsValido` y `Normalizar`; clase aparte: la propiedad `Telefono` de las entidades taparía el nombre) |
| `Vehiculo.anio` | opcional; de 1900 al año que viene | `Vehiculo.AnioMinimo`/`AnioMaximo`; la pantalla usa un `RangeValidator` con esos límites puestos desde el código |
| `Cliente.codigoPostal` | opcional; 4 dígitos o CPA (`C1406GZA`), en mayúsculas | `Cliente.EsCodigoPostalValido` |
| `Cliente.provincia` | opcional; una de las 23 provincias o CABA | `Cliente.Provincias` (lista fija del desplegable) |
| `Producto.sku`, `Producto.codigoBarras` | opcionales; letras, números y guiones; únicos; el SKU en mayúsculas | `Producto.EsCodigoValido` + `ProductoDAL` (unicidad) |
| `Producto.alicuotaIva` | 21, 10,5, 27, 5 o 2,5 si es gravado; 0 si es exento o no gravado | `Iva.Alicuotas`; en un desplegable, el valor va en formato invariante `"0.##"` (la base la devuelve como `10.50`) |
| Imagen de un producto (`ImagenProducto`, desde 2026-10-08) | opcional; PNG o JPG de hasta 5 MB, reconocido por sus primeros bytes y no por la extensión; se guarda en el mismo formato, a lo sumo de 800 px de lado, con una miniatura de 120 px | `Modelo/Imagen.cs` (`Preparar`); los límites llegan a la pantalla desde ahí |

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

  **Quitar una línea de insumo (sesión 2026-10-05)** es la inversa de `Agregar`
  (`DetalleOrdenInsumoDAL.Quitar`): mismo batch atómico de tres escrituras (reponer stock, entrada
  en el kardex, borrar la línea), por lo que es la segunda excepción a "`Registrar` es el único
  camino". Solo con la orden Abierta o En proceso (guarda en el DAL, no solo en la UI). El kardex usa
  `TipoCancelacionOrden` con la descripción distinguiendo el caso, para no tocar el CHECK
  `CK_MovStock_origen`.

  **Un estado que el servidor fuerza no se ofrece como opción en el alta.** `TurnoDAL.Crear` y
  `OrdenDeTrabajoDAL.Crear` imponen `Solicitado`/`Abierta`; la pantalla debe mostrarlo como texto fijo
  y dejar el desplegable solo para editar (Turnos lo hacía mal hasta 2026-10-05; Órdenes ya estaba
  bien). Mantener el `DropDownList` con sus opciones cargadas aunque esté oculto.

  **Walk-in con cliente y vehículo nuevos, sin salir de la pantalla.** Como `OrdenDeTrabajo.
  idVehiculo` es `NOT NULL`, se extendió el mecanismo de `Response.Redirect` + query string que ya
  conectaba `Vehiculos.aspx` ↔ `Clientes.aspx` (ver «Cross-page posting no funciona con
  FriendlyUrls» más abajo) agregando un tercer origen `"orden"` en paralelo al `"vehiculo"`
  existente — sin tocar esa lógica. Cualquier pantalla futura que necesite el mismo atajo de alta
  en cascada sigue este patrón: un origen nuevo, hidden fields propios (prefijo distinto, acá
  `hdnOr*`), nunca reescribir la rama existente.

- **Compras (Fase 4, sesión 2026-09-14).** Primera pantalla de Fase 4, sobre
  `ComprobanteCompra`/`DetalleCompra`/`CuentaCorrienteProveedor` (con sus DAL). Dos decisiones de
  producto confirmadas con el usuario antes de diseñar (detalle en `Docs/EstadoActual.md`): las
  cuentas corrientes van a llevar ajuste manual (todavía sin construir), y una compra "Contado"
  registra su medio de pago en la propia compra sin pasar por Pagos/Cuenta corriente — esto
  último exigió agregar `ComprobanteCompra.medioPago` al esquema (ver «Base de datos» arriba).

  **Patrón nuevo, distinto al de Órdenes: líneas en memoria (`ViewState`), no franja
  progresiva.** Una compra es la transcripción de una factura que ya llega completa (a diferencia
  de una orden de trabajo, donde el trabajo se descubre progresivamente) — tiene más sentido
  armar todas las líneas en pantalla y persistir todo junto con un solo "Guardar". La clase de
  Modelo que vive en `ViewState` mientras tanto (`DetalleCompra`) se marcó `[Serializable]`:
  primera vez que hace falta en el proyecto. Usar este patrón (no el de Órdenes) para cualquier
  pantalla futura donde la entidad llega completa de una sola vez.

  **Tercera aparición del bug de `DropDownList` sin `<option>`s — esta vez, falso positivo en las
  pruebas, no en la app.** Mismo síntoma que `ddlVehiculo`/`ddlEstado` (ver entrada de Turnos/
  Órdenes arriba), pero con causa distinta: acá `ddlMedioPago` sí tenía opciones cargadas — el
  problema fue que la prueba con `curl` posteó su valor mientras el `Panel` que lo contiene
  estaba oculto (`condicionPago = "Cuenta corriente"`), algo que un browser real nunca haría
  porque ese campo ni siquiera existe en el DOM en ese momento. Distinción a tener presente: el
  bug real es "el control nunca tuvo opciones ni siquiera cuando se necesitaba"; esto era "se
  posteó un campo que no correspondía a la vista actual" — un error de la prueba, no del código.

- **Ventas (Fase 4, sesión 2026-09-14).** Sobre `ComprobanteVenta`/`DetalleComprobanteVenta`/
  `CuentaCorrienteCliente` (con sus DAL). Primera pantalla de Fase 4 que modifica código de una
  fase ya entregada en vez de solo agregar algo nuevo: `OrdenDeTrabajoDAL.Cerrar` reemplaza el
  paso de `Cerrada` por el `ddlEstado` genérico (ver entrada de Turnos/Órdenes arriba —
  `Cancelar`/`Cerrar` comparten el mismo criterio: un estado con efecto colateral necesita su
  propio botón, no un dropdown). `Cerrar` genera la venta primero y recién si eso sale bien
  actualiza el estado — al revés dejaría una orden `Cerrada` sin venta si algo fallara en el
  medio, el peor de los dos escenarios.

  **`ComprobanteVentaDAL.GenerarDesdeOrden` no lleva `idUsuario`,** a diferencia de `Cancelar`:
  se evaluó por simetría, pero ni `ComprobanteVenta` ni el movimiento de `CuentaCorrienteCliente`
  que genera tienen dónde guardarlo (`idUsuario` en cuenta corriente solo se puebla en `Ajuste`,
  ver entrada de Compras) — se descartó el parámetro por no tener ningún uso real.

  Mismo mecanismo de batch atómico que `ComprobanteCompraDAL.Crear` (Compras), esta vez copiando
  las líneas de `DetalleOrdenServicio`/`DetalleOrdenInsumo` ya existentes en vez de recibirlas de
  un formulario. Guarda de idempotencia en C# (no hay `UNIQUE` en `ComprobanteVenta.idOrden`):
  `GenerarDesdeOrden` rechaza si la orden ya tiene una venta generada.

  `Ventas.aspx` es la primera pantalla puramente de solo lectura del proyecto — ni siquiera tiene
  el concepto de `EsSoloLectura` por rol, porque no hay ninguna escritura que restringir para
  nadie (acceso completo para los 3 roles).

- **Pagos (Fase 4, sesión 2026-09-14) — cierra Fase 4.** Sobre `Pago`/`PagoDAL`. Es el único
  punto del sistema donde una misma operación puede terminar escribiendo en
  `CuentaCorrienteCliente` **o** `CuentaCorrienteProveedor` según un dato de entrada
  (`Pago.Tipo`), decidido en tiempo de ejecución — el resto de Fase 4 siempre supo de antemano a
  qué tabla escribir. `PagoDAL.Registrar` lo resuelve con una rama `if` dentro del mismo batch
  atómico (no dos métodos separados): el `INSERT Pago` y la validación de que el monto no supere
  el saldo pendiente del comprobante elegido son comunes a los dos casos.

  **No aportó ningún patrón de UI nuevo a propósito** — reutiliza tal cual los dos buscadores
  desplegables ya existentes (cliente de Turnos/Órdenes, proveedor de Compras), alternados con un
  `ddlTipo`. La única pieza nueva es el desplegable "comprobante a pagar", poblado con
  `ComprobanteVentaDAL.ListarPendientesPorCliente`/`ComprobanteCompraDAL.
  ListarPendientesPorProveedor` (un método nuevo en cada uno de esos DAL, no en `PagoDAL` — mismo
  criterio de "cada entidad expone las consultas sobre sí misma" que `TurnoDAL.ListarPorCliente`).

  Un pago "a cuenta general" (sin comprobante puntual) no tiene techo de monto — solo se valida
  el techo cuando se imputa a un comprobante puntual, contra su `saldoPendiente`.

  **Reemplazado el 2026-10-05 (Fase 6):** ya no se elige comprobante ni existe `ddlComprobante`.
  `PagoDAL.Registrar` reparte el monto solo: cancela primero las deudas del titular (más vieja
  primero) y el sobrante queda a favor ("a cuenta"). Como `Pago` tiene un solo `idVenta`/`idCompra`,
  un pago que toca N comprobantes se guarda como N filas de `Pago` (más una sin comprobante para el
  sobrante), cada una con su movimiento de cuenta corriente, en un único batch atómico. Ya no hay
  tope de monto. Además, una venta (al cerrar la orden) o compra a cuenta corriente nueva aprovecha
  el saldo a favor: nace con `saldoPendiente = total − crédito`, sin movimiento extra. Y cerrar una
  orden sin servicios ni insumos se rechaza en `ComprobanteVentaDAL.GenerarDesdeOrden` (hay que
  cancelarla). Sin migración de datos viejos: los datos de desarrollo no importan y los scripts
  `01` a `04` ya generan una base coherente con esta regla.

- **Rol Lectura y alta pública de usuarios (sesión 2026-09-21/2026-09-22) — fuera de las 6 fases
  del Roadmap.** Pedido explícito del usuario, sin precedente en `Docs/Lubricentro_Requerimientos.md`
  (que en §10 da por fuera de alcance cualquier portal público): un botón "Crear cuenta nueva" en
  `Login.aspx` lleva a `~/Registro`, pantalla pública (no hereda `PaginaSegura`, mismo molde visual
  que `RecuperarClave`/`RestablecerClave`) donde cualquier visitante elige su propia contraseña.
  `UsuarioDAL.Registrar` (nuevo, junto a `Crear`) valida, chequea mail duplicado, hashea la
  contraseña elegida (a diferencia de `Crear`, que siempre genera una temporal y la manda por
  mail) y fuerza el rol al alta — primero se probó con `Nivel.Empleado`, y a pedido posterior del
  usuario se cambió a un rol nuevo y más restringido, `Nivel.Lectura`. Al terminar, inicia sesión
  automáticamente (`SesionUsuario.Iniciar`) y redirige a `~/Default`, igual que un login exitoso.

  **`Nivel.Lectura = 4`** queda por debajo de Empleado en la jerarquía. Su fila en `MenuNivel` (ver
  `02_DatosIniciales.sql`) replica el mismo `WHERE` que ya excluía Administración/Reportes para
  Empleado, pero con `soloLectura = 1` para **absolutamente todas** las filas restantes — a
  diferencia de Empleado, que hoy solo tiene `soloLectura = 1` en 6 pantallas puntuales
  (Proveedores/Insumos/Compras/Servicios/las dos Cuentas corrientes) y acceso de escritura
  completo en Clientes/Vehículos/Turnos/Órdenes/Ventas/Pagos.

  **Se encontró un bug real, no cosmético, al construir esto.** El flag `PaginaSegura.EsSoloLectura`
  ya era genérico (lo calcula `MenuDAL.ObtenerPermiso` desde `MenuNivel.soloLectura`, para
  cualquier rol/pantalla), y tres pantallas (`Turnos`, `OrdenesDeTrabajo`, `Pagos`) ya mostraban un
  banner "solo consulta" cuando ese flag daba `true` — pero **ningún método de escritura lo
  chequeaba**: el banner era decorativo, el botón de Guardar seguía funcionando. Quedó latente
  porque hasta ahora ningún rol seedeado tenía `soloLectura = 1` en esas tres pantallas. Se corrigió
  agregando `if (EsSoloLectura) return;` al principio de cada handler de escritura (Guardar,
  Cancelar/Cerrar orden, agregar servicio/insumo, registrar pago), más ocultar el panel del
  formulario y la columna "Acciones" de la grilla — mismo patrón ya establecido en `Proveedores.
  aspx.cs`. `Clientes.aspx`/`Vehiculos.aspx` no tenían ni siquiera el banner: se les agregó el
  patrón completo desde cero. **Probado el peor caso a propósito:** reusar el `__VIEWSTATE` de una
  vista de Admin (con el formulario completo habilitado) posteado con la cookie de sesión de un
  usuario Lectura — el servidor acepta el postback (event validation no lo rechaza, porque ese
  viewstate sí tenía el control registrado) pero no escribe nada, confirmando que la guarda real es
  el `if (EsSoloLectura) return;` del código, no el ocultamiento de UI.

  **Los tres scripts de `Database/` se puertaron a mano y se re-verificaron de punta a punta.**
  `01_Esquema.sql` no necesitó ningún cambio de esquema (`Nivel` ya era genérica, sin `CHECK` de
  roles) — solo un comentario. `02_DatosIniciales.sql` ganó la fila de `Nivel` y el bloque de
  `MenuNivel` de Lectura. `03_UsuariosDePrueba.sql` ganó un tercer usuario
  (`lectura@lubricentro.com` / `Lectura123!`), con hash/salt generados en PowerShell replicando
  exacto `PasswordHasher.cs` (PBKDF2-SHA256, 25.000 iteraciones, salt 16 bytes, hash 32) en vez de
  inventados — verificado corriendo `01 → 02 → 03` contra una base descartable y logueándose de
  verdad con esas credenciales contra la app corriendo. Corridos después los 4 scripts (`01` a
  `04_DatosDemo.sql`) contra la base de desarrollo real, a pedido explícito del usuario.

  **Los indicadores visuales de "solo consulta" se sacaron de la interfaz a pedido del usuario**,
  en tres lugares distintos, **sin tocar la restricción real** (que sigue viviendo en `EsSoloLectura`
  y sus guardas): el badge `<span class="badge">consulta</span>` que `Site.Master.cs` agregaba a
  cada ítem del menú desplegable, el banner `pnlSoloLectura` ("Tu rol tiene acceso de solo consulta
  a esta pantalla") de las 15 pantallas que lo tenían, y el sufijo `" (solo consulta)"` que
  `Default.aspx.cs` agregaba a la lista "Tus accesos" del Inicio. En las 3 pantallas de Reportes
  (todavía cascarón) sacar el banner dejó `Page_Load` vacío — se eliminó el método entero en vez de
  dejar un cascarón sin usar.

- **Fase 6 (sesión 2026-09-27) — primera prueba de flujo completo, encuentra un bug real.** Se
  armó un harness de consola descartable (no commiteado: proyecto `.csproj` aparte referenciando
  `BIZ.dll` + `App.config` con la misma cadena de LocalDB) que simula Turno → Orden → Cierre →
  Venta → Pago → Cuenta corriente de punta a punta, llamando directo a los métodos de `BIZ/Data`
  (no HTTP: WSL no llega a los puertos de IIS Express). Confirmó correcto: rechazo de stock
  insuficiente, totales de la venta generada, pago parcial actualizando `saldoPendiente` y la
  cuenta corriente, rechazo de pago que excede el saldo, rechazo de cancelar una orden ya Cerrada.

  **Bug real encontrado, mismo patrón que el hallazgo del rol Lectura:** `btnAgregarServicio_Click`,
  `btnAgregarInsumo_Click` y `gvServicios_RowCommand` (Quitar) en `OrdenesDeTrabajo.aspx.cs` sólo
  chequeaban `EsSoloLectura`, nunca el estado de la orden. La UI esconde el panel de alta cuando
  la orden es terminal (`pnlAgregarServicio.Visible = !esTerminal`), pero nada en el DAL lo
  bloqueaba — un POST directo (o un tab viejo) podía agregar/quitar líneas de una orden ya
  `Cerrada` o `Cancelada`: descontando stock real sin que se reflejara en la venta ya generada
  (huérfano), o revirtiendo silenciosamente lo que `Cancelar` ya había repuesto. Reproducido con
  el harness antes de corregir. **Corregido** agregando el chequeo de
  `OrdenDeTrabajo.EstadosEditables` dentro de `DetalleOrdenServicioDAL.Agregar`/`Quitar` y
  `DetalleOrdenInsumoDAL.Agregar` (la guarda real vive en el DAL, no sólo en el `Visible` de la
  UI) — re-verificado con el mismo harness, las 3 rutas ahora rechazan y el stock no se mueve.

- **Fase 6 (sesión 2026-09-28, Alexis con Claude) — revisión de permisos por rol, sin gaps.**
  Auditoría de solo lectura sobre las ~17-18 pantallas de negocio contra la matriz de
  `Docs/Lubricentro_Requerimientos.md` §5+§9.6 (4 roles): `MenuNivel` en `02_DatosIniciales.sql`
  coincide con la matriz, las 17 pantallas heredan la clase base correcta
  (`PaginaSegura`/`PaginaConSesion`/`Page` público, sin huecos de autenticación), y en las 11
  pantallas con banda "solo consulta" **cada** método de escritura chequea `EsSoloLectura` — no
  sólo la UI. **17/17 OK, 0 gaps reales** (a diferencia del hallazgo de ayer, que era sobre estado
  de orden, no sobre rol). Detalle completo en `Docs/EstadoActual.md`, sesión 2026-09-28.

- **Pulido de interfaz (sesión 2026-10-07): modales, listas en el navegador, selectores con
  búsqueda y cuenta corriente opcional.** Pedido del usuario en 12 puntos; el patrón quedó descrito
  en «Formularios en modales y listas en el navegador» (arriba) y el detalle, en
  `Docs/EstadoActual.md`. Decisiones que dejan precedente:

  **El filtro de las listas corre en el navegador sobre la lista completa, no en el servidor.** Se
  reemplazó el cuadro "Buscar" + botón de cada ABM por un filtro instantáneo, y los 10
  `XxxDAL.Buscar` quedaron sin uso y se borraron (mismo criterio que con los wrappers de
  `MovimientoStockDAL`: no se deja código sin llamador). Por la misma razón, el paginado de Insumos
  y de los reportes pasó al navegador: con paginado de servidor, ordenar y filtrar solo habrían
  visto la página actual. Si algún día una lista crece demasiado para traerla entera, hay que
  volver a filtrar en el servidor; hoy los volúmenes de un lubricentro no lo justifican.

  **El modal lo reabre el servidor, no se mantiene abierto solo.** Web Forms recarga la página en
  cada postback completo, así que cada handler que deja el formulario a la vista llama a
  `Interfaz.AbrirModal` (y los errores van adentro del modal). Donde eso haría parpadear el modal
  en cada paso (líneas de una orden o de una compra, elegir un titular), el cuerpo va en un
  `UpdatePanel`.

  **Cuenta corriente opcional: el flag no toca `BIZ`.** Se evaluó obligar a cobrar dentro del
  cierre de la orden (venta + pago en un solo paso). El usuario eligió que cerrar la orden de un
  cliente sin cuenta corriente lleve a Pagos con la venta cargada: la venta y su movimiento de
  cuenta corriente se siguen generando igual para todos, y el cobro es el `PagoDAL.Registrar` de
  siempre. Que el interruptor lo maneje solo quien escribe en Cuenta corriente de clientes es
  criterio nuestro (aplica la matriz de §5), pendiente de confirmar con el usuario.

- **Segundo pedido del 2026-10-07 (13 puntos): barra lateral, opciones, columnas y "Ver";
  datos fiscales, productos, factura, roles editables y cambio de dueño.** Detalle en
  `Docs/EstadoActual.md` y Requerimientos §9.7 a §9.12. Lo que deja precedente:

  **Producto como supertipo, no una tabla única.** El usuario eligió entre supertipo + subtipos,
  una sola tabla, o solo una pantalla común. Con el supertipo las FK de órdenes, compras, ventas
  y kardex siguen apuntando a `Servicio`/`Insumo` (el id es el mismo que el del `Producto`) y no
  hubo que tocarlas; la FK compuesta `(id, tipo)` de cada subtipo garantiza que sean excluyentes.
  En C# es una sola clase plana (`Producto`, con los campos de stock vacíos en un servicio) para
  poder listar las dos subcategorías en un mismo GridView.

  **Precios finales con IVA.** El usuario dejó la decisión a criterio: no cambia ningún total y es
  lo habitual al público. El IVA de cada línea se calcula una vez, al generar la venta, y queda
  guardado: la factura no recalcula con el IVA actual del producto.

  **Opciones de filtro y columnas en el navegador.** Mismo criterio que el filtro de texto de la
  sesión anterior: el servidor manda todo y la tabla filtra. Las casillas "Incluir inactivos" y
  los desplegables de estado que hacían postback se reemplazaron por opciones de la tabla, y los
  parámetros de filtro de `TurnoDAL`/`OrdenDeTrabajoDAL`/`ComprobanteCompraDAL.Listar` se
  borraron. La elección de columnas es preferencia de cada persona (localStorage).

  **"Ver" deja visible la columna Acciones en modo consulta** (antes se escondía entera): se
  esconden solo los enlaces de escritura. Por eso el patrón de `EsSoloLectura` cambió (ver
  «Reglas transversales» arriba).

  **Roles editables sobre `MenuNivel`.** No hizo falta tabla nueva: `NivelDAL.Guardar` reescribe
  las filas del rol en un batch atómico (pantallas con acceso más sus grupos). Admin no se edita
  para que nadie pueda dejar el sistema sin quien administre permisos; Lectura no se borra porque
  la usa `~/Registro`. Con permisos editables, toda pantalla tiene que respetar `EsSoloLectura`:
  Usuarios no lo hacía (solo la veía Admin) y se corrigió.

  **Cambio de dueño: acción propia y bloqueada con trabajo en curso.** Antes se cambiaba desde
  "Editar" sin ninguna validación, y un turno u orden en curso quedaba a nombre de alguien que ya
  no era el dueño (después `TurnoDAL`/`OrdenDeTrabajoDAL.ValidarReferencias` rechazaban editarlos).

  **Verificación de punta a punta en un navegador real.** Además de MSBuild y `aspnet_compiler`,
  se manejó Chrome sin ventana con puppeteer-core contra IIS Express (pruebas en el scratchpad,
  no commiteadas): recorrido de las 17 pantallas y los flujos de los 13 puntos con tres roles.
  Encontró un bug real (la alícuota `10.50` de la base contra el `10.5` del desplegable).

- **Login, teléfonos y datos del comercio (sesión 2026-10-08).** Detalle en
  `Docs/EstadoActual.md`. Lo que deja precedente:

  **La configuración que se edita desde la aplicación va en la base, no en `Web.config`.** Los
  datos del comercio pasaron de `appSettings` a la tabla `Emisor` (una fila) con su pantalla en
  Administración. Escribir `Web.config` desde la aplicación reinicia el sitio y pide permisos de
  escritura sobre la carpeta. `Web.config` queda para lo que depende de la máquina (cadena de
  conexión, mails).

  **Nada de utilidades de `display` de Bootstrap (`d-block`, `d-flex`…) en un validador.** Llevan
  `!important` y le ganan al `display:none` en línea con el que ASP.NET arranca un validador
  `Display="Dynamic"`: el mensaje de error se ve desde que abre la página. Pasaba en las cuatro
  pantallas de ingreso. El bloque se da con una regla propia sin `!important`
  (`.campo .text-danger`, `.login-card .text-danger`).

  **Un botón de Web Forms es un `<input>`**, así que la regla general `input { max-width: 280px }`
  del template de Site.css también lo corta: un botón a todo el ancho necesita `max-width: 100%`.

  **Los teléfonos se guardan solo con los dígitos** (`FormatoTelefono.Normalizar`), como el DNI y
  el CUIT. A diferencia del CUIT, no se les da formato al mostrarlos: la característica tiene de 2
  a 4 dígitos según la zona y no hay una regla simple para cortarla.

- **Imagen de cada producto (sesión 2026-10-08).** Detalle en `Docs/EstadoActual.md` y
  Requerimientos §9.13. Lo que deja precedente:

  **Los archivos van en la base, en una tabla aparte.** En una carpeta del sitio, cada archivo
  quedaría solo en la máquina donde se subió (la base es compartida por VPN y cada PC corre su
  IIS). La tabla aparte evita arrastrar los archivos en cada consulta de la entidad, y un handler
  (`.ashx`) los sirve con la fecha en la dirección, para que el navegador los guarde.

  **Se achica al subir, no al mostrar.** `Imagen.Preparar` deja la imagen en 800 px y arma la
  miniatura una sola vez; la lista pide solo la miniatura. Una foto de teléfono se endereza según
  su orientación EXIF: el navegador endereza el original, pero la copia achicada pierde ese dato.

  **El campo de archivo se vacía en cada postback.** Por eso la imagen se valida antes de guardar
  la entidad, y si el guardado falla, el mensaje pide volver a elegirla.

  **Las imágenes de ejemplo son propias.** Las fotos de productos de marca tienen derechos de sus
  dueños y los bancos libres ponen condiciones (Pixabay no deja usar marcas en un uso comercial):
  las de `Database\ImagenesDemo` se dibujaron para el proyecto.
