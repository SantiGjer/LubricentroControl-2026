using System;
using System.Data;
using System.Globalization;
using System.Linq;
using BIZ.Modelo;

namespace BIZ.Data
{
    // Asistente de consultas: la IA (ServicioIA) solo elige una consulta del catálogo cerrado y el
    // período; las consultas son las de abajo, parametrizadas. El texto de la respuesta lo arma C#,
    // para que ningún número pase por el modelo. Es de solo lectura: no escribe nada en la base.
    public static class AsistenteDAL
    {
        /* --- Consultas del catálogo --------------------------------------------
           desde y hasta son días completos: ambos se incluyen.                    */

        // Ticket promedio = importe promedio de las ventas del período, sin las anuladas con nota
        // de crédito (mismo criterio que sp_VentasPorPeriodo y Reportes/VentasPorPeriodo).
        public static void TicketPromedio(DateTime desde, DateTime hasta, out decimal promedio, out int cantidad)
        {
            var tabla = AccesoDatos.Consultar(
                @"SELECT AVG(v.total) AS promedio, COUNT(*) AS cantidad
                  FROM ComprobanteVenta v
                  LEFT JOIN NotaCredito nc ON nc.idVenta = v.idVenta
                  WHERE v.fecha >= @desde AND v.fecha < @hastaExclusiva
                    AND nc.idNotaCredito IS NULL",
                AccesoDatos.Param("@desde", desde.Date),
                AccesoDatos.Param("@hastaExclusiva", hasta.Date.AddDays(1)));

            var fila = tabla.Rows[0];
            promedio = AccesoDatos.LeerDecimal(fila, "promedio");
            cantidad = AccesoDatos.LeerInt(fila, "cantidad");
        }

        // Top 5 de marca y modelo por cantidad de órdenes (sin las canceladas). Marca y modelo son
        // texto libre: se normalizan para que "Ford" y "FORD " cuenten juntos.
        public static DataTable MarcaModeloMasAtendido(DateTime desde, DateTime hasta)
        {
            return AccesoDatos.Consultar(
                @"SELECT TOP 5 n.marca AS Marca, n.modelo AS Modelo, COUNT(*) AS Ordenes
                  FROM OrdenDeTrabajo o
                  JOIN Vehiculo v ON v.idVehiculo = o.idVehiculo
                  CROSS APPLY (SELECT
                      CASE WHEN LTRIM(RTRIM(ISNULL(v.marca, N''))) = N''
                           THEN N'Sin dato' ELSE UPPER(LTRIM(RTRIM(v.marca))) END AS marca,
                      CASE WHEN LTRIM(RTRIM(ISNULL(v.modelo, N''))) = N''
                           THEN N'Sin dato' ELSE UPPER(LTRIM(RTRIM(v.modelo))) END AS modelo) n
                  WHERE o.estado <> N'Cancelada'
                    AND o.fecha >= @desde AND o.fecha < @hastaExclusiva
                  GROUP BY n.marca, n.modelo
                  ORDER BY COUNT(*) DESC, n.marca, n.modelo",
                AccesoDatos.Param("@desde", desde.Date),
                AccesoDatos.Param("@hastaExclusiva", hasta.Date.AddDays(1)));
        }

        /* --- Despachador ------------------------------------------------------- */

        // Pregunta en lenguaje natural: la IA interpreta, C# valida y ejecuta.
        public static RespuestaAsistente Responder(string pregunta)
        {
            if (string.IsNullOrWhiteSpace(pregunta))
                return RespuestaAsistente.Error("Escribí una pregunta.");

            InterpretacionConsulta interpretacion;
            var interpretar = ServicioIA.Interpretar(pregunta.Trim(), out interpretacion);
            if (!interpretar.Exito)
                return RespuestaAsistente.Error(interpretar.Mensaje);

            // Lista blanca: lo que la IA diga que no esté en el catálogo ("ninguna", un id inventado) no se ejecuta.
            var consulta = ConsultaAsistente.Buscar(interpretacion.Consulta);
            if (consulta == null)
                return RespuestaAsistente.Ok(MensajeNoSe());

            DateTime desde, hasta;
            ResolverPeriodo(interpretacion.Desde, interpretacion.Hasta, out desde, out hasta);
            return Ejecutar(consulta, desde, hasta);
        }

        // Botones de ejemplo: ejecuta la consulta del mes actual sin pasar por la IA, así la pantalla
        // sigue respondiendo aunque Ollama esté cerrado.
        public static RespuestaAsistente EjecutarDirecto(string idConsulta)
        {
            var consulta = ConsultaAsistente.Buscar(idConsulta);
            if (consulta == null)
                return RespuestaAsistente.Ok(MensajeNoSe());

            DateTime desde, hasta;
            MesActual(out desde, out hasta);
            return Ejecutar(consulta, desde, hasta);
        }

        private static RespuestaAsistente Ejecutar(ConsultaAsistente consulta, DateTime desde, DateTime hasta)
        {
            var periodo = "del " + desde.ToString("dd/MM/yyyy") + " al " + hasta.ToString("dd/MM/yyyy");

            try
            {
                switch (consulta.Id)
                {
                    case ConsultaAsistente.TicketPromedio:
                    {
                        decimal promedio;
                        int cantidad;
                        TicketPromedio(desde, hasta, out promedio, out cantidad);
                        if (cantidad == 0)
                            return RespuestaAsistente.Ok("No hay ventas " + periodo + ", así que no se puede calcular el ticket promedio.");
                        return RespuestaAsistente.Ok("Ticket promedio " + periodo + ": $ " + promedio.ToString("N2") +
                            " (" + cantidad + (cantidad == 1 ? " venta" : " ventas") + ").");
                    }

                    case ConsultaAsistente.MarcaModeloMasAtendido:
                    {
                        var tabla = MarcaModeloMasAtendido(desde, hasta);
                        if (tabla.Rows.Count == 0)
                            return RespuestaAsistente.Ok("No hay órdenes de trabajo " + periodo + ".");
                        var primero = tabla.Rows[0];
                        var ordenes = AccesoDatos.LeerInt(primero, "Ordenes");
                        return RespuestaAsistente.Ok("El vehículo más atendido " + periodo + " es " +
                            AccesoDatos.LeerString(primero, "Marca") + " " + AccesoDatos.LeerString(primero, "Modelo") +
                            " (" + ordenes + (ordenes == 1 ? " orden" : " órdenes") + "). Top 5:", tabla);
                    }

                    default:
                        return RespuestaAsistente.Ok(MensajeNoSe());
                }
            }
            catch (Exception)
            {
                return RespuestaAsistente.Error("No se pudo consultar la base de datos. Probá de nuevo en un momento.");
            }
        }

        private static string MensajeNoSe()
        {
            return "Todavía no sé responder eso. Puedo responder: " +
                string.Join("; ", ConsultaAsistente.Catalogo.Select(c => c.Descripcion)) + ".";
        }

        // Fechas de la IA: si falta alguna o no es una fecha válida, se usa el mes actual; si vienen
        // al revés, se dan vuelta.
        private static void ResolverPeriodo(string textoDesde, string textoHasta, out DateTime desde, out DateTime hasta)
        {
            var validas = DateTime.TryParseExact(textoDesde, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                              DateTimeStyles.None, out desde)
                          & DateTime.TryParseExact(textoHasta, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                              DateTimeStyles.None, out hasta);
            if (!validas)
            {
                MesActual(out desde, out hasta);
                return;
            }

            if (desde > hasta)
            {
                var aux = desde;
                desde = hasta;
                hasta = aux;
            }
        }

        private static void MesActual(out DateTime desde, out DateTime hasta)
        {
            var hoy = DateTime.Today;
            desde = new DateTime(hoy.Year, hoy.Month, 1);
            hasta = desde.AddMonths(1).AddDays(-1);
        }
    }
}
