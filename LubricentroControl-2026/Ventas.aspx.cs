using System;
using BIZ.Data;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // Comprobantes de venta (Fase 4). Solo lectura: no se carga a mano, se genera
    // automáticamente al cerrar una orden de trabajo (OrdenDeTrabajoDAL.Cerrar →
    // ComprobanteVentaDAL.GenerarDesdeOrden, Requerimientos §6.6). Acceso completo para todos los
    // roles — no hay nada que escribir acá de todas formas. El detalle se abre en un modal.
    public partial class Ventas : PaginaSegura
    {
        private const string IdModal = "modalVenta";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Los avisos se muestran una sola vez (el Literal guarda su texto en el ViewState).
            pnlMensaje.Visible = false;

            if (IsPostBack) return;

            CargarGrilla();
        }

        // El filtro por texto lo hace la tabla en el navegador (Lubricentro.js).
        private void CargarGrilla()
        {
            gvVentas.DataSource = ComprobanteVentaDAL.Listar();
            gvVentas.DataBind();
        }

        protected void gvVentas_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Ver") return;

            Seleccionar(LeerIdOculto(Convert.ToString(e.CommandArgument)));
        }

        private void Seleccionar(int idVenta)
        {
            var venta = ComprobanteVentaDAL.ObtenerPorId(idVenta);
            if (venta == null)
            {
                MostrarMensaje("La venta no existe.", false);
                CargarGrilla();
                return;
            }

            litTituloDetalle.Text = "Venta " + Server.HtmlEncode(venta.NumeroComprobante);
            litClienteInfo.Text = Server.HtmlEncode(venta.NombreCliente);
            litVehiculoInfo.Text = Server.HtmlEncode(venta.Patente);
            litFechaInfo.Text = venta.Fecha.ToString("dd/MM/yyyy HH:mm");
            litSubtotalInfo.Text = venta.Subtotal.ToString("N2");
            litImpuestosInfo.Text = venta.Impuestos.ToString("N2");
            litTotalInfo.Text = venta.Total.ToString("N2");
            litSaldoInfo.Text = venta.SaldoPendiente.ToString("N2");

            gvDetalleVenta.DataSource = DetalleComprobanteVentaDAL.ListarPorVenta(idVenta);
            gvDetalleVenta.DataBind();

            Interfaz.AbrirModal(this, IdModal);
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
