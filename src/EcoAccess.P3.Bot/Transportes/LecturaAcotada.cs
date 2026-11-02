using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Bot.Transportes
{

    /// <summary>Límite sobre bytes efectivamente leídos, además de cualquier longitud declarada.</summary>
    internal static class LecturaAcotada
    {
        internal static async Task<byte[]> DesdeHttpAsync(HttpContent content, int maximo, CancellationToken ct)
        {
            if (content.Headers.ContentLength > maximo) throw new InvalidOperationException("Contenido demasiado grande.");
            using var stream = await content.ReadAsStreamAsync(ct);
            return await DesdeStreamAsync(stream, maximo, ct);
        }
        internal static async Task<byte[]> DesdeStreamAsync(Stream stream, int maximo, CancellationToken ct)
        {
            using var memoria = new MemoryStream();
            byte[] buffer = new byte[8192]; int leidos;
            while ((leidos = await stream.ReadAsync(buffer, ct)) > 0)
            {
                if (memoria.Length + leidos > maximo) throw new InvalidOperationException("Contenido demasiado grande.");
                memoria.Write(buffer, 0, leidos);
            }
            return memoria.ToArray();
        }
    }
}
