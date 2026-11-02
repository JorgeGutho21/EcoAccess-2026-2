using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Sockets;
using System.Net.Http.Headers;
using EcoAccess.P3.Bot.Dispositivos;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    [DoNotParallelize]
    public sealed class TransporteDispositivoHttpTests
    {
        private readonly RelojAjustable _reloj = new(new DateTimeOffset(2026, 10, 28, 5, 0, 0, TimeSpan.Zero));
        private static readonly byte[] Clave = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        private CredencialDispositivo Credencial() => new("sim-http", 1, 7, Clave);
        private ProtocoloDispositivoHmac Protocolo(CredencialDispositivo c) => new(_reloj, (id, v) => id == c.Dispositivo && v == c.VersionClave ? c : null);
        private ComandoApertura Comando() => new(42, 7, "operacion-http", _reloj.Ahora.UtcDateTime.AddMinutes(2));
        private static Uri Direccion()
        {
            var reserva = new TcpListener(IPAddress.Loopback, 0); reserva.Start();
            int puerto = ((IPEndPoint)reserva.LocalEndpoint).Port; reserva.Stop();
            return new Uri($"http://127.0.0.1:{puerto}/comandos/");
        }

        [TestMethod]
        public async Task HttpRealEntregaSoloConReciboAutenticadoDelMismoMensaje()
        {
            using var c = Credencial(); var p = Protocolo(c); Uri uri = Direccion(); int entregados = 0;
            using var servidor = new ServidorMensajesDispositivoHttp(uri, (paquete, _) =>
            {
                if (!p.ValidarComando(paquete).Exitoso) return Task.FromResult(RespuestaHttpDispositivo.Rechazar());
                entregados++; return Task.FromResult(new RespuestaHttpDispositivo(200, p.FirmarRecepcion(c, paquete, true)));
            });
            using var ct = new CancellationTokenSource(); Task tarea = servidor.EjecutarAsync(ct.Token);
            try
            {
                if (tarea.IsFaulted) await tarea;
                using var canal = new CanalDispositivoHttp(uri, p, c);
                Assert.IsTrue(await canal.EnviarAperturaAsync(Comando())); Assert.AreEqual(1, entregados);
            }
            finally { ct.Cancel(); await tarea; }
        }

        [TestMethod]
        [DataRow(200)]
        [DataRow(302)]
        [DataRow(503)]
        public async Task EstadoHttpSinReciboValidoNoAcreditaEntrega(int estado)
        {
            using var c = Credencial(); var p = Protocolo(c); Uri uri = Direccion();
            using var servidor = new ServidorMensajesDispositivoHttp(uri, (_, _) => Task.FromResult(new RespuestaHttpDispositivo(estado, Array.Empty<byte>())));
            using var ct = new CancellationTokenSource(); Task tarea = servidor.EjecutarAsync(ct.Token);
            try
            {
                if (tarea.IsFaulted) await tarea;
                using var canal = new CanalDispositivoHttp(uri, p, c);
                Assert.IsFalse(await canal.EnviarAperturaAsync(Comando()));
            }
            finally { ct.Cancel(); await tarea; }
        }

        [TestMethod]
        public async Task RechazoFirmadoYReciboDeOtraOrdenNoAcreditanEntrega()
        {
            using var c = Credencial(); var p = Protocolo(c); Uri uri = Direccion(); int modo = 0;
            using var servidor = new ServidorMensajesDispositivoHttp(uri, (paquete, _) => Task.FromResult(new RespuestaHttpDispositivo(200,
                modo == 0 ? p.FirmarRecepcion(c, paquete, false) : p.FirmarRecepcion(c, p.FirmarComando(c, Comando() with { IdOrden = 99 }), true))));
            using var ct = new CancellationTokenSource(); Task tarea = servidor.EjecutarAsync(ct.Token);
            try
            {
                if (tarea.IsFaulted) await tarea;
                using var canal = new CanalDispositivoHttp(uri, p, c);
                Assert.IsFalse(await canal.EnviarAperturaAsync(Comando())); modo = 1;
                Assert.IsFalse(await canal.EnviarAperturaAsync(Comando()));
            }
            finally { ct.Cancel(); await tarea; }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CuerpoExcesivoInclusoFragmentadoNoLlegaAlProcesador(bool fragmentado)
        {
            Uri uri = Direccion(); int procesados = 0;
            using var servidor = new ServidorMensajesDispositivoHttp(uri, (_, _) => { procesados++; return Task.FromResult(new RespuestaHttpDispositivo(200, Array.Empty<byte>())); });
            using var ct = new CancellationTokenSource(); Task tarea = servidor.EjecutarAsync(ct.Token);
            try
            {
                if (tarea.IsFaulted) await tarea;
                using var cliente = new HttpClient(new HttpClientHandler { UseProxy = false });
                using HttpContent contenido = fragmentado ? new ContenidoFragmentado(new byte[4097]) : new ByteArrayContent(new byte[4097]);
                contenido.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                using var respuesta = await cliente.PostAsync(uri, contenido);
                Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, respuesta.StatusCode); Assert.AreEqual(0, procesados);
            }
            finally { ct.Cancel(); await tarea; }
        }

        [TestMethod]
        public async Task FalloDelProcesadorNoFiltraDetallesNiTerminaElServidor()
        {
            using var c = Credencial(); var p = Protocolo(c); Uri uri = Direccion(); int llamadas = 0;
            using var servidor = new ServidorMensajesDispositivoHttp(uri, (paquete, _) =>
            {
                if (++llamadas == 1) throw new InvalidOperationException("detalle privado que no debe viajar por HTTP");
                return Task.FromResult(new RespuestaHttpDispositivo(200, p.FirmarRecepcion(c, paquete, true)));
            });
            using var ct = new CancellationTokenSource(); Task tarea = servidor.EjecutarAsync(ct.Token);
            try
            {
                if (tarea.IsFaulted) await tarea;
                using var cliente = new HttpClient(new HttpClientHandler { UseProxy = false });
                using var contenido = new ByteArrayContent(p.FirmarComando(c, Comando())); contenido.Headers.ContentType = new("application/json");
                using var respuesta = await cliente.PostAsync(uri, contenido);
                Assert.AreEqual(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode); Assert.AreEqual("", await respuesta.Content.ReadAsStringAsync());
                using var canal = new CanalDispositivoHttp(uri, p, c); Assert.IsTrue(await canal.EnviarAperturaAsync(Comando()));
            }
            finally { ct.Cancel(); await tarea; }
        }

        [TestMethod]
        [DataRow("http://localhost:8182/comandos/")]
        [DataRow("http://192.168.1.5:8182/comandos/")]
        [DataRow("http://0.0.0.0:8182/comandos/")]
        [DataRow("http://127.0.0.1:8182/comandos/?clave=x")]
        [DataRow("http://usuario@127.0.0.1:8182/comandos/")]
        [DataRow("http://127.0.0.1:80/comandos/")]
        [DataRow("http://127.0.0.1:8182/otra/")]
        public void ConfiguracionNoPuedeExponerElPiloto(string direccion)
        {
            using var c = Credencial(); var p = Protocolo(c); var uri = new Uri(direccion);
            Assert.ThrowsExactly<ArgumentException>(() => new CanalDispositivoHttp(uri, p, c));
            Assert.ThrowsExactly<ArgumentException>(() => new ServidorMensajesDispositivoHttp(uri, (_, _) => Task.FromResult(new RespuestaHttpDispositivo(200, Array.Empty<byte>()))));
        }

        [TestMethod]
        public async Task CierreEsperaCallbackRealAunqueIgnoreCancelacion()
        {
            var dentro = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var completar = new TaskCompletionSource<RespuestaHttpDispositivo>(TaskCreationOptions.RunContinuationsAsynchronously);
            Uri uri = Direccion(); using var ct = new CancellationTokenSource();
            using var servidor = new ServidorMensajesDispositivoHttp(uri, (_, _) => { dentro.TrySetResult(true); return completar.Task; });
            Task tarea = servidor.EjecutarAsync(ct.Token);
            using var cliente = new HttpClient(new HttpClientHandler { UseProxy = false });
            using var contenido = new ByteArrayContent(new byte[] { 1 }); contenido.Headers.ContentType = new("application/json");
            Task<HttpResponseMessage> peticion = cliente.PostAsync(uri, contenido);
            try
            {
                await dentro.Task.WaitAsync(TimeSpan.FromSeconds(3)); ct.Cancel(); await Task.Delay(100);
                Assert.IsFalse(tarea.IsCompleted, "No se libera la sesión mientras la callback aún puede escribir.");
                completar.TrySetResult(new(200, Array.Empty<byte>())); await tarea.WaitAsync(TimeSpan.FromSeconds(3));
            }
            finally
            {
                completar.TrySetResult(new(503, Array.Empty<byte>())); ct.Cancel(); await tarea.WaitAsync(TimeSpan.FromSeconds(3));
                try { using var respuesta = await peticion; } catch (HttpRequestException) { }
            }
        }

        private sealed class ContenidoFragmentado : HttpContent
        {
            private readonly byte[] _bytes;

            public ContenidoFragmentado(byte[] bytes)
            {
                _bytes = bytes;
            }

            protected override bool TryComputeLength(out long length) { length = 0; return false; }
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => stream.WriteAsync(_bytes).AsTask();
        }
    }
}
