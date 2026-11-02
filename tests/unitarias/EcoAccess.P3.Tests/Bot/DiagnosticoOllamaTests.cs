using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class DiagnosticoOllamaTests
    {
        private sealed class Transporte : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public Transporte(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            { ct.ThrowIfCancellationRequested(); return Task.FromResult(_responder(request)); }
        }
        private static HttpResponseMessage Json(string texto) => new(HttpStatusCode.OK) { Content = new StringContent(texto) };
        private static string NuevaCarpeta() => Path.Combine(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework"), "prueba-ollama-" + Guid.NewGuid().ToString("N"));

        [TestMethod]
        public async Task SaludSoloGetLocalNoGeneraNiRevelaNombresDeOtrosModelos()
        {
            int llamadas = 0;
            using var diagnostico = new DiagnosticoOllamaLocal(new HttpClient(new Transporte(r =>
            {
                llamadas++; Assert.AreEqual(HttpMethod.Get, r.Method); Assert.AreEqual("127.0.0.1", r.RequestUri!.Host);
                return Json(r.RequestUri.AbsolutePath switch
                {
                    "/api/version" => "{\"version\":\"0.40.13\"}",
                    "/api/tags" => "{\"models\":[{\"name\":\"lfm3-vl:latest\"}]}",
                    _ => "{\"models\":[{\"name\":\"nombre-privado-no-publicar\"}]}"
                });
            })));
            var r = await diagnostico.ComprobarAsync(); Assert.IsTrue(r.Disponible); Assert.IsTrue(r.ModeloInstalado);
            Assert.AreEqual(1, r.OtrosModelosResidentes); Assert.IsFalse(r.ListoParaInferencia); Assert.AreEqual(3, llamadas);
            Assert.DoesNotContain("nombre-privado", System.Text.Json.JsonSerializer.Serialize(r));
        }

        [TestMethod]
        [DataRow("{\"version\":\"texto privado\"}")]
        [DataRow("respuesta privada no JSON")]
        public async Task SaludInvalidaDevuelveAlternativaManualSinRespuestaLibre(string cuerpo)
        {
            using var d = new DiagnosticoOllamaLocal(new HttpClient(new Transporte(_ => Json(cuerpo))));
            var r = await d.ComprobarAsync(); Assert.IsFalse(r.Disponible);
            Assert.DoesNotContain("privad", System.Text.Json.JsonSerializer.Serialize(r));
            StringAssert.Contains(r.Mensaje, "manualmente");
        }

        [TestMethod]
        public void DosInstanciasCompartenExclusionYLiberanAlTerminar()
        {
            string carpeta = NuevaCarpeta(); Directory.CreateDirectory(carpeta);
            try
            {
                using (var primera = ExclusionInferenciaLocal.Adquirir(carpeta))
                    Assert.ThrowsExactly<InvalidOperationException>(() => ExclusionInferenciaLocal.Adquirir(carpeta));
                using var siguiente = ExclusionInferenciaLocal.Adquirir(carpeta);
                Assert.IsTrue(siguiente.CanWrite);
            }
            finally { File.Delete(Path.Combine(carpeta, "vision-inferencia.lock")); Directory.Delete(carpeta); }
        }
    }
}
