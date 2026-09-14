using System;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // Alta de pagos de cliente o de proveedor (Fase 4, última pantalla). Acceso completo para
    // los 3 roles (Requerimientos §5) — a diferencia de Compras/Cuentas corrientes, acá Empleado
    // también puede cobrar. Un pago no se edita ni se borra una vez cargado.
    public partial class Pagos : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            pnlSoloLectura.Visible = EsSoloLectura;

            if (IsPostBack) return;

            CargarTipo();
            CargarMedioPago();
            LimpiarComprobante();
            CargarGrilla();
        }

        private void CargarTipo()
        {
            ddlTipo.Items.Clear();
            ddlTipo.Items.Add(new ListItem("Cliente", Pago.TipoCliente));
            ddlTipo.Items.Add(new ListItem("Proveedor", Pago.TipoProveedor));
        }

        private void CargarMedioPago()
        {
            ddlMedioPago.Items.Clear();
            foreach (var medio in Pago.MediosPago)
                ddlMedioPago.Items.Add(new ListItem(medio, medio));
        }

        // Deja ddlComprobante con solo su placeholder. Tiene que quedar poblado así ya en el
        // primer Page_Load (mismo bug ya documentado con ddlVehiculo/ddlEstado/ddlMedioPago en
        // sesiones anteriores: un DropDownList sin ningún <option> rechaza cualquier valor
        // posteado, incluso "").
        private void LimpiarComprobante()
        {
            ddlComprobante.Items.Clear();
            ddlComprobante.Items.Add(new ListItem("(a cuenta general)", ""));
        }

        protected void ddlTipo_SelectedIndexChanged(object sender, EventArgs e)
        {
            var esCliente = ddlTipo.SelectedValue == Pago.TipoCliente;
            pnlCliente.Visible = esCliente;
            pnlProveedor.Visible = !esCliente;

            hdnIdCliente.Value = string.Empty;
            litClienteSeleccionado.Text = "(sin seleccionar)";
            hdnIdProveedor.Value = string.Empty;
            litProveedorSeleccionado.Text = "(sin seleccionar)";
            LimpiarComprobante();
        }

        private void CargarGrilla()
        {
            var texto = txtBuscar.Text;

            gvPagos.DataSource = string.IsNullOrWhiteSpace(texto)
                ? PagoDAL.Listar()
                : PagoDAL.Buscar(texto);
            gvPagos.DataBind();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        // --- Selector de cliente ----------------------------------------------------------

        protected void btnBuscarCliente_Click(object sender, EventArgs e)
        {
            rptResultadosCliente.DataSource = ClienteDAL.Buscar(txtBuscarCliente.Text, incluirInactivos: false);
            rptResultadosCliente.DataBind();
            pnlResultadosCliente.Visible = true;
        }

        protected void rptResultadosCliente_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Seleccionar") return;

            SeleccionarCliente(LeerIdOculto(Convert.ToString(e.CommandArgument)));

            pnlResultadosCliente.Visible = false;
            txtBuscarCliente.Text = string.Empty;
        }

        private void SeleccionarCliente(int idCliente)
        {
            var cliente = ClienteDAL.ObtenerPorId(idCliente);
            if (cliente == null) return;

            hdnIdCliente.Value = cliente.IdCliente.ToString();
            litClienteSeleccionado.Text = cliente.NombreCompleto + " — DNI " + cliente.Dni;

            LimpiarComprobante();
            foreach (var venta in ComprobanteVentaDAL.ListarPendientesPorCliente(idCliente))
                ddlComprobante.Items.Add(new ListItem(
                    venta.NumeroComprobante + " (saldo: " + venta.SaldoPendiente.ToString("N2") + ")",
                    venta.IdVenta.ToString()));
        }

        protected void valCliente_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = ddlTipo.SelectedValue != Pago.TipoCliente || LeerIdOculto(hdnIdCliente.Value) > 0;
        }

        // --- Selector de proveedor ----------------------------------------------------------

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

            LimpiarComprobante();
            foreach (var compra in ComprobanteCompraDAL.ListarPendientesPorProveedor(idProveedor))
                ddlComprobante.Items.Add(new ListItem(
                    compra.NumeroComprobante + " (saldo: " + compra.SaldoPendiente.ToString("N2") + ")",
                    compra.IdCompra.ToString()));
        }

        protected void valProveedor_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = ddlTipo.SelectedValue != Pago.TipoProveedor || LeerIdOculto(hdnIdProveedor.Value) > 0;
        }

        // --- Alta -----------------------------------------------------------------------

        protected void btnRegistrar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var esCliente = ddlTipo.SelectedValue == Pago.TipoCliente;
            var idComprobante = LeerIdOculto(ddlComprobante.SelectedValue);

            decimal monto;
            decimal.TryParse(txtMonto.Text, out monto);

            var pago = new Pago
            {
                Tipo = ddlTipo.SelectedValue,
                IdCliente = esCliente ? LeerIdOculto(hdnIdCliente.Value) : (int?)null,
                IdProveedor = !esCliente ? LeerIdOculto(hdnIdProveedor.Value) : (int?)null,
                IdVenta = esCliente && idComprobante > 0 ? idComprobante : (int?)null,
                IdCompra = !esCliente && idComprobante > 0 ? idComprobante : (int?)null,
                IdUsuario = UsuarioActual.IdUsuario,
                MedioPago = ddlMedioPago.SelectedValue,
                Monto = monto,
                Observaciones = txtObservaciones.Text
            };

            var resultado = PagoDAL.Registrar(pago);

            MostrarMensaje(resultado.Mensaje, resultado.Exito);

            if (resultado.Exito)
            {
                LimpiarFormulario();
                CargarGrilla();
            }
        }

        // 0 (ID inexistente, cae en "no existe"/valida en falso) si el campo llegara vacío o
        // manipulado, en vez de reventar con FormatException.
        private static int LeerIdOculto(string valor)
        {
            int id;
            return int.TryParse(valor, out id) ? id : 0;
        }

        private void LimpiarFormulario()
        {
            ddlTipo.SelectedIndex = 0;
            pnlCliente.Visible = true;
            pnlProveedor.Visible = false;
            hdnIdCliente.Value = string.Empty;
            litClienteSeleccionado.Text = "(sin seleccionar)";
            txtBuscarCliente.Text = string.Empty;
            pnlResultadosCliente.Visible = false;
            hdnIdProveedor.Value = string.Empty;
            litProveedorSeleccionado.Text = "(sin seleccionar)";
            txtBuscarProveedor.Text = string.Empty;
            pnlResultadosProveedor.Visible = false;
            LimpiarComprobante();
            ddlMedioPago.SelectedIndex = 0;
            txtMonto.Text = string.Empty;
            txtObservaciones.Text = string.Empty;
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
