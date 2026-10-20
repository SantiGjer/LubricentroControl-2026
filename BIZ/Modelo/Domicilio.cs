using System;
using System.Text;
using System.Text.RegularExpressions;

namespace BIZ.Modelo
{
    // Reglas del domicilio, compartidas por Cliente y Proveedor (Docs/Lubricentro_Requerimientos.md
    // §9.8 y §9.14): las provincias, el formato del código postal y cómo se escribe el domicilio en
    // una línea. Qué localidad y provincia le corresponden a un código postal lo sabe
    // CodigosPostales. No es un método de Cliente o Proveedor por el mismo motivo que
    // FormatoTelefono: las dos entidades tienen propiedades (CodigoPostal, Provincia) que taparían
    // el nombre de una clase llamada así.
    public static class Domicilio
    {
        public const string Caba = "Ciudad Autónoma de Buenos Aires";

        // Las 23 provincias y la Ciudad Autónoma de Buenos Aires, en orden alfabético.
        public static readonly string[] Provincias =
        {
            "Buenos Aires", "Catamarca", "Chaco", "Chubut", Caba, "Córdoba",
            "Corrientes", "Entre Ríos", "Formosa", "Jujuy", "La Pampa", "La Rioja", "Mendoza", "Misiones",
            "Neuquén", "Río Negro", "Salta", "San Juan", "San Luis", "Santa Cruz", "Santa Fe",
            "Santiago del Estero", "Tierra del Fuego", "Tucumán"
        };

        public const string MensajeCodigoPostal =
            "El código postal debe tener 4 números (1638) o el formato CPA (C1406GZA).";

        // Código postal: el de 4 dígitos (1638) o el CPA de 8 caracteres (C1406GZA): la letra de
        // la provincia, los 4 dígitos y 3 letras de la cuadra.
        private static readonly Regex FormatoCodigoPostal =
            new Regex(@"^(\d{4}|[A-Z]\d{4}[A-Z]{3})$", RegexOptions.Compiled);

        // Vacía es válida: la provincia no es obligatoria.
        public static bool EsProvinciaValida(string provincia)
        {
            return string.IsNullOrWhiteSpace(provincia) || Array.IndexOf(Provincias, provincia) >= 0;
        }

        // Vacío es válido: el código postal no es obligatorio.
        public static bool EsCodigoPostalValido(string codigoPostal)
        {
            return string.IsNullOrWhiteSpace(codigoPostal) ||
                   FormatoCodigoPostal.IsMatch(codigoPostal.Trim().ToUpperInvariant());
        }

        // En mayúsculas y sin espacios en los bordes; vacío queda null.
        public static string NormalizarCodigoPostal(string codigoPostal)
        {
            return string.IsNullOrWhiteSpace(codigoPostal) ? null : codigoPostal.Trim().ToUpperInvariant();
        }

        // Dirección, localidad, provincia y código postal, lo que esté cargado: "Av. Mitre 1560,
        // Avellaneda, Buenos Aires (CP 1870)". La provincia no se repite si es la misma localidad
        // (la Ciudad Autónoma de Buenos Aires).
        public static string Armar(string direccion, string localidad, string provincia, string codigoPostal)
        {
            if (!string.IsNullOrWhiteSpace(localidad) && !string.IsNullOrWhiteSpace(provincia) &&
                string.Equals(localidad.Trim(), provincia.Trim(), StringComparison.OrdinalIgnoreCase))
                provincia = null;

            var partes = new StringBuilder();
            foreach (var parte in new[] { direccion, localidad, provincia })
            {
                if (string.IsNullOrWhiteSpace(parte)) continue;
                if (partes.Length > 0) partes.Append(", ");
                partes.Append(parte.Trim());
            }
            if (!string.IsNullOrWhiteSpace(codigoPostal))
                partes.Append(partes.Length > 0 ? " (CP " : "(CP ").Append(codigoPostal.Trim()).Append(")");
            return partes.ToString();
        }
    }
}
