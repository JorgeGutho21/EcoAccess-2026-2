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

        // Caso real del 10/11/2026: con una evaluación en marcha, la foto de un ciudadano fallaba al instante.
        [TestMethod]
        public async Task ElBotEsperaSuTurnoYLoObtieneCuandoElOtroProcesoLibera()
        {
            string carpeta = NuevaCarpeta(); Directory.CreateDirectory(carpeta);
            try
            {
                var ocupado = ExclusionInferenciaLocal.Adquirir(carpeta);
                var espera = ExclusionInferenciaLocal.AdquirirAsync(carpeta, TimeSpan.FromSeconds(5), prioritario: true, CancellationToken.None);
                await Task.Delay(300);
                Assert.IsFalse(espera.IsCompleted, "Mientras el otro proceso infiere, el bot espera en vez de fallar.");
                Assert.IsTrue(File.Exists(Path.Combine(carpeta, "vision-prioridad.aviso")), "El bot avisa que hay una persona esperando.");
                ocupado.Dispose();
                using var obtenido = await espera;
                Assert.IsTrue(obtenido.CanWrite);
                Assert.IsFalse(File.Exists(Path.Combine(carpeta, "vision-prioridad.aviso")), "Al obtener el turno retira el aviso.");
            }
            finally { BorrarCarpeta(carpeta); }
        }

        [TestMethod]
        public async Task UnaEvaluacionCedeElTurnoMientrasUnCiudadanoEspera()
        {
            string carpeta = NuevaCarpeta(); Directory.CreateDirectory(carpeta);
            try
            {
                File.WriteAllText(Path.Combine(carpeta, "vision-prioridad.aviso"), DateTime.UtcNow.ToString("O"));
                var evaluacion = ExclusionInferenciaLocal.AdquirirAsync(carpeta, TimeSpan.FromSeconds(5), prioritario: false, CancellationToken.None);
                await Task.Delay(300);
                Assert.IsFalse(evaluacion.IsCompleted, "Con el modelo libre, la evaluación igual espera al ciudadano.");
                File.Delete(Path.Combine(carpeta, "vision-prioridad.aviso"));
                using var obtenido = await evaluacion;
                Assert.IsTrue(obtenido.CanWrite);
            }
            finally { BorrarCarpeta(carpeta); }
        }

        [TestMethod]
        public async Task LaEsperaTieneLimiteYTerminaConElMismoMensaje()
        {
            string carpeta = NuevaCarpeta(); Directory.CreateDirectory(carpeta);
            try
            {
                using var ocupado = ExclusionInferenciaLocal.Adquirir(carpeta);
                var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                    ExclusionInferenciaLocal.AdquirirAsync(carpeta, TimeSpan.FromMilliseconds(400), prioritario: true, CancellationToken.None));
                StringAssert.Contains(error.Message, "selección manual");
            }
            finally { BorrarCarpeta(carpeta); }
        }

        private static void BorrarCarpeta(string carpeta)
        {
            foreach (string archivo in Directory.GetFiles(carpeta)) File.Delete(archivo);
            Directory.Delete(carpeta);
        }
    }
}
