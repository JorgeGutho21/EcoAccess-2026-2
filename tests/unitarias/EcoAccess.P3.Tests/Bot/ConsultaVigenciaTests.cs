using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Bot.Dispositivos;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass, DoNotParallelize]
    public sealed class ConsultaVigenciaTests
    {
        private readonly RelojAjustable _reloj = new(new DateTimeOffset(2026, 10, 28, 5, 0, 0, TimeSpan.Zero));
        private static CredencialDispositivo Credencial() => new("vigencia-prueba", 1, 7, Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());
        private ProtocoloDispositivoHmac Protocolo(CredencialDispositivo c) => new(_reloj, (id, v) => id == c.Dispositivo && v == c.VersionClave ? c : null);
        private ComandoApertura Comando() => new(42, 7, "operacion-prueba", _reloj.Ahora.UtcDateTime.AddMinutes(2));
        private static Uri Direccion() => new($"http://127.0.0.1:{PuertosHttpPruebas.Siguiente()}/vigencia/");

        [TestMethod]
        public void CadaConsultaTieneIdentidadNuevaYNoReutilizaRecibos()
        {
            using var c = Credencial(); var p = Protocolo(c); var comando = Comando();
            byte[] una = p.FirmarConsultaVigencia(c, comando), otra = p.FirmarConsultaVigencia(c, comando);
            Assert.IsFalse(una.SequenceEqual(otra)); Assert.AreEqual(comando, p.ValidarConsultaVigencia(una).Valor);
            byte[] recibo = p.FirmarRecepcion(c, una, true);
            Assert.IsTrue(p.ValidarRecepcion(recibo, una).Valor.Aceptado);
            Assert.IsFalse(p.ValidarRecepcion(recibo, otra).Exitoso);
            byte[] entrega = p.FirmarComando(c, comando);
            Assert.IsFalse(p.ValidarRecepcion(p.FirmarRecepcion(c, entrega, true), una).Exitoso);
            Assert.IsFalse(p.ValidarComando(una).Exitoso); Assert.IsFalse(p.ValidarEvento(una).Exitoso);
        }

        [TestMethod]
        [DataRow(30)]
        [DataRow(5)]
        public void ConsultaYRespuestaNoSobrevivenSuLimiteNiElDeLaOrden(int segundos)
        {
            using var c = Credencial(); var p = Protocolo(c);
            var comando = Comando() with { VenceUtc = _reloj.Ahora.UtcDateTime.AddSeconds(segundos) };
            byte[] consulta = p.FirmarConsultaVigencia(c, comando), recibo = p.FirmarRecepcion(c, consulta, true);
            _reloj.Avanzar(TimeSpan.FromSeconds(segundos));
            Assert.IsFalse(p.ValidarConsultaVigencia(consulta).Exitoso); Assert.IsFalse(p.ValidarRecepcion(recibo, consulta).Exitoso);
            Assert.ThrowsExactly<ArgumentException>(() => p.FirmarRecepcion(c, consulta, true));
        }

        [TestMethod]
        public void FirmaAlteradaYDispositivoDesconocidoNoConsultanAutoridad()
        {
            using var c = Credencial(); var p = Protocolo(c); byte[] consulta = p.FirmarConsultaVigencia(c, Comando());
            Assert.IsFalse(new ProtocoloDispositivoHmac(_reloj, (_, _) => null).ValidarConsultaVigencia(consulta).Exitoso);
            consulta[^3] ^= 1; Assert.IsFalse(p.ValidarConsultaVigencia(consulta).Exitoso);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task HttpDistingueAutorizacionYRevocacionFirmadas(bool vigente)
        {
            using var c = Credencial(); var p = Protocolo(c); Uri uri = Direccion(); using var ct = new CancellationTokenSource();
            using var servidor = new ServidorMensajesDispositivoHttp(uri, (bytes, _) => Task.FromResult(
                p.ValidarConsultaVigencia(bytes).Exitoso ? new RespuestaHttpDispositivo(200, p.FirmarRecepcion(c, bytes, vigente)) : RespuestaHttpDispositivo.Rechazar()));
            Task tarea = servidor.EjecutarAsync(ct.Token);
            try { using var canal = new CanalDispositivoHttp(uri, p, c); Assert.AreEqual(vigente, await canal.ConsultarVigenciaAsync(Comando())); }
            finally { ct.Cancel(); await tarea; }
        }

        [TestMethod]
        public async Task ErrorDeAutoridadNoSeConvierteEnRevocacionNiAutorizacion()
        {
            using var c = Credencial(); var p = Protocolo(c); Uri uri = Direccion(); using var ct = new CancellationTokenSource();
            using var servidor = new ServidorMensajesDispositivoHttp(uri, (_, _) => Task.FromResult(new RespuestaHttpDispositivo(503, Array.Empty<byte>())));
            Task tarea = servidor.EjecutarAsync(ct.Token);
            try { using var canal = new CanalDispositivoHttp(uri, p, c); await Assert.ThrowsExactlyAsync<IOException>(() => canal.ConsultarVigenciaAsync(Comando())); }
            finally { ct.Cancel(); await tarea; }
        }
    }
}
