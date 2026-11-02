using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EcoAccess.P3.Bot.Telegram;

namespace EcoAccess.P3.Bot
{

    /// <summary>
    /// Modo webhook (alternativa al polling): Telegram hace POST de cada update. Solo se acepta la petición si trae
    /// en <c>X-Telegram-Bot-Api-Secret-Token</c> el secreto registrado con setWebhook (comparación en tiempo
    /// constante). Devuelve 200 aunque la atención falle: el procesador ya avisó a la persona y un reintento de
    /// Telegram lo descartaría como duplicado.
    /// </summary>
    public sealed class ReceptorWebhook
    {
        public const string EncabezadoSecreto = "X-Telegram-Bot-Api-Secret-Token";

        /// <summary>Un update de texto o botón cabe de sobra; algo mayor no viene de Telegram.</summary>
        public const int TamanoMaximo = 1 << 20;

        private readonly ProcesadorUpdates _procesador;
        private readonly byte[] _secreto;

        public ReceptorWebhook(ProcesadorUpdates procesador, string secreto)
        {
            _procesador = procesador ?? throw new ArgumentNullException(nameof(procesador));
            if (!SecretoValido(secreto))
            {
                throw new ArgumentException(
                    "El secreto del webhook debe tener entre 16 y 256 caracteres A-Z, a-z, 0-9, _ o -.", nameof(secreto));
            }

            _secreto = Encoding.UTF8.GetBytes(secreto);
        }

        /// <summary>Telegram admite 1-256 caracteres; aquí se exigen al menos 16 para que no se adivine.</summary>
        public static bool SecretoValido(string? secreto) =>
            secreto is { Length: >= 16 and <= 256 }
            && secreto.All(c => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or '-');

        /// <summary>Comprueba el encabezado antes de leer el cuerpo, para no gastar tiempo en peticiones ajenas.</summary>
        public bool SecretoCorrecto(string? secretoRecibido) =>
            secretoRecibido is not null
            && Compat.FixedTimeEquals(Encoding.UTF8.GetBytes(secretoRecibido), _secreto);

        /// <returns>Código HTTP: 200 atendido, 401 secreto ausente o distinto, 400 cuerpo ilegible, 413 demasiado grande.</returns>
        public async Task<int> RecibirAsync(string? secretoRecibido, ReadOnlyMemory<byte> cuerpo, CancellationToken cancellationToken)
        {
            if (!SecretoCorrecto(secretoRecibido))
            {
                return 401;
            }

            if (cuerpo.Length > TamanoMaximo)
            {
                return 413;
            }

            UpdateRecibido update;
            try
            {
                using JsonDocument documento = JsonDocument.Parse(cuerpo);
                update = TraductorUpdates.Traducir(documento.RootElement);
            }
            catch (JsonException)
            {
                return 400;
            }

            await _procesador.ProcesarAsync(update, cancellationToken).ConfigureAwait(false);
            return 200;
        }
    }
}
