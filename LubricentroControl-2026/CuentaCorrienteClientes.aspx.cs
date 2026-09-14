using System;
using BIZ.Data;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // Cuenta corriente de clientes (Fase 4). Calco de CuentaCorrienteProveedores.aspx: Admin y
    // Encargado pueden ver el historial y registrar ajustes manuales; Empleado, solo consulta
    // (Requerimientos §5) — "solo consulta" acá no esconde toda la pantalla, solo la franja de
    // ajuste (mismo criterio que la cuenta corriente de proveedores).
    public partial class CuentaCorrienteClientes : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            pnlSoloLectura.Visible = EsSoloLectura;

            if (IsPostBack) return;

            CargarGrilla();
        }

        private void CargarGrilla()
        {
            var texto = txtBuscar.Text;

            gvClientes.DataSource = string.IsNullOrWhiteSpace(texto)
                ? ClienteDAL.Listar(incluirInactivos: true)
                : ClienteDAL.Buscar(texto, incluirInactivos: true);
            gvClientes.DataBind();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        protected void gvClientes_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Seleccionar") return;

            Seleccionar(LeerIdOculto(Convert.ToString(e.CommandArgument)));
        }

        private void Seleccionar(int idCliente)
        {
            var cliente = ClienteDAL.ObtenerPorId(idCliente);
            if (cliente == null)
            {
                MostrarMensaje("El cliente no existe.", false);
                CargarGrilla();
                return;
            }

            ViewState["IdCliente"] = idCliente;

            pnlDetalle.Visible = true;
            pnlAjuste.Visible = !EsSoloLectura;
            litClienteSeleccionado.Text = "Cuenta corriente: " + cliente.NombreCompleto;
            litSaldoActual.Text = CuentaCorrienteClienteDAL.ObtenerSaldoActual(idCliente).ToString("N2");

            gvHistorial.DataSource = CuentaCorrienteClienteDAL.ListarPorCliente(idCliente);
            gvHistorial.DataBind();
        }

        protected void btnRegistrarAjuste_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid) return;

            var idCliente = LeerIdOculto(Convert.ToString(ViewState["IdCliente"]));
            if (idCliente <= 0)
            {
                MostrarMensaje("Seleccioná un cliente antes de registrar un ajuste.", false);
                return;
            }

            decimal monto;
            decimal.TryParse(txtMontoAjuste.Text, out monto);

            var resultado = CuentaCorrienteClienteDAL.RegistrarAjuste(
                idCliente, monto, txtMotivoAjuste.Text, UsuarioActual.IdUsuario);

            MostrarMensaje(resultado.Mensaje, resultado.Exito);

            if (resultado.Exito)
            {
                txtMontoAjuste.Text = string.Empty;
                txtMotivoAjuste.Text = string.Empty;
                Seleccionar(idCliente);
            }
        }

        // 0 (ID inexistente, cae en "no existe"/valida en falso) si el campo llegara vacío o
        // manipulado, en vez de reventar con FormatException.
        private static int LeerIdOculto(string valor)
        {
            int id;
            return int.TryParse(valor, out id) ? id : 0;
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
