using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.PanelCanecas
{
    /// <summary>
    /// Único dueño del puerto serie del ESP32 (firmware hardware/CalibracionTresCanecas). Envía una orden a la vez y
    /// espera su respuesta JSON; los eventos espontáneos ({"evt":...}) se guardan en el historial.
    /// Busca la estación: primero el puerto indicado y luego los demás, preguntando ESTADO (no mueve motores).
    /// </summary>
    internal sealed class EnlaceEsp32 : IDisposable
    {
        public const string Firmware = "ecoaccess-calibracion-v1";
        private readonly SemaphoreSlim _turno = new(1, 1);
        private readonly object _bloqueo = new();
        private readonly string _preferido;
        private SerialPort? _puerto;
        private TaskCompletionSource<string>? _pendiente;
        public event Action<string>? Evento;

        public EnlaceEsp32(string preferido) => _preferido = preferido;

        public string? Puerto { get { lock (_bloqueo) return _puerto?.IsOpen == true ? _puerto.PortName : null; } }
        public string? UltimoError { get; private set; }

        public async Task<string> EnviarAsync(string orden, TimeSpan? limite = null)
        {
            await _turno.WaitAsync().ConfigureAwait(false);
            try
            {
                if (Puerto is null && !await ConectarAsync().ConfigureAwait(false))
                    throw new InvalidOperationException(UltimoError ?? "Estación desconectada.");
                try { return await IntercambiarAsync(orden, limite ?? TimeSpan.FromSeconds(3)).ConfigureAwait(false); }
                catch (Exception e) when (e is System.IO.IOException or InvalidOperationException or TimeoutException or UnauthorizedAccessException)
                {
                    // El ESP32 se desconectó o reinició: cerrar, reconectar y reintentar UNA vez (las órdenes de
                    // movimiento son idempotentes en el firmware solo por estado, así que no se repiten si ya hubo respuesta).
                    Cerrar();
                    UltimoError = "Se perdió la conexión con el ESP32: " + e.Message;
                    if (!await ConectarAsync().ConfigureAwait(false)) throw new InvalidOperationException(UltimoError);
                    return await IntercambiarAsync(orden, limite ?? TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                }
            }
            finally { _turno.Release(); }
        }

        private async Task<string> IntercambiarAsync(string orden, TimeSpan limite)
        {
            SerialPort p;
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_bloqueo)
            {
                p = _puerto ?? throw new InvalidOperationException("Estación desconectada.");
                _pendiente = tcs;
            }
            p.Write(orden + "\n");
            var fin = await Task.WhenAny(tcs.Task, Task.Delay(limite)).ConfigureAwait(false);
            lock (_bloqueo) _pendiente = null;
            if (fin != tcs.Task) throw new TimeoutException("El ESP32 no respondió a «" + orden.Split(' ')[0] + "».");
            return tcs.Task.Result;
        }

        private async Task<bool> ConectarAsync()
        {
            var candidatos = new List<string>();
            if (SerialPort.GetPortNames().Contains(_preferido, StringComparer.OrdinalIgnoreCase)) candidatos.Add(_preferido);
            candidatos.AddRange(SerialPort.GetPortNames().Where(n => !n.Equals(_preferido, StringComparison.OrdinalIgnoreCase)).OrderBy(n => n));
            if (candidatos.Count == 0) { UltimoError = "No hay puertos COM: conecta el ESP32 por USB."; return false; }
            foreach (string nombre in candidatos)
            {
                var p = new SerialPort(nombre, 115200) { DtrEnable = false, RtsEnable = false, NewLine = "\n", ReadTimeout = 500, WriteTimeout = 1000 };
                try { p.Open(); }
                catch (Exception e) { UltimoError = $"{nombre} ocupado o no disponible ({e.GetType().Name}). Cierra el Monitor Serie de Arduino IDE y la estación de Telegram."; p.Dispose(); continue; }
                lock (_bloqueo) _puerto = p;
                var lector = new Thread(() => Leer(p)) { IsBackground = true, Name = "lector-" + nombre };
                lector.Start();
                try
                {
                    await Task.Delay(300).ConfigureAwait(false);
                    string r = await IntercambiarAsync("ESTADO", TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                    if (r.Contains(Firmware)) { UltimoError = null; return true; }
                    UltimoError = $"{nombre} responde, pero no tiene el firmware de calibración ({Firmware}).";
                }
                catch (Exception) { UltimoError = $"{nombre} no respondió como estación de calibración."; }
                Cerrar();
            }
            return false;
        }

        private void Leer(SerialPort p)
        {
            while (true)
            {
                string linea;
                try { linea = p.ReadLine().Trim(); }
                catch (TimeoutException) { continue; }
                catch (Exception) { return; }  // puerto cerrado o USB desconectado
                if (linea.Length == 0 || linea[0] != '{') continue;
                if (linea.StartsWith("{\"evt\"", StringComparison.Ordinal)) { Evento?.Invoke(linea); continue; }
                TaskCompletionSource<string>? tcs;
                lock (_bloqueo) tcs = _pendiente;
                tcs?.TrySetResult(linea);
            }
        }

        private void Cerrar()
        {
            lock (_bloqueo)
            {
                try { _puerto?.Close(); } catch (Exception) { }
                _puerto?.Dispose();
                _puerto = null;
            }
        }

        public void Dispose() { Cerrar(); _turno.Dispose(); }
    }
}
