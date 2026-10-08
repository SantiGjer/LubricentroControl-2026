using System;
using System.Text.RegularExpressions;

namespace BIZ.Modelo
{
    // Lo que se vende en una orden de trabajo (Docs/Lubricentro_Requerimientos.md §9.9). Supertipo
    // de dos subcategorías, fijas desde el alta: Servicio (mano de obra, sin stock) e Insumo (con
    // stock y kardex). En la base son tres tablas — Producto con lo común, Servicio e Insumo con el
    // mismo id —; acá es una sola clase plana para poder listar las dos subcategorías juntas en una
    // grilla: marca, unidad y stock solo tienen sentido en un insumo.
    public class Producto
    {
        public const string TipoServicio = "Servicio";
        public const string TipoInsumo = "Insumo";

        public static readonly string[] Tipos = { TipoServicio, TipoInsumo };

        // Lista fija para el DropDownList de unidad de medida (Requerimientos §9.3).
        public static readonly string[] UnidadesDeMedida = { "Unidad", "Litro", "Kilogramo", "Caja", "Metro" };

        // SKU y código de barras: letras, números y guiones, sin espacios.
        private static readonly Regex FormatoCodigo = new Regex(@"^[A-Za-z0-9-]{1,50}$", RegexOptions.Compiled);

        public int IdProducto { get; set; }
        public string Tipo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Sku { get; set; }
        public string CodigoBarras { get; set; }

        // Precio final al público, con el IVA incluido.
        public decimal Precio { get; set; }

        public string TipoIva { get; set; }
        public decimal AlicuotaIva { get; set; }
        public bool Activo { get; set; }

        // Solo insumos.
        public string Marca { get; set; }
        public string UnidadMedida { get; set; }

        // No se edita directo: todo cambio pasa por MovimientoStockDAL, para que quede respaldado
        // por un movimiento en el kardex. Ver ProductoDAL.Actualizar.
        public decimal StockActual { get; set; }

        public decimal StockMinimo { get; set; }

        // Fecha de la imagen del producto, null si no tiene (§9.13). La imagen no viaja con el
        // producto: la pantalla la pide aparte (ImagenProducto.ashx) con esta fecha en la dirección,
        // para que al cambiarla el navegador no siga mostrando la anterior.
        public DateTime? FechaImagen { get; set; }

        public bool TieneImagen
        {
            get { return FechaImagen.HasValue; }
        }

        public bool EsInsumo
        {
            get { return Tipo == TipoInsumo; }
        }

        public bool EsServicio
        {
            get { return Tipo == TipoServicio; }
        }

        // Insumo activo con el stock por debajo del mínimo.
        public bool StockBajo
        {
            get { return EsInsumo && Activo && StockActual < StockMinimo; }
        }

        // "21 %", "Exento" o "No gravado".
        public string IvaDescripcion
        {
            get { return Iva.Describir(TipoIva, AlicuotaIva); }
        }

        // Vacío es válido: el SKU y el código de barras no son obligatorios.
        public static bool EsCodigoValido(string codigo)
        {
            return string.IsNullOrWhiteSpace(codigo) || FormatoCodigo.IsMatch(codigo.Trim());
        }

        // Valida los campos obligatorios y los normaliza. Un servicio no lleva marca, unidad ni
        // stock; un producto que no está gravado no lleva alícuota.
        public ResultadoOperacion Validar()
        {
            if (Array.IndexOf(Tipos, Tipo) < 0)
                return ResultadoOperacion.Error("Elegí si el producto es un servicio o un insumo.");

            if (string.IsNullOrWhiteSpace(Nombre))
                return ResultadoOperacion.Error("El nombre es obligatorio.");

            if (Precio < 0)
                return ResultadoOperacion.Error("El precio no puede ser negativo.");

            if (!Iva.EsTipoValido(TipoIva))
                return ResultadoOperacion.Error("Elegí el tipo de IVA.");

            if (TipoIva == Iva.Gravado && !Iva.EsAlicuotaValida(AlicuotaIva))
                return ResultadoOperacion.Error("Elegí la alícuota de IVA.");

            if (!EsCodigoValido(Sku))
                return ResultadoOperacion.Error("El SKU solo puede tener letras, números y guiones.");

            if (!EsCodigoValido(CodigoBarras))
                return ResultadoOperacion.Error("El código de barras solo puede tener letras, números y guiones.");

            if (EsInsumo)
            {
                if (StockMinimo < 0)
                    return ResultadoOperacion.Error("El stock mínimo no puede ser negativo.");

                if (StockActual < 0)
                    return ResultadoOperacion.Error("El stock no puede ser negativo.");

                if (!string.IsNullOrWhiteSpace(UnidadMedida) && Array.IndexOf(UnidadesDeMedida, UnidadMedida) < 0)
                    return ResultadoOperacion.Error("La unidad de medida no es válida.");

                Marca = string.IsNullOrWhiteSpace(Marca) ? null : Marca.Trim();
                UnidadMedida = string.IsNullOrWhiteSpace(UnidadMedida) ? null : UnidadMedida;
            }
            else
            {
                Marca = null;
                UnidadMedida = null;
                StockActual = 0;
                StockMinimo = 0;
            }

            if (TipoIva != Iva.Gravado) AlicuotaIva = 0;

            Nombre = Nombre.Trim();
            Descripcion = string.IsNullOrWhiteSpace(Descripcion) ? null : Descripcion.Trim();
            Sku = string.IsNullOrWhiteSpace(Sku) ? null : Sku.Trim().ToUpperInvariant();
            CodigoBarras = string.IsNullOrWhiteSpace(CodigoBarras) ? null : CodigoBarras.Trim();

            return ResultadoOperacion.Ok();
        }
    }
}
