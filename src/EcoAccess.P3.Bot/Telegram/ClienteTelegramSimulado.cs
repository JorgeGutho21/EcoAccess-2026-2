using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot.Telegram
{

    /// <summary>
    /// Telegram en memoria para las pruebas del polling y del procesador. Cada llamada a
    /// <see cref="ObtenerUpdatesAsync"/> entrega el siguiente lote o fallo encolado; con la cola vacía avisa por
    /// <see cref="AlVaciarse"/> (la prueba suele cancelar ahí) y devuelve un lote vacío, como un long polling sin
    /// novedades. No abre conexiones ni necesita token.
    /// </summary>
    public sealed class ClienteTelegramSimulado : IClienteTelegram
    {
        private readonly Queue<object> _cola = new();
        private readonly object _candado = new();

        /// <summary>Se invoca cada vez que se pide un lote y ya no queda nada encolado.</summary>
        public Action? AlVaciarse { get; set; }

        /// <summary>Si es true, <see cref="EnviarAsync"/> falla como una caída de red al responder.</summary>
        public bool FallarEnvios { get; set; }

        public List<long> OffsetsPedidos { get; } = new List<long>();

        public List<RespuestaBot> Enviadas { get; } = new List<RespuestaBot>();

        public List<string> BotonesRespondidos { get; } = new List<string>();

        public void Encolar(params UpdateRecibido[] lote)
        {
            lock (_candado)
            {
                _cola.Enqueue(lote);
            }
        }

        /// <summary>La próxima petición de updates falla con <paramref name="error"/> (desconexión, 429, 409…).</summary>
        public void EncolarFallo(ErrorTelegramException error)
        {
            Compat.ThrowIfNull(error, nameof(error));
            lock (_candado)
            {
                _cola.Enqueue(error);
            }
        }

        public Task<IReadOnlyList<UpdateRecibido>> ObtenerUpdatesAsync(long offset, TimeSpan espera, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            object? siguiente;
            lock (_candado)
            {
                OffsetsPedidos.Add(offset);
                _cola.TryDequeue(out siguiente);
            }

            switch (siguiente)
            {
                case ErrorTelegramException error:
                    throw error;
                case UpdateRecibido[] lote:
                    return Task.FromResult<IReadOnlyList<UpdateRecibido>>(lote);
                default:
                    AlVaciarse?.Invoke();
                    return Task.FromResult<IReadOnlyList<UpdateRecibido>>(Array.Empty<UpdateRecibido>());
            }
        }

        public Task EnviarAsync(RespuestaBot respuesta, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FallarEnvios)
            {
                throw new ErrorTelegramException("No se pudo contactar a Telegram (simulado).");
            }

            lock (_candado)
            {
                Enviadas.Add(respuesta);
            }

            return Task.CompletedTask;
        }

        public Task ResponderBotonAsync(string idBoton, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_candado)
            {
                BotonesRespondidos.Add(idBoton);
            }

            return Task.CompletedTask;
        }
    }
}
