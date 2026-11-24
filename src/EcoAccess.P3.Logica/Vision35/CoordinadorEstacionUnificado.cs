using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Logica.Vision35
{
    /// <summary>Canal desde el que se originó una solicitud de apertura o diagnóstico.</summary>
    public enum OrigenSolicitudEstacion
    {
        WindowsPc = 1,
        Telegram = 2,
        ReconocimientoCamara = 3,
        RegistroManual = 4,
    }

    /// <summary>Registro auditable de cada operación coordinada entre PC y Telegram.</summary>
    public sealed record RegistroOperacionEstacion(
        string ClaveIdempotencia,
        DateTime FechaUtc,
        OrigenSolicitudEstacion Origen,
        string Solicitante,
        CombinacionCanecas Combinacion,
        ModoOperacionHardware ModoEjecucion,
        bool Autorizada,
        bool Exitosa,
        IReadOnlyList<ResultadoCanecaSecuencial> ResultadosCanecas,
        string Mensaje);

    /// <summary>
    /// Coordinador único compartido entre la aplicación Windows, el reconocimiento visual y el chatbot de Telegram (Fases 4 y 5).
    /// Serializa el acceso al controlador de canecas (evitando competencia por COM3), exige autorización explícita y sesión
    /// supervisada vigente para modo físico, previene duplicados por clave de idempotencia y mantiene historial en vivo.
    /// </summary>
    public sealed class CoordinadorEstacionUnificado
    {
        private readonly SemaphoreSlim _colaEjecucion = new(1, 1);
        private readonly HashSet<string> _clavesEjecutadas = new(StringComparer.Ordinal);
        private readonly HashSet<long> _chatsAutorizados = new();
        private readonly List<RegistroOperacionEstacion> _historial = new();
        private readonly object _bloqueoEstado = new();
        private readonly IEjecutorAperturas _ejecutorDemo;
        private readonly IEjecutorAperturas? _ejecutorFisico;
        private readonly IReadOnlyList<CanecaEstacion> _estacion;
        private readonly TimeProvider _reloj;
        private int _secuenciaOrden;

        private bool _modoFisicoHabilitado;
        private DateTime _expiracionSesionFisicaUtc;
        private string? _supervisorActivo;
        private bool _hardwareConSensoresFisicos;

        public CoordinadorEstacionUnificado(
            IEjecutorAperturas ejecutorDemo,
            IReadOnlyList<CanecaEstacion> estacion,
            IEjecutorAperturas? ejecutorFisico = null,
            IEnumerable<long>? chatsTelegramAutorizados = null,
            TimeProvider? reloj = null)
        {
            _ejecutorDemo = ejecutorDemo ?? throw new ArgumentNullException(nameof(ejecutorDemo));
            _estacion = estacion ?? throw new ArgumentNullException(nameof(estacion));
            _ejecutorFisico = ejecutorFisico;
            _reloj = reloj ?? TimeProvider.System;
            _secuenciaOrden = SemillaOrdenes(_reloj.GetUtcNow().UtcDateTime);
            if (chatsTelegramAutorizados is not null)
            {
                foreach (long c in chatsTelegramAutorizados) _chatsAutorizados.Add(c);
            }
            string? envChats = Environment.GetEnvironmentVariable("ECOACCESS_TELEGRAM_CHATS_AUTORIZADOS");
            if (!string.IsNullOrWhiteSpace(envChats))
            {
                foreach (string parte in envChats!.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    if (long.TryParse(parte.Trim(), out long idChat)) _chatsAutorizados.Add(idChat);
            }
        }

        /// <summary>
        /// Primer número de orden de este proceso. El firmware de la estación guarda en su memoria no volátil cada idOrden
        /// usado y rechaza los repetidos: si cada arranque contara desde 1, la segunda prueba física (o el bot tras reiniciarse)
        /// recibiría «rechazado». Se parte de 1 000 000 000 (lejos de los ID de ORDEN_APERTURA en Oracle) más cuatro números
        /// por segundo transcurrido desde 2026: una orden física tarda más de 10 s, así que un arranque posterior nunca alcanza
        /// los números de uno anterior. Alcanza hasta 2035.
        /// </summary>
        internal static int SemillaOrdenes(DateTime ahoraUtc)
        {
            const int Base = 1_000_000_000;
            double segundos = (ahoraUtc - new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            long semilla = Base + 4L * (long)Math.Max(0, segundos);
            return (int)Math.Min(semilla, int.MaxValue - 1_000_000);
        }

        public ModoOperacionHardware ModoActual
        {
            get
            {
                lock (_bloqueoEstado)
                {
                    if (_modoFisicoHabilitado && _ejecutorFisico is not null && _reloj.GetUtcNow().UtcDateTime <= _expiracionSesionFisicaUtc)
                        return ModoOperacionHardware.HardwareFisicoSupervisado;
                    return ModoOperacionHardware.Simulacion;
                }
            }
        }

        public IReadOnlyList<RegistroOperacionEstacion> Historial
        {
            get { lock (_bloqueoEstado) { return _historial.ToList(); } }
        }

        public string DescribirModoActual()
        {
            lock (_bloqueoEstado)
            {
                DateTime ahora = _reloj.GetUtcNow().UtcDateTime;
                if (_modoFisicoHabilitado && _ejecutorFisico is not null && ahora <= _expiracionSesionFisicaUtc)
                {
                    int minRest = Math.Max(1, (int)Math.Ceiling((_expiracionSesionFisicaUtc - ahora).TotalMinutes));
                    return $"MODO FÍSICO SUPERVISADO activo (supervisor: {_supervisorActivo}, vigencia restante: ~{minRest} min).";
                }
                return "MODO DEMO (simulación sin movimiento físico).";
            }
        }

        public void HabilitarSesionFisicaSupervisada(TimeSpan vigencia, string supervisor, bool conSensoresFisicos = false)
        {
            if (_ejecutorFisico is null)
                throw new InvalidOperationException("No hay controlador físico configurado para habilitar el modo físico.");
            if (vigencia <= TimeSpan.Zero || vigencia > TimeSpan.FromHours(4))
                throw new ArgumentOutOfRangeException(nameof(vigencia), "La sesión supervisada debe durar entre 1 segundo y 4 horas.");
            lock (_bloqueoEstado)
            {
                _modoFisicoHabilitado = true;
                _expiracionSesionFisicaUtc = _reloj.GetUtcNow().UtcDateTime.Add(vigencia);
                _supervisorActivo = string.IsNullOrWhiteSpace(supervisor) ? "Supervisor" : supervisor.Trim();
                _hardwareConSensoresFisicos = conSensoresFisicos;
            }
        }

        public void DeshabilitarSesionFisica()
        {
            lock (_bloqueoEstado)
            {
                _modoFisicoHabilitado = false;
                _expiracionSesionFisicaUtc = DateTime.MinValue;
                _supervisorActivo = null;
            }
        }

        public bool EsChatAutorizado(long chatId, long telegramUserId, bool esAdminOSupervisor = false)
        {
            if (esAdminOSupervisor) return true;
            lock (_bloqueoEstado)
            {
                return _chatsAutorizados.Contains(chatId) || _chatsAutorizados.Contains(telegramUserId);
            }
        }

        /// <summary>Interpreta comandos de Telegram como /verde, /blanca, /negra o /abrir verde blanca negra.</summary>
        public static bool IntentarParsearComandoCanecas(string? texto, out IReadOnlyList<CategoriaMaterial> canecas)
        {
            canecas = Array.Empty<CategoriaMaterial>();
            if (string.IsNullOrWhiteSpace(texto)) return false;
            string[] tokens = texto!.Trim().ToLowerInvariant().Split(new[] { ' ', ',', '+', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return false;

            var seleccionadas = new List<CategoriaMaterial>();
            if (tokens.Length == 1)
            {
                if (tokens[0] == "/verde") seleccionadas.Add(CategoriaMaterial.OrganicoAprovechable);
                else if (tokens[0] == "/blanca") seleccionadas.Add(CategoriaMaterial.Aprovechable);
                else if (tokens[0] == "/negra") seleccionadas.Add(CategoriaMaterial.NoAprovechable);
                else return false;
            }
            else if (tokens[0] == "/abrir")
            {
                for (int i = 1; i < tokens.Length; i++)
                {
                    if (tokens[i] is "verde" or "can1") seleccionadas.Add(CategoriaMaterial.OrganicoAprovechable);
                    else if (tokens[i] is "blanca" or "can2") seleccionadas.Add(CategoriaMaterial.Aprovechable);
                    else if (tokens[i] is "negra" or "can3") seleccionadas.Add(CategoriaMaterial.NoAprovechable);
                    else if (tokens[i] is "todas" or "3" or "tres")
                    {
                        seleccionadas.Add(CategoriaMaterial.OrganicoAprovechable);
                        seleccionadas.Add(CategoriaMaterial.Aprovechable);
                        seleccionadas.Add(CategoriaMaterial.NoAprovechable);
                    }
                    else return false;
                }
            }
            else return false;

            canecas = MotorCombinacionesCanecas.CalcularCanecasUnicas(seleccionadas);
            return canecas.Count > 0;
        }

        /// <summary>
        /// Ejecuta de forma serializada y segura la apertura de 1, 2 o 3 canecas únicas desde PC o Telegram.
        /// Nunca compite por el puerto serial, bloquea duplicados y diferencia honestamente simulación de modo físico.
        /// </summary>
        public async Task<RegistroOperacionEstacion> SolicitarAperturaAsync(
            string claveIdempotencia,
            OrigenSolicitudEstacion origen,
            string solicitante,
            IEnumerable<CategoriaMaterial> categoriasSolicitadas,
            bool solicitarModoFisico,
            bool usuarioAutorizado,
            CancellationToken ct = default)
        {
            DateTime ahora = _reloj.GetUtcNow().UtcDateTime;
            var unicas = MotorCombinacionesCanecas.CalcularCanecasUnicas(categoriasSolicitadas);
            var combinacion = MotorCombinacionesCanecas.CalcularCombinacion(unicas);

            if (!usuarioAutorizado)
            {
                var denegada = new RegistroOperacionEstacion(claveIdempotencia, ahora, origen, solicitante, combinacion,
                    ModoActual, Autorizada: false, Exitosa: false, Array.Empty<ResultadoCanecaSecuencial>(),
                    "Operación denegada: el usuario o chat no está autorizado para activar canecas.");
                RegistrarHistorial(denegada);
                return denegada;
            }

            if (unicas.Count == 0)
            {
                var vacia = new RegistroOperacionEstacion(claveIdempotencia, ahora, origen, solicitante, CombinacionCanecas.Ninguna,
                    ModoActual, Autorizada: true, Exitosa: false, Array.Empty<ResultadoCanecaSecuencial>(),
                    "No se especificó ninguna caneca válida (verde, blanca o negra).");
                RegistrarHistorial(vacia);
                return vacia;
            }

            await _colaEjecucion.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!_clavesEjecutadas.Add(claveIdempotencia))
                {
                    var dup = new RegistroOperacionEstacion(claveIdempotencia, ahora, origen, solicitante, combinacion,
                        ModoActual, Autorizada: true, Exitosa: false, Array.Empty<ResultadoCanecaSecuencial>(),
                        "Solicitud duplicada bloqueada por idempotencia (no se repitió la apertura).");
                    RegistrarHistorial(dup);
                    return dup;
                }

                ModoOperacionHardware modoEfectivo = ModoOperacionHardware.Simulacion;
                IEjecutorAperturas ejecutor = _ejecutorDemo;
                bool conSensores = false;

                if (solicitarModoFisico)
                {
                    if (ModoActual != ModoOperacionHardware.HardwareFisicoSupervisado || _ejecutorFisico is null)
                    {
                        var bloq = new RegistroOperacionEstacion(claveIdempotencia, ahora, origen, solicitante, combinacion,
                            ModoOperacionHardware.Simulacion, Autorizada: false, Exitosa: false, Array.Empty<ResultadoCanecaSecuencial>(),
                            "Modo físico deshabilitado o sesión supervisada vencida: habilítalo localmente en la estación antes de mover motores reales.");
                        RegistrarHistorial(bloq);
                        return bloq;
                    }
                    modoEfectivo = ModoOperacionHardware.HardwareFisicoSupervisado;
                    ejecutor = _ejecutorFisico;
                    lock (_bloqueoEstado) { conSensores = _hardwareConSensoresFisicos; }
                }

                var resultados = new List<ResultadoCanecaSecuencial>();
                foreach (CategoriaMaterial cat in unicas)
                {
                    CanecaEstacion? caneca = _estacion.FirstOrDefault(c => c.Categoria == cat && c.EnServicio);
                    string claveCaneca = $"{claveIdempotencia}:{MotorReglasMultirresiduo.NombreCortoCaneca(cat)}";
                    if (caneca is null)
                    {
                        resultados.Add(new ResultadoCanecaSecuencial(cat, 0, claveCaneca, false, false, false, ejecutor.EsSimulado,
                            EstadoEjecucionApertura.Rechazada, $"Caneca {MotorReglasMultirresiduo.NombreCortoCaneca(cat)} fuera de servicio.", conSensores, !conSensores));
                        continue;
                    }

                    int idOrden = Interlocked.Increment(ref _secuenciaOrden);
                    var cmd = new ComandoApertura(idOrden, caneca.IdContenedor, claveCaneca, _reloj.GetUtcNow().UtcDateTime.Add(ServicioInteracciones.VigenciaOrden));
                    ResultadoEjecucionApertura ej;
                    try { ej = await ejecutor.AbrirAsync(cmd, ct).ConfigureAwait(false); }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                    catch { ej = new ResultadoEjecucionApertura(EstadoEjecucionApertura.SinRespuesta, Array.Empty<TipoEventoDispositivo>()); }

                    bool apOk = ej.Estado is EstadoEjecucionApertura.Completada or EstadoEjecucionApertura.FalloCierre || ej.Eventos.Contains(TipoEventoDispositivo.Apertura);
                    bool ciOk = ej.Estado == EstadoEjecucionApertura.Completada || ej.Eventos.Contains(TipoEventoDispositivo.Cierre) || ej.Eventos.Contains(TipoEventoDispositivo.CierreSinSensor);
                    string txt = ejecutor.EsSimulado
                        ? (apOk ? "Ejecutada en MODO DEMO (simulación sin movimiento físico)." : $"Fallo simulado ({ej.Estado}).")
                        : (apOk ? "Comando enviado y confirmado por el controlador físico ESP32 (confirmación física por observación supervisada)." : $"Fallo de controlador físico ({ej.Estado}).");

                    resultados.Add(new ResultadoCanecaSecuencial(cat, caneca.IdContenedor, claveCaneca, true, apOk, ciOk, ejecutor.EsSimulado, ej.Estado, txt, conSensores, ejecutor.EsSimulado || !conSensores));
                }

                bool todasOk = resultados.Count > 0 && resultados.All(r => r.AperturaConfirmada);
                string etiquetaModo = modoEfectivo == ModoOperacionHardware.HardwareFisicoSupervisado ? "MODO FÍSICO SUPERVISADO" : "MODO DEMO (simulación)";
                string nombres = string.Join(" + ", unicas.Select(MotorReglasMultirresiduo.NombreCortoCaneca));
                string resumen = todasOk
                    ? $"[{etiquetaModo}] Secuencia completada para: {nombres} ({resultados.Count} caneca(s))."
                    : $"[{etiquetaModo}] Incidencia parcial o total al abrir: {nombres}.";

                var reg = new RegistroOperacionEstacion(claveIdempotencia, ahora, origen, solicitante, combinacion,
                    modoEfectivo, Autorizada: true, Exitosa: todasOk, resultados, resumen);
                RegistrarHistorial(reg);
                return reg;
            }
            finally
            {
                _colaEjecucion.Release();
            }
        }

        private void RegistrarHistorial(RegistroOperacionEstacion reg)
        {
            lock (_bloqueoEstado)
            {
                _historial.Add(reg);
                if (_historial.Count > 200) _historial.RemoveAt(0);
            }
        }
    }
}