using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class CuentaCorrienteProveedorDAL
    {
        private const string SelectBase = @"
            SELECT m.idMovimiento, m.idProveedor, p.razonSocial, m.fecha, m.tipoMovimiento,
                   m.idCompra, m.idPago, m.debe, m.haber, m.saldo, m.descripcion, m.idUsuario,
                   u.nombre + ' ' + u.apellido AS nombreUsuario
            FROM CuentaCorrienteProveedor m
            INNER JOIN Proveedor p ON p.idProveedor = m.idProveedor
            LEFT JOIN Usuario u ON u.idUsuario = m.idUsuario";

        private static CuentaCorrienteProveedor Mapear(DataRow fila)
        {
            return new CuentaCorrienteProveedor
            {
                IdMovimiento = AccesoDatos.LeerInt(fila, "idMovimiento"),
                IdProveedor = AccesoDatos.LeerInt(fila, "idProveedor"),
                RazonSocial = AccesoDatos.LeerString(fila, "razonSocial"),
                Fecha = AccesoDatos.LeerFecha(fila, "fecha"),
                TipoMovimiento = AccesoDatos.LeerString(fila, "tipoMovimiento"),
                IdCompra = AccesoDatos.LeerIntNullable(fila, "idCompra"),
                IdPago = AccesoDatos.LeerIntNullable(fila, "idPago"),
                Debe = AccesoDatos.LeerDecimal(fila, "debe"),
                Haber = AccesoDatos.LeerDecimal(fila, "haber"),
                Saldo = AccesoDatos.LeerDecimal(fila, "saldo"),
                Descripcion = AccesoDatos.LeerString(fila, "descripcion"),
                IdUsuario = AccesoDatos.LeerIntNullable(fila, "idUsuario"),
                NombreUsuario = AccesoDatos.LeerString(fila, "nombreUsuario")
            };
        }

        // Historial de un proveedor, más reciente primero.
        public static List<CuentaCorrienteProveedor> ListarPorProveedor(int idProveedor)
        {
            var lista = new List<CuentaCorrienteProveedor>();
            foreach (DataRow fila in AccesoDatos.Consultar(
                SelectBase + " WHERE m.idProveedor = @idProveedor ORDER BY m.idMovimiento DESC",
                AccesoDatos.Param("@idProveedor", idProveedor)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Último saldo registrado, o 0 si el proveedor todavía no tiene movimientos.
        public static decimal ObtenerSaldoActual(int idProveedor)
        {
            var resultado = AccesoDatos.Escalar(
                "SELECT TOP 1 saldo FROM CuentaCorrienteProveedor WHERE idProveedor = @idProveedor ORDER BY idMovimiento DESC",
                AccesoDatos.Param("@idProveedor", idProveedor));

            return resultado == null ? 0m : System.Convert.ToDecimal(resultado);
        }

        // Núcleo de escritura: una sola fila, con el saldo acumulado calculado por subquery en
        // el propio INSERT (no hace falta SELECT + INSERT por separado: es un único statement,
        // atómico por sí solo). Los casos que necesitan ir atómicamente junto con otra escritura
        // (una Compra a cuenta corriente, un Pago) arman su propio batch con este mismo texto en
        // vez de llamar a este método — mismo criterio que DetalleOrdenInsumoDAL.Agregar con
        // MovimientoStockDAL.Registrar (ver CLAUDE.md, Historial de decisiones).
        private static void Registrar(int idProveedor, string tipoMovimiento, decimal debe, decimal haber,
            int? idCompra, int? idPago, int? idUsuario, string descripcion)
        {
            const string sql = @"
                INSERT INTO CuentaCorrienteProveedor
                    (idProveedor, tipoMovimiento, idCompra, idPago, debe, haber, saldo, descripcion, idUsuario)
                VALUES (
                    @idProveedor, @tipoMovimiento, @idCompra, @idPago, @debe, @haber,
                    ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteProveedor
                            WHERE idProveedor = @idProveedor ORDER BY idMovimiento DESC), 0) + @debe - @haber,
                    @descripcion, @idUsuario);";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@idProveedor", idProveedor),
                AccesoDatos.Param("@tipoMovimiento", tipoMovimiento),
                AccesoDatos.Param("@idCompra", idCompra),
                AccesoDatos.Param("@idPago", idPago),
                AccesoDatos.Param("@debe", debe),
                AccesoDatos.Param("@haber", haber),
                AccesoDatos.Param("@descripcion", descripcion),
                AccesoDatos.Param("@idUsuario", idUsuario));
        }

        // Ajuste manual: un solo campo con signo (positivo aumenta la deuda con el proveedor,
        // negativo la reduce) — mismo criterio ya validado en el ajuste de stock de Insumos, sin
        // radio Entrada/Salida separado.
        public static ResultadoOperacion RegistrarAjuste(int idProveedor, decimal monto, string motivo, int idUsuario)
        {
            if (monto == 0)
                return ResultadoOperacion.Error("El monto del ajuste no puede ser cero.");

            if (string.IsNullOrWhiteSpace(motivo))
                return ResultadoOperacion.Error("El motivo del ajuste es obligatorio.");

            if (ProveedorDAL.ObtenerPorId(idProveedor) == null)
                return ResultadoOperacion.Error("El proveedor no existe.");

            Registrar(idProveedor, CuentaCorrienteProveedor.TipoAjuste,
                debe: monto > 0 ? monto : 0, haber: monto < 0 ? -monto : 0,
                idCompra: null, idPago: null, idUsuario: idUsuario, descripcion: motivo.Trim());

            return ResultadoOperacion.Ok("Ajuste registrado.");
        }
    }
}
