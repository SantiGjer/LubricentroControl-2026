using System;
using System.Security.Cryptography;
using BIZ.Data;
using BIZ.Modelo;

namespace BIZ.Negocio
{
    /// <summary>Login, cambio de contraseña y recuperación por mail.</summary>
    public static class SeguridadNegocio
    {
        /// <summary>Minutos de vigencia del token de recuperación.</summary>
        public const int MinutosVigenciaToken = 60;

        /// <summary>Largo mínimo exigido a una contraseña nueva.</summary>
        public const int LargoMinimoPassword = 8;

        /// <summary>
        /// Valida las credenciales. Devuelve el usuario en <paramref name="usuario"/>
        /// solo si el login fue correcto.
        /// </summary>
        public static ResultadoOperacion Autenticar(string email, string password, out Usuario usuario)
        {
            usuario = null;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return ResultadoOperacion.Error("Ingresá tu mail y tu contraseña.");

            var encontrado = UsuarioDAL.ObtenerPorEmail(email.Trim());

            // Mensaje único para usuario inexistente y contraseña incorrecta:
            // no queremos que el login sirva para averiguar qué mails existen.
            if (encontrado == null || !PasswordHasher.Verificar(password, encontrado.PasswordSalt, encontrado.PasswordHash))
                return ResultadoOperacion.Error("Mail o contraseña incorrectos.");

            if (!encontrado.Activo)
                return ResultadoOperacion.Error("Tu usuario está desactivado. Consultá con un administrador.");

            usuario = encontrado;
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

        public static ResultadoOperacion CambiarPassword(int idUsuario, string passwordActual,
                                                         string passwordNueva, string repeticion)
        {
            var usuario = UsuarioDAL.ObtenerPorId(idUsuario);
            if (usuario == null)
                return ResultadoOperacion.Error("El usuario no existe.");

            if (!PasswordHasher.Verificar(passwordActual, usuario.PasswordSalt, usuario.PasswordHash))
                return ResultadoOperacion.Error("La contraseña actual no es correcta.");

            var validacion = ValidarPassword(passwordNueva, repeticion);
            if (!validacion.Exito) return validacion;

            EstablecerPassword(idUsuario, passwordNueva);
            return ResultadoOperacion.Ok("Contraseña actualizada.");
        }

        public static void EstablecerPassword(int idUsuario, string passwordNueva)
        {
            var salt = PasswordHasher.GenerarSalt();
            var hash = PasswordHasher.Hashear(passwordNueva, salt);
            UsuarioDAL.ActualizarPassword(idUsuario, hash, salt);
        }

        /// <summary>
        /// Genera un token de recuperación y manda el mail con el enlace.
        /// Devuelve Ok aunque el mail no exista: informar lo contrario permitiría
        /// enumerar las cuentas del sistema desde afuera.
        /// </summary>
        public static ResultadoOperacion SolicitarRecuperacion(string email, Func<string, string> armarEnlace)
        {
            const string mensajeGenerico =
                "Si el mail está registrado, te enviamos las instrucciones para restablecer la contraseña.";

            if (string.IsNullOrWhiteSpace(email))
                return ResultadoOperacion.Error("Ingresá tu mail.");

            var usuario = UsuarioDAL.ObtenerPorEmail(email.Trim());
            if (usuario == null || !usuario.Activo)
                return ResultadoOperacion.Ok(mensajeGenerico);

            RecuperacionClaveDAL.InvalidarPendientes(usuario.IdUsuario);

            var token = GenerarToken();
            RecuperacionClaveDAL.Insertar(new RecuperacionClave
            {
                IdUsuario = usuario.IdUsuario,
                Token = token,
                FechaVencimiento = DateTime.Now.AddMinutes(MinutosVigenciaToken)
            });

            var cuerpo = ServicioMail.ArmarCuerpoRecuperacion(
                usuario.NombreCompleto, armarEnlace(token), MinutosVigenciaToken);

            ServicioMail.Enviar(usuario.Email, "Restablecer tu contraseña — LubricentroControl", cuerpo);

            return ResultadoOperacion.Ok(mensajeGenerico);
        }

        /// <summary>Valida el token sin consumirlo, para decidir si se muestra el formulario.</summary>
        public static ResultadoOperacion ValidarToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return ResultadoOperacion.Error("El enlace no es válido.");

            var recuperacion = RecuperacionClaveDAL.ObtenerPorToken(token);
            if (recuperacion == null)
                return ResultadoOperacion.Error("El enlace no es válido.");

            if (recuperacion.Usado)
                return ResultadoOperacion.Error("Este enlace ya fue usado. Pedí uno nuevo.");

            if (recuperacion.FechaVencimiento <= DateTime.Now)
                return ResultadoOperacion.Error("El enlace venció. Pedí uno nuevo.");

            return ResultadoOperacion.Ok();
        }

        /// <summary>Consume el token y deja la contraseña nueva.</summary>
        public static ResultadoOperacion RestablecerPassword(string token, string passwordNueva, string repeticion)
        {
            var validacionToken = ValidarToken(token);
            if (!validacionToken.Exito) return validacionToken;

            var validacionPassword = ValidarPassword(passwordNueva, repeticion);
            if (!validacionPassword.Exito) return validacionPassword;

            var recuperacion = RecuperacionClaveDAL.ObtenerPorToken(token);
            EstablecerPassword(recuperacion.IdUsuario, passwordNueva);
            RecuperacionClaveDAL.MarcarUsado(recuperacion.IdRecuperacion);

            return ResultadoOperacion.Ok("Listo, ya podés ingresar con tu contraseña nueva.");
        }

        /// <summary>Token aleatorio apto para URL (Base64 sin caracteres conflictivos).</summary>
        private static string GenerarToken()
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
    }
}
