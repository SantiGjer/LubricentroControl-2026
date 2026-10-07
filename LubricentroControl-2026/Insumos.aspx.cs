using System;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // ABM de insumos. Admin y Encargado, acceso completo; Empleado, solo consulta
    // (Requerimientos §5): se le esconde el botón de alta, el modal (incluidos el ajuste de stock
    // y el historial) y la columna de Acciones, y los métodos de escritura igual cortan al
    // principio por las dudas.
    public partial class Insumos : PaginaSegura
    {
        // Índice de la columna "Acciones" en gvInsumos.Columns — no tiene sentido para el
        // Empleado si no hay formulario donde cargar la selección.
        private const int ColumnaAcciones = 7;

        private const string IdModal = "modalInsumo";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Los avisos se muestran una sola vez (el Literal guarda su texto en el ViewState).
            pnlMensaje.Visible = false;
            pnlMensajeFormulario.Visible = false;

            if (IsPostBack) return;

            CargarUnidadesMedida();

            if (EsSoloLectura)
            {
                btnNuevo.Visible = false;
                pnlFormulario.Visible = false;
                gvInsumos.Columns[ColumnaAcciones].Visible = false;
            }

            CargarGrilla();
        }

        private void CargarUnidadesMedida()
        {
            ddlUnidadMedida.Items.Clear();
            ddlUnidadMedida.Items.Add(new ListItem("(sin especificar)", ""));
            foreach (var unidad in Insumo.UnidadesDeMedida)
                ddlUnidadMedida.Items.Add(new ListItem(unidad, unidad));
        }

        // El filtro por texto y el paginado los hace la tabla en el navegador (Lubricentro.js,
        // data-filas-por-pagina), sobre todos los insumos.
        private void CargarGrilla()
        {
            gvInsumos.DataSource = InsumoDAL.Listar(chkIncluirInactivos.Checked);
            gvInsumos.DataBind();
        }

        protected void gvInsumos_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var insumo = (Insumo)e.Row.DataItem;
            if (insumo.Activo && insumo.StockActual < insumo.StockMinimo)
                e.Row.Style.Add("background-color", "#f8d7da");
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

            decimal stockMinimo, precioVenta, stockInicial;
            decimal.TryParse(txtStockMinimo.Text, out stockMinimo);
            decimal.TryParse(txtPrecioVenta.Text, out precioVenta);
            decimal.TryParse(txtStockInicial.Text, out stockInicial);

            var idInsumo = LeerIdOculto(hdnIdInsumo.Value);
            var insumo = new Insumo
            {
                IdInsumo = idInsumo,
                Nombre = txtNombre.Text,
                Marca = txtMarca.Text,
                UnidadMedida = ddlUnidadMedida.SelectedValue,
                StockMinimo = stockMinimo,
                PrecioVenta = precioVenta,
                StockActual = stockInicial,
                Activo = ActivoDesdeHidden()
            };

            var resultado = idInsumo == 0
                ? InsumoDAL.Crear(insumo, UsuarioActual.IdUsuario)
                : InsumoDAL.Actualizar(insumo);

            if (!resultado.Exito)
            {
                MostrarMensajeFormulario(resultado.Mensaje, false);
                return;
            }

            MostrarMensaje(resultado.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void btnBorrar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var baja = InsumoDAL.Desactivar(LeerIdOculto(hdnIdInsumo.Value));
            if (!baja.Exito)
            {
                MostrarMensajeFormulario(baja.Mensaje, false);
                return;
            }

            MostrarMensaje(baja.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void btnReactivar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var alta = InsumoDAL.Reactivar(LeerIdOculto(hdnIdInsumo.Value));
            if (!alta.Exito)
            {
                MostrarMensajeFormulario(alta.Mensaje, false);
                return;
            }

            MostrarMensaje(alta.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        // El ajuste deja el modal abierto (con el stock y el historial ya actualizados): lo
        // normal es seguir mirando el mismo insumo.
        protected void btnRegistrarAjuste_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var idInsumo = LeerIdOculto(hdnIdInsumo.Value);
            if (!Page.IsValid)
            {
                MostrarFormulario();
                return;
            }

            decimal cantidad;
            decimal.TryParse(txtCantidadAjuste.Text, out cantidad);

            // Un solo campo con signo: positivo suma stock, negativo resta. RegistrarAjusteManual
            // sigue pidiendo la cantidad siempre positiva + el signo aparte, así que se traduce acá.
            var resultado = MovimientoStockDAL.RegistrarAjusteManual(
                idInsumo, Math.Abs(cantidad), cantidad > 0, txtMotivoAjuste.Text, UsuarioActual.IdUsuario);

            if (resultado.Exito)
            {
                txtCantidadAjuste.Text = string.Empty;
                txtMotivoAjuste.Text = string.Empty;
                Seleccionar(idInsumo);
                CargarGrilla();
            }

            MostrarMensajeFormulario(resultado.Mensaje, resultado.Exito);
        }

        protected void gvInsumos_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;
            if (e.CommandName != "Editar") return;

            Seleccionar(LeerIdOculto(Convert.ToString(e.CommandArgument)));
        }

        private void Seleccionar(int idInsumo)
        {
            var insumo = InsumoDAL.ObtenerPorId(idInsumo);
            if (insumo == null)
            {
                MostrarMensaje("El insumo no existe.", false);
                CargarGrilla();
                return;
            }

            hdnIdInsumo.Value = insumo.IdInsumo.ToString();
            hdnActivo.Value = insumo.Activo.ToString();
            txtNombre.Text = insumo.Nombre;
            txtMarca.Text = insumo.Marca;
            ddlUnidadMedida.SelectedValue = insumo.UnidadMedida ?? string.Empty;
            txtStockMinimo.Text = insumo.StockMinimo.ToString("N2");
            txtPrecioVenta.Text = insumo.PrecioVenta.ToString("N2");
            btnBorrar.Visible = insumo.Activo;
            btnReactivar.Visible = !insumo.Activo;

            pnlStockInicial.Visible = false;
            pnlStockActual.Visible = true;
            litStockActual.Text = insumo.StockActual.ToString("N2");

            litTituloFormulario.Text = insumo.Activo ? "Editar insumo" : "Editar insumo (inactivo)";

            // Editando, el modal se agranda para sumar el ajuste al costado y el historial abajo.
            pnlDialogo.CssClass = "modal-dialog modal-xl";
            pnlDatos.CssClass = "col-lg-7";
            pnlAjuste.Visible = true;
            pnlHistorial.Visible = true;
            gvHistorial.DataSource = MovimientoStockDAL.ListarPorInsumo(idInsumo);
            gvHistorial.DataBind();

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
            hdnIdInsumo.Value = string.Empty;
            hdnActivo.Value = bool.TrueString;
            txtNombre.Text = string.Empty;
            txtMarca.Text = string.Empty;
            if (ddlUnidadMedida.Items.Count > 0) ddlUnidadMedida.SelectedIndex = 0;
            txtStockMinimo.Text = string.Empty;
            txtPrecioVenta.Text = string.Empty;
            btnBorrar.Visible = false;
            btnReactivar.Visible = false;
            litTituloFormulario.Text = "Nuevo insumo";

            txtStockInicial.Text = string.Empty;
            pnlStockInicial.Visible = true;
            pnlStockActual.Visible = false;

            pnlDialogo.CssClass = "modal-dialog modal-lg";
            pnlDatos.CssClass = "col-12";
            pnlAjuste.Visible = false;
            pnlHistorial.Visible = false;
            txtCantidadAjuste.Text = string.Empty;
            txtMotivoAjuste.Text = string.Empty;
        }

        private void MostrarFormulario()
        {
            Interfaz.AbrirModal(this, IdModal);
        }

        // Aviso adentro del modal (error al guardar, resultado de un ajuste), que vuelve a abrirse.
        private void MostrarMensajeFormulario(string mensajeHtml, bool exito)
        {
            pnlMensajeFormulario.CssClass = "alert " + (exito ? "alert-success" : "alert-danger");
            litMensajeFormulario.Text = mensajeHtml;
            pnlMensajeFormulario.Visible = true;
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
