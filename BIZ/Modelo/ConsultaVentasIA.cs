using System;
using System.Collections.Generic;
using System.Linq;

namespace BIZ.Modelo
{
    // Una consulta que sabe responder el asistente de ventas y stock (Reportes/AsistenteVentas,
    // Requerimientos §9.21). El catálogo es CERRADO: la IA solo elige un id de esta lista, el período
    // y, si la consulta lo usa, el filtro (un producto, un cliente, un cajero o el tipo de ítem que
    // nombra la pregunta); nunca escribe SQL. Para sumar una consulta: entrada acá, método en
    // AsistenteVentasDAL y un case en AsistenteVentasDAL.Ejecutar.
    public class ConsultaVentasIA
    {
        public const string MasVendidos = "mas_vendidos";
        public const string Deudores = "deudores";
        public const string StockBajo = "stock_bajo";
        public const string StockInsumo = "stock_insumo";
        public const string Reposicion = "reposicion";
        public const string VentasPorCajero = "ventas_por_cajero";
        public const string ResumenVentas = "resumen_ventas";

        public string Id { get; private set; }
        public string Descripcion { get; private set; }

        // Qué tiene que poner la IA en "filtro" para esta consulta; null si la consulta no usa filtro.
        public string Filtro { get; private set; }

        // true = la consulta no depende del período (la deuda y el stock son los de hoy): la IA deja las
        // fechas vacías. Escribe la mitad, y en una PC sin placa de video eso es la mitad del tiempo.
        public bool SinPeriodo { get; private set; }

        public string[] Ejemplos { get; private set; }

        private ConsultaVentasIA(string id, string descripcion, string filtro, bool sinPeriodo, params string[] ejemplos)
        {
            Id = id;
            Descripcion = descripcion;
            Filtro = filtro;
            SinPeriodo = sinPeriodo;
            Ejemplos = ejemplos;
        }

        // El primer ejemplo de cada consulta es el texto de su botón en la pantalla.
        private static readonly List<ConsultaVentasIA> _catalogo = new List<ConsultaVentasIA>
        {
            new ConsultaVentasIA(MasVendidos,
                "Ranking de los productos y servicios más vendidos en el período (por cantidad vendida)",
                "\"servicio\" si pregunta por servicios, \"insumo\" si pregunta por insumos o productos, " +
                "o el nombre de un producto (por ejemplo \"aceite\"); si no, vacío", false,
                "¿Qué es lo que más vendemos este mes?",
                "¿Qué servicio hacemos más?",
                "¿Cuáles fueron los insumos más vendidos en septiembre?",
                "Ranking de lo más vendido del año"),
            new ConsultaVentasIA(Deudores,
                "Clientes que más deben en su cuenta corriente, o lo que debe un cliente",
                "el nombre del cliente si la pregunta nombra uno; si no, vacío", true,
                "¿Qué cliente me debe más?",
                "¿Quiénes me deben plata?",
                "¿Cuánto debe Juan Pérez?"),
            new ConsultaVentasIA(StockBajo,
                "Lista de los insumos que están por debajo del stock mínimo (sin nombrar un insumo en particular)",
                null, true,
                "¿Qué insumos tengo que reponer?",
                "¿Qué está por debajo del mínimo?",
                "¿Qué me falta en el depósito?"),
            new ConsultaVentasIA(StockInsumo,
                "Cuánto stock queda de un insumo que nombra la pregunta, o de todos (cantidad, mínimo y último movimiento)",
                "el nombre del insumo si la pregunta nombra uno; si no, vacío", true,
                "¿Cuánto stock tengo de cada insumo?",
                "¿Cuánto aceite 15W40 me queda?",
                "¿Tenemos lámparas H4?",
                "¿Quedan filtros de aire?"),
            new ConsultaVentasIA(Reposicion,
                "Qué conviene comprar y para cuántos días alcanza cada insumo, según lo consumido en los últimos 30 días",
                null, true,
                "¿Qué tengo que comprar?",
                "¿Para cuántos días me alcanza el stock?",
                "¿Qué insumo se me va a terminar primero?"),
            new ConsultaVentasIA(VentasPorCajero,
                "Ventas facturadas por cada cajero (el usuario que emitió la factura) en el período, o por un cajero",
                "el nombre del cajero si la pregunta nombra uno; si no, vacío", false,
                "¿Cuánto facturó cada cajero este mes?",
                "¿Cuánto facturó Juan Empleado este mes?",
                "¿Qué cajero vendió más en septiembre?"),
            new ConsultaVentasIA(ResumenVentas,
                "Resumen de ventas del período: total vendido, anulado con notas de crédito, neto, cobrado y lo que falta cobrar",
                null, false,
                "Resumen de ventas de este mes",
                "¿Cuánto vendimos en septiembre?",
                "¿Cuánto se anuló con notas de crédito este mes?",
                "¿Cuánto nos queda por cobrar de las ventas del mes?")
        };

        public static IReadOnlyList<ConsultaVentasIA> Catalogo
        {
            get { return _catalogo; }
        }

        // Lista blanca: devuelve null si el id no está en el catálogo.
        public static ConsultaVentasIA Buscar(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return _catalogo.FirstOrDefault(c => string.Equals(c.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
