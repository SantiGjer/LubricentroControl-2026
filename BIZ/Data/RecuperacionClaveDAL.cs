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

        /// <summary>
        /// Invalida los pedidos anteriores del usuario: al pedir un token nuevo,
        /// los viejos dejan de servir.
        /// </summary>
        public static void InvalidarPendientes(int idUsuario)
        {
            AccesoDatos.Ejecutar(
                "UPDATE RecuperacionClave SET usado = 1, fechaUso = @ahora WHERE idUsuario = @idUsuario AND usado = 0",
                AccesoDatos.Param("@ahora", DateTime.Now),
                AccesoDatos.Param("@idUsuario", idUsuario));
        }
    }
}
