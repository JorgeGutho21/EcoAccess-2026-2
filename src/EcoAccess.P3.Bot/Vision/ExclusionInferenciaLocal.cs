using System;
using System.IO;

namespace EcoAccess.P3.Bot.Vision
{

    /// <summary>Exclusión entre procesos EcoAccess; no intenta detener modelos o procesos ajenos.</summary>
    internal static class ExclusionInferenciaLocal
    {
        internal static FileStream Adquirir(string directorio)
        {
            RutasVisionLocal.ValidarDirectorio(directorio);
            Directory.CreateDirectory(directorio);
            string archivo = Path.Combine(directorio, "vision-inferencia.lock");
            if (new FileInfo(archivo).LinkTarget() is not null) throw new InvalidDataException("Archivo de exclusión inválido.");
            try { return new FileStream(archivo, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) { throw new InvalidOperationException("Otra inferencia local de EcoAccess está activa. La selección manual sigue disponible."); }
        }
    }
}
