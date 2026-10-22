# Registro de cambios

Resumen de los últimos envíos al repositorio, del más nuevo al más viejo. El detalle de cada
sesión está en `Docs/EstadoActual.md` y las decisiones de negocio en
`Docs/Lubricentro_Requerimientos.md` §9.

> **Todos, salvo el 3, cambian el esquema de la base.** Con una base anterior, recrearla con `01` a `04`
> (el `01` borra los datos); `05` es opcional y carga las imágenes de ejemplo. El 1, el 2, el 4 y el 5
> traen además un script para poner al día, sin perder datos, una base que ya tiene lo anterior.
> Ver `CLAUDE.md`, «Al levantar el proyecto en otra máquina».

---

## 1. 2026-10-22 — Factura y nota de crédito vinculadas con el comercio (`Emisor`)

**Cambia el esquema:** `Factura` y `NotaCredito` con `idEmisor` (NOT NULL, DEFAULT 1) y su clave
externa a `Emisor`, que dejaba de estar suelta en los diagramas. Una base existente se pone al día
con `Database\Actualizar_2026-10-22_Emisor.sql`, sin perder datos. El código no cambia.

---

## 2. 2026-10-22 — Pendientes de la entrega final

Requerimientos §9.19. **Cambia el esquema** (dos CHECK nuevos); una base existente se pone al día
con `Database\Actualizar_2026-10-22_CuentaCorrienteProveedor.sql`, sin perder datos, y conviene
volver a correr `06`.

- Cerrar una orden guarda la venta, su movimiento y el estado `Cerrada` en una sola transacción.
- El saldo de cada movimiento de cuenta corriente (clientes y proveedores) y el saldo a favor que
  se aplica a una venta o a una compra se leen con bloqueo, dentro de la transacción: dos
  movimientos simultáneos ya no parten del mismo saldo.
- `CuentaCorrienteProveedor` con `CK_CCProv_debeHaber` y `CK_CCProv_origen`.
- Números `V-` y `C-` sin tope de 6 dígitos.
- Página de error propia (`~/Error`, `customErrors`) y registro en `App_Data\Errores.log`.
- Validadores de cantidad y precio en las líneas de Compras.
- El token de recuperación de clave se guarda como hash (SHA-256).
- `Web.config` con `MailModoDesarrollo=true`: los mails quedan en `App_Data\MailsEnviados`.
- `06`: `sp_HistorialVehiculo` marca las ventas anuladas y `sp_RegistrarPagoCliente` bloquea el
  saldo del cliente al empezar.

Verificado con un programa de prueba sobre una base nueva y sobre una copia de la base de Alexis
(50 de 50 en las dos, con concurrencia), con la pantalla real en IIS Express (13 de 13), y en SQL
Server 2008 R2. Antes del arreglo, dos ventas simultáneas usaban el mismo saldo a favor 7 de cada
10 veces y la cadena de saldos se cortaba; después, 0 de 10.

---

## 3. 2026-10-21 — Procedimientos almacenados

Requerimientos §9.18. **No cambia las tablas:** script nuevo y opcional,
`Database\06_ProcedimientosAlmacenados.sql`, que se corre después de `01` y `02`.

- Seis procedimientos: `sp_AjustarStock` y `sp_RegistrarPagoCliente` (con transacción) y
  `sp_StockBajo`, `sp_VentasPorPeriodo`, `sp_SaldosCuentaCorriente` y `sp_HistorialVehiculo`.
- Repiten las reglas de la aplicación, pero la aplicación no los usa.
- Se ejecutan con un usuario administrador (`lubricentro_app` no tiene permiso).
- 2026-10-22: `sp_VentasPorPeriodo` descuenta las ventas anuladas con nota de crédito, como el
  reporte de la aplicación (columna `anulada` y totales `notasCredito`, `totalAnulado` y
  `netoVendido`; lo cobrado no cuenta las anuladas). Con una base que ya tiene los procedimientos,
  alcanza con volver a correr `06`.

Verificado sobre una base nueva creada con `01` a `05`: el script corre dos veces sin errores, las
operaciones dan lo esperado y los errores no dejan cambios a medias.

---

## 4. 2026-10-20 — Nota de crédito

Requerimientos §9.17. Tabla nueva: `NotaCredito`. **Sin ella Ventas, Pagos y Órdenes fallan:** con
una base que ya tiene lo del 2026-10-18, correr `Database\Actualizar_2026-10-20_NotaCredito.sql`.

- Desde Ventas, una venta (facturada o no) se anula con una nota de crédito: completa, una por venta,
  con motivo obligatorio. Toma la letra de la factura (o la que le correspondería), numera aparte por
  letra y punto de venta, y se imprime con el formato de la factura.
- Acredita el total en la cuenta corriente del cliente (movimiento "Nota de crédito") y deja la venta
  sin saldo pendiente. No repone stock. Una venta anulada no se factura.
- Ventas distingue las anuladas (opción "Anuladas"); Ventas por período resta las notas de crédito.
- La factura y la nota de crédito muestran el operador que las emitió, y Ventas suma la columna
  "Facturó" con un filtro de un botón por usuario, para ver las ventas de cada cajero. Esto no cambia
  la base: sale del usuario que cada comprobante ya guardaba.

Verificado con MSBuild, `aspnet_compiler`, base creada con `01` a `05`, 19 pruebas sobre `BIZ.dll`
(con dos emisiones simultáneas de la misma venta) y el recorrido de la pantalla en IIS Express.

---

## 5. 2026-10-18 — Una venta por orden, restricciones y stock en relativo

Commit `36263a1`. Script para una base existente: `Database\Actualizar_2026-10-18_Restricciones.sql`.

- `UQ_ComprobanteVenta_orden`: dos cierres simultáneos de la misma orden ya no generan dos ventas.
- CHECK nuevos en `ComprobanteVenta`, `DetalleComprobanteVenta` y `CuentaCorrienteCliente`.
- El stock se actualiza en relativo, con la condición de no quedar negativo en el mismo `UPDATE`: dos
  movimientos simultáneos sobre el mismo insumo ya no se pisan.

Verificado con pruebas en SQL y un programa con hilos simultáneos: antes duplicaba ventas y vendía de
más; ahora no.

---

## 6. 2026-10-13 — Código postal, proveedores, usuario de la base y tablas que entran en la pantalla

Requerimientos §9.12 y §9.14 a §9.16.

**Código postal que completa el domicilio**
- En Clientes y Proveedores, al escribir el código postal se eligen solos la provincia y la
  localidad principal; las demás localidades del código quedan como sugerencias.
- La guía (2.105 códigos) va embebida en `BIZ.dll` (`Modelo/CodigosPostales.txt`, datos de
  GeoNames, CC BY 4.0): no hace falta correr ningún script. La consulta `CodigoPostal.ashx`
  (exige sesión).
- Resuelve la CABA, la letra del CPA manda sobre la provincia y un código desconocido toma la
  provincia de sus vecinos.
- Una localidad escrita a mano no se pisa, salvo que el código nuevo sea de otra provincia. El
  servidor valida igual.
- `Modelo/Domicilio.cs`: provincias, formato del código postal y domicilio en una línea,
  compartido por Cliente y Proveedor.

**Proveedores con más datos**
- Nombre de fantasía, condición frente al IVA (obligatoria), rubro, contacto y su teléfono,
  domicilio completo, CBU o CVU, alias y observaciones.
- El selector de proveedor de Compras y Pagos también lo encuentra por el nombre de fantasía.

**Usuario de la base**
- `01_Esquema.sql` crea el login de SQL Server `lubricentro_app` (solo `db_datareader` y
  `db_datawriter`) y `Web.config` entra con él, en vez de con el usuario de Windows.
- LocalDB no acepta conexiones desde otra PC: para compartir la base por VPN Radmin hace falta un
  SQL Server con TCP/IP y modo mixto.

**Interfaz**
- Las tablas se achican de a pasos para entrar en la pantalla; desde "Columnas" no se suma una
  columna que no entra. En pantallas angostas la tabla se desplaza dentro de su marco.
- Documento, CUIT y teléfono nunca se parten en dos renglones.
- "Cambiar dueño" queda solo en la edición del vehículo (ya no en cada fila).

**Datos de ejemplo**
- 6 insumos nuevos con stock bajo, con sus ilustraciones.

Verificado con MSBuild, `aspnet_compiler`, base recreada con `01` a `05` y 89 pruebas de punta a
punta en Chrome.

---

## 7. 2026-10-08 — Imagen de cada producto, datos del comercio editables, teléfonos y login

Commit `b95f53c`. Requerimientos §9.13. Tablas nuevas: `Emisor` e `ImagenProducto`.

**Imagen de cada producto**
- PNG o JPG opcional, de hasta 5 MB, con vista previa en el formulario de Productos; miniatura en
  la lista y grande en "Ver".
- Se guarda en la base, achicada a 800 px y con una miniatura de 120 px (`Modelo/Imagen.cs`); una
  foto de teléfono se endereza según su EXIF. La sirve `ImagenProducto.ashx`.
- 20 ilustraciones propias para los productos de ejemplo (`05_ImagenesDemo.sql`).

**Datos del comercio**
- Pasan de `Web.config` a la tabla `Emisor`, editable desde Administración > Datos del comercio
  (`~/DatosComercio`).

**Teléfonos**
- Se guardan solo con los dígitos (y el "+" inicial).

**Pantallas de ingreso**
- Título "Lubricentro Control" en el login, botón "Ingresar" centrado y los mensajes de los
  validadores ya no se ven apenas abre la página.

---

## 8. 2026-10-08 — Barra lateral, listas, productos con IVA, factura, datos fiscales y roles

Commit `e1f137f`. Segundo pedido del 2026-10-07, en 13 puntos (Requerimientos §9.7 a §9.12).

**Interfaz**
- Menú en una barra lateral con íconos; en el celular se abre con un botón.
- Listas con opciones de filtro en botones, elección de columnas (guardada en el navegador) y
  "Ver" con todos los datos del registro.
- Turnos: los de hoy primero y marcados.

**Reglas de negocio**
- Producto como supertipo de Servicio e Insumo, en una sola pantalla (`~/Productos`), con SKU,
  código de barras, tipo de IVA y alícuota. Se borraron las pantallas Insumos y Servicios.
- Precios finales con IVA incluido: cada línea de venta guarda el IVA que contiene.
- Factura A/B/C imprimible desde Ventas, numerada por letra y punto de venta, sin validez fiscal.
- Cliente: persona o empresa, tipo de documento, condición frente al IVA y domicilio completo.
- Cerrar la orden de un cliente con cuenta corriente pregunta si el saldo queda en la cuenta o se
  cobra.
- Cambio de dueño de un vehículo, rechazado con una orden o un turno en curso.
- Roles y permisos editables por pantalla (`~/Roles`).

Verificado con MSBuild, `aspnet_compiler` y 142 pruebas de punta a punta en Chrome.
