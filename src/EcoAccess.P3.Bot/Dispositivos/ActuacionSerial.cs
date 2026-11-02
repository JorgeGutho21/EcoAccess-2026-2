using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Bot.Dispositivos
{

    internal sealed record PermisoActuacionSerial(ComandoApertura Comando, string Desafio, int ValidezMs);
    internal sealed record ReciboActuacionSerial(string Desafio, string Sha256Paquete, bool Aceptado, string Estado);

    /// <summary>Una sesión USB, sin Oracle ni credenciales de Telegram. No interpreta sensores.</summary>
    internal interface IEnlaceActuadorSerial : IDisposable
    {
        Task<string> IntercambiarAsync(string linea, CancellationToken ct);
    }

    /// <summary>Extiende el sobre HMAC existente con desafío efímero del microcontrolador.
    /// Sin RTC: el ESP32 exige su nonce nuevo y edad monotónica; el bridge limita la vigencia restante.</summary>
    internal sealed class ActuacionSerial
    {
        private readonly CredencialDispositivo _credencial;
        private readonly ProtocoloDispositivoHmac _protocolo;
        private readonly Func<IEnlaceActuadorSerial> _abrir;
        private readonly Func<ComandoApertura, CancellationToken, Task<bool>> _autorizado;
        private readonly TimeProvider _reloj;

        public ActuacionSerial(CredencialDispositivo credencial, ProtocoloDispositivoHmac protocolo, Func<IEnlaceActuadorSerial> abrir, Func<ComandoApertura, CancellationToken, Task<bool>> autorizado, TimeProvider reloj)
        {
            _credencial = credencial;
            _protocolo = protocolo;
            _abrir = abrir;
            _autorizado = autorizado;
            _reloj = reloj;
        }

        internal async Task EjecutarAsync(ComandoApertura comando, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (comando.VenceUtc.Kind != DateTimeKind.Utc || comando.IdOrden <= 0 || comando.IdContenedor != _credencial.IdContenedor
                || Compat.IsNullOrWhiteSpace(comando.ClaveIdempotencia) || comando.ClaveIdempotencia.Length > 80)
                throw new InvalidDataException("Comando serial inválido.");
            using var enlace = _abrir();
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
            string respuesta = await enlace.IntercambiarAsync(Encoding.UTF8.GetString(paquete), ct);
            if (Encoding.UTF8.GetByteCount(respuesta) > ProtocoloDispositivoHmac.MaximoPaquete)
                throw new InvalidDataException("Respuesta serial excesiva.");
            var recibo = _protocolo.Leer<ReciboActuacionSerial>(Encoding.UTF8.GetBytes(respuesta), "recepcion-serial");
            if (recibo is not { } r || r.Cuerpo.Desafio != nonce
                || r.Cuerpo.Sha256Paquete != Compat.ToHexString(Compat.Sha256(paquete))
                || !r.Cuerpo.Aceptado || r.Cuerpo.Estado != "pwm-completado-sin-sensor")
                throw new IOException("No hay confirmación autenticada del movimiento solicitado; no se repite automáticamente.");
            // Este resultado solo confirma ejecución de PWM. No demuestra apertura, cierre físico ni ingreso.
        }
    }
}
