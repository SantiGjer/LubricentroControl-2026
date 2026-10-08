namespace BIZ.Modelo
{
    // Línea de un comprobante de venta — copiada 1 a 1 desde DetalleOrdenServicio/
    // DetalleOrdenInsumo al generar la venta (ComprobanteVentaDAL.GenerarDesdeOrden). precioUnitario
    // es el que ya estaba aplicado en la orden, no un lookup nuevo al catálogo. Precio y subtotal son
    // finales (con IVA); el IVA del producto se copia al generar la venta y importeIva es el IVA que
    // contiene el subtotal (Requerimientos §9.10).
    public class DetalleComprobanteVenta
    {
        public const string TipoServicio = "S";
        public const string TipoInsumo = "I";

        public int IdDetalle { get; set; }
        public int IdVenta { get; set; }
        public string TipoItem { get; set; }
        public int? IdServicio { get; set; }
        public int? IdInsumo { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public string TipoIva { get; set; }
        public decimal AlicuotaIva { get; set; }
        public decimal ImporteIva { get; set; }

        // Subtotal sin el IVA.
        public decimal Neto
        {
            get { return Subtotal - ImporteIva; }
        }

        // Precio unitario sin el IVA (lo que muestra una factura A, que discrimina el IVA).
        public decimal PrecioUnitarioNeto
        {
            get { return Cantidad == 0 ? 0 : System.Math.Round(Neto / Cantidad, 2, System.MidpointRounding.AwayFromZero); }
        }

        public string IvaDescripcion
        {
            get { return Iva.Describir(TipoIva, AlicuotaIva); }
        }
    }
}
