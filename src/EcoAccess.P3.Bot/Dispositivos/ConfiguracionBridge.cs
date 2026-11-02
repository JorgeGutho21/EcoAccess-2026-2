using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace EcoAccess.P3.Bot.Dispositivos
{

    public sealed record ConfiguracionBridge(int IdContenedor, string Dispositivo, int VersionClave,
        Uri Eventos, Uri Comandos, string DirectorioEstado)
    {
        public Uri Vigencia => new(Eventos, "/vigencia/");
        public static ConfiguracionBridge Leer(IConfiguration configuracion)
        {
            if (!int.TryParse(configuracion["Bridge:IdContenedor"], out int id) || id <= 0
                || !int.TryParse(configuracion["Bridge:VersionClave"] ?? "1", out int version) || version <= 0
                || Compat.IsNullOrWhiteSpace(configuracion["Bridge:Dispositivo"]) || Compat.IsNullOrWhiteSpace(configuracion["Bridge:DirectorioEstado"]))
                throw new ArgumentException("Configura Bridge:IdContenedor, Dispositivo, VersionClave y DirectorioEstado.");
            var eventos = new Uri(configuracion["Bridge:Eventos"] ?? "http://127.0.0.1:8182/eventos/");
            var comandos = new Uri(configuracion["Bridge:Comandos"] ?? "http://127.0.0.1:8183/comandos/");
            var c = new ConfiguracionBridge(id, configuracion["Bridge:Dispositivo"]!, version, eventos, comandos, configuracion["Bridge:DirectorioEstado"]!);
            c.Validar(); return c;
        }

        public void Validar()
        {
            DireccionDispositivoLocal.Validar(Eventos); DireccionDispositivoLocal.Validar(Comandos);
            if (Eventos.AbsolutePath != "/eventos/" || Comandos.AbsolutePath != "/comandos/" || Eventos.Port == Comandos.Port)
                throw new ArgumentException("El piloto requiere endpoints separados para eventos y comandos.");
            if (IdContenedor <= 0 || VersionClave <= 0 || Compat.IsNullOrWhiteSpace(Dispositivo) || Dispositivo.Length > 40
                || Dispositivo.Any(c => !Compat.IsAsciiLetterOrDigit(c) && c is not '_' and not '-'))
                throw new ArgumentException("Binding del bridge inválido.");
            ValidarRutaPrivada(DirectorioEstado);
        }

        internal static void ValidarRutaPrivada(string directorio)
        {
            if (!Compat.IsPathFullyQualified(directorio)) throw new ArgumentException("El estado requiere una ruta absoluta y estable.");
            string ruta = Compat.TrimEndingDirectorySeparator(Path.GetFullPath(directorio));
            if (ruta == Compat.TrimEndingDirectorySeparator(Path.GetPathRoot(ruta)!) || ruta.StartsWith(@"\\", StringComparison.Ordinal))
                throw new ArgumentException("No uses una raíz de disco ni una carpeta de red para el diario.");
            for (DirectoryInfo? carpeta = new DirectoryInfo(ruta); carpeta is not null; carpeta = carpeta.Parent)
                if (carpeta.Name.Equals("outputs", StringComparison.OrdinalIgnoreCase)
                    || Directory.Exists(Path.Combine(carpeta.FullName, ".git")) || File.Exists(Path.Combine(carpeta.FullName, ".git")))
                    throw new ArgumentException("El estado durable debe conservarse fuera de Git y outputs.");
        }
    }

    /// <summary>Una instancia por función/dispositivo; conserva el binding y nunca elimina el diario al cerrar.</summary>
    internal sealed class SesionBridgeLocal : IDisposable
    {
        private readonly FileStream _exclusiva;
        internal string Directorio { get; }
        internal SesionBridgeLocal(ConfiguracionBridge configuracion, string funcion)
        {
            configuracion.Validar();
            if (funcion is not ("bridge" or "simulador")) throw new ArgumentException("Función local inválida.");
            Directorio = Path.Combine(Path.GetFullPath(configuracion.DirectorioEstado), funcion);
            Directory.CreateDirectory(Directorio);
            _exclusiva = new FileStream(Path.Combine(Directorio, "proceso.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            try
            {
                string vinculacion = FormattableString.Invariant($"{configuracion.Dispositivo}\n{configuracion.IdContenedor}\n{configuracion.VersionClave}");
                string ruta = Path.Combine(Directorio, "vinculacion.txt");
                if (File.Exists(ruta))
                {
                    if (new FileInfo(ruta).Length > 256 || File.ReadAllText(ruta, Encoding.UTF8) != vinculacion)
                        throw new InvalidOperationException("El diario pertenece a otro binding; no se sobrescribe.");
                }
                else
                {
                    using var archivo = new FileStream(ruta, FileMode.CreateNew, FileAccess.Write, FileShare.None, 256, FileOptions.WriteThrough);
                    archivo.Write(Encoding.UTF8.GetBytes(vinculacion)); archivo.Flush(flushToDisk: true);
                }
            }
            catch { _exclusiva.Dispose(); throw; }
        }
        public void Dispose() => _exclusiva.Dispose();
    }
}
