using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Decide cómo abrir el informe avanzado. Solo abre un archivo local con Power BI Desktop: EcoAccess no publica datos
    /// ni usa enlaces públicos, y su dashboard no depende de que Power BI exista.
    /// </summary>
    internal static class AccesoPowerBi
    {
        internal const string RutaInforme = @"reportes\powerbi\EcoAccess.pbix";
        internal const string RutaGuia = @"reportes\powerbi\LEEME.md";

        internal sealed record Estado(string? Programa, string? Informe, string? Guia)
        {
            internal bool SePuedeAbrir => Programa is not null && Informe is not null;
        }

        /// <summary>Instalaciones conocidas de Power BI Desktop: instalador tradicional y Microsoft Store.</summary>
        internal static IEnumerable<string> ProgramasConocidos(string? archivosPrograma, string? archivosProgramaX86, string? datosLocales)
        {
            foreach (string? raiz in new[] { archivosPrograma, archivosProgramaX86 })
                if (!Compat.IsNullOrWhiteSpace(raiz)) yield return Path.Combine(raiz!, "Microsoft Power BI Desktop", "bin", "PBIDesktop.exe");
            if (!Compat.IsNullOrWhiteSpace(datosLocales)) yield return Path.Combine(datosLocales!, "Microsoft", "WindowsApps", "PBIDesktopStore.exe");
        }

        internal static string? BuscarRaizProyecto(string? desde)
        {
            for (string? actual = desde; !Compat.IsNullOrWhiteSpace(actual); actual = Path.GetDirectoryName(actual))
                if (File.Exists(Path.Combine(actual!, "EcoAccess.P3.sln"))) return actual;
            return null;
        }

        internal static Estado Consultar(string? raizProyecto, IEnumerable<string> programas, Func<string, bool> existe)
        {
            string? En(string relativa)
            {
                if (raizProyecto is null) return null;
                string ruta = Path.Combine(raizProyecto, relativa);
                return existe(ruta) ? ruta : null;
            }
            return new Estado(programas.FirstOrDefault(existe), En(RutaInforme), En(RutaGuia));
        }

        internal static string Explicar(Estado estado)
        {
            if (estado.SePuedeAbrir) return "Se abrirá el informe local en Power BI Desktop. Pedirá tus credenciales de la cuenta de informes de solo lectura.";
            string falta = estado.Programa is null
                ? "Power BI Desktop no está instalado en este equipo."
                : "Todavía no existe el informe EcoAccess.pbix: se construye una vez en Power BI Desktop.";
            return falta + " El dashboard de EcoAccess sigue funcionando sin él. Pasos: reportes\\powerbi\\LEEME.md.";
        }
    }
}
