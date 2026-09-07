using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class InsumoDAL
    {
        private const string SelectBase = @"
            SELECT idInsumo, nombre, marca, unidadMedida, stockActual, stockMinimo, precioVenta, activo
            FROM Insumo";

        private static Insumo Mapear(DataRow fila)
        {
            return new Insumo
            {
                IdInsumo = AccesoDatos.LeerInt(fila, "idInsumo"),
                Nombre = AccesoDatos.LeerString(fila, "nombre"),
                Marca = AccesoDatos.LeerString(fila, "marca"),
                UnidadMedida = AccesoDatos.LeerString(fila, "unidadMedida"),
                StockActual = AccesoDatos.LeerDecimal(fila, "stockActual"),
                StockMinimo = AccesoDatos.LeerDecimal(fila, "stockMinimo"),
                PrecioVenta = AccesoDatos.LeerDecimal(fila, "precioVenta"),
                Activo = AccesoDatos.LeerBool(fila, "activo")
            };
        }

        public static List<Insumo> Listar(bool incluirInactivos = true)
        {
            var sql = SelectBase +
                      (incluirInactivos ? "" : " WHERE activo = 1") +
                      " ORDER BY nombre";

            var lista = new List<Insumo>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static Insumo ObtenerPorId(int idInsumo)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE idInsumo = @idInsumo",
                AccesoDatos.Param("@idInsumo", idInsumo));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        // Buscador rápido por nombre o marca.
        public static List<Insumo> Buscar(string texto, bool incluirInactivos = false)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Listar(incluirInactivos);

            var sql = SelectBase +
                      " WHERE (nombre LIKE @texto OR marca LIKE @texto)" +
                      (incluirInactivos ? "" : " AND activo = 1") +
                      " ORDER BY nombre";

            var lista = new List<Insumo>();
            foreach (DataRow fila in AccesoDatos.Consultar(
                sql, AccesoDatos.Param("@texto", "%" + texto.Trim() + "%")).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Insumos con stock por debajo del mínimo — referencia directa para el futuro
        // reporte "Stock bajo / a reponer" (Requerimientos §6.9).
        public static List<Insumo> ListarStockBajo()
        {
            const string sql = @"
                SELECT idInsumo, nombre, marca, unidadMedida, stockActual, stockMinimo, precioVenta, activo
                FROM Insumo
                WHERE activo = 1 AND stockActual < stockMinimo
                ORDER BY nombre";

            var lista = new List<Insumo>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        private static int Insertar(Insumo insumo)
        {
            const string sql = @"
                INSERT INTO Insumo (nombre, marca, unidadMedida, stockActual, stockMinimo, precioVenta, activo)
                VALUES (@nombre, @marca, @unidadMedida, 0, @stockMinimo, @precioVenta, @activo);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var id = AccesoDatos.Escalar(sql,
                AccesoDatos.Param("@nombre", insumo.Nombre),
                AccesoDatos.Param("@marca", insumo.Marca),
                AccesoDatos.Param("@unidadMedida", insumo.UnidadMedida),
                AccesoDatos.Param("@stockMinimo", insumo.StockMinimo),
                AccesoDatos.Param("@precioVenta", insumo.PrecioVenta),
                AccesoDatos.Param("@activo", insumo.Activo));

            return System.Convert.ToInt32(id);
        }

        // Se inserta siempre con stockActual = 0; si se pidió stock inicial (insumo.StockActual > 0),
        // se registra como un ajuste manual aparte, para que todo el stock quede respaldado por un
        // movimiento en el kardex desde el primer insumo (invariante: stockActual == Σ(entrada - salida)).
        public static ResultadoOperacion Crear(Insumo insumo, int idUsuario)
        {
            var validacion = insumo.Validar();
            if (!validacion.Exito) return validacion;

            var stockInicial = insumo.StockActual;
            insumo.IdInsumo = Insertar(insumo);
            insumo.StockActual = 0;

            if (stockInicial > 0)
            {
                var ajuste = MovimientoStockDAL.RegistrarAjusteManual(
                    insumo.IdInsumo, stockInicial, true, "Alta de insumo — stock inicial", idUsuario);
                if (!ajuste.Exito) return ajuste;

                insumo.StockActual = stockInicial;
            }

            return ResultadoOperacion.Ok("Insumo creado.");
        }

        // No incluye stockActual: todo cambio de stock pasa por MovimientoStockDAL.
        public static ResultadoOperacion Actualizar(Insumo insumo)
        {
            var validacion = insumo.Validar();
            if (!validacion.Exito) return validacion;

            if (ObtenerPorId(insumo.IdInsumo) == null)
                return ResultadoOperacion.Error("El insumo no existe.");

            const string sql = @"
                UPDATE Insumo
                SET nombre = @nombre, marca = @marca, unidadMedida = @unidadMedida,
                    stockMinimo = @stockMinimo, precioVenta = @precioVenta, activo = @activo
                WHERE idInsumo = @idInsumo";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@nombre", insumo.Nombre),
                AccesoDatos.Param("@marca", insumo.Marca),
                AccesoDatos.Param("@unidadMedida", insumo.UnidadMedida),
                AccesoDatos.Param("@stockMinimo", insumo.StockMinimo),
                AccesoDatos.Param("@precioVenta", insumo.PrecioVenta),
                AccesoDatos.Param("@activo", insumo.Activo),
                AccesoDatos.Param("@idInsumo", insumo.IdInsumo));

            return ResultadoOperacion.Ok("Insumo actualizado.");
        }

        // Baja lógica: el insumo puede estar referenciado por compras y órdenes de trabajo,
        // así que nunca se borra físicamente.
        public static ResultadoOperacion Desactivar(int idInsumo)
        {
            var insumo = ObtenerPorId(idInsumo);
            if (insumo == null)
                return ResultadoOperacion.Error("El insumo no existe.");

            if (!insumo.Activo)
                return ResultadoOperacion.Ok("El insumo ya estaba desactivado.");

            AccesoDatos.Ejecutar(
                "UPDATE Insumo SET activo = 0 WHERE idInsumo = @idInsumo",
                AccesoDatos.Param("@idInsumo", idInsumo));

            return ResultadoOperacion.Ok("Insumo desactivado.");
        }
    }
}
