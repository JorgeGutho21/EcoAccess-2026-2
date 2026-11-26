using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Vision35.Expo
{
    /// <summary>Canal que originó la operación (columna CANAL de OPERACION_ESTACION).</summary>
    public enum CanalEstacion { PC, TELEGRAM, WEB }

    /// <summary>Tipo de operación: nunca se confunde una prueba técnica con una disposición.</summary>
    public enum TipoOperacionEstacion { PRUEBA_TECNICA, APERTURA_MANUAL, RECONOCIMIENTO_AUTOMATICO, DISPOSICION_DECLARADA }

    /// <summary>Estados del registro de una operación (columna ESTADO_OPERACION).</summary>
    public enum EstadoOperacionEstacion
    {
        RECIBIDA, CLASIFICADA, PENDIENTE_APERTURA, EN_EJECUCION, CICLO_COMPLETADO, DISPOSICION_CONFIRMADA, REGISTRADA, ERROR, PENDIENTE_SINCRONIZACION,
    }

    /// <summary>Qué confirmó el hardware. PWM_CONFIRMADO NO significa que la tapa se movió: no hay sensores.</summary>
    public enum EstadoHardwareEstacion { PWM_CONFIRMADO, SIMULADO, RECHAZADO, SIN_RESPUESTA, NO_SOLICITADO, DUPLICADA_BLOQUEADA }

    /// <summary>Una fila de OPERACION_ESTACION (V030). Sin la foto ni el identificador de Telegram.</summary>
    public sealed record OperacionEstacion(
        string Clave,
        DateTime FechaUtc,
        CanalEstacion Canal,
        TipoOperacionEstacion Tipo,
        int? IdUsuario,
        string? Solicitante,
        string Estacion,
        string? Materiales,
        string? Categorias,
        string? Canecas,
        int? CantidadResiduos,
        int DuracionMs,
        string Modalidad,
        string? ResultadoReconocimiento,
        EstadoHardwareEstacion EstadoHardware,
        EstadoOperacionEstacion Estado,
        bool DisposicionDeclarada = false,
        bool DisposicionVerificada = false,
        string? ModeloVision = null,
        long? LatenciaMs = null,
        bool SincronizadaOffline = false,
        string? Detalle = null,
        string? TipoUsuario = null,
        string? Metodo = null,
        double? Confianza = null,
        string? SesionAnonima = null,
        IReadOnlyList<DetalleOperacionEstacion>? Detalles = null);

    /// <summary>
    /// Un residuo dentro de una operación (V033, OPERACION_ESTACION_DETALLE). Varios residuos de la misma caneca son
    /// varias filas de detalle de UNA operación; la apertura se calcula por canecas distintas.
    /// </summary>
    public sealed record DetalleOperacionEstacion(string Codigo, string Nombre, string? MaterialOracle, string Caneca, int Cantidad, string Metodo, double? Confianza = null);

    /// <summary>Persistencia de operaciones (Oracle Cloud). Idempotente por <see cref="OperacionEstacion.Clave"/>.</summary>
    public interface IRegistroOperacionesEstacion
    {
        /// <summary>Inserta o actualiza (MERGE por clave). Lanza si Oracle no confirmó.</summary>
        Task GuardarAsync(OperacionEstacion operacion, CancellationToken ct = default);
        Task<IReadOnlyList<OperacionEstacion>> RecientesAsync(int? idUsuario, int maximo, CancellationToken ct = default);
    }

    /// <summary>
    /// Registro con cola local persistente: si Oracle no responde, la operación queda en
    /// %ProgramData%\EcoAccessP3Framework\Estacion\operaciones-pendientes.jsonl y se reintenta en la siguiente operación o
    /// con <see cref="SincronizarAsync"/>. Re-sincronizar nunca repite una apertura física: solo escribe la fila (MERGE por clave).
    /// </summary>
    public sealed class RegistroOperacionesConCola
    {
        private static readonly JsonSerializerOptions Opciones = new() { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
        private readonly IRegistroOperacionesEstacion? _oracle;
        private readonly string _archivo;
        private readonly string? _archivoWeb;
        private readonly string? _archivoCiudadano;
        private readonly string _sincronizadas;
        private readonly SemaphoreSlim _turno = new(1, 1);

        public RegistroOperacionesConCola(IRegistroOperacionesEstacion? oracle, string? carpeta = null, bool subirOperacionesWeb = false)
        {
            _oracle = oracle;
            string baseDir = carpeta ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework");
            _archivo = Path.Combine(baseDir, "Estacion", "operaciones-pendientes.jsonl");
            _archivoWeb = subirOperacionesWeb ? Path.Combine(baseDir, "Panel", "operaciones-web.jsonl") : null;
            // Bitácora de la pasarela ciudadana (GitHub Pages → pasarela → panel). Solo la escribe la pasarela; aquí se lee.
            _archivoCiudadano = subirOperacionesWeb ? Path.Combine(baseDir, "Ciudadano", "operaciones-ciudadanas.jsonl") : null;
            _sincronizadas = Path.Combine(baseDir, "Estacion", "bitacoras-sincronizadas.txt");
        }

        /// <summary>Eventos de las bitácoras web aún no confirmados por Oracle (para el panel y el informe).</summary>
        public int PendientesWeb
        {
            get
            {
                try
                {
                    var hechas = LeerSincronizadas();
                    return new[] { _archivoWeb, _archivoCiudadano }.Where(a => a is not null && File.Exists(a))
                        .SelectMany(a => File.ReadAllLines(a!)).Count(l => IdBitacora(l) is { } id && !hechas.Contains(id));
                }
                catch (IOException) { return -1; }
            }
        }

        public bool OracleConfigurado => _oracle is not null;
        public IRegistroOperacionesEstacion? Oracle => _oracle;

        /// <summary>Registra en Oracle o deja pendiente. Devuelve true si Oracle confirmó la transacción.</summary>
        public async Task<bool> RegistrarAsync(OperacionEstacion op, CancellationToken ct = default)
        {
            if (_oracle is null) { Encolar(op with { Estado = EstadoOperacionEstacion.PENDIENTE_SINCRONIZACION }); return false; }
            try
            {
                await _oracle.GuardarAsync(op, ct).ConfigureAwait(false);
                _ = Task.Run(() => SincronizarAsync(CancellationToken.None));
                return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception)
            {
                Encolar(op with { Estado = EstadoOperacionEstacion.PENDIENTE_SINCRONIZACION, SincronizadaOffline = true });
                return false;
            }
        }

        public int Pendientes
        {
            get { try { return File.Exists(_archivo) ? File.ReadAllLines(_archivo).Count(l => l.Length > 2) : 0; } catch (IOException) { return -1; } }
        }

        /// <summary>Sube las pendientes (y, en el anfitrión, las de la web de control). Devuelve cuántas quedaron registradas.</summary>
        public async Task<int> SincronizarAsync(CancellationToken ct = default)
        {
            if (_oracle is null) return 0;
            if (!await _turno.WaitAsync(0, ct).ConfigureAwait(false)) return 0;
            int subidas = 0;
            try
            {
                subidas += await SubirArchivoAsync(_archivo, l => JsonSerializer.Deserialize<OperacionEstacion>(l, Opciones), ct).ConfigureAwait(false);
                if (_archivoWeb is not null) subidas += await SubirBitacoraAsync(_archivoWeb, DesdeWeb, ct).ConfigureAwait(false);
                if (_archivoCiudadano is not null) subidas += await SubirBitacoraAsync(_archivoCiudadano, DesdeCiudadano, ct).ConfigureAwait(false);
            }
            finally { _turno.Release(); }
            return subidas;
        }

        /// <summary>
        /// Sincronizador de bitácoras que otro proceso sigue escribiendo (panel y pasarela ciudadana): NO se reescriben.
        /// Cada evento tiene un identificador único; tras el COMMIT de Oracle (MERGE por clave, idempotente) el identificador
        /// se anota en bitacoras-sincronizadas.txt, que sobrevive a reinicios. Un fallo transitorio deja el evento para el
        /// siguiente ciclo (cada 30 s). Aquí solo hay registros: sincronizar nunca vuelve a mover una caneca.
        /// </summary>
        private async Task<int> SubirBitacoraAsync(string archivo, Func<string, OperacionEstacion?> leer, CancellationToken ct)
        {
            if (!File.Exists(archivo)) return 0;
            string[] lineas;
            try { using var fs = new FileStream(archivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete); using var sr = new StreamReader(fs, Encoding.UTF8); lineas = sr.ReadToEnd().Split('\n'); }
            catch (IOException) { return 0; }
            var hechas = LeerSincronizadas();
            int subidas = 0;
            foreach (string l in lineas.Select(x => x.Trim()).Where(x => x.Length > 2))
            {
                string? id = IdBitacora(l);
                if (id is null || hechas.Contains(id)) continue;
                OperacionEstacion? op = null;
                try { op = leer(l); } catch (Exception) { }
                if (op is null) { Anotar(id); hechas.Add(id); continue; }   // línea no válida: se descarta una sola vez, sin reintentos infinitos
                // Una confirmación del ciudadano se aplica sobre la fila de su apertura: si esa fila aún no está en Oracle, espera.
                if (op.Tipo == TipoOperacionEstacion.DISPOSICION_DECLARADA && !hechas.Contains(op.Clave)) continue;
                try
                {
                    await _oracle!.GuardarAsync(op, ct).ConfigureAwait(false);
                    Anotar(id); hechas.Add(id); subidas++;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception) { break; }   // Oracle no disponible: se reintenta todo lo pendiente en el siguiente ciclo
            }
            return subidas;
        }

        private HashSet<string> LeerSincronizadas()
        {
            try { return File.Exists(_sincronizadas) ? new HashSet<string>(File.ReadAllLines(_sincronizadas).Where(x => x.Length > 0), StringComparer.Ordinal) : new(StringComparer.Ordinal); }
            catch (IOException) { return new(StringComparer.Ordinal); }
        }

        private void Anotar(string id)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_sincronizadas)!);
            lock (_sincronizadas) File.AppendAllText(_sincronizadas, id + Environment.NewLine, new UTF8Encoding(false));
        }

        /// <summary>Identificador único del evento: «idOperacion» (pasarela) o «clave» (panel).</summary>
        internal static string? IdBitacora(string linea)
        {
            try
            {
                using var d = JsonDocument.Parse(linea);
                var r = d.RootElement;
                string? id = r.TryGetProperty("idOperacion", out var a) ? a.GetString() : r.TryGetProperty("clave", out var b) ? b.GetString() : null;
                return string.IsNullOrWhiteSpace(id) || id!.Length > 100 ? null : id;
            }
            catch (JsonException) { return null; }
        }

        /// <summary>
        /// Evento de la pasarela ciudadana → fila de OPERACION_ESTACION (canal WEB). La apertura usa «web:&lt;clave&gt;» como clave
        /// de idempotencia; la confirmación del ciudadano actualiza esa misma fila a DISPOSICION_CONFIRMADA (declarada, sin sensor).
        /// </summary>
        internal static OperacionEstacion? DesdeCiudadano(string linea)
        {
            using var d = JsonDocument.Parse(linea);
            var r = d.RootElement;
            string Txt(string k) => r.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
            int? Ent(string k) => r.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int n) ? n : null;
            DateTime fecha = DateTime.Parse(Txt("fechaUtc"), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            int? usuario = Ent("idUsuario") is int u && u > 0 ? u : null;
            string solicitante = Recortar("web-ciudadana" + (Txt("sesion").Length > 0 ? ":" + Txt("sesion") : ""), 60)!;
            if (Txt("tipo") == "DISPOSICION_CONFIRMADA_CIUDADANO")
            {
                string origen = Txt("operacionOrigen");
                if (!origen.StartsWith("web:", StringComparison.Ordinal)) return null;
                return new OperacionEstacion(origen, fecha, CanalEstacion.WEB, TipoOperacionEstacion.DISPOSICION_DECLARADA, usuario, solicitante,
                    ServicioEstacionExpo.NombreEstacion, null, null, null, null, 0, "NINGUNA", null, EstadoHardwareEstacion.PWM_CONFIRMADO,
                    EstadoOperacionEstacion.DISPOSICION_CONFIRMADA, DisposicionDeclarada: true, SincronizadaOffline: true,
                    Detalle: "Depósito declarado por la persona en la web ciudadana (sin sensor de depósito).");
            }
            string clave = Txt("idOperacion");
            if (!clave.StartsWith("web:", StringComparison.Ordinal)) return null;
            string letras = new string("VBN".Where(c => Txt("canecas").ToUpperInvariant().IndexOf(c) >= 0).ToArray());
            if (letras.Length == 0) return null;
            bool comandada = Txt("estadoApertura") == "COMANDADA";
            var mats = new List<string>();
            if (r.TryGetProperty("materiales", out var lista) && lista.ValueKind == JsonValueKind.Array)
                foreach (var m in lista.EnumerateArray())
                    if (m.ValueKind == JsonValueKind.Object)
                        mats.Add(string.Join(" ", new[] { "id", "oracle" }.Select(k => m.TryGetProperty(k, out var x) && x.ValueKind == JsonValueKind.String ? x.GetString() : null).Where(x => !string.IsNullOrEmpty(x))));
            string consulta = Txt("consulta");
            string materiales = string.Join(", ", mats.Where(x => x.Length > 0)) + (consulta.Length > 0 ? " | consulta: " + consulta : "");
            int duracion = comandada ? Math.Max(0, Math.Min(60000, Ent("tiempoProgramadoMs") ?? 0)) : 0;
            string metodo = Txt("metodo") == "VOZ" ? "VOZ" : "TEXTO";
            string invitado = Txt("invitado");
            var detalles = new List<DetalleOperacionEstacion>();
            if (r.TryGetProperty("materiales", out var mats2) && mats2.ValueKind == JsonValueKind.Array)
                foreach (var m in mats2.EnumerateArray())
                {
                    if (m.ValueKind != JsonValueKind.Object) continue;
                    string Prop(string k) => m.TryGetProperty(k, out var x) && x.ValueKind == JsonValueKind.String ? x.GetString() ?? "" : "";
                    string id = Prop("id");
                    if (id.Length == 0 || id.Length > 10 || detalles.Any(d => d.Codigo == id)) continue;
                    string caneca = Prop("caneca") switch { "verde" => "VERDE", "blanca" => "BLANCA", "negra" => "NEGRA", _ => id[0] switch { 'V' => "VERDE", 'B' => "BLANCA", _ => "NEGRA" } };
                    detalles.Add(new DetalleOperacionEstacion(id, Recortar(Prop("nombre").Length > 0 ? Prop("nombre") : id, 80)!, Prop("oracle").Length > 0 ? Recortar(Prop("oracle"), 40) : null, caneca, 1, metodo));
                }
            return new OperacionEstacion(clave, fecha, CanalEstacion.WEB, TipoOperacionEstacion.RECONOCIMIENTO_AUTOMATICO, usuario, solicitante,
                ServicioEstacionExpo.NombreEstacion, Recortar(materiales, 400), CategoriasDe(letras), NombresCanecas(letras), mats.Count == 0 ? null : Math.Min(99, mats.Count),
                duracion, comandada ? (Txt("modo") == "grupo" ? "SIMULTANEA" : "SECUENCIAL") : "NINGUNA", "TEXTO_DETERMINISTA",
                comandada ? EstadoHardwareEstacion.PWM_CONFIRMADO : EstadoHardwareEstacion.RECHAZADO,
                comandada ? EstadoOperacionEstacion.REGISTRADA : EstadoOperacionEstacion.ERROR, SincronizadaOffline: true,
                Detalle: Recortar(comandada ? "Apertura comandada desde la web ciudadana (texto o voz); movimiento confirmado solo por observación." : "Web ciudadana: " + Txt("error"), 300),
                TipoUsuario: usuario is null ? "INVITADO" : "REGISTRADO", Metodo: metodo,
                SesionAnonima: System.Text.RegularExpressions.Regex.IsMatch(invitado, "^[A-Za-z0-9-]{8,40}$") ? invitado : null,
                Detalles: detalles.Count == 0 ? null : detalles);
        }

        private async Task<int> SubirArchivoAsync(string archivo, Func<string, OperacionEstacion?> leer, CancellationToken ct)
        {
            if (!File.Exists(archivo)) return 0;
            string[] lineas;
            try { lineas = File.ReadAllLines(archivo); } catch (IOException) { return 0; }
            var restantes = new List<string>(); int subidas = 0;
            foreach (string l in lineas.Where(x => x.Trim().Length > 2))
            {
                OperacionEstacion? op = null;
                try { op = leer(l); } catch (Exception) { }
                if (op is null) continue;
                try
                {
                    var final = op.Estado == EstadoOperacionEstacion.PENDIENTE_SINCRONIZACION
                        ? op with { Estado = EstadoOperacionEstacion.REGISTRADA, SincronizadaOffline = true } : op;
                    await _oracle!.GuardarAsync(final, ct).ConfigureAwait(false); subidas++;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception) { restantes.Add(l); }
            }
            try { File.WriteAllLines(archivo, restantes, new UTF8Encoding(false)); } catch (IOException) { }
            return subidas;
        }

        private static OperacionEstacion? DesdeWeb(string linea)
        {
            using var d = JsonDocument.Parse(linea);
            var r = d.RootElement;
            string canecas = r.GetProperty("canecas").GetString() ?? "";
            return new OperacionEstacion(r.GetProperty("clave").GetString()!, DateTime.Parse(r.GetProperty("fechaUtc").GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                CanalEstacion.WEB, TipoOperacionEstacion.APERTURA_MANUAL, null, Recortar(r.GetProperty("solicitante").GetString(), 60), ServicioEstacionExpo.NombreEstacion,
                null, CategoriasDe(canecas), NombresCanecas(canecas), null, r.GetProperty("duracionMs").GetInt32(),
                r.GetProperty("modalidad").GetString() == "secuencial" ? "SECUENCIAL" : "SIMULTANEA", null, EstadoHardwareEstacion.PWM_CONFIRMADO,
                EstadoOperacionEstacion.REGISTRADA, SincronizadaOffline: true, Detalle: "Apertura manual desde la web de control de canecas.");
        }

        internal static string NombresCanecas(string letras) => string.Join(",", letras.Select(c => c switch { 'V' => "VERDE", 'B' => "BLANCA", _ => "NEGRA" }));
        internal static string CategoriasDe(string letras) => string.Join(",", letras.Select(c => c switch { 'V' => "ORGANICO_APROVECHABLE", 'B' => "APROVECHABLE", _ => "NO_APROVECHABLE" }));
        internal static string? Recortar(string? s, int n) => s is null ? null : s.Length <= n ? s : s.Substring(0, n);

        private void Encolar(OperacionEstacion op)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_archivo)!);
                lock (_archivo) File.AppendAllText(_archivo, JsonSerializer.Serialize(op, Opciones) + Environment.NewLine, new UTF8Encoding(false));
            }
            catch (IOException) { }
        }
    }

    /// <summary>Respuesta del panel de la estación a una apertura agrupada.</summary>
    public sealed record ResultadoAperturaEstacion(bool Aceptada, bool Duplicada, bool Simulada, string Letras, int DuracionMs, string Modalidad, int FinEstimadoMs, string Mensaje);

    /// <summary>Estado leído del panel de la estación.</summary>
    public sealed record EstadoEstacionExpo(bool Disponible, bool Conectado, bool ControlActivo, string Texto, string? Puerto, string? Firmware);

    /// <summary>La estación física (panel local, único dueño del puerto del ESP32) o su simulación.</summary>
    public interface IEstacionCanecasExpo
    {
        bool EsSimulada { get; }
        Task<EstadoEstacionExpo> EstadoAsync(CancellationToken ct = default);
        Task<ResultadoAperturaEstacion> AbrirAsync(string clave, IReadOnlyList<CategoriaMaterial> canecas, string modalidad, string origen, string solicitante, string? materiales, CancellationToken ct = default);
    }

    /// <summary>
    /// Dónde está el único servicio central de hardware (el panel ECOACCESS ESP32, dueño del puerto COM) y con qué clave
    /// hablarle. Orden: variables ECOACCESS_PANEL_URL / ECOACCESS_PANEL_CLAVE; luego %LOCALAPPDATA%\EcoAccessP3\anfitrion.txt
    /// (líneas «url=http://&lt;anfitrion&gt;:8090» y «clave=…», lo escribe scripts\instalacion\Configurar-Anfitrion.ps1 en los
    /// equipos de los compañeros); por último el panel de este mismo equipo con %ProgramData%\EcoAccessP3Framework\Panel\clave.txt.
    /// Cambiar de anfitrión es volver a ejecutar ese script: no requiere tocar el código.
    /// </summary>
    public static class AnfitrionEstacion
    {
        public const string UrlLocal = "http://127.0.0.1:8090";
        public static string ArchivoConfiguracion => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EcoAccessP3", "anfitrion.txt");

        /// <summary>true si este equipo usa un anfitrión de la red (no su propio panel).</summary>
        public static bool EsRemoto(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var u) && !(u.IsLoopback || string.Equals(u.Host, "localhost", StringComparison.OrdinalIgnoreCase));

        public static (string Url, string? Clave) Leer()
        {
            string? url = Environment.GetEnvironmentVariable("ECOACCESS_PANEL_URL");
            string? clave = Environment.GetEnvironmentVariable("ECOACCESS_PANEL_CLAVE");
            try
            {
                if (File.Exists(ArchivoConfiguracion))
                    (url, clave) = Interpretar(File.ReadAllLines(ArchivoConfiguracion), url, clave);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            url = string.IsNullOrWhiteSpace(url) ? UrlLocal : url!.Trim();
            // También vale para un anfitrión de red: el método anterior copiaba clave.txt del anfitrión en este equipo.
            if (string.IsNullOrWhiteSpace(clave))
            {
                string archivo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Panel", "clave.txt");
                try { clave = File.Exists(archivo) ? File.ReadAllText(archivo).Trim() : null; } catch (IOException) { clave = null; } catch (UnauthorizedAccessException) { clave = null; }
            }
            return (url, string.IsNullOrWhiteSpace(clave) ? null : clave!.Trim());
        }

        /// <summary>Las variables de entorno mandan; el archivo completa lo que falte.</summary>
        public static (string? Url, string? Clave) Interpretar(IEnumerable<string> lineas, string? url, string? clave)
        {
            foreach (var l in lineas)
            {
                int i = l.IndexOf('=');
                if (i <= 0 || l.TrimStart().StartsWith("#", StringComparison.Ordinal)) continue;
                string k = l.Substring(0, i).Trim().ToLowerInvariant(), v = l.Substring(i + 1).Trim();
                if (k == "url" && string.IsNullOrWhiteSpace(url) && v.Length > 0) url = v;
                else if (k == "clave" && string.IsNullOrWhiteSpace(clave) && v.Length > 0) clave = v;
            }
            return (url, clave);
        }
    }

    /// <summary>
    /// Cliente del servicio central de hardware (panel ECOACCESS ESP32, local o del anfitrión; ver <see cref="AnfitrionEstacion"/>).
    /// No abre el puerto COM: así el PC, Telegram y la web nunca compiten por el ESP32.
    /// </summary>
    public sealed class ClienteEstacionPanel : IEstacionCanecasExpo
    {
        private static readonly HttpClient Http = new(new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(6) };
        private readonly string _url;
        private readonly string? _clave;

        public ClienteEstacionPanel(string? url = null)
        {
            var anfitrion = AnfitrionEstacion.Leer();
            _url = (url ?? anfitrion.Url).TrimEnd('/');
            _clave = anfitrion.Clave;
        }

        /// <summary>URL del panel al que habla este equipo (local o el anfitrión de la red).</summary>
        public string Url => _url;

        public bool EsSimulada => false;

        public async Task<EstadoEstacionExpo> EstadoAsync(CancellationToken ct = default)
        {
            try
            {
                using var req = Peticion(HttpMethod.Get, "/api/estacion/estado", null);
                using var res = await Http.SendAsync(req, ct).ConfigureAwait(false);
                using var d = JsonDocument.Parse(await res.Content.ReadAsStringAsync().ConfigureAwait(false));
                var r = d.RootElement;
                // Puede abrir si hay sesión supervisada o si la estación está en servicio (estado por defecto; solo una
                // pausa manual lo desactiva). Paneles anteriores sin «enServicio» se leen como antes.
                bool enServicio = r.TryGetProperty("enServicio", out var es) && es.ValueKind == JsonValueKind.True;
                return new EstadoEstacionExpo(true, r.GetProperty("conectado").GetBoolean(), r.GetProperty("sesionSupervisada").GetBoolean() || enServicio,
                    r.GetProperty("texto").GetString() ?? "", r.TryGetProperty("puerto", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null,
                    r.TryGetProperty("identificador", out var f) && f.ValueKind == JsonValueKind.String ? f.GetString() : null);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception)
            {
                return new EstadoEstacionExpo(false, false, false, "El panel de la estación no está en ejecución (acceso directo «ECOACCESS ESP32»).", null, null);
            }
        }

        public async Task<ResultadoAperturaEstacion> AbrirAsync(string clave, IReadOnlyList<CategoriaMaterial> canecas, string modalidad, string origen, string solicitante, string? materiales, CancellationToken ct = default)
        {
            string letras = Letras(canecas);
            var cuerpo = JsonSerializer.Serialize(new Dictionary<string, string?> { ["canecas"] = letras, ["modalidad"] = modalidad, ["origen"] = origen, ["clave"] = clave, ["solicitante"] = solicitante, ["materiales"] = materiales });
            try
            {
                using var req = Peticion(HttpMethod.Post, "/api/estacion/abrir-grupo", cuerpo);
                using var res = await Http.SendAsync(req, ct).ConfigureAwait(false);
                using var d = JsonDocument.Parse(await res.Content.ReadAsStringAsync().ConfigureAwait(false));
                var r = d.RootElement;
                bool ok = r.TryGetProperty("ok", out var o) && o.ValueKind == JsonValueKind.True;
                bool dup = r.TryGetProperty("duplicada", out var du) && du.ValueKind == JsonValueKind.True;
                string msg = r.TryGetProperty(ok ? "msg" : "error", out var m) ? m.GetString() ?? "" : "";
                return ok
                    ? new ResultadoAperturaEstacion(true, false, false, letras, r.GetProperty("duracionMs").GetInt32(), modalidad, r.GetProperty("finEstimadoMs").GetInt32(), msg)
                    : new ResultadoAperturaEstacion(false, dup, false, letras, 0, modalidad, 0, (int)res.StatusCode == 423 ? (msg.Length > 0 ? msg : "La estación está en pausa manual: el anfitrión puede reanudarla en ECOACCESS ESP32.")
                        : (int)res.StatusCode == 401 ? "El anfitrión rechazó la clave de la estación: vuelve a ejecutar Configurar-Anfitrion.ps1 en este equipo." : msg);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception e)
            {
                return new ResultadoAperturaEstacion(false, false, false, letras, 0, modalidad, 0, "No se pudo contactar el panel de la estación (" + e.GetType().Name + ").");
            }
        }

        private HttpRequestMessage Peticion(HttpMethod metodo, string ruta, string? json)
        {
            var req = new HttpRequestMessage(metodo, _url + ruta);
            if (_clave is not null) req.Headers.TryAddWithoutValidation("X-Clave", _clave);
            if (json is not null) req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return req;
        }

        public static string Letras(IEnumerable<CategoriaMaterial> canecas) => new string(MotorCombinacionesCanecas.CalcularCanecasUnicas(canecas)
            .Select(c => c switch { CategoriaMaterial.OrganicoAprovechable => 'V', CategoriaMaterial.Aprovechable => 'B', _ => 'N' }).ToArray());
    }

    /// <summary>Estación simulada (equipos sin ESP32 o demostración sin hardware): no mueve nada y lo declara.</summary>
    public sealed class EstacionExpoSimulada : IEstacionCanecasExpo
    {
        private readonly HashSet<string> _claves = new(StringComparer.Ordinal);
        public bool EsSimulada => true;
        public Task<EstadoEstacionExpo> EstadoAsync(CancellationToken ct = default) =>
            Task.FromResult(new EstadoEstacionExpo(true, false, true, "Simulación: este equipo no controla canecas físicas.", null, "simulacion"));
        public Task<ResultadoAperturaEstacion> AbrirAsync(string clave, IReadOnlyList<CategoriaMaterial> canecas, string modalidad, string origen, string solicitante, string? materiales, CancellationToken ct = default)
        {
            string letras = ClienteEstacionPanel.Letras(canecas);
            lock (_claves)
                if (!_claves.Add(clave)) return Task.FromResult(new ResultadoAperturaEstacion(false, true, true, letras, 0, modalidad, 0, "Solicitud duplicada: no se repitió."));
            int ms = PoliticaTiemposApertura.DuracionMs(letras.Length);
            return Task.FromResult(new ResultadoAperturaEstacion(true, false, true, letras, ms, modalidad, ms, "Simulación (sin movimiento físico)."));
        }
    }

    /// <summary>Resultado de una operación completa para la interfaz (Telegram o PC).</summary>
    public sealed record ResultadoOperacionExpo(
        bool AperturaEnviada,
        bool Simulada,
        bool Duplicada,
        bool RegistradaEnOracle,
        IReadOnlyList<CategoriaMaterial> Canecas,
        int DuracionMs,
        string Modalidad,
        string Mensaje,
        string Clave)
    {
        /// <summary>
        /// Registro en Oracle aún en curso (solo cuando se pidió no esperarlo): la respuesta al ciudadano no espera a la nube;
        /// el aviso de fin de ciclo lo espera antes de afirmar «Registro guardado correctamente».
        /// </summary>
        public Task<bool>? RegistroEnCurso { get; init; }
    }

    /// <summary>
    /// Orquestador único del Modo ExpoSoftware, compartido por el PC y Telegram: recibe las canecas (de una foto reconocida
    /// o de una elección manual), aplica la política 7/14/21 s, pide la apertura al panel de la estación y registra la
    /// operación en Oracle (o la deja pendiente de sincronización). Idempotente por clave.
    /// </summary>
    public sealed class ServicioEstacionExpo
    {
        public const string NombreEstacion = "estacion_tres_canecas";
        private readonly IEstacionCanecasExpo _estacion;
        private readonly RegistroOperacionesConCola _registro;
        private readonly HashSet<string> _clavesLocales = new(StringComparer.Ordinal);

        public ServicioEstacionExpo(IEstacionCanecasExpo estacion, RegistroOperacionesConCola registro, string? modalidad = null)
        {
            _estacion = estacion; _registro = registro;
            Modalidad = (modalidad ?? Environment.GetEnvironmentVariable("ECOACCESS_APERTURA_MODALIDAD")) == "secuencial" ? "secuencial" : "simultanea";
        }

        /// <summary>«simultanea» (por defecto, arranque escalonado 250 ms) o «secuencial» (una tapa tras otra).</summary>
        public string Modalidad { get; }
        public IEstacionCanecasExpo Estacion => _estacion;
        public RegistroOperacionesConCola Registro => _registro;

        public Task<EstadoEstacionExpo> EstadoAsync(CancellationToken ct = default) => _estacion.EstadoAsync(ct);

        /// <summary>Abre las canecas indicadas y registra la operación. Nunca abre si no hay canecas.</summary>
        public async Task<ResultadoOperacionExpo> EjecutarAsync(
            string clave, CanalEstacion canal, TipoOperacionEstacion tipo, IReadOnlyList<CategoriaMaterial> canecas,
            int? idUsuario, string solicitante, ResultadoExpo9? reconocimiento = null, CancellationToken ct = default, bool esperarRegistro = true)
        {
            var unicas = MotorCombinacionesCanecas.CalcularCanecasUnicas(canecas);
            int ms = PoliticaTiemposApertura.DuracionMs(unicas.Count);
            string? materiales = reconocimiento is null ? null
                : ReconocimientoTexto(reconocimiento);
            if (unicas.Count == 0)
                return new ResultadoOperacionExpo(false, _estacion.EsSimulada, false, false, unicas, 0, Modalidad, "No hay ninguna caneca que abrir.", clave);

            bool repetida; lock (_clavesLocales) repetida = !_clavesLocales.Add(clave);
            if (repetida)
                return new ResultadoOperacionExpo(false, _estacion.EsSimulada, true, false, unicas, ms, Modalidad, "Esta operación ya se ejecutó; no se repite la apertura.", clave);

            var reloj = Stopwatch.StartNew();
            ResultadoAperturaEstacion r = await _estacion.AbrirAsync(clave, unicas, Modalidad, canal.ToString().ToLowerInvariant(), solicitante, materiales, ct).ConfigureAwait(false);
            long msEstacion = reloj.ElapsedMilliseconds;
            var hardware = r.Duplicada ? EstadoHardwareEstacion.DUPLICADA_BLOQUEADA
                : !r.Aceptada ? (r.Mensaje.Contains("contactar") ? EstadoHardwareEstacion.SIN_RESPUESTA : EstadoHardwareEstacion.RECHAZADO)
                : r.Simulada ? EstadoHardwareEstacion.SIMULADO : EstadoHardwareEstacion.PWM_CONFIRMADO;
            var op = new OperacionEstacion(clave, DateTime.UtcNow, canal, tipo, idUsuario, RegistroOperacionesConCola.Recortar(solicitante, 60), NombreEstacion,
                RegistroOperacionesConCola.Recortar(materiales, 400), string.Join(",", unicas.Select(Categoria)), RegistroOperacionesConCola.NombresCanecas(ClienteEstacionPanel.Letras(unicas)),
                reconocimiento?.Materiales.Sum(m => m.Cantidad), r.Aceptada ? r.DuracionMs : 0, r.Aceptada ? Modalidad.ToUpperInvariant() : "NINGUNA",
                reconocimiento?.Estado.ToString(), hardware, r.Aceptada ? EstadoOperacionEstacion.EN_EJECUCION : EstadoOperacionEstacion.ERROR,
                ModeloVision: reconocimiento?.Modelo, LatenciaMs: reconocimiento is null ? msEstacion : reconocimiento.Medicion.TotalMs + msEstacion,
                Detalle: RegistroOperacionesConCola.Recortar(r.Mensaje, 300),
                TipoUsuario: idUsuario is null ? "INVITADO" : "REGISTRADO", Metodo: reconocimiento is null ? "MANUAL" : "FOTO",
                Detalles: DetallesDe(reconocimiento));
            if (!esperarRegistro)
            {
                // La orden ya salió hacia el ESP32: el registro en la nube corre en paralelo y no retrasa la respuesta.
                Task<bool> enCurso = Task.Run(() => _registro.RegistrarAsync(op, CancellationToken.None));
                return new ResultadoOperacionExpo(r.Aceptada, r.Simulada, r.Duplicada, false, unicas, r.DuracionMs, Modalidad, r.Mensaje, clave) { RegistroEnCurso = enCurso };
            }
            bool oracle = await _registro.RegistrarAsync(op, ct).ConfigureAwait(false);
            return new ResultadoOperacionExpo(r.Aceptada, r.Simulada, r.Duplicada, oracle, unicas, r.DuracionMs, Modalidad, r.Mensaje, clave);
        }

        /// <summary>Tras el tiempo de apertura: marca el ciclo como completado en Oracle (o en la cola).</summary>
        public async Task<bool> MarcarCicloCompletadoAsync(ResultadoOperacionExpo r, CanalEstacion canal, TipoOperacionEstacion tipo, int? idUsuario, string solicitante,
            ResultadoExpo9? reconocimiento, bool disposicionDeclarada, CancellationToken ct = default)
        {
            var op = new OperacionEstacion(r.Clave, DateTime.UtcNow, canal, disposicionDeclarada ? TipoOperacionEstacion.DISPOSICION_DECLARADA : tipo, idUsuario,
                RegistroOperacionesConCola.Recortar(solicitante, 60), NombreEstacion, reconocimiento is null ? null : RegistroOperacionesConCola.Recortar(ReconocimientoTexto(reconocimiento), 400),
                string.Join(",", r.Canecas.Select(Categoria)), RegistroOperacionesConCola.NombresCanecas(ClienteEstacionPanel.Letras(r.Canecas)),
                reconocimiento?.Materiales.Sum(m => m.Cantidad), r.DuracionMs, r.Modalidad.ToUpperInvariant(), reconocimiento?.Estado.ToString(),
                r.Simulada ? EstadoHardwareEstacion.SIMULADO : EstadoHardwareEstacion.PWM_CONFIRMADO,
                disposicionDeclarada ? EstadoOperacionEstacion.DISPOSICION_CONFIRMADA : EstadoOperacionEstacion.CICLO_COMPLETADO,
                DisposicionDeclarada: disposicionDeclarada, ModeloVision: reconocimiento?.Modelo, LatenciaMs: reconocimiento?.Medicion.TotalMs,
                Detalle: "Ciclo terminado según el temporizador del ESP32; apertura física confirmada solo por observación.",
                TipoUsuario: idUsuario is null ? "INVITADO" : "REGISTRADO", Metodo: reconocimiento is null ? null : "FOTO",
                Detalles: DetallesDe(reconocimiento));
            return await _registro.RegistrarAsync(op, ct).ConfigureAwait(false);
        }

        /// <summary>Detalle por residuo de una foto reconocida (null si fue una apertura manual sin residuos).</summary>
        public static IReadOnlyList<DetalleOperacionEstacion>? DetallesDe(ResultadoExpo9? r) => r is null || r.Materiales.Count == 0 ? null
            : r.Materiales.Select(m => new DetalleOperacionEstacion(m.Material.Codigo, RegistroOperacionesConCola.Recortar(m.Material.NombreCorto, 80)!,
                m.Material.CodigoOracle, RegistroOperacionesConCola.NombresCanecas(ClienteEstacionPanel.Letras(new[] { m.Material.Caneca })),
                Math.Max(1, Math.Min(99, m.Cantidad)), "FOTO")).ToList();

        public static string ReconocimientoTexto(ResultadoExpo9 r) =>
            string.Join(", ", r.Materiales.Select(m => $"{m.Material.Codigo} {m.Material.NombreCorto}" + (m.Cantidad > 1 ? " x" + m.Cantidad.ToString(CultureInfo.InvariantCulture) : "")))
            + (r.Desconocidos.Count > 0 ? " | no reconocido: " + string.Join(", ", r.Desconocidos) : "");

        public static string Categoria(CategoriaMaterial c) => c switch
        {
            CategoriaMaterial.OrganicoAprovechable => "ORGANICO_APROVECHABLE",
            CategoriaMaterial.Aprovechable => "APROVECHABLE",
            _ => "NO_APROVECHABLE",
        };
    }
}
