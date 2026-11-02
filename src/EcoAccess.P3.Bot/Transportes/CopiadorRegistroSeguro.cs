using System;
using System.IO;
using System.Threading.Tasks;

namespace EcoAccess.P3.Bot.Transportes
{
    /// <summary>Si falla el disco, continúa drenando el pipe para permitir el cierre del proceso supervisado.</summary>
    public static class CopiadorRegistroSeguro
    {
        public static async Task<bool> CopiarAsync(Stream entrada, Stream salida)
        {
            bool completo = true; byte[] buffer = new byte[4096]; Stream destino = salida;
            try
            {
                int cantidad;
                while ((cantidad = await entrada.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    // Se vacía en cada bloque: el registro debe poder leerse mientras el proceso sigue activo.
                    try { await destino.WriteAsync(buffer, 0, cantidad).ConfigureAwait(false); await destino.FlushAsync().ConfigureAwait(false); }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ObjectDisposedException)
                    { completo = false; destino = Stream.Null; }
                }
                try { await destino.FlushAsync().ConfigureAwait(false); }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ObjectDisposedException) { completo = false; }
                return completo;
            }
            catch (Exception e) when (e is IOException || e is ObjectDisposedException) { return false; }
        }
    }
}
