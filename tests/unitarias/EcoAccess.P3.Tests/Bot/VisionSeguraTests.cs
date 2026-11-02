using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using SkiaSharp;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class PreparadorImagenVisionSeguraTests
    {
        [TestMethod]
        public void PngRealSeDecodificaReduceYCodificaComoJpegSinConfiarEnElMimeDeclarado()
        {
            byte[] original = VisionSeguraFixture.Imagen(2048, 512, SKEncodedImageFormat.Png);
            byte[] copia = original.ToArray();

            ImagenResiduo preparada = PreparadorImagen.Preparar(new ImagenResiduo(original, "text/plain"));

            Assert.AreEqual("image/jpeg", preparada.TipoMime);
            CollectionAssert.AreEqual(copia, original, "Preparar no debe modificar los bytes recibidos.");
            Assert.AreEqual("image/jpeg", ServicioClasificacionImagen.TipoMimeReal(preparada.Contenido));
            using var bitmap = SKBitmap.Decode(preparada.Contenido);
            Assert.IsNotNull(bitmap);
            Assert.AreEqual(1024, bitmap.Width);
            Assert.AreEqual(256, bitmap.Height);
            Assert.IsLessThanOrEqualTo(ServicioClasificacionImagen.MaximoBytes, preparada.Contenido.Length);
        }

        [TestMethod]
        public void TransparenciaSeComponeSobreBlancoYLasImagenesPequenasNoSeAmplian()
        {
            using var bitmap = new SKBitmap(60, 40, SKColorType.Rgba8888, SKAlphaType.Premul);
            bitmap.Erase(SKColors.Transparent);
            using var imagen = SKImage.FromBitmap(bitmap);
            using var png = imagen.Encode(SKEncodedImageFormat.Png, 100);

            var preparada = PreparadorImagen.Preparar(new ImagenResiduo(png.ToArray(), "image/png"));

            using var decodificada = SKBitmap.Decode(preparada.Contenido);
            Assert.AreEqual(60, decodificada.Width);
            Assert.AreEqual(40, decodificada.Height);
            SKColor pixel = decodificada.GetPixel(30, 20);
            Assert.IsTrue(pixel.Red > 245 && pixel.Green > 245 && pixel.Blue > 245);
            Assert.AreEqual((byte)255, pixel.Alpha);
        }

        [TestMethod]
        [DataRow(1, 0, 1, 2, 3)]
        [DataRow(2, 1, 0, 3, 2)]
        [DataRow(3, 3, 2, 1, 0)]
        [DataRow(4, 2, 3, 0, 1)]
        [DataRow(5, 0, 2, 1, 3)]
        [DataRow(6, 2, 0, 3, 1)]
        [DataRow(7, 3, 1, 2, 0)]
        [DataRow(8, 1, 3, 0, 2)]
        public void OrientacionExifSeAplicaALosPixelesYSeEliminanExifYComentario(int orientacion, int supIzq, int supDer, int infIzq, int infDer)
        {
            byte[] jpeg = VisionSeguraFixture.ConExifYComentario(VisionSeguraFixture.Imagen(80, 40, SKEncodedImageFormat.Jpeg), orientacion);
            using (var datos = SKData.CreateCopy(jpeg))
            using (var codec = SKCodec.Create(datos))
            {
                Assert.IsNotNull(codec, "La entrada sintética debe ser un JPEG realmente decodificable.");
                Assert.AreEqual(orientacion, (int)codec.EncodedOrigin);
            }

            var preparada = PreparadorImagen.Preparar(new ImagenResiduo(jpeg, "image/jpeg"));

            using var salida = SKBitmap.Decode(preparada.Contenido);
            Assert.AreEqual(orientacion >= 5 ? 40 : 80, salida.Width);
            Assert.AreEqual(orientacion >= 5 ? 80 : 40, salida.Height);
            SKColor[] esquinas =
            new SKColor[]
    {
        salida.GetPixel(7, 7),
        salida.GetPixel(salida.Width - 8, 7),
        salida.GetPixel(7, salida.Height - 8),
        salida.GetPixel(salida.Width - 8, salida.Height - 8)
    };
            int[] esperados = new int[] { supIzq, supDer, infIzq, infDer };
            for (int i = 0; i < esquinas.Length; i++)
                VisionSeguraFixture.ColorAproximado(VisionSeguraFixture.Colores[esperados[i]], esquinas[i]);
            Assert.IsLessThan(0, preparada.Contenido.AsSpan().IndexOf(Encoding.UTF8.GetBytes("Exif\0\0")));
            Assert.IsLessThan(0, preparada.Contenido.AsSpan().IndexOf(Encoding.ASCII.GetBytes(VisionSeguraFixture.ComentarioPrivado)));
            using var datosSalida = SKData.CreateCopy(preparada.Contenido);
            using var codecSalida = SKCodec.Create(datosSalida);
            Assert.AreEqual(SKEncodedOrigin.TopLeft, codecSalida.EncodedOrigin);
        }

        [TestMethod]
        public void EncabezadoConFirmaPeroSinImagenDecodificableSeRechaza()
        {
            byte[] falso = new byte[] { 0xff, 0xd8, 0xff, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            Assert.AreEqual("image/jpeg", ServicioClasificacionImagen.TipoMimeReal(falso));
            Assert.ThrowsExactly<ArgumentException>(() => PreparadorImagen.Preparar(new ImagenResiduo(falso, "image/jpeg")));
        }

        [TestMethod]
        public void PngTruncadoNoSePresentaComoImagenValida()
        {
            byte[] imagen = VisionSeguraFixture.Imagen(80, 40, SKEncodedImageFormat.Png);
            Assert.ThrowsExactly<ArgumentException>(() => PreparadorImagen.Preparar(new ImagenResiduo(imagen.Take(imagen.Length / 2).ToArray(), "image/png")));
        }

        [TestMethod]
        public void ImagenComprimidaConDemasiadosPixelesSeRechazaAntesDeDecodificarElBitmapCompleto()
        {
            byte[] grande = VisionSeguraFixture.Imagen(5000, 4001, SKEncodedImageFormat.Png);
            Assert.IsLessThan(ServicioClasificacionImagen.MaximoBytes, grande.Length, "El caso debe superar píxeles, no bytes.");
            Assert.ThrowsExactly<ArgumentException>(() => PreparadorImagen.Preparar(new ImagenResiduo(grande, "image/png")));
        }
    }

    [TestClass]
    public sealed class DescargaFotoTelegramVisionSeguraTests
    {
        private const string Token = "123456:TOKEN-DE-PRUEBA-NO-REAL";

        [TestMethod]
        public async Task DescargaUsaGetFileYHostFijoSinInterpretarFileIdComoUrl()
        {
            byte[] imagen = VisionSeguraFixture.Imagen(30, 20, SKEncodedImageFormat.Png);
            var manejador = new VisionSeguraFixture.Manejador((peticion, numero) => numero == 1
                ? Metadatos("photos/prueba_1.png", imagen.Length)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(imagen) { Headers = { ContentType = new MediaTypeHeaderValue("image/png") } },
                });
            using var http = new HttpClient(manejador);
            using var cliente = new ClienteTelegramHttp(Token, http, propio: false);

            ImagenResiduo recibida = await cliente.DescargarFotoAsync("https://destino-ajeno.invalid/no-es-una-url-de-descarga");

            CollectionAssert.AreEqual(imagen, recibida.Contenido);
            Assert.AreEqual("image/png", recibida.TipoMime);
            Assert.HasCount(2, manejador.Peticiones);
            Assert.AreEqual(HttpMethod.Post, manejador.Peticiones[0].Metodo);
            Assert.AreEqual(new Uri("https://api.telegram.org/bot" + Token + "/getFile"), manejador.Peticiones[0].Uri);
            using var cuerpo = JsonDocument.Parse(manejador.Peticiones[0].Cuerpo);
            Assert.AreEqual("https://destino-ajeno.invalid/no-es-una-url-de-descarga", cuerpo.RootElement.GetProperty("file_id").GetString());
            Assert.AreEqual(HttpMethod.Get, manejador.Peticiones[1].Metodo);
            Assert.AreEqual(new Uri("https://api.telegram.org/file/bot" + Token + "/photos/prueba_1.png"), manejador.Peticiones[1].Uri);
            Assert.IsTrue(manejador.Peticiones.All(p => p.Uri.Scheme == "https" && p.Uri.Host == "api.telegram.org"));
        }

        [TestMethod]
        [DataRow("https://destino-ajeno.invalid/foto.jpg")]
        [DataRow("//destino-ajeno.invalid/foto.jpg")]
        [DataRow("../secretos")]
        [DataRow("photos/../foto.jpg")]
        [DataRow("photos/%2e%2e/foto.jpg")]
        [DataRow("photos/./foto.jpg")]
        [DataRow("photos//foto.jpg")]
        [DataRow("photos\\foto.jpg")]
        [DataRow("photos/foto.jpg?otra=ruta")]
        [DataRow("photos/foto.jpg#fragmento")]
        [DataRow("photos/foto con espacios.jpg")]
        [DataRow("photos/foto_ñ.jpg")]
        [DataRow("/photos/foto.jpg")]
        public async Task RutaMaliciosaEnMetadatosSeRechazaAntesDeDescargar(string ruta)
        {
            var manejador = new VisionSeguraFixture.Manejador((_, _) => Metadatos(ruta));
            using var http = new HttpClient(manejador);
            using var cliente = new ClienteTelegramHttp(Token, http, false);
            var error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.DescargarFotoAsync("foto-ficticia"));
            Assert.HasCount(1, manejador.Peticiones);
            VisionSeguraFixture.SinCredencial(error, Token);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("foto\ncon-control")]
        public async Task ReferenciaInvalidaNoHacePeticiones(string referencia)
        {
            var manejador = new VisionSeguraFixture.Manejador((_, _) => throw new AssertFailedException("No debe consultar getFile."));
            using var http = new HttpClient(manejador);
            using var cliente = new ClienteTelegramHttp(Token, http, false);
            await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.DescargarFotoAsync(referencia));
            Assert.IsEmpty(manejador.Peticiones);
        }

        [TestMethod]
        public async Task TamanoDeclaradoExcesivoNoIniciaLaDescarga()
        {
            var manejador = new VisionSeguraFixture.Manejador((_, _) => Metadatos("photos/foto.jpg", ServicioClasificacionImagen.MaximoBytes + 1));
            using var http = new HttpClient(manejador);
            using var cliente = new ClienteTelegramHttp(Token, http, false);
            await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.DescargarFotoAsync("foto-ficticia"));
            Assert.HasCount(1, manejador.Peticiones);
        }

        [TestMethod]
        public async Task StreamSinContentLengthSeLimitaAunqueMetadatosNoDeclarenTamano()
        {
            var stream = new VisionSeguraFixture.StreamContado(ServicioClasificacionImagen.MaximoBytes + 200_000);
            var contenido = new VisionSeguraFixture.ContenidoSinLongitud(stream);
            Assert.IsNull(contenido.Headers.ContentLength);
            var manejador = new VisionSeguraFixture.Manejador((_, numero) => numero == 1 ? Metadatos("photos/foto.jpg")
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = contenido });
            using var http = new HttpClient(manejador);
            using var cliente = new ClienteTelegramHttp(Token, http, false);
            var error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.DescargarFotoAsync("foto-ficticia"));
            Assert.HasCount(2, manejador.Peticiones);
            Assert.IsTrue(stream.Leidos > ServicioClasificacionImagen.MaximoBytes && stream.Leidos <= ServicioClasificacionImagen.MaximoBytes + 8192);
            VisionSeguraFixture.SinCredencial(error, Token);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Redirect)]
        [DataRow(HttpStatusCode.TemporaryRedirect)]
        [DataRow(HttpStatusCode.Unauthorized)]
        [DataRow(HttpStatusCode.BadGateway)]
        public async Task DescargaConErrorORedireccionNoSigueOtraRutaNiExponeToken(HttpStatusCode estado)
        {
            var manejador = new VisionSeguraFixture.Manejador((_, numero) => numero == 1 ? Metadatos("photos/foto.jpg")
                : new HttpResponseMessage(estado)
                {
                    Headers = { Location = new Uri("https://destino-ajeno.invalid/foto") },
                    Content = new StringContent(Token),
                });
            using var http = new HttpClient(manejador);
            using var cliente = new ClienteTelegramHttp(Token, http, false);
            var error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.DescargarFotoAsync("foto-ficticia"));
            Assert.HasCount(2, manejador.Peticiones);
            VisionSeguraFixture.SinCredencial(error, Token);
        }

        [TestMethod]
        public async Task FalloDeRedEnDescargaNoPropagaUriQueContieneToken()
        {
            var manejador = new VisionSeguraFixture.Manejador((peticion, numero) => numero == 1 ? Metadatos("photos/foto.jpg")
                : throw new HttpRequestException("No se pudo abrir " + peticion.RequestUri));
            using var http = new HttpClient(manejador);
            using var cliente = new ClienteTelegramHttp(Token, http, false);
            var error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.DescargarFotoAsync("foto-ficticia"));
            VisionSeguraFixture.SinCredencial(error, Token);
            Assert.DoesNotContain("api.telegram.org", error.Message);
        }

        [TestMethod]
        public async Task RedireccionDeGetFileNoIniciaDescargaNiSigueLocation()
        {
            var manejador = new VisionSeguraFixture.Manejador((_, _) => new HttpResponseMessage(HttpStatusCode.Redirect)
            {
                Headers = { Location = new Uri("https://destino-ajeno.invalid/metadatos") },
                Content = new StringContent("{\"ok\":true,\"result\":{\"file_path\":\"photos/foto.jpg\"}}"),
            });
            using var http = new HttpClient(manejador);
            using var cliente = new ClienteTelegramHttp(Token, http, false);
            var error = await Assert.ThrowsExactlyAsync<ErrorTelegramException>(() => cliente.DescargarFotoAsync("foto-ficticia"));
            Assert.HasCount(1, manejador.Peticiones);
            VisionSeguraFixture.SinCredencial(error, Token);
        }

        [TestMethod]
        public async Task CancelacionPreviaNoConsultaMetadatosNiDescarga()
        {
            var manejador = new VisionSeguraFixture.Manejador((_, _) => throw new AssertFailedException("No debe salir una petición."));
            using var http = new HttpClient(manejador);
            using var cliente = new ClienteTelegramHttp(Token, http, false);
            using var cancelacion = new CancellationTokenSource();
            cancelacion.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => cliente.DescargarFotoAsync("foto-ficticia", cancelacion.Token));
            Assert.IsEmpty(manejador.Peticiones);
        }

        private static HttpResponseMessage Metadatos(string ruta, int? tamano = null)
        {
            var resultado = new Dictionary<string, object?> { ["file_path"] = ruta };
            if (tamano is not null) resultado["file_size"] = tamano;
            return VisionSeguraFixture.Json(HttpStatusCode.OK, JsonSerializer.Serialize(new { ok = true, result = resultado }));
        }
    }

    internal static class VisionSeguraFixture
    {
        internal const string ComentarioPrivado = "metadato-ficticio-que-no-debe-salir";
        internal static readonly SKColor[] Colores = new SKColor[] { SKColors.Red, SKColors.Lime, SKColors.Blue, SKColors.Yellow };

        internal static byte[] Imagen(int ancho, int alto, SKEncodedImageFormat formato)
        {
            using var bitmap = new SKBitmap(ancho, alto);
            using (var canvas = new SKCanvas(bitmap))
            using (var pintura = new SKPaint())
            {
                for (int i = 0; i < 4; i++)
                {
                    pintura.Color = Colores[i];
                    float x = (i % 2) * ancho / 2f, y = (i / 2) * alto / 2f;
                    canvas.DrawRect(x, y, ancho / 2f, alto / 2f, pintura);
                }
            }
            using var imagen = SKImage.FromBitmap(bitmap);
            using var codificada = imagen.Encode(formato, 100);
            return codificada.ToArray();
        }

        internal static ImagenResiduo Foto() => new(Imagen(80, 40, SKEncodedImageFormat.Png), "image/png");

        internal static byte[] ConExifYComentario(byte[] jpeg, int orientacion)
        {
            byte[] exif = new byte[]
    {
        69,
        120,
        105,
        102,
        0,
        0,
        73,
        73,
        42,
        0,
        8,
        0,
        0,
        0,
        1,
        0,
        0x12,
        0x01,
        3,
        0,
        1,
        0,
        0,
        0,
        (byte)orientacion,
        0,
        0,
        0,
        0,
        0,
        0,
        0
    };
            using var memoria = new MemoryStream();
            memoria.Write(jpeg, 0, 2);
            Segmento(memoria, 0xe1, exif);
            Segmento(memoria, 0xfe, Encoding.ASCII.GetBytes(ComentarioPrivado));
            memoria.Write(jpeg, 2, jpeg.Length - 2);
            return memoria.ToArray();
        }

        private static void Segmento(Stream salida, byte marcador, byte[] datos)
        {
            int longitud = datos.Length + 2;
            salida.Write(new byte[] { 0xff, marcador, (byte)(longitud >> 8), (byte)longitud });
            salida.Write(datos);
        }

        internal static void ColorAproximado(SKColor esperado, SKColor actual) => Assert.IsTrue(
            Math.Abs(esperado.Red - actual.Red) < 55 && Math.Abs(esperado.Green - actual.Green) < 55 && Math.Abs(esperado.Blue - actual.Blue) < 55,
            $"Se esperaba color {esperado}, se obtuvo {actual}.");

        internal static void SinCredencial(Exception error, string secreto)
        {
            Assert.DoesNotContain(secreto, error.Message);
            Assert.IsNull(error.InnerException);
        }

        internal static HttpResponseMessage Json(HttpStatusCode estado, string cuerpo) => new(estado)
        { Content = new StringContent(cuerpo, Encoding.UTF8, "application/json") };

        internal sealed record Peticion(HttpMethod Metodo, Uri Uri, string Cuerpo, string? Esquema, string? Autorizacion);

        internal sealed class Manejador : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, int, HttpResponseMessage> _responder;

            public Manejador(Func<HttpRequestMessage, int, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            internal List<Peticion> Peticiones { get; } = new List<Peticion>();
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string cuerpo = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
                Peticiones.Add(new Peticion(request.Method, request.RequestUri!, cuerpo, request.Headers.Authorization?.Scheme, request.Headers.Authorization?.Parameter));
                return _responder(request, Peticiones.Count);
            }
        }

        internal sealed class ContenidoSinLongitud : HttpContent
        {
            private readonly Stream _stream;

            public ContenidoSinLongitud(Stream stream)
            {
                _stream = stream;
            }

            protected override bool TryComputeLength(out long length) { length = 0; return false; }
            protected override Task SerializeToStreamAsync(Stream destino, TransportContext? context) => _stream.CopyToAsync(destino);
            protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult(_stream);
            protected override void Dispose(bool disposing) { if (disposing) _stream.Dispose(); base.Dispose(disposing); }
        }

        internal sealed class StreamContado : Stream
        {
            private readonly int _longitud;

            public StreamContado(int longitud)
            {
                _longitud = longitud;
            }

            internal int Leidos { get; private set; }
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => Leidos; set => throw new NotSupportedException(); }
            public override int Read(byte[] buffer, int offset, int count)
            {
                int cantidad = Math.Min(count, _longitud - Leidos);
                buffer.AsSpan(offset, cantidad).Clear();
                Leidos += cantidad;
                return cantidad;
            }
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(Read(buffer, offset, count));
            }
            public override void Flush() => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
