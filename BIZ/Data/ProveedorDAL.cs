using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class ProveedorDAL
    {
        private const string SelectBase = @"
            SELECT idProveedor, razonSocial, cuit, telefono, email, direccion, activo
            FROM Proveedor";

        private static Proveedor Mapear(DataRow fila)
        {
            return new Proveedor
            {
                IdProveedor = AccesoDatos.LeerInt(fila, "idProveedor"),
                RazonSocial = AccesoDatos.LeerString(fila, "razonSocial"),
                Cuit = AccesoDatos.LeerString(fila, "cuit"),
                Telefono = AccesoDatos.LeerString(fila, "telefono"),
                Email = AccesoDatos.LeerString(fila, "email"),
                Direccion = AccesoDatos.LeerString(fila, "direccion"),
                Activo = AccesoDatos.LeerBool(fila, "activo")
            };
        }

        public static List<Proveedor> Listar(bool incluirInactivos = true)
        {
            var sql = SelectBase +
                      (incluirInactivos ? "" : " WHERE activo = 1") +
                      " ORDER BY razonSocial";

            var lista = new List<Proveedor>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static Proveedor ObtenerPorId(int idProveedor)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE idProveedor = @idProveedor",
                AccesoDatos.Param("@idProveedor", idProveedor));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        // Buscador rápido por razón social o CUIT. El CUIT se busca solo por sus dígitos,
        // así "20-12345678-6" y "20123456786" encuentran lo mismo.
        public static List<Proveedor> Buscar(string texto, bool incluirInactivos = false)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Listar(incluirInactivos);

            var soloDigitos = new string(texto.Where(char.IsDigit).ToArray());

            var condiciones = "razonSocial LIKE @texto";
            if (soloDigitos.Length > 0)
                condiciones += " OR cuit LIKE @textoCuit";

            var sql = SelectBase +
                      " WHERE (" + condiciones + ")" +
                      (incluirInactivos ? "" : " AND activo = 1") +
                      " ORDER BY razonSocial";

            var parametros = new List<SqlParameter> { AccesoDatos.Param("@texto", "%" + texto.Trim() + "%") };
            if (soloDigitos.Length > 0)
                parametros.Add(AccesoDatos.Param("@textoCuit", "%" + soloDigitos + "%"));

            var lista = new List<Proveedor>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql, parametros.ToArray()).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static bool ExisteCuit(string cuit, int idProveedorExcluido = 0)
        {
            var cantidad = AccesoDatos.Escalar(
                "SELECT COUNT(*) FROM Proveedor WHERE cuit = @cuit AND idProveedor <> @id",
                AccesoDatos.Param("@cuit", cuit),
                AccesoDatos.Param("@id", idProveedorExcluido));

            return System.Convert.ToInt32(cantidad) > 0;
        }

        private static int Insertar(Proveedor proveedor)
        {
            const string sql = @"
                INSERT INTO Proveedor (razonSocial, cuit, telefono, email, direccion, activo)
                VALUES (@razonSocial, @cuit, @telefono, @email, @direccion, @activo);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var id = AccesoDatos.Escalar(sql,
                AccesoDatos.Param("@razonSocial", proveedor.RazonSocial),
                AccesoDatos.Param("@cuit", proveedor.Cuit),
                AccesoDatos.Param("@telefono", proveedor.Telefono),
                AccesoDatos.Param("@email", proveedor.Email),
                AccesoDatos.Param("@direccion", proveedor.Direccion),
                AccesoDatos.Param("@activo", proveedor.Activo));

            return System.Convert.ToInt32(id);
        }

        public static ResultadoOperacion Crear(Proveedor proveedor)
        {
            var validacion = proveedor.Validar();
            if (!validacion.Exito) return validacion;

            if (ExisteCuit(proveedor.Cuit))
                return ResultadoOperacion.Error("Ya existe un proveedor con ese CUIT.");

            proveedor.IdProveedor = Insertar(proveedor);

            return ResultadoOperacion.Ok("Proveedor creado.");
        }

        public static ResultadoOperacion Actualizar(Proveedor proveedor)
        {
            var validacion = proveedor.Validar();
            if (!validacion.Exito) return validacion;

            if (ObtenerPorId(proveedor.IdProveedor) == null)
                return ResultadoOperacion.Error("El proveedor no existe.");

            if (ExisteCuit(proveedor.Cuit, proveedor.IdProveedor))
                return ResultadoOperacion.Error("Ya existe otro proveedor con ese CUIT.");

            const string sql = @"
                UPDATE Proveedor
                SET razonSocial = @razonSocial, cuit = @cuit, telefono = @telefono,
                    email = @email, direccion = @direccion, activo = @activo
                WHERE idProveedor = @idProveedor";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@razonSocial", proveedor.RazonSocial),
                AccesoDatos.Param("@cuit", proveedor.Cuit),
                AccesoDatos.Param("@telefono", proveedor.Telefono),
                AccesoDatos.Param("@email", proveedor.Email),
                AccesoDatos.Param("@direccion", proveedor.Direccion),
                AccesoDatos.Param("@activo", proveedor.Activo),
                AccesoDatos.Param("@idProveedor", proveedor.IdProveedor));

            return ResultadoOperacion.Ok("Proveedor actualizado.");
        }

        // Baja lógica: el proveedor puede estar referenciado por compras y su cuenta
        // corriente, así que nunca se borra físicamente.
        public static ResultadoOperacion Desactivar(int idProveedor)
        {
            var proveedor = ObtenerPorId(idProveedor);
            if (proveedor == null)
                return ResultadoOperacion.Error("El proveedor no existe.");

            if (!proveedor.Activo)
                return ResultadoOperacion.Ok("El proveedor ya estaba desactivado.");

            AccesoDatos.Ejecutar(
                "UPDATE Proveedor SET activo = 0 WHERE idProveedor = @idProveedor",
                AccesoDatos.Param("@idProveedor", idProveedor));

            return ResultadoOperacion.Ok("Proveedor desactivado.");
        }
    }
}
