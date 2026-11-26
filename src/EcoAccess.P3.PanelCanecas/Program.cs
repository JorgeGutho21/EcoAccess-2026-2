// Proyecto EcoAccess P3 · Centro de control local de las tres canecas.
// Uso: EcoAccess.P3.PanelCanecas.exe [--puerto COM3] [--http 8090]
//   http://localhost:8090/       en este equipo (sin clave)
//   http://<ip-del-equipo>:8090/ en la red local, con la clave de %ProgramData%\EcoAccessP3Framework\Panel\clave.txt
//                                (requiere una vez, como administrador: netsh http add urlacl url=http://+:8090/ user=<usuario>)
// Los motores solo se mueven con una sesión supervisada habilitada DESDE ESTE EQUIPO (2 horas).
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
        private static readonly Dictionary<string, string> UltimaOperacion = new() { ["V"] = "Sin operaciones", ["B"] = "Sin operaciones", ["N"] = "Sin operaciones" };
        private static readonly Dictionary<string, string> Nombre = new() { ["V"] = "Verde", ["B"] = "Blanca", ["N"] = "Negra" };
        private static EnlaceEsp32 _esp32 = null!;
        private static string _clave = "";
        private static DateTime _sesionHastaUtc = DateTime.MinValue;

        /// <summary>
        /// «Estación en servicio»: el anfitrión la activa una vez para la jornada (hasta las 23:59 locales). Mientras está
        /// activa, las aperturas ciudadanas autenticadas (Telegram con QR, web, DOS CLICS, app C#) se ejecutan sin una
        /// sesión supervisada. Calibración, posiciones y pruebas técnicas siguen exigiendo la sesión supervisada.
        /// DETENER TODO la desactiva. Nunca se activa sola al encender Windows: solo se restaura si el anfitrión la activó
        /// hoy y el servicio se reinició.
        /// </summary>
        private static DateTime _servicioHastaUtc = DateTime.MinValue;
        private static readonly string ArchivoServicio = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Panel", "servicio.txt");

        private static void GuardarServicio()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ArchivoServicio)!);
                if (_servicioHastaUtc == DateTime.MinValue) { if (File.Exists(ArchivoServicio)) File.Delete(ArchivoServicio); }
                else File.WriteAllText(ArchivoServicio, _servicioHastaUtc.ToString("o", System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void RestaurarServicio()
        {
            try
            {
                if (!File.Exists(ArchivoServicio)) return;
                if (DateTime.TryParse(File.ReadAllText(ArchivoServicio).Trim(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime hasta)
                    && hasta.ToUniversalTime() > DateTime.UtcNow)
                {
                    _servicioHastaUtc = hasta.ToUniversalTime();
                    Anotar("sesion", $"Estación en servicio restaurada tras reiniciar el panel (activada hoy por el anfitrión, hasta las {_servicioHastaUtc.ToLocalTime():HH:mm}).");
                }
                else File.Delete(ArchivoServicio);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        /// <summary>Fin de la jornada: 23:59 de hoy en hora local.</summary>
        internal static DateTime FinDeJornadaUtc(DateTime ahoraLocal) => ahoraLocal.Date.AddDays(1).AddMinutes(-1).ToUniversalTime();
        private static string _wwwroot = "";

        private static int Main(string[] args)
        {
            string puerto = Valor(args, "--puerto") ?? "COM3";
            int http = int.TryParse(Valor(args, "--http"), out int h) && h is > 1024 and < 65536 ? h : 8090;
            bool soloPuerto = args.Contains("--solo-puerto");
            // Una instancia por puerto HTTP. Una instancia de prueba en otro puerto HTTP debe usar --solo-puerto para no
            // disputarle el ESP32 a la instancia principal.
            using var unica = new Mutex(false, @"Global\EcoAccessP3PanelCanecas" + (http == 8090 ? "" : "-" + http));
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

            _esp32 = new EnlaceEsp32(puerto, soloPuerto);
            _esp32.Evento += (tipo, texto) => Anotar(tipo, tipo == "esp32" ? Interpretar(texto) : texto);

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
            Console.WriteLine($"Centro de control de canecas: http://localhost:{http}/" + (red ? " (también en la red local, con clave)" : " (solo este equipo)"));
            Anotar("panel", red ? "Servicio iniciado (este equipo y red local con clave)." : "Servicio iniciado (solo este equipo).");
            RestaurarServicio();
            _ = Task.Run(VigilarPasarelaAsync);
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

                if (metodo == "GET" && ruta == "/api/estacion/estado") { await ResponderAsync(res, 200, Estado(local)); return; }
                if (metodo == "GET" && ruta == "/api/diagnostico") { await ResponderAsync(res, 200, await DiagnosticoAsync()); return; }
                if (metodo == "GET" && ruta == "/api/historial")
                {
                    object[] copia; lock (Historial) copia = Historial.AsEnumerable().Reverse().Take(60).ToArray();
                    await ResponderAsync(res, 200, new { ok = true, historial = copia }); return;
                }
                if (metodo == "POST" && ruta == "/api/estacion/buscar")
                {
                    _esp32.BuscarAhora(); Anotar("panel", "Búsqueda del dispositivo solicitada.");
                    await ResponderAsync(res, 200, new { ok = true, msg = "Buscando el ESP32…" }); return;
                }
                if (metodo == "POST" && ruta == "/api/estacion/probar-comunicacion")
                {
                    var reloj = System.Diagnostics.Stopwatch.StartNew();
                    try { await _esp32.EnviarAsync("ESTADO"); await ResponderAsync(res, 200, new { ok = true, msg = $"El ESP32 respondió en {reloj.ElapsedMilliseconds} ms (sin mover motores)." }); }
                    catch (Exception e) { await ResponderAsync(res, 503, new { ok = false, error = e.Message }); }
                    return;
                }
                if (metodo == "POST" && ruta == "/api/sesion/habilitar")
                {
                    if (!local) { await ResponderAsync(res, 403, new { ok = false, error = "La sesión supervisada solo se habilita desde la estación anfitriona." }); return; }
                    _sesionHastaUtc = DateTime.UtcNow.AddHours(2); Anotar("sesion", "Sesión supervisada habilitada por 2 horas.");
                    await ResponderAsync(res, 200, new { ok = true }); return;
                }
                if (metodo == "POST" && ruta == "/api/estacion/servicio")
                {
                    if (!local) { await ResponderAsync(res, 403, new { ok = false, error = "La estación solo se pone en servicio desde el equipo anfitrión." }); return; }
                    bool activar = cuerpo.TryGetValue("activo", out var act) && act is true;
                    _servicioHastaUtc = activar ? FinDeJornadaUtc(DateTime.Now) : DateTime.MinValue;
                    GuardarServicio();
                    Anotar("sesion", activar ? $"Estación en servicio hasta las {_servicioHastaUtc.ToLocalTime():HH:mm}: Telegram, web, DOS CLICS y la app pueden abrir con autorización." : "Estación fuera de servicio: las aperturas ciudadanas quedan en pausa.");
                    await ResponderAsync(res, 200, new { ok = true, enServicio = activar, servicioHasta = activar ? _servicioHastaUtc.ToLocalTime().ToString("HH:mm") : null }); return;
                }
                if (metodo == "POST" && ruta == "/api/sesion/terminar") { _sesionHastaUtc = DateTime.MinValue; Anotar("sesion", "Sesión supervisada terminada."); await ResponderAsync(res, 200, new { ok = true }); return; }
                if (metodo == "POST" && ruta == "/api/estacion/detener")
                {
                    // DETENER TODO: desactiva el control (nuevas órdenes bloqueadas hasta volver a activarlo) y avisa al ESP32.
                    _sesionHastaUtc = DateTime.MinValue;
                    if (_servicioHastaUtc != DateTime.MinValue) { _servicioHastaUtc = DateTime.MinValue; GuardarServicio(); Anotar("sesion", "DETENER TODO: la estación quedó fuera de servicio."); }
                    if (_esp32.Estado != EstadoConexion.Conectado)
                    {
                        Anotar("error", "DETENER TODO: control desactivado, pero el ESP32 está desconectado y no recibió la orden.");
                        await ResponderAsync(res, 503, new { ok = false, error = "Control desactivado. El ESP32 está desconectado y NO recibió la orden de detención: si hay una tapa abierta, corta la alimentación." }); return;
                    }
                    await OrdenAsync(res, "DETENER", "DETENER TODO: cola cancelada, tapas cerrando y control desactivado"); return;   // nunca exige sesión
                }
                if ((m = Regex.Match(ruta, @"^/api/canecas/([VBN])/detener$")).Success && metodo == "POST")
                {
                    string c = m.Groups[1].Value;
                    if (await OrdenAsync(res, "DETENER " + c, $"{Nombre[c]}: detenida (fuera de la cola y cerrando)")) lock (UltimaOperacion) UltimaOperacion[c] = "detenida · " + DateTime.Now.ToString("HH:mm:ss");
                    return;
                }

                // Calibración (no mueve motores): aplicar, guardar y descartar, por caneca o las tres.
                if ((m = Regex.Match(ruta, @"^/api/canecas/([VBN])/calibracion$")).Success && metodo == "PUT")
                {
                    string c = m.Groups[1].Value;
                    int cerr = Entero(cuerpo, "cerrado"), abr = Entero(cuerpo, "abierto"), ms = Entero(cuerpo, "ms");
                    await OrdenAsync(res, $"CAL {c} {cerr} {abr} {ms}", $"{Nombre[c]}: calibración aplicada (sin guardar) cerrada {cerr}°, abierta {abr}°, {ms / 1000.0:0.#} s"); return;
                }
                if ((m = Regex.Match(ruta, @"^/api/(?:canecas/([VBN])/)?calibracion/(guardar|descartar)$")).Success && metodo == "POST")
                {
                    string c = m.Groups[1].Value, accion = m.Groups[2].Value;
                    await GuardarODescartarAsync(res, c, accion); return;
                }

                bool sesion = DateTime.UtcNow < _sesionHastaUtc;
                bool enServicio = DateTime.UtcNow < _servicioHastaUtc;
                if (metodo == "POST" && ruta == "/api/estacion/abrir-grupo" && (sesion || enServicio)) { await AbrirGrupoAsync(res, cuerpo, local); return; }
                if (!sesion)
                {
                    bool apertura = ruta == "/api/estacion/abrir-grupo";
                    string origenRechazo = Texto(cuerpo, "origen") ?? (local ? "este equipo" : "red");
                    if (apertura) Anotar("rechazo", $"Apertura pedida por {origenRechazo} rechazada: la estación no está en servicio. Actívala con «ESTACIÓN EN SERVICIO».");
                    await ResponderAsync(res, 423, new { ok = false, error = apertura
                        ? "La estación no está en servicio: el anfitrión debe activar «ESTACIÓN EN SERVICIO» en ECOACCESS ESP32."
                        : "Requiere sesión supervisada: habilítala desde la estación anfitriona." });
                    return;
                }
                if ((m = Regex.Match(ruta, @"^/api/canecas/([VBN])/(abrir|cerrar|probar|posicion)$")).Success && metodo == "POST")
                {
                    string c = m.Groups[1].Value, accion = m.Groups[2].Value;
                    string orden = accion switch { "abrir" or "probar" => "CICLO " + c, "cerrar" => "CERRAR " + c, _ => $"POS {c} {Entero(cuerpo, "grados")}" };
                    string texto = accion switch
                    {
                        "abrir" => $"{Nombre[c]}: apertura solicitada",
                        "probar" => $"{Nombre[c]}: prueba técnica de ciclo",
                        "cerrar" => $"{Nombre[c]}: cierre solicitado",
                        _ => $"{Nombre[c]}: prueba de posición {Entero(cuerpo, "grados")}°",
                    };
                    if (await OrdenAsync(res, orden, texto)) lock (UltimaOperacion) UltimaOperacion[c] = texto.Substring(texto.IndexOf(':') + 2) + " · " + DateTime.Now.ToString("HH:mm:ss");
                    return;
                }
                if (metodo == "POST" && ruta == "/api/pruebas/secuencial")
                {
                    string canecas = new string((cuerpo.TryGetValue("canecas", out var v) ? v?.ToString() ?? "" : "VBN").ToUpperInvariant().Where(ch => ch is 'V' or 'B' or 'N').ToArray());
                    if (canecas.Length == 0 || canecas.Length > 15) { await ResponderAsync(res, 400, new { ok = false, error = "Secuencia inválida" }); return; }
                    string etiqueta = cuerpo.TryGetValue("etiqueta", out var e) ? e?.ToString() ?? "" : "";
                    await OrdenAsync(res, "SEC " + canecas, $"Secuencia {etiqueta} {string.Join(" → ", canecas.Select(ch => Nombre[ch.ToString()]))}".Replace("  ", " ")); return;
                }
                if (metodo == "POST" && ruta == "/api/pruebas/combinaciones")
                {
                    await OrdenAsync(res, "SEC VBNVBVNBNVBN", "Prueba de las siete combinaciones C01–C07 (una caneca a la vez)"); return;
                }
                if (metodo == "POST" && ruta == "/api/estacion/abrir-grupo") { await AbrirGrupoAsync(res, cuerpo, local); return; }
                if (metodo == "POST" && ruta == "/api/pruebas/c01-c07")
                {
                    // Las siete combinaciones con la política automática (7/14/21 s), una combinación tras otra, sin intervención.
                    _ = Task.Run(() => PruebaCombinacionesAsync(Texto(cuerpo, "modalidad") == "secuencial"));
                    await ResponderAsync(res, 200, new { ok = true, msg = "Prueba C01–C07 iniciada (≈ 1 min 40 s). Observa cada tapa y anota el resultado." }); return;
                }
                await ResponderAsync(res, 404, new { ok = false, error = "Ruta no encontrada" });
            }
            catch (InvalidDataException e) { try { await ResponderAsync(res, 400, new { ok = false, error = e.Message }); } catch (Exception) { } }
            catch (Exception e) { try { await ResponderAsync(res, 500, new { ok = false, error = e.Message }); } catch (Exception) { } }
        }

        private static object Estado(bool local)
        {
            var conexion = _esp32.Estado;
            bool sesion = DateTime.UtcNow < _sesionHastaUtc;
            object? esp = conexion == EstadoConexion.Conectado && _esp32.UltimoEstadoJson is { } j ? Json.DeserializeObject(j) : null;
            string clave = conexion switch
            {
                EstadoConexion.UsbDesconectado => "usb-desconectado",
                EstadoConexion.UsbDetectado => "usb-detectado",
                EstadoConexion.PuertoOcupado => "puerto-ocupado",
                EstadoConexion.SinRespuesta => "sin-respuesta",
                _ => sesion ? "lista" : "conectado",
            };
            Dictionary<string, string> ultima; lock (UltimaOperacion) ultima = new(UltimaOperacion);
            var d = _esp32.Dispositivo;
            return new
            {
                ok = true,
                estado = clave,
                texto = conexion == EstadoConexion.Conectado ? (sesion ? "Estación lista: puede mover las canecas." : "ESP32 conectado. Habilita la sesión supervisada para mover las canecas.") : EnlaceEsp32.Texto(conexion),
                conectado = conexion == EstadoConexion.Conectado,
                usb = conexion != EstadoConexion.UsbDesconectado,
                puerto = _esp32.Puerto ?? d?.Puerto,
                dispositivo = d is null ? null : new { d.Descripcion, d.Vid, d.Pid },
                identificador = esp is Dictionary<string, object> e && e.TryGetValue("firmware", out var fw) ? "estacion_tres_canecas · " + fw : null,
                politica = new { una = DuracionPolitica(1), dos = DuracionPolitica(2), tres = DuracionPolitica(3), escalonMs = EscalonSimultaneoMs },
                ultimaRespuesta = _esp32.UltimaRespuesta?.ToString("HH:mm:ss"),
                error = conexion == EstadoConexion.Conectado ? null : _esp32.UltimoError,
                esp32 = esp,
                ultimaOperacion = ultima,
                sesionSupervisada = sesion,
                sesionHasta = sesion ? _sesionHastaUtc.ToLocalTime().ToString("HH:mm") : null,
                enServicio = DateTime.UtcNow < _servicioHastaUtc,
                servicioHasta = DateTime.UtcNow < _servicioHastaUtc ? _servicioHastaUtc.ToLocalTime().ToString("HH:mm") : null,
                anfitrion = local,
                ocupada = TurnoGrupo.CurrentCount == 0 || (esp is Dictionary<string, object> e2 && e2.TryGetValue("canecas", out var cs) && cs is System.Collections.IEnumerable lista
                    && lista.OfType<Dictionary<string, object>>().Any(c => c.TryGetValue("estado", out var s) && !string.Equals(s as string, "REPOSO", StringComparison.Ordinal))),
                bot = PulsoBot(),
                pasarela = _pasarela,
            };
        }

        // Estado de la pasarela ciudadana (GitHub Pages → túnel → pasarela). Se consulta cada 5 s su puerto de operador
        // (127.0.0.1:8093, solo local); el panel no depende de ella para funcionar.
        private static object _pasarela = new { activa = false, tunel = (string?)null, qrActivo = false };
        private static readonly System.Net.Http.HttpClient HttpPasarela = new() { Timeout = TimeSpan.FromSeconds(2) };
        private static async Task VigilarPasarelaAsync()
        {
            while (true)
            {
                try
                {
                    var d = Json.DeserializeObject(await HttpPasarela.GetStringAsync("http://127.0.0.1:8093/estado")) as Dictionary<string, object>;
                    string? tunel = d is not null && d.TryGetValue("publico", out var p) ? p as string : null;
                    _pasarelaActiva = d is not null; _pasarelaTunel = tunel;
                    _pasarela = new
                    {
                        activa = d is not null,
                        tunel,
                        qrActivo = d is not null && d.TryGetValue("activa", out var a) && a is true,
                    };
                }
                catch (Exception) { _pasarelaActiva = false; _pasarelaTunel = null; _pasarela = new { activa = false, tunel = (string?)null, qrActivo = false }; }
                await Task.Delay(5000);
            }
        }

        /// <summary>
        /// Estado del bot y de Oracle según el pulso que escribe el bot cada 30 s (%ProgramData%\EcoAccessP3Framework\Estacion\bot-estado.json).
        /// El panel no abre conexiones a Oracle: solo informa lo que el bot comprobó.
        /// </summary>
        private static object PulsoBot()
        {
            var p = LeerPulso();
            return new { activo = p.Vivo && p.Hace is < PulsoVigenteS, oracle = p.Oracle, pendientes = p.Pendientes, hace = p.Hace };
        }

        /// <summary>Un pulso del bot más viejo que esto se considera caducado: no se informa como dato actual.</summary>
        internal const int PulsoVigenteS = 90;

        private sealed class Pulso
        {
            public bool Existe, Vivo;
            public int? Hace;
            public bool? Oracle;
            public int Pendientes;
            public string? Modelo;
        }

        private static Pulso LeerPulso()
        {
            string archivo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Estacion", "bot-estado.json");
            var r = new Pulso();
            try
            {
                if (!File.Exists(archivo)) return r;
                var d = Json.Deserialize<Dictionary<string, object>>(File.ReadAllText(archivo)) ?? new();
                r.Existe = true;
                if (d.TryGetValue("utc", out var u) && DateTime.TryParse(u as string, null, System.Globalization.DateTimeStyles.RoundtripKind, out var t))
                    r.Hace = Math.Max(0, (int)(DateTime.UtcNow - t.ToUniversalTime()).TotalSeconds);
                r.Vivo = d.TryGetValue("pid", out var p) && int.TryParse(Convert.ToString(p, System.Globalization.CultureInfo.InvariantCulture), out int pid) && ProcesoVivo(pid);
                r.Oracle = d.TryGetValue("oracle", out var o) && o is bool b ? b : null;
                r.Pendientes = d.TryGetValue("pendientes", out var n) && int.TryParse(Convert.ToString(n, System.Globalization.CultureInfo.InvariantCulture), out int k) ? k : 0;
                r.Modelo = d.TryGetValue("modelo", out var m) ? m as string : null;
            }
            catch (Exception) { r.Existe = false; }
            return r;
        }

        // ───── Diagnóstico (solo lectura): nunca envía órdenes al ESP32; usa el último ESTADO que ya leyó el monitor. ─────
        private static volatile bool _pasarelaActiva;
        private static volatile string? _pasarelaTunel;
        private static readonly System.Net.Http.HttpClient HttpDiagnostico = new() { Timeout = TimeSpan.FromSeconds(4) };

        private static string Antiguedad(int s) => s < 60 ? $"hace {s} s" : s < 3600 ? $"hace {s / 60} min" : $"hace {s / 3600} h {s % 3600 / 60} min";

        private static async Task<object> DiagnosticoAsync()
        {
            var c = new List<Dictionary<string, object?>>();
            void Poner(string id, string nombre, string estado, string valor, string detalle, string? accion = null) =>
                c.Add(new Dictionary<string, object?> { ["id"] = id, ["nombre"] = nombre, ["estado"] = estado, ["valor"] = valor, ["detalle"] = detalle, ["accion"] = accion });

            // Comprobaciones de red en paralelo (lecturas GET con límite de tiempo).
            var tOllama = ConsultarOllamaAsync();
            string? tunel = _pasarelaActiva ? _pasarelaTunel : null;
            var tTunel = tunel is null ? Task.FromResult<(bool, string)>((false, "")) : ConsultarTunelAsync(tunel);

            var conexion = _esp32.Estado;
            var disp = _esp32.Dispositivo;
            string? puerto = _esp32.Puerto ?? disp?.Puerto;
            var esp = conexion == EstadoConexion.Conectado && _esp32.UltimoEstadoJson is { } j ? Json.DeserializeObject(j) as Dictionary<string, object> : null;
            var canecas = (esp is not null && esp.TryGetValue("canecas", out var cs) ? cs as object[] : null)?.OfType<Dictionary<string, object>>().ToList() ?? new();

            // 1. USB
            if (conexion == EstadoConexion.UsbDesconectado)
                Poner("usb", "Cable USB", "mal", "Desconectado", "No se detecta ningún dispositivo USB-serie.", "Conecta el cable USB de datos del ESP32 (no uno solo de carga) y pulsa «Buscar ESP32».");
            else
                Poner("usb", "Cable USB", "ok", "Conectado", disp is null ? "Hay al menos un puerto serie en el equipo." : $"{disp.Descripcion}{(disp.Vid.Length > 0 ? $" · VID {disp.Vid} · PID {disp.Pid}" : "")}");

            // 2. Puerto COM
            if (_esp32.Puerto is { } abierto) Poner("com", "Puerto COM", "ok", abierto, "Abierto por este panel (único dueño del puerto).");
            else if (conexion == EstadoConexion.PuertoOcupado) Poner("com", "Puerto COM", "mal", puerto ?? "Ocupado", "Otro programa tiene abierto el puerto.", "Cierra el Monitor Serie de Arduino IDE u otro programa que use el puerto y pulsa «Buscar ESP32».");
            else if (puerto is not null) Poner("com", "Puerto COM", "aviso", puerto, "Detectado, pero el panel no lo tiene abierto.", "Pulsa «Buscar ESP32».");
            else Poner("com", "Puerto COM", "mal", "No detectado", "Ningún puerto COM corresponde a una placa ESP32.", "Revisa el cable y el controlador USB-serie (CP210x o CH340) en el Administrador de dispositivos.");

            // 3. Identidad de la placa
            if (disp is null) Poner("identidad", "Identidad ESP32", "inactivo", "Desconocida", "Sin dispositivo que identificar.");
            else if (EnlaceEsp32.VidConocidos.Contains(disp.Vid)) Poner("identidad", "Identidad ESP32", "ok", "Placa reconocida", $"Puente USB-serie habitual de ESP32 (VID {disp.Vid}).");
            else Poner("identidad", "Identidad ESP32", conexion == EstadoConexion.Conectado ? "ok" : "aviso", "Sin VID conocido",
                conexion == EstadoConexion.Conectado ? "El puente USB no es de un fabricante habitual, pero el firmware respondió." : "El dispositivo no tiene un VID de ESP32 conocido.",
                conexion == EstadoConexion.Conectado ? null : "Comprueba que el dispositivo conectado sea la placa de la estación.");

            // 4. Firmware
            string? fw = esp is not null && esp.TryGetValue("firmware", out var f) ? f as string : null;
            if (fw is null) Poner("firmware", "Firmware", "inactivo", "No verificable", "Se verifica cuando el ESP32 responde.", conexion == EstadoConexion.SinRespuesta ? "Carga hardware/CalibracionTresCanecas (v2) en la placa." : null);
            else if (!fw.EndsWith("v1", StringComparison.Ordinal)) Poner("firmware", "Firmware", "ok", fw, "Calibración y aperturas simultáneas (orden GRUPO).");
            else Poner("firmware", "Firmware", "aviso", fw, "Versión sin aperturas simultáneas: las combinaciones fallarán.", "Carga hardware/CalibracionTresCanecas v2 con Arduino IDE.");

            // 5. Comunicación serial
            if (conexion == EstadoConexion.Conectado && _esp32.UltimaRespuesta is { } ur)
            {
                int s = Math.Max(0, (int)(DateTime.Now - ur).TotalSeconds);
                Poner("serial", "Comunicación serial", s <= 5 ? "ok" : "aviso", s <= 5 ? "Responde" : "Sin actualización", $"Última respuesta a ESTADO {Antiguedad(s)} ({ur:HH:mm:ss}).", s <= 5 ? null : "Pulsa «Probar comunicación»; si no responde, desconecta y conecta el USB.");
            }
            else Poner("serial", "Comunicación serial", "mal", "Sin respuesta", (_esp32.UltimoError ?? EnlaceEsp32.Texto(conexion)) + (_esp32.UltimaRespuesta is { } u2 ? $" Última respuesta: {u2:HH:mm:ss}." : ""), "Revisa el cable y pulsa «Buscar ESP32». La posición de las tapas es desconocida.");

            // 6. Canales GPIO
            if (canecas.Count == 3)
                Poner("canales", "Canales GPIO", "ok", "3 configurados", string.Join(" · ", canecas.Select(k => $"{Nombre[(string)k["c"]]} GPIO {k["pin"]}")) + ". Un solo ESP32; sin medición de posición.");
            else Poner("canales", "Canales GPIO", "inactivo", "Sin datos", "Esperados: verde GPIO 23, blanca GPIO 19, negra GPIO 18.");

            // 7. Calibraciones
            if (canecas.Count == 0) Poner("calibracion", "Calibraciones", "inactivo", "Sin datos", "Se leen del ESP32 cuando está conectado.");
            else
            {
                var pend = canecas.Where(k => k.TryGetValue("pendiente", out var p) && p is true).Select(k => Nombre[(string)k["c"]]).ToList();
                Poner("calibracion", "Calibraciones", pend.Count == 0 ? "ok" : "aviso", pend.Count == 0 ? "Guardadas" : $"{pend.Count} sin guardar",
                    string.Join(" · ", canecas.Select(k => $"{Nombre[(string)k["c"]]} {k["cerrado"]}°/{k["abierto"]}° {Convert.ToInt32(k["ms"]) / 1000.0:0.#} s")),
                    pend.Count == 0 ? null : $"Guarda o restaura la calibración de: {string.Join(", ", pend)}.");
            }

            // 8–10. Bot, Oracle y operaciones pendientes (según el pulso del bot; el panel no abre conexión a Oracle).
            var pulso = LeerPulso();
            bool vigente = pulso.Existe && pulso.Vivo && pulso.Hace is < PulsoVigenteS;
            string edad = pulso.Hace is { } hh ? Antiguedad(hh) : "sin fecha";
            if (!pulso.Existe) Poner("bot", "Bot de Telegram", "inactivo", "Sin pulso", "No hay archivo de pulso del bot en esta estación.", "Inicia el servicio de Telegram de la estación.");
            else if (!pulso.Vivo) Poner("bot", "Bot de Telegram", "mal", "Detenido", $"El proceso del bot no está en ejecución (último pulso {edad}).", "Inicia el servicio de Telegram de la estación.");
            else if (!vigente) Poner("bot", "Bot de Telegram", "aviso", "Sin actualización", $"El proceso existe, pero su último pulso es de {edad}.", "Revisa el registro del bot; reinícialo si sigue sin pulso.");
            else Poner("bot", "Bot de Telegram", "ok", "Conectado", $"Pulso {edad}.");

            if (!vigente || pulso.Oracle is null) Poner("oracle", "Oracle", "inactivo", "Desconocido", pulso.Existe ? $"Sin un pulso vigente del bot que lo confirme (último {edad})." : "Sin pulso del bot que lo confirme.", "Oracle se comprueba a través del bot: inícialo para conocer su estado.");
            else if (pulso.Oracle == true) Poner("oracle", "Oracle", "ok", "Conectado", $"Confirmado por el bot {edad}.");
            else Poner("oracle", "Oracle", "mal", "Sin conexión", $"El bot no pudo conectarse ({edad}).", "Revisa la red y la base de datos; las operaciones quedan en cola local.");

            if (!vigente) Poner("pendientes", "Operaciones por subir a Oracle", "inactivo", "Desconocido", "Requiere un pulso vigente del bot.");
            else Poner("pendientes", "Operaciones por subir a Oracle", pulso.Pendientes == 0 ? "ok" : "aviso", pulso.Pendientes == 0 ? "Ninguna" : $"{pulso.Pendientes} en cola",
                pulso.Pendientes == 0 ? "No hay operaciones esperando registro." : "Se subirán cuando Oracle responda.", pulso.Pendientes == 0 ? null : "Verifica la conexión a Oracle.");

            // 11. Pasarela y túnel HTTPS
            if (!_pasarelaActiva) Poner("pasarela", "Pasarela web y túnel", "inactivo", "Detenida", "La pasarela ciudadana (127.0.0.1:8093) no responde. El panel funciona sin ella.", "Inicia la pasarela si se usará la web ciudadana.");
            else if (tunel is null) Poner("pasarela", "Pasarela web y túnel", "aviso", "Sin túnel", "La pasarela responde, pero no tiene túnel HTTPS público.", "Reinicia el túnel desde la pasarela.");
            else
            {
                var (alcanzable, detalle) = await tTunel;
                Poner("pasarela", "Pasarela web y túnel", alcanzable ? "ok" : "mal", alcanzable ? "Túnel en línea" : "Túnel sin respuesta", $"{tunel} · {detalle}", alcanzable ? null : "Reinicia el túnel desde la pasarela y genera un QR nuevo.");
            }

            // 12. Reconocimiento visual (Ollama)
            var (ollamaOk, modelos) = await tOllama;
            string? esperado = pulso.Modelo;
            if (!ollamaOk) Poner("vision", "Reconocimiento visual", "mal", "Sin respuesta", "Ollama no responde en 127.0.0.1:11434.", "Inicia Ollama («ollama serve») o el servicio de la estación.");
            else if (modelos.Count == 0) Poner("vision", "Reconocimiento visual", "aviso", "Sin modelo cargado", "Ollama responde; el modelo se cargará en la primera consulta (más lenta).", esperado is null ? null : $"Precarga «{esperado}».");
            else if (esperado is not null && !modelos.Contains(esperado)) Poner("vision", "Reconocimiento visual", "aviso", "Otro modelo", $"Cargado: {string.Join(", ", modelos)}; el bot usa «{esperado}».", $"Carga «{esperado}».");
            else Poner("vision", "Reconocimiento visual", "ok", "Modelo cargado", string.Join(", ", modelos));

            // 13. Errores recientes del historial
            List<(string hora, string tipo, string texto)> errores;
            lock (Historial) errores = Historial.Select(x => (Dictionary<string, object>)x).Where(x => (string)x["tipo"] is "error" or "rechazo").Reverse().Take(3)
                .Select(x => ((string)x["hora"], (string)x["tipo"], (string)x["texto"])).ToList();
            int totalErr; lock (Historial) totalErr = Historial.Count(x => (string)((Dictionary<string, object>)x)["tipo"] is "error" or "rechazo");
            Poner("errores", "Errores recientes", totalErr == 0 ? "ok" : "aviso", totalErr == 0 ? "Ninguno" : $"{totalErr} en el historial",
                totalErr == 0 ? "Sin errores ni rechazos desde que inició el servicio." : string.Join(" · ", errores.Select(e => $"{e.hora} {e.texto}")),
                totalErr == 0 ? null : "Revisa el historial técnico.");

            return new
            {
                ok = true,
                hora = DateTime.Now.ToString("HH:mm:ss"),
                resumen = new { ok = c.Count(x => (string)x["estado"]! == "ok"), aviso = c.Count(x => (string)x["estado"]! == "aviso"), mal = c.Count(x => (string)x["estado"]! == "mal"), inactivo = c.Count(x => (string)x["estado"]! == "inactivo") },
                componentes = c,
            };
        }

        private static async Task<(bool ok, List<string> modelos)> ConsultarOllamaAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var r = await HttpDiagnostico.GetAsync("http://127.0.0.1:11434/api/ps", cts.Token);
                if (!r.IsSuccessStatusCode) return (false, new());
                var d = Json.DeserializeObject(await r.Content.ReadAsStringAsync()) as Dictionary<string, object>;
                var lista = (d is not null && d.TryGetValue("models", out var m) ? m as object[] : null)?.OfType<Dictionary<string, object>>()
                    .Select(x => x.TryGetValue("name", out var n) ? n as string : null).Where(n => n is not null).Select(n => n!).ToList() ?? new();
                return (true, lista);
            }
            catch (Exception) { return (false, new()); }
        }

        private static async Task<(bool ok, string detalle)> ConsultarTunelAsync(string url)
        {
            var reloj = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using var r = await HttpDiagnostico.GetAsync(url, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
                int codigo = (int)r.StatusCode;
                // Cloudflare responde 530/502 cuando el túnel ya no llega a la pasarela.
                return (codigo < 500, $"HTTP {codigo} en {reloj.ElapsedMilliseconds} ms");
            }
            catch (Exception e) { return (false, "sin respuesta (" + (e is TaskCanceledException ? "tiempo agotado" : e.GetBaseException().Message) + ")"); }
        }

        private static bool ProcesoVivo(int pid)
        {
            try { using var p = System.Diagnostics.Process.GetProcessById(pid); return !p.HasExited && p.ProcessName.StartsWith("EcoAccess.P3.Bot", StringComparison.OrdinalIgnoreCase); }
            catch (Exception) { return false; }
        }

        // ───── Aperturas automáticas compartidas por PC, Telegram y la web (único dueño del puerto del ESP32) ─────
        private static readonly HashSet<string> ClavesEjecutadas = new(StringComparer.Ordinal);
        private static readonly Queue<string> OrdenClaves = new();
        private static readonly SemaphoreSlim TurnoGrupo = new(1, 1);
        private static readonly string[] Combinaciones = { "V", "B", "N", "VB", "VN", "BN", "VBN" };

        /// <summary>Política automática: 1 caneca 7 s, 2 canecas 14 s, 3 canecas 21 s (según canecas DISTINTAS).</summary>
        internal static int DuracionPolitica(int canecas) => canecas <= 1 ? 7000 : canecas == 2 ? 14000 : 21000;
        internal const int EscalonSimultaneoMs = 250;

        /// <summary>
        /// POST /api/estacion/abrir-grupo {canecas:"VBN", modalidad:"simultanea"|"secuencial", origen, clave, solicitante, materiales}.
        /// Idempotente por «clave» (una entrega repetida de Telegram o un doble clic no abre dos veces). Exige sesión supervisada.
        /// </summary>
        private static async Task AbrirGrupoAsync(HttpListenerResponse res, Dictionary<string, object> cuerpo, bool local)
        {
            // Se respeta el orden pedido (en secuencial es el orden de apertura); sin repetir canecas.
            string canecas = new string((Texto(cuerpo, "canecas") ?? "").ToUpperInvariant().Where(ch => ch is 'V' or 'B' or 'N').Distinct().ToArray());
            if (canecas.Length == 0) { await ResponderAsync(res, 400, new { ok = false, error = "Indica al menos una caneca (V, B o N)." }); return; }
            string modalidad = Texto(cuerpo, "modalidad") == "secuencial" ? "secuencial" : "simultanea";
            string origen = Texto(cuerpo, "origen") ?? (local ? "web" : "remoto");
            string clave = Texto(cuerpo, "clave") ?? Guid.NewGuid().ToString("N");
            if (clave.Length > 100) clave = clave.Substring(0, 100);
            string solicitante = Texto(cuerpo, "solicitante") ?? origen;
            string materiales = Texto(cuerpo, "materiales") ?? "";
            int ms = DuracionPolitica(canecas.Length);

            await TurnoGrupo.WaitAsync();
            try
            {
                bool duplicada; lock (ClavesEjecutadas) duplicada = ClavesEjecutadas.Contains(clave);
                if (duplicada)
                {
                    Anotar("rechazo", $"Solicitud duplicada ({origen}) bloqueada: no se repitió la apertura.");
                    await ResponderAsync(res, 409, new { ok = false, duplicada = true, error = "Solicitud duplicada: esta operación ya se ejecutó y no se repite." });
                    return;
                }
                string nombres = string.Join(" + ", canecas.Select(ch => Nombre[ch.ToString()]));
                // Simultánea: arranque escalonado de 250 ms para no sumar el pico de corriente de los tres servos; todas quedan
                // abiertas a la vez. Secuencial: cada tapa abre, espera el tiempo del grupo y cierra antes de la siguiente.
                int escalon = modalidad == "simultanea" ? EscalonSimultaneoMs : ms + 1200;
                string orden = $"GRUPO {canecas} {ms} {escalon}";
                string r;
                try { r = await _esp32.EnviarAsync(orden); }
                catch (Exception e)
                {
                    Anotar("error", $"Apertura {nombres} ({origen}): {e.Message}");
                    await ResponderAsync(res, 503, new { ok = false, error = e.Message }); return;
                }
                var d = Json.DeserializeObject(r) as Dictionary<string, object>;
                bool ok = d is not null && d.TryGetValue("ok", out var o) && o is true;
                if (!ok)
                {
                    string err = Traducir(d?["error"]?.ToString());
                    if (err.StartsWith("uso:", StringComparison.Ordinal) || err == "orden desconocida") err = "El ESP32 tiene el firmware v1 (sin aperturas simultáneas). Carga hardware/CalibracionTresCanecas v2.";
                    Anotar("rechazo", $"Apertura {nombres} ({origen}) → {err}");
                    await ResponderAsync(res, 409, new { ok = false, error = err }); return;
                }
                lock (ClavesEjecutadas)
                {
                    ClavesEjecutadas.Add(clave); OrdenClaves.Enqueue(clave);
                    while (OrdenClaves.Count > 2000) ClavesEjecutadas.Remove(OrdenClaves.Dequeue());
                }
                int finMs = modalidad == "simultanea" ? ms + (canecas.Length - 1) * escalon + 1000 : canecas.Length * (ms + 1200);
                string texto = $"Apertura {(modalidad == "simultanea" ? "simultánea" : "secuencial")} {nombres} · {ms / 1000} s ({origen})";
                Anotar("orden", texto);
                lock (UltimaOperacion) foreach (char ch in canecas) UltimaOperacion[ch.ToString()] = $"{origen}: abierta {ms / 1000} s · {DateTime.Now:HH:mm:ss}";
                RegistrarOperacionWeb(origen, clave, solicitante, canecas, ms, modalidad, materiales);
                _ = _esp32.EnviarAsync("ESTADO").ContinueWith(_ => { });
                await ResponderAsync(res, 200, new { ok = true, canecas, duracionMs = ms, modalidad, escalonMs = escalon, finEstimadoMs = finMs, clave,
                    msg = texto + ". PWM aplicado por el ESP32; la apertura física se confirma por observación." });
            }
            finally { TurnoGrupo.Release(); }
        }

        /// <summary>
        /// Las operaciones originadas en la web de control se anotan en un archivo compartido de la estación; el servicio de
        /// Telegram (que tiene la conexión a Oracle) las sube como «prueba técnica» o «apertura manual» del canal Web.
        /// PC y Telegram registran en Oracle sus propias operaciones, así que aquí solo se anotan las de la web.
        /// </summary>
        private static void RegistrarOperacionWeb(string origen, string clave, string solicitante, string canecas, int ms, string modalidad, string materiales)
        {
            if (origen != "web") return;
            try
            {
                string archivo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Panel", "operaciones-web.jsonl");
                string linea = Json.Serialize(new { clave = "web:" + clave, fechaUtc = DateTime.UtcNow.ToString("o"), canal = "WEB", tipo = "APERTURA_MANUAL", solicitante, canecas, duracionMs = ms, modalidad, materiales });
                lock (Historial) File.AppendAllText(archivo, linea + Environment.NewLine, new UTF8Encoding(false));
            }
            catch (Exception e) { Anotar("error", "No se pudo anotar la operación para Oracle: " + e.Message); }
        }

        private static async Task PruebaCombinacionesAsync(bool secuencial)
        {
            Anotar("prueba", $"Inicio de la prueba C01–C07 ({(secuencial ? "secuencial" : "simultánea")}).");
            for (int i = 0; i < Combinaciones.Length; i++)
            {
                if (DateTime.UtcNow >= _sesionHastaUtc) { Anotar("prueba", "Prueba C01–C07 detenida: el control se desactivó."); return; }
                string c = Combinaciones[i]; int ms = DuracionPolitica(c.Length);
                int escalon = secuencial ? ms + 1200 : EscalonSimultaneoMs;
                try
                {
                    string r = await _esp32.EnviarAsync($"GRUPO {c} {ms} {escalon}");
                    Anotar(r.Contains("\"ok\":true") ? "prueba" : "rechazo", $"C0{i + 1}: {string.Join(" + ", c.Select(ch => Nombre[ch.ToString()]))} · {ms / 1000} s{(r.Contains("\"ok\":true") ? "" : " → " + r)}");
                }
                catch (Exception e) { Anotar("error", $"C0{i + 1}: {e.Message}"); return; }
                int espera = secuencial ? c.Length * (ms + 1200) : ms + (c.Length - 1) * escalon + 1000;
                await Task.Delay(espera + 1500);
            }
            Anotar("prueba", "Prueba C01–C07 terminada. La apertura física se confirma por observación.");
        }

        private static string? Texto(Dictionary<string, object> d, string k) =>
            d.TryGetValue(k, out var v) && v is not null && Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) is { Length: > 0 } s ? s.Trim() : null;

        private static async Task GuardarODescartarAsync(HttpListenerResponse res, string c, string accion)
        {
            string orden = (accion == "guardar" ? "GUARDAR" : "DESCARTAR") + (c.Length > 0 ? " " + c : "");
            string quien = c.Length > 0 ? Nombre[c] : "Las tres canecas";
            string r;
            try { r = await _esp32.EnviarAsync(orden); }
            catch (Exception e) { await ResponderAsync(res, 503, new { ok = false, error = e.Message }); return; }
            var d = Json.DeserializeObject(r) as Dictionary<string, object>;
            if (d is null || !(d.TryGetValue("ok", out var o) && o is true)) { Anotar("rechazo", $"{quien}: {accion} → {d?["error"]}"); await ResponderAsync(res, 409, d ?? new()); return; }
            // Verificación real: volver a leer el ESP32 y comprobar que no quedan cambios pendientes en lo guardado.
            try
            {
                var estado = Json.DeserializeObject(await _esp32.EnviarAsync("ESTADO")) as Dictionary<string, object>;
                var canecas = (estado?["canecas"] as object[] ?? Array.Empty<object>()).OfType<Dictionary<string, object>>();
                bool verificado = canecas.Where(x => c.Length == 0 || (string)x["c"] == c).All(x => x["pendiente"] is false);
                string texto = accion == "guardar"
                    ? (verificado ? $"{quien}: calibración guardada y verificada en el ESP32" : $"{quien}: el ESP32 no confirmó el guardado")
                    : $"{quien}: se restauró la última calibración guardada";
                Anotar(verificado ? "calibracion" : "error", texto);
                await ResponderAsync(res, verificado ? 200 : 500, new { ok = verificado, msg = texto, error = verificado ? null : texto });
            }
            catch (Exception e) { await ResponderAsync(res, 503, new { ok = false, error = "Guardado sin verificar: " + e.Message }); }
        }

        private static async Task<bool> OrdenAsync(HttpListenerResponse res, string orden, string descripcion)
        {
            string r;
            try { r = await _esp32.EnviarAsync(orden); }
            catch (Exception e) { Anotar("error", descripcion + ": " + e.Message); await ResponderAsync(res, 503, new { ok = false, error = e.Message }); return false; }
            var d = Json.DeserializeObject(r) as Dictionary<string, object>;
            bool ok = d is not null && d.TryGetValue("ok", out var o) && o is true;
            Anotar(ok ? "orden" : "rechazo", descripcion + (ok ? "" : " → " + Traducir(d?["error"]?.ToString())));
            await ResponderAsync(res, ok ? 200 : 409, ok ? d! : new Dictionary<string, object> { ["ok"] = false, ["error"] = Traducir(d?["error"]?.ToString()) });
            _ = _esp32.EnviarAsync("ESTADO").ContinueWith(_ => { });   // refrescar el estado mostrado cuanto antes
            return ok;
        }

        private static string Traducir(string? e) => e switch
        {
            "ocupado" => "Hay otra caneca en movimiento: espera a que termine o pulsa Detener.",
            "angulos fuera de 0..180" => "Los ángulos deben estar entre 0° y 180°.",
            "el recorrido debe ser de al menos 5 grados" => "El recorrido debe ser de al menos 5°.",
            "tiempo fuera de 1000..30000 ms" => "El tiempo debe estar entre 1 y 30 segundos.",
            null => "Respuesta inválida del ESP32.",
            _ => e,
        };

        private static string Interpretar(string evento)
        {
            var m = Regex.Match(evento, "\"evt\":\"(\\w+)\"(?:,\"c\":\"([VBN])\")?");
            if (!m.Success) return evento;
            string c = m.Groups[2].Success ? Nombre[m.Groups[2].Value] : "";
            return m.Groups[1].Value switch
            {
                "abierta" => $"{c}: PWM de apertura aplicado",
                "cerrando" => $"{c}: PWM de cierre aplicado",
                "cerrada" => $"{c}: ciclo completado (PWM apagado)",
                "inicio" => "El ESP32 se reinició (ninguna tapa se movió al encender).",
                _ => evento,
            };
        }

        private static async Task ArchivoAsync(HttpListenerResponse res, string ruta)
        {
            string nombre = ruta.Length == 0 ? "index.html" : ruta.TrimStart('/');
            string completo = Path.GetFullPath(Path.Combine(_wwwroot, nombre));
            if (!completo.StartsWith(_wwwroot, StringComparison.OrdinalIgnoreCase) || !File.Exists(completo)) { res.StatusCode = 404; res.Close(); return; }
            res.ContentType = Path.GetExtension(completo).ToLowerInvariant() switch { ".html" => "text/html; charset=utf-8", ".js" => "text/javascript; charset=utf-8", ".css" => "text/css; charset=utf-8", ".png" => "image/png", ".ico" => "image/x-icon", ".svg" => "image/svg+xml", ".webp" => "image/webp", ".woff2" => "font/woff2", _ => "application/octet-stream" };
            res.Headers["Cache-Control"] = Path.GetExtension(completo) is ".png" or ".webp" or ".woff2" ? "max-age=86400" : "no-store";
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
                Historial.Add(new Dictionary<string, object> { ["hora"] = DateTime.Now.ToString("HH:mm:ss"), ["tipo"] = tipo, ["texto"] = texto });
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
