using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot.EstacionWeb
{
    /// <summary>Lo que la estación web pide: abrir la caneca de un color. La solicitud se repite tal cual al reintentar.</summary>
    public sealed record SolicitudAperturaWeb(string Solicitud, int IdCaneca, string Color);

    /// <summary>
    /// Respuesta para la estación web. <c>Aceptada</c> solo es verdadera cuando el bridge ya registró en Oracle que la tapa
    /// abrió; nunca antes. <c>AbiertoMs</c> es la ventana de depósito del firmware.
    /// </summary>
    public sealed record RespuestaAperturaWeb(bool Aceptada, string Mensaje, int? AbiertoMs = null);

    /// <summary>
    /// Convierte un toque (o una frase) en la estación web en una apertura autorizada, por el mismo camino que el visitante
    /// de la aplicación de escritorio: solicitud sin usuario (canal Simulador) → orden con clave de idempotencia → el bridge
    /// de esa caneca la toma, la firma (HMAC) y el ESP32 mueve la tapa. Este servicio no toca el hardware: solo escribe en
    /// Oracle mediante <see cref="ServicioInteracciones"/>, que conserva todas sus reglas (caneca en servicio, material
    /// habilitado y admitido, una orden vigente por caneca, vencimiento). Sin sensor, el depósito queda sin verificar y no
    /// otorga ecopuntos.
    /// </summary>
    public sealed class ServicioAperturasWeb
    {
        private const int MaximoSolicitudesRecordadas = 200;

        private readonly ServicioInteracciones _interacciones;
        private readonly IRepositorioContenedores _contenedores;
        private readonly IRepositorioMateriales _materiales;
        private readonly IReadOnlyDictionary<string, int> _canecas;
        private readonly IReadOnlyDictionary<string, int> _materialPorColor;
        private readonly TimeProvider _reloj;
        private readonly TimeSpan _espera;
        private readonly TimeSpan _intervalo;
        private readonly Func<TimeSpan, CancellationToken, Task> _pausa;
        private readonly SemaphoreSlim _turno = new(1, 1);
        // Reintentos de la misma solicitud reutilizan su interacción y su clave: Oracle nunca emite dos órdenes por ella.
        private readonly Dictionary<string, int> _interaccionPorSolicitud = new(StringComparer.Ordinal);
        private readonly Queue<string> _ordenSolicitudes = new();

        public ServicioAperturasWeb(
            ServicioInteracciones interacciones,
            IRepositorioContenedores contenedores,
            IRepositorioMateriales materiales,
            IReadOnlyDictionary<string, int> canecasPorColor,
            IReadOnlyDictionary<string, int>? materialPorColor,
            TimeProvider reloj,
            TimeSpan esperaConfirmacion,
            TimeSpan? intervaloConsulta = null,
            Func<TimeSpan, CancellationToken, Task>? pausa = null)
        {
            _interacciones = interacciones ?? throw new ArgumentNullException(nameof(interacciones));
            _contenedores = contenedores ?? throw new ArgumentNullException(nameof(contenedores));
            _materiales = materiales ?? throw new ArgumentNullException(nameof(materiales));
            if (canecasPorColor is null || canecasPorColor.Count == 0) throw new ArgumentException("Indica al menos una caneca.", nameof(canecasPorColor));
            if (canecasPorColor.Values.Any(id => id <= 0) || canecasPorColor.Values.Distinct().Count() != canecasPorColor.Count)
                throw new ArgumentException("Cada color necesita una caneca distinta con identificador positivo.", nameof(canecasPorColor));
            _canecas = canecasPorColor.ToDictionary(p => Normalizar(p.Key), p => p.Value, StringComparer.Ordinal);
            _materialPorColor = (materialPorColor ?? new Dictionary<string, int>()).ToDictionary(p => Normalizar(p.Key), p => p.Value, StringComparer.Ordinal);
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
            _espera = esperaConfirmacion > TimeSpan.Zero ? esperaConfirmacion : throw new ArgumentOutOfRangeException(nameof(esperaConfirmacion));
            _intervalo = intervaloConsulta is { } i && i > TimeSpan.Zero ? i : TimeSpan.FromMilliseconds(500);
            _pausa = pausa ?? ((t, ct) => Task.Delay(t, ct));
        }

        public static int VentanaMs => MensajesTelegram.VentanaDepositoSegundos * 1000;

        /// <summary>Atiende una solicitud a la vez: nunca hay dos tapas en movimiento por culpa de la estación web.</summary>
        public async Task<RespuestaAperturaWeb> SolicitarAsync(SolicitudAperturaWeb solicitud, CancellationToken cancellationToken = default)
        {
            if (solicitud is null) throw new ArgumentNullException(nameof(solicitud));
            if (!SolicitudValida(solicitud.Solicitud))
                return new(false, "La solicitud no es válida.");
            string color = Normalizar(solicitud.Color);
            if (!_canecas.TryGetValue(color, out int idCaneca) || idCaneca != solicitud.IdCaneca)
                return new(false, "Esa caneca no pertenece a esta estación.");

            await _turno.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await AbrirAsync(solicitud.Solicitud, color, idCaneca, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _turno.Release();
            }
        }

        private async Task<RespuestaAperturaWeb> AbrirAsync(string idSolicitud, string color, int idCaneca, CancellationToken ct)
        {
            Contenedor? caneca = await _contenedores.ObtenerPorIdAsync(idCaneca, ct).ConfigureAwait(false);
            if (caneca is null) return new(false, "La caneca no está registrada en EcoAccess.");
            if (!caneca.EstaEnServicio) return new(false, $"La caneca {color} está fuera de servicio.");

            int? material = await MaterialAsync(color, caneca, ct).ConfigureAwait(false);
            if (material is null) return new(false, $"La caneca {color} no tiene materiales habilitados.");

            string clave = "web-" + idSolicitud;
            if (!_interaccionPorSolicitud.TryGetValue(idSolicitud, out int idInteraccion))
            {
                var inicio = await _interacciones.IniciarVisitanteAsync(idCaneca, ct).ConfigureAwait(false);
                if (!inicio.Exitoso) return new(false, inicio.Mensaje ?? "No se pudo registrar la solicitud.");
                idInteraccion = inicio.Valor;
                Recordar(idSolicitud, idInteraccion);
            }

            var orden = await _interacciones.AutorizarAperturaVisitanteAsync(idInteraccion, material.Value, clave, ct).ConfigureAwait(false);
            if (!orden.Exitoso) return new(false, orden.Mensaje ?? "La caneca no pudo recibir la orden.");

            return await EsperarAperturaAsync(idInteraccion, clave, color, ct).ConfigureAwait(false);
        }

        /// <summary>Sigue la orden en Oracle hasta que el bridge informe la apertura, o hasta que falle o se acabe la espera.</summary>
        private async Task<RespuestaAperturaWeb> EsperarAperturaAsync(int idInteraccion, string clave, string color, CancellationToken ct)
        {
            DateTimeOffset limite = _reloj.GetUtcNow() + _espera;
            while (true)
            {
                var consulta = await _interacciones.ConsultarOperacionVisitanteAsync(idInteraccion, clave, ct).ConfigureAwait(false);
                if (consulta.Exitoso)
                {
                    switch (consulta.Valor.Orden.Estado)
                    {
                        case EstadoOrdenApertura.Abierta or EstadoOrdenApertura.Detectada:
                            return new(true, $"Caneca {color} abierta.", VentanaMs);
                        case EstadoOrdenApertura.Cerrada:
                            return new(true, $"Caneca {color} abierta y ya cerrada.", 0);
                        case EstadoOrdenApertura.Fallida:
                            return new(false, $"La caneca {color} informó un fallo y no abrió.");
                        case EstadoOrdenApertura.Vencida:
                            return new(false, $"La caneca {color} no respondió a tiempo.");
                    }
                }
                if (_reloj.GetUtcNow() >= limite)
                    return new(false, $"La caneca {color} no confirmó la apertura. Revisa que su bridge esté encendido; la orden vence sola.");
                await _pausa(_intervalo, ct).ConfigureAwait(false);
            }
        }

        /// <summary>Material configurado para el color o, si no hay, el primer material habilitado que la caneca admite.</summary>
        private async Task<int?> MaterialAsync(string color, Contenedor caneca, CancellationToken ct)
        {
            if (_materialPorColor.TryGetValue(color, out int configurado)) return configurado;
            foreach (int id in caneca.MaterialesAdmitidos.OrderBy(m => m))
            {
                if (await _materiales.ObtenerPorIdAsync(id, ct).ConfigureAwait(false) is { Habilitado: true }) return id;
            }
            return null;
        }

        private void Recordar(string solicitud, int idInteraccion)
        {
            _interaccionPorSolicitud[solicitud] = idInteraccion;
            _ordenSolicitudes.Enqueue(solicitud);
            while (_ordenSolicitudes.Count > MaximoSolicitudesRecordadas)
                _interaccionPorSolicitud.Remove(_ordenSolicitudes.Dequeue());
        }

        internal static bool SolicitudValida(string? solicitud) =>
            solicitud is { Length: >= 8 and <= OrdenApertura.LongitudMaximaClave - 4 } && solicitud.All(c => Compat.IsAsciiLetterOrDigit(c) || c == '-');

        private static string Normalizar(string? color) => (color ?? string.Empty).Trim().ToLowerInvariant();
    }
}
