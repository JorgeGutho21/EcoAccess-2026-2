using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;
using SkiaSharp;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class GraficaPersonalTests
    {
        private static EstadisticasPersonales Datos()
        {
            DateTime hasta = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            return new(new(hasta.AddDays(-30), hasta, IdUsuario: 3), new(hasta.AddDays(-60), hasta.AddDays(-30), IdUsuario: 3),
                RepositorioEstadisticasEnMemoria.ResumenFijo, RepositorioEstadisticasEnMemoria.ResumenFijo, Array.Empty<FilaAgrupada>(),
                new FilaAgrupada[] { new(null, new DateOnly(2026, 10, 31), "2026-10-31", 2, 1, 0, 0, 0, 0) });
        }
        [TestMethod]
        public void GraficaLocalGeneraPngAcotadoSinConsultaDeDatos()
        {
            byte[] png = GraficaEstadisticasPersonal.Crear(Datos());
            Assert.IsTrue(png.Length is > 1000 and < 200000);
            using var imagen = SKBitmap.Decode(png);
            Assert.AreEqual(1000, imagen.Width); Assert.AreEqual(620, imagen.Height);
        }
        [TestMethod]
        public async Task ClienteEnviaTextoYFotoSoloAlChatAutorizado_YConservaErrores429()
        {
            var rutas = new List<string>(); int intentos = 0;
            using var http = new HttpClient(new VisionSeguraFixture.Manejador((r, _) =>
            {
                rutas.Add(r.RequestUri!.AbsolutePath.Split('/').Last());
                if (r.RequestUri.AbsolutePath.EndsWith("sendPhoto", StringComparison.Ordinal))
                {
                    Assert.IsInstanceOfType<MultipartFormDataContent>(r.Content);
                    intentos++;
                    if (intentos == 2) return VisionSeguraFixture.Json((HttpStatusCode)429, "{\"ok\":false,\"parameters\":{\"retry_after\":7}}");
                }
                return VisionSeguraFixture.Json(HttpStatusCode.OK, "{\"ok\":true,\"result\":{}}");
            }));
            using var cliente = new ClienteTelegramHttp("123456:clave-sintetica-sin-cuenta-real", http, false);
            var respuesta = new RespuestaBot(700000001, "Estadísticas privadas") { Estadisticas = Datos() };
            await cliente.EnviarAsync(respuesta, default);
            CollectionAssert.AreEqual(new[] { "sendMessage", "sendPhoto" }, rutas);
            var error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.EnviarAsync(respuesta, default));
            Assert.AreEqual(TimeSpan.FromSeconds(7), error.EsperaSugerida);
            Assert.DoesNotContain("clave-sintetica", error.Message);
        }
    }
}
