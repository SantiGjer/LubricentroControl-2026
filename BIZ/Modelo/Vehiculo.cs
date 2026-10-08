using System;
using System.Text.RegularExpressions;

namespace BIZ.Modelo
{
    public class Vehiculo
    {
        // Lista fija del DropDownList de tipo de combustible (Docs/Lubricentro_Requerimientos.md §9.2).
        public static readonly string[] TiposCombustible =
            { "Nafta", "Diésel", "GNC", "Eléctrico", "Híbrido" };

        // Acepta el formato viejo (3 letras + 3 números) y el Mercosur (2 letras + 3 números + 2 letras).
        private static readonly Regex FormatoPatente =
            new Regex(@"^([A-Z]{3}\d{3}|[A-Z]{2}\d{3}[A-Z]{2})$", RegexOptions.Compiled);

        // Rango del año del modelo: desde 1900 hasta el año que viene (un 0 km puede salir ya
        // con el modelo del año siguiente).
        public const int AnioMinimo = 1900;

        public static int AnioMaximo
        {
            get { return DateTime.Today.Year + 1; }
        }

        public int IdVehiculo { get; set; }
        public int IdCliente { get; set; }

        // Vienen del JOIN con Cliente; solo para mostrar en la grilla, no se guardan.
        public string NombreCliente { get; set; }
        public string TipoDocumentoCliente { get; set; }
        public string NumeroDocumentoCliente { get; set; }

        public string DocumentoCliente
        {
            get { return Cliente.FormatearDocumento(TipoDocumentoCliente, NumeroDocumentoCliente); }
        }

        public string Patente { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public int? Anio { get; set; }
        public string TipoCombustible { get; set; }
        public bool Activo { get; set; }

        // Acepta minúsculas: la usa el CustomValidator antes de normalizar en Validar().
        public static bool EsPatenteValida(string patente)
        {
            return patente != null && FormatoPatente.IsMatch(patente.Trim().ToUpperInvariant());
        }

        // El año es opcional: sin año también es válido.
        public static bool EsAnioValido(int? anio)
        {
            return !anio.HasValue || (anio.Value >= AnioMinimo && anio.Value <= AnioMaximo);
        }

        // Valida los campos obligatorios, normaliza la patente a mayúsculas y le saca
        // los espacios de los bordes a marca/modelo.
        public ResultadoOperacion Validar()
        {
            if (IdCliente <= 0)
                return ResultadoOperacion.Error("Seleccioná el cliente dueño del vehículo.");

            if (string.IsNullOrWhiteSpace(Patente))
                return ResultadoOperacion.Error("La patente es obligatoria.");

            if (!EsPatenteValida(Patente))
                return ResultadoOperacion.Error("La patente no tiene un formato válido (ej. ABC123 o AB123CD).");

            Patente = Patente.Trim().ToUpperInvariant();

            if (!EsAnioValido(Anio))
                return ResultadoOperacion.Error("El año tiene que estar entre " + AnioMinimo + " y " + AnioMaximo + ".");

            if (!string.IsNullOrWhiteSpace(TipoCombustible) && Array.IndexOf(TiposCombustible, TipoCombustible) < 0)
                return ResultadoOperacion.Error("El tipo de combustible no es válido.");

            Marca = string.IsNullOrWhiteSpace(Marca) ? null : Marca.Trim();
            Modelo = string.IsNullOrWhiteSpace(Modelo) ? null : Modelo.Trim();

            return ResultadoOperacion.Ok();
        }
    }
}
