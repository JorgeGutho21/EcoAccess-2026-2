using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    /// <summary>Sobrecargas de cadenas de .NET moderno; se resuelven como métodos de extensión.</summary>
    public static class ExtensionesCadena
    {
        public static bool Contains(this string s, char c) => s.IndexOf(c) >= 0;
        public static bool Contains(this string s, char c, StringComparison modo) => s.IndexOf(c.ToString(), modo) >= 0;
        public static bool Contains(this string s, string valor, StringComparison modo) => s.IndexOf(valor, modo) >= 0;
        public static bool StartsWith(this string s, char c) => s.Length > 0 && s[0] == c;
        public static bool EndsWith(this string s, char c) => s.Length > 0 && s[s.Length - 1] == c;
        public static int IndexOf(this string s, char c, StringComparison modo) => s.IndexOf(c.ToString(), modo);
        public static string[] Split(this string s, char c, StringSplitOptions opciones = StringSplitOptions.None) => s.Split(new[] { c }, opciones);
        public static string[] Split(this string s, char c, int cantidad, StringSplitOptions opciones = StringSplitOptions.None) => s.Split(new[] { c }, cantidad, opciones);
        public static string[] Split(this string s, string separador, StringSplitOptions opciones = StringSplitOptions.None) => s.Split(new[] { separador }, opciones);
        public static string[] Split(this string s, string separador, int cantidad, StringSplitOptions opciones = StringSplitOptions.None) => s.Split(new[] { separador }, cantidad, opciones);
        public static string Trim(this string s, char c) => s.Trim(new[] { c });

        public static string Replace(this string s, string anterior, string nuevo, StringComparison modo)
        {
            if (modo == StringComparison.Ordinal) return s.Replace(anterior, nuevo);
            var r = new Text.StringBuilder();
            var i = 0;
            while (true)
            {
                var j = s.IndexOf(anterior, i, modo);
                if (j < 0) break;
                r.Append(s, i, j - i).Append(nuevo);
                i = j + anterior.Length;
            }
            return r.Append(s, i, s.Length - i).ToString();
        }

        public static Text.StringBuilder AppendJoin(this Text.StringBuilder sb, char separador, IEnumerable<string> valores) =>
            sb.Append(string.Join(separador.ToString(), valores));

        public static Text.StringBuilder AppendJoin(this Text.StringBuilder sb, string separador, IEnumerable<string> valores) =>
            sb.Append(string.Join(separador, valores));

        public static int GetHashCode(this string s, StringComparison modo) =>
            (modo == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).GetHashCode(s);
    }
}

namespace System.Linq
{
    /// <summary>Operadores de LINQ añadidos en versiones posteriores de .NET.</summary>
    public static class ExtensionesLinq
    {
        public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> f) => f.OrderBy(x => x);
        public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> f, IComparer<T> c) => f.OrderBy(x => x, c);
        public static IOrderedEnumerable<T> OrderDescending<T>(this IEnumerable<T> f) => f.OrderByDescending(x => x);
        public static T? MaxBy<T, K>(this IEnumerable<T> f, Func<T, K> clave) => f.OrderByDescending(clave).FirstOrDefault();
        public static T? MinBy<T, K>(this IEnumerable<T> f, Func<T, K> clave) => f.OrderBy(clave).FirstOrDefault();

        public static IEnumerable<T> DistinctBy<T, K>(this IEnumerable<T> f, Func<T, K> clave)
        {
            var vistos = new HashSet<K>();
            foreach (var x in f) if (vistos.Add(clave(x))) yield return x;
        }

        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> f, int n)
        {
            var l = f as IList<T> ?? f.ToList();
            return l.Skip(Math.Max(0, l.Count - n));
        }

        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> f, int n)
        {
            var l = f as IList<T> ?? f.ToList();
            return l.Take(Math.Max(0, l.Count - n));
        }

        public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> f, int tamano)
        {
            var actual = new List<T>(tamano);
            foreach (var x in f)
            {
                actual.Add(x);
                if (actual.Count == tamano) { yield return actual.ToArray(); actual.Clear(); }
            }
            if (actual.Count > 0) yield return actual.ToArray();
        }
    }
}

namespace System.Collections.Generic
{
    /// <summary>Utilidades de diccionarios de .NET moderno.</summary>
    public static class ExtensionesDiccionario
    {
        public static V? GetValueOrDefault<K, V>(this IReadOnlyDictionary<K, V> d, K clave) where K : notnull =>
            d.TryGetValue(clave, out var v) ? v : default;

        public static V GetValueOrDefault<K, V>(this IReadOnlyDictionary<K, V> d, K clave, V omision) where K : notnull =>
            d.TryGetValue(clave, out var v) ? v : omision;

        public static V? GetValueOrDefault<K, V>(this Dictionary<K, V> d, K clave) where K : notnull =>
            d.TryGetValue(clave, out var v) ? v : default;

        public static V GetValueOrDefault<K, V>(this Dictionary<K, V> d, K clave, V omision) where K : notnull =>
            d.TryGetValue(clave, out var v) ? v : omision;

        public static bool Remove<K, V>(this Dictionary<K, V> d, K clave, out V valor) where K : notnull
        {
            if (!d.TryGetValue(clave, out valor!)) return false;
            d.Remove(clave);
            return true;
        }

        public static bool TryRemove<K, V>(this System.Collections.Concurrent.ConcurrentDictionary<K, V> d, KeyValuePair<K, V> par) where K : notnull =>
            ((ICollection<KeyValuePair<K, V>>)d).Remove(par);

        public static long NextInt64(this Random aleatorio, long maximoExclusivo) => (long)(aleatorio.NextDouble() * maximoExclusivo);

        public static void Deconstruct<K, V>(this KeyValuePair<K, V> par, out K clave, out V valor)
        {
            clave = par.Key;
            valor = par.Value;
        }

        public static bool TryDequeue<T>(this Queue<T> cola, out T valor)
        {
            if (cola.Count == 0)
            {
                valor = default!;
                return false;
            }
            valor = cola.Dequeue();
            return true;
        }

        public static bool TryAdd<K, V>(this Dictionary<K, V> d, K clave, V valor) where K : notnull
        {
            if (d.ContainsKey(clave)) return false;
            d.Add(clave, valor);
            return true;
        }
    }
}

namespace System.Threading.Tasks
{
    /// <summary>Espera con tiempo límite o cancelación sobre tareas que no aceptan token, como en .NET 6+.</summary>
    public static class ExtensionesTarea
    {
        public static async Task WaitAsync(this Task t, CancellationToken ct)
        {
            if (t.IsCompleted || !ct.CanBeCanceled) { await t.ConfigureAwait(false); return; }
            var cancelacion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (ct.Register(() => cancelacion.TrySetResult(true)))
            {
                if (await Task.WhenAny(t, cancelacion.Task).ConfigureAwait(false) != t) throw new OperationCanceledException(ct);
            }
            await t.ConfigureAwait(false);
        }

        public static async Task<T> WaitAsync<T>(this Task<T> t, CancellationToken ct)
        {
            await ((Task)t).WaitAsync(ct).ConfigureAwait(false);
            return await t.ConfigureAwait(false);
        }

        public static async Task WaitAsync(this Task t, TimeSpan limite, CancellationToken ct = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var ganador = await Task.WhenAny(t, Task.Delay(limite, cts.Token)).ConfigureAwait(false);
            if (ganador != t)
            {
                ct.ThrowIfCancellationRequested();
                throw new TimeoutException();
            }
            cts.Cancel();
            await t.ConfigureAwait(false);
        }

        public static async Task<T> WaitAsync<T>(this Task<T> t, TimeSpan limite, CancellationToken ct = default)
        {
            await ((Task)t).WaitAsync(limite, ct).ConfigureAwait(false);
            return await t.ConfigureAwait(false);
        }

        public static Task CancelAsync(this CancellationTokenSource cts)
        {
            cts.Cancel();
            return Task.CompletedTask;
        }
    }
}

namespace System.Diagnostics
{
    /// <summary>Espera asíncrona de procesos, como en .NET 5+.</summary>
    public static class ExtensionesProceso
    {
        public static Task WaitForExitAsync(this Process proceso, CancellationToken ct = default) =>
            Task.Run(() => proceso.WaitForExit(), CancellationToken.None).WaitAsync(ct);
    }
}

namespace System.IO
{
    /// <summary>Lectura de flujos y comprobación de enlaces en sistemas de archivos.</summary>
    public static class ExtensionesArchivos
    {
        public static string? LinkTarget(this FileSystemInfo info) =>
            info.Exists && (info.Attributes & FileAttributes.ReparsePoint) != 0 ? info.FullName : null;

        public static void ReadExactly(this Stream s, byte[] destino, int inicio, int cantidad)
        {
            var total = 0;
            while (total < cantidad)
            {
                var n = s.Read(destino, inicio + total, cantidad - total);
                if (n == 0) throw new EndOfStreamException();
                total += n;
            }
        }

        public static void Write(this Stream s, byte[] datos) => s.Write(datos, 0, datos.Length);

        public static ValueTask<int> ReadAsync(this Stream s, Memory<byte> destino, CancellationToken ct = default)
        {
            if (!MemoryMarshal.TryGetArray<byte>(destino, out var segmento)) throw new NotSupportedException("Se requiere un búfer respaldado por arreglo.");
            return new ValueTask<int>(s.ReadAsync(segmento.Array!, segmento.Offset, segmento.Count, ct));
        }

        public static ValueTask WriteAsync(this Stream s, ReadOnlyMemory<byte> datos, CancellationToken ct = default)
        {
            if (!MemoryMarshal.TryGetArray(datos, out var segmento)) throw new NotSupportedException("Se requiere un búfer respaldado por arreglo.");
            return new ValueTask(s.WriteAsync(segmento.Array!, segmento.Offset, segmento.Count, ct));
        }

        public static Task<string?> ReadLineAsync(this TextReader lector, CancellationToken ct) =>
            lector.ReadLineAsync().WaitAsync(ct)!;

        public static void ReadExactly(this Stream s, byte[] destino) => s.ReadExactly(destino, 0, destino.Length);
    }
}

namespace System.Net.Http
{
    /// <summary>Lectura de contenido HTTP con token de cancelación.</summary>
    public static class ExtensionesHttp
    {
        public static Task<string> ReadAsStringAsync(this HttpContent c, CancellationToken ct) => c.ReadAsStringAsync().WaitAsync(ct);
        public static Task<Stream> ReadAsStreamAsync(this HttpContent c, CancellationToken ct) => c.ReadAsStreamAsync().WaitAsync(ct);
        public static Task<byte[]> ReadAsByteArrayAsync(this HttpContent c, CancellationToken ct) => c.ReadAsByteArrayAsync().WaitAsync(ct);
    }
}
