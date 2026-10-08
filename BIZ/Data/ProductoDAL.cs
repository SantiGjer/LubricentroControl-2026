using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    // Productos: el supertipo Producto con sus dos subcategorías, Servicio e Insumo (Requerimientos
    // §9.9). Un alta escribe la fila de Producto y la de su subtipo juntas; el tipo queda fijo desde
    // ahí (las órdenes, compras, ventas y el kardex apuntan al subtipo, no se puede mover).
    public static class ProductoDAL
    {
        private const string SelectBase = @"
            SELECT p.idProducto, p.tipo, p.nombre, p.descripcion, p.sku, p.codigoBarras, p.precio,
                   p.tipoIva, p.alicuotaIva, p.activo,
                   i.marca, i.unidadMedida, i.stockActual, i.stockMinimo
            FROM Producto p
            LEFT JOIN Insumo i ON i.idInsumo = p.idProducto";

        private static Producto Mapear(DataRow fila)
        {
            return new Producto
            {
                IdProducto = AccesoDatos.LeerInt(fila, "idProducto"),
                Tipo = AccesoDatos.LeerString(fila, "tipo"),
                Nombre = AccesoDatos.LeerString(fila, "nombre"),
                Descripcion = AccesoDatos.LeerString(fila, "descripcion"),
                Sku = AccesoDatos.LeerString(fila, "sku"),
                CodigoBarras = AccesoDatos.LeerString(fila, "codigoBarras"),
                Precio = AccesoDatos.LeerDecimal(fila, "precio"),
                TipoIva = AccesoDatos.LeerString(fila, "tipoIva"),
                AlicuotaIva = AccesoDatos.LeerDecimal(fila, "alicuotaIva"),
                Activo = AccesoDatos.LeerBool(fila, "activo"),
                Marca = AccesoDatos.LeerString(fila, "marca"),
                UnidadMedida = AccesoDatos.LeerString(fila, "unidadMedida"),
                StockActual = AccesoDatos.LeerDecimal(fila, "stockActual"),
                StockMinimo = AccesoDatos.LeerDecimal(fila, "stockMinimo")
            };
        }

        // tipo null = las dos subcategorías.
        public static List<Producto> Listar(string tipo = null, bool incluirInactivos = true)
        {
            var sql = SelectBase + " WHERE 1 = 1" +
                      (tipo == null ? "" : " AND p.tipo = @tipo") +
                      (incluirInactivos ? "" : " AND p.activo = 1") +
                      " ORDER BY p.nombre";

            var lista = new List<Producto>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql, AccesoDatos.Param("@tipo", tipo)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static Producto ObtenerPorId(int idProducto)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE p.idProducto = @idProducto",
                AccesoDatos.Param("@idProducto", idProducto));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        // Insumos activos con el stock por debajo del mínimo — reporte de stock bajo e Inicio
        // (Requerimientos §6.9).
        public static List<Producto> ListarStockBajo()
        {
            const string sql = SelectBase + @"
                WHERE p.tipo = @tipo AND p.activo = 1 AND i.stockActual < i.stockMinimo
                ORDER BY p.nombre";

            var lista = new List<Producto>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql, AccesoDatos.Param("@tipo", Producto.TipoInsumo)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        private static bool ExisteCodigo(string columna, string valor, int idProductoExcluido)
        {
            if (valor == null) return false;

            // columna es una constante de esta clase (sku o codigoBarras), nunca un dato de entrada.
            var cantidad = AccesoDatos.Escalar(
                "SELECT COUNT(*) FROM Producto WHERE " + columna + " = @valor AND idProducto <> @id",
                AccesoDatos.Param("@valor", valor),
                AccesoDatos.Param("@id", idProductoExcluido));

            return System.Convert.ToInt32(cantidad) > 0;
        }

        // SKU y código de barras no se repiten entre productos (también lo exigen los índices
        // únicos filtrados de la base); acá se valida antes para dar un mensaje claro.
        private static ResultadoOperacion ValidarCodigosUnicos(Producto producto)
        {
            if (ExisteCodigo("sku", producto.Sku, producto.IdProducto))
                return ResultadoOperacion.Error("Ya existe otro producto con el SKU " + producto.Sku + ".");

            if (ExisteCodigo("codigoBarras", producto.CodigoBarras, producto.IdProducto))
                return ResultadoOperacion.Error("Ya existe otro producto con el código de barras " + producto.CodigoBarras + ".");

            return ResultadoOperacion.Ok();
        }

        // Inserta Producto y la fila de su subtipo en un solo batch atómico (mismo patrón
        // XACT_ABORT/BEGIN TRAN/COMMIT del resto de BIZ/Data): no puede quedar un producto sin
        // subcategoría. Un insumo nace con stock 0; si se pidió stock inicial, se registra después
        // como un ajuste manual, para que todo el stock quede respaldado por el kardex (invariante
        // stockActual == Σ(entrada - salida), Requerimientos §9.3).
        public static ResultadoOperacion Crear(Producto producto, int idUsuario)
        {
            var validacion = producto.Validar();
            if (!validacion.Exito) return validacion;

            var codigos = ValidarCodigosUnicos(producto);
            if (!codigos.Exito) return codigos;

            const string sql = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                INSERT INTO Producto (tipo, nombre, descripcion, sku, codigoBarras, precio, tipoIva, alicuotaIva, activo)
                VALUES (@tipo, @nombre, @descripcion, @sku, @codigoBarras, @precio, @tipoIva, @alicuotaIva, @activo);

                DECLARE @id INT = CAST(SCOPE_IDENTITY() AS INT);

                IF @tipo = 'Insumo'
                    INSERT INTO Insumo (idInsumo, marca, unidadMedida, stockActual, stockMinimo)
                    VALUES (@id, @marca, @unidadMedida, 0, @stockMinimo);
                ELSE
                    INSERT INTO Servicio (idServicio) VALUES (@id);

                COMMIT TRANSACTION;

                SELECT @id;";

            var id = AccesoDatos.Escalar(sql,
                AccesoDatos.Param("@tipo", producto.Tipo),
                AccesoDatos.Param("@nombre", producto.Nombre),
                AccesoDatos.Param("@descripcion", producto.Descripcion),
                AccesoDatos.Param("@sku", producto.Sku),
                AccesoDatos.Param("@codigoBarras", producto.CodigoBarras),
                AccesoDatos.Param("@precio", producto.Precio),
                AccesoDatos.Param("@tipoIva", producto.TipoIva),
                AccesoDatos.Param("@alicuotaIva", producto.AlicuotaIva),
                AccesoDatos.Param("@activo", producto.Activo),
                AccesoDatos.Param("@marca", producto.Marca),
                AccesoDatos.Param("@unidadMedida", producto.UnidadMedida),
                AccesoDatos.Param("@stockMinimo", producto.StockMinimo));

            producto.IdProducto = System.Convert.ToInt32(id);

            var stockInicial = producto.StockActual;
            producto.StockActual = 0;
            if (producto.EsInsumo && stockInicial > 0)
            {
                var ajuste = MovimientoStockDAL.RegistrarAjusteManual(
                    producto.IdProducto, stockInicial, true, "Alta de insumo — stock inicial", idUsuario);
                if (!ajuste.Exito) return ajuste;

                producto.StockActual = stockInicial;
            }

            return ResultadoOperacion.Ok(producto.EsInsumo ? "Insumo creado." : "Servicio creado.");
        }

        // No toca el tipo (queda fijo desde el alta) ni el stock (todo cambio de stock pasa por
        // MovimientoStockDAL).
        public static ResultadoOperacion Actualizar(Producto producto)
        {
            var existente = ObtenerPorId(producto.IdProducto);
            if (existente == null)
                return ResultadoOperacion.Error("El producto no existe.");

            producto.Tipo = existente.Tipo;
            producto.StockActual = existente.StockActual;

            var validacion = producto.Validar();
            if (!validacion.Exito) return validacion;

            var codigos = ValidarCodigosUnicos(producto);
            if (!codigos.Exito) return codigos;

            const string sql = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                UPDATE Producto
                SET nombre = @nombre, descripcion = @descripcion, sku = @sku, codigoBarras = @codigoBarras,
                    precio = @precio, tipoIva = @tipoIva, alicuotaIva = @alicuotaIva, activo = @activo
                WHERE idProducto = @idProducto;

                IF @tipo = 'Insumo'
                    UPDATE Insumo
                    SET marca = @marca, unidadMedida = @unidadMedida, stockMinimo = @stockMinimo
                    WHERE idInsumo = @idProducto;

                COMMIT TRANSACTION;";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@idProducto", producto.IdProducto),
                AccesoDatos.Param("@tipo", producto.Tipo),
                AccesoDatos.Param("@nombre", producto.Nombre),
                AccesoDatos.Param("@descripcion", producto.Descripcion),
                AccesoDatos.Param("@sku", producto.Sku),
                AccesoDatos.Param("@codigoBarras", producto.CodigoBarras),
                AccesoDatos.Param("@precio", producto.Precio),
                AccesoDatos.Param("@tipoIva", producto.TipoIva),
                AccesoDatos.Param("@alicuotaIva", producto.AlicuotaIva),
                AccesoDatos.Param("@activo", producto.Activo),
                AccesoDatos.Param("@marca", producto.Marca),
                AccesoDatos.Param("@unidadMedida", producto.UnidadMedida),
                AccesoDatos.Param("@stockMinimo", producto.StockMinimo));

            return ResultadoOperacion.Ok(producto.EsInsumo ? "Insumo actualizado." : "Servicio actualizado.");
        }

        // Baja lógica: el producto puede estar referenciado por órdenes, compras, ventas y el
        // kardex, así que nunca se borra físicamente.
        public static ResultadoOperacion Desactivar(int idProducto)
        {
            var producto = ObtenerPorId(idProducto);
            if (producto == null)
                return ResultadoOperacion.Error("El producto no existe.");

            if (!producto.Activo)
                return ResultadoOperacion.Ok("El producto ya estaba desactivado.");

            AccesoDatos.Ejecutar(
                "UPDATE Producto SET activo = 0 WHERE idProducto = @idProducto",
                AccesoDatos.Param("@idProducto", idProducto));

            return ResultadoOperacion.Ok("Producto desactivado.");
        }

        // Deshace la baja lógica. El stock de un insumo no se toca: quedó como estaba.
        public static ResultadoOperacion Reactivar(int idProducto)
        {
            var producto = ObtenerPorId(idProducto);
            if (producto == null)
                return ResultadoOperacion.Error("El producto no existe.");

            if (producto.Activo)
                return ResultadoOperacion.Ok("El producto ya estaba activo.");

            AccesoDatos.Ejecutar(
                "UPDATE Producto SET activo = 1 WHERE idProducto = @idProducto",
                AccesoDatos.Param("@idProducto", idProducto));

            return ResultadoOperacion.Ok("Producto reactivado.");
        }
    }
}
