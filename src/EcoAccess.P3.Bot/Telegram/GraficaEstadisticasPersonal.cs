using System;
using System.Linq;
using EcoAccess.P3.Logica.Contratos;
using SkiaSharp;

namespace EcoAccess.P3.Bot.Telegram
{

    /// <summary>Presentación local de datos previamente autorizados. No consulta repositorios ni escribe archivos.</summary>
    internal static class GraficaEstadisticasPersonal
    {
        internal static byte[] Crear(EstadisticasPersonales datos)
        {
            using var superficie = SKSurface.Create(new SKImageInfo(1000, 620));
            var lienzo = superficie.Canvas; lienzo.Clear(new SKColor(245, 250, 246));
            using var tinta = new SKPaint { Color = new SKColor(28, 66, 48), IsAntialias = true };
            using var verde = new SKPaint { Color = new SKColor(30, 126, 82), IsAntialias = true };
            using var gris = new SKPaint { Color = new SKColor(105, 125, 116), IsAntialias = true };
            using var fuente = new SKFont(SKTypeface.Default, 26);
            lienzo.DrawText("EcoAccess • Tu actividad personal", 40, 48, SKTextAlign.Left, fuente, tinta);
            fuente.Size = 19;
            lienzo.DrawText("Depósitos registrados; no equivalen a peso ni a puntos", 40, 80, SKTextAlign.Left, fuente, gris);
            int maximo = Math.Max(1, Math.Max(datos.Actual.Depositos.Total, datos.Anterior.Depositos.Total));
            void Barra(string etiqueta, int valor, float y, SKPaint color)
            {
                lienzo.DrawText(etiqueta, 40, y + 22, SKTextAlign.Left, fuente, tinta);
                float ancho = (float)Compat.Clamp((double)valor / maximo, 0, 1) * 560;
                lienzo.DrawRect(300, y, ancho, 32, color);
                lienzo.DrawText(valor.ToString(System.Globalization.CultureInfo.InvariantCulture), 880, y + 23, SKTextAlign.Left, fuente, tinta);
            }
            Barra("30 días anteriores", datos.Anterior.Depositos.Total, 110, gris);
            Barra("Últimos 30 días", datos.Actual.Depositos.Total, 165, verde);
            fuente.Size = 22; lienzo.DrawText("Días recientes con actividad (Colombia)", 40, 260, SKTextAlign.Left, fuente, tinta);
            var dias = datos.Evolucion.Where(f => f.Dia is not null).OrderByDescending(f => f.Dia).Take(7).OrderBy(f => f.Dia).ToArray();
            int maxDia = Math.Max(1, dias.Length == 0 ? 0 : dias.Max(f => f.Depositos));
            fuente.Size = 16;
            for (int i = 0; i < dias.Length; i++)
            {
                float alto = (float)Compat.Clamp((double)dias[i].Depositos / maxDia, 0, 1) * 200;
                float x = 80 + i * 125;
                lienzo.DrawRect(x, 510 - alto, 65, alto, verde);
                lienzo.DrawText(dias[i].Depositos.ToString(System.Globalization.CultureInfo.InvariantCulture), x + 20, 500 - alto, SKTextAlign.Left, fuente, tinta);
                lienzo.DrawText(dias[i].Dia!.Value.ToString("MM-dd", System.Globalization.CultureInfo.InvariantCulture), x, 540, SKTextAlign.Left, fuente, tinta);
            }
            if (dias.Length == 0) lienzo.DrawText("No hay depósitos en el período seleccionado.", 60, 360, SKTextAlign.Left, fuente, gris);
            lienzo.DrawText("Solicitudes, depósitos y verificaciones son hechos diferentes. Consulta /estadisticas.", 40, 590, SKTextAlign.Left, fuente, gris);
            using var imagen = superficie.Snapshot(); using var png = imagen.Encode(SKEncodedImageFormat.Png, 100);
            return png.ToArray();
        }
    }
}
