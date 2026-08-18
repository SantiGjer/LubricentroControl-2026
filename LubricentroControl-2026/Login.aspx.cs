using System;
using System.Web;
using System.Web.UI;
using BIZ.Modelo;
using BIZ.Negocio;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    public partial class Login : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Si ya hay sesión no tiene sentido mostrar el formulario.
            if (!IsPostBack && SesionUsuario.HayUsuario)
                Response.Redirect("~/Default");
        }

        protected void btnIngresar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            Usuario usuario;
            var resultado = SeguridadNegocio.Autenticar(txtEmail.Text, txtPassword.Text, out usuario);

            if (!resultado.Exito)
            {
                MostrarError(resultado.Mensaje);
                txtPassword.Text = string.Empty;
                return;
            }

            SesionUsuario.Iniciar(usuario);
            Response.Redirect(DestinoPostLogin());
        }

        private void MostrarError(string mensaje)
        {
            litMensaje.Text = HttpUtility.HtmlEncode(mensaje);
            pnlMensaje.Visible = true;
        }

        /// <summary>
        /// Vuelve a la pantalla que disparó el login, pero solo si es una URL local:
        /// un ReturnUrl externo permitiría usar el login como redirector a otro sitio.
        /// </summary>
        private string DestinoPostLogin()
        {
            var returnUrl = Request.QueryString["ReturnUrl"];
            return EsUrlLocal(returnUrl) ? returnUrl : "~/Default";
        }

        private static bool EsUrlLocal(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;

            // "//host" y "/\host" son URLs absolutas a otro sitio disfrazadas de relativas.
            if (url.StartsWith("//", StringComparison.Ordinal)) return false;
            if (url.StartsWith("/\\", StringComparison.Ordinal)) return false;

            return url.StartsWith("/", StringComparison.Ordinal);
        }
    }
}
