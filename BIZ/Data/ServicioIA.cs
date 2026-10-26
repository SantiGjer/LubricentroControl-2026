using System;
using System.Configuration;
using System.Globalization;
using System.Net.Http;
using System.Text;
using BIZ.Modelo;
using Newtonsoft.Json.Linq;

namespace BIZ.Data
{
    // Lo que la IA entendió de la pregunta: qué consulta del catálogo y qué período.
    // Las fechas vienen como texto y las valida quien las usa (AsistenteDAL).
    public class InterpretacionConsulta
    {
        public string Consulta { get; set; }
        public string Desde { get; set; }
        public string Hasta { get; set; }
    }

    // Cliente de un modelo de lenguaje LOCAL (Ollama, LM Studio...) por el endpoint compatible
    // con OpenAI (/v1/chat/completions). La configuración vive en appSettings de Web.config:
    // IA.Url, IA.Modelo e IA.TimeoutSegundos. La IA solo interpreta la pregunta: elige una consulta
    // del catálogo cerrado y el período. No escribe SQL ni arma la respuesta.
    public static class ServicioIA
    {
        private const string MensajeNoDisponible =
            "El asistente no está disponible: verificá que Ollama esté abierto y el modelo descargado.";

        public static string Modelo
        {
            get
            {
                var valor = ConfigurationManager.AppSettings["IA.Modelo"];
                return string.IsNullOrWhiteSpace(valor) ? "qwen2.5:3b" : valor.Trim();
            }
        }

        private static string Url
        {
            get
            {
                var valor = ConfigurationManager.AppSettings["IA.Url"];
                return string.IsNullOrWhiteSpace(valor)
                    ? "http://localhost:11434/v1/chat/completions" : valor.Trim();
            }
        }

        private static int TimeoutSegundos
        {
            get
            {
                int valor;
                return int.TryParse(ConfigurationManager.AppSettings["IA.TimeoutSegundos"],
                    NumberStyles.Integer, CultureInfo.InvariantCulture, out valor) && valor > 0 ? valor : 60;
            }
        }

        // Pide a la IA que interprete la pregunta. Devuelve el error en vez de propagar la excepción:
        // que Ollama esté cerrado no debe tumbar la pantalla.
        public static ResultadoOperacion Interpretar(string pregunta, out InterpretacionConsulta interpretacion)
        {
            interpretacion = null;

            string contenido;
            try
            {
                contenido = Llamar(pregunta);
            }
            catch (Exception)
            {
                // Conexión rechazada, timeout, HTTP de error: para el usuario es lo mismo.
                return ResultadoOperacion.Error(MensajeNoDisponible);
            }

            try
            {
                var json = JObject.Parse(LimpiarRespuesta(contenido));
                interpretacion = new InterpretacionConsulta
                {
                    Consulta = (string)json["consulta"],
                    Desde = (string)json["desde"],
                    Hasta = (string)json["hasta"]
                };
                return ResultadoOperacion.Ok();
            }
            catch (Exception)
            {
                return ResultadoOperacion.Error(
                    "El asistente devolvió una respuesta que no pude interpretar. Probá reformular la pregunta.");
            }
        }

        // POST síncrono (Web Forms, sin async de página) con timeout. Devuelve el texto del mensaje del modelo.
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

        // Prompt de sistema: sale del catálogo, así que una consulta nueva queda disponible sin tocar esto.
        internal static string ArmarPromptSistema(DateTime hoy)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Sos el intérprete de preguntas de un sistema de gestión de un lubricentro.");
            sb.AppendLine("Tu única tarea es elegir cuál de estas consultas corresponde a la pregunta del usuario:");
            foreach (var c in ConsultaAsistente.Catalogo)
            {
                sb.AppendLine("- \"" + c.Id + "\": " + c.Descripcion + ". Ejemplos: " +
                    string.Join(" | ", c.Ejemplos));
            }
            sb.AppendLine("Si la pregunta no corresponde a ninguna, usá \"ninguna\".");
            sb.AppendLine("Hoy es " + hoy.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) +
                " (" + hoy.ToString("dddd", new CultureInfo("es-AR")) + ").");
            sb.AppendLine("Calculá el período que pide el usuario (\"este mes\", \"septiembre\", \"el año\", \"ayer\"...) " +
                "con fechas completas. Si no dice período, usá el mes actual completo.");
            sb.AppendLine("Respondé SOLO con un JSON, sin texto adicional, con exactamente este formato:");
            sb.Append("{\"consulta\": \"<id o ninguna>\", \"desde\": \"yyyy-MM-dd\", \"hasta\": \"yyyy-MM-dd\"}");
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
