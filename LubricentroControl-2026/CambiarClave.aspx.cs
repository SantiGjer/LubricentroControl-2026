using System;
using System.Web;
using BIZ.Data;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // No está en el menú: alcanza con estar logueado.
    public partial class CambiarClave : PaginaConSesion
    {
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var resultado = UsuarioDAL.CambiarPassword(
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
