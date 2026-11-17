using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Bot.Telegram;

namespace EcoAccess.P3.Bot
{

    /// <summary>Por qué terminó el polling.</summary>
    public enum FinPolling
    {
        Cancelado,

        /// <summary>Telegram respondió 409: hay un webhook activo u otra instancia con el mismo token.</summary>
        Conflicto,

        /// <summary>Telegram respondió 401 o 404: el token fue revocado o no existe.</summary>
        TokenRechazado,
    }

    /// <summary>
    /// Long polling con getUpdates (modo por defecto). Los updates de una misma persona se atienden en orden; los de
    /// personas distintas, en paralelo (como máximo <see cref="MaximoSimultaneos"/>). Cada lote se confirma pidiendo
    /// el siguiente con offset = update_id + 1. Ante una desconexión espera 1, 2, 4… segundos (como
    /// máximo 60) y vuelve a intentar; si Telegram indica retry_after, espera eso. Un update repetido que Telegram
    /// reenvíe tras un corte lo descarta la lógica por su update_id.
    /// </summary>
    public sealed class BuclePolling
    {
        public static readonly TimeSpan EsperaLargoPolling = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(60);

        private readonly IClienteTelegram _cliente;
        private readonly ProcesadorUpdates _procesador;
        private readonly Action<string> _registro;
        private readonly Func<TimeSpan, CancellationToken, Task> _demora;
        private readonly Action<EstadoPolling>? _alLatir;
        private readonly TimeProvider _reloj;
        private readonly DateTime _inicioUtc;
        private readonly DateTime _compilacionUtc;
        private long _atendidos;
        private DateTime _ultimoContactoUtc;
        private string? _ultimoError;

        public BuclePolling(
            IClienteTelegram cliente,
            ProcesadorUpdates procesador,
            Action<string> registro,
            Func<TimeSpan, CancellationToken, Task>? demora = null,
            Action<EstadoPolling>? alLatir = null,
            TimeProvider? reloj = null)
        {
            _alLatir = alLatir;
            _reloj = reloj ?? TimeProvider.System;
            _inicioUtc = _reloj.GetUtcNow().UtcDateTime;
            _compilacionUtc = System.IO.File.GetLastWriteTimeUtc(typeof(BuclePolling).Assembly.Location);
            _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
            _procesador = procesador ?? throw new ArgumentNullException(nameof(procesador));
            _registro = RegistroTecnicoSeguro.Proteger(registro);
            _demora = demora ?? Task.Delay;
        }

        /// <summary>Siguiente offset que se pedirá; sirve para observar el avance en las pruebas.</summary>
        public long Offset { get; private set; }

        public async Task<FinPolling> EjecutarAsync(CancellationToken cancellationToken)
        {
            // Lo ya confirmado con el offset se termina al detener: se concede un margen antes de cortar la atención.
            using var drenaje = new CancellationTokenSource();
            using CancellationTokenRegistration alDetener = cancellationToken.Register(() => drenaje.CancelAfter(MargenDetencion));
            int fallosSeguidos = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                IReadOnlyList<UpdateRecibido> lote;
                try
                {
                    lote = await _cliente.ObtenerUpdatesAsync(Offset, EsperaLargoPolling, cancellationToken).ConfigureAwait(false);
                    fallosSeguidos = 0;
                    _ultimoError = null;
                    _ultimoContactoUtc = _reloj.GetUtcNow().UtcDateTime;
                    Latir(fallosSeguidos);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ErrorTelegramException error) when (error.EsConflicto)
                {
                    _registro(error.Message);
                    return FinPolling.Conflicto;
                }
                catch (ErrorTelegramException error) when (error.EsTokenRechazado)
                {
                    _registro(error.Message);
                    return FinPolling.TokenRechazado;
                }
                catch (ErrorTelegramException error)
                {
                    fallosSeguidos++;
                    TimeSpan espera = error.EsperaSugerida ?? Retroceso(fallosSeguidos);
                    _ultimoError = error.Message;
                    Latir(fallosSeguidos);
                    _registro($"Sin conexión con Telegram ({error.Message}). Reintento en {espera.TotalSeconds:0} s.");
                    try
                    {
                        await _demora(espera, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    continue;
                }
                catch (Exception error)
                {
                    // Un fallo inesperado no debe dejar al bot en silencio: se registra y se vuelve a intentar con espera.
                    fallosSeguidos++;
                    TimeSpan espera = Retroceso(fallosSeguidos);
                    _ultimoError = error.GetType().Name;
                    Latir(fallosSeguidos);
                    _registro($"Fallo inesperado al pedir mensajes ({error.GetType().Name}). Reintento en {espera.TotalSeconds:0} s.");
                    try
                    {
                        await _demora(espera, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    continue;
                }

                foreach (UpdateRecibido update in lote.OrderBy(u => u.UpdateId))
                {
                    if (update.UpdateId < Offset)
                    {
                        continue;
                    }

                    // Cola por persona: sus mensajes se atienden en orden, y una foto en análisis no detiene a los demás.
                    long persona = update.Mensaje?.TelegramUserId ?? 0;
                    Task anterior = _colas.GetValueOrDefault(persona) ?? Task.CompletedTask;
                    _colas[persona] = AtenderEnColaAsync(anterior, update, drenaje.Token);
                    Offset = update.UpdateId + 1;
                    _atendidos++;
                }

                if (lote.Count > 0) Latir(fallosSeguidos);
                foreach (long terminada in _colas.Where(c => c.Value.IsCompleted).Select(c => c.Key).ToArray()) _colas.Remove(terminada);
                // Contrapresión: no se piden más updates mientras haya demasiadas personas en atención.
                while (_colas.Count >= MaximoPendientes && !cancellationToken.IsCancellationRequested)
                {
                    await Task.WhenAny(_colas.Values).ConfigureAwait(false);
                    foreach (long terminada in _colas.Where(c => c.Value.IsCompleted).Select(c => c.Key).ToArray()) _colas.Remove(terminada);
                }
            }

            await Task.WhenAll(_colas.Values).ConfigureAwait(false);
            return FinPolling.Cancelado;
        }

        private void Latir(int fallosSeguidos) => _alLatir?.Invoke(new EstadoPolling(_inicioUtc, _ultimoContactoUtc, Offset - 1, _atendidos,
            fallosSeguidos, _ultimoError, System.Diagnostics.Process.GetCurrentProcess().Id, _compilacionUtc));

        /// <summary>Tiempo que se concede al detener para terminar los mensajes ya aceptados (incluye una foto).</summary>
        public static readonly TimeSpan MargenDetencion = TimeSpan.FromSeconds(15);

        /// <summary>Personas con mensajes en atención a la vez antes de dejar de pedir updates.</summary>
        public const int MaximoPendientes = 64;

        /// <summary>Atenciones simultáneas (descarga y clasificación incluidas) para no saturar Oracle ni la red.</summary>
        public const int MaximoSimultaneos = 32;

        private readonly Dictionary<long, Task> _colas = new Dictionary<long, Task>();
        private readonly SemaphoreSlim _simultaneos = new(MaximoSimultaneos, MaximoSimultaneos);

        private async Task AtenderEnColaAsync(Task anterior, UpdateRecibido update, CancellationToken cancellationToken)
        {
            try { await anterior.ConfigureAwait(false); }
            catch (Exception) { _registro("La atención anterior no terminó correctamente; se conserva la cola de esta persona."); }
            try
            {
                await _simultaneos.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await _procesador.ProcesarAsync(update, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // La parada no deja tareas con excepción; el update ya quedó registrado como recibido.
            }
            catch (Exception error)
            {
                _registro($"No se completó una atención ({error.GetType().Name}); la cola seguirá disponible.");
            }
            finally
            {
                _simultaneos.Release();
            }
        }

        /// <summary>1 s tras el primer fallo, el doble en cada fallo seguido, como máximo <see cref="EsperaMaxima"/>.</summary>
        public static TimeSpan Retroceso(int fallosSeguidos)
        {
            Compat.ThrowIfLessThan(fallosSeguidos, 1, nameof(fallosSeguidos));
            double segundos = Math.Pow(2, Math.Min(fallosSeguidos - 1, 6));
            return TimeSpan.FromSeconds(Math.Min(segundos, EsperaMaxima.TotalSeconds));
        }
    }
}
