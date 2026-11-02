using System;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using SkiaSharp;

namespace EcoAccess.P3.Bot.Vision
{

    /// <summary>Decodifica con límites, aplica orientación y vuelve a codificar sin EXIF antes de salir a la API.</summary>
    public static class PreparadorImagen
    {
        public const long MaximoPixeles = 20_000_000;
        public const int LadoMaximo = 1024;

        public static ImagenResiduo Preparar(ImagenResiduo imagen, int ladoMaximo = LadoMaximo)
        {
            if (ServicioClasificacionImagen.TipoMimeReal(imagen.Contenido) is null)
                throw new ArgumentException("La imagen no tiene un formato o tamaño permitido.");
            using var datos = SKData.CreateCopy(imagen.Contenido);
            using var codec = SKCodec.Create(datos);
            if (codec is null || codec.Info.Width <= 0 || codec.Info.Height <= 0
                || (long)codec.Info.Width * codec.Info.Height > MaximoPixeles || codec.FrameCount > 1)
                throw new ArgumentException("La imagen no se puede decodificar dentro de los límites permitidos.");
            int w = codec.Info.Width, h = codec.Info.Height;
            using var bitmap = new SKBitmap(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
            if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
                throw new ArgumentException("La imagen está incompleta o dañada.");
            int origen = (int)codec.EncodedOrigin;
            bool intercambiar = origen >= 5;
            int ancho = intercambiar ? h : w, alto = intercambiar ? w : h;
            float escala = Math.Min(1f, (float)ladoMaximo / Math.Max(ancho, alto));
            int salidaW = Math.Max(1, (int)Math.Round(ancho * escala)), salidaH = Math.Max(1, (int)Math.Round(alto * escala));
            using var salida = new SKBitmap(salidaW, salidaH);
            using (var canvas = new SKCanvas(salida))
            {
                canvas.Clear(SKColors.White);
                var m = origen switch
                {
                    2 => Matriz(-1, 0, w, 0, 1, 0),
                    3 => Matriz(-1, 0, w, 0, -1, h),
                    4 => Matriz(1, 0, 0, 0, -1, h),
                    5 => Matriz(0, 1, 0, 1, 0, 0),
                    6 => Matriz(0, -1, h, 1, 0, 0),
                    7 => Matriz(0, -1, h, -1, 0, w),
                    8 => Matriz(0, 1, 0, -1, 0, w),
                    _ => SKMatrix.Identity,
                };
                canvas.SetMatrix(SKMatrix.Concat(SKMatrix.CreateScale((float)salidaW / ancho, (float)salidaH / alto), m));
                canvas.DrawBitmap(bitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
            }
            using var foto = SKImage.FromBitmap(salida);
            using var jpeg = foto.Encode(SKEncodedImageFormat.Jpeg, 85);
            if (jpeg is null || jpeg.Size > ServicioClasificacionImagen.MaximoBytes)
                throw new ArgumentException("No se pudo preparar la imagen dentro del límite de tamaño.");
            return new ImagenResiduo(jpeg.ToArray(), "image/jpeg");
        }
        private static SKMatrix Matriz(float sx, float kx, float tx, float ky, float sy, float ty) => new()
        {
            ScaleX = sx,
            SkewX = kx,
            TransX = tx,
            SkewY = ky,
            ScaleY = sy,
            TransY = ty,
            Persp2 = 1,
        };
    }
}
