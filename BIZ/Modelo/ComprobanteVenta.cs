using System;

namespace BIZ.Modelo
{
    // Comprobante de venta (Docs/Lubricentro_Requerimientos.md §6.6). No se carga a mano: nace
    // automáticamente al cerrar una orden de trabajo (ver OrdenDeTrabajoDAL.Cerrar →
    // ComprobanteVentaDAL.GenerarDesdeOrden). Comprobante interno, sin validez fiscal.
    public class ComprobanteVenta
    {
        public int IdVenta { get; set; }
        public int IdOrden { get; set; }
        public int IdCliente { get; set; }
        public string NumeroComprobante { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuestos { get; set; }
        public decimal Total { get; set; }
        public decimal SaldoPendiente { get; set; }

        // Del JOIN con Cliente/OrdenDeTrabajo; solo para mostrar, no se guardan.
        public string NombreCliente { get; set; }
        public string Patente { get; set; }
    }
}
