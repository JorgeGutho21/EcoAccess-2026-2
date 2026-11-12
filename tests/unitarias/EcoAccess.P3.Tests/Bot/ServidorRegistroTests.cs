using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Sockets;
using System.Text;
using EcoAccess.P3.Bot;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Bot
{

    /// <summary>Levanta el <see cref="ServidorRegistro"/> real en 127.0.0.1 con un puerto libre; no sale a la red.</summary>
    [TestClass]
    public sealed class ServidorRegistroTests
    {
        private const long Telegram = 555_000_333;

        private sealed class Servidor : IAsyncDisposable, IDisposable
        {
            private readonly CancellationTokenSource _cancelacion = new();

            public Servidor(TimeSpan plazoLectura, int simultaneas = ServidorRegistro.SimultaneasPredeterminadas)
            {
                Puerto = PuertosHttpPruebas.Siguiente();
                var usuarios = new RepositorioUsuariosEnMemoria();
                Vinculos = new RepositorioVinculosEnMemoria(usuarios);
                var reloj = new RelojAjustable(new DateTimeOffset(2026, 3, 10, 8, 30, 0, TimeSpan.Zero));
                Vinculacion = new ServicioVinculacionTelegram(usuarios, Vinculos, new GeneradorTokensSecuencial(), reloj);
                var pagina = new PaginaRegistro(Vinculacion, new LimitadorManual(), new LimitadorManual(), reloj);
                Ejecucion = new ServidorRegistro(pagina, $"http://127.0.0.1:{Puerto}/registro/", t => Registro.Add(t), plazoLectura, simultaneas)
                    .EjecutarAsync(_cancelacion.Token);
            }

            public int Puerto { get; }

            public Task Ejecucion { get; }

            public RepositorioVinculosEnMemoria Vinculos { get; }

            public ServicioVinculacionTelegram Vinculacion { get; }

            public System.Collections.Concurrent.ConcurrentBag<string> Registro { get; } = new System.Collections.Concurrent.ConcurrentBag<string>();

            public Uri Ruta(string consulta = "") => new($"http://127.0.0.1:{Puerto}/registro/{consulta}");

            public async Task<string> TokenAsync() => (await Vinculacion.SolicitarAsync(Telegram, Telegram)).Valor!.Token;

            public void Cancelar() => _cancelacion.Cancel();

            public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();



            public async ValueTask DisposeAsync()
            {
                _cancelacion.Cancel();
                await Ejecucion.WaitAsync(TimeSpan.FromSeconds(10));
                _cancelacion.Dispose();
            }

        }

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

        [TestMethod]
        public async Task Get_ConToken_DevuelveFormularioHtmlConEncabezadosDeSeguridad()
        {
            using var servidor = new Servidor(TimeSpan.FromSeconds(5));
            string token = await servidor.TokenAsync();

            using HttpResponseMessage r = await Http.GetAsync(servidor.Ruta($"?t={token}"));

            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
            Assert.AreEqual("text/html", r.Content.Headers.ContentType?.MediaType);
            Assert.AreEqual("utf-8", r.Content.Headers.ContentType?.CharSet);
            foreach ((string nombre, string valor) in PaginaRegistro.Encabezados)
            {
                Assert.IsTrue(r.Headers.TryGetValues(nombre, out var valores) || r.Content.Headers.TryGetValues(nombre, out valores), nombre);
                Assert.AreEqual(valor, string.Join(", ", valores!), nombre);
            }

            StringAssert.Contains(await r.Content.ReadAsStringAsync(), "Crea tu cuenta de EcoAccess");
        }

        [TestMethod]
        public async Task Post_Formulario_CreaLaCuentaYElSegundoEnvioDevuelve410()
        {
            using var servidor = new Servidor(TimeSpan.FromSeconds(5));
            string token = await servidor.TokenAsync();
            var campos = new Dictionary<string, string> { ["t"] = token, ["nombre"] = "Ana Pérez", ["correo"] = "ana@ejemplo.com", ["acepta"] = "si" };

            using (HttpResponseMessage r = await Http.PostAsync(servidor.Ruta(), new FormUrlEncodedContent(campos)))
            {
                Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
                StringAssert.Contains(await r.Content.ReadAsStringAsync(), "vinculada");
            }

            using (HttpResponseMessage r = await Http.PostAsync(servidor.Ruta(), new FormUrlEncodedContent(campos)))
            {
                Assert.AreEqual(HttpStatusCode.Gone, r.StatusCode);
            }

            Assert.HasCount(1, servidor.Vinculos.Vinculos);
        }

        /// <summary>
        /// Regresión del 408 de la serie paralela completa (11/11/2026). El cliente de pruebas de .NET enviaba
        /// «Expect: 100-continue» y esperaba antes de mandar el cuerpo; el servidor tenía hilos libres. El servidor debe
        /// seguir atendiendo a un cliente que sí use esa cabecera, y responder con un cliente que no la use (como los
        /// navegadores).
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Post_ConYSinExpect100Continue_CreaLaCuenta(bool expect)
        {
            using var servidor = new Servidor(TimeSpan.FromSeconds(5));
            string token = await servidor.TokenAsync();
            var campos = new Dictionary<string, string> { ["t"] = token, ["nombre"] = "Ana Pérez", ["correo"] = "ana@ejemplo.com", ["acepta"] = "si" };
            using var peticion = new HttpRequestMessage(HttpMethod.Post, servidor.Ruta()) { Content = new FormUrlEncodedContent(campos) };
            peticion.Headers.ExpectContinue = expect;

            using HttpResponseMessage r = await Http.SendAsync(peticion);

            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
            Assert.HasCount(1, servidor.Vinculos.Vinculos);
        }

        [TestMethod]
        public async Task OtroMetodo_Devuelve405ConAllow()
        {
            using var servidor = new Servidor(TimeSpan.FromSeconds(5));

            using HttpResponseMessage r = await Http.SendAsync(new HttpRequestMessage(HttpMethod.Put, servidor.Ruta()));

            Assert.AreEqual(HttpStatusCode.MethodNotAllowed, r.StatusCode);
            Assert.AreEqual("GET, POST", r.Content.Headers.Allow.Count > 0 ? string.Join(", ", r.Content.Headers.Allow) : r.Headers.GetValues("Allow").Single());
            Assert.IsTrue(r.Headers.Contains("X-Frame-Options"), "Hasta los rechazos llevan los encabezados.");
        }

        [TestMethod]
        [DataRow("admin")]
        [DataRow("eventos/")]
        [DataRow("comandos/")]
        [DataRow("secrets.json")]
        public async Task SubrutaAjenaNoMuestraFormularioNiConsultaElToken(string subruta)
        {
            using var servidor = new Servidor(TimeSpan.FromSeconds(5));
            servidor.Vinculos.AntesDeLeerTokenAsync = _ => throw new InvalidOperationException("No debe consultar Oracle para otra ruta.");
            using var respuesta = await Http.GetAsync(new Uri(servidor.Ruta(), subruta + "?t=token-ficticio-largo-12345"));
            Assert.AreEqual(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.AreEqual(string.Empty, await respuesta.Content.ReadAsStringAsync());
            Assert.IsTrue(respuesta.Headers.Contains("X-Frame-Options"));
        }

        [TestMethod]
        public async Task CuerpoAnunciadoDemasiadoGrande_Devuelve413SinLeerlo()
        {
            using var servidor = new Servidor(TimeSpan.FromSeconds(5));
            var contenido = new ByteArrayContent(new byte[PaginaRegistro.TamanoMaximo + 1]);
            contenido.Headers.ContentType = new("application/x-www-form-urlencoded");

            using HttpResponseMessage r = await Http.PostAsync(servidor.Ruta(), contenido);

            Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, r.StatusCode);
            Assert.AreEqual(string.Empty, await r.Content.ReadAsStringAsync(), "El atajo responde sin pasar por la página.");
        }

        [TestMethod]
        public async Task CuerpoPorTrozosDemasiadoGrande_Devuelve413()
        {
            using var servidor = new Servidor(TimeSpan.FromSeconds(5));
            var contenido = new ByteArrayContent(new byte[PaginaRegistro.TamanoMaximo * 3]);
            contenido.Headers.ContentType = new("application/x-www-form-urlencoded");
            using var peticion = new HttpRequestMessage(HttpMethod.Post, servidor.Ruta()) { Content = contenido };
            peticion.Headers.TransferEncodingChunked = true;

            using HttpResponseMessage r = await Http.SendAsync(peticion);

            Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, r.StatusCode);
            StringAssert.Contains(await r.Content.ReadAsStringAsync(), "<!doctype html>", "Lo rechaza la página tras leer hasta el tope.");
        }

        /// <summary>Abre una conexión que anuncia 100 bytes de formulario y no envía ninguno.</summary>
        private static async Task<TcpClient> ClienteLentoAsync(int puerto)
        {
            var lento = new TcpClient();
            await lento.ConnectAsync(IPAddress.Loopback, puerto);
            string cabecera = $"POST /registro/ HTTP/1.1\r\nHost: 127.0.0.1:{puerto}\r\n" +
                              "Content-Type: application/x-www-form-urlencoded\r\nContent-Length: 100\r\n\r\n";
            await lento.GetStream().WriteAsync(Encoding.ASCII.GetBytes(cabecera));
            return lento;
        }

        [TestMethod]
        public async Task ClienteLento_NoRetieneAOtrasPeticiones()
        {
            using var servidor = new Servidor(TimeSpan.FromSeconds(8));
            using TcpClient lento = await ClienteLentoAsync(servidor.Puerto);

            var reloj = System.Diagnostics.Stopwatch.StartNew();
            using HttpResponseMessage r = await Http.GetAsync(servidor.Ruta("?t=token-que-no-existe-0000"));

            Assert.AreEqual(HttpStatusCode.NotFound, r.StatusCode);
            Assert.IsLessThan(TimeSpan.FromSeconds(4), reloj.Elapsed, "La respuesta no esperó al plazo del cliente lento.");
        }

        [TestMethod]
        public async Task CupoLleno_Devuelve503ConRetryAfterYLuegoSeRecupera()
        {
            using var servidor = new Servidor(TimeSpan.FromSeconds(5), simultaneas: 1);
            var dentro = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var liberar = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            servidor.Vinculos.AntesDeLeerTokenAsync = async ct =>
            {
                dentro.TrySetResult(true);
                await liberar.Task.WaitAsync(ct);
            };
            Task<HttpResponseMessage> primera = Http.GetAsync(servidor.Ruta("?t=token-que-no-existe-0000"));
            try
            {
                // La primera petición ya ocupa el cupo; no depender del reloj ni del orden de TCP.
                await dentro.Task.WaitAsync(TimeSpan.FromSeconds(5));
                using HttpResponseMessage r = await Http.GetAsync(servidor.Ruta("?t=token-que-no-existe-0000"));
                Assert.AreEqual(HttpStatusCode.ServiceUnavailable, r.StatusCode, "Con el único cupo ocupado se responde 503.");
                Assert.AreEqual("5", r.Headers.GetValues("Retry-After").Single());
                Assert.IsTrue(r.Headers.Contains("X-Frame-Options"));
            }
            finally { liberar.TrySetResult(true); }
            using HttpResponseMessage completada = await primera;
            Assert.AreEqual(HttpStatusCode.NotFound, completada.StatusCode);
            servidor.Vinculos.AntesDeLeerTokenAsync = null;
            using var plazoRecuperacion = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            HttpStatusCode recuperado;
            do
            {
                using HttpResponseMessage despues = await Http.GetAsync(servidor.Ruta("?t=token-que-no-existe-0000"), plazoRecuperacion.Token);
                recuperado = despues.StatusCode;
                if (recuperado == HttpStatusCode.ServiceUnavailable) await Task.Delay(10, plazoRecuperacion.Token);
            } while (recuperado == HttpStatusCode.ServiceUnavailable);
            Assert.AreEqual(HttpStatusCode.NotFound, recuperado, "Al completar la primera petición se libera el cupo.");
        }

        [TestMethod]
        public async Task ClienteLento_Recibe408YElServidorSigueAtendiendo()
        {
            using var servidor = new Servidor(TimeSpan.FromMilliseconds(300));

            // Anuncia 100 bytes y no envía ninguno.
            using (var lento = new TcpClient())
            {
                await lento.ConnectAsync(IPAddress.Loopback, servidor.Puerto);
                NetworkStream flujo = lento.GetStream();
                string cabecera = $"POST /registro/ HTTP/1.1\r\nHost: 127.0.0.1:{servidor.Puerto}\r\n" +
                                  "Content-Type: application/x-www-form-urlencoded\r\nContent-Length: 100\r\n\r\n";
                await flujo.WriteAsync(Encoding.ASCII.GetBytes(cabecera));
                using var lector = new StreamReader(flujo, Encoding.ASCII);
                string? estado = await lector.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));
                StringAssert.Contains(estado, " 408 ");
            }

            using HttpResponseMessage r = await Http.GetAsync(servidor.Ruta("?t=token-que-no-existe-0000"));
            Assert.AreEqual(HttpStatusCode.NotFound, r.StatusCode);
        }

        [TestMethod]
        public async Task Cancelacion_TerminaLaEjecucion()
        {
            using var servidor = new Servidor(TimeSpan.FromSeconds(5));
            using (HttpResponseMessage r = await Http.GetAsync(servidor.Ruta()))
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, r.StatusCode);
            }

            servidor.Cancelar();

            await servidor.Ejecucion.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.AreEqual(TaskStatus.RanToCompletion, servidor.Ejecucion.Status);
            Assert.Contains(t => t.StartsWith("Página de registro escuchando", StringComparison.Ordinal), servidor.Registro);
        }

        [TestMethod]
        public void Constructor_ValidaArgumentos()
        {
            var usuarios = new RepositorioUsuariosEnMemoria();
            var pagina = new PaginaRegistro(
                new ServicioVinculacionTelegram(usuarios, new RepositorioVinculosEnMemoria(usuarios), new GeneradorTokensSecuencial(), TimeProvider.System),
                new LimitadorManual(), new LimitadorManual(), TimeProvider.System);

            Assert.ThrowsExactly<ArgumentNullException>(() => new ServidorRegistro((PaginaRegistro)null!, "http://127.0.0.1:1/x/", _ => { }));
            Assert.ThrowsExactly<ArgumentException>(() => new ServidorRegistro(pagina, " ", _ => { }));
            Assert.ThrowsExactly<ArgumentException>(() => new ServidorRegistro(pagina, "http://0.0.0.0:1/x/", _ => { }));
            _ = new ServidorRegistro(pagina, "http://+:8081/registro/", _ => { });
            Assert.ThrowsExactly<ArgumentNullException>(() => new ServidorRegistro(pagina, "http://127.0.0.1:1/x/", null!));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new ServidorRegistro(pagina, "http://127.0.0.1:1/x/", _ => { }, TimeSpan.Zero));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new ServidorRegistro(pagina, "http://127.0.0.1:1/x/", _ => { }, TimeSpan.FromSeconds(1), 0));
        }

        /// <summary>El servidor asegura hilos de trabajo y de E/S para su cupo y nunca baja los mínimos.</summary>
        [TestMethod]
        [DoNotParallelize]
        public void AsegurarHilos_SubeLosMinimosDeTrabajoYDeEntradaSalidaSinBajarlos()
        {
            ServidorRegistro.AsegurarHilos(ServidorRegistro.SimultaneasPredeterminadas);
            ThreadPool.GetMinThreads(out int trabajo, out int entradaSalida);
            Assert.IsGreaterThanOrEqualTo(ServidorRegistro.SimultaneasPredeterminadas, trabajo);
            Assert.IsGreaterThanOrEqualTo(ServidorRegistro.SimultaneasPredeterminadas, entradaSalida);

            ServidorRegistro.AsegurarHilos(1);
            ThreadPool.GetMinThreads(out int trabajoDespues, out int entradaSalidaDespues);
            Assert.AreEqual(trabajo, trabajoDespues, "Nunca baja el mínimo de hilos de trabajo.");
            Assert.AreEqual(entradaSalida, entradaSalidaDespues, "Nunca baja el mínimo de hilos de E/S.");
        }

        [TestMethod]
        [DoNotParallelize]
        public async Task Post_ConElPoolOcupadoPorRegistrosQueBloquean_NoRespondeTiempoAgotado()
        {
            using var servidor = new Servidor(TimeSpan.FromSeconds(3));
            using var liberar = new ManualResetEventSlim(false);
            Task[] bloqueos = Enumerable.Range(0, 40).Select(_ => Task.Run(() => liberar.Wait(TimeSpan.FromSeconds(4)))).ToArray();
            try
            {
                using var formulario = new FormUrlEncodedContent(new Dictionary<string, string> { ["t"] = "x" });
                using HttpResponseMessage r = await Http.PostAsync(servidor.Ruta(), formulario);
                Assert.AreNotEqual(HttpStatusCode.RequestTimeout, r.StatusCode, string.Join(" | ", servidor.Registro));
            }
            finally
            {
                liberar.Set();
                await Task.WhenAll(bloqueos);
            }
        }
    }
}
