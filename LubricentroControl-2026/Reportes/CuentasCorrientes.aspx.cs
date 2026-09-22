using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026.Reportes
{
    // Reporte de cuentas corrientes (Requerimientos §6.9): deudas de clientes y a proveedores,
    // con saldo actual. Decidido con el usuario: entran clientes y proveedores con saldo
    // distinto de cero, cualquier signo (deuda o a favor) — no es un listado completo ni sólo
    // deudores. Dos secciones independientes en la misma pantalla, cada una con su propio
    // resumen + grilla paginada, mismo formato que StockBajo y VentasPorPeriodo.
    public partial class CuentasCorrientes : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            CargarClientes();
            CargarProveedores();
        }

        private void CargarClientes()
        {
            var saldos = CuentaCorrienteClienteDAL.ListarSaldos();

            gvClientes.DataSource = saldos;
            gvClientes.DataBind();

            litResumenClientes.Text = ArmarResumenClientes(saldos);
        }

        private void CargarProveedores()
        {
            var saldos = CuentaCorrienteProveedorDAL.ListarSaldos();

            gvProveedores.DataSource = saldos;
            gvProveedores.DataBind();

            litResumenProveedores.Text = ArmarResumenProveedores(saldos);
        }

        private static string ArmarResumenClientes(List<CuentaCorrienteCliente> saldos)
        {
            if (saldos.Count == 0)
                return "Ningún cliente tiene saldo pendiente.";

            var deudores = saldos.Where(s => s.Saldo > 0).ToList();
            var aFavor = saldos.Where(s => s.Saldo < 0).ToList();

            var texto = "";
            if (deudores.Count > 0)
                texto += "<b>" + deudores.Count + "</b> " + (deudores.Count == 1 ? "cliente debe" : "clientes deben")
                    + " un total de <b>$" + deudores.Sum(s => s.Saldo).ToString("N2") + "</b>";

            if (aFavor.Count > 0)
            {
                if (texto.Length > 0) texto += ", y ";
                texto += "<b>" + aFavor.Count + "</b> " + (aFavor.Count == 1 ? "tiene" : "tienen")
                    + " saldo a favor por <b>$" + Math.Abs(aFavor.Sum(s => s.Saldo)).ToString("N2") + "</b>";
            }

            return texto + ".";
        }

        private static string ArmarResumenProveedores(List<CuentaCorrienteProveedor> saldos)
        {
            if (saldos.Count == 0)
                return "No hay saldo pendiente con ningún proveedor.";

            var deudas = saldos.Where(s => s.Saldo > 0).ToList();
            var aFavor = saldos.Where(s => s.Saldo < 0).ToList();

            var texto = "";
            if (deudas.Count > 0)
                texto += "Le debemos a <b>" + deudas.Count + "</b> " + (deudas.Count == 1 ? "proveedor" : "proveedores")
                    + " un total de <b>$" + deudas.Sum(s => s.Saldo).ToString("N2") + "</b>";

            if (aFavor.Count > 0)
            {
                if (texto.Length > 0) texto += ", y ";
                texto += "tenemos saldo a favor con <b>" + aFavor.Count + "</b> por <b>$"
                    + Math.Abs(aFavor.Sum(s => s.Saldo)).ToString("N2") + "</b>";
            }

            return texto + ".";
        }

        protected void gvClientes_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvClientes.PageIndex = e.NewPageIndex;
            CargarClientes();
        }

        protected void gvProveedores_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvProveedores.PageIndex = e.NewPageIndex;
            CargarProveedores();
        }
    }
}
