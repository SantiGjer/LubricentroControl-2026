using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class DetalleOrdenInsumoDAL
    {
        private const string SelectBase = @"
            SELECT d.idDetalle, d.idOrden, d.idInsumo, i.nombre AS nombreInsumo,
                   d.cantidad, d.precioUnitario
            FROM DetalleOrdenInsumo d
            INNER JOIN Insumo i ON i.idInsumo = d.idInsumo";

        private static DetalleOrdenInsumo Mapear(DataRow fila)
        {
            return new DetalleOrdenInsumo
            {
                IdDetalle = AccesoDatos.LeerInt(fila, "idDetalle"),
                IdOrden = AccesoDatos.LeerInt(fila, "idOrden"),
                IdInsumo = AccesoDatos.LeerInt(fila, "idInsumo"),
                NombreInsumo = AccesoDatos.LeerString(fila, "nombreInsumo"),
                Cantidad = AccesoDatos.LeerDecimal(fila, "cantidad"),
                PrecioUnitario = AccesoDatos.LeerDecimal(fila, "precioUnitario")
            };
        }

        public static List<DetalleOrdenInsumo> ListarPorOrden(int idOrden)
        {
            var lista = new List<DetalleOrdenInsumo>();
            foreach (DataRow fila in AccesoDatos.Consultar(
                SelectBase + " WHERE d.idOrden = @idOrden ORDER BY d.idDetalle",
                AccesoDatos.Param("@idOrden", idOrden)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Agregar una línea de insumo son tres escrituras que tienen que ir juntas: descontar
        // stock, dejar el movimiento en el kardex, e insertar la línea de detalle. A diferencia
        // de MovimientoStockDAL.Registrar (que atomiza las primeras dos), acá hace falta una
        // tercera — se replica el mismo patrón SET XACT_ABORT ON/BEGIN TRAN/COMMIT (ver
        // CLAUDE.md) en vez de encadenar dos llamadas separadas, para no dejar un movimiento de
        // stock sin su línea de detalle si la segunda escritura fallara.
        public static ResultadoOperacion Agregar(int idOrden, int idInsumo, decimal cantidad, int idUsuario)
        {
            if (cantidad <= 0)
                return ResultadoOperacion.Error("La cantidad debe ser mayor a cero.");

            var insumo = InsumoDAL.ObtenerPorId(idInsumo);
            if (insumo == null)
                return ResultadoOperacion.Error("El insumo no existe.");
            if (!insumo.Activo)
                return ResultadoOperacion.Error("El insumo está dado de baja.");

            var stockResultante = insumo.StockActual - cantidad;
            if (stockResultante < 0)
                return ResultadoOperacion.Error(
                    "Stock insuficiente. Disponible: " + insumo.StockActual + ", se intentó descontar " + cantidad + ".");

            const string sql = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                UPDATE Insumo SET stockActual = @stockResultante WHERE idInsumo = @idInsumo;

                INSERT INTO MovimientoStock
                    (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
                VALUES
                    (@idInsumo, @tipoMovimiento, NULL, @idOrden, @idUsuario, 0, @cantidad, @stockResultante, @descripcion);

                INSERT INTO DetalleOrdenInsumo (idOrden, idInsumo, cantidad, precioUnitario)
                VALUES (@idOrden, @idInsumo, @cantidad, @precioUnitario);

                COMMIT TRANSACTION;";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@idInsumo", idInsumo),
                AccesoDatos.Param("@tipoMovimiento", MovimientoStock.TipoOrden),
                AccesoDatos.Param("@idOrden", idOrden),
                AccesoDatos.Param("@idUsuario", idUsuario),
                AccesoDatos.Param("@cantidad", cantidad),
                AccesoDatos.Param("@stockResultante", stockResultante),
                AccesoDatos.Param("@descripcion", "Orden de trabajo #" + idOrden),
                AccesoDatos.Param("@precioUnitario", insumo.PrecioVenta));

            return ResultadoOperacion.Ok("Insumo agregado. Stock actualizado.");
        }
    }
}
