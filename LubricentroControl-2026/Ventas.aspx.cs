using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // Comprobantes de venta (Fase 4), sus facturas y sus notas de crédito. La venta no se carga a
    // mano: se genera automáticamente al cerrar una orden de trabajo (OrdenDeTrabajoDAL.Cerrar →
    // ComprobanteVentaDAL.GenerarDesdeOrden, Requerimientos §6.6). Desde acá se consulta, se
    // factura (FacturaDAL.Emitir, §9.10) y se anula con una nota de crédito (NotaCreditoDAL.Emitir,
    // §9.17): son las dos escrituras de la pantalla, así que quien la tiene en solo consulta ve las
    // facturas y las notas de crédito pero no las genera.
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

        // El filtro por texto y las opciones (saldo, factura, estado y quién facturó) los hace la
        // tabla en el navegador (Lubricentro.js). El texto incluye las columnas ocultas, así que
        // también encuentra las ventas por quién las anuló.
        private void CargarGrilla()
        {
            var ventas = ComprobanteVentaDAL.Listar();
            gvVentas.DataSource = ventas;
            gvVentas.DataBind();

            // Trazabilidad: un botón del filtro "Facturó" por cada usuario que emitió alguna factura.
            rptCajeros.DataSource = ventas.Where(v => v.NombreUsuarioFactura != null)
                                          .Select(v => v.NombreUsuarioFactura)
                                          .Distinct()
                                          .OrderBy(nombre => nombre)
                                          .ToList();
            rptCajeros.DataBind();
        }

        protected void gvVentas_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var venta = (ComprobanteVenta)e.Row.DataItem;
            e.Row.Attributes["data-saldo"] = venta.SaldoPendiente > 0 ? "pendiente" : "saldada";
            e.Row.Attributes["data-factura"] = venta.Facturada ? "si" : "no";
            e.Row.Attributes["data-estado"] = venta.Anulada ? "anulada" : "vigente";

            // Una venta anulada sigue en la lista (es historia), pero se distingue: gris y con el
            // número tachado.
            if (venta.Anulada)
            {
                e.Row.Style.Add("color", "#6c757d");
                e.Row.Cells[0].Style.Add("text-decoration", "line-through");
            }
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
                case "VerNotaCredito":
                    VerNotaCredito(idVenta);
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
            litFacturaInfo.Text = venta.Facturada
                ? Server.HtmlEncode(venta.NumeroFactura + " · emitida por " + venta.NombreUsuarioFactura)
                : "Sin facturar";
            litNotaCreditoInfo.Text = venta.Anulada
                ? Server.HtmlEncode(venta.NumeroNotaCredito + " · emitida por " + venta.NombreUsuarioNotaCredito)
                : "—";

            // Una venta anulada no se factura ni se vuelve a anular.
            btnVerFactura.Visible = venta.Facturada;
            btnFacturar.Visible = !venta.Facturada && !venta.Anulada && !EsSoloLectura;
            btnVerNotaCredito.Visible = venta.Anulada;
            pnlEmitirNotaCredito.Visible = !venta.Anulada && !EsSoloLectura;
            txtMotivoNotaCredito.Text = string.Empty;

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

        protected void btnVerNotaCredito_Click(object sender, EventArgs e)
        {
            VerNotaCredito(LeerIdOculto(hdnIdVenta.Value));
        }

        // Anula la venta con una nota de crédito (o trae la que ya tenía) y la muestra lista para
        // imprimir. El motivo es obligatorio (validador del grupo NotaCredito y NotaCreditoDAL).
        protected void btnEmitirNotaCredito_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var idVenta = LeerIdOculto(hdnIdVenta.Value);
            if (!Page.IsValid)
            {
                Interfaz.AbrirModal(this, IdModal);
                return;
            }

            NotaCredito notaCredito;
            var resultado = NotaCreditoDAL.Emitir(idVenta, txtMotivoNotaCredito.Text, UsuarioActual.IdUsuario, out notaCredito);
            if (!resultado.Exito)
            {
                MostrarMensaje(Server.HtmlEncode(resultado.Mensaje), false);
                return;
            }

            CargarGrilla();
            MostrarMensaje(Server.HtmlEncode(resultado.Mensaje), true);
            MostrarNotaCredito(notaCredito);
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

        private void VerNotaCredito(int idVenta)
        {
            var notaCredito = NotaCreditoDAL.ObtenerPorVenta(idVenta);
            if (notaCredito == null)
            {
                MostrarMensaje("Esa venta no tiene nota de crédito.", false);
                return;
            }

            MostrarNotaCredito(notaCredito);
        }

        private void MostrarFactura(Factura factura)
        {
            var venta = ComprobanteVentaDAL.ObtenerPorId(factura.IdVenta);
            MostrarComprobante(factura, "Factura", "FACTURA", venta == null ? "" : ReferenciaVenta(venta), null);
        }

        // La nota de crédito se imprime con la misma hoja que la factura, con su título, la factura
        // que anula (si la venta se había facturado) y el motivo.
        private void MostrarNotaCredito(NotaCredito notaCredito)
        {
            var venta = ComprobanteVentaDAL.ObtenerPorId(notaCredito.IdVenta);
            var referencia = (notaCredito.NumeroFactura != null ? "Anula la factura " + notaCredito.NumeroFactura + " · " : "Anula la ") +
                             (venta == null ? "venta" : ReferenciaVenta(venta));
            MostrarComprobante(notaCredito, "Nota de crédito", "NOTA DE CRÉDITO", referencia, notaCredito.Motivo);
        }

        private static string ReferenciaVenta(ComprobanteVenta venta)
        {
            return "Venta " + venta.NumeroComprobante + " · Orden de trabajo #" + venta.IdOrden + " · Vehículo " + venta.Patente;
        }

        // Arma la hoja de la factura o de la nota de crédito (las dos son IComprobanteImpreso). La A
        // discrimina el IVA (precios y subtotales netos, IVA por alícuota); la B muestra precios
        // finales e informa el IVA contenido (Ley 27.743); la C, de un emisor monotributista o
        // exento, solo precios finales. Las líneas son las de la venta.
        private void MostrarComprobante(IComprobanteImpreso comprobante, string nombre, string tituloHoja,
                                        string referencia, string motivo)
        {
            var lineas = DetalleComprobanteVentaDAL.ListarPorVenta(comprobante.IdVenta);
            var totales = TotalesFactura.Calcular(lineas);

            litTituloFactura.Text = Server.HtmlEncode(nombre + " " + comprobante.Tipo + " " + comprobante.NumeroCompleto);
            litDocumentoTitulo.Text = Server.HtmlEncode(tituloHoja);
            litEmisorNombre.Text = Server.HtmlEncode(comprobante.EmisorRazonSocial);
            litEmisorDomicilio.Text = Server.HtmlEncode(comprobante.EmisorDomicilio);
            litEmisorCondicion.Text = Server.HtmlEncode(comprobante.EmisorCondicionIva);
            litFacturaLetra.Text = Server.HtmlEncode(comprobante.Tipo);
            litFacturaCodigo.Text = comprobante.CodigoComprobante;
            litFacturaNumero.Text = comprobante.NumeroCompleto;
            litFacturaFecha.Text = comprobante.Fecha.ToString("dd/MM/yyyy");
            litEmisorCuit.Text = Server.HtmlEncode(comprobante.EmisorCuitFormateado);
            litEmisorIibb.Text = Server.HtmlEncode(comprobante.EmisorIngresosBrutos ?? "—");
            litEmisorInicio.Text = Server.HtmlEncode(comprobante.EmisorInicioActividades ?? "—");

            litReceptorNombre.Text = Server.HtmlEncode(comprobante.ReceptorNombre);
            litReceptorDocumento.Text = Server.HtmlEncode(comprobante.ReceptorDocumento);
            litReceptorCondicion.Text = Server.HtmlEncode(comprobante.ReceptorCondicionIva);
            litReceptorDomicilio.Text = Server.HtmlEncode(comprobante.ReceptorDomicilio ?? "—");
            litCondicionVenta.Text = Server.HtmlEncode(comprobante.CondicionVenta);
            litReferencia.Text = Server.HtmlEncode(referencia);
            litOperador.Text = Server.HtmlEncode(comprobante.NombreUsuario);
            phMotivoNotaCredito.Visible = motivo != null;
            litMotivoNotaCredito.Text = motivo == null ? "" : Server.HtmlEncode(motivo);

            litFacturaLineas.Text = ArmarLineas(comprobante, lineas);
            litFacturaTotales.Text = ArmarTotales(comprobante, totales);

            pnlTransparencia.Visible = comprobante.InformaIvaContenido;
            litIvaContenido.Text = totales.IvaTotal.ToString("N2");

            Interfaz.AbrirModal(this, IdModalFactura);
        }

        private static string ArmarLineas(IComprobanteImpreso comprobante, System.Collections.Generic.List<DetalleComprobanteVenta> lineas)
        {
            var discrimina = comprobante.DiscriminaIva;
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

        private static string ArmarTotales(IComprobanteImpreso comprobante, TotalesFactura totales)
        {
            var html = new StringBuilder("<dl>");

            if (comprobante.DiscriminaIva)
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
