using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Logica.Vision35;
using EcoAccess.P3.Logica.Vision35.Expo;
using EcoAccess.P3.Tests.Logica.Dobles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// QR fijo de la estación en Telegram (Modo Expo): escanearlo autoriza a la persona por una sesión temporal, sin pedir
    /// permiso a un supervisor; una foto ya identificada se abre sola al escanear; escanear sin residuo no abre nada; la
    /// apertura no se repite y hay un límite por persona. Estación y Oracle son dobles; Ollama es un servidor local falso
    /// que siempre «ve» un limón (caneca verde, 7 s). No se mueve ningún motor.
    /// </summary>
    public sealed partial class ServicioConversacionTelegramTests
    {
        private sealed class EstacionFisicaDoble : IEstacionCanecasExpo
        {
            public readonly List<string> Ordenes = new();
            public bool EsSimulada => false;
            public Task<EstadoEstacionExpo> EstadoAsync(CancellationToken ct = default) =>
                Task.FromResult(new EstadoEstacionExpo(true, true, true, "acepta", "COM-DOBLE", "doble"));
            public Task<ResultadoAperturaEstacion> AbrirAsync(string clave, IReadOnlyList<CategoriaMaterial> canecas, string modalidad, string origen, string solicitante, string? materiales, CancellationToken ct = default)
            {
                string letras = ClienteEstacionPanel.Letras(canecas);
                lock (Ordenes) Ordenes.Add(clave + "=" + letras);
                int ms = PoliticaTiemposApertura.DuracionMs(letras.Length);
                return Task.FromResult(new ResultadoAperturaEstacion(true, false, false, letras, ms, modalidad, ms, "Orden confirmada por el ESP32 (doble)."));
            }
        }

        private sealed class OracleOperacionesDoble : IRegistroOperacionesEstacion
        {
            public readonly Dictionary<string, OperacionEstacion> Filas = new();
            public Task GuardarAsync(OperacionEstacion operacion, CancellationToken ct = default) { lock (Filas) Filas[operacion.Clave] = operacion; return Task.CompletedTask; }
            public Task<IReadOnlyList<OperacionEstacion>> RecientesAsync(int? idUsuario, int maximo, CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<OperacionEstacion>>(Filas.Values.ToList());
        }

        /// <summary>Ollama falso: toda clasificación responde «limón»; toda pregunta sí/no responde «no».</summary>
        private sealed class OllamaLimon : IDisposable
        {
            private readonly HttpListener _oyente = new();
            public string Url { get; }
            public OllamaLimon()
            {
                var l = new TcpListener(IPAddress.Loopback, 0); l.Start(); int puerto = ((IPEndPoint)l.LocalEndpoint).Port; l.Stop();
                Url = $"http://localhost:{puerto}";
                _oyente.Prefixes.Add(Url + "/"); _oyente.Start();
                _ = Task.Run(async () =>
                {
                    while (_oyente.IsListening)
                    {
                        HttpListenerContext ctx;
                        try { ctx = await _oyente.GetContextAsync().ConfigureAwait(false); } catch { return; }
                        string cuerpo; using (var r = new StreamReader(ctx.Request.InputStream, Encoding.UTF8)) cuerpo = await r.ReadToEndAsync().ConfigureAwait(false);
                        string contenido = cuerpo.Contains("\"answer\"") || cuerpo.Contains("yes") && cuerpo.Contains("Is it")
                            ? "{\"answer\":\"no\"}" : "{\"items\":[{\"name\":\"lime\",\"label\":\"lime\",\"count\":1}]}";
                        byte[] b = Encoding.UTF8.GetBytes("{\"message\":{\"role\":\"assistant\",\"content\":" + System.Text.Json.JsonSerializer.Serialize(contenido) + "},\"done\":true}");
                        ctx.Response.ContentType = "application/json"; ctx.Response.ContentLength64 = b.Length;
                        await ctx.Response.OutputStream.WriteAsync(b, 0, b.Length).ConfigureAwait(false);
                        ctx.Response.Close();
                    }
                });
            }
            public void Dispose() { try { _oyente.Stop(); _oyente.Close(); } catch (ObjectDisposedException) { } }
        }

        private (ServicioConversacionTelegram Eco, EstacionFisicaDoble Estacion, OracleOperacionesDoble Oracle, OllamaLimon Ollama) EcoConQrDeEstacion()
        {
            var ollama = new OllamaLimon();
            var estacion = new EstacionFisicaDoble();
            var oracle = new OracleOperacionesDoble();
            var motor = new MotorFalso("ollama-local:modelo", BotellaEnPet);
            var selector = new SelectorVision(ModoVision.Local, motor, null, TimeSpan.FromSeconds(30), _ => { });
            byte[] jpeg = FotoJpegUnica();
            var eco = new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj,
                vision: new ServicioClasificacionImagen(selector, _materiales, new ServicioCatalogoAmbiental(_materiales, _contenedores)),
                descargarFoto: (_, _) => Task.FromResult(new ImagenResiduo(jpeg, "image/jpeg")),
                flujoRapido: true, qrDemo: null);
            eco.EstacionExpo = new ServicioEstacionExpo(estacion, new RegistroOperacionesConCola(oracle, Path.Combine(Path.GetTempPath(), "ecoaccess-qr-" + Guid.NewGuid().ToString("N"))), "simultanea");
            eco.ReconocedorExpo = new ReconocedorExpo9(urlOllama: ollama.Url, limite: TimeSpan.FromSeconds(20));
            return (eco, estacion, oracle, ollama);
        }

        /// <summary>JPEG real y distinto en cada prueba (el reconocedor guarda en caché por huella de la imagen).</summary>
        private static byte[] FotoJpegUnica()
        {
            using var bmp = new System.Drawing.Bitmap(64, 64);
            var azar = new Random(Guid.NewGuid().GetHashCode());
            for (int x = 0; x < 64; x++) for (int y = 0; y < 64; y++) bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(azar.Next(256), azar.Next(256), azar.Next(256)));
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            return ms.ToArray();
        }

        private Task<IReadOnlyList<RespuestaBot>> FotoAsync(ServicioConversacionTelegram eco, long cuenta = Invitado) =>
            eco.ProcesarAsync(new MensajeEntrante(Interlocked.Increment(ref _update), cuenta, cuenta, true, null, null, ArchivoFoto: "foto-" + Guid.NewGuid().ToString("N")));

        private static string Todo(IReadOnlyList<RespuestaBot> r) => string.Join("\n", r.Select(x => x.Texto));

        [TestMethod]
        public async Task FotoIdentificadaAntesDelQr_SeAbreSolaAlEscanearYUnaSolaVez()
        {
            var (eco, estacion, oracle, ollama) = EcoConQrDeEstacion();
            using (ollama)
            {
                string antes = Todo(await FotoAsync(eco));
                StringAssert.Contains(antes, "Escanea el QR de la estación");
                Assert.IsFalse(antes.Contains("imón"), "Telegram no nombra el residuo identificado.");
                Assert.IsFalse(antes.Contains("supervisor"), "Ya no se pide autorización a un supervisor.");
                Assert.AreEqual(0, estacion.Ordenes.Count, "Sin QR no se abre nada.");

                string tras = Todo(await EnviarConAsync(eco, "/start " + _caneca.IdentificadorQr, cuenta: Invitado));
                Assert.AreEqual(1, estacion.Ordenes.Count, "Al escanear se ejecuta la apertura pendiente.");
                StringAssert.EndsWith(estacion.Ordenes[0], "=V");
                StringAssert.Contains(tras, "🟢 Caneca verde abierta.\nTienes 7 segundos para desechar tu residuo.");
                foreach (var tecnico in new[] { "limón", "Limón", "%", "Oracle", "ESP32", "COM", "confianza" })
                    Assert.IsFalse(tras.Contains(tecnico), "La respuesta de apertura no lleva detalles técnicos: " + tecnico);

                string otraVez = Todo(await EnviarConAsync(eco, "/start " + _caneca.IdentificadorQr, cuenta: Invitado));
                Assert.AreEqual(1, estacion.Ordenes.Count, "Volver a escanear no repite la apertura.");
                StringAssert.Contains(otraVez, "Estación vinculada");

                await Task.Delay(200);
                var fila = oracle.Filas.Values.Single();
                Assert.AreEqual(CanalEstacion.TELEGRAM, fila.Canal);
                StringAssert.Contains(fila.Canecas ?? "", "VERDE");
            }
        }

        [TestMethod]
        public async Task QrSinResiduoIdentificado_NoAbreNada()
        {
            var (eco, estacion, _, ollama) = EcoConQrDeEstacion();
            using (ollama)
            {
                string r = Todo(await EnviarConAsync(eco, "/start " + _caneca.IdentificadorQr, cuenta: Invitado));
                StringAssert.Contains(r, "Estación vinculada");
                Assert.AreEqual(0, estacion.Ordenes.Count);
            }
        }

        [TestMethod]
        public async Task QrPrimero_LasFotosSiguientesAbrenSinVolverAEscanear()
        {
            var (eco, estacion, _, ollama) = EcoConQrDeEstacion();
            using (ollama)
            {
                await EnviarConAsync(eco, "/start " + _caneca.IdentificadorQr, cuenta: Invitado);
                _reloj.Avanzar(TimeSpan.FromMinutes(10));
                string r = Todo(await FotoAsync(eco));
                Assert.AreEqual(1, estacion.Ordenes.Count, "La sesión del QR sigue vigente a los 10 minutos.");
                StringAssert.Contains(r, "🟢 Caneca verde abierta.\nTienes 7 segundos para desechar tu residuo.");
                Assert.IsFalse(r.Contains("imón"), "Telegram no le explica al usuario qué residuo fotografió.");
            }
        }

        [TestMethod]
        public async Task SesionDelQrVence_YNoAbreSinVolverAEscanear()
        {
            var (eco, estacion, _, ollama) = EcoConQrDeEstacion();
            using (ollama)
            {
                await EnviarConAsync(eco, "/start " + _caneca.IdentificadorQr, cuenta: Invitado);
                _reloj.Avanzar(ServicioConversacionTelegram.VigenciaSesionEstacion + TimeSpan.FromMinutes(1));
                string r = Todo(await FotoAsync(eco));
                Assert.AreEqual(0, estacion.Ordenes.Count);
                StringAssert.Contains(r, "Escanea el QR de la estación");
            }
        }

        [TestMethod]
        public async Task LimiteDeAperturasPorPersona()
        {
            var (eco, estacion, _, ollama) = EcoConQrDeEstacion();
            using (ollama)
            {
                await EnviarConAsync(eco, "/start " + _caneca.IdentificadorQr, cuenta: Invitado);
                string ultima = "";
                for (int i = 0; i < 5; i++) ultima = Todo(await EnviarConAsync(eco, boton: ServicioConversacionTelegram.PrefijoExpo + "man:N", cuenta: Invitado));
                Assert.AreEqual(4, estacion.Ordenes.Count, "4 aperturas por minuto como máximo.");
                StringAssert.Contains(ultima, "límite de aperturas");
            }
        }
    }
}
