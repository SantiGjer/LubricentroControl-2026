using System;
using System.Web;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // ABM de usuarios y asignación de rol. En el menú solo la ve Admin;
    // PaginaSegura vuelve a chequearlo por si se entra escribiendo la URL.
    public partial class Usuarios : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            CargarNiveles();
            CargarGrilla();
        }

        private void CargarNiveles()
        {
            ddlNivel.DataSource = NivelDAL.Listar();
            ddlNivel.DataTextField = "Nombre";
            ddlNivel.DataValueField = "IdNivel";
            ddlNivel.DataBind();
        }

        private void CargarGrilla()
        {
            gvUsuarios.DataSource = UsuarioDAL.Listar();
            gvUsuarios.DataBind();
        }

        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
            litTituloFormulario.Text = "Nuevo usuario";
            pnlFormulario.Visible = true;
        }

        protected void btnCancelar_Click(object sender, EventArgs e)
        {
            pnlFormulario.Visible = false;
            LimpiarFormulario();
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var usuario = new Usuario
            {
                IdUsuario = string.IsNullOrEmpty(hdnIdUsuario.Value) ? 0 : int.Parse(hdnIdUsuario.Value),
                Nombre = txtNombre.Text,
                Apellido = txtApellido.Text,
                Email = txtEmail.Text,
                IdNivel = int.Parse(ddlNivel.SelectedValue),
                Activo = chkActivo.Checked
            };

            ResultadoOperacion resultado;
            if (usuario.IdUsuario == 0)
            {
                string passwordTemporal;
                resultado = UsuarioDAL.Crear(usuario, out passwordTemporal);

                if (resultado.Exito)
                    MostrarMensaje(resultado.Mensaje + " Contraseña temporal: <b>" +
                                   HttpUtility.HtmlEncode(passwordTemporal) + "</b> (también se envió por mail).", true);
            }
            else
            {
                resultado = UsuarioDAL.Actualizar(usuario);
                if (resultado.Exito) MostrarMensaje(resultado.Mensaje, true);
            }

            if (!resultado.Exito)
            {
                MostrarMensaje(resultado.Mensaje, false);
                return;
            }

            pnlFormulario.Visible = false;
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void gvUsuarios_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int idUsuario;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out idUsuario)) return;

            switch (e.CommandName)
            {
                case "Editar":
                    Editar(idUsuario);
                    break;

                case "Desactivar":
                    var baja = UsuarioDAL.Desactivar(idUsuario, UsuarioActual.IdUsuario);
                    MostrarMensaje(baja.Mensaje, baja.Exito);
                    CargarGrilla();
                    break;

                case "Blanquear":
                    string passwordTemporal;
                    var blanqueo = UsuarioDAL.BlanquearPassword(idUsuario, out passwordTemporal);
                    MostrarMensaje(
                        blanqueo.Exito
                            ? blanqueo.Mensaje + " Contraseña temporal: <b>" +
                              HttpUtility.HtmlEncode(passwordTemporal) + "</b> (también se envió por mail)."
                            : blanqueo.Mensaje,
                        blanqueo.Exito);
                    break;
            }
        }

        private void Editar(int idUsuario)
        {
            var usuario = UsuarioDAL.ObtenerPorId(idUsuario);
            if (usuario == null)
            {
                MostrarMensaje("El usuario no existe.", false);
                CargarGrilla();
                return;
            }

            hdnIdUsuario.Value = usuario.IdUsuario.ToString();
            txtNombre.Text = usuario.Nombre;
            txtApellido.Text = usuario.Apellido;
            txtEmail.Text = usuario.Email;
            ddlNivel.SelectedValue = usuario.IdNivel.ToString();
            chkActivo.Checked = usuario.Activo;

            litTituloFormulario.Text = "Editar usuario";
            pnlFormulario.Visible = true;
        }

        private void LimpiarFormulario()
        {
            hdnIdUsuario.Value = string.Empty;
            txtNombre.Text = string.Empty;
            txtApellido.Text = string.Empty;
            txtEmail.Text = string.Empty;
            chkActivo.Checked = true;
            if (ddlNivel.Items.Count > 0) ddlNivel.SelectedIndex = 0;
        }

        // El mensaje ya viene con HTML armado por el llamador, no se re-escapa acá.
        private void MostrarMensaje(string mensajeHtml, bool exito)
        {
            pnlMensaje.CssClass = "alert " + (exito ? "alert-success" : "alert-danger");
            litMensaje.Text = mensajeHtml;
            pnlMensaje.Visible = true;
        }
    }
}
