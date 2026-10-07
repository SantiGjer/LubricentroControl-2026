using System;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // ABM de servicios. Admin y Encargado, acceso completo; Empleado, solo consulta
    // (Requerimientos §5): se le esconde el botón de alta, el modal del formulario y la columna
    // de Acciones, y los métodos de escritura igual cortan al principio por las dudas.
    public partial class Servicios : PaginaSegura
    {
        // Índice de la columna "Acciones" en gvServicios.Columns.
        private const int ColumnaAcciones = 4;

        private const string IdModal = "modalServicio";

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
                gvServicios.Columns[ColumnaAcciones].Visible = false;
            }

            CargarGrilla();
        }

        // El filtro por texto lo hace la tabla en el navegador (Lubricentro.js).
        private void CargarGrilla()
        {
            gvServicios.DataSource = ServicioDAL.Listar(chkIncluirInactivos.Checked);
            gvServicios.DataBind();
        }

        protected void chkIncluirInactivos_CheckedChanged(object sender, EventArgs e)
        {
            CargarGrilla();
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

            var baja = ServicioDAL.Desactivar(LeerIdOculto(hdnIdServicio.Value));
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

            var alta = ServicioDAL.Reactivar(LeerIdOculto(hdnIdServicio.Value));
            if (!alta.Exito)
            {
                MostrarErrorFormulario(alta.Mensaje);
                return;
            }

            MostrarMensaje(alta.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void gvServicios_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;
            if (e.CommandName != "Editar") return;

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
            btnReactivar.Visible = !servicio.Activo;

            litTituloFormulario.Text = servicio.Activo ? "Editar servicio" : "Editar servicio (inactivo)";
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
            hdnIdServicio.Value = string.Empty;
            hdnActivo.Value = bool.TrueString;
            txtNombre.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
            txtPrecioBase.Text = string.Empty;
            btnBorrar.Visible = false;
            btnReactivar.Visible = false;
            litTituloFormulario.Text = "Nuevo servicio";
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
