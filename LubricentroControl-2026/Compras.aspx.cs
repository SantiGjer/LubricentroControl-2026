using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // ABM de compras a proveedores (Fase 4). Admin y Encargado, acceso completo; Empleado, solo
    // consulta (Requerimientos §5), mismo patrón que Proveedores/Insumos. A diferencia de
    // Órdenes de trabajo, una compra no tiene franja de alta progresiva ni se edita después de
    // creada: las líneas se arman en memoria (ViewState) mientras se transcribe la factura del
    // proveedor, y "Guardar compra" persiste todo junto (ComprobanteCompraDAL.Crear).
    public partial class Compras : PaginaSegura
    {
        // Índice de la columna "Acciones" en gvCompras.Columns.
        private const int ColumnaAcciones = 6;

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
            pnlSoloLectura.Visible = EsSoloLectura;

            if (IsPostBack) return;

            if (EsSoloLectura)
            {
                pnlFormulario.Visible = false;
                gvCompras.Columns[ColumnaAcciones].Visible = false;
            }

            CargarCondicionPago();
            CargarMedioPago();
            CargarInsumos();
            CargarGrilla();
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

        private void CargarGrilla()
        {
            var texto = txtBuscar.Text;

            gvCompras.DataSource = string.IsNullOrWhiteSpace(texto)
                ? ComprobanteCompraDAL.Listar(chkSoloConSaldo.Checked)
                : ComprobanteCompraDAL.Buscar(texto);
            gvCompras.DataBind();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        protected void chkSoloConSaldo_CheckedChanged(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        // --- Selector de proveedor: buscador desplegable dentro del UpdatePanel ---------

        protected void btnBuscarProveedor_Click(object sender, EventArgs e)
        {
            rptResultadosProveedor.DataSource = ProveedorDAL.Buscar(txtBuscarProveedor.Text, incluirInactivos: false);
            rptResultadosProveedor.DataBind();
            pnlResultadosProveedor.Visible = true;
        }

        protected void rptResultadosProveedor_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Seleccionar") return;

            SeleccionarProveedor(LeerIdOculto(Convert.ToString(e.CommandArgument)));

            pnlResultadosProveedor.Visible = false;
            txtBuscarProveedor.Text = string.Empty;
        }

        private void SeleccionarProveedor(int idProveedor)
        {
            var proveedor = ProveedorDAL.ObtenerPorId(idProveedor);
            if (proveedor == null) return;

            hdnIdProveedor.Value = proveedor.IdProveedor.ToString();
            litProveedorSeleccionado.Text = proveedor.RazonSocial;
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

        // --- Líneas en memoria (todavía no persistidas) ----------------------------------

        protected void btnAgregarLinea_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            decimal cantidad, precio;
            if (!decimal.TryParse(txtCantidadLinea.Text, out cantidad) || cantidad <= 0)
            {
                MostrarMensaje("La cantidad de la línea debe ser un número mayor a cero.", false);
                return;
            }
            if (!decimal.TryParse(txtPrecioLinea.Text, out precio) || precio < 0)
            {
                MostrarMensaje("El precio unitario debe ser un número mayor o igual a cero.", false);
                return;
            }

            var idInsumo = LeerIdOculto(ddlInsumo.SelectedValue);
            var insumo = InsumoDAL.ObtenerPorId(idInsumo);
            if (insumo == null)
            {
                MostrarMensaje("El insumo seleccionado no existe.", false);
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
        }

        protected void btnGuardarCompra_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid) return;

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
                MostrarMensaje(resultado.Mensaje, false);
                return;
            }

            MostrarMensaje(resultado.Mensaje, true);
            ViewState["LineasPendientes"] = new List<DetalleCompra>();
            CargarGrilla();
            Seleccionar(compra.IdCompra);
        }

        protected void gvCompras_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;
            if (e.CommandName != "Seleccionar") return;

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

            litProveedorInfo.Text = compra.RazonSocial;
            litFechaInfo.Text = compra.Fecha.ToString("dd/MM/yyyy HH:mm");
            litCondicionInfo.Text = compra.CondicionPago;
            litMedioPagoInfo.Text = compra.MedioPago ?? "—";
            litSubtotalInfo.Text = compra.Subtotal.ToString("N2");
            litImpuestosInfo.Text = compra.Impuestos.ToString("N2");
            litTotalInfo.Text = compra.Total.ToString("N2");
            litSaldoInfo.Text = compra.SaldoPendiente.ToString("N2");

            gvDetalleCompra.DataSource = DetalleCompraDAL.ListarPorCompra(idCompra);
            gvDetalleCompra.DataBind();

            litTituloFormulario.Text = compra.NumeroComprobante;
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

            hdnIdProveedor.Value = string.Empty;
            litProveedorSeleccionado.Text = "(sin seleccionar)";
            txtBuscarProveedor.Text = string.Empty;
            pnlResultadosProveedor.Visible = false;
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

        // El mensaje ya viene con HTML armado por el llamador, no se re-escapa acá.
        private void MostrarMensaje(string mensajeHtml, bool exito)
        {
            pnlMensaje.CssClass = "alert " + (exito ? "alert-success" : "alert-danger");
            litMensaje.Text = mensajeHtml;
            pnlMensaje.Visible = true;
        }
    }
}
