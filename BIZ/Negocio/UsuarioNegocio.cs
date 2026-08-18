using System.Collections.Generic;
using System.Text.RegularExpressions;
using BIZ.Data;
using BIZ.Modelo;

namespace BIZ.Negocio
{
    /// <summary>ABM de usuarios y asignación de rol.</summary>
    public static class UsuarioNegocio
    {
        private static readonly Regex FormatoEmail =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public static List<Usuario> Listar()
        {
            return UsuarioDAL.Listar();
        }

        public static Usuario ObtenerPorId(int idUsuario)
        {
            return UsuarioDAL.ObtenerPorId(idUsuario);
        }

        public static List<Nivel> ListarNiveles()
        {
            return NivelDAL.Listar();
        }

        /// <summary>
        /// Da de alta el usuario con una contraseña temporal y se la manda por mail.
        /// La contraseña generada vuelve en <paramref name="passwordTemporal"/> para
        /// poder mostrarla en pantalla si el mail no sale.
        /// </summary>
        public static ResultadoOperacion Crear(Usuario usuario, out string passwordTemporal)
        {
            passwordTemporal = null;

            var validacion = Validar(usuario);
            if (!validacion.Exito) return validacion;

            if (UsuarioDAL.ExisteEmail(usuario.Email))
                return ResultadoOperacion.Error("Ya existe un usuario con ese mail.");

            passwordTemporal = PasswordHasher.GenerarPasswordTemporal();
            usuario.PasswordSalt = PasswordHasher.GenerarSalt();
            usuario.PasswordHash = PasswordHasher.Hashear(passwordTemporal, usuario.PasswordSalt);

            usuario.IdUsuario = UsuarioDAL.Insertar(usuario);

            ServicioMail.Enviar(usuario.Email, "Tu cuenta en LubricentroControl",
                ServicioMail.ArmarCuerpoAltaUsuario(usuario.NombreCompleto, usuario.Email, passwordTemporal));

            return ResultadoOperacion.Ok("Usuario creado.");
        }

        public static ResultadoOperacion Actualizar(Usuario usuario)
        {
            var validacion = Validar(usuario);
            if (!validacion.Exito) return validacion;

            if (UsuarioDAL.ExisteEmail(usuario.Email, usuario.IdUsuario))
                return ResultadoOperacion.Error("Ya existe otro usuario con ese mail.");

            var actual = UsuarioDAL.ObtenerPorId(usuario.IdUsuario);
            if (actual == null)
                return ResultadoOperacion.Error("El usuario no existe.");

            // No dejar el sistema sin ningún administrador activo.
            var dejaDeSerAdminActivo = actual.EsAdmin && (usuario.IdNivel != Nivel.Admin || !usuario.Activo);
            if (dejaDeSerAdminActivo && UsuarioDAL.ContarAdminsActivos() <= 1)
                return ResultadoOperacion.Error(
                    "Es el único administrador activo: asigná otro administrador antes de cambiarlo.");

            UsuarioDAL.Actualizar(usuario);
            return ResultadoOperacion.Ok("Usuario actualizado.");
        }

        /// <summary>Baja lógica. No se permite desactivarse a uno mismo ni al último Admin.</summary>
        public static ResultadoOperacion Desactivar(int idUsuario, int idUsuarioLogueado)
        {
            if (idUsuario == idUsuarioLogueado)
                return ResultadoOperacion.Error("No podés desactivar tu propio usuario.");

            var usuario = UsuarioDAL.ObtenerPorId(idUsuario);
            if (usuario == null)
                return ResultadoOperacion.Error("El usuario no existe.");

            if (!usuario.Activo)
                return ResultadoOperacion.Ok("El usuario ya estaba desactivado.");

            if (usuario.EsAdmin && UsuarioDAL.ContarAdminsActivos() <= 1)
                return ResultadoOperacion.Error("Es el único administrador activo: no se puede desactivar.");

            UsuarioDAL.Desactivar(idUsuario);
            return ResultadoOperacion.Ok("Usuario desactivado.");
        }

        /// <summary>Blanquea la contraseña y manda la nueva por mail.</summary>
        public static ResultadoOperacion BlanquearPassword(int idUsuario, out string passwordTemporal)
        {
            passwordTemporal = null;

            var usuario = UsuarioDAL.ObtenerPorId(idUsuario);
            if (usuario == null)
                return ResultadoOperacion.Error("El usuario no existe.");

            passwordTemporal = PasswordHasher.GenerarPasswordTemporal();
            SeguridadNegocio.EstablecerPassword(idUsuario, passwordTemporal);

            ServicioMail.Enviar(usuario.Email, "Tu contraseña de LubricentroControl fue restablecida",
                ServicioMail.ArmarCuerpoAltaUsuario(usuario.NombreCompleto, usuario.Email, passwordTemporal));

            return ResultadoOperacion.Ok("Contraseña restablecida.");
        }

        private static ResultadoOperacion Validar(Usuario usuario)
        {
            if (usuario == null)
                return ResultadoOperacion.Error("No hay datos para guardar.");

            if (string.IsNullOrWhiteSpace(usuario.Nombre))
                return ResultadoOperacion.Error("El nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(usuario.Apellido))
                return ResultadoOperacion.Error("El apellido es obligatorio.");

            if (string.IsNullOrWhiteSpace(usuario.Email))
                return ResultadoOperacion.Error("El mail es obligatorio.");

            if (!FormatoEmail.IsMatch(usuario.Email.Trim()))
                return ResultadoOperacion.Error("El mail no tiene un formato válido.");

            if (usuario.IdNivel <= 0)
                return ResultadoOperacion.Error("Seleccioná un rol.");

            usuario.Nombre = usuario.Nombre.Trim();
            usuario.Apellido = usuario.Apellido.Trim();
            usuario.Email = usuario.Email.Trim();

            return ResultadoOperacion.Ok();
        }
    }
}
