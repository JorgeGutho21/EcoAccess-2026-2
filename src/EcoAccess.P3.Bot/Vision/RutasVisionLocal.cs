using System.IO;
using EcoAccess.P3.Bot.Dispositivos;

namespace EcoAccess.P3.Bot.Vision
{

    internal static class RutasVisionLocal
    {
        internal static void ValidarDirectorio(string directorio)
        {
            ConfiguracionBridge.ValidarRutaPrivada(directorio);
            for (DirectoryInfo? d = new(Path.GetFullPath(directorio)); d is not null; d = d.Parent)
                if (d.Exists && (d.LinkTarget() is not null || (d.Attributes & FileAttributes.ReparsePoint) != 0))
                    throw new InvalidDataException("No se permiten enlaces ni junctions en rutas de visión local.");
        }
    }
}
