using System;

namespace BIZ.Modelo
{
    // Kardex de stock: cada fila es un movimiento de entrada o salida sobre un Insumo,
    // con el stock resultante ya calculado (Docs/Lubricentro_Requerimientos.md §9.3).
    public class MovimientoStock
    {
        public const string TipoCompra = "Compra";
        public const string TipoOrden = "Orden";
        public const string TipoCancelacionOrden = "CancelacionOrden";
        public const string TipoAjusteManual = "AjusteManual";

        public int IdMovimiento { get; set; }
        public int IdInsumo { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoMovimiento { get; set; }
        public int? IdCompra { get; set; }
        public int? IdOrden { get; set; }
        public int IdUsuario { get; set; }
        public decimal Entrada { get; set; }
        public decimal Salida { get; set; }
        public decimal StockResultante { get; set; }
        public string Descripcion { get; set; }

        // Del JOIN con Usuario; solo para mostrar en el kardex, no se guarda en esta tabla.
        public string NombreUsuario { get; set; }
    }
}
