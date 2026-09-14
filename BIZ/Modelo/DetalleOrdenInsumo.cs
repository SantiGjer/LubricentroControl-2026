namespace BIZ.Modelo
{
    // Línea de insumo utilizado en una orden de trabajo. precioUnitario es un snapshot del
    // Insumo.precioVenta vigente al momento de agregar la línea (mismo criterio que
    // DetalleOrdenServicio.precioAplicado). Agregar una línea descuenta stock automáticamente
    // (Requerimientos §6.4/§7) — ver DetalleOrdenInsumoDAL.Agregar.
    public class DetalleOrdenInsumo
    {
        public int IdDetalle { get; set; }
        public int IdOrden { get; set; }
        public int IdInsumo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }

        // Del JOIN con Insumo; solo para mostrar en la grilla, no se guarda.
        public string NombreInsumo { get; set; }
    }
}
