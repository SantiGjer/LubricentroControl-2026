using System;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // Pantalla pendiente de implementar.
    public partial class OrdenesDeTrabajo : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            pnlSoloLectura.Visible = EsSoloLectura;
        }
    }
}