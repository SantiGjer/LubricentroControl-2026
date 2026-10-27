using System.Data;

namespace BIZ.Modelo
{
    // Lo que el asistente de ventas y stock le devuelve a la pantalla: el texto ya armado (por C#,
    // nunca por la IA) y, si corresponde, la tabla para mostrar debajo, con los valores ya formateados.
    public class RespuestaVentasIA
    {
        // false = no se pudo responder (la IA o la base no contestaron); la pantalla lo muestra como aviso.
        public bool Exito { get; private set; }
        public string Texto { get; private set; }
        public DataTable Tabla { get; private set; }

        private RespuestaVentasIA(bool exito, string texto, DataTable tabla)
        {
            Exito = exito;
            Texto = texto;
            Tabla = tabla;
        }

        public static RespuestaVentasIA Ok(string texto, DataTable tabla = null)
        {
            return new RespuestaVentasIA(true, texto, tabla);
        }

        public static RespuestaVentasIA Error(string texto)
        {
            return new RespuestaVentasIA(false, texto, null);
        }
    }
}
