using System;
using System.Configuration;
using System.IO;
using System.Net.Mail;

namespace BIZ.Negocio
{
    /// <summary>
    /// Envío de mails. La configuración vive en &lt;system.net&gt;/&lt;mailSettings&gt; de Web.config.
    /// En desarrollo está apuntado a una carpeta local (SpecifiedPickupDirectory): los mails
    /// se escriben como archivos .eml en App_Data\MailsEnviados en vez de salir a Internet.
    /// </summary>
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

        /// <summary>
        /// Con MailModoDesarrollo=true los mails se guardan como .eml en
        /// App_Data\MailsEnviados en lugar de salir por SMTP. Así el circuito de
        /// recuperación de clave se puede probar entero sin un servidor de correo.
        /// </summary>
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

        private static SmtpClient CrearCliente()
        {
            // Sin modo desarrollo toma la configuración de <system.net>/<mailSettings>.
            if (!ModoDesarrollo) return new SmtpClient();

            var carpeta = CarpetaMailsDesarrollo;
            if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);

            return new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = carpeta
            };
        }

        /// <summary>
        /// Envía el mail. Devuelve el resultado en vez de propagar la excepción:
        /// que falle el SMTP no debe tumbar la pantalla que lo pidió.
        /// </summary>
        public static ResultadoOperacion Enviar(string destinatario, string asunto, string cuerpoHtml)
        {
            try
            {
                using (var mensaje = new MailMessage())
                {
                    mensaje.From = new MailAddress(Remitente, "LubricentroControl");
                    mensaje.To.Add(destinatario);
                    mensaje.Subject = asunto;
                    mensaje.Body = cuerpoHtml;
                    mensaje.IsBodyHtml = true;

                    using (var cliente = CrearCliente())
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
