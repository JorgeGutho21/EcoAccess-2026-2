using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace EcoAccess.P3.Bot.Dispositivos
{

    public sealed record PaquetePendiente(string Nombre, byte[] Datos);

    /// <summary>Cola local de bytes inmutables: guardar antes de acusar, retirar solo tras confirmación autenticada.</summary>
    public sealed class ColaPaquetesDispositivo
    {
        public const int MaximoPendientes = 256;
        private static readonly Regex NombreValido = new(@"^\d{19}-[A-F0-9]{64}\.json$", RegexOptions.CultureInvariant);
        private readonly string _directorio;
        private readonly string _revision;
        private readonly object _bloqueo = new();
        private long _secuencia;

        public ColaPaquetesDispositivo(string directorio)
        {
            if (!Compat.IsPathFullyQualified(directorio)) throw new ArgumentException("La cola requiere una ruta absoluta estable.");
            _directorio = Compat.TrimEndingDirectorySeparator(Path.GetFullPath(directorio)); _revision = Path.Combine(_directorio, "requiere-revision");
            Directory.CreateDirectory(_directorio); Directory.CreateDirectory(_revision);
            foreach (string archivo in Archivos(_directorio).Concat(Archivos(_revision)))
            {
                long numero = long.Parse(Path.GetFileName(archivo).Substring(0, 19), CultureInfo.InvariantCulture);
                _secuencia = Math.Max(_secuencia, numero);
            }
        }

        public bool Agregar(byte[] paquete)
        {
            Compat.ThrowIfNull(paquete, nameof(paquete));
            if (paquete.Length is 0 or > ProtocoloDispositivoHmac.MaximoPaquete) throw new ArgumentException("Paquete inválido.");
            byte[] copia = paquete.ToArray(); string hash = Compat.ToHexString(Compat.Sha256(copia));
            lock (_bloqueo)
            {
                string[] pendientes = Archivos(_directorio), revision = Archivos(_revision);
                if (revision.Any(p => p.EndsWith("-" + hash + ".json", StringComparison.Ordinal))) return false;
                if (pendientes.Any(p => p.EndsWith("-" + hash + ".json", StringComparison.Ordinal))) return true;
                if (pendientes.Length >= MaximoPendientes || revision.Length >= MaximoPendientes) return false;
                string nombre = checked(++_secuencia).ToString("D19", CultureInfo.InvariantCulture) + "-" + hash + ".json";
                string temporal = Path.Combine(_directorio, Guid.NewGuid().ToString("N") + ".tmp");
                using (var archivo = new FileStream(temporal, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    archivo.Write(copia); archivo.Flush(flushToDisk: true);
                }
                // La publicación es por renombrado dentro de la misma carpeta; nunca se sobreescribe otro mensaje.
                File.Move(temporal, Path.Combine(_directorio, nombre)); return true;
            }
        }

        public PaquetePendiente? Primero()
        {
            lock (_bloqueo)
            {
                string? ruta = Archivos(_directorio).Order(StringComparer.Ordinal).FirstOrDefault();
                if (ruta is null) return null;
                using var archivo = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (archivo.Length is 0 or > ProtocoloDispositivoHmac.MaximoPaquete) throw new InvalidDataException("Paquete local alterado; requiere revisión.");
                byte[] bytes = new byte[(int)archivo.Length]; archivo.ReadExactly(bytes);
                string nombre = Path.GetFileName(ruta);
                if (!nombre.EndsWith("-" + Compat.ToHexString(Compat.Sha256(bytes)) + ".json", StringComparison.Ordinal))
                    throw new InvalidDataException("Hash local incorrecto; requiere revisión.");
                return new PaquetePendiente(nombre, bytes);
            }
        }

        public void Confirmar(PaquetePendiente paquete)
        {
            lock (_bloqueo) File.Delete(Ruta(paquete.Nombre));
        }

        public void ConservarParaRevision(PaquetePendiente paquete)
        {
            lock (_bloqueo)
            {
                if (Archivos(_revision).Length >= MaximoPendientes) throw new InvalidOperationException("Revisión local llena; no se eliminan registros.");
                File.Move(Ruta(paquete.Nombre), Path.Combine(_revision, paquete.Nombre));
            }
        }

        public int Pendientes { get { lock (_bloqueo) return Archivos(_directorio).Length; } }
        public int EnRevision { get { lock (_bloqueo) return Archivos(_revision).Length; } }

        private string Ruta(string nombre)
        {
            if (!NombreValido.IsMatch(nombre)) throw new ArgumentException("Nombre de paquete inválido.");
            string ruta = Path.GetFullPath(Path.Combine(_directorio, nombre));
            if (Path.GetDirectoryName(ruta) != _directorio) throw new ArgumentException("Paquete fuera de la cola.");
            return ruta;
        }

        private static string[] Archivos(string directorio)
        {
            var archivos = Directory.EnumerateFiles(directorio, "*.json").Take(MaximoPendientes + 1).ToArray();
            if (archivos.Length > MaximoPendientes || archivos.Any(a => !NombreValido.IsMatch(Path.GetFileName(a))))
                throw new InvalidDataException("Cola local inválida o excedida; se conserva para revisión.");
            return archivos;
        }
    }
}
