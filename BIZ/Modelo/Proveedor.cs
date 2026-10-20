using System;
using System.Text;
using System.Text.RegularExpressions;

namespace BIZ.Modelo
{
    public class Proveedor
    {
        // Largos máximos de los campos de texto libre (los mismos de la tabla Proveedor).
        public const int LargoObservaciones = 500;

        private static readonly Regex FormatoCuit = new Regex(@"^\d{11}$", RegexOptions.Compiled);

        private static readonly Regex FormatoEmail =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        // CBU o CVU: 22 dígitos (Docs/Lubricentro_Requerimientos.md §9.14). Como con el CUIT, no se
        // valida el dígito verificador.
        private static readonly Regex FormatoCbu = new Regex(@"^\d{22}$", RegexOptions.Compiled);

        // Alias de la cuenta: de 6 a 20 letras, números, puntos o guiones.
        private static readonly Regex FormatoAlias = new Regex(@"^[A-Z0-9.-]{6,20}$", RegexOptions.Compiled);

        public int IdProveedor { get; set; }
        public string RazonSocial { get; set; }

        // El nombre con el que se lo conoce, si no es la razón social ("Filtros del Sur").
        public string NombreFantasia { get; set; }

        public string Cuit { get; set; }

        // Responsable Inscripto, Monotributista o Exento (Iva.CondicionesDeQuienFactura): define
        // qué factura le entrega al lubricentro (A, o C de un monotributista).
        public string CondicionIva { get; set; }

        // Qué le vende al lubricentro: lubricantes, filtros, baterías…
        public string Rubro { get; set; }

        public string Telefono { get; set; }
        public string Email { get; set; }

        // Persona con la que se trata (el vendedor o el representante) y su teléfono.
        public string Contacto { get; set; }
        public string TelefonoContacto { get; set; }

        public string Direccion { get; set; }
        public string Localidad { get; set; }
        public string Provincia { get; set; }
        public string CodigoPostal { get; set; }

        // Cuenta para pagarle por transferencia: CBU (o CVU) y alias.
        public string Cbu { get; set; }
        public string Alias { get; set; }

        public string Observaciones { get; set; }
        public bool Activo { get; set; }

        // 11 dígitos; tolera guiones u otros separadores, se validan solo los dígitos
        // (Docs/Lubricentro_Requerimientos.md §9.1).
        public static bool EsCuitValido(string cuit)
        {
            return cuit != null && FormatoCuit.IsMatch(SoloDigitos(cuit));
        }

        public static bool EsEmailValido(string email)
        {
            return string.IsNullOrWhiteSpace(email) || FormatoEmail.IsMatch(email.Trim());
        }

        // Vacío es válido: el CBU no es obligatorio. Se acepta con espacios o guiones entre los
        // números y se guarda solo con los dígitos.
        public static bool EsCbuValido(string cbu)
        {
            return string.IsNullOrWhiteSpace(cbu) || FormatoCbu.IsMatch(NormalizarCbu(cbu));
        }

        // Vacío es válido: el alias no es obligatorio. Se guarda en mayúsculas.
        public static bool EsAliasValido(string alias)
        {
            return string.IsNullOrWhiteSpace(alias) || FormatoAlias.IsMatch(alias.Trim().ToUpperInvariant());
        }

        // Inserta los guiones para mostrar (NN-NNNNNNNN-N). Se guarda sin ellos.
        public static string FormatearCuit(string cuit)
        {
            var digitos = SoloDigitos(cuit ?? "");
            return digitos.Length == 11
                ? digitos.Substring(0, 2) + "-" + digitos.Substring(2, 8) + "-" + digitos.Substring(10, 1)
                : cuit;
        }

        // También la usa DatosEmisor, que valida y muestra el CUIT del comercio con estos mismos métodos.
        internal static string SoloDigitos(string valor)
        {
            var digitos = new StringBuilder();
            foreach (var c in valor)
                if (char.IsDigit(c)) digitos.Append(c);
            return digitos.ToString();
        }

        // Los espacios y guiones con que suele copiarse un CBU no cuentan; cualquier otro caracter
        // queda, para que falle el formato.
        private static string NormalizarCbu(string cbu)
        {
            return cbu.Trim().Replace(" ", "").Replace("-", "");
        }

        // Valida los campos obligatorios y el formato de los demás, y los normaliza: CUIT, CBU y
        // teléfonos solo con los dígitos, alias y código postal en mayúsculas, y sin espacios en los
        // bordes el resto (vacío queda null).
        public ResultadoOperacion Validar()
        {
            if (string.IsNullOrWhiteSpace(RazonSocial))
                return ResultadoOperacion.Error("La razón social es obligatoria.");

            if (string.IsNullOrWhiteSpace(Cuit))
                return ResultadoOperacion.Error("El CUIT es obligatorio.");

            if (!EsCuitValido(Cuit))
                return ResultadoOperacion.Error("El CUIT debe tener 11 números, con o sin guiones.");

            if (Array.IndexOf(Iva.CondicionesDeQuienFactura, CondicionIva) < 0)
                return ResultadoOperacion.Error(
                    "La condición frente al IVA tiene que ser Responsable Inscripto, Monotributista o Exento.");

            if (!FormatoTelefono.EsValido(Telefono) || !FormatoTelefono.EsValido(TelefonoContacto))
                return ResultadoOperacion.Error(FormatoTelefono.MensajeError);

            if (!EsEmailValido(Email))
                return ResultadoOperacion.Error("El mail no tiene un formato válido.");

            if (!Domicilio.EsProvinciaValida(Provincia))
                return ResultadoOperacion.Error("La provincia no es válida.");

            if (!Domicilio.EsCodigoPostalValido(CodigoPostal))
                return ResultadoOperacion.Error(Domicilio.MensajeCodigoPostal);

            if (!EsCbuValido(Cbu))
                return ResultadoOperacion.Error("El CBU o CVU debe tener 22 números.");

            if (!EsAliasValido(Alias))
                return ResultadoOperacion.Error("El alias debe tener de 6 a 20 letras, números, puntos o guiones.");

            if (Observaciones != null && Observaciones.Trim().Length > LargoObservaciones)
                return ResultadoOperacion.Error("Las observaciones no pueden pasar de " + LargoObservaciones + " caracteres.");

            RazonSocial = RazonSocial.Trim();
            NombreFantasia = Limpiar(NombreFantasia);
            Cuit = SoloDigitos(Cuit);
            Rubro = Limpiar(Rubro);
            Telefono = FormatoTelefono.Normalizar(Telefono);
            Email = Limpiar(Email);
            Contacto = Limpiar(Contacto);
            TelefonoContacto = FormatoTelefono.Normalizar(TelefonoContacto);
            Direccion = Limpiar(Direccion);
            Localidad = Limpiar(Localidad);
            Provincia = Limpiar(Provincia);
            CodigoPostal = Domicilio.NormalizarCodigoPostal(CodigoPostal);
            Cbu = string.IsNullOrWhiteSpace(Cbu) ? null : NormalizarCbu(Cbu);
            Alias = string.IsNullOrWhiteSpace(Alias) ? null : Alias.Trim().ToUpperInvariant();
            Observaciones = Limpiar(Observaciones);

            return ResultadoOperacion.Ok();
        }

        private static string Limpiar(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }
    }
}
