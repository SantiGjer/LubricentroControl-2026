# Estado actual del sistema

**Proyecto:** LubricentroControl 2026 · Programación Avanzada — USAL
**Última actualización:** 14 de septiembre de 2026 (noche)

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

**Fase 1, 2 y 3 completas. Fase 4 arrancada:** Compras hecha, siguen Cuenta corriente de
Proveedores, Ventas, Cuenta corriente de Clientes y Pagos (en ese orden, ver plan de Fase 4).

| Fase | Contenido | Estado |
|---|---|:---:|
| 1 | Login, roles, menú dinámico, ABM de usuarios, capa de datos | ✅ Completa |
| 2 | ABM de Clientes, Vehículos, Proveedores, Insumos, Servicios | ✅ Completa |
| 3 | Turnos y Órdenes de trabajo | ✅ Completa |
| 4 | Compras, Ventas, Pagos, Cuentas corrientes | 🔶 Compras hecha, 4 pantallas pendientes |
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
- Las 21 tablas del diagrama E/R creadas, con los datos semilla de seguridad, más
  `MovimientoStock` (kardex de stock, agregada en Fase 2 — 22 tablas en total hoy).
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
- **ABM de Insumos + kardex de stock**: alta con stock inicial (registra un ajuste automático),
  búsqueda por nombre/marca, ajuste manual de stock (un solo campo con signo — positivo suma,
  negativo resta, sin radio Entrada/Salida) e historial de movimientos, ambos en una franja
  debajo del ABM (historial a la izquierda, ajuste a la derecha, ratio 60/40 igual que el
  buscador/formulario de arriba) que solo aparece al editar un insumo existente. Grilla principal
  con estilo de tabla de Bootstrap (rayada, con bordes, hover) y resaltado en rojo de las filas
  con stock por debajo del mínimo; paginada a 30 filas con pie "Página X de Y" +
  Anterior/Siguiente centrado. Modo solo-consulta para Empleado igual que Proveedores.
- **ABM de Servicios**: alta/baja lógica/edición, búsqueda por nombre. El más simple de los cinco
  (nombre, descripción, precio base) — mismo patrón que Proveedores. Modo solo-consulta para
  Empleado.
- **Turnos** (primera pantalla de Fase 3): alta/edición, sin baja lógica (no aplica — `Turno` no
  tiene columna `activo`; "cancelar" es simplemente llevar el campo `estado` a `Cancelado` desde
  el mismo formulario). Selector de cliente con el mismo buscador desplegable de
  Clientes/Vehículos, **sin** el atajo "Nuevo cliente" (decisión de alcance: no está en el
  requerimiento de Turnos, sí lo está en el walk-in de Órdenes). Selector de vehículo opcional,
  poblado con los vehículos activos del cliente elegido. Búsqueda por nombre/apellido/DNI del
  cliente más filtro por estado. Acceso completo para los 3 roles (Admin/Encargado/Empleado),
  sin modo solo-consulta.
- **Órdenes de trabajo** (segunda y última pantalla de Fase 3, cierra la fase): alta walk-in o
  con turno previo, cliente/vehículo/turno fijos una vez creada la orden (se ven como texto de
  solo lectura al editar, no como desplegables). Franja de detalle (aparece solo editando una
  orden ya creada) con dos columnas Servicios/Insumos, cada una con su mini-alta + grilla —
  agregar un insumo descuenta stock automáticamente (kardex con `MovimientoStock.TipoOrden`) y
  agregar un servicio no toca stock. "Cancelar orden" (botón aparte de "Guardar", con confirmación)
  repone el stock de todos los insumos cargados (`TipoCancelacionOrden`) y es irreversible. Sin
  "Quitar" en líneas de insumo (si hay que corregir, se cancela la orden entera); sí en líneas de
  servicio (`DELETE` simple, sin efecto colateral). El walk-in con cliente **y vehículo** nuevos
  funciona de punta a punta: "Nuevo cliente"/"Nuevo vehículo" desde Órdenes reutilizan y extienden
  el mecanismo de `Response.Redirect` + query string que ya conectaba Vehículos↔Clientes (un
  tercer origen `"orden"` agregado en paralelo al `"vehiculo"` existente, sin tocarlo). Acceso
  completo para los 3 roles, sin modo solo-consulta.
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

### Qué NO funciona todavía

Quedan 7 pantallas de negocio como **cascarones** (Ventas, Pagos, las dos cuentas corrientes y
los tres reportes): existen, están enlazadas desde el menú y respetan los permisos por rol, pero
no tienen funcionalidad.

---

## 2. Historial de sesiones

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

**Fase 2 terminada.** Las cinco pantallas maestras (Clientes, Vehículos, Proveedores, Insumos,
Servicios) están hechas, verificadas contra IIS Express y con su modo solo-consulta para
Empleado donde corresponde.

**Fase 3 terminada.** Turnos y Órdenes de trabajo, las dos pantallas, hechas y verificadas contra
IIS Express.

**Fase 4 arrancada — Compras hecha, siguen 4 pantallas más, en este orden** (plan completo de
Fase 4 acordado con el usuario, guarda las decisiones de diseño de las 5 pantallas):

1. ~~Compras~~ ✅ (esta sesión).
2. **Cuenta corriente de Proveedores** — pantalla de solo consulta + el ajuste manual pendiente
   (decisión 1 de la sesión de Compras, todavía sin construir), sobre `CuentaCorrienteProveedorDAL`
   ya escrito.
3. **Ventas** — pantalla de solo lectura (no se carga a mano) + gancho en `OrdenDeTrabajoDAL`
   que genera la venta al cerrar una orden — **toca código de Fase 3 ya entregado**: hay que sacar
   `Cerrada` de `OrdenDeTrabajo.EstadosEditables` y agregar un botón `btnCerrarOrden` dedicado
   (mismo criterio que `btnCancelarOrden`), porque cerrar pasa a tener el efecto colateral de
   generar la venta.
4. **Cuenta corriente de Clientes** — mismo patrón que el punto 2.
5. **Pagos** — usa los `Registrar` de ambas cuentas corrientes, ya construidos en 2 y 4.

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
- **Borrar los 35 "Insumo generico" de prueba** cargados para poder ver el paginado con datos
  reales — no deben llegar a la entrega final.
- **Botones de navegación cruzada Cliente↔Vehículo pendientes**: hoy solo existe "Nuevo cliente"
  desde Vehículos. Falta un botón "Ver vehículos" desde la ficha de un Cliente (la consulta
  `VehiculoDAL.ListarPorCliente` ya existe, sin usar, pensada para esto).
