using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Bot;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Bot
{
    /// <summary>
    /// Cliente HTTP de Telegram con un transporte falso (ningún mensaje sale a Telegram): getFile, descarga de la foto,
    /// fotos y documentos entrantes, tiempo agotado, errores 5xx, el reintento ÚNICO de getFile, la idempotencia por
    /// update_id de extremo a extremo (HTTP → bucle de polling) y la respuesta breve con las canecas.
    /// </summary>
    [TestClass]
    public sealed class ClienteTelegramHttpDescargaTests
    {
        // Token de forma válida inventado para la prueba; no pertenece a ningún bot.
        private const string Token = "123456:TOKEN-DE-PRUEBA-NO-REAL";
        private static readonly byte[] FotoJpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 0xFF, 0xD9 };

        /// <summary>Transporte falso: responde por método de la Bot API y registra cada petición.</summary>
        private sealed class Transporte : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, string, int, HttpResponseMessage> _responder;
            private int _llamadas;

            public Transporte(Func<HttpRequestMessage, string, int, HttpResponseMessage> responder) => _responder = responder;

            public List<(HttpMethod Metodo, Uri Ruta, string Cuerpo)> Peticiones { get; } = new();

            public int Cuenta(string metodo) { lock (Peticiones) return Peticiones.Count(p => p.Ruta.AbsolutePath.EndsWith("/" + metodo, StringComparison.Ordinal)); }

            public int Descargas { get { lock (Peticiones) return Peticiones.Count(p => p.Metodo == HttpMethod.Get); } }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string cuerpo = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync();
                int n;
                lock (Peticiones) { Peticiones.Add((request.Method, request.RequestUri!, cuerpo)); n = ++_llamadas; }
                return _responder(request, cuerpo, n);
            }
        }

        private static HttpResponseMessage Json(HttpStatusCode estado, string json) =>
            new(estado) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        private static HttpResponseMessage Foto() =>
            new(HttpStatusCode.OK) { Content = new ByteArrayContent(FotoJpeg) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg") } } };

        private const string GetFileOk = @"{""ok"":true,""result"":{""file_id"":""AgAD-foto"",""file_size"":11,""file_path"":""photos/file_1.jpg""}}";

        private static bool EsGetFile(HttpRequestMessage r) => r.RequestUri!.AbsolutePath.EndsWith("/getFile", StringComparison.Ordinal);

        private static (ClienteTelegramHttp Cliente, Transporte Transporte) Crear(Func<HttpRequestMessage, string, int, HttpResponseMessage> responder)
        {
            var t = new Transporte(responder);
            return (new ClienteTelegramHttp(Token, new HttpClient(t), propio: true), t);
        }

        // ───────────────────────── getFile y descarga ─────────────────────────

        [TestMethod]
        public async Task GetFileYDescarga_DevuelveLosBytesYElTipoSinReintentar()
        {
            var (cliente, t) = Crear((r, _, _) => r.Method == HttpMethod.Get ? Foto() : Json(HttpStatusCode.OK, GetFileOk));
            using (cliente)
            {
                ImagenResiduo imagen = await cliente.DescargarFotoAsync("AgAD-foto");

                CollectionAssert.AreEqual(FotoJpeg, imagen.Contenido);
                Assert.AreEqual("image/jpeg", imagen.TipoMime);
            }

            Assert.AreEqual(1, t.Cuenta("getFile"));
            Assert.AreEqual(1, t.Descargas);
            using JsonDocument cuerpo = JsonDocument.Parse(t.Peticiones[0].Cuerpo);
            Assert.AreEqual("AgAD-foto", cuerpo.RootElement.GetProperty("file_id").GetString());
            Assert.AreEqual(HttpMethod.Post, t.Peticiones[0].Metodo);
            // La descarga usa la raíz de archivos de la Bot API y la ruta que devolvió getFile.
            Assert.AreEqual("https://api.telegram.org/file/bot" + Token + "/photos/file_1.jpg", t.Peticiones[1].Ruta.ToString());
        }

        [TestMethod]
        [DataRow(HttpStatusCode.InternalServerError)]
        [DataRow(HttpStatusCode.BadGateway)]
        [DataRow(HttpStatusCode.ServiceUnavailable)]
        public async Task GetFile5xxUnaVez_SeReintentaUnaSolaVezYDescarga(HttpStatusCode estado)
        {
            var (cliente, t) = Crear((r, _, n) => r.Method == HttpMethod.Get ? Foto()
                : n == 1 ? Json(estado, @"{""ok"":false,""error_code"":500,""description"":""Internal Server Error""}") : Json(HttpStatusCode.OK, GetFileOk));
            using (cliente)
            {
                ImagenResiduo imagen = await cliente.DescargarFotoAsync("AgAD-foto");
                CollectionAssert.AreEqual(FotoJpeg, imagen.Contenido);
            }

            Assert.AreEqual(2, t.Cuenta("getFile"));
            Assert.AreEqual(1, t.Descargas);
        }

        [TestMethod]
        public async Task GetFile5xxDosVeces_FallaTrasUnUnicoReintento()
        {
            var (cliente, t) = Crear((_, _, _) => Json(HttpStatusCode.BadGateway, @"{""ok"":false,""error_code"":502,""description"":""Bad Gateway""}"));
            using (cliente)
            {
                ErrorTelegramException error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.DescargarFotoAsync("AgAD-foto"));
                Assert.DoesNotContain(Token, error.Message);
            }

            Assert.AreEqual(2, t.Cuenta("getFile"), "getFile admite exactamente un reintento.");
            Assert.AreEqual(0, t.Descargas);
        }

        [TestMethod]
        public async Task GetFileTiempoAgotado_SeReintentaUnaVez()
        {
            // Sin cancelación del llamador: el transporte agota su tiempo (lo mismo que hace HttpClient al vencer el límite).
            var (cliente, t) = Crear((r, _, n) => r.Method == HttpMethod.Get ? Foto()
                : n == 1 ? throw new TaskCanceledException("tiempo agotado") : Json(HttpStatusCode.OK, GetFileOk));
            using (cliente)
            {
                ImagenResiduo imagen = await cliente.DescargarFotoAsync("AgAD-foto");
                Assert.HasCount(FotoJpeg.Length, imagen.Contenido);
            }

            Assert.AreEqual(2, t.Cuenta("getFile"));
            Assert.AreEqual(1, t.Descargas);
        }

        [TestMethod]
        public async Task GetFileTiempoAgotadoDosVeces_FallaConMensajePropio()
        {
            var (cliente, t) = Crear((_, _, _) => throw new TaskCanceledException("tiempo agotado"));
            using (cliente)
            {
                ErrorTelegramException error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.DescargarFotoAsync("AgAD-foto"));
                StringAssert.Contains(error.Message, "a tiempo");
            }

            Assert.AreEqual(2, t.Cuenta("getFile"));
        }

        [TestMethod]
        public async Task SinConexionEnGetFile_SeReintentaUnaVez()
        {
            var (cliente, t) = Crear((r, _, n) => r.Method == HttpMethod.Get ? Foto()
                : n == 1 ? throw new HttpRequestException("No se pudo conectar a " + r.RequestUri) : Json(HttpStatusCode.OK, GetFileOk));
            using (cliente)
            {
                await cliente.DescargarFotoAsync("AgAD-foto");
            }

            Assert.AreEqual(2, t.Cuenta("getFile"));
        }

        [TestMethod]
        [DataRow(HttpStatusCode.BadRequest, @"{""ok"":false,""error_code"":400,""description"":""Bad Request: file is too big""}")]
        [DataRow((HttpStatusCode)429, @"{""ok"":false,""error_code"":429,""description"":""Too Many Requests"",""parameters"":{""retry_after"":3}}")]
        [DataRow(HttpStatusCode.OK, @"{""ok"":true,""result"":{""file_id"":""x"",""file_size"":99999999,""file_path"":""photos/a.jpg""}}")]
        [DataRow(HttpStatusCode.OK, @"{""ok"":true,""result"":{""file_id"":""x"",""file_path"":""../secreto/a.jpg""}}")]
        [DataRow(HttpStatusCode.OK, @"{""ok"":true,""result"":{""file_id"":""x""}}")]
        [DataRow(HttpStatusCode.OK, @"{""ok"":true,""result"":{""file_id"":""x"",""file_path"":42}}")]
        [DataRow(HttpStatusCode.OK, @"{""ok"":true,""result"":{""file_id"":""x"",""file_size"":""grande"",""file_path"":""photos/a.jpg""}}")]
        [DataRow(HttpStatusCode.OK, @"{""ok"":true}")]
        public async Task RespuestaDefinitivaDeGetFile_NoSeReintentaNiSeDescarga(HttpStatusCode estado, string json)
        {
            var (cliente, t) = Crear((r, _, _) => r.Method == HttpMethod.Get ? Foto() : Json(estado, json));
            using (cliente)
            {
                await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.DescargarFotoAsync("AgAD-foto"));
            }

            Assert.AreEqual(1, t.Cuenta("getFile"), "Un error definitivo no se repite.");
            Assert.AreEqual(0, t.Descargas);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.InternalServerError)]
        [DataRow(HttpStatusCode.NotFound)]
        public async Task FalloAlDescargarElArchivo_NoSeRepiteLaDescarga(HttpStatusCode estado)
        {
            var (cliente, t) = Crear((r, _, _) => r.Method == HttpMethod.Get ? new HttpResponseMessage(estado) : Json(HttpStatusCode.OK, GetFileOk));
            using (cliente)
            {
                ErrorTelegramException error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.DescargarFotoAsync("AgAD-foto"));
                Assert.DoesNotContain(Token, error.Message);
            }

            Assert.AreEqual(1, t.Cuenta("getFile"));
            Assert.AreEqual(1, t.Descargas, "La descarga del archivo nunca se repite.");
        }

        [TestMethod]
        public async Task TiempoAgotadoEnLaDescarga_NoSeRepite()
        {
            var (cliente, t) = Crear((r, _, _) => r.Method == HttpMethod.Get ? throw new TaskCanceledException("tiempo agotado") : Json(HttpStatusCode.OK, GetFileOk));
            using (cliente)
            {
                await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.DescargarFotoAsync("AgAD-foto"));
            }

            Assert.AreEqual(1, t.Cuenta("getFile"));
            Assert.AreEqual(1, t.Descargas);
        }

        [TestMethod]
        public async Task ArchivoMasGrandeQueElLimite_SeCortaYNoSeRepite()
        {
            // getFile sin file_size (Telegram no siempre lo informa): el límite lo impone la lectura acotada.
            var grande = new byte[ServicioClasificacionImagen.MaximoBytes + 1];
            var (cliente, t) = Crear((r, _, _) => r.Method == HttpMethod.Get
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(grande) }
                : Json(HttpStatusCode.OK, @"{""ok"":true,""result"":{""file_id"":""x"",""file_path"":""photos/a.jpg""}}"));
            using (cliente)
            {
                await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.DescargarFotoAsync("AgAD-foto"));
            }

            Assert.AreEqual(1, t.Cuenta("getFile"));
            Assert.AreEqual(1, t.Descargas);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("con\ncontrol")]
        public async Task ReferenciaDeFotoInvalida_NoLlamaATelegram(string archivo)
        {
            var (cliente, t) = Crear((_, _, _) => Json(HttpStatusCode.OK, GetFileOk));
            using (cliente)
            {
                await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.DescargarFotoAsync(archivo));
            }

            Assert.IsEmpty(t.Peticiones);
        }

        [TestMethod]
        public async Task CancelacionDelLlamador_SePropagaSinReintentar()
        {
            using var cancelacion = new CancellationTokenSource();
            var (cliente, t) = Crear((_, _, _) => { cancelacion.Cancel(); throw new TaskCanceledException(); });
            using (cliente)
            {
                await Assert.ThrowsAsync<OperationCanceledException>(() => cliente.DescargarFotoAsync("AgAD-foto", cancelacion.Token));
            }

            Assert.AreEqual(1, t.Cuenta("getFile"));
        }

        // ───────────────────────── Fotos y documentos entrantes (getUpdates) ─────────────────────────

        private static async Task<MensajeEntrante?> RecibirAsync(string mensajeJson)
        {
            var (cliente, _) = Crear((_, _, _) => Json(HttpStatusCode.OK,
                @"{""ok"":true,""result"":[{""update_id"":70,""message"":" + mensajeJson + "}]}"));
            using (cliente)
            {
                IReadOnlyList<UpdateRecibido> lote = await cliente.ObtenerUpdatesAsync(70, TimeSpan.FromSeconds(1), CancellationToken.None);
                Assert.HasCount(1, lote);
                Assert.AreEqual(70, lote[0].UpdateId);
                return lote[0].Mensaje;
            }
        }

        private const string Remitente = @"""from"":{""id"":801},""chat"":{""id"":801,""type"":""private""}";

        [TestMethod]
        public async Task FotoEntrante_EligeLaVarianteSuficienteMasPequena()
        {
            MensajeEntrante? m = await RecibirAsync("{" + Remitente + @",""photo"":[
                {""file_id"":""f90"",""width"":90,""height"":68,""file_size"":1200},
                {""file_id"":""f320"",""width"":320,""height"":240,""file_size"":15000},
                {""file_id"":""f800"",""width"":800,""height"":600,""file_size"":60000},
                {""file_id"":""f1280"",""width"":1280,""height"":960,""file_size"":140000}]}");

            Assert.IsNotNull(m);
            Assert.AreEqual("f800", m.ArchivoFoto);
            Assert.IsFalse(m.ArchivoNoCompatible);
            Assert.AreEqual(801, m.ChatId);
        }

        [TestMethod]
        [DataRow("image/jpeg")]
        [DataRow("image/png")]
        [DataRow("image/webp")]
        public async Task DocumentoDeImagen_SeTrataComoFoto(string mime)
        {
            MensajeEntrante? m = await RecibirAsync("{" + Remitente + @",""document"":{""file_id"":""doc-1"",""mime_type"":""" + mime + @""",""file_size"":200000}}");

            Assert.IsNotNull(m);
            Assert.AreEqual("doc-1", m.ArchivoFoto);
            Assert.IsFalse(m.ArchivoNoCompatible);
        }

        [TestMethod]
        [DataRow(@"{""file_id"":""doc-2"",""mime_type"":""application/pdf"",""file_size"":2000}")]
        [DataRow(@"{""file_id"":""doc-3"",""mime_type"":""image/jpeg"",""file_size"":99999999}")]
        [DataRow(@"{""file_id"":""doc-4"",""mime_type"":""image/heic"",""file_size"":2000}")]
        public async Task DocumentoNoCompatible_SeMarcaSinDescargar(string documento)
        {
            MensajeEntrante? m = await RecibirAsync("{" + Remitente + @",""document"":" + documento + "}");

            Assert.IsNotNull(m);
            Assert.IsNull(m.ArchivoFoto);
            Assert.IsTrue(m.ArchivoNoCompatible);
        }

        // ───────────────────────── Idempotencia por update_id (HTTP → bucle de polling) ─────────────────────────

        [TestMethod]
        public async Task UpdateRepetidoPorTelegram_SeAtiendeUnaSolaVezYAvanzaElOffset()
        {
            using var parar = new CancellationTokenSource();
            var offsets = new List<long>();
            var (cliente, t) = Crear((r, cuerpo, _) =>
            {
                if (!r.RequestUri!.AbsolutePath.EndsWith("/getUpdates", StringComparison.Ordinal)) return Json(HttpStatusCode.OK, @"{""ok"":true,""result"":{}}");
                long offset;
                using (JsonDocument d = JsonDocument.Parse(cuerpo)) offset = d.RootElement.GetProperty("offset").GetInt64();
                int n; lock (offsets) { offsets.Add(offset); n = offsets.Count; }
                string M(long id) => @"{""update_id"":" + id + @",""message"":{" + Remitente + @",""text"":""hola " + id + @"""}}";
                return n switch
                {
                    1 => Json(HttpStatusCode.OK, @"{""ok"":true,""result"":[" + M(50) + "]}"),
                    // Tras un corte, Telegram vuelve a entregar el 50 junto con el 51 (y uno viejo, el 49).
                    2 => Json(HttpStatusCode.OK, @"{""ok"":true,""result"":[" + M(50) + "," + M(49) + "," + M(51) + "]}"),
                    _ => Cancelar(parar),
                };
            });
            var atendidos = new List<long>();
            using (cliente)
            {
                var procesador = new ProcesadorUpdates((m, _) =>
                {
                    lock (atendidos) atendidos.Add(m.UpdateId);
                    return Task.FromResult<IReadOnlyList<RespuestaBot>>(new[] { new RespuestaBot(m.ChatId, "Recibido.") });
                }, cliente, _ => { });
                var bucle = new BuclePolling(cliente, procesador, _ => { }, (_, _) => Task.CompletedTask);

                FinPolling fin = await bucle.EjecutarAsync(parar.Token);

                Assert.AreEqual(FinPolling.Cancelado, fin);
                Assert.AreEqual(52, bucle.Offset);
            }

            CollectionAssert.AreEqual(new long[] { 50, 51 }, atendidos);
            CollectionAssert.AreEqual(new long[] { 0, 51, 52 }, offsets);
            Assert.AreEqual(2, t.Cuenta("sendMessage"), "Una respuesta por update atendido; el repetido no responde de nuevo.");
        }

        private static HttpResponseMessage Cancelar(CancellationTokenSource parar)
        {
            parar.Cancel();
            throw new TaskCanceledException();
        }

        // ───────────────────────── Respuesta breve con las canecas ─────────────────────────

        [TestMethod]
        public async Task RespuestaBreveConCanecas_UnSoloMensajeConLasTresEnUnaFila()
        {
            var (cliente, t) = Crear((_, _, _) => Json(HttpStatusCode.OK, @"{""ok"":true,""result"":{""message_id"":1}}"));
            var respuesta = new RespuestaBot(801, "♻️ Botella PET → caneca BLANCA.\nAbriendo la blanca 7 s.", new[]
            {
                new BotonBot("🟢 Verde", "menu:verde"), new BotonBot("⚪ Blanca", "menu:blanca"), new BotonBot("⚫ Negra", "menu:negra"),
            }) { Columnas = 3 };
            using (cliente)
            {
                await cliente.EnviarAsync(respuesta, CancellationToken.None);
            }

            var (_, ruta, cuerpo) = t.Peticiones.Single();
            StringAssert.EndsWith(ruta.AbsolutePath, "/sendMessage");
            using JsonDocument d = JsonDocument.Parse(cuerpo);
            string texto = d.RootElement.GetProperty("text").GetString()!;
            Assert.AreEqual(respuesta.Texto, texto);
            Assert.IsLessThanOrEqualTo(1024, texto.Length, "Cabe como leyenda de foto.");
            JsonElement filas = d.RootElement.GetProperty("reply_markup").GetProperty("inline_keyboard");
            Assert.AreEqual(1, filas.GetArrayLength());
            CollectionAssert.AreEqual(new[] { "menu:verde", "menu:blanca", "menu:negra" },
                filas[0].EnumerateArray().Select(b => b.GetProperty("callback_data").GetString()).ToArray());
        }

        [TestMethod]
        public async Task RespuestaConImagenNoValida_CaeATextoSinTocarLaCache()
        {
            // Una clave de imagen fuera del patrón no consulta la caché de file_id ni sube archivos: solo envía el texto.
            var (cliente, t) = Crear((_, _, _) => Json(HttpStatusCode.OK, @"{""ok"":true,""result"":{""message_id"":2}}"));
            using (cliente)
            {
                await cliente.EnviarAsync(new RespuestaBot(801, "Abriendo la verde 7 s.", new[] { new BotonBot("🗑️ Abrir caneca", "menu:canecas") })
                { Imagen = "../Eco Canecas" }, CancellationToken.None);
            }

            Assert.HasCount(1, t.Peticiones);
            Assert.AreEqual(1, t.Cuenta("sendMessage"));
            Assert.AreEqual(0, t.Cuenta("sendPhoto"));
        }

        [TestMethod]
        public async Task Envio5xx_NoSeRepiteDentroDelCliente()
        {
            // El cliente no reintenta envíos (un reenvío podría duplicar el mensaje): informa el fallo y la capa superior decide.
            var (cliente, t) = Crear((_, _, _) => Json(HttpStatusCode.InternalServerError, @"{""ok"":false,""error_code"":500,""description"":""Internal""}"));
            using (cliente)
            {
                ErrorTelegramException error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(
                    () => cliente.EnviarAsync(new RespuestaBot(801, "Abriendo la negra 7 s."), CancellationToken.None));
                StringAssert.Contains(error.Message, "HTTP 500");
            }

            Assert.AreEqual(1, t.Cuenta("sendMessage"));
        }
    }
}
