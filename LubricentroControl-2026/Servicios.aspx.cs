using System;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // ABM de servicios. Admin y Encargado, acceso completo; Empleado, solo consulta
    // (Requerimientos §5): se le esconde todo el formulario, la columna de Acciones, y
    // los métodos de escritura del DAL igual rechazan la operación por las dudas.
    public partial class Servicios : PaginaSegura
    {
        // Índice de la columna "Acciones" en gvServicios.Columns.
        private const int ColumnaAcciones = 4;

        protected void Page_Load(object sender, EventArgs e)
        {
            pnlSoloLectura.Visible = EsSoloLectura;

            if (IsPostBack) return;

            if (EsSoloLectura)
            {
                pnlFormulario.Visible = false;
                gvServicios.Columns[ColumnaAcciones].Visible = false;
            }

            CargarGrilla();
        }

        private void CargarGrilla()
        {
            var incluirInactivos = chkIncluirInactivos.Checked;
            var texto = txtBuscar.Text;

            gvServicios.DataSource = string.IsNullOrWhiteSpace(texto)
                ? ServicioDAL.Listar(incluirInactivos)
                : ServicioDAL.Buscar(texto, incluirInactivos);
            gvServicios.DataBind();
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

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid) return;

            decimal precioBase;
            decimal.TryParse(txtPrecioBase.Text, out precioBase);

            var servicio = new Servicio
            {
                IdServicio = LeerIdOculto(hdnIdServicio.Value),
                Nombre = txtNombre.Text,
                Descripcion = txtDescripcion.Text,
                PrecioBase = precioBase,
                Activo = ActivoDesdeHidden()
            };

            var resultado = servicio.IdServicio == 0
                ? ServicioDAL.Crear(servicio)
                : ServicioDAL.Actualizar(servicio);

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

            var baja = ServicioDAL.Desactivar(LeerIdOculto(hdnIdServicio.Value));

            MostrarMensaje(baja.Mensaje, baja.Exito);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void gvServicios_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;
            if (e.CommandName != "Seleccionar") return;

            Seleccionar(LeerIdOculto(Convert.ToString(e.CommandArgument)));
        }

        private void Seleccionar(int idServicio)
        {
            var servicio = ServicioDAL.ObtenerPorId(idServicio);
            if (servicio == null)
            {
                MostrarMensaje("El servicio no existe.", false);
                CargarGrilla();
                return;
            }

            hdnIdServicio.Value = servicio.IdServicio.ToString();
            hdnActivo.Value = servicio.Activo.ToString();
            txtNombre.Text = servicio.Nombre;
            txtDescripcion.Text = servicio.Descripcion;
            txtPrecioBase.Text = servicio.PrecioBase.ToString("N2");
            btnBorrar.Visible = servicio.Activo;

            litTituloFormulario.Text = "Editar servicio";
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
            hdnIdServicio.Value = string.Empty;
            hdnActivo.Value = bool.TrueString;
            txtNombre.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
            txtPrecioBase.Text = string.Empty;
            btnBorrar.Visible = false;
            litTituloFormulario.Text = "Nuevo servicio";
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
