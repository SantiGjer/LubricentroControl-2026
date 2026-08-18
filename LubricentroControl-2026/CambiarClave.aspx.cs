using System;
using System.Web;
using BIZ.Negocio;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    /// <summary>No está en el menú: alcanza con estar logueado.</summary>
    public partial class CambiarClave : PaginaConSesion
    {
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var resultado = SeguridadNegocio.CambiarPassword(
                UsuarioActual.IdUsuario, txtActual.Text, txtNueva.Text, txtRepetir.Text);

            pnlMensaje.CssClass = "alert " + (resultado.Exito ? "alert-success" : "alert-danger");
            litMensaje.Text = HttpUtility.HtmlEncode(resultado.Mensaje);
            pnlMensaje.Visible = true;

            if (resultado.Exito)
            {
                txtActual.Text = string.Empty;
                txtNueva.Text = string.Empty;
                txtRepetir.Text = string.Empty;
            }
        }
    }
}
