using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class ServicioDAL
    {
        private const string SelectBase = @"
            SELECT idServicio, nombre, descripcion, precioBase, activo
            FROM Servicio";

        private static Servicio Mapear(DataRow fila)
        {
            return new Servicio
            {
                IdServicio = AccesoDatos.LeerInt(fila, "idServicio"),
                Nombre = AccesoDatos.LeerString(fila, "nombre"),
                Descripcion = AccesoDatos.LeerString(fila, "descripcion"),
                PrecioBase = AccesoDatos.LeerDecimal(fila, "precioBase"),
                Activo = AccesoDatos.LeerBool(fila, "activo")
            };
        }

        public static List<Servicio> Listar(bool incluirInactivos = true)
        {
            var sql = SelectBase +
                      (incluirInactivos ? "" : " WHERE activo = 1") +
                      " ORDER BY nombre";

            var lista = new List<Servicio>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static Servicio ObtenerPorId(int idServicio)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE idServicio = @idServicio",
                AccesoDatos.Param("@idServicio", idServicio));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        // Buscador rápido por nombre.
        public static List<Servicio> Buscar(string texto, bool incluirInactivos = false)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Listar(incluirInactivos);

            var sql = SelectBase +
                      " WHERE nombre LIKE @texto" +
                      (incluirInactivos ? "" : " AND activo = 1") +
                      " ORDER BY nombre";

            var lista = new List<Servicio>();
            foreach (DataRow fila in AccesoDatos.Consultar(
                sql, AccesoDatos.Param("@texto", "%" + texto.Trim() + "%")).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        private static int Insertar(Servicio servicio)
        {
            const string sql = @"
                INSERT INTO Servicio (nombre, descripcion, precioBase, activo)
                VALUES (@nombre, @descripcion, @precioBase, @activo);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var id = AccesoDatos.Escalar(sql,
                AccesoDatos.Param("@nombre", servicio.Nombre),
                AccesoDatos.Param("@descripcion", servicio.Descripcion),
                AccesoDatos.Param("@precioBase", servicio.PrecioBase),
                AccesoDatos.Param("@activo", servicio.Activo));

            return System.Convert.ToInt32(id);
        }

        public static ResultadoOperacion Crear(Servicio servicio)
        {
            var validacion = servicio.Validar();
            if (!validacion.Exito) return validacion;

            servicio.IdServicio = Insertar(servicio);

            return ResultadoOperacion.Ok("Servicio creado.");
        }

        public static ResultadoOperacion Actualizar(Servicio servicio)
        {
            var validacion = servicio.Validar();
            if (!validacion.Exito) return validacion;

            if (ObtenerPorId(servicio.IdServicio) == null)
                return ResultadoOperacion.Error("El servicio no existe.");

            const string sql = @"
                UPDATE Servicio
                SET nombre = @nombre, descripcion = @descripcion, precioBase = @precioBase, activo = @activo
                WHERE idServicio = @idServicio";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@nombre", servicio.Nombre),
                AccesoDatos.Param("@descripcion", servicio.Descripcion),
                AccesoDatos.Param("@precioBase", servicio.PrecioBase),
                AccesoDatos.Param("@activo", servicio.Activo),
                AccesoDatos.Param("@idServicio", servicio.IdServicio));

            return ResultadoOperacion.Ok("Servicio actualizado.");
        }

        // Baja lógica: el servicio puede estar referenciado por órdenes de trabajo,
        // así que nunca se borra físicamente.
        public static ResultadoOperacion Desactivar(int idServicio)
        {
            var servicio = ObtenerPorId(idServicio);
            if (servicio == null)
                return ResultadoOperacion.Error("El servicio no existe.");

            if (!servicio.Activo)
                return ResultadoOperacion.Ok("El servicio ya estaba desactivado.");

            AccesoDatos.Ejecutar(
                "UPDATE Servicio SET activo = 0 WHERE idServicio = @idServicio",
                AccesoDatos.Param("@idServicio", idServicio));

            return ResultadoOperacion.Ok("Servicio desactivado.");
        }
    }
}
