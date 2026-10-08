using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // Página maestra: barra lateral con el menú del rol (armado desde la base), la marca y el
    // usuario logueado. En las pantallas de ingreso (sin usuario) no hay barra lateral.
    public partial class SiteMaster : MasterPage
    {
        // Isotipo de la marca: la misma gota de aceite de la pantalla de ingreso.
        protected const string Isotipo =
            "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\">" +
            "<path d=\"M12 2.2c-.35 0-.66.18-.85.47C9.5 5.3 5.5 10.6 5.5 14.6a6.5 6.5 0 0 0 13 0c0-4-4-9.3-5.65-11.93A1 1 0 0 0 12 2.2z\" fill=\"#c0272d\" />" +
            "<path d=\"M9.3 14.4a.8.8 0 0 1 .8.8 2.2 2.2 0 0 0 2.2 2.2.8.8 0 0 1 0 1.6 3.8 3.8 0 0 1-3.8-3.8.8.8 0 0 1 .8-.8z\" fill=\"#fff\" opacity=\".85\" />" +
            "</svg>";

        // Íconos de las opciones de primer nivel (columna Menu.icono), dibujados con trazos para
        // que tomen el color del texto. Uno que no esté acá usa el genérico.
        private static readonly Dictionary<string, string> Iconos = new Dictionary<string, string>
        {
            { "inicio", "<path d=\"M3.5 10.5 12 3.5l8.5 7\"/><path d=\"M5.5 9v11h5v-6h3v6h5V9\"/>" },
            { "clientes", "<circle cx=\"9\" cy=\"8\" r=\"3.5\"/><path d=\"M2.5 20c0-3.6 2.9-6.5 6.5-6.5s6.5 2.9 6.5 6.5\"/>" +
                          "<path d=\"M16 4.7a3.5 3.5 0 0 1 0 6.6\"/><path d=\"M18.2 13.9c1.9.9 3.3 2.9 3.3 5.1\"/>" },
            { "operacion", "<path d=\"M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.4-3.4a6 6 0 0 1-7.9 7.9l-6.9 6.9a2.1 2.1 0 0 1-3-3l6.9-6.9a6 6 0 0 1 7.9-7.9z\"/>" },
            { "compras", "<circle cx=\"9\" cy=\"20\" r=\"1.4\"/><circle cx=\"18\" cy=\"20\" r=\"1.4\"/>" +
                         "<path d=\"M2.5 3.5h2.6l2.6 11.6a1.4 1.4 0 0 0 1.4 1.1h8.6a1.4 1.4 0 0 0 1.4-1.1L20.8 7.5H6\"/>" },
            { "ventas", "<rect x=\"2.5\" y=\"6\" width=\"19\" height=\"12\" rx=\"2\"/><circle cx=\"12\" cy=\"12\" r=\"2.6\"/>" +
                        "<path d=\"M6 12h.01M18 12h.01\"/>" },
            { "reportes", "<path d=\"M3.5 3.5v17h17\"/><path d=\"M8 16.5v-4\"/><path d=\"M12.5 16.5v-8\"/><path d=\"M17 16.5v-6\"/>" },
            { "administracion", "<path d=\"M12 3 4.5 6v5.6c0 4.4 3.2 8.3 7.5 9.4 4.3-1.1 7.5-5 7.5-9.4V6z\"/><path d=\"m9 12 2.2 2.2L15.5 10\"/>" }
        };

        private const string IconoGenerico = "<circle cx=\"12\" cy=\"12\" r=\"3.5\"/>";

        // con-menu / sin-menu, para el <body>: con usuario logueado va la barra lateral.
        protected string ClaseCuerpo
        {
            get { return SesionUsuario.HayUsuario ? "con-menu" : "sin-menu"; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var usuario = SesionUsuario.Actual;

            // En Login y en las pantallas de recuperación no hay nada que navegar.
            phNavegacion.Visible = usuario != null;
            if (usuario == null) return;

            litUsuario.Text = HttpUtility.HtmlEncode(usuario.NombreCompleto);
            litRol.Text = HttpUtility.HtmlEncode(usuario.NombreNivel);
            litIniciales.Text = HttpUtility.HtmlEncode(Iniciales(usuario));

            litMenu.Text = RenderizarMenu(MenuDAL.ObtenerArbol(usuario.IdNivel));
        }

        protected void lnkCerrarSesion_Click(object sender, EventArgs e)
        {
            SesionUsuario.Cerrar();
            Response.Redirect("~/Login");
        }

        private static string Iniciales(Usuario usuario)
        {
            var iniciales = "";
            if (!string.IsNullOrEmpty(usuario.Nombre)) iniciales += usuario.Nombre.Substring(0, 1);
            if (!string.IsNullOrEmpty(usuario.Apellido)) iniciales += usuario.Apellido.Substring(0, 1);
            return iniciales.ToUpperInvariant();
        }

        private string RenderizarMenu(List<ItemMenu> opciones)
        {
            var html = new StringBuilder("<ul class=\"menu-lista\">");
            var rutaActual = RutaLogicaActual();

            foreach (var opcion in opciones)
            {
                if (opcion.EsGrupo)
                    RenderizarGrupo(html, opcion, rutaActual);
                else
                    RenderizarLink(html, opcion, rutaActual);
            }

            return html.Append("</ul>").ToString();
        }

        private static void RenderizarIcono(StringBuilder html, string icono)
        {
            string trazos;
            if (icono == null || !Iconos.TryGetValue(icono, out trazos)) trazos = IconoGenerico;

            html.Append("<svg class=\"menu-icono\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" ")
                .Append("stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">")
                .Append(trazos)
                .Append("</svg>");
        }

        // Grupo desplegable (collapse de Bootstrap). Arranca abierto si contiene la pantalla
        // actual; Lubricentro.restaurarMenu abre además los que el usuario dejó abiertos.
        private void RenderizarGrupo(StringBuilder html, ItemMenu grupo, string rutaActual)
        {
            var activo = grupo.Hijos.Exists(h => EsRutaActual(h, rutaActual));
            var idGrupo = "menuGrupo" + grupo.IdMenu;

            html.Append("<li class=\"menu-item menu-grupo").Append(activo ? " contiene-activo" : "").Append("\">");
            html.Append("<button type=\"button\" class=\"menu-enlace menu-grupo-boton").Append(activo ? "" : " collapsed")
                .Append("\" data-bs-toggle=\"collapse\" data-bs-target=\"#").Append(idGrupo)
                .Append("\" aria-expanded=\"").Append(activo ? "true" : "false")
                .Append("\" aria-controls=\"").Append(idGrupo).Append("\">");
            RenderizarIcono(html, grupo.Icono);
            html.Append("<span class=\"menu-texto\">").Append(HttpUtility.HtmlEncode(grupo.Texto)).Append("</span>")
                .Append("<span class=\"menu-flecha\" aria-hidden=\"true\"></span></button>");

            html.Append("<div class=\"collapse").Append(activo ? " show" : "").Append("\" id=\"").Append(idGrupo)
                .Append("\" data-grupo=\"").Append(grupo.IdMenu).Append("\"><ul class=\"menu-sublista\">");

            foreach (var hijo in grupo.Hijos)
            {
                var esActual = EsRutaActual(hijo, rutaActual);
                html.Append("<li><a class=\"menu-subenlace").Append(esActual ? " activo" : "")
                    .Append("\" href=\"").Append(HttpUtility.HtmlEncode(ResolveUrl(hijo.Path))).Append("\"")
                    .Append(esActual ? " aria-current=\"page\"" : "").Append(">")
                    .Append(HttpUtility.HtmlEncode(hijo.Texto))
                    .Append("</a></li>");
            }

            html.Append("</ul></div></li>");
        }

        private void RenderizarLink(StringBuilder html, ItemMenu opcion, string rutaActual)
        {
            var esActual = EsRutaActual(opcion, rutaActual);

            html.Append("<li class=\"menu-item\"><a class=\"menu-enlace").Append(esActual ? " activo" : "")
                .Append("\" href=\"").Append(HttpUtility.HtmlEncode(ResolveUrl(opcion.Path))).Append("\"")
                .Append(esActual ? " aria-current=\"page\"" : "").Append(">");
            RenderizarIcono(html, opcion.Icono);
            html.Append("<span class=\"menu-texto\">").Append(HttpUtility.HtmlEncode(opcion.Texto)).Append("</span></a></li>");
        }

        private static bool EsRutaActual(ItemMenu opcion, string rutaActual)
        {
            return !string.IsNullOrEmpty(opcion.Path) &&
                   string.Equals(opcion.Path, rutaActual, StringComparison.OrdinalIgnoreCase);
        }

        // Ruta de la pantalla actual sin .aspx, para marcar la opción activa.
        private string RutaLogicaActual()
        {
            var ruta = Request.AppRelativeCurrentExecutionFilePath ?? string.Empty;
            if (ruta.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                ruta = ruta.Substring(0, ruta.Length - ".aspx".Length);
            return ruta;
        }
    }
}
