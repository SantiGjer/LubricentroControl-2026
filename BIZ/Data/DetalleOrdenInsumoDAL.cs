using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class DetalleOrdenInsumoDAL
    {
        private const string SelectBase = @"
            SELECT d.idDetalle, d.idOrden, d.idInsumo, p.nombre AS nombreInsumo,
                   d.cantidad, d.precioUnitario, p.tipoIva, p.alicuotaIva
            FROM DetalleOrdenInsumo d
            INNER JOIN Producto p ON p.idProducto = d.idInsumo";

        private static DetalleOrdenInsumo Mapear(DataRow fila)
        {
            return new DetalleOrdenInsumo
            {
                IdDetalle = AccesoDatos.LeerInt(fila, "idDetalle"),
                IdOrden = AccesoDatos.LeerInt(fila, "idOrden"),
                IdInsumo = AccesoDatos.LeerInt(fila, "idInsumo"),
                NombreInsumo = AccesoDatos.LeerString(fila, "nombreInsumo"),
                Cantidad = AccesoDatos.LeerDecimal(fila, "cantidad"),
                PrecioUnitario = AccesoDatos.LeerDecimal(fila, "precioUnitario"),
                TipoIva = AccesoDatos.LeerString(fila, "tipoIva"),
                AlicuotaIva = AccesoDatos.LeerDecimal(fila, "alicuotaIva")
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
        // stock sin su línea de detalle si la segunda escritura fallara. El stock se descuenta en
        // relativo, con la condición de no quedar negativo en el mismo UPDATE (2026-10-18, mismo
        // criterio que MovimientoStockDAL.Registrar): si no alcanza, no se escribe nada.
        public static ResultadoOperacion Agregar(int idOrden, int idInsumo, decimal cantidad, int idUsuario)
        {
            var orden = OrdenDeTrabajoDAL.ObtenerPorId(idOrden);
            if (orden == null)
                return ResultadoOperacion.Error("La orden no existe.");
            if (System.Array.IndexOf(OrdenDeTrabajo.EstadosEditables, orden.Estado) < 0)
                return ResultadoOperacion.Error(
                    "Una orden " + orden.Estado.ToLowerInvariant() + " no admite cambios en el detalle.");

            if (cantidad <= 0)
                return ResultadoOperacion.Error("La cantidad debe ser mayor a cero.");

            var insumo = ProductoDAL.ObtenerPorId(idInsumo);
            if (insumo == null || !insumo.EsInsumo)
                return ResultadoOperacion.Error("El insumo no existe.");
            if (!insumo.Activo)
                return ResultadoOperacion.Error("El insumo está dado de baja.");

            const string sql = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                DECLARE @stockResultante DECIMAL(12,2) = NULL;

                UPDATE Insumo SET stockActual = stockActual - @cantidad
                WHERE idInsumo = @idInsumo AND stockActual - @cantidad >= 0;

                IF @@ROWCOUNT = 1
                BEGIN
                    SELECT @stockResultante = stockActual FROM Insumo WHERE idInsumo = @idInsumo;

                    INSERT INTO MovimientoStock
                        (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
                    VALUES
                        (@idInsumo, @tipoMovimiento, NULL, @idOrden, @idUsuario, 0, @cantidad, @stockResultante, @descripcion);

                    INSERT INTO DetalleOrdenInsumo (idOrden, idInsumo, cantidad, precioUnitario)
                    VALUES (@idOrden, @idInsumo, @cantidad, @precioUnitario);
                END

                COMMIT TRANSACTION;

                SELECT @stockResultante;";

            var stockResultante = AccesoDatos.Escalar(sql,
                AccesoDatos.Param("@idInsumo", idInsumo),
                AccesoDatos.Param("@tipoMovimiento", MovimientoStock.TipoOrden),
                AccesoDatos.Param("@idOrden", idOrden),
                AccesoDatos.Param("@idUsuario", idUsuario),
                AccesoDatos.Param("@cantidad", cantidad),
                AccesoDatos.Param("@descripcion", "Orden de trabajo #" + idOrden),
                AccesoDatos.Param("@precioUnitario", insumo.Precio));

            if (stockResultante == null)
                return MovimientoStockDAL.StockInsuficiente(idInsumo, cantidad);

            return ResultadoOperacion.Ok("Insumo agregado. Stock actualizado.");
        }

        public static DetalleOrdenInsumo ObtenerPorId(int idDetalle)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE d.idDetalle = @idDetalle",
                AccesoDatos.Param("@idDetalle", idDetalle));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        // Inverso de Agregar: repone el stock, deja la entrada en el kardex y borra la línea,
        // las tres escrituras juntas (mismo patrón XACT_ABORT/BEGIN TRAN/COMMIT). Solo sobre una
        // orden Abierta o En proceso: una Cerrada ya copió la línea a la venta, y una Cancelada
        // ya repuso el stock de todas sus líneas (quitar una repondría dos veces). El kardex usa
        // TipoCancelacionOrden (ya admitido por CK_MovStock_origen) para no tocar el esquema.
        // Desde el 2026-10-18 primero se borra la línea y solo si se borró se repone el stock,
        // en relativo: si dos usuarios quitan la misma línea a la vez, el segundo no repone de
        // nuevo.
        public static ResultadoOperacion Quitar(int idDetalle, int idUsuario)
        {
            var detalle = ObtenerPorId(idDetalle);
            if (detalle == null)
                return ResultadoOperacion.Error("La línea de insumo no existe.");

            var orden = OrdenDeTrabajoDAL.ObtenerPorId(detalle.IdOrden);
            if (orden == null)
                return ResultadoOperacion.Error("La orden no existe.");
            if (orden.Estado == OrdenDeTrabajo.EstadoCerrada || orden.Estado == OrdenDeTrabajo.EstadoCancelada)
                return ResultadoOperacion.Error("Una orden " + orden.Estado.ToLowerInvariant() + " no se puede modificar.");

            var insumo = ProductoDAL.ObtenerPorId(detalle.IdInsumo);
            if (insumo == null || !insumo.EsInsumo)
                return ResultadoOperacion.Error("El insumo no existe.");

            const string sql = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                DECLARE @stockResultante DECIMAL(12,2) = NULL;

                DELETE FROM DetalleOrdenInsumo WHERE idDetalle = @idDetalle;

                IF @@ROWCOUNT = 1
                BEGIN
                    UPDATE Insumo SET stockActual = stockActual + @cantidad WHERE idInsumo = @idInsumo;

                    SELECT @stockResultante = stockActual FROM Insumo WHERE idInsumo = @idInsumo;

                    INSERT INTO MovimientoStock
                        (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
                    VALUES
                        (@idInsumo, @tipoMovimiento, NULL, @idOrden, @idUsuario, @cantidad, 0, @stockResultante, @descripcion);
                END

                COMMIT TRANSACTION;

                SELECT @stockResultante;";

            var stockResultante = AccesoDatos.Escalar(sql,
                AccesoDatos.Param("@idInsumo", detalle.IdInsumo),
                AccesoDatos.Param("@tipoMovimiento", MovimientoStock.TipoCancelacionOrden),
                AccesoDatos.Param("@idOrden", detalle.IdOrden),
                AccesoDatos.Param("@idUsuario", idUsuario),
                AccesoDatos.Param("@cantidad", detalle.Cantidad),
                AccesoDatos.Param("@descripcion", "Reposición por quitar insumo de la orden #" + detalle.IdOrden),
                AccesoDatos.Param("@idDetalle", idDetalle));

            if (stockResultante == null)
                return ResultadoOperacion.Error("La línea de insumo ya no existe: la quitó otro usuario.");

            return ResultadoOperacion.Ok("Insumo quitado. Se repuso el stock.");
        }
    }
}
