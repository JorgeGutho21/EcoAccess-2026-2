using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Vision35.Expo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica.Vision35
{
    /// <summary>
    /// Matriz de 28 pruebas SIMULADAS: las 7 combinaciones de canecas (V, B, N, VB, VN, BN, VBN) por los 4 canales
    /// (ECOACCESS P3 = PC, Telegram, ECOACCESS DOS CLICS / WEB y ECOACCESS ESP32 = panel). Pasan por el coordinador real
    /// <see cref="ServicioEstacionExpo"/>; la estación es un doble que registra la orden y NUNCA toca un puerto serie.
    /// El canal «panel» se prueba con el cliente HTTP real (<see cref="ClienteEstacionPanel"/>) contra un panel falso en
    /// 127.0.0.1 que responde como /api/estacion/abrir-grupo. Ninguna de estas pruebas acredita movimiento físico.
    /// </summary>
    [TestClass]
    public sealed class MatrizCombinacionesCanalesTests
    {
        private static readonly string[] Combinaciones = { "V", "B", "N", "VB", "VN", "BN", "VBN" };

        private static IReadOnlyList<CategoriaMaterial> Canecas(string letras) => letras.Select(c => c switch
        {
            'V' => CategoriaMaterial.OrganicoAprovechable,
            'B' => CategoriaMaterial.Aprovechable,
            _ => CategoriaMaterial.NoAprovechable,
        }).ToList();

        private static int Esperado(string letras) => letras.Length switch { 1 => 7000, 2 => 14000, _ => 21000 };

        /// <summary>Estación doble: acepta, rechaza (control desactivado) o no responde (desconectada).</summary>
        private sealed class EstacionDoble : IEstacionCanecasExpo
        {
            public string Modo = "acepta";
            public readonly List<string> Ordenes = new();
            public bool EsSimulada => true;
            public Task<EstadoEstacionExpo> EstadoAsync(CancellationToken ct = default) =>
                Task.FromResult(new EstadoEstacionExpo(true, Modo != "desconectada", Modo == "acepta", Modo, Modo == "desconectada" ? null : "COM-SIM", "doble"));
            public Task<ResultadoAperturaEstacion> AbrirAsync(string clave, IReadOnlyList<CategoriaMaterial> canecas, string modalidad, string origen, string solicitante, string? materiales, CancellationToken ct = default)
            {
                string letras = ClienteEstacionPanel.Letras(canecas);
                lock (Ordenes) Ordenes.Add(origen + ":" + letras);
                int ms = PoliticaTiemposApertura.DuracionMs(letras.Length);
                return Task.FromResult(Modo switch
                {
                    "acepta" => new ResultadoAperturaEstacion(true, false, true, letras, ms, modalidad, ms, "Simulación (sin movimiento físico)."),
                    "rechaza" => new ResultadoAperturaEstacion(false, false, true, letras, 0, modalidad, 0, "El control físico está desactivado."),
                    _ => new ResultadoAperturaEstacion(false, false, true, letras, 0, modalidad, 0, "No se pudo contactar el panel de la estación (HttpRequestException)."),
                });
            }
        }

        /// <summary>Oracle doble: guarda en memoria lo que se registraría (MERGE por clave).</summary>
        private sealed class OracleDoble : IRegistroOperacionesEstacion
        {
            public readonly Dictionary<string, OperacionEstacion> Filas = new();
            public Task GuardarAsync(OperacionEstacion operacion, CancellationToken ct = default) { lock (Filas) Filas[operacion.Clave] = operacion; return Task.CompletedTask; }
            public Task<IReadOnlyList<OperacionEstacion>> RecientesAsync(int? idUsuario, int maximo, CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<OperacionEstacion>>(Filas.Values.ToList());
        }

        private static (ServicioEstacionExpo Servicio, EstacionDoble Estacion, OracleDoble Oracle, string Carpeta) Crear()
        {
            string carpeta = Path.Combine(Path.GetTempPath(), "ecoaccess-matriz-" + Guid.NewGuid().ToString("N"));
            var estacion = new EstacionDoble();
            var oracle = new OracleDoble();
            return (new ServicioEstacionExpo(estacion, new RegistroOperacionesConCola(oracle, carpeta), "simultanea"), estacion, oracle, carpeta);
        }

        // ---- Canales PC, Telegram y web (DOS CLICS): 7 combinaciones cada uno = 21 pruebas ----

        public static IEnumerable<object[]> CasosCoordinador() =>
            from canal in new[] { CanalEstacion.PC, CanalEstacion.TELEGRAM, CanalEstacion.WEB }
            from c in Combinaciones
            select new object[] { canal, c };

        [TestMethod]
        [DynamicData(nameof(CasosCoordinador), DynamicDataSourceType.Method)]
        public async Task CombinacionPorCanalAbreCierraYRegistraSinRepetir(CanalEstacion canal, string letras)
        {
            var (servicio, estacion, oracle, carpeta) = Crear();
            try
            {
                string clave = $"matriz-{canal}-{letras}";
                int? usuario = canal == CanalEstacion.WEB ? null : 7;
                var r = await servicio.EjecutarAsync(clave, canal, TipoOperacionEstacion.APERTURA_MANUAL, Canecas(letras), usuario, "prueba-matriz");

                Assert.IsTrue(r.AperturaEnviada, $"{canal} {letras}: la orden debía aceptarse");
                Assert.IsTrue(r.Simulada, "la estación de la prueba es simulada");
                Assert.AreEqual(Esperado(letras), r.DuracionMs, "política 7/14/21 s");
                CollectionAssert.AreEqual(new[] { canal.ToString().ToLowerInvariant() + ":" + letras }, estacion.Ordenes, "una sola orden con las canecas exactas");

                OperacionEstacion fila = oracle.Filas[clave];
                Assert.AreEqual(canal, fila.Canal);
                Assert.AreEqual(EstadoOperacionEstacion.EN_EJECUCION, fila.Estado);
                Assert.AreEqual(EstadoHardwareEstacion.SIMULADO, fila.EstadoHardware, "una simulación nunca se registra como PWM confirmado");
                Assert.AreEqual(Esperado(letras), fila.DuracionMs);
                Assert.AreEqual(usuario is null ? "INVITADO" : "REGISTRADO", fila.TipoUsuario);
                Assert.IsFalse(fila.DisposicionDeclarada, "abrir no es depositar");

                // Doble clic o reintento con la misma clave: no se repite la orden.
                var otra = await servicio.EjecutarAsync(clave, canal, TipoOperacionEstacion.APERTURA_MANUAL, Canecas(letras), usuario, "prueba-matriz");
                Assert.IsFalse(otra.AperturaEnviada);
                Assert.IsTrue(otra.Duplicada);
                Assert.AreEqual(1, estacion.Ordenes.Count, "la segunda solicitud no llegó a la estación");

                // Cierre: el temporizador del controlador termina el ciclo; se marca sin declarar depósito.
                Assert.IsTrue(await servicio.MarcarCicloCompletadoAsync(r, canal, TipoOperacionEstacion.APERTURA_MANUAL, usuario, "prueba-matriz", null, false));
                Assert.AreEqual(EstadoOperacionEstacion.CICLO_COMPLETADO, oracle.Filas[clave].Estado);
                Assert.AreEqual(1, oracle.Filas.Count, "MERGE por clave: una sola fila por operación");
            }
            finally { try { Directory.Delete(carpeta, true); } catch (IOException) { } }
        }

        [TestMethod]
        [DataRow("rechaza", EstadoHardwareEstacion.RECHAZADO)]
        [DataRow("desconectada", EstadoHardwareEstacion.SIN_RESPUESTA)]
        public async Task SinControlODesconectadaNoAbreNiRegistraDeposito(string modo, EstadoHardwareEstacion esperado)
        {
            var (servicio, estacion, oracle, carpeta) = Crear();
            try
            {
                estacion.Modo = modo;
                foreach (string letras in Combinaciones)
                {
                    var r = await servicio.EjecutarAsync("fallo-" + modo + letras, CanalEstacion.TELEGRAM, TipoOperacionEstacion.RECONOCIMIENTO_AUTOMATICO, Canecas(letras), null, "prueba");
                    Assert.IsFalse(r.AperturaEnviada, letras);
                    Assert.AreEqual(0, r.DuracionMs, "sin apertura no hay tiempo de apertura");
                    var fila = oracle.Filas["fallo-" + modo + letras];
                    Assert.AreEqual(EstadoOperacionEstacion.ERROR, fila.Estado);
                    Assert.AreEqual(esperado, fila.EstadoHardware);
                    Assert.AreEqual("NINGUNA", fila.Modalidad);
                    Assert.IsFalse(fila.DisposicionDeclarada);
                }
                // Reconexión: la misma estación vuelve a aceptar órdenes nuevas.
                estacion.Modo = "acepta";
                var ok = await servicio.EjecutarAsync("reconectada", CanalEstacion.PC, TipoOperacionEstacion.APERTURA_MANUAL, Canecas("VBN"), 1, "prueba");
                Assert.IsTrue(ok.AperturaEnviada);
                Assert.AreEqual(21000, ok.DuracionMs);
            }
            finally { try { Directory.Delete(carpeta, true); } catch (IOException) { } }
        }

        [TestMethod]
        public async Task SinCanecasNuncaAbre()
        {
            var (servicio, estacion, _, carpeta) = Crear();
            try
            {
                var r = await servicio.EjecutarAsync("vacia", CanalEstacion.WEB, TipoOperacionEstacion.APERTURA_MANUAL, Array.Empty<CategoriaMaterial>(), null, "prueba");
                Assert.IsFalse(r.AperturaEnviada);
                Assert.AreEqual(0, estacion.Ordenes.Count);
            }
            finally { try { Directory.Delete(carpeta, true); } catch (IOException) { } }
        }

        // ---- Canal panel (ECOACCESS ESP32): 7 combinaciones contra un panel HTTP falso = 7 pruebas ----

        [TestMethod]
        [DataRow("V")] [DataRow("B")] [DataRow("N")] [DataRow("VB")] [DataRow("VN")] [DataRow("BN")] [DataRow("VBN")]
        public async Task PanelRecibeGrupoExactoYTiempoDePolitica(string letras)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int puerto = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            using var http = new HttpListener();
            http.Prefixes.Add($"http://127.0.0.1:{puerto}/");
            http.Start();
            string? cuerpoRecibido = null;
            var atender = Task.Run(async () =>
            {
                var ctx = await http.GetContextAsync();
                using (var lector = new StreamReader(ctx.Request.InputStream, Encoding.UTF8)) cuerpoRecibido = await lector.ReadToEndAsync();
                using var doc = JsonDocument.Parse(cuerpoRecibido);
                string c = doc.RootElement.GetProperty("canecas").GetString()!;
                int ms = c.Length <= 1 ? 7000 : c.Length == 2 ? 14000 : 21000;   // igual que DuracionPolitica del panel
                byte[] r = Encoding.UTF8.GetBytes($"{{\"ok\":true,\"duracionMs\":{ms},\"finEstimadoMs\":{ms},\"msg\":\"GRUPO {c} {ms}\"}}");
                ctx.Response.ContentType = "application/json";
                await ctx.Response.OutputStream.WriteAsync(r, 0, r.Length);
                ctx.Response.Close();
            });

            var cliente = new ClienteEstacionPanel($"http://127.0.0.1:{puerto}");
            var res = await cliente.AbrirAsync("panel-" + letras, Canecas(letras), "simultanea", "panel", "operador", null);
            await atender;
            http.Stop();

            Assert.IsTrue(res.Aceptada);
            Assert.AreEqual(letras, res.Letras);
            Assert.AreEqual(Esperado(letras), res.DuracionMs);
            using var enviado = JsonDocument.Parse(cuerpoRecibido!);
            Assert.AreEqual(letras, enviado.RootElement.GetProperty("canecas").GetString(), "el panel recibe exactamente las canecas pedidas");
            Assert.AreEqual("panel-" + letras, enviado.RootElement.GetProperty("clave").GetString(), "la clave viaja para la idempotencia del panel");
        }
    }
}
