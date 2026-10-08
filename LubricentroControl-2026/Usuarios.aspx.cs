using System;
using System.Web;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // ABM de usuarios y asignación de rol. Por defecto solo la ve Admin; PaginaSegura vuelve a
    // chequearlo por si se entra escribiendo la URL. Como los permisos se editan en Roles, otro rol
    // puede recibirla en solo consulta: entonces ve la lista y "Ver", sin alta, edición, blanqueo
    // ni baja (y cada método de escritura corta al principio, por las dudas). El formulario de
    // alta/edición se abre en un modal sobre la lista.
    public partial class Usuarios : PaginaSegura
    {
        private const string IdModal = "modalUsuario";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Los avisos se muestran una sola vez: además de no repetir el último aviso en cada
            // postback, así la contraseña temporal no queda a la vista más de lo necesario.
            pnlMensaje.Visible = false;
            pnlErrorFormulario.Visible = false;

            if (IsPostBack) return;

            if (EsSoloLectura)
            {
                btnNuevo.Visible = false;
                pnlFormulario.Visible = false;
            }

            CargarNiveles();
            CargarGrilla();
        }

        // Los roles salen de la base (se crean en Roles): el desplegable del formulario y las
        // opciones de filtro por rol.
        private void CargarNiveles()
        {
            var niveles = NivelDAL.Listar();

            ddlNivel.DataSource = niveles;
            ddlNivel.DataTextField = "Nombre";
            ddlNivel.DataValueField = "IdNivel";
            ddlNivel.DataBind();

            rptRolesFiltro.DataSource = niveles;
            rptRolesFiltro.DataBind();
        }

        private void CargarGrilla()
        {
            gvUsuarios.DataSource = UsuarioDAL.Listar();
            gvUsuarios.DataBind();
        }

        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            LimpiarFormulario();
            MostrarFormulario();
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid)
            {
                MostrarFormulario();
                return;
            }

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
                MostrarErrorFormulario(resultado.Mensaje);
                return;
            }

            LimpiarFormulario();
            CargarGrilla();
        }

        protected void btnReactivar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            int idUsuario;
            int.TryParse(hdnIdUsuario.Value, out idUsuario);

            var alta = UsuarioDAL.Reactivar(idUsuario);
            if (!alta.Exito)
            {
                MostrarErrorFormulario(alta.Mensaje);
                return;
            }

            MostrarMensaje(alta.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void gvUsuarios_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;

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
            btnReactivar.Visible = !usuario.Activo;

            litTituloFormulario.Text = usuario.Activo ? "Editar usuario" : "Editar usuario (inactivo)";
            MostrarFormulario();
        }

        private void LimpiarFormulario()
        {
            hdnIdUsuario.Value = string.Empty;
            txtNombre.Text = string.Empty;
            txtApellido.Text = string.Empty;
            txtEmail.Text = string.Empty;
            chkActivo.Checked = true;
            if (ddlNivel.Items.Count > 0) ddlNivel.SelectedIndex = 0;
            btnReactivar.Visible = false;
            litTituloFormulario.Text = "Nuevo usuario";
        }

        private void MostrarFormulario()
        {
            Interfaz.AbrirModal(this, IdModal);
        }

        // Un error al guardar se muestra adentro del modal, que vuelve a abrirse con lo cargado.
        private void MostrarErrorFormulario(string mensajeHtml)
        {
            litErrorFormulario.Text = mensajeHtml;
            pnlErrorFormulario.Visible = true;
            MostrarFormulario();
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
