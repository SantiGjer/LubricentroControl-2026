using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace LubricentroControl_2026.Utilidades
{
    // Ayudas de interfaz que comparten las pantallas del menú. La parte del navegador (modales,
    // tablas ordenables y filtrables, selectores con búsqueda) vive en Scripts\Lubricentro.js.
    public static class Interfaz
    {
        // Vuelve a abrir un modal de Bootstrap cuando termina de cargar la página: después de un
        // postback (Nuevo, Editar, un error al guardar) el formulario tiene que seguir a la vista.
        // Hay un solo modal abierto por vez, así que la clave del script es fija.
        public static void AbrirModal(Page pagina, string idModal)
        {
            ScriptManager.RegisterStartupScript(pagina, typeof(Interfaz), "abrirModal",
                "Lubricentro.abrirModal('" + idModal + "');", true);
        }

        // Opciones de un selector con búsqueda (.selector-busqueda), en JSON para su atributo
        // data-opciones: [{ "v": valor, "t": texto }, ...].
        public static string OpcionesJson(IEnumerable<ListItem> opciones)
        {
            var lista = new List<object>();
            foreach (var opcion in opciones)
                lista.Add(new { v = opcion.Value, t = opcion.Text });

            return new JavaScriptSerializer().Serialize(lista);
        }
    }
}
