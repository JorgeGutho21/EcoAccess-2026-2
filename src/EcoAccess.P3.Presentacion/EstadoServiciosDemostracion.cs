using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text.Json;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Estado local de los servicios que inició scripts/desarrollo/EcoAccess-Desarrollo.ps1: lee su manifiesto privado y comprueba
    /// que cada PID siga siendo el mismo proceso (hora de inicio). Solo lectura: no inicia, detiene ni contacta servicios.
    /// </summary>
    public static class EstadoServiciosDemostracion
    {
        public sealed record Servicio(string Nombre, bool Activo, string Detalle);
        public sealed record Resumen(IReadOnlyList<Servicio> Servicios, string? UltimaOperacion, IReadOnlyList<string> Errores);

        private static readonly string Control =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3", "Desarrollo");

        public static Resumen Consultar(string? directorioControl = null, string? secretosBot = null)
        {
            string control = directorioControl ?? Control;
            var servicios = new List<Servicio>();
            var errores = new List<string>();
            string? ultima = null;
            var procesos = LeerManifiesto(Path.Combine(control, "sesion.json"), out string? directorioSesion);
            foreach (string nombre in new[] { "Telegram", "Bridge", "Simulador" })
            {
                var registro = procesos.FirstOrDefault(p => p.Componente == nombre);
                bool activo = registro is not null && ProcesoVigente(registro.IdProceso, registro.InicioTicks);
                servicios.Add(new Servicio(nombre, activo, registro is null ? "no iniciado por el supervisor"
                    : activo ? $"proceso {registro.IdProceso} en ejecución" : "detenido"));
            }

            if (directorioSesion is not null && Directory.Exists(directorioSesion))
            {
                ultima = UltimasLineas(Path.Combine(directorioSesion, "Bridge.salida.log"), 1).FirstOrDefault();
                foreach (string nombre in new[] { "Telegram", "Bridge", "Simulador" })
                    errores.AddRange(UltimasLineas(Path.Combine(directorioSesion, nombre + ".error.log"), 3)
                        .Where(l => !l.Contains("Identidad comprobada", StringComparison.Ordinal) && !l.Contains("escuchando", StringComparison.Ordinal)
                            && !l.Contains("Bot en polling", StringComparison.Ordinal))
                        .Select(l => $"{nombre}: {l}"));
            }

            string secretos = secretosBot ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft", "UserSecrets", "ecoaccess-p3-framework", "secrets.json");
            string? proveedor = LeerValor(secretos, "Vision:Proveedor");
            servicios.Add(EstadoVisionLocal(proveedor, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "EcoAccessP3", "Vision", "estado-local.json")));
            return new Resumen(servicios, ultima, errores);
        }

        /// <summary>
        /// Reconocimiento local: si Ollama y el modelo exacto están instalados, si el servidor está en marcha y el resultado
        /// de la última inferencia real (con foto) que registró el bot o la comprobación del lanzador. No hace peticiones.
        /// </summary>
        private static Servicio EstadoVisionLocal(string? proveedor, string archivoEstado)
        {
            const string Nombre = "IA de fotos (Qwen3-VL 2B local)";
            if (proveedor != "OllamaLocal")
                return new Servicio(Nombre, false, $"modo activo: {(Compat.IsNullOrEmpty(proveedor) ? "selección manual" : proveedor)}");
            bool instalado = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama", "ollama.exe"));
            bool modelo = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".ollama", "models", "manifests", "registry.ollama.ai", "library", "qwen3-vl", "2b-instruct"));
            bool servidor = Process.GetProcessesByName("ollama").Length > 0;
            string ultima = "inferencia visual aún no comprobada";
            bool comprobada = false;
            try
            {
                if (File.Exists(archivoEstado) && new FileInfo(archivoEstado).Length <= 4096)
                {
                    using var documento = JsonDocument.Parse(File.ReadAllText(archivoEstado));
                    var raiz = documento.RootElement;
                    comprobada = raiz.GetProperty("Exitosa").GetBoolean();
                    DateTime fecha = raiz.GetProperty("FechaUtc").GetDateTime();
                    string hora = HoraColombiaTexto(fecha);
                    ultima = comprobada
                        ? $"última inferencia correcta a las {hora}, {raiz.GetProperty("DuracionMs").GetInt64() / 1000.0:N1} s"
                        : $"última inferencia falló a las {hora}: {raiz.GetProperty("Error").GetString()}";
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException or InvalidOperationException or FormatException) { }
            string detalle = $"Ollama {(instalado ? "instalado" : "no instalado")} · modelo {(modelo ? "instalado" : "no instalado")} · " +
                $"servidor {(servidor ? "en marcha" : "detenido")} · {ultima}";
            return new Servicio(Nombre, instalado && modelo && servidor && comprobada, detalle);
        }

        private static string HoraColombiaTexto(DateTime fechaUtc) =>
            new DateTimeOffset(DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc)).ToOffset(TimeSpan.FromHours(-5)).ToString("HH:mm");

        private static string? LeerValor(string ruta, string clave)
        {
            try
            {
                if (!File.Exists(ruta) || new FileInfo(ruta).Length > 1024 * 1024) return null;
                using var documento = JsonDocument.Parse(File.ReadAllText(ruta));
                return Valor(documento.RootElement, clave);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException) { return null; }
        }

        private sealed record Registro(string Componente, int IdProceso, long InicioTicks);

        private static List<Registro> LeerManifiesto(string ruta, out string? directorio)
        {
            directorio = null;
            var lista = new List<Registro>();
            try
            {
                if (!File.Exists(ruta) || new FileInfo(ruta).Length > 16384) return lista;
                using var documento = JsonDocument.Parse(File.ReadAllText(ruta));
                directorio = documento.RootElement.GetProperty("Directorio").GetString();
                foreach (var p in documento.RootElement.GetProperty("Procesos").EnumerateArray())
                    lista.Add(new Registro(p.GetProperty("Componente").GetString() ?? "", p.GetProperty("IdProceso").GetInt32(), p.GetProperty("InicioTicks").GetInt64()));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException or InvalidOperationException)
            {
                lista.Clear();
            }
            return lista;
        }

        private static bool ProcesoVigente(int id, long inicioTicks)
        {
            try
            {
                using var proceso = Process.GetProcessById(id);
                return proceso.StartTime.ToUniversalTime().Ticks == inicioTicks;
            }
            catch (Exception e) when (e is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }

        /// <summary>User Secrets admite «Seccion:Clave» plana o anidada.</summary>
        private static string? Valor(JsonElement raiz, string clave)
        {
            if (raiz.TryGetProperty(clave, out var plano) && plano.ValueKind == JsonValueKind.String) return plano.GetString();
            JsonElement nodo = raiz;
            foreach (string parte in clave.Split(':'))
                if (nodo.ValueKind != JsonValueKind.Object || !nodo.TryGetProperty(parte, out nodo)) return null;
            return nodo.ValueKind == JsonValueKind.String ? nodo.GetString() : null;
        }

        private static IEnumerable<string> UltimasLineas(string ruta, int cantidad)
        {
            try
            {
                using var flujo = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (flujo.Length > 64 * 1024) flujo.Seek(-64 * 1024, SeekOrigin.End);
                using var lector = new StreamReader(flujo);
                return lector.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).Where(l => l.Length > 0).TakeLast(cantidad).ToList();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return Array.Empty<string>(); }
        }
    }
}
