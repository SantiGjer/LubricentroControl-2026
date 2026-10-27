using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BIZ.Modelo;
using Newtonsoft.Json.Linq;

namespace BIZ.Data
{
    // Lo que la IA entendió de la pregunta: qué consulta del catálogo, qué período y, si la consulta
    // lo usa, el filtro. Las fechas y el filtro vienen como texto y los valida AsistenteVentasDAL.
    public class InterpretacionVentasIA
    {
        public string Consulta { get; set; }
        public string Desde { get; set; }
        public string Hasta { get; set; }
        public string Filtro { get; set; }
    }

    // Cliente del modelo de lenguaje LOCAL (Ollama) del asistente de ventas y stock, por el endpoint
    // compatible con OpenAI (/v1/chat/completions). Usa las mismas claves de Web.config que el asistente
    // de consultas (IA.Url, IA.Modelo e IA.TimeoutSegundos): los dos le preguntan al mismo Ollama. La IA
    // solo interpreta la pregunta: elige una consulta del catálogo (ConsultaVentasIA), el período y el
    // filtro. No escribe SQL ni arma la respuesta.
    public static class ServicioIAVentas
    {
        private const string MensajeNoDisponible =
            "El asistente no está disponible: verificá que Ollama esté abierto y el modelo descargado.";

        public static string Modelo
        {
            get { return Clave("IA.Modelo", "qwen2.5:3b"); }
        }

        private static string Url
        {
            get { return Clave("IA.Url", "http://localhost:11434/v1/chat/completions"); }
        }

        private static int TimeoutSegundos
        {
            get
            {
                int valor;
                return int.TryParse(ConfigurationManager.AppSettings["IA.TimeoutSegundos"], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out valor) && valor > 0 ? valor : 60;
            }
        }

        private static string Clave(string nombre, string porDefecto)
        {
            var valor = ConfigurationManager.AppSettings[nombre];
            return string.IsNullOrWhiteSpace(valor) ? porDefecto : valor.Trim();
        }

        // Le pide a la IA que interprete la pregunta. Devuelve el error en vez de propagar la excepción:
        // que Ollama esté cerrado no tiene que tumbar la pantalla.
        public static ResultadoOperacion Interpretar(string pregunta, out InterpretacionVentasIA interpretacion)
        {
            interpretacion = null;

            string contenido;
            try
            {
                contenido = Llamar(pregunta);
            }
            catch (TaskCanceledException)
            {
                // Así avisa HttpClient que se agotó el tiempo: Ollama está abierto pero no llegó a contestar.
                // En una PC sin placa de video la primera pregunta (la que carga el modelo) es la más lenta.
                return ResultadoOperacion.Error("El asistente tardó más de " + TimeoutSegundos +
                    " segundos en responder. La primera pregunta es la más lenta porque la IA carga el modelo: " +
                    "probá de nuevo en un momento.");
            }
            catch (Exception)
            {
                // Conexión rechazada, error HTTP, respuesta sin el formato de OpenAI: para el usuario es lo mismo.
                return ResultadoOperacion.Error(MensajeNoDisponible);
            }

            try
            {
                var json = JObject.Parse(LimpiarRespuesta(contenido));
                interpretacion = new InterpretacionVentasIA
                {
                    Consulta = (string)json["consulta"],
                    Desde = (string)json["desde"],
                    Hasta = (string)json["hasta"],
                    Filtro = (string)json["filtro"]
                };
                return ResultadoOperacion.Ok();
            }
            catch (Exception)
            {
                return ResultadoOperacion.Error(
                    "El asistente devolvió una respuesta que no pude interpretar. Probá reformular la pregunta.");
            }
        }

        // POST síncrono (Web Forms, sin páginas asincrónicas) con tiempo máximo. Devuelve el texto que
        // escribió el modelo: choices[0].message.content de la respuesta.
        private static string Llamar(string pregunta)
        {
            var cuerpo = new JObject
            {
                ["model"] = Modelo,
                ["temperature"] = 0,
                ["stream"] = false,
                ["response_format"] = new JObject { ["type"] = "json_object" },
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = ArmarPromptSistema(DateTime.Today) },
                    new JObject { ["role"] = "user", ["content"] = pregunta }
                }
            };

            using (var cliente = new HttpClient())
            {
                cliente.Timeout = TimeSpan.FromSeconds(TimeoutSegundos);
                using (var contenido = new StringContent(cuerpo.ToString(), Encoding.UTF8, "application/json"))
                {
                    var respuesta = cliente.PostAsync(Url, contenido).GetAwaiter().GetResult();
                    respuesta.EnsureSuccessStatusCode();
                    var texto = respuesta.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    return (string)JObject.Parse(texto)["choices"][0]["message"]["content"];
                }
            }
        }

        // Instrucción de sistema: sale del catálogo, así que una consulta nueva queda disponible sin
        // tocar esto. Pide un JSON con la consulta, el período y el filtro, y nada más.
        internal static string ArmarPromptSistema(DateTime hoy)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Sos el intérprete de preguntas del asistente de ventas y stock de un lubricentro.");
            sb.AppendLine("Tu única tarea es elegir cuál de estas consultas corresponde a la pregunta del usuario:");
            foreach (var c in ConsultaVentasIA.Catalogo)
                sb.AppendLine("- \"" + c.Id + "\": " + c.Descripcion + ". Ejemplos: " + string.Join(" | ", c.Ejemplos) +
                    (c.Filtro == null ? "" : ". Filtro: " + c.Filtro));
            sb.AppendLine("Si la pregunta no corresponde a ninguna, usá \"ninguna\".");
            sb.AppendLine("Hoy es " + hoy.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) +
                " (" + hoy.ToString("dddd", new CultureInfo("es-AR")) + ").");
            sb.AppendLine("Calculá el período que pide el usuario (\"este mes\", \"septiembre\", \"el año\", \"ayer\"...) " +
                "con fechas completas. Si no dice período, usá el mes actual completo.");
            var sinPeriodo = ConsultaVentasIA.Catalogo.Where(c => c.SinPeriodo).Select(c => "\"" + c.Id + "\"").ToList();
            if (sinPeriodo.Count > 0)
                sb.AppendLine("Las consultas " + string.Join(", ", sinPeriodo) +
                    " no dependen del período: para ellas dejá \"desde\" y \"hasta\" vacíos.");
            sb.AppendLine("En \"filtro\" va solo lo que indica la consulta elegida, copiado de la pregunta; " +
                "si la consulta no tiene filtro o la pregunta no lo nombra, dejalo vacío.");
            sb.AppendLine("Respondé SOLO con un JSON, sin texto adicional, con exactamente este formato:");
            sb.Append("{\"consulta\": \"<id o ninguna>\", \"desde\": \"yyyy-MM-dd\", \"hasta\": \"yyyy-MM-dd\", \"filtro\": \"<texto o vacío>\"}");
            return sb.ToString();
        }

        // Algunos modelos envuelven el JSON en un bloque ``` aunque se les pida lo contrario.
        private static string LimpiarRespuesta(string texto)
        {
            if (texto == null) return "";
            var inicio = texto.IndexOf('{');
            var fin = texto.LastIndexOf('}');
            return inicio >= 0 && fin > inicio ? texto.Substring(inicio, fin - inicio + 1) : texto;
        }
    }
}
