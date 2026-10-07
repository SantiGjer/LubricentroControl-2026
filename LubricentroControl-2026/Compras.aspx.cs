using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // ABM de compras a proveedores (Fase 4). Admin y Encargado, acceso completo; Empleado, solo
    // consulta (Requerimientos §5), mismo patrón que Proveedores/Insumos. A diferencia de
    // Órdenes de trabajo, una compra no tiene alta progresiva ni se edita después de creada: las
    // líneas se arman en memoria (ViewState) mientras se transcribe la factura del proveedor, y
    // "Guardar compra" persiste todo junto (ComprobanteCompraDAL.Crear). El alta y la vista de una
    // compra ya registrada comparten el mismo modal.
    public partial class Compras : PaginaSegura
    {
        // Índice de la columna "Acciones" en gvCompras.Columns.
        private const int ColumnaAcciones = 6;

        private const string IdModal = "modalCompra";

        // Líneas todavía no guardadas de la compra en curso.
        private List<DetalleCompra> LineasPendientes
        {
            get
            {
                var lista = ViewState["LineasPendientes"] as List<DetalleCompra>;
                if (lista == null)
                {
                    lista = new List<DetalleCompra>();
                    ViewState["LineasPendientes"] = lista;
                }
                return lista;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Los avisos se muestran una sola vez (el Literal guarda su texto en el ViewState).
            pnlMensaje.Visible = false;
            pnlMensajeFormulario.Visible = false;

            if (IsPostBack) return;

            if (EsSoloLectura)
            {
                btnNuevaCompra.Visible = false;
                pnlFormulario.Visible = false;
                gvCompras.Columns[ColumnaAcciones].Visible = false;
            }

            CargarCondicionPago();
            CargarMedioPago();
            CargarInsumos();
            LimpiarFormulario();
            CargarGrilla();
        }

        // Opciones del selector de proveedor (los activos). Se evalúa al dibujar el modal.
        protected string OpcionesProveedores
        {
            get { return Selectores.OpcionesProveedores(); }
        }

        private void CargarCondicionPago()
        {
            ddlCondicionPago.Items.Clear();
            foreach (var condicion in ComprobanteCompra.CondicionesPago)
                ddlCondicionPago.Items.Add(new ListItem(condicion, condicion));
        }

        private void CargarMedioPago()
        {
            ddlMedioPago.Items.Clear();
            ddlMedioPago.Items.Add(new ListItem("(seleccioná)", ""));
            foreach (var medio in ComprobanteCompra.MediosPago)
                ddlMedioPago.Items.Add(new ListItem(medio, medio));
        }

        private void CargarInsumos()
        {
            ddlInsumo.Items.Clear();
            foreach (var insumo in InsumoDAL.Listar(incluirInactivos: false))
                ddlInsumo.Items.Add(new ListItem(
                    insumo.Nombre + " (stock: " + insumo.StockActual.ToString("N2") + ")",
                    insumo.IdInsumo.ToString()));
        }

        protected void ddlCondicionPago_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActualizarVisibilidadMedioPago();
        }

        private void ActualizarVisibilidadMedioPago()
        {
            pnlMedioPago.Visible = ddlCondicionPago.SelectedValue == ComprobanteCompra.CondicionContado;
        }

        // "Solo con saldo pendiente" filtra en el servidor; el texto, la tabla en el navegador.
        private void CargarGrilla()
        {
            gvCompras.DataSource = ComprobanteCompraDAL.Listar(chkSoloConSaldo.Checked);
            gvCompras.DataBind();
        }

        // El filtro de la tabla busca también por CUIT, que no es una columna: va en data-buscar.
        protected void gvCompras_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var cuit = ((ComprobanteCompra)e.Row.DataItem).Cuit;
            e.Row.Attributes["data-buscar"] = cuit + " " + Proveedor.FormatearCuit(cuit);
        }

        protected void chkSoloConSaldo_CheckedChanged(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        protected void valProveedor_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = LeerIdOculto(hdnIdProveedor.Value) > 0;
        }

        protected void valMedioPago_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = ddlCondicionPago.SelectedValue != ComprobanteCompra.CondicionContado
                || !string.IsNullOrEmpty(ddlMedioPago.SelectedValue);
        }

        // --- Líneas en memoria (todavía no persistidas; postbacks parciales del UpdatePanel) ---

        protected void btnAgregarLinea_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            decimal cantidad, precio;
            if (!decimal.TryParse(txtCantidadLinea.Text, out cantidad) || cantidad <= 0)
            {
                MostrarMensajeFormulario("La cantidad de la línea debe ser un número mayor a cero.", false);
                return;
            }
            if (!decimal.TryParse(txtPrecioLinea.Text, out precio) || precio < 0)
            {
                MostrarMensajeFormulario("El precio unitario debe ser un número mayor o igual a cero.", false);
                return;
            }

            var idInsumo = LeerIdOculto(ddlInsumo.SelectedValue);
            var insumo = InsumoDAL.ObtenerPorId(idInsumo);
            if (insumo == null)
            {
                MostrarMensajeFormulario("El insumo seleccionado no existe.", false);
                return;
            }

            LineasPendientes.Add(new DetalleCompra
            {
                IdInsumo = idInsumo,
                NombreInsumo = insumo.Nombre,
                Cantidad = cantidad,
                PrecioUnitario = precio
            });

            txtCantidadLinea.Text = string.Empty;
            txtPrecioLinea.Text = string.Empty;
            CargarLineasPendientes();
        }

        protected void gvLineasPendientes_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;
            if (e.CommandName != "Quitar") return;

            var indice = Convert.ToInt32(e.CommandArgument);
            if (indice >= 0 && indice < LineasPendientes.Count)
                LineasPendientes.RemoveAt(indice);

            CargarLineasPendientes();
        }

        private void CargarLineasPendientes()
        {
            gvLineasPendientes.DataSource = LineasPendientes;
            gvLineasPendientes.DataBind();
        }

        // --- ABM ------------------------------------------------------------------------

        protected void btnNuevaCompra_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            LimpiarFormulario();
            MostrarFormulario();
        }

        protected void btnGuardarCompra_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid)
            {
                MostrarFormulario();
                return;
            }

            decimal impuestos;
            decimal.TryParse(txtImpuestos.Text, out impuestos);

            var compra = new ComprobanteCompra
            {
                IdProveedor = LeerIdOculto(hdnIdProveedor.Value),
                CondicionPago = ddlCondicionPago.SelectedValue,
                MedioPago = ddlCondicionPago.SelectedValue == ComprobanteCompra.CondicionContado
                    ? ddlMedioPago.SelectedValue : null,
                Impuestos = impuestos
            };

            var resultado = ComprobanteCompraDAL.Crear(compra, LineasPendientes, UsuarioActual.IdUsuario);

            if (!resultado.Exito)
            {
                MostrarMensajeFormulario(resultado.Mensaje, false);
                return;
            }

            // El modal pasa a mostrar la compra recién registrada (con su número ya asignado).
            ViewState["LineasPendientes"] = new List<DetalleCompra>();
            CargarGrilla();
            Seleccionar(compra.IdCompra);
            MostrarMensajeFormulario(resultado.Mensaje, true);
        }

        protected void gvCompras_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;
            if (e.CommandName != "Ver") return;

            Seleccionar(LeerIdOculto(Convert.ToString(e.CommandArgument)));
        }

        private void Seleccionar(int idCompra)
        {
            var compra = ComprobanteCompraDAL.ObtenerPorId(idCompra);
            if (compra == null)
            {
                MostrarMensaje("La compra no existe.", false);
                CargarGrilla();
                return;
            }

            hdnIdCompra.Value = compra.IdCompra.ToString();
            pnlAltaCompra.Visible = false;
            pnlCompraExistente.Visible = true;
            btnGuardarCompra.Visible = false;
            litBotonCerrar.Text = "Cerrar";

            litProveedorInfo.Text = Server.HtmlEncode(compra.RazonSocial);
            litFechaInfo.Text = compra.Fecha.ToString("dd/MM/yyyy HH:mm");
            litCondicionInfo.Text = compra.CondicionPago;
            litMedioPagoInfo.Text = compra.MedioPago ?? "—";
            litSubtotalInfo.Text = compra.Subtotal.ToString("N2");
            litImpuestosInfo.Text = compra.Impuestos.ToString("N2");
            litTotalInfo.Text = compra.Total.ToString("N2");
            litSaldoInfo.Text = compra.SaldoPendiente.ToString("N2");

            gvDetalleCompra.DataSource = DetalleCompraDAL.ListarPorCompra(idCompra);
            gvDetalleCompra.DataBind();

            litTituloFormulario.Text = "Compra " + compra.NumeroComprobante;
            MostrarFormulario();
        }

        // 0 (ID inexistente, cae en "no existe"/valida en falso) si el campo oculto llegara
        // vacío o manipulado, en vez de reventar con FormatException.
        private static int LeerIdOculto(string valor)
        {
            int id;
            return int.TryParse(valor, out id) ? id : 0;
        }

        private void LimpiarFormulario()
        {
            hdnIdCompra.Value = string.Empty;
            pnlAltaCompra.Visible = true;
            pnlCompraExistente.Visible = false;
            btnGuardarCompra.Visible = true;
            litBotonCerrar.Text = "Cancelar";

            hdnIdProveedor.Value = string.Empty;
            txtProveedor.Text = string.Empty;
            ddlCondicionPago.SelectedIndex = 0;
            ActualizarVisibilidadMedioPago();
            ddlMedioPago.SelectedIndex = 0;
            txtImpuestos.Text = "0";
            txtCantidadLinea.Text = string.Empty;
            txtPrecioLinea.Text = string.Empty;
            ViewState["LineasPendientes"] = new List<DetalleCompra>();
            CargarLineasPendientes();

            litTituloFormulario.Text = "Nueva compra";
        }

        private void MostrarFormulario()
        {
            Interfaz.AbrirModal(this, IdModal);
        }

        // Aviso adentro del modal (error al guardar o al agregar una línea, compra registrada).
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
