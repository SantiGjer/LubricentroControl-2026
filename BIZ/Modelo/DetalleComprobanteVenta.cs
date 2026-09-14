namespace BIZ.Modelo
{
    // Línea de un comprobante de venta — copiada 1 a 1 desde DetalleOrdenServicio/
    // DetalleOrdenInsumo al generar la venta (ComprobanteVentaDAL.GenerarDesdeOrden). precioUnitario
    // es el que ya estaba aplicado en la orden, no un lookup nuevo al catálogo.
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
    }
}
