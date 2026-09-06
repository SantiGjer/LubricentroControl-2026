using System;
using System.Web;
using System.Web.UI;
using BIZ.Modelo;

namespace LubricentroControl_2026.Seguridad
{
    // Página que exige usuario logueado, sin mirar permisos de menú.
    // La usan las pantallas que no figuran en el menú (cambio de clave, acceso denegado).
    public class PaginaConSesion : Page
    {
        protected Usuario UsuarioActual
        {
            get { return SesionUsuario.Actual; }
        }

        // Ruta de la pantalla tal como está registrada en la tabla Url (sin .aspx).
        protected string RutaLogica
        {
            get
            {
                var ruta = Request.AppRelativeCurrentExecutionFilePath ?? string.Empty;
                if (ruta.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                    ruta = ruta.Substring(0, ruta.Length - ".aspx".Length);
                return ruta;
            }
        }

        protected override void OnPreInit(EventArgs e)
        {
            base.OnPreInit(e);

            if (!SesionUsuario.HayUsuario)
            {
                Redirigir("~/Login?ReturnUrl=" + HttpUtility.UrlEncode(Request.RawUrl));
                return;
            }

            VerificarPermisos();
        }

        // Redirige cortando el request. Se usa endResponse=true a propósito:
        // con endResponse=false el ciclo de vida sigue y Page_Load igual se ejecuta
        // sin usuario en sesión, que fue justo el bug que esto evita.
        protected void Redirigir(string destino)
        {
            Response.Redirect(destino, true);
        }

        // Punto de extensión: PaginaSegura lo usa para chequear el menú.
        protected virtual void VerificarPermisos()
        {
        }

        protected void RedirigirAccesoDenegado()
        {
            Redirigir("~/AccesoDenegado");
        }
    }
}
