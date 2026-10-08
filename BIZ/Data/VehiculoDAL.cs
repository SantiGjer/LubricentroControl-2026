using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class VehiculoDAL
    {
        private const string SelectBase = @"
            SELECT v.idVehiculo, v.idCliente, c.denominacion AS nombreCliente, c.tipoDocumento, c.numeroDocumento,
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
                TipoDocumentoCliente = AccesoDatos.LeerString(fila, "tipoDocumento"),
                NumeroDocumentoCliente = AccesoDatos.LeerString(fila, "numeroDocumento"),
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
                      " ORDER BY c.denominacion, v.patente";

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

        // Vehículos de un cliente puntual (selectores de vehículo de Turnos y Órdenes).
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

        // No cambia el dueño: eso es CambiarDueno, que valida que el vehículo no tenga nada en
        // curso a nombre del dueño actual.
        public static ResultadoOperacion Actualizar(Vehiculo vehiculo)
        {
            var existente = ObtenerPorId(vehiculo.IdVehiculo);
            if (existente == null)
                return ResultadoOperacion.Error("El vehículo no existe.");

            vehiculo.IdCliente = existente.IdCliente;

            var validacion = vehiculo.Validar();
            if (!validacion.Exito) return validacion;

            if (ExistePatente(vehiculo.Patente, vehiculo.IdVehiculo))
                return ResultadoOperacion.Error("Ya existe otro vehículo con esa patente.");

            const string sql = @"
                UPDATE Vehiculo
                SET patente = @patente, marca = @marca, modelo = @modelo,
                    anio = @anio, tipoCombustible = @tipoCombustible, activo = @activo
                WHERE idVehiculo = @idVehiculo";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@patente", vehiculo.Patente),
                AccesoDatos.Param("@marca", vehiculo.Marca),
                AccesoDatos.Param("@modelo", vehiculo.Modelo),
                AccesoDatos.Param("@anio", vehiculo.Anio),
                AccesoDatos.Param("@tipoCombustible", vehiculo.TipoCombustible),
                AccesoDatos.Param("@activo", vehiculo.Activo),
                AccesoDatos.Param("@idVehiculo", vehiculo.IdVehiculo));

            return ResultadoOperacion.Ok("Vehículo actualizado.");
        }

        // Transfiere el vehículo a otro cliente (Requerimientos §9.12). El historial no se mueve:
        // cada turno, orden y venta guarda su propio cliente, así que lo hecho para el dueño
        // anterior queda a su nombre. Por eso no se permite mientras el vehículo tenga una orden en
        // el taller o un turno vigente: quedarían a nombre de un cliente que ya no es el dueño
        // (y TurnoDAL/OrdenDeTrabajoDAL rechazarían editarlos). Hay que cerrarlos o cancelarlos antes.
        public static ResultadoOperacion CambiarDueno(int idVehiculo, int idClienteNuevo)
        {
            var vehiculo = ObtenerPorId(idVehiculo);
            if (vehiculo == null)
                return ResultadoOperacion.Error("El vehículo no existe.");

            if (!vehiculo.Activo)
                return ResultadoOperacion.Error("El vehículo está dado de baja: reactivalo antes de cambiarle el dueño.");

            var cliente = ClienteDAL.ObtenerPorId(idClienteNuevo);
            if (cliente == null)
                return ResultadoOperacion.Error("Seleccioná el nuevo dueño.");

            if (!cliente.Activo)
                return ResultadoOperacion.Error("El cliente elegido está dado de baja.");

            if (cliente.IdCliente == vehiculo.IdCliente)
                return ResultadoOperacion.Error("Ese cliente ya es el dueño del vehículo.");

            var ordenes = OrdenDeTrabajoDAL.ContarEnCursoPorVehiculo(idVehiculo);
            if (ordenes > 0)
                return ResultadoOperacion.Error("El vehículo tiene " +
                    (ordenes == 1 ? "una orden de trabajo en curso" : ordenes + " órdenes de trabajo en curso") +
                    " a nombre de " + vehiculo.NombreCliente + ". Cerrala o cancelala antes de cambiar el dueño.");

            var turnos = TurnoDAL.ContarVigentesPorVehiculo(idVehiculo);
            if (turnos > 0)
                return ResultadoOperacion.Error("El vehículo tiene " +
                    (turnos == 1 ? "un turno pendiente" : turnos + " turnos pendientes") +
                    " a nombre de " + vehiculo.NombreCliente + ". Completalo o cancelalo antes de cambiar el dueño.");

            AccesoDatos.Ejecutar(
                "UPDATE Vehiculo SET idCliente = @idCliente WHERE idVehiculo = @idVehiculo",
                AccesoDatos.Param("@idCliente", idClienteNuevo),
                AccesoDatos.Param("@idVehiculo", idVehiculo));

            return ResultadoOperacion.Ok("El vehículo " + vehiculo.Patente + " pasó a nombre de " + cliente.Denominacion + ".");
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

        // Deshace la baja lógica: el vehículo vuelve con el mismo dueño y sus órdenes intactas.
        public static ResultadoOperacion Reactivar(int idVehiculo)
        {
            var vehiculo = ObtenerPorId(idVehiculo);
            if (vehiculo == null)
                return ResultadoOperacion.Error("El vehículo no existe.");

            if (vehiculo.Activo)
                return ResultadoOperacion.Ok("El vehículo ya estaba activo.");

            AccesoDatos.Ejecutar(
                "UPDATE Vehiculo SET activo = 1 WHERE idVehiculo = @idVehiculo",
                AccesoDatos.Param("@idVehiculo", idVehiculo));

            return ResultadoOperacion.Ok("Vehículo reactivado.");
        }
    }
}
