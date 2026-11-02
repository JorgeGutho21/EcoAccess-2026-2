using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Sockets;
using System.Text;
using EcoAccess.P3.Bot;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Bot
{

    /// <summary>Levanta el <see cref="ServidorWebhook"/> real en 127.0.0.1 con un puerto libre; no sale a la red.</summary>
    [TestClass]
    public sealed class ServidorWebhookTests
    {
        private const string Secreto = "secreto_de_prueba-0123456789";
        private const string UpdateValido =
            @"{""update_id"":91,""message"":{""from"":{""id"":700},""chat"":{""id"":700,""type"":""private""},""text"":""/help""}}";

        private sealed class Servidor : IAsyncDisposable, IDisposable
        {
            private readonly CancellationTokenSource _cancelacion = new();

            public Servidor(TimeSpan plazoLectura)
            {
                Puerto = PuertosHttpPruebas.Siguiente();
                var procesador = new ProcesadorUpdates((m, _) =>
                {
                    lock (Atendidos)
                    {
                        Atendidos.Add(m.UpdateId);
                    }

                    return Task.FromResult<IReadOnlyList<RespuestaBot>>(Array.Empty<RespuestaBot>());
                }, new ClienteTelegramSimulado(), _ => { });
                var servidor = new ServidorWebhook(
                    new ReceptorWebhook(procesador, Secreto), $"http://127.0.0.1:{Puerto}/telegram/", t => Registro.Add(t), plazoLectura);
                Ejecucion = servidor.EjecutarAsync(_cancelacion.Token);
            }

            public int Puerto { get; }

            public Task Ejecucion { get; }

            public List<long> Atendidos { get; } = new List<long>();

            public System.Collections.Concurrent.ConcurrentBag<string> Registro { get; } = new System.Collections.Concurrent.ConcurrentBag<string>();

            public Uri Ruta => new($"http://127.0.0.1:{Puerto}/telegram/");

            public void Cancelar() => _cancelacion.Cancel();

            public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();



            public async ValueTask DisposeAsync()
            {
                _cancelacion.Cancel();
                await Ejecucion.WaitAsync(TimeSpan.FromSeconds(10));
                _cancelacion.Dispose();
            }

        }

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

        private static async Task<HttpStatusCode> PostAsync(Uri ruta, string? secreto, string cuerpo)
        {
            using var peticion = new HttpRequestMessage(HttpMethod.Post, ruta) { Content = new StringContent(cuerpo, Encoding.UTF8, "application/json") };
            if (secreto is not null)
            {
                peticion.Headers.Add(ReceptorWebhook.EncabezadoSecreto, secreto);
            }

            using HttpResponseMessage respuesta = await Http.SendAsync(peticion);
            return respuesta.StatusCode;
        }

        [TestMethod]
        public async Task SecretoCorrecto_Devuelve200YAtiende()
        {
            using var servidor = new Servidor(TimeSpan.FromSeconds(5));

            Assert.AreEqual(HttpStatusCode.OK, await PostAsync(servidor.Ruta, Secreto, UpdateValido));

            CollectionAssert.AreEqual(new long[] { 91 }, servidor.Atendidos);
        }

        [TestMethod]
        public async Task SecretoAusenteODistinto_Devuelve401SinAtender()
        {
            using var servidor = new Servidor(TimeSpan.FromSeconds(5));

            Assert.AreEqual(HttpStatusCode.Unauthorized, await PostAsync(servidor.Ruta, null, UpdateValido));
            Assert.AreEqual(HttpStatusCode.Unauthorized, await PostAsync(servidor.Ruta, Secreto + "x", UpdateValido));

            Assert.IsEmpty(servidor.Atendidos);
        }

        [TestMethod]
        public async Task MetodoDistintoDePost_Devuelve405()
        {
            using var servidor = new Servidor(TimeSpan.FromSeconds(5));

            using HttpResponseMessage respuesta = await Http.GetAsync(servidor.Ruta);

            Assert.AreEqual(HttpStatusCode.MethodNotAllowed, respuesta.StatusCode);
        }

        [TestMethod]
        public async Task ClienteLento_Recibe408YElServidorSigueAtendiendo()
        {
            using var servidor = new Servidor(TimeSpan.FromMilliseconds(300));

            // Anuncia 100 bytes y no envía ninguno.
            using (var lento = new TcpClient())
            {
                await lento.ConnectAsync(IPAddress.Loopback, servidor.Puerto);
                NetworkStream flujo = lento.GetStream();
                string cabecera = $"POST /telegram/ HTTP/1.1\r\nHost: 127.0.0.1:{servidor.Puerto}\r\n" +
                                  $"{ReceptorWebhook.EncabezadoSecreto}: {Secreto}\r\nContent-Length: 100\r\n\r\n";
                await flujo.WriteAsync(Encoding.ASCII.GetBytes(cabecera));
                using var lector = new StreamReader(flujo, Encoding.ASCII);
                string? estado = await lector.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));
                StringAssert.Contains(estado, " 408 ");
            }

            Assert.AreEqual(HttpStatusCode.OK, await PostAsync(servidor.Ruta, Secreto, UpdateValido));
            CollectionAssert.AreEqual(new long[] { 91 }, servidor.Atendidos);
        }

        [TestMethod]
        public async Task Cancelacion_TerminaLaEjecucion()
        {
            using var servidor = new Servidor(TimeSpan.FromSeconds(5));
            Assert.AreEqual(HttpStatusCode.OK, await PostAsync(servidor.Ruta, Secreto, UpdateValido));

            servidor.Cancelar();

            await servidor.Ejecucion.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.AreEqual(TaskStatus.RanToCompletion, servidor.Ejecucion.Status);
        }

        [TestMethod]
        public void Constructor_ValidaArgumentos()
        {
            var receptor = new ReceptorWebhook(new ProcesadorUpdates((_, _) => Task.FromResult<IReadOnlyList<RespuestaBot>>(Array.Empty<RespuestaBot>()),
                new ClienteTelegramSimulado(), _ => { }), Secreto);

            Assert.ThrowsExactly<ArgumentNullException>(() => new ServidorWebhook(null!, "http://127.0.0.1:1/x/", _ => { }));
            Assert.ThrowsExactly<ArgumentException>(() => new ServidorWebhook(receptor, " ", _ => { }));
            Assert.ThrowsExactly<ArgumentNullException>(() => new ServidorWebhook(receptor, "http://127.0.0.1:1/x/", null!));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new ServidorWebhook(receptor, "http://127.0.0.1:1/x/", _ => { }, TimeSpan.Zero));
        }
    }
}
