using System;

namespace BIZ.Modelo
{
    // IVA de los productos y condición frente al IVA de clientes y del comercio
    // (Docs/Lubricentro_Requerimientos.md §9.8 y §9.10). Los precios se cargan finales, con el
    // IVA incluido: el IVA de una línea es el que ya contiene su importe, no uno que se le suma.
    public static class Iva
    {
        // Tipo de IVA de un producto. Solo un producto gravado lleva alícuota.
        public const string Gravado = "Gravado";
        public const string Exento = "Exento";
        public const string NoGravado = "No gravado";

        public static readonly string[] Tipos = { Gravado, Exento, NoGravado };

        // Alícuotas vigentes, la general primero.
        public static readonly decimal[] Alicuotas = { 21m, 10.5m, 27m, 5m, 2.5m };

        // Condición frente al IVA (clientes y comercio). Define la letra de la factura.
        public const string ConsumidorFinal = "Consumidor Final";
        public const string ResponsableInscripto = "Responsable Inscripto";
        public const string Monotributista = "Monotributista";
        public const string SujetoExento = "Exento";

        public static readonly string[] Condiciones =
            { ConsumidorFinal, ResponsableInscripto, Monotributista, SujetoExento };

        public static bool EsTipoValido(string tipoIva)
        {
            return Array.IndexOf(Tipos, tipoIva) >= 0;
        }

        public static bool EsAlicuotaValida(decimal alicuota)
        {
            return Array.IndexOf(Alicuotas, alicuota) >= 0;
        }

        public static bool EsCondicionValida(string condicion)
        {
            return Array.IndexOf(Condiciones, condicion) >= 0;
        }

        // IVA contenido en un importe final. El neto se redondea a 2 decimales (hacia afuera,
        // igual que ROUND de SQL Server) y el IVA es la diferencia: así neto + IVA da siempre el
        // importe exacto.
        public static decimal Contenido(decimal importeConIva, string tipoIva, decimal alicuota)
        {
            if (tipoIva != Gravado || alicuota <= 0) return 0m;

            var neto = Math.Round(importeConIva / (1m + alicuota / 100m), 2, MidpointRounding.AwayFromZero);
            return importeConIva - neto;
        }

        // "21 %", "10,5 %", "Exento" o "No gravado", para mostrar.
        public static string Describir(string tipoIva, decimal alicuota)
        {
            return tipoIva == Gravado ? alicuota.ToString("0.##") + " %" : tipoIva;
        }
    }
}
