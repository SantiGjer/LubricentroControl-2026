namespace BIZ.Modelo
{
    public class Servicio
    {
        public int IdServicio { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal PrecioBase { get; set; }
        public bool Activo { get; set; }

        // Valida los campos obligatorios y les saca los espacios de los bordes.
        public ResultadoOperacion Validar()
        {
            if (string.IsNullOrWhiteSpace(Nombre))
                return ResultadoOperacion.Error("El nombre es obligatorio.");

            if (PrecioBase < 0)
                return ResultadoOperacion.Error("El precio base no puede ser negativo.");

            Nombre = Nombre.Trim();
            Descripcion = string.IsNullOrWhiteSpace(Descripcion) ? null : Descripcion.Trim();

            return ResultadoOperacion.Ok();
        }
    }
}
