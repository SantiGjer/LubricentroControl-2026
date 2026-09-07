using System;

namespace BIZ.Modelo
{
    public class Insumo
    {
        // Lista fija para el DropDownList de unidad de medida (Docs/Lubricentro_Requerimientos.md §9.3).
        public static readonly string[] UnidadesDeMedida = { "Unidad", "Litro", "Kilogramo", "Caja", "Metro" };

        public int IdInsumo { get; set; }
        public string Nombre { get; set; }
        public string Marca { get; set; }
        public string UnidadMedida { get; set; }

        // No se edita directo: todo cambio pasa por MovimientoStockDAL.Registrar, para que
        // quede backeado por un movimiento en el kardex. Ver InsumoDAL.Actualizar.
        public decimal StockActual { get; set; }

        public decimal StockMinimo { get; set; }
        public decimal PrecioVenta { get; set; }
        public bool Activo { get; set; }

        // Valida los campos obligatorios y les saca los espacios de los bordes.
        public ResultadoOperacion Validar()
        {
            if (string.IsNullOrWhiteSpace(Nombre))
                return ResultadoOperacion.Error("El nombre es obligatorio.");

            if (PrecioVenta < 0)
                return ResultadoOperacion.Error("El precio de venta no puede ser negativo.");

            if (StockMinimo < 0)
                return ResultadoOperacion.Error("El stock mínimo no puede ser negativo.");

            if (!string.IsNullOrWhiteSpace(UnidadMedida) && Array.IndexOf(UnidadesDeMedida, UnidadMedida) < 0)
                return ResultadoOperacion.Error("La unidad de medida no es válida.");

            Nombre = Nombre.Trim();
            Marca = string.IsNullOrWhiteSpace(Marca) ? null : Marca.Trim();

            return ResultadoOperacion.Ok();
        }
    }
}
