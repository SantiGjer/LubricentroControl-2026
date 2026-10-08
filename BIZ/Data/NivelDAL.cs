using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using BIZ.Modelo;

namespace BIZ.Data
{
    // Roles y sus permisos por pantalla (Requerimientos §9.11). Los permisos viven en MenuNivel:
    // una fila por pantalla (y por grupo del menú) que el rol ve, con soloLectura para "Consulta".
    public static class NivelDAL
    {
        private const string SelectBase = @"
            SELECT n.idNivel, n.nombre, n.jerarquia,
                   (SELECT COUNT(*) FROM Usuario u WHERE u.idNivel = n.idNivel) AS cantidadUsuarios,
                   (SELECT COUNT(*) FROM MenuNivel mn
                    INNER JOIN Menu m ON m.idMenu = mn.idMenu
                    WHERE mn.idNivel = n.idNivel AND m.idUrl IS NOT NULL) AS cantidadPantallas
            FROM Nivel n";

        private static Nivel Mapear(DataRow fila)
        {
            return new Nivel
            {
                IdNivel = AccesoDatos.LeerInt(fila, "idNivel"),
                Nombre = AccesoDatos.LeerString(fila, "nombre"),
                Jerarquia = AccesoDatos.LeerInt(fila, "jerarquia"),
                CantidadUsuarios = AccesoDatos.LeerInt(fila, "cantidadUsuarios"),
                CantidadPantallas = AccesoDatos.LeerInt(fila, "cantidadPantallas")
            };
        }

        public static List<Nivel> Listar()
        {
            var lista = new List<Nivel>();
            foreach (DataRow fila in AccesoDatos.Consultar(SelectBase + " ORDER BY n.jerarquia, n.nombre").Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static Nivel ObtenerPorId(int idNivel)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE n.idNivel = @idNivel",
                AccesoDatos.Param("@idNivel", idNivel));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        public static bool ExisteNombre(string nombre, int idNivelExcluido = 0)
        {
            var cantidad = AccesoDatos.Escalar(
                "SELECT COUNT(*) FROM Nivel WHERE nombre = @nombre AND idNivel <> @id",
                AccesoDatos.Param("@nombre", nombre),
                AccesoDatos.Param("@id", idNivelExcluido));

            return System.Convert.ToInt32(cantidad) > 0;
        }

        // Alta o edición de un rol junto con todos sus permisos, en un solo batch atómico (mismo
        // patrón XACT_ABORT/BEGIN TRAN/COMMIT del resto de BIZ/Data): se borran las filas de
        // MenuNivel del rol y se vuelven a insertar las pantallas con acceso, más los grupos del
        // menú que las contienen (sin el grupo, la pantalla no aparece en el menú — ver
        // ItemMenu.ArmarArbol). Inicio queda siempre con acceso: es adonde lleva el login.
        // El rol Admin no se edita (Nivel.Admin).
        public static ResultadoOperacion Guardar(Nivel nivel, List<PermisoPantalla> permisos)
        {
            if (nivel.IdNivel == Nivel.Admin)
                return ResultadoOperacion.Error("El rol Admin tiene siempre acceso completo a todo: no se edita.");

            var validacion = nivel.Validar();
            if (!validacion.Exito) return validacion;

            if (nivel.IdNivel > 0 && ObtenerPorId(nivel.IdNivel) == null)
                return ResultadoOperacion.Error("El rol no existe.");

            if (ExisteNombre(nivel.Nombre, nivel.IdNivel))
                return ResultadoOperacion.Error("Ya existe un rol con ese nombre.");

            // Solo se aceptan pantallas que existen en el menú, con un acceso conocido.
            var pantallas = new Dictionary<int, PermisoPantalla>();
            foreach (var pantalla in MenuDAL.ListarPermisos(0))
                pantallas[pantalla.IdMenu] = pantalla;

            var conAcceso = new Dictionary<int, bool>(); // idMenu → soloLectura
            foreach (var permiso in permisos ?? new List<PermisoPantalla>())
            {
                PermisoPantalla pantalla;
                if (!pantallas.TryGetValue(permiso.IdMenu, out pantalla))
                    return ResultadoOperacion.Error("Una de las pantallas no existe.");
                if (System.Array.IndexOf(PermisoPantalla.Accesos, permiso.Acceso) < 0)
                    return ResultadoOperacion.Error("El acceso a " + pantalla.Pantalla + " no es válido.");

                if (permiso.Acceso != PermisoPantalla.SinAcceso)
                    conAcceso[permiso.IdMenu] = permiso.Acceso == PermisoPantalla.Consulta;
            }

            foreach (var pantalla in pantallas.Values)
                if (pantalla.EsInicio) conAcceso[pantalla.IdMenu] = false;

            var sql = new StringBuilder(@"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                DECLARE @id INT = @idNivel;

                IF @id = 0
                BEGIN
                    INSERT INTO Nivel (nombre, jerarquia)
                    VALUES (@nombre, ISNULL((SELECT MAX(jerarquia) FROM Nivel), 0) + 1);
                    SET @id = CAST(SCOPE_IDENTITY() AS INT);
                END
                ELSE
                    UPDATE Nivel SET nombre = @nombre WHERE idNivel = @id;

                DELETE FROM MenuNivel WHERE idNivel = @id;
            ");

            var parametros = new List<SqlParameter>
            {
                AccesoDatos.Param("@idNivel", nivel.IdNivel),
                AccesoDatos.Param("@nombre", nivel.Nombre)
            };

            var i = 0;
            foreach (var par in conAcceso)
            {
                var suf = (i++).ToString();
                sql.Append("INSERT INTO MenuNivel (idMenu, idNivel, soloLectura) VALUES (@menu" + suf +
                           ", @id, @soloLectura" + suf + ");\n");
                parametros.Add(AccesoDatos.Param("@menu" + suf, par.Key));
                parametros.Add(AccesoDatos.Param("@soloLectura" + suf, par.Value));
            }

            sql.Append(@"
                INSERT INTO MenuNivel (idMenu, idNivel, soloLectura)
                SELECT DISTINCT m.idMenuPadre, @id, 0
                FROM Menu m
                INNER JOIN MenuNivel mn ON mn.idMenu = m.idMenu AND mn.idNivel = @id
                WHERE m.idMenuPadre IS NOT NULL;

                COMMIT TRANSACTION;

                SELECT @id;");

            var esAlta = nivel.IdNivel == 0;
            nivel.IdNivel = System.Convert.ToInt32(AccesoDatos.Escalar(sql.ToString(), parametros.ToArray()));

            return ResultadoOperacion.Ok(esAlta ? "Rol creado." : "Rol actualizado.");
        }

        // Solo se borra un rol sin usuarios (Usuario.idNivel lo referencia), y nunca Admin ni
        // Lectura, que el código usa por id.
        public static ResultadoOperacion Borrar(int idNivel)
        {
            var nivel = ObtenerPorId(idNivel);
            if (nivel == null)
                return ResultadoOperacion.Error("El rol no existe.");

            if (nivel.IdNivel == Nivel.Admin)
                return ResultadoOperacion.Error("El rol Admin no se puede borrar.");

            if (nivel.IdNivel == Nivel.Lectura)
                return ResultadoOperacion.Error(
                    "El rol " + nivel.Nombre + " no se puede borrar: es el que reciben las cuentas creadas desde el registro público.");

            if (nivel.CantidadUsuarios > 0)
                return ResultadoOperacion.Error("El rol " + nivel.Nombre + " tiene " +
                    (nivel.CantidadUsuarios == 1 ? "un usuario" : nivel.CantidadUsuarios + " usuarios") +
                    " (activos o no): asignales otro rol antes de borrarlo.");

            AccesoDatos.Ejecutar(@"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;
                DELETE FROM MenuNivel WHERE idNivel = @idNivel;
                DELETE FROM Nivel WHERE idNivel = @idNivel;
                COMMIT TRANSACTION;",
                AccesoDatos.Param("@idNivel", idNivel));

            return ResultadoOperacion.Ok("Rol borrado.");
        }
    }
}
