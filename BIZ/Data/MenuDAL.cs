using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class MenuDAL
    {
        /// <summary>
        /// Devuelve, en plano, las opciones de menú visibles para un rol.
        /// El armado del árbol lo hace MenuNegocio.
        /// </summary>
        public static List<ItemMenu> ListarPorNivel(int idNivel)
        {
            const string sql = @"
                SELECT m.idMenu, m.texto, m.idUrl, u.path, m.idMenuPadre, m.orden, mn.soloLectura
                FROM Menu m
                INNER JOIN MenuNivel mn ON mn.idMenu = m.idMenu AND mn.idNivel = @idNivel
                LEFT JOIN Url u ON u.idUrl = m.idUrl
                WHERE m.activo = 1
                ORDER BY m.orden, m.texto";

            var lista = new List<ItemMenu>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql, AccesoDatos.Param("@idNivel", idNivel)).Rows)
            {
                lista.Add(new ItemMenu
                {
                    IdMenu = AccesoDatos.LeerInt(fila, "idMenu"),
                    Texto = AccesoDatos.LeerString(fila, "texto"),
                    IdUrl = AccesoDatos.LeerIntNullable(fila, "idUrl"),
                    Path = AccesoDatos.LeerString(fila, "path"),
                    IdMenuPadre = AccesoDatos.LeerIntNullable(fila, "idMenuPadre"),
                    Orden = AccesoDatos.LeerInt(fila, "orden"),
                    SoloLectura = AccesoDatos.LeerBool(fila, "soloLectura")
                });
            }
            return lista;
        }

        /// <summary>
        /// Permiso de un rol sobre una pantalla concreta. Devuelve null si el rol
        /// no tiene acceso — es lo que usa la guarda de PaginaSegura.
        /// </summary>
        public static ItemMenu ObtenerPermiso(int idNivel, string path)
        {
            const string sql = @"
                SELECT TOP 1 m.idMenu, m.texto, m.idUrl, u.path, m.idMenuPadre, m.orden, mn.soloLectura
                FROM Menu m
                INNER JOIN MenuNivel mn ON mn.idMenu = m.idMenu AND mn.idNivel = @idNivel
                INNER JOIN Url u ON u.idUrl = m.idUrl
                WHERE m.activo = 1 AND u.path = @path";

            var tabla = AccesoDatos.Consultar(sql,
                AccesoDatos.Param("@idNivel", idNivel),
                AccesoDatos.Param("@path", path));

            if (tabla.Rows.Count == 0) return null;

            var fila = tabla.Rows[0];
            return new ItemMenu
            {
                IdMenu = AccesoDatos.LeerInt(fila, "idMenu"),
                Texto = AccesoDatos.LeerString(fila, "texto"),
                IdUrl = AccesoDatos.LeerIntNullable(fila, "idUrl"),
                Path = AccesoDatos.LeerString(fila, "path"),
                IdMenuPadre = AccesoDatos.LeerIntNullable(fila, "idMenuPadre"),
                Orden = AccesoDatos.LeerInt(fila, "orden"),
                SoloLectura = AccesoDatos.LeerBool(fila, "soloLectura")
            };
        }

        /// <summary>True si la pantalla está registrada en Url (esté o no permitida para el rol).</summary>
        public static bool ExisteUrl(string path)
        {
            var cantidad = AccesoDatos.Escalar(
                "SELECT COUNT(*) FROM Url WHERE path = @path",
                AccesoDatos.Param("@path", path));

            return System.Convert.ToInt32(cantidad) > 0;
        }
    }
}
