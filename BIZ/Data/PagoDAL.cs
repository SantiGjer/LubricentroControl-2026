using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class PagoDAL
    {
        private const string SelectBase = @"
            SELECT p.idPago, p.tipo, p.idCliente, cl.nombre + ' ' + cl.apellido AS nombreCliente,
                   p.idProveedor, pr.razonSocial, p.idVenta, p.idCompra, p.idUsuario,
                   p.fecha, p.medioPago, p.monto, p.observaciones
            FROM Pago p
            LEFT JOIN Cliente cl ON cl.idCliente = p.idCliente
            LEFT JOIN Proveedor pr ON pr.idProveedor = p.idProveedor";

        private static Pago Mapear(DataRow fila)
        {
            return new Pago
            {
                IdPago = AccesoDatos.LeerInt(fila, "idPago"),
                Tipo = AccesoDatos.LeerString(fila, "tipo"),
                IdCliente = AccesoDatos.LeerIntNullable(fila, "idCliente"),
                NombreCliente = AccesoDatos.LeerString(fila, "nombreCliente"),
                IdProveedor = AccesoDatos.LeerIntNullable(fila, "idProveedor"),
                RazonSocial = AccesoDatos.LeerString(fila, "razonSocial"),
                IdVenta = AccesoDatos.LeerIntNullable(fila, "idVenta"),
                IdCompra = AccesoDatos.LeerIntNullable(fila, "idCompra"),
                IdUsuario = AccesoDatos.LeerInt(fila, "idUsuario"),
                Fecha = AccesoDatos.LeerFecha(fila, "fecha"),
                MedioPago = AccesoDatos.LeerString(fila, "medioPago"),
                Monto = AccesoDatos.LeerDecimal(fila, "monto"),
                Observaciones = AccesoDatos.LeerString(fila, "observaciones")
            };
        }

        public static List<Pago> Listar()
        {
            var lista = new List<Pago>();
            foreach (DataRow fila in AccesoDatos.Consultar(SelectBase + " ORDER BY p.fecha DESC").Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static Pago ObtenerPorId(int idPago)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE p.idPago = @idPago",
                AccesoDatos.Param("@idPago", idPago));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        // Nombre/apellido del cliente o razón social del proveedor.
        public static List<Pago> Buscar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Listar();

            const string sql = SelectBase + @"
                WHERE cl.nombre LIKE @texto OR cl.apellido LIKE @texto OR pr.razonSocial LIKE @texto
                ORDER BY p.fecha DESC";

            var lista = new List<Pago>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql, AccesoDatos.Param("@texto", "%" + texto.Trim() + "%")).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static List<Pago> ListarPorVenta(int idVenta)
        {
            var lista = new List<Pago>();
            foreach (DataRow fila in AccesoDatos.Consultar(
                SelectBase + " WHERE p.idVenta = @idVenta ORDER BY p.fecha",
                AccesoDatos.Param("@idVenta", idVenta)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static List<Pago> ListarPorCompra(int idCompra)
        {
            var lista = new List<Pago>();
            foreach (DataRow fila in AccesoDatos.Consultar(
                SelectBase + " WHERE p.idCompra = @idCompra ORDER BY p.fecha",
                AccesoDatos.Param("@idCompra", idCompra)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Registra el pago y, en el mismo batch atómico, descuenta el saldoPendiente del
        // comprobante puntual (si vino uno) y deja el movimiento en la cuenta corriente que
        // corresponda (Cliente o Proveedor) — mismo mecanismo que ComprobanteCompraDAL.Crear/
        // ComprobanteVentaDAL.GenerarDesdeOrden. Un pago "a cuenta general" (sin IdVenta/IdCompra)
        // solo hace las últimas dos escrituras.
        public static ResultadoOperacion Registrar(Pago pago)
        {
            var validacion = pago.Validar();
            if (!validacion.Exito) return validacion;

            string descripcionCC;

            if (pago.Tipo == Pago.TipoCliente)
            {
                pago.IdProveedor = null;
                var cliente = ClienteDAL.ObtenerPorId(pago.IdCliente.Value);
                if (cliente == null) return ResultadoOperacion.Error("El cliente no existe.");

                if (pago.IdVenta.HasValue)
                {
                    var venta = ComprobanteVentaDAL.ObtenerPorId(pago.IdVenta.Value);
                    if (venta == null) return ResultadoOperacion.Error("La venta no existe.");
                    if (venta.IdCliente != pago.IdCliente.Value)
                        return ResultadoOperacion.Error("La venta seleccionada no pertenece a ese cliente.");
                    if (pago.Monto > venta.SaldoPendiente)
                        return ResultadoOperacion.Error("El monto supera el saldo pendiente de la venta.");
                    descripcionCC = "Pago de venta " + venta.NumeroComprobante;
                }
                else
                {
                    pago.IdCompra = null;
                    descripcionCC = "Pago a cuenta";
                }
            }
            else
            {
                pago.IdCliente = null;
                var proveedor = ProveedorDAL.ObtenerPorId(pago.IdProveedor.Value);
                if (proveedor == null) return ResultadoOperacion.Error("El proveedor no existe.");

                if (pago.IdCompra.HasValue)
                {
                    var compra = ComprobanteCompraDAL.ObtenerPorId(pago.IdCompra.Value);
                    if (compra == null) return ResultadoOperacion.Error("La compra no existe.");
                    if (compra.IdProveedor != pago.IdProveedor.Value)
                        return ResultadoOperacion.Error("La compra seleccionada no pertenece a ese proveedor.");
                    if (pago.Monto > compra.SaldoPendiente)
                        return ResultadoOperacion.Error("El monto supera el saldo pendiente de la compra.");
                    descripcionCC = "Pago de compra " + compra.NumeroComprobante;
                }
                else
                {
                    pago.IdVenta = null;
                    descripcionCC = "Pago a cuenta";
                }
            }

            var sql = new StringBuilder();
            var parametros = new List<SqlParameter>();

            sql.Append(@"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                INSERT INTO Pago (tipo, idCliente, idProveedor, idVenta, idCompra, idUsuario, medioPago, monto, observaciones)
                VALUES (@tipo, @idCliente, @idProveedor, @idVenta, @idCompra, @idUsuario, @medioPago, @monto, @observaciones);

                DECLARE @idPago INT = CAST(SCOPE_IDENTITY() AS INT);
            ");

            parametros.Add(AccesoDatos.Param("@tipo", pago.Tipo));
            parametros.Add(AccesoDatos.Param("@idCliente", pago.IdCliente));
            parametros.Add(AccesoDatos.Param("@idProveedor", pago.IdProveedor));
            parametros.Add(AccesoDatos.Param("@idVenta", pago.IdVenta));
            parametros.Add(AccesoDatos.Param("@idCompra", pago.IdCompra));
            parametros.Add(AccesoDatos.Param("@idUsuario", pago.IdUsuario));
            parametros.Add(AccesoDatos.Param("@medioPago", pago.MedioPago));
            parametros.Add(AccesoDatos.Param("@monto", pago.Monto));
            parametros.Add(AccesoDatos.Param("@observaciones", pago.Observaciones));

            if (pago.IdVenta.HasValue)
            {
                sql.Append("UPDATE ComprobanteVenta SET saldoPendiente = saldoPendiente - @monto WHERE idVenta = @idVenta;");
            }
            else if (pago.IdCompra.HasValue)
            {
                sql.Append("UPDATE ComprobanteCompra SET saldoPendiente = saldoPendiente - @monto WHERE idCompra = @idCompra;");
            }

            if (pago.Tipo == Pago.TipoCliente)
            {
                sql.Append(@"
                    INSERT INTO CuentaCorrienteCliente
                        (idCliente, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
                    VALUES (
                        @idCliente, @tipoMovimientoCC, NULL, @idPago, 0, @monto,
                        ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente
                                WHERE idCliente = @idCliente ORDER BY idMovimiento DESC), 0) - @monto,
                        @descripcionCC, NULL);
                ");
                parametros.Add(AccesoDatos.Param("@tipoMovimientoCC", CuentaCorrienteCliente.TipoPago));
            }
            else
            {
                sql.Append(@"
                    INSERT INTO CuentaCorrienteProveedor
                        (idProveedor, tipoMovimiento, idCompra, idPago, debe, haber, saldo, descripcion, idUsuario)
                    VALUES (
                        @idProveedor, @tipoMovimientoCC, NULL, @idPago, 0, @monto,
                        ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteProveedor
                                WHERE idProveedor = @idProveedor ORDER BY idMovimiento DESC), 0) - @monto,
                        @descripcionCC, NULL);
                ");
                parametros.Add(AccesoDatos.Param("@tipoMovimientoCC", CuentaCorrienteProveedor.TipoPago));
            }
            parametros.Add(AccesoDatos.Param("@descripcionCC", descripcionCC));

            sql.Append(@"
                COMMIT TRANSACTION;
                SELECT @idPago;
            ");

            var id = AccesoDatos.Escalar(sql.ToString(), parametros.ToArray());
            pago.IdPago = System.Convert.ToInt32(id);

            return ResultadoOperacion.Ok("Pago registrado.");
        }
    }
}
