using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class MovimientoStockDAL
    {
        private const string SelectBase = @"
            SELECT m.idMovimiento, m.idInsumo, m.fecha, m.tipoMovimiento, m.idCompra, m.idOrden,
                   m.idUsuario, u.nombre + ' ' + u.apellido AS nombreUsuario,
                   m.entrada, m.salida, m.stockResultante, m.descripcion
            FROM MovimientoStock m
            INNER JOIN Usuario u ON u.idUsuario = m.idUsuario";

        private static MovimientoStock Mapear(DataRow fila)
        {
            return new MovimientoStock
            {
                IdMovimiento = AccesoDatos.LeerInt(fila, "idMovimiento"),
                IdInsumo = AccesoDatos.LeerInt(fila, "idInsumo"),
                Fecha = AccesoDatos.LeerFecha(fila, "fecha"),
                TipoMovimiento = AccesoDatos.LeerString(fila, "tipoMovimiento"),
                IdCompra = AccesoDatos.LeerIntNullable(fila, "idCompra"),
                IdOrden = AccesoDatos.LeerIntNullable(fila, "idOrden"),
                IdUsuario = AccesoDatos.LeerInt(fila, "idUsuario"),
                NombreUsuario = AccesoDatos.LeerString(fila, "nombreUsuario"),
                Entrada = AccesoDatos.LeerDecimal(fila, "entrada"),
                Salida = AccesoDatos.LeerDecimal(fila, "salida"),
                StockResultante = AccesoDatos.LeerDecimal(fila, "stockResultante"),
                Descripcion = AccesoDatos.LeerString(fila, "descripcion")
            };
        }

        // Kardex de un insumo, más reciente primero. La usa la grilla de historial de Insumos.aspx.
        public static List<MovimientoStock> ListarPorInsumo(int idInsumo)
        {
            var sql = SelectBase + " WHERE m.idInsumo = @idInsumo ORDER BY m.fecha DESC, m.idMovimiento DESC";

            var lista = new List<MovimientoStock>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql, AccesoDatos.Param("@idInsumo", idInsumo)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Núcleo atómico: valida stock suficiente y aplica el movimiento, dejando tanto el
        // ajuste de Insumo.stockActual como su registro en MovimientoStock en una sola
        // transacción. SET XACT_ABORT ON es necesario: BEGIN TRAN/COMMIT solos no revierten
        // el UPDATE si el INSERT falla a mitad de camino (ver CLAUDE.md).
        public static ResultadoOperacion Registrar(int idInsumo, string tipoMovimiento,
            decimal entrada, decimal salida, int? idCompra, int? idOrden, int idUsuario, string descripcion)
        {
            var insumo = InsumoDAL.ObtenerPorId(idInsumo);
            if (insumo == null)
                return ResultadoOperacion.Error("El insumo no existe.");

            var stockResultante = insumo.StockActual + entrada - salida;
            if (stockResultante < 0)
                return ResultadoOperacion.Error(
                    "Stock insuficiente. Disponible: " + insumo.StockActual + ", se intentó descontar " + salida + ".");

            const string sql = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                UPDATE Insumo SET stockActual = @stockResultante WHERE idInsumo = @idInsumo;

                INSERT INTO MovimientoStock
                    (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
                VALUES
                    (@idInsumo, @tipoMovimiento, @idCompra, @idOrden, @idUsuario, @entrada, @salida, @stockResultante, @descripcion);

                COMMIT TRANSACTION;";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@idInsumo", idInsumo),
                AccesoDatos.Param("@tipoMovimiento", tipoMovimiento),
                AccesoDatos.Param("@idCompra", idCompra),
                AccesoDatos.Param("@idOrden", idOrden),
                AccesoDatos.Param("@idUsuario", idUsuario),
                AccesoDatos.Param("@entrada", entrada),
                AccesoDatos.Param("@salida", salida),
                AccesoDatos.Param("@stockResultante", stockResultante),
                AccesoDatos.Param("@descripcion", descripcion));

            return ResultadoOperacion.Ok("Stock actualizado.");
        }

        // La usa el botón "Ajustar stock" de Insumos.aspx (y InsumoDAL.Crear, para el stock inicial).
        public static ResultadoOperacion RegistrarAjusteManual(int idInsumo, decimal cantidad,
            bool esEntrada, string motivo, int idUsuario)
        {
            if (cantidad <= 0)
                return ResultadoOperacion.Error("La cantidad a ajustar debe ser mayor a cero.");

            if (string.IsNullOrWhiteSpace(motivo))
                return ResultadoOperacion.Error("El motivo del ajuste es obligatorio.");

            return esEntrada
                ? Registrar(idInsumo, MovimientoStock.TipoAjusteManual, cantidad, 0, null, null, idUsuario, motivo.Trim())
                : Registrar(idInsumo, MovimientoStock.TipoAjusteManual, 0, cantidad, null, null, idUsuario, motivo.Trim());
        }
    }
}
