// Proyecto EcoAccess P3 · Pasarela ciudadana entre la página web pública (GitHub Pages) y la estación física.
//
//   Página pública (HTTPS) ──► túnel HTTPS (cloudflared) ──► esta pasarela 127.0.0.1:8092 ──► panel 127.0.0.1:8090 ──► ESP32
//
// - El panel de canecas sigue siendo el ÚNICO dueño del puerto COM: la pasarela nunca abre el puerto serie.
// - Dos puertos separados: 8092 (público, lo único que el túnel expone: /api/v1/…) y 8093 (operador, solo este equipo).
// - Sin permiso presencial (QR emitido en la estación) no se mueve nada. Recargar la página, reenviar una solicitud
//   antigua o repetir una clave no repite la apertura. Oracle, Ollama, el COM y el panel nunca quedan expuestos.
//
// Uso: EcoAccess.P3.PasarelaCiudadana.exe [--sin-tunel] [--panel http://127.0.0.1:8090] [--publico 8092] [--operador 8093]
//        [--pagina https://jorgegutho21.github.io/EcoAccess-2026-2/] [--origenes https://jorgegutho21.github.io,http://localhost:5173]
//        [--bitacora ruta.jsonl] [--cloudflared ruta\cloudflared.exe] [--cuentas http://127.0.0.1:8094]
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace EcoAccess.P3.PasarelaCiudadana
{
    internal static class Program
    {
        private static readonly JavaScriptSerializer Json = new() { MaxJsonLength = 1 << 16 };
        private static readonly HttpClient Panel = new() { Timeout = TimeSpan.FromSeconds(12) };
        private static readonly PermisoPresencial Permiso = new();
        /// <summary>
        /// Permiso del navegador del PROPIO anfitrión (accesos ECOACCESS WEB y ECOACCESS DOS CLICS de este equipo). Lo emite
        /// solo el puerto de operador (loopback + encabezado), dura la jornada y NO invalida el QR de los celulares.
        /// </summary>
        private static readonly PermisoPresencial PermisoAnfitrion = new();
        // Copia en memoria del permiso del anfitrión (nunca en disco ni en registros) para reutilizarlo: abrir el acceso
        // otra vez o en otra pestaña no deja sin sesión a las pestañas ya abiertas. Solo se entrega por el puerto de operador.
        private static string? _permisoAnfitrionTexto;
        /// <summary>Permiso con el que llegó la petición pública en curso (QR de la estación o navegador del anfitrión).</summary>
        private static readonly System.Threading.AsyncLocal<PermisoPresencial?> PermisoActual = new();
        private static PermisoPresencial PermisoVigente => PermisoActual.Value ?? Permiso;
        private static readonly Limitador Limite = new(TimeSpan.FromSeconds(6), 40, TimeSpan.FromMinutes(10));
        private static readonly Idempotencia<Dictionary<string, object>> Hechas = new();
        private static readonly SemaphoreSlim Turno = new(1, 1);
        private static readonly List<object> Eventos = new();
        private static string _panel = "http://127.0.0.1:8090";
        private static string _pagina = "https://jorgegutho21.github.io/EcoAccess-2026-2/";
        private static HashSet<string> _origenes = new(StringComparer.OrdinalIgnoreCase);
        private static string? _urlPublica;
        private static DateTime _ocupadaHastaUtc = DateTime.MinValue;
        private static string _bitacora = "";
        private static string _wwwroot = "";
        private static int _puertoPublico = 8092;

        private static int Main(string[] args)
        {
            int puertoPublico = _puertoPublico = Entero(Valor(args, "--publico"), 8092), puertoOperador = Entero(Valor(args, "--operador"), 8093);
            // Una sola pasarela por puerto público: la de la estación (8092) es única; las pruebas usan otro puerto y otra bitácora.
            using var unica = new Mutex(false, @"Global\EcoAccessP3PasarelaCiudadana-" + puertoPublico);
            if (!unica.WaitOne(0)) { Console.Error.WriteLine("La pasarela ciudadana ya está en ejecución."); return 3; }
            _panel = (Valor(args, "--panel") ?? _panel).TrimEnd('/');
            _pagina = Valor(args, "--pagina") ?? _pagina;
            _origenes = new HashSet<string>((Valor(args, "--origenes") ?? "https://jorgegutho21.github.io,http://localhost:5173,http://127.0.0.1:5173")
                .Split(',').Select(o => o.Trim().TrimEnd('/')).Where(o => o.Length > 0), StringComparer.OrdinalIgnoreCase);
            string carpeta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Ciudadano");
            Directory.CreateDirectory(carpeta);
            _bitacora = Valor(args, "--bitacora") ?? Path.Combine(carpeta, "operaciones-ciudadanas.jsonl");
            _cuentas = (Valor(args, "--cuentas") ?? _cuentas).TrimEnd('/');   // las pruebas usan otra ruta
            _wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");

            var publico = new HttpListener(); publico.Prefixes.Add($"http://127.0.0.1:{puertoPublico}/");
            var operador = new HttpListener(); operador.Prefixes.Add($"http://127.0.0.1:{puertoOperador}/"); operador.Prefixes.Add($"http://localhost:{puertoOperador}/");
            try { publico.Start(); operador.Start(); }
            catch (HttpListenerException e)
            {
                Console.Error.WriteLine($"No se pudo abrir el puerto {puertoPublico} u {puertoOperador}: {e.Message}. Usa --publico y --operador con puertos libres.");
                return 4;
            }
            Console.WriteLine($"Pasarela ciudadana: API pública en http://127.0.0.1:{puertoPublico}/api/v1/  ·  operador en http://localhost:{puertoOperador}/");
            Anotar("inicio", "Pasarela iniciada. Ninguna caneca se mueve sin permiso presencial y sesión supervisada del panel.");
            if (!args.Contains("--sin-tunel")) _ = Task.Run(() => TunelAsync(puertoPublico, Valor(args, "--cloudflared")));
            _ = Task.Run(() => Bucle(operador, AtenderOperadorAsync));
            Bucle(publico, AtenderPublicoAsync);
            return 0;
        }

        private static void Bucle(HttpListener l, Func<HttpListenerContext, Task> atender)
        {
            while (l.IsListening)
            {
                HttpListenerContext ctx;
                try { ctx = l.GetContext(); } catch (HttpListenerException) { break; } catch (ObjectDisposedException) { break; }
                _ = Task.Run(async () => { try { await atender(ctx); } catch (Exception e) { try { await Responder(ctx.Response, 500, new { ok = false, error = "Error interno de la pasarela." }); } catch { } Anotar("error", e.Message); } });
            }
        }

        // ─────────────── API pública (lo único accesible por el túnel) ───────────────
        private static async Task AtenderPublicoAsync(HttpListenerContext ctx)
        {
            var req = ctx.Request; var res = ctx.Response;
            string? origen = req.Headers["Origin"];
            if (origen is not null)
            {
                if (!_origenes.Contains(origen.TrimEnd('/'))) { await Responder(res, 403, new { ok = false, error = "Origen no autorizado." }); return; }
                res.AddHeader("Access-Control-Allow-Origin", origen); res.AddHeader("Vary", "Origin");
                res.AddHeader("Access-Control-Allow-Headers", "Authorization, Content-Type");
                res.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                res.AddHeader("Access-Control-Max-Age", "600");
            }
            res.AddHeader("Cache-Control", "no-store");
            res.AddHeader("X-Content-Type-Options", "nosniff");
            if (req.HttpMethod == "OPTIONS") { res.StatusCode = 204; res.Close(); return; }
            string ruta = req.Url.AbsolutePath.TrimEnd('/');
            if (!ruta.StartsWith("/api/v1/", StringComparison.Ordinal)) { await Responder(res, 404, new { ok = false, error = "No encontrado." }); return; }
            var ahora = DateTime.UtcNow;
            // Cuenta e historial oficial: no mueven nada, por eso no exigen el permiso presencial (sí CORS y límite).
            if (req.HttpMethod == "POST" && ruta.StartsWith("/api/v1/cuenta/", StringComparison.Ordinal)) { await CuentaAsync(req, res, ruta.Substring("/api/v1".Length)); return; }
            PermisoActual.Value = Permiso.Valido(req.Headers["Authorization"], ahora) ? Permiso
                : PermisoAnfitrion.Valido(req.Headers["Authorization"], ahora) ? PermisoAnfitrion : null;
            if (PermisoActual.Value is null)
            {
                await Responder(res, 401, new { ok = false, codigo = "permiso", error = "El permiso de la estación no es válido o expiró. Escanea de nuevo el código QR." });
                return;
            }
            if (req.HttpMethod == "GET" && ruta == "/api/v1/estado") { await Responder(res, 200, await EstadoPublicoAsync()); return; }
            if (req.HttpMethod == "POST" && ruta == "/api/v1/aperturas") { await AperturaAsync(req, res); return; }
            var m = Regex.Match(ruta, "^/api/v1/aperturas/([A-Za-z0-9_-]{16,64})/confirmacion$");
            if (req.HttpMethod == "POST" && m.Success) { await ConfirmarAsync(res, m.Groups[1].Value); return; }
            await Responder(res, 404, new { ok = false, error = "No encontrado." });
        }

        // ─────────────── Cuenta vinculada con Telegram e historial oficial (los resuelve el bot en 127.0.0.1:8094) ───────────────
        private static readonly Limitador LimiteCuenta = new(TimeSpan.FromMilliseconds(150), 60, TimeSpan.FromMinutes(1));
        private static readonly string[] RutasCuenta = { "/cuenta/codigo", "/cuenta/canjear", "/cuenta/historial", "/cuenta/desvincular" };
        private static string _cuentas = "http://127.0.0.1:8094";

        private static async Task CuentaAsync(HttpListenerRequest req, HttpListenerResponse res, string ruta)
        {
            if (!RutasCuenta.Contains(ruta)) { await Responder(res, 404, new { ok = false, error = "No encontrado." }); return; }
            if (LimiteCuenta.Intentar(DateTime.UtcNow) is not null) { await Responder(res, 429, new { ok = false, error = "Demasiadas consultas. Intenta en un minuto." }); return; }
            var cuerpo = await LeerCuerpoAsync(req) ?? new Dictionary<string, object>();
            var limpio = new Dictionary<string, object>();
            foreach (string k in new[] { "codigo", "token" }) if (Texto(cuerpo, k) is { Length: > 0 and <= 80 } v) limpio[k] = v;
            var (codigo, datos) = await PostCuentasAsync(ruta, limpio);
            await Responder(res, codigo, datos);
        }

        private static async Task<(int codigo, Dictionary<string, object> datos)> PostCuentasAsync(string ruta, object cuerpo)
        {
            try
            {
                using var msg = new HttpRequestMessage(HttpMethod.Post, _cuentas + ruta) { Content = new StringContent(Json.Serialize(cuerpo), Encoding.UTF8, "application/json") };
                msg.Headers.Add("X-Clave", ClaveEstacion());
                using var r = await Panel.SendAsync(msg);
                var d = Json.DeserializeObject(await r.Content.ReadAsStringAsync()) as Dictionary<string, object> ?? new();
                return ((int)r.StatusCode, d);
            }
            catch (Exception) { return (503, new Dictionary<string, object> { ["ok"] = false, ["error"] = "El servicio de cuentas (bot EcoAccess) no está disponible en la estación." }); }
        }

        /// <summary>ID_USUARIO de la cuenta vinculada (o null). Solo se usa para anotar la operación en la bitácora.</summary>
        private static async Task<int?> UsuarioDeCuentaAsync(string? token)
        {
            if (token is null || token.Length < 30 || token.Length > 80) return null;
            var (codigo, d) = await PostCuentasAsync("/cuenta/usuario", new Dictionary<string, object> { ["token"] = token });
            return codigo == 200 && d.TryGetValue("idUsuario", out var v) && v is int id && id > 0 ? id : null;
        }

        private static string ClaveEstacion()
        {
            string archivo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Panel", "clave.txt");
            try { return File.Exists(archivo) ? File.ReadAllText(archivo).Trim() : ""; } catch (IOException) { return ""; }
        }

        private static async Task<Dictionary<string, object>?> EstadoPanelAsync()
        {
            try { return Json.DeserializeObject(await Panel.GetStringAsync(_panel + "/api/estacion/estado")) as Dictionary<string, object>; }
            catch (Exception) { return null; }
        }

        private static async Task<object> EstadoPublicoAsync()
        {
            var p = await EstadoPanelAsync();
            string estado, texto;
            bool conectado = p is not null && p.TryGetValue("conectado", out var c) && c is true;
            bool sesion = Operativa(p);
            if (p is null) { estado = "desconectada"; texto = "El servicio de la estación no responde."; }
            else if (!conectado) { estado = "desconectada"; texto = "El ESP32 de la estación no está conectado."; }
            else if (!sesion) { estado = "sin-sesion-supervisada"; texto = "La estación está en mantenimiento. Inténtalo en unos minutos."; }
            else if (DateTime.UtcNow < _ocupadaHastaUtc || Ocupada(p)) { estado = "ocupada"; texto = "Hay una caneca en movimiento. Espera un momento."; }
            else { estado = "disponible"; texto = "Estación disponible."; }
            return new
            {
                ok = true, estacion = "estacion_tres_canecas", estado, texto,
                modo = GrupoDisponible(p) ? "grupo" : "ciclo",
                firmware = Firmware(p),
                politica = new { una = Reglas.DuracionPolitica(1), dos = Reglas.DuracionPolitica(2), tres = Reglas.DuracionPolitica(3) },
                sesion = new { expira = PermisoVigente.ExpiraUtc.ToString("o") },
                sensores = false,
            };
        }

        private static bool Ocupada(Dictionary<string, object> p) =>
            p.TryGetValue("esp32", out var e) && e is Dictionary<string, object> esp && esp.TryGetValue("canecas", out var cs) && cs is object[] lista
            && lista.OfType<Dictionary<string, object>>().Any(x => x.TryGetValue("estado", out var st) && st is string t && t != "REPOSO");
        private static string? Firmware(Dictionary<string, object>? p) =>
            p is not null && p.TryGetValue("esp32", out var e) && e is Dictionary<string, object> esp && esp.TryGetValue("firmware", out var f) ? f as string : null;
        /// <summary>El panel v2 anuncia «politica» y el firmware v2 acepta GRUPO; con v1 se usa el ciclo calibrado.</summary>
        private static bool GrupoDisponible(Dictionary<string, object>? p) =>
            p is not null && p.ContainsKey("politica") && (Firmware(p) ?? "").IndexOf("v2", StringComparison.OrdinalIgnoreCase) >= 0;
        /// <summary>
        /// La estación admite aperturas si está EN SERVICIO (estado por defecto del panel; solo una pausa manual o DETENER TODO
        /// lo quita) o si el anfitrión abrió la sesión supervisada. Antes solo valía la sesión supervisada, y la web quedaba
        /// bloqueada mientras Telegram y la app sí abrían: un mismo estado operativo para todos los canales.
        /// </summary>
        internal static bool Operativa(Dictionary<string, object>? p) =>
            p is not null && ((p.TryGetValue("enServicio", out var e) && e is true) || (p.TryGetValue("sesionSupervisada", out var s) && s is true));

        private static Dictionary<string, int> MsCalibrados(Dictionary<string, object>? p)
        {
            var r = new Dictionary<string, int>();
            if (p is not null && p.TryGetValue("esp32", out var e) && e is Dictionary<string, object> esp && esp.TryGetValue("canecas", out var cs) && cs is object[] lista)
                foreach (var x in lista.OfType<Dictionary<string, object>>())
                    if (x.TryGetValue("c", out var c) && x.TryGetValue("ms", out var ms)) r[Convert.ToString(c)!] = Convert.ToInt32(ms);
            return r;
        }

        private static async Task AperturaAsync(HttpListenerRequest req, HttpListenerResponse res)
        {
            var cuerpo = await LeerCuerpoAsync(req);
            if (cuerpo is null) { await Responder(res, 400, new { ok = false, error = "Solicitud inválida." }); return; }
            string? clave = Texto(cuerpo, "clave");
            string canecas = Reglas.NormalizarCanecas(Texto(cuerpo, "canecas"));
            if (!Reglas.ClaveValida(clave) || canecas.Length == 0) { await Responder(res, 400, new { ok = false, error = "Solicitud inválida." }); return; }
            var previa = Hechas.Buscar(clave!);
            if (previa is not null) { Anotar("duplicada", $"Clave repetida {clave!.Substring(0, 8)}…: no se repitió la apertura."); await Responder(res, 200, new Dictionary<string, object>(previa) { ["repetida"] = true }); return; }
            if (!Reglas.Reciente(Texto(cuerpo, "creadoEn"), DateTime.UtcNow)) { await Responder(res, 409, new { ok = false, codigo = "antigua", error = "La solicitud es antigua y no se ejecutó. Vuelve a identificar tu residuo." }); return; }

            if (!await Turno.WaitAsync(0)) { await Responder(res, 409, new { ok = false, codigo = "ocupada", error = "La estación está atendiendo otra apertura. Espera un momento." }); return; }
            try
            {
                var p = await EstadoPanelAsync();
                if (p is null || !(p.TryGetValue("conectado", out var c) && c is true)) { await Responder(res, 503, new { ok = false, codigo = "desconectada", error = "La estación está desconectada." }); return; }
                if (!Operativa(p)) { await Responder(res, 423, new { ok = false, codigo = "sin-sesion-supervisada", error = "La estación está en mantenimiento. Inténtalo en unos minutos." }); return; }
                if (DateTime.UtcNow < _ocupadaHastaUtc || Ocupada(p)) { await Responder(res, 409, new { ok = false, codigo = "ocupada", error = "Hay una caneca en movimiento. Espera un momento." }); return; }
                string? rechazo = Limite.Intentar(DateTime.UtcNow);
                if (rechazo is not null) { await Responder(res, 429, new { ok = false, codigo = "limite", error = rechazo }); return; }

                var plan = Reglas.Planificar(canecas, GrupoDisponible(p), MsCalibrados(p));
                object cuerpoPanel = plan.Modo == "grupo"
                    ? new { canecas, modalidad = "simultanea", origen = "web-ciudadana", clave, solicitante = "web-ciudadana", materiales = Materiales(cuerpo) }
                    : plan.Orden.EndsWith("secuencial", StringComparison.Ordinal) ? new { canecas, etiqueta = "web" } : (object)new { };
                var (ok, codigo, respuesta) = await PostPanelAsync(plan.Orden, cuerpoPanel);
                // Cuenta vinculada con Telegram (opcional): solo anota el ID_USUARIO en la bitácora; se resuelve después de la orden.
                int? usuario = await UsuarioDeCuentaAsync(Texto(cuerpo, "cuenta"));
                if (!ok)
                {
                    string error = respuesta.TryGetValue("error", out var er) ? Convert.ToString(er) ?? "" : "La estación rechazó la orden.";
                    Registrar(clave!, cuerpo, plan, "RECHAZADA", error, usuario);
                    await Responder(res, codigo == 423 ? 423 : 409, new { ok = false, codigo = codigo == 423 ? "sin-sesion-supervisada" : "rechazada", error }); return;
                }
                _ocupadaHastaUtc = DateTime.UtcNow.AddMilliseconds(plan.TotalMs + 500);
                var resultado = new Dictionary<string, object>
                {
                    ["ok"] = true, ["clave"] = clave!, ["modo"] = plan.Modo, ["movimientoMs"] = plan.MovimientoMs, ["totalMs"] = plan.TotalMs,
                    ["estadoFisico"] = "comandado",   // sin sensores: la orden se aplicó, el movimiento se confirma por observación
                    ["plan"] = plan.Tramos.Select(t => new Dictionary<string, object> { ["caneca"] = t.Caneca, ["inicioMs"] = t.InicioMs, ["abiertaMs"] = t.AbiertaMs }).ToArray(),
                    ["aviso"] = plan.Modo == "ciclo" ? "Firmware v1: se usa el tiempo de apertura calibrado en el ESP32." : "",
                };
                Hechas.Guardar(clave!, resultado);
                Registrar(clave!, cuerpo, plan, "COMANDADA", null, usuario);
                Anotar("apertura", $"Apertura {canecas} ({plan.Modo}) comandada · {string.Join(", ", plan.Tramos.Select(t => $"{t.Caneca} {t.AbiertaMs / 1000.0:0.#} s"))}");
                await Responder(res, 200, resultado);
            }
            finally { Turno.Release(); }
        }

        private static async Task ConfirmarAsync(HttpListenerResponse res, string clave)
        {
            if (Hechas.Buscar(clave) is null) { await Responder(res, 404, new { ok = false, error = "No hay una apertura con esa clave en esta sesión." }); return; }
            EscribirBitacora(new Dictionary<string, object>
            {
                ["idOperacion"] = "web:" + clave + ":confirmacion", ["operacionOrigen"] = "web:" + clave, ["fechaUtc"] = DateTime.UtcNow.ToString("o"),
                ["sesion"] = PermisoVigente.IdSesion ?? "", ["canal"] = "WEB", ["tipo"] = "DISPOSICION_CONFIRMADA_CIUDADANO",
                ["verificacionFisica"] = "SIN_SENSOR", ["estadoRegistro"] = "PENDIENTE_ORACLE", ["estacion"] = "estacion_tres_canecas",
            });
            Anotar("confirmacion", $"Disposición confirmada por la persona ({clave.Substring(0, 8)}…).");
            await Responder(res, 200, new { ok = true });
        }

        private static object[] Materiales(Dictionary<string, object> cuerpo) =>
            cuerpo.TryGetValue("materiales", out var m) && m is object[] l
                ? l.OfType<Dictionary<string, object>>().Take(10).Select(x => (object)new { id = Recortar(Texto(x, "id"), 12), oracle = Recortar(Texto(x, "oracle"), 40), nombre = Recortar(Texto(x, "nombre"), 80),
                    caneca = Texto(x, "caneca") is "verde" or "blanca" or "negra" ? Texto(x, "caneca") : null }).ToArray()
                : Array.Empty<object>();

        /// <summary>
        /// Bitácora persistente e idempotente (una línea JSON por evento, clave única). El sincronizador con Oracle la sube al
        /// esquema y marca cada clave; reprocesarla nunca vuelve a abrir una caneca porque aquí solo hay registros.
        /// </summary>
        private static void Registrar(string clave, Dictionary<string, object> cuerpo, PlanApertura plan, string estado, string? error, int? usuario)
        {
            EscribirBitacora(new Dictionary<string, object>
            {
                ["idOperacion"] = "web:" + clave, ["fechaUtc"] = DateTime.UtcNow.ToString("o"), ["sesion"] = PermisoVigente.IdSesion ?? "",
                ["canal"] = "WEB", ["tipo"] = "OPERACION_AUTOMATICA", ["consulta"] = Recortar(Texto(cuerpo, "consulta"), 160) ?? "",
                ["materiales"] = Materiales(cuerpo), ["canecas"] = plan.Canecas,
                ["contenedores"] = plan.Canecas.Select(c => c switch { 'V' => 1400, 'B' => 1093, _ => 1401 }).ToArray(),
                ["reconocimiento"] = "TEXTO_DETERMINISTA", ["estadoApertura"] = estado, ["modo"] = plan.Modo,
                ["tiempoProgramadoMs"] = plan.Tramos.Count == 0 ? 0 : plan.Tramos.Max(t => t.AbiertaMs), ["error"] = error ?? "",
                ["verificacionFisica"] = "SIN_SENSOR", ["estadoRegistro"] = "PENDIENTE_ORACLE", ["estacion"] = "estacion_tres_canecas",
                ["idUsuario"] = usuario ?? 0,
                ["metodo"] = Texto(cuerpo, "metodo") == "VOZ" ? "VOZ" : "TEXTO",
                ["invitado"] = Texto(cuerpo, "invitado") is { } inv && Regex.IsMatch(inv, "^[A-Za-z0-9-]{8,40}$") ? inv : "",
            });
        }

        private static readonly object CandadoBitacora = new();
        private static void EscribirBitacora(Dictionary<string, object> linea)
        {
            try { lock (CandadoBitacora) File.AppendAllText(_bitacora, Json.Serialize(linea) + Environment.NewLine, new UTF8Encoding(false)); }
            catch (Exception e) { Anotar("error", "No se pudo escribir la bitácora: " + e.Message); }
        }

        // ─────────────── Operador (solo este equipo; nunca pasa por el túnel) ───────────────
        private static async Task AtenderOperadorAsync(HttpListenerContext ctx)
        {
            var req = ctx.Request; var res = ctx.Response;
            if (!IPAddress.IsLoopback(req.RemoteEndPoint.Address) || req.Headers["Cf-Ray"] is not null || req.Headers["X-Forwarded-For"] is not null)
            { await Responder(res, 403, new { ok = false }); return; }
            string host = req.Url.Host;
            if (host != "localhost" && host != "127.0.0.1") { await Responder(res, 403, new { ok = false }); return; }   // DNS rebinding
            string ruta = req.Url.AbsolutePath.TrimEnd('/');
            if (req.HttpMethod == "GET" && (ruta == "" || ruta == "/index.html"))
            {
                string html = File.Exists(Path.Combine(_wwwroot, "operador.html")) ? File.ReadAllText(Path.Combine(_wwwroot, "operador.html")) : "<h1>Falta wwwroot/operador.html</h1>";
                byte[] b = Encoding.UTF8.GetBytes(html); res.ContentType = "text/html; charset=utf-8"; res.AddHeader("Cache-Control", "no-store");
                await res.OutputStream.WriteAsync(b, 0, b.Length); res.Close(); return;
            }
            // Las acciones exigen un encabezado propio: otra página web abierta en este equipo no puede dispararlas.
            if (req.HttpMethod == "POST" && req.Headers["X-EcoAccess-Operador"] != "1") { await Responder(res, 403, new { ok = false, error = "Falta el encabezado del operador." }); return; }
            if (req.HttpMethod == "GET" && ruta == "/estado")
            {
                object[] ev; lock (Eventos) ev = Eventos.AsEnumerable().Reverse().Take(40).ToArray();
                await Responder(res, 200, new { ok = true, publico = _urlPublica, activa = Permiso.Activo(DateTime.UtcNow), expira = Permiso.ExpiraUtc, sesion = Permiso.IdSesion, estacion = await EstadoPublicoAsync(), eventos = ev });
                return;
            }
            if (req.HttpMethod == "POST" && ruta == "/habilitar")
            {
                // Habilita también la sesión supervisada del panel (2 h), que es quien autoriza mover los motores.
                var (ok, _, r) = await PostPanelAsync("/api/sesion/habilitar", new { });
                if (!ok) { await Responder(res, 503, new { ok = false, error = "No se pudo habilitar la sesión supervisada del panel: " + (r.TryGetValue("error", out var e) ? e : "sin respuesta") }); return; }
                string permiso = Permiso.Emitir(TimeSpan.FromHours(2), DateTime.UtcNow);
                Anotar("sesion", $"Estación ciudadana habilitada por 2 horas ({Permiso.IdSesion}). Cualquier QR anterior quedó invalidado.");
                string Enlace(string api) => _pagina + "#e=" + Uri.EscapeDataString(api) + "&t=" + permiso;
                await Responder(res, 200, new { ok = true, expira = Permiso.ExpiraUtc, enlacePublico = _urlPublica is null ? null : Enlace(_urlPublica), enlaceLocal = "http://localhost:5173/#e=" + Uri.EscapeDataString("http://127.0.0.1:" + _puertoPublico) + "&t=" + permiso });
                return;
            }
            if (req.HttpMethod == "POST" && ruta == "/enlace-anfitrion")
            {
                // Enlace para el navegador de ESTE equipo (accesos ECOACCESS WEB / DOS CLICS): reutiliza el permiso del
                // anfitrión si sigue vigente o emite uno de 12 h. No toca el QR de los celulares ni la sesión supervisada.
                if (_urlPublica is null) { await Responder(res, 503, new { ok = false, error = "El túnel HTTPS aún no está listo. Inténtalo en unos segundos." }); return; }
                var cuerpoAnf = await LeerCuerpoAsync(req);
                string destino = Texto(cuerpoAnf ?? new Dictionary<string, object>(), "destino") == "dos-clics" ? "dos-clics/" : "";
                string permisoAnf;
                if (PermisoAnfitrion.Activo(DateTime.UtcNow.AddHours(1)) && _permisoAnfitrionTexto is not null) permisoAnf = _permisoAnfitrionTexto;
                else
                {
                    permisoAnf = _permisoAnfitrionTexto = PermisoAnfitrion.Emitir(TimeSpan.FromHours(12), DateTime.UtcNow);
                    Anotar("sesion", $"Navegador del anfitrión vinculado por 12 horas ({PermisoAnfitrion.IdSesion}); el QR de los celulares sigue vigente.");
                }
                await Responder(res, 200, new { ok = true, expira = PermisoAnfitrion.ExpiraUtc, enlace = _pagina + destino + "#e=" + Uri.EscapeDataString(_urlPublica) + "&t=" + permisoAnf });
                return;
            }
            if (req.HttpMethod == "POST" && ruta == "/terminar")
            {
                Permiso.Revocar(); PermisoAnfitrion.Revocar(); _permisoAnfitrionTexto = null; Anotar("sesion", "Estación ciudadana terminada: los QR emitidos dejaron de funcionar.");
                await Responder(res, 200, new { ok = true }); return;
            }
            await Responder(res, 404, new { ok = false });
        }

        // ─────────────── Túnel HTTPS (cloudflared) ───────────────
        private static async Task TunelAsync(int puerto, string? ruta)
        {
            string exe = ruta ?? new[] { @"C:\Program Files (x86)\cloudflared\cloudflared.exe", @"C:\Program Files\cloudflared\cloudflared.exe", "cloudflared" }.FirstOrDefault(p => p == "cloudflared" || File.Exists(p))!;
            CerrarTunelesHuerfanos(puerto);
            for (int intento = 1; ; intento++)
            {
                try
                {
                    var psi = new ProcessStartInfo(exe, $"tunnel --no-autoupdate --url http://127.0.0.1:{puerto}") { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true, CreateNoWindow = true };
                    using var p = Process.Start(psi)!;
                    AppDomain.CurrentDomain.ProcessExit += (_, _) => { try { if (!p.HasExited) p.Kill(); } catch { } };
                    Anotar("tunel", "Abriendo túnel HTTPS…");
                    string? linea;
                    while ((linea = await p.StandardError.ReadLineAsync()) is not null)
                    {
                        var m = Regex.Match(linea, @"https://[a-z0-9-]+\.trycloudflare\.com");
                        if (m.Success && _urlPublica != m.Value) { _urlPublica = m.Value; Anotar("tunel", "Túnel HTTPS activo: " + _urlPublica + " (genera un QR nuevo para usarlo)."); }
                    }
                    p.WaitForExit();
                    Anotar("tunel", $"El túnel se cerró (código {p.ExitCode}). Reintentando…");
                }
                catch (Exception e) { Anotar("tunel", "No se pudo iniciar cloudflared: " + e.Message); }
                _urlPublica = null;
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(60, 5 * intento)));
            }
        }

        /// <summary>
        /// Si la pasarela anterior se cerró de golpe, su cloudflared sigue vivo apuntando a este puerto con una dirección
        /// vieja. Se cierran solo los cloudflared cuyo comando contiene «--url http://127.0.0.1:{puerto}».
        /// </summary>
        private static void CerrarTunelesHuerfanos(int puerto)
        {
            try
            {
                using var buscador = new System.Management.ManagementObjectSearcher("SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='cloudflared.exe'");
                foreach (System.Management.ManagementObject o in buscador.Get())
                {
                    string cmd = Convert.ToString(o["CommandLine"]) ?? "";
                    if (!cmd.Contains($"--url http://127.0.0.1:{puerto}")) continue;
                    try { using var p = Process.GetProcessById(Convert.ToInt32(o["ProcessId"])); p.Kill(); Anotar("tunel", $"Túnel anterior cerrado (PID {p.Id})."); } catch (Exception) { }
                }
            }
            catch (Exception) { /* sin WMI se continúa: el túnel nuevo funciona igual */ }
        }

        // ─────────────── Utilidades ───────────────
        private static async Task<(bool ok, int codigo, Dictionary<string, object> cuerpo)> PostPanelAsync(string ruta, object cuerpo)
        {
            try
            {
                using var contenido = new StringContent(Json.Serialize(cuerpo), Encoding.UTF8, "application/json");
                using var r = await Panel.PostAsync(_panel + ruta, contenido);
                var d = Json.DeserializeObject(await r.Content.ReadAsStringAsync()) as Dictionary<string, object> ?? new();
                return (r.IsSuccessStatusCode && d.TryGetValue("ok", out var o) && o is true, (int)r.StatusCode, d);
            }
            catch (Exception e) { return (false, 503, new Dictionary<string, object> { ["error"] = "El panel de canecas no responde: " + e.Message }); }
        }

        private static async Task<Dictionary<string, object>?> LeerCuerpoAsync(HttpListenerRequest req)
        {
            if (req.ContentLength64 > 4096 || !(req.ContentType ?? "").StartsWith("application/json", StringComparison.OrdinalIgnoreCase)) return null;
            using var lector = new StreamReader(req.InputStream, Encoding.UTF8);
            var buffer = new char[4097]; int n = await lector.ReadBlockAsync(buffer, 0, buffer.Length);
            if (n > 4096) return null;
            try { return Json.DeserializeObject(new string(buffer, 0, n)) as Dictionary<string, object>; } catch (Exception) { return null; }
        }

        private static async Task Responder(HttpListenerResponse res, int codigo, object cuerpo)
        {
            byte[] b = Encoding.UTF8.GetBytes(Json.Serialize(cuerpo));
            res.StatusCode = codigo; res.ContentType = "application/json; charset=utf-8"; res.ContentLength64 = b.Length;
            await res.OutputStream.WriteAsync(b, 0, b.Length); res.Close();
        }

        private static void Anotar(string tipo, string texto)
        {
            lock (Eventos) { Eventos.Add(new { hora = DateTime.Now.ToString("HH:mm:ss"), tipo, texto }); if (Eventos.Count > 300) Eventos.RemoveAt(0); }
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {tipo}: {texto}");
        }

        private static string? Texto(Dictionary<string, object> d, string k) => d.TryGetValue(k, out var v) && v is not null ? Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture)?.Trim() : null;
        private static string? Recortar(string? s, int n) => s is null ? null : s.Length <= n ? s : s.Substring(0, n);
        private static string? Valor(string[] a, string n) { int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
        private static int Entero(string? s, int defecto) => int.TryParse(s, out int v) && v is > 1024 and < 65536 ? v : defecto;
    }
}
