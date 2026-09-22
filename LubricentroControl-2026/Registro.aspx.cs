using System;
using System.Web;
using System.Web.UI;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    public partial class Registro : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Si ya hay sesión no tiene sentido mostrar el formulario.
            if (!IsPostBack && SesionUsuario.HayUsuario)
                Response.Redirect("~/Default");
        }

        protected void btnRegistrar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var usuario = new Usuario
            {
                Nombre = txtNombre.Text,
                Apellido = txtApellido.Text,
                Email = txtEmail.Text
            };

            var resultado = UsuarioDAL.Registrar(usuario, txtPassword.Text, txtRepetir.Text);
            if (!resultado.Exito)
            {
                MostrarError(resultado.Mensaje);
                return;
            }

            SesionUsuario.Iniciar(usuario);
            Response.Redirect("~/Default");
        }

        private void MostrarError(string mensaje)
        {
            litMensaje.Text = HttpUtility.HtmlEncode(mensaje);
            pnlMensaje.Visible = true;
        }
    }
}
