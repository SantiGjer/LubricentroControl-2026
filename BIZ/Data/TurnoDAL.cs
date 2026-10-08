using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class TurnoDAL
    {
        private const string SelectBase = @"
            SELECT t.idTurno, t.idCliente, c.denominacion AS nombreCliente, c.tipoDocumento, c.numeroDocumento,
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
                TipoDocumento = AccesoDatos.LeerString(fila, "tipoDocumento"),
                NumeroDocumento = AccesoDatos.LeerString(fila, "numeroDocumento"),
                IdVehiculo = AccesoDatos.LeerIntNullable(fila, "idVehiculo"),
                Patente = AccesoDatos.LeerString(fila, "patente"),
                FechaSolicitud = AccesoDatos.LeerFecha(fila, "fechaSolicitud"),
                FechaHoraAsignada = AccesoDatos.LeerFecha(fila, "fechaHoraAsignada"),
                Estado = AccesoDatos.LeerString(fila, "estado"),
                Observaciones = AccesoDatos.LeerString(fila, "observaciones")
            };
        }

        // Todos los turnos, primero los de hoy (por hora), después los próximos (el más cercano
        // primero) y al final los pasados (el más reciente primero). El estado y el "cuándo" los
        // filtra la tabla en el navegador (opciones de Turnos.aspx).
        public static List<Turno> Listar()
        {
            const string sql = SelectBase + @"
                ORDER BY
                    CASE WHEN t.fechaHoraAsignada >= @hoy AND t.fechaHoraAsignada < @manana THEN 0
                         WHEN t.fechaHoraAsignada >= @manana THEN 1
                         ELSE 2 END,
                    CASE WHEN t.fechaHoraAsignada >= @hoy THEN t.fechaHoraAsignada END ASC,
                    t.fechaHoraAsignada DESC";

            var hoy = System.DateTime.Today;
            var lista = new List<Turno>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql,
                AccesoDatos.Param("@hoy", hoy),
                AccesoDatos.Param("@manana", hoy.AddDays(1))).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Turnos todavía vigentes (Solicitado o Confirmado) de un vehículo: mientras haya alguno,
        // no se le cambia el dueño (VehiculoDAL.CambiarDueno).
        public static int ContarVigentesPorVehiculo(int idVehiculo)
        {
            var cantidad = AccesoDatos.Escalar(
                @"SELECT COUNT(*) FROM Turno
                  WHERE idVehiculo = @idVehiculo AND estado IN (@estadoSolicitado, @estadoConfirmado)",
                AccesoDatos.Param("@idVehiculo", idVehiculo),
                AccesoDatos.Param("@estadoSolicitado", Turno.EstadoSolicitado),
                AccesoDatos.Param("@estadoConfirmado", Turno.EstadoConfirmado));

            return System.Convert.ToInt32(cantidad);
        }

        // Turnos de un día que siguen vigentes (Solicitado o Confirmado), por hora. Para el
        // Inicio: los Completados y Cancelados ya no necesitan atención. "dia" puede traer hora;
        // se compara contra el día completo (>= medianoche y < medianoche siguiente).
        public static List<Turno> ListarVigentesDelDia(System.DateTime dia)
        {
            const string sql = SelectBase + @"
                WHERE t.fechaHoraAsignada >= @desde AND t.fechaHoraAsignada < @hasta
                  AND t.estado IN (@estadoSolicitado, @estadoConfirmado)
                ORDER BY t.fechaHoraAsignada ASC";

            var desde = dia.Date;
            var lista = new List<Turno>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql,
                AccesoDatos.Param("@desde", desde),
                AccesoDatos.Param("@hasta", desde.AddDays(1)),
                AccesoDatos.Param("@estadoSolicitado", Turno.EstadoSolicitado),
                AccesoDatos.Param("@estadoConfirmado", Turno.EstadoConfirmado)).Rows)
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
