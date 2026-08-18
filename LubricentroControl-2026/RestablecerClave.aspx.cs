using System;
using System.Web;
using System.Web.UI;
using BIZ.Negocio;

namespace LubricentroControl_2026
{
    public partial class RestablecerClave : Page
    {
        private string Token
        {
            get { return Request.QueryString["token"]; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            // Se valida el token antes de mostrar el formulario, sin consumirlo.
            var validacion = SeguridadNegocio.ValidarToken(Token);
            if (!validacion.Exito)
                MostrarMensaje(validacion.Mensaje, false, ocultarFormulario: true);
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var resultado = SeguridadNegocio.RestablecerPassword(Token, txtPassword.Text, txtRepetir.Text);
            MostrarMensaje(resultado.Mensaje, resultado.Exito, ocultarFormulario: resultado.Exito);
        }

        private void MostrarMensaje(string mensaje, bool exito, bool ocultarFormulario)
        {
            pnlMensaje.CssClass = "alert " + (exito ? "alert-success" : "alert-danger");
            litMensaje.Text = HttpUtility.HtmlEncode(mensaje);
            pnlMensaje.Visible = true;
            pnlFormulario.Visible = !ocultarFormulario;
        }
    }
}
