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
            SELECT v.idVenta, v.idOrden, v.idCliente, c.nombre + ' ' + c.apellido AS nombreCliente,
                   ve.patente, v.numeroComprobante, v.fecha, v.subtotal, v.impuestos, v.total, v.saldoPendiente
            FROM ComprobanteVenta v
            INNER JOIN Cliente c ON c.idCliente = v.idCliente
            INNER JOIN OrdenDeTrabajo o ON o.idOrden = v.idOrden
            INNER JOIN Vehiculo ve ON ve.idVehiculo = o.idVehiculo";

        private static ComprobanteVenta Mapear(DataRow fila)
        {
            return new ComprobanteVenta
            {
                IdVenta = AccesoDatos.LeerInt(fila, "idVenta"),
                IdOrden = AccesoDatos.LeerInt(fila, "idOrden"),
                IdCliente = AccesoDatos.LeerInt(fila, "idCliente"),
                NombreCliente = AccesoDatos.LeerString(fila, "nombreCliente"),
                Patente = AccesoDatos.LeerString(fila, "patente"),
                NumeroComprobante = AccesoDatos.LeerString(fila, "numeroComprobante"),
                Fecha = AccesoDatos.LeerFecha(fila, "fecha"),
                Subtotal = AccesoDatos.LeerDecimal(fila, "subtotal"),
                Impuestos = AccesoDatos.LeerDecimal(fila, "impuestos"),
                Total = AccesoDatos.LeerDecimal(fila, "total"),
                SaldoPendiente = AccesoDatos.LeerDecimal(fila, "saldoPendiente")
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

        // Ventas de un cliente con saldo pendiente — para el desplegable "comprobante a pagar"
        // de Pagos.aspx.
        public static List<ComprobanteVenta> ListarPendientesPorCliente(int idCliente)
        {
            var lista = new List<ComprobanteVenta>();
            foreach (DataRow fila in AccesoDatos.Consultar(
                SelectBase + " WHERE v.idCliente = @idCliente AND v.saldoPendiente > 0 ORDER BY v.fecha",
                AccesoDatos.Param("@idCliente", idCliente)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Cliente, patente o número de comprobante.
        public static List<ComprobanteVenta> Buscar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Listar();

            const string sql = SelectBase + @"
                WHERE c.nombre LIKE @texto OR c.apellido LIKE @texto
                   OR ve.patente LIKE @texto OR v.numeroComprobante LIKE @texto
                ORDER BY v.fecha DESC";

            var lista = new List<ComprobanteVenta>();
            var comodin = "%" + texto.Trim() + "%";
            foreach (DataRow fila in AccesoDatos.Consultar(sql, AccesoDatos.Param("@texto", comodin)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Se llama desde OrdenDeTrabajoDAL.Cerrar, nunca directo desde la pantalla de Ventas —
        // el comprobante no se carga a mano (Requerimientos §6.6). Copia las líneas ya cargadas
        // en la orden (servicios e insumos, con el precio que ya tenían aplicado, sin volver a
        // mirar el catálogo) y genera el movimiento de cuenta corriente del cliente, todo en un
        // solo batch atómico — mismo mecanismo que ComprobanteCompraDAL.Crear.
        public static ResultadoOperacion GenerarDesdeOrden(int idOrden)
        {
            if (ObtenerPorOrden(idOrden) != null)
                return ResultadoOperacion.Error("Esta orden ya tiene una venta generada.");

            var orden = OrdenDeTrabajoDAL.ObtenerPorId(idOrden);
            if (orden == null)
                return ResultadoOperacion.Error("La orden no existe.");

            var servicios = DetalleOrdenServicioDAL.ListarPorOrden(idOrden);
            var insumos = DetalleOrdenInsumoDAL.ListarPorOrden(idOrden);

            decimal subtotal = 0;
            foreach (var linea in servicios) subtotal += linea.Cantidad * linea.PrecioAplicado;
            foreach (var linea in insumos) subtotal += linea.Cantidad * linea.PrecioUnitario;

            const decimal impuestos = 0; // Sin tasa definida en los Requerimientos — ver §9.5.
            var total = subtotal + impuestos;

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
            parametros.Add(AccesoDatos.Param("@saldoPendiente", total));

            var indice = 0;
            foreach (var linea in servicios)
            {
                var suf = indice.ToString();
                sql.Append(@"
                    INSERT INTO DetalleComprobanteVenta
                        (idVenta, tipoItem, idServicio, idInsumo, descripcion, cantidad, precioUnitario, subtotal)
                    VALUES (@idVenta, 'S', @idServicio" + suf + @", NULL, @descripcion" + suf + @",
                            @cantidad" + suf + @", @precio" + suf + @", @detSubtotal" + suf + @");
                ");
                parametros.Add(AccesoDatos.Param("@idServicio" + suf, linea.IdServicio));
                parametros.Add(AccesoDatos.Param("@descripcion" + suf, linea.NombreServicio));
                parametros.Add(AccesoDatos.Param("@cantidad" + suf, linea.Cantidad));
                parametros.Add(AccesoDatos.Param("@precio" + suf, linea.PrecioAplicado));
                parametros.Add(AccesoDatos.Param("@detSubtotal" + suf, linea.Cantidad * linea.PrecioAplicado));
                indice++;
            }

            foreach (var linea in insumos)
            {
                var suf = indice.ToString();
                sql.Append(@"
                    INSERT INTO DetalleComprobanteVenta
                        (idVenta, tipoItem, idServicio, idInsumo, descripcion, cantidad, precioUnitario, subtotal)
                    VALUES (@idVenta, 'I', NULL, @idInsumo" + suf + @", @descripcion" + suf + @",
                            @cantidad" + suf + @", @precio" + suf + @", @detSubtotal" + suf + @");
                ");
                parametros.Add(AccesoDatos.Param("@idInsumo" + suf, linea.IdInsumo));
                parametros.Add(AccesoDatos.Param("@descripcion" + suf, linea.NombreInsumo));
                parametros.Add(AccesoDatos.Param("@cantidad" + suf, linea.Cantidad));
                parametros.Add(AccesoDatos.Param("@precio" + suf, linea.PrecioUnitario));
                parametros.Add(AccesoDatos.Param("@detSubtotal" + suf, linea.Cantidad * linea.PrecioUnitario));
                indice++;
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

            return ResultadoOperacion.Ok("Venta generada.");
        }
    }
}
