using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class DetalleOrdenServicioDAL
    {
        private const string SelectBase = @"
            SELECT d.idDetalle, d.idOrden, d.idServicio, s.nombre AS nombreServicio,
                   d.cantidad, d.precioAplicado
            FROM DetalleOrdenServicio d
            INNER JOIN Servicio s ON s.idServicio = d.idServicio";

        private static DetalleOrdenServicio Mapear(DataRow fila)
        {
            return new DetalleOrdenServicio
            {
                IdDetalle = AccesoDatos.LeerInt(fila, "idDetalle"),
                IdOrden = AccesoDatos.LeerInt(fila, "idOrden"),
                IdServicio = AccesoDatos.LeerInt(fila, "idServicio"),
                NombreServicio = AccesoDatos.LeerString(fila, "nombreServicio"),
                Cantidad = AccesoDatos.LeerDecimal(fila, "cantidad"),
                PrecioAplicado = AccesoDatos.LeerDecimal(fila, "precioAplicado")
            };
        }

        public static List<DetalleOrdenServicio> ListarPorOrden(int idOrden)
        {
            var lista = new List<DetalleOrdenServicio>();
            foreach (DataRow fila in AccesoDatos.Consultar(
                SelectBase + " WHERE d.idOrden = @idOrden ORDER BY d.idDetalle",
                AccesoDatos.Param("@idOrden", idOrden)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Cerrada/Cancelada no admite tocar el detalle: la venta (si la orden se cerró) ya
        // quedó generada con las líneas de ese momento, y una orden cancelada ya repuso su
        // stock — agregar o quitar después dejaría el detalle desincronizado de la venta o del
        // kardex. Mismo criterio en DetalleOrdenInsumoDAL.Agregar.
        private static ResultadoOperacion ValidarOrdenEditable(int idOrden)
        {
            var orden = OrdenDeTrabajoDAL.ObtenerPorId(idOrden);
            if (orden == null)
                return ResultadoOperacion.Error("La orden no existe.");
            if (System.Array.IndexOf(OrdenDeTrabajo.EstadosEditables, orden.Estado) < 0)
                return ResultadoOperacion.Error(
                    "Una orden " + orden.Estado.ToLowerInvariant() + " no admite cambios en el detalle.");
            return ResultadoOperacion.Ok();
        }

        // precioAplicado es un snapshot del precioBase vigente del servicio al momento de
        // agregar la línea (no toca stock, así que no necesita transacción especial).
        public static ResultadoOperacion Agregar(int idOrden, int idServicio, decimal cantidad)
        {
            var ordenEditable = ValidarOrdenEditable(idOrden);
            if (!ordenEditable.Exito) return ordenEditable;

            if (cantidad <= 0)
                return ResultadoOperacion.Error("La cantidad debe ser mayor a cero.");

            var servicio = ServicioDAL.ObtenerPorId(idServicio);
            if (servicio == null)
                return ResultadoOperacion.Error("El servicio no existe.");
            if (!servicio.Activo)
                return ResultadoOperacion.Error("El servicio está dado de baja.");

            const string sql = @"
                INSERT INTO DetalleOrdenServicio (idOrden, idServicio, cantidad, precioAplicado)
                VALUES (@idOrden, @idServicio, @cantidad, @precioAplicado)";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@idOrden", idOrden),
                AccesoDatos.Param("@idServicio", idServicio),
                AccesoDatos.Param("@cantidad", cantidad),
                AccesoDatos.Param("@precioAplicado", servicio.PrecioBase));

            return ResultadoOperacion.Ok("Servicio agregado.");
        }

        // Sin efecto colateral (no toca stock): un DELETE simple alcanza, pero primero hay que
        // saber a qué orden pertenece la línea para poder validar que siga editable.
        public static ResultadoOperacion Quitar(int idDetalle)
        {
            var idOrden = AccesoDatos.Escalar(
                "SELECT idOrden FROM DetalleOrdenServicio WHERE idDetalle = @idDetalle",
                AccesoDatos.Param("@idDetalle", idDetalle));
            if (idOrden == null)
                return ResultadoOperacion.Error("La línea no existe.");

            var ordenEditable = ValidarOrdenEditable(System.Convert.ToInt32(idOrden));
            if (!ordenEditable.Exito) return ordenEditable;

            AccesoDatos.Ejecutar(
                "DELETE FROM DetalleOrdenServicio WHERE idDetalle = @idDetalle",
                AccesoDatos.Param("@idDetalle", idDetalle));

            return ResultadoOperacion.Ok("Servicio quitado.");
        }
    }
}
