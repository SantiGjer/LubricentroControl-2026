using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class DetalleCompraDAL
    {
        private const string SelectBase = @"
            SELECT d.idDetalle, d.idCompra, d.idInsumo, i.nombre AS nombreInsumo,
                   d.cantidad, d.precioUnitario
            FROM DetalleCompra d
            INNER JOIN Insumo i ON i.idInsumo = d.idInsumo";

        private static DetalleCompra Mapear(DataRow fila)
        {
            return new DetalleCompra
            {
                IdDetalle = AccesoDatos.LeerInt(fila, "idDetalle"),
                IdCompra = AccesoDatos.LeerInt(fila, "idCompra"),
                IdInsumo = AccesoDatos.LeerInt(fila, "idInsumo"),
                NombreInsumo = AccesoDatos.LeerString(fila, "nombreInsumo"),
                Cantidad = AccesoDatos.LeerDecimal(fila, "cantidad"),
                PrecioUnitario = AccesoDatos.LeerDecimal(fila, "precioUnitario")
            };
        }

        // La inserción de líneas va dentro del batch atómico de ComprobanteCompraDAL.Crear —
        // acá solo el listado, para mostrar una compra ya guardada.
        public static List<DetalleCompra> ListarPorCompra(int idCompra)
        {
            var lista = new List<DetalleCompra>();
            foreach (DataRow fila in AccesoDatos.Consultar(
                SelectBase + " WHERE d.idCompra = @idCompra ORDER BY d.idDetalle",
                AccesoDatos.Param("@idCompra", idCompra)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }
    }
}
