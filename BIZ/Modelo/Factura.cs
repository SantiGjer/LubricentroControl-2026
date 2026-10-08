using System;
using System.Collections.Generic;

namespace BIZ.Modelo
{
    // Factura de una venta (Docs/Lubricentro_Requerimientos.md §9.10). Se genera a pedido desde
    // Ventas y es sin validez fiscal: no hay CAE de ARCA (la facturación electrónica real sigue
    // fuera de alcance, §10). Guarda los datos del comercio y del cliente de cuando se emitió; las
    // líneas y los importes son los de la venta, que no cambia.
    public class Factura
    {
        public const string TipoA = "A";
        public const string TipoB = "B";
        public const string TipoC = "C";

        public const string CondicionVentaContado = "Contado";
        public const string CondicionVentaCuentaCorriente = "Cuenta corriente";

        public int IdFactura { get; set; }
        public int IdVenta { get; set; }
        public string Tipo { get; set; }
        public int PuntoVenta { get; set; }
        public int Numero { get; set; }
        public DateTime Fecha { get; set; }
        public int IdUsuario { get; set; }
        public string CondicionVenta { get; set; }

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

        // "00001-00000012": punto de venta de 5 dígitos y número de 8.
        public string NumeroCompleto
        {
            get { return PuntoVenta.ToString("00000") + "-" + Numero.ToString("00000000"); }
        }

        // Código de comprobante de ARCA: 01 factura A, 06 factura B, 11 factura C.
        public string CodigoComprobante
        {
            get { return Tipo == TipoA ? "01" : Tipo == TipoB ? "06" : "11"; }
        }

        // La A discrimina el IVA (neto e IVA por alícuota); la B muestra precios finales con el IVA
        // contenido informado aparte (Ley 27.743); la C, emitida por un monotributista o un exento,
        // no lleva IVA.
        public bool DiscriminaIva
        {
            get { return Tipo == TipoA; }
        }

        public bool InformaIvaContenido
        {
            get { return Tipo == TipoB; }
        }

        public string EmisorCuitFormateado
        {
            get { return Proveedor.FormatearCuit(EmisorCuit); }
        }

        public string ReceptorDocumento
        {
            get { return Cliente.FormatearDocumento(ReceptorTipoDocumento, ReceptorNumeroDocumento); }
        }

        // "A 00001-00000012".
        public static string FormatearNumero(string tipo, int puntoVenta, int numero)
        {
            return tipo + " " + puntoVenta.ToString("00000") + "-" + numero.ToString("00000000");
        }

        // Letra según quién emite y quién recibe: un responsable inscripto le factura A a otro
        // responsable inscripto y a un monotributista, y B al resto; un monotributista o un exento
        // emite siempre C.
        public static string DeterminarTipo(string condicionEmisor, string condicionReceptor)
        {
            if (condicionEmisor != Iva.ResponsableInscripto) return TipoC;

            return condicionReceptor == Iva.ResponsableInscripto || condicionReceptor == Iva.Monotributista
                ? TipoA
                : TipoB;
        }
    }

    // Totales de una factura armados desde las líneas de la venta: neto gravado, IVA por alícuota,
    // exento y no gravado. Total = suma de los subtotales finales de las líneas.
    public class TotalesFactura
    {
        public decimal NetoGravado { get; private set; }
        public decimal Exento { get; private set; }
        public decimal NoGravado { get; private set; }
        public decimal IvaTotal { get; private set; }
        public decimal Total { get; private set; }

        // Alícuota → IVA, de la mayor a la menor.
        public SortedDictionary<decimal, decimal> IvaPorAlicuota { get; private set; }

        public static TotalesFactura Calcular(IEnumerable<DetalleComprobanteVenta> lineas)
        {
            var totales = new TotalesFactura
            {
                IvaPorAlicuota = new SortedDictionary<decimal, decimal>(
                    Comparer<decimal>.Create((a, b) => b.CompareTo(a)))
            };

            foreach (var linea in lineas)
            {
                totales.Total += linea.Subtotal;

                if (linea.TipoIva == Iva.Exento)
                    totales.Exento += linea.Subtotal;
                else if (linea.TipoIva == Iva.NoGravado)
                    totales.NoGravado += linea.Subtotal;
                else
                {
                    totales.NetoGravado += linea.Neto;
                    totales.IvaTotal += linea.ImporteIva;

                    decimal acumulado;
                    totales.IvaPorAlicuota.TryGetValue(linea.AlicuotaIva, out acumulado);
                    totales.IvaPorAlicuota[linea.AlicuotaIva] = acumulado + linea.ImporteIva;
                }
            }

            return totales;
        }
    }

    // Datos del comercio que emite las facturas (salen de Web.config, ver FacturaDAL.LeerEmisor).
    public class DatosEmisor
    {
        public string RazonSocial { get; set; }
        public string Cuit { get; set; }
        public string CondicionIva { get; set; }
        public string Domicilio { get; set; }
        public string IngresosBrutos { get; set; }
        public string InicioActividades { get; set; }
        public int PuntoVenta { get; set; }

        // Un consumidor final no factura: el comercio tiene que ser responsable inscripto,
        // monotributista o exento.
        public ResultadoOperacion Validar()
        {
            if (string.IsNullOrWhiteSpace(RazonSocial))
                return ResultadoOperacion.Error("Falta la razón social del comercio (Emisor.RazonSocial en Web.config).");

            if (!Proveedor.EsCuitValido(Cuit))
                return ResultadoOperacion.Error("El CUIT del comercio (Emisor.Cuit en Web.config) debe tener 11 números.");

            if (CondicionIva != Iva.ResponsableInscripto && CondicionIva != Iva.Monotributista &&
                CondicionIva != Iva.SujetoExento)
                return ResultadoOperacion.Error(
                    "La condición frente al IVA del comercio (Emisor.CondicionIva en Web.config) tiene que ser " +
                    "Responsable Inscripto, Monotributista o Exento.");

            if (PuntoVenta < 1 || PuntoVenta > 99999)
                return ResultadoOperacion.Error("El punto de venta (Emisor.PuntoVenta en Web.config) tiene que estar entre 1 y 99999.");

            return ResultadoOperacion.Ok();
        }
    }
}
