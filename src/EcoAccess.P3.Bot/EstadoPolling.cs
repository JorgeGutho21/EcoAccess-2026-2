using System;
using System.IO;
using System.Text.Json;

namespace EcoAccess.P3.Bot
{

    /// <summary>
    /// Latido del consumidor de Telegram. Distingue un bot que de verdad recibe mensajes de uno con el token válido pero
    /// detenido o bloqueado. Sin token, textos ni datos de personas.
    /// </summary>
    public sealed record EstadoPolling(DateTime InicioUtc, DateTime UltimoContactoUtc, long UltimoUpdateId, long Atendidos,
        int FallosSeguidos, string? UltimoError, int Proceso, DateTime CompilacionUtc)
    {
        public static string ArchivoPredeterminado { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Telegram", "estado-polling.json");

        /// <summary>Un fallo al escribir el latido nunca interrumpe la atención de mensajes.</summary>
        public void Guardar(string? archivo = null)
        {
            try
            {
                archivo ??= ArchivoPredeterminado;
                Directory.CreateDirectory(Path.GetDirectoryName(archivo)!);
                string temporal = archivo + ".tmp";
                File.WriteAllText(temporal, JsonSerializer.Serialize(this));
                Compat.MoverSobrescribiendo(temporal, archivo);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }
    }
}
