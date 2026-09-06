using System;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class RecuperacionClaveDAL
    {
        public static void Insertar(RecuperacionClave recuperacion)
        {
            const string sql = @"
                INSERT INTO RecuperacionClave (idUsuario, token, fechaVencimiento, usado)
                VALUES (@idUsuario, @token, @fechaVencimiento, 0)";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@idUsuario", recuperacion.IdUsuario),
                AccesoDatos.Param("@token", recuperacion.Token),
                AccesoDatos.Param("@fechaVencimiento", recuperacion.FechaVencimiento));
        }

        public static RecuperacionClave ObtenerPorToken(string token)
        {
            const string sql = @"
                SELECT idRecuperacion, idUsuario, token, fechaSolicitud, fechaVencimiento, usado, fechaUso
                FROM RecuperacionClave
                WHERE token = @token";

            var tabla = AccesoDatos.Consultar(sql, AccesoDatos.Param("@token", token));
            if (tabla.Rows.Count == 0) return null;

            var fila = tabla.Rows[0];
            return new RecuperacionClave
            {
                IdRecuperacion = AccesoDatos.LeerInt(fila, "idRecuperacion"),
                IdUsuario = AccesoDatos.LeerInt(fila, "idUsuario"),
                Token = AccesoDatos.LeerString(fila, "token"),
                FechaSolicitud = AccesoDatos.LeerFecha(fila, "fechaSolicitud"),
                FechaVencimiento = AccesoDatos.LeerFecha(fila, "fechaVencimiento"),
                Usado = AccesoDatos.LeerBool(fila, "usado"),
                FechaUso = AccesoDatos.LeerFechaNullable(fila, "fechaUso")
            };
        }

        public static void MarcarUsado(int idRecuperacion)
        {
            AccesoDatos.Ejecutar(
                "UPDATE RecuperacionClave SET usado = 1, fechaUso = @ahora WHERE idRecuperacion = @id",
                AccesoDatos.Param("@ahora", DateTime.Now),
                AccesoDatos.Param("@id", idRecuperacion));
        }

        // Invalida los pedidos anteriores del usuario: al pedir un token nuevo,
        // los viejos dejan de servir.
        public static void InvalidarPendientes(int idUsuario)
        {
            AccesoDatos.Ejecutar(
                "UPDATE RecuperacionClave SET usado = 1, fechaUso = @ahora WHERE idUsuario = @idUsuario AND usado = 0",
                AccesoDatos.Param("@ahora", DateTime.Now),
                AccesoDatos.Param("@idUsuario", idUsuario));
        }

        // Genera un token de recuperación y manda el mail con el enlace.
        // Devuelve Ok aunque el mail no exista: informar lo contrario permitiría
        // enumerar las cuentas del sistema desde afuera.
        public static ResultadoOperacion SolicitarRecuperacion(string email, Func<string, string> armarEnlace)
        {
            const string mensajeGenerico =
                "Si el mail está registrado, te enviamos las instrucciones para restablecer la contraseña.";

            if (string.IsNullOrWhiteSpace(email))
                return ResultadoOperacion.Error("Ingresá tu mail.");

            var usuario = UsuarioDAL.ObtenerPorEmail(email.Trim());
            if (usuario == null || !usuario.Activo)
                return ResultadoOperacion.Ok(mensajeGenerico);

            InvalidarPendientes(usuario.IdUsuario);

            var token = RecuperacionClave.GenerarToken();
            Insertar(new RecuperacionClave
            {
                IdUsuario = usuario.IdUsuario,
                Token = token,
                FechaVencimiento = DateTime.Now.AddMinutes(RecuperacionClave.MinutosVigenciaToken)
            });

            var cuerpo = ServicioMail.ArmarCuerpoRecuperacion(
                usuario.NombreCompleto, armarEnlace(token), RecuperacionClave.MinutosVigenciaToken);

            ServicioMail.Enviar(usuario.Email, "Restablecer tu contraseña — LubricentroControl", cuerpo);

            return ResultadoOperacion.Ok(mensajeGenerico);
        }

        // Valida el token sin consumirlo, para decidir si se muestra el formulario.
        public static ResultadoOperacion ValidarToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return ResultadoOperacion.Error("El enlace no es válido.");

            var recuperacion = ObtenerPorToken(token);
            if (recuperacion == null)
                return ResultadoOperacion.Error("El enlace no es válido.");

            return recuperacion.Validar();
        }

        // Consume el token y deja la contraseña nueva.
        public static ResultadoOperacion RestablecerPassword(string token, string passwordNueva, string repeticion)
        {
            var validacionToken = ValidarToken(token);
            if (!validacionToken.Exito) return validacionToken;

            var validacionPassword = Usuario.ValidarPassword(passwordNueva, repeticion);
            if (!validacionPassword.Exito) return validacionPassword;

            var recuperacion = ObtenerPorToken(token);
            UsuarioDAL.EstablecerPassword(recuperacion.IdUsuario, passwordNueva);
            MarcarUsado(recuperacion.IdRecuperacion);

            return ResultadoOperacion.Ok("Listo, ya podés ingresar con tu contraseña nueva.");
        }
    }
}
