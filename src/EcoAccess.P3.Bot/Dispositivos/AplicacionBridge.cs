using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.Extensions.Configuration;
using EcoAccess.P3.Bot.Transportes;

namespace EcoAccess.P3.Bot.Dispositivos
{

    /// <summary>Modos de consola del mismo ejecutable, independientes del token y del registro de Telegram.</summary>
    public static class AplicacionBridge
    {
        public static Task<int> EjecutarSimuladorAsync(IConfiguration configuracion, TimeProvider reloj) =>
            EjecutarAsync("--dispositivo-simulado", configuracion, null, null, reloj);

        public static Task<int> EjecutarSerialAsync(IConfiguration configuracion, TimeProvider reloj) =>
            EjecutarAsync("--dispositivo-serial", configuracion, null, null, reloj);

        public static async Task<int> EjecutarAsync(string modo, IConfiguration configuracion,
            IRepositorioInteracciones? repositorio, ServicioInteracciones? servicio, TimeProvider reloj)
        {
            byte[]? clave = null;
            using var parar = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelar = (_, e) => { e.Cancel = true; parar.Cancel(); };
            Console.CancelKeyPress += cancelar;

            try
            {
                using var control = new ControlParadaLocal(parar);
                if (modo is not ("--bridge" or "--dispositivo-simulado" or "--dispositivo-serial")) throw new ArgumentException("Modo de bridge inválido.");
                var c = ConfiguracionBridge.Leer(configuracion);
                string? secreto = configuracion["Bridge:ClaveHmacBase64"];
                if (!Compat.IsNullOrWhiteSpace(secreto)) clave = Convert.FromBase64String(secreto);
                else
                {
                    string? archivo = configuracion["Bridge:ArchivoClaveHmac"];
                    if (Compat.IsNullOrWhiteSpace(archivo) || !Compat.IsPathFullyQualified(archivo)) throw new ArgumentException("Falta la clave HMAC privada.");
                    ConfiguracionBridge.ValidarRutaPrivada(Path.GetDirectoryName(archivo)!);
                    using var entrada = new FileStream(archivo, FileMode.Open, FileAccess.Read, FileShare.Read);
                    if (entrada.Length is < 32 or > 128) throw new ArgumentException("Longitud de clave HMAC inválida.");
                    clave = new byte[(int)entrada.Length]; entrada.ReadExactly(clave);
                }
                using var credencial = new CredencialDispositivo(c.Dispositivo, c.VersionClave, c.IdContenedor, clave);
                Compat.ZeroMemory(clave); clave = null;
                void Registrar(string texto) => Console.WriteLine($"[{reloj.GetUtcNow():O}] {texto}");
                if (modo == "--bridge") await new BridgeLocal(c, credencial,
                    repositorio ?? throw new ArgumentException("Falta repositorio del bridge."),
                    servicio ?? throw new ArgumentException("Falta servicio del bridge."), reloj, Registrar).EjecutarAsync(parar.Token);
                else if (modo == "--dispositivo-serial")
                {
                    string puerto = configuracion["Bridge:PuertoSerial"] ?? throw new ArgumentException("Falta puerto COM.");
                    var protocolo = new ProtocoloDispositivoHmac(reloj, (id, v) => id == c.Dispositivo && v == c.VersionClave ? credencial : null);
                    using var autoridad = new CanalDispositivoHttp(c.Vigencia, protocolo, credencial);
                    var serial = new ActuacionSerial(credencial, protocolo, () => new EnlacePuertoSerial(puerto), autoridad.ConsultarVigenciaAsync, reloj);
                    await new DispositivoHttpConColas(c, credencial, reloj, Registrar, actuar: async (comando, guardar, ct) =>
                    {
                        await serial.EjecutarAsync(comando, ct);
                        // Hechos observados por el gateway: recibo autenticado de ejecución PWM; no apertura física.
                        await guardar(new(comando.IdContenedor, $"usb:{comando.IdOrden}:acuse", EcoAccess.P3.Entidades.TipoEventoDispositivo.Acuse,
                            comando.IdOrden, reloj.GetUtcNow().UtcDateTime), ct);
                        await guardar(new(comando.IdContenedor, $"usb:{comando.IdOrden}:sin-sensor", EcoAccess.P3.Entidades.TipoEventoDispositivo.Fallo,
                            comando.IdOrden, reloj.GetUtcNow().UtcDateTime), ct);
                    }).EjecutarAsync(parar.Token);
                }
                else
                {
                    if (!Enum.TryParse(configuracion["Bridge:EscenarioSimulado"] ?? "Normal", true, out EscenarioCaneca escenario) || !Compat.IsDefined(escenario))
                        throw new ArgumentException("Escenario simulado inválido.");
                    await new DispositivoSimuladoHttp(c, credencial, reloj, Registrar, escenario).EjecutarAsync(parar.Token);
                }
                return 0;
            }
            catch (OperationCanceledException) when (parar.IsCancellationRequested) { return 0; }
            catch (Exception e) when (e is ArgumentException or FormatException)
            {
                Console.Error.WriteLine("Configuración del bridge inválida. Revisa Bridge:IdContenedor, Dispositivo, VersionClave, ArchivoClaveHmac (o ClaveHmacBase64), DirectorioEstado y endpoints locales. No compartas la clave.");
                return 1;
            }
            catch (Exception)
            {
                Console.Error.WriteLine("El bridge no pudo iniciar o completar el ciclo. Revisa Oracle, puertos y diario; no borres las reservas ni repitas una actuación incierta.");
                return 1;
            }
            finally
            {
                Console.CancelKeyPress -= cancelar;
                if (clave is not null) Compat.ZeroMemory(clave);
            }
        }
    }
}
