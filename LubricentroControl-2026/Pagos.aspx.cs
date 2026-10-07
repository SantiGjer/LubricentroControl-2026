using System;
using System.Web;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // Alta de pagos de cliente o de proveedor (Fase 4, última pantalla). Acceso completo para
    // Admin, Encargado y Empleado (Requerimientos §5) — a diferencia de Compras/Cuentas
    // corrientes, acá Empleado también puede cobrar —; Lectura, solo consulta. Un pago no se edita
    // ni se borra una vez cargado. No se elige comprobante: PagoDAL.Registrar cancela primero las
    // deudas más viejas y deja el sobrante a favor. El formulario se abre en un modal sobre la
    // lista, y se abre solo cuando Órdenes de trabajo manda a cobrar la venta de un cliente sin
    // cuenta corriente (?idVenta=).
    public partial class Pagos : PaginaSegura
    {
        private const string IdModal = "modalPago";

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

            CargarTipo();
            CargarMedioPago();
            CargarGrilla();

            if (!EsSoloLectura && Request.QueryString["idVenta"] != null)
                PrepararCobroDeVenta(LeerIdOculto(Request.QueryString["idVenta"]));
        }

        // Opciones de los selectores de titular (activos). Se evalúan al dibujar el modal.
        protected string OpcionesClientes
        {
            get { return Selectores.OpcionesClientes(); }
        }

        protected string OpcionesProveedores
        {
            get { return Selectores.OpcionesProveedores(); }
        }

        // Llegada desde Órdenes de trabajo al cerrar la orden de un cliente sin cuenta corriente:
        // el formulario se abre con el cliente y el saldo de esa venta ya cargados. El monto se
        // lee de la base, no del query string.
        private void PrepararCobroDeVenta(int idVenta)
        {
            var venta = ComprobanteVentaDAL.ObtenerPorId(idVenta);
            if (venta == null || venta.SaldoPendiente <= 0) return;

            LimpiarFormulario();
            SeleccionarCliente(venta.IdCliente);
            txtMonto.Text = venta.SaldoPendiente.ToString("0.00");
            txtObservaciones.Text = "Cobro de la venta " + venta.NumeroComprobante;

            litVieneDeOrden.Text = "La orden se cerró y generó la venta <b>" + HttpUtility.HtmlEncode(venta.NumeroComprobante)
                + "</b> por <b>$" + venta.Total.ToString("N2") + "</b>. Como el cliente no tiene cuenta corriente, "
                + "registrá ahora el cobro de los <b>$" + venta.SaldoPendiente.ToString("N2") + "</b> pendientes.";
            pnlVieneDeOrden.Visible = true;

            MostrarFormulario();
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
            txtCliente.Text = string.Empty;
            hdnIdProveedor.Value = string.Empty;
            txtProveedor.Text = string.Empty;
            lblSaldoTitular.Visible = false;
        }

        // El filtro por texto lo hace la tabla en el navegador (Lubricentro.js).
        private void CargarGrilla()
        {
            gvPagos.DataSource = PagoDAL.Listar();
            gvPagos.DataBind();
        }

        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            LimpiarFormulario();
            MostrarFormulario();
        }

        // --- Selector de cliente ----------------------------------------------------------

        // Lo dispara el selector con búsqueda al elegir (postback parcial): muestra el saldo.
        protected void hdnIdCliente_ValueChanged(object sender, EventArgs e)
        {
            SeleccionarCliente(LeerIdOculto(hdnIdCliente.Value));
        }

        private void SeleccionarCliente(int idCliente)
        {
            var cliente = ClienteDAL.ObtenerPorId(idCliente);
            if (cliente == null) return;

            hdnIdCliente.Value = cliente.IdCliente.ToString();
            txtCliente.Text = Selectores.TextoCliente(cliente);

            MostrarSaldo(CuentaCorrienteClienteDAL.ObtenerSaldoActual(idCliente));
        }

        protected void valCliente_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = ddlTipo.SelectedValue != Pago.TipoCliente || LeerIdOculto(hdnIdCliente.Value) > 0;
        }

        // --- Selector de proveedor ----------------------------------------------------------

        protected void hdnIdProveedor_ValueChanged(object sender, EventArgs e)
        {
            SeleccionarProveedor(LeerIdOculto(hdnIdProveedor.Value));
        }

        private void SeleccionarProveedor(int idProveedor)
        {
            var proveedor = ProveedorDAL.ObtenerPorId(idProveedor);
            if (proveedor == null) return;

            hdnIdProveedor.Value = proveedor.IdProveedor.ToString();
            txtProveedor.Text = Selectores.TextoProveedor(proveedor);

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
            if (!Page.IsValid)
            {
                MostrarFormulario();
                return;
            }

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

            if (!resultado.Exito)
            {
                litErrorFormulario.Text = resultado.Mensaje;
                pnlErrorFormulario.Visible = true;
                MostrarFormulario();
                return;
            }

            MostrarMensaje(resultado.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
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
            txtCliente.Text = string.Empty;
            hdnIdProveedor.Value = string.Empty;
            txtProveedor.Text = string.Empty;
            lblSaldoTitular.Visible = false;
            ddlMedioPago.SelectedIndex = 0;
            txtMonto.Text = string.Empty;
            txtObservaciones.Text = string.Empty;
            pnlVieneDeOrden.Visible = false;
        }

        private void MostrarFormulario()
        {
            Interfaz.AbrirModal(this, IdModal);
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
