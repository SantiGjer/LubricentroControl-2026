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

        // Kardex de un insumo, más reciente primero. La usa la grilla de historial de Productos.aspx.
        public static List<MovimientoStock> ListarPorInsumo(int idInsumo)
        {
            var sql = SelectBase + " WHERE m.idInsumo = @idInsumo ORDER BY m.fecha DESC, m.idMovimiento DESC";

            var lista = new List<MovimientoStock>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql, AccesoDatos.Param("@idInsumo", idInsumo)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Núcleo atómico: aplica el movimiento a Insumo.stockActual y lo registra en
        // MovimientoStock en una sola transacción. SET XACT_ABORT ON es necesario: BEGIN
        // TRAN/COMMIT solos no revierten el UPDATE si el INSERT falla a mitad de camino (ver
        // CLAUDE.md).
        //
        // El stock se actualiza en relativo (stockActual + entrada - salida) y la condición de
        // que no quede negativo va en el WHERE del mismo UPDATE (2026-10-18). Antes se leía el
        // stock, se calculaba el resultado en C# y se guardaba ese número: si dos movimientos
        // sobre el mismo insumo leían a la vez, el segundo pisaba al primero. Si el UPDATE no
        // toca ninguna fila es que el stock no alcanzaba, y no se registra nada. El stock
        // resultante del kardex se lee después del UPDATE, dentro de la transacción: la fila ya
        // está bloqueada por el UPDATE hasta el COMMIT.
        public static ResultadoOperacion Registrar(int idInsumo, string tipoMovimiento,
            decimal entrada, decimal salida, int? idCompra, int? idOrden, int idUsuario, string descripcion)
        {
            var insumo = ProductoDAL.ObtenerPorId(idInsumo);
            if (insumo == null || !insumo.EsInsumo)
                return ResultadoOperacion.Error("El insumo no existe.");

            const string sql = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                DECLARE @stockResultante DECIMAL(12,2) = NULL;

                UPDATE Insumo SET stockActual = stockActual + @entrada - @salida
                WHERE idInsumo = @idInsumo AND stockActual + @entrada - @salida >= 0;

                IF @@ROWCOUNT = 1
                BEGIN
                    SELECT @stockResultante = stockActual FROM Insumo WHERE idInsumo = @idInsumo;

                    INSERT INTO MovimientoStock
                        (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
                    VALUES
                        (@idInsumo, @tipoMovimiento, @idCompra, @idOrden, @idUsuario, @entrada, @salida, @stockResultante, @descripcion);
                END

                COMMIT TRANSACTION;

                SELECT @stockResultante;";

            var stockResultante = AccesoDatos.Escalar(sql,
                AccesoDatos.Param("@idInsumo", idInsumo),
                AccesoDatos.Param("@tipoMovimiento", tipoMovimiento),
                AccesoDatos.Param("@idCompra", idCompra),
                AccesoDatos.Param("@idOrden", idOrden),
                AccesoDatos.Param("@idUsuario", idUsuario),
                AccesoDatos.Param("@entrada", entrada),
                AccesoDatos.Param("@salida", salida),
                AccesoDatos.Param("@descripcion", descripcion));

            if (stockResultante == null)
                return StockInsuficiente(idInsumo, salida);

            return ResultadoOperacion.Ok("Stock actualizado.");
        }

        // Rechazo por stock insuficiente, con el stock de ese momento (se vuelve a leer: el
        // de antes del UPDATE pudo cambiar mientras tanto). Lo usa también
        // DetalleOrdenInsumoDAL.Agregar.
        internal static ResultadoOperacion StockInsuficiente(int idInsumo, decimal salida)
        {
            var insumo = ProductoDAL.ObtenerPorId(idInsumo);
            var disponible = insumo == null ? 0m : insumo.StockActual;

            return ResultadoOperacion.Error(
                "Stock insuficiente. Disponible: " + disponible + ", se intentó descontar " + salida + ".");
        }

        // La usa el botón "Ajustar stock" de Productos.aspx (y ProductoDAL.Crear, para el stock inicial).
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
