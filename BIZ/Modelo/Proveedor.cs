using System.Text;
using System.Text.RegularExpressions;

namespace BIZ.Modelo
{
    public class Proveedor
    {
        private static readonly Regex FormatoCuit = new Regex(@"^\d{11}$", RegexOptions.Compiled);

        private static readonly Regex FormatoEmail =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public int IdProveedor { get; set; }
        public string RazonSocial { get; set; }
        public string Cuit { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Direccion { get; set; }
        public bool Activo { get; set; }

        // 11 dígitos; tolera guiones u otros separadores, se validan solo los dígitos
        // (Docs/Lubricentro_Requerimientos.md §9.1).
        public static bool EsCuitValido(string cuit)
        {
            return cuit != null && FormatoCuit.IsMatch(SoloDigitos(cuit));
        }

        public static bool EsEmailValido(string email)
        {
            return string.IsNullOrWhiteSpace(email) || FormatoEmail.IsMatch(email.Trim());
        }

        // Inserta los guiones para mostrar (NN-NNNNNNNN-N). Se guarda sin ellos.
        public static string FormatearCuit(string cuit)
        {
            var digitos = SoloDigitos(cuit ?? "");
            return digitos.Length == 11
                ? digitos.Substring(0, 2) + "-" + digitos.Substring(2, 8) + "-" + digitos.Substring(10, 1)
                : cuit;
        }

        private static string SoloDigitos(string valor)
        {
            var digitos = new StringBuilder();
            foreach (var c in valor)
                if (char.IsDigit(c)) digitos.Append(c);
            return digitos.ToString();
        }

        // Valida los campos obligatorios, normaliza el CUIT (le saca los guiones si
        // vino con ellos) y les saca los espacios de los bordes a los demás.
        public ResultadoOperacion Validar()
        {
            if (string.IsNullOrWhiteSpace(RazonSocial))
                return ResultadoOperacion.Error("La razón social es obligatoria.");

            if (string.IsNullOrWhiteSpace(Cuit))
                return ResultadoOperacion.Error("El CUIT es obligatorio.");

            if (!EsCuitValido(Cuit))
                return ResultadoOperacion.Error("El CUIT debe tener 11 números, con o sin guiones.");

            if (!EsEmailValido(Email))
                return ResultadoOperacion.Error("El mail no tiene un formato válido.");

            RazonSocial = RazonSocial.Trim();
            Cuit = SoloDigitos(Cuit);
            Telefono = string.IsNullOrWhiteSpace(Telefono) ? null : Telefono.Trim();
            Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
            Direccion = string.IsNullOrWhiteSpace(Direccion) ? null : Direccion.Trim();

            return ResultadoOperacion.Ok();
        }
    }
}
