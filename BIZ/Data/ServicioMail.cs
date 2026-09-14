using System;
using System.Configuration;
using System.IO;
using System.Net.Mail;
using BIZ.Modelo;

namespace BIZ.Data
{
    // Envío de mails. La configuración vive en <system.net>/<mailSettings> de Web.config.
    // En desarrollo (MailModoDesarrollo=true) no se manda nada por SMTP: se escribe un .txt
    // simple en App_Data\MailsEnviados con el destinatario, el asunto y el cuerpo, para poder
    // abrirlo con cualquier editor de texto sin necesidad de un cliente de mail.
    public static class ServicioMail
    {
        private static string Remitente
        {
            get
            {
                var valor = ConfigurationManager.AppSettings["MailRemitente"];
                return string.IsNullOrWhiteSpace(valor) ? "no-reply@lubricentro.com" : valor;
            }
        }

        // Con MailModoDesarrollo=true los mails se guardan como .txt en
        // App_Data\MailsEnviados en lugar de salir por SMTP. Así el circuito de
        // recuperación de clave se puede probar entero sin un servidor de correo.
        private static bool ModoDesarrollo
        {
            get
            {
                bool valor;
                return bool.TryParse(ConfigurationManager.AppSettings["MailModoDesarrollo"], out valor) && valor;
            }
        }

        public static string CarpetaMailsDesarrollo
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "MailsEnviados");
            }
        }

        // Envía el mail. Devuelve el resultado en vez de propagar la excepción:
        // que falle el SMTP no debe tumbar la pantalla que lo pidió.
        public static ResultadoOperacion Enviar(string destinatario, string asunto, string cuerpoHtml)
        {
            if (ModoDesarrollo) return GuardarComoTexto(destinatario, asunto, cuerpoHtml);

            try
            {
                using (var mensaje = new MailMessage())
                {
                    mensaje.From = new MailAddress(Remitente, "LubricentroControl");
                    mensaje.To.Add(destinatario);
                    mensaje.Subject = asunto;
                    mensaje.Body = cuerpoHtml;
                    mensaje.IsBodyHtml = true;

                    using (var cliente = new SmtpClient())
                    {
                        cliente.Send(mensaje);
                    }
                }
                return ResultadoOperacion.Ok();
            }
            catch (Exception ex)
            {
                return ResultadoOperacion.Error("No se pudo enviar el mail: " + ex.Message);
            }
        }

        // Reemplaza el envío real en desarrollo: escribe un .txt plano (destinatario, asunto y
        // cuerpo) en vez de un .eml, que trae el cuerpo codificado en base64 y no se puede leer
        // directo con el Bloc de notas.
        private static ResultadoOperacion GuardarComoTexto(string destinatario, string asunto, string cuerpoHtml)
        {
            try
            {
                var carpeta = CarpetaMailsDesarrollo;
                if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);

                var nombreArchivo = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".txt";
                var contenido =
                    "Para: " + destinatario + Environment.NewLine +
                    "Asunto: " + asunto + Environment.NewLine +
                    "Fecha: " + DateTime.Now + Environment.NewLine +
                    Environment.NewLine +
                    cuerpoHtml;

                File.WriteAllText(Path.Combine(carpeta, nombreArchivo), contenido);
                return ResultadoOperacion.Ok();
            }
            catch (Exception ex)
            {
                return ResultadoOperacion.Error("No se pudo guardar el mail simulado: " + ex.Message);
            }
        }

        public static string ArmarCuerpoRecuperacion(string nombreUsuario, string enlace, int minutosVigencia)
        {
            return
                "<p>Hola " + Escapar(nombreUsuario) + ",</p>" +
                "<p>Recibimos un pedido para restablecer tu contraseña de LubricentroControl.</p>" +
                "<p><a href=\"" + Escapar(enlace) + "\">Restablecer mi contraseña</a></p>" +
                "<p>El enlace es de un solo uso y vence en " + minutosVigencia + " minutos.</p>" +
                "<p>Si no pediste esto, ignorá este mensaje: tu contraseña sigue igual.</p>";
        }

        public static string ArmarCuerpoAltaUsuario(string nombreUsuario, string email, string passwordTemporal)
        {
            return
                "<p>Hola " + Escapar(nombreUsuario) + ",</p>" +
                "<p>Se creó tu cuenta en LubricentroControl.</p>" +
                "<p>Usuario: <b>" + Escapar(email) + "</b><br/>" +
                "Contraseña temporal: <b>" + Escapar(passwordTemporal) + "</b></p>" +
                "<p>Cambiala apenas ingreses por primera vez.</p>";
        }

        private static string Escapar(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return string.Empty;
            return texto.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
