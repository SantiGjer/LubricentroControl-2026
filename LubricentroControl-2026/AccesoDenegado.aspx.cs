using System;
using System.Web;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    public partial class AccesoDenegado : PaginaConSesion
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            litNivel.Text = HttpUtility.HtmlEncode(UsuarioActual.NombreNivel);
        }
    }
}
