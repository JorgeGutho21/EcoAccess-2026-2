using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace EcoAccess.P3.Tests.Bot
{

    /// <summary>
    /// Cliente de Groq contra un HTTP falso: prueban el contrato y los fallos, no el servicio real ni la calidad del modelo.
    /// </summary>
    [TestClass]
    public sealed class VisionGroqTests
    {
        private const string Clave = "clave-ficticia-de-prueba";
        private const string PropuestaPet = "{\"objeto\":\"pet_bottle\",\"certeza\":\"high\",\"estado\":\"clean\",\"requiere_aclaracion\":false}";
        private static readonly Material[] Catalogo = { new("PET", "Botella PET", CategoriaMaterial.Aprovechable, null) };

        private string _carpeta = null!;
        private readonly List<string> _registros = new();

        [TestInitialize]
        public void Preparar()
        {
            _carpeta = Path.Combine(Path.GetTempPath(), "ecoaccess-groq-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_carpeta);
        }

        [TestCleanup]
        public void Limpiar() => Directory.Delete(_carpeta, true);

        private static ConfiguracionGroq Ajustes(int maximoDia = 100, decimal presupuesto = 5m) =>
            new(Clave, ConfiguracionVision.ModeloGroq, TimeSpan.FromSeconds(10), maximoDia, presupuesto, 0.80m, 4.00m);

        private static ImagenResiduo Foto(EnvioExterno envio = EnvioExterno.Autorizado, int lado = 40) =>
            new(VisionSeguraFixture.Imagen(lado, lado, SKEncodedImageFormat.Png), "image/png", envio);

        private static HttpResponseMessage Respuesta(string contenido, string modelo = ConfiguracionVision.ModeloGroq, string fin = "stop") =>
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    model = modelo,
                    choices = new[] { new { finish_reason = fin, message = new { content = contenido } } },
                    usage = new { prompt_tokens = 2100, completion_tokens = 20 },
                }), Encoding.UTF8, "application/json"),
            };

        private (ClasificadorGroq Clasificador, VisionSeguraFixture.Manejador Http, UsoGroq Uso) Crear(
            Func<HttpRequestMessage, int, HttpResponseMessage> responder, ConfiguracionGroq? ajustes = null, TimeProvider? reloj = null)
        {
            var http = new VisionSeguraFixture.Manejador(responder);
            var uso = new UsoGroq(Path.Combine(_carpeta, "uso.json"), reloj);
            var clasificador = new ClasificadorGroq(ajustes ?? Ajustes(), uso, _registros.Add, new HttpClient(http),
                (_, _) => Task.CompletedTask, reloj, Path.Combine(_carpeta, "estado.json"));
            return (clasificador, http, uso);
        }

        [TestMethod]
        public async Task EnviaElModeloExactoConImagenYSalidaEstructurada()
        {
            var (c, http, _) = Crear((_, _) => Respuesta(PropuestaPet));
            PropuestaVision p = await c.ClasificarAsync(Foto(), Catalogo);

            Assert.AreEqual("PET", p.CodigoMaterial);
            Assert.AreEqual(EstadoObservable.Limpio, p.Estado);
            var peticion = http.Peticiones.Single();
            Assert.AreEqual("https://api.groq.com/openai/v1/chat/completions", peticion.Uri.AbsoluteUri);
            Assert.AreEqual("Bearer", peticion.Esquema);
            Assert.AreEqual(Clave, peticion.Autorizacion);
            using var cuerpo = JsonDocument.Parse(peticion.Cuerpo);
            var raiz = cuerpo.RootElement;
            Assert.AreEqual("qwen/qwen3.8-27b", raiz.GetProperty("model").GetString());
            Assert.AreEqual("none", raiz.GetProperty("reasoning_effort").GetString());
            Assert.AreEqual("json_schema", raiz.GetProperty("response_format").GetProperty("type").GetString());
            string url = raiz.GetProperty("messages")[1].GetProperty("content")[1].GetProperty("image_url").GetProperty("url").GetString()!;
            StringAssert.StartsWith(url, "data:image/jpeg;base64,");
        }

        [TestMethod]
        public async Task LaFotoNoAutorizadaNuncaLlegaAlServicio()
        {
            var (c, http, _) = Crear((_, _) => Respuesta(PropuestaPet));
            await Assert.ThrowsExactlyAsync<VisionExternaNoAutorizadaException>(() => c.ClasificarAsync(Foto(EnvioExterno.Prohibido), Catalogo));
            Assert.IsEmpty(http.Peticiones);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Unauthorized)]
        [DataRow(HttpStatusCode.Forbidden)]
        public async Task CredencialRechazada_NoInsisteYNoExponeLaClave(HttpStatusCode codigo)
        {
            var (c, http, _) = Crear((_, _) => new HttpResponseMessage(codigo));
            var error = await Assert.ThrowsExactlyAsync<FalloGroqException>(() => c.ClasificarAsync(Foto(), Catalogo));
            await Assert.ThrowsExactlyAsync<FalloGroqException>(() => c.ClasificarAsync(Foto(lado: 41), Catalogo));

            Assert.HasCount(1, http.Peticiones);
            Assert.DoesNotContain(Clave, error.Message);
            Assert.IsFalse(_registros.Any(r => r.Contains(Clave)));
        }

        [TestMethod]
        public async Task Limite429_RespetaRetryAfterSinReintentar()
        {
            var reloj = new RelojAjustable(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));
            var (c, http, _) = Crear((_, n) =>
            {
                if (n > 1) return Respuesta(PropuestaPet);
                var limite = new HttpResponseMessage((HttpStatusCode)429);
                limite.Headers.TryAddWithoutValidation("retry-after", "30");
                return limite;
            }, reloj: reloj);

            await Assert.ThrowsExactlyAsync<FalloGroqException>(() => c.ClasificarAsync(Foto(), Catalogo));
            await Assert.ThrowsExactlyAsync<FalloGroqException>(() => c.ClasificarAsync(Foto(lado: 41), Catalogo));
            Assert.HasCount(1, http.Peticiones, "Durante la pausa no se envía nada.");

            reloj.Avanzar(TimeSpan.FromSeconds(31));
            Assert.AreEqual("PET", (await c.ClasificarAsync(Foto(lado: 42), Catalogo)).CodigoMaterial);
            Assert.HasCount(2, http.Peticiones);
        }

        [TestMethod]
        public async Task FalloTransitorio_ReintentaUnaVezYTermina()
        {
            var (c, http, _) = Crear((_, n) => n == 1 ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : Respuesta(PropuestaPet));
            Assert.AreEqual("PET", (await c.ClasificarAsync(Foto(), Catalogo)).CodigoMaterial);
            Assert.HasCount(2, http.Peticiones);

            var (siempre, http2, _) = Crear((_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError));
            await Assert.ThrowsExactlyAsync<FalloGroqException>(() => siempre.ClasificarAsync(Foto(), Catalogo));
            Assert.HasCount(2, http2.Peticiones, "El reintento es limitado.");
        }

        [TestMethod]
        [DataRow("texto suelto")]
        [DataRow("<think>razono</think>{\"objeto\":\"pet_bottle\",\"certeza\":\"high\"}")]
        [DataRow("{\"objeto\":\"pet_bottle\"}")]
        [DataRow("{\"objeto\":\"abrir_caneca\",\"certeza\":\"high\"}")]
        [DataRow("{\"objeto\":\"pet_bottle\",\"certeza\":\"segura\"}")]
        [DataRow("{\"objeto\":\"pet_bottle\",\"certeza\":\"high\",\"accion\":\"abrir\"}")]
        [DataRow("{\"objeto\":\"pet_bottle\",\"certeza\":\"high\",\"estado\":\"nuevo\"}")]
        [DataRow("{\"objeto\":\"pet_bottle\",\"certeza\":\"high\",\"requiere_aclaracion\":\"no\"}")]
        public async Task RespuestaInvalida_SeRechazaSinConvertirseEnPropuesta(string contenido)
        {
            var (c, _, _) = Crear((_, _) => Respuesta(contenido));
            await Assert.ThrowsExactlyAsync<FalloGroqException>(() => c.ClasificarAsync(Foto(), Catalogo));
        }

        [TestMethod]
        public async Task RespuestaIncompletaOTraOModeloDistinto_SeRechazan()
        {
            var (incompleta, _, _) = Crear((_, _) => Respuesta(PropuestaPet, fin: "length"));
            await Assert.ThrowsExactlyAsync<FalloGroqException>(() => incompleta.ClasificarAsync(Foto(), Catalogo));
            var (sustituto, _, _) = Crear((_, _) => Respuesta(PropuestaPet, modelo: "otro/modelo"));
            await Assert.ThrowsExactlyAsync<FalloGroqException>(() => sustituto.ClasificarAsync(Foto(), Catalogo));
        }

        [TestMethod]
        public async Task SiElModeloPideAclarar_NoSeProponeMaterial()
        {
            var (c, _, _) = Crear((_, _) => Respuesta("{\"objeto\":\"pet_bottle\",\"certeza\":\"high\",\"estado\":\"unknown\",\"requiere_aclaracion\":true}"));
            PropuestaVision p = await c.ClasificarAsync(Foto(), Catalogo);
            Assert.AreEqual(TipoPropuestaVision.Incierta, p.Tipo);
            Assert.IsNull(p.CodigoMaterial);
        }

        [TestMethod]
        public async Task ElLimiteDiarioSobreviveAlReinicioYElCostoSeCalcula()
        {
            var (c, http, uso) = Crear((_, _) => Respuesta(PropuestaPet), Ajustes(maximoDia: 1));
            await c.ClasificarAsync(Foto(), Catalogo);
            await Assert.ThrowsExactlyAsync<FalloGroqException>(() => c.ClasificarAsync(Foto(lado: 41), Catalogo));
            Assert.HasCount(1, http.Peticiones);

            // 2100 tokens a 0,80 y 20 a 4,00 por millón.
            Assert.AreEqual(0.00176m, uso.Actual.CostoUsd);
            Assert.AreEqual(2100, uso.Actual.TokensEntrada);

            var (trasReinicio, http2, _) = Crear((_, _) => Respuesta(PropuestaPet), Ajustes(maximoDia: 1));
            await Assert.ThrowsExactlyAsync<FalloGroqException>(() => trasReinicio.ClasificarAsync(Foto(lado: 42), Catalogo));
            Assert.IsEmpty(http2.Peticiones);
        }

        [TestMethod]
        public async Task LaMismaFotoSimultaneaComparteUnaSolaLlamada()
        {
            var liberar = new TaskCompletionSource<bool>();
            var http = new ManejadorRetenido(liberar.Task, () => Respuesta(PropuestaPet));
            using var c = new ClasificadorGroq(Ajustes(), new UsoGroq(Path.Combine(_carpeta, "uso.json")), _registros.Add, new HttpClient(http),
                (_, _) => Task.CompletedTask, null, Path.Combine(_carpeta, "estado.json"));
            ImagenResiduo foto = Foto();
            var a = c.ClasificarAsync(foto, Catalogo);
            var b = c.ClasificarAsync(foto, Catalogo);
            liberar.SetResult(true);
            await Task.WhenAll(a, b);
            Assert.AreEqual(1, http.Llamadas);
        }

        [TestMethod]
        public void ModoInvalidoOAusente_NoEnviaNadaAfuera()
        {
            foreach (string? valor in new[] { null, "", "Nube", "1" })
                Assert.AreEqual(ModoVision.Local, ConfiguracionVision.LeerModo(Config(("Vision:Modo", valor)), _ => { }));
            Assert.AreEqual(ModoVision.Automatico, ConfiguracionVision.LeerModo(Config(("Vision:Modo", "automatico")), _ => { }));
            Assert.AreEqual(ModoVision.Groq, ConfiguracionVision.LeerModo(Config(("Vision:Modo", "Groq")), _ => { }));
        }

        [TestMethod]
        public void LaClaveNoApareceAlConvertirLaConfiguracionEnTexto()
        {
            ConfiguracionGroq? ajustes = ConfiguracionVision.LeerGroq(Config(("Vision:Groq:ApiKey", Clave)), _ => { });
            Assert.IsNotNull(ajustes);
            Assert.AreEqual("qwen/qwen3.8-27b", ajustes.Modelo);
            Assert.DoesNotContain(Clave, ajustes.ToString());
        }

        private static IConfiguration Config(params (string Clave, string? Valor)[] valores) => new ConfigurationBuilder()
            .AddInMemoryCollection(valores.Select(v => new KeyValuePair<string, string?>(v.Clave, v.Valor))).Build();

        [TestMethod]
        public async Task Diagnostico_DistingueCadaEstadoSinEnviarImagenes()
        {
            Assert.AreEqual(EstadoGroq.NoConfigurado, (await DiagnosticoGroq.ComprobarAsync(null)).Estado);

            async Task<EstadoGroq> Con(Func<HttpResponseMessage> responder)
            {
                using var cliente = new HttpClient(new VisionSeguraFixture.Manejador((_, _) => responder()));
                return (await DiagnosticoGroq.ComprobarAsync(Ajustes(), cliente)).Estado;
            }

            HttpResponseMessage Lista(string id) => new(HttpStatusCode.OK)
            { Content = new StringContent("{\"data\":[{\"id\":\"" + id + "\"}]}", Encoding.UTF8, "application/json") };

            Assert.AreEqual(EstadoGroq.Operativo, await Con(() => Lista(ConfiguracionVision.ModeloGroq)));
            Assert.AreEqual(EstadoGroq.ModeloNoDisponible, await Con(() => Lista("otro/modelo")));
            Assert.AreEqual(EstadoGroq.CredencialRechazada, await Con(() => new HttpResponseMessage(HttpStatusCode.Unauthorized)));
            Assert.AreEqual(EstadoGroq.NoDisponible, await Con(() => new HttpResponseMessage(HttpStatusCode.BadGateway)));
        }

        [TestMethod]
        public void Metricas_SeparanFalsasAdmisionesRechazosYFallosTecnicos()
        {
            var catalogo = new[]
            {
                new Material("PET", "PET", CategoriaMaterial.Aprovechable, null),
                new Material("CONDON", "Condón", CategoriaMaterial.NoAprovechable, null),
            };
            var detalles = new[]
            {
                new DetalleEvaluacionVision("A", "PET", "PET", 100),
                new DetalleEvaluacionVision("B", "CONDON", "PET", 200),
                new DetalleEvaluacionVision("C", "PET", "Incierta", 300),
                new DetalleEvaluacionVision("D", "PET", "ProveedorNoDisponible", 400),
            };
            var informe = new InformeVisionArchivos("c", "l", "p", DateTime.UtcNow, "h", 0.7m, 4, detalles, 1,
                EvaluadorClasificacion.Evaluar(detalles.Select(d => new MuestraEvaluacion(d.Esperada, d.Obtenida)).ToArray()));

            ResumenVision r = MetricasVision.Calcular(informe, catalogo);

            Assert.AreEqual(1, r.Aciertos);
            Assert.AreEqual(1, r.FalsasAdmisiones);
            Assert.AreEqual(2, r.FalsosRechazos);
            Assert.AreEqual(1, r.Abstenciones);
            Assert.AreEqual(1, r.FallosTecnicos);
            Assert.AreEqual(0.75m, r.DisponibilidadObservada);
            Assert.AreEqual(200, r.LatenciaMedianaMs);
            Assert.AreEqual(400, r.LatenciaP95Ms);
            Assert.IsNull(r.LatenciaP99Ms, "Con cuatro fotos P99 no se puede interpretar.");
        }

        [TestMethod]
        [DataRow("{\"document\":{\"file_id\":\"f1\",\"mime_type\":\"application/pdf\",\"file_size\":100}}", true, null)]
        [DataRow("{\"document\":{\"file_id\":\"f1\",\"mime_type\":\"image/png\",\"file_size\":99999999}}", true, null)]
        [DataRow("{\"document\":{\"file_id\":\"f1\",\"mime_type\":\"image/jpeg\",\"file_size\":100}}", false, "f1")]
        [DataRow("{\"voice\":{\"file_id\":\"v\"}}", true, null)]
        [DataRow("{\"video\":{\"file_id\":\"v\"}}", true, null)]
        public void TraductorDeUpdates_NoIgnoraAdjuntos(string adjunto, bool noCompatible, string? foto)
        {
            string mensaje = adjunto.Substring(1, adjunto.Length - 2);
            using var documento = JsonDocument.Parse("{\"update_id\":1,\"message\":{\"from\":{\"id\":7},\"chat\":{\"id\":7,\"type\":\"private\"}," + mensaje + "}}");
            UpdateRecibido update = TraductorUpdates.Traducir(documento.RootElement);
            Assert.IsNotNull(update.Mensaje);
            Assert.AreEqual(noCompatible, update.Mensaje.ArchivoNoCompatible);
            Assert.AreEqual(foto, update.Mensaje.ArchivoFoto);
        }

        private sealed class ManejadorRetenido : HttpMessageHandler
        {
            private readonly Task _liberar;
            private readonly Func<HttpResponseMessage> _responder;
            private int _llamadas;

            public ManejadorRetenido(Task liberar, Func<HttpResponseMessage> responder)
            {
                _liberar = liberar;
                _responder = responder;
            }

            public int Llamadas => Volatile.Read(ref _llamadas);

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                Interlocked.Increment(ref _llamadas);
                await _liberar;
                return _responder();
            }
        }
    }

    /// <summary>Selector de motores con clasificadores falsos: qué motor actúa en cada modo.</summary>
    [TestClass]
    public sealed class SelectorVisionTests
    {
        private static readonly Material[] Catalogo = { new("PET", "Botella PET", CategoriaMaterial.Aprovechable, null) };
        private static ImagenResiduo Foto(EnvioExterno envio) => new(new byte[] { 1 }, "image/png", envio);
        private static readonly PropuestaVision Valida = new(TipoPropuestaVision.MaterialIdentificado, "PET", 0.9m);

        internal sealed class Motor : IClasificadorMaterial
        {
            private readonly Func<PropuestaVision> _responder;
            public Motor(string nombre, Func<PropuestaVision> responder) { Proveedor = nombre; _responder = responder; }
            public string Proveedor { get; }
            public int Llamadas { get; private set; }
            public Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken ct = default)
            {
                Llamadas++;
                return Task.FromResult(_responder());
            }
        }

        private static SelectorVision Selector(ModoVision modo, IClasificadorMaterial? local, IClasificadorMaterial? externo) =>
            new(modo, local, externo, TimeSpan.FromSeconds(30), _ => { });

        private static PropuestaVision Caido() => throw new InvalidOperationException("caído");

        [TestMethod]
        public async Task Local_NuncaUsaElExternoAunqueFalle()
        {
            var local = new Motor("local", Caido);
            var externo = new Motor("externo", () => Valida);
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Selector(ModoVision.Local, local, externo).ClasificarAsync(Foto(EnvioExterno.Autorizado), Catalogo));
            Assert.AreEqual(0, externo.Llamadas);
            Assert.AreEqual(PoliticaVisionExterna.Ninguna, Selector(ModoVision.Local, local, externo).Politica);
        }

        [TestMethod]
        public async Task Automatico_RespuestaInciertaLocalNoEsUnFalloYNoSaleLaFoto()
        {
            var local = new Motor("local", () => new PropuestaVision(TipoPropuestaVision.Incierta, null, null));
            var externo = new Motor("externo", () => Valida);
            PropuestaVision p = await Selector(ModoVision.Automatico, local, externo).ClasificarAsync(Foto(EnvioExterno.Autorizado), Catalogo);
            Assert.AreEqual(TipoPropuestaVision.Incierta, p.Tipo);
            Assert.AreEqual("local", p.Proveedor);
            Assert.AreEqual(0, externo.Llamadas);
        }

        [TestMethod]
        public async Task Automatico_LocalCaido_SoloUsaElExternoConAutorizacion()
        {
            var local = new Motor("local", Caido);
            var externo = new Motor("externo", () => Valida);
            await Assert.ThrowsExactlyAsync<VisionExternaNoAutorizadaException>(() => Selector(ModoVision.Automatico, local, externo).ClasificarAsync(Foto(EnvioExterno.Prohibido), Catalogo));
            Assert.AreEqual(0, externo.Llamadas);

            PropuestaVision p = await Selector(ModoVision.Automatico, local, externo).ClasificarAsync(Foto(EnvioExterno.Autorizado), Catalogo);
            Assert.AreEqual("externo", p.Proveedor);
            Assert.AreEqual(1, externo.Llamadas);
        }

        [TestMethod]
        public async Task Automatico_SegundaOpinionVaDirectoAlExterno()
        {
            var local = new Motor("local", () => Valida);
            var externo = new Motor("externo", () => Valida);
            await Selector(ModoVision.Automatico, local, externo).ClasificarAsync(Foto(EnvioExterno.SegundaOpinion), Catalogo);
            Assert.AreEqual(0, local.Llamadas);
            Assert.AreEqual(1, externo.Llamadas);
        }

        [TestMethod]
        public void SinExterno_LaConversacionNoPideAutorizacion()
        {
            Assert.AreEqual(PoliticaVisionExterna.Ninguna, Selector(ModoVision.Automatico, new Motor("local", () => Valida), null).Politica);
            Assert.AreEqual(PoliticaVisionExterna.Principal, Selector(ModoVision.Groq, null, new Motor("externo", () => Valida)).Politica);
            Assert.AreEqual(PoliticaVisionExterna.Respaldo, Selector(ModoVision.Automatico, new Motor("l", () => Valida), new Motor("e", () => Valida)).Politica);
        }
    }
}
