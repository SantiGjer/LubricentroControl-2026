using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class VehiculoDAL
    {
        private const string SelectBase = @"
            SELECT v.idVehiculo, v.idCliente, c.nombre + ' ' + c.apellido AS nombreCliente,
                   v.patente, v.marca, v.modelo, v.anio, v.tipoCombustible, v.activo
            FROM Vehiculo v
            INNER JOIN Cliente c ON c.idCliente = v.idCliente";

        private static Vehiculo Mapear(DataRow fila)
        {
            return new Vehiculo
            {
                IdVehiculo = AccesoDatos.LeerInt(fila, "idVehiculo"),
                IdCliente = AccesoDatos.LeerInt(fila, "idCliente"),
                NombreCliente = AccesoDatos.LeerString(fila, "nombreCliente"),
                Patente = AccesoDatos.LeerString(fila, "patente"),
                Marca = AccesoDatos.LeerString(fila, "marca"),
                Modelo = AccesoDatos.LeerString(fila, "modelo"),
                Anio = AccesoDatos.LeerIntNullable(fila, "anio"),
                TipoCombustible = AccesoDatos.LeerString(fila, "tipoCombustible"),
                Activo = AccesoDatos.LeerBool(fila, "activo")
            };
        }

        public static List<Vehiculo> Listar(bool incluirInactivos = true)
        {
            var sql = SelectBase +
                      (incluirInactivos ? "" : " WHERE v.activo = 1") +
                      " ORDER BY c.apellido, c.nombre, v.patente";

            var lista = new List<Vehiculo>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static Vehiculo ObtenerPorId(int idVehiculo)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE v.idVehiculo = @idVehiculo",
                AccesoDatos.Param("@idVehiculo", idVehiculo));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        // Vehículos de un cliente puntual. La usa el botón "Ver vehículos" de Clientes.
        public static List<Vehiculo> ListarPorCliente(int idCliente, bool incluirInactivos = true)
        {
            var sql = SelectBase + " WHERE v.idCliente = @idCliente" +
                      (incluirInactivos ? "" : " AND v.activo = 1") +
                      " ORDER BY v.patente";

            var lista = new List<Vehiculo>();
            foreach (DataRow fila in AccesoDatos.Consultar(
                sql, AccesoDatos.Param("@idCliente", idCliente)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Buscador rápido por patente, marca o modelo (Requerimientos §6.2/§9.2).
        public static List<Vehiculo> Buscar(string texto, bool incluirInactivos = false)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Listar(incluirInactivos);

            var sql = SelectBase +
                      " WHERE (v.patente LIKE @texto OR v.marca LIKE @texto OR v.modelo LIKE @texto)" +
                      (incluirInactivos ? "" : " AND v.activo = 1") +
                      " ORDER BY c.apellido, c.nombre, v.patente";

            var lista = new List<Vehiculo>();
            foreach (DataRow fila in AccesoDatos.Consultar(
                sql, AccesoDatos.Param("@texto", "%" + texto.Trim() + "%")).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static bool ExistePatente(string patente, int idVehiculoExcluido = 0)
        {
            var cantidad = AccesoDatos.Escalar(
                "SELECT COUNT(*) FROM Vehiculo WHERE patente = @patente AND idVehiculo <> @id",
                AccesoDatos.Param("@patente", patente),
                AccesoDatos.Param("@id", idVehiculoExcluido));

            return System.Convert.ToInt32(cantidad) > 0;
        }

        private static int Insertar(Vehiculo vehiculo)
        {
            const string sql = @"
                INSERT INTO Vehiculo (idCliente, patente, marca, modelo, anio, tipoCombustible, activo)
                VALUES (@idCliente, @patente, @marca, @modelo, @anio, @tipoCombustible, @activo);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var id = AccesoDatos.Escalar(sql,
                AccesoDatos.Param("@idCliente", vehiculo.IdCliente),
                AccesoDatos.Param("@patente", vehiculo.Patente),
                AccesoDatos.Param("@marca", vehiculo.Marca),
                AccesoDatos.Param("@modelo", vehiculo.Modelo),
                AccesoDatos.Param("@anio", vehiculo.Anio),
                AccesoDatos.Param("@tipoCombustible", vehiculo.TipoCombustible),
                AccesoDatos.Param("@activo", vehiculo.Activo));

            return System.Convert.ToInt32(id);
        }

        public static ResultadoOperacion Crear(Vehiculo vehiculo)
        {
            var validacion = vehiculo.Validar();
            if (!validacion.Exito) return validacion;

            if (ClienteDAL.ObtenerPorId(vehiculo.IdCliente) == null)
                return ResultadoOperacion.Error("El cliente no existe.");

            if (ExistePatente(vehiculo.Patente))
                return ResultadoOperacion.Error("Ya existe un vehículo con esa patente.");

            vehiculo.IdVehiculo = Insertar(vehiculo);

            return ResultadoOperacion.Ok("Vehículo creado.");
        }

        public static ResultadoOperacion Actualizar(Vehiculo vehiculo)
        {
            var validacion = vehiculo.Validar();
            if (!validacion.Exito) return validacion;

            if (ObtenerPorId(vehiculo.IdVehiculo) == null)
                return ResultadoOperacion.Error("El vehículo no existe.");

            if (ClienteDAL.ObtenerPorId(vehiculo.IdCliente) == null)
                return ResultadoOperacion.Error("El cliente no existe.");

            if (ExistePatente(vehiculo.Patente, vehiculo.IdVehiculo))
                return ResultadoOperacion.Error("Ya existe otro vehículo con esa patente.");

            const string sql = @"
                UPDATE Vehiculo
                SET idCliente = @idCliente, patente = @patente, marca = @marca, modelo = @modelo,
                    anio = @anio, tipoCombustible = @tipoCombustible, activo = @activo
                WHERE idVehiculo = @idVehiculo";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@idCliente", vehiculo.IdCliente),
                AccesoDatos.Param("@patente", vehiculo.Patente),
                AccesoDatos.Param("@marca", vehiculo.Marca),
                AccesoDatos.Param("@modelo", vehiculo.Modelo),
                AccesoDatos.Param("@anio", vehiculo.Anio),
                AccesoDatos.Param("@tipoCombustible", vehiculo.TipoCombustible),
                AccesoDatos.Param("@activo", vehiculo.Activo),
                AccesoDatos.Param("@idVehiculo", vehiculo.IdVehiculo));

            return ResultadoOperacion.Ok("Vehículo actualizado.");
        }

        // Baja lógica: el vehículo puede estar referenciado por órdenes de trabajo,
        // así que nunca se borra físicamente.
        public static ResultadoOperacion Desactivar(int idVehiculo)
        {
            var vehiculo = ObtenerPorId(idVehiculo);
            if (vehiculo == null)
                return ResultadoOperacion.Error("El vehículo no existe.");

            if (!vehiculo.Activo)
                return ResultadoOperacion.Ok("El vehículo ya estaba desactivado.");

            AccesoDatos.Ejecutar(
                "UPDATE Vehiculo SET activo = 0 WHERE idVehiculo = @idVehiculo",
                AccesoDatos.Param("@idVehiculo", idVehiculo));

            return ResultadoOperacion.Ok("Vehículo desactivado.");
        }
    }
}
