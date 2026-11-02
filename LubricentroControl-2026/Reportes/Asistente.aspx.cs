using System;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026.Reportes
{
    // Asistente de consultas: el usuario pregunta en lenguaje natural, una IA local elige una consulta
    // del catálogo cerrado (ConsultaAsistente) y AsistenteDAL la responde con datos reales. Son dos
    // cajas de preguntas independientes, una por grupo del catálogo: la de arriba (Negocio) y la de
    // abajo, de ventas y stock (VentasStock, Requerimientos §9.21); cada una muestra y limpia solo su
    // resultado. Pantalla de solo lectura: no escribe nada, así que EsSoloLectura no cambia su
    // comportamiento.
    public partial class Asistente : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // El Literal guarda su texto en el ViewState: se oculta al principio para no repetir
            // el último aviso en cada postback.
            pnlMensaje.Visible = false;
            pnlMensajeVentas.Visible = false;

            litModelo.Text = ServicioIA.Modelo;

            // Los botones de ejemplo salen del catálogo; en los postbacks los restaura el ViewState.
            if (IsPostBack) return;

            rptEjemplos.DataSource = ConsultaAsistente.CatalogoDe(GrupoAsistente.Negocio);
            rptEjemplos.DataBind();
            rptEjemplosVentas.DataSource = ConsultaAsistente.CatalogoDe(GrupoAsistente.VentasStock);
            rptEjemplosVentas.DataBind();
        }

        protected void btnPreguntar_Click(object sender, EventArgs e)
        {
            Mostrar(AsistenteDAL.Responder(txtPregunta.Text, GrupoAsistente.Negocio));
        }

        // Botón de ejemplo: ejecuta la consulta del mes actual sin pasar por la IA.
        protected void rptEjemplos_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Ejemplo") return;

            Mostrar(AsistenteDAL.EjecutarDirecto(Convert.ToString(e.CommandArgument)));
        }

        // Saca la respuesta (o el error) de la pantalla y vacía la pregunta.
        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            gvTop.Visible = false;
            pnlRespuesta.Visible = false;
            btnLimpiar.Visible = false;
            txtPregunta.Text = "";
        }

        private void Mostrar(RespuestaAsistente respuesta)
        {
            gvTop.Visible = false;
            pnlRespuesta.Visible = false;
            btnLimpiar.Visible = true;

            if (!respuesta.Exito)
            {
                pnlMensaje.CssClass = "alert alert-danger";
                litMensaje.Text = Server.HtmlEncode(respuesta.Texto);
                pnlMensaje.Visible = true;
                return;
            }

            litRespuesta.Text = respuesta.Texto;
            pnlRespuesta.Visible = true;

            if (respuesta.Tabla != null && respuesta.Tabla.Rows.Count > 0)
            {
                gvTop.DataSource = respuesta.Tabla;
                gvTop.DataBind();
                gvTop.Visible = true;
            }
        }

        /* --- Asistente de ventas y stock (la caja de abajo) ------------------------ */

        protected void btnPreguntarVentas_Click(object sender, EventArgs e)
        {
            MostrarVentas(AsistenteDAL.Responder(txtPreguntaVentas.Text, GrupoAsistente.VentasStock));
        }

        // Botón de ejemplo: la consulta del mes actual, sin pasar por la IA.
        protected void rptEjemplosVentas_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Ejemplo") return;

            MostrarVentas(AsistenteDAL.EjecutarDirecto(Convert.ToString(e.CommandArgument)));
        }

        // Saca la respuesta (o el error) de esta caja y vacía su pregunta.
        protected void btnLimpiarVentas_Click(object sender, EventArgs e)
        {
            gvResultadoVentas.Visible = false;
            pnlRespuestaVentas.Visible = false;
            btnLimpiarVentas.Visible = false;
            txtPreguntaVentas.Text = "";
        }

        private void MostrarVentas(RespuestaAsistente respuesta)
        {
            gvResultadoVentas.Visible = false;
            pnlRespuestaVentas.Visible = false;
            btnLimpiarVentas.Visible = true;

            if (!respuesta.Exito)
            {
                pnlMensajeVentas.CssClass = "alert alert-danger";
                litMensajeVentas.Text = Server.HtmlEncode(respuesta.Texto);
                pnlMensajeVentas.Visible = true;
                return;
            }

            litRespuestaVentas.Text = respuesta.Texto;
            pnlRespuestaVentas.Visible = true;

            if (respuesta.Tabla != null && respuesta.Tabla.Rows.Count > 0)
            {
                gvResultadoVentas.DataSource = respuesta.Tabla;
                gvResultadoVentas.DataBind();
                AlinearNumeros();
                gvResultadoVentas.Visible = true;
            }
        }

        // Las columnas salen de cada respuesta: las que son números (cantidades, importes, días) van a la
        // derecha, como en el resto de las tablas.
        private void AlinearNumeros()
        {
            for (var c = 0; c < gvResultadoVentas.HeaderRow.Cells.Count; c++)
            {
                var columna = c;
                if (!gvResultadoVentas.Rows.Cast<GridViewRow>().All(f => EsNumero(f.Cells[columna].Text))) continue;

                gvResultadoVentas.HeaderRow.Cells[c].HorizontalAlign = HorizontalAlign.Right;
                foreach (GridViewRow fila in gvResultadoVentas.Rows)
                    fila.Cells[c].HorizontalAlign = HorizontalAlign.Right;
            }
        }

        private static bool EsNumero(string texto)
        {
            texto = HttpUtility.HtmlDecode(texto ?? "").Replace("$", "").Trim();
            decimal numero;
            return texto == "—" || decimal.TryParse(texto, NumberStyles.Number, CultureInfo.CurrentCulture, out numero);
        }
    }
}
