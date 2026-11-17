using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace EcoAccess.P3.Bot.Dispositivos
{

    internal sealed class EnlacePuertoSerial : IEnlaceActuadorSerial
    {
        private readonly SerialPort _puerto;
        internal EnlacePuertoSerial(string nombre)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(nombre, @"\ACOM[1-9][0-9]{0,3}\z"))
                throw new ArgumentException("Selecciona un puerto COM local válido.");
            // Antes de tocar el puerto: en pruebas automatizadas o con ECOACCESS_SIN_HARDWARE=1 ningún COM se abre.
            GuardiaHardware.ExigirPermitido("el puerto " + nombre);
            _puerto = new SerialPort(nombre, 115200) { ReadTimeout = 250, WriteTimeout = 1000, DtrEnable = false, RtsEnable = false };
            try { _puerto.Open(); } catch { _puerto.Dispose(); throw; }
        }
        public Task<string> IntercambiarAsync(string linea, CancellationToken ct) => Task.Run(() =>
        {
            if (linea.Length > 4096 || linea.Contains('\n') || linea.Contains('\r')) throw new InvalidDataException("Línea serial inválida.");
            ct.ThrowIfCancellationRequested();
            _puerto.Write(linea + "\n");
            return Leer(TimeSpan.FromSeconds(8), ct);
        }, ct);

        public Task<string> LeerLineaAsync(TimeSpan limite, CancellationToken ct) => Task.Run(() => Leer(limite, ct), ct);

        private string Leer(TimeSpan limite, CancellationToken ct)
        {
            var tiempo = Stopwatch.StartNew(); var datos = new List<byte>();
            while (tiempo.Elapsed < limite)
            {
                ct.ThrowIfCancellationRequested();
                int b;
                try { b = _puerto.ReadByte(); } catch (TimeoutException) { continue; }
                if (b < 0) throw new IOException("Se desconectó el actuador.");
                if (b == '\n') return Encoding.UTF8.GetString(datos.ToArray()).TrimEnd('\r');
                if (datos.Count >= 4096) throw new InvalidDataException("Línea serial excesiva.");
                datos.Add((byte)b);
            }
            throw new TimeoutException("El actuador no confirmó a tiempo.");
        }
        public void Dispose() => _puerto.Dispose();
    }
}
