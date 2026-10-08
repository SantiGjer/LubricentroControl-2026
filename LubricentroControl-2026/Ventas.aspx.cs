using System;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // Comprobantes de venta (Fase 4) y sus facturas. La venta no se carga a mano: se genera
    // automáticamente al cerrar una orden de trabajo (OrdenDeTrabajoDAL.Cerrar →
    // ComprobanteVentaDAL.GenerarDesdeOrden, Requerimientos §6.6). Desde acá se consulta y se
    // factura (FacturaDAL.Emitir, Requerimientos §9.10): facturar es la única escritura de la
    // pantalla, así que quien la tiene en solo consulta ve las facturas pero no las genera.
    public partial class Ventas : PaginaSegura
    {
        private const string IdModal = "modalVenta";
        private const string IdModalFactura = "modalFactura";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Los avisos se muestran una sola vez (el Literal guarda su texto en el ViewState).
            pnlMensaje.Visible = false;

            if (IsPostBack) return;

            CargarGrilla();

            // Llegada desde Pagos después de cobrar una venta: se abre su detalle para facturarla.
            if (Request.QueryString["ver"] != null)
                Seleccionar(LeerIdOculto(Request.QueryString["ver"]));
        }

        // El filtro por texto y las opciones (saldo, factura) los hace la tabla en el navegador
        // (Lubricentro.js).
        private void CargarGrilla()
        {
            gvVentas.DataSource = ComprobanteVentaDAL.Listar();
            gvVentas.DataBind();
        }

        protected void gvVentas_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var venta = (ComprobanteVenta)e.Row.DataItem;
            e.Row.Attributes["data-saldo"] = venta.SaldoPendiente > 0 ? "pendiente" : "saldada";
            e.Row.Attributes["data-factura"] = venta.Facturada ? "si" : "no";
        }

        protected void gvVentas_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            var idVenta = LeerIdOculto(Convert.ToString(e.CommandArgument));

            switch (e.CommandName)
            {
                case "Ver":
                    Seleccionar(idVenta);
                    break;
                case "VerFactura":
                    VerFactura(idVenta);
                    break;
                case "Facturar":
                    Facturar(idVenta);
                    break;
            }
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

            hdnIdVenta.Value = venta.IdVenta.ToString();
            litTituloDetalle.Text = "Venta " + Server.HtmlEncode(venta.NumeroComprobante);
            litClienteInfo.Text = Server.HtmlEncode(venta.NombreCliente + " — " + venta.DocumentoCliente);
            litVehiculoInfo.Text = Server.HtmlEncode(venta.Patente);
            litFechaInfo.Text = venta.Fecha.ToString("dd/MM/yyyy HH:mm");
            litOrdenInfo.Text = "#" + venta.IdOrden;
            litSubtotalInfo.Text = venta.Subtotal.ToString("N2");
            litImpuestosInfo.Text = venta.Impuestos.ToString("N2");
            litTotalInfo.Text = venta.Total.ToString("N2");
            litSaldoInfo.Text = venta.SaldoPendiente.ToString("N2");
            litFacturaInfo.Text = venta.Facturada ? Server.HtmlEncode(venta.NumeroFactura) : "Sin facturar";

            btnVerFactura.Visible = venta.Facturada;
            btnFacturar.Visible = !venta.Facturada && !EsSoloLectura;

            gvDetalleVenta.DataSource = DetalleComprobanteVentaDAL.ListarPorVenta(idVenta);
            gvDetalleVenta.DataBind();

            Interfaz.AbrirModal(this, IdModal);
        }

        protected void btnVerFactura_Click(object sender, EventArgs e)
        {
            VerFactura(LeerIdOculto(hdnIdVenta.Value));
        }

        protected void btnFacturar_Click(object sender, EventArgs e)
        {
            Facturar(LeerIdOculto(hdnIdVenta.Value));
        }

        // Emite la factura (o trae la que ya tenía) y la muestra lista para imprimir.
        private void Facturar(int idVenta)
        {
            if (EsSoloLectura) return;

            Factura factura;
            var resultado = FacturaDAL.Emitir(idVenta, UsuarioActual.IdUsuario, out factura);
            if (!resultado.Exito)
            {
                MostrarMensaje(Server.HtmlEncode(resultado.Mensaje), false);
                return;
            }

            CargarGrilla();
            MostrarMensaje(Server.HtmlEncode(resultado.Mensaje), true);
            MostrarFactura(factura);
        }

        private void VerFactura(int idVenta)
        {
            var factura = FacturaDAL.ObtenerPorVenta(idVenta);
            if (factura == null)
            {
                MostrarMensaje("Esa venta todavía no tiene factura.", false);
                return;
            }

            MostrarFactura(factura);
        }

        // Arma la hoja de la factura. La A discrimina el IVA (precios y subtotales netos, IVA por
        // alícuota); la B muestra precios finales e informa el IVA contenido (Ley 27.743); la C,
        // de un emisor monotributista o exento, solo precios finales.
        private void MostrarFactura(Factura factura)
        {
            var venta = ComprobanteVentaDAL.ObtenerPorId(factura.IdVenta);
            var lineas = DetalleComprobanteVentaDAL.ListarPorVenta(factura.IdVenta);
            var totales = TotalesFactura.Calcular(lineas);

            litTituloFactura.Text = "Factura " + Server.HtmlEncode(factura.Tipo + " " + factura.NumeroCompleto);
            litEmisorNombre.Text = Server.HtmlEncode(factura.EmisorRazonSocial);
            litEmisorDomicilio.Text = Server.HtmlEncode(factura.EmisorDomicilio);
            litEmisorCondicion.Text = Server.HtmlEncode(factura.EmisorCondicionIva);
            litFacturaLetra.Text = Server.HtmlEncode(factura.Tipo);
            litFacturaCodigo.Text = factura.CodigoComprobante;
            litFacturaNumero.Text = factura.NumeroCompleto;
            litFacturaFecha.Text = factura.Fecha.ToString("dd/MM/yyyy");
            litEmisorCuit.Text = Server.HtmlEncode(factura.EmisorCuitFormateado);
            litEmisorIibb.Text = Server.HtmlEncode(factura.EmisorIngresosBrutos ?? "—");
            litEmisorInicio.Text = Server.HtmlEncode(factura.EmisorInicioActividades ?? "—");

            litReceptorNombre.Text = Server.HtmlEncode(factura.ReceptorNombre);
            litReceptorDocumento.Text = Server.HtmlEncode(factura.ReceptorDocumento);
            litReceptorCondicion.Text = Server.HtmlEncode(factura.ReceptorCondicionIva);
            litReceptorDomicilio.Text = Server.HtmlEncode(factura.ReceptorDomicilio ?? "—");
            litCondicionVenta.Text = Server.HtmlEncode(factura.CondicionVenta);
            litReferencia.Text = venta == null
                ? ""
                : Server.HtmlEncode("Venta " + venta.NumeroComprobante + " · Orden de trabajo #" + venta.IdOrden +
                                    " · Vehículo " + venta.Patente);

            litFacturaLineas.Text = ArmarLineas(factura, lineas);
            litFacturaTotales.Text = ArmarTotales(factura, totales);

            pnlTransparencia.Visible = factura.InformaIvaContenido;
            litIvaContenido.Text = totales.IvaTotal.ToString("N2");

            Interfaz.AbrirModal(this, IdModalFactura);
        }

        private static string ArmarLineas(Factura factura, System.Collections.Generic.List<DetalleComprobanteVenta> lineas)
        {
            var discrimina = factura.DiscriminaIva;
            var html = new StringBuilder("<table class=\"factura-lineas\"><thead><tr>");
            html.Append("<th class=\"numero\">Cantidad</th><th>Descripción</th>")
                .Append("<th class=\"numero\">").Append(discrimina ? "Precio unitario (neto)" : "Precio unitario").Append("</th>");
            if (discrimina) html.Append("<th class=\"numero\">IVA</th>");
            html.Append("<th class=\"numero\">").Append(discrimina ? "Subtotal (neto)" : "Subtotal").Append("</th>")
                .Append("</tr></thead><tbody>");

            foreach (var linea in lineas)
            {
                html.Append("<tr><td class=\"numero\">").Append(linea.Cantidad.ToString("N2")).Append("</td>")
                    .Append("<td>").Append(HttpUtility.HtmlEncode(linea.Descripcion)).Append("</td>")
                    .Append("<td class=\"numero\">")
                    .Append((discrimina ? linea.PrecioUnitarioNeto : linea.PrecioUnitario).ToString("N2")).Append("</td>");
                if (discrimina)
                    html.Append("<td class=\"numero\">").Append(HttpUtility.HtmlEncode(linea.IvaDescripcion)).Append("</td>");
                html.Append("<td class=\"numero\">").Append((discrimina ? linea.Neto : linea.Subtotal).ToString("N2"))
                    .Append("</td></tr>");
            }

            return html.Append("</tbody></table>").ToString();
        }

        private static string ArmarTotales(Factura factura, TotalesFactura totales)
        {
            var html = new StringBuilder("<dl>");

            if (factura.DiscriminaIva)
            {
                Fila(html, "Neto gravado", totales.NetoGravado, false);
                foreach (var iva in totales.IvaPorAlicuota)
                    Fila(html, "IVA " + iva.Key.ToString("0.##") + " %", iva.Value, false);
                if (totales.Exento > 0) Fila(html, "Exento", totales.Exento, false);
                if (totales.NoGravado > 0) Fila(html, "No gravado", totales.NoGravado, false);
            }
            else
            {
                Fila(html, "Subtotal", totales.Total, false);
            }

            Fila(html, "Total", totales.Total, true);
            return html.Append("</dl>").ToString();
        }

        private static void Fila(StringBuilder html, string concepto, decimal importe, bool esTotal)
        {
            var clase = esTotal ? " class=\"factura-total\"" : "";
            html.Append("<dt").Append(clase).Append(">").Append(HttpUtility.HtmlEncode(concepto)).Append("</dt>")
                .Append("<dd").Append(clase).Append(">$ ").Append(importe.ToString("N2")).Append("</dd>");
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
