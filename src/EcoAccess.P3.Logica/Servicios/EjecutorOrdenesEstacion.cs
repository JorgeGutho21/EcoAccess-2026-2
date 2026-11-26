using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Vision35.Expo;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>Qué caneca física (V, B o N) es cada contenedor de la Estación Oficial EcoAccess.</summary>
    public static class CanecasEstacionOficial
    {
        public const string QrBlanca = "EcoAccessCanecaPiloto1";
        public const string QrVerde = "EcoAccessPilotoInternoVerde1";
        public const string QrNegra = "EcoAccessPilotoInternoNegra1";

        /// <summary>
        /// Letra de la caneca física, primero por su QR (no cambia) y luego por el color en el nombre. Null si el
        /// contenedor no corresponde a ninguna de las tres canecas físicas: esa orden nunca se envía al ESP32.
        /// </summary>
        public static char? Letra(Contenedor c)
        {
            if (c is null) return null;
            switch (c.IdentificadorQr)
            {
                case QrVerde: return 'V';
                case QrBlanca: return 'B';
                case QrNegra: return 'N';
            }
            string n = (c.Nombre ?? "").ToLowerInvariant();
            if (n.Contains("demo") || n.Contains("simulaci")) return null;
            if (n.Contains("verde")) return 'V';
            if (n.Contains("blanca")) return 'B';
            if (n.Contains("negra")) return 'N';
            return null;
        }

        public static CategoriaMaterial Categoria(char letra) => letra switch
        {
            'V' => CategoriaMaterial.OrganicoAprovechable,
            'B' => CategoriaMaterial.Aprovechable,
            _ => CategoriaMaterial.NoAprovechable,
        };
    }

    /// <summary>Resultado real de ejecutar una orden de apertura en la estación física.</summary>
    /// <param name="ComandoConfirmado">El ESP32 confirmó la orden (no hay sensor de posición de la tapa).</param>
    public sealed record ResultadoEjecucionOrden(bool ComandoConfirmado, char? Caneca, int DuracionMs, string Mensaje);

    /// <summary>
    /// Une las órdenes de apertura de Oracle (registro manual del operario, depósito del ciudadano o del invitado en la
    /// app) con el único servicio central de hardware: el panel ECOACCESS ESP32, dueño del puerto COM.
    ///
    /// Antes estas órdenes solo se insertaban en ORDEN_APERTURA y esperaban a un bridge HMAC que abría el puerto serie por su
    /// cuenta. Ese bridge solo vigilaba una caneca y ya no puede abrir el COM, que es del panel, así que la orden vencía sin
    /// moverse ninguna tapa («La caneca no respondió a tiempo»).
    ///
    /// Secuencia: la orden se entrega al panel con su clave de idempotencia (la misma clave nunca abre dos veces). Solo si
    /// el ESP32 la confirma se registran los eventos Acuse y Apertura. Al terminar la ventana de 7/14/21 s se registra
    /// CierreSinSensor, que deja el depósito pendiente de verificación. Si el panel o el ESP32 rechazan la orden, no se
    /// registra ninguna apertura y se devuelve el motivo real.
    /// </summary>
    public sealed class EjecutorOrdenesEstacion
    {
        private readonly IRepositorioInteracciones _interacciones;
        private readonly IRepositorioContenedores _contenedores;
        private readonly ServicioInteracciones _servicio;
        private readonly IEstacionCanecasExpo _estacion;
        private readonly TimeProvider _reloj;
        private readonly Func<TimeSpan, CancellationToken, Task> _esperar;

        public EjecutorOrdenesEstacion(IRepositorioInteracciones interacciones, IRepositorioContenedores contenedores, ServicioInteracciones servicio,
            IEstacionCanecasExpo estacion, TimeProvider reloj, Func<TimeSpan, CancellationToken, Task>? esperar = null)
        {
            _interacciones = interacciones ?? throw new ArgumentNullException(nameof(interacciones));
            _contenedores = contenedores ?? throw new ArgumentNullException(nameof(contenedores));
            _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            _estacion = estacion ?? throw new ArgumentNullException(nameof(estacion));
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
            _esperar = esperar ?? ((t, ct) => Task.Delay(t, ct));
        }

        /// <summary>
        /// Ejecuta la orden en la caneca física. Devuelve en cuanto el ESP32 confirma (o rechaza) la orden; el cierre se
        /// registra después en <paramref name="cierre"/>, que quien llama puede esperar o no.
        /// </summary>
        public async Task<(ResultadoEjecucionOrden Resultado, Task Cierre)> EjecutarAsync(int idOrden, string origen, string solicitante, string? materiales, CancellationToken ct = default)
        {
            OrdenApertura? orden = await _interacciones.ObtenerOrdenAsync(idOrden, ct).ConfigureAwait(false);
            if (orden is null) return (new(false, null, 0, "No existe esa orden."), Task.CompletedTask);
            Contenedor? caneca = await _contenedores.ObtenerPorIdAsync(orden.IdContenedor, ct).ConfigureAwait(false);
            char? letra = caneca is null ? null : CanecasEstacionOficial.Letra(caneca);
            if (letra is null)
                return (new(false, null, 0, "Esa caneca no es una de las tres canecas físicas de la Estación Oficial EcoAccess; no se envió al ESP32."), Task.CompletedTask);
            if (_estacion.EsSimulada)
                return (new(false, letra, 0, "Este equipo no está conectado a la estación física (ECOACCESS ESP32). La solicitud quedó registrada sin abrir ninguna caneca."), Task.CompletedTask);

            var canal = new CanalEstacionPanel(_estacion, letra.Value, origen, solicitante, materiales);
            var despacho = new ServicioDespachoOrdenes(_interacciones, _servicio, canal, _reloj, TimeSpan.FromSeconds(12));
            ResultadoDespacho r = await despacho.DespacharAsync(idOrden, ct).ConfigureAwait(false);
            ResultadoAperturaEstacion? hw = canal.Ultimo;
            if (r != ResultadoDespacho.Enviada || hw is null || !hw.Aceptada)
            {
                string motivo = hw is { Aceptada: false } ? hw.Mensaje
                    : r == ResultadoDespacho.Vencida ? "La orden venció antes de llegar a la caneca."
                    : r == ResultadoDespacho.YaEnviada ? "Esta orden ya se había enviado: no se repite la apertura."
                    : "La estación no confirmó la orden.";
                return (new(false, letra, 0, motivo), Task.CompletedTask);
            }

            // El ESP32 confirmó la orden: acuse y apertura (estado honesto: comando confirmado por el controlador).
            await EventoAsync(orden, TipoEventoDispositivo.Acuse, CancellationToken.None).ConfigureAwait(false);
            await EventoAsync(orden, TipoEventoDispositivo.Apertura, CancellationToken.None).ConfigureAwait(false);
            int ms = hw.DuracionMs;
            Task cierre = Task.Run(async () =>
            {
                await _esperar(TimeSpan.FromMilliseconds(Math.Max(ms, hw.FinEstimadoMs) + 1500), CancellationToken.None).ConfigureAwait(false);
                await EventoAsync(orden, TipoEventoDispositivo.CierreSinSensor, CancellationToken.None).ConfigureAwait(false);
            });
            return (new(true, letra, ms, $"Caneca abierta. Puedes desechar tu residuo. Tiempo disponible: {ms / 1000} segundos."), cierre);
        }

        private Task<Resultado<EventoRegistrado>> EventoAsync(OrdenApertura orden, TipoEventoDispositivo tipo, CancellationToken ct) =>
            _servicio.RegistrarEventoAsync(new EventoRecibido(orden.IdContenedor, $"panel-{orden.IdContenedor}-{orden.Id}-{(int)tipo}", tipo, orden.Id,
                _reloj.GetUtcNow().UtcDateTime), ct);

        /// <summary>Canal de la orden hacia el panel: entrega = el ESP32 confirmó la orden.</summary>
        private sealed class CanalEstacionPanel : ICanalDispositivo
        {
            private readonly IEstacionCanecasExpo _estacion;
            private readonly char _letra;
            private readonly string _origen, _solicitante;
            private readonly string? _materiales;

            public CanalEstacionPanel(IEstacionCanecasExpo estacion, char letra, string origen, string solicitante, string? materiales)
            { _estacion = estacion; _letra = letra; _origen = origen; _solicitante = solicitante; _materiales = materiales; }

            public string Canal => "panel-esp32";
            public ResultadoAperturaEstacion? Ultimo { get; private set; }

            public async Task<bool> EnviarAperturaAsync(ComandoApertura comando, CancellationToken cancellationToken = default)
            {
                Ultimo = await _estacion.AbrirAsync("orden-" + comando.ClaveIdempotencia, new List<CategoriaMaterial> { CanecasEstacionOficial.Categoria(_letra) },
                    "simultanea", _origen, _solicitante, _materiales, cancellationToken).ConfigureAwait(false);
                return Ultimo.Aceptada;
            }
        }
    }
}
