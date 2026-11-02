using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot.Telegram
{

    /// <summary>
    /// Modo <c>--simulado</c>: la consola hace de chat privado de Telegram para probar el bot contra Oracle sin
    /// token ni red. Cada línea es un mensaje; «#n» pulsa el botón n del último mensaje con botones. Para escanear
    /// el QR de una caneca se escribe su identificador, como lo haría el enlace del QR.
    /// Los update_id parten de la hora actual en milisegundos (más de 10^12) para no chocar con los de ejecuciones
    /// anteriores registrados en UPDATE_TELEGRAM ni con los de Telegram real, que son mucho menores.
    /// </summary>
    public sealed class ChatConsola : IClienteTelegram
    {
        /// <summary>Cuenta de Telegram ficticia; no corresponde a ninguna persona.</summary>
        public const long UsuarioSimulado = 1_000_001;

        private readonly TextReader _entrada;
        private readonly TextWriter _salida;
        private readonly Action _alTerminar;
        private long _siguienteUpdate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        private int _siguienteBoton;
        private IReadOnlyList<BotonBot> _ultimosBotones = Array.Empty<BotonBot>();

        public ChatConsola(TextReader entrada, TextWriter salida, Action alTerminar)
        {
            _entrada = entrada ?? throw new ArgumentNullException(nameof(entrada));
            _salida = salida ?? throw new ArgumentNullException(nameof(salida));
            _alTerminar = alTerminar ?? throw new ArgumentNullException(nameof(alTerminar));
        }

        public async Task<IReadOnlyList<UpdateRecibido>> ObtenerUpdatesAsync(long offset, TimeSpan espera, CancellationToken cancellationToken)
        {
            await _salida.WriteAsync("tú> ").ConfigureAwait(false);
            string? linea = await _entrada.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (linea is null)
            {
                _alTerminar();
                return Array.Empty<UpdateRecibido>();
            }

            linea = linea.Trim();
            if (linea.Length == 0)
            {
                return Array.Empty<UpdateRecibido>();
            }

            long updateId = _siguienteUpdate++;
            if (linea.StartsWith('#'))
            {
                if (!int.TryParse(linea.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out int numero)
                    || numero < 1 || numero > _ultimosBotones.Count)
                {
                    await _salida.WriteLineAsync("(no hay un botón con ese número)").ConfigureAwait(false);
                    return Array.Empty<UpdateRecibido>();
                }

                BotonBot boton = _ultimosBotones[numero - 1];
                string idBoton = "consola-" + (++_siguienteBoton).ToString(CultureInfo.InvariantCulture);
                var pulsado = new MensajeEntrante(updateId, UsuarioSimulado, UsuarioSimulado, true, null, boton.Dato);
                return new UpdateRecibido[] { new UpdateRecibido(updateId, pulsado, idBoton) };
            }

            var mensaje = new MensajeEntrante(updateId, UsuarioSimulado, UsuarioSimulado, true, linea, null);
            return new UpdateRecibido[] { new UpdateRecibido(updateId, mensaje, null) };
        }

        public async Task EnviarAsync(RespuestaBot respuesta, CancellationToken cancellationToken)
        {
            Compat.ThrowIfNull(respuesta, nameof(respuesta));
            await _salida.WriteLineAsync("bot> " + respuesta.Texto.Replace("\n", "\n     ", StringComparison.Ordinal))
                .ConfigureAwait(false);
            for (int i = 0; i < respuesta.Botones.Count; i++)
            {
                await _salida.WriteLineAsync($"     [#{i + 1}] {respuesta.Botones[i].Texto}").ConfigureAwait(false);
            }

            if (respuesta.Botones.Count > 0)
            {
                _ultimosBotones = respuesta.Botones;
            }
        }

        public Task ResponderBotonAsync(string idBoton, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
