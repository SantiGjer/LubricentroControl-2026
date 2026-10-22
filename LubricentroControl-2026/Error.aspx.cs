using System;
using System.Web.UI;

namespace LubricentroControl_2026
{
    // Hereda de Page y no de PaginaConSesion: tiene que verse aunque no haya nadie logueado (un
    // error en Login, o la sesión vencida). El detalle del error no se muestra acá: lo escribe
    // Global.Application_Error en App_Data\Errores.log.
    public partial class PaginaError : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
