using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace BIZ.Modelo
{
    // Provincia y localidades de un código postal argentino, para completar el domicilio mientras
    // se escribe el código (Docs/Lubricentro_Requerimientos.md §9.15). Los datos van en
    // CodigosPostales.txt, recurso embebido en BIZ.dll: son de consulta, no se editan desde la
    // aplicación (como Domicilio.Provincias), y así no hace falta cargarlos en la base. Vienen de
    // GeoNames (www.geonames.org, licencia Creative Commons Attribution 4.0), ya limpios: el
    // encabezado del archivo explica cómo se armaron.
    //
    // Un código de 4 dígitos puede tener varias localidades (la ciudad y los pueblos de alrededor)
    // y, en los límites, más de una provincia: vale la que tiene más localidades con ese código. El
    // CPA trae la provincia en la letra (B1638ABC es de Buenos Aires), así que con él no hay duda.
    public static class CodigosPostales
    {
        private const string Recurso = "BIZ.Modelo.CodigosPostales.txt";

        // La Ciudad Autónoma de Buenos Aires usa del 1000 al 1499 (C1000 a C1499 en el CPA). No
        // viene en los datos de GeoNames: se resuelve acá.
        private const int PrimeroCaba = 1000;
        private const int UltimoCaba = 1499;

        // Letra del CPA de cada provincia (la del código ISO 3166-2:AR).
        private static readonly Dictionary<char, string> ProvinciaPorLetra = new Dictionary<char, string>
        {
            { 'A', "Salta" }, { 'B', "Buenos Aires" }, { 'C', Domicilio.Caba }, { 'D', "San Luis" },
            { 'E', "Entre Ríos" }, { 'F', "La Rioja" }, { 'G', "Santiago del Estero" }, { 'H', "Chaco" },
            { 'J', "San Juan" }, { 'K', "Catamarca" }, { 'L', "La Pampa" }, { 'M', "Mendoza" },
            { 'N', "Misiones" }, { 'P', "Formosa" }, { 'Q', "Neuquén" }, { 'R', "Río Negro" },
            { 'S', "Santa Fe" }, { 'T', "Tucumán" }, { 'U', "Chubut" }, { 'V', "Tierra del Fuego" },
            { 'W', "Corrientes" }, { 'X', "Córdoba" }, { 'Y', "Jujuy" }, { 'Z', "Santa Cruz" }
        };

        // Se lee una sola vez, la primera vez que se busca un código.
        private static readonly Lazy<Guia> Datos = new Lazy<Guia>(Cargar);

        // Lo que se sabe de un código válido, o null si es inválido o no se conoce ni su provincia.
        public static LugarPostal Buscar(string codigoPostal)
        {
            if (string.IsNullOrWhiteSpace(codigoPostal) || !Domicilio.EsCodigoPostalValido(codigoPostal))
                return null;

            var codigo = Domicilio.NormalizarCodigoPostal(codigoPostal);
            var esCpa = codigo.Length == 8;
            var numero = int.Parse(esCpa ? codigo.Substring(1, 4) : codigo, CultureInfo.InvariantCulture);

            string provinciaDeLaLetra = null;
            if (esCpa && !ProvinciaPorLetra.TryGetValue(codigo[0], out provinciaDeLaLetra))
                return null;

            if (provinciaDeLaLetra == Domicilio.Caba || (!esCpa && numero >= PrimeroCaba && numero <= UltimoCaba))
                return new LugarPostal(Domicilio.Caba, Domicilio.Caba, new[] { Domicilio.Caba });

            List<LugarPostal> lugares;
            Datos.Value.PorCodigo.TryGetValue(numero, out lugares);

            if (provinciaDeLaLetra != null)
            {
                var lugar = lugares == null ? null : lugares.Find(l => l.Provincia == provinciaDeLaLetra);
                return lugar ?? new LugarPostal(provinciaDeLaLetra, null, new string[0]);
            }

            if (lugares != null) return lugares[0];

            // Un código que no está en los datos: si los códigos vecinos (el anterior y el siguiente
            // que sí están) son de la misma provincia, casi seguro que este también. La localidad no
            // se puede saber.
            var provincia = Datos.Value.ProvinciaVecina(numero);
            return provincia == null ? null : new LugarPostal(provincia, null, new string[0]);
        }

        private static Guia Cargar()
        {
            var guia = new Guia();
            var ensamblado = typeof(CodigosPostales).Assembly;
            using (var flujo = ensamblado.GetManifestResourceStream(Recurso))
            {
                if (flujo == null)
                    throw new InvalidOperationException("Falta el recurso " + Recurso + " en BIZ.dll.");

                using (var lector = new StreamReader(flujo, Encoding.UTF8))
                {
                    string linea;
                    while ((linea = lector.ReadLine()) != null)
                    {
                        if (linea.Length == 0 || linea[0] == '#') continue;

                        // código, provincia, localidad principal (puede faltar) y localidades.
                        var campos = linea.Split('\t');
                        var numero = int.Parse(campos[0], CultureInfo.InvariantCulture);
                        var localidades = campos[3].Length == 0 ? new string[0] : campos[3].Split('|');
                        var lugar = new LugarPostal(campos[1], campos[2].Length == 0 ? null : campos[2], localidades);

                        List<LugarPostal> lugares;
                        if (!guia.PorCodigo.TryGetValue(numero, out lugares))
                            guia.PorCodigo[numero] = lugares = new List<LugarPostal>();
                        lugares.Add(lugar);
                    }
                }
            }

            guia.Codigos = new List<int>(guia.PorCodigo.Keys);
            guia.Codigos.Sort();
            return guia;
        }

        private class Guia
        {
            // Para cada código, sus lugares por provincia: primero el de la provincia que vale para
            // el código de 4 dígitos (el archivo ya viene en ese orden).
            public readonly Dictionary<int, List<LugarPostal>> PorCodigo = new Dictionary<int, List<LugarPostal>>();

            public List<int> Codigos;

            public string ProvinciaVecina(int numero)
            {
                var indice = Codigos.BinarySearch(numero);
                if (indice >= 0) return null;

                var siguiente = ~indice;
                if (siguiente == 0 || siguiente == Codigos.Count) return null;

                var anterior = PorCodigo[Codigos[siguiente - 1]][0].Provincia;
                var posterior = PorCodigo[Codigos[siguiente]][0].Provincia;
                return anterior == posterior ? anterior : null;
            }
        }
    }

    // Lo que dice la guía de un código postal: la provincia, la localidad principal (null si el
    // código tiene varias y ninguna se destaca, o si no se sabe) y todas las localidades del código,
    // en orden alfabético, para elegir entre ellas.
    public class LugarPostal
    {
        public LugarPostal(string provincia, string localidad, IList<string> localidades)
        {
            Provincia = provincia;
            Localidad = localidad;
            Localidades = localidades;
        }

        public string Provincia { get; private set; }
        public string Localidad { get; private set; }
        public IList<string> Localidades { get; private set; }
    }
}
