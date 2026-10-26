using System.Data;

namespace BIZ.Modelo
{
    // Lo que el asistente le devuelve a la pantalla: el texto ya armado (por C#, nunca por la IA)
    // y, si la consulta es un ranking, la tabla para mostrar debajo.
    public class RespuestaAsistente
    {
        // false = no se pudo responder (IA caída, base caída); la pantalla lo muestra como aviso.
        public bool Exito { get; private set; }
        public string Texto { get; private set; }
        public DataTable Tabla { get; private set; }

        private RespuestaAsistente(bool exito, string texto, DataTable tabla)
        {
            Exito = exito;
            Texto = texto;
            Tabla = tabla;
        }

        public static RespuestaAsistente Ok(string texto, DataTable tabla = null)
        {
            return new RespuestaAsistente(true, texto, tabla);
        }

        public static RespuestaAsistente Error(string texto)
        {
            return new RespuestaAsistente(false, texto, null);
        }
    }
}
