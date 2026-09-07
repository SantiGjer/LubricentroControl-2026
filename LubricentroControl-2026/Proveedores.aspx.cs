using System;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // ABM de proveedores. Admin y Encargado, acceso completo; Empleado, solo consulta
    // (Requerimientos §5): se le esconde todo el formulario, la grilla de Acciones, y
    // los métodos de escritura del DAL igual rechazan la operación por las dudas.
    public partial class Proveedores : PaginaSegura
    {
        // Índice de la columna "Acciones" en gvProveedores.Columns — no tiene sentido
        // para el Empleado si no hay formulario donde cargar la selección.
        private const int ColumnaAcciones = 5;

        protected void Page_Load(object sender, EventArgs e)
        {
            pnlSoloLectura.Visible = EsSoloLectura;

            if (IsPostBack) return;

            if (EsSoloLectura)
            {
                pnlFormulario.Visible = false;
                gvProveedores.Columns[ColumnaAcciones].Visible = false;
            }

            CargarGrilla();
        }

        private void CargarGrilla()
        {
            var incluirInactivos = chkIncluirInactivos.Checked;
            var texto = txtBuscar.Text;

            gvProveedores.DataSource = string.IsNullOrWhiteSpace(texto)
                ? ProveedorDAL.Listar(incluirInactivos)
                : ProveedorDAL.Buscar(texto, incluirInactivos);
            gvProveedores.DataBind();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        protected void chkIncluirInactivos_CheckedChanged(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            LimpiarFormulario();
        }

        protected void valCuit_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = Proveedor.EsCuitValido(args.Value);
        }

        protected void valEmail_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = Proveedor.EsEmailValido(args.Value);
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid) return;

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
                MostrarMensaje(resultado.Mensaje, false);
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

            MostrarMensaje(baja.Mensaje, baja.Exito);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void gvProveedores_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;
            if (e.CommandName != "Seleccionar") return;

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

            litTituloFormulario.Text = "Editar proveedor";
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
            litTituloFormulario.Text = "Nuevo proveedor";
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
