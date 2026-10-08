using System;

namespace BIZ.Modelo
{
    // Comprobante de venta (Docs/Lubricentro_Requerimientos.md §6.6). No se carga a mano: nace
    // automáticamente al cerrar una orden de trabajo (ver OrdenDeTrabajoDAL.Cerrar →
    // ComprobanteVentaDAL.GenerarDesdeOrden). Comprobante interno, sin validez fiscal; la factura
    // se genera aparte, a pedido (FacturaDAL.Emitir). Total = precios finales con IVA; Subtotal es
    // el neto e Impuestos el IVA contenido (§9.10).
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

        // Del JOIN con Cliente/OrdenDeTrabajo/Factura; solo para mostrar, no se guardan.
        public string NombreCliente { get; set; }
        public string TipoDocumentoCliente { get; set; }
        public string NumeroDocumentoCliente { get; set; }
        public string Patente { get; set; }

        // "B 00001-00000012" si ya se le emitió la factura; null si no.
        public string NumeroFactura { get; set; }

        public string DocumentoCliente
        {
            get { return Cliente.FormatearDocumento(TipoDocumentoCliente, NumeroDocumentoCliente); }
        }

        public bool Facturada
        {
            get { return NumeroFactura != null; }
        }
    }
}
