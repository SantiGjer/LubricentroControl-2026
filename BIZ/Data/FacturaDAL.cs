using System.Data;
using System.Globalization;
using BIZ.Modelo;

namespace BIZ.Data
{
    // Facturas de las ventas (Requerimientos §9.10). Una por venta, a pedido desde la pantalla de
    // Ventas; numeración correlativa por letra y punto de venta.
    public static class FacturaDAL
    {
        private const string SelectBase = @"
            SELECT idFactura, idVenta, tipo, puntoVenta, numero, fecha, idUsuario, condicionVenta,
                   emisorRazonSocial, emisorCuit, emisorCondicionIva, emisorDomicilio, emisorIngresosBrutos,
                   emisorInicioActividades, receptorNombre, receptorTipoDocumento, receptorNumeroDocumento,
                   receptorCondicionIva, receptorDomicilio
            FROM Factura";

        private static Factura Mapear(DataRow fila)
        {
            return new Factura
            {
                IdFactura = AccesoDatos.LeerInt(fila, "idFactura"),
                IdVenta = AccesoDatos.LeerInt(fila, "idVenta"),
                Tipo = AccesoDatos.LeerString(fila, "tipo"),
                PuntoVenta = AccesoDatos.LeerInt(fila, "puntoVenta"),
                Numero = AccesoDatos.LeerInt(fila, "numero"),
                Fecha = AccesoDatos.LeerFecha(fila, "fecha"),
                IdUsuario = AccesoDatos.LeerInt(fila, "idUsuario"),
                CondicionVenta = AccesoDatos.LeerString(fila, "condicionVenta"),
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
                ReceptorDomicilio = AccesoDatos.LeerString(fila, "receptorDomicilio")
            };
        }

        public static Factura ObtenerPorVenta(int idVenta)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE idVenta = @idVenta",
                AccesoDatos.Param("@idVenta", idVenta));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        // Emite la factura de una venta, o devuelve la que ya tenía: una venta se factura una sola
        // vez (UQ_Factura_venta) y la factura no se anula ni se edita. Los datos del comercio salen
        // de EmisorDAL y quedan copiados en la factura. La letra sale de la condición frente al IVA
        // del comercio y del cliente (Factura.DeterminarTipo); el número es el siguiente de esa
        // letra y punto de venta, tomado con UPDLOCK/HOLDLOCK dentro del mismo batch del INSERT
        // para que dos emisiones simultáneas no repitan número.
        public static ResultadoOperacion Emitir(int idVenta, int idUsuario, out Factura factura)
        {
            factura = ObtenerPorVenta(idVenta);
            if (factura != null)
                return ResultadoOperacion.Ok("La venta ya estaba facturada.");

            var venta = ComprobanteVentaDAL.ObtenerPorId(idVenta);
            if (venta == null)
                return ResultadoOperacion.Error("La venta no existe.");

            var cliente = ClienteDAL.ObtenerPorId(venta.IdCliente);
            if (cliente == null)
                return ResultadoOperacion.Error("El cliente de la venta no existe.");

            var emisor = EmisorDAL.Obtener();
            if (emisor == null)
                return ResultadoOperacion.Error(
                    "Faltan los datos del comercio que factura: se cargan en Administración, Datos del comercio.");

            var validacionEmisor = emisor.Validar();
            if (!validacionEmisor.Exito)
                return ResultadoOperacion.Error("Revisá los datos del comercio (Administración, Datos del comercio): " +
                                                validacionEmisor.Mensaje);

            var tipo = Factura.DeterminarTipo(emisor.CondicionIva, cliente.CondicionIva);

            const string sql = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                DECLARE @numero INT = ISNULL((
                    SELECT MAX(numero) FROM Factura WITH (UPDLOCK, HOLDLOCK)
                    WHERE tipo = @tipo AND puntoVenta = @puntoVenta), 0) + 1;

                INSERT INTO Factura
                    (idVenta, tipo, puntoVenta, numero, idUsuario, condicionVenta,
                     emisorRazonSocial, emisorCuit, emisorCondicionIva, emisorDomicilio, emisorIngresosBrutos,
                     emisorInicioActividades, receptorNombre, receptorTipoDocumento, receptorNumeroDocumento,
                     receptorCondicionIva, receptorDomicilio)
                VALUES
                    (@idVenta, @tipo, @puntoVenta, @numero, @idUsuario, @condicionVenta,
                     @emisorRazonSocial, @emisorCuit, @emisorCondicionIva, @emisorDomicilio, @emisorIngresosBrutos,
                     @emisorInicioActividades, @receptorNombre, @receptorTipoDocumento, @receptorNumeroDocumento,
                     @receptorCondicionIva, @receptorDomicilio);

                COMMIT TRANSACTION;";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@idVenta", idVenta),
                AccesoDatos.Param("@tipo", tipo),
                AccesoDatos.Param("@puntoVenta", emisor.PuntoVenta),
                AccesoDatos.Param("@idUsuario", idUsuario),
                AccesoDatos.Param("@condicionVenta",
                    cliente.CuentaCorriente ? Factura.CondicionVentaCuentaCorriente : Factura.CondicionVentaContado),
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
                AccesoDatos.Param("@receptorDomicilio", string.IsNullOrEmpty(cliente.DomicilioCompleto) ? null : cliente.DomicilioCompleto));

            factura = ObtenerPorVenta(idVenta);
            return ResultadoOperacion.Ok("Se emitió la factura " +
                Factura.FormatearNumero(factura.Tipo, factura.PuntoVenta, factura.Numero) + ".");
        }
    }
}
