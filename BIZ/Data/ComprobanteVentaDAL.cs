using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class ComprobanteVentaDAL
    {
        private const string SelectBase = @"
            SELECT v.idVenta, v.idOrden, v.idCliente, c.denominacion AS nombreCliente,
                   c.tipoDocumento, c.numeroDocumento,
                   ve.patente, v.numeroComprobante, v.fecha, v.subtotal, v.impuestos, v.total, v.saldoPendiente,
                   f.tipo AS tipoFactura, f.puntoVenta, f.numero AS numeroFactura
            FROM ComprobanteVenta v
            INNER JOIN Cliente c ON c.idCliente = v.idCliente
            INNER JOIN OrdenDeTrabajo o ON o.idOrden = v.idOrden
            INNER JOIN Vehiculo ve ON ve.idVehiculo = o.idVehiculo
            LEFT JOIN Factura f ON f.idVenta = v.idVenta";

        private static ComprobanteVenta Mapear(DataRow fila)
        {
            var tipoFactura = AccesoDatos.LeerString(fila, "tipoFactura");

            return new ComprobanteVenta
            {
                IdVenta = AccesoDatos.LeerInt(fila, "idVenta"),
                IdOrden = AccesoDatos.LeerInt(fila, "idOrden"),
                IdCliente = AccesoDatos.LeerInt(fila, "idCliente"),
                NombreCliente = AccesoDatos.LeerString(fila, "nombreCliente"),
                TipoDocumentoCliente = AccesoDatos.LeerString(fila, "tipoDocumento"),
                NumeroDocumentoCliente = AccesoDatos.LeerString(fila, "numeroDocumento"),
                Patente = AccesoDatos.LeerString(fila, "patente"),
                NumeroComprobante = AccesoDatos.LeerString(fila, "numeroComprobante"),
                Fecha = AccesoDatos.LeerFecha(fila, "fecha"),
                Subtotal = AccesoDatos.LeerDecimal(fila, "subtotal"),
                Impuestos = AccesoDatos.LeerDecimal(fila, "impuestos"),
                Total = AccesoDatos.LeerDecimal(fila, "total"),
                SaldoPendiente = AccesoDatos.LeerDecimal(fila, "saldoPendiente"),
                NumeroFactura = tipoFactura == null
                    ? null
                    : Factura.FormatearNumero(tipoFactura, AccesoDatos.LeerInt(fila, "puntoVenta"),
                                              AccesoDatos.LeerInt(fila, "numeroFactura"))
            };
        }

        public static List<ComprobanteVenta> Listar()
        {
            var lista = new List<ComprobanteVenta>();
            foreach (DataRow fila in AccesoDatos.Consultar(SelectBase + " ORDER BY v.fecha DESC").Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static ComprobanteVenta ObtenerPorId(int idVenta)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE v.idVenta = @idVenta",
                AccesoDatos.Param("@idVenta", idVenta));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public static ComprobanteVenta ObtenerPorOrden(int idOrden)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE v.idOrden = @idOrden",
                AccesoDatos.Param("@idOrden", idOrden));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        // Ventas de un cliente con saldo pendiente, la más vieja primero — PagoDAL.Registrar las
        // cancela en ese orden.
        public static List<ComprobanteVenta> ListarPendientesPorCliente(int idCliente)
        {
            var lista = new List<ComprobanteVenta>();
            foreach (DataRow fila in AccesoDatos.Consultar(
                SelectBase + " WHERE v.idCliente = @idCliente AND v.saldoPendiente > 0 ORDER BY v.fecha, v.idVenta",
                AccesoDatos.Param("@idCliente", idCliente)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Ventas de un período, para Reportes/VentasPorPeriodo.aspx. "hasta" es inclusive del
        // día completo aunque "fecha" tenga hora (DEFAULT GETDATE()): se compara contra el día
        // siguiente en vez de la fecha exacta, para no perder ventas cargadas después de la
        // medianoche de ese día.
        public static List<ComprobanteVenta> ListarPorPeriodo(DateTime desde, DateTime hasta)
        {
            var lista = new List<ComprobanteVenta>();
            foreach (DataRow fila in AccesoDatos.Consultar(
                SelectBase + " WHERE v.fecha >= @desde AND v.fecha < @hastaExclusiva ORDER BY v.fecha",
                AccesoDatos.Param("@desde", desde.Date),
                AccesoDatos.Param("@hastaExclusiva", hasta.Date.AddDays(1))).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Una línea de la venta a generar, armada desde un servicio o un insumo de la orden.
        private class LineaVenta
        {
            public string TipoItem;
            public int IdProducto;
            public string Descripcion;
            public decimal Cantidad;
            public decimal Precio;
            public string TipoIva;
            public decimal AlicuotaIva;

            public decimal Subtotal
            {
                get { return Cantidad * Precio; }
            }

            public decimal ImporteIva
            {
                get { return Iva.Contenido(Subtotal, TipoIva, AlicuotaIva); }
            }
        }

        // Se llama desde OrdenDeTrabajoDAL.Cerrar, nunca directo desde la pantalla de Ventas —
        // el comprobante no se carga a mano (Requerimientos §6.6). Copia las líneas ya cargadas
        // en la orden (servicios e insumos, con el precio que ya tenían aplicado, sin volver a
        // mirar el catálogo) y genera el movimiento de cuenta corriente del cliente, todo en un
        // solo batch atómico — mismo mecanismo que ComprobanteCompraDAL.Crear. Cada línea guarda el
        // IVA del producto de ese momento y el IVA que contiene su precio final (§9.10): la venta
        // suma el neto en subtotal y el IVA en impuestos, y el total no cambia.
        public static ResultadoOperacion GenerarDesdeOrden(int idOrden)
        {
            if (ObtenerPorOrden(idOrden) != null)
                return ResultadoOperacion.Error("Esta orden ya tiene una venta generada.");

            var orden = OrdenDeTrabajoDAL.ObtenerPorId(idOrden);
            if (orden == null)
                return ResultadoOperacion.Error("La orden no existe.");

            var servicios = DetalleOrdenServicioDAL.ListarPorOrden(idOrden);
            var insumos = DetalleOrdenInsumoDAL.ListarPorOrden(idOrden);

            // Una orden sin ninguna línea generaría una venta en $0 que no se puede borrar ni
            // editar: se rechaza acá, que es donde se leen las líneas, para cubrir cualquier
            // camino que llegue a generar la venta.
            if (servicios.Count == 0 && insumos.Count == 0)
                return ResultadoOperacion.Error(
                    "La orden no tiene servicios ni insumos cargados, así que no se puede cerrar. " +
                    "Cargá al menos uno, o cancelá la orden si no corresponde cobrar nada.");

            var lineas = new List<LineaVenta>();
            foreach (var linea in servicios)
                lineas.Add(new LineaVenta
                {
                    TipoItem = DetalleComprobanteVenta.TipoServicio, IdProducto = linea.IdServicio,
                    Descripcion = linea.NombreServicio, Cantidad = linea.Cantidad, Precio = linea.PrecioAplicado,
                    TipoIva = linea.TipoIva, AlicuotaIva = linea.AlicuotaIva
                });
            foreach (var linea in insumos)
                lineas.Add(new LineaVenta
                {
                    TipoItem = DetalleComprobanteVenta.TipoInsumo, IdProducto = linea.IdInsumo,
                    Descripcion = linea.NombreInsumo, Cantidad = linea.Cantidad, Precio = linea.PrecioUnitario,
                    TipoIva = linea.TipoIva, AlicuotaIva = linea.AlicuotaIva
                });

            decimal total = 0, impuestos = 0;
            foreach (var linea in lineas)
            {
                total += linea.Subtotal;
                impuestos += linea.ImporteIva;
            }
            var subtotal = total - impuestos;

            // Si el cliente tiene saldo a favor (cuenta corriente negativa, por un pago de más),
            // se aplica a esta venta: nace con saldoPendiente = total - crédito aplicado. No hace
            // falta ningún movimiento extra: el débito de la venta ya netea el saldo a favor.
            var saldoPrevio = CuentaCorrienteClienteDAL.ObtenerSaldoActual(orden.IdCliente);
            var creditoAplicado = saldoPrevio < 0 ? System.Math.Min(-saldoPrevio, total) : 0;
            var saldoPendiente = total - creditoAplicado;

            var sql = new StringBuilder();
            var parametros = new List<SqlParameter>();

            sql.Append(@"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                INSERT INTO ComprobanteVenta
                    (idOrden, idCliente, numeroComprobante, subtotal, impuestos, total, saldoPendiente)
                VALUES
                    (@idOrden, @idCliente, '(pendiente)', @subtotal, @impuestos, @total, @saldoPendiente);

                DECLARE @idVenta INT = CAST(SCOPE_IDENTITY() AS INT);
            ");

            parametros.Add(AccesoDatos.Param("@idOrden", idOrden));
            parametros.Add(AccesoDatos.Param("@idCliente", orden.IdCliente));
            parametros.Add(AccesoDatos.Param("@subtotal", subtotal));
            parametros.Add(AccesoDatos.Param("@impuestos", impuestos));
            parametros.Add(AccesoDatos.Param("@total", total));
            parametros.Add(AccesoDatos.Param("@saldoPendiente", saldoPendiente));

            for (var i = 0; i < lineas.Count; i++)
            {
                var linea = lineas[i];
                var suf = i.ToString();
                var esServicio = linea.TipoItem == DetalleComprobanteVenta.TipoServicio;

                sql.Append(@"
                    INSERT INTO DetalleComprobanteVenta
                        (idVenta, tipoItem, idServicio, idInsumo, descripcion, cantidad, precioUnitario, subtotal,
                         tipoIva, alicuotaIva, importeIva)
                    VALUES (@idVenta, @tipoItem" + suf + @", @idServicio" + suf + @", @idInsumo" + suf + @",
                            @descripcion" + suf + @", @cantidad" + suf + @", @precio" + suf + @", @detSubtotal" + suf + @",
                            @tipoIva" + suf + @", @alicuotaIva" + suf + @", @importeIva" + suf + @");
                ");
                parametros.Add(AccesoDatos.Param("@tipoItem" + suf, linea.TipoItem));
                parametros.Add(AccesoDatos.Param("@idServicio" + suf, esServicio ? (int?)linea.IdProducto : null));
                parametros.Add(AccesoDatos.Param("@idInsumo" + suf, esServicio ? null : (int?)linea.IdProducto));
                parametros.Add(AccesoDatos.Param("@descripcion" + suf, linea.Descripcion));
                parametros.Add(AccesoDatos.Param("@cantidad" + suf, linea.Cantidad));
                parametros.Add(AccesoDatos.Param("@precio" + suf, linea.Precio));
                parametros.Add(AccesoDatos.Param("@detSubtotal" + suf, linea.Subtotal));
                parametros.Add(AccesoDatos.Param("@tipoIva" + suf, linea.TipoIva));
                parametros.Add(AccesoDatos.Param("@alicuotaIva" + suf, linea.AlicuotaIva));
                parametros.Add(AccesoDatos.Param("@importeIva" + suf, linea.ImporteIva));
            }

            sql.Append(@"
                INSERT INTO CuentaCorrienteCliente
                    (idCliente, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
                VALUES (
                    @idCliente, @tipoMovimientoCC, @idVenta, NULL, @total, 0,
                    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente
                            WHERE idCliente = @idCliente ORDER BY idMovimiento DESC), 0) + @total,
                    'Venta #' + CAST(@idVenta AS VARCHAR(10)), NULL);

                UPDATE ComprobanteVenta
                SET numeroComprobante = 'V-' + RIGHT('000000' + CAST(@idVenta AS VARCHAR(10)), 6)
                WHERE idVenta = @idVenta;

                COMMIT TRANSACTION;

                SELECT @idVenta;
            ");
            parametros.Add(AccesoDatos.Param("@tipoMovimientoCC", CuentaCorrienteCliente.TipoVenta));

            AccesoDatos.Escalar(sql.ToString(), parametros.ToArray());

            return ResultadoOperacion.Ok("Se generó la venta correspondiente." +
                (creditoAplicado > 0
                    ? " Se aplicaron " + creditoAplicado.ToString("N2") + " de saldo a favor del cliente."
                    : ""));
        }
    }
}
