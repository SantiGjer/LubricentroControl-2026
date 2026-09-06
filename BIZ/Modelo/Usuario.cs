using System;
using System.Text.RegularExpressions;

namespace BIZ.Modelo
{
    public class Usuario
    {
        // Largo mínimo exigido a una contraseña nueva.
        public const int LargoMinimoPassword = 8;

        private static readonly Regex FormatoEmail =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }

        // Hash PBKDF2 en Base64. Nunca se muestra ni se loguea.
        public string PasswordHash { get; set; }

        // Salt aleatorio en Base64, propio de este usuario.
        public string PasswordSalt { get; set; }

        public int IdNivel { get; set; }
        public string NombreNivel { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaAlta { get; set; }

        public string NombreCompleto
        {
            get { return (Nombre + " " + Apellido).Trim(); }
        }

        // True si el usuario tiene al menos el nivel pedido (jerarquía menor o igual).
        public bool TieneNivelMinimo(int idNivelRequerido)
        {
            return IdNivel <= idNivelRequerido;
        }

        public bool EsAdmin
        {
            get { return IdNivel == Nivel.Admin; }
        }

        // Valida los campos obligatorios y les saca los espacios de los bordes.
        public ResultadoOperacion Validar()
        {
            if (string.IsNullOrWhiteSpace(Nombre))
                return ResultadoOperacion.Error("El nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(Apellido))
                return ResultadoOperacion.Error("El apellido es obligatorio.");

            if (string.IsNullOrWhiteSpace(Email))
                return ResultadoOperacion.Error("El mail es obligatorio.");

            if (!FormatoEmail.IsMatch(Email.Trim()))
                return ResultadoOperacion.Error("El mail no tiene un formato válido.");

            if (IdNivel <= 0)
                return ResultadoOperacion.Error("Seleccioná un rol.");

            Nombre = Nombre.Trim();
            Apellido = Apellido.Trim();
            Email = Email.Trim();

            return ResultadoOperacion.Ok();
        }

        public static ResultadoOperacion ValidarPassword(string password, string repeticion)
        {
            if (string.IsNullOrWhiteSpace(password))
                return ResultadoOperacion.Error("Ingresá la contraseña nueva.");

            if (password.Length < LargoMinimoPassword)
                return ResultadoOperacion.Error(
                    "La contraseña debe tener al menos " + LargoMinimoPassword + " caracteres.");

            if (password != repeticion)
                return ResultadoOperacion.Error("Las contraseñas no coinciden.");

            return ResultadoOperacion.Ok();
        }
    }
}
