namespace BIZ.Negocio
{
    /// <summary>
    /// Resultado de una operación de negocio. Evita usar excepciones para
    /// comunicar validaciones esperables (mail duplicado, credenciales inválidas, etc.).
    /// </summary>
    public class ResultadoOperacion
    {
        public bool Exito { get; private set; }
        public string Mensaje { get; private set; }

        private ResultadoOperacion(bool exito, string mensaje)
        {
            Exito = exito;
            Mensaje = mensaje;
        }

        public static ResultadoOperacion Ok(string mensaje = null)
        {
            return new ResultadoOperacion(true, mensaje);
        }

        public static ResultadoOperacion Error(string mensaje)
        {
            return new ResultadoOperacion(false, mensaje);
        }
    }
}
