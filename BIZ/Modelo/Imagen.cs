using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace BIZ.Modelo
{
    // Imagen de un producto (Docs/Lubricentro_Requerimientos.md §9.13): el archivo que se ve en el
    // formulario y en "Ver", su miniatura para la lista, y las reglas para aceptar uno subido. Se
    // acepta PNG o JPG de hasta 5 MB y se guarda en el mismo formato: a lo sumo de 800 px de lado
    // (la pantalla nunca la muestra más grande) y la miniatura de 120 px (la lista la muestra a 36).
    public class Imagen
    {
        public const string TipoPng = "image/png";
        public const string TipoJpeg = "image/jpeg";

        public const int MegabytesMaximos = 5;
        public const int BytesMaximos = MegabytesMaximos * 1024 * 1024;

        public const int LadoImagen = 800;
        public const int LadoMiniatura = 120;

        // Más que una foto de teléfono: un archivo así pesa poco comprimido pero ocupa cientos de
        // MB de memoria al abrirlo para achicarlo.
        private const long PixelesMaximos = 40000000;

        private const long CalidadJpeg = 85;

        // Orientación que anota la cámara en la foto (EXIF): el navegador la endereza al mostrar
        // el archivo original, pero una copia achicada ya no lleva ese dato y hay que girarla.
        private const int PropiedadOrientacion = 0x0112;

        private static readonly byte[] FirmaPng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] FirmaJpeg = { 0xFF, 0xD8, 0xFF };

        // image/png o image/jpeg.
        public string TipoContenido { get; set; }

        // Al leer una imagen de la base viene solo uno de los dos archivos (ProductoDAL.ObtenerImagen).
        public byte[] Contenido { get; set; }
        public byte[] Miniatura { get; set; }

        public DateTime FechaActualizacion { get; set; }

        // Valida un archivo subido y arma la imagen a guardar: el mismo archivo si ya mide hasta
        // LadoImagen (sin volver a comprimirlo) o una copia achicada, y siempre la miniatura.
        public static ResultadoOperacion Preparar(byte[] archivo, out Imagen imagen)
        {
            imagen = null;

            if (archivo == null || archivo.Length == 0)
                return ResultadoOperacion.Error("El archivo de la imagen está vacío.");

            if (archivo.Length > BytesMaximos)
                return ResultadoOperacion.Error("La imagen no puede pesar más de " + MegabytesMaximos + " MB.");

            // El formato sale de los primeros bytes del archivo, no de la extensión del nombre.
            var tipo = TipoDe(archivo);
            if (tipo == null)
                return ResultadoOperacion.Error("La imagen tiene que ser PNG o JPG.");

            try
            {
                using (var flujo = new MemoryStream(archivo))
                using (var original = Image.FromStream(flujo, false, false))
                {
                    if ((long)original.Width * original.Height > PixelesMaximos)
                        return ResultadoOperacion.Error("La imagen es demasiado grande: achicala antes de subirla.");

                    var giro = Giro(original);
                    var entra = Math.Max(original.Width, original.Height) <= LadoImagen;

                    imagen = new Imagen
                    {
                        TipoContenido = tipo,
                        Contenido = entra ? archivo : Achicar(original, giro, LadoImagen, tipo),
                        Miniatura = Achicar(original, giro, LadoMiniatura, tipo)
                    };
                }
            }
            // Así avisa GDI+ que no pudo leer el archivo: dañado, o que solo empieza como un PNG o un JPG.
            catch (Exception ex) when (ex is ArgumentException || ex is ExternalException || ex is OutOfMemoryException)
            {
                imagen = null;
                return ResultadoOperacion.Error("El archivo no es una imagen PNG o JPG válida.");
            }

            return ResultadoOperacion.Ok();
        }

        private static string TipoDe(byte[] archivo)
        {
            if (EmpiezaCon(archivo, FirmaPng)) return TipoPng;
            if (EmpiezaCon(archivo, FirmaJpeg)) return TipoJpeg;
            return null;
        }

        private static bool EmpiezaCon(byte[] archivo, byte[] firma)
        {
            if (archivo.Length < firma.Length) return false;

            for (var i = 0; i < firma.Length; i++)
                if (archivo[i] != firma[i]) return false;

            return true;
        }

        // Cómo enderezar la foto según la orientación EXIF (1 = derecha; 2 a 8, giros y espejos).
        private static RotateFlipType Giro(Image imagen)
        {
            if (Array.IndexOf(imagen.PropertyIdList, PropiedadOrientacion) < 0)
                return RotateFlipType.RotateNoneFlipNone;

            var valor = imagen.GetPropertyItem(PropiedadOrientacion).Value;
            switch (valor == null || valor.Length < 2 ? 1 : BitConverter.ToUInt16(valor, 0))
            {
                case 2: return RotateFlipType.RotateNoneFlipX;
                case 3: return RotateFlipType.Rotate180FlipNone;
                case 4: return RotateFlipType.Rotate180FlipX;
                case 5: return RotateFlipType.Rotate90FlipX;
                case 6: return RotateFlipType.Rotate90FlipNone;
                case 7: return RotateFlipType.Rotate270FlipX;
                case 8: return RotateFlipType.Rotate270FlipNone;
                default: return RotateFlipType.RotateNoneFlipNone;
            }
        }

        // Copia enderezada y de a lo sumo lado x lado, en el mismo formato: el PNG conserva la
        // transparencia y el JPG sale con calidad 85.
        private static byte[] Achicar(Image original, RotateFlipType giro, int lado, string tipo)
        {
            var escala = Math.Min(1.0, (double)lado / Math.Max(original.Width, original.Height));
            var ancho = Math.Max(1, (int)Math.Round(original.Width * escala));
            var alto = Math.Max(1, (int)Math.Round(original.Height * escala));
            var formato = tipo == TipoJpeg ? PixelFormat.Format24bppRgb : PixelFormat.Format32bppArgb;

            using (var copia = new Bitmap(ancho, alto, formato))
            {
                using (var grafico = Graphics.FromImage(copia))
                using (var atributos = new ImageAttributes())
                {
                    grafico.CompositingMode = CompositingMode.SourceCopy;
                    grafico.CompositingQuality = CompositingQuality.HighQuality;
                    grafico.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    grafico.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    // Sin esto GDI+ deja un borde gris semitransparente alrededor de la copia.
                    atributos.SetWrapMode(WrapMode.TileFlipXY);
                    grafico.DrawImage(original, new Rectangle(0, 0, ancho, alto),
                        0, 0, original.Width, original.Height, GraphicsUnit.Pixel, atributos);
                }

                copia.RotateFlip(giro);

                using (var flujo = new MemoryStream())
                {
                    if (tipo == TipoJpeg)
                    {
                        using (var parametros = new EncoderParameters(1))
                        {
                            parametros.Param[0] = new EncoderParameter(Encoder.Quality, CalidadJpeg);
                            copia.Save(flujo, CodificadorJpeg(), parametros);
                        }
                    }
                    else
                    {
                        copia.Save(flujo, ImageFormat.Png);
                    }

                    return flujo.ToArray();
                }
            }
        }

        private static ImageCodecInfo CodificadorJpeg()
        {
            foreach (var codificador in ImageCodecInfo.GetImageEncoders())
                if (codificador.FormatID == ImageFormat.Jpeg.Guid) return codificador;

            throw new InvalidOperationException("Falta el codificador JPEG de Windows.");
        }
    }
}
