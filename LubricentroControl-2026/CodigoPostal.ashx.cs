using System;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.SessionState;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // Provincia y localidades de un código postal, para completar el domicilio mientras se escribe
    // (Requerimientos §9.15; lo pide Lubricentro.js desde los campos con data-completa-localidad).
    // CodigoPostal.ashx?codigo=1870 responde
    //   { "provincia": "Buenos Aires", "localidad": "Avellaneda", "localidades": ["Avellaneda", …] }
    // y {} si el código no es válido o no se conoce. Pide sesión, como el resto del sistema
    // (IReadOnlySessionState: sin esa marca un handler no ve la sesión).
    public class CodigoPostal : IHttpHandler, IReadOnlySessionState
    {
        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext contexto)
        {
            var respuesta = contexto.Response;

            if (!SesionUsuario.HayUsuario)
            {
                respuesta.StatusCode = 403;
                return;
            }

            var lugar = CodigosPostales.Buscar(contexto.Request.QueryString["codigo"]);
            object datos = lugar == null
                ? (object)new { }
                : new { provincia = lugar.Provincia, localidad = lugar.Localidad, localidades = lugar.Localidades };

            // La guía no cambia mientras corre la aplicación: el navegador puede guardar la respuesta.
            respuesta.Cache.SetCacheability(HttpCacheability.Private);
            respuesta.Cache.SetMaxAge(TimeSpan.FromDays(1));
            respuesta.ContentType = "application/json";
            respuesta.AddHeader("X-Content-Type-Options", "nosniff");
            respuesta.Write(new JavaScriptSerializer().Serialize(datos));
        }
    }
}
