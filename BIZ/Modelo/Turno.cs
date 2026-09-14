using System;

namespace BIZ.Modelo
{
    // Turno de agenda (Docs/Lubricentro_Requerimientos.md §6.3). Lo carga solo el personal,
    // no hay portal público. El vínculo con Vehiculo es opcional (walk-in sin vehículo asociado
    // todavía) y el vínculo con OrdenDeTrabajo, cuando exista Fase 3/Órdenes, también es opcional.
    public class Turno
    {
        public const string EstadoSolicitado = "Solicitado";
        public const string EstadoConfirmado = "Confirmado";
        public const string EstadoCompletado = "Completado";
        public const string EstadoCancelado = "Cancelado";

        public static readonly string[] Estados =
            { EstadoSolicitado, EstadoConfirmado, EstadoCompletado, EstadoCancelado };

        public int IdTurno { get; set; }
        public int IdCliente { get; set; }
        public int? IdVehiculo { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public DateTime FechaHoraAsignada { get; set; }
        public string Estado { get; set; }
        public string Observaciones { get; set; }

        // Del JOIN con Cliente/Vehiculo; solo para mostrar en la grilla, no se guardan.
        public string NombreCliente { get; set; }
        public string Dni { get; set; }
        public string Patente { get; set; }

        // Valida los campos obligatorios y le saca los espacios a observaciones.
        // No valida existencia/estado de cliente y vehículo: eso necesita ir a la base,
        // así que queda en TurnoDAL (mismo criterio que Vehiculo.Validar()/VehiculoDAL).
        public ResultadoOperacion Validar()
        {
            if (IdCliente <= 0)
                return ResultadoOperacion.Error("Seleccioná el cliente del turno.");

            if (FechaHoraAsignada == default(DateTime))
                return ResultadoOperacion.Error("La fecha y hora del turno son obligatorias.");

            if (string.IsNullOrWhiteSpace(Estado) || Array.IndexOf(Estados, Estado) < 0)
                return ResultadoOperacion.Error("El estado del turno no es válido.");

            Observaciones = string.IsNullOrWhiteSpace(Observaciones) ? null : Observaciones.Trim();

            return ResultadoOperacion.Ok();
        }
    }
}
