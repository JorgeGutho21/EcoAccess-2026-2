using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using EcoAccess.P3.Bot.Dispositivos;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class RecepcionesDispositivoTests
    {
        private readonly RelojAjustable _reloj = new(new DateTimeOffset(2026, 10, 28, 5, 0, 0, TimeSpan.Zero));
        // Clave sintética sin vínculo con secretos ni dispositivos del proyecto.
        private static readonly byte[] Clave = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        private CredencialDispositivo Credencial(string id = "simulado", int version = 1, int contenedor = 7) => new(id, version, contenedor, Clave);
        private ProtocoloDispositivoHmac Protocolo(params CredencialDispositivo[] credenciales) => new(_reloj,
            (id, v) => credenciales.SingleOrDefault(c => c.Dispositivo == id && c.VersionClave == v));
        private ComandoApertura Comando(int orden = 42, string nonce = "operacion-unica") => new(orden, 7, nonce, _reloj.Ahora.UtcDateTime.AddMinutes(2));

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void RecepcionAutenticadaDistingueAceptacionYRechazo(bool aceptado)
        {
            using var c = Credencial(); var p = Protocolo(c);
            byte[] comando = p.FirmarComando(c, Comando());
            var r = p.ValidarRecepcion(p.FirmarRecepcion(c, comando, aceptado), comando);
            Assert.IsTrue(r.Exitoso); Assert.AreEqual(aceptado, r.Valor.Aceptado);
        }

        [TestMethod]
        public void ReciboDeOtraOrdenONonceNoConfirmaElMensajeEsperado()
        {
            using var c = Credencial(); var p = Protocolo(c);
            byte[] original = p.FirmarComando(c, Comando());
            byte[] recibo = p.FirmarRecepcion(c, original, true);
            Assert.IsFalse(p.ValidarRecepcion(recibo, p.FirmarComando(c, Comando(43))).Exitoso);
            Assert.IsFalse(p.ValidarRecepcion(recibo, p.FirmarComando(c, Comando(nonce: "otra-operacion"))).Exitoso);
        }

        [TestMethod]
        public void ReciboDeOtroDispositivoOVersionNoConfirmaElMensajeEsperado()
        {
            using var c = Credencial(); using var otro = Credencial("otro"); using var version = Credencial(version: 2);
            var p = Protocolo(c, otro, version); byte[] esperado = p.FirmarComando(c, Comando());
            foreach (var x in new[] { otro, version })
            {
                byte[] original = p.FirmarComando(x, Comando());
                Assert.IsFalse(p.ValidarRecepcion(p.FirmarRecepcion(x, original, true), esperado).Exitoso);
                Assert.ThrowsExactly<ArgumentException>(() => p.FirmarRecepcion(c, original, true));
            }
        }

        [TestMethod]
        public void EventoYReciboAlteradoOVacioNoSeConfundenConEntrega()
        {
            using var c = Credencial(); var p = Protocolo(c); byte[] comando = p.FirmarComando(c, Comando());
            byte[] evento = p.FirmarEvento(c, new EventoRecibido(7, "evento-1", TipoEventoDispositivo.Acuse, 42, _reloj.Ahora.UtcDateTime));
            Assert.IsFalse(p.ValidarRecepcion(evento, comando).Exitoso);
            byte[] recibo = p.FirmarRecepcion(c, comando, true); recibo[^3] ^= 1;
            Assert.IsFalse(p.ValidarRecepcion(recibo, comando).Exitoso);
            Assert.IsFalse(p.ValidarRecepcion(Array.Empty<byte>(), comando).Exitoso);
            Assert.IsFalse(p.ValidarRecepcion(Encoding.UTF8.GetBytes("{}"), comando).Exitoso);
        }

        [TestMethod]
        public void EventoTieneReciboPropioYNoPuedeReutilizarElDeUnComando()
        {
            using var c = Credencial(); var p = Protocolo(c); byte[] comando = p.FirmarComando(c, Comando());
            byte[] evento = p.FirmarEvento(c, new EventoRecibido(7, "evento-1", TipoEventoDispositivo.Acuse, 42, _reloj.Ahora.UtcDateTime));
            Assert.IsTrue(p.ValidarRecepcion(p.FirmarRecepcion(c, evento, true), evento).Valor.Aceptado);
            Assert.IsFalse(p.ValidarRecepcion(p.FirmarRecepcion(c, comando, true), evento).Exitoso);
        }

        [TestMethod]
        public void RecepcionCaducaEnElInstanteExactoDelLimite()
        {
            using var c = Credencial(); var p = Protocolo(c); byte[] comando = p.FirmarComando(c, Comando());
            byte[] recibo = p.FirmarRecepcion(c, comando, true);
            _reloj.Avanzar(TimeSpan.FromSeconds(29)); Assert.IsTrue(p.ValidarRecepcion(recibo, comando).Exitoso);
            _reloj.Avanzar(TimeSpan.FromSeconds(1)); Assert.IsFalse(p.ValidarRecepcion(recibo, comando).Exitoso);
        }

        [TestMethod]
        public void ComandoYEventoCaducanComoLaOrdenEnElInstanteExacto()
        {
            using var c = Credencial(); var p = Protocolo(c); byte[] comando = p.FirmarComando(c, Comando());
            byte[] evento = p.FirmarEvento(c, new EventoRecibido(7, "evento-1", TipoEventoDispositivo.Acuse, 42, _reloj.Ahora.UtcDateTime));
            _reloj.Avanzar(TimeSpan.FromMinutes(2));
            Assert.IsFalse(p.ValidarComando(comando).Exitoso); Assert.IsFalse(p.ValidarEvento(evento).Exitoso);
            Assert.ThrowsExactly<ArgumentException>(() => p.FirmarRecepcion(c, comando, true));
        }
    }
}
