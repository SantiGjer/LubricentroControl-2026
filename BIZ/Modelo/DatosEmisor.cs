using System;

namespace BIZ.Modelo
{
    // Datos del comercio que emite las facturas (Docs/Lubricentro_Requerimientos.md §9.10): una
    // sola fila en la tabla Emisor, que se edita desde Administración > Datos del comercio
    // (EmisorDAL). Cada factura copia estos datos al emitirse, así que cambiarlos no toca las que
    // ya salieron.
    public class DatosEmisor
    {
        public const int PuntoVentaMinimo = 1;
        public const int PuntoVentaMaximo = 99999;

        // Un consumidor final no factura: el comercio es responsable inscripto, monotributista o
        // exento. La condición define la letra (Factura.DeterminarTipo).
        public static readonly string[] Condiciones =
            { Iva.ResponsableInscripto, Iva.Monotributista, Iva.SujetoExento };

        public string RazonSocial { get; set; }
        public string Cuit { get; set; }
        public string CondicionIva { get; set; }
        public string Domicilio { get; set; }
        public string IngresosBrutos { get; set; }
        public DateTime? InicioActividades { get; set; }
        public int PuntoVenta { get; set; }

        // Valida y normaliza: CUIT sin guiones y espacios de los bordes fuera.
        public ResultadoOperacion Validar()
        {
            if (string.IsNullOrWhiteSpace(RazonSocial))
                return ResultadoOperacion.Error("La razón social es obligatoria.");

            if (string.IsNullOrWhiteSpace(Cuit))
                return ResultadoOperacion.Error("El CUIT es obligatorio.");

            if (!Proveedor.EsCuitValido(Cuit))
                return ResultadoOperacion.Error("El CUIT debe tener 11 números, con o sin guiones.");

            if (Array.IndexOf(Condiciones, CondicionIva) < 0)
                return ResultadoOperacion.Error(
                    "La condición frente al IVA tiene que ser Responsable Inscripto, Monotributista o Exento.");

            if (PuntoVenta < PuntoVentaMinimo || PuntoVenta > PuntoVentaMaximo)
                return ResultadoOperacion.Error("El punto de venta tiene que estar entre 1 y 99999.");

            if (InicioActividades.HasValue && InicioActividades.Value.Date > DateTime.Today)
                return ResultadoOperacion.Error("El inicio de actividades no puede ser una fecha futura.");

            RazonSocial = RazonSocial.Trim();
            Cuit = Proveedor.SoloDigitos(Cuit);
            Domicilio = string.IsNullOrWhiteSpace(Domicilio) ? null : Domicilio.Trim();
            IngresosBrutos = string.IsNullOrWhiteSpace(IngresosBrutos) ? null : IngresosBrutos.Trim();
            if (InicioActividades.HasValue) InicioActividades = InicioActividades.Value.Date;

            return ResultadoOperacion.Ok();
        }
    }
}
