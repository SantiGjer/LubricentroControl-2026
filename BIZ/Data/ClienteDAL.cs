using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class ClienteDAL
    {
        private const string SelectBase = @"
            SELECT idCliente, nombre, apellido, dni, telefono, email, direccion, cuentaCorriente,
                   activo, fechaAlta
            FROM Cliente";

        private static Cliente Mapear(DataRow fila)
        {
            return new Cliente
            {
                IdCliente = AccesoDatos.LeerInt(fila, "idCliente"),
                Nombre = AccesoDatos.LeerString(fila, "nombre"),
                Apellido = AccesoDatos.LeerString(fila, "apellido"),
                Dni = AccesoDatos.LeerString(fila, "dni"),
                Telefono = AccesoDatos.LeerString(fila, "telefono"),
                Email = AccesoDatos.LeerString(fila, "email"),
                Direccion = AccesoDatos.LeerString(fila, "direccion"),
                CuentaCorriente = AccesoDatos.LeerBool(fila, "cuentaCorriente"),
                Activo = AccesoDatos.LeerBool(fila, "activo"),
                FechaAlta = AccesoDatos.LeerFecha(fila, "fechaAlta")
            };
        }

        public static List<Cliente> Listar(bool incluirInactivos = true)
        {
            var sql = SelectBase +
                      (incluirInactivos ? "" : " WHERE activo = 1") +
                      " ORDER BY apellido, nombre";

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

        public static bool ExisteDni(string dni, int idClienteExcluido = 0)
        {
            var cantidad = AccesoDatos.Escalar(
                "SELECT COUNT(*) FROM Cliente WHERE dni = @dni AND idCliente <> @id",
                AccesoDatos.Param("@dni", dni),
                AccesoDatos.Param("@id", idClienteExcluido));

            return System.Convert.ToInt32(cantidad) > 0;
        }

        private static int Insertar(Cliente cliente)
        {
            const string sql = @"
                INSERT INTO Cliente (nombre, apellido, dni, telefono, email, direccion, cuentaCorriente, activo)
                VALUES (@nombre, @apellido, @dni, @telefono, @email, @direccion, @cuentaCorriente, @activo);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var id = AccesoDatos.Escalar(sql,
                AccesoDatos.Param("@nombre", cliente.Nombre),
                AccesoDatos.Param("@apellido", cliente.Apellido),
                AccesoDatos.Param("@dni", cliente.Dni),
                AccesoDatos.Param("@telefono", cliente.Telefono),
                AccesoDatos.Param("@email", cliente.Email),
                AccesoDatos.Param("@direccion", cliente.Direccion),
                AccesoDatos.Param("@cuentaCorriente", cliente.CuentaCorriente),
                AccesoDatos.Param("@activo", cliente.Activo));

            return System.Convert.ToInt32(id);
        }

        public static ResultadoOperacion Crear(Cliente cliente)
        {
            var validacion = cliente.Validar();
            if (!validacion.Exito) return validacion;

            if (ExisteDni(cliente.Dni))
                return ResultadoOperacion.Error("Ya existe un cliente con ese DNI.");

            cliente.IdCliente = Insertar(cliente);

            return ResultadoOperacion.Ok("Cliente creado.");
        }

        public static ResultadoOperacion Actualizar(Cliente cliente)
        {
            var validacion = cliente.Validar();
            if (!validacion.Exito) return validacion;

            if (ObtenerPorId(cliente.IdCliente) == null)
                return ResultadoOperacion.Error("El cliente no existe.");

            if (ExisteDni(cliente.Dni, cliente.IdCliente))
                return ResultadoOperacion.Error("Ya existe otro cliente con ese DNI.");

            const string sql = @"
                UPDATE Cliente
                SET nombre = @nombre, apellido = @apellido, dni = @dni, telefono = @telefono,
                    email = @email, direccion = @direccion, cuentaCorriente = @cuentaCorriente,
                    activo = @activo
                WHERE idCliente = @idCliente";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@nombre", cliente.Nombre),
                AccesoDatos.Param("@apellido", cliente.Apellido),
                AccesoDatos.Param("@dni", cliente.Dni),
                AccesoDatos.Param("@telefono", cliente.Telefono),
                AccesoDatos.Param("@email", cliente.Email),
                AccesoDatos.Param("@direccion", cliente.Direccion),
                AccesoDatos.Param("@cuentaCorriente", cliente.CuentaCorriente),
                AccesoDatos.Param("@activo", cliente.Activo),
                AccesoDatos.Param("@idCliente", cliente.IdCliente));

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
