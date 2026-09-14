using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class DetalleComprobanteVentaDAL
    {
        private static DetalleComprobanteVenta Mapear(DataRow fila)
        {
            return new DetalleComprobanteVenta
            {
                IdDetalle = AccesoDatos.LeerInt(fila, "idDetalle"),
                IdVenta = AccesoDatos.LeerInt(fila, "idVenta"),
                TipoItem = AccesoDatos.LeerString(fila, "tipoItem"),
                IdServicio = AccesoDatos.LeerIntNullable(fila, "idServicio"),
                IdInsumo = AccesoDatos.LeerIntNullable(fila, "idInsumo"),
                Descripcion = AccesoDatos.LeerString(fila, "descripcion"),
                Cantidad = AccesoDatos.LeerDecimal(fila, "cantidad"),
                PrecioUnitario = AccesoDatos.LeerDecimal(fila, "precioUnitario"),
                Subtotal = AccesoDatos.LeerDecimal(fila, "subtotal")
            };
        }

        // La inserción de líneas va dentro del batch atómico de
        // ComprobanteVentaDAL.GenerarDesdeOrden — acá solo el listado, para mostrar una venta ya generada.
        public static List<DetalleComprobanteVenta> ListarPorVenta(int idVenta)
        {
            const string sql = @"
                SELECT idDetalle, idVenta, tipoItem, idServicio, idInsumo, descripcion, cantidad, precioUnitario, subtotal
                FROM DetalleComprobanteVenta
                WHERE idVenta = @idVenta
                ORDER BY idDetalle";

            var lista = new List<DetalleComprobanteVenta>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql, AccesoDatos.Param("@idVenta", idVenta)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }
    }
}
