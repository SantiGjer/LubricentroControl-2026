using System;

namespace BIZ.Modelo
{
    // Orden de trabajo (Docs/Lubricentro_Requerimientos.md §6.4). Nace con turno previo
    // (idTurno) o walk-in (idTurno null); a diferencia de Turno, acá el vehículo es obligatorio
    // (la orden es sobre un vehículo concreto).
    public class OrdenDeTrabajo
    {
        public const string EstadoAbierta = "Abierta";
        public const string EstadoEnProceso = "En proceso";
        public const string EstadoCerrada = "Cerrada";
        public const string EstadoCancelada = "Cancelada";

        // Los que ofrece el ddlEstado del formulario. Cerrada tampoco está desde Fase 4: cerrar
        // una orden genera automáticamente la venta (Requerimientos §6.6), así que se llega solo
        // por OrdenDeTrabajoDAL.Cerrar — mismo criterio que Cancelada/Cancelar, que repone el
        // stock de los insumos cargados.
        public static readonly string[] EstadosEditables =
            { EstadoAbierta, EstadoEnProceso };

        // Los 4 estados posibles, para el filtro de búsqueda (que sí necesita poder
        // encontrar órdenes Canceladas).
        public static readonly string[] Estados =
            { EstadoAbierta, EstadoEnProceso, EstadoCerrada, EstadoCancelada };

        public int IdOrden { get; set; }
        public int? IdTurno { get; set; }
        public int IdCliente { get; set; }
        public int IdVehiculo { get; set; }
        public int IdUsuario { get; set; }
        public DateTime Fecha { get; set; }
        public int? Kilometraje { get; set; }
        public string Observaciones { get; set; }
        public string Estado { get; set; }

        // Del JOIN con Cliente/Vehiculo; solo para mostrar en la grilla/formulario.
        public string NombreCliente { get; set; }
        public string Dni { get; set; }
        public string Patente { get; set; }

        // Valida los campos obligatorios. No valida existencia/estado de cliente, vehículo ni
        // turno: eso necesita ir a la base, queda en OrdenDeTrabajoDAL (mismo criterio que Turno).
        public ResultadoOperacion Validar()
        {
            if (IdCliente <= 0)
                return ResultadoOperacion.Error("Seleccioná el cliente de la orden.");

            if (IdVehiculo <= 0)
                return ResultadoOperacion.Error("Seleccioná el vehículo de la orden.");

            if (string.IsNullOrWhiteSpace(Estado) || Array.IndexOf(EstadosEditables, Estado) < 0)
                return ResultadoOperacion.Error("El estado de la orden no es válido.");

            if (Kilometraje.HasValue && Kilometraje.Value < 0)
                return ResultadoOperacion.Error("El kilometraje no puede ser negativo.");

            Observaciones = string.IsNullOrWhiteSpace(Observaciones) ? null : Observaciones.Trim();

            return ResultadoOperacion.Ok();
        }
    }
}
