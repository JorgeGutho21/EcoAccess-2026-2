// Proyecto EcoAccess P3 · Panel web de control y calibración de las tres canecas.
// Uso: EcoAccess.P3.PanelCanecas.exe [--puerto COM3] [--http 8090]
//   http://localhost:8090/       en este equipo (sin clave)
//   http://<ip-del-equipo>:8090/ en la red local, con la clave de %ProgramData%\EcoAccessP3Framework\Panel\clave.txt
//                                (requiere una vez, como administrador: netsh http add urlacl url=http://+:8090/ user=<usuario>)
// Los motores solo se mueven con una sesión supervisada habilitada DESDE ESTE EQUIPO (botón del panel, 2 horas).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace EcoAccess.P3.PanelCanecas
{
    internal static class Program
    {
        private static readonly JavaScriptSerializer Json = new();
        private static readonly List<object> Historial = new();
        private static EnlaceEsp32 _esp32 = null!;
        private static string _clave = "";
        private static DateTime _sesionHastaUtc = DateTime.MinValue;
        private static string _wwwroot = "";

        private static int Main(string[] args)
        {
            string puerto = Valor(args, "--puerto") ?? "COM3";
            int http = int.TryParse(Valor(args, "--http"), out int h) && h is > 1024 and < 65536 ? h : 8090;
            using var unica = new Mutex(false, @"Global\EcoAccessP3PanelCanecas");
            if (!unica.WaitOne(0)) { Console.Error.WriteLine("El panel ya está en ejecución."); return 3; }

            string carpeta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Panel");
            Directory.CreateDirectory(carpeta);
            string archivoClave = Path.Combine(carpeta, "clave.txt");
            if (!File.Exists(archivoClave))
            {
                var bytes = new byte[12]; using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
                File.WriteAllText(archivoClave, string.Concat(bytes.Select(b => b.ToString("x2"))));
            }
            _clave = File.ReadAllText(archivoClave).Trim();
            _wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");

            _esp32 = new EnlaceEsp32(puerto);
            _esp32.Evento += linea => Anotar("esp32", linea);

            var servidor = new HttpListener();
            bool red = true;
            servidor.Prefixes.Add($"http://+:{http}/");
            try { servidor.Start(); }
            catch (HttpListenerException)
            {
                red = false;
                servidor = new HttpListener();
                servidor.Prefixes.Add($"http://localhost:{http}/");
                servidor.Prefixes.Add($"http://127.0.0.1:{http}/");
                servidor.Start();
            }
            Console.WriteLine($"Panel de canecas: http://localhost:{http}/" + (red ? $" (también en la red local, con clave)" : " (solo este equipo; ver netsh en Program.cs para la red local)"));
            Anotar("panel", red ? "iniciado con acceso de red local" : "iniciado solo en este equipo");
            while (true)
            {
                HttpListenerContext ctx;
                try { ctx = servidor.GetContext(); } catch (HttpListenerException) { break; }
                _ = Task.Run(() => AtenderAsync(ctx));
            }
            return 0;
        }

        private static async Task AtenderAsync(HttpListenerContext ctx)
        {
            var req = ctx.Request; var res = ctx.Response;
            try
            {
                string ruta = req.Url.AbsolutePath.TrimEnd('/');
                bool local = IPAddress.IsLoopback(req.RemoteEndPoint.Address);
                if (!ruta.StartsWith("/api", StringComparison.Ordinal)) { await ArchivoAsync(res, ruta); return; }
                if (!local && !ClaveValida(req.Headers["X-Clave"])) { await ResponderAsync(res, 401, new { ok = false, error = "Clave de acceso requerida" }); return; }

                string metodo = req.HttpMethod;
                var cuerpo = metodo is "POST" or "PUT" ? LeerCuerpo(req) : new Dictionary<string, object>();
                Match m;

                if (metodo == "GET" && ruta == "/api/estacion/estado") { await EstadoAsync(res, local); return; }
                if (metodo == "GET" && ruta == "/api/historial")
                {
                    object[] copia; lock (Historial) copia = Historial.AsEnumerable().Reverse().Take(80).ToArray();
                    await ResponderAsync(res, 200, new { ok = true, historial = copia }); return;
                }
                if (metodo == "POST" && ruta == "/api/sesion/habilitar")
                {
                    if (!local) { await ResponderAsync(res, 403, new { ok = false, error = "La sesión supervisada solo se habilita desde la estación anfitriona." }); return; }
                    _sesionHastaUtc = DateTime.UtcNow.AddHours(2); Anotar("sesion", "sesión supervisada habilitada 2 h");
                    await ResponderAsync(res, 200, new { ok = true, hastaUtc = _sesionHastaUtc.ToString("o") }); return;
                }
                if (metodo == "POST" && ruta == "/api/sesion/terminar") { _sesionHastaUtc = DateTime.MinValue; Anotar("sesion", "sesión terminada"); await ResponderAsync(res, 200, new { ok = true }); return; }
                if (metodo == "POST" && ruta == "/api/estacion/detener") { await OrdenAsync(res, "DETENER", "detener"); return; }   // detener nunca exige sesión

                bool sesion = DateTime.UtcNow < _sesionHastaUtc;
                if ((m = Regex.Match(ruta, @"^/api/canecas/([VBN])/calibracion$")).Success && metodo == "PUT")
                {
                    int cerr = Entero(cuerpo, "cerrado"), abr = Entero(cuerpo, "abierto"), ms = Entero(cuerpo, "ms");
                    await OrdenAsync(res, $"CAL {m.Groups[1].Value} {cerr} {abr} {ms}", $"calibración {m.Groups[1].Value}: cerrado {cerr}°, abierto {abr}°, {ms} ms"); return;
                }
                if (metodo == "POST" && ruta == "/api/calibracion/guardar") { await OrdenAsync(res, "GUARDAR", "calibración guardada en el ESP32"); return; }
                if (metodo == "POST" && ruta == "/api/calibracion/descartar") { await OrdenAsync(res, "DESCARTAR", "calibración restaurada"); return; }

                if (!sesion) { await ResponderAsync(res, 423, new { ok = false, error = "Requiere sesión supervisada: habilítala en el panel desde la estación anfitriona." }); return; }
                if ((m = Regex.Match(ruta, @"^/api/canecas/([VBN])/(abrir|cerrar|probar|posicion)$")).Success && metodo == "POST")
                {
                    string c = m.Groups[1].Value, accion = m.Groups[2].Value;
                    string orden = accion switch { "abrir" => "ABRIR " + c, "cerrar" => "CERRAR " + c, "probar" => "CICLO " + c, _ => $"POS {c} {Entero(cuerpo, "grados")}" };
                    await OrdenAsync(res, orden, $"{accion} {c}" + (accion == "posicion" ? $" a {Entero(cuerpo, "grados")}°" : "")); return;
                }
                if (metodo == "POST" && ruta == "/api/pruebas/secuencial")
                {
                    string canecas = new string((cuerpo.TryGetValue("canecas", out var v) ? v?.ToString() ?? "" : "VBN").ToUpperInvariant().Where(ch => ch is 'V' or 'B' or 'N').ToArray());
                    if (canecas.Length == 0 || canecas.Length > 12) { await ResponderAsync(res, 400, new { ok = false, error = "Secuencia inválida" }); return; }
                    await OrdenAsync(res, "SEC " + canecas, "secuencia " + canecas); return;
                }
                if (metodo == "POST" && ruta == "/api/pruebas/combinaciones")
                {
                    // C01..C07 en orden, una caneca a la vez: V, B, N, VB, VN, BN, VBN.
                    await OrdenAsync(res, "SEC VBNVBVNBNVBN", "siete combinaciones C01–C07"); return;
                }
                await ResponderAsync(res, 404, new { ok = false, error = "Ruta no encontrada" });
            }
            catch (Exception e)
            {
                try { await ResponderAsync(res, 500, new { ok = false, error = e.Message }); } catch (Exception) { }
            }
        }

        private static async Task EstadoAsync(HttpListenerResponse res, bool local)
        {
            object? esp = null; string? error = null;
            try { esp = Json.DeserializeObject(await _esp32.EnviarAsync("ESTADO")); }
            catch (Exception e) { error = e.Message; }
            await ResponderAsync(res, 200, new
            {
                ok = esp is not null,
                conectado = esp is not null,
                puerto = _esp32.Puerto,
                estacion = "estacion_tres_canecas (firmware de calibración)",
                esp32 = esp,
                error = error ?? _esp32.UltimoError,
                sesionSupervisada = DateTime.UtcNow < _sesionHastaUtc,
                sesionHastaUtc = DateTime.UtcNow < _sesionHastaUtc ? _sesionHastaUtc.ToString("o") : null,
                anfitrion = local,
                aviso = "PWM aplicado no significa tapa abierta: no hay sensores de posición. Detener por software no reemplaza cortar la alimentación.",
            });
        }

        private static async Task OrdenAsync(HttpListenerResponse res, string orden, string descripcion)
        {
            string r;
            try { r = await _esp32.EnviarAsync(orden); }
            catch (Exception e) { Anotar("error", descripcion + ": " + e.Message); await ResponderAsync(res, 503, new { ok = false, error = e.Message }); return; }
            var d = Json.DeserializeObject(r) as Dictionary<string, object>;
            bool ok = d is not null && d.TryGetValue("ok", out var o) && o is true;
            Anotar(ok ? "orden" : "rechazo", descripcion + " → " + (ok ? d!["msg"] : d?["error"]));
            await ResponderAsync(res, ok ? 200 : 409, d ?? new Dictionary<string, object> { ["ok"] = false, ["error"] = "Respuesta inválida" });
        }

        private static async Task ArchivoAsync(HttpListenerResponse res, string ruta)
        {
            string nombre = ruta.Length == 0 ? "index.html" : ruta.TrimStart('/');
            string completo = Path.GetFullPath(Path.Combine(_wwwroot, nombre));
            if (!completo.StartsWith(_wwwroot, StringComparison.OrdinalIgnoreCase) || !File.Exists(completo)) { res.StatusCode = 404; res.Close(); return; }
            res.ContentType = Path.GetExtension(completo) switch { ".html" => "text/html; charset=utf-8", ".js" => "text/javascript; charset=utf-8", ".css" => "text/css; charset=utf-8", ".svg" => "image/svg+xml", _ => "application/octet-stream" };
            res.Headers["Cache-Control"] = "no-store";
            byte[] datos = File.ReadAllBytes(completo);
            await res.OutputStream.WriteAsync(datos, 0, datos.Length);
            res.Close();
        }

        private static async Task ResponderAsync(HttpListenerResponse res, int codigo, object cuerpo)
        {
            byte[] datos = Encoding.UTF8.GetBytes(Json.Serialize(cuerpo));
            res.StatusCode = codigo; res.ContentType = "application/json; charset=utf-8"; res.Headers["Cache-Control"] = "no-store";
            await res.OutputStream.WriteAsync(datos, 0, datos.Length);
            res.Close();
        }

        private static Dictionary<string, object> LeerCuerpo(HttpListenerRequest req)
        {
            using var lector = new StreamReader(req.InputStream, Encoding.UTF8);
            string texto = lector.ReadToEnd();
            if (texto.Length > 2048) throw new InvalidDataException("Cuerpo demasiado grande.");
            return string.IsNullOrWhiteSpace(texto) ? new Dictionary<string, object>() : Json.Deserialize<Dictionary<string, object>>(texto) ?? new();
        }

        private static int Entero(Dictionary<string, object> d, string k)
        {
            if (!d.TryGetValue(k, out var v) || v is null || !int.TryParse(Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture), out int n))
                throw new InvalidDataException($"Falta el valor numérico «{k}».");
            return n;
        }

        private static bool ClaveValida(string? recibida)
        {
            if (string.IsNullOrEmpty(recibida) || recibida!.Length != _clave.Length) return false;
            int dif = 0; for (int i = 0; i < _clave.Length; i++) dif |= _clave[i] ^ recibida[i];
            return dif == 0;
        }

        private static void Anotar(string tipo, string texto)
        {
            lock (Historial)
            {
                Historial.Add(new { hora = DateTime.Now.ToString("HH:mm:ss"), tipo, texto });
                if (Historial.Count > 300) Historial.RemoveAt(0);
            }
        }

        private static string? Valor(string[] args, string nombre)
        {
            int i = Array.IndexOf(args, nombre);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
