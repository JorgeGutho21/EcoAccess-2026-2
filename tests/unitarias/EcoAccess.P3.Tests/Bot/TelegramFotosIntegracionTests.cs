using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Text;
using System.Text.Json;
using EcoAccess.P3.Bot;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioConversacionTelegramTests
    {
        private sealed class TelegramFotoTransporte : HttpMessageHandler
        {
            public int Descargas;
            public List<string> Respuestas { get; } = new List<string>();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                if (request.Method == HttpMethod.Get)
                {
                    Interlocked.Increment(ref Descargas);
                    return new(HttpStatusCode.OK) { Content = new ByteArrayContent(FotoDelDoble) };
                }
                if (request.RequestUri!.AbsolutePath.EndsWith("sendMessage", StringComparison.Ordinal))
                {
                    string cuerpo = await request.Content!.ReadAsStringAsync(ct);
                    lock (Respuestas) Respuestas.Add(cuerpo);
                }
                string json = request.RequestUri!.AbsolutePath.EndsWith("getFile", StringComparison.Ordinal)
                    ? @"{""ok"":true,""result"":{""file_path"":""photos/foto.png""}}"
                    : @"{""ok"":true,""result"":true}";
                return new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TelegramFoto_TransporteConsentimientoYConfirmacionesConcurrentes_DescarganUnaVez(bool mismoUpdate)
        {
            var transporte = new TelegramFotoTransporte();
            using var cliente = new ClienteTelegramHttp("123456:TOKEN-SINTETICO-SIN-BOT", new HttpClient(transporte), propio: true);
            var servicio = ConVision(TipoPropuestaVision.MaterialIdentificado, cliente.DescargarFotoAsync, consentimiento: true);
            await PrepararConAsync(servicio);
            var procesador = new ProcesadorUpdates(servicio.ProcesarAsync, cliente, _ => { });

            // Entra el mismo JSON que enviaría Telegram, no un DTO de foto fabricado directamente.
            long update = Interlocked.Increment(ref _update);
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                update_id = update,
                message = new
                {
                    from = new { id = Vinculado },
                    chat = new { id = Vinculado, type = "private" },
                    photo = new[] { new { file_id = "foto-real-del-transporte-ficticio", width = 30, height = 30 } }
                },
            }));
            await procesador.ProcesarAsync(TraductorUpdates.Traducir(json.RootElement), CancellationToken.None);
            Assert.AreEqual(0, transporte.Descargas, "La foto no sale de Telegram antes del consentimiento.");
            using var aviso = JsonDocument.Parse(transporte.Respuestas.Single());
            string consentimiento = aviso.RootElement.GetProperty("reply_markup").GetProperty("inline_keyboard")[0][0]
                .GetProperty("callback_data").GetString()!;
            StringAssert.StartsWith(consentimiento, ServicioConversacionTelegram.PrefijoConsentirFoto);

            long id = Interlocked.Increment(ref _update);
            var primero = new UpdateRecibido(id, new MensajeEntrante(id, Vinculado, Vinculado, true, null, consentimiento), "cb-1");
            long segundoId = mismoUpdate ? id : Interlocked.Increment(ref _update);
            var segundo = new UpdateRecibido(segundoId,
                new MensajeEntrante(segundoId, Vinculado, Vinculado, true, null, consentimiento), "cb-2");
            await Task.WhenAll(procesador.ProcesarAsync(primero, CancellationToken.None), procesador.ProcesarAsync(segundo, CancellationToken.None));

            Assert.AreEqual(1, transporte.Descargas);
            Assert.IsTrue(transporte.Respuestas.Any(r => r.Contains(ServicioConversacionTelegram.ConfirmarSi, StringComparison.Ordinal)));
            Assert.IsEmpty(_interacciones.Ordenes, "Clasificar no autoriza apertura.");
            Assert.IsEmpty(_interacciones.Depositos);
        }
    }
}
