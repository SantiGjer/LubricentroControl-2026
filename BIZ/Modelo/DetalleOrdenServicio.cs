namespace BIZ.Modelo
{
    // Línea de servicio realizado en una orden de trabajo. precioAplicado es un snapshot del
    // Servicio.precioBase vigente al momento de agregar la línea, no un lookup en caliente
    // (si el precio base cambia después, las líneas ya cargadas no se ven afectadas).
    public class DetalleOrdenServicio
    {
        public int IdDetalle { get; set; }
        public int IdOrden { get; set; }
        public int IdServicio { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioAplicado { get; set; }

        // Del JOIN con Servicio; solo para mostrar en la grilla, no se guarda.
        public string NombreServicio { get; set; }
    }
}
