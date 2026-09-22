using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026.Reportes
{
    // Reporte de stock bajo (Requerimientos §6.9): insumos activos cuyo stockActual quedó por
    // debajo de su stockMinimo. Es de consulta por naturaleza — no hay acción de escritura que
    // deshabilitar. El acceso es todo o nada por rol ("Reportes financieros", Requerimientos §5),
    // no hay caso de solo-consulta parcial que manejar acá.
    public partial class StockBajo : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            CargarReporte();
        }

        private void CargarReporte()
        {
            // El filtro (activo = 1 AND stockActual < stockMinimo) lo hace el DAL; el orden lo
            // decide el reporte, que es lo que cambia según para qué se mira: acá primero lo más
            // urgente de reponer, no alfabético como en el ABM.
            var insumos = InsumoDAL.ListarStockBajo()
                .OrderByDescending(i => i.StockMinimo - i.StockActual)
                .ThenBy(i => i.Nombre)
                .ToList();

            gvStockBajo.DataSource = insumos;
            gvStockBajo.DataBind();

            litResumen.Text = ArmarResumen(insumos);
        }

        private static string ArmarResumen(List<Insumo> insumos)
        {
            if (insumos.Count == 0)
                return "Ningún insumo activo está por debajo de su stock mínimo.";

            var texto = insumos.Count == 1
                ? "<b>1</b> insumo por debajo del stock mínimo"
                : "<b>" + insumos.Count + "</b> insumos por debajo del stock mínimo";

            var sinStock = insumos.Count(i => i.StockActual <= 0);
            if (sinStock > 0)
                texto += sinStock == 1
                    ? ", <b>1</b> de ellos sin stock"
                    : ", <b>" + sinStock + "</b> de ellos sin stock";

            return texto + ".";
        }

        protected void gvStockBajo_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvStockBajo.PageIndex = e.NewPageIndex;
            CargarReporte();
        }

        // Todas las filas están bajo el mínimo, así que resaltarlas todas no diría nada: se marca
        // solo el caso grave, el insumo que ya quedó en cero y frena el trabajo.
        protected void gvStockBajo_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var insumo = (Insumo)e.Row.DataItem;
            if (insumo.StockActual <= 0)
                e.Row.Style.Add("background-color", "#f8d7da");
        }
    }
}
