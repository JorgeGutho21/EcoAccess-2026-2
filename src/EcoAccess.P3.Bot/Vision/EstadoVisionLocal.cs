using System;
using System.IO;
using System.Text.Json;

namespace EcoAccess.P3.Bot.Vision
{

    /// <summary>
    /// Último resultado del reconocimiento local, para el diagnóstico del administrador y del lanzador. Se guarda en
    /// <c>C:\ProgramData\EcoAccessP3\Vision\estado-local.json</c> sin imágenes, respuestas del modelo ni datos de personas:
    /// solo si funcionó, cuánto tardó y un error técnico resumido.
    /// </summary>
    public static class EstadoVisionLocal
    {
        public sealed record Estado(string Modelo, bool Exitosa, DateTime FechaUtc, long DuracionMs, string? Error, string Origen);

        public static string Archivo { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Vision", "estado-local.json");

        private static readonly object Cerrojo = new();

        /// <summary>Origen del registro: «bot» para fotos reales, «comprobacion» para la prueba con imagen de referencia.</summary>
        public static string Origen { get; set; } = "bot";

        public static void Registrar(bool exitosa, TimeSpan duracion, string? error) => Registrar(Archivo, exitosa, duracion, error);

        public static void Registrar(string archivo, bool exitosa, TimeSpan duracion, string? error)
        {
            var estado = new Estado(ClasificadorOllamaLocal.Modelo, exitosa, DateTime.UtcNow, (long)duracion.TotalMilliseconds, error, Origen);
            try
            {
                lock (Cerrojo)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(archivo)!);
                    string temporal = archivo + ".tmp";
                    File.WriteAllText(temporal, JsonSerializer.Serialize(estado));
                    Compat.MoverSobrescribiendo(temporal, archivo);
                }
            }
            catch (Exception) { /* El diagnóstico nunca interrumpe la clasificación. */ }
        }

        public static Estado? Leer(string? archivo = null)
        {
            archivo ??= Archivo;
            try { return File.Exists(archivo) ? JsonSerializer.Deserialize<Estado>(File.ReadAllText(archivo)) : null; }
            catch (Exception) { return null; }
        }
    }
}
