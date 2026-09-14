using System;

namespace BIZ.Modelo
{
    // Movimiento de cuenta corriente de un proveedor (Docs/Lubricentro_Requerimientos.md §6.8).
    // Mismo patrón que MovimientoStock: kardex con saldo acumulado calculado y guardado en cada
    // fila (stockResultante allá, saldo acá).
    public class CuentaCorrienteProveedor
    {
        public const string TipoCompra = "Compra";
        public const string TipoPago = "Pago";
        public const string TipoAjuste = "Ajuste";

        public int IdMovimiento { get; set; }
        public int IdProveedor { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoMovimiento { get; set; }
        public int? IdCompra { get; set; }
        public int? IdPago { get; set; }
        public decimal Debe { get; set; }
        public decimal Haber { get; set; }
        public decimal Saldo { get; set; }
        public string Descripcion { get; set; }
        public int? IdUsuario { get; set; }

        // Del JOIN; solo para mostrar, no se guardan.
        public string RazonSocial { get; set; }
        public string NombreUsuario { get; set; }
    }
}
