using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class MenuDAL
    {
        // Devuelve, en plano, las opciones de menú visibles para un rol.
        // El armado del árbol lo hace ItemMenu.ArmarArbol.
        public static List<ItemMenu> ListarPorNivel(int idNivel)
        {
            const string sql = @"
                SELECT m.idMenu, m.texto, m.idUrl, u.path, m.idMenuPadre, m.orden, m.icono, mn.soloLectura
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
                    Icono = AccesoDatos.LeerString(fila, "icono"),
                    SoloLectura = AccesoDatos.LeerBool(fila, "soloLectura")
                });
            }
            return lista;
        }

        // Menú de un rol, ya armado como árbol y listo para renderizar.
        public static List<ItemMenu> ObtenerArbol(int idNivel)
        {
            return ItemMenu.ArmarArbol(ListarPorNivel(idNivel));
        }

        // Permiso de un rol sobre una pantalla concreta. Devuelve null si el rol
        // no tiene acceso — es lo que usa la guarda de PaginaSegura.
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

        // Todas las pantallas del menú (las hojas, con el texto de su grupo) y el acceso que tiene
        // un rol a cada una: sin fila en MenuNivel = sin acceso; soloLectura = consulta; si no,
        // completo. Es la matriz de la pantalla de Roles; con idNivel 0 (rol nuevo) todo queda
        // sin acceso. En el mismo orden que el menú.
        public static List<PermisoPantalla> ListarPermisos(int idNivel)
        {
            const string sql = @"
                SELECT m.idMenu, ISNULL(g.texto, '') AS grupo, m.texto AS pantalla, u.path,
                       mn.idNivel AS conAcceso, mn.soloLectura
                FROM Menu m
                INNER JOIN Url u ON u.idUrl = m.idUrl
                LEFT JOIN Menu g ON g.idMenu = m.idMenuPadre
                LEFT JOIN MenuNivel mn ON mn.idMenu = m.idMenu AND mn.idNivel = @idNivel
                WHERE m.activo = 1
                ORDER BY ISNULL(g.orden, m.orden), m.orden";

            var lista = new List<PermisoPantalla>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql, AccesoDatos.Param("@idNivel", idNivel)).Rows)
            {
                var acceso = fila.IsNull("conAcceso")
                    ? PermisoPantalla.SinAcceso
                    : AccesoDatos.LeerBool(fila, "soloLectura") ? PermisoPantalla.Consulta : PermisoPantalla.Completo;

                lista.Add(new PermisoPantalla
                {
                    IdMenu = AccesoDatos.LeerInt(fila, "idMenu"),
                    Grupo = AccesoDatos.LeerString(fila, "grupo"),
                    Pantalla = AccesoDatos.LeerString(fila, "pantalla"),
                    Path = AccesoDatos.LeerString(fila, "path"),
                    Acceso = acceso
                });
            }
            return lista;
        }

        // True si la pantalla está registrada en Url (esté o no permitida para el rol).
        public static bool ExisteUrl(string path)
        {
            var cantidad = AccesoDatos.Escalar(
                "SELECT COUNT(*) FROM Url WHERE path = @path",
                AccesoDatos.Param("@path", path));

            return System.Convert.ToInt32(cantidad) > 0;
        }
    }
}
