using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;
using BIZ.Negocio;

namespace BIZ.Data
{
    public static class UsuarioDAL
    {
        private const string SelectBase = @"
            SELECT u.idUsuario, u.nombre, u.apellido, u.email, u.passwordHash, u.passwordSalt,
                   u.idNivel, n.nombre AS nombreNivel, u.activo, u.fechaAlta
            FROM Usuario u
            INNER JOIN Nivel n ON n.idNivel = u.idNivel";

        private static Usuario Mapear(DataRow fila)
        {
            return new Usuario
            {
                IdUsuario = AccesoDatos.LeerInt(fila, "idUsuario"),
                Nombre = AccesoDatos.LeerString(fila, "nombre"),
                Apellido = AccesoDatos.LeerString(fila, "apellido"),
                Email = AccesoDatos.LeerString(fila, "email"),
                PasswordHash = AccesoDatos.LeerString(fila, "passwordHash"),
                PasswordSalt = AccesoDatos.LeerString(fila, "passwordSalt"),
                IdNivel = AccesoDatos.LeerInt(fila, "idNivel"),
                NombreNivel = AccesoDatos.LeerString(fila, "nombreNivel"),
                Activo = AccesoDatos.LeerBool(fila, "activo"),
                FechaAlta = AccesoDatos.LeerFecha(fila, "fechaAlta")
            };
        }

        public static List<Usuario> Listar(bool incluirInactivos = true)
        {
            var sql = SelectBase +
                      (incluirInactivos ? "" : " WHERE u.activo = 1") +
                      " ORDER BY u.apellido, u.nombre";

            var lista = new List<Usuario>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static Usuario ObtenerPorId(int idUsuario)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE u.idUsuario = @idUsuario",
                AccesoDatos.Param("@idUsuario", idUsuario));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public static Usuario ObtenerPorEmail(string email)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE u.email = @email",
                AccesoDatos.Param("@email", email));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public static bool ExisteEmail(string email, int idUsuarioExcluido = 0)
        {
            var cantidad = AccesoDatos.Escalar(
                "SELECT COUNT(*) FROM Usuario WHERE email = @email AND idUsuario <> @id",
                AccesoDatos.Param("@email", email),
                AccesoDatos.Param("@id", idUsuarioExcluido));

            return System.Convert.ToInt32(cantidad) > 0;
        }

        private static int Insertar(Usuario usuario)
        {
            const string sql = @"
                INSERT INTO Usuario (nombre, apellido, email, passwordHash, passwordSalt, idNivel, activo)
                VALUES (@nombre, @apellido, @email, @hash, @salt, @idNivel, @activo);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var id = AccesoDatos.Escalar(sql,
                AccesoDatos.Param("@nombre", usuario.Nombre),
                AccesoDatos.Param("@apellido", usuario.Apellido),
                AccesoDatos.Param("@email", usuario.Email),
                AccesoDatos.Param("@hash", usuario.PasswordHash),
                AccesoDatos.Param("@salt", usuario.PasswordSalt),
                AccesoDatos.Param("@idNivel", usuario.IdNivel),
                AccesoDatos.Param("@activo", usuario.Activo));

            return System.Convert.ToInt32(id);
        }

        // Da de alta el usuario con una contraseña temporal y se la manda por mail.
        // La contraseña generada vuelve en passwordTemporal para poder mostrarla en
        // pantalla si el mail no sale.
        public static ResultadoOperacion Crear(Usuario usuario, out string passwordTemporal)
        {
            passwordTemporal = null;

            var validacion = usuario.Validar();
            if (!validacion.Exito) return validacion;

            if (ExisteEmail(usuario.Email))
                return ResultadoOperacion.Error("Ya existe un usuario con ese mail.");

            passwordTemporal = PasswordHasher.GenerarPasswordTemporal();
            usuario.PasswordSalt = PasswordHasher.GenerarSalt();
            usuario.PasswordHash = PasswordHasher.Hashear(passwordTemporal, usuario.PasswordSalt);

            usuario.IdUsuario = Insertar(usuario);

            ServicioMail.Enviar(usuario.Email, "Tu cuenta en LubricentroControl",
                ServicioMail.ArmarCuerpoAltaUsuario(usuario.NombreCompleto, usuario.Email, passwordTemporal));

            return ResultadoOperacion.Ok("Usuario creado.");
        }

        // No dejar el sistema sin ningún administrador activo, ni permitir mail duplicado.
        public static ResultadoOperacion Actualizar(Usuario usuario)
        {
            var validacion = usuario.Validar();
            if (!validacion.Exito) return validacion;

            if (ExisteEmail(usuario.Email, usuario.IdUsuario))
                return ResultadoOperacion.Error("Ya existe otro usuario con ese mail.");

            var actual = ObtenerPorId(usuario.IdUsuario);
            if (actual == null)
                return ResultadoOperacion.Error("El usuario no existe.");

            var dejaDeSerAdminActivo = actual.EsAdmin && (usuario.IdNivel != Nivel.Admin || !usuario.Activo);
            if (dejaDeSerAdminActivo && ContarAdminsActivos() <= 1)
                return ResultadoOperacion.Error(
                    "Es el único administrador activo: asigná otro administrador antes de cambiarlo.");

            const string sql = @"
                UPDATE Usuario
                SET nombre = @nombre, apellido = @apellido, email = @email,
                    idNivel = @idNivel, activo = @activo
                WHERE idUsuario = @idUsuario";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@nombre", usuario.Nombre),
                AccesoDatos.Param("@apellido", usuario.Apellido),
                AccesoDatos.Param("@email", usuario.Email),
                AccesoDatos.Param("@idNivel", usuario.IdNivel),
                AccesoDatos.Param("@activo", usuario.Activo),
                AccesoDatos.Param("@idUsuario", usuario.IdUsuario));

            return ResultadoOperacion.Ok("Usuario actualizado.");
        }

        public static void ActualizarPassword(int idUsuario, string hash, string salt)
        {
            AccesoDatos.Ejecutar(
                "UPDATE Usuario SET passwordHash = @hash, passwordSalt = @salt WHERE idUsuario = @idUsuario",
                AccesoDatos.Param("@hash", hash),
                AccesoDatos.Param("@salt", salt),
                AccesoDatos.Param("@idUsuario", idUsuario));
        }

        // Genera hash y salt nuevos y los guarda. La usan CambiarPassword, BlanquearPassword
        // y RecuperacionClaveDAL.RestablecerPassword.
        public static void EstablecerPassword(int idUsuario, string passwordNueva)
        {
            var salt = PasswordHasher.GenerarSalt();
            var hash = PasswordHasher.Hashear(passwordNueva, salt);
            ActualizarPassword(idUsuario, hash, salt);
        }

        // Baja lógica: el usuario puede estar referenciado por órdenes y pagos,
        // así que nunca se borra físicamente. No se permite desactivarse a uno
        // mismo ni al último Admin.
        public static ResultadoOperacion Desactivar(int idUsuario, int idUsuarioLogueado)
        {
            if (idUsuario == idUsuarioLogueado)
                return ResultadoOperacion.Error("No podés desactivar tu propio usuario.");

            var usuario = ObtenerPorId(idUsuario);
            if (usuario == null)
                return ResultadoOperacion.Error("El usuario no existe.");

            if (!usuario.Activo)
                return ResultadoOperacion.Ok("El usuario ya estaba desactivado.");

            if (usuario.EsAdmin && ContarAdminsActivos() <= 1)
                return ResultadoOperacion.Error("Es el único administrador activo: no se puede desactivar.");

            AccesoDatos.Ejecutar(
                "UPDATE Usuario SET activo = 0 WHERE idUsuario = @idUsuario",
                AccesoDatos.Param("@idUsuario", idUsuario));

            return ResultadoOperacion.Ok("Usuario desactivado.");
        }

        // Blanquea la contraseña y manda la nueva por mail.
        public static ResultadoOperacion BlanquearPassword(int idUsuario, out string passwordTemporal)
        {
            passwordTemporal = null;

            var usuario = ObtenerPorId(idUsuario);
            if (usuario == null)
                return ResultadoOperacion.Error("El usuario no existe.");

            passwordTemporal = PasswordHasher.GenerarPasswordTemporal();
            EstablecerPassword(idUsuario, passwordTemporal);

            ServicioMail.Enviar(usuario.Email, "Tu contraseña de LubricentroControl fue restablecida",
                ServicioMail.ArmarCuerpoAltaUsuario(usuario.NombreCompleto, usuario.Email, passwordTemporal));

            return ResultadoOperacion.Ok("Contraseña restablecida.");
        }

        // Cantidad de administradores activos — evita quedarse sin ningún Admin.
        public static int ContarAdminsActivos()
        {
            var cantidad = AccesoDatos.Escalar(
                "SELECT COUNT(*) FROM Usuario WHERE idNivel = @idNivel AND activo = 1",
                AccesoDatos.Param("@idNivel", Nivel.Admin));

            return System.Convert.ToInt32(cantidad);
        }

        // Valida las credenciales. Devuelve el usuario en "usuario" solo si el login fue correcto.
        public static ResultadoOperacion Autenticar(string email, string password, out Usuario usuario)
        {
            usuario = null;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return ResultadoOperacion.Error("Ingresá tu mail y tu contraseña.");

            var encontrado = ObtenerPorEmail(email.Trim());

            // Mensaje único para usuario inexistente y contraseña incorrecta:
            // no queremos que el login sirva para averiguar qué mails existen.
            if (encontrado == null || !PasswordHasher.Verificar(password, encontrado.PasswordSalt, encontrado.PasswordHash))
                return ResultadoOperacion.Error("Mail o contraseña incorrectos.");

            if (!encontrado.Activo)
                return ResultadoOperacion.Error("Tu usuario está desactivado. Consultá con un administrador.");

            usuario = encontrado;
            return ResultadoOperacion.Ok();
        }

        public static ResultadoOperacion CambiarPassword(int idUsuario, string passwordActual,
                                                          string passwordNueva, string repeticion)
        {
            var usuario = ObtenerPorId(idUsuario);
            if (usuario == null)
                return ResultadoOperacion.Error("El usuario no existe.");

            if (!PasswordHasher.Verificar(passwordActual, usuario.PasswordSalt, usuario.PasswordHash))
                return ResultadoOperacion.Error("La contraseña actual no es correcta.");

            var validacion = Usuario.ValidarPassword(passwordNueva, repeticion);
            if (!validacion.Exito) return validacion;

            EstablecerPassword(idUsuario, passwordNueva);
            return ResultadoOperacion.Ok("Contraseña actualizada.");
        }
    }
}
