using System;

namespace BIZ.Modelo
{
    // Lo que tienen en común la factura y la nota de crédito para imprimirse con el mismo formato
    // (Ventas.aspx): letra, número, código de ARCA, fecha y los datos del comercio y del cliente del
    // momento en que se emitió. Las dos se generan desde una venta, cuyas líneas son las que se
    // imprimen.
    public interface IComprobanteImpreso
    {
        int IdVenta { get; }
        string Tipo { get; }
        DateTime Fecha { get; }
        string CondicionVenta { get; }
        string NumeroCompleto { get; }
        string CodigoComprobante { get; }
        bool DiscriminaIva { get; }
        bool InformaIvaContenido { get; }

        string EmisorRazonSocial { get; }
        string EmisorCuitFormateado { get; }
        string EmisorCondicionIva { get; }
        string EmisorDomicilio { get; }
        string EmisorIngresosBrutos { get; }
        string EmisorInicioActividades { get; }

        string ReceptorNombre { get; }
        string ReceptorDocumento { get; }
        string ReceptorCondicionIva { get; }
        string ReceptorDomicilio { get; }
    }
}
