using System;

namespace BIZ.Modelo
{
    // Comprobante de compra a un proveedor (Docs/Lubricentro_Requerimientos.md §6.5). A
    // diferencia de OrdenDeTrabajo, no tiene franja de alta progresiva: las líneas se arman en
    // memoria en la pantalla y se guardan todas juntas con la cabecera (ComprobanteCompraDAL.Crear).
    public class ComprobanteCompra
    {
        public const string CondicionContado = "Contado";
        public const string CondicionCuentaCorriente = "Cuenta corriente";

        public static readonly string[] CondicionesPago = { CondicionContado, CondicionCuentaCorriente };

        // Solo tiene sentido cuando la condición de pago es Contado (Requerimientos §9.5) —
        // mismo dominio que Pago.MediosPago.
        public static readonly string[] MediosPago = { "Efectivo", "Transferencia", "Tarjeta" };

        public int IdCompra { get; set; }
        public int IdProveedor { get; set; }
        public string NumeroComprobante { get; set; }
        public DateTime Fecha { get; set; }
        public string CondicionPago { get; set; }
        public string MedioPago { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuestos { get; set; }
        public decimal Total { get; set; }
        public decimal SaldoPendiente { get; set; }

        // Del JOIN con Proveedor; solo para mostrar, no se guarda.
        public string RazonSocial { get; set; }

        // Valida los campos de cabecera. No valida existencia/estado del proveedor ni las
        // líneas: eso necesita ir a la base, queda en ComprobanteCompraDAL.
        public ResultadoOperacion Validar()
        {
            if (IdProveedor <= 0)
                return ResultadoOperacion.Error("Seleccioná el proveedor de la compra.");

            if (Array.IndexOf(CondicionesPago, CondicionPago) < 0)
                return ResultadoOperacion.Error("La condición de pago no es válida.");

            if (CondicionPago == CondicionContado)
            {
                if (string.IsNullOrWhiteSpace(MedioPago) || Array.IndexOf(MediosPago, MedioPago) < 0)
                    return ResultadoOperacion.Error("Seleccioná el medio de pago de la compra al contado.");
            }
            else
            {
                // Una compra a cuenta corriente todavía no tiene medio de pago: recién se sabe
                // cuando se registre el Pago que la cancele.
                MedioPago = null;
            }

            if (Impuestos < 0)
                return ResultadoOperacion.Error("Los impuestos no pueden ser negativos.");

            return ResultadoOperacion.Ok();
        }
    }
}
