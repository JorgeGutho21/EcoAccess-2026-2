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
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class ClienteTelegramHttpTests
    {
        // Token de forma válida inventado para la prueba; no pertenece a ningún bot.
        private const string Token = "123456:TOKEN-DE-PRUEBA-NO-REAL";

        private sealed class ManejadorFalso : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, string, HttpResponseMessage> _responder;

            public ManejadorFalso(Func<HttpRequestMessage, string, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            public List<(Uri Ruta, string Cuerpo)> Peticiones { get; } = new List<(Uri Ruta, string Cuerpo)>();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string cuerpo = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
                Peticiones.Add((request.RequestUri!, cuerpo));
                return _responder(request, cuerpo);
            }
        }

        private static HttpResponseMessage Json(HttpStatusCode estado, string json) =>
            new(estado) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        private static (ClienteTelegramHttp Cliente, ManejadorFalso Manejador) Crear(Func<HttpRequestMessage, string, HttpResponseMessage> responder)
        {
            var manejador = new ManejadorFalso(responder);
            return (new ClienteTelegramHttp(Token, new HttpClient(manejador), propio: true), manejador);
        }

        [TestMethod]
        public async Task ObtenerUpdates_EnviaOffsetYTimeoutYTraduce()
        {
            (ClienteTelegramHttp cliente, ManejadorFalso manejador) = Crear((_, _) => Json(HttpStatusCode.OK,
                @"{""ok"":true,""result"":[{""update_id"":9,""message"":{""from"":{""id"":7},""chat"":{""id"":7,""type"":""private""},""text"":""hola""}}]}"));
            using (cliente)
            {
                IReadOnlyList<UpdateRecibido> lote = await cliente.ObtenerUpdatesAsync(9, TimeSpan.FromSeconds(30), CancellationToken.None);

                Assert.HasCount(1, lote);
                Assert.AreEqual("hola", lote[0].Mensaje?.Texto);
            }

            (Uri ruta, string cuerpo) = manejador.Peticiones.Single();
            Assert.AreEqual("https://api.telegram.org/bot" + Token + "/getUpdates", ruta.ToString());
            using JsonDocument enviado = JsonDocument.Parse(cuerpo);
            Assert.AreEqual(9, enviado.RootElement.GetProperty("offset").GetInt64());
            Assert.AreEqual(30, enviado.RootElement.GetProperty("timeout").GetInt32());
            Assert.AreEqual(2, enviado.RootElement.GetProperty("allowed_updates").GetArrayLength());
        }

        [TestMethod]
        public async Task Enviar_ConBotones_ArmaTecladoUnBotonPorFila()
        {
            (ClienteTelegramHttp cliente, ManejadorFalso manejador) = Crear((_, _) => Json(HttpStatusCode.OK, @"{""ok"":true,""result"":{}}"));
            using (cliente)
            {
                await cliente.EnviarAsync(
                    new RespuestaBot(55, "Elige", new BotonBot[] { new BotonBot("Plástico", "mat:1"), new BotonBot("Vidrio", "mat:2") }),
                    CancellationToken.None);
            }

            using JsonDocument enviado = JsonDocument.Parse(manejador.Peticiones.Single().Cuerpo);
            JsonElement raiz = enviado.RootElement;
            Assert.AreEqual(55, raiz.GetProperty("chat_id").GetInt64());
            Assert.AreEqual("Elige", raiz.GetProperty("text").GetString());
            JsonElement filas = raiz.GetProperty("reply_markup").GetProperty("inline_keyboard");
            Assert.AreEqual(2, filas.GetArrayLength());
            Assert.AreEqual("mat:2", filas[1][0].GetProperty("callback_data").GetString());
            Assert.AreEqual("Vidrio", filas[1][0].GetProperty("text").GetString());
        }

        [TestMethod]
        public async Task Enviar_SinBotones_NoIncluyeTeclado()
        {
            (ClienteTelegramHttp cliente, ManejadorFalso manejador) = Crear((_, _) => Json(HttpStatusCode.OK, @"{""ok"":true,""result"":{}}"));
            using (cliente)
            {
                await cliente.EnviarAsync(new RespuestaBot(55, "Hola"), CancellationToken.None);
            }

            using JsonDocument enviado = JsonDocument.Parse(manejador.Peticiones.Single().Cuerpo);
            Assert.IsFalse(enviado.RootElement.TryGetProperty("reply_markup", out _));
        }

        [TestMethod]
        public async Task ResponderBoton_EnviaElIdDelCallback()
        {
            (ClienteTelegramHttp cliente, ManejadorFalso manejador) = Crear((_, _) => Json(HttpStatusCode.OK, @"{""ok"":true,""result"":true}"));
            using (cliente)
            {
                await cliente.ResponderBotonAsync("cb-3", CancellationToken.None);
            }

            (Uri ruta, string cuerpo) = manejador.Peticiones.Single();
            StringAssert.EndsWith(ruta.AbsolutePath, "/answerCallbackQuery");
            StringAssert.Contains(cuerpo, "\"callback_query_id\":\"cb-3\"");
        }

        [TestMethod]
        public async Task Respuesta429_TraeRetryAfter()
        {
            (ClienteTelegramHttp cliente, _) = Crear((_, _) => Json((HttpStatusCode)429,
                @"{""ok"":false,""error_code"":429,""description"":""Too Many Requests"",""parameters"":{""retry_after"":7}}"));
            using (cliente)
            {
                ErrorTelegramException error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(
                    () => cliente.EnviarAsync(new RespuestaBot(1, "x"), CancellationToken.None));

                Assert.AreEqual(TimeSpan.FromSeconds(7), error.EsperaSugerida);
                Assert.IsFalse(error.EsConflicto);
            }
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Unauthorized)]
        [DataRow(HttpStatusCode.NotFound)]
        public async Task Respuesta401o404_EsTokenRechazado(HttpStatusCode estado)
        {
            (ClienteTelegramHttp cliente, _) = Crear((_, _) => Json(estado, @"{""ok"":false,""description"":""Unauthorized""}"));
            using (cliente)
            {
                ErrorTelegramException error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(
                    () => cliente.ObtenerUpdatesAsync(0, TimeSpan.FromSeconds(1), CancellationToken.None));

                Assert.IsTrue(error.EsTokenRechazado);
                Assert.IsFalse(error.EsConflicto);
                Assert.DoesNotContain(Token, error.Message);
            }
        }

        [TestMethod]
        [DataRow("123456:ABCDEFGHIJKLMNOPQRSTUV_-0123", true)]
        [DataRow(Token, true)]
        [DataRow(null, false)]
        [DataRow("", false)]
        [DataRow("1234:ABCDEFGHIJKLMNOPQRSTUV", false)]
        [DataRow("123456:CORTO", false)]
        [DataRow("abcdef:ABCDEFGHIJKLMNOPQRSTUV", false)]
        [DataRow("123456:ABCDEFGHIJKLMNOPQRSTUV?x=1", false)]
        [DataRow("123456:ABCDEFGHIJKLMNOPQRSTUV#x", false)]
        [DataRow("123456:ABCDEFGHIJ/../KLMNOPQRSTUV", false)]
        [DataRow("123456:ABCDEFGHIJKLMNOPQRSTUV ", false)]
        public void TokenValido_SoloAceptaLaFormaDeBotFather(string? token, bool esperado)
        {
            Assert.AreEqual(esperado, ClienteTelegramHttp.TokenValido(token));
        }

        [TestMethod]
        public void Constructor_TokenMalFormado_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new ClienteTelegramHttp("123456:ABCDEFGHIJKLMNOPQRSTUV?x=1"));
        }

        [TestMethod]
        public async Task Respuesta409_EsConflicto()
        {
            (ClienteTelegramHttp cliente, _) = Crear((_, _) => Json(HttpStatusCode.Conflict,
                @"{""ok"":false,""error_code"":409,""description"":""Conflict: can't use getUpdates method while webhook is active""}"));
            using (cliente)
            {
                ErrorTelegramException error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(
                    () => cliente.ObtenerUpdatesAsync(0, TimeSpan.FromSeconds(1), CancellationToken.None));

                Assert.IsTrue(error.EsConflicto);
                Assert.IsNull(error.EsperaSugerida);
            }
        }

        [TestMethod]
        public async Task OkFalseConEstado200_EsFallo()
        {
            (ClienteTelegramHttp cliente, _) = Crear((_, _) => Json(HttpStatusCode.OK, @"{""ok"":false}"));
            using (cliente)
            {
                await Assert.ThrowsExactlyAsync<ErrorTelegramException>(
                    () => cliente.EnviarAsync(new RespuestaBot(1, "x"), CancellationToken.None));
            }
        }

        [TestMethod]
        public async Task CuerpoIlegible_EsErrorTelegram()
        {
            (ClienteTelegramHttp cliente, _) = Crear((_, _) => new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("<html>502</html>"),
            });
            using (cliente)
            {
                await Assert.ThrowsExactlyAsync<ErrorTelegramException>(
                    () => cliente.EnviarAsync(new RespuestaBot(1, "x"), CancellationToken.None));
            }
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("https://webhook.example/privado?secreto=no-revelar")]
        public async Task DiagnosticoLeeIdentidadYWebhookSinConsumirUpdatesNiExponerUrl(string webhook)
        {
            var (cliente, manejador) = Crear((r, _) => Json(HttpStatusCode.OK, r.RequestUri!.AbsolutePath.EndsWith("getMe", StringComparison.Ordinal)
                ? @"{""ok"":true,""result"":{""id"":123456,""is_bot"":true,""username"":""EcoPruebaBot""}}"
                : JsonSerializer.Serialize(new { ok = true, result = new { url = webhook, pending_update_count = 2 } })));
            using (cliente)
            {
                var estado = await cliente.ComprobarConfiguracionAsync("ecopruebabot");
                Assert.AreEqual(123456L, estado.IdBot); Assert.AreEqual(webhook.Length > 0, estado.WebhookActivo); Assert.AreEqual(2, estado.ActualizacionesPendientes);
                CollectionAssert.AreEqual(new[] { "getMe", "getWebhookInfo" }, manejador.Peticiones.Select(x => x.Ruta.Segments.Last()).ToArray());
                Assert.DoesNotContain("no-revelar", estado.ToString());
            }
        }

        [TestMethod]
        [DataRow("{\"id\":\"abc\",\"is_bot\":true,\"username\":\"EcoPruebaBot\"}")]
        [DataRow("{\"id\":123456,\"is_bot\":false,\"username\":\"EcoPruebaBot\"}")]
        [DataRow("{\"id\":123456,\"is_bot\":true,\"username\":\"OtroBot\"}")]
        [DataRow("{}")]
        public async Task IdentidadDistintaOMalformadaImpideConsultarWebhook(string resultado)
        {
            var (cliente, manejador) = Crear((_, _) => Json(HttpStatusCode.OK, "{\"ok\":true,\"result\":" + resultado + "}"));
            using (cliente)
            {
                await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.ComprobarConfiguracionAsync("EcoPruebaBot"));
                Assert.HasCount(1, manejador.Peticiones);
            }
        }

        [TestMethod]
        public async Task EstadoWebhookInvalidoSeRechazaSinRevelarContenido()
        {
            var (cliente, _) = Crear((r, _) => Json(HttpStatusCode.OK, r.RequestUri!.AbsolutePath.EndsWith("getMe", StringComparison.Ordinal)
                ? @"{""ok"":true,""result"":{""id"":123456,""is_bot"":true,""username"":""EcoPruebaBot""}}"
                : @"{""ok"":true,""result"":{""url"":""https://ejemplo.invalid/secreto"",""pending_update_count"":""inválido""}}"));
            using (cliente)
            {
                var error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.ComprobarConfiguracionAsync("EcoPruebaBot"));
                Assert.DoesNotContain("secreto", error.Message); Assert.DoesNotContain(Token, error.Message);
            }
        }

        [TestMethod]
        public async Task FalloDeRed_NoRevelaElToken()
        {
            (ClienteTelegramHttp cliente, _) = Crear((peticion, _) =>
                throw new HttpRequestException($"No se pudo conectar a {peticion.RequestUri}"));
            using (cliente)
            {
                ErrorTelegramException error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(
                    () => cliente.ObtenerUpdatesAsync(0, TimeSpan.FromSeconds(1), CancellationToken.None));

                Assert.DoesNotContain(Token, error.Message);
                Assert.DoesNotContain("api.telegram.org", error.Message);
                Assert.IsNull(error.InnerException);
            }
        }

        [TestMethod]
        public async Task CancelacionPropia_SePropaga()
        {
            using var cancelacion = new CancellationTokenSource();
            (ClienteTelegramHttp cliente, _) = Crear((_, _) =>
            {
                cancelacion.Cancel();
                throw new TaskCanceledException();
            });
            using (cliente)
            {
                await Assert.ThrowsAsync<OperationCanceledException>(
                    () => cliente.ObtenerUpdatesAsync(0, TimeSpan.FromSeconds(1), cancelacion.Token));
            }
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("123:abc/def")]
        [DataRow("123 abc")]
        [DataRow("123456:TOKEN-DE-PRUEBA-NO-REAL\n")]
        public void TokenMalFormado_SeRechazaSinCitarlo(string token)
        {
            ArgumentException error = Assert.ThrowsExactly<ArgumentException>(() => new ClienteTelegramHttp(token));

            if (token.Trim().Length > 0)
            {
                Assert.DoesNotContain(token, error.Message);
            }
        }
    }
}
