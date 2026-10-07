using System.Text.RegularExpressions;

namespace BIZ.Modelo
{
    // Formato de teléfono, compartido por Cliente y Proveedor (en los dos es opcional). Acepta
    // los separadores habituales al escribir un número — espacios, guiones, puntos, paréntesis y
    // un "+" adelante — y exige entre 6 y 15 dígitos: un número local sin característica tiene
    // de 6 a 8, y uno internacional completo no pasa de 15. No es un método de Cliente o Proveedor
    // porque las dos entidades tienen una propiedad Telefono que taparía el nombre de la clase.
    public static class FormatoTelefono
    {
        public const int MinimoDigitos = 6;
        public const int MaximoDigitos = 15;

        public const string MensajeError =
            "El teléfono solo puede tener números, espacios, guiones o paréntesis (entre 6 y 15 números).";

        private static readonly Regex Formato = new Regex(@"^\+?[0-9 ().-]+$", RegexOptions.Compiled);

        // Vacío es válido: el teléfono no es obligatorio.
        public static bool EsValido(string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono)) return true;

            var texto = telefono.Trim();
            if (!Formato.IsMatch(texto)) return false;

            var digitos = 0;
            foreach (var c in texto)
                if (c >= '0' && c <= '9') digitos++;

            return digitos >= MinimoDigitos && digitos <= MaximoDigitos;
        }
    }
}
