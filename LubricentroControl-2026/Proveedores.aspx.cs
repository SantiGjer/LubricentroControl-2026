using System;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // ABM de proveedores. Admin y Encargado, acceso completo; Empleado, solo consulta
    // (Requerimientos §5): se le esconde el botón de alta, el modal del formulario y "Editar", y
    // los métodos de escritura igual cortan al principio por las dudas. "Ver" queda para todos.
    public partial class Proveedores : PaginaSegura
    {
        private const string IdModal = "modalProveedor";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Los avisos se muestran una sola vez (el Literal guarda su texto en el ViewState).
            pnlMensaje.Visible = false;
            pnlErrorFormulario.Visible = false;

            if (IsPostBack) return;

            if (EsSoloLectura)
            {
                btnNuevo.Visible = false;
                pnlFormulario.Visible = false;
            }

            CargarGrilla();
        }

        // Todos, activos e inactivos: el filtro por texto y el estado los aplica la tabla en el
        // navegador (Lubricentro.js).
        private void CargarGrilla()
        {
            gvProveedores.DataSource = ProveedorDAL.Listar(incluirInactivos: true);
            gvProveedores.DataBind();
        }

        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            LimpiarFormulario();
            MostrarFormulario();
        }

        protected void valCuit_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = Proveedor.EsCuitValido(args.Value);
        }

        protected void valTelefono_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = FormatoTelefono.EsValido(args.Value);
        }

        protected void valEmail_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = Proveedor.EsEmailValido(args.Value);
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid)
            {
                MostrarFormulario();
                return;
            }

            var proveedor = new Proveedor
            {
                IdProveedor = LeerIdOculto(hdnIdProveedor.Value),
                RazonSocial = txtRazonSocial.Text,
                Cuit = txtCuit.Text,
                Telefono = txtTelefono.Text,
                Email = txtEmail.Text,
                Direccion = txtDireccion.Text,
                Activo = ActivoDesdeHidden()
            };

            var resultado = proveedor.IdProveedor == 0
                ? ProveedorDAL.Crear(proveedor)
                : ProveedorDAL.Actualizar(proveedor);

            if (!resultado.Exito)
            {
                MostrarErrorFormulario(resultado.Mensaje);
                return;
            }

            MostrarMensaje(resultado.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void btnBorrar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var baja = ProveedorDAL.Desactivar(LeerIdOculto(hdnIdProveedor.Value));
            if (!baja.Exito)
            {
                MostrarErrorFormulario(baja.Mensaje);
                return;
            }

            MostrarMensaje(baja.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void btnReactivar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var alta = ProveedorDAL.Reactivar(LeerIdOculto(hdnIdProveedor.Value));
            if (!alta.Exito)
            {
                MostrarErrorFormulario(alta.Mensaje);
                return;
            }

            MostrarMensaje(alta.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void gvProveedores_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;
            if (e.CommandName != "Editar") return;

            Seleccionar(LeerIdOculto(Convert.ToString(e.CommandArgument)));
        }

        private void Seleccionar(int idProveedor)
        {
            var proveedor = ProveedorDAL.ObtenerPorId(idProveedor);
            if (proveedor == null)
            {
                MostrarMensaje("El proveedor no existe.", false);
                CargarGrilla();
                return;
            }

            hdnIdProveedor.Value = proveedor.IdProveedor.ToString();
            hdnActivo.Value = proveedor.Activo.ToString();
            txtRazonSocial.Text = proveedor.RazonSocial;
            txtCuit.Text = proveedor.Cuit;
            txtTelefono.Text = proveedor.Telefono;
            txtEmail.Text = proveedor.Email;
            txtDireccion.Text = proveedor.Direccion;
            btnBorrar.Visible = proveedor.Activo;
            btnReactivar.Visible = !proveedor.Activo;

            litTituloFormulario.Text = proveedor.Activo ? "Editar proveedor" : "Editar proveedor (inactivo)";
            MostrarFormulario();
        }

        // Por defecto activo si el campo oculto llegara vacío o manipulado
        // (ej. un POST armado a mano sin ese campo).
        private bool ActivoDesdeHidden()
        {
            bool activo;
            return !bool.TryParse(hdnActivo.Value, out activo) || activo;
        }

        // 0 (ID inexistente, cae en "no existe" en el DAL) si el campo oculto llegara
        // vacío o manipulado, en vez de reventar con FormatException.
        private static int LeerIdOculto(string valor)
        {
            int id;
            return int.TryParse(valor, out id) ? id : 0;
        }

        private void LimpiarFormulario()
        {
            hdnIdProveedor.Value = string.Empty;
            hdnActivo.Value = bool.TrueString;
            txtRazonSocial.Text = string.Empty;
            txtCuit.Text = string.Empty;
            txtTelefono.Text = string.Empty;
            txtEmail.Text = string.Empty;
            txtDireccion.Text = string.Empty;
            btnBorrar.Visible = false;
            btnReactivar.Visible = false;
            litTituloFormulario.Text = "Nuevo proveedor";
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
