# Lubricentro — Roadmap de Ejecución

**Basado en:** `Lubricentro_Requerimientos.md`
**Enfoque:** 6 fases, avanzando en paralelo sobre todas las pantallas (no se termina un módulo 100% antes de arrancar el siguiente)

---

## Cómo se piensa este roadmap

En vez de terminar **Clientes** al 100% y recién ahí arrancar **Vehículos**, cada fase agrega una "capa" de funcionalidad que cruza **todas las pantallas a la vez**. Así, desde temprano hay un sistema navegable de punta a punta, aunque todavía le falten cosas — y se puede repartir trabajo en pantallas distintas sin pisarse.

---

## Fase 1 — Cimientos: Login, Roles y Menú

**Objetivo:** que el esqueleto completo de la aplicación exista y se pueda navegar, aunque las pantallas todavía no hagan nada.

- Login con mail + contraseña hasheada.
- Recuperación de contraseña por mail.
- ABM de Usuarios y asignación de rol (admin / encargado / empleado).
- Menú principal dinámico: cada rol ve solo las opciones que le corresponden.
- **Todas las pantallas del sistema creadas como "cascarón"** (aunque estén vacías) y enlazadas desde el menú — esto es lo que habilita trabajar en paralelo después.
- Capa `BIZ/Data` (DAL) funcionando de punta a punta con al menos un caso de prueba real (confirmar que la conexión a SQL Server vía VPN Radmin anda).

📌 *Esta fase es la que pediste priorizar: sin esto, no hay dónde "enchufar" el resto.*

---

## Fase 2 — ABM de entidades maestras (en paralelo)

**Objetivo:** poder cargar y consultar los datos base del negocio. Son pantallas independientes entre sí → se desarrollan todas a la vez.

- Clientes (+ sus vehículos)
- Proveedores
- Insumos (catálogo + stock inicial)
- Servicios (catálogo + precio base)
- Validaciones de formulario (campos obligatorios, formatos de mail/DNI/patente, etc.)

📌 *Ninguna de estas pantallas depende de otra — ideal para repartir entre pantallas distintas en simultáneo.*

---

## Fase 3 — Turnos y Órdenes de Trabajo

**Objetivo:** el corazón operativo del día a día del lubricentro.

- Alta y gestión de turnos.
- Carga de orden de trabajo — **con turno previo o walk-in** (sin turno), las dos vías.
- Detalle de servicios e insumos aplicados a la orden.
- Campo **estado** de la orden (Abierta / Cerrada / Cancelada).
- Descuento automático de stock al cargar la orden.

📌 *Depende de que Fase 2 ya tenga Clientes, Vehículos, Servicios e Insumos cargables.*

---

## Fase 4 — Circuito de plata: Compras, Ventas, Pagos, Cuentas corrientes

**Objetivo:** que el dinero se mueva bien por el sistema, en los dos sentidos (a favor y en contra).

- Registro de compras a proveedores (con alta automática de stock).
- Generación automática de la venta al cerrar una orden de trabajo.
- Registro de pagos (cliente y proveedor), con medio de pago (efectivo / transferencia / tarjeta).
- Cuentas corrientes de cliente y de proveedor, con saldo actualizado.

📌 *Depende de Fase 3 (para que existan órdenes que generar como venta) y de Proveedores/Insumos de Fase 2.*

---

## Fase 5 — Reportes

**Objetivo:** que el dueño pueda tomar decisiones con datos reales — necesita que ya haya información cargada de las fases anteriores.

- Stock bajo / a reponer.
- Ventas por período.
- Cuentas corrientes (deudas de clientes y a proveedores).

📌 *Es la fase que menos sentido tiene adelantar — sin datos de Fases 2 a 4, un reporte no se puede ni probar bien.*

---

## Fase 6 — Integración, pruebas y pulido

**Objetivo:** que todo el circuito funcione junto, sin baches.

- Pruebas de flujo completo: turno → orden → venta → pago → cuenta corriente.
- Validaciones cruzadas entre módulos (ej: no permitir cerrar una orden sin stock suficiente).
- Revisión de permisos por rol en cada pantalla (que un empleado no vea lo que no debe).
- Mejora de mensajes de error y UX (Bootstrap).
- Documentación final y preparación para la entrega.

---

## Resumen visual

| Fase | Foco | Depende de |
|---|---|---|
| 1 | Login + Menú + esqueleto de pantallas | — |
| 2 | ABM de Clientes, Vehículos, Proveedores, Insumos, Servicios | Fase 1 |
| 3 | Turnos + Órdenes de trabajo | Fase 2 |
| 4 | Compras, Ventas, Pagos, Cuentas corrientes | Fase 3 |
| 5 | Reportes | Fase 4 |
| 6 | Integración, pruebas y pulido | Todas |
