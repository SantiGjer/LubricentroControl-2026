# Estado actual del sistema

**Proyecto:** LubricentroControl 2026 · Programación Avanzada — USAL
**Última actualización:** 8 de octubre de 2026

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

**Fase 1 a 5 completas. Falta Fase 6** (integración, pruebas y pulido — sin empezar). Las 5
pantallas de Fase 4 (Compras, Cuenta corriente de Proveedores, Ventas, Cuenta corriente de
Clientes, Pagos) están hechas y verificadas contra IIS Express. Los 3 reportes de Fase 5 (Stock
bajo, Ventas por período, Cuentas corrientes) están hechos y verificados con `MSBuild`/
`aspnet_compiler`, pero **ninguno se probó todavía contra IIS Express** (WSL no llega a esos
puertos — queda para Alexis con F5 en Visual Studio, ver Fase 6).

| Fase | Contenido | Estado |
|---|---|:---:|
| 1 | Login, roles, menú dinámico, ABM de usuarios, capa de datos | ✅ Completa |
| 2 | ABM de Clientes, Vehículos, Proveedores, Insumos, Servicios | ✅ Completa |
| 3 | Turnos y Órdenes de trabajo | ✅ Completa |
| 4 | Compras, Ventas, Pagos, Cuentas corrientes | ✅ Completa |
| 5 | Reportes | ✅ Completa |
| 6 | Integración, pruebas y pulido | 🔶 En curso (flujo completo y permisos revisados) |

### Qué funciona hoy

- Login por mail y contraseña hasheada, con cierre de sesión.
- Recuperación de contraseña por mail, con token de un solo uso y vencimiento a 60 minutos.
- Cambio de contraseña propia.
- ABM de usuarios con asignación de rol, alta con contraseña temporal enviada por mail,
  blanqueo de clave y baja lógica.
- Menú principal armado dinámicamente desde la base según el rol del usuario.
- Control de acceso por pantalla verificado del lado del servidor: esconder la opción del menú
  no alcanza, la guarda corre en cada request.
- Las 21 tablas del diagrama E/R creadas, con los datos semilla de seguridad, más
  `MovimientoStock` (kardex de stock, agregada en Fase 2), `Producto` (supertipo de Servicio e
  Insumo) y `Factura`, las dos del 2026-10-07, y `Emisor` (datos del comercio) e `ImagenProducto`
  (imagen de cada producto), las dos del 2026-10-08 — 27 tablas en total hoy, contando `MenuNivel`.
- Capa `BIZ/Data` funcionando de punta a punta contra SQL Server.
- **Interfaz común de las pantallas de gestión (desde 2026-10-07):** barra lateral fija a la
  izquierda con el menú del rol (grupos desplegables que se recuerdan, el isotipo del ingreso y el
  usuario abajo; en un celular se abre desde una barra superior). Cada lista va a todo el ancho con
  una barra de herramientas (buscador, "Columnas" y "Nuevo …") y debajo una sección de opciones de
  filtro en botones (estado, tipo, saldo…); columnas ordenables, elegibles y paginado en el
  navegador (`Scripts/Lubricentro.js`). Cada fila tiene "Ver" (todos sus datos, también para quien
  solo consulta; también con clic en el nombre). Alta, edición y detalle se abren en un modal. Para
  elegir cliente, dueño o proveedor hay selectores con búsqueda, y la edición de un registro dado
  de baja ofrece "Reactivar".
- **ABM de Clientes**: alta, baja lógica, edición y reactivación. Persona física o empresa (razón
  social), tipo de identificación (DNI, CUIT, CUIL, LE, LC, pasaporte), condición frente al IVA y
  domicilio completo con provincia y código postal (desde 2026-10-07). El buscador encuentra también
  por la patente de sus vehículos. **Cuenta corriente opcional** por cliente, con un interruptor en
  el formulario.
- **ABM de Vehículos**: alta, baja lógica, edición y reactivación; filtro por patente, marca, modelo
  o dueño. Selector de dueño con búsqueda en el alta y **"Cambiar dueño"** después (no con una
  orden o un turno en curso), validación de formato de patente y del año (desde 1900), y dropdown
  fijo de tipo de combustible.
- **"Nuevo cliente" desde Vehículos**: si al cargar un vehículo el dueño todavía no existe como
  cliente, se puede crear sin perder los datos del vehículo ya tipeados; al crearlo se vuelve
  automáticamente con ese cliente ya seleccionado como dueño.
- **ABM de Proveedores**: alta/baja lógica/edición, búsqueda por razón social o CUIT (tolera
  guiones en ambos sentidos), CUIT formateado en la grilla. Primera pantalla real en modo
  **solo consulta para el rol Empleado**: se le esconde el formulario entero y la columna de
  acciones de la grilla, no solo los botones — solo ve el buscador y los resultados.
- **ABM de Productos + kardex de stock** (desde 2026-10-07 reemplaza a Insumos y Servicios):
  servicios e insumos en una sola lista, con opciones para ver uno u otro. Cada producto con SKU,
  código de barras, precio final y tipo de IVA/alícuota; un insumo suma marca, unidad, stock mínimo
  y stock inicial (registra un ajuste automático), y editándolo, el ajuste manual de stock (un solo
  campo con signo) y el historial de movimientos. Resaltado en rojo de los insumos con stock por
  debajo del mínimo; grilla paginada a 30 filas en el navegador. Modo solo-consulta para Empleado.
  **Imagen opcional por producto** (desde 2026-10-08): PNG o JPG que se elige en el formulario (con
  vista previa) y se ve en miniatura junto al nombre y en grande en "Ver".
- **Turnos** (primera pantalla de Fase 3): alta/edición, sin baja lógica (no aplica — `Turno` no
  tiene columna `activo`; "cancelar" es simplemente llevar el campo `estado` a `Cancelado` desde
  el mismo formulario; en el alta el estado se muestra como texto fijo "Solicitado" y el
  desplegable de estado aparece recién al editar). Selector de cliente con búsqueda (el mismo que el
  dueño en Vehículos), fijo al editar, **sin** el atajo "Nuevo cliente" (decisión de alcance: no está en el
  requerimiento de Turnos, sí lo está en el walk-in de Órdenes). Selector de vehículo opcional,
  poblado con los vehículos activos del cliente elegido. Búsqueda por nombre/apellido/DNI del
  cliente más filtro por estado. Acceso completo para los 3 roles (Admin/Encargado/Empleado),
  sin modo solo-consulta. Desde 2026-10-07 la lista arranca con los turnos de hoy (marcados),
  después los próximos y al final los pasados, con opciones por "cuándo" y por estado.
- **Órdenes de trabajo** (segunda y última pantalla de Fase 3, cierra la fase): alta walk-in o
  con turno previo, cliente/vehículo/turno fijos una vez creada la orden (se ven como texto de
  solo lectura al editar, no como desplegables). Franja de detalle (aparece solo editando una
  orden ya creada) con dos columnas Servicios/Insumos, cada una con su mini-alta + grilla —
  agregar un insumo descuenta stock automáticamente (kardex con `MovimientoStock.TipoOrden`) y
  agregar un servicio no toca stock. "Cancelar orden" (botón aparte de "Guardar", con confirmación)
  repone el stock de todos los insumos cargados (`TipoCancelacionOrden`) y es irreversible.
  "Quitar" en líneas de insumo (`DetalleOrdenInsumoDAL.Quitar`, con confirmación) repone el stock
  de esa línea y deja la entrada en el kardex (`TipoCancelacionOrden`, descripción "quitar
  insumo"), solo con la orden Abierta o En proceso; en líneas de servicio es un `DELETE` simple,
  sin efecto colateral. El walk-in con cliente **y vehículo** nuevos
  funciona de punta a punta: "Nuevo cliente"/"Nuevo vehículo" desde Órdenes reutilizan y extienden
  el mecanismo de `Response.Redirect` + query string que ya conectaba Vehículos↔Clientes (un
  tercer origen `"orden"` agregado en paralelo al `"vehiculo"` existente, sin tocarlo). La lista
  arranca filtrada en "Abierta", de la más nueva a la más vieja. Cerrar la orden de un cliente sin
  cuenta corriente lleva directo a Pagos, con el saldo de la venta ya cargado; con cuenta corriente
  (desde 2026-10-07) se elige si el saldo queda en la cuenta o se cobra ahora. "Ver" muestra la
  orden completa a todos los roles. Acceso completo para los 3 roles, sin modo solo-consulta.
- **Compras** (primera pantalla de Fase 4): sin franja de alta progresiva como Órdenes — una
  compra es la transcripción de una factura que ya llega completa, así que las líneas se arman
  **en memoria** (`ViewState`, primer uso de este patrón en el proyecto) y "Guardar compra" las
  persiste todas juntas con la cabecera en un solo batch atómico. Suma stock automáticamente por
  cada línea (kardex con `MovimientoStock.TipoCompra`). Condición de pago "Contado" registra el
  medio de pago en la propia compra (columna nueva `ComprobanteCompra.medioPago`) y queda
  `saldoPendiente = 0` sin tocar Pagos/Cuenta corriente; "Cuenta corriente" deja
  `saldoPendiente = total` y genera el movimiento en `CuentaCorrienteProveedor`. Numeración
  correlativa automática (`C-000001`, ...) derivada del propio `IDENTITY`. Una compra ya guardada
  no se edita — se ve de solo lectura. Modo solo-consulta para Empleado, igual que Proveedores/
  Insumos.
- **Cuenta corriente de Proveedores** (segunda pantalla de Fase 4): lista de proveedores con "Ver
  cuenta", que abre en un modal el saldo actual + historial (fecha, tipo, debe, haber, saldo,
  descripción, usuario) del proveedor elegido. A diferencia de Proveedores/
  Insumos, el modo solo-consulta de Empleado **no esconde toda la pantalla**: puede buscar y ver
  el historial/saldo de cualquier proveedor, solo se le esconde la franja "Registrar ajuste"
  (motivo + monto con signo, mismo patrón que el ajuste de stock de Insumos). El usuario que hizo
  cada ajuste queda registrado (`CuentaCorrienteProveedor.idUsuario`, columna nueva de esta fase);
  los movimientos automáticos de compra no llevan usuario (se muestran con la celda vacía).
- **Ventas** (tercera pantalla de Fase 4): **de solo lectura, sin alta** — el comprobante se
  genera automáticamente al cerrar una orden de trabajo (`OrdenDeTrabajoDAL.Cerrar` →
  `ComprobanteVentaDAL.GenerarDesdeOrden`), nunca se carga a mano. Lista de ventas; "Ver" abre en
  un modal la cabecera (cliente, vehículo, fecha, subtotal/impuestos/total/saldo pendiente) +
  detalle línea por línea (copiado 1 a 1 de los servicios/insumos que ya tenía la
  orden, con el precio que ya tenían aplicado). Numeración correlativa (`V-000001`, ...), mismo
  esquema que Compras. Acceso completo para los 3 roles, sin modo solo-consulta. **Órdenes de
  trabajo ganó un botón "Cerrar orden"** (separado de "Guardar", con confirmación, mismo criterio
  que "Cancelar orden") porque cerrar pasó a tener el efecto colateral de generar la venta —
  `Cerrada` salió de `OrdenDeTrabajo.EstadosEditables`.
- **Cuenta corriente de Clientes** (cuarta pantalla de Fase 4): calco exacto de Cuenta corriente
  de Proveedores — lista de clientes, historial+saldo+ajuste manual del elegido en un modal, mismo
  matiz de permisos (solo consulta esconde nada más el ajuste, no toda la pantalla). Sin DAL
  nuevo: `CuentaCorrienteClienteDAL` ya había quedado escrito en la sesión de Ventas. Desde
  2026-10-07 lista por defecto solo los clientes con cuenta corriente habilitada (con una casilla
  para ver al resto) y suma "Editar cliente" para cambiarla.
- **Pagos** (quinta y última pantalla de Fase 4, **cierra la fase**): alta de pago de cliente o
  de proveedor. **Sin elegir comprobante** (cambio del 2026-10-05): el monto se reparte solo,
  primero cancela las deudas más viejas (`saldoPendiente > 0`) y lo que sobre queda "a cuenta
  general", o sea a favor en la cuenta corriente — sin tope de monto. La pantalla muestra la
  deuda o el saldo a favor del titular elegido. Selectores con búsqueda de cliente y de proveedor,
  alternados con un `ddlTipo`. El formulario se abre solo, con el cliente y el monto cargados,
  cuando Órdenes manda a cobrar la venta de un cliente sin cuenta corriente. Un pago no se edita
  ni se borra. **Acceso completo para los 3 roles** (a diferencia de Compras/
  Cuentas corrientes, acá Empleado también puede cobrar — Requerimientos §5).
- **Reporte de Stock bajo** (primero de los 3 reportes de Fase 5): grilla de solo lectura sobre
  `InsumoDAL.ListarStockBajo()` (ya escrita desde Fase 2, sin usar hasta ahora), insumos activos
  con `stockActual` por debajo de `stockMinimo`, ordenados por faltante de mayor a menor.
  Resumen arriba con cantidad total y cuántos están en cero, esas mismas filas resaltadas en rojo
  en la grilla. Sin filtros ni parámetros — a diferencia de los otros dos reportes, no había nada
  de diseño que acordar antes de construirlo: el DAL ya traía el filtro exacto de
  Requerimientos §6.9. Mismo acceso que el resto de Fase 5 (sin acceso para Empleado).
- **Reporte de Ventas por período** (segundo de los 3): filtro de rango de fechas (`Desde`/
  `Hasta`, inputs `type="date"`, default el mes en curso al entrar a la pantalla) sobre
  `ComprobanteVentaDAL.ListarPorPeriodo`, método nuevo, sin cambio de esquema. Mismo formato
  resumen + grilla paginada que Stock bajo: cantidad de ventas y total del período arriba (más el
  total pendiente de cobro si lo hay), filas con saldo pendiente resaltadas en rojo en la grilla.
  `CompareValidator` rechaza un rango con `Hasta` anterior a `Desde`. Mismo acceso que el resto de
  Fase 5 (sin acceso para Empleado).
- **Reporte de Cuentas corrientes** (tercero y último de Fase 5, cierra la fase): dos secciones
  independientes en una sola pantalla (Clientes, Proveedores), cada una con su propio resumen +
  grilla paginada. Sobre `CuentaCorrienteClienteDAL`/`CuentaCorrienteProveedorDAL.ListarSaldos`,
  dos métodos nuevos (`CROSS APPLY` para traer el último saldo de cada uno), sin cambio de
  esquema. Decidido con el usuario: entran los que tienen saldo distinto de cero, cualquier signo
  (deuda o a favor) — no es un listado completo de todos ni sólo deudores. Sin resaltado de filas
  (a diferencia de Stock bajo/Ventas: acá no hay un subconjunto "más urgente" claro dentro de la
  lista, ya viene filtrada a lo que importa). Mismo acceso que el resto de Fase 5 (sin acceso
  para Empleado). **Cierra Fase 5 — falta sólo Fase 6** (integración, pruebas y pulido).

- **Factura de una venta** (desde 2026-10-07): "Facturar" en Ventas emite la factura A, B o C según
  la condición frente al IVA del comercio y del cliente, con numeración propia por letra y punto de
  venta, y se ve e imprime en la misma pantalla. Sin validez fiscal (sin CAE). Los precios son
  finales: la venta guarda el IVA que contiene cada línea. Los datos del comercio que salen en la
  factura se editan en Administración > Datos del comercio (desde 2026-10-08).
- **Roles y permisos** (desde 2026-10-07, en Administración): crear, renombrar y borrar roles y
  elegir para cada pantalla sin acceso / consulta / completo. Admin queda fijo con todo; Lectura no
  se borra.
- **Alta pública de usuario (`~/Registro`)**: link "Crear cuenta nueva" desde `Login.aspx`, sin
  necesitar sesión previa. El visitante elige su propia contraseña (a diferencia del ABM de
  Usuarios, que siempre genera una temporal y la manda por mail); si el mail no existe, crea la
  cuenta y loguea automático. Siempre queda en el rol nuevo **Lectura** (`Nivel.Lectura = 4`, el
  más restringido de los cuatro), nunca en Empleado.
- **Rol Lectura**: ve las mismas pantallas que Empleado, pero en modo solo consulta en
  absolutamente todas — incluidas Clientes, Vehículos, Turnos, Órdenes, Ventas y Pagos, donde
  Empleado sí tiene alta/edición completa. Sin acceso a Usuarios ni a Reportes.
- Los indicadores visuales de "solo consulta" (badge del menú, banner de cada pantalla, sufijo en
  "Tus accesos" del Inicio) se sacaron de la interfaz — la restricción real (formulario oculto,
  columna Acciones oculta, guarda `if (EsSoloLectura) return;` en cada escritura) sigue intacta.

### Qué NO funciona todavía

Las 15 pantallas de negocio existen y funcionan. Queda **Fase 6** completa (Roadmap): pruebas de
flujo de punta a punta (turno → orden → venta → pago → cuenta corriente), validaciones cruzadas
entre módulos, revisión de permisos por rol pantalla por pantalla, mejora de mensajes de error y
UX, y documentación final. Ver también los pendientes puntuales sin fase asignada en la
sección 4 más abajo (contraseña del admin, usuarios de prueba, VPN Radmin, etc.).

---

## 2. Historial de sesiones

### 2026-10-08 (cont. 2) — Imagen de cada producto e imágenes de ejemplo

El usuario pidió una imagen para cada producto y preguntó de dónde sacar imágenes PNG para
cargarlas. Decisiones (las de negocio, en Requerimientos §9.13):

- **La imagen va en la base, no en una carpeta del sitio.** La base es compartida (VPN Radmin) y
  cada máquina corre su propio IIS: un archivo en disco quedaría solo en la PC donde se subió.
  Además no hace falta permiso de escritura sobre la carpeta (mismo motivo que `Emisor`). Va en una
  tabla aparte, `ImagenProducto` (una fila por producto, opcional), para que las consultas de
  productos no arrastren los archivos: `ProductoDAL` solo trae la fecha de la imagen.
- **Se guarda achicada y con una miniatura.** Se acepta PNG o JPG de hasta 5 MB (se reconoce por
  los primeros bytes, no por la extensión); si mide más de 800 px de lado se achica, en el mismo
  formato (el PNG conserva la transparencia), y siempre se arma una miniatura de 120 px para la
  lista: unos 7 KB contra 30 KB o más, que con cientos de productos se nota. Una foto de teléfono se
  endereza según su orientación EXIF; si no, la copia achicada quedaría acostada (el navegador
  endereza el original, pero la copia pierde ese dato). Es el primer uso de `System.Drawing`, en
  `BIZ\Modelo\Imagen.cs`.
- **La pantalla la pide aparte, a `ImagenProducto.ashx`.** El handler exige sesión
  (`IReadOnlySessionState`), pero no mira permisos de pantalla: la imagen de un producto no es un
  dato reservado. La dirección lleva la fecha de la imagen (`&v=`), así que el navegador la guarda
  un año y una imagen nueva cambia la dirección.
- **En la pantalla:** la miniatura va dentro de la celda del nombre (no suma texto, así que no
  cambia cómo se ordena ni se filtra), y "Ver" muestra la imagen grande (`data-imagen`, genérico en
  `Lubricentro.js`). En el formulario, la sección "Imagen" muestra la actual o la recién elegida
  antes de guardar, y "Quitar la imagen". Un archivo que no es PNG ni JPG, o de más de 5 MB, lo
  descarta el navegador sin subirlo; `maxRequestLength` pasó a 10 MB para que uno algo más pesado
  llegue y se rechace con mensaje, no con la página de error de ASP.NET.
- **El navegador vacía el campo de archivo en cada envío.** Si el producto no se guarda (SKU
  repetido, un validador del servidor), la imagen elegida se pierde: el mensaje pide volver a
  elegirla. Por eso la imagen se valida antes de guardar el producto, y se guarda después (en un
  alta, recién ahí hay id).

**Imágenes de ejemplo.** No hay un banco abierto de fotos de este rubro, y las fotos de producto
de marcas (YPF, Shell, Fram…) tienen derechos de sus dueños. Se dibujaron 20 ilustraciones propias
(SVG pasado a PNG con Chrome, sin derechos de terceros), una por producto de `04_DatosDemo.sql`:
los insumos como objetos (bidón, filtro, batería…) y los servicios como insignias redondas con el
estilo del isotipo. Están en `Database\ImagenesDemo` y las carga `05_ImagenesDemo.sql` (opcional),
generado pasando cada PNG por `Imagen.Preparar`, así que sus miniaturas son las mismas que daría
una subida desde la pantalla. Los binarios van partidos en renglones con `\` al final (T-SQL une
las partes), para no tener renglones de 60.000 caracteres. Para fotos reales: el catálogo del
distribuidor o del fabricante (suelen darlo a los revendedores) o fotos propias; los bancos libres
sirven con cuidado: Pixabay no permite usar marcas en un uso comercial, y en Openverse cada imagen
trae su propia licencia.

La base de desarrollo no se recreó: se le creó la tabla y se corrió `05`. Verificado: las reglas
de `Imagen.Preparar` llamadas directo sobre `BIZ.dll` (achica, conserva la transparencia, endereza
una foto con EXIF 6, rechaza un texto con extensión .png, un PNG dañado y uno de 6 MB); 25 pruebas
en Chrome sin ventana (subir, vista previa, guardar, "Ver", reemplazar, quitar, alta con imagen,
archivos rechazados, SKU repetido con imagen elegida, Empleado solo ve, caché del handler, 404 y
403 sin sesión); y el recorrido de las 18 pantallas del Admin sin errores. El producto de prueba
se borró. Ojo al borrar en `Producto` desde `sqlcmd`: sin `-I` (`QUOTED_IDENTIFIER`) falla por los
índices filtrados, igual que al insertar.

### 2026-10-08 (cont.) — Login, teléfonos sin separadores y datos del comercio editables

Pedido del usuario en tres puntos más uno agregado sobre la marcha:

- **Botón "Ingresar" corrido a la izquierda.** No era el markup: la regla general
  `input { max-width: 280px }` del template de Site.css cortaba el botón (es un `<input>`) dentro
  de una tarjeta de 400px. `.login-btn` ahora lleva `max-width: 100%`, igual que `.login-input`.
- **Sin el título "Ingresar"** arriba del login: repetía el texto del botón. En su lugar, el
  nombre "Lubricentro Control" pasó de subtítulo chico y gris a título de la tarjeta
  (`h1.login-nombre`), con el estilo de la marca de la barra lateral ("Control" en rojo). Las
  otras tres pantallas de ingreso tienen su propio título y lo siguen llevando como subtítulo.
- **Bug encontrado de paso, en las cuatro pantallas de ingreso:** los mensajes de los validadores
  ("Ingresá tu mail.", etc.) se veían **apenas abría la página**, sin haber enviado nada. Llevaban
  `CssClass="… d-block"`, y el `display: block !important` de Bootstrap le gana al
  `style="display:none"` con el que ASP.NET arranca un validador `Display="Dynamic"`. Se sacó
  `d-block` y el bloque lo da `.login-card .text-danger` sin `!important` (mismo criterio que
  `.campo .text-danger` en las pantallas de gestión). No usar utilidades de `display` de Bootstrap
  en validadores.
- **Teléfonos sin separadores.** Los de ejemplo tenían guiones y los cargados a mano no. Se
  guardan como el DNI y el CUIT: `FormatoTelefono.Normalizar` deja solo los dígitos (y el `+`
  inicial). El formulario sigue aceptando guiones, espacios y paréntesis al escribir. No se le da
  formato al mostrarlo: la característica tiene de 2 a 4 dígitos según la zona y no hay regla
  simple para saber dónde cortar. `04_DatosDemo.sql` ya trae los números sin guiones.
- **Datos del comercio editables (pedido nuevo).** Pasaron de `Web.config` (`Emisor.*`) a la
  tabla `Emisor`, de una sola fila (`CK_Emisor_unico`), con la pantalla **Datos del comercio** en
  Administración (`~/DatosComercio`, por defecto solo Admin, como Usuarios y Roles). Se eligió la
  base y no escribir `Web.config` desde la aplicación: eso reinicia el sitio y necesita permisos
  de escritura sobre la carpeta. El inicio de actividades pasó a ser una fecha (`DATE`), y la factura
  lo sigue copiando como texto `dd/MM/yyyy`. La pantalla aclara que el cambio vale para las facturas
  siguientes: cada factura ya guardaba una copia de los datos del comercio.

La base de desarrollo no se recreó, para no perder lo cargado a mano: se le aplicó un script
puntual (tabla `Emisor`, filas de `Url`/`Menu`/`MenuNivel` para Admin y teléfonos sin
separadores). `01` a `04` se probaron desde cero en una instancia de LocalDB descartable.
Verificado en Chrome sin ventana (puppeteer-core, en el scratchpad): login, guardar los datos del
comercio (con el CUIT inválido rechazado), facturar una venta con los datos nuevos (sale C en el
punto de venta 2), editar un teléfono con guiones y paréntesis, y Encargado sin acceso. La
factura de prueba se borró y se restauraron los datos de ejemplo.

### 2026-10-08 — Qué hay que saber al levantar el proyecto en otra máquina

A pedido del usuario, lo que hay que saber al clonar el repo en otra máquina quedó en `CLAUDE.md`
(«Al levantar el proyecto en otra máquina»): es lo que Claude lee en cualquier máquina, mientras
que su memoria es local de cada PC. Al relevarlo aparecieron dos cosas que no estaban anotadas:

- **Mails sin credenciales:** `MailModoDesarrollo=false` está commiteado, pero
  `Web.MailSettings.config` está gitignoreado (en esta PC tampoco existe). El envío falla sin
  romper la pantalla porque `ServicioMail.Enviar` atrapa la excepción, pero los tres llamadores
  (`UsuarioDAL.Crear`, `UsuarioDAL.BlanquearPassword`, `RecuperacionClaveDAL`) descartan el
  resultado: la recuperación de clave no avisa que el mail no salió.
- **Importes que se tipean en el otro formato:** verificado con `decimal.TryParse` y
  `BaseCompareValidator.CanConvert` en en-GB (la configuración de esta PC) y en es-AR. El
  `CompareValidator Type="Currency"` rechaza el formato de la otra configuración, pero las líneas
  de Compras (cantidad y precio unitario) no tienen validador: en esta PC `1500,50` se guarda como
  `150050`. Anotado en la sección 4, sin corregir.

Corregido de paso: los cuerpos de los mails de alta y de recuperación de clave seguían diciendo
"LubricentroControl" junto (punto 1 del pedido anterior, que se había dado por hecho).

Las pruebas de punta a punta se repitieron hoy: la funcional había fallado en "turnos de hoy
primero" porque la base era del día anterior (los turnos de ejemplo se fechan al correr `04`).
Con la base recreada pasaron las tres suites (61, 65 y 16 pruebas).

### 2026-10-07 (cont.) — Barra lateral, opciones de filtro, columnas y "Ver"; datos fiscales, productos, factura y roles editables

Segundo pedido del usuario del día, en 13 puntos. Antes de arrancar se commiteó el pulido anterior
y se arregló LocalDB en esta máquina (ver la sección 4). Decisiones (las de negocio, en
Requerimientos §9.7 a §9.12):

- **Tres decisiones del usuario antes de diseñar:** productos como supertipo con Servicio e
  Insumo de subtipos (frente a una sola tabla o solo una pantalla común); roles completos (crear,
  renombrar, borrar y permisos, no solo los permisos de los 4 roles); y los precios, que el usuario
  dejó a criterio ("no importan los registros que ya están"): quedaron **finales, con el IVA
  incluido**, para no cambiar ningún total. Si se quisieran netos, el cambio está en un solo lugar
  (`Iva.Contenido` y `ComprobanteVentaDAL.GenerarDesdeOrden`).
- **Opciones de filtro en el navegador, no en el servidor.** Las casillas y desplegables que
  filtraban en el servidor ("Incluir inactivos", estado de turnos y órdenes, "Solo con saldo") pasaron
  a botones agrupados debajo de la barra (`.opciones-tabla`), que filtran en el navegador sobre la
  lista completa, mismo criterio que el filtro de texto de la sesión anterior. Cada grupo filtra por
  una columna (por el texto del encabezado) o por un atributo de la fila (`data-saldo`, `data-cuando`,
  `data-stock`, `data-factura`); "A|B" acepta cualquiera de los dos (ej. "En curso").
- **La lupa se encimaba con el texto** por especificidad de CSS: `.pantalla-abm input.filtro-tabla-texto`
  e `.pantalla-abm input[type="search"]` pesan lo mismo y la regla general (padding 0.5rem 0.7rem) iba
  después. Ahora la del buscador va después y con más especificidad.
- **Columnas y "Ver" sin código por pantalla.** Cada grilla trae todas las columnas; las que no
  hacen falta siempre van con la clase `oculta` (escondidas de entrada, también antes de que arranque
  el JS para que no parpadeen). "Columnas" las muestra o esconde y se recuerda en el navegador
  (localStorage, preferencia de cada persona). "Ver" arma una ventana con todas las columnas de la
  fila, incluidas las escondidas; las acciones de la fila con `.accion-detalle` (Editar, Cambiar
  dueño) aparecen en su pie. Donde el detalle tiene más que la fila (Órdenes, Ventas, Compras,
  cuentas corrientes), "Ver" es del servidor. Consecuencia: **la columna Acciones ya no se esconde
  en modo consulta**; se esconden solo los enlaces de escritura (`Visible='<%# PuedeEscribir %>'`).
- **Nombre del cliente en un solo lugar:** columna calculada `Cliente.denominacion` (razón social o
  "Nombre Apellido"), que todos los DAL leen como `nombreCliente` en vez de concatenar.
- **Cambio de dueño con su propio modal**, y editando el vehículo el dueño se ve fijo:
  `VehiculoDAL.Actualizar` ya no toca `idCliente`. Antes se podía cambiar desde "Editar" sin
  validar nada, lo que dejaba turnos y órdenes en curso a nombre de alguien que ya no era el dueño
  (y después no se podían editar). "Nuevo cliente" desde ese modal reusa el origen `"vehiculo"` de
  Clientes con un parámetro más (`cambioDueno=1`), sin tocar la rama existente.
- **Cierre de orden:** "Cerrar orden…" abre una confirmación en el mismo modal (sin ir al servidor)
  con el total; con cuenta corriente ofrece "Dejar en cuenta corriente" o "Cobrar ahora". Los botones
  van como `PostBackTrigger` porque hacen `Response.Redirect`.
- **Factura:** tabla nueva con copia de los datos del comercio y del cliente, número tomado con
  `UPDLOCK, HOLDLOCK` en el mismo batch del INSERT. La impresión deja solo la hoja (`@media print`
  con `visibility`).
- **Roles:** los permisos se guardan en un solo batch atómico (`NivelDAL.Guardar`: borra las filas
  de `MenuNivel` del rol y vuelve a insertar las pantallas con acceso y sus grupos, sin los que el
  menú no las muestra). Con permisos editables cualquier pantalla puede quedar "en consulta" para
  cualquier rol: **Usuarios no chequeaba `EsSoloLectura`** (solo la veía Admin) y ahora sí, igual que
  Roles y la factura de Ventas. `Usuario.TieneNivelMinimo` (sin uso y que comparaba ids de rol) se
  borró: con roles nuevos dejaba de tener sentido.
- **Bug encontrado probando:** editar un producto con alícuota 10,5 % reventaba porque la base
  devuelve `10.50` y el desplegable tenía `10.5`. Corregido normalizando los dos lados.
- **Los `.designer.cs` se generaron con un script** (scratchpad, no commiteado) que lee los `.aspx` y
  declara cada control con ID fuera de las plantillas de grilla; antes de usarlo se comprobó que
  reproducía sin diferencias los designers existentes de seis pantallas.
- **Verificación:** rebuild limpio y `aspnet_compiler` sin errores; base recreada con `01` a `04`.
  Pruebas de punta a punta con Chrome sin ventana (puppeteer-core contra IIS Express, en el
  scratchpad, no commiteadas): las 17 pantallas sin errores de servidor ni de JavaScript para Admin,
  y los flujos de los 13 puntos con Admin, Empleado y Lectura, más la vista de celular — 142
  verificaciones, todas bien. Las facturas A y B se revisaron también en modo impresión.

### 2026-10-07 — Pulido de interfaz: modales, listas ordenables y filtrables, selectores con búsqueda y cuenta corriente opcional

Pedido del usuario en 12 puntos. Acá va lo que no se ve leyendo el código.

- **Formularios en modales (Bootstrap).** Todas las pantallas con alta o edición pasaron de dos
  columnas (lista y formulario siempre visible) a lista a todo el ancho más un modal. "Nuevo …" va en
  una barra de herramientas arriba de la lista y "Editar" (antes "Seleccionar") en la fila. El
  servidor vuelve a abrir el modal después de cada postback que lo necesita
  (`Utilidades/Interfaz.AbrirModal`, un startup script): Nuevo, Editar, o un error de validación o
  del DAL, que ahora se muestra *adentro* del modal. Donde hay varias idas y vueltas sin salir del
  formulario (Órdenes: elegir cliente, agregar o quitar líneas; Compras: condición de pago y líneas;
  Pagos: tipo y titular; Turnos: cliente y sus vehículos), el cuerpo del modal va en un
  `UpdatePanel` para que no se cierre. Los botones del pie hacen postback completo para refrescar
  la lista. Compras, Ventas y las dos cuentas corrientes usan el mismo modal para el detalle
  ("Ver"). Con `EsSoloLectura` también se esconde el botón "Nuevo".
- **Orden y filtro en todas las listas, en el navegador** (`Scripts/Lubricentro.js`). Clic en el
  encabezado ordena: detecta fechas `dd/MM/yyyy`, números con el formato regional del servidor (el
  master publica los separadores en el `<body>`) y texto sin acentos. El filtro exige todas las
  palabras en cualquier columna, sin acentos, y "1143215678" encuentra "11-4321-5678". El cuadro
  "Buscar" con botón de cada ABM se reemplazó por ese filtro instantáneo, así que los 10
  `XxxDAL.Buscar` quedaron sin uso y **se borraron**. Lo que se buscaba pero no es columna (el DNI en
  Turnos y Órdenes, el CUIT en Compras) va en `data-buscar` de la fila. El paginado de Insumos y de
  los reportes pasó al navegador (`data-filas-por-pagina`), para que ordenar y filtrar abarquen
  todas las filas y no solo la página. El estado de cada tabla sobrevive a los postbacks en
  `hdnEstadoTablas` (master), codificado con `encodeURIComponent`: un "<" en el filtro dispararía la
  request validation de ASP.NET.
- **Selectores con búsqueda** (`.selector-busqueda`) para el dueño (Vehículos), el cliente (Turnos,
  Órdenes, Pagos) y el proveedor (Compras, Pagos): un campo con el botón de búsqueda adentro y la
  lista desplegable de todos los activos, que se filtra al escribir. Reemplazan el TextBox + "Buscar"
  + Repeater. Cuando el servidor tiene que reaccionar (cargar vehículos y turnos, mostrar el saldo),
  `data-postback` dispara el `ValueChanged` del HiddenField en un postback parcial. Opciones y
  textos en `Utilidades/Selectores.cs`. En Turnos el cliente queda deshabilitado al editar: ya era
  fijo en `TurnoDAL.Actualizar`, pero la pantalla dejaba cambiarlo, y eso permitía guardar un
  vehículo de otro cliente.
- **Cuenta corriente opcional por cliente** (`Cliente.cuentaCorriente`, columna nueva, default 0),
  con un interruptor en el alta y la edición. Decisión del usuario: **sin cuenta corriente, cerrar
  la orden lleva directo a Pagos** con el cliente y el saldo de esa venta ya cargados
  (`~/Pagos?idVenta=`). Con cuenta corriente, la deuda queda en la cuenta como antes. No se bloquea
  nada más (se puede cobrar parcial). Criterio nuestro, a confirmar: el interruptor lo cambia solo
  quien escribe en Cuenta corriente de clientes (Admin y Encargado); para Empleado se ve
  deshabilitado y el servidor conserva lo guardado, aplicando la matriz de §5. Cuenta corriente de
  clientes lista por defecto solo a los que la tienen (con una casilla para ver al resto) y suma
  "Editar cliente" (`~/Clientes?editar=N&volver=ctacte`, que al guardar vuelve). `04_DatosDemo.sql`
  la habilita para Juan, Carlos y Valentina, los que la usan en los datos de ejemplo.
- **Validaciones nuevas.** Teléfono de Cliente y Proveedor (`Modelo/FormatoTelefono`, CustomValidator
  + `Validar()`): números, espacios, guiones, puntos, paréntesis y un "+" inicial, entre 6 y 15
  dígitos. Año del vehículo entre 1900 y el año que viene (`Vehiculo.AnioMinimo`/`AnioMaximo`;
  RangeValidator con los límites puestos desde el modelo, más `Validar()`).
- **Reactivar** en la edición de un registro dado de baja (Clientes, Vehículos, Proveedores,
  Insumos, Servicios, Usuarios): `XxxDAL.Reactivar`, espejo de `Desactivar`.
- **Órdenes de trabajo** arranca filtrada en "Abierta", de la más nueva a la más vieja (el usuario
  eligió entre tres opciones).
- **Login, Registro, Recuperar y Restablecer clave:** tarjeta clara (antes oscura) con la paleta del
  resto de las pantallas, isotipo de gota y botón rojo.
- **Inicio:** el botón de cada tarjeta quedaba pegado a la tabla por especificidad de CSS
  (`a.enlace-boton { margin-top: 0 }` le ganaba a `.enlace-tablero`). Ahora va en un pie separado
  por una línea, y las tarjetas de una misma fila tienen el mismo alto.
- **Los avisos ya no se repiten:** cada pantalla oculta `pnlMensaje` al principio de cada request.
  Antes, el Literal (que guarda su texto en el ViewState) repetía el último aviso en cada postback,
  incluida la contraseña temporal de Usuarios.
- **Verificación:** rebuild limpio y `aspnet_compiler` sin errores. 61 pruebas del JS con jsdom
  (orden, formatos, filtro, paginado, estado entre postbacks, selector, modal), no commiteadas. Con
  IIS Express sobre el sitio precompilado, Login, Registro y Recuperar responden 200 y las
  pantallas del menú redirigen al login. **No se probó ninguna pantalla con datos ni se vio en el
  navegador:** en esta máquina no arrancan ni LocalDB ni el servicio SQL Server (ver la sección 4).
  Falta recrear la base (`01` a `04`; el `01` agrega la columna) y recorrer las pantallas con F5.

### 2026-10-06 (cont. 2) — Inicio como tablero: turnos de hoy y stock bajo

Pedido del usuario: sacar "Tus accesos" del Inicio y convertirlo en un tablero con información
importante. Primera tanda con dos tarjetas, cada una con su botón a la pantalla donde se resuelve.
Sin cambio de esquema.

- **Turnos de hoy:** los turnos de la fecha actual que siguen vigentes (**Solicitado** y **Confirmado**),
  ordenados por hora, con hora, cliente, vehículo y estado; resumen arriba ("2 turnos pendientes para
  hoy (2 solicitados, 0 confirmados)") y botón "Ir a Turnos". Método nuevo
  `TurnoDAL.ListarVigentesDelDia(dia)`. Se interpretó "turnos abiertos y solicitados para el día" como
  los vigentes de hoy; los Completados y Cancelados no aparecen.
- **Stock bajo:** los 3 insumos con mayor faltante (mínimo menos actual, desempate por nombre), con
  resumen ("1 insumo por debajo del mínimo; se muestran los 3 con mayor faltante") y el insumo en cero
  resaltado en rojo, igual que en el reporte. Sin DAL nuevo: usa `InsumoDAL.ListarStockBajo()`.
- **El botón respeta los permisos del rol.** Reportes no está disponible para Empleado ni Lectura, así
  que el botón de stock lleva a "Stock bajo" (reporte) si el rol puede verlo, a Insumos si no, y se
  oculta si no tiene ninguna de las dos (idem el de Turnos). Se decide con
  `MenuDAL.ObtenerPermiso(idNivel, path)`, el mismo chequeo que usa la guarda de `PaginaSegura`.
- **Se eliminó** la lista "Tus accesos" (`MostrarAccesos`/`AgregarAcceso`, el literal `litAccesos` y sus
  estilos). Respaldo del Inicio anterior en `Respaldos\2026-10-06_inicio_antes_del_tablero`.
- **Verificación:** rebuild limpio y `aspnet_compiler -v /` sin errores; con login real como Admin,
  Empleado y Lectura el Inicio responde 200, muestra los 2 turnos solicitados de hoy y el insumo bajo
  de la base de desarrollo, y el botón de stock apunta a "Stock bajo" para Admin y a Insumos para
  Empleado y Lectura. **No se vio en el navegador.** Un login de prueba devolvió un 500 aislado que no
  se repitió (coincidió con una recompilación de la app).
- **Tarjeta "Órdenes en curso" (misma sesión, a pedido):** tercera tarjeta, **horizontal y a todo el
  ancho** debajo de las otras dos (`col-12`, en vez de dos mitades). Lista las órdenes que siguen en el
  taller — **Abierta y En proceso**, no solo "Abierta", porque las dos son trabajo sin cerrar — con
  número, ingreso, cliente, vehículo y estado, la más antigua primero (las que más llevan esperando),
  hasta 10 filas y resumen arriba ("1 orden en curso (1 abierta, 0 en proceso)"). Botón "Ir a Órdenes
  de trabajo" sujeto al permiso del rol. Método nuevo `OrdenDeTrabajoDAL.ListarEnCurso()`. Verificado
  con login real como Admin y Empleado (se ve la orden abierta de la base de desarrollo).
- **Título del tablero (misma sesión, a pedido):** se quitó el saludo "Hola, <nombre>" y la línea
  "Estás trabajando con el rol …"; el Inicio ahora se titula **"Panel de control"** (en español, a
  diferencia de "Dashboard"). Se sacaron `litNombre`/`litNivel` del markup, del designer y de
  `Page_Load`. El nombre y el rol ya no se ven en esta pantalla.
- **Fondo de las pantallas de ingreso (misma sesión, a pedido):** Login, Registro, Recuperar clave y
  Restablecer clave tenían el fondo blanco del sitio con su tarjeta oscura; ahora llevan el mismo gris
  (`#cdd0d4`) que el resto, con una regla `html:has(.login-page)` / `body:has(.login-page)` en
  `Content\Site.css`. Se revisaron todas las pantallas: esas cuatro eran las únicas con fondo blanco
  (Cambiar contraseña y Acceso denegado ya quedaron en gris con la tanda de estilos). La tarjeta
  oscura del Login no cambió. **No se vio en el navegador.**
- **Integración con el remoto (al subir el repo):** `origin/main` tenía 4 commits de Alexis Pallares
  (27 y 28 de septiembre: validación de que una orden Cerrada/Cancelada no admita cambios en el
  detalle, y la revisión de permisos por rol). Se integraron con `rebase`. No chocan con lo de esta
  sesión: su guarda vive en `Agregar` de insumos y servicios, y `Quitar` de insumos (nuevo) trae la
  suya. Hubo un único conflicto, en este mismo documento (fecha de actualización, fila de Fase 6 y
  orden de la bitácora), resuelto conservando las dos versiones.
- **Pendiente para próximas tandas:** más tarjetas (la idea es ir sumando información importante).

### 2026-10-06 (cont.) — Fase 6: estilo básico en todas las pantallas restantes

Segunda tanda del mismo día: el estilo básico llegó a **todas** las pantallas del menú. Solo markup y
CSS (sin code-behind ni designer, mismos ids).

- **Pantallas:** Turnos, Órdenes de trabajo, Compras, Pagos, Cuenta corriente de clientes y de
  proveedores, Ventas, los tres Reportes (Stock bajo, Ventas por período, Cuentas corrientes), Cambiar
  contraseña y Acceso denegado. Quedan **con su estilo propio, sin tocar**, las pantallas de ingreso
  (Login, Registro, Recuperar y Restablecer clave), que usan la tarjeta oscura del Login.
- **Casos particulares:** en Órdenes, las columnas de servicios e insumos van en paneles grises
  (`panel-gris`) y los botones "Cerrar orden" (gris oscuro) y "Cancelar orden" (borde rojo) se
  distinguen de "Guardar" (rojo); en Reportes, los resúmenes (`alert-secondary`) y las filas
  resaltadas (fondo puesto desde el código) se conservan, el filtro de fechas lleva todas las
  etiquetas arriba (`barra-fechas`) y los títulos/notas llevan sangría (`titulo-seccion`,
  `texto-nota`); Cambiar contraseña pasó de columnas `col-4` vacías a un panel único; en Acceso
  denegado el enlace "Volver al inicio" es un botón (`enlace-boton`). Los campos de fecha, hora y
  contraseña entraron al estilo de los campos de texto.
- **Respaldo:** carpeta `Respaldos\2026-10-06_resto_antes_del_estilo` con los `.bak` de las 12 pantallas
  y `Site.css`, más un `LEEME.txt` (los de Reportes llevan el prefijo `Reportes_`).
- **Verificación:** rebuild limpio, `aspnet_compiler -v /` sin errores, `div` abiertos y cerrados
  balanceados en las 17 pantallas tocadas y, con un login real, las 19 pantallas responden 200 con el
  contenedor de estilo, sin bordes viejos y sin errores. **No se vieron en el navegador**; conviene
  recorrerlas con F5, sobre todo Órdenes (con una orden abierta, para ver el detalle), Compras y Pagos.

### 2026-10-06 — Fase 6: estilo básico extendido al resto de los ABM y a Inicio

Se extendió a las demás pantallas de gestión el estilo básico probado en Clientes (ver la entrada
del 2026-10-05 más abajo). Solo markup y CSS: sin cambios de code-behind ni de designer.

- **Pantallas:** Vehículos, Proveedores, Insumos, Servicios, Usuarios e Inicio (Clientes ya lo tenía).
  El resto se hizo en la entrada "(cont.)" de arriba.
- **CSS genérico:** las clases pasaron de `pantalla-clientes`/`tabla-clientes` a `pantalla-abm`/
  `tabla-abm`; cada pantalla pone su contenido dentro de `<div class="pantalla-abm">`. Clases nuevas en
  `Content\Site.css`: `panel-gris` (panel de formulario fuera de la columna derecha, usado en
  Usuarios), `boton-chico` (botones en línea con un campo, Vehículos), `etiqueta-inline` (etiqueta
  junto a una casilla), `tabla-compacta` (grillas con muchas columnas, Insumos) y las de Inicio
  (`lista-accesos`, `texto-inicio`, `titulo-accesos`). También se estilaron `select`, `textarea`, el
  desplegable de resultados del buscador de dueño y se ocultaron los `hr`.
- **Casos particulares:** Insumos conserva el rojo de las filas con stock bajo (el código lo pone como
  estilo en la fila; hay una regla para que las celdas no lo tapen) y su panel "Ajustar stock" quedó
  dentro de su propio contenedor; Usuarios pasó de columnas `col-4` vacías a un panel único; la grilla
  de Insumos y su historial dejaron las clases `table table-striped...` de Bootstrap.
- **Inicio:** se quitó la frase "El menú de arriba muestra solo las secciones habilitadas para ese rol."
  (queda "Estás trabajando con el rol …") y los accesos pasaron de lista con viñetas a botones.
- **Respaldo para volver atrás:** carpeta `Respaldos\2026-10-06_ABM_antes_del_estilo` con los `.bak` de
  Clientes, Vehículos, Proveedores, Insumos, Servicios, Usuarios, Inicio (`Default.aspx.bak`, con la
  frase original) y `Site.css` en su estado anterior, más un `LEEME.txt`.
- **Verificación:** rebuild limpio, `aspnet_compiler -v /` sin errores y, con un login real, las 7
  pantallas responden 200 con el contenedor de estilo y sin errores. **No se vieron en el navegador.**

### 2026-10-05 (cont. 2) — Fase 6: estilo básico de CSS en Clientes

Restyling visual de `Clientes.aspx`, a pedido del usuario. Sin cambios de code-behind ni de designer
(mismos controles y `id`). Un primer intento con el estilo del Login se descartó y se revirtió; esta
versión es más simple.

- **Acotado a la pantalla:** el contenido va dentro de `<div class="pantalla-clientes">` y todas las
  reglas nuevas de `Content\Site.css` (bloque "Pantalla Clientes: estilo básico") cuelgan de esa clase,
  así que no afectan a ninguna otra pantalla.
- **Qué cambió:** se sacaron los bordes (`row border border-1`) de todas las filas; relleno general en
  el buscador y el formulario (paneles gris claro con esquinas redondeadas); etiquetas arriba de cada
  campo; inputs a ancho completo; grilla con encabezado negro, filas alternadas en gris claro, más
  aire en cada celda y resaltado rojo claro al pasar el mouse; botones en rojo (acción principal),
  gris (secundaria) y borde rojo (Borrar). Paleta rojo `#c0272d`, negro `#212529` y grises.
  Ajustes posteriores del mismo día: la tabla lleva líneas en todas las celdas (grilla completa) y el
  fondo de toda la página es gris claro (`#cdd0d4`; se probó un gris oscuro `#3a3e43` y se volvió atrás),
  con los blancos suavizados (paneles `#e6e8ea`, campos `#f2f3f4`, filas `#e6e8ea`/`#dcdfe2`) para que no
  brillen, y títulos más grandes y oscuros (`h1` 2.4rem y `h2` 1.7rem, ambos en negro `#0d0f11` y
  peso 800), aplicado con `html:has(.pantalla-clientes)` /
  `body:has(...)` para que no afecte a otras pantallas; las filas de la tabla se aclararon
  (`#fafafa` / `#eef0f1`) para distinguirse del fondo. `:has()` necesita un navegador moderno
  (Chrome/Edge 105+, Firefox 121+); en uno viejo solo se pinta el contenedor.
  **Por qué "no se veían los cambios":** la página enlaza `/Content/Site.css` sin número de versión
  (con `debug="true"` el bundle no lo agrega) y el navegador seguía usando la copia vieja en caché;
  el servidor ya entregaba el CSS nuevo (verificado con un login real y el HTML de Clientes). Se agregó
  a `Web.config` `<system.webServer><staticContent><clientCache cacheControlMode="DisableCache" />`
  — **solo para desarrollo, quitarlo o poner `UseMaxAge` antes de la entrega.**
- **Respaldo para volver atrás:** carpeta `Respaldos\2026-10-05_Clientes_antes_del_estilo` (fuera del
  proyecto web) con `Clientes.aspx.bak`, `Site.css.bak` y un `LEEME.txt` con los pasos.
- **Verificación:** rebuild limpio y `aspnet_compiler -v /` sin errores. **No se vio en el navegador**:
  revisar con F5 y Ctrl+F5 (caché del bundle `~/Content/css`). El BOM de `Clientes.aspx` y de
  `Site.css` se conservó.

### 2026-10-05 (cont.) — Fase 6: cierre de órdenes vacías, pagos que cancelan primero la deuda y saldo a favor

Corrige los dos hallazgos de la sesión de pruebas del 2026-10-03. Sin cambio de esquema.

**Cerrar una orden sin líneas.** `ComprobanteVentaDAL.GenerarDesdeOrden` (donde ya se leen las líneas)
rechaza si no hay ningún servicio ni insumo: "...no se puede cerrar. Cargá al menos uno, o cancelá la
orden si no corresponde cobrar nada." La orden queda como estaba y no se genera venta. Cancelar una
orden vacía ya funcionaba. `Cerrar` ahora propaga el mensaje de la venta.

**Pagos: primero se cancela la deuda.** Regla decidida con el usuario: un pago ya no se imputa a un
comprobante elegido a mano. `PagoDAL.Registrar` lee los comprobantes con `saldoPendiente > 0` del
titular (más viejo primero, desempate por id), reparte el monto y el sobrante queda "a cuenta" (cuenta
corriente a favor, sin tope). Vale igual para clientes (ventas) y proveedores (compras).
- **Opción A, sin cambio de esquema:** como `Pago` tiene un solo `idVenta`/`idCompra`, un pago que toca
  N comprobantes se guarda como N filas de `Pago` (una por comprobante, más una sin comprobante para
  el sobrante), cada una con su movimiento de cuenta corriente (`idPago` propio) — todo en un solo
  batch atómico. Así queda trazado qué venta/compra canceló cada parte; el costo es que un pago
  aparece como varias líneas en la grilla de Pagos y en la cuenta corriente.
- **Se sacó el tope** "El monto supera el saldo pendiente": ahora el exceso es válido (queda a favor).
- `Pagos.aspx`: sin `ddlComprobante`; muestra "Deuda actual / Saldo a favor / Sin deuda" del titular
  elegido y un texto de ayuda. El mensaje del resultado cuenta el reparto ("Se aplicó a: V-000002
  (11.000,00), V-000005 (1.000,00). Quedaron 2.000,00 a favor.").

**Saldo a favor en ventas y compras nuevas.** Si el cliente tiene la cuenta corriente en negativo, la
venta que se genera al cerrar una orden nace con `saldoPendiente = total − crédito` (el débito de la
venta ya netea la cuenta corriente, sin movimiento extra). Lo mismo para una compra a cuenta corriente
con un proveedor al que se le pagó de más. Los mensajes lo informan. Limitación conocida: el crédito se
toma de la cuenta corriente completa, así que un ajuste manual a favor con deudas pendientes sin
cancelar se descuenta de la venta nueva y no de la más vieja.

**Datos existentes: sin script de migración.** Se escribió un script para reconciliar los
`saldoPendiente` viejos con la nueva regla y se descartó: el usuario aclaró que los datos de la base
de desarrollo no importan, solo hacen falta los scripts para crearla (`01`, `02`) y cargar datos
genéricos (`03`, `04`). Los datos demo de `04` ya cumplen la regla nueva (se comprobó que una
reconciliación no cambiaba nada), así que **no hizo falta tocar ningún script de `Database/`**. Para
llevar una base con datos viejos a la regla nueva basta recrearla (`01` a `04`).

**Verificación:** rebuild limpio y `aspnet_compiler -v /` sin errores. Probado contra una base
descartable (`01`→`04` con otro nombre, ya borrada) con un programa de consola que llama a los DAL
reales: pago que cubre dos ventas con sobrante, pago menor y exacto, pago sin deuda, pago a proveedor,
venta con crédito aplicado, compra con crédito aplicado, cierre de orden vacía rechazado, cancelar
orden vacía, monto 0 rechazado — todo correcto. **No se probó en el navegador** (IIS Express/F5): faltan las pantallas Pagos y
Órdenes. Los `.aspx` perdieron el BOM al editarlos y se restauró.

### 2026-10-05 — Fase 6: "Quitar" insumo de una orden y estado fijo en el alta de Turnos

Dos correcciones de pulido pedidas por el usuario, ambas sobre pantallas ya entregadas. Sin cambio
de esquema.

**"Quitar" una línea de insumo de una orden.** Hasta hoy no existía: era una decisión de diseño de
Fase 3 (si había un error, se cancelaba la orden entera), no un bug. Se agregó
`DetalleOrdenInsumoDAL.Quitar(idDetalle, idUsuario)` (más `ObtenerPorId`), que invierte `Agregar`
en un solo batch `XACT_ABORT`/`BEGIN TRAN`/`COMMIT`: repone `Insumo.stockActual`, inserta la entrada
en `MovimientoStock` y borra la línea. Decisiones:

- **Solo con la orden Abierta o En proceso, y la guarda vive en el DAL**, no solo en la UI (el botón
  además se esconde en estados terminales). Cerrada: la venta ya copió las líneas. Cancelada: el
  stock ya se repuso entero, quitar una línea lo repondría dos veces.
- **El kardex usa `TipoCancelacionOrden`** (ya admitido por `CK_MovStock_origen`), distinguido por
  la descripción ("Reposición por quitar insumo de la orden #N"), en vez de un tipo nuevo — evita
  tocar el CHECK de `01_Esquema.sql` y migrar la base en uso.
- UI: columna "Acciones" con "Quitar" y confirmación en `gvInsumosOrden`, calco de `gvServicios`;
  `gvInsumosOrden_RowCommand` corta con `EsSoloLectura` y refresca catálogo (el combo muestra stock)
  y detalle. Sin controles nuevos, el designer no cambió.
- Cruces revisados: Cancelar orden repone solo las líneas que sigan cargadas (sin doble reposición);
  Cerrar/Ventas copia las líneas existentes al cerrar; el invariante de kardex se mantiene.
- **Efecto a tener presente:** ahora se puede quitar la última línea y cerrar, lo que facilitaba
  caer en la venta de $0 — corregido ese mismo día (ver la entrada "cont.").

**Turnos: estado fijo en el alta.** `TurnoDAL.Crear` ya forzaba `Solicitado`, pero el formulario
dejaba elegir cualquier estado y descartaba la elección sin avisar. Ahora, en el alta, se muestra el
texto fijo "Solicitado" (`litEstadoNuevo`, control nuevo) y `ddlEstado` solo aparece al editar un
turno existente (`Page_Load`, `LimpiarFormulario` y `Seleccionar`). `ddlEstado` conserva sus opciones
cargadas aunque esté oculto, para no repetir el bug de postback de un `DropDownList` sin `<option>`.
Se revisaron el resto de las pantallas con desplegables: Órdenes ya lo hacía bien (`pnlEstado` oculto
en el alta), y los demás (Compras, Pagos, Usuarios, Vehículos) son datos que sí se guardan.

**Verificación:** rebuild limpio (`MSBuild`, Debug) y `aspnet_compiler -v /` sin errores en ambos
cambios. **No se probó contra IIS Express** — queda para F5 (agregar insumo ×3, quitar y ver stock
y kardex; quitar con dos líneas del mismo insumo; cancelar tras quitar; Turnos: alta, editar,
"Nuevo"). Nota: la herramienta de edición saca el BOM de los `.aspx`; se restauró a mano en
`OrdenesDeTrabajo.aspx` y `Turnos.aspx` (ver «Codificación» en `CLAUDE.md`).

### 2026-10-03 — Arranca Fase 6: pruebas de flujo completo (Turno → Orden → Venta → Pago → Cuenta corriente)

Primera sesión de Fase 6. Alcance acordado con el usuario: solo el frente "pruebas de flujo
completo", sin tocar código — los bugs que aparecieran quedan documentados acá para una sesión
futura de "validaciones cruzadas" (el otro frente de Fase 6). Antes de probar se investigó el
código real (`TurnoDAL`, `OrdenDeTrabajoDAL`, `DetalleOrdenServicioDAL`, `DetalleOrdenInsumoDAL`,
`ComprobanteVentaDAL`, `PagoDAL`, `CuentaCorrienteClienteDAL`) cruzado contra
`Docs/Lubricentro_Requerimientos.md`, para saber qué casos borde ejercitar a propósito.

**Metodológico: sin browser real disponible, otra vez, pero por un motivo distinto al de
sesiones anteriores.** Esta sesión sí tenía herramientas de automatización de Chrome
(`claude-in-chrome`) cargadas, pero el Chrome que controlan corre en una red distinta a la de
este entorno (llega a internet, no a `localhost:8123` donde corre IIS Express acá) — se descartó
en los primeros minutos con una prueba directa. Se volvió al método ya usado en sesiones
anteriores: requests HTTP armados a mano con PowerShell (`Invoke-WebRequest` con `-WebSession`,
extrayendo `__VIEWSTATE`/`__EVENTVALIDATION`/`__EVENTTARGET` de cada respuesta). Dato nuevo para
la próxima vez que se necesite este método: los campos dentro de un `<asp:UpdatePanel>` casi
siempre **no** llevan el ID del UpdatePanel como prefijo en su `name` renderizado (confirmado
mirando el HTML real en Vehículos/Turnos/Pagos) — conviene volcar todos los `name="..."` de la
página con una regex amplia antes de adivinar un prefijo, en vez de asumir por analogía con otra
pantalla.

**Datos de prueba:** un Cliente, un Vehículo, un Servicio y un Insumo dedicados (sufijo "E2E"),
más un Turno, creados y editados vía los ABM reales (no por SQL directo). Todo borrado al cerrar
la sesión — los conteos de tabla volvieron exactos a los de antes de empezar.

**Pasada A (con turno):** Turno `Solicitado` → editado a `Confirmado` → Orden de trabajo creada
desde ese turno (vía el atajo `?idVehiculoNuevo=&idCliente=&idTurno=` que ya usa el flujo de
"Nuevo vehículo desde Órdenes") → línea de servicio (x2) + línea de insumo (x3, bajó stock
100→97) → **Cerrar orden** → Venta `V-000005` generada por $3.500 (2×$1.000 + 3×$500),
`CuentaCorrienteCliente` con `debe=3.500`. Pago parcial de $2.000 imputado a esa venta
(`saldoPendiente` 3.500→1.500) + pago de $1.500 "a cuenta general" (sin comprobante puntual) →
saldo de cuenta corriente en $0. Todo correcto.

**Pasada B (walk-in, sin turno):** misma cliente/vehículo, orden creada sin `idTurno` → una línea
de servicio → Cerrar → Venta `V-000006` por $1.000. Correcto. (No se re-probó el alta en cascada
de cliente/vehículo nuevos desde cero — ya extensamente verificada en la sesión de Fase 3 — esta
pasada reusó las entidades E2E para enfocarse en el camino "sin turno" del lado de la orden.)

**Casos borde ejercitados (ver tabla de pendientes en sección 4 para el único que resultó bug
real):**

- **Cerrar una orden sin ninguna línea → BUG CONFIRMADO.** Generó `V-000007` por $0,00 con un
  movimiento de `CuentaCorrienteCliente` `debe=0/haber=0` — exactamente el gap que había
  anticipado la lectura de código (`OrdenDeTrabajoDAL.Cerrar`/`ComprobanteVentaDAL.
  GenerarDesdeOrden` no chequean que la orden tenga líneas). Queda en sección 4.
- Cerrar o cancelar una orden ya `Cerrada`: la UI ni siquiera renderiza los botones
  `btnCerrarOrden`/`btnCancelarOrden` para una orden terminal (confirmado inspeccionando los
  campos de la página, no solo leyendo el código) — doble resguardo con el bloqueo del DAL ya
  confirmado por lectura de código. No es un bug.
- Cancelar una orden `Abierta` con un insumo cargado: stock reconstruido correctamente
  (92→97) y kardex completo y correcto (`Orden` seguido de `CancelacionOrden` con las cantidades
  exactas). No es un bug.
- Pagar más del saldo pendiente de una venta puntual (intento de $1.500 contra una venta con
  $1.000 pendientes): rechazado con "El monto supera el saldo pendiente de la venta.", sin
  insertar ninguna fila en `Pago`. No es un bug.
- Pago "a cuenta general" sin tope: se aceptó sin problema (confirmado con el pago de $1.500 de
  la Pasada A). **Confirmado con el usuario como comportamiento intencional, no bug** — un
  cliente puede dejar un anticipo mayor a lo que debe puntualmente.
- Rol Lectura en Turnos/Órdenes/Pagos (con el usuario de prueba `lectura@lubricentro.com`): las
  tres pantallas siguen sin botón de escritura y sin columna Acciones — sin regresión del bug
  histórico corregido en la sesión de Alta pública de usuario.

**Hallazgo nuevo, no bug pero sí confuso — anotado en sección 4:** `ComprobanteVenta.
saldoPendiente` y el saldo corrido de `CuentaCorrienteCliente` son dos campos independientes (ya
señalado como posible punto débil al leer `PagoDAL.Registrar`). Se confirmó en la práctica: tras
el pago "a cuenta general" de la Pasada A, la cuenta corriente del cliente quedó en $0, pero
`ComprobanteVenta` de `V-000005` sigue mostrando `saldoPendiente = 1.500` porque ese pago nunca
apuntó a esa venta puntual. Es coherente con el diseño (un pago a cuenta general no se imputa a
ningún comprobante), pero `Ventas.aspx` seguiría mostrando esa venta como "con saldo pendiente"
aunque el cliente ya no deba nada en términos generales — puede confundir a quien mire solo esa
pantalla.

**Verificación:** rebuild limpio (`MSBuild`, Debug) antes de empezar. Cada paso confirmado con una
consulta `sqlcmd` de solo lectura contra LocalDB (stock, saldo, `saldoPendiente`, numeración de
comprobantes, kardex). Al cerrar, limpieza completa vía un solo batch SQL transaccional (orden de
dependencia inversa) y los conteos de las 9 tablas tocadas volvieron exactos a los de antes de
empezar la sesión.

### 2026-09-28 — Segundo frente de Fase 6: revisión de permisos por rol, sin gaps

Auditoría de solo lectura sobre las ~17-18 pantallas de negocio, contra la matriz de permisos de
`Docs/Lubricentro_Requerimientos.md` §5 + §9.6 (4 roles: Admin/Encargado/Empleado/Lectura).

**Qué se chequeó:** (1) que las 21 filas de `MenuNivel` en `02_DatosIniciales.sql` coincidan con
la matriz — qué pantalla ve cada rol y en cuál tiene `soloLectura=1`; (2) que las 17 pantallas de
negocio+Usuarios+Reportes hereden la clase base correcta (`PaginaSegura` vs `PaginaConSesion` vs
`Page` público) — un error acá sería hueco de autenticación, no sólo de permisos; (3) que en las
11 pantallas con banda "solo consulta" (Clientes, Vehículos, Turnos, Órdenes, Proveedores,
Insumos, Servicios, Compras, las 2 Cuentas corrientes, Pagos), **cada** método de escritura
arranque con `if (EsSoloLectura) return;` — no sólo que la UI esconda el panel (mismo patrón de
bug ya encontrado dos veces: rol Lectura 22/09, estado de orden 27/09).

**Resultado: 17/17 pantallas OK, 0 gaps reales.** El mecanismo base (`PaginaSegura` +
`MenuDAL.ObtenerPermiso`, `INNER JOIN MenuNivel`) bloquea por URL forzada sin necesitar código por
pantalla — sin fila en `MenuNivel` para (rol, ruta), `PaginaSegura.OnPreInit` redirige a
`AccesoDenegado` antes de que corra `Page_Load`. Único punto a tener presente, no es un bug: los
6 rubros de "Insumos, proveedores, compras" con solo-consulta para Empleado en la matriz del
documento en los hechos son 6 pantallas (se sumó **Servicios** al mismo criterio de catálogo,
extensión intencional ya documentada, no una desviación).

Ventas y las 3 pantallas de Reportes no necesitan `EsSoloLectura` — Ventas no tiene ningún método
de escritura (confirmado), Usuarios/Reportes son acceso binario (0 filas en `MenuNivel` para
Empleado/Lectura, bloqueados por el `INNER JOIN` sin código extra).

**Sigue Fase 6:** mejora de mensajes de error/UX, documentación final, y probar los 3 reportes de
Fase 5 contra IIS Express (sigue sin hacerse, ver sección 4).

### 2026-09-27 — Arranca Fase 6: primera prueba de flujo completo, encuentra un bug real

Primer frente de Fase 6 (integración, pruebas y pulido): pruebas de flujo completo Turno → Orden
→ Cierre de orden → Venta → Pago → Cuenta corriente, con validaciones cruzadas entre módulos.

**Cómo se probó, ya que no hay proyecto de tests ni forma de llegar a IIS Express desde WSL.** Se
armó un harness de consola descartable (no commiteado, vive fuera del repo): un `.csproj` de
consola clásico (mismo `TargetFrameworkVersion v4.7.2`) que referencia el `BIZ.dll` ya compilado
y trae su propio `App.config` con la misma cadena de conexión a LocalDB que usa la app real. Llama
directo a los métodos de `BIZ/Data` (no HTTP) — más fiel que reimplementar el SQL a mano, porque
también ejercita las validaciones de C#, no sólo el esquema.

**Flujo feliz, todo correcto:** cliente/vehículo/insumo/servicio/turno de prueba → orden desde el
turno → agregar 1 servicio + 3 unidades de insumo (stock 10→7, con `MovimientoStock` correcto) →
intento de agregar 999 unidades rechazado por stock insuficiente → cerrar la orden (genera la
venta, subtotal/total 2500 = 1 servicio a 1000 + 3 insumos a 500) → pago parcial de 1000 (saldo
pendiente de la venta 2500→1500, mismo movimiento reflejado en `CuentaCorrienteCliente`) → intento
de pagar de más rechazado → intento de cancelar la orden ya Cerrada rechazado.

**Bug real encontrado, mismo patrón que el hallazgo del rol Lectura (22/09): la UI escondía el
control, pero el DAL no tenía guarda propia.** `btnAgregarServicio_Click`,
`btnAgregarInsumo_Click` y `gvServicios_RowCommand` (Quitar) en `OrdenesDeTrabajo.aspx.cs` sólo
chequeaban `EsSoloLectura` — nunca si la orden ya estaba `Cerrada`/`Cancelada`. El panel se oculta
en la UI cuando la orden es terminal (`pnlAgregarServicio.Visible = !esTerminal`), pero un POST
directo contra `DetalleOrdenServicioDAL.Agregar`/`Quitar` o `DetalleOrdenInsumoDAL.Agregar` lo
aceptaba igual: se podía agregar una línea de insumo a una orden ya Cerrada (descontando stock
real, sin que apareciera en la venta ya generada — huérfano) o a una ya Cancelada (revirtiendo en
silencio lo que `Cancelar` había repuesto). Reproducido primero con el harness (`FAIL`, el insumo
bajó de 7 a 5 tras "cerrar" la orden), después corregido.

**Corrección:** `DetalleOrdenServicioDAL` gana `ValidarOrdenEditable(idOrden)` (privado), usado en
`Agregar` y en `Quitar` (que antes ni siquiera sabía a qué orden pertenecía la línea — ahora la
busca primero). `DetalleOrdenInsumoDAL.Agregar` hace el mismo chequeo en línea (no se compartió el
helper entre clases — mismo criterio ya asentado en el proyecto de preferir una pequeña
duplicación a acoplar dos DAL entre sí). Las tres rutas devuelven
`"Una orden <estado> no admite cambios en el detalle."` en vez de escribir.

**Verificación:** rebuild limpio (`MSBuild`) + `aspnet_compiler -v /` sin errores. Reproducido el
bug y confirmada la corrección con el mismo harness contra LocalDB (las 3 rutas rechazan, stock
sin cambios); todo el resto del flujo feliz sigue en verde tras el fix. Datos de prueba del
harness borrados de LocalDB al cerrar la sesión (verificado sin huérfanos en `MovimientoStock`).

**Sigue Fase 6:** revisión de permisos por rol pantalla por pantalla, y los pendientes de la
sección 4. Sin acordar todavía si hay más validaciones cruzadas para revisar en Turnos/Compras
(mismo patrón "UI esconde, DAL no valida" podría repetirse ahí — no se auditó esta sesión).

### 2026-09-22 (cont. 2) — Tercer reporte de Fase 5: Cuentas corrientes, cierra la fase

Sobre `CuentaCorrienteCliente`/`Proveedor` y sus DAL. A diferencia de Stock bajo, este también
necesitaba diseño acordado (Ventas por período también lo necesitó) — decidido con el usuario:
entran clientes y proveedores con saldo distinto de cero, cualquier signo (deuda **o** a favor),
no un listado completo ni sólo deudores.

**Sin filtro de fechas** (a diferencia de Ventas por período): es una foto del saldo actual, no
un rango — no había nada de tiempo que acotar.

**`ListarSaldos()`, método nuevo en los dos DAL** (`CuentaCorrienteClienteDAL` y
`CuentaCorrienteProveedorDAL`, mismo patrón en los dos). Trae el último movimiento de cada
cliente/proveedor con `CROSS APPLY` en vez de una subquery correlacionada por columna:

```sql
SELECT c.idCliente, c.nombre + ' ' + c.apellido AS nombreCliente, u.saldo, u.fecha
FROM Cliente c
CROSS APPLY (
    SELECT TOP 1 saldo, fecha FROM CuentaCorrienteCliente
    WHERE idCliente = c.idCliente ORDER BY idMovimiento DESC
) u
WHERE u.saldo <> 0
ORDER BY u.saldo DESC
```

El `CROSS APPLY` hace dos cosas a la vez: trae el saldo más reciente y excluye de entrada a
quien nunca tuvo un movimiento (no hay fila del lado derecho para comparar contra `<> 0`) — no
hace falta un `LEFT JOIN` + `WHERE ... IS NOT NULL` aparte. Mismo criterio de signo que ya regía
en las pantallas de Fase 4: para Cliente, positivo = debe; para Proveedor, positivo = le
debemos (confirmado releyendo `ComprobanteCompraDAL.Crear`, que carga la compra a cuenta
corriente como `debe`).

**Pantalla con dos secciones independientes, no una sola grilla combinada.** Clientes y
Proveedores son entidades distintas con su propio DAL — mezclarlas en una grilla habría
necesitado una columna "tipo" artificial y un modelo de fila que no es ninguna de las dos
entidades reales. Cada sección repite el patrón resumen + grilla paginada ya establecido, con su
propio `CargarClientes`/`CargarProveedores` y su propio `PageIndexChanging` (la paginación de una
sección no debe reiniciar la otra).

**Resumen con dos cláusulas condicionales, no una** (Stock bajo y Ventas por período tenían como
mucho una cláusula extra opcional). Acá el conjunto ya viene mezclado en signo, así que el
resumen separa cuántos deben y cuánto suman, y cuántos están a favor y cuánto suman — cualquiera
de las dos cláusulas puede faltar si ese lado quedó vacío (ej.: todos los saldos son deudas, cero
a favor).

**Sin resaltado de filas**, a diferencia de Stock bajo (insumos en cero) y Ventas por período
(saldo pendiente). Ahí el resaltado marcaba un subconjunto más urgente *dentro de* una lista ya
filtrada a algo más amplio. Acá la lista ya viene filtrada a exactamente lo que importa (saldo
≠ 0) — no hay un subconjunto adicional que separar visualmente.

**`pnlSoloLectura` ya no existe** (ver la entrada de Santi más abajo, "Alta pública de usuario,
rol Lectura..." — sacó el banner de las 3 pantallas de Reportes mientras yo tenía este trabajo
sin commitear): esta pantalla se construyó y luego se rebaseó sobre ese cambio, sin el banner.

**Verificación:** rebuild limpio de la solución (`MSBuild`, Debug) y `aspnet_compiler -v /` sin
errores — designer.cs sincronizado con el markup, en los dos DAL y en la pantalla. No se corrió
contra IIS Express en esta sesión (WSL no llega a esos puertos).

**Cierra Fase 5.** Los tres reportes de Requerimientos §6.9 (Stock bajo, Ventas por período,
Cuentas corrientes) están hechos. Sigue Fase 6 — ver Roadmap.

### 2026-09-22 (cont.) — Segundo reporte de Fase 5: Ventas por período

Sobre `ComprobanteVenta`/`ComprobanteVentaDAL`. A diferencia de Stock bajo, este sí necesitaba
diseño acordado antes de tocar código (filtros, formato de salida) — decidido con el usuario:
filtro de rango de fechas (`Desde`/`Hasta`) y formato resumen + detalle, igual que Stock bajo.

**Filtro de fechas:** dos `<asp:TextBox TextMode="Date">` (mismo patrón que `txtFecha` en
Turnos.aspx — HTML5 `type="date"`, postea `yyyy-MM-dd` siempre, se parsea con
`DateTime.TryParseExact` + `CultureInfo.InvariantCulture`, sin depender de la cultura del
navegador ni del servidor). Default al entrar a la pantalla: primer día del mes en curso hasta
hoy, para no mostrar la grilla vacía en el primer ingreso. `CompareValidator` (`Type="Date"`,
`Operator="GreaterThanEqual"`, ya usado en el proyecto para confirmar contraseña — ver
CambiarClave/RestablecerClave) rechaza `Hasta` anterior a `Desde` antes de ir al servidor.

**`ComprobanteVentaDAL.ListarPorPeriodo(desde, hasta)`, método nuevo.** Reutiliza el
`SelectBase` ya existente (mismo JOIN con Cliente/OrdenDeTrabajo/Vehiculo que `Listar`/`Buscar`).
`fecha` es `DATETIME` (`DEFAULT GETDATE()`, con hora), así que el filtro compara
`>= @desde AND < @hastaExclusiva` (`hasta.Date.AddDays(1)`) en vez de contra `@hasta` a secas —
si no, se pierden las ventas cargadas después de la medianoche del último día elegido.

**Resumen + grilla, mismo patrón que Stock bajo:** texto arriba con cantidad de ventas y total
del período, más una cláusula extra con el total pendiente de cobro si `SaldoPendiente` suma más
de cero (mismo criterio condicional que el "de ellos sin stock" de Stock bajo). Grilla paginada
(`PageSize=30`, mismo `PagerTemplate`) con las columnas ya usadas en `Ventas.aspx` (Número,
Fecha, Cliente, Vehículo, Total, Saldo pendiente) menos la columna Acciones — acá no hay
detalle por fila, es un listado del período, no un maestro-detalle. Filas con saldo pendiente
resaltadas en rojo (`RowDataBound`), el mismo criterio visual de "solo se resalta lo que necesita
atención" que Stock bajo con los insumos en cero.

**Verificación:** rebuild limpio de la solución (`MSBuild`, Debug) y `aspnet_compiler -v /` sin
errores — designer.cs sincronizado con el markup. No se corrió contra IIS Express en esta
sesión (WSL no llega a esos puertos — ver más abajo).

### 2026-09-21/2026-09-22 — Alta pública de usuario, rol Lectura, y limpieza de indicadores "solo consulta"

Trabajo fuera de las 6 fases del Roadmap, a pedido explícito del usuario: una vía para que
cualquier visitante se cree su propia cuenta desde `Login.aspx`, sin depender de un Admin. Sin
precedente en `Docs/Lubricentro_Requerimientos.md` (§10 da por fuera de alcance cualquier portal
público) — se documentó como decisión nueva en §9.6.

**`~/Registro` (nuevo).** Pantalla pública (no hereda `PaginaSegura`, mismo molde visual que
`RecuperarClave`/`RestablecerClave`: `login-page`/`login-card`/`login-stripe`): nombre, apellido,
mail, contraseña + repetir. `UsuarioDAL.Registrar` (nuevo, junto a `Crear`) valida, chequea mail
duplicado y hashea la contraseña que el visitante eligió — a diferencia de `Crear` (ABM de
Usuarios), que siempre genera una temporal y la manda por mail. Al terminar, inicia sesión
automáticamente y redirige a `~/Default`, igual que un login exitoso. **El rol que recibe cambió
en la propia sesión:** primero se implementó con `Nivel.Empleado` (el más bajo de los 3 roles
existentes en ese momento), y a pedido posterior del usuario se creó un rol nuevo más restringido
(`Nivel.Lectura`) y se cambió `Registrar` para asignar ese en vez de Empleado.

**Rol nuevo `Lectura` (`idNivel = 4`, jerarquía por debajo de Empleado).** Ve las mismas 17
pantallas que ve Empleado (todo menos Administración y Reportes), pero en modo solo consulta en
**absolutamente todas** — incluidas Clientes, Vehículos, Turnos, Órdenes, Ventas y Pagos, donde
Empleado sigue teniendo alta/edición completa. La fila de `MenuNivel` para Lectura en
`02_DatosIniciales.sql` es el mismo `WHERE` que ya excluía Administración/Reportes para Empleado,
pero con `soloLectura = 1` en las 17 filas en vez de en 6.

**Bug real encontrado al construir esto, no solo cosmético.** `PaginaSegura.EsSoloLectura` ya era
genérico (calculado por `MenuDAL.ObtenerPermiso` desde `MenuNivel.soloLectura`), y `Turnos`,
`OrdenesDeTrabajo` y `Pagos` ya mostraban un banner "solo consulta" cuando ese flag daba `true` —
pero **ningún método de escritura lo chequeaba**: el banner era decorativo nada más, el botón de
Guardar seguía funcionando. Quedó sin detectar hasta ahora porque ningún rol seedeado tenía
`soloLectura = 1` en esas tres pantallas todavía. Se corrigió agregando `if (EsSoloLectura)
return;` al principio de cada handler de escritura (Guardar, Cancelar/Cerrar orden, agregar
servicio/insumo, registrar pago) más ocultar el panel del formulario (`pnlFormulario`, nuevo en
estas 3) y la columna "Acciones" de la grilla — mismo patrón ya establecido en
`Proveedores.aspx.cs`. `Clientes.aspx`/`Vehiculos.aspx` no tenían ni el banner: se les agregó el
patrón completo desde cero (nuevo también ahí `pnlFormulario`).

**Verificado con el peor caso a propósito, no solo con la UI oculta:** reusar el `__VIEWSTATE` de
una vista de Admin (formulario completo habilitado) posteado con la cookie de sesión de un usuario
Lectura — el servidor acepta el postback (event validation no lo rechaza, porque ese viewstate sí
tenía el control registrado como válido) pero no escribe nada en la base, confirmando que la
guarda real es el `if (EsSoloLectura) return;` del código, no el ocultamiento visual del panel.

**Los 3 scripts de `Database/` se pusieron al día y se re-verificaron de punta a punta, no solo el
cambio incremental de antes.** `01_Esquema.sql`: sin cambio de esquema (`Nivel` ya es genérica, sin
`CHECK` de roles), solo un comentario. `02_DatosIniciales.sql`: fila de `Nivel` + bloque de
`MenuNivel` de Lectura. `03_UsuariosDePrueba.sql`: tercer usuario
(`lectura@lubricentro.com` / `Lectura123!`) con hash/salt generados de verdad en PowerShell
replicando `PasswordHasher.cs` (PBKDF2-SHA256, 25.000 iteraciones, salt 16 bytes, hash 32) en vez
de inventados. Verificado corriendo `01 → 02 → 03` contra una base descartable
(`LubricentroControlVerifTmp`) y logueándose de verdad con esas credenciales contra la app
corriendo, antes de tocar la base de desarrollo real. A pedido explícito del usuario, se corrieron
después los 4 scripts (`01` a `04_DatosDemo.sql`) contra la base de desarrollo real — recrea el
esquema y repone los mismos 10 registros de ejemplo por entidad que ya tenía.

**Los indicadores visuales de "solo consulta" se sacaron de la interfaz, en 3 pedidos separados
del usuario, sin tocar la restricción real.** El badge `<span class="badge">consulta</span>` que
`Site.Master.cs` agregaba a cada ítem del menú desplegable (`RenderizarGrupo`); el banner
`pnlSoloLectura` ("Tu rol tiene acceso de solo consulta a esta pantalla") de las 15 pantallas que
lo tenían — sacado con una pasada de PowerShell sobre los 3 archivos de cada pantalla
(`.aspx`/`.aspx.cs`/`.aspx.designer.cs`), preservando el BOM de cada archivo; y el sufijo
`" (solo consulta)"` que `Default.aspx.cs` agregaba a cada ítem de la lista "Tus accesos" del
Inicio. En las 3 pantallas de Reportes (todavía cascarón "Pendiente") sacar el banner dejó
`Page_Load` vacío — se eliminó el método entero en vez de dejar un cascarón sin usar.

**Verificación, en cada paso:** rebuild limpio + `aspnet_compiler` sin errores. Contra IIS Express
+ LocalDB: alta por `~/Registro` con mail nuevo (queda en rol Lectura, sesión iniciada, menú
restringido correcto), mail duplicado y contraseña corta rechazados: menú del rol Lectura sin
Usuarios/Reportes en las 12 pantallas con escritura restringida — banner ausente, botón de
Guardar/Registrar ausente, columna Acciones ausente — y el replay de viewstate ya descripto arriba
sin escribir nada; Admin sigue viendo el formulario completo en Proveedores sin regresión; los 4
scripts SQL corridos en secuencia sobre una base descartable con los conteos esperados en
`Nivel`/`MenuNivel`/`Usuario`. Usuarios de prueba (`empleado.prueba.*@`, `lectura.prueba@`,
`verif.rol.lectura@`) borrados de la base real al cerrar cada verificación —
**`lectura@lubricentro.com` sembrado por `03_UsuariosDePrueba.sql` sí quedó en la base real**, ver
sección 4.

### 2026-09-15 (noche) — Arranca Fase 5: reporte de Stock bajo

Primer reporte de Fase 5, sobre `InsumoDAL.ListarStockBajo()` (escrita desde Fase 2, sin usar
hasta ahora). Sin capa BIZ nueva ni cambio de esquema: el acceso (`Url`/`Menu`/`MenuNivel` de
`~/Reportes/StockBajo`, sin acceso para Empleado — Requerimientos §5) ya estaba desde que se armó
el menú en Fase 1; esta sesión solo llenó el cascarón con la pantalla.

**El único de los 3 reportes sin nada de diseño que acordar primero:** sin filtros ni parámetros,
orden por faltante (`stockMinimo - stockActual`) de mayor a menor con desempate alfabético,
resumen arriba (cuántos insumos bajo el mínimo, cuántos en cero) y esas filas en cero resaltadas
en rojo en la grilla. `VentasPorPeriodo` y `CuentasCorrientes` siguen como cascarón — esos dos sí
necesitan decidir rango de fechas/formato de salida antes de tocarlos.

**Verificación:** rebuild limpio de la solución (`MSBuild`, Debug) y `aspnet_compiler -c` sin
errores — designer.cs sincronizado con el markup. No se corrió contra IIS Express en esta
sesión; queda pendiente el paso a mano si se quiere el mismo nivel de verificación que las
pantallas de Fase 4.

### 2026-09-15 — Rediseño visual de Login / RecuperarClave / RestablecerClave

Restyling puramente visual de las tres pantallas públicas (`Login.aspx`, `RecuperarClave.aspx`,
`RestablecerClave.aspx`) y las reglas nuevas en `Content/Site.css` (bloque `/* ===== Login
===== */`). Sin cambios de code-behind ni de lógica: mismos controles (`asp:TextBox`,
`asp:RequiredFieldValidator`, `asp:CustomValidator`/`CompareValidator`, `ValidationGroup`),
mismos `id`, mismo flujo de postback — solo se reemplazó la grilla de `<div class="row border
border-1">` heredada del template por una tarjeta oscura centrada (`.login-page`/`.login-card`,
franja de gradiente superior, inputs y botón con la paleta de `Site.css`). Las tres pantallas
comparten las mismas clases para quedar visualmente idénticas entre sí.

### 2026-09-15 (cont.) — Envío real de mails por SMTP (Gmail)

Habilitado el envío real de `ServicioMail.Enviar`, a pedido del usuario. No hizo falta tocar
ningún `.cs`: la rama de envío real ya estaba escrita (`new SmtpClient()` sin argumentos, que lee
`<system.net>/<mailSettings>/<smtp>` de `Web.config`); lo único que faltaba era ese nodo.

**Config partida en dos archivos, para no commitear la contraseña.** `Web.config` ahora tiene
`<system.net><mailSettings><smtp configSource="Web.MailSettings.config" /></mailSettings>
</system.net>` — `configSource` es un mecanismo nativo de `System.Configuration` (sin
dependencias nuevas) que delega una sección entera a un archivo aparte. Ese archivo
(`Web.MailSettings.config`, con el host/usuario/contraseña de Gmail reales) se gitignoreó; se dejó
un `Web.MailSettings.config.example` commiteado con placeholders y los 4 pasos para completarlo
(activar verificación en 2 pasos, generar una contraseña de aplicación en
`myaccount.google.com/apppasswords`, completar `userName`/`password`, y alinear
`MailRemitente`/`MailModoDesarrollo` en `Web.config`).

**Proveedor elegido: Gmail con contraseña de aplicación**, puerto 587 + `enableSsl="true"`
(STARTTLS) — `SmtpClient` de `System.Net.Mail` no soporta bien SSL directo en 465. `MailRemitente`
tiene que ser la misma cuenta que se autentica en el SMTP: Gmail reescribe/rechaza el `From` si no
coincide con la cuenta autenticada (o un alias verificado) — quedó documentado como comentario en
el propio `Web.config`.

**Verificado en dos niveles:** un envío de prueba directo con `System.Net.Mail.SmtpClient` desde
PowerShell (autenticación y entrega confirmadas), y después contra el IIS Express que el usuario
ya tenía corriendo en el puerto 8123 — `GET /Login` y `GET /Content/css` devolviendo el HTML/CSS
esperado (esto último fue para descartar, a pedido del usuario, que el cambio de mail hubiera
afectado el rediseño de Login/RecuperarClave/RestablecerClave que se lo veía tocado en el working
directory: no tenía nada que ver, era caché del navegador sobre el bundle `~/Content/css`).

**Estado actual, a tener presente antes de commitear:** `Web.config` quedó con
`MailModoDesarrollo=false` y `MailRemitente` apuntando a la cuenta de Gmail real que se usó para
probar — o sea, ahora mismo el sistema manda mails reales, no `.txt`. El default seguro para el
resto del equipo/corrector sigue siendo `MailModoDesarrollo=true` (así lo documenta `CLAUDE.md`);
falta decidir si se vuelve a `true` antes de commitear o si se deja así a propósito. Ver también
sección 4, "Pendientes conocidos".

### 2026-09-15 — Script de datos de ejemplo (`Database/04_DatosDemo.sql`)

Nuevo script, a pedido del usuario, para no tener que cargar datos de prueba a mano desde la UI:
10 filas de ejemplo en cada entidad de negocio (Clientes, Vehículos, Proveedores, Servicios,
Insumos, Turnos, Órdenes de trabajo, Compras), más el circuito de dinero/stock derivado armado a
mano (4 Ventas generadas al cerrar órdenes, 10 Pagos, kardex completo de `MovimientoStock`,
`CuentaCorrienteCliente`/`Proveedor`) — no son filas sueltas, replica a mano las mismas reglas que
los DAL reales (signos de `debe`/`haber`, formato `V-`/`C-000001`, invariante de stock, los
`CHECK` del esquema). Corre después de `01_Esquema.sql` + `02_DatosIniciales.sql` (con
`03_UsuariosDePrueba.sql` opcional — usa el admin sembrado por `02` si `03` no se corrió). **No es
idempotente**: asume tablas de negocio vacías; para recargar hay que recrear el esquema primero.

**Bug real encontrado al probarlo:** el comentario de cabecera mencionaba `BIZ/Data/*.cs` — ese
`/*` abre un comentario anidado que nunca cierra, y rompe el parseo de todo el script (`Msg 113:
Missing end comment mark`). T-SQL cuenta pares `/* */` anidados; cualquier `/*` suelto dentro de
un comentario existente hay que evitarlo. Se resolvió reescribiendo la frase sin la secuencia
`/*`.

**Verificado contra LocalDB:** conteos de fila esperados en las 13 tablas tocadas, el `SELECT` de
invariante de kardex (`stockActual == Σ(entrada − salida)`) no devuelve ninguna fila, y los saldos
finales de cuenta corriente (clientes y proveedores) y `saldoPendiente` de ventas/compras
coinciden exactamente con los calculados en el diseño. Nota cosmética: por el intento fallido
antes de la corrección, la numeración de comprobantes quedó con un gap (`V-000003`/`C-000006` en
vez de arrancar en 1) — comportamiento normal de `IDENTITY` tras un intento fallido, no un bug.

### 2026-09-14 (noche, cont. 5) — Recuperación de clave: entrega en `.txt` en vez de `.eml`

Cambio puntual en `ServicioMail.cs`, pedido por el usuario para poder abrir el mail simulado sin
un cliente de correo: el `.eml` que generaba `SmtpClient` con `SpecifiedPickupDirectory` traía el
cuerpo codificado en base64 y no se podía leer con el Bloc de notas. Ahora, en modo desarrollo
(`MailModoDesarrollo=true`), `Enviar` no pasa más por `SmtpClient` — escribe directo un `.txt`
plano (`Para` / `Asunto` / `Fecha` / cuerpo, un archivo por envío, nombrado con fecha y hora) en
la misma carpeta `App_Data\MailsEnviados`. Fuera de modo desarrollo el envío real sigue igual
(`SmtpClient` con la config de `<system.net>/<mailSettings>`).

No fue necesario ningún cambio de base de datos — la pregunta pendiente sobre si además conviene
guardar el token de recuperación **hasheado** en vez de en texto plano (`RecuperacionClave.token`)
sigue abierta, sin decidir (ver sección 4, "Pendientes conocidos").

**Verificación:** rebuild limpio de `BIZ`. Contra IIS Express + LocalDB, pedido de recuperación
con `admin@lubricentro.com`: se generó el `.txt` en `App_Data\MailsEnviados` con el link y token
en texto plano, legible directo. Los `.eml` de sesiones anteriores quedaron intactos en la misma
carpeta (no se borraron, son solo de esta fecha en adelante en `.txt`).

### 2026-09-14 (noche, cont. 4) — Pagos, cierra Fase 4

Quinta y última pantalla de Fase 4, sobre `Pago`/`PagoDAL`. Con esto termina el circuito de
dinero completo: Compras → Cuenta corriente de Proveedores, Órdenes → Ventas → Cuenta corriente
de Clientes, y ahora Pagos conecta las puntas (reduce el saldo pendiente de una venta/compra
puntual, o entra directo a la cuenta corriente si es "a cuenta general").

**Único caso con dos entidades relacionadas ("comprobante puntual" vs "a cuenta general") en una
sola operación, y el primero que decide en tiempo de ejecución CUÁL de las dos cuentas corrientes
tocar** (`CuentaCorrienteCliente` o `CuentaCorrienteProveedor`, según `Pago.Tipo`) — el resto de
las escrituras de Fase 4 siempre supieron de antemano a qué tabla escribir. `PagoDAL.Registrar`
resuelve esto con un `if` sobre `pago.Tipo` que arma una rama distinta del mismo batch atómico,
no dos métodos separados — el `INSERT Pago` y la validación de saldo son comunes a los dos casos.

**Reutiliza los dos buscadores desplegables ya existentes tal cual**, sin ninguna adaptación: el
de cliente (visto por primera vez en Turnos/Órdenes) y el de proveedor (visto por primera vez en
Compras), alternados en la misma pantalla con un `ddlTipo` que muestra uno u otro. Ningún patrón
nuevo esta vez — la única pieza genuinamente nueva es el desplegable "comprobante a pagar", que
se repuebla al elegir cliente/proveedor con `ComprobanteVentaDAL.ListarPendientesPorCliente`/
`ComprobanteCompraDAL.ListarPendientesPorProveedor` (dos métodos nuevos, uno en cada DAL,
filtrando `saldoPendiente > 0` — mismo criterio de "cada entidad expone las consultas sobre sí
misma" que ya usa `TurnoDAL.ListarPorCliente`).

**Verificación:** rebuild limpio + `aspnet_compiler` sin errores. Contra IIS Express + LocalDB:
pago parcial de una venta (saldo pendiente baja, movimiento `haber` correcto en
`CuentaCorrienteCliente`, `idVenta = NULL`/`idPago` seteado — el trazado va por `idPago`, no por
`idVenta`, en un movimiento de tipo `Pago`), pago total de una compra (saldo pendiente a $0),
rechazo de monto mayor al saldo pendiente de un comprobante puntual, pago "a cuenta general" sin
comprobante (sin tope de monto), acceso completo confirmado como Empleado (a diferencia de
Compras/Cuentas corrientes). Datos de prueba borrados al cerrar la sesión.

**Fase 4 completa: Compras, Cuenta corriente de Proveedores, Ventas, Cuenta corriente de
Clientes, Pagos — las 5 pantallas hechas y verificadas.**

### 2026-09-14 (noche, cont. 3) — Cuenta corriente de Clientes

Cuarta pantalla de Fase 4. Pura pantalla, sin BIZ nueva: `CuentaCorrienteClienteDAL` ya había
quedado escrito en la sesión de Ventas (`ComprobanteVentaDAL.GenerarDesdeOrden` lo necesitaba
para el movimiento de la venta), igual que pasó con `CuentaCorrienteProveedorDAL` en la sesión de
Compras. Calco pieza por pieza de `CuentaCorrienteProveedores.aspx`: mismo layout, mismo criterio
de permisos (solo consulta esconde nada más la franja de ajuste), mismo patrón de ajuste con
signo único.

**Verificación:** rebuild limpio + `aspnet_compiler` sin errores. Contra IIS Express + LocalDB:
selección de cliente con historial vacío (saldo $0), ajuste positivo con motivo (saldo actualizado,
usuario registrado), acceso completo confirmado como Admin y modo solo-consulta confirmado como
Empleado (ve historial, sin la franja de ajuste). Con esto, **Fase 4 queda en 4 de 5 pantallas —
solo falta Pagos**. Datos de prueba (un cliente) borrados al cerrar la sesión.

### 2026-09-14 (noche, cont. 2) — Ventas, toca Órdenes de trabajo ya entregado

Tercera pantalla de Fase 4, sobre 3 entidades nuevas (`ComprobanteVenta`,
`DetalleComprobanteVenta`, `CuentaCorrienteCliente`, cada una con su Modelo + DAL). Primera vez
que una pantalla de Fase 4 modifica código de una fase ya entregada en vez de solo agregar algo
nuevo.

**`OrdenDeTrabajoDAL.Cerrar` es el nuevo `Cancelar`.** Cerrar una orden ya no es una transición
más del `ddlEstado` genérico: pasa a tener el efecto colateral de generar la venta
(Requerimientos §6.6), así que se sacó `EstadoCerrada` de `OrdenDeTrabajo.EstadosEditables`
(queda `{Abierta, En proceso}`) y se agregó un botón `btnCerrarOrden` dedicado en
`OrdenesDeTrabajo.aspx`, con confirmación — calco exacto del patrón que ya tenía "Cancelar
orden". `Cerrar` primero genera la venta (`ComprobanteVentaDAL.GenerarDesdeOrden`) y recién si
eso sale bien actualiza el estado — al revés dejaría la orden marcada `Cerrada` sin venta si algo
fallara en el medio, que es el peor de los dos escenarios posibles.

**`GenerarDesdeOrden` no necesitó ningún parámetro `idUsuario`,** a diferencia de `Cancelar`. Se
evaluó pasarlo por simetría, pero ni `ComprobanteVenta` ni el movimiento de
`CuentaCorrienteCliente` que genera tienen dónde guardarlo (el criterio ya establecido es que
`idUsuario` en cuenta corriente solo se puebla en `Ajuste`) — se descartó agregar un parámetro
sin ningún lugar donde usarlo.

**Mismo patrón de batch atómico que `ComprobanteCompraDAL.Crear`, aplicado a Ventas:** un
`StringBuilder` arma `INSERT ComprobanteVenta` + un `INSERT DetalleComprobanteVenta` por cada
línea de servicio y de insumo que ya tenía la orden (copiadas tal cual, sin volver a mirar el
precio del catálogo) + `INSERT CuentaCorrienteCliente` con el saldo por subquery + el `UPDATE`
final que arma el número de comprobante (`V-000001`, ...) desde el propio `SCOPE_IDENTITY()`.
Guarda de idempotencia explícita: `GenerarDesdeOrden` rechaza si la orden ya tiene una venta
generada (no hay `UNIQUE` en `ComprobanteVenta.idOrden`, así que la guarda vive en C#, no en el
esquema).

**Ventas.aspx es la primera pantalla puramente de solo lectura del proyecto** — ni siquiera tiene
el concepto de `EsSoloLectura` por rol, porque no hay ninguna escritura que restringir para nadie
(acceso completo para los 3 roles, matriz §5). Buscador+grilla a la izquierda, cabecera +
detalle de solo lectura a la derecha — mismo layout de dos columnas que el resto, sin ningún
botón de acción salvo "Ver".

**Verificación:** rebuild limpio + `aspnet_compiler` sin errores. Contra IIS Express + LocalDB con
requests HTTP armados a mano: orden con un servicio y un insumo cargados, cerrada desde
`OrdenesDeTrabajo.aspx` con el botón nuevo — venta generada con el detalle y los montos
correctos, movimiento en `CuentaCorrienteCliente` (`idUsuario = NULL`), numeración correlativa;
vista de detalle en `Ventas.aspx` mostrando todo correcto; regresión de "Cancelar orden" con una
segunda orden (stock repuesto correctamente, sin generar ninguna venta); acceso completo
confirmado como Empleado en Ventas. Datos de prueba (cliente, vehículo, servicio, insumo,
órdenes, venta) borrados al cerrar la sesión.

### 2026-09-14 (noche, cont.) — Cuenta corriente de Proveedores

Segunda pantalla de Fase 4. Sin capa BIZ nueva: el DAL (`CuentaCorrienteProveedorDAL.
ListarPorProveedor`/`ObtenerSaldoActual`/`RegistrarAjuste`) ya había quedado escrito en la sesión
de Compras, porque `ComprobanteCompraDAL.Crear` ya necesitaba `CuentaCorrienteProveedor` para las
compras a cuenta corriente. Esta sesión fue pura pantalla.

**Decisión de layout distinta a Proveedores/Insumos: el modo solo-consulta no esconde el
formulario entero.** En Proveedores/Insumos, "solo consulta" para Empleado significa "no ve
ningún formulario, solo la grilla" — tiene sentido ahí porque el formulario ES la pantalla
completa (alta/edición). Acá la pantalla ya es de consulta para todos los roles (ver
historial/saldo); lo único que cambia con el rol es si además puede *escribir* un ajuste. Por
eso `EsSoloLectura` esconde nada más la franja `pnlAjuste`, dejando visible el resto
(buscador, grilla de proveedores, y el panel de historial+saldo una vez elegido uno) para los
3 roles por igual.

**Reutilización directa del patrón de ajuste de Insumos**, sin cambios: campo único con signo
(positivo aumenta la deuda, negativo la reduce), sin radio Entrada/Salida — el mismo criterio que
ya se había validado con el usuario para el stock.

**Verificación:** rebuild limpio + `aspnet_compiler` sin errores. Contra IIS Express + LocalDB con
requests HTTP armados a mano: selección de proveedor con historial vacío (saldo $0), ajuste
positivo y negativo (saldo acumulado correcto, usuario registrado), rechazo de monto cero,
historial mixto mostrando una fila de Compra (usuario en blanco, por diseño) junto a ajustes
manuales (usuario visible) en el orden correcto; acceso completo confirmado como Admin y modo
solo-consulta confirmado como Empleado (ve historial y saldo, sin la franja de ajuste — un intento
forzado de postear el botón de ajuste igual lo rechaza, aunque por una razón distinta a la
esperada: el botón ni siquiera se renderiza para ese rol, así que ASP.NET lo frena por validación
de eventos antes de que el código llegue a chequear `EsSoloLectura`). Datos de prueba (proveedor,
compra de prueba, movimientos) borrados al cerrar la sesión.

### 2026-09-14 (noche) — Arranca Fase 4: pantalla de Compras

Primera de las 5 pantallas de Fase 4 (el Roadmap agrupa 4, pero Cuentas corrientes son 2
pantallas separadas — Cliente y Proveedor). Sobre 3 entidades nuevas (`ComprobanteCompra`,
`DetalleCompra`, `CuentaCorrienteProveedor`, cada una con su Modelo + DAL).

**Dos decisiones de alcance confirmadas con el usuario antes de diseñar:**

1. **Las cuentas corrientes sí van a llevar ajuste manual** (motivo + monto con signo, calco del
   patrón de Insumos.aspx) — el requerimiento §6.8 no lo pide explícitamente, pero el esquema ya
   reservaba un tipo `Ajuste` para esto. Todavía no implementado (queda para la pantalla de
   Cuenta corriente de Proveedores, próxima sesión).
2. **Una compra "Contado" queda fuera del circuito de Pagos/Cuenta corriente, pero el medio de
   pago usado se registra igual** en la propia compra — el usuario lo pidió explícitamente
   ("que se guarde el registro del método de pago así como los insumos utilizados para bajar el
   stock"). Esto obligó a **agregar una columna nueva al esquema** que no estaba en el diagrama
   original: `ComprobanteCompra.medioPago` (`NULL`, con dos `CHECK` nuevos atando su dominio y su
   obligatoriedad a `condicionPago = Contado` — mismo criterio que `CK_Pago_titular`).

**Segundo cambio de esquema, más discreto:** `CuentaCorrienteCliente`/`Proveedor` no tenían
`idUsuario` (a diferencia de `MovimientoStock`). Ahora que existe el ajuste manual (decisión 1),
aplica el mismo criterio que ya se usó para `MovimientoStock.idUsuario`: se agregó `idUsuario
INT NULL` a las dos tablas, poblado solo en movimientos de tipo `Ajuste` (los automáticos de
Venta/Compra/Pago ya son trazables por otro lado).

**Recrear el esquema local pisó datos reales del usuario** (cliente, vehículo, turno y su propio
usuario `Santi@gmail.com`) — a diferencia de la sesión 2026-09-07 (que recreó el esquema sin
avisar y borró datos de prueba cargados a mano), esta vez se le avisó explícitamente antes de
hacerlo y el usuario confirmó seguir adelante.

**Patrón nuevo: líneas en memoria vía `ViewState`, no franja progresiva.** A diferencia de
Órdenes (donde cada línea se persiste al toque porque el trabajo se descubre progresivamente),
una compra es la transcripción de una factura que ya llega completa — tiene más sentido armar
todas las líneas en pantalla y guardar todo junto. `DetalleCompra` se marcó `[Serializable]`
(primera clase del Modelo que lo necesita) para poder vivir en `ViewState["LineasPendientes"]`
mientras se arma la compra.

**`ComprobanteCompraDAL.Crear` es el batch atómico más grande del proyecto hasta ahora:** un
`StringBuilder` arma dinámicamente `N` bloques de `UPDATE Insumo` + `INSERT MovimientoStock` +
`INSERT DetalleCompra` (uno por línea, con parámetros sufijados `@idInsumo0`, `@idInsumo1`, ...)
más, si la condición es "Cuenta corriente", un `INSERT CuentaCorrienteProveedor` final con el
saldo calculado por subquery — todo en un solo `SET XACT_ABORT ON`/`BEGIN TRAN`/`COMMIT`. El
número de comprobante nace de un `UPDATE` final usando el propio `SCOPE_IDENTITY()`, sin tabla de
secuencia aparte.

**Bug real encontrado — tercera vez con el mismo síntoma, pero causa distinta:** `MovimientoStock.
idUsuario` es `NOT NULL` desde Fase 2, y el batch de `ComprobanteCompraDAL.Crear` lo insertaba
como `NULL` a propósito (el razonamiento de "solo Ajuste necesita idUsuario" aplicaba a
`CuentaCorrienteProveedor`, no a `MovimientoStock`, que siempre lo exigió). Se agregó un
parámetro `idUsuario` a `Crear` — el usuario que carga la compra queda registrado en cada
movimiento de stock que genera, igual que ya pasa en Órdenes.

**Falso bug en las pruebas (no en el código real), útil para la próxima sesión:** al probar con
requests HTTP crudos, postear `ddlMedioPago` con cualquier valor mientras `pnlMedioPago` está
oculto (condición de pago "Cuenta corriente") tira el mismo `HttpUnhandledException` de
validación de eventos ya visto con `ddlVehiculo`/`ddlEstado` en sesiones anteriores — pero acá
`ddlMedioPago` sí tiene opciones cargadas, el problema es que el panel que lo contiene no se
renderiza. Un browser real nunca postea un campo que no está en el DOM actual, así que no es un
bug de la aplicación — es nada más una trampa a tener presente al armar el próximo request a
mano: omitir del POST cualquier control que esté dentro de un `Panel`/sección oculta en el último
render simulado.

**Verificación:** rebuild limpio + `aspnet_compiler` sin errores. Contra IIS Express + LocalDB
(recreada con el esquema nuevo) con requests HTTP armados a mano: alta de compra Contado (medio
de pago guardado, sin fila en `Pago` ni `CuentaCorrienteProveedor`, `saldoPendiente = 0`) y a
Cuenta corriente (`saldoPendiente = total`, movimiento en `CuentaCorrienteProveedor` con
`idUsuario = NULL`); suba de stock y kardex correctos en ambos casos; numeración correlativa
(`C-000002`, `C-000003` — el gap en `C-000001` confirma que el `ROLLBACK` del bug de arriba
efectivamente deshizo el `INSERT` de la cabecera, aunque el `IDENTITY` no se recicla); vista de
solo lectura de una compra ya guardada; rechazo de cantidad ≤ 0 y de guardar sin líneas; acceso
completo como Admin y modo solo-consulta confirmado como Empleado (sin formulario, sin columna de
acciones). Datos de prueba (proveedor, insumos, compras) borrados al cerrar la sesión.

### 2026-09-14 — Órdenes de trabajo, cierra Fase 3

Segunda y última pantalla de Fase 3, sobre 3 entidades nuevas (`OrdenDeTrabajo`,
`DetalleOrdenServicio`, `DetalleOrdenInsumo`, cada una con su Modelo + DAL) más un método nuevo en
`TurnoDAL` (`ListarPorCliente`, filtrado a `Solicitado`/`Confirmado`, para el selector de turno).
La pantalla más compleja hasta ahora: primera con líneas de detalle, primera que mueve stock
automáticamente desde una pantalla de negocio (no solo desde el ajuste manual de Insumos), y
primera que necesita un alta en cascada (cliente → vehículo → orden) para el walk-in.

**Decisión de diseño: cliente/vehículo/turno quedan fijos una vez creada la orden.** Se eligen al
crear, no se pueden reasignar después — evita todo el problema de "¿qué pasa si el desplegable de
vehículo ya no tiene la opción que tenía la orden?" (el mismo tipo de dolor de cabeza que las
sesiones de Turnos ya habían dejado documentado). Consecuencia directa: editando una orden ya
creada, cliente/vehículo/turno se muestran como texto de solo lectura (`litVehiculoInfo`/
`litTurnoInfo`), no como los desplegables interactivos que sí aparecen en "Nueva orden".

**Mismo bug de Turnos, en un control distinto — event validation en `ddlEstado`.** El
`DropDownList` de estado vive dentro de un `Panel` (`pnlEstado`) que arranca `Visible="false"` en
el markup (el estado no aplica al alta, la orden nace `Abierta`); si el panel nunca pasa a
visible en un request, `ddlEstado` no se renderiza y ASP.NET no registra ningún `<option>` suyo
para la validación de eventos — postear cualquier valor para ese control (aunque sea uno que
normalmente sería válido) tira el mismo `HttpUnhandledException: Argumento de postback no válido`
que ya había aparecido con `ddlVehiculo` en Turnos. No es un bug del código real (ningún browser
real envía el valor de un control que nunca estuvo en el DOM), pero sí una trampa a tener presente
para probar con requests HTTP crudos: cualquier control dentro de un `Panel`/sección condicional
hay que omitirlo del POST si esa sección no estuvo visible en el render que se está simulando.

**Atomicidad de "agregar línea de insumo": batch propio en `DetalleOrdenInsumoDAL.Agregar`, no
reutiliza `MovimientoStockDAL.Registrar` tal cual.** Agregar una línea de insumo son tres
escrituras que tienen que ir juntas (`UPDATE Insumo.stockActual`, `INSERT MovimientoStock`,
`INSERT DetalleOrdenInsumo`), y `Registrar` solo atomiza las primeras dos. Se replicó el mismo
patrón `SET XACT_ABORT ON; BEGIN TRANSACTION; ...; COMMIT;` directo en `DetalleOrdenInsumoDAL`,
duplicando ~15 líneas de SQL en vez de acoplar `Data/MovimientoStockDAL` a
`Data/DetalleOrdenInsumoDAL`. La reposición de stock al **cancelar** una orden no tiene este
problema (no inserta detalle nuevo, solo revierte los movimientos existentes) y sí llama
directo a `MovimientoStockDAL.Registrar`, una vez por cada línea de insumo cargada.

**Walk-in con cliente y vehículo nuevos, de punta a punta.** Como `OrdenDeTrabajo.idVehiculo` es
`NOT NULL`, un walk-in genuinamente nuevo necesita poder crear cliente **y** vehículo sin salir de
la pantalla — más allá de lo que hizo falta en Turnos. Se extendió el mecanismo ya existente entre
`Vehiculos.aspx` y `Clientes.aspx` (`Response.Redirect` + query string, `CLAUDE.md` → "Cross-page
posting no funciona con FriendlyUrls") agregando un tercer origen `"orden"` en paralelo al
`"vehiculo"` que ya tenían esas dos pantallas — **sin modificar la lógica de ese flujo
existente**, solo sumando ramas nuevas (`else if (origen == "orden")`) con sus propios hidden
fields (`hdnVieneDeOrden`, `hdnOrKilometraje`, `hdnOrObservaciones`, `hdnOrIdTurno`) y su propio
panel de "volver sin crear". Probado el circuito completo: Órdenes → Clientes (crear cliente) →
Órdenes (cliente preseleccionado, `ddlVehiculo` vacío) → Vehículos (crear vehículo, dueño
preseleccionado) → Órdenes (cliente y vehículo ya seleccionados) → Guardar. También se probó
explícitamente que el flujo `"vehiculo"` original (Vehículos → Clientes → Vehículos) sigue
funcionando exactamente igual que antes de este cambio.

**Verificación:** rebuild limpio + `aspnet_compiler` sin errores. Contra IIS Express + LocalDB con
requests HTTP armados a mano (mismo método que la sesión de Turnos — sin navegador disponible en
el entorno; lanzado con la herramienta PowerShell en background y `dangerouslyDisableSandbox`,
como quedó anotado en la sesión anterior): alta walk-in con cliente/vehículo ya existentes, alta
walk-in con cliente y vehículo nuevos (circuito completo de 5 saltos), agregar línea de servicio y
de insumo (confirmado el descuento de stock y la fila de kardex con `idOrden` seteado), rechazo de
insumo con stock insuficiente (sin fila huérfana en `DetalleOrdenInsumo` ni en `MovimientoStock`),
quitar una línea de servicio, transición `Abierta` → `En proceso` con cambio de
kilometraje/observaciones, cancelar una orden con insumos cargados (stock repuesto, kardex con
`TipoCancelacionOrden`), UI de estado terminal (sin `ddlEstado`/botones de edición, historial de
detalle igual visible), acceso completo confirmado como Empleado, y la regresión del flujo
`"vehiculo"` existente. Los datos de prueba (órdenes, clientes y vehículos creados durante la
sesión) se borraron al cerrar, dejando la base como estaba antes de empezar.

### 2026-09-11 — Arranca Fase 3: pantalla de Turnos

Primera pantalla de Fase 3, sobre `BIZ/Modelo/Turno.cs` y `BIZ/Data/TurnoDAL.cs` nuevos (patrón
`Vehiculo`/`VehiculoDAL`). Se confirmó con el usuario seguir el orden del Roadmap (Turnos y
Órdenes antes que Compras/Cuentas corrientes, que dependen de que existan Órdenes cerradas) y,
dentro de Fase 3, arrancar por Turnos por ser más simple.

**Bug real encontrado en la propia sesión, no solo en teoría — `ddlVehiculo` sin ninguna opción
en el alta:** el `DropDownList` de vehículo del formulario se poblaba únicamente al elegir un
cliente (`SeleccionarCliente` → `CargarVehiculosDelCliente`), pero `Page_Load` nunca lo inicializaba
en el primer `GET` de la pantalla (`Nuevo turno`, sin cliente todavía). Con el control sin ningún
`<option>` renderizado, ASP.NET rechaza **cualquier** valor posteado para ese control — incluso
`""` — con `HttpUnhandledException: Argumento de postback no válido` (la validación de eventos
compara contra la lista de opciones que el servidor efectivamente renderizó). Se detectó recién
al probar el alta contra IIS Express con requests HTTP crudos (sin navegador en la sesión): la
UI real nunca dispara esto porque el buscador de cliente siempre repuebla el combo antes de que
el usuario pueda tocar "Guardar", pero un test de alta "en frío" (sin pasar por el buscador) lo
expone al toque. Se arregló con un método único `LimpiarVehiculos()` (dejar solo el placeholder
"(sin vehículo asociado)") llamado tanto en `Page_Load` como en `LimpiarFormulario` y al empezar
`CargarVehiculosDelCliente` — moraleja para las pantallas de Fase 3/4 que vengan: todo
`DropDownList` poblado dinámicamente necesita alguna carga base en el primer `Page_Load`, no solo
en el flujo que lo repuebla más tarde.

**Decisión de alcance:** a diferencia de "Nuevo cliente" desde Vehículos, Turnos **no** tiene ese
atajo hacia `Clientes.aspx` — no está en el alcance de Requerimientos §6.3 (es específicamente el
flujo walk-in de Órdenes, §6.4). Si el cliente no existe todavía, se da de alta primero en
`Clientes.aspx` y después se carga el turno.

**Validación de pertenencia vehículo↔cliente:** `TurnoDAL` valida que, si se envía un
`idVehiculo`, ese vehículo exista, esté activo y sea del cliente seleccionado — no alcanza con que
el combo del formulario ya filtre por cliente, porque una request armada a mano podría mandar
cualquier combinación. Probado explícitamente forzando un request con cliente A y vehículo de
cliente B: rechazado con "El vehículo seleccionado no pertenece a ese cliente.", sin tocar la base.

**`TurnoDAL.Crear` ignora el estado que venga del formulario** y fuerza `Solicitado` siempre — el
`ddlEstado` del alta es cosmético (arranca en `Solicitado` por ser el primer item), la fuente de
verdad es el DAL, no lo que el cliente HTTP mande.

**Nota de infraestructura para la próxima sesión que necesite IIS Express standalone:** lanzarlo
con el `Bash` tool (con o sin `dangerouslyDisableSandbox`) deja el proceso zombie — sin sockets
reales — apenas termina la llamada a la herramienta, y una segunda corrida en el mismo puerto falla
con "no se puede crear un archivo que ya existe" (reserva de URL en `http.sys` que quedó
huérfana). Lo que sí funcionó: lanzarlo con la herramienta **PowerShell** en background
(`dangerouslyDisableSandbox: true`) — ahí el proceso sigue vivo y sirviendo requests después de
que la llamada a la herramienta "termina". Si hace falta reintentar, matar antes cualquier
`iisexpress` colgado (`Get-Process iisexpress | Stop-Process -Force`) o cambiar de puerto.

**Verificación:** rebuild limpio (`MSBuild`) + `aspnet_compiler` sin errores. Contra IIS Express +
LocalDB, con requests HTTP armados a mano (extrayendo `__VIEWSTATE`/`__EVENTVALIDATION` de cada
respuesta, sin navegador disponible en el entorno — el mismo método que sesiones anteriores):
alta de turno sin vehículo, edición asociando un vehículo y cambiando fecha/hora/estado a
`Confirmado`, búsqueda por DNI del cliente, búsqueda sin resultados, filtro por estado sin
resultados, rechazo de fecha/hora/cliente vacíos (sin insertar fila), rechazo de vehículo de otro
cliente (forzado a mano), y alta exitosa logueado como `empleado@lubricentro.com` (acceso
completo, sin panel de solo-lectura) — confirma la matriz de permisos §5 para esta pantalla. Los
datos de prueba (turnos y un cliente extra creado para el test de pertenencia) se borraron al
cerrar la sesión.

### 2026-09-07 (noche, cont. 2) — Refinamiento de UI en Insumos, Fase 2 cerrada

Serie de ajustes de UX sobre `Insumos.aspx` pedidos después de ver la pantalla funcionando:

- **Ajuste de stock simplificado**: se sacó el radio Entrada/Salida — un solo campo de cantidad
  con signo (positivo suma, negativo resta). El code-behind traduce a `Math.Abs(cantidad)` +
  `esEntrada = cantidad > 0` antes de llamar a `MovimientoStockDAL.RegistrarAjusteManual`, que no
  cambió. El validador pasó de `GreaterThan 0` a `NotEqual 0` (rechaza cero, acepta negativos).
- **Franja Historial/Ajuste**: estaba `col-6`/`col-6` (50/50); pasó a `col-7`/`col-5` (60/40),
  igual ratio que el buscador/formulario de arriba — Historial (la grilla de datos) del lado
  grande, Ajustar stock (el formulario) del lado chico. También se invirtió qué va a la izquierda:
  Historial ahora a la izquierda, Ajuste a la derecha.
- **Grillas con estilo de tabla**: `CssClass="table table-striped table-bordered table-hover"` en
  `gvInsumos` y `gvHistorial` — Bootstrap ya está cargado en el proyecto, no es una dependencia
  nueva. El resaltado inline de stock bajo (`style="background-color:#f8d7da"` en
  `RowDataBound`) convive sin problema: un estilo inline siempre gana por especificidad sobre una
  clase CSS. **Solo se aplicó en Insumos por ahora** — decidir después si se replica en
  Clientes/Vehículos/Proveedores/Servicios para consistencia.
- **Paginado real en `gvInsumos`** (no en `gvHistorial`): `AllowPaging="true" PageSize="30"`. Se
  evaluó explícitamente contra un contenedor con scroll (que no reduce lo que viaja al navegador
  y no resuelve el problema de fondo) — se eligió paginado nativo. Hace falta resetear
  `gvInsumos.PageIndex = 0` en cualquier acción que cambie qué se está mirando (buscar, incluir
  inactivos, guardar, borrar, ajustar) para no quedar apuntando a una página que ya no existe.
- **Pager custom**: el pager numérico automático de `GridView` (links "1 2 3...") se reemplazó
  por un `PagerTemplate` con texto **"Página X de Y"** centrado (`PagerStyle-HorizontalAlign`) y
  dos `LinkButton` Anterior/Siguiente (`CommandName="Page"`, `CommandArgument="Prev"/"Next"` —
  comandos que `GridView` ya reconoce, disparan el mismo `OnPageIndexChanging` de siempre). Se
  perdió el salto directo a una página puntual (no hacía falta para la cantidad de páginas de
  este proyecto). **Bug de test, no de la app**: al simular el click de "Siguiente" a mano con
  `__EVENTTARGET=gvInsumos` y `__EVENTARGUMENT=Page$Next` (el formato del pager viejo) saltó
  "Argumento de postback no válido" — la validación de eventos lo rechazó correctamente, porque
  un `PagerTemplate` con `LinkButton`s propios postea con el `UniqueID` real de cada botón, no con
  ese formato. Se resolvió usando el `__doPostBack(...)` real que renderiza la página.

Se cargaron **35 insumos genéricos de prueba** ("Insumo generico 01".."35", stock variado) para
poder ver el paginado con datos reales — quedan en la base a propósito, ver pendiente abajo.

**Verificación:** `aspnet_compiler` en cada paso (son todos cambios de markup, sin tocar
`InsumoDAL`/`MovimientoStockDAL`/esquema). Contra IIS Express: ajuste positivo y negativo,
cantidad cero rechazada, ratio 60/40 confirmado leyendo las clases `col-7`/`col-5` del HTML
renderizado, clases de tabla presentes, paginado con "Página 1 de 2" → "Página 2 de 2" navegando
con los botones reales, búsqueda desde la página 2 vuelve a página 1 sin error.

### 2026-09-07 (noche, cont.) — Insumos y Servicios: Fase 2 completa

Últimas dos pantallas de Fase 2, sobre la capa BIZ de Insumo ya escrita en la sesión anterior.

**`Insumos.aspx`**: mismo layout de dos columnas que las demás, con una franja de ancho completo
debajo (decisión confirmada con el usuario) que solo aparece al seleccionar un insumo existente
— nunca en "Nuevo insumo" ni para el rol Empleado:

- Izquierda: "Ajustar stock" — radio Entrada/Salida, cantidad, motivo obligatorio. Llama a
  `MovimientoStockDAL.RegistrarAjusteManual`.
- Derecha: grilla de historial (`MovimientoStockDAL.ListarPorInsumo`), más reciente primero.

El campo "Stock inicial" del formulario principal solo se usa una vez, al crear (dispara el
ajuste de alta en `InsumoDAL.Crear`); al editar se reemplaza por un `Label` de solo lectura con
el stock actual — todo cambio posterior pasa por el ajuste, no por "Guardar".

La grilla principal resalta con fondo rojo (`#f8d7da`, inline `style`, no clase de Bootstrap —
el `GridView` no usa `class="table"` en ningún lado del proyecto, así que `.table-danger` no
hubiera aplicado) las filas con `stockActual < stockMinimo` (decisión confirmada con el usuario,
conecta con el reporte "Stock bajo" de Requerimientos §6.9).

**`Servicios.aspx`**: el más simple de los cinco ABM de Fase 2 (nombre, descripción, precio
base) — sin campos con formato especial, sin unicidad. Calco directo de `Proveedores.aspx` con
menos campos.

Ambas, modo solo-consulta para Empleado con el mismo patrón ya establecido (esconder
`pnlFormulario` entero + columna "Acciones", chequeo de `EsSoloLectura` al principio de cada
método de escritura).

**Verificación:** rebuild limpio + `aspnet_compiler`. Probado contra IIS Express: alta de insumo
con stock inicial (y su movimiento de alta visible en el historial), ajuste de entrada y de
salida, rechazo de ajuste que dejaría stock negativo, resaltado de stock bajo, ABM completo de
Servicios, y modo solo-consulta/acceso completo para Empleado/Encargado en las dos pantallas.

Al recrear el esquema en la sesión anterior había corrido solo `02_DatosIniciales.sql` y no
`03_UsuariosDePrueba.sql` — Encargado/Empleado no existían y el login fallaba. Se repuso
corriendo el script opcional; quedó anotado acá para no repetir la confusión.

### 2026-09-07 (noche) — Kardex de stock (`MovimientoStock`) y capa BIZ de Insumo

Antes de escribir la pantalla de Insumos, surgió que `stockActual` era un número que se pisaba sin
dejar rastro. El usuario pidió explícitamente: ajustes manuales de stock con historial (quién,
cuándo, cuánto, motivo), y que cancelar una orden de trabajo reponga el stock no utilizado. Se
resolvió con una entidad nueva y su capa BIZ — **sin tocar ninguna pantalla todavía** (eso es el
siguiente paso, al construir `Insumos.aspx`).

**Entidad nueva `MovimientoStock`** (`Database/01_Esquema.sql`): kardex con `entrada`/`salida`/
`stockResultante` (mismo patrón que `CuentaCorrienteCliente`/`Proveedor`: fecha, tipo, FKs
opcionales al origen, descripción libre). Dos diferencias deliberadas respecto de ese patrón:

- **`idUsuario NOT NULL`**: las `CuentaCorriente*` no lo tienen, pero acá se pidió explícitamente
  poder saber quién hizo cada ajuste — hay precedente directo en `Pago`, que tampoco es una
  `CuentaCorriente*` y sí guarda `idUsuario`.
- **`CK_MovStock_unSentido`**: fuerza que cada fila sea o entrada o salida, nunca las dos a la vez
  ni ninguna — pensado para que los reportes de Fase 5 puedan sumar columnas sin filas ambiguas.

También se agregó `CK_MovStock_origen` (mismo criterio que `CK_Pago_titular`: ata el tipo de
movimiento a qué FK debe estar poblada), que de paso vuelve innecesario un `CHECK` aparte sobre
los 4 valores literales de `tipoMovimiento` — cualquier valor fuera de esos 4 falla las tres
ramas del `OR`. Se sumaron además `CK_Insumo_stockActual`/`stockMinimo >= 0` (red de seguridad;
`Insumo` ya tenía `CK_Insumo_precioVenta` y le faltaban estos dos).

**Atomicidad:** `AccesoDatos.cs` no expone transacciones (cada método abre su propia conexión).
`MovimientoStockDAL.Registrar` arma un solo batch de texto SQL con
`SET XACT_ABORT ON; BEGIN TRANSACTION; ...; COMMIT TRANSACTION;`, sin tocar esa API. Verificado a
mano con `sqlcmd`, reproduciendo el SQL exacto que genera el DAL (sin `TRY/CATCH`, igual que el
código real): un `INSERT` que viola un `CHECK` revierte el `UPDATE` anterior — confirmado leyendo
el stock desde una conexión separada después del fallo. (Un primer intento de verificación con
`TRY/CATCH` alrededor del batch dio un falso negativo — el `TRY/CATCH` posterga el rollback hasta
el final del batch en vez de dispararlo con `XACT_ABORT`; no es un problema del código real, que
no envuelve nada en `try/catch`, solo del script de prueba ad-hoc.)

**Validación de stock insuficiente:** en C#, antes del batch (mismo patrón que `ExisteDni`/
`ExisteCuit` — validación de negocio en C#, no en el `WHERE` del SQL). El `CHECK` en `Insumo`
queda como red de seguridad para carreras entre requests concurrentes, no como mecanismo
principal.

**Alta de insumo con stock inicial:** `InsumoDAL.Crear` inserta siempre con `stockActual = 0` y,
si se pidió stock inicial, dispara un `AjusteManual` aparte por ese valor — mantiene el invariante
`stockActual == Σ(entrada - salida)` desde el primer insumo. `InsumoDAL.Actualizar` no toca
`stockActual` en absoluto: todo cambio de stock pasa por `MovimientoStockDAL`.

**Deliberadamente no se agregaron** wrappers (`RegistrarEntradaPorCompra`, etc.) para que Fase 3/4
los usen — no tienen ningún caller hoy, hubiera sido diseñar para un requerimiento hipotético
futuro. Cuando se construyan `CompraDAL`/`OrdenDeTrabajoDAL`, van a llamar directo a
`MovimientoStockDAL.Registrar(...)` con las constantes `MovimientoStock.Tipo*` ya definidas.

**Nota operativa:** recrear el esquema (`01_Esquema.sql` es idempotente, borra y recrea) para
agregar las tablas/constraints nuevas **borró los datos de prueba que el usuario había cargado a
mano** (clientes, un vehículo) sin pedir confirmación antes — a tener en cuenta para la próxima
vez que haga falta tocar el esquema con datos reales cargados.

**Verificación:** rebuild limpio de la solución. Recreación de LocalDB desde
`01_Esquema.sql`/`02_DatosIniciales.sql` sin errores. Smoke test manual vía `sqlcmd`: ajuste
manual de entrada válido, rechazo de `CK_MovStock_unSentido` (entrada y salida a la vez), rechazo
de `CK_MovStock_origen` (tipo `Compra` sin `idCompra`), rechazo de `CK_Insumo_stockActual`
(stock negativo directo), y la prueba de atomicidad descrita arriba. No hay verificación funcional
de punta a punta todavía — no existe pantalla; eso queda para cuando se construya `Insumos.aspx`.

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
- Circuito de recuperación de contraseña completo: `RecuperarClave` genera el `.txt` en
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

**Fase 2 terminada.** Las cinco pantallas maestras (Clientes, Vehículos, Proveedores, Insumos,
Servicios) están hechas, verificadas contra IIS Express y con su modo solo-consulta para
Empleado donde corresponde.

**Fase 3 terminada.** Turnos y Órdenes de trabajo, las dos pantallas, hechas y verificadas contra
IIS Express.

**Fase 4 terminada.** Las 5 pantallas (Compras, Cuenta corriente de Proveedores, Ventas, Cuenta
corriente de Clientes, Pagos) hechas y verificadas contra IIS Express.

**Fase 5 terminada.** Los tres reportes (Stock bajo, Ventas por período, Cuentas corrientes)
hechos sobre datos reales de Fase 4, cada uno verificado con `MSBuild` + `aspnet_compiler` sin
errores. Ninguno necesitó cambio de esquema.

**Sigue Fase 6 — Integración, pruebas y pulido**, según el Roadmap: pruebas de flujo completo
(turno → orden → venta → pago → cuenta corriente), validaciones cruzadas entre módulos (ej. no
permitir cerrar una orden sin stock suficiente), revisión de permisos por rol pantalla por
pantalla, mejora de mensajes de error y UX (Bootstrap), y documentación final para la entrega.
Sin acordar todavía por dónde arrancar dentro de Fase 6 — a diferencia de las fases anteriores,
acá no hay una pantalla nueva por vez: son 5 frentes transversales que tocan código ya
entregado. Ver también los reportes de Stock bajo/Ventas por período pendientes de probar contra
IIS Express (ningún reporte de Fase 5 se probó en caliente todavía — WSL no llega a esos
puertos).

### Repaso de redacción, pendiente

Con la codificación ya arreglada, queda revisar los textos visibles de punta a punta:
consistencia del voseo (hoy se mezcla «Ingresá» con formas neutras) y los rótulos abreviados del
menú frente a los títulos de cada pantalla («Cta. cte. clientes» vs. «Cuenta corriente de
clientes»). La abreviatura en el menú es deliberada por espacio en la barra de navegación; lo que
falta es decidir si se unifica el criterio.

---

## 4. Pendientes conocidos, sin fecha

Cosas que hay que resolver antes de la entrega, anotadas para no perderlas:

- ~~**LocalDB no arranca en la máquina de Federico (2026-10-07).**~~ **Resuelto el mismo día.** Era
  el problema de Windows 11 con discos NVMe que informan sectores de más de 4 KB ("misaligned log
  IOs" en el `error.log`). La clave de Microsoft
  (`reg add "HKLM\SYSTEM\CurrentControlSet\Services\stornvme\Parameters\Device" /v ForcedPhysicalSectorSizeInBytes /t REG_MULTI_SZ /d "* 4095" /f`
  y reiniciar) **no alcanzó sola**: los arranques fallidos de antes habían dejado el `master` de la
  instancia apuntando a rutas inexistentes. Hubo que borrarla y crearla de nuevo
  (`sqllocaldb stop MSSQLLocalDB -k`, `delete`, `create`, `start`; no tenía bases propias) y correr
  `01` a `04`.

- ~~**BUG: cerrar una orden sin ninguna línea genera una venta en $0.**~~ **Resuelto el
  2026-10-05**: `GenerarDesdeOrden` rechaza el cierre si no hay servicios ni insumos (ver la
  entrada de esa fecha).
- ~~**Hallazgo: `ComprobanteVenta.saldoPendiente` desactualizado frente a la cuenta corriente.**~~
  **Resuelto el 2026-10-05**: los pagos cancelan primero las deudas más viejas y el sobrante queda
  a favor; ver la entrada de esa fecha. Los saldos viejos de la base de desarrollo no se migran
  (esos datos no importan): una base recreada con `01` a `04` ya sale coherente.
- **Cambiar la contraseña del administrador.** Hoy es la sembrada por el script (`Admin123!`).
- **Borrar los usuarios de prueba** (`encargado@lubricentro.com`, `empleado@lubricentro.com`,
  `lectura@lubricentro.com`) y el script `03_UsuariosDePrueba.sql` de la entrega final.
- **Conmutar a la VPN Radmin:** cambiar la cadena `LubricentroDB` en `Web.config`.
- **Salida real de mails — mecanismo listo, falta decidir el estado final.** `<system.net>/
  <mailSettings>` ya está configurado (Gmail + contraseña de aplicación, ver sesión
  2026-09-15 cont.), con las credenciales en `Web.MailSettings.config` (gitignoreado). Hoy
  `Web.config` quedó con `MailModoDesarrollo=false` (envío real activo, cuenta de prueba del
  usuario) en vez del `true` documentado como default seguro en `CLAUDE.md` — falta decidir si
  se revierte a `true` antes de commitear/entregar, o si se deja en `false` a propósito. En una
  máquina sin `Web.MailSettings.config` (cualquiera que clone el repo) los mails no salen y nadie
  avisa; ver la entrada del 2026-10-08.
- **Token de recuperación de clave guardado en texto plano.** `RecuperacionClave.token` guarda el
  valor tal cual, no un hash — a diferencia de la contraseña, que sí está hasheada. El usuario
  pidió evaluar guardar el hash del token en vez del token; decisión pendiente de confirmar antes
  de implementarla (no requiere columna nueva, `token` ya es `NVARCHAR(100)`).
- **`customErrors`:** con `debug="true"` y sin `customErrors`, un error muestra el stack trace
  completo en pantalla. Antes de entregar conviene una página de error propia.
- **El esquema de negocio nunca se ejerció por completo.** Con Fase 2 cerrada ya se ejercitaron
  Cliente, Vehiculo, Proveedor, Insumo y Servicio (más el `MovimientoStock` agregado en Fase 2);
  las tablas de Turnos, Órdenes, Compras, Ventas, Pagos y Cuentas Corrientes siguen sin usar —
  esperable que aparezcan ajustes de tipos o restricciones al escribir esas pantallas.
- **Estilos Bootstrap más elaborados (tablas, paginado) — parcialmente resuelto.** Se aplicaron
  clases de tabla (`table table-striped table-bordered table-hover`) y paginado real con pager
  custom en `Insumos.aspx` (ver sesión 2026-09-07 cont. 2). Queda pendiente: (a) decidir si se
  replica en Clientes, Vehículos, Proveedores y Servicios para consistencia visual; (b) la
  alineación de columnas numéricas (stock, precio) a la derecha, mencionada pero no encarada
  ("opción 2", diferida explícitamente por el usuario).
- ~~Borrar los 35 "Insumo generico" de prueba cargados para ver el paginado~~ — **resuelto solo,
  sesión 2026-09-27**: ya no existen. El esquema se recreó varias veces desde la sesión 2026-09-07
  que los cargó (`01_Esquema.sql` borra todo); `Insumo` hoy tiene exactamente las 10 filas de
  `04_DatosDemo.sql`, verificado por conteo total, sin nombres "genérico" ni activos ni de baja.
- **Botones de navegación cruzada Cliente↔Vehículo pendientes**: hoy solo existe "Nuevo cliente"
  desde Vehículos. Falta un botón "Ver vehículos" desde la ficha de un Cliente. **En parte resuelto
  el 2026-10-07:** "Ver" de un cliente muestra las patentes de sus vehículos y el buscador de
  Clientes encuentra por patente; el salto directo a Vehículos sigue sin hacerse.
- **Datos del comercio para la factura:** los que carga `02_DatosIniciales.sql` en la tabla
  `Emisor` son de ejemplo ("Lubricentro Control S.R.L.", CUIT inventado). Reemplazarlos por los
  reales antes de usarla, desde Administración > Datos del comercio (desde el 2026-10-08; antes
  estaban en `Web.config`).
- **Imágenes de los productos:** las de `05_ImagenesDemo.sql` son ilustraciones de ejemplo. Con
  los productos reales, cargar fotos propias o las del catálogo del distribuidor desde Productos
  (ver la entrada del 2026-10-08, cont. 2).
- **Formato de números según la cultura del servidor:** los importes salen con `N2` y la cultura de
  la máquina (en esta, en-GB: `3,500.00`). Si se quiere el formato argentino (`3.500,00`) hay que fijar
  `<globalization culture="es-AR" uiCulture="es-AR">`. Revisado el 2026-10-08: los importes que se
  tipean se leen con la misma cultura (`decimal.TryParse` sin cultura), así que dentro de una
  máquina es coherente, y el formato de la otra cultura lo rechaza el `CompareValidator
  Type="Currency"` de cada campo.
- **Las líneas de Compras no validan el formato de los números.** Cantidad y precio unitario no
  tienen `CompareValidator` (`btnAgregarLinea_Click` usa `decimal.TryParse` directo): en una
  máquina en inglés `1500,50` se guarda como `150050`, y en una en es-AR pasa lo mismo con
  `1500.50`, sin aviso. Hace falta el validador aunque se fije la cultura.
