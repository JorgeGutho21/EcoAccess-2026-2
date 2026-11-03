using System;
using SkiaSharp;

namespace EcoAccess.P3.Bot.Vision
{

    /// <summary>
    /// Mide si una foto tiene detalle suficiente, sin modelo: ante una foto borrosa o vacía LFM3-VL inventa un objeto antes que
    /// abstenerse. Umbrales fijados con las fotos de ajuste (ver docs/vision/RECONOCIMIENTO_GENERAL.md); son conservadores, y
    /// un desenfoque leve sigue llegando al modelo.
    /// </summary>
    public static class CalidadImagen
    {
        private const int Lado = 256;

        /// <summary>Por debajo no hay bordes: desenfoque fuerte. En las fotos de ajuste: 1,7 o menos en las borrosas, 5,4 o más en las normales.</summary>
        public const double DetalleMinimo = 2.5;

        /// <summary>Por debajo no hay contraste: superficie vacía o foto tapada.</summary>
        public const double ContrasteMinimo = 3.0;

        public static bool Insuficiente(byte[] jpeg)
        {
            (double detalle, double contraste) = Medir(jpeg);
            return detalle < DetalleMinimo || contraste < ContrasteMinimo;
        }

        /// <summary>Desviación del laplaciano y de la luminosidad, sobre la imagen reducida y sin los bordes.</summary>
        public static (double Detalle, double Contraste) Medir(byte[] jpeg)
        {
            using SKBitmap original = SKBitmap.Decode(jpeg) ?? throw new ArgumentException("La imagen no se puede decodificar.");
            float escala = Math.Min(1f, (float)Lado / Math.Max(original.Width, original.Height));
            int ancho = Math.Max(3, (int)(original.Width * escala)), alto = Math.Max(3, (int)(original.Height * escala));
            using SKBitmap pequena = original.Resize(new SKImageInfo(ancho, alto), new SKSamplingOptions(SKFilterMode.Linear)) ?? throw new ArgumentException("La imagen no se puede reducir.");
            var gris = new double[ancho, alto];
            for (int y = 0; y < alto; y++)
                for (int x = 0; x < ancho; x++)
                {
                    SKColor c = pequena.GetPixel(x, y);
                    gris[x, y] = 0.299 * c.Red + 0.587 * c.Green + 0.114 * c.Blue;
                }
            double sumaL = 0, cuadradoL = 0, sumaG = 0, cuadradoG = 0;
            int n = 0;
            for (int y = 1; y < alto - 1; y++)
                for (int x = 1; x < ancho - 1; x++)
                {
                    double l = gris[x - 1, y] + gris[x + 1, y] + gris[x, y - 1] + gris[x, y + 1] - 4 * gris[x, y];
                    sumaL += l; cuadradoL += l * l; sumaG += gris[x, y]; cuadradoG += gris[x, y] * gris[x, y]; n++;
                }
            return (Math.Sqrt(Math.Max(0, cuadradoL / n - sumaL / n * (sumaL / n))), Math.Sqrt(Math.Max(0, cuadradoG / n - sumaG / n * (sumaG / n))));
        }
    }
}
