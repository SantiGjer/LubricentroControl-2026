namespace BIZ.Modelo
{
    // Línea de servicio realizado en una orden de trabajo. precioAplicado es un snapshot del
    // precio vigente del servicio al momento de agregar la línea, no un lookup en caliente
    // (si el precio cambia después, las líneas ya cargadas no se ven afectadas).
    public class DetalleOrdenServicio
    {
        public int IdDetalle { get; set; }
        public int IdOrden { get; set; }
        public int IdServicio { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioAplicado { get; set; }

        // Del JOIN con Producto; solo para mostrar y para copiar el IVA a la venta, no se guardan.
        public string NombreServicio { get; set; }
        public string TipoIva { get; set; }
        public decimal AlicuotaIva { get; set; }

        public decimal Subtotal
        {
            get { return Cantidad * PrecioAplicado; }
        }
    }
}
