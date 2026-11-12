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
                string pedido = r.Content!.ReadAsStringAsync().Result;
                if (pedido.Contains(VocabularioVision.PreguntaSujetoVivo)) return Respuesta("{\"respuesta\":\"no\"}");
                if (System.Linq.Enumerable.Any(VocabularioVision.PreguntasContenido.Values, v => pedido.Contains(v.Pregunta))) return Respuesta("{\"respuesta\":\"no\"}");
                cuerpo = pedido;
                return Respuesta("{\"objeto\":\"plastic_bag\",\"certeza\":\"high\"}");
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

        private static HttpResponseMessage Respuesta(string contenido) => VisionSeguraFixture.Json(HttpStatusCode.OK,
            JsonSerializer.Serialize(new { model = ClasificadorOllamaLocal.Modelo, done = true, done_reason = "stop", message = new { content = contenido } }));

        /// <summary>
        /// Modelo simulado que contesta según la pregunta: nombre libre, lectura por material, «¿hay personas?» (computadores)
        /// y «¿el sujeto es una persona o un animal vivo?». Devuelve también cuántas veces se hizo la última.
        /// </summary>
        private ClasificadorOllamaLocal Modelo(string nombre, string lectura, string sujetoVivo, int[] preguntasSujeto) =>
            new(new HttpClient(new VisionSeguraFixture.Manejador((r, _) =>
            {
                string pedido = r.Content!.ReadAsStringAsync().Result;
                if (pedido.Contains(VocabularioVision.PreguntaSujetoVivo)) { preguntasSujeto[0]++; return Respuesta(sujetoVivo); }
                if (pedido.Contains(VocabularioVision.PreguntaPersonas)) return Respuesta("{\"respuesta\":\"no\"}");
                if (System.Linq.Enumerable.Any(VocabularioVision.PreguntasContenido.Values, v => pedido.Contains(v.Pregunta))) return Respuesta("{\"respuesta\":\"no\"}");
                return Respuesta(pedido.Contains(VocabularioVision.PreguntaNombre) ? nombre : lectura);
            })), archivoEstado: Path.Combine(_carpeta, "estado-local.json"));

        /// <summary>
        /// Regresión de la ronda 6 (persona-03): el nombre y la lectura dicen cartón, pero el sujeto es una persona o un
        /// animal. La propuesta pasa a «personas», sin material ni confianza: no ofrece depositar ni abre. Una respuesta
        /// mal formada o ausente cuenta como sujeto vivo (fallo seguro).
        /// </summary>
        [TestMethod]
        [DataRow("cardboard box", "{\"objeto\":\"cardboard\",\"certeza\":\"high\"}", "{\"respuesta\":\"yes\"}", DisplayName = "persona leída como cartón")]
        [DataRow("plastic bottle", "{\"objeto\":\"pet_bottle\",\"certeza\":\"high\"}", "{\"respuesta\":\"yes\"}", DisplayName = "animal con una botella")]
        [DataRow("drink can", "{\"objeto\":\"can\",\"certeza\":\"high\"}", "no es json", DisplayName = "respuesta mal formada")]
        [DataRow("paper", "{\"objeto\":\"paper\",\"certeza\":\"high\"}", "{\"otra\":\"no\"}", DisplayName = "respuesta sin campo")]
        public async Task SujetoVivo_AnulaElMaterialYNoPuedeAbrir(string nombre, string lectura, string sujeto)
        {
            var preguntas = new int[1];
            using var c = Modelo(nombre, lectura, sujeto, preguntas);

            PropuestaVision p = await c.ClasificarAsync(Imagen(), Catalogo);

            Assert.AreEqual(1, preguntas[0], "La comprobación del sujeto se hace una vez antes de cualquier oferta.");
            Assert.AreEqual("people", p.ClaveObjeto);
            Assert.AreEqual(TipoPropuestaVision.SinResiduo, p.Tipo);
            Assert.IsNull(p.CodigoMaterial);
            Assert.IsNull(p.ConfianzaDeclarada);
        }

        [TestMethod]
        public async Task ResiduoSinSujetoVivo_ConservaElMaterial()
        {
            var preguntas = new int[1];
            using var c = Modelo("plastic water bottle", "{\"objeto\":\"pet_bottle\",\"certeza\":\"high\"}", "{\"respuesta\":\"no\"}", preguntas);

            PropuestaVision p = await c.ClasificarAsync(Imagen(), Catalogo);

            Assert.AreEqual(1, preguntas[0]);
            Assert.AreEqual("plastic_bottle", p.ClaveObjeto);
            Assert.AreEqual("PET", p.CodigoMaterial);
        }

        /// <summary>Lo que ya solo orienta (un perro, una persona, comida servida) no necesita la pregunta adicional.</summary>
        [TestMethod]
        [DataRow("dog")]
        [DataRow("woman smiling")]
        [DataRow("plate of food")]
        public async Task LoQueNoPuedeAbrir_NoHaceLaPreguntaDelSujeto(string nombre)
        {
            var preguntas = new int[1];
            using var c = Modelo(nombre, "{\"objeto\":\"no_waste\",\"certeza\":\"high\"}", "{\"respuesta\":\"yes\"}", preguntas);

            PropuestaVision p = await c.ClasificarAsync(Imagen(), Catalogo);

            Assert.AreEqual(0, preguntas[0]);
            Assert.IsNull(p.CodigoMaterial);
        }

        /// <summary>El motor externo responde todo en una llamada: sin «sujeto_vivo» = false explícito, no hay material.</summary>
        [TestMethod]
        [DataRow(true, true)]
        [DataRow(null, true)]
        [DataRow(false, false)]
        public void MotorExterno_SujetoVivoOAusente_NoProponeMaterial(bool? sujetoVivo, bool esperaPersona)
        {
            var campos = new Dictionary<string, object> { ["nombre"] = "cardboard box", ["hay_personas"] = false, ["objeto"] = "cardboard", ["certeza"] = "high", ["estado"] = "clean", ["requiere_aclaracion"] = false };
            if (sujetoVivo is { } valor) campos["sujeto_vivo"] = valor;

            PropuestaVision p = VocabularioVision.Leer(JsonSerializer.Serialize(campos));

            Assert.AreEqual(esperaPersona ? "people" : "cardboard", p.ClaveObjeto);
            Assert.AreEqual(esperaPersona ? null : "CARTON", p.CodigoMaterial);
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
