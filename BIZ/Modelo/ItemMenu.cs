using System.Collections.Generic;

namespace BIZ.Modelo
{
    // Opción del menú principal. Mapea la tabla Menu del diagrama E/R; se llama
    // ItemMenu para no chocar con System.Web.UI.WebControls.Menu en los code-behind.
    public class ItemMenu
    {
        public int IdMenu { get; set; }
        public string Texto { get; set; }

        // Null cuando la opción es un grupo desplegable y no un link.
        public int? IdUrl { get; set; }

        // Ruta de la pantalla (ej: ~/Clientes). Null si es un grupo.
        public string Path { get; set; }

        public int? IdMenuPadre { get; set; }
        public int Orden { get; set; }

        // El rol ve la pantalla pero no puede modificar (matriz de permisos §5).
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

        // Arma el árbol a partir de la lista plana que devuelve MenuDAL.ListarPorNivel.
        // Los grupos que quedaron sin hijos visibles se descartan.
        public static List<ItemMenu> ArmarArbol(List<ItemMenu> planas)
        {
            var porId = new Dictionary<int, ItemMenu>();
            foreach (var item in planas)
                porId[item.IdMenu] = item;

            var raiz = new List<ItemMenu>();
            foreach (var item in planas)
            {
                ItemMenu padre;
                if (item.IdMenuPadre.HasValue && porId.TryGetValue(item.IdMenuPadre.Value, out padre))
                    padre.Hijos.Add(item);
                else if (!item.IdMenuPadre.HasValue)
                    raiz.Add(item);
                // Si el padre no está en el menú del rol, el hijo tampoco se muestra.
            }

            raiz.RemoveAll(m => m.EsGrupo && m.Hijos.Count == 0);

            foreach (var item in raiz)
                item.Hijos.Sort((a, b) => a.Orden.CompareTo(b.Orden));

            raiz.Sort((a, b) => a.Orden.CompareTo(b.Orden));
            return raiz;
        }
    }
}
