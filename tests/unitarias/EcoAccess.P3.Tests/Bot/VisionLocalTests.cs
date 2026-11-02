using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Text.Json;
using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Tests.Logica.Dobles;
using Microsoft.Extensions.Configuration;
using SkiaSharp;

namespace EcoAccess.P3.Tests.Bot
{
    /// <summary>Selección y contrato del reconocimiento local con Qwen.</summary>
    [TestClass]
    public sealed class VisionLocalTests
    {
        private string _carpeta = null!;
        private static readonly Material[] Catalogo = new Material[] { new("PET", "Botella PET", CategoriaMaterial.Aprovechable, null) };
        [TestMethod]
        public void SinProveedorExplicito_SeleccionaModeloLocalVerificado()
        {
            using var elegido = SeleccionVisionLocal.Crear(new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["Vision:Ollama:ServidorSinNubeVerificado"] = "true" }).Build(), _ => { });
            Assert.IsNotNull(elegido);
            Assert.AreEqual("ollama-local:" + ClasificadorOllamaLocal.Modelo, elegido.Proveedor);
        }

        [TestMethod]
        [DataRow("GeminiGratis")]
        [DataRow("OpenAi")]
        public void ProveedorNoLocal_NoSeActiva(string proveedor)
        {
            var registros = new List<string>();
            using var elegido = SeleccionVisionLocal.Crear(new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["Vision:Proveedor"] = proveedor }).Build(), registros.Add);
            Assert.IsNull(elegido);
            Assert.HasCount(1, registros);
        }

        private static ImagenResiduo Imagen() => new(VisionSeguraFixture.Imagen(30, 30, SKEncodedImageFormat.Png), "image/png");

        [TestInitialize]
        public void Preparar()
        {
            string padre = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework");
            _carpeta = Path.Combine(padre, "pruebas-vision-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_carpeta);
        }
        [TestCleanup]
        public void RetirarSoloDiarioPropio()
        {
            string padre = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework");
            string absoluta = Path.GetFullPath(_carpeta);
            if (Path.GetDirectoryName(absoluta) != Compat.TrimEndingDirectorySeparator(Path.GetFullPath(padre)) || !Path.GetFileName(absoluta).StartsWith("pruebas-vision-", StringComparison.Ordinal))
                throw new InvalidOperationException("Ruta de fixture inesperada.");
            Directory.Delete(absoluta, true);
        }
        [TestMethod]
        public async Task OllamaSoloLlamaLoopbackModeloInstructSinFlujoNiPersistenciaDeSesion()
        {
            string estado = Path.Combine(_carpeta, "estado-local.json");
            string? cuerpo = null;
            using var c = new ClasificadorOllamaLocal(new HttpClient(new VisionSeguraFixture.Manejador((r, _) =>
            {
                Assert.AreEqual("http://127.0.0.1:11434/api/chat", r.RequestUri!.AbsoluteUri);
                cuerpo = r.Content!.ReadAsStringAsync().Result;
                return VisionSeguraFixture.Json(HttpStatusCode.OK, JsonSerializer.Serialize(new
                {
                    model = ClasificadorOllamaLocal.Modelo,
                    done = true,
                    done_reason = "stop",
                    message = new { content = "{\"objeto\":\"plastic_bag\",\"certeza\":\"high\"}" }
                }));
            })), archivoEstado: estado);
            PropuestaVision p = await c.ClasificarAsync(Imagen(), Catalogo);
            Assert.AreEqual(TipoPropuestaVision.MaterialIdentificado, p.Tipo);
            Assert.AreEqual("BOLSA_PLASTICA", p.CodigoMaterial);
            Assert.AreEqual(ObjetoVisible.BolsaPlastica, p.Objeto);
            // La foto viaja en base64 dentro de images y el modelo queda cargado un rato para la siguiente.
            using var json = JsonDocument.Parse(cuerpo!);
            string imagen = json.RootElement.GetProperty("messages")[1].GetProperty("images")[0].GetString()!;
            Assert.IsGreaterThan(0, Convert.FromBase64String(imagen).Length);
            Assert.AreEqual("15m", json.RootElement.GetProperty("keep_alive").GetString());
            Assert.IsTrue(EstadoVisionLocal.Leer(estado)!.Exitosa);
        }

        [TestMethod]
        [DataRow("{\"tipo\":\"MaterialIdentificado\",\"codigo\":\"PET\",\"confianza\":0.9}")]
        [DataRow("{\"tipo\":\"MaterialIdentificado\",\"codigo\":\"PET\",\"confianza\":0.9,\"objeto\":\"Nave espacial\"}")]
        [DataRow("no es json")]
        public async Task OllamaRespuestaSinObjetoValidoNoSeConvierteEnPropuesta(string contenido)
        {
            string estado = Path.Combine(_carpeta, "estado-local.json");
            using var c = new ClasificadorOllamaLocal(new HttpClient(new VisionSeguraFixture.Manejador((_, _) =>
                VisionSeguraFixture.Json(HttpStatusCode.OK, JsonSerializer.Serialize(new { model = ClasificadorOllamaLocal.Modelo, done = true, done_reason = "stop", message = new { content = contenido } })))), archivoEstado: estado);
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => c.ClasificarAsync(Imagen(), Catalogo));
            Assert.IsFalse(EstadoVisionLocal.Leer(estado)!.Exitosa);
        }
    }
}
