namespace BIZ.Modelo
{
    // Línea de insumo utilizado en una orden de trabajo. precioUnitario es un snapshot del precio
    // del producto vigente al momento de agregar la línea (mismo criterio que
    // DetalleOrdenServicio.precioAplicado). Agregar una línea descuenta stock automáticamente
    // (Requerimientos §6.4/§7) — ver DetalleOrdenInsumoDAL.Agregar.
    public class DetalleOrdenInsumo
    {
        public int IdDetalle { get; set; }
        public int IdOrden { get; set; }
        public int IdInsumo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }

        // Del JOIN con Producto; solo para mostrar y para copiar el IVA a la venta, no se guardan.
        public string NombreInsumo { get; set; }
        public string TipoIva { get; set; }
        public decimal AlicuotaIva { get; set; }

        public decimal Subtotal
        {
            get { return Cantidad * PrecioUnitario; }
        }
    }
}
