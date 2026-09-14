using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class ComprobanteCompraDAL
    {
        private const string SelectBase = @"
            SELECT c.idCompra, c.idProveedor, p.razonSocial, c.numeroComprobante, c.fecha,
                   c.condicionPago, c.medioPago, c.subtotal, c.impuestos, c.total, c.saldoPendiente
            FROM ComprobanteCompra c
            INNER JOIN Proveedor p ON p.idProveedor = c.idProveedor";

        private static ComprobanteCompra Mapear(DataRow fila)
        {
            return new ComprobanteCompra
            {
                IdCompra = AccesoDatos.LeerInt(fila, "idCompra"),
                IdProveedor = AccesoDatos.LeerInt(fila, "idProveedor"),
                RazonSocial = AccesoDatos.LeerString(fila, "razonSocial"),
                NumeroComprobante = AccesoDatos.LeerString(fila, "numeroComprobante"),
                Fecha = AccesoDatos.LeerFecha(fila, "fecha"),
                CondicionPago = AccesoDatos.LeerString(fila, "condicionPago"),
                MedioPago = AccesoDatos.LeerString(fila, "medioPago"),
                Subtotal = AccesoDatos.LeerDecimal(fila, "subtotal"),
                Impuestos = AccesoDatos.LeerDecimal(fila, "impuestos"),
                Total = AccesoDatos.LeerDecimal(fila, "total"),
                SaldoPendiente = AccesoDatos.LeerDecimal(fila, "saldoPendiente")
            };
        }

        public static List<ComprobanteCompra> Listar(bool soloConSaldoPendiente = false)
        {
            var sql = SelectBase +
                      (soloConSaldoPendiente ? " WHERE c.saldoPendiente > 0" : "") +
                      " ORDER BY c.fecha DESC";

            var lista = new List<ComprobanteCompra>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        public static ComprobanteCompra ObtenerPorId(int idCompra)
        {
            var tabla = AccesoDatos.Consultar(
                SelectBase + " WHERE c.idCompra = @idCompra",
                AccesoDatos.Param("@idCompra", idCompra));

            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        // Compras de un proveedor con saldo pendiente — para el desplegable "comprobante a
        // pagar" de Pagos.aspx.
        public static List<ComprobanteCompra> ListarPendientesPorProveedor(int idProveedor)
        {
            var lista = new List<ComprobanteCompra>();
            foreach (DataRow fila in AccesoDatos.Consultar(
                SelectBase + " WHERE c.idProveedor = @idProveedor AND c.saldoPendiente > 0 ORDER BY c.fecha",
                AccesoDatos.Param("@idProveedor", idProveedor)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Razón social, CUIT (sin importar guiones) o número de comprobante.
        public static List<ComprobanteCompra> Buscar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Listar();

            const string sql = SelectBase + @"
                WHERE p.razonSocial LIKE @texto OR p.cuit LIKE @textoCuit OR c.numeroComprobante LIKE @texto
                ORDER BY c.fecha DESC";

            var lista = new List<ComprobanteCompra>();
            var comodin = "%" + texto.Trim() + "%";
            var comodinCuit = "%" + texto.Trim().Replace("-", "") + "%";
            foreach (DataRow fila in AccesoDatos.Consultar(sql,
                AccesoDatos.Param("@texto", comodin),
                AccesoDatos.Param("@textoCuit", comodinCuit)).Rows)
                lista.Add(Mapear(fila));
            return lista;
        }

        // Todo en un solo batch atómico: la cabecera, cada línea (que además suma stock y deja
        // su fila en el kardex), y si es a cuenta corriente, el movimiento en
        // CuentaCorrienteProveedor — mismo mecanismo XACT_ABORT/BEGIN TRAN/COMMIT que
        // MovimientoStockDAL.Registrar y DetalleOrdenInsumoDAL.Agregar, extendido a N líneas.
        // No hay franja de alta progresiva como en Órdenes: una compra es la transcripción de
        // una factura que ya llega completa, así que las líneas se arman en memoria en la
        // pantalla (ViewState) y se guardan todas juntas acá.
        public static ResultadoOperacion Crear(ComprobanteCompra compra, List<DetalleCompra> lineas, int idUsuario)
        {
            var validacion = compra.Validar();
            if (!validacion.Exito) return validacion;

            if (lineas == null || lineas.Count == 0)
                return ResultadoOperacion.Error("Agregá al menos una línea a la compra.");

            var proveedor = ProveedorDAL.ObtenerPorId(compra.IdProveedor);
            if (proveedor == null)
                return ResultadoOperacion.Error("El proveedor no existe.");
            if (!proveedor.Activo)
                return ResultadoOperacion.Error("El proveedor está dado de baja.");

            foreach (var linea in lineas)
            {
                if (linea.Cantidad <= 0)
                    return ResultadoOperacion.Error("La cantidad de cada línea debe ser mayor a cero.");
                if (linea.PrecioUnitario < 0)
                    return ResultadoOperacion.Error("El precio unitario no puede ser negativo.");

                var insumo = InsumoDAL.ObtenerPorId(linea.IdInsumo);
                if (insumo == null)
                    return ResultadoOperacion.Error("Uno de los insumos de la compra no existe.");
                if (!insumo.Activo)
                    return ResultadoOperacion.Error("Uno de los insumos de la compra está dado de baja.");
            }

            compra.Subtotal = 0;
            foreach (var linea in lineas)
                compra.Subtotal += linea.Cantidad * linea.PrecioUnitario;
            compra.Total = compra.Subtotal + compra.Impuestos;
            compra.SaldoPendiente = compra.CondicionPago == ComprobanteCompra.CondicionCuentaCorriente
                ? compra.Total : 0;

            var sql = new StringBuilder();
            var parametros = new List<SqlParameter>();

            sql.Append(@"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                INSERT INTO ComprobanteCompra
                    (idProveedor, numeroComprobante, condicionPago, medioPago, subtotal, impuestos, total, saldoPendiente)
                VALUES
                    (@idProveedor, '(pendiente)', @condicionPago, @medioPago, @subtotal, @impuestos, @total, @saldoPendiente);

                DECLARE @idCompra INT = CAST(SCOPE_IDENTITY() AS INT);
            ");

            parametros.Add(AccesoDatos.Param("@idProveedor", compra.IdProveedor));
            parametros.Add(AccesoDatos.Param("@condicionPago", compra.CondicionPago));
            parametros.Add(AccesoDatos.Param("@medioPago", compra.MedioPago));
            parametros.Add(AccesoDatos.Param("@subtotal", compra.Subtotal));
            parametros.Add(AccesoDatos.Param("@impuestos", compra.Impuestos));
            parametros.Add(AccesoDatos.Param("@total", compra.Total));
            parametros.Add(AccesoDatos.Param("@saldoPendiente", compra.SaldoPendiente));

            for (var i = 0; i < lineas.Count; i++)
            {
                var linea = lineas[i];
                var suf = i.ToString();

                sql.Append(@"
                    UPDATE Insumo SET stockActual = stockActual + @cantidad" + suf + @" WHERE idInsumo = @idInsumo" + suf + @";

                    INSERT INTO MovimientoStock
                        (idInsumo, tipoMovimiento, idCompra, idOrden, idUsuario, entrada, salida, stockResultante, descripcion)
                    VALUES (
                        @idInsumo" + suf + @", @tipoMovimientoStock, @idCompra, NULL, @idUsuario, @cantidad" + suf + @", 0,
                        (SELECT stockActual FROM Insumo WHERE idInsumo = @idInsumo" + suf + @"),
                        'Compra #' + CAST(@idCompra AS VARCHAR(10)));

                    INSERT INTO DetalleCompra (idCompra, idInsumo, cantidad, precioUnitario)
                    VALUES (@idCompra, @idInsumo" + suf + @", @cantidad" + suf + @", @precio" + suf + @");
                ");

                parametros.Add(AccesoDatos.Param("@idInsumo" + suf, linea.IdInsumo));
                parametros.Add(AccesoDatos.Param("@cantidad" + suf, linea.Cantidad));
                parametros.Add(AccesoDatos.Param("@precio" + suf, linea.PrecioUnitario));
            }

            parametros.Add(AccesoDatos.Param("@tipoMovimientoStock", MovimientoStock.TipoCompra));
            parametros.Add(AccesoDatos.Param("@idUsuario", idUsuario));

            if (compra.CondicionPago == ComprobanteCompra.CondicionCuentaCorriente)
            {
                sql.Append(@"
                    INSERT INTO CuentaCorrienteProveedor
                        (idProveedor, tipoMovimiento, idCompra, idPago, debe, haber, saldo, descripcion, idUsuario)
                    VALUES (
                        @idProveedor, @tipoMovimientoCC, @idCompra, NULL, @total, 0,
                        ISNULL((SELECT TOP 1 saldo FROM CuentaCorrienteProveedor
                                WHERE idProveedor = @idProveedor ORDER BY idMovimiento DESC), 0) + @total,
                        'Compra #' + CAST(@idCompra AS VARCHAR(10)), NULL);
                ");
                parametros.Add(AccesoDatos.Param("@tipoMovimientoCC", CuentaCorrienteProveedor.TipoCompra));
            }

            sql.Append(@"
                UPDATE ComprobanteCompra
                SET numeroComprobante = 'C-' + RIGHT('000000' + CAST(@idCompra AS VARCHAR(10)), 6)
                WHERE idCompra = @idCompra;

                COMMIT TRANSACTION;

                SELECT @idCompra;
            ");

            var id = AccesoDatos.Escalar(sql.ToString(), parametros.ToArray());
            compra.IdCompra = System.Convert.ToInt32(id);

            return ResultadoOperacion.Ok("Compra registrada.");
        }
    }
}
