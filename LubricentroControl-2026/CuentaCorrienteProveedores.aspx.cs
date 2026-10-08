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
    // Cuenta corriente de proveedores (Fase 4). Admin y Encargado pueden ver el historial y
    // registrar ajustes manuales; Empleado, solo consulta (Requerimientos §5) — a diferencia de
    // Proveedores/Productos, acá "solo consulta" no esconde toda la pantalla: Empleado igual puede
    // buscar un proveedor y ver su historial/saldo, solo se le esconde el ajuste. El detalle se
    // abre en un modal sobre la lista.
    public partial class CuentaCorrienteProveedores : PaginaSegura
    {
        private const string IdModal = "modalCuenta";

        // Saldo actual de cada proveedor con saldo distinto de cero (el resto, 0).
        private Dictionary<int, decimal> saldos = new Dictionary<int, decimal>();

        protected void Page_Load(object sender, EventArgs e)
        {
            // Los avisos se muestran una sola vez (el Literal guarda su texto en el ViewState).
            pnlMensaje.Visible = false;
            pnlMensajeFormulario.Visible = false;

            if (IsPostBack) return;

            CargarGrilla();
        }

        // El filtro por texto y la opción de saldo los aplica la tabla en el navegador
        // (Lubricentro.js).
        private void CargarGrilla()
        {
            saldos = CuentaCorrienteProveedorDAL.ListarSaldos().ToDictionary(s => s.IdProveedor, s => s.Saldo);

            gvProveedores.DataSource = ProveedorDAL.Listar(incluirInactivos: true);
            gvProveedores.DataBind();
        }

        protected decimal SaldoDe(int idProveedor)
        {
            decimal saldo;
            return saldos.TryGetValue(idProveedor, out saldo) ? saldo : 0m;
        }

        // data-saldo para la opción "Saldo": positivo = le debemos al proveedor, negativo = a favor nuestro.
        protected void gvProveedores_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var saldo = SaldoDe(((Proveedor)e.Row.DataItem).IdProveedor);
            e.Row.Attributes["data-saldo"] = saldo > 0 ? "debe" : saldo < 0 ? "a-favor" : "cero";
        }

        protected void gvProveedores_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Ver") return;

            Seleccionar(LeerIdOculto(Convert.ToString(e.CommandArgument)));
        }

        private void Seleccionar(int idProveedor)
        {
            var proveedor = ProveedorDAL.ObtenerPorId(idProveedor);
            if (proveedor == null)
            {
                MostrarMensaje("El proveedor no existe.", false);
                CargarGrilla();
                return;
            }

            ViewState["IdProveedor"] = idProveedor;

            pnlAjuste.Visible = !EsSoloLectura;
            pnlHistorial.CssClass = EsSoloLectura ? "col-12" : "col-lg-8";
            litProveedorSeleccionado.Text = "Cuenta corriente: " + Server.HtmlEncode(proveedor.RazonSocial);
            litSaldoActual.Text = CuentaCorrienteProveedorDAL.ObtenerSaldoActual(idProveedor).ToString("N2");

            gvHistorial.DataSource = CuentaCorrienteProveedorDAL.ListarPorProveedor(idProveedor);
            gvHistorial.DataBind();

            Interfaz.AbrirModal(this, IdModal);
        }

        protected void btnRegistrarAjuste_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var idProveedor = LeerIdOculto(Convert.ToString(ViewState["IdProveedor"]));
            if (idProveedor <= 0)
            {
                MostrarMensaje("Seleccioná un proveedor antes de registrar un ajuste.", false);
                return;
            }

            if (!Page.IsValid)
            {
                Interfaz.AbrirModal(this, IdModal);
                return;
            }

            decimal monto;
            decimal.TryParse(txtMontoAjuste.Text, out monto);

            var resultado = CuentaCorrienteProveedorDAL.RegistrarAjuste(
                idProveedor, monto, txtMotivoAjuste.Text, UsuarioActual.IdUsuario);

            if (resultado.Exito)
            {
                txtMontoAjuste.Text = string.Empty;
                txtMotivoAjuste.Text = string.Empty;
                CargarGrilla();
                Seleccionar(idProveedor);
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
