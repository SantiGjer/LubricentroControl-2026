using System;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026.Reportes
{
    // Asistente de consultas: el usuario pregunta en lenguaje natural, una IA local elige una consulta
    // del catálogo cerrado (ConsultaAsistente) y AsistenteDAL la responde con datos reales. Pantalla de
    // solo lectura: no escribe nada, así que EsSoloLectura no cambia su comportamiento.
    public partial class Asistente : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // El Literal guarda su texto en el ViewState: se oculta al principio para no repetir
            // el último aviso en cada postback.
            pnlMensaje.Visible = false;

            litModelo.Text = ServicioIA.Modelo;

            // Los botones de ejemplo salen del catálogo; en los postbacks los restaura el ViewState.
            if (IsPostBack) return;

            rptEjemplos.DataSource = ConsultaAsistente.Catalogo;
            rptEjemplos.DataBind();
        }

        protected void btnPreguntar_Click(object sender, EventArgs e)
        {
            Mostrar(AsistenteDAL.Responder(txtPregunta.Text));
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
    }
}
