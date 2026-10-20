# Registro de cambios

Resumen de los últimos tres envíos al repositorio, del más nuevo al más viejo. El detalle de cada
sesión está en `Docs/EstadoActual.md` y las decisiones de negocio en
`Docs/Lubricentro_Requerimientos.md` §9.

> **Los tres cambian el esquema de la base.** Con una base anterior, recrearla con `01` a `04`
> (el `01` borra los datos); `05` es opcional y carga las imágenes de ejemplo. Ver `CLAUDE.md`,
> «Al levantar el proyecto en otra máquina».

---

## 1. 2026-10-13 — Código postal, proveedores, usuario de la base y tablas que entran en la pantalla

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

## 2. 2026-10-08 — Imagen de cada producto, datos del comercio editables, teléfonos y login

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

## 3. 2026-10-08 — Barra lateral, listas, productos con IVA, factura, datos fiscales y roles

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
