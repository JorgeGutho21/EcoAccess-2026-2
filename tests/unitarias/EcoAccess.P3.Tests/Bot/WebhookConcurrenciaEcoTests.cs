using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Text;
using EcoAccess.P3.Bot;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class WebhookConcurrenciaEcoTests
    {
        [TestMethod]
        public async Task SaturacionDevuelve503YLaParadaDrenaPeticionesActivas()
        {
            const string secreto = "secreto-fixture-webhook-0123456789";
            int puerto = PuertosHttpPruebas.Siguiente(), activas = 0;
            var ocho = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var liberar = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancelar = new CancellationTokenSource();
            var p = new ProcesadorUpdates(async (_, ct) =>
            { if (Interlocked.Increment(ref activas) == 8) ocho.TrySetResult(true); await liberar.Task.WaitAsync(ct); return Array.Empty<RespuestaBot>(); },
                new ClienteTelegramSimulado(), _ => { });
            var s = new ServidorWebhook(new(p, secreto), $"http://127.0.0.1:{puerto}/telegram/", _ => { });
            Task ejecucion = s.EjecutarAsync(cancelar.Token);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            async Task<HttpResponseMessage> Enviar(int id)
            {
                using var peticion = new HttpRequestMessage(HttpMethod.Post, $"http://127.0.0.1:{puerto}/telegram/");
                peticion.Headers.Add(ReceptorWebhook.EncabezadoSecreto, secreto);
                peticion.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
                {
                    update_id = id,
                    message = new { from = new { id }, chat = new { id, type = "private" }, text = "/estado" }
                }), Encoding.UTF8, "application/json");
                return await http.SendAsync(peticion);
            }
            Task<HttpResponseMessage>[] primeras = Enumerable.Range(700, 8).Select(Enviar).ToArray();
            try
            {
                await ocho.Task.WaitAsync(TimeSpan.FromSeconds(5));
                using var rechazada = await Enviar(800);
                Assert.AreEqual(HttpStatusCode.ServiceUnavailable, rechazada.StatusCode);
                Assert.AreEqual(TimeSpan.FromSeconds(1), rechazada.Headers.RetryAfter?.Delta);
                using (var clienteQueAbandona = new System.Net.Sockets.TcpClient())
                {
                    await clienteQueAbandona.ConnectAsync("127.0.0.1", puerto);
                    await clienteQueAbandona.GetStream().WriteAsync(Encoding.ASCII.GetBytes(
                        $"POST /telegram/ HTTP/1.1\r\nHost: 127.0.0.1:{puerto}\r\nContent-Length: 0\r\n\r\n"));
                }
                using var todaviaDisponible = await Enviar(801);
                Assert.AreEqual(HttpStatusCode.ServiceUnavailable, todaviaDisponible.StatusCode);
                cancelar.Cancel();
                await ejecucion.WaitAsync(TimeSpan.FromSeconds(5));
            }
            finally
            {
                liberar.TrySetResult(true); cancelar.Cancel();
                await ejecucion.WaitAsync(TimeSpan.FromSeconds(5));
                foreach (var solicitud in primeras)
                    try { using var respuesta = await solicitud; } catch (HttpRequestException) { }
            }
        }

        [TestMethod]
        public async Task UnaConversacionLentaNoBloqueaLaConsultaDeOtra()
        {
            const string secreto = "secreto-fixture-webhook-0123456789";
            int puerto = PuertosHttpPruebas.Siguiente();
            var entrada = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var liberar = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancelar = new CancellationTokenSource();
            var procesador = new ProcesadorUpdates(async (m, ct) =>
            {
                if (m.TelegramUserId == 700) { entrada.TrySetResult(true); await liberar.Task.WaitAsync(ct); }
                return Array.Empty<RespuestaBot>();
            }, new ClienteTelegramSimulado(), _ => { });
            var servidor = new ServidorWebhook(new(procesador, secreto), $"http://127.0.0.1:{puerto}/telegram/", _ => throw new IOException("registro-fixture"));
            Task ejecucion = servidor.EjecutarAsync(cancelar.Token);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            async Task<HttpResponseMessage> Enviar(long id)
            {
                using var peticion = new HttpRequestMessage(HttpMethod.Post, $"http://127.0.0.1:{puerto}/telegram/");
                peticion.Headers.Add(ReceptorWebhook.EncabezadoSecreto, secreto);
                string cuerpo = System.Text.Json.JsonSerializer.Serialize(new
                {
                    update_id = id,
                    message = new { from = new { id }, chat = new { id, type = "private" }, text = "/estado" }
                });
                peticion.Content = new StringContent(cuerpo, Encoding.UTF8, "application/json");
                return await http.SendAsync(peticion);
            }
            Task<HttpResponseMessage>? lenta = null;
            try
            {
                lenta = Enviar(700);
                await entrada.Task.WaitAsync(TimeSpan.FromSeconds(5));
                using var rapida = await Enviar(701).WaitAsync(TimeSpan.FromSeconds(3));
                Assert.AreEqual(HttpStatusCode.OK, rapida.StatusCode);
                Assert.IsFalse(lenta.IsCompleted);
            }
            finally
            {
                liberar.TrySetResult(true);
                if (lenta is not null) { using var resultado = await lenta; }
                cancelar.Cancel();
                await ejecucion.WaitAsync(TimeSpan.FromSeconds(5));
            }
        }
    }
}
