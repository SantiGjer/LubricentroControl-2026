using System;
using System.Text;
using System.Text.RegularExpressions;

namespace BIZ.Modelo
{
    public class Cliente
    {
        // Tipo de cliente (Docs/Lubricentro_Requerimientos.md §9.8): una persona física se nombra
        // por nombre y apellido; una empresa, por su razón social.
        public const string TipoPersonaFisica = "Persona física";
        public const string TipoEmpresa = "Empresa";

        public static readonly string[] TiposCliente = { TipoPersonaFisica, TipoEmpresa };

        // Tipos de documento. CUIT y CUIL tienen 11 dígitos; DNI, LE y LC, 7 u 8.
        public const string DocumentoDni = "DNI";
        public const string DocumentoCuit = "CUIT";
        public const string DocumentoCuil = "CUIL";
        public const string DocumentoLe = "LE";
        public const string DocumentoLc = "LC";
        public const string DocumentoPasaporte = "Pasaporte";

        public static readonly string[] TiposDocumento =
            { DocumentoDni, DocumentoCuit, DocumentoCuil, DocumentoLe, DocumentoLc, DocumentoPasaporte };

        // Las 23 provincias y la Ciudad Autónoma de Buenos Aires, en orden alfabético.
        public static readonly string[] Provincias =
        {
            "Buenos Aires", "Catamarca", "Chaco", "Chubut", "Ciudad Autónoma de Buenos Aires", "Córdoba",
            "Corrientes", "Entre Ríos", "Formosa", "Jujuy", "La Pampa", "La Rioja", "Mendoza", "Misiones",
            "Neuquén", "Río Negro", "Salta", "San Juan", "San Luis", "Santa Cruz", "Santa Fe",
            "Santiago del Estero", "Tierra del Fuego", "Tucumán"
        };

        private static readonly Regex FormatoDni = new Regex(@"^\d{7,8}$", RegexOptions.Compiled);
        private static readonly Regex FormatoCuit = new Regex(@"^\d{11}$", RegexOptions.Compiled);
        private static readonly Regex FormatoPasaporte = new Regex(@"^[A-Z0-9]{6,12}$", RegexOptions.Compiled);

        // Código postal: el de 4 dígitos (1638) o el CPA de 8 caracteres (C1406GZA).
        private static readonly Regex FormatoCodigoPostal =
            new Regex(@"^(\d{4}|[A-Z]\d{4}[A-Z]{3})$", RegexOptions.Compiled);

        private static readonly Regex FormatoEmail =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public int IdCliente { get; set; }
        public string TipoCliente { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string RazonSocial { get; set; }
        public string TipoDocumento { get; set; }
        public string NumeroDocumento { get; set; }
        public string CondicionIva { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Direccion { get; set; }
        public string Localidad { get; set; }
        public string Provincia { get; set; }
        public string CodigoPostal { get; set; }

        // True si el cliente puede quedar debiendo (fiado). Sin cuenta corriente, al cerrar su
        // orden la pantalla de Órdenes lo lleva directo a cobrar la venta en Pagos.
        public bool CuentaCorriente { get; set; }

        public bool Activo { get; set; }
        public DateTime FechaAlta { get; set; }

        public bool EsEmpresa
        {
            get { return TipoCliente == TipoEmpresa; }
        }

        // Cómo se lo nombra en las pantallas: la razón social de una empresa, o "Nombre Apellido".
        // Es la misma regla que la columna calculada Cliente.denominacion de la base.
        public string Denominacion
        {
            get { return EsEmpresa ? RazonSocial : (Nombre + " " + Apellido).Trim(); }
        }

        // "DNI 30111222" o "CUIT 20-25333444-5".
        public string Documento
        {
            get { return FormatearDocumento(TipoDocumento, NumeroDocumento); }
        }

        // Dirección, localidad, provincia y código postal, lo que esté cargado.
        public string DomicilioCompleto
        {
            get { return ArmarDomicilio(Direccion, Localidad, Provincia, CodigoPostal); }
        }

        public static string ArmarDomicilio(string direccion, string localidad, string provincia, string codigoPostal)
        {
            var partes = new StringBuilder();
            foreach (var parte in new[] { direccion, localidad, provincia })
            {
                if (string.IsNullOrWhiteSpace(parte)) continue;
                if (partes.Length > 0) partes.Append(", ");
                partes.Append(parte.Trim());
            }
            if (!string.IsNullOrWhiteSpace(codigoPostal))
                partes.Append(partes.Length > 0 ? " (CP " : "(CP ").Append(codigoPostal.Trim()).Append(")");
            return partes.ToString();
        }

        public static bool EsTipoDocumentoNumerico(string tipoDocumento)
        {
            return tipoDocumento != DocumentoPasaporte;
        }

        // Los documentos numéricos se aceptan con puntos, guiones o espacios y se guardan solo
        // con los dígitos; el pasaporte, en mayúsculas y sin espacios.
        public static string NormalizarNumeroDocumento(string tipoDocumento, string numero)
        {
            if (numero == null) return null;

            if (!EsTipoDocumentoNumerico(tipoDocumento))
                return numero.Replace(" ", "").Trim().ToUpperInvariant();

            var limpio = new StringBuilder();
            foreach (var c in numero)
            {
                if (char.IsDigit(c)) limpio.Append(c);
                else if (c != '.' && c != '-' && c != ' ') return numero.Trim(); // deja que falle el formato
            }
            return limpio.ToString();
        }

        // Formato según el tipo (Requerimientos §9.1 y §9.8): DNI, LE y LC con 7 u 8 dígitos;
        // CUIT y CUIL con 11; pasaporte, de 6 a 12 letras y números.
        public static bool EsNumeroDocumentoValido(string tipoDocumento, string numero)
        {
            var normalizado = NormalizarNumeroDocumento(tipoDocumento, numero);
            if (string.IsNullOrEmpty(normalizado)) return false;

            switch (tipoDocumento)
            {
                case DocumentoCuit:
                case DocumentoCuil:
                    return FormatoCuit.IsMatch(normalizado);
                case DocumentoPasaporte:
                    return FormatoPasaporte.IsMatch(normalizado);
                default:
                    return FormatoDni.IsMatch(normalizado);
            }
        }

        public static string MensajeFormatoDocumento(string tipoDocumento)
        {
            switch (tipoDocumento)
            {
                case DocumentoCuit:
                case DocumentoCuil:
                    return "El " + tipoDocumento + " debe tener 11 números, con o sin guiones.";
                case DocumentoPasaporte:
                    return "El pasaporte debe tener entre 6 y 12 letras o números.";
                default:
                    return "El " + (tipoDocumento ?? "documento") + " debe tener 7 u 8 números, sin puntos.";
            }
        }

        // "DNI 30111222" o "CUIT 20-25333444-5" (los de 11 dígitos se muestran con guiones).
        public static string FormatearDocumento(string tipoDocumento, string numero)
        {
            if (string.IsNullOrEmpty(numero)) return tipoDocumento;

            var mostrado = numero;
            if ((tipoDocumento == DocumentoCuit || tipoDocumento == DocumentoCuil) && numero.Length == 11)
                mostrado = numero.Substring(0, 2) + "-" + numero.Substring(2, 8) + "-" + numero.Substring(10, 1);

            return tipoDocumento + " " + mostrado;
        }

        // Vacío es válido: el código postal no es obligatorio.
        public static bool EsCodigoPostalValido(string codigoPostal)
        {
            return string.IsNullOrWhiteSpace(codigoPostal) ||
                   FormatoCodigoPostal.IsMatch(codigoPostal.Trim().ToUpperInvariant());
        }

        public static bool EsEmailValido(string email)
        {
            return string.IsNullOrWhiteSpace(email) || FormatoEmail.IsMatch(email.Trim());
        }

        // Valida los campos obligatorios y su formato, y los normaliza (espacios de los bordes,
        // documento sin separadores, código postal en mayúsculas). Una empresa, y cualquiera que
        // no sea consumidor final, se identifica con CUIT (§9.8); la base lo vuelve a exigir con
        // CK_Cliente_cuit.
        public ResultadoOperacion Validar()
        {
            if (Array.IndexOf(TiposCliente, TipoCliente) < 0)
                return ResultadoOperacion.Error("Elegí si el cliente es una persona física o una empresa.");

            if (EsEmpresa)
            {
                if (string.IsNullOrWhiteSpace(RazonSocial))
                    return ResultadoOperacion.Error("La razón social es obligatoria.");

                RazonSocial = RazonSocial.Trim();
                Nombre = null;
                Apellido = null;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Nombre))
                    return ResultadoOperacion.Error("El nombre es obligatorio.");

                if (string.IsNullOrWhiteSpace(Apellido))
                    return ResultadoOperacion.Error("El apellido es obligatorio.");

                Nombre = Nombre.Trim();
                Apellido = Apellido.Trim();
                RazonSocial = null;
            }

            if (Array.IndexOf(TiposDocumento, TipoDocumento) < 0)
                return ResultadoOperacion.Error("Elegí el tipo de documento.");

            if (string.IsNullOrWhiteSpace(NumeroDocumento))
                return ResultadoOperacion.Error("El número de documento es obligatorio.");

            if (!EsNumeroDocumentoValido(TipoDocumento, NumeroDocumento))
                return ResultadoOperacion.Error(MensajeFormatoDocumento(TipoDocumento));

            if (!Iva.EsCondicionValida(CondicionIva))
                return ResultadoOperacion.Error("Elegí la condición frente al IVA.");

            if (EsEmpresa && TipoDocumento != DocumentoCuit)
                return ResultadoOperacion.Error("Una empresa se identifica con su CUIT.");

            if (CondicionIva != Iva.ConsumidorFinal && TipoDocumento != DocumentoCuit)
                return ResultadoOperacion.Error("Un cliente " + CondicionIva.ToLowerInvariant() +
                                                " se identifica con su CUIT.");

            if (!FormatoTelefono.EsValido(Telefono))
                return ResultadoOperacion.Error(FormatoTelefono.MensajeError);

            if (!EsEmailValido(Email))
                return ResultadoOperacion.Error("El mail no tiene un formato válido.");

            if (!string.IsNullOrWhiteSpace(Provincia) && Array.IndexOf(Provincias, Provincia) < 0)
                return ResultadoOperacion.Error("La provincia no es válida.");

            if (!EsCodigoPostalValido(CodigoPostal))
                return ResultadoOperacion.Error("El código postal debe tener 4 números (1638) o el formato CPA (C1406GZA).");

            NumeroDocumento = NormalizarNumeroDocumento(TipoDocumento, NumeroDocumento);
            Telefono = string.IsNullOrWhiteSpace(Telefono) ? null : Telefono.Trim();
            Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
            Direccion = string.IsNullOrWhiteSpace(Direccion) ? null : Direccion.Trim();
            Localidad = string.IsNullOrWhiteSpace(Localidad) ? null : Localidad.Trim();
            Provincia = string.IsNullOrWhiteSpace(Provincia) ? null : Provincia;
            CodigoPostal = string.IsNullOrWhiteSpace(CodigoPostal) ? null : CodigoPostal.Trim().ToUpperInvariant();

            return ResultadoOperacion.Ok();
        }
    }
}
