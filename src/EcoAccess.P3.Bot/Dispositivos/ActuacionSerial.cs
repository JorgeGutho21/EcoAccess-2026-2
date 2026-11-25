using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot.Dispositivos
{

    internal sealed record PermisoActuacionSerial(ComandoApertura Comando, string Desafio, int ValidezMs);
    internal sealed record ReciboActuacionSerial(string Desafio, string Sha256Paquete, bool Aceptado, string Estado);

    /// <summary>Una sesión USB, sin Oracle ni credenciales de Telegram. No interpreta sensores.</summary>
    internal interface IEnlaceActuadorSerial : IDisposable
    {
        Task<string> IntercambiarAsync(string linea, CancellationToken ct);

        /// <summary>Espera la siguiente línea sin enviar nada: el cierre llega al terminar la ventana de apertura.</summary>
        Task<string> LeerLineaAsync(TimeSpan limite, CancellationToken ct);
    }

    /// <summary>Extiende el sobre HMAC existente con desafío efímero del microcontrolador.
    /// Sin RTC: el ESP32 exige su nonce nuevo y edad monotónica; el bridge limita la vigencia restante.</summary>
    internal sealed class ActuacionSerial
    {
        internal const string EstadoApertura = "pwm-apertura-aplicada";
        internal const string EstadoCierre = "pwm-completado-sin-sensor";
        /// <summary>Ventana de depósito (7 s) más el margen de mover y responder.</summary>
        private static readonly TimeSpan EsperaCierre = TimeSpan.FromSeconds(MensajesTelegram.VentanaDepositoSegundos + 10);

        private readonly CredencialDispositivo _credencial;
        private readonly ProtocoloDispositivoHmac _protocolo;
        private readonly Func<IEnlaceActuadorSerial> _abrir;
        private readonly Func<ComandoApertura, CancellationToken, Task<bool>> _autorizado;
        private readonly TimeProvider _reloj;
        private readonly TimeSpan _pausaPuertoOcupado;

        /// <summary>
        /// Margen mínimo de vigencia para seguir esperando un puerto ocupado. Con una estación de tres tapas, los tres
        /// bridges comparten el mismo COM; Windows lo abre en exclusiva y eso serializa las tapas (nunca dos a la vez).
        /// </summary>
        internal static readonly TimeSpan MargenEsperaPuerto = TimeSpan.FromSeconds(2);

        public ActuacionSerial(CredencialDispositivo credencial, ProtocoloDispositivoHmac protocolo, Func<IEnlaceActuadorSerial> abrir,
            Func<ComandoApertura, CancellationToken, Task<bool>> autorizado, TimeProvider reloj, TimeSpan? pausaPuertoOcupado = null)
        {
            _credencial = credencial;
            _protocolo = protocolo;
            _abrir = abrir;
            _autorizado = autorizado;
            _reloj = reloj;
            _pausaPuertoOcupado = pausaPuertoOcupado ?? TimeSpan.FromMilliseconds(200);
        }

        /// <summary>
        /// Abre el enlace. Si otro bridge de la misma estación lo tiene (acceso denegado), espera su turno mientras la
        /// orden siga vigente. No otorga nada: el desafío, la reautorización con Oracle y la vigencia se comprueban después.
        /// </summary>
        private async Task<IEnlaceActuadorSerial> AbrirEsperandoTurnoAsync(ComandoApertura comando, CancellationToken ct)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                try { return _abrir(); }
                catch (UnauthorizedAccessException) when (comando.VenceUtc - _reloj.GetUtcNow().UtcDateTime > MargenEsperaPuerto)
                {
                    await Task.Delay(_pausaPuertoOcupado, ct);
                }
            }
        }

        /// <summary>
        /// Ejecuta el ciclo completo. <paramref name="alAbrir"/> se llama cuando el microcontrolador confirma que aplicó
        /// la posición de apertura; el método termina cuando confirma que aplicó la de cierre.
        /// </summary>
        internal async Task EjecutarAsync(ComandoApertura comando, Func<CancellationToken, Task> alAbrir, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (comando.VenceUtc.Kind != DateTimeKind.Utc || comando.IdOrden <= 0 || comando.IdContenedor != _credencial.IdContenedor
                || Compat.IsNullOrWhiteSpace(comando.ClaveIdempotencia) || comando.ClaveIdempotencia.Length > 80)
                throw new InvalidDataException("Comando serial inválido.");
            using var enlace = await AbrirEsperandoTurnoAsync(comando, ct);
            string linea = await enlace.IntercambiarAsync("?", ct);
            if (linea.Length > 128) throw new InvalidDataException("Desafío serial inválido.");
            using var desafio = JsonDocument.Parse(linea, new JsonDocumentOptions { MaxDepth = 2 });
            if (desafio.RootElement.EnumerateObject().Count() != 1
                || !desafio.RootElement.TryGetProperty("ecoaccessDesafio", out var n))
                throw new InvalidDataException("Desafío serial inválido.");
            string? nonce = n.GetString();
            if (nonce?.Length != 32 || nonce.Any(c => !Compat.IsAsciiHexDigit(c)))
                throw new InvalidDataException("Desafío serial inválido.");
            // Revalidar después de abrir UART y obtener el desafío, justo antes de otorgar el permiso.
            if (!await _autorizado(comando, ct)) throw new IOException("La orden ya no está autorizada.");
            int ms = (int)Compat.Clamp((comando.VenceUtc - _reloj.GetUtcNow().UtcDateTime).TotalMilliseconds, 0, 5000);
            if (ms < 100 || comando.IdContenedor != _credencial.IdContenedor || comando.IdOrden <= 0)
                throw new InvalidDataException("Orden serial no vigente.");
            byte[] paquete = ProtocoloDispositivoHmac.CrearSobre(_credencial, "actuacion-serial", new PermisoActuacionSerial(comando, nonce, ms));
            string apertura = await enlace.IntercambiarAsync(Encoding.UTF8.GetString(paquete), ct);
            ValidarRecibo(apertura, paquete, nonce, EstadoApertura);
            await alAbrir(ct);
            string cierre = await enlace.LeerLineaAsync(EsperaCierre, ct);
            ValidarRecibo(cierre, paquete, nonce, EstadoCierre);
            // Estos recibos solo confirman ejecución de PWM. No demuestran posición física ni ingreso de un residuo.
        }

        private void ValidarRecibo(string respuesta, byte[] paquete, string nonce, string estadoEsperado)
        {
            if (Encoding.UTF8.GetByteCount(respuesta) > ProtocoloDispositivoHmac.MaximoPaquete)
                throw new InvalidDataException("Respuesta serial excesiva.");
            var recibo = _protocolo.Leer<ReciboActuacionSerial>(Encoding.UTF8.GetBytes(respuesta), "recepcion-serial");
            if (recibo is not { } r || r.Cuerpo.Desafio != nonce
                || r.Cuerpo.Sha256Paquete != Compat.ToHexString(Compat.Sha256(paquete))
                || !r.Cuerpo.Aceptado || r.Cuerpo.Estado != estadoEsperado)
                throw new IOException("No hay confirmación autenticada del movimiento solicitado; no se repite automáticamente.");
        }
    }
}
