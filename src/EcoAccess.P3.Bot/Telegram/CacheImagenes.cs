using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EcoAccess.P3.Bot.Telegram
{
    /// <summary>
    /// file_id de Telegram de las imágenes oficiales que este bot ya envió (son válidos solo para el mismo bot).
    /// Se guardan en %ProgramData%\EcoAccessP3Framework\Telegram\imagenes-file-id.json para sobrevivir a reinicios.
    /// No contiene secretos ni datos personales: solo la clave lógica de la imagen y su identificador público de archivo.
    /// </summary>
    internal static class CacheImagenes
    {
        private static readonly object Bloqueo = new();
        private static readonly string Archivo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Telegram", "imagenes-file-id.json");
        private static Dictionary<string, string>? _ids;

        public static string? Obtener(string clave) { lock (Bloqueo) return Cargar().TryGetValue(clave, out var id) ? id : null; }

        public static void Guardar(string clave, string fileId) { lock (Bloqueo) { Cargar()[clave] = fileId; Escribir(); } }

        public static void Olvidar(string clave) { lock (Bloqueo) { if (Cargar().Remove(clave)) Escribir(); } }

        private static Dictionary<string, string> Cargar()
        {
            if (_ids is not null) return _ids;
            try { _ids = File.Exists(Archivo) ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Archivo)) : null; }
            catch (Exception) { _ids = null; }
            return _ids ??= new Dictionary<string, string>(StringComparer.Ordinal);
        }

        private static void Escribir()
        {
            try { Directory.CreateDirectory(Path.GetDirectoryName(Archivo)!); File.WriteAllText(Archivo, JsonSerializer.Serialize(_ids)); }
            catch (Exception) { /* sin caché solo se vuelve a subir la imagen */ }
        }
    }
}
