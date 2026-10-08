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
            SELECT p.idPago, p.tipo, p.idCliente, cl.denominacion AS nombreCliente,
                   p.idProveedor, pr.razonSocial, p.idVenta, p.idCompra, p.idUsuario,
                   u.nombre + ' ' + u.apellido AS nombreUsuario,
                   ISNULL(v.numeroComprobante, c.numeroComprobante) AS numeroComprobante,
                   p.fecha, p.medioPago, p.monto, p.observaciones
            FROM Pago p
            INNER JOIN Usuario u ON u.idUsuario = p.idUsuario
            LEFT JOIN Cliente cl ON cl.idCliente = p.idCliente
            LEFT JOIN Proveedor pr ON pr.idProveedor = p.idProveedor
            LEFT JOIN ComprobanteVenta v ON v.idVenta = p.idVenta
            LEFT JOIN ComprobanteCompra c ON c.idCompra = p.idCompra";

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
                NombreUsuario = AccesoDatos.LeerString(fila, "nombreUsuario"),
                NumeroComprobante = AccesoDatos.LeerString(fila, "numeroComprobante"),
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

        // Una fila de reparto de un pago: el comprobante al que se imputa (null = "a cuenta
        // general", el sobrante) y cuánto se le aplica.
        private class Imputacion
        {
            public int? IdComprobante;
            public string Numero;
            public decimal Monto;
        }

        // Registra el pago repartiéndolo solo: primero cancela las deudas del titular, de la más
        // vieja a la más nueva (saldoPendiente > 0), y lo que sobre queda "a cuenta general", o
        // sea a favor en la cuenta corriente. No se elige comprobante: siempre se cancela primero
        // la deuda. Como Pago tiene un solo idVenta/idCompra, un pago que toca varios comprobantes
        // se guarda como una fila de Pago por comprobante (más una sin comprobante para el
        // sobrante), cada una con su movimiento de cuenta corriente — todo en un solo batch
        // atómico, mismo mecanismo que ComprobanteCompraDAL.Crear/ComprobanteVentaDAL.GenerarDesdeOrden.
        public static ResultadoOperacion Registrar(Pago pago)
        {
            var validacion = pago.Validar();
            if (!validacion.Exito) return validacion;

            var esCliente = pago.Tipo == Pago.TipoCliente;
            var imputaciones = new List<Imputacion>();
            var restante = pago.Monto;

            if (esCliente)
            {
                pago.IdProveedor = null;
                var cliente = ClienteDAL.ObtenerPorId(pago.IdCliente.Value);
                if (cliente == null) return ResultadoOperacion.Error("El cliente no existe.");

                foreach (var venta in ComprobanteVentaDAL.ListarPendientesPorCliente(pago.IdCliente.Value))
                {
                    if (restante <= 0) break;
                    var aplicado = System.Math.Min(restante, venta.SaldoPendiente);
                    imputaciones.Add(new Imputacion { IdComprobante = venta.IdVenta, Numero = venta.NumeroComprobante, Monto = aplicado });
                    restante -= aplicado;
                }
            }
            else
            {
                pago.IdCliente = null;
                var proveedor = ProveedorDAL.ObtenerPorId(pago.IdProveedor.Value);
                if (proveedor == null) return ResultadoOperacion.Error("El proveedor no existe.");

                foreach (var compra in ComprobanteCompraDAL.ListarPendientesPorProveedor(pago.IdProveedor.Value))
                {
                    if (restante <= 0) break;
                    var aplicado = System.Math.Min(restante, compra.SaldoPendiente);
                    imputaciones.Add(new Imputacion { IdComprobante = compra.IdCompra, Numero = compra.NumeroComprobante, Monto = aplicado });
                    restante -= aplicado;
                }
            }

            if (restante > 0)
                imputaciones.Add(new Imputacion { IdComprobante = null, Numero = null, Monto = restante });

            var sql = new StringBuilder();
            var parametros = new List<SqlParameter>();

            sql.Append(@"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                DECLARE @idPago INT;
                DECLARE @primerIdPago INT = NULL;
            ");

            parametros.Add(AccesoDatos.Param("@tipo", pago.Tipo));
            parametros.Add(AccesoDatos.Param("@idCliente", pago.IdCliente));
            parametros.Add(AccesoDatos.Param("@idProveedor", pago.IdProveedor));
            parametros.Add(AccesoDatos.Param("@idUsuario", pago.IdUsuario));
            parametros.Add(AccesoDatos.Param("@medioPago", pago.MedioPago));
            parametros.Add(AccesoDatos.Param("@observaciones", pago.Observaciones));
            parametros.Add(AccesoDatos.Param("@tipoMovimientoCC",
                esCliente ? CuentaCorrienteCliente.TipoPago : CuentaCorrienteProveedor.TipoPago));

            for (var i = 0; i < imputaciones.Count; i++)
            {
                var imp = imputaciones[i];
                var suf = i.ToString();
                var descripcion = imp.IdComprobante.HasValue
                    ? (esCliente ? "Pago de venta " : "Pago de compra ") + imp.Numero
                    : "Pago a cuenta";

                sql.Append(@"
                    INSERT INTO Pago (tipo, idCliente, idProveedor, idVenta, idCompra, idUsuario, medioPago, monto, observaciones)
                    VALUES (@tipo, @idCliente, @idProveedor, @idVenta" + suf + @", @idCompra" + suf + @", @idUsuario, @medioPago, @monto" + suf + @", @observaciones);

                    SET @idPago = CAST(SCOPE_IDENTITY() AS INT);
                    IF @primerIdPago IS NULL SET @primerIdPago = @idPago;
                ");

                parametros.Add(AccesoDatos.Param("@idVenta" + suf, esCliente ? imp.IdComprobante : null));
                parametros.Add(AccesoDatos.Param("@idCompra" + suf, !esCliente ? imp.IdComprobante : null));
                parametros.Add(AccesoDatos.Param("@monto" + suf, imp.Monto));
                parametros.Add(AccesoDatos.Param("@descripcionCC" + suf, descripcion));

                if (imp.IdComprobante.HasValue)
                {
                    sql.Append(esCliente
                        ? "UPDATE ComprobanteVenta SET saldoPendiente = saldoPendiente - @monto" + suf + " WHERE idVenta = @idVenta" + suf + ";"
                        : "UPDATE ComprobanteCompra SET saldoPendiente = saldoPendiente - @monto" + suf + " WHERE idCompra = @idCompra" + suf + ";");
                }

                if (esCliente)
                {
                    sql.Append(@"
                        INSERT INTO CuentaCorrienteCliente
                            (idCliente, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
                        VALUES (
                            @idCliente, @tipoMovimientoCC, NULL, @idPago, 0, @monto" + suf + @",
                            ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente
                                    WHERE idCliente = @idCliente ORDER BY idMovimiento DESC), 0) - @monto" + suf + @",
                            @descripcionCC" + suf + @", NULL);
                    ");
                }
                else
                {
                    sql.Append(@"
                        INSERT INTO CuentaCorrienteProveedor
                            (idProveedor, tipoMovimiento, idCompra, idPago, debe, haber, saldo, descripcion, idUsuario)
                        VALUES (
                            @idProveedor, @tipoMovimientoCC, NULL, @idPago, 0, @monto" + suf + @",
                            ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteProveedor
                                    WHERE idProveedor = @idProveedor ORDER BY idMovimiento DESC), 0) - @monto" + suf + @",
                            @descripcionCC" + suf + @", NULL);
                    ");
                }
            }

            sql.Append(@"
                COMMIT TRANSACTION;
                SELECT @primerIdPago;
            ");

            var id = AccesoDatos.Escalar(sql.ToString(), parametros.ToArray());
            pago.IdPago = System.Convert.ToInt32(id);

            return ResultadoOperacion.Ok(ArmarMensaje(imputaciones));
        }

        // Cuenta cómo quedó repartido el pago: qué deudas canceló y cuánto quedó a favor.
        private static string ArmarMensaje(List<Imputacion> imputaciones)
        {
            var aplicadas = new List<string>();
            decimal aFavor = 0;
            foreach (var imp in imputaciones)
            {
                if (imp.IdComprobante.HasValue)
                    aplicadas.Add(imp.Numero + " (" + imp.Monto.ToString("N2") + ")");
                else
                    aFavor = imp.Monto;
            }

            var mensaje = "Pago registrado.";
            if (aplicadas.Count > 0)
                mensaje += " Se aplicó a: " + string.Join(", ", aplicadas) + ".";
            if (aFavor > 0)
                mensaje += " Quedaron " + aFavor.ToString("N2") + " a favor.";
            return mensaje;
        }
    }
}
