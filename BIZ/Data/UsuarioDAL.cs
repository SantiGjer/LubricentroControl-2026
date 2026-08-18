using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

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

        /// <summary>Inserta el usuario y devuelve el id generado.</summary>
        public static int Insertar(Usuario usuario)
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

        /// <summary>Actualiza los datos del usuario. No toca la contraseña.</summary>
        public static void Actualizar(Usuario usuario)
        {
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
        }

        public static void ActualizarPassword(int idUsuario, string hash, string salt)
        {
            AccesoDatos.Ejecutar(
                "UPDATE Usuario SET passwordHash = @hash, passwordSalt = @salt WHERE idUsuario = @idUsuario",
                AccesoDatos.Param("@hash", hash),
                AccesoDatos.Param("@salt", salt),
                AccesoDatos.Param("@idUsuario", idUsuario));
        }

        /// <summary>
        /// Baja lógica: el usuario puede estar referenciado por órdenes y pagos,
        /// así que nunca se borra físicamente.
        /// </summary>
        public static void Desactivar(int idUsuario)
        {
            AccesoDatos.Ejecutar(
                "UPDATE Usuario SET activo = 0 WHERE idUsuario = @idUsuario",
                AccesoDatos.Param("@idUsuario", idUsuario));
        }

        /// <summary>Cantidad de administradores activos — evita quedarse sin ningún Admin.</summary>
        public static int ContarAdminsActivos()
        {
            var cantidad = AccesoDatos.Escalar(
                "SELECT COUNT(*) FROM Usuario WHERE idNivel = @idNivel AND activo = 1",
                AccesoDatos.Param("@idNivel", Nivel.Admin));

            return System.Convert.ToInt32(cantidad);
        }
    }
}
