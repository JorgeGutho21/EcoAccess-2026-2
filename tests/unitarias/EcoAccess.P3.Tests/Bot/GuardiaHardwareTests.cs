using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Bot.Dispositivos;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Tests.Logica.Dobles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Bot
{
    /// <summary>
    /// Ninguna prueba automatizada puede llegar a la estación física: ni abriendo un COM ni enviando un comando a un bridge
    /// que corra en otro proceso. Estas pruebas no abren ningún puerto real: la guardia lanza antes de crear el SerialPort.
    /// </summary>
    [TestClass]
    public sealed class GuardiaHardwareTests
    {
        private static readonly byte[] Clave = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        private readonly RelojAjustable _reloj = new(new DateTimeOffset(2026, 11, 12, 6, 0, 0, TimeSpan.Zero));

        [TestMethod]
        public void ElProcesoDePruebasQuedaBloqueadoSinConfigurarNada()
        {
            Assert.IsTrue(GuardiaHardware.Bloqueado);
            StringAssert.Contains(GuardiaHardware.Motivo, "prueba");
        }

        [TestMethod]
        [DataRow("COM3")]
        [DataRow("COM4")]
        [DataRow("COM1")]
        public void NingunPuertoComSeAbreEnPruebas(string puerto)
        {
            var error = Assert.ThrowsExactly<HardwareBloqueadoException>(() => new EnlacePuertoSerial(puerto));
            StringAssert.Contains(error.Message, puerto);
            StringAssert.Contains(error.Message, "No se envió nada");
        }

        [TestMethod]
        [DataRow(8183)]
        [DataRow(8283)]
        [DataRow(8182)]
        public async Task UnComandoNoLlegaAUnBridgeDeOtroProceso(int puerto)
        {
            using var credencial = new CredencialDispositivo("esp32-blanca", 1, 1093, Clave);
            var protocolo = new ProtocoloDispositivoHmac(_reloj, (id, v) => credencial);
            using var canal = new CanalDispositivoHttp(new Uri($"http://127.0.0.1:{puerto}/comandos/"), protocolo, credencial);
            var comando = new ComandoApertura(1, 1093, "guardia", _reloj.Ahora.UtcDateTime.AddMinutes(2));
            await Assert.ThrowsExactlyAsync<HardwareBloqueadoException>(() => canal.EnviarAperturaAsync(comando));
        }

        /// <summary>Un evento va del dispositivo al bridge y no mueve nada: el simulador entre procesos debe poder enviarlo.</summary>
        [TestMethod]
        public async Task LosEventosHaciaUnBridgeNoSeBloquean()
        {
            using var credencial = new CredencialDispositivo("esp32-blanca", 1, 1093, Clave);
            var protocolo = new ProtocoloDispositivoHmac(_reloj, (id, v) => credencial);
            var reserva = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0); reserva.Start();
            int libre = ((System.Net.IPEndPoint)reserva.LocalEndpoint).Port; reserva.Stop();
            using var canal = new CanalDispositivoHttp(new Uri($"http://127.0.0.1:{libre}/eventos/"), protocolo, credencial);
            // Nadie escucha: falla la red, no la guardia.
            Exception error = await Assert.ThrowsAsync<Exception>(() => canal.EnviarPaqueteAsync(new byte[] { (byte)'{', (byte)'}' }));
            Assert.IsNotInstanceOfType<HardwareBloqueadoException>(error);
        }

        [TestMethod]
        public void SoloSeAutorizaUnHijoVivoEnModoDispositivoSimulado()
        {
            using var actual = System.Diagnostics.Process.GetCurrentProcess();
            Assert.ThrowsExactly<HardwareBloqueadoException>(() => GuardiaHardware.AutorizarSimuladorHijo(actual, 8183));
            var bridge = new System.Diagnostics.Process { StartInfo = new System.Diagnostics.ProcessStartInfo("EcoAccess.P3.Bot.exe", "--bridge") };
            Assert.Throws<Exception>(() => GuardiaHardware.AutorizarSimuladorHijo(bridge, 8183));
        }

        [TestMethod]
        public void ElBloqueoExplicitoNoSePuedeDeshacer()
        {
            GuardiaHardware.BloquearEsteProceso("misión nocturna sin hardware");
            GuardiaHardware.BloquearEsteProceso("otro motivo");
            Assert.IsTrue(GuardiaHardware.Bloqueado);
            Assert.ThrowsExactly<ArgumentException>(() => GuardiaHardware.BloquearEsteProceso(" "));
        }
    }
}
