# Lubricentro — Especificación de Requerimientos

**Proyecto:** Sistema de gestión para Lubricentro
**Materia:** Programación Avanzada 2026 · USAL
**Fecha:** Agosto 2026
**Estado del documento:** Primer borrador (v1) — resultado de la sesión de levantamiento de requerimientos

---

## 1. Objetivo del proyecto

Desarrollar un sistema de gestión integral para un Lubricentro que permita administrar clientes, vehículos, turnos, órdenes de trabajo, proveedores, stock de insumos, ventas, pagos y cuentas corrientes, con control de acceso por roles de usuario.

El sistema reemplaza la gestión manual/dispersa actual, centralizando la operación diaria del negocio en una única aplicación web.

---

## 2. Alcance del proyecto

Se desarrolla el sistema **completo** desde el inicio (no se plantea un MVP reducido). Los módulos incluidos son:

- Seguridad / Login / Recuperación de contraseña
- Clientes y Vehículos
- Turnos (Agenda)
- Órdenes de Trabajo
- Proveedores, Insumos y Compras
- Ventas / Comprobantes
- Pagos
- Cuentas Corrientes (Cliente y Proveedor)
- Reportes

**Fuera de alcance** (ver sección 10) por decisión explícita: facturación fiscal (AFIP), portal público para que el cliente pida turno por su cuenta, notificaciones automáticas.

---

## 3. Stack tecnológico

| Capa | Tecnología |
|---|---|
| Framework web | ASP.NET Web Forms sobre .NET Framework 4.7.2 (no .NET Core / .NET 5+) |
| Frontend | Bootstrap 5.2.3 + jQuery 3.7.0 + Modernizr, bundling vía `System.Web.Optimization` |
| Acceso a datos | ADO.NET puro (`SqlConnection` / `SqlCommand`, SQL parametrizado) — **sin ORM** (no Entity Framework) |
| Base de datos | SQL Server |
| Conectividad a BD | VPN Radmin |

---

## 4. Arquitectura de la solución

Solución con **2 proyectos** dentro de la misma solución .NET (no 4 — `Modelo` y `Data` son carpetas internas de `BIZ`, no proyectos aparte):

```
Proyecto (Solución)
├── BIZ
│    ├── Modelo   (entidades: Cliente, Vehiculo, etc.)
│    └── Data/DAL (acceso a datos con ADO.NET)
└── F.END / Avanzada2026 (páginas .aspx — capa web)
```

| Proyecto | Carpetas internas | Responsabilidad |
|---|---|---|
| `BIZ` | `Modelo` (entidades) + `Data`/`DAL` (acceso a datos) | Lógica de negocio, reglas de validación (ej: "no permitir vender sin stock") **y** acceso a la base de datos, todo dentro del mismo proyecto |
| `F.END` (`Avanzada2026`) | Páginas `.aspx` | Capa web — lo que ve y usa el usuario |

`Data` y `DAL` (Data Access Layer) son el mismo concepto — la carpeta que ejecuta el SQL parametrizado (`SqlConnection`/`SqlCommand`) contra SQL Server.

---

## 5. Roles y permisos

Existen **3 roles** de usuario, con acceso jerárquico: **Admin > Encargado > Empleado**.

Login por mail + contraseña (hasheada). Recuperación de contraseña vía mail (token de recuperación con vencimiento).

### Matriz de permisos (v1)

| Acción | Admin | Encargado | Empleado |
|---|:---:|:---:|:---:|
| Gestión de usuarios y roles | ✅ | ❌ | ❌ |
| Clientes, vehículos, turnos | ✅ | ✅ | ✅ |
| Órdenes de trabajo | ✅ | ✅ | ✅ |
| Insumos, proveedores, compras | ✅ | ✅ | 👁️ Solo consulta |
| Ventas y cobro de pagos | ✅ | ✅ | ✅ |
| Cuentas corrientes | ✅ | ✅ | 👁️ Solo consulta |
| Reportes financieros | ✅ | ✅ | ❌ |

---

## 6. Módulos y requerimientos funcionales

### 6.1 Seguridad / Login / Recuperación de contraseña
- Login con mail y contraseña hasheada.
- Recuperación de contraseña por mail (genera token con fecha de vencimiento, de un solo uso).
- Menú dinámico según el nivel/rol del usuario logueado (entidades `Menu`, `Url`, `Nivel` del diagrama).

### 6.2 Clientes y Vehículos
- ABM de clientes (alta, baja, modificación) — datos de contacto (nombre, apellido, DNI, teléfono, email, dirección).
- Cada cliente puede tener uno o varios vehículos asociados (patente, marca, modelo, año, tipo de combustible).
- Búsqueda rápida de cliente por nombre, DNI o patente del vehículo.

### 6.3 Turnos (Agenda)
- El turno **solo lo carga el personal** (encargado o empleado) — no hay portal público para que el cliente lo pida solo.
- Un turno tiene fecha de solicitud, fecha/hora asignada y estado.
- **Estados propuestos** (a confirmar, ver sección 9): Solicitado → Confirmado → Completado / Cancelado.

### 6.4 Órdenes de Trabajo
- Una orden de trabajo puede originarse **de dos formas**:
  - **Con turno previo:** el cliente pidió turno, y al llegar se carga la orden vinculada a ese turno.
  - **Walk-in (sin turno):** el cliente llega directo; si es cliente existente se busca su ficha, si es nuevo se da de alta en el momento, y se crea la orden directamente, sin turno asociado.
  - ➜ El vínculo Turno–Orden es **opcional**, no obligatorio.
- La orden registra: fecha, kilometraje del vehículo, observaciones.
- Incluye el detalle de:
  - **Servicios realizados** (`DetalleOrdenServicio`): qué servicio, precio aplicado.
  - **Insumos utilizados** (`DetalleOrdenInsumo`): qué insumo, cantidad, precio unitario aplicado.
- **Al cargar la orden, el stock de los insumos usados se descuenta automáticamente.**

### 6.5 Proveedores, Insumos y Compras
- ABM de proveedores (razón social, CUIT, teléfono, dirección).
- ABM de insumos/productos (nombre, marca, unidad de medida, stock actual, stock mínimo, precio de venta).
- Registro de compras a proveedores (`ComprobanteCompra` + `DetalleCompra`), con condición de pago, subtotal, impuestos, total, número de comprobante.
- **Al cargar una compra, el stock del insumo se suma automáticamente** (simétrico a la venta).
- El pago de una compra **puede quedar pendiente** (compra "a cuenta" con el proveedor).

### 6.6 Ventas / Comprobantes
- El comprobante de venta **se genera automáticamente al cerrar la orden de trabajo** (no es un paso manual separado).
- Es un **comprobante interno simple**, sin validez fiscal (sin integración AFIP).
- Incluye detalle línea por línea (servicios + insumos consumidos en la orden), subtotal, impuestos, total y saldo pendiente.

### 6.7 Pagos
- Medios de pago aceptados: **efectivo, transferencia, tarjeta**.
- El cliente puede pagar el total o dejar un **saldo pendiente** (queda registrado en su cuenta corriente).
- Un pago puede aplicarse a una venta puntual o a la cuenta corriente en general.

### 6.8 Cuentas Corrientes (Cliente y Proveedor)
- **Cuenta corriente de cliente:** registra movimientos (ventas no cobradas del todo, pagos recibidos) con saldo actualizado.
- **Cuenta corriente de proveedor:** registra movimientos (compras no pagadas del todo, pagos realizados) con saldo actualizado.
- Ambas permiten que el saldo quede a favor o en contra (fiado, en ambos sentidos).

### 6.9 Reportes
Reportes prioritarios definidos por el dueño del negocio:
- **Stock bajo / a reponer:** insumos con `stockActual` por debajo de `stockMinimo`.
  `InsumoDAL.ListarStockBajo()` ya existe para este reporte.
- **Ventas por período:** total vendido en un rango de fechas.
- **Cuentas corrientes:** deudas de clientes y deudas a proveedores, con saldo actual.

Nota (2026-09-07): con `MovimientoStock` (§9.3) ya existente, un futuro reporte de historial de
movimientos de stock (kardex completo, no solo el de stock bajo) es viable sin cambios de esquema
— queda pendiente de priorizar, no comprometido para Fase 5 todavía.

---

## 7. Reglas de negocio clave

1. Toda orden de trabajo puede o no estar asociada a un turno (participación opcional).
2. El stock de insumos se actualiza automáticamente en ambos sentidos: baja al usarse en una orden, sube al cargarse una compra. **Cancelar una orden de trabajo repone el stock de los insumos que no llegaron a usarse.** Cada cambio de stock (compra, orden, cancelación, ajuste manual) queda registrado con fecha, usuario y motivo — ver §9.3.
3. La venta no se carga manualmente: nace automáticamente al cerrar una orden de trabajo.
4. Tanto clientes como proveedores pueden operar "a cuenta" (saldo pendiente permitido en ambos casos).
5. El acceso a compras, cuentas corrientes y reportes financieros está restringido para el rol Empleado (solo consulta o sin acceso, según el módulo).
6. No hay emisión de comprobantes fiscales (AFIP) en esta versión del sistema.

---

## 8. Modelo de entidades (referencia)

Basado en el diagrama E/R provisto (`01__Modelo_Conceptual_Diagrama_ER.pdf`, notación Date cap. 13.4):

**Negocio:** Cliente, Vehiculo, Turno, Servicio, OrdenDeTrabajo, DetalleOrdenServicio, DetalleOrdenInsumo, Insumo, Proveedor, ComprobanteCompra, DetalleCompra, ComprobanteVenta, DetalleComprobanteVenta, Pago, CuentaCorrienteCliente, CuentaCorrienteProveedor.

**Seguridad / Login / Menú:** Usuario, Nivel, Url, Menu, RecuperacionClave.

*(21 entidades en total, 17 normales y 4 débiles; 28 vínculos, según el diagrama original.)*

**Nota (2026-09-07):** se sumó `MovimientoStock` en Fase 2, como entidad adicional a las 21 del
diagrama original — kardex de stock, ver §9.3. No es parte del diagrama entregado por la
cátedra; es un agregado posterior justificado por una necesidad real de trazabilidad.

---

## 9. Supuestos a confirmar

Detalles menores que se resuelven con una propuesta razonable, pendientes de validar con el dueño del negocio:

- **Catálogo de servicios:** se asume una lista fija mantenida por Admin/Encargado (nombre, descripción, precio base), sin categorías adicionales por ahora.

### 9.1 Formato de DNI, CUIT y patente (confirmado 2026-09-06)

- **DNI (`Cliente.dni`):** 7 u 8 dígitos, solo números, sin puntos. Se guarda y se muestra tal
  cual (`12345678`).
- **CUIT (`Proveedor.cuit`):** 11 dígitos. Se guarda **sin guiones** (`20123456786`) y se
  **muestra en pantalla con guiones**, formato `NN-NNNNNNNN-N` (`20-12345678-6`).
- **Patente (`Vehiculo.patente`):** acepta **ambos formatos** vigentes en Argentina —
  el viejo (3 letras + 3 números, ej. `ABC123`) y el Mercosur (2 letras + 3 números + 2 letras,
  ej. `AB123CD`). Se guarda en mayúsculas.

### 9.2 Diseño de Clientes y Vehículos (confirmado 2026-09-06)

- **Dos pantallas independientes, no maestro-detalle.** `Clientes` y `Vehiculos` quedan
  separadas tal como ya estaban en el menú (§6.2 habla de "clientes con sus vehículos asociados",
  pero eso se resuelve con navegación cruzada, no con una grilla anidada).
- **Búsqueda rápida (§6.2) partida en dos:** el buscador por nombre/apellido/DNI vive en
  `Clientes`; el de patente/marca/modelo vive en `Vehiculos`. Cada pantalla tiene un botón por
  fila para saltar a la otra ya filtrada (ej. "Ver vehículos" en la fila del cliente, "Ver
  cliente" en la fila del vehículo).
- **El mismo buscador de clientes se reutiliza como selector de dueño** en el alta de un
  Vehículo, en vez de un `DropDownList` con todos los clientes (no escala igual que el de roles
  en `Usuarios`, que es una lista fija de 3).
- **Tipo de combustible:** `DropDownList` con lista fija — Nafta, Diésel, GNC, Eléctrico,
  Híbrido.

### 9.3 Kardex de stock (confirmado 2026-09-07)

- **Cada cambio de stock queda registrado** en una entidad nueva, `MovimientoStock` (no estaba en
  el diagrama original — ver nota en §8): quién, cuándo, cuánto y por qué.
- **Invariante:** `Insumo.stockActual` siempre es igual a la suma de `entrada - salida` de todos
  sus movimientos. Se mantiene desde el primer insumo: al dar de alta uno con stock inicial, se
  inserta con `stockActual = 0` y se registra un ajuste manual aparte por el valor cargado.
- **Tipos de movimiento:** `Compra` (sube stock, Fase 4, todavía sin implementar), `Orden` (baja
  stock, implementado en Fase 3 — `DetalleOrdenInsumoDAL.Agregar`), `CancelacionOrden` (repone el
  stock no utilizado si se cancela una orden, implementado en Fase 3 —
  `OrdenDeTrabajoDAL.Cancelar`), `AjusteManual` (alta con stock inicial, o corrección manual con
  motivo obligatorio, implementado en Fase 2).
- **`idUsuario` es obligatorio** en `MovimientoStock` (a diferencia de `CuentaCorrienteCliente`/
  `Proveedor`, que no lo tienen): se pidió explícitamente poder saber quién hizo cada ajuste
  manual.
- **Stock insuficiente para una salida se rechaza** con un mensaje de error — no se permite que
  el stock quede negativo.
- **Unidad de medida** (`Insumo.unidadMedida`): `DropDownList` con lista fija — Unidad, Litro,
  Kilogramo, Caja, Metro.

### 9.4 Turnos y Órdenes de trabajo (confirmado 2026-09-11/2026-09-14)

- **Estados de Turno:** `Solicitado` (default al crear) → `Confirmado` → `Completado` /
  `Cancelado`, fijados por `CHECK`. Sin reglas de transición forzadas — el estado es un campo
  editable más, no hay una máquina de estados que impida, por ejemplo, volver de `Completado` a
  `Solicitado`.
- **Estados de Orden de Trabajo:** `Abierta` (default al crear) → `En proceso` → `Cerrada` /
  `Cancelada`, fijados por `CHECK`. A diferencia de Turno, acá `Cancelada` **sí** tiene un efecto
  colateral (repone el stock de los insumos cargados) y por eso no se ofrece en el mismo
  `DropDownList` que los otros tres estados: se llega solo por una acción "Cancelar orden"
  separada, con confirmación.
- **Cliente/vehículo (y en Orden, turno) quedan fijos una vez creada la fila** — no se pueden
  reasignar editando. En Turno el vehículo es opcional; en Orden el vehículo es obligatorio
  (`idVehiculo NOT NULL`) y el turno es opcional (walk-in vs. turno previo, §6.4).
- **"Nuevo cliente" desde Turnos: fuera de alcance.** El alta de cliente en el momento es
  específicamente del flujo walk-in de Órdenes (§6.4); si un turno es para un cliente que no
  existe todavía, se lo da de alta primero en `Clientes.aspx`.
- **Walk-in de Órdenes con cliente y vehículo nuevos:** al ser `idVehiculo NOT NULL`, un walk-in
  genuinamente nuevo permite crear cliente **y** vehículo sin salir de la pantalla de Órdenes
  (atajos "Nuevo cliente"/"Nuevo vehículo" que redirigen a `Clientes.aspx`/`Vehiculos.aspx` y
  vuelven con los datos de la orden en curso preservados).
- **Sin "deshacer" una línea de insumo individual** en una Orden: si hace falta corregir una
  cantidad cargada mal, se cancela la orden completa (que repone todo el stock) y se rehace. Las
  líneas de servicio sí se pueden quitar libremente (no tienen efecto sobre stock).

### 9.5 Compras — condición de pago y medio de pago (confirmado 2026-09-14)

- **Una compra "Contado" no genera ninguna fila en `Pago` ni en `CuentaCorrienteProveedor`**
  (`saldoPendiente = 0` desde el alta) — se asume pagada por fuera del circuito de cobranzas del
  sistema. **Pero el medio de pago usado sí queda registrado**, en una columna nueva
  `ComprobanteCompra.medioPago` (no estaba en el diagrama original) — decisión explícita del
  dueño del negocio, que quiso que quedara trazado igual que se traza el insumo que baja stock.
- **Una compra "Cuenta corriente" no tiene medio de pago** (`medioPago = NULL`): todavía no se
  pagó nada, así que no hay medio de pago que registrar — recién se sabrá cuando se cargue el
  `Pago` que la cancele (total o parcialmente).
- **Ajuste manual de cuenta corriente:** confirmado que sí se va a construir (motivo + monto con
  signo, mismo patrón que el ajuste de stock de Insumos), aunque el requerimiento §6.8 no lo pide
  explícitamente — el esquema ya reservaba un tipo `Ajuste` para esto. Para trazar quién hizo el
  ajuste se agregó `CuentaCorrienteCliente`/`Proveedor.idUsuario` (nullable, poblado solo en
  movimientos `Ajuste` — los automáticos de Venta/Compra/Pago no lo necesitan).
- **Numeración de comprobantes** (pendiente en §9 desde el documento original): correlativo
  interno derivado del propio `IDENTITY` de la tabla, con prefijo por tipo — `C-000001`,
  `C-000002`, ... para compras; `V-000001`, ... para ventas. Sin tabla de secuencia aparte.

---

## 10. Fuera de alcance (por ahora)

- Integración fiscal con AFIP (facturación electrónica real).
- Portal público para que el cliente pida turno online por su cuenta.
- Notificaciones automáticas por mail/SMS (recordatorio de turno, aviso de stock bajo, etc.).
- Multi-sucursal (se asume un único local).

Estos puntos quedan como posibles mejoras para una segunda etapa del proyecto.

---

## 11. Próximos pasos

1. Validar este documento con el dueño del negocio (confirmar sección 9).
2. Definir el diseño físico de la base de datos (tablas, claves, tipos de datos) a partir del diagrama E/R y estas reglas de negocio.
3. Traducir cada módulo en tareas concretas de desarrollo (pantallas `.aspx`, clases en `BIZ`/`Model`/`Data`).
4. Priorizar el orden de desarrollo de los módulos.
