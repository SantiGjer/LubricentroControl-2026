using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class ClienteDAL
    {
        private const string SelectBase = @"
            SELECT idCliente, tipoCliente, nombre, apellido, razonSocial, tipoDocumento, numeroDocumento,
                   condicionIva, telefono, email, direccion, localidad, provincia, codigoPostal,
                   cuentaCorriente, activo, fechaAlta
            FROM Cliente";

        private static Cliente Mapear(DataRow fila)
        {
            return new Cliente
            {
                IdCliente = AccesoDatos.LeerInt(fila, "idCliente"),
                TipoCliente = AccesoDatos.LeerString(fila, "tipoCliente"),
                Nombre = AccesoDatos.LeerString(fila, "nombre"),
                Apellido = AccesoDatos.LeerString(fila, "apellido"),
                RazonSocial = AccesoDatos.LeerString(fila, "razonSocial"),
                TipoDocumento = AccesoDatos.LeerString(fila, "tipoDocumento"),
                NumeroDocumento = AccesoDatos.LeerString(fila, "numeroDocumento"),
                CondicionIva = AccesoDatos.LeerString(fila, "condicionIva"),
                Telefono = AccesoDatos.LeerString(fila, "telefono"),
                Email = AccesoDatos.LeerString(fila, "email"),
                Direccion = AccesoDatos.LeerString(fila, "direccion"),
                Localidad = AccesoDatos.LeerString(fila, "localidad"),
                Provincia = AccesoDatos.LeerString(fila, "provincia"),
                CodigoPostal = AccesoDatos.LeerString(fila, "codigoPostal"),
                CuentaCorriente = AccesoDatos.LeerBool(fila, "cuentaCorriente"),
                Activo = AccesoDatos.LeerBool(fila, "activo"),
                FechaAlta = AccesoDatos.LeerFecha(fila, "fechaAlta")
            };
        }

        public static List<Cliente> Listar(bool incluirInactivos = true)
        {
            var sql = SelectBase +
                      (incluirInactivos ? "" : " WHERE activo = 1") +
                      " ORDER BY denominacion";

            var lista = new List<Cliente>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static Cliente ObtenerPorId(int idCliente)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE idCliente = @idCliente",
                AccesoDatos.Param("@idCliente", idCliente));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        // El mismo número con otro tipo no choca (un DNI y un pasaporte pueden coincidir): la
        // unicidad es del par, igual que UQ_Cliente_documento.
        public static bool ExisteDocumento(string tipoDocumento, string numeroDocumento, int idClienteExcluido = 0)
        {
            var cantidad = AccesoDatos.Escalar(
                @"SELECT COUNT(*) FROM Cliente
                  WHERE tipoDocumento = @tipo AND numeroDocumento = @numero AND idCliente <> @id",
                AccesoDatos.Param("@tipo", tipoDocumento),
                AccesoDatos.Param("@numero", numeroDocumento),
                AccesoDatos.Param("@id", idClienteExcluido));

            return System.Convert.ToInt32(cantidad) > 0;
        }

        private static List<SqlParameter> ParametrosDe(Cliente cliente)
        {
            return new List<SqlParameter>
            {
                AccesoDatos.Param("@tipoCliente", cliente.TipoCliente),
                AccesoDatos.Param("@nombre", cliente.Nombre),
                AccesoDatos.Param("@apellido", cliente.Apellido),
                AccesoDatos.Param("@razonSocial", cliente.RazonSocial),
                AccesoDatos.Param("@tipoDocumento", cliente.TipoDocumento),
                AccesoDatos.Param("@numeroDocumento", cliente.NumeroDocumento),
                AccesoDatos.Param("@condicionIva", cliente.CondicionIva),
                AccesoDatos.Param("@telefono", cliente.Telefono),
                AccesoDatos.Param("@email", cliente.Email),
                AccesoDatos.Param("@direccion", cliente.Direccion),
                AccesoDatos.Param("@localidad", cliente.Localidad),
                AccesoDatos.Param("@provincia", cliente.Provincia),
                AccesoDatos.Param("@codigoPostal", cliente.CodigoPostal),
                AccesoDatos.Param("@cuentaCorriente", cliente.CuentaCorriente),
                AccesoDatos.Param("@activo", cliente.Activo)
            };
        }

        private static int Insertar(Cliente cliente)
        {
            const string sql = @"
                INSERT INTO Cliente (tipoCliente, nombre, apellido, razonSocial, tipoDocumento, numeroDocumento,
                                     condicionIva, telefono, email, direccion, localidad, provincia, codigoPostal,
                                     cuentaCorriente, activo)
                VALUES (@tipoCliente, @nombre, @apellido, @razonSocial, @tipoDocumento, @numeroDocumento,
                        @condicionIva, @telefono, @email, @direccion, @localidad, @provincia, @codigoPostal,
                        @cuentaCorriente, @activo);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var id = AccesoDatos.Escalar(sql, ParametrosDe(cliente).ToArray());
            return System.Convert.ToInt32(id);
        }

        public static ResultadoOperacion Crear(Cliente cliente)
        {
            var validacion = cliente.Validar();
            if (!validacion.Exito) return validacion;

            if (ExisteDocumento(cliente.TipoDocumento, cliente.NumeroDocumento))
                return ResultadoOperacion.Error("Ya existe un cliente con ese " + cliente.TipoDocumento + ".");

            cliente.IdCliente = Insertar(cliente);

            return ResultadoOperacion.Ok("Cliente creado.");
        }

        public static ResultadoOperacion Actualizar(Cliente cliente)
        {
            var validacion = cliente.Validar();
            if (!validacion.Exito) return validacion;

            if (ObtenerPorId(cliente.IdCliente) == null)
                return ResultadoOperacion.Error("El cliente no existe.");

            if (ExisteDocumento(cliente.TipoDocumento, cliente.NumeroDocumento, cliente.IdCliente))
                return ResultadoOperacion.Error("Ya existe otro cliente con ese " + cliente.TipoDocumento + ".");

            const string sql = @"
                UPDATE Cliente
                SET tipoCliente = @tipoCliente, nombre = @nombre, apellido = @apellido, razonSocial = @razonSocial,
                    tipoDocumento = @tipoDocumento, numeroDocumento = @numeroDocumento, condicionIva = @condicionIva,
                    telefono = @telefono, email = @email, direccion = @direccion, localidad = @localidad,
                    provincia = @provincia, codigoPostal = @codigoPostal, cuentaCorriente = @cuentaCorriente,
                    activo = @activo
                WHERE idCliente = @idCliente";

            var parametros = ParametrosDe(cliente);
            parametros.Add(AccesoDatos.Param("@idCliente", cliente.IdCliente));
            AccesoDatos.Ejecutar(sql, parametros.ToArray());

            return ResultadoOperacion.Ok("Cliente actualizado.");
        }

        // Baja lógica: el cliente puede estar referenciado por vehículos, órdenes, ventas
        // y su cuenta corriente, así que nunca se borra físicamente.
        public static ResultadoOperacion Desactivar(int idCliente)
        {
            var cliente = ObtenerPorId(idCliente);
            if (cliente == null)
                return ResultadoOperacion.Error("El cliente no existe.");

            if (!cliente.Activo)
                return ResultadoOperacion.Ok("El cliente ya estaba desactivado.");

            AccesoDatos.Ejecutar(
                "UPDATE Cliente SET activo = 0 WHERE idCliente = @idCliente",
                AccesoDatos.Param("@idCliente", idCliente));

            return ResultadoOperacion.Ok("Cliente desactivado.");
        }

        // Deshace la baja lógica: el cliente vuelve tal cual estaba, con su historial intacto.
        public static ResultadoOperacion Reactivar(int idCliente)
        {
            var cliente = ObtenerPorId(idCliente);
            if (cliente == null)
                return ResultadoOperacion.Error("El cliente no existe.");

            if (cliente.Activo)
                return ResultadoOperacion.Ok("El cliente ya estaba activo.");

            AccesoDatos.Ejecutar(
                "UPDATE Cliente SET activo = 1 WHERE idCliente = @idCliente",
                AccesoDatos.Param("@idCliente", idCliente));

            return ResultadoOperacion.Ok("Cliente reactivado.");
        }
    }
}
