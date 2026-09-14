using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class OrdenDeTrabajoDAL
    {
        private const string SelectBase = @"
            SELECT o.idOrden, o.idTurno, o.idCliente, c.nombre + ' ' + c.apellido AS nombreCliente,
                   c.dni, o.idVehiculo, v.patente, o.idUsuario,
                   o.fecha, o.kilometraje, o.observaciones, o.estado
            FROM OrdenDeTrabajo o
            INNER JOIN Cliente c ON c.idCliente = o.idCliente
            INNER JOIN Vehiculo v ON v.idVehiculo = o.idVehiculo";

        private static OrdenDeTrabajo Mapear(DataRow fila)
        {
            return new OrdenDeTrabajo
            {
                IdOrden = AccesoDatos.LeerInt(fila, "idOrden"),
                IdTurno = AccesoDatos.LeerIntNullable(fila, "idTurno"),
                IdCliente = AccesoDatos.LeerInt(fila, "idCliente"),
                NombreCliente = AccesoDatos.LeerString(fila, "nombreCliente"),
                Dni = AccesoDatos.LeerString(fila, "dni"),
                IdVehiculo = AccesoDatos.LeerInt(fila, "idVehiculo"),
                Patente = AccesoDatos.LeerString(fila, "patente"),
                IdUsuario = AccesoDatos.LeerInt(fila, "idUsuario"),
                Fecha = AccesoDatos.LeerFecha(fila, "fecha"),
                Kilometraje = AccesoDatos.LeerIntNullable(fila, "kilometraje"),
                Observaciones = AccesoDatos.LeerString(fila, "observaciones"),
                Estado = AccesoDatos.LeerString(fila, "estado")
            };
        }

        public static List<OrdenDeTrabajo> Listar(string estado = null)
        {
            var sql = SelectBase +
                      (string.IsNullOrWhiteSpace(estado) ? "" : " WHERE o.estado = @estado") +
                      " ORDER BY o.fecha DESC";

            var lista = new List<OrdenDeTrabajo>();
            var tabla = string.IsNullOrWhiteSpace(estado)
                ? AccesoDatos.Consultar(sql)
                : AccesoDatos.Consultar(sql, AccesoDatos.Param("@estado", estado));

            foreach (DataRow fila in tabla.Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static OrdenDeTrabajo ObtenerPorId(int idOrden)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE o.idOrden = @idOrden",
                AccesoDatos.Param("@idOrden", idOrden));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        // Buscador por nombre/apellido/DNI del cliente o por patente del vehículo, con filtro
        // opcional de estado.
        public static List<OrdenDeTrabajo> Buscar(string texto, string estado = null)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Listar(estado);

            var sql = SelectBase +
                      " WHERE (c.nombre LIKE @texto OR c.apellido LIKE @texto OR c.dni LIKE @texto OR v.patente LIKE @texto)" +
                      (string.IsNullOrWhiteSpace(estado) ? "" : " AND o.estado = @estado") +
                      " ORDER BY o.fecha DESC";

            var lista = new List<OrdenDeTrabajo>();
            var tabla = string.IsNullOrWhiteSpace(estado)
                ? AccesoDatos.Consultar(sql, AccesoDatos.Param("@texto", "%" + texto.Trim() + "%"))
                : AccesoDatos.Consultar(sql,
                    AccesoDatos.Param("@texto", "%" + texto.Trim() + "%"),
                    AccesoDatos.Param("@estado", estado));

            foreach (DataRow fila in tabla.Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Cliente activo; vehículo activo y de ese cliente; si vino turno, que exista y sea
        // de ese cliente. Mismo criterio que TurnoDAL.ValidarReferencias.
        private static ResultadoOperacion ValidarReferencias(OrdenDeTrabajo orden)
        {
            var cliente = ClienteDAL.ObtenerPorId(orden.IdCliente);
            if (cliente == null)
                return ResultadoOperacion.Error("El cliente no existe.");
            if (!cliente.Activo)
                return ResultadoOperacion.Error("El cliente está dado de baja.");

            var vehiculo = VehiculoDAL.ObtenerPorId(orden.IdVehiculo);
            if (vehiculo == null)
                return ResultadoOperacion.Error("El vehículo no existe.");
            if (!vehiculo.Activo)
                return ResultadoOperacion.Error("El vehículo está dado de baja.");
            if (vehiculo.IdCliente != orden.IdCliente)
                return ResultadoOperacion.Error("El vehículo seleccionado no pertenece a ese cliente.");

            if (orden.IdTurno.HasValue)
            {
                var turno = TurnoDAL.ObtenerPorId(orden.IdTurno.Value);
                if (turno == null)
                    return ResultadoOperacion.Error("El turno no existe.");
                if (turno.IdCliente != orden.IdCliente)
                    return ResultadoOperacion.Error("El turno seleccionado no pertenece a ese cliente.");
            }

            return ResultadoOperacion.Ok();
        }

        public static ResultadoOperacion Crear(OrdenDeTrabajo orden)
        {
            // Toda orden nueva arranca Abierta, sin importar qué haya quedado seleccionado
            // en el formulario (mismo criterio que Turno con Solicitado).
            orden.Estado = OrdenDeTrabajo.EstadoAbierta;

            var validacion = orden.Validar();
            if (!validacion.Exito) return validacion;

            var referencias = ValidarReferencias(orden);
            if (!referencias.Exito) return referencias;

            const string sql = @"
                INSERT INTO OrdenDeTrabajo (idTurno, idCliente, idVehiculo, idUsuario, kilometraje, observaciones, estado)
                VALUES (@idTurno, @idCliente, @idVehiculo, @idUsuario, @kilometraje, @observaciones, @estado);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var id = AccesoDatos.Escalar(sql,
                AccesoDatos.Param("@idTurno", orden.IdTurno),
                AccesoDatos.Param("@idCliente", orden.IdCliente),
                AccesoDatos.Param("@idVehiculo", orden.IdVehiculo),
                AccesoDatos.Param("@idUsuario", orden.IdUsuario),
                AccesoDatos.Param("@kilometraje", orden.Kilometraje),
                AccesoDatos.Param("@observaciones", orden.Observaciones),
                AccesoDatos.Param("@estado", orden.Estado));

            orden.IdOrden = System.Convert.ToInt32(id);

            return ResultadoOperacion.Ok("Orden creada.");
        }

        // Solo kilometraje, observaciones y estado (Abierta/En proceso) — cliente, vehículo y
        // turno quedan fijos desde el alta (Requerimientos: la orden es sobre un cliente/vehículo
        // concreto). Cerrar y Cancelar una orden son OrdenDeTrabajoDAL.Cerrar/Cancelar, no pasan
        // por acá — las dos tienen un efecto colateral (generar la venta, reponer stock) que no
        // tiene que poder dispararse solo por elegir un valor en un dropdown genérico.
        public static ResultadoOperacion Actualizar(OrdenDeTrabajo orden)
        {
            var existente = ObtenerPorId(orden.IdOrden);
            if (existente == null)
                return ResultadoOperacion.Error("La orden no existe.");

            if (existente.Estado == OrdenDeTrabajo.EstadoCerrada || existente.Estado == OrdenDeTrabajo.EstadoCancelada)
                return ResultadoOperacion.Error("Una orden " + existente.Estado.ToLowerInvariant() + " no se puede modificar.");

            orden.IdCliente = existente.IdCliente;
            orden.IdVehiculo = existente.IdVehiculo;
            orden.IdTurno = existente.IdTurno;

            var validacion = orden.Validar();
            if (!validacion.Exito) return validacion;

            const string sql = @"
                UPDATE OrdenDeTrabajo
                SET kilometraje = @kilometraje, observaciones = @observaciones, estado = @estado
                WHERE idOrden = @idOrden";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@kilometraje", orden.Kilometraje),
                AccesoDatos.Param("@observaciones", orden.Observaciones),
                AccesoDatos.Param("@estado", orden.Estado),
                AccesoDatos.Param("@idOrden", orden.IdOrden));

            return ResultadoOperacion.Ok("Orden actualizada.");
        }

        // Cancela la orden y repone el stock de todos los insumos cargados (no llegaron a
        // usarse — Requerimientos §7.2). Solo se puede cancelar una orden Abierta o En proceso.
        public static ResultadoOperacion Cancelar(int idOrden, int idUsuario)
        {
            var orden = ObtenerPorId(idOrden);
            if (orden == null)
                return ResultadoOperacion.Error("La orden no existe.");

            if (orden.Estado == OrdenDeTrabajo.EstadoCerrada || orden.Estado == OrdenDeTrabajo.EstadoCancelada)
                return ResultadoOperacion.Error("Una orden " + orden.Estado.ToLowerInvariant() + " no se puede cancelar.");

            foreach (var detalle in DetalleOrdenInsumoDAL.ListarPorOrden(idOrden))
            {
                var reposicion = MovimientoStockDAL.Registrar(
                    detalle.IdInsumo, MovimientoStock.TipoCancelacionOrden,
                    entrada: detalle.Cantidad, salida: 0,
                    idCompra: null, idOrden: idOrden, idUsuario: idUsuario,
                    descripcion: "Reposición por cancelación de orden #" + idOrden);

                if (!reposicion.Exito) return reposicion;
            }

            AccesoDatos.Ejecutar(
                "UPDATE OrdenDeTrabajo SET estado = @estado WHERE idOrden = @idOrden",
                AccesoDatos.Param("@estado", OrdenDeTrabajo.EstadoCancelada),
                AccesoDatos.Param("@idOrden", idOrden));

            return ResultadoOperacion.Ok("Orden cancelada. Se repuso el stock de los insumos cargados.");
        }

        // Cierra la orden y genera automáticamente el comprobante de venta (Requerimientos §6.6)
        // — por eso Cerrada salió de EstadosEditables y pasa a tener su propio botón, igual que
        // Cancelar. Solo se puede cerrar una orden Abierta o En proceso.
        public static ResultadoOperacion Cerrar(int idOrden)
        {
            var orden = ObtenerPorId(idOrden);
            if (orden == null)
                return ResultadoOperacion.Error("La orden no existe.");

            if (orden.Estado == OrdenDeTrabajo.EstadoCerrada || orden.Estado == OrdenDeTrabajo.EstadoCancelada)
                return ResultadoOperacion.Error("Una orden " + orden.Estado.ToLowerInvariant() + " no se puede cerrar.");

            var venta = ComprobanteVentaDAL.GenerarDesdeOrden(idOrden);
            if (!venta.Exito) return venta;

            AccesoDatos.Ejecutar(
                "UPDATE OrdenDeTrabajo SET estado = @estado WHERE idOrden = @idOrden",
                AccesoDatos.Param("@estado", OrdenDeTrabajo.EstadoCerrada),
                AccesoDatos.Param("@idOrden", idOrden));

            return ResultadoOperacion.Ok("Orden cerrada. Se generó la venta correspondiente.");
        }
    }
}
