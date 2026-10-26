using System;
using System.Collections.Generic;
using System.Linq;

namespace BIZ.Modelo
{
    // Una consulta que el asistente sabe responder. El catálogo es CERRADO: la IA solo elige un
    // id de esta lista (y el período); nunca escribe SQL. Para sumar una consulta: método en
    // AsistenteDAL, entrada acá y un case en AsistenteDAL.Ejecutar.
    public class ConsultaAsistente
    {
        public const string TicketPromedio = "ticket_promedio";
        public const string MarcaModeloMasAtendido = "marca_modelo_mas_atendido";

        public string Id { get; private set; }
        public string Descripcion { get; private set; }
        public string[] Ejemplos { get; private set; }

        private ConsultaAsistente(string id, string descripcion, params string[] ejemplos)
        {
            Id = id;
            Descripcion = descripcion;
            Ejemplos = ejemplos;
        }

        private static readonly List<ConsultaAsistente> _catalogo = new List<ConsultaAsistente>
        {
            new ConsultaAsistente(TicketPromedio,
                "Ticket promedio: importe promedio de las ventas del período",
                "¿Cuál es el ticket promedio de este mes?",
                "¿Cuánto gasta en promedio un cliente por visita?",
                "Ticket promedio de septiembre"),
            new ConsultaAsistente(MarcaModeloMasAtendido,
                "Marca y modelo de vehículo más atendidos (top 5 por cantidad de órdenes de trabajo)",
                "¿Qué marca y modelo atendemos más?",
                "¿Qué auto viene más al lubricentro?",
                "Vehículos más atendidos del año")
        };

        public static IReadOnlyList<ConsultaAsistente> Catalogo
        {
            get { return _catalogo; }
        }

        // Lista blanca: devuelve null si el id no está en el catálogo.
        public static ConsultaAsistente Buscar(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return _catalogo.FirstOrDefault(c =>
                string.Equals(c.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
