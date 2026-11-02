using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot
{

    /// <summary>
    /// Atiende un update: lo pasa a la lógica (<see cref="ServicioConversacionTelegram.ProcesarAsync"/>), envía las
    /// respuestas y contesta el botón pulsado. Nunca deja escapar una excepción salvo la cancelación propia, de modo
    /// que un update que falla no detiene el polling ni el webhook.
    /// <para>
    /// Límite conocido: la lógica registra el update como procesado antes de atenderlo (control de duplicados). Si
    /// después falla (por ejemplo, Oracle caído), el update no se reintenta; la persona recibe
    /// <see cref="ErrorGenerico"/> y vuelve a escribir.
    /// </para>
    /// </summary>
    public sealed class ProcesadorUpdates
    {
        public const string ErrorGenerico = "No pudimos atender tu mensaje en este momento. Intenta de nuevo en unos minutos.";

        private readonly Func<MensajeEntrante, CancellationToken, Task<IReadOnlyList<RespuestaBot>>> _atender;
        private readonly IClienteTelegram _cliente;
        private readonly Action<string> _registro;

        public ProcesadorUpdates(
            Func<MensajeEntrante, CancellationToken, Task<IReadOnlyList<RespuestaBot>>> atender,
            IClienteTelegram cliente,
            Action<string> registro)
        {
            _atender = atender ?? throw new ArgumentNullException(nameof(atender));
            _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
            _registro = RegistroTecnicoSeguro.Proteger(registro);
        }

        public async Task ProcesarAsync(UpdateRecibido update, CancellationToken cancellationToken)
        {
            Compat.ThrowIfNull(update, nameof(update));
            if (update.IdBoton is not null)
            {
                // Primero se quita el reloj del botón: Telegram lo muestra hasta recibir esta respuesta.
                await IntentarAsync(() => _cliente.ResponderBotonAsync(update.IdBoton, cancellationToken), update.UpdateId,
                    "contestar el botón", cancellationToken).ConfigureAwait(false);
            }

            if (update.Mensaje is null)
            {
                return;
            }

            IReadOnlyList<RespuestaBot> respuestas;
            var reloj = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                respuestas = await _atender(update.Mensaje, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception error)
            {
                _registro($"Update {update.UpdateId}: falló la atención ({error.GetType().Name}).");
                respuestas = update.Mensaje.ChatPrivado
                    ? new RespuestaBot[] { new RespuestaBot(update.Mensaje.ChatId, ErrorGenerico, Array.Empty<BotonBot>()) }
                    : Array.Empty<RespuestaBot>();
            }

            long atencion = reloj.ElapsedMilliseconds;
            foreach (RespuestaBot respuesta in respuestas)
            {
                await IntentarAsync(() => _cliente.EnviarAsync(respuesta, cancellationToken), update.UpdateId,
                    "enviar la respuesta", cancellationToken).ConfigureAwait(false);
            }

            // Medición sin contenido ni identidad: solo el tipo de mensaje y los tiempos.
            string tipo = update.Mensaje.ArchivoFoto is not null ? "foto" : update.Mensaje.DatoBoton is not null ? "botón" : "texto";
            _registro($"Tiempo {tipo}: atención {atencion} ms, envío {reloj.ElapsedMilliseconds - atencion} ms.");
        }

        /// <summary>Un fallo al hablar con Telegram se registra y se sigue: el update ya quedó atendido.</summary>
        private async Task IntentarAsync(Func<Task> accion, long updateId, string que, CancellationToken cancellationToken)
        {
            try
            {
                await accion().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ErrorTelegramException error)
            {
                _registro($"Update {updateId}: no se pudo {que} ({error.GetType().Name}).");
            }
            catch (Exception error)
            {
                _registro($"Update {updateId}: no se pudo {que} ({error.GetType().Name}).");
            }
        }
    }
}
