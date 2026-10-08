using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026.Reportes
{
    // Reporte de ventas por período (Requerimientos §6.9): total vendido en el rango de
    // fechas que elige el usuario. Mismo formato resumen + grilla paginada que StockBajo.
    public partial class VentasPorPeriodo : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            // Default: mes actual completo, para no dejar la pantalla vacía en el primer
            // ingreso. El usuario cambia el rango y filtra de nuevo si quiere otro período.
            var hoy = DateTime.Today;
            txtDesde.Text = new DateTime(hoy.Year, hoy.Month, 1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            txtHasta.Text = hoy.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            CargarReporte();
        }

        protected void btnFiltrar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            CargarReporte();
        }

        private void CargarReporte()
        {
            // Los inputs HTML5 type="date" siempre postean en yyyy-MM-dd, sin importar la
            // configuración regional del navegador ni la cultura del hilo del servidor — se
            // parsean exactos e invariantes para no depender de ninguna (mismo criterio que
            // Turnos.aspx.cs con txtFecha).
            DateTime desde, hasta;
            var desdeValida = DateTime.TryParseExact(txtDesde.Text, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out desde);
            var hastaValida = DateTime.TryParseExact(txtHasta.Text, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out hasta);

            if (!desdeValida || !hastaValida)
            {
                MostrarMensaje("Las fechas no tienen un formato válido.", false);
                return;
            }

            var ventas = ComprobanteVentaDAL.ListarPorPeriodo(desde, hasta);

            gvVentas.DataSource = ventas;
            gvVentas.DataBind();

            litResumen.Text = ArmarResumen(ventas);
        }

        private static string ArmarResumen(List<ComprobanteVenta> ventas)
        {
            if (ventas.Count == 0)
                return "Ninguna venta en el período elegido.";

            var total = ventas.Sum(v => v.Total);
            var texto = ventas.Count == 1
                ? "<b>1</b> venta por un total de <b>$" + total.ToString("N2") + "</b>"
                : "<b>" + ventas.Count + "</b> ventas por un total de <b>$" + total.ToString("N2") + "</b>";

            var pendiente = ventas.Sum(v => v.SaldoPendiente);
            if (pendiente > 0)
                texto += ", de las cuales <b>$" + pendiente.ToString("N2") + "</b> siguen pendientes de cobro";

            // Los precios son finales: el total ya contiene el IVA (Requerimientos §9.10).
            texto += ". IVA contenido: <b>$" + ventas.Sum(v => v.Impuestos).ToString("N2") + "</b>";

            return texto + ".";
        }

        // Igual que StockBajo: se resalta sólo el caso que necesita atención, no toda la fila.
        // Acá, la venta que todavía tiene saldo pendiente de cobro.
        protected void gvVentas_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var venta = (ComprobanteVenta)e.Row.DataItem;
            if (venta.SaldoPendiente > 0)
                e.Row.Style.Add("background-color", "#f8d7da");
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
