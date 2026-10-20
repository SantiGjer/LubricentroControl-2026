using System;

namespace BIZ.Modelo
{
    // Nota de crédito de una venta (Docs/Lubricentro_Requerimientos.md §9.17): anula la venta
    // completa, una por venta. Lleva la letra de la factura que anula (o la que le correspondería
    // si la venta no se facturó) y numera aparte de las facturas, por letra y punto de venta. Como
    // la factura, es sin validez fiscal y guarda los datos del comercio y del cliente del momento en
    // que se emitió; los importes son los de la venta.
    public class NotaCredito : IComprobanteImpreso
    {
        public int IdNotaCredito { get; set; }
        public int IdVenta { get; set; }
        public int? IdFactura { get; set; }
        public string Tipo { get; set; }
        public int PuntoVenta { get; set; }
        public int Numero { get; set; }
        public DateTime Fecha { get; set; }
        public int IdUsuario { get; set; }
        public string Motivo { get; set; }
        public string CondicionVenta { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuestos { get; set; }
        public decimal Total { get; set; }

        public string EmisorRazonSocial { get; set; }
        public string EmisorCuit { get; set; }
        public string EmisorCondicionIva { get; set; }
        public string EmisorDomicilio { get; set; }
        public string EmisorIngresosBrutos { get; set; }
        public string EmisorInicioActividades { get; set; }

        public string ReceptorNombre { get; set; }
        public string ReceptorTipoDocumento { get; set; }
        public string ReceptorNumeroDocumento { get; set; }
        public string ReceptorCondicionIva { get; set; }
        public string ReceptorDomicilio { get; set; }

        // Del JOIN con Factura: "A 00001-00000003" si anula una factura; null si la venta no se
        // había facturado. Solo para mostrar.
        public string NumeroFactura { get; set; }

        // "00001-00000012": punto de venta de 5 dígitos y número de 8.
        public string NumeroCompleto
        {
            get { return PuntoVenta.ToString("00000") + "-" + Numero.ToString("00000000"); }
        }

        // Código de comprobante de ARCA: 03 nota de crédito A, 08 nota de crédito B, 13 nota de crédito C.
        public string CodigoComprobante
        {
            get { return Tipo == Factura.TipoA ? "03" : Tipo == Factura.TipoB ? "08" : "13"; }
        }

        // Como en la factura: la A discrimina el IVA y la B informa el IVA contenido (Ley 27.743).
        public bool DiscriminaIva
        {
            get { return Tipo == Factura.TipoA; }
        }

        public bool InformaIvaContenido
        {
            get { return Tipo == Factura.TipoB; }
        }

        public string EmisorCuitFormateado
        {
            get { return Proveedor.FormatearCuit(EmisorCuit); }
        }

        public string ReceptorDocumento
        {
            get { return Cliente.FormatearDocumento(ReceptorTipoDocumento, ReceptorNumeroDocumento); }
        }

        // "NC A 00001-00000012".
        public static string FormatearNumero(string tipo, int puntoVenta, int numero)
        {
            return "NC " + Factura.FormatearNumero(tipo, puntoVenta, numero);
        }
    }
}
