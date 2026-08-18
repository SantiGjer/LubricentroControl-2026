using System.Collections.Generic;
using System.Linq;
using BIZ.Data;
using BIZ.Modelo;

namespace BIZ.Negocio
{
    public static class MenuNegocio
    {
        /// <summary>
        /// Menú de un rol, ya armado como árbol y listo para renderizar.
        /// Los grupos que quedaron sin hijos visibles se descartan.
        /// </summary>
        public static List<ItemMenu> ObtenerArbol(int idNivel)
        {
            var planas = MenuDAL.ListarPorNivel(idNivel);
            var porId = planas.ToDictionary(m => m.IdMenu);

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

        /// <summary>True si el rol puede entrar a la pantalla.</summary>
        public static bool PuedeAcceder(int idNivel, string path)
        {
            return MenuDAL.ObtenerPermiso(idNivel, path) != null;
        }

        /// <summary>
        /// True si el rol entra a la pantalla pero sin poder modificar.
        /// Devuelve false cuando directamente no tiene acceso — consultarlo
        /// siempre junto con PuedeAcceder.
        /// </summary>
        public static bool EsSoloLectura(int idNivel, string path)
        {
            var permiso = MenuDAL.ObtenerPermiso(idNivel, path);
            return permiso != null && permiso.SoloLectura;
        }
    }
}
