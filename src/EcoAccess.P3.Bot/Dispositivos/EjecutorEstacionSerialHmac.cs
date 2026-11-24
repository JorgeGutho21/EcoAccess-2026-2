using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Logica.Vision35;
using Microsoft.Extensions.Configuration;

namespace EcoAccess.P3.Bot.Dispositivos
{
    /// <summary>
    /// Ejecutor físico real de la estación de tres canecas (Blanca 1093, Verde 1400, Negra 1401) sobre el puerto serial
    /// compartido (COM3), reutilizando exactamente ActuacionSerial + ProtocoloDispositivoHmac + bridge-*.json privados.
    /// Serializa las aperturas una por una (nunca compite por COM3) y respeta siempre GuardiaHardware.
    /// </summary>
    internal sealed class EjecutorEstacionSerialHmac : IEjecutorAperturas, IDisposable
    {
        private sealed record EntradaCaneca(CredencialDispositivo Credencial, ProtocoloDispositivoHmac Protocolo, string PuertoSerial);

        private readonly Dictionary<int, EntradaCaneca> _canecas;
        private readonly TimeProvider _reloj;
        private readonly SemaphoreSlim _bloqueoSerial = new(1, 1);

        public bool EsSimulado => false;

        public IReadOnlyCollection<int> ContenedoresConfigurados => _canecas.Keys;

        private EjecutorEstacionSerialHmac(Dictionary<int, EntradaCaneca> canecas, TimeProvider reloj)
        {
            _canecas = canecas;
            _reloj = reloj;
        }

        /// <summary>
        /// Carga las credenciales privadas bridge-blanca.json, bridge-verde.json y bridge-negra.json desde ProgramData
        /// (o el directorio indicado). Nunca imprime ni expone las claves HMAC.
        /// </summary>
        public static EjecutorEstacionSerialHmac? IntentarCargarDesdeProgramData(TimeProvider reloj, string? directorioBridge = null)
        {
            string dir = directorioBridge ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "EcoAccessP3Framework",
                "Bridge");
            if (!Directory.Exists(dir)) return null;

            string[] archivos = { "bridge-blanca.json", "bridge-verde.json", "bridge-negra.json" };
            var entradas = new Dictionary<int, EntradaCaneca>();
            try
            {
                ConfiguracionBridge.ValidarRutaPrivada(dir);
                foreach (string nombre in archivos)
                {
                    string ruta = Path.Combine(dir, nombre);
                    if (!File.Exists(ruta)) continue;
                    IConfiguration cfg = new ConfigurationBuilder()
                        .AddJsonFile(ruta, optional: false, reloadOnChange: false)
                        .Build();
                    var c = ConfiguracionBridge.Leer(cfg);
                    string? puerto = cfg["Bridge:PuertoSerial"];
                    if (Compat.IsNullOrWhiteSpace(puerto)) continue;
                    byte[] clave = LeerClavePrivada(cfg);
                    try
                    {
                        var cred = new CredencialDispositivo(c.Dispositivo, c.VersionClave, c.IdContenedor, clave);
                        var proto = new ProtocoloDispositivoHmac(reloj, (id, v) => id == c.Dispositivo && v == c.VersionClave ? cred : null);
                        entradas[c.IdContenedor] = new EntradaCaneca(cred, proto, puerto!);
                    }
                    finally
                    {
                        Compat.ZeroMemory(clave);
                    }
                }

                if (entradas.Count == 0) return null;
                return new EjecutorEstacionSerialHmac(entradas, reloj);
            }
            catch
            {
                foreach (var e in entradas.Values) e.Credencial.Dispose();
                return null;
            }
        }

        private static byte[] LeerClavePrivada(IConfiguration cfg)
        {
            string? secreto = cfg["Bridge:ClaveHmacBase64"];
            if (!Compat.IsNullOrWhiteSpace(secreto)) return Convert.FromBase64String(secreto!);
            string? archivo = cfg["Bridge:ArchivoClaveHmac"];
            if (Compat.IsNullOrWhiteSpace(archivo) || !Compat.IsPathFullyQualified(archivo!))
                throw new ArgumentException("Falta ArchivoClaveHmac privada.");
            ConfiguracionBridge.ValidarRutaPrivada(Path.GetDirectoryName(archivo!)!);
            using var fs = new FileStream(archivo!, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length is < 32 or > 128) throw new ArgumentException("Longitud de clave HMAC inválida.");
            byte[] buf = new byte[(int)fs.Length];
            fs.ReadExactly(buf);
            return buf;
        }

        public async Task<ResultadoEjecucionApertura> AbrirAsync(ComandoApertura comando, CancellationToken ct = default)
        {
            GuardiaHardware.ExigirPermitido($"estación física (caneca {comando.IdContenedor})");
            if (!_canecas.TryGetValue(comando.IdContenedor, out EntradaCaneca? entrada))
                return new ResultadoEjecucionApertura(EstadoEjecucionApertura.Rechazada, Array.Empty<TipoEventoDispositivo>());

            await _bloqueoSerial.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                string puertoEfectivo = entrada.PuertoSerial;
                if (!System.IO.Ports.SerialPort.GetPortNames().Any(p => string.Equals(p, puertoEfectivo, StringComparison.OrdinalIgnoreCase)))
                {
                    string? descubierto = DescubridorPuertoEsp32.ResolverPuerto(puertoEfectivo, out _);
                    if (!Compat.IsNullOrWhiteSpace(descubierto)) puertoEfectivo = descubierto!;
                }

                var eventos = new List<TipoEventoDispositivo>();
                var actuacion = new ActuacionSerial(
                    entrada.Credencial,
                    entrada.Protocolo,
                    () => new EnlacePuertoSerial(puertoEfectivo),
                    (_, _) => Task.FromResult(true),
                    _reloj);

                await actuacion.EjecutarAsync(comando, _ =>
                {
                    eventos.Add(TipoEventoDispositivo.Acuse);
                    eventos.Add(TipoEventoDispositivo.Apertura);
                    return Task.CompletedTask;
                }, ct).ConfigureAwait(false);

                eventos.Add(TipoEventoDispositivo.CierreSinSensor);
                return new ResultadoEjecucionApertura(EstadoEjecucionApertura.Completada, eventos);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (HardwareBloqueadoException)
            {
                throw;
            }
            catch (Exception)
            {
                return new ResultadoEjecucionApertura(EstadoEjecucionApertura.SinRespuesta, new[] { TipoEventoDispositivo.Fallo });
            }
            finally
            {
                _bloqueoSerial.Release();
            }
        }

        public void Dispose()
        {
            foreach (var e in _canecas.Values) e.Credencial.Dispose();
            _bloqueoSerial.Dispose();
        }
    }
}
