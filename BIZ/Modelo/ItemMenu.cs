using System.Collections.Generic;

namespace BIZ.Modelo
{
    /// <summary>
    /// Opción del menú principal. Mapea la tabla <c>Menu</c> del diagrama E/R; se llama
    /// ItemMenu para no chocar con System.Web.UI.WebControls.Menu en los code-behind.
    /// </summary>
    public class ItemMenu
    {
        public int IdMenu { get; set; }
        public string Texto { get; set; }

        /// <summary>Null cuando la opción es un grupo desplegable y no un link.</summary>
        public int? IdUrl { get; set; }

        /// <summary>Ruta de la pantalla (ej: <c>~/Clientes</c>). Null si es un grupo.</summary>
        public string Path { get; set; }

        public int? IdMenuPadre { get; set; }
        public int Orden { get; set; }

        /// <summary>El rol ve la pantalla pero no puede modificar (matriz de permisos §5).</summary>
        public bool SoloLectura { get; set; }

        public List<ItemMenu> Hijos { get; set; }

        public ItemMenu()
        {
            Hijos = new List<ItemMenu>();
        }

        public bool EsGrupo
        {
            get { return !IdUrl.HasValue; }
        }
    }
}
