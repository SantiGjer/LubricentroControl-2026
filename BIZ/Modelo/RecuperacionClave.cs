using System;
using System.Security.Cryptography;

namespace BIZ.Modelo
{
    // Token de recuperación de contraseña: de un solo uso y con vencimiento.
    public class RecuperacionClave
    {
        // Minutos de vigencia del token de recuperación.
        public const int MinutosVigenciaToken = 60;

        public int IdRecuperacion { get; set; }
        public int IdUsuario { get; set; }
        // Al insertar, el token que viaja en el enlace; leído de la base, su hash (HashToken).
        public string Token { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public bool Usado { get; set; }
        public DateTime? FechaUso { get; set; }

        public bool EstaVigente
        {
            get { return !Usado && FechaVencimiento > DateTime.Now; }
        }

        // Valida el token ya cargado (asume que se encontró por RecuperacionClaveDAL.ObtenerPorToken).
        public ResultadoOperacion Validar()
        {
            if (Usado)
                return ResultadoOperacion.Error("Este enlace ya fue usado. Pedí uno nuevo.");

            if (FechaVencimiento <= DateTime.Now)
                return ResultadoOperacion.Error("El enlace venció. Pedí uno nuevo.");

            return ResultadoOperacion.Ok();
        }

        // Token aleatorio apto para URL (Base64 sin caracteres conflictivos).
        public static string GenerarToken()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes)
                          .Replace('+', '-')
                          .Replace('/', '_')
                          .TrimEnd('=');
        }

        // La base guarda el SHA-256 del token (64 caracteres hexadecimales), no el token: quien lea
        // la tabla RecuperacionClave no puede armar un enlace válido. Desde el 2026-10-22; como la
        // contraseña, pero sin salt ni iteraciones, porque el token ya es aleatorio de 256 bits.
        public static string HashToken(string token)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token ?? ""));
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }
    }
}
