using System;
using System.Text.RegularExpressions;

namespace BIZ.Modelo
{
    public class Cliente
    {
        private static readonly Regex FormatoDni = new Regex(@"^\d{7,8}$", RegexOptions.Compiled);

        private static readonly Regex FormatoEmail =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public int IdCliente { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Dni { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Direccion { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaAlta { get; set; }

        public string NombreCompleto
        {
            get { return (Nombre + " " + Apellido).Trim(); }
        }

        // 7 u 8 dígitos, sin puntos (Docs/Lubricentro_Requerimientos.md §9.1).
        public static bool EsDniValido(string dni)
        {
            return dni != null && FormatoDni.IsMatch(dni.Trim());
        }

        // Valida los campos obligatorios y les saca los espacios de los bordes.
        public ResultadoOperacion Validar()
        {
            if (string.IsNullOrWhiteSpace(Nombre))
                return ResultadoOperacion.Error("El nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(Apellido))
                return ResultadoOperacion.Error("El apellido es obligatorio.");

            if (string.IsNullOrWhiteSpace(Dni))
                return ResultadoOperacion.Error("El DNI es obligatorio.");

            if (!EsDniValido(Dni.Trim()))
                return ResultadoOperacion.Error("El DNI debe tener 7 u 8 números, sin puntos.");

            if (!string.IsNullOrWhiteSpace(Email) && !FormatoEmail.IsMatch(Email.Trim()))
                return ResultadoOperacion.Error("El mail no tiene un formato válido.");

            Nombre = Nombre.Trim();
            Apellido = Apellido.Trim();
            Dni = Dni.Trim();
            Telefono = string.IsNullOrWhiteSpace(Telefono) ? null : Telefono.Trim();
            Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
            Direccion = string.IsNullOrWhiteSpace(Direccion) ? null : Direccion.Trim();

            return ResultadoOperacion.Ok();
        }
    }
}
