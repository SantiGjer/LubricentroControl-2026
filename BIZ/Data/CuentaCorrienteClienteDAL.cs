using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class CuentaCorrienteClienteDAL
    {
        private const string SelectBase = @"
            SELECT m.idMovimiento, m.idCliente, c.nombre + ' ' + c.apellido AS nombreCliente,
                   m.fecha, m.tipoMovimiento, m.idVenta, m.idPago, m.debe, m.haber, m.saldo,
                   m.descripcion, m.idUsuario, u.nombre + ' ' + u.apellido AS nombreUsuario
            FROM CuentaCorrienteCliente m
            INNER JOIN Cliente c ON c.idCliente = m.idCliente
            LEFT JOIN Usuario u ON u.idUsuario = m.idUsuario";

        private static CuentaCorrienteCliente Mapear(DataRow fila)
        {
            return new CuentaCorrienteCliente
            {
                IdMovimiento = AccesoDatos.LeerInt(fila, "idMovimiento"),
                IdCliente = AccesoDatos.LeerInt(fila, "idCliente"),
                NombreCliente = AccesoDatos.LeerString(fila, "nombreCliente"),
                Fecha = AccesoDatos.LeerFecha(fila, "fecha"),
                TipoMovimiento = AccesoDatos.LeerString(fila, "tipoMovimiento"),
                IdVenta = AccesoDatos.LeerIntNullable(fila, "idVenta"),
                IdPago = AccesoDatos.LeerIntNullable(fila, "idPago"),
                Debe = AccesoDatos.LeerDecimal(fila, "debe"),
                Haber = AccesoDatos.LeerDecimal(fila, "haber"),
                Saldo = AccesoDatos.LeerDecimal(fila, "saldo"),
                Descripcion = AccesoDatos.LeerString(fila, "descripcion"),
                IdUsuario = AccesoDatos.LeerIntNullable(fila, "idUsuario"),
                NombreUsuario = AccesoDatos.LeerString(fila, "nombreUsuario")
            };
        }

        // Historial de un cliente, más reciente primero.
        public static List<CuentaCorrienteCliente> ListarPorCliente(int idCliente)
        {
            var lista = new List<CuentaCorrienteCliente>();
            foreach (DataRow fila in AccesoDatos.Consultar(
                SelectBase + " WHERE m.idCliente = @idCliente ORDER BY m.idMovimiento DESC",
                AccesoDatos.Param("@idCliente", idCliente)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Último saldo registrado, o 0 si el cliente todavía no tiene movimientos.
        public static decimal ObtenerSaldoActual(int idCliente)
        {
            var resultado = AccesoDatos.Escalar(
                "SELECT TOP 1 saldo FROM CuentaCorrienteCliente WHERE idCliente = @idCliente ORDER BY idMovimiento DESC",
                AccesoDatos.Param("@idCliente", idCliente));

            return resultado == null ? 0m : System.Convert.ToDecimal(resultado);
        }

        // Núcleo de escritura: una sola fila, con el saldo acumulado calculado por subquery en
        // el propio INSERT. Los casos que necesitan ir atómicamente junto con otra escritura (una
        // Venta generada al cerrar una orden, un Pago) arman su propio batch con este mismo texto
        // en vez de llamar a este método — mismo criterio que
        // CuentaCorrienteProveedorDAL.Registrar (ver CLAUDE.md, Historial de decisiones).
        private static void Registrar(int idCliente, string tipoMovimiento, decimal debe, decimal haber,
            int? idVenta, int? idPago, int? idUsuario, string descripcion)
        {
            const string sql = @"
                INSERT INTO CuentaCorrienteCliente
                    (idCliente, tipoMovimiento, idVenta, idPago, debe, haber, saldo, descripcion, idUsuario)
                VALUES (
                    @idCliente, @tipoMovimiento, @idVenta, @idPago, @debe, @haber,
                    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteCliente
                            WHERE idCliente = @idCliente ORDER BY idMovimiento DESC), 0) + @debe - @haber,
                    @descripcion, @idUsuario);";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@idCliente", idCliente),
                AccesoDatos.Param("@tipoMovimiento", tipoMovimiento),
                AccesoDatos.Param("@idVenta", idVenta),
                AccesoDatos.Param("@idPago", idPago),
                AccesoDatos.Param("@debe", debe),
                AccesoDatos.Param("@haber", haber),
                AccesoDatos.Param("@descripcion", descripcion),
                AccesoDatos.Param("@idUsuario", idUsuario));
        }

        // Ajuste manual: un solo campo con signo (positivo aumenta lo que el cliente debe,
        // negativo lo reduce) — mismo criterio que el ajuste de stock de Insumos y el de
        // CuentaCorrienteProveedor.
        public static ResultadoOperacion RegistrarAjuste(int idCliente, decimal monto, string motivo, int idUsuario)
        {
            if (monto == 0)
                return ResultadoOperacion.Error("El monto del ajuste no puede ser cero.");

            if (string.IsNullOrWhiteSpace(motivo))
                return ResultadoOperacion.Error("El motivo del ajuste es obligatorio.");

            if (ClienteDAL.ObtenerPorId(idCliente) == null)
                return ResultadoOperacion.Error("El cliente no existe.");

            Registrar(idCliente, CuentaCorrienteCliente.TipoAjuste,
                debe: monto > 0 ? monto : 0, haber: monto < 0 ? -monto : 0,
                idVenta: null, idPago: null, idUsuario: idUsuario, descripcion: motivo.Trim());

            return ResultadoOperacion.Ok("Ajuste registrado.");
        }
    }
}
