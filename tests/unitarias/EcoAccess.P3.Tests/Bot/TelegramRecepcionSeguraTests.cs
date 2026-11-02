using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Text;
using System.Text.Json;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class TelegramRecepcionSeguraTests
    {
        private const string Token = "123456:TOKEN-SINTETICO-SIN-BOT";

        private sealed class Transporte : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public Transporte(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            public List<Uri> Rutas { get; } = new List<Uri>();
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                Rutas.Add(request.RequestUri!);
                return Task.FromResult(_responder(request));
            }
        }

        private static HttpResponseMessage Json(string cuerpo) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(cuerpo, Encoding.UTF8, "application/json"),
        };

        [TestMethod]
        [DataRow("null")]
        [DataRow("[]")]
        [DataRow("42")]
        [DataRow("\"texto\"")]
        public void MensajeOBotonMalformado_NoBloqueaElRestoDelLote(string valor)
        {
            using var doc = JsonDocument.Parse(@"[{""update_id"":10,""message"":VALOR},
 {""update_id"":11,""callback_query"":VALOR},
 {""update_id"":12,""message"":{""from"":{""id"":7},""chat"":{""id"":7,""type"":""private""},""text"":""/ayuda""}}]".Replace("VALOR", valor, StringComparison.Ordinal));
            var lote = TraductorUpdates.TraducirLista(doc.RootElement);
            Assert.HasCount(3, lote);
            Assert.IsNull(lote[0].Mensaje);
            Assert.IsNull(lote[1].Mensaje);
            Assert.AreEqual("/ayuda", lote[2].Mensaje?.Texto);
        }

        [TestMethod]
        public void IdFueraDeRango_NoPuedeDesbordarElOffset()
        {
            using var doc = JsonDocument.Parse(@"[{""update_id"":-1},{""update_id"":9223372036854775807},{""update_id"":0}]");
            var lote = TraductorUpdates.TraducirLista(doc.RootElement);
            Assert.HasCount(1, lote);
            Assert.AreEqual(0L, lote[0].UpdateId);
        }

        [TestMethod]
        public async Task Foto_SeleccionaVarianteValidaYDescargaSoloDesdeTelegram()
        {
            using var doc = JsonDocument.Parse(@"{""update_id"":1,""message"":{""from"":{""id"":7},""chat"":{""id"":7,""type"":""private""},""photo"":[
{""file_id"":""pequena"",""width"":10,""height"":10},
{""file_id"":""demasiado-grande"",""width"":9000,""height"":9000,""file_size"":99999999},
{""file_id"":""elegida"",""width"":100,""height"":100,""file_size"":3}]}}");
            var update = TraductorUpdates.Traducir(doc.RootElement);
            Assert.AreEqual("elegida", update.Mensaje?.ArchivoFoto);
            var transporte = new Transporte(r => r.RequestUri!.AbsolutePath.EndsWith("getFile", StringComparison.Ordinal)
                ? Json(@"{""ok"":true,""result"":{""file_path"":""photos/residuo.jpg"",""file_size"":3}}")
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[] { 1, 2, 3 }) });
            using var cliente = new ClienteTelegramHttp(Token, new HttpClient(transporte), propio: true);
            var imagen = await cliente.DescargarFotoAsync(update.Mensaje!.ArchivoFoto!);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, imagen.Contenido);
            Assert.HasCount(2, transporte.Rutas);
            Assert.AreEqual("https://api.telegram.org/file/bot" + Token + "/photos/residuo.jpg", transporte.Rutas[1].AbsoluteUri);
        }

        [TestMethod]
        [DataRow("https://ajeno.example/foto.jpg")]
        [DataRow("../foto.jpg")]
        [DataRow("photos/%2e%2e/secreto")]
        [DataRow("photos/foto.jpg?token=x")]
        [DataRow("/foto.jpg")]
        [DataRow("photos//foto.jpg")]
        public async Task RutaDeArchivoManipulada_NoIniciaDescarga(string ruta)
        {
            var transporte = new Transporte(_ => Json(JsonSerializer.Serialize(new { ok = true, result = new { file_path = ruta } })));
            using var cliente = new ClienteTelegramHttp(Token, new HttpClient(transporte), propio: true);
            var error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.DescargarFotoAsync("foto"));
            Assert.HasCount(1, transporte.Rutas);
            Assert.DoesNotContain(Token, error.Message);
            Assert.DoesNotContain(ruta, error.Message);
        }

        [TestMethod]
        public async Task TamanoDeclaradoExcesivo_NoIniciaDescarga()
        {
            var transporte = new Transporte(_ => Json(@"{""ok"":true,""result"":{""file_path"":""photos/foto.jpg"",""file_size"":99999999}}"));
            using var cliente = new ClienteTelegramHttp(Token, new HttpClient(transporte), propio: true);
            await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.DescargarFotoAsync("foto"));
            Assert.HasCount(1, transporte.Rutas);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Unauthorized)]
        [DataRow(HttpStatusCode.NotFound)]
        [DataRow(HttpStatusCode.Conflict)]
        public async Task ErrorPermanenteConHtml_ConservaLaDecisionDeDetenerPolling(HttpStatusCode estado)
        {
            var transporte = new Transporte(_ => new HttpResponseMessage(estado) { Content = new StringContent("<html>" + Token + "</html>") });
            using var cliente = new ClienteTelegramHttp(Token, new HttpClient(transporte), propio: true);
            var error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.ObtenerUpdatesAsync(0, TimeSpan.FromSeconds(1), CancellationToken.None));
            Assert.AreEqual(estado == HttpStatusCode.Conflict, error.EsConflicto);
            Assert.AreEqual(estado != HttpStatusCode.Conflict, error.EsTokenRechazado);
            Assert.DoesNotContain(Token, error.ToString());
            Assert.HasCount(1, transporte.Rutas);
        }

        [TestMethod]
        public async Task RespuestaJsonExcesiva_SeRechazaSinFiltrarContenido()
        {
            var transporte = new Transporte(_ => Json("{\"secreto\":\"" + new string('x', 1024 * 1024) + Token + "\"}"));
            using var cliente = new ClienteTelegramHttp(Token, new HttpClient(transporte), propio: true);
            var error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.ObtenerUpdatesAsync(0, TimeSpan.FromSeconds(1), CancellationToken.None));
            Assert.DoesNotContain(Token, error.ToString());
            Assert.IsLessThan(300, error.Message.Length);
        }
    }
}
