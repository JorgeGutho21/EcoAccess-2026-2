using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Bot;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    /// <summary>
    /// Personas distintas al mismo tiempo: chat, página HTTP local y Oracle reales, con los límites de producción. Telegram se
    /// reemplaza por llamadas directas al servicio, así que esto no prueba la red de Telegram ni el túnel público. Si
    /// ECOACCESS_CARGA_SALIDA indica una carpeta, deja allí los tiempos medidos.
    /// </summary>
    [TestClass]
    [TestCategory(Categoria)]
    [TestCategory("CargaUsuarios")]
    [DoNotParallelize]
    public sealed class CargaUsuariosOracleTests : BaseIntegracionOracle
    {
        private const string Clave = "ciudadano-ficticio-2026";
        private static readonly Uri PaginaPublica = new("https://ecoaccess.example/registro/");

        private sealed class Entorno : IDisposable
        {
            private readonly CancellationTokenSource _cancelar = new();
            private readonly Task _servidor;
            public HttpClient Http { get; } = new(new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(60) };
            public string Base { get; }
            public ServicioConversacionTelegram Eco { get; }
            public ServicioVinculacionTelegram Vinculacion { get; }

            public Entorno(FabricaConexionesOracle fabrica)
            {
                var escucha = new TcpListener(IPAddress.Loopback, 0);
                escucha.Start();
                int puerto = ((IPEndPoint)escucha.LocalEndpoint).Port;
                escucha.Stop();
                Base = $"http://127.0.0.1:{puerto}/registro/";
                var usuarios = new RepositorioUsuariosOracle(fabrica);
                var materiales = new RepositorioMaterialesOracle(fabrica);
                var contenedores = new RepositorioContenedoresOracle(fabrica);
                var interacciones = new RepositorioInteraccionesOracle(fabrica);
                Vinculacion = new ServicioVinculacionTelegram(usuarios, new RepositorioVinculosTelegramOracle(fabrica), new GeneradorTokensAleatorio(), TimeProvider.System);
                Eco = new ServicioConversacionTelegram(new RepositorioConversacionesTelegramOracle(fabrica), contenedores, materiales, Vinculacion,
                    new ServicioInteracciones(usuarios, interacciones, contenedores, materiales, TimeProvider.System),
                    new LimitadorUsoEnMemoria(20, TimeSpan.FromMinutes(1)), PaginaPublica, TimeProvider.System,
                    ecopuntos: new ServicioEcopuntos(usuarios, interacciones, materiales, new RepositorioEcopuntosOracle(fabrica), TimeProvider.System),
                    flujoRapido: true);
                var pagina = new PaginaVinculacion(Vinculacion,
                    new ServicioAutenticacion(new RepositorioCredencialesOracle(fabrica), usuarios, new HasherPbkdf2(), TimeProvider.System),
                    new ServicioRegistroCiudadanos(new RepositorioRegistroCiudadanosOracle(fabrica), new HasherPbkdf2(), TimeProvider.System),
                    new LimitadorUsoEnMemoria(120, TimeSpan.FromMinutes(1)), new LimitadorUsoEnMemoria(10, TimeSpan.FromMinutes(1)), TimeProvider.System);
                _servidor = new ServidorRegistro(pagina, Base, _ => { }).EjecutarAsync(_cancelar.Token);
            }

            public void Dispose()
            {
                _cancelar.Cancel();
                try { _servidor.Wait(TimeSpan.FromSeconds(5)); } catch (AggregateException) { }
                Http.Dispose();
            }
        }

        private readonly List<long> _cuentasTelegram = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<long>> _tiempos = new();
        private static long _siguienteUpdate = DateTime.UtcNow.Ticks / 1000;

        // Se anotan ANTES de usarse: si una persona del grupo falla, Task.WhenAll lanza y los ids de las demás nunca
        // llegan a RegistrarUsuario. La limpieza los recupera por correo y borra también los update_id generados.
        private readonly ConcurrentBag<string> _correos = new();
        private readonly ConcurrentBag<long> _updates = new();

        private long NuevaCuentaTelegram()
        {
            long cuenta = 900_000_000_000L + Math.Abs(Guid.NewGuid().GetHashCode() % 1_000_000_000L);
            lock (_cuentasTelegram) _cuentasTelegram.Add(cuenta);
            return cuenta;
        }

        private async Task<T> MedirAsync<T>(string paso, Func<Task<T>> accion)
        {
            var reloj = Stopwatch.StartNew();
            T valor = await accion();
            _tiempos.GetOrAdd(paso, _ => new ConcurrentBag<long>()).Add(reloj.ElapsedMilliseconds);
            return valor;
        }

        private async Task<RespuestaBot> EnviarAsync(Entorno e, long cuenta, string? texto, string? boton = null)
        {
            long update = Interlocked.Increment(ref _siguienteUpdate);
            _updates.Add(update);
            IReadOnlyList<RespuestaBot> r = await MedirAsync("chat", () => e.Eco.ProcesarAsync(
                new MensajeEntrante(update, cuenta, cuenta, true, texto, boton)));
            RespuestaBot respuesta = r.Single();
            Assert.AreEqual(cuenta, respuesta.ChatId, "Una respuesta llegó a otra conversación.");
            return respuesta;
        }

        private static string Token(RespuestaBot enlace)
        {
            string url = Regex.Match(enlace.Texto, @"https://\S+").Value;
            Assert.IsFalse(string.IsNullOrEmpty(url), "No llegó el enlace: " + enlace.Texto);
            return Regex.Match(url, @"[?&]t=([A-Za-z0-9_-]+)").Groups[1].Value;
        }

        private async Task<(string Cookie, string Csrf)> AbrirFormularioAsync(Entorno e, string token, string modo)
        {
            using HttpResponseMessage r = await MedirAsync("abrir_pagina", () => e.Http.GetAsync($"{e.Base}?t={token}&m={modo}"));
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
            string html = await r.Content.ReadAsStringAsync();
            string cookie = r.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
            return (cookie, Regex.Match(html, "name=\"csrf\" value=\"([0-9a-f]{64})\"").Groups[1].Value);
        }

        private async Task<(HttpStatusCode Codigo, string Html)> EnviarFormularioAsync(Entorno e, string paso, string cookie, Dictionary<string, string> campos)
        {
            using var peticion = new HttpRequestMessage(HttpMethod.Post, e.Base) { Content = new FormUrlEncodedContent(campos) };
            peticion.Headers.Add("Cookie", cookie);
            using HttpResponseMessage r = await MedirAsync(paso, () => e.Http.SendAsync(peticion));
            return (r.StatusCode, await r.Content.ReadAsStringAsync());
        }

        private static Dictionary<string, string> Crear(string token, string csrf, string correo) => new()
        {
            ["t"] = token, ["csrf"] = csrf, ["modo"] = "crear", ["nombre"] = "Ciudadano ficticio de carga", ["correo"] = correo,
            ["clave"] = Clave, ["confirmacion"] = Clave, ["acepta"] = "si",
        };

        private static string CodigoDe(string html)
        {
            string codigo = Regex.Match(html, @"/confirmar ([A-Z0-9]{8})").Groups[1].Value;
            Assert.AreEqual(8, codigo.Length, "La página no entregó el código de confirmación.");
            return codigo;
        }

        private string Correo()
        {
            string correo = "it.carga." + Sufijo().ToLowerInvariant() + "@example.invalid";
            _correos.Add(correo);
            return correo;
        }

        /// <summary>Crear cuenta completa: chat, página, Oracle, vuelta al bot y consulta de sus datos.</summary>
        private async Task<int> RecorridoNuevoAsync(Entorno e, long cuenta, string correo)
        {
            Assert.AreEqual(MensajesConversacion.OfertaVinculo, (await EnviarAsync(e, cuenta, "/vincular")).Texto);
            string token = Token(await EnviarAsync(e, cuenta, null, "menu:crear"));
            (string cookie, string csrf) = await AbrirFormularioAsync(e, token, "crear");
            (HttpStatusCode codigo, string html) = await EnviarFormularioAsync(e, "crear_cuenta", cookie, Crear(token, csrf, correo));
            Assert.AreEqual(HttpStatusCode.OK, codigo, html);
            Assert.AreEqual(MensajesConversacion.VinculoListo, (await EnviarAsync(e, cuenta, "/start " + EnlaceTelegram.PrefijoConfirmar + CodigoDe(html))).Texto);
            Assert.AreEqual(MensajesConversacion.Saldo(0), (await EnviarAsync(e, cuenta, "¿Cuántos ecopuntos tengo?")).Texto);
            Assert.AreEqual(MensajesConversacion.SaludoDeNuevo, (await EnviarAsync(e, cuenta, "/start")).Texto);
            return (await e.Vinculacion.ObtenerUsuarioVinculadoAsync(cuenta)) ?? throw new AssertFailedException("El vínculo no quedó en Oracle.");
        }

        [TestCleanup]
        public async Task QuitarDatosDeTelegramAsync()
        {
            if (Fabrica is null) return;
            using OracleConnection conexion = await Fabrica.AbrirAsync(CancellationToken.None);
            foreach (long cuenta in _cuentasTelegram)
                foreach (string tabla in new[] { "vinculacion_pendiente", "token_vinculacion", "vinculo_telegram", "conversacion_telegram" })
                {
                    using OracleCommand comando = ComandosOracle.Crear(conexion, $"DELETE FROM {tabla} WHERE telegram_user_id=:cuenta");
                    comando.Agregar("cuenta", OracleDbType.Int64, cuenta);
                    await comando.ExecuteNonQueryAsync();
                }
            foreach (long update in _updates)
            {
                using OracleCommand comando = ComandosOracle.Crear(conexion, "DELETE FROM update_telegram WHERE update_id=:id");
                comando.Agregar("id", OracleDbType.Int64, update);
                await comando.ExecuteNonQueryAsync();
            }
            // Cuentas creadas por la página aunque el recorrido fallara después: la limpieza base las borra con sus
            // roles y credenciales (MSTest ejecuta esta limpieza antes que la de BaseIntegracionOracle).
            foreach (string correo in _correos)
            {
                using OracleCommand comando = ComandosOracle.Crear(conexion, "SELECT id_usuario FROM usuario WHERE correo_electronico=:correo");
                comando.Agregar("correo", OracleDbType.Varchar2, correo);
                if (await comando.ExecuteScalarAsync() is { } id && id is not DBNull) RegistrarUsuario(Convert.ToInt32(id));
            }
        }

        private void GuardarMetricas(string escenario, int personas, long totalMs, TimeSpan cpu, int errores)
        {
            string? carpeta = Environment.GetEnvironmentVariable("ECOACCESS_CARGA_SALIDA");
            if (string.IsNullOrWhiteSpace(carpeta)) return;
            var pasos = _tiempos.ToDictionary(p => p.Key, p =>
            {
                long[] t = p.Value.OrderBy(x => x).ToArray();
                return new { Operaciones = t.Length, MediaMs = (long)t.Average(), P95Ms = t[Math.Max(0, (int)Math.Ceiling(0.95 * t.Length) - 1)], MaximoMs = t[t.Length - 1] };
            });
            Directory.CreateDirectory(carpeta);
            File.WriteAllText(Path.Combine(carpeta, $"{escenario}_{personas}.json"), JsonSerializer.Serialize(new
            {
                Escenario = escenario, Personas = personas, Errores = errores, TotalMs = totalMs, CpuProcesoMs = (long)cpu.TotalMilliseconds,
                MemoriaPicoMb = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024), Nucleos = Environment.ProcessorCount, Pasos = pasos,
            }, new JsonSerializerOptions { WriteIndented = true }));
        }

        [TestMethod]
        [DataRow(10)]
        [DataRow(20)]
        public async Task UsuariosNuevosSimultaneosQuedanAisladosYSobrevivenAlReinicio(int personas)
        {
            using var e = new Entorno(Fabrica);
            var cuentas = Enumerable.Range(0, personas).Select(_ => NuevaCuentaTelegram()).ToArray();
            var correos = Enumerable.Range(0, personas).Select(_ => Correo()).ToArray();
            TimeSpan cpu = Process.GetCurrentProcess().TotalProcessorTime;
            var reloj = Stopwatch.StartNew();

            int[] ids = await Task.WhenAll(Enumerable.Range(0, personas).Select(i => Task.Run(() => RecorridoNuevoAsync(e, cuentas[i], correos[i]))));

            reloj.Stop();
            foreach (int id in ids) RegistrarUsuario(id);
            Assert.AreEqual(personas, ids.Distinct().Count(), "Dos personas terminaron en la misma cuenta.");
            GuardarMetricas("nuevos", personas, reloj.ElapsedMilliseconds, Process.GetCurrentProcess().TotalProcessorTime - cpu, 0);

            // Cada cuenta corresponde a su correo, y otro proceso que arranque de cero lee lo mismo de Oracle.
            var usuarios = new RepositorioUsuariosOracle(new FabricaConexionesOracle(LeerCadena()!));
            var reinicio = new ServicioVinculacionTelegram(usuarios, new RepositorioVinculosTelegramOracle(new FabricaConexionesOracle(LeerCadena()!)), new GeneradorTokensAleatorio(), TimeProvider.System);
            for (int i = 0; i < personas; i++)
            {
                Assert.AreEqual(ids[i], await reinicio.ObtenerUsuarioVinculadoAsync(cuentas[i]));
                Assert.AreEqual(correos[i], (await usuarios.ObtenerPorIdAsync(ids[i]))!.CorreoElectronico);
            }
        }

        [TestMethod]
        [DataRow(10)]
        [DataRow(20)]
        public async Task CuentasExistentesSeVinculanSimultaneamenteSinDuplicarse(int personas)
        {
            var registro = new ServicioRegistroCiudadanos(new RepositorioRegistroCiudadanosOracle(Fabrica), new HasherPbkdf2(), TimeProvider.System);
            var correos = Enumerable.Range(0, personas).Select(_ => Correo()).ToArray();
            var ids = new int[personas];
            for (int i = 0; i < personas; i++)
            {
                var alta = await registro.RegistrarAsync("Ciudadano ficticio existente", correos[i], Clave, Clave, true);
                Assert.IsTrue(alta.Exitoso, alta.Mensaje);
                ids[i] = RegistrarUsuario(alta.Valor);
            }
            using var e = new Entorno(Fabrica);
            var cuentas = Enumerable.Range(0, personas).Select(_ => NuevaCuentaTelegram()).ToArray();
            TimeSpan cpu = Process.GetCurrentProcess().TotalProcessorTime;
            var reloj = Stopwatch.StartNew();

            await Task.WhenAll(Enumerable.Range(0, personas).Select(i => Task.Run(async () =>
            {
                string token = Token(await EnviarAsync(e, cuentas[i], "Ya tengo cuenta"));
                (string cookie, string csrf) = await AbrirFormularioAsync(e, token, "entrar");
                var (codigo, html) = await EnviarFormularioAsync(e, "iniciar_sesion", cookie, new Dictionary<string, string>
                    { ["t"] = token, ["csrf"] = csrf, ["modo"] = "entrar", ["correo"] = correos[i], ["clave"] = Clave });
                Assert.AreEqual(HttpStatusCode.OK, codigo, html);
                Assert.AreEqual(MensajesConversacion.VinculoListo, (await EnviarAsync(e, cuentas[i], "/confirmar " + CodigoDe(html))).Texto);
            })));

            reloj.Stop();
            GuardarMetricas("existentes", personas, reloj.ElapsedMilliseconds, Process.GetCurrentProcess().TotalProcessorTime - cpu, 0);
            for (int i = 0; i < personas; i++) Assert.AreEqual(ids[i], await e.Vinculacion.ObtenerUsuarioVinculadoAsync(cuentas[i]), "La cuenta quedó ligada a otra persona.");
        }

        [TestMethod]
        public async Task ElMismoCorreoAlMismoTiempoCreaUnaSolaCuenta()
        {
            using var e = new Entorno(Fabrica);
            string correo = Correo();
            var cuentas = Enumerable.Range(0, 5).Select(_ => NuevaCuentaTelegram()).ToArray();

            var respuestas = await Task.WhenAll(cuentas.Select(cuenta => Task.Run(async () =>
            {
                string token = Token(await EnviarAsync(e, cuenta, null, "menu:crear"));
                (string cookie, string csrf) = await AbrirFormularioAsync(e, token, "crear");
                return (await EnviarFormularioAsync(e, "crear_cuenta", cookie, Crear(token, csrf, correo))).Html;
            })));

            Assert.HasCount(1, respuestas.Where(r => r.Contains("/confirmar ")).ToArray(), "Solo una persona puede crear esa cuenta.");
            Assert.AreEqual(1, await ContarAsync($"SELECT COUNT(*) FROM usuario WHERE LOWER(correo_electronico)='{correo}'"));
            using OracleConnection conexion = await Fabrica.AbrirAsync(CancellationToken.None);
            using OracleCommand id = ComandosOracle.Crear(conexion, $"SELECT id_usuario FROM usuario WHERE LOWER(correo_electronico)='{correo}'");
            RegistrarUsuario(ConversionOracle.Entero(await id.ExecuteScalarAsync() ?? throw new InvalidOperationException("Sin fila.")));
        }

        [TestMethod]
        public async Task UnEnlaceUsadoNoSirveOtraVezYElCodigoSoloValeParaSuCuenta()
        {
            using var e = new Entorno(Fabrica);
            long duena = NuevaCuentaTelegram(), ajena = NuevaCuentaTelegram();
            string token = Token(await EnviarAsync(e, duena, null, "menu:crear"));
            (string cookie, string csrf) = await AbrirFormularioAsync(e, token, "crear");
            var campos = Crear(token, csrf, Correo());

            var (_, html) = await EnviarFormularioAsync(e, "crear_cuenta", cookie, campos);
            var repetido = await EnviarFormularioAsync(e, "crear_cuenta", cookie, campos);
            string codigo = CodigoDe(html);
            RespuestaBot intruso = await EnviarAsync(e, ajena, "/confirmar " + codigo);
            RespuestaBot propio = await EnviarAsync(e, duena, "/confirmar " + codigo);
            int id = RegistrarUsuario((await e.Vinculacion.ObtenerUsuarioVinculadoAsync(duena)) ?? throw new AssertFailedException("La dueña no quedó vinculada."));
            using HttpResponseMessage reabrir = await e.Http.GetAsync($"{e.Base}?t={token}");
            RespuestaBot segunda = await EnviarAsync(e, duena, "/confirmar " + codigo);

            Assert.AreEqual(HttpStatusCode.Forbidden, repetido.Codigo, "El formulario se usa una sola vez.");
            Assert.AreNotEqual(MensajesConversacion.VinculoListo, intruso.Texto);
            Assert.IsNull(await e.Vinculacion.ObtenerUsuarioVinculadoAsync(ajena), "Conocer el código no da acceso a la cuenta.");
            Assert.AreEqual(MensajesConversacion.VinculoListo, propio.Texto);
            Assert.AreNotEqual(HttpStatusCode.OK, reabrir.StatusCode, "El enlace no sirve tras vincular.");
            Assert.AreNotEqual(MensajesConversacion.VinculoListo, segunda.Texto);
            Assert.IsTrue(id > 0);
        }

        [TestMethod]
        public async Task UnaCuentaYaVinculadaNoSePasaAOtroTelegramConSoloConocerElCorreo()
        {
            using var e = new Entorno(Fabrica);
            long primera = NuevaCuentaTelegram(), intruso = NuevaCuentaTelegram();
            string correo = Correo();
            RegistrarUsuario(await RecorridoNuevoAsync(e, primera, correo));

            string token = Token(await EnviarAsync(e, intruso, "Ya tengo cuenta"));
            (string cookie, string csrf) = await AbrirFormularioAsync(e, token, "entrar");
            var (_, html) = await EnviarFormularioAsync(e, "iniciar_sesion", cookie, new Dictionary<string, string>
                { ["t"] = token, ["csrf"] = csrf, ["modo"] = "entrar", ["correo"] = correo, ["clave"] = "contrasena-equivocada-2026" });

            Assert.DoesNotContain("/confirmar ", html, "Sin la contraseña no hay código.");
            Assert.IsNull(await e.Vinculacion.ObtenerUsuarioVinculadoAsync(intruso));
        }

        /// <summary>
        /// QR oficial contra la base real (caneca piloto de database/03_datos_prueba/08_piloto_qr_oficial.sql), en modo
        /// software: identifica la estación, no crea solicitudes ni órdenes, y un QR ajeno se rechaza.
        /// </summary>
        [TestMethod]
        public async Task QrOficialIdentificaLaEstacionPilotoSinCrearSolicitudesEnModoSoftware()
        {
            using var e = new Entorno(Fabrica);
            e.Eco.SinAperturaFisica = true;
            Contenedor? piloto = await new RepositorioContenedoresOracle(Fabrica).ObtenerPorIdentificadorQrAsync(EnlaceTelegram.IdentificadorQrOficial);
            if (piloto is null) Assert.Inconclusive("La base conectada no tiene la caneca del QR oficial (ejecuta 08_piloto_qr_oficial.sql).");
            Assert.AreEqual(ServicioCatalogoAmbiental.NombreCanecaPiloto, piloto!.Nombre);
            Assert.IsTrue(piloto.EstaEnServicio);
            int interaccionesAntes = await ContarAsync("SELECT COUNT(*) FROM interaccion");
            int ordenesAntes = await ContarAsync("SELECT COUNT(*) FROM orden_apertura");

            long cuenta = NuevaCuentaTelegram();
            string saludo = (await EnviarAsync(e, cuenta, "/start")).Texto;
            Assert.IsFalse(saludo.Contains(MensajesTelegram.QrDesconocido), saludo);
            string qr = (await EnviarAsync(e, cuenta, "/start " + EnlaceTelegram.IdentificadorQrOficial)).Texto;
            StringAssert.Contains(qr, ServicioCatalogoAmbiental.NombreCanecaPiloto);
            StringAssert.Contains(qr, MensajesTelegram.AperturaPendiente);
            Assert.IsFalse(qr.Contains(MensajesTelegram.QrDesconocido), qr);
            Assert.AreEqual(MensajesTelegram.QrDesconocido, (await EnviarAsync(e, NuevaCuentaTelegram(), "/start QrFalsoDePrueba0000000")).Texto);

            Assert.AreEqual(interaccionesAntes, await ContarAsync("SELECT COUNT(*) FROM interaccion"), "El modo software no crea solicitudes.");
            Assert.AreEqual(ordenesAntes, await ContarAsync("SELECT COUNT(*) FROM orden_apertura"), "El modo software no emite órdenes.");
        }

        [TestMethod]
        public async Task RepetirLaMismaActualizacionNoDuplicaRespuestas()
        {
            using var e = new Entorno(Fabrica);
            long cuenta = NuevaCuentaTelegram();
            long update = Interlocked.Increment(ref _siguienteUpdate);
            _updates.Add(update);
            var mensaje = new MensajeEntrante(update, cuenta, cuenta, true, "/vincular", null);

            var resultados = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Task.Run(() => e.Eco.ProcesarAsync(mensaje))));

            Assert.AreEqual(1, resultados.Count(r => r.Count > 0), "Una actualización repetida se atiende una sola vez.");
        }
    }
}
