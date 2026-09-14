using System;

namespace BIZ.Modelo
{
    // Pago de cliente o de proveedor (Docs/Lubricentro_Requerimientos.md §6.7). Puede imputarse
    // a un comprobante puntual (IdVenta para tipo Cliente, IdCompra para tipo Proveedor) o
    // quedar "a cuenta general" (los dos en null) — en los dos casos genera un movimiento de
    // cuenta corriente. No se edita ni se borra una vez cargado.
    public class Pago
    {
        public const string TipoCliente = "C";
        public const string TipoProveedor = "P";

        public static readonly string[] MediosPago = { "Efectivo", "Transferencia", "Tarjeta" };

        public int IdPago { get; set; }
        public string Tipo { get; set; }
        public int? IdCliente { get; set; }
        public int? IdProveedor { get; set; }
        public int? IdVenta { get; set; }
        public int? IdCompra { get; set; }
        public int IdUsuario { get; set; }
        public DateTime Fecha { get; set; }
        public string MedioPago { get; set; }
        public decimal Monto { get; set; }
        public string Observaciones { get; set; }

        // Del JOIN; solo para mostrar, no se guardan.
        public string NombreCliente { get; set; }
        public string RazonSocial { get; set; }

        // Valida los campos propios. La existencia del comprobante puntual (si vino) y que su
        // saldoPendiente alcance necesitan ir a la base — quedan en PagoDAL.
        public ResultadoOperacion Validar()
        {
            if (Tipo != TipoCliente && Tipo != TipoProveedor)
                return ResultadoOperacion.Error("El tipo de pago no es válido.");

            if (Tipo == TipoCliente && (!IdCliente.HasValue || IdCliente.Value <= 0))
                return ResultadoOperacion.Error("Seleccioná el cliente del pago.");

            if (Tipo == TipoProveedor && (!IdProveedor.HasValue || IdProveedor.Value <= 0))
                return ResultadoOperacion.Error("Seleccioná el proveedor del pago.");

            if (string.IsNullOrWhiteSpace(MedioPago) || Array.IndexOf(MediosPago, MedioPago) < 0)
                return ResultadoOperacion.Error("Seleccioná el medio de pago.");

            if (Monto <= 0)
                return ResultadoOperacion.Error("El monto debe ser mayor a cero.");

            Observaciones = string.IsNullOrWhiteSpace(Observaciones) ? null : Observaciones.Trim();

            return ResultadoOperacion.Ok();
        }
    }
}
