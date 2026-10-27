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
    // Asistente de ventas y stock (Requerimientos §9.21): el usuario pregunta en lenguaje natural, una
    // IA local (Ollama) elige una consulta del catálogo cerrado (ConsultaVentasIA) y AsistenteVentasDAL
    // la responde con datos reales. Pantalla de solo lectura: no escribe nada, así que EsSoloLectura no
    // cambia su comportamiento.
    public partial class AsistenteVentas : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // El Literal guarda su texto en el ViewState: se oculta al principio para no repetir el
            // último aviso en cada postback.
            pnlMensaje.Visible = false;

            litModelo.Text = ServicioIAVentas.Modelo;

            // Los botones de ejemplo salen del catálogo; en los postbacks los restaura el ViewState.
            if (IsPostBack) return;

            rptEjemplos.DataSource = ConsultaVentasIA.Catalogo;
            rptEjemplos.DataBind();
        }

        protected void btnPreguntar_Click(object sender, EventArgs e)
        {
            Mostrar(AsistenteVentasDAL.Responder(txtPregunta.Text));
        }

        // Botón de ejemplo: la consulta del mes actual, sin pasar por la IA.
        protected void rptEjemplos_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Ejemplo") return;

            Mostrar(AsistenteVentasDAL.EjecutarDirecto(Convert.ToString(e.CommandArgument)));
        }

        // Saca la respuesta (o el error) de la pantalla y vacía la pregunta.
        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            gvResultado.Visible = false;
            pnlRespuesta.Visible = false;
            btnLimpiar.Visible = false;
            txtPregunta.Text = "";
        }

        private void Mostrar(RespuestaVentasIA respuesta)
        {
            gvResultado.Visible = false;
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
                gvResultado.DataSource = respuesta.Tabla;
                gvResultado.DataBind();
                AlinearNumeros();
                gvResultado.Visible = true;
            }
        }

        // Las columnas salen de cada respuesta: las que son números (cantidades, importes, días) van a la
        // derecha, como en el resto de las tablas.
        private void AlinearNumeros()
        {
            for (var c = 0; c < gvResultado.HeaderRow.Cells.Count; c++)
            {
                var columna = c;
                if (!gvResultado.Rows.Cast<GridViewRow>().All(f => EsNumero(f.Cells[columna].Text))) continue;

                gvResultado.HeaderRow.Cells[c].HorizontalAlign = HorizontalAlign.Right;
                foreach (GridViewRow fila in gvResultado.Rows)
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
