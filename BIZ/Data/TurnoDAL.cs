using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class TurnoDAL
    {
        private const string SelectBase = @"
            SELECT t.idTurno, t.idCliente, c.nombre + ' ' + c.apellido AS nombreCliente, c.dni,
                   t.idVehiculo, v.patente,
                   t.fechaSolicitud, t.fechaHoraAsignada, t.estado, t.observaciones
            FROM Turno t
            INNER JOIN Cliente c ON c.idCliente = t.idCliente
            LEFT JOIN Vehiculo v ON v.idVehiculo = t.idVehiculo";

        private static Turno Mapear(DataRow fila)
        {
            return new Turno
            {
                IdTurno = AccesoDatos.LeerInt(fila, "idTurno"),
                IdCliente = AccesoDatos.LeerInt(fila, "idCliente"),
                NombreCliente = AccesoDatos.LeerString(fila, "nombreCliente"),
                Dni = AccesoDatos.LeerString(fila, "dni"),
                IdVehiculo = AccesoDatos.LeerIntNullable(fila, "idVehiculo"),
                Patente = AccesoDatos.LeerString(fila, "patente"),
                FechaSolicitud = AccesoDatos.LeerFecha(fila, "fechaSolicitud"),
                FechaHoraAsignada = AccesoDatos.LeerFecha(fila, "fechaHoraAsignada"),
                Estado = AccesoDatos.LeerString(fila, "estado"),
                Observaciones = AccesoDatos.LeerString(fila, "observaciones")
            };
        }

        public static List<Turno> Listar(string estado = null)
        {
            var sql = SelectBase +
                      (string.IsNullOrWhiteSpace(estado) ? "" : " WHERE t.estado = @estado") +
                      " ORDER BY t.fechaHoraAsignada ASC";

            var lista = new List<Turno>();
            var tabla = string.IsNullOrWhiteSpace(estado)
                ? AccesoDatos.Consultar(sql)
                : AccesoDatos.Consultar(sql, AccesoDatos.Param("@estado", estado));

            foreach (DataRow fila in tabla.Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static Turno ObtenerPorId(int idTurno)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE t.idTurno = @idTurno",
                AccesoDatos.Param("@idTurno", idTurno));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        // Buscador rápido por nombre/apellido/DNI del cliente, con filtro opcional de estado.
        public static List<Turno> Buscar(string texto, string estado = null)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Listar(estado);

            var sql = SelectBase +
                      " WHERE (c.nombre LIKE @texto OR c.apellido LIKE @texto OR c.dni LIKE @texto)" +
                      (string.IsNullOrWhiteSpace(estado) ? "" : " AND t.estado = @estado") +
                      " ORDER BY t.fechaHoraAsignada ASC";

            var lista = new List<Turno>();
            var tabla = string.IsNullOrWhiteSpace(estado)
                ? AccesoDatos.Consultar(sql, AccesoDatos.Param("@texto", "%" + texto.Trim() + "%"))
                : AccesoDatos.Consultar(sql,
                    AccesoDatos.Param("@texto", "%" + texto.Trim() + "%"),
                    AccesoDatos.Param("@estado", estado));

            foreach (DataRow fila in tabla.Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Turnos vigentes (Solicitado/Confirmado) de un cliente, para poblar el selector de
        // turno de OrdenesDeTrabajo.aspx — no tiene sentido enlazar una orden a un turno ya
        // Completado o Cancelado.
        public static List<Turno> ListarPorCliente(int idCliente)
        {
            const string sql = SelectBase + @"
                WHERE t.idCliente = @idCliente
                  AND t.estado IN (@estadoSolicitado, @estadoConfirmado)
                ORDER BY t.fechaHoraAsignada DESC";

            var lista = new List<Turno>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql,
                AccesoDatos.Param("@idCliente", idCliente),
                AccesoDatos.Param("@estadoSolicitado", Turno.EstadoSolicitado),
                AccesoDatos.Param("@estadoConfirmado", Turno.EstadoConfirmado)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Cliente activo, y si vino vehículo, que exista, esté activo y sea suyo.
        // Comparte criterio con VehiculoDAL.Crear/Actualizar (existencia va a la base, no al Modelo).
        private static ResultadoOperacion ValidarReferencias(Turno turno)
        {
            var cliente = ClienteDAL.ObtenerPorId(turno.IdCliente);
            if (cliente == null)
                return ResultadoOperacion.Error("El cliente no existe.");
            if (!cliente.Activo)
                return ResultadoOperacion.Error("El cliente está dado de baja.");

            if (turno.IdVehiculo.HasValue)
            {
                var vehiculo = VehiculoDAL.ObtenerPorId(turno.IdVehiculo.Value);
                if (vehiculo == null)
                    return ResultadoOperacion.Error("El vehículo no existe.");
                if (!vehiculo.Activo)
                    return ResultadoOperacion.Error("El vehículo está dado de baja.");
                if (vehiculo.IdCliente != turno.IdCliente)
                    return ResultadoOperacion.Error("El vehículo seleccionado no pertenece a ese cliente.");
            }

            return ResultadoOperacion.Ok();
        }

        public static ResultadoOperacion Crear(Turno turno)
        {
            // El estado de un turno nuevo siempre arranca en Solicitado, sin importar
            // qué haya quedado seleccionado en el formulario.
            turno.Estado = Turno.EstadoSolicitado;

            var validacion = turno.Validar();
            if (!validacion.Exito) return validacion;

            var referencias = ValidarReferencias(turno);
            if (!referencias.Exito) return referencias;

            const string sql = @"
                INSERT INTO Turno (idCliente, idVehiculo, fechaHoraAsignada, estado, observaciones)
                VALUES (@idCliente, @idVehiculo, @fechaHoraAsignada, @estado, @observaciones);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var id = AccesoDatos.Escalar(sql,
                AccesoDatos.Param("@idCliente", turno.IdCliente),
                AccesoDatos.Param("@idVehiculo", turno.IdVehiculo),
                AccesoDatos.Param("@fechaHoraAsignada", turno.FechaHoraAsignada),
                AccesoDatos.Param("@estado", turno.Estado),
                AccesoDatos.Param("@observaciones", turno.Observaciones));

            turno.IdTurno = System.Convert.ToInt32(id);

            return ResultadoOperacion.Ok("Turno creado.");
        }

        public static ResultadoOperacion Actualizar(Turno turno)
        {
            var validacion = turno.Validar();
            if (!validacion.Exito) return validacion;

            if (ObtenerPorId(turno.IdTurno) == null)
                return ResultadoOperacion.Error("El turno no existe.");

            var referencias = ValidarReferencias(turno);
            if (!referencias.Exito) return referencias;

            const string sql = @"
                UPDATE Turno
                SET idVehiculo = @idVehiculo, fechaHoraAsignada = @fechaHoraAsignada,
                    estado = @estado, observaciones = @observaciones
                WHERE idTurno = @idTurno";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@idVehiculo", turno.IdVehiculo),
                AccesoDatos.Param("@fechaHoraAsignada", turno.FechaHoraAsignada),
                AccesoDatos.Param("@estado", turno.Estado),
                AccesoDatos.Param("@observaciones", turno.Observaciones),
                AccesoDatos.Param("@idTurno", turno.IdTurno));

            return ResultadoOperacion.Ok("Turno actualizado.");
        }
    }
}
