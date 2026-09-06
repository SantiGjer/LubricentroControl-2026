using System;
using System.Web;
using System.Web.UI;
using BIZ.Data;

namespace LubricentroControl_2026
{
    public partial class RecuperarClave : Page
    {
        protected void btnEnviar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var resultado = RecuperacionClaveDAL.SolicitarRecuperacion(txtEmail.Text, ArmarEnlace);

            pnlMensaje.CssClass = "alert " + (resultado.Exito ? "alert-success" : "alert-danger");
            litMensaje.Text = HttpUtility.HtmlEncode(resultado.Mensaje);
            pnlMensaje.Visible = true;

            // Si salió bien, se oculta el formulario para no reenviar sin querer.
            pnlFormulario.Visible = !resultado.Exito;
        }

        // Enlace absoluto al formulario de restablecimiento, con el token.
        private string ArmarEnlace(string token)
        {
            var baseUri = new Uri(Request.Url, ResolveUrl("~/RestablecerClave"));
            return baseUri + "?token=" + HttpUtility.UrlEncode(token);
        }
    }
}
