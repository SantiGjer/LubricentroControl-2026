using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // Cuenta corriente de clientes (Fase 4). Calco de CuentaCorrienteProveedores.aspx: Admin y
    // Encargado pueden ver el historial y registrar ajustes manuales; Empleado, solo consulta
    // (Requerimientos §5) — "solo consulta" acá no esconde toda la pantalla, solo el ajuste y el
    // botón "Editar cliente". La lista arranca mostrando solo a los clientes con la cuenta corriente
    // habilitada (Cliente.CuentaCorriente, opción "Cuenta corriente"); "Editar cliente" lleva a
    // Clientes.aspx para cambiarla.
    public partial class CuentaCorrienteClientes : PaginaSegura
    {
        private const string IdModal = "modalCuenta";

        // Saldo actual de cada cliente con movimientos de saldo distinto de cero (el resto, 0).
        private Dictionary<int, decimal> saldos = new Dictionary<int, decimal>();

        // Quien puede escribir acá y en Clientes ve el botón "Editar cliente" (el interruptor
        // de cuenta corriente de Clientes.aspx sigue el mismo permiso).
        protected bool PuedeEditarCliente { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Los avisos se muestran una sola vez (el Literal guarda su texto en el ViewState).
            pnlMensaje.Visible = false;
            pnlMensajeFormulario.Visible = false;

            var permisoClientes = MenuDAL.ObtenerPermiso(UsuarioActual.IdNivel, "~/Clientes");
            PuedeEditarCliente = !EsSoloLectura && permisoClientes != null && !permisoClientes.SoloLectura;

            if (IsPostBack) return;

            CargarGrilla();
        }

        protected string UrlEditarCliente(int idCliente)
        {
            return "~/Clientes?editar=" + idCliente + "&volver=ctacte";
        }

        // Todos los clientes con su saldo: el texto y las opciones (cuenta habilitada o no, saldo)
        // los filtra la tabla en el navegador (Lubricentro.js).
        private void CargarGrilla()
        {
            saldos = CuentaCorrienteClienteDAL.ListarSaldos().ToDictionary(s => s.IdCliente, s => s.Saldo);

            gvClientes.DataSource = ClienteDAL.Listar(incluirInactivos: true);
            gvClientes.DataBind();
        }

        protected decimal SaldoDe(int idCliente)
        {
            decimal saldo;
            return saldos.TryGetValue(idCliente, out saldo) ? saldo : 0m;
        }

        // data-saldo para la opción "Saldo": positivo = el cliente debe, negativo = a su favor.
        protected void gvClientes_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var saldo = SaldoDe(((Cliente)e.Row.DataItem).IdCliente);
            e.Row.Attributes["data-saldo"] = saldo > 0 ? "debe" : saldo < 0 ? "a-favor" : "cero";
        }

        protected void gvClientes_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Ver") return;

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

            pnlAjuste.Visible = !EsSoloLectura;
            pnlHistorial.CssClass = EsSoloLectura ? "col-12" : "col-lg-8";
            pnlSinCuenta.Visible = !cliente.CuentaCorriente;
            lnkEditarCliente.Visible = PuedeEditarCliente;
            lnkEditarCliente.NavigateUrl = UrlEditarCliente(idCliente);

            litClienteSeleccionado.Text = "Cuenta corriente: " + Server.HtmlEncode(cliente.Denominacion);
            litSaldoActual.Text = CuentaCorrienteClienteDAL.ObtenerSaldoActual(idCliente).ToString("N2");

            gvHistorial.DataSource = CuentaCorrienteClienteDAL.ListarPorCliente(idCliente);
            gvHistorial.DataBind();

            Interfaz.AbrirModal(this, IdModal);
        }

        protected void btnRegistrarAjuste_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var idCliente = LeerIdOculto(Convert.ToString(ViewState["IdCliente"]));
            if (idCliente <= 0)
            {
                MostrarMensaje("Seleccioná un cliente antes de registrar un ajuste.", false);
                return;
            }

            if (!Page.IsValid)
            {
                Interfaz.AbrirModal(this, IdModal);
                return;
            }

            decimal monto;
            decimal.TryParse(txtMontoAjuste.Text, out monto);

            var resultado = CuentaCorrienteClienteDAL.RegistrarAjuste(
                idCliente, monto, txtMotivoAjuste.Text, UsuarioActual.IdUsuario);

            if (resultado.Exito)
            {
                txtMontoAjuste.Text = string.Empty;
                txtMotivoAjuste.Text = string.Empty;
                CargarGrilla();
                Seleccionar(idCliente);
            }
            else
            {
                Interfaz.AbrirModal(this, IdModal);
            }

            pnlMensajeFormulario.CssClass = "alert " + (resultado.Exito ? "alert-success" : "alert-danger");
            litMensajeFormulario.Text = resultado.Mensaje;
            pnlMensajeFormulario.Visible = true;
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
