using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    /// <summary>Equivalentes para .NET Framework 4.8 de utilidades estáticas que solo existen en .NET moderno.</summary>
    public static class Compat
    {
        private static readonly ThreadLocal<Random> AleatorioPorHilo =
            new ThreadLocal<Random>(() => new Random(RandomInt32(int.MaxValue)));

        public static Random RandomShared => AleatorioPorHilo.Value!;
        public static int ProcessId { get { using var p = Process.GetCurrentProcess(); return p.Id; } }

        public static bool IsNullOrWhiteSpace([NotNullWhen(false)] string? valor) => string.IsNullOrWhiteSpace(valor);

        public static bool IsNullOrEmpty([NotNullWhen(false)] string? valor) => string.IsNullOrEmpty(valor);

        public static void ThrowIfNull([NotNull] object? valor, string nombre)
        {
            if (valor is null) throw new ArgumentNullException(nombre);
        }

        public static void ThrowIfNullOrWhiteSpace(string? valor, string nombre)
        {
            if (valor is null) throw new ArgumentNullException(nombre);
            if (string.IsNullOrWhiteSpace(valor)) throw new ArgumentException("El valor no puede estar vacío.", nombre);
        }

        public static void ThrowIfLessThan<T>(T valor, T minimo, string nombre) where T : IComparable<T>
        {
            if (valor.CompareTo(minimo) < 0) throw new ArgumentOutOfRangeException(nombre, valor, null);
        }

        public static void ThrowIfGreaterThan<T>(T valor, T maximo, string nombre) where T : IComparable<T>
        {
            if (valor.CompareTo(maximo) > 0) throw new ArgumentOutOfRangeException(nombre, valor, null);
        }

        public static void ThrowIfNegativeOrZero(int valor, string nombre)
        {
            if (valor <= 0) throw new ArgumentOutOfRangeException(nombre, valor, null);
        }

        public static void ThrowIfDisposed(bool liberado, object instancia)
        {
            if (liberado) throw new ObjectDisposedException(instancia.GetType().FullName);
        }

        public static bool IsAsciiLetterOrDigit(char c) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        public static bool IsAsciiDigit(char c) => c >= '0' && c <= '9';
        public static bool IsAsciiLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        public static bool IsAsciiHexDigit(char c) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

        public static string ToHexString(byte[] datos)
        {
            var sb = new StringBuilder(datos.Length * 2);
            foreach (var b in datos) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }

        public static string ToHexStringLower(byte[] datos) => ToHexString(datos).ToLowerInvariant();

        public static byte[] FromHexString(string texto)
        {
            if (texto.Length % 2 != 0) throw new FormatException("Longitud hexadecimal inválida.");
            var r = new byte[texto.Length / 2];
            for (var i = 0; i < r.Length; i++)
            {
                if (!IsAsciiHexDigit(texto[2 * i]) || !IsAsciiHexDigit(texto[2 * i + 1])) throw new FormatException("Carácter hexadecimal inválido.");
                r[i] = Convert.ToByte(texto.Substring(2 * i, 2), 16);
            }
            return r;
        }

        public static byte[] RandomGetBytes(int cantidad)
        {
            var r = new byte[cantidad];
            RandomFill(r);
            return r;
        }

        public static void RandomFill(byte[] destino)
        {
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(destino);
        }

        public static int RandomGetInt32(int maximoExclusivo) => RandomInt32(maximoExclusivo);
        public static int RandomGetInt32(int minimo, int maximoExclusivo) => minimo + RandomInt32(maximoExclusivo - minimo);

        private static int RandomInt32(int maximoExclusivo)
        {
            if (maximoExclusivo <= 0) throw new ArgumentOutOfRangeException(nameof(maximoExclusivo));
            var limite = uint.MaxValue - (uint.MaxValue % (uint)maximoExclusivo);
            var buffer = new byte[4];
            uint valor;
            do
            {
                RandomFill(buffer);
                valor = BitConverter.ToUInt32(buffer, 0);
            } while (valor >= limite);
            return (int)(valor % (uint)maximoExclusivo);
        }

        public static string RandomGetString(string alfabeto, int longitud)
        {
            var sb = new StringBuilder(longitud);
            for (var i = 0; i < longitud; i++) sb.Append(alfabeto[RandomInt32(alfabeto.Length)]);
            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            var diferencia = 0;
            for (var i = 0; i < a.Length; i++) diferencia |= a[i] ^ b[i];
            return diferencia == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void ZeroMemory(byte[] datos) => Array.Clear(datos, 0, datos.Length);

        public static byte[] Sha256(byte[] datos)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(datos);
        }

        public static byte[] HmacSha256(byte[] clave, byte[] datos)
        {
            using var hmac = new HMACSHA256(clave);
            return hmac.ComputeHash(datos);
        }

        /// <summary>
        /// PBKDF2 igual al estándar, con las mismas iteraciones. En .NET Framework, Rfc2898DeriveBytes es código administrado y
        /// tarda mucho más que la implementación nativa de Windows; con varias personas a la vez agotaba el plazo de la página.
        /// El resultado es idéntico, así que las credenciales ya guardadas siguen valiendo.
        /// </summary>
        public static byte[] Pbkdf2(byte[] clave, byte[] sal, int iteraciones, HashAlgorithmName algoritmo, int longitud)
        {
            if (algoritmo == HashAlgorithmName.SHA256 && Pbkdf2Nativo(clave, sal, iteraciones, longitud) is { } nativo) return nativo;
            return Pbkdf2Administrado(clave, sal, iteraciones, algoritmo, longitud);
        }

        public static byte[] Pbkdf2Administrado(byte[] clave, byte[] sal, int iteraciones, HashAlgorithmName algoritmo, int longitud)
        {
            using var derivador = new Rfc2898DeriveBytes(clave, sal, iteraciones, algoritmo);
            return derivador.GetBytes(longitud);
        }

        private const uint BanderaHmac = 0x00000008;

        [System.Runtime.InteropServices.DllImport("bcrypt.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int BCryptOpenAlgorithmProvider(out IntPtr algoritmo, string nombre, string? proveedor, uint banderas);

        [System.Runtime.InteropServices.DllImport("bcrypt.dll")]
        private static extern int BCryptCloseAlgorithmProvider(IntPtr algoritmo, uint banderas);

        [System.Runtime.InteropServices.DllImport("bcrypt.dll")]
        private static extern int BCryptDeriveKeyPBKDF2(IntPtr algoritmo, byte[] clave, int largoClave, byte[] sal, int largoSal, ulong iteraciones, byte[] salida, int largoSalida, uint banderas);

        /// <summary>Null si Windows no lo permite: quien llama usa entonces la versión administrada.</summary>
        private static byte[]? Pbkdf2Nativo(byte[] clave, byte[] sal, int iteraciones, int longitud)
        {
            try
            {
                if (BCryptOpenAlgorithmProvider(out IntPtr algoritmo, "SHA256", null, BanderaHmac) != 0) return null;
                try
                {
                    var salida = new byte[longitud];
                    return BCryptDeriveKeyPBKDF2(algoritmo, clave, clave.Length, sal, sal.Length, (ulong)iteraciones, salida, longitud, 0) == 0 ? salida : null;
                }
                finally { BCryptCloseAlgorithmProvider(algoritmo, 0); }
            }
            catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException) { return null; }
        }

        public static byte[] Pbkdf2(string clave, byte[] sal, int iteraciones, HashAlgorithmName algoritmo, int longitud) =>
            Pbkdf2(Encoding.UTF8.GetBytes(clave), sal, iteraciones, algoritmo, longitud);

        public static async Task<byte[]> ReadAllBytesAsync(string ruta, CancellationToken ct = default)
        {
            using var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
            var r = new byte[fs.Length];
            var total = 0;
            while (total < r.Length)
            {
                var n = await fs.ReadAsync(r, total, r.Length - total, ct).ConfigureAwait(false);
                if (n == 0) throw new EndOfStreamException();
                total += n;
            }
            return r;
        }

        public static async Task<string> ReadAllTextAsync(string ruta, CancellationToken ct = default)
        {
            using var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
            using var lector = new StreamReader(fs, Encoding.UTF8, true);
            ct.ThrowIfCancellationRequested();
            return await lector.ReadToEndAsync().ConfigureAwait(false);
        }

        public static async Task<string[]> ReadAllLinesAsync(string ruta, CancellationToken ct = default)
        {
            var texto = await ReadAllTextAsync(ruta, ct).ConfigureAwait(false);
            var lineas = new List<string>();
            using var lector = new StringReader(texto);
            string? linea;
            while ((linea = lector.ReadLine()) != null) lineas.Add(linea);
            return lineas.ToArray();
        }

        public static async Task WriteAllBytesAsync(string ruta, byte[] datos, CancellationToken ct = default)
        {
            using var fs = new FileStream(ruta, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
            await fs.WriteAsync(datos, 0, datos.Length, ct).ConfigureAwait(false);
        }

        public static Task WriteAllTextAsync(string ruta, string texto, CancellationToken ct = default) =>
            WriteAllBytesAsync(ruta, new UTF8Encoding(false).GetBytes(texto), ct);

        public static Task WriteAllTextAsync(string ruta, string texto, Encoding codificacion, CancellationToken ct = default) =>
            WriteAllBytesAsync(ruta, codificacion.GetPreamble().Concat(codificacion.GetBytes(texto)).ToArray(), ct);

        public static async Task AppendAllTextAsync(string ruta, string texto, CancellationToken ct = default)
        {
            var datos = new UTF8Encoding(false).GetBytes(texto);
            using var fs = new FileStream(ruta, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, true);
            await fs.WriteAsync(datos, 0, datos.Length, ct).ConfigureAwait(false);
        }

        /// <summary>Reemplaza el destino de forma atómica cuando ya existe; equivale a File.Move con sobrescritura.</summary>
        public static void MoverSobrescribiendo(string origen, string destino)
        {
            if (File.Exists(destino)) File.Replace(origen, destino, null, true);
            else File.Move(origen, destino);
        }

        public static bool TryCreateMailAddress(string? texto, [NotNullWhen(true)] out MailAddress? direccion)
        {
            direccion = null;
            if (string.IsNullOrEmpty(texto)) return false;
            try
            {
                direccion = new MailAddress(texto);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static bool IsDefined<T>(T valor) where T : struct, Enum => Enum.IsDefined(typeof(T), valor);
        public static string[] GetNames<T>() where T : struct, Enum => Enum.GetNames(typeof(T));
        public static T[] GetValues<T>() where T : struct, Enum => (T[])Enum.GetValues(typeof(T));
        public static T ParseEnum<T>(string texto) where T : struct, Enum => (T)Enum.Parse(typeof(T), texto);
        public static T ParseEnum<T>(string texto, bool ignorarMayusculas) where T : struct, Enum => (T)Enum.Parse(typeof(T), texto, ignorarMayusculas);

        public static bool IsPathFullyQualified(string ruta)
        {
            if (ruta.Length < 3) return false;
            if (ruta.StartsWith(@"\\", StringComparison.Ordinal)) return true;
            return IsAsciiLetter(ruta[0]) && ruta[1] == ':' && (ruta[2] == '\\' || ruta[2] == '/');
        }

        public static string TrimEndingDirectorySeparator(string ruta)
        {
            var raiz = Path.GetPathRoot(ruta);
            return ruta.Length > (raiz?.Length ?? 0) && (ruta[ruta.Length - 1] == Path.DirectorySeparatorChar || ruta[ruta.Length - 1] == Path.AltDirectorySeparatorChar)
                ? ruta.Substring(0, ruta.Length - 1)
                : ruta;
        }

        public static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
        public static long Clamp(long v, long min, long max) => v < min ? min : v > max ? max : v;
        public static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
        public static double Clamp(double v, double min, double max) => v < min ? min : v > max ? max : v;
        public static decimal Clamp(decimal v, decimal min, decimal max) => v < min ? min : v > max ? max : v;

        public static TimeSpan GetElapsedTime(long marcaInicial) =>
            TimeSpan.FromSeconds((Stopwatch.GetTimestamp() - marcaInicial) / (double)Stopwatch.Frequency);
    }
}
