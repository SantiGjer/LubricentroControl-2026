using System;
using System.Text;
using System.Web;
using BIZ.Modelo;
using BIZ.Negocio;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    public partial class _Default : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            litNombre.Text = HttpUtility.HtmlEncode(UsuarioActual.NombreCompleto);
            litNivel.Text = HttpUtility.HtmlEncode(UsuarioActual.NombreNivel);

            MostrarAccesos();
        }

        private void MostrarAccesos()
        {
            var html = new StringBuilder("<ul>");

            foreach (var opcion in MenuNegocio.ObtenerArbol(UsuarioActual.IdNivel))
            {
                if (opcion.EsGrupo)
                {
                    foreach (var hijo in opcion.Hijos)
                        AgregarAcceso(html, hijo);
                }
                else
                {
                    AgregarAcceso(html, opcion);
                }
            }

            html.Append("</ul>");
            litAccesos.Text = html.ToString();
        }

        private void AgregarAcceso(StringBuilder html, ItemMenu opcion)
        {
            html.Append("<li><a href=\"").Append(HttpUtility.HtmlEncode(ResolveUrl(opcion.Path))).Append("\">")
                .Append(HttpUtility.HtmlEncode(opcion.Texto))
                .Append("</a>");

            if (opcion.SoloLectura)
                html.Append(" (solo consulta)");

            html.Append("</li>");
        }
    }
}
