using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.PanelCanecas
{
    /// <summary>Estado real de la conexión, calculado por el backend (nunca fijo en la página).</summary>
    internal enum EstadoConexion { UsbDesconectado, UsbDetectado, PuertoOcupado, SinRespuesta, Conectado }

    internal sealed record DispositivoUsb(string Puerto, string Descripcion, string Vid, string Pid);

    /// <summary>
    /// Único dueño del puerto serie del ESP32 (firmware hardware/CalibracionTresCanecas). Una orden a la vez.
    /// Un monitor en segundo plano detecta el USB por VID/PID, conecta, consulta ESTADO cada segundo (no mueve motores) y,
    /// si el cable se desconecta, cierra el puerto y vuelve a buscar. Al reconectar no reenvía ninguna orden anterior.
    /// </summary>
    internal sealed class EnlaceEsp32 : IDisposable
    {
        public const string Firmware = "ecoaccess-calibracion-v";   // v1 (calibración) o v2 (además GRUPO: aperturas simultáneas)
        // Puentes USB-serie habituales en placas ESP32: CP210x, CH340/CH9102, FTDI y USB nativo de Espressif.
        private static readonly string[] VidConocidos = { "10C4", "1A86", "0403", "303A" };
        private readonly SemaphoreSlim _turno = new(1, 1);
        private readonly object _bloqueo = new();
        private readonly string _preferido;
        private readonly CancellationTokenSource _fin = new();
        private SerialPort? _puerto;
        private TaskCompletionSource<string>? _pendiente;
        private volatile bool _buscarYa = true;

        public event Action<string, string>? Evento;   // (tipo, texto)
        public EstadoConexion Estado { get; private set; } = EstadoConexion.UsbDesconectado;
        public string? Puerto { get { lock (_bloqueo) return _puerto?.IsOpen == true ? _puerto.PortName : null; } }
        public DispositivoUsb? Dispositivo { get; private set; }
        public string? UltimoError { get; private set; }
        public DateTime? UltimaRespuesta { get; private set; }
        public string? UltimoEstadoJson { get; private set; }

        public EnlaceEsp32(string preferido)
        {
            _preferido = preferido;
            _ = Task.Run(MonitorAsync);
        }

        public void BuscarAhora() => _buscarYa = true;

        /// <summary>Envía una orden y devuelve la respuesta JSON del firmware. Falla rápido si no hay estación.</summary>
        public async Task<string> EnviarAsync(string orden, TimeSpan? limite = null)
        {
            if (Estado != EstadoConexion.Conectado) throw new InvalidOperationException(Texto(Estado));
            await _turno.WaitAsync().ConfigureAwait(false);
            try
            {
                string r = await IntercambiarAsync(orden, limite ?? TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                UltimaRespuesta = DateTime.Now;
                if (orden == "ESTADO") UltimoEstadoJson = r;
                return r;
            }
            catch (Exception e) when (e is System.IO.IOException or InvalidOperationException or TimeoutException or UnauthorizedAccessException)
            {
                Perder("la orden «" + orden.Split(' ')[0] + "» no tuvo respuesta (" + e.Message + ")");
                throw new InvalidOperationException("Se perdió la comunicación con el ESP32. La posición de las tapas puede ser desconocida.");
            }
            finally { _turno.Release(); }
        }

        public static string Texto(EstadoConexion e) => e switch
        {
            EstadoConexion.UsbDesconectado => "USB desconectado: no se detecta la placa.",
            EstadoConexion.UsbDetectado => "USB detectado: conectando con el ESP32…",
            EstadoConexion.PuertoOcupado => "Puerto ocupado por otro programa (Monitor Serie de Arduino IDE o la estación de Telegram).",
            EstadoConexion.SinRespuesta => "El USB está conectado, pero el ESP32 no responde con el firmware de calibración.",
            _ => "ESP32 conectado.",
        };

        private async Task MonitorAsync()
        {
            while (!_fin.IsCancellationRequested)
            {
                try
                {
                    if (Puerto is null)
                    {
                        var usb = BuscarUsb();
                        bool hayPuertos = SerialPort.GetPortNames().Length > 0;
                        if (usb.Count == 0 && !hayPuertos) { Cambiar(EstadoConexion.UsbDesconectado, "No hay ningún dispositivo USB-serie conectado."); Dispositivo = null; }
                        else if (_buscarYa || Estado is EstadoConexion.UsbDesconectado or EstadoConexion.UsbDetectado or EstadoConexion.SinRespuesta or EstadoConexion.PuertoOcupado)
                        {
                            _buscarYa = false;
                            Cambiar(EstadoConexion.UsbDetectado, null);
                            await ConectarAsync(usb).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        if (!SerialPort.GetPortNames().Contains(Puerto, StringComparer.OrdinalIgnoreCase)) Perder("el cable USB se desconectó");
                        else if (_buscarYa) { _buscarYa = false; await EnviarAsync("ESTADO").ConfigureAwait(false); }
                        else await EnviarAsync("ESTADO").ConfigureAwait(false);
                    }
                }
                catch (Exception) { /* ya quedó anotado en Perder/Cambiar */ }
                // Cada segundo: la cuenta regresiva de la página sale de este sondeo real (ESTADO tarda ~45 ms y no mueve motores).
                try { await Task.Delay(TimeSpan.FromSeconds(1), _fin.Token).ConfigureAwait(false); } catch (OperationCanceledException) { return; }
            }
        }

        private static List<DispositivoUsb> BuscarUsb()
        {
            var lista = new List<DispositivoUsb>();
            try
            {
                using var s = new ManagementObjectSearcher("SELECT Name, PNPDeviceID FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");
                foreach (ManagementObject o in s.Get())
                {
                    string nombre = o["Name"]?.ToString() ?? "", id = o["PNPDeviceID"]?.ToString() ?? "";
                    var com = Regex.Match(nombre, @"\((COM\d+)\)"); var vid = Regex.Match(id, @"VID_([0-9A-F]{4})", RegexOptions.IgnoreCase); var pid = Regex.Match(id, @"PID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
                    if (com.Success) lista.Add(new DispositivoUsb(com.Groups[1].Value, Regex.Replace(nombre, @"\s*\(COM\d+\)", ""), vid.Success ? vid.Groups[1].Value.ToUpperInvariant() : "", pid.Success ? pid.Groups[1].Value.ToUpperInvariant() : ""));
                }
            }
            catch (Exception) { }
            return lista;
        }

        private async Task ConectarAsync(List<DispositivoUsb> usb)
        {
            // Orden: placas ESP32 conocidas (por VID), el puerto preferido y luego los demás.
            var candidatos = usb.Where(d => VidConocidos.Contains(d.Vid)).Select(d => d.Puerto).ToList();
            if (SerialPort.GetPortNames().Contains(_preferido, StringComparer.OrdinalIgnoreCase)) candidatos.Add(_preferido);
            candidatos.AddRange(SerialPort.GetPortNames().OrderBy(n => n));
            bool ocupado = false;
            foreach (string nombre in candidatos.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var p = new SerialPort(nombre, 115200) { DtrEnable = false, RtsEnable = false, NewLine = "\n", ReadTimeout = 500, WriteTimeout = 1000 };
                try { p.Open(); }
                catch (UnauthorizedAccessException) { ocupado = true; p.Dispose(); continue; }
                catch (Exception) { p.Dispose(); continue; }
                lock (_bloqueo) _puerto = p;
                new Thread(() => Leer(p)) { IsBackground = true, Name = "lector-" + nombre }.Start();
                try
                {
                    await Task.Delay(300).ConfigureAwait(false);
                    string r = await IntercambiarAsync("ESTADO", TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                    if (r.Contains(Firmware))
                    {
                        Dispositivo = usb.FirstOrDefault(d => d.Puerto.Equals(nombre, StringComparison.OrdinalIgnoreCase)) ?? new DispositivoUsb(nombre, "Puerto serie", "", "");
                        UltimaRespuesta = DateTime.Now; UltimoEstadoJson = r;
                        Cambiar(EstadoConexion.Conectado, null);
                        Evento?.Invoke("conexion", $"ESP32 conectado en {nombre} ({Dispositivo.Descripcion}). No se reenvió ninguna orden anterior.");
                        return;
                    }
                }
                catch (Exception) { }
                Cerrar();
            }
            Dispositivo = usb.FirstOrDefault(d => VidConocidos.Contains(d.Vid));
            if (ocupado) Cambiar(EstadoConexion.PuertoOcupado, "Otro programa tiene abierto el puerto del ESP32.");
            else if (usb.Count == 0 && SerialPort.GetPortNames().Length == 0) Cambiar(EstadoConexion.UsbDesconectado, "No hay ningún dispositivo USB-serie conectado.");
            else Cambiar(EstadoConexion.SinRespuesta, "Hay un dispositivo USB, pero no responde con el firmware de calibración (" + Firmware + ").");
        }

        private void Perder(string motivo)
        {
            bool estabaConectado = Estado == EstadoConexion.Conectado;
            Cerrar();
            UltimoEstadoJson = null;
            Cambiar(SerialPort.GetPortNames().Length == 0 ? EstadoConexion.UsbDesconectado : EstadoConexion.SinRespuesta, "Comunicación perdida: " + motivo + ".");
            if (estabaConectado) Evento?.Invoke("error", "Comunicación perdida: " + motivo + ". La posición física de las tapas es desconocida.");
        }

        private void Cambiar(EstadoConexion nuevo, string? error)
        {
            if (nuevo != Estado && nuevo != EstadoConexion.UsbDetectado && nuevo != EstadoConexion.Conectado && error is not null && error != UltimoError)
                Evento?.Invoke("aviso", error);
            Estado = nuevo; UltimoError = error;
        }

        private async Task<string> IntercambiarAsync(string orden, TimeSpan limite)
        {
            SerialPort p;
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_bloqueo) { p = _puerto ?? throw new InvalidOperationException("Sin puerto."); _pendiente = tcs; }
            p.Write(orden + "\n");
            var fin = await Task.WhenAny(tcs.Task, Task.Delay(limite)).ConfigureAwait(false);
            lock (_bloqueo) _pendiente = null;
            if (fin != tcs.Task) throw new TimeoutException("sin respuesta");
            return tcs.Task.Result;
        }

        private void Leer(SerialPort p)
        {
            while (true)
            {
                string linea;
                try { linea = p.ReadLine().Trim(); }
                catch (TimeoutException) { continue; }
                catch (Exception) { return; }
                if (linea.Length == 0 || linea[0] != '{') continue;
                if (linea.StartsWith("{\"evt\"", StringComparison.Ordinal)) { Evento?.Invoke("esp32", linea); continue; }
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
                _pendiente?.TrySetException(new System.IO.IOException("puerto cerrado"));
            }
        }

        public void Dispose() { _fin.Cancel(); Cerrar(); _turno.Dispose(); }
    }
}
