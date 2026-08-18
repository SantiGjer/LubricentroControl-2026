using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using BIZ.Modelo;
using BIZ.Negocio;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var usuario = SesionUsuario.Actual;

            // En Login y en las pantallas de recuperación no hay nada que navegar.
            phNavegacion.Visible = usuario != null;
            if (usuario == null) return;

            litUsuario.Text = HttpUtility.HtmlEncode(usuario.NombreCompleto) +
                              " <span class=\"badge bg-secondary\">" +
                              HttpUtility.HtmlEncode(usuario.NombreNivel) + "</span>";

            litMenu.Text = RenderizarMenu(MenuNegocio.ObtenerArbol(usuario.IdNivel));
        }

        protected void lnkCerrarSesion_Click(object sender, EventArgs e)
        {
            SesionUsuario.Cerrar();
            Response.Redirect("~/Login");
        }

        private string RenderizarMenu(List<ItemMenu> opciones)
        {
            var html = new StringBuilder();
            var rutaActual = RutaLogicaActual();

            foreach (var opcion in opciones)
            {
                if (opcion.EsGrupo)
                    RenderizarGrupo(html, opcion, rutaActual);
                else
                    RenderizarLink(html, opcion, rutaActual);
            }
            return html.ToString();
        }

        private void RenderizarGrupo(StringBuilder html, ItemMenu grupo, string rutaActual)
        {
            var activo = grupo.Hijos.Exists(h => EsRutaActual(h, rutaActual));

            html.Append("<li class=\"nav-item dropdown\">");
            html.Append("<a class=\"nav-link dropdown-toggle").Append(activo ? " active" : "")
                .Append("\" href=\"#\" role=\"button\" data-bs-toggle=\"dropdown\" aria-expanded=\"false\">")
                .Append(HttpUtility.HtmlEncode(grupo.Texto))
                .Append("</a>");
            html.Append("<ul class=\"dropdown-menu\">");

            foreach (var hijo in grupo.Hijos)
            {
                html.Append("<li><a class=\"dropdown-item")
                    .Append(EsRutaActual(hijo, rutaActual) ? " active" : "")
                    .Append("\" href=\"").Append(HttpUtility.HtmlEncode(ResolveUrl(hijo.Path))).Append("\">")
                    .Append(HttpUtility.HtmlEncode(hijo.Texto));

                if (hijo.SoloLectura)
                    html.Append(" <span class=\"badge bg-light text-dark\">consulta</span>");

                html.Append("</a></li>");
            }

            html.Append("</ul></li>");
        }

        private void RenderizarLink(StringBuilder html, ItemMenu opcion, string rutaActual)
        {
            html.Append("<li class=\"nav-item\"><a class=\"nav-link")
                .Append(EsRutaActual(opcion, rutaActual) ? " active" : "")
                .Append("\" href=\"").Append(HttpUtility.HtmlEncode(ResolveUrl(opcion.Path))).Append("\">")
                .Append(HttpUtility.HtmlEncode(opcion.Texto))
                .Append("</a></li>");
        }

        private static bool EsRutaActual(ItemMenu opcion, string rutaActual)
        {
            return !string.IsNullOrEmpty(opcion.Path) &&
                   string.Equals(opcion.Path, rutaActual, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Ruta de la pantalla actual sin .aspx, para marcar la opción activa.</summary>
        private string RutaLogicaActual()
        {
            var ruta = Request.AppRelativeCurrentExecutionFilePath ?? string.Empty;
            if (ruta.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                ruta = ruta.Substring(0, ruta.Length - ".aspx".Length);
            return ruta;
        }
    }
}
