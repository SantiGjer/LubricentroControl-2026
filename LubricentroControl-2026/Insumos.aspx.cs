using System;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // ABM de insumos. Admin y Encargado, acceso completo; Empleado, solo consulta
    // (Requerimientos §5): se le esconde todo el formulario (incluido el ajuste de stock
    // y el historial), la columna de Acciones, y los métodos de escritura del DAL igual
    // rechazan la operación por las dudas.
    public partial class Insumos : PaginaSegura
    {
        // Índice de la columna "Acciones" en gvInsumos.Columns — no tiene sentido para el
        // Empleado si no hay formulario donde cargar la selección.
        private const int ColumnaAcciones = 7;

        protected void Page_Load(object sender, EventArgs e)
        {
            pnlSoloLectura.Visible = EsSoloLectura;

            if (IsPostBack) return;

            CargarUnidadesMedida();

            if (EsSoloLectura)
            {
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

        private void CargarGrilla()
        {
            var incluirInactivos = chkIncluirInactivos.Checked;
            var texto = txtBuscar.Text;

            gvInsumos.DataSource = string.IsNullOrWhiteSpace(texto)
                ? InsumoDAL.Listar(incluirInactivos)
                : InsumoDAL.Buscar(texto, incluirInactivos);
            gvInsumos.DataBind();
        }

        protected void gvInsumos_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var insumo = (Insumo)e.Row.DataItem;
            if (insumo.Activo && insumo.StockActual < insumo.StockMinimo)
                e.Row.Style.Add("background-color", "#f8d7da");
        }

        protected void gvInsumos_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvInsumos.PageIndex = e.NewPageIndex;
            CargarGrilla();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            gvInsumos.PageIndex = 0;
            CargarGrilla();
        }

        protected void chkIncluirInactivos_CheckedChanged(object sender, EventArgs e)
        {
            gvInsumos.PageIndex = 0;
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
                MostrarMensaje(resultado.Mensaje, false);
                return;
            }

            MostrarMensaje(resultado.Mensaje, true);
            LimpiarFormulario();
            gvInsumos.PageIndex = 0;
            CargarGrilla();
        }

        protected void btnBorrar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var baja = InsumoDAL.Desactivar(LeerIdOculto(hdnIdInsumo.Value));

            MostrarMensaje(baja.Mensaje, baja.Exito);
            LimpiarFormulario();
            gvInsumos.PageIndex = 0;
            CargarGrilla();
        }

        protected void btnRegistrarAjuste_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid) return;

            var idInsumo = LeerIdOculto(hdnIdInsumo.Value);
            decimal cantidad;
            decimal.TryParse(txtCantidadAjuste.Text, out cantidad);

            // Un solo campo con signo: positivo suma stock, negativo resta. RegistrarAjusteManual
            // sigue pidiendo la cantidad siempre positiva + el signo aparte, así que se traduce acá.
            var resultado = MovimientoStockDAL.RegistrarAjusteManual(
                idInsumo, Math.Abs(cantidad), cantidad > 0, txtMotivoAjuste.Text, UsuarioActual.IdUsuario);

            MostrarMensaje(resultado.Mensaje, resultado.Exito);

            if (resultado.Exito)
            {
                txtCantidadAjuste.Text = string.Empty;
                txtMotivoAjuste.Text = string.Empty;
                Seleccionar(idInsumo);
                gvInsumos.PageIndex = 0;
                CargarGrilla();
            }
        }

        protected void gvInsumos_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;
            if (e.CommandName != "Seleccionar") return;

            Seleccionar(LeerIdOculto(Convert.ToString(e.CommandArgument)));
        }

        private void Seleccionar(int idInsumo)
        {
            var insumo = InsumoDAL.ObtenerPorId(idInsumo);
            if (insumo == null)
            {
                MostrarMensaje("El insumo no existe.", false);
                pnlAjusteHistorial.Visible = false;
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

            txtStockInicial.Visible = false;
            litStockActual.Visible = true;
            litStockActual.Text = "Stock actual: " + insumo.StockActual.ToString("N2");

            litTituloFormulario.Text = "Editar insumo";

            pnlAjusteHistorial.Visible = true;
            gvHistorial.DataSource = MovimientoStockDAL.ListarPorInsumo(idInsumo);
            gvHistorial.DataBind();
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
            litTituloFormulario.Text = "Nuevo insumo";

            txtStockInicial.Text = string.Empty;
            txtStockInicial.Visible = true;
            litStockActual.Visible = false;

            pnlAjusteHistorial.Visible = false;
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
