using System;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // Alta de pagos de cliente o de proveedor (Fase 4, última pantalla). Acceso completo para
    // los 3 roles (Requerimientos §5) — a diferencia de Compras/Cuentas corrientes, acá Empleado
    // también puede cobrar. Un pago no se edita ni se borra una vez cargado. No se elige
    // comprobante: PagoDAL.Registrar cancela primero las deudas más viejas y deja el sobrante a favor.
    public partial class Pagos : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            if (EsSoloLectura)
                pnlFormulario.Visible = false;

            CargarTipo();
            CargarMedioPago();
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

        // Texto del saldo actual del titular elegido: positivo = deuda, negativo = a favor.
        private void MostrarSaldo(decimal saldo)
        {
            lblSaldoTitular.Visible = true;
            lblSaldoTitular.Text = saldo > 0
                ? "Deuda actual: " + saldo.ToString("N2")
                : saldo < 0
                    ? "Saldo a favor: " + (-saldo).ToString("N2")
                    : "Sin deuda.";
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
            lblSaldoTitular.Visible = false;
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

            MostrarSaldo(CuentaCorrienteClienteDAL.ObtenerSaldoActual(idCliente));
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

            MostrarSaldo(CuentaCorrienteProveedorDAL.ObtenerSaldoActual(idProveedor));
        }

        protected void valProveedor_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = ddlTipo.SelectedValue != Pago.TipoProveedor || LeerIdOculto(hdnIdProveedor.Value) > 0;
        }

        // --- Alta -----------------------------------------------------------------------

        protected void btnRegistrar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid) return;

            var esCliente = ddlTipo.SelectedValue == Pago.TipoCliente;

            decimal monto;
            decimal.TryParse(txtMonto.Text, out monto);

            var pago = new Pago
            {
                Tipo = ddlTipo.SelectedValue,
                IdCliente = esCliente ? LeerIdOculto(hdnIdCliente.Value) : (int?)null,
                IdProveedor = !esCliente ? LeerIdOculto(hdnIdProveedor.Value) : (int?)null,
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
            lblSaldoTitular.Visible = false;
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
