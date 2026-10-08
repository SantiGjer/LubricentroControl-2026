using System;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // Inicio: tablero con lo que hay que atender hoy. Cada tarjeta lleva un botón a la pantalla
    // donde se resuelve, solo si el rol de la persona tiene acceso a esa pantalla.
    public partial class _Default : PaginaSegura
    {
        // Cuántos insumos con stock bajo se muestran en el tablero (el resto, en el reporte).
        private const int CantidadStockBajo = 3;

        // Cuántas órdenes en curso se listan en el tablero (el resto, en Órdenes de trabajo).
        private const int CantidadOrdenes = 10;

        protected void Page_Load(object sender, EventArgs e)
        {
            CargarTurnosDeHoy();
            CargarStockBajo();
            CargarOrdenesEnCurso();
        }

        private bool TieneAcceso(string path)
        {
            return MenuDAL.ObtenerPermiso(UsuarioActual.IdNivel, path) != null;
        }

        // Turnos de hoy que siguen vigentes: Solicitados (falta confirmar) y Confirmados.
        private void CargarTurnosDeHoy()
        {
            var turnos = TurnoDAL.ListarVigentesDelDia(DateTime.Today);

            gvTurnosHoy.DataSource = turnos;
            gvTurnosHoy.DataBind();

            if (turnos.Count > 0)
            {
                var solicitados = turnos.Count(t => t.Estado == Turno.EstadoSolicitado);
                var confirmados = turnos.Count(t => t.Estado == Turno.EstadoConfirmado);
                litResumenTurnos.Text = turnos.Count + (turnos.Count == 1 ? " turno pendiente" : " turnos pendientes")
                    + " para hoy (" + solicitados + (solicitados == 1 ? " solicitado" : " solicitados")
                    + ", " + confirmados + (confirmados == 1 ? " confirmado" : " confirmados") + ").";
            }

            lnkTurnos.Visible = TieneAcceso("~/Turnos");
        }

        // Los 3 insumos con mayor faltante (stock mínimo menos stock actual).
        private void CargarStockBajo()
        {
            var bajos = ProductoDAL.ListarStockBajo();

            gvStockBajo.DataSource = bajos
                .OrderByDescending(i => i.StockMinimo - i.StockActual)
                .ThenBy(i => i.Nombre)
                .Take(CantidadStockBajo)
                .ToList();
            gvStockBajo.DataBind();

            if (bajos.Count > 0)
            {
                litResumenStock.Text = bajos.Count + (bajos.Count == 1 ? " insumo por debajo del mínimo." : " insumos por debajo del mínimo.")
                    + (bajos.Count > CantidadStockBajo ? " Se muestran los " + CantidadStockBajo + " con mayor faltante." : "");
            }

            // Reportes no está disponible para todos los roles: quien no lo ve va a Productos.
            if (TieneAcceso("~/Reportes/StockBajo"))
            {
                lnkStock.NavigateUrl = "~/Reportes/StockBajo";
                lnkStock.Text = "Ver reporte de stock bajo";
            }
            else if (TieneAcceso("~/Productos"))
            {
                lnkStock.NavigateUrl = "~/Productos";
                lnkStock.Text = "Ir a Productos";
            }
            else
            {
                lnkStock.Visible = false;
            }
        }

        // Órdenes que siguen en el taller (Abierta o En proceso), la más antigua primero. Se
        // muestran las más viejas porque son las que más tiempo llevan esperando.
        private void CargarOrdenesEnCurso()
        {
            var ordenes = OrdenDeTrabajoDAL.ListarEnCurso();

            gvOrdenesEnCurso.DataSource = ordenes.Take(CantidadOrdenes).ToList();
            gvOrdenesEnCurso.DataBind();

            if (ordenes.Count > 0)
            {
                var abiertas = ordenes.Count(o => o.Estado == OrdenDeTrabajo.EstadoAbierta);
                var enProceso = ordenes.Count(o => o.Estado == OrdenDeTrabajo.EstadoEnProceso);
                litResumenOrdenes.Text = ordenes.Count + (ordenes.Count == 1 ? " orden en curso" : " órdenes en curso")
                    + " (" + abiertas + (abiertas == 1 ? " abierta" : " abiertas") + ", " + enProceso + " en proceso)."
                    + (ordenes.Count > CantidadOrdenes ? " Se muestran las " + CantidadOrdenes + " más antiguas." : "");
            }

            lnkOrdenes.Visible = TieneAcceso("~/OrdenesDeTrabajo");
        }

        // Mismo criterio que el reporte de stock bajo: se marca solo el insumo que ya quedó en cero.
        protected void gvStockBajo_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var insumo = (Producto)e.Row.DataItem;
            if (insumo.StockActual <= 0)
                e.Row.Style.Add("background-color", "#f8d7da");
        }
    }
}
