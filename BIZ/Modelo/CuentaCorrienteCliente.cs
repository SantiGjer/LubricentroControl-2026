using System;

namespace BIZ.Modelo
{
    // Movimiento de cuenta corriente de un cliente (Docs/Lubricentro_Requerimientos.md §6.8).
    // Mismo patrón que CuentaCorrienteProveedor/MovimientoStock: kardex con saldo acumulado
    // calculado y guardado en cada fila.
    public class CuentaCorrienteCliente
    {
        public const string TipoVenta = "Venta";
        public const string TipoPago = "Pago";
        public const string TipoAjuste = "Ajuste";

        public int IdMovimiento { get; set; }
        public int IdCliente { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoMovimiento { get; set; }
        public int? IdVenta { get; set; }
        public int? IdPago { get; set; }
        public decimal Debe { get; set; }
        public decimal Haber { get; set; }
        public decimal Saldo { get; set; }
        public string Descripcion { get; set; }
        public int? IdUsuario { get; set; }

        // Del JOIN; solo para mostrar, no se guardan.
        public string NombreCliente { get; set; }
        public string NombreUsuario { get; set; }
    }
}
