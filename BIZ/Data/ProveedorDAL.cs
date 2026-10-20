using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class ProveedorDAL
    {
        private const string SelectBase = @"
            SELECT idProveedor, razonSocial, nombreFantasia, cuit, condicionIva, rubro, telefono, email,
                   contacto, telefonoContacto, direccion, localidad, provincia, codigoPostal, cbu, alias,
                   observaciones, activo
            FROM Proveedor";

        private static Proveedor Mapear(DataRow fila)
        {
            return new Proveedor
            {
                IdProveedor = AccesoDatos.LeerInt(fila, "idProveedor"),
                RazonSocial = AccesoDatos.LeerString(fila, "razonSocial"),
                NombreFantasia = AccesoDatos.LeerString(fila, "nombreFantasia"),
                Cuit = AccesoDatos.LeerString(fila, "cuit"),
                CondicionIva = AccesoDatos.LeerString(fila, "condicionIva"),
                Rubro = AccesoDatos.LeerString(fila, "rubro"),
                Telefono = AccesoDatos.LeerString(fila, "telefono"),
                Email = AccesoDatos.LeerString(fila, "email"),
                Contacto = AccesoDatos.LeerString(fila, "contacto"),
                TelefonoContacto = AccesoDatos.LeerString(fila, "telefonoContacto"),
                Direccion = AccesoDatos.LeerString(fila, "direccion"),
                Localidad = AccesoDatos.LeerString(fila, "localidad"),
                Provincia = AccesoDatos.LeerString(fila, "provincia"),
                CodigoPostal = AccesoDatos.LeerString(fila, "codigoPostal"),
                Cbu = AccesoDatos.LeerString(fila, "cbu"),
                Alias = AccesoDatos.LeerString(fila, "alias"),
                Observaciones = AccesoDatos.LeerString(fila, "observaciones"),
                Activo = AccesoDatos.LeerBool(fila, "activo")
            };
        }

        private static List<SqlParameter> ParametrosDe(Proveedor proveedor)
        {
            return new List<SqlParameter>
            {
                AccesoDatos.Param("@razonSocial", proveedor.RazonSocial),
                AccesoDatos.Param("@nombreFantasia", proveedor.NombreFantasia),
                AccesoDatos.Param("@cuit", proveedor.Cuit),
                AccesoDatos.Param("@condicionIva", proveedor.CondicionIva),
                AccesoDatos.Param("@rubro", proveedor.Rubro),
                AccesoDatos.Param("@telefono", proveedor.Telefono),
                AccesoDatos.Param("@email", proveedor.Email),
                AccesoDatos.Param("@contacto", proveedor.Contacto),
                AccesoDatos.Param("@telefonoContacto", proveedor.TelefonoContacto),
                AccesoDatos.Param("@direccion", proveedor.Direccion),
                AccesoDatos.Param("@localidad", proveedor.Localidad),
                AccesoDatos.Param("@provincia", proveedor.Provincia),
                AccesoDatos.Param("@codigoPostal", proveedor.CodigoPostal),
                AccesoDatos.Param("@cbu", proveedor.Cbu),
                AccesoDatos.Param("@alias", proveedor.Alias),
                AccesoDatos.Param("@observaciones", proveedor.Observaciones),
                AccesoDatos.Param("@activo", proveedor.Activo)
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
                INSERT INTO Proveedor (razonSocial, nombreFantasia, cuit, condicionIva, rubro, telefono, email,
                                       contacto, telefonoContacto, direccion, localidad, provincia, codigoPostal,
                                       cbu, alias, observaciones, activo)
                VALUES (@razonSocial, @nombreFantasia, @cuit, @condicionIva, @rubro, @telefono, @email,
                        @contacto, @telefonoContacto, @direccion, @localidad, @provincia, @codigoPostal,
                        @cbu, @alias, @observaciones, @activo);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var id = AccesoDatos.Escalar(sql, ParametrosDe(proveedor).ToArray());

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
                SET razonSocial = @razonSocial, nombreFantasia = @nombreFantasia, cuit = @cuit,
                    condicionIva = @condicionIva, rubro = @rubro, telefono = @telefono, email = @email,
                    contacto = @contacto, telefonoContacto = @telefonoContacto, direccion = @direccion,
                    localidad = @localidad, provincia = @provincia, codigoPostal = @codigoPostal,
                    cbu = @cbu, alias = @alias, observaciones = @observaciones, activo = @activo
                WHERE idProveedor = @idProveedor";

            var parametros = ParametrosDe(proveedor);
            parametros.Add(AccesoDatos.Param("@idProveedor", proveedor.IdProveedor));
            AccesoDatos.Ejecutar(sql, parametros.ToArray());

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

        // Deshace la baja lógica: el proveedor vuelve con sus compras y su cuenta corriente intactas.
        public static ResultadoOperacion Reactivar(int idProveedor)
        {
            var proveedor = ObtenerPorId(idProveedor);
            if (proveedor == null)
                return ResultadoOperacion.Error("El proveedor no existe.");

            if (proveedor.Activo)
                return ResultadoOperacion.Ok("El proveedor ya estaba activo.");

            AccesoDatos.Ejecutar(
                "UPDATE Proveedor SET activo = 1 WHERE idProveedor = @idProveedor",
                AccesoDatos.Param("@idProveedor", idProveedor));

            return ResultadoOperacion.Ok("Proveedor reactivado.");
        }
    }
}
