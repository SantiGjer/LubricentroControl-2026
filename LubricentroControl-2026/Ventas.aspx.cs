using System;
using BIZ.Data;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // Comprobantes de venta (Fase 4). Solo lectura: no se carga a mano, se genera
    // automáticamente al cerrar una orden de trabajo (OrdenDeTrabajoDAL.Cerrar →
    // ComprobanteVentaDAL.GenerarDesdeOrden, Requerimientos §6.6). Acceso completo para los
    // 3 roles (Requerimientos §5) — no hay nada que escribir acá de todas formas.
    public partial class Ventas : PaginaSegura
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

            gvVentas.DataSource = string.IsNullOrWhiteSpace(texto)
                ? ComprobanteVentaDAL.Listar()
                : ComprobanteVentaDAL.Buscar(texto);
            gvVentas.DataBind();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        protected void gvVentas_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Seleccionar") return;

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

            pnlDetalle.Visible = true;
            litTituloDetalle.Text = venta.NumeroComprobante;
            litClienteInfo.Text = venta.NombreCliente;
            litVehiculoInfo.Text = venta.Patente;
            litFechaInfo.Text = venta.Fecha.ToString("dd/MM/yyyy HH:mm");
            litSubtotalInfo.Text = venta.Subtotal.ToString("N2");
            litImpuestosInfo.Text = venta.Impuestos.ToString("N2");
            litTotalInfo.Text = venta.Total.ToString("N2");
            litSaldoInfo.Text = venta.SaldoPendiente.ToString("N2");

            gvDetalleVenta.DataSource = DetalleComprobanteVentaDAL.ListarPorVenta(idVenta);
            gvDetalleVenta.DataBind();
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
