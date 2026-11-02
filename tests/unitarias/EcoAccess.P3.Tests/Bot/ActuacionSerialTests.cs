using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using System.Text;
using EcoAccess.P3.Bot.Dispositivos;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class ActuacionSerialTests
    {
        private const string Nonce = "0123456789abcdef0123456789abcdef";
        private readonly RelojAjustable _reloj = new(new DateTimeOffset(2026, 11, 1, 1, 0, 0, TimeSpan.Zero));
        private sealed class Enlace : IEnlaceActuadorSerial
        {
            private readonly Func<string, string> _responder;

            public Enlace(Func<string, string> responder)
            {
                _responder = responder;
            }

            internal int Llamadas;
            internal bool Cerrado;
            public Task<string> IntercambiarAsync(string linea, CancellationToken ct) { ct.ThrowIfCancellationRequested(); Llamadas++; return Task.FromResult(_responder(linea)); }
            public void Dispose() => Cerrado = true;
        }
        [TestMethod]
        public async Task PermisoNuevoLigadoAComandoYRecibo_NoConfirmaSensor()
        {
            using var clave = new CredencialDispositivo("prueba-usb", 1, 7, Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());
            var protocolo = new ProtocoloDispositivoHmac(_reloj, (_, _) => clave);
            var comando = new ComandoApertura(42, 7, "orden-usb-prueba", _reloj.GetUtcNow().UtcDateTime.AddSeconds(2));
            var enlace = new Enlace(linea =>
            {
                if (linea == "?") return "{\"ecoaccessDesafio\":\"" + Nonce + "\"}";
                byte[] original = Encoding.UTF8.GetBytes(linea);
                var permiso = protocolo.Leer<PermisoActuacionSerial>(original, "actuacion-serial")!.Value.Cuerpo;
                Assert.AreEqual(comando, permiso.Comando); Assert.AreEqual(Nonce, permiso.Desafio);
                Assert.AreEqual(2000, permiso.ValidezMs);
                return Encoding.UTF8.GetString(ProtocoloDispositivoHmac.CrearSobre(clave, "recepcion-serial",
                    new ReciboActuacionSerial(Nonce, Compat.ToHexString(Compat.Sha256(original)), true, "pwm-completado-sin-sensor")));
            });
            await new ActuacionSerial(clave, protocolo, () => enlace, (_, _) => Task.FromResult(true), _reloj).EjecutarAsync(comando, default);
            Assert.AreEqual(2, enlace.Llamadas); Assert.IsTrue(enlace.Cerrado);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, true)]
        public async Task RevocacionOVencimientoDuranteHandshake_NoEntregaPermiso(bool autorizado, bool vencer)
        {
            using var clave = new CredencialDispositivo("prueba-usb", 1, 7, new byte[32]);
            var protocolo = new ProtocoloDispositivoHmac(_reloj, (_, _) => clave);
            var comando = new ComandoApertura(42, 7, "prueba", _reloj.GetUtcNow().UtcDateTime.AddSeconds(1));
            var enlace = new Enlace(_ => "{\"ecoaccessDesafio\":\"" + Nonce + "\"}");
            var actuar = new ActuacionSerial(clave, protocolo, () => enlace, (_, _) =>
            { if (vencer) _reloj.Avanzar(TimeSpan.FromSeconds(2)); return Task.FromResult(autorizado); }, _reloj);
            if (vencer) await Assert.ThrowsExactlyAsync<InvalidDataException>(() => actuar.EjecutarAsync(comando, default));
            else await Assert.ThrowsExactlyAsync<IOException>(() => actuar.EjecutarAsync(comando, default));
            Assert.AreEqual(1, enlace.Llamadas); Assert.IsTrue(enlace.Cerrado);
        }

        [TestMethod]
        public async Task ReciboSinFirmaSeRechaza_YCancelacionNoEsExito()
        {
            using var clave = new CredencialDispositivo("prueba-usb", 1, 7, new byte[32]);
            var protocolo = new ProtocoloDispositivoHmac(_reloj, (_, _) => clave);
            var comando = new ComandoApertura(42, 7, "prueba", _reloj.GetUtcNow().UtcDateTime.AddSeconds(10));
            var enlace = new Enlace(l => l == "?" ? "{\"ecoaccessDesafio\":\"" + Nonce + "\"}" : "{\"aceptado\":true}");
            var actuar = new ActuacionSerial(clave, protocolo, () => enlace, (_, _) => Task.FromResult(true), _reloj);
            await Assert.ThrowsExactlyAsync<IOException>(() => actuar.EjecutarAsync(comando, default));
            using var cancelar = new CancellationTokenSource(); cancelar.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => actuar.EjecutarAsync(comando, cancelar.Token));
        }
    }
}
