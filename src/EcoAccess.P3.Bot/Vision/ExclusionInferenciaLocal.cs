using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Bot.Vision
{

    /// <summary>Exclusión entre procesos EcoAccess; no intenta detener modelos o procesos ajenos.</summary>
    internal static class ExclusionInferenciaLocal
    {
        /// <summary>Cuánto se considera vigente el aviso de un ciudadano esperando (si el bot muere, no bloquea para siempre).</summary>
        internal static readonly TimeSpan VigenciaAviso = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan Pausa = TimeSpan.FromMilliseconds(100);

        /// <summary>Un solo intento: si otro proceso tiene el modelo, falla en el acto.</summary>
        internal static FileStream Adquirir(string directorio)
        {
            string archivo = Preparar(directorio, "vision-inferencia.lock");
            return Intentar(archivo) ?? throw new InvalidOperationException("Otra inferencia local de EcoAccess está activa. La selección manual sigue disponible.");
        }

        /// <summary>
        /// Espera a que el modelo quede libre en vez de fallar. Caso real del 10/11/2026: mientras corría una evaluación,
        /// la foto de un ciudadano fallaba al instante con «no disponible» aunque cada inferencia dura 1–4 s.
        /// Quien es prioritario (el bot que atiende a una persona) deja un aviso mientras espera; los procesos de
        /// evaluación no toman el modelo mientras ese aviso esté vigente, así que no le ganan el turno una y otra vez.
        /// </summary>
        internal static async Task<FileStream> AdquirirAsync(string directorio, TimeSpan espera, bool prioritario, CancellationToken ct)
        {
            string archivo = Preparar(directorio, "vision-inferencia.lock");
            string aviso = Path.Combine(directorio, "vision-prioridad.aviso");
            if (new FileInfo(aviso).LinkTarget() is not null) throw new InvalidDataException("Archivo de aviso inválido.");
            var reloj = Stopwatch.StartNew();
            bool avisado = false;
            try
            {
                while (true)
                {
                    bool cederTurno = !prioritario && AvisoVigente(aviso);
                    if (!cederTurno && Intentar(archivo) is { } obtenido) return obtenido;
                    if (reloj.Elapsed >= espera)
                        throw new InvalidOperationException("Otra inferencia local de EcoAccess está activa. La selección manual sigue disponible.");
                    if (prioritario)
                    {
                        File.WriteAllText(aviso, DateTime.UtcNow.ToString("O"));
                        avisado = true;
                    }
                    await Task.Delay(Pausa, ct).ConfigureAwait(false);
                }
            }
            finally
            {
                if (avisado) { try { File.Delete(aviso); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
            }
        }

        private static string Preparar(string directorio, string nombre)
        {
            RutasVisionLocal.ValidarDirectorio(directorio);
            Directory.CreateDirectory(directorio);
            string archivo = Path.Combine(directorio, nombre);
            if (new FileInfo(archivo).LinkTarget() is not null) throw new InvalidDataException("Archivo de exclusión inválido.");
            return archivo;
        }

        private static FileStream? Intentar(string archivo)
        {
            try { return new FileStream(archivo, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) { return null; }
        }

        private static bool AvisoVigente(string aviso)
        {
            try { return File.Exists(aviso) && DateTime.UtcNow - File.GetLastWriteTimeUtc(aviso) < VigenciaAviso; }
            catch (IOException) { return false; }
        }
    }
}
