using System;
using System.Security.Cryptography;

namespace BIZ.Negocio
{
    /// <summary>
    /// Hash de contraseñas con PBKDF2-SHA256, salt aleatorio por usuario.
    /// Si se cambia cualquiera de estas constantes, los hashes existentes
    /// (incluido el del admin sembrado en 02_DatosIniciales.sql) dejan de validar.
    /// </summary>
    public static class PasswordHasher
    {
        private const int Iteraciones = 25000;
        private const int LargoSalt = 16;
        private const int LargoHash = 32;

        public static string GenerarSalt()
        {
            var salt = new byte[LargoSalt];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }

        public static string Hashear(string password, string saltBase64)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (saltBase64 == null) throw new ArgumentNullException("saltBase64");

            var salt = Convert.FromBase64String(saltBase64);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iteraciones, HashAlgorithmName.SHA256))
            {
                return Convert.ToBase64String(pbkdf2.GetBytes(LargoHash));
            }
        }

        public static bool Verificar(string password, string saltBase64, string hashEsperado)
        {
            if (string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(saltBase64) ||
                string.IsNullOrEmpty(hashEsperado))
                return false;

            try
            {
                return SonIguales(Hashear(password, saltBase64), hashEsperado);
            }
            catch (FormatException)
            {
                // Salt corrupto en la base: se trata como credencial inválida.
                return false;
            }
        }

        /// <summary>Comparación en tiempo constante, para no filtrar información por el tiempo de respuesta.</summary>
        private static bool SonIguales(string a, string b)
        {
            if (a.Length != b.Length) return false;

            var diferencia = 0;
            for (var i = 0; i < a.Length; i++)
                diferencia |= a[i] ^ b[i];

            return diferencia == 0;
        }

        /// <summary>Genera una contraseña aleatoria legible, para el alta de usuarios.</summary>
        public static string GenerarPasswordTemporal()
        {
            const string alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            var bytes = new byte[10];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            var resultado = new char[10];
            for (var i = 0; i < bytes.Length; i++)
                resultado[i] = alfabeto[bytes[i] % alfabeto.Length];

            return new string(resultado);
        }
    }
}
