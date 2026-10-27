using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using BIZ.Modelo;

namespace BIZ.Data
{
    // Asistente de ventas y stock (Reportes/AsistenteVentas, Requerimientos §9.21): la IA
    // (ServicioIAVentas) solo elige una consulta del catálogo cerrado (ConsultaVentasIA), el período y
    // el filtro; las consultas son las de abajo. Solo lectura, SQL parametrizado, y el texto de la
    // respuesta lo arma C#, para que ningún número pase por el modelo. El filtro se usa como
    // parámetro de LIKE, palabra por palabra y sin distinguir mayúsculas ni tildes.
    public static class AsistenteVentasDAL
    {
        // La reposición mira el consumo de los últimos 30 días.
        private const int DiasConsumo = 30;

        // Palabras que no sirven para buscar un nombre ("filtro de aire" busca "filtro" y "aire").
        private static readonly string[] PalabrasVacias = { "de", "del", "la", "las", "el", "los", "y", "para" };

        /* --- Despachador ----------------------------------------------------------- */

        // Pregunta en lenguaje natural: la IA interpreta, C# valida y ejecuta.
        public static RespuestaVentasIA Responder(string pregunta)
        {
            if (string.IsNullOrWhiteSpace(pregunta))
                return RespuestaVentasIA.Error("Escribí una pregunta.");

            InterpretacionVentasIA interpretacion;
            var interpretar = ServicioIAVentas.Interpretar(pregunta.Trim(), out interpretacion);
            if (!interpretar.Exito)
                return RespuestaVentasIA.Error(interpretar.Mensaje);

            // Lista blanca: lo que la IA diga que no esté en el catálogo ("ninguna", un id inventado) no se ejecuta.
            var consulta = ConsultaVentasIA.Buscar(interpretacion.Consulta);
            if (consulta == null)
                return RespuestaVentasIA.Ok(MensajeNoSe());

            DateTime desde, hasta;
            ResolverPeriodo(interpretacion.Desde, interpretacion.Hasta, out desde, out hasta);
            return Ejecutar(consulta, desde, hasta, interpretacion.Filtro);
        }

        // Botones de ejemplo: la consulta del mes actual, sin filtro y sin pasar por la IA, así la
        // pantalla sigue respondiendo aunque Ollama esté cerrado.
        public static RespuestaVentasIA EjecutarDirecto(string idConsulta)
        {
            var consulta = ConsultaVentasIA.Buscar(idConsulta);
            if (consulta == null)
                return RespuestaVentasIA.Ok(MensajeNoSe());

            DateTime desde, hasta;
            MesActual(out desde, out hasta);
            return Ejecutar(consulta, desde, hasta, null);
        }

        public static RespuestaVentasIA Ejecutar(ConsultaVentasIA consulta, DateTime desde, DateTime hasta, string filtro)
        {
            var periodo = "del " + desde.ToString("dd/MM/yyyy") + " al " + hasta.ToString("dd/MM/yyyy");
            filtro = LimpiarFiltro(filtro);

            try
            {
                switch (consulta.Id)
                {
                    case ConsultaVentasIA.MasVendidos:
                        return MasVendidos(desde, hasta, periodo, filtro);
                    case ConsultaVentasIA.Deudores:
                        return Deudores(filtro);
                    case ConsultaVentasIA.StockBajo:
                        // "¿Quedan bujías?": si la IA eligió stock bajo pero nombró un insumo, la pregunta
                        // es por ese insumo.
                        return filtro == null ? StockBajo() : StockInsumo(filtro);
                    case ConsultaVentasIA.StockInsumo:
                        return StockInsumo(filtro);
                    case ConsultaVentasIA.Reposicion:
                        return Reposicion();
                    case ConsultaVentasIA.VentasPorCajero:
                        return VentasPorCajero(desde, hasta, periodo, filtro);
                    case ConsultaVentasIA.ResumenVentas:
                        return ResumenVentas(desde, hasta, periodo);
                    default:
                        return RespuestaVentasIA.Ok(MensajeNoSe());
                }
            }
            catch (Exception)
            {
                return RespuestaVentasIA.Error("No se pudo consultar la base de datos. Probá de nuevo en un momento.");
            }
        }

        private static string MensajeNoSe()
        {
            return "Todavía no sé responder eso. Puedo responder: " +
                string.Join("; ", ConsultaVentasIA.Catalogo.Select(c => c.Descripcion)) + ".";
        }

        // Fechas de la IA: si falta alguna o no es una fecha válida, el mes actual; si vienen al revés,
        // se dan vuelta.
        private static void ResolverPeriodo(string textoDesde, string textoHasta, out DateTime desde, out DateTime hasta)
        {
            var validas = DateTime.TryParseExact(textoDesde, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                              DateTimeStyles.None, out desde)
                          & DateTime.TryParseExact(textoHasta, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                              DateTimeStyles.None, out hasta);
            if (!validas)
            {
                MesActual(out desde, out hasta);
                return;
            }

            if (desde > hasta)
            {
                var aux = desde;
                desde = hasta;
                hasta = aux;
            }
        }

        private static void MesActual(out DateTime desde, out DateTime hasta)
        {
            var hoy = DateTime.Today;
            desde = new DateTime(hoy.Year, hoy.Month, 1);
            hasta = desde.AddMonths(1).AddDays(-1);
        }

        /* --- Ventas -------------------------------------------------------------- */

        // Lo vendido en el período, sin las ventas anuladas con nota de crédito. Sin filtro, top 5 de
        // servicios y top 5 de insumos (un servicio y un litro de aceite no se comparan); con
        // "servicio" o "insumo", top 10 de ese tipo; con otra palabra ("aceite"), el ranking de lo
        // que se llama así.
        private static RespuestaVentasIA MasVendidos(DateTime desde, DateTime hasta, string periodo, string filtro)
        {
            var tipoItem = TipoItem(filtro);
            var nombre = tipoItem == null ? filtro : null;
            var parametros = new List<SqlParameter>
            {
                AccesoDatos.Param("@desde", desde.Date),
                AccesoDatos.Param("@hastaExclusiva", hasta.Date.AddDays(1)),
                AccesoDatos.Param("@tipoItem", tipoItem),
                AccesoDatos.Param("@porTipo", tipoItem == null ? 5 : 10),
                AccesoDatos.Param("@tipoServicio", DetalleComprobanteVenta.TipoServicio)
            };
            var condicionNombre = nombre == null ? "" : " AND " + CondicionPalabras("d.descripcion", nombre, parametros);

            var filas = AccesoDatos.Consultar(
                @"WITH vendido AS (
                      SELECT d.tipoItem, ISNULL(d.idServicio, d.idInsumo) AS idProducto,
                             MAX(d.descripcion) AS descripcion, SUM(d.cantidad) AS cantidad,
                             SUM(d.subtotal) AS importe, COUNT(DISTINCT d.idVenta) AS ventas
                      FROM DetalleComprobanteVenta d
                      JOIN ComprobanteVenta v ON v.idVenta = d.idVenta
                      LEFT JOIN NotaCredito nc ON nc.idVenta = v.idVenta
                      WHERE v.fecha >= @desde AND v.fecha < @hastaExclusiva
                        AND nc.idNotaCredito IS NULL
                        AND (@tipoItem IS NULL OR d.tipoItem = @tipoItem)" + condicionNombre + @"
                      GROUP BY d.tipoItem, ISNULL(d.idServicio, d.idInsumo)),
                  ranking AS (
                      SELECT vendido.*, ROW_NUMBER() OVER (PARTITION BY tipoItem
                                                           ORDER BY cantidad DESC, importe DESC) AS puesto
                      FROM vendido)
                  SELECT r.tipoItem, COALESCE(p.nombre, r.descripcion) AS nombre, r.cantidad, r.importe, r.ventas
                  FROM ranking r
                  LEFT JOIN Producto p ON p.idProducto = r.idProducto
                  WHERE r.puesto <= @porTipo
                  ORDER BY CASE r.tipoItem WHEN @tipoServicio THEN 0 ELSE 1 END, r.puesto",
                parametros.ToArray()).Rows;

            if (filas.Count == 0)
                return RespuestaVentasIA.Ok("No hay ventas" +
                    (tipoItem == null ? "" : " de " + (tipoItem == DetalleComprobanteVenta.TipoServicio ? "servicios" : "insumos")) +
                    (nombre == null ? "" : " de «" + nombre + "»") + " " + periodo + " (sin contar las anuladas).");

            var tabla = NuevaTabla("Tipo", "Producto", "Cantidad", "Importe", "Ventas");
            DataRow servicio = null, insumo = null;
            foreach (DataRow f in filas)
            {
                var esServicio = AccesoDatos.LeerString(f, "tipoItem") == DetalleComprobanteVenta.TipoServicio;
                if (esServicio && servicio == null) servicio = f;
                if (!esServicio && insumo == null) insumo = f;
                tabla.Rows.Add(esServicio ? "Servicio" : "Insumo", AccesoDatos.LeerString(f, "nombre"),
                    Cantidad(AccesoDatos.LeerDecimal(f, "cantidad")), Pesos(AccesoDatos.LeerDecimal(f, "importe")),
                    AccesoDatos.LeerInt(f, "ventas").ToString());
            }

            var partes = new List<string>();
            if (servicio != null)
                partes.Add("el servicio más vendido es " + AccesoDatos.LeerString(servicio, "nombre") + " (" +
                    Cantidad(AccesoDatos.LeerDecimal(servicio, "cantidad")) + " veces, " +
                    Pesos(AccesoDatos.LeerDecimal(servicio, "importe")) + ")");
            if (insumo != null)
                partes.Add("el insumo más vendido es " + AccesoDatos.LeerString(insumo, "nombre") + " (" +
                    Cantidad(AccesoDatos.LeerDecimal(insumo, "cantidad")) + " vendidos, " +
                    Pesos(AccesoDatos.LeerDecimal(insumo, "importe")) + ")");

            return RespuestaVentasIA.Ok("Sin contar las ventas anuladas, " + periodo +
                (nombre == null ? " " : " y entre lo que se llama «" + nombre + "», ") + string.Join(" y ", partes) +
                (tipoItem == null ? ". Los 5 primeros de cada uno:" : ". Los 10 primeros:"), tabla);
        }

        // Ventas facturadas por cada cajero: el usuario que emitió la factura (la misma trazabilidad
        // que el filtro "Facturó" de Ventas), por fecha de la factura. Neto = sin las ventas que
        // después se anularon con nota de crédito.
        private static RespuestaVentasIA VentasPorCajero(DateTime desde, DateTime hasta, string periodo, string filtro)
        {
            var parametros = new List<SqlParameter>
            {
                AccesoDatos.Param("@desde", desde.Date),
                AccesoDatos.Param("@hastaExclusiva", hasta.Date.AddDays(1))
            };
            var condicion = filtro == null ? "" : " AND " + CondicionPalabras("u.nombre + ' ' + u.apellido", filtro, parametros);

            var filas = AccesoDatos.Consultar(
                @"SELECT u.nombre + ' ' + u.apellido AS cajero, COUNT(*) AS facturas, SUM(v.total) AS facturado,
                         SUM(CASE WHEN nc.idNotaCredito IS NULL THEN 0 ELSE 1 END) AS anuladas,
                         SUM(CASE WHEN nc.idNotaCredito IS NULL THEN 0 ELSE v.total END) AS anulado
                  FROM Factura f
                  JOIN ComprobanteVenta v ON v.idVenta = f.idVenta
                  JOIN Usuario u ON u.idUsuario = f.idUsuario
                  LEFT JOIN NotaCredito nc ON nc.idVenta = v.idVenta
                  WHERE f.fecha >= @desde AND f.fecha < @hastaExclusiva" + condicion + @"
                  GROUP BY u.idUsuario, u.nombre, u.apellido
                  ORDER BY SUM(v.total) - SUM(CASE WHEN nc.idNotaCredito IS NULL THEN 0 ELSE v.total END) DESC",
                parametros.ToArray()).Rows;

            if (filas.Count == 0)
            {
                if (filtro == null)
                    return RespuestaVentasIA.Ok("No se emitieron facturas " + periodo + ".");

                // ¿No hay ningún usuario con ese nombre, o hay pero no facturó en el período?
                var usuarios = new List<SqlParameter>();
                var existe = AccesoDatos.Consultar(
                    "SELECT TOP 1 nombre + ' ' + apellido AS usuario FROM Usuario u WHERE " +
                    CondicionPalabras("u.nombre + ' ' + u.apellido", filtro, usuarios), usuarios.ToArray()).Rows;
                return RespuestaVentasIA.Ok(existe.Count == 0
                    ? "No hay ningún usuario que se llame «" + filtro + "»."
                    : AccesoDatos.LeerString(existe[0], "usuario") + " no emitió facturas " + periodo + ".");
            }

            if (filas.Count == 1 && filtro != null)
            {
                var f = filas[0];
                var anuladas = AccesoDatos.LeerInt(f, "anuladas");
                var facturado = AccesoDatos.LeerDecimal(f, "facturado");
                var anulado = AccesoDatos.LeerDecimal(f, "anulado");
                var facturas = AccesoDatos.LeerInt(f, "facturas");
                return RespuestaVentasIA.Ok(AccesoDatos.LeerString(f, "cajero") + " facturó " + facturas +
                    (facturas == 1 ? " venta" : " ventas") + " " + periodo + " por " + Pesos(facturado) + "." +
                    (anuladas == 0 ? "" : " De esas, " + anuladas + " se " + (anuladas == 1 ? "anuló" : "anularon") +
                        " con nota de crédito (" + Pesos(anulado) + "): neto " + Pesos(facturado - anulado) + "."));
            }

            var tabla = NuevaTabla("Cajero", "Facturas", "Facturado", "Anuladas", "Neto");
            foreach (DataRow f in filas)
                tabla.Rows.Add(AccesoDatos.LeerString(f, "cajero"), AccesoDatos.LeerInt(f, "facturas").ToString(),
                    Pesos(AccesoDatos.LeerDecimal(f, "facturado")), AccesoDatos.LeerInt(f, "anuladas").ToString(),
                    Pesos(AccesoDatos.LeerDecimal(f, "facturado") - AccesoDatos.LeerDecimal(f, "anulado")));

            var primero = filas[0];
            return RespuestaVentasIA.Ok("Quien más facturó " + periodo + " es " + AccesoDatos.LeerString(primero, "cajero") +
                ": neto " + Pesos(AccesoDatos.LeerDecimal(primero, "facturado") - AccesoDatos.LeerDecimal(primero, "anulado")) +
                " (sin las ventas anuladas). Por cajero:", tabla);
        }

        // Los mismos totales que sp_VentasPorPeriodo y Reportes/VentasPorPeriodo: las notas de crédito
        // restan en el período en que se emiten, y lo cobrado no cuenta las ventas anuladas.
        private static RespuestaVentasIA ResumenVentas(DateTime desde, DateTime hasta, string periodo)
        {
            var f = AccesoDatos.Consultar(
                @"SELECT COUNT(*) AS cantidad, ISNULL(SUM(v.total), 0) AS vendido,
                         ISNULL(SUM(CASE WHEN nc.idNotaCredito IS NULL THEN v.total - v.saldoPendiente ELSE 0 END), 0) AS cobrado,
                         ISNULL(SUM(v.saldoPendiente), 0) AS pendiente,
                         (SELECT COUNT(*) FROM NotaCredito
                          WHERE fecha >= @desde AND fecha < @hastaExclusiva) AS notas,
                         (SELECT ISNULL(SUM(total), 0) FROM NotaCredito
                          WHERE fecha >= @desde AND fecha < @hastaExclusiva) AS anulado
                  FROM ComprobanteVenta v
                  LEFT JOIN NotaCredito nc ON nc.idVenta = v.idVenta
                  WHERE v.fecha >= @desde AND v.fecha < @hastaExclusiva",
                AccesoDatos.Param("@desde", desde.Date),
                AccesoDatos.Param("@hastaExclusiva", hasta.Date.AddDays(1))).Rows[0];

            var cantidad = AccesoDatos.LeerInt(f, "cantidad");
            var notas = AccesoDatos.LeerInt(f, "notas");
            if (cantidad == 0 && notas == 0)
                return RespuestaVentasIA.Ok("No hay ventas " + periodo + ".");

            var vendido = AccesoDatos.LeerDecimal(f, "vendido");
            var anulado = AccesoDatos.LeerDecimal(f, "anulado");
            var tabla = NuevaTabla("Concepto", "Importe");
            tabla.Rows.Add("Ventas (" + cantidad + ")", Pesos(vendido));
            tabla.Rows.Add("Anulado con notas de crédito (" + notas + ")", Pesos(anulado));
            tabla.Rows.Add("Neto vendido", Pesos(vendido - anulado));
            tabla.Rows.Add("Cobrado", Pesos(AccesoDatos.LeerDecimal(f, "cobrado")));
            tabla.Rows.Add("Pendiente de cobro", Pesos(AccesoDatos.LeerDecimal(f, "pendiente")));

            return RespuestaVentasIA.Ok("Resumen de ventas " + periodo + ": " + cantidad +
                (cantidad == 1 ? " venta" : " ventas") + ", neto vendido " + Pesos(vendido - anulado) + ".", tabla);
        }

        /* --- Cuenta corriente ------------------------------------------------------ */

        // Sin filtro: los 5 que más deben y el total adeudado. Con filtro: lo que debe el cliente que
        // se llama así. El saldo es el del último movimiento de la cuenta (mismo criterio que
        // sp_SaldosCuentaCorriente): positivo debe, negativo tiene a favor.
        private static RespuestaVentasIA Deudores(string filtro)
        {
            if (filtro == null)
            {
                var deudores = AccesoDatos.Consultar(
                    @"SELECT c.denominacion AS cliente, u.saldo, u.fecha
                      FROM Cliente c
                      CROSS APPLY (SELECT TOP 1 saldo, fecha FROM CuentaCorrienteCliente
                                   WHERE idCliente = c.idCliente ORDER BY idMovimiento DESC) u
                      WHERE u.saldo > 0
                      ORDER BY u.saldo DESC, c.denominacion").Rows;

                if (deudores.Count == 0)
                    return RespuestaVentasIA.Ok("Ningún cliente debe plata en su cuenta corriente.");

                decimal total = 0;
                var tabla = NuevaTabla("Cliente", "Debe", "Último movimiento");
                foreach (DataRow f in deudores)
                {
                    total += AccesoDatos.LeerDecimal(f, "saldo");
                    if (tabla.Rows.Count < 5)
                        tabla.Rows.Add(AccesoDatos.LeerString(f, "cliente"), Pesos(AccesoDatos.LeerDecimal(f, "saldo")),
                            AccesoDatos.LeerFecha(f, "fecha").ToString("dd/MM/yyyy"));
                }

                var primero = deudores[0];
                return RespuestaVentasIA.Ok("El cliente que más debe es " + AccesoDatos.LeerString(primero, "cliente") +
                    ": " + Pesos(AccesoDatos.LeerDecimal(primero, "saldo")) + ". En total, " + deudores.Count +
                    (deudores.Count == 1 ? " cliente debe " : " clientes deben ") + Pesos(total) + "." +
                    (deudores.Count == 1 ? "" : " Los que más deben:"), tabla);
            }

            var parametros = new List<SqlParameter>();
            var clientes = AccesoDatos.Consultar(
                @"SELECT TOP 6 c.denominacion AS cliente, u.saldo, u.fecha
                  FROM Cliente c
                  OUTER APPLY (SELECT TOP 1 saldo, fecha FROM CuentaCorrienteCliente
                               WHERE idCliente = c.idCliente ORDER BY idMovimiento DESC) u
                  WHERE " + CondicionPalabras("c.denominacion", filtro, parametros) + @"
                  ORDER BY c.denominacion",
                parametros.ToArray()).Rows;

            if (clientes.Count == 0)
                return RespuestaVentasIA.Ok("No encontré ningún cliente que se llame «" + filtro + "».");

            if (clientes.Count == 1)
            {
                var c = clientes[0];
                var nombre = AccesoDatos.LeerString(c, "cliente");
                var fecha = AccesoDatos.LeerFechaNullable(c, "fecha");
                if (fecha == null)
                    return RespuestaVentasIA.Ok(nombre + " no tiene movimientos en su cuenta corriente.");
                var saldo = AccesoDatos.LeerDecimal(c, "saldo");
                var desdeCuando = " (último movimiento: " + fecha.Value.ToString("dd/MM/yyyy") + ").";
                if (saldo > 0) return RespuestaVentasIA.Ok(nombre + " debe " + Pesos(saldo) + desdeCuando);
                if (saldo < 0) return RespuestaVentasIA.Ok(nombre + " no debe nada: tiene " + Pesos(-saldo) + " a favor" + desdeCuando);
                return RespuestaVentasIA.Ok(nombre + " no debe nada" + desdeCuando);
            }

            var lista = NuevaTabla("Cliente", "Saldo", "Último movimiento");
            foreach (DataRow c in clientes)
            {
                var fecha = AccesoDatos.LeerFechaNullable(c, "fecha");
                var saldo = fecha == null ? 0m : AccesoDatos.LeerDecimal(c, "saldo");
                lista.Rows.Add(AccesoDatos.LeerString(c, "cliente"),
                    saldo < 0 ? Pesos(-saldo) + " a favor" : Pesos(saldo),
                    fecha == null ? "—" : fecha.Value.ToString("dd/MM/yyyy"));
            }
            return RespuestaVentasIA.Ok("Encontré " + clientes.Count + " clientes con «" + filtro + "»:", lista);
        }

        /* --- Stock ----------------------------------------------------------------- */

        // Mismo criterio que el reporte de stock bajo (ProductoDAL.ListarStockBajo): insumos activos
        // con stock actual menor que el mínimo. Primero el que más falta.
        private static RespuestaVentasIA StockBajo()
        {
            var filas = AccesoDatos.Consultar(
                @"SELECT p.nombre, i.stockActual, i.stockMinimo, i.unidadMedida
                  FROM Insumo i
                  JOIN Producto p ON p.idProducto = i.idInsumo
                  WHERE p.activo = 1 AND i.stockActual < i.stockMinimo
                  ORDER BY i.stockMinimo - i.stockActual DESC, p.nombre").Rows;

            if (filas.Count == 0)
                return RespuestaVentasIA.Ok("Ningún insumo está por debajo del stock mínimo.");

            var tabla = NuevaTabla("Insumo", "Stock", "Mínimo", "Faltan", "Unidad");
            foreach (DataRow f in filas)
            {
                var stock = AccesoDatos.LeerDecimal(f, "stockActual");
                var minimo = AccesoDatos.LeerDecimal(f, "stockMinimo");
                tabla.Rows.Add(AccesoDatos.LeerString(f, "nombre"), Cantidad(stock), Cantidad(minimo),
                    Cantidad(minimo - stock), AccesoDatos.LeerString(f, "unidadMedida"));
            }

            var primero = filas[0];
            return RespuestaVentasIA.Ok((filas.Count == 1 ? "Hay 1 insumo" : "Hay " + filas.Count + " insumos") +
                " por debajo del mínimo. El más urgente es " + AccesoDatos.LeerString(primero, "nombre") + ": quedan " +
                Cantidad(AccesoDatos.LeerDecimal(primero, "stockActual")) + " y el mínimo es " +
                Cantidad(AccesoDatos.LeerDecimal(primero, "stockMinimo")) + ".", tabla);
        }

        // Sin filtro: el stock de todos los insumos activos. Con filtro: los insumos cuyo nombre o
        // marca tienen esas palabras, con su último movimiento del kardex.
        private static RespuestaVentasIA StockInsumo(string filtro)
        {
            var parametros = new List<SqlParameter>();
            var condicion = filtro == null ? "" : " AND " + CondicionPalabras("p.nombre + ' ' + ISNULL(i.marca, '')", filtro, parametros);

            var filas = AccesoDatos.Consultar(
                @"SELECT p.nombre, i.stockActual, i.stockMinimo, i.unidadMedida,
                         m.fecha, m.tipoMovimiento, m.entrada, m.salida
                  FROM Insumo i
                  JOIN Producto p ON p.idProducto = i.idInsumo
                  OUTER APPLY (SELECT TOP 1 fecha, tipoMovimiento, entrada, salida FROM MovimientoStock
                               WHERE idInsumo = i.idInsumo ORDER BY idMovimiento DESC) m
                  WHERE p.activo = 1" + condicion + @"
                  ORDER BY p.nombre",
                parametros.ToArray()).Rows;

            if (filas.Count == 0)
                return RespuestaVentasIA.Ok(filtro == null
                    ? "No hay insumos cargados."
                    : "No encontré ningún insumo con «" + filtro + "». Los servicios no llevan stock.");

            if (filas.Count == 1 && filtro != null)
            {
                var f = filas[0];
                var stock = AccesoDatos.LeerDecimal(f, "stockActual");
                var minimo = AccesoDatos.LeerDecimal(f, "stockMinimo");
                var texto = new StringBuilder(AccesoDatos.LeerString(f, "nombre") + ": quedan " + Cantidad(stock) + " (" +
                    AccesoDatos.LeerString(f, "unidadMedida") + ") y el mínimo es " + Cantidad(minimo) +
                    (stock < minimo ? ", así que hay que reponer." : "."));
                var fecha = AccesoDatos.LeerFechaNullable(f, "fecha");
                if (fecha != null)
                {
                    var entrada = AccesoDatos.LeerDecimal(f, "entrada");
                    texto.Append(" Último movimiento: " + DescribirMovimiento(AccesoDatos.LeerString(f, "tipoMovimiento")) +
                        " el " + fecha.Value.ToString("dd/MM/yyyy") + " (" +
                        (entrada > 0 ? "+" + Cantidad(entrada) : "−" + Cantidad(AccesoDatos.LeerDecimal(f, "salida"))) + ").");
                }
                return RespuestaVentasIA.Ok(texto.ToString());
            }

            var tabla = NuevaTabla("Insumo", "Stock", "Mínimo", "Unidad", "Estado");
            var bajos = 0;
            foreach (DataRow f in filas)
            {
                var stock = AccesoDatos.LeerDecimal(f, "stockActual");
                var minimo = AccesoDatos.LeerDecimal(f, "stockMinimo");
                if (stock < minimo) bajos++;
                tabla.Rows.Add(AccesoDatos.LeerString(f, "nombre"), Cantidad(stock), Cantidad(minimo),
                    AccesoDatos.LeerString(f, "unidadMedida"),
                    stock <= 0 ? "Sin stock" : stock < minimo ? "Bajo el mínimo" : "OK");
            }

            return RespuestaVentasIA.Ok(filtro == null
                ? "Stock de los " + filas.Count + " insumos activos; " + bajos + (bajos == 1 ? " está" : " están") + " por debajo del mínimo:"
                : "Encontré " + filas.Count + " insumos con «" + filtro + "»:", tabla);
        }

        // Proyección para comprar: consumo de los últimos 30 días (lo que salió por órdenes de trabajo,
        // menos lo que volvió por cancelarlas o quitar la línea), cuántos días alcanza el stock a ese
        // ritmo y cuánto comprar para cubrir otros 30 días sin bajar del mínimo:
        //     comprar = consumo de 30 días + mínimo − stock (si da negativo, nada).
        private static RespuestaVentasIA Reposicion()
        {
            var filas = AccesoDatos.Consultar(
                @"SELECT p.nombre, i.stockActual, i.stockMinimo, i.unidadMedida, ISNULL(m.consumo, 0) AS consumo
                  FROM Insumo i
                  JOIN Producto p ON p.idProducto = i.idInsumo
                  OUTER APPLY (SELECT SUM(CASE ms.tipoMovimiento WHEN @orden THEN ms.salida
                                                                 WHEN @cancelacion THEN -ms.entrada
                                                                 ELSE 0 END) AS consumo
                               FROM MovimientoStock ms
                               WHERE ms.idInsumo = i.idInsumo AND ms.fecha >= @desde) m
                  WHERE p.activo = 1",
                AccesoDatos.Param("@orden", MovimientoStock.TipoOrden),
                AccesoDatos.Param("@cancelacion", MovimientoStock.TipoCancelacionOrden),
                AccesoDatos.Param("@desde", DateTime.Today.AddDays(-DiasConsumo))).Rows;

            var proyeccion = new List<Tuple<string, decimal, decimal, int?, decimal, string>>();
            foreach (DataRow f in filas)
            {
                var stock = AccesoDatos.LeerDecimal(f, "stockActual");
                var minimo = AccesoDatos.LeerDecimal(f, "stockMinimo");
                var consumo = Math.Max(0m, AccesoDatos.LeerDecimal(f, "consumo"));
                if (consumo == 0 && stock >= minimo) continue;

                int? dias = null;
                if (consumo > 0) dias = (int)Math.Floor(Math.Max(0m, stock) / (consumo / DiasConsumo));
                var comprar = Math.Max(0m, Math.Ceiling(consumo + minimo - stock));
                proyeccion.Add(Tuple.Create(AccesoDatos.LeerString(f, "nombre"), stock, consumo, dias, comprar,
                    AccesoDatos.LeerString(f, "unidadMedida")));
            }

            if (proyeccion.Count == 0)
                return RespuestaVentasIA.Ok("En los últimos " + DiasConsumo + " días no se consumió ningún insumo y ninguno " +
                    "está por debajo del mínimo: por ahora no hace falta comprar.");

            // Primero lo que se termina antes; lo que no tuvo consumo (pero está bajo el mínimo), al final.
            var ordenada = proyeccion.OrderBy(p => p.Item4 ?? int.MaxValue).ThenByDescending(p => p.Item5).ToList();
            var tabla = NuevaTabla("Insumo", "Stock", "Consumo 30 días", "Alcanza (días)", "Comprar", "Unidad");
            foreach (var p in ordenada)
                tabla.Rows.Add(p.Item1, Cantidad(p.Item2), Cantidad(p.Item3),
                    p.Item4 == null ? "—" : p.Item4.Value.ToString(), Cantidad(p.Item5), p.Item6);

            var primero = ordenada[0];
            var aComprar = ordenada.Count(p => p.Item5 > 0);
            return RespuestaVentasIA.Ok((primero.Item4 == null
                    ? "En los últimos " + DiasConsumo + " días no hubo consumo, pero hay insumos por debajo del mínimo."
                    : "Al ritmo de los últimos " + DiasConsumo + " días, lo primero que se termina es " + primero.Item1 +
                      ": alcanza para " + primero.Item4.Value + (primero.Item4.Value == 1 ? " día." : " días.")) +
                " Conviene comprar " + aComprar + (aComprar == 1 ? " insumo" : " insumos") +
                " (comprar = lo usado en 30 días + el mínimo − lo que hay):", tabla);
        }

        /* --- Auxiliares -------------------------------------------------------------- */

        // El filtro que trae la IA, sin comillas ni signos; null si vino vacío o "todos".
        private static string LimpiarFiltro(string filtro)
        {
            if (string.IsNullOrWhiteSpace(filtro)) return null;
            var limpio = filtro.Trim().Trim('"', '\'', '«', '»', '¿', '?', '¡', '!', '.', ',', ' ');
            if (limpio.Length > 60) limpio = limpio.Substring(0, 60).Trim();
            var comparable = SinTildes(limpio).ToLowerInvariant();
            string[] vacios = { "", "vacio", "ninguno", "ninguna", "null", "todos", "todas", "-", "n/a" };
            return vacios.Contains(comparable) ? null : limpio;
        }

        // "servicio" → S, "insumo" (o "producto", "repuesto") → I; otra cosa, los dos.
        private static string TipoItem(string filtro)
        {
            if (filtro == null) return null;
            var texto = SinTildes(filtro).ToLowerInvariant();
            if (texto.Contains("serv")) return DetalleComprobanteVenta.TipoServicio;
            if (texto.Contains("insum") || texto.Contains("product") || texto.Contains("repuest"))
                return DetalleComprobanteVenta.TipoInsumo;
            return null;
        }

        // "col LIKE @f0 AND col LIKE @f1...": cada palabra del filtro tiene que aparecer, sin distinguir
        // mayúsculas ni tildes. Las palabras van como parámetros (los comodines de LIKE que traigan se
        // escapan), así que el texto de la IA nunca forma parte del SQL. A las palabras largas se les
        // saca la "s" final para que el plural encuentre al singular ("baterías" → "Batería 12V").
        private static string CondicionPalabras(string columna, string filtro, List<SqlParameter> parametros)
        {
            var palabras = filtro.Split(new[] { ' ', ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length >= 2 && !PalabrasVacias.Contains(p.ToLowerInvariant()))
                .Select(p => p.Length > 4 && (p.EndsWith("s") || p.EndsWith("S")) ? p.Substring(0, p.Length - 1) : p)
                .Take(5).ToList();
            if (palabras.Count == 0) palabras.Add(filtro);

            var partes = new List<string>();
            foreach (var palabra in palabras)
            {
                var nombre = "@f" + parametros.Count;
                parametros.Add(AccesoDatos.Param(nombre, "%" + EscaparLike(palabra) + "%"));
                partes.Add("(" + columna + ") COLLATE Latin1_General_CI_AI LIKE " + nombre + " ESCAPE '\\'");
            }
            return "(" + string.Join(" AND ", partes) + ")";
        }

        private static string EscaparLike(string texto)
        {
            return texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
        }

        private static string SinTildes(string texto)
        {
            var sb = new StringBuilder();
            foreach (var c in texto.Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string DescribirMovimiento(string tipo)
        {
            switch (tipo)
            {
                case MovimientoStock.TipoCompra: return "compra";
                case MovimientoStock.TipoOrden: return "uso en una orden";
                case MovimientoStock.TipoCancelacionOrden: return "devolución de una orden";
                case MovimientoStock.TipoAjusteManual: return "ajuste manual";
                default: return tipo;
            }
        }

        // La tabla que se muestra debajo de la respuesta, ya con los textos formateados.
        private static DataTable NuevaTabla(params string[] columnas)
        {
            var tabla = new DataTable();
            foreach (var c in columnas) tabla.Columns.Add(c, typeof(string));
            return tabla;
        }

        private static string Pesos(decimal importe)
        {
            return "$ " + importe.ToString("N2");
        }

        private static string Cantidad(decimal cantidad)
        {
            return cantidad.ToString("0.##");
        }
    }
}
