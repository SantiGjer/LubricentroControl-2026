using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using BIZ.Modelo;

namespace BIZ.Data
{
    // Notas de crédito de las ventas (Requerimientos §9.17). Una por venta, a pedido desde la
    // pantalla de Ventas: anula la venta completa.
    public static class NotaCreditoDAL
    {
        private const string SelectBase = @"
            SELECT nc.idNotaCredito, nc.idVenta, nc.idFactura, nc.tipo, nc.puntoVenta, nc.numero, nc.fecha,
                   nc.idUsuario, nc.motivo, nc.condicionVenta, nc.subtotal, nc.impuestos, nc.total,
                   nc.emisorRazonSocial, nc.emisorCuit, nc.emisorCondicionIva, nc.emisorDomicilio,
                   nc.emisorIngresosBrutos, nc.emisorInicioActividades, nc.receptorNombre,
                   nc.receptorTipoDocumento, nc.receptorNumeroDocumento, nc.receptorCondicionIva,
                   nc.receptorDomicilio,
                   f.tipo AS tipoFactura, f.puntoVenta AS puntoVentaFactura, f.numero AS numeroFactura,
                   u.nombre + ' ' + u.apellido AS nombreUsuario
            FROM NotaCredito nc
            LEFT JOIN Factura f ON f.idFactura = nc.idFactura
            INNER JOIN Usuario u ON u.idUsuario = nc.idUsuario";

        private static NotaCredito Mapear(DataRow fila)
        {
            var tipoFactura = AccesoDatos.LeerString(fila, "tipoFactura");

            return new NotaCredito
            {
                IdNotaCredito = AccesoDatos.LeerInt(fila, "idNotaCredito"),
                IdVenta = AccesoDatos.LeerInt(fila, "idVenta"),
                IdFactura = AccesoDatos.LeerIntNullable(fila, "idFactura"),
                Tipo = AccesoDatos.LeerString(fila, "tipo"),
                PuntoVenta = AccesoDatos.LeerInt(fila, "puntoVenta"),
                Numero = AccesoDatos.LeerInt(fila, "numero"),
                Fecha = AccesoDatos.LeerFecha(fila, "fecha"),
                IdUsuario = AccesoDatos.LeerInt(fila, "idUsuario"),
                Motivo = AccesoDatos.LeerString(fila, "motivo"),
                CondicionVenta = AccesoDatos.LeerString(fila, "condicionVenta"),
                Subtotal = AccesoDatos.LeerDecimal(fila, "subtotal"),
                Impuestos = AccesoDatos.LeerDecimal(fila, "impuestos"),
                Total = AccesoDatos.LeerDecimal(fila, "total"),
                EmisorRazonSocial = AccesoDatos.LeerString(fila, "emisorRazonSocial"),
                EmisorCuit = AccesoDatos.LeerString(fila, "emisorCuit"),
                EmisorCondicionIva = AccesoDatos.LeerString(fila, "emisorCondicionIva"),
                EmisorDomicilio = AccesoDatos.LeerString(fila, "emisorDomicilio"),
                EmisorIngresosBrutos = AccesoDatos.LeerString(fila, "emisorIngresosBrutos"),
                EmisorInicioActividades = AccesoDatos.LeerString(fila, "emisorInicioActividades"),
                ReceptorNombre = AccesoDatos.LeerString(fila, "receptorNombre"),
                ReceptorTipoDocumento = AccesoDatos.LeerString(fila, "receptorTipoDocumento"),
                ReceptorNumeroDocumento = AccesoDatos.LeerString(fila, "receptorNumeroDocumento"),
                ReceptorCondicionIva = AccesoDatos.LeerString(fila, "receptorCondicionIva"),
                ReceptorDomicilio = AccesoDatos.LeerString(fila, "receptorDomicilio"),
                NombreUsuario = AccesoDatos.LeerString(fila, "nombreUsuario"),
                NumeroFactura = tipoFactura == null
                    ? null
                    : Factura.FormatearNumero(tipoFactura, AccesoDatos.LeerInt(fila, "puntoVentaFactura"),
                                              AccesoDatos.LeerInt(fila, "numeroFactura"))
            };
        }

        public static NotaCredito ObtenerPorVenta(int idVenta)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE nc.idVenta = @idVenta",
                AccesoDatos.Param("@idVenta", idVenta));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        // Notas de crédito emitidas en un período, para Reportes/VentasPorPeriodo.aspx: restan de lo
        // vendido en el período en que se emiten. "hasta" es inclusive del día completo, mismo
        // criterio que ComprobanteVentaDAL.ListarPorPeriodo.
        public static List<NotaCredito> ListarPorPeriodo(DateTime desde, DateTime hasta)
        {
            var lista = new List<NotaCredito>();
            foreach (DataRow fila in AccesoDatos.Consultar(
                SelectBase + " WHERE nc.fecha >= @desde AND nc.fecha < @hastaExclusiva ORDER BY nc.fecha",
                AccesoDatos.Param("@desde", desde.Date),
                AccesoDatos.Param("@hastaExclusiva", hasta.Date.AddDays(1))).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Emite la nota de crédito que anula una venta, o devuelve la que ya tenía: una por venta
        // (UQ_NotaCredito_venta), y no se anula ni se edita. Lleva la letra y la condición de venta
        // de la factura que anula; si la venta no se facturó, las que tendría su factura (mismo
        // criterio que FacturaDAL.Emitir). En un solo batch atómico:
        //   - la nota de crédito, con el número siguiente de su letra y punto de venta, tomado con
        //     UPDLOCK/HOLDLOCK como en la factura para que dos emisiones no repitan número;
        //   - el saldo pendiente de la venta en 0;
        //   - el movimiento NotaCredito en la cuenta corriente del cliente, al haber por el total:
        //     lo que debía de esa venta se cancela, y lo que ya había pagado le queda a favor.
        // No toca el stock: los insumos de la venta ya se usaron en el vehículo.
        public static ResultadoOperacion Emitir(int idVenta, string motivo, int idUsuario, out NotaCredito notaCredito)
        {
            notaCredito = ObtenerPorVenta(idVenta);
            if (notaCredito != null)
                return YaEmitida(notaCredito);

            if (string.IsNullOrWhiteSpace(motivo))
                return ResultadoOperacion.Error("El motivo de la nota de crédito es obligatorio.");

            motivo = motivo.Trim();
            if (motivo.Length > 300)
                return ResultadoOperacion.Error("El motivo no puede tener más de 300 caracteres.");

            var venta = ComprobanteVentaDAL.ObtenerPorId(idVenta);
            if (venta == null)
                return ResultadoOperacion.Error("La venta no existe.");

            var cliente = ClienteDAL.ObtenerPorId(venta.IdCliente);
            if (cliente == null)
                return ResultadoOperacion.Error("El cliente de la venta no existe.");

            var emisor = EmisorDAL.Obtener();
            if (emisor == null)
                return ResultadoOperacion.Error(
                    "Faltan los datos del comercio que emite: se cargan en Administración, Datos del comercio.");

            var validacionEmisor = emisor.Validar();
            if (!validacionEmisor.Exito)
                return ResultadoOperacion.Error("Revisá los datos del comercio (Administración, Datos del comercio): " +
                                                validacionEmisor.Mensaje);

            var factura = FacturaDAL.ObtenerPorVenta(idVenta);
            var tipo = factura != null
                ? factura.Tipo
                : Factura.DeterminarTipo(emisor.CondicionIva, cliente.CondicionIva);
            var condicionVenta = factura != null
                ? factura.CondicionVenta
                : cliente.CuentaCorriente ? Factura.CondicionVentaCuentaCorriente : Factura.CondicionVentaContado;

            const string sql = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                DECLARE @numero INT = ISNULL((
                    SELECT MAX(numero) FROM NotaCredito WITH (UPDLOCK, HOLDLOCK)
                    WHERE tipo = @tipo AND puntoVenta = @puntoVenta), 0) + 1;

                DECLARE @saldoAnterior DECIMAL(12,2) = ISNULL((
                    SELECT TOP 1 saldo FROM CuentaCorrienteCliente WITH (UPDLOCK, HOLDLOCK)
                    WHERE idCliente = @idCliente ORDER BY idMovimiento DESC), 0);

                INSERT INTO NotaCredito
                    (idVenta, idFactura, tipo, puntoVenta, numero, idUsuario, motivo, condicionVenta,
                     subtotal, impuestos, total,
                     emisorRazonSocial, emisorCuit, emisorCondicionIva, emisorDomicilio, emisorIngresosBrutos,
                     emisorInicioActividades, receptorNombre, receptorTipoDocumento, receptorNumeroDocumento,
                     receptorCondicionIva, receptorDomicilio)
                VALUES
                    (@idVenta, @idFactura, @tipo, @puntoVenta, @numero, @idUsuario, @motivo, @condicionVenta,
                     @subtotal, @impuestos, @total,
                     @emisorRazonSocial, @emisorCuit, @emisorCondicionIva, @emisorDomicilio, @emisorIngresosBrutos,
                     @emisorInicioActividades, @receptorNombre, @receptorTipoDocumento, @receptorNumeroDocumento,
                     @receptorCondicionIva, @receptorDomicilio);

                UPDATE ComprobanteVenta SET saldoPendiente = 0 WHERE idVenta = @idVenta;

                INSERT INTO CuentaCorrienteCliente
                    (idCliente, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
                VALUES (
                    @idCliente, @tipoMovimientoCC, @idVenta, NULL, 0, @total,
                    @saldoAnterior - @total,
                    'Nota de crédito ' + @tipo + ' ' + RIGHT('00000' + CAST(@puntoVenta AS VARCHAR(5)), 5) + '-' +
                        RIGHT('00000000' + CAST(@numero AS VARCHAR(8)), 8) + ' (anula ' + @numeroVenta + ')',
                    @idUsuario);

                COMMIT TRANSACTION;";

            try
            {
                AccesoDatos.Ejecutar(sql,
                    AccesoDatos.Param("@idVenta", idVenta),
                    AccesoDatos.Param("@idFactura", factura == null ? (int?)null : factura.IdFactura),
                    AccesoDatos.Param("@tipo", tipo),
                    AccesoDatos.Param("@puntoVenta", emisor.PuntoVenta),
                    AccesoDatos.Param("@idUsuario", idUsuario),
                    AccesoDatos.Param("@motivo", motivo),
                    AccesoDatos.Param("@condicionVenta", condicionVenta),
                    AccesoDatos.Param("@subtotal", venta.Subtotal),
                    AccesoDatos.Param("@impuestos", venta.Impuestos),
                    AccesoDatos.Param("@total", venta.Total),
                    AccesoDatos.Param("@emisorRazonSocial", emisor.RazonSocial),
                    AccesoDatos.Param("@emisorCuit", emisor.Cuit),
                    AccesoDatos.Param("@emisorCondicionIva", emisor.CondicionIva),
                    AccesoDatos.Param("@emisorDomicilio", emisor.Domicilio),
                    AccesoDatos.Param("@emisorIngresosBrutos", emisor.IngresosBrutos),
                    AccesoDatos.Param("@emisorInicioActividades", emisor.InicioActividades.HasValue
                        ? emisor.InicioActividades.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                        : null),
                    AccesoDatos.Param("@receptorNombre", cliente.Denominacion),
                    AccesoDatos.Param("@receptorTipoDocumento", cliente.TipoDocumento),
                    AccesoDatos.Param("@receptorNumeroDocumento", cliente.NumeroDocumento),
                    AccesoDatos.Param("@receptorCondicionIva", cliente.CondicionIva),
                    AccesoDatos.Param("@receptorDomicilio", string.IsNullOrEmpty(cliente.DomicilioCompleto) ? null : cliente.DomicilioCompleto),
                    AccesoDatos.Param("@idCliente", venta.IdCliente),
                    AccesoDatos.Param("@tipoMovimientoCC", CuentaCorrienteCliente.TipoNotaCredito),
                    AccesoDatos.Param("@numeroVenta", venta.NumeroComprobante));
            }
            catch (SqlException ex) when (ex.Number == 2627 && ex.Message.Contains("UQ_NotaCredito_venta"))
            {
                // Otro usuario anuló la misma venta entre el chequeo del principio y este INSERT: la
                // base rechazó la segunda nota de crédito y XACT_ABORT revirtió todo el batch.
                notaCredito = ObtenerPorVenta(idVenta);
                return YaEmitida(notaCredito);
            }

            notaCredito = ObtenerPorVenta(idVenta);
            return ResultadoOperacion.Ok("Se emitió la nota de crédito " +
                NotaCredito.FormatearNumero(notaCredito.Tipo, notaCredito.PuntoVenta, notaCredito.Numero) +
                ". Se acreditaron " + venta.Total.ToString("N2") + " en la cuenta corriente del cliente.");
        }

        private static ResultadoOperacion YaEmitida(NotaCredito notaCredito)
        {
            return ResultadoOperacion.Ok("La venta ya tenía la nota de crédito " +
                NotaCredito.FormatearNumero(notaCredito.Tipo, notaCredito.PuntoVenta, notaCredito.Numero) + ".");
        }
    }
}
