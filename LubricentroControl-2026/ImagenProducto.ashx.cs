using System;
using System.Globalization;
using System.Web;
using System.Web.SessionState;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // Imagen de un producto para un <img> (Requerimientos §9.13): ImagenProducto.ashx?id=5&v=… da
    // la imagen entera, y con &miniatura=1, la de la lista. Va aparte de la página para que el
    // archivo no viaje dentro del HTML y el navegador lo pueda guardar. Pide sesión, como el resto
    // del sistema (IReadOnlySessionState: sin esa marca un handler no ve la sesión), pero no mira
    // permisos de pantalla: la imagen de un producto no es un dato reservado.
    public class ImagenProducto : IHttpHandler, IReadOnlySessionState
    {
        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext contexto)
        {
            var pedido = contexto.Request;
            var respuesta = contexto.Response;

            if (!SesionUsuario.HayUsuario)
            {
                respuesta.StatusCode = 403;
                return;
            }

            int idProducto;
            var imagen = int.TryParse(pedido.QueryString["id"], out idProducto)
                ? ProductoDAL.ObtenerImagen(idProducto, pedido.QueryString["miniatura"] == "1")
                : null;

            if (imagen == null)
            {
                respuesta.StatusCode = 404;
                return;
            }

            // Pedida con la versión vigente, el navegador la guarda y no la vuelve a pedir: una
            // imagen nueva cambia la versión y, con ella, la dirección.
            if (pedido.QueryString["v"] == Version(imagen.FechaActualizacion))
            {
                respuesta.Cache.SetCacheability(HttpCacheability.Private);
                respuesta.Cache.SetMaxAge(TimeSpan.FromDays(365));
            }
            else
            {
                respuesta.Cache.SetCacheability(HttpCacheability.NoCache);
            }

            respuesta.ContentType = imagen.TipoContenido;
            respuesta.AddHeader("X-Content-Type-Options", "nosniff");
            respuesta.BinaryWrite(imagen.Miniatura ?? imagen.Contenido);
        }

        // Dirección de la imagen de un producto (o de su miniatura), o null si no tiene.
        public static string Url(Producto producto, bool miniatura)
        {
            if (producto == null || !producto.TieneImagen) return null;

            return VirtualPathUtility.ToAbsolute("~/ImagenProducto.ashx") +
                   "?id=" + producto.IdProducto.ToString(CultureInfo.InvariantCulture) +
                   (miniatura ? "&miniatura=1" : "") +
                   "&v=" + Version(producto.FechaImagen.Value);
        }

        private static string Version(DateTime fecha)
        {
            return fecha.Ticks.ToString(CultureInfo.InvariantCulture);
        }
    }
}
