using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using EcoAccess.P3.Bot.Dispositivos;
using EcoAccess.P3.Bot;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    [TestClass, TestCategory(Categoria), DoNotParallelize]
    public sealed partial class BridgeOracleTests : BaseIntegracionOracle
    {
        private readonly string _marca = Guid.NewGuid().ToString("N");
        private int _ventanaSimulada;
        private readonly List<Hijo> _hijos = new List<Hijo>();
        private readonly List<string> _registros = new List<string>();
        private string? _estado;
        private string? _cadena;
        private string? _clave;
        private int _deposito;
        private int _regla;
        private long _telegramDemo;
        private readonly List<long> _updatesDemo = new List<long>();
        private sealed record Caso(ConfiguracionBridge Config, RepositorioInteraccionesOracle Repo,
            ServicioInteracciones Servicio, int Ciudadano, int Operario, int Admin, int Material);

        [TestMethod]
        public async Task RecorridoTelegramManualOracleBridgeSimulador_DevuelveDepositoSinDuplicarlo()
        {
            var e = await PrepararAsync();
            _telegramDemo = 8_000_000_000L + Compat.RandomGetInt32(1, int.MaxValue);
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            var vincular = new ServicioVinculacionTelegram(usuarios, new RepositorioVinculosTelegramOracle(Fabrica), new GeneradorTokensAleatorio(), TimeProvider.System);
            string token = (await vincular.SolicitarAsync(_telegramDemo, _telegramDemo)).Valor!.Token;
            string codigo = (await vincular.PrepararVinculacionCuentaExistenteAsync(token, e.Ciudadano)).Valor!.Codigo;
            Assert.IsTrue((await vincular.ConfirmarVinculacionAsync(_telegramDemo, _telegramDemo, codigo)).Exitoso);
            var contenedores = new RepositorioContenedoresOracle(Fabrica);
            var conversacion = new ServicioConversacionTelegram(new RepositorioConversacionesTelegramOracle(Fabrica), contenedores,
                new RepositorioMaterialesOracle(Fabrica), vincular, e.Servicio, new LimitadorUsoEnMemoria(30, TimeSpan.FromMinutes(1)), null, TimeProvider.System);
            var telegram = new ClienteTelegramSimulado();
            var procesador = new ProcesadorUpdates(conversacion.ProcesarAsync, telegram, _registros.Add);
            long siguiente = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 + Compat.RandomGetInt32(1, 500);
            async Task MensajeAsync(string? texto = null, string? boton = null)
            {
                long id = Interlocked.Increment(ref siguiente); lock (_updatesDemo) _updatesDemo.Add(id);
                string json = boton is null
                    ? JsonSerializer.Serialize(new { update_id = id, message = new { from = new { id = _telegramDemo }, chat = new { id = _telegramDemo, type = "private" }, text = texto } })
                    : JsonSerializer.Serialize(new { update_id = id, callback_query = new { id = "cb-" + id, from = new { id = _telegramDemo }, message = new { chat = new { id = _telegramDemo, type = "private" } }, data = boton } });
                using var doc = JsonDocument.Parse(json);
                await procesador.ProcesarAsync(TraductorUpdates.Traducir(doc.RootElement), CancellationToken.None);
            }
            Hijo bridge = Iniciar(e, "--bridge"), sim = Iniciar(e, "--dispositivo-simulado");
            await ListoAsync(bridge, e.Config.Eventos); await ListoAsync(sim, e.Config.Comandos);
            var caneca = (await contenedores.ObtenerPorIdAsync(e.Config.IdContenedor))!;
            await MensajeAsync("/start " + caneca.IdentificadorQr);
            await MensajeAsync("/manual");
            StringAssert.Contains(telegram.Enviadas.Last().Texto, "no es reconocimiento visual");
            await MensajeAsync(boton: "mat:" + e.Material);
            await Task.WhenAll(MensajeAsync(boton: "conf:si"), MensajeAsync(boton: "conf:si"));
            var estado = (await new RepositorioConversacionesTelegramOracle(Fabrica).ObtenerAsync(_telegramDemo))!;
            int orden;
            using (var conexion = await Fabrica.AbrirAsync(CancellationToken.None))
            using (var comando = ComandosOracle.Crear(conexion, "SELECT COUNT(*), MAX(id_orden) FROM orden_apertura WHERE id_interaccion=:id"))
            {
                comando.Agregar("id", OracleDbType.Int32, estado.IdInteraccion!.Value);
                using var lector = await comando.ExecuteReaderAsync(); await lector.ReadAsync();
                Assert.AreEqual(1, Convert.ToInt32(lector.GetValue(0)));
                orden = Convert.ToInt32(lector.GetValue(1));
            }
            await EsperarAsync(async () => (await e.Repo.ObtenerOrdenAsync(orden))?.Estado == EstadoOrdenApertura.Cerrada, bridge, sim);
            var deposito = (await e.Repo.ObtenerDepositoPorOrdenAsync(orden))!; _deposito = deposito.Id;
            Assert.AreEqual(1, await CantidadDepositosAsync(orden));
            Assert.IsFalse(deposito.EstaVerificado); Assert.IsNull(deposito.PesoMedidoKg);
            await MensajeAsync("/estado");
            StringAssert.Contains(telegram.Enviadas.Last().Texto, "material pendiente de verificación");
        }

        [TestMethod]
        public async Task DosProcesosHttpRegistranDepositoReinicianYNoDuplicanEcopuntos()
        {
            var e = await PrepararAsync();
            Hijo bridge = Iniciar(e, "--bridge"), sim = Iniciar(e, "--dispositivo-simulado");
            await ListoAsync(bridge, e.Config.Eventos); await ListoAsync(sim, e.Config.Comandos);
            Hijo duplicado = Iniciar(e, "--dispositivo-simulado");
            await duplicado.Proceso.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.AreEqual(1, duplicado.Proceso.ExitCode, "Una segunda instancia no obtiene el mismo diario."); await CerrarAsync(duplicado);
            int orden = await OrdenAsync(e);
            await EsperarAsync(async () => (await e.Repo.ObtenerOrdenAsync(orden))?.Estado == EstadoOrdenApertura.Cerrada, bridge, sim);
            var deposito = (await e.Repo.ObtenerDepositoPorOrdenAsync(orden))!; _deposito = deposito.Id;
            Assert.IsNull(deposito.IdMaterialVerificado); Assert.IsNull(deposito.PesoMedidoKg); Assert.IsNull(deposito.PesoEstimadoKg);
            await EsperarAsync(() => Task.FromResult(!Directory.EnumerateFiles(Path.Combine(_estado!, "simulador", "eventos"), "*.json").Any()), bridge, sim);

            // Reinicio abrupto de nuestros dos procesos, conservando el mismo diario y binding.
            await CerrarAsync(sim); await CerrarAsync(bridge);
            bridge = Iniciar(e, "--bridge"); sim = Iniciar(e, "--dispositivo-simulado");
            await ListoAsync(bridge, e.Config.Eventos); await ListoAsync(sim, e.Config.Comandos);
            using var c = new CredencialDispositivo(e.Config.Dispositivo, 1, e.Config.IdContenedor, Convert.FromBase64String(_clave!));
            var p = new ProtocoloDispositivoHmac(TimeProvider.System, (id, v) => id == c.Dispositivo && v == 1 ? c : null);
            var original = (await e.Repo.ObtenerOrdenAsync(orden))!;
            // La guardia de hardware solo deja enviar comandos a un hijo propio en modo --dispositivo-simulado.
            GuardiaHardware.AutorizarSimuladorHijo(sim.Proceso, e.Config.Comandos.Port);
            using var canal = new CanalDispositivoHttp(e.Config.Comandos, p, c);
            Assert.IsTrue(await canal.EnviarAperturaAsync(new(original.Id, original.IdContenedor, original.ClaveIdempotencia, original.FechaVencimientoUtc)),
                "La recepción solo guarda bytes: la vigencia remota impedirá actuar sobre la orden cerrada.");
            await EsperarAsync(() => Task.FromResult(Directory.Exists(Path.Combine(_estado!, "simulador", "comandos", "requiere-revision"))
                && Directory.EnumerateFiles(Path.Combine(_estado!, "simulador", "comandos", "requiere-revision")).Any()), bridge, sim);
            Assert.HasCount(2, Directory.GetFiles(Path.Combine(_estado!, "simulador", "reservas")));
            using var eventos = new CanalDispositivoHttp(e.Config.Eventos, p, c);
            var repetido = new EventoRecibido(e.Config.IdContenedor, $"sim-{e.Config.IdContenedor}-{orden}-1", TipoEventoDispositivo.Acuse, orden, DateTime.UtcNow);
            // Usar la fecha exacta del evento persistido: un mismo ID con otro contenido no es un reintento.
            using (var conexion = await Fabrica.AbrirAsync(CancellationToken.None))
            using (var cmd = ComandosOracle.Crear(conexion, "SELECT fecha_dispositivo_utc FROM evento_dispositivo WHERE id_contenedor=:c AND id_externo=:e"))
            {
                cmd.Agregar("c", OracleDbType.Int32, e.Config.IdContenedor); cmd.Agregar("e", OracleDbType.Varchar2, repetido.IdExterno);
                repetido = repetido with { FechaDispositivoUtc = DateTime.SpecifyKind(Convert.ToDateTime(await cmd.ExecuteScalarAsync()), DateTimeKind.Utc) };
            }
            var recibo = await eventos.EnviarPaqueteAsync(p.FirmarEvento(c, repetido));
            Assert.IsTrue(recibo.Exitoso && recibo.Valor.Aceptado); Assert.IsTrue(recibo.Valor.Registro!.Repetido);
            byte[] alterado = p.FirmarEvento(c, repetido); alterado[^3] ^= 1;
            Assert.IsFalse((await eventos.EnviarPaqueteAsync(alterado)).Exitoso);
            using (var desconocido = new CredencialDispositivo("desconocido", 1, e.Config.IdContenedor, Convert.FromBase64String(_clave!)))
            {
                var otroProtocolo = new ProtocoloDispositivoHmac(TimeProvider.System, (id, v) => id == desconocido.Dispositivo && v == 1 ? desconocido : null);
                Assert.IsFalse((await eventos.EnviarPaqueteAsync(otroProtocolo.FirmarEvento(desconocido, repetido))).Exitoso);
            }
            Assert.AreEqual(_deposito, (await new RepositorioInteraccionesOracle(Fabrica).ObtenerDepositoPorOrdenAsync(orden))!.Id);
            Assert.AreEqual(1, await CantidadDepositosAsync(orden));

            var puntos = new ServicioEcopuntos(new RepositorioUsuariosOracle(Fabrica), e.Repo, new RepositorioMaterialesOracle(Fabrica),
                new RepositorioEcopuntosOracle(Fabrica), TimeProvider.System);
            var regla = await puntos.RegistrarReglaAsync(e.Admin, e.Material, 10, "Tarifa ficticia de integración bridge", DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddHours(1));
            Assert.IsTrue(regla.Exitoso, regla.Mensaje); _regla = regla.Valor;
            string clave = "bridge-" + _marca;
            Assert.IsFalse((await puntos.AcreditarAsync(e.Operario, _deposito, _regla, clave)).Exitoso, "Un sensor no verifica el material.");
            Assert.IsTrue((await e.Servicio.VerificarDepositoAsync(e.Operario, _deposito, e.Material)).Exitoso);
            var credito = await puntos.AcreditarAsync(e.Operario, _deposito, _regla, clave);
            Assert.IsTrue(credito.Exitoso, credito.Mensaje);
            Assert.AreEqual(credito.Valor.Single().Id, (await puntos.AcreditarAsync(e.Operario, _deposito, _regla, clave)).Valor.Single().Id);
            Assert.IsFalse((await puntos.AcreditarAsync(e.Operario, _deposito, _regla, "otra-" + _marca)).Exitoso);
            var saldo = await new RepositorioEcopuntosOracle(Fabrica).ConsultarAsync(e.Ciudadano);
            Assert.AreEqual(10L, saldo.Saldo); Assert.HasCount(1, saldo.Movimientos);
        }

        [TestMethod]
        public async Task OracleInaccesibleAntesDeReservarConservaEntradaYRecuperaLaMismaOrden()
        {
            var e = await PrepararAsync(); int orden = await OrdenAsync(e);
            var o = (await e.Repo.ObtenerOrdenAsync(orden))!;
            using var c = new CredencialDispositivo(e.Config.Dispositivo, 1, e.Config.IdContenedor, Convert.FromBase64String(_clave!));
            var p = new ProtocoloDispositivoHmac(TimeProvider.System, (id, v) => id == c.Dispositivo && v == 1 ? c : null);
            var entrada = new ColaPaquetesDispositivo(Path.Combine(_estado!, "simulador", "comandos"));
            entrada.Agregar(p.FirmarComando(c, new(o.Id, o.IdContenedor, o.ClaveIdempotencia, o.FechaVencimientoUtc)));
            var rota = new OracleConnectionStringBuilder(_cadena!) { DataSource = "localhost:1/FREEPDB1", Pooling = false };
            rota["Connection Timeout"] = 1;
            Hijo caido = Iniciar(e, "--bridge", rota.ConnectionString), sim = Iniciar(e, "--dispositivo-simulado");
            await ListoAsync(caido, e.Config.Eventos); await ListoAsync(sim, e.Config.Comandos); await Task.Delay(2500);
            Assert.AreEqual(1, entrada.Pendientes);
            Assert.IsEmpty(Directory.GetFiles(Path.Combine(_estado!, "simulador", "reservas")));
            Assert.IsNull(await e.Repo.ObtenerDepositoPorOrdenAsync(orden));
            await CerrarAsync(caido);
            Hijo bridge = Iniciar(e, "--bridge");
            await ListoAsync(bridge, e.Config.Eventos);
            await EsperarAsync(async () => (await e.Repo.ObtenerOrdenAsync(orden))?.Estado == EstadoOrdenApertura.Cerrada, bridge, sim);
            Assert.IsNotNull(await e.Repo.ObtenerDepositoPorOrdenAsync(orden));
            Assert.AreEqual(1, await CantidadDepositosAsync(orden));
            Assert.HasCount(2, Directory.GetFiles(Path.Combine(_estado!, "simulador", "reservas")), "Una marca de orden y una de nonce; no se repite la actuación.");
        }

        [TestMethod]
        public async Task CiudadanoRevocadoEnOracleNoActuaAunqueElComandoYaEsteGuardado()
        {
            var e = await PrepararAsync(); int orden = await OrdenAsync(e); var o = (await e.Repo.ObtenerOrdenAsync(orden))!;
            using var c = new CredencialDispositivo(e.Config.Dispositivo, 1, e.Config.IdContenedor, Convert.FromBase64String(_clave!));
            var p = new ProtocoloDispositivoHmac(TimeProvider.System, (id, v) => id == c.Dispositivo && v == 1 ? c : null);
            var entradas = new ColaPaquetesDispositivo(Path.Combine(_estado!, "simulador", "comandos"));
            entradas.Agregar(p.FirmarComando(c, new(o.Id, o.IdContenedor, o.ClaveIdempotencia, o.FechaVencimientoUtc)));
            // Se modifica exclusivamente el ciudadano ficticio creado por esta prueba.
            using (var conexion = await Fabrica.AbrirAsync(CancellationToken.None))
            using (var cmd = ComandosOracle.Crear(conexion, "UPDATE usuario SET activo=0 WHERE id_usuario=:id"))
            { cmd.Agregar("id", OracleDbType.Int32, e.Ciudadano); Assert.AreEqual(1, await cmd.ExecuteNonQueryAsync()); }
            Hijo bridge = Iniciar(e, "--bridge"), sim = Iniciar(e, "--dispositivo-simulado");
            await ListoAsync(bridge, e.Config.Eventos); await ListoAsync(sim, e.Config.Comandos);
            await EsperarAsync(() => Task.FromResult(entradas.EnRevision == 1), bridge, sim);
            Assert.AreEqual(0, entradas.Pendientes);
            Assert.IsEmpty(Directory.GetFiles(Path.Combine(_estado!, "simulador", "reservas")));
            Assert.IsNull(await e.Repo.ObtenerDepositoPorOrdenAsync(orden));
        }

        [TestMethod]
        public async Task RevisionLlenaNoReservaNiActuaSobreUnComandoPreviamenteEncolado()
        {
            var e = await PrepararAsync(); int orden = await OrdenAsync(e); var o = (await e.Repo.ObtenerOrdenAsync(orden))!;
            using var c = new CredencialDispositivo(e.Config.Dispositivo, 1, e.Config.IdContenedor, Convert.FromBase64String(_clave!));
            var p = new ProtocoloDispositivoHmac(TimeProvider.System, (id, v) => id == c.Dispositivo && v == 1 ? c : null);
            var entradas = new ColaPaquetesDispositivo(Path.Combine(_estado!, "simulador", "comandos"));
            entradas.Agregar(p.FirmarComando(c, new(o.Id, o.IdContenedor, o.ClaveIdempotencia, o.FechaVencimientoUtc)));
            string revision = Path.Combine(_estado!, "simulador", "eventos", "requiere-revision"); Directory.CreateDirectory(revision);
            byte[] dato = new byte[] { 1 }; string hash = Compat.ToHexString(Compat.Sha256(dato));
            for (int i = 1; i <= ColaPaquetesDispositivo.MaximoPendientes; i++) File.WriteAllBytes(Path.Combine(revision, i.ToString("D19") + "-" + hash + ".json"), dato);
            Hijo sim = Iniciar(e, "--dispositivo-simulado"); await ListoAsync(sim, e.Config.Comandos); await Task.Delay(750);
            Assert.AreEqual(1, entradas.Pendientes); Assert.IsEmpty(Directory.GetFiles(Path.Combine(_estado!, "simulador", "reservas")));
            Assert.IsNull(await e.Repo.ObtenerDepositoPorOrdenAsync(orden));
            Assert.AreEqual(EstadoOrdenApertura.Emitida, (await e.Repo.ObtenerOrdenAsync(orden))!.Estado);
        }

        private async Task<Caso> PrepararAsync()
        {
            _estado = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework"), "pruebas-bridge-" + _marca);
            Directory.CreateDirectory(_estado);
            _cadena = LeerCadena()!;
            _clave = Convert.ToBase64String(Compat.RandomGetBytes(32));
            var usuarios = new RepositorioUsuariosOracle(Fabrica); var materiales = new RepositorioMaterialesOracle(Fabrica);
            var canecas = new RepositorioContenedoresOracle(Fabrica); DateTime ahora = DateTime.UtcNow;
            int material = RegistrarMaterial(await materiales.AgregarAsync(new Material("DEMO_PET_" + _marca[..12].ToUpperInvariant(), "Botella PET (simulación)", CategoriaMaterial.Aprovechable, "Dato de ensayo aislado, sin medición física.")));
            int punto = RegistrarPunto(await new RepositorioPuntosInstalacionOracle(Fabrica).AgregarAsync(new PuntoInstalacion("Punto ficticio bridge " + _marca[..12], null)));
            var caneca = new Contenedor(punto, "Caneca ficticia bridge " + _marca[..12], new GeneradorIdentificadorQrAleatorio().Generar(), ahora);
            caneca.AdmitirMaterial(material); int id = RegistrarContenedor(await canecas.AgregarAsync(caneca)); await canecas.CambiarEstadoAsync(id, EstadoContenedor.EnServicio);
            var ciudadano = new Usuario("Ciudadano ficticio bridge", "br.c" + _marca + "@example.invalid", CodigoRol.Ciudadano, ahora);
            ciudadano.RegistrarConsentimiento("prueba-bridge", ahora); int titular = RegistrarUsuario(await usuarios.AgregarAsync(ciudadano));
            int operario = RegistrarUsuario(await usuarios.AgregarAsync(new Usuario("Operario ficticio bridge", "br.o" + _marca + "@example.invalid", CodigoRol.Operario, ahora)));
            int admin = RegistrarUsuario(await usuarios.AgregarAsync(new Usuario("Administrador ficticio bridge", "br.a" + _marca + "@example.invalid", CodigoRol.Administrador, ahora)));
            var a = new TcpListener(IPAddress.Loopback, 0); var b = new TcpListener(IPAddress.Loopback, 0); a.Start(); b.Start();
            int pa = ((IPEndPoint)a.LocalEndpoint).Port, pb = ((IPEndPoint)b.LocalEndpoint).Port; a.Stop(); b.Stop();
            var config = new ConfiguracionBridge(id, "prueba_" + _marca[..12], 1, new($"http://127.0.0.1:{pa}/eventos/"), new($"http://127.0.0.1:{pb}/comandos/"), _estado);
            var repo = new RepositorioInteraccionesOracle(Fabrica);
            return new(config, repo, new ServicioInteracciones(usuarios, repo, canecas, materiales, TimeProvider.System), titular, operario, admin, material);
        }

        private async Task<int> OrdenAsync(Caso e)
        {
            var inicio = await e.Servicio.IniciarAsync(e.Ciudadano, e.Config.IdContenedor, CanalInteraccion.Simulador); Assert.IsTrue(inicio.Exitoso, inicio.Mensaje);
            var orden = await e.Servicio.AutorizarAperturaAsync(e.Ciudadano, inicio.Valor, e.Material, "br-" + _marca); Assert.IsTrue(orden.Exitoso, orden.Mensaje); return orden.Valor;
        }

        private async Task<int> CantidadDepositosAsync(int orden)
        {
            using var c = await Fabrica.AbrirAsync(CancellationToken.None);
            using var cmd = ComandosOracle.Crear(c, "SELECT COUNT(*) FROM deposito WHERE id_orden=:id");
            cmd.Agregar("id", OracleDbType.Int32, orden);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private sealed record Hijo(Process Proceso, Task<string> Salida, Task<string> Error);
        private Hijo Iniciar(Caso e, string modo, string? conexion = null, EscenarioCaneca escenario = EscenarioCaneca.Normal)
        {
            string raiz = RepositorioLocal(); string configuracion = typeof(BridgeLocal).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Debug";
            string dll = Path.Combine(raiz, "src", "EcoAccess.P3.Bot", "bin", configuracion, "net48", "EcoAccess.P3.Bot.exe");
            Assert.IsTrue(File.Exists(dll), "Compilar el bot antes de esta prueba de procesos.");
            string archivo = Path.Combine(_estado!, "bridge-prueba.json"); File.WriteAllText(archivo, "{}");
            var info = new ProcessStartInfo(dll, modo) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = raiz, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string variable in info.Environment.Keys.Where(k => k.StartsWith("ECOACCESS_", StringComparison.OrdinalIgnoreCase)).ToArray())
                info.Environment.Remove(variable);

            info.Environment["ECOACCESS_ARCHIVO_BRIDGE"] = archivo;
            // El simulador no obtiene la cadena ni carga User Secrets; arranca y actúa mediante /vigencia/.
            if (modo != "--dispositivo-simulado") info.Environment["ECOACCESS_ConnectionStrings__OracleP3"] = conexion ?? _cadena;
            info.Environment["ECOACCESS_Bridge__IdContenedor"] = e.Config.IdContenedor.ToString();
            info.Environment["ECOACCESS_Bridge__Dispositivo"] = e.Config.Dispositivo; info.Environment["ECOACCESS_Bridge__VersionClave"] = "1";
            info.Environment["ECOACCESS_Bridge__ClaveHmacBase64"] = _clave; info.Environment["ECOACCESS_Bridge__DirectorioEstado"] = _estado;
            info.Environment["ECOACCESS_Bridge__Eventos"] = e.Config.Eventos.AbsoluteUri; info.Environment["ECOACCESS_Bridge__Comandos"] = e.Config.Comandos.AbsoluteUri;
            info.Environment["ECOACCESS_Bridge__EscenarioSimulado"] = escenario.ToString();
            info.Environment["ECOACCESS_Bridge__VentanaSimuladaSegundos"] = _ventanaSimulada.ToString();
            var proceso = Process.Start(info)!; var hijo = new Hijo(proceso, proceso.StandardOutput.ReadToEndAsync(), proceso.StandardError.ReadToEndAsync()); _hijos.Add(hijo); return hijo;
        }

        private static async Task ListoAsync(Hijo hijo, Uri uri)
        {
            using var cliente = new HttpClient(new HttpClientHandler { UseProxy = false }) { Timeout = TimeSpan.FromSeconds(1) };
            for (int i = 0; i < 80; i++)
            {
                Assert.IsFalse(hijo.Proceso.HasExited, "El proceso terminó antes de abrir su endpoint. Consultar registro redactado.");
                try { using var r = await cliente.GetAsync(uri); if (r.StatusCode == HttpStatusCode.MethodNotAllowed) return; }
                catch (HttpRequestException) { }
                catch (TaskCanceledException) { }
                await Task.Delay(250);
            }
            Assert.Fail("El endpoint de nuestro proceso no inició a tiempo; GET solo verifica escucha, no conexión autenticada.");
        }

        private static async Task EsperarAsync(Func<Task<bool>> condicion, params Hijo[] hijos)
        {
            for (int i = 0; i < 160; i++)
            {
                Assert.IsTrue(hijos.All(h => !h.Proceso.HasExited), "Un proceso propio terminó antes de completar el escenario.");
                if (await condicion()) return; await Task.Delay(250);
            }
            Assert.Fail("La operación de bridge no completó su evidencia dentro del límite.");
        }

        private async Task CerrarAsync(Hijo hijo)
        {
            if (!_hijos.Contains(hijo)) return;
            if (!hijo.Proceso.HasExited) hijo.Proceso.Kill();
            await hijo.Proceso.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            _registros.Add(await hijo.Salida + await hijo.Error); _hijos.Remove(hijo); hijo.Proceso.Dispose();
        }

        [TestCleanup]
        public async Task CerrarProcesosYLimpiarSoloFixturesPropios()
        {
            foreach (var hijo in _hijos.ToArray()) await CerrarAsync(hijo);
            if (Fabrica is not null && !ConservarDemo)
            {
                using var c = await Fabrica.AbrirAsync(CancellationToken.None);
                await LimpiarAnalisisPropioAsync(c);
                await LimpiarTelegramAsync(c);
                if (_telegramDemo > 0)
                {
                    foreach (string tabla in new[] { "conversacion_telegram", "vinculacion_pendiente", "vinculo_telegram", "token_vinculacion" })
                    {
                        using var cmd = ComandosOracle.Crear(c, $"DELETE FROM {tabla} WHERE telegram_user_id=:id");
                        cmd.Agregar("id", OracleDbType.Int64, _telegramDemo); await cmd.ExecuteNonQueryAsync();
                    }
                    foreach (long update in _updatesDemo)
                    {
                        using var cmd = ComandosOracle.Crear(c, "DELETE FROM update_telegram WHERE update_id=:id");
                        cmd.Agregar("id", OracleDbType.Int64, update); await cmd.ExecuteNonQueryAsync();
                    }
                }
                // Reversos/correcciones referencian créditos anteriores: limpiar cada operación
                // propia de más reciente a más antigua, primero sus movimientos y luego la operación.
                if (_deposito > 0)
                {
                    var operaciones = new List<int>();
                    using (var buscar = ComandosOracle.Crear(c, "SELECT id_operacion FROM operacion_ecopuntos WHERE id_deposito=:id ORDER BY id_operacion DESC"))
                    {
                        buscar.Agregar("id", OracleDbType.Int32, _deposito);
                        using var lector = await buscar.ExecuteReaderAsync();
                        while (await lector.ReadAsync()) operaciones.Add(lector.GetInt32(0));
                    }
                    foreach (int operacion in operaciones)
                        foreach (string sql in new[] { "DELETE FROM movimiento_ecopuntos WHERE id_operacion=:id", "DELETE FROM operacion_ecopuntos WHERE id_operacion=:id" })
                        {
                            using var borrar = ComandosOracle.Crear(c, sql);
                            borrar.Agregar("id", OracleDbType.Int32, operacion); await borrar.ExecuteNonQueryAsync();
                        }
                }
                foreach (var dato in new[] { ("DELETE FROM regla_ecopuntos WHERE id_regla=:id", _regla) })
                    if (dato.Item2 > 0) { using var cmd = ComandosOracle.Crear(c, dato.Item1); cmd.Agregar("id", OracleDbType.Int32, dato.Item2); await cmd.ExecuteNonQueryAsync(); }
            }
            string texto = string.Join("\n", _registros); string? password = _cadena is null ? null : new OracleConnectionStringBuilder(_cadena).Password;
            bool filtrado = (_clave is not null && texto.Contains(_clave, StringComparison.Ordinal)) || (!Compat.IsNullOrEmpty(password) && texto.Contains(password, StringComparison.Ordinal));
            if (_clave is not null) texto = texto.Replace(_clave, "[clave protegida]"); if (!Compat.IsNullOrEmpty(password)) texto = texto.Replace(password, "[credencial protegida]");
            string evidencia = Path.Combine(DirectorioGitLocal(), "evidencia-diseno-20261026", "bridge-procesos-" + _marca + ".log"); Directory.CreateDirectory(Path.GetDirectoryName(evidencia)!); File.WriteAllText(evidencia, texto);
            if (_estado is not null)
            {
                string ruta = Path.GetFullPath(_estado); string padre = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework");
                if (Path.GetDirectoryName(ruta) != Compat.TrimEndingDirectorySeparator(Path.GetFullPath(padre)) || Path.GetFileName(ruta) != "pruebas-bridge-" + _marca) throw new InvalidOperationException("Ruta temporal inesperada.");
                if (Directory.Exists(ruta)) Directory.Delete(ruta, recursive: true);
            }
            Assert.IsFalse(filtrado, "El proceso filtró una credencial; el registro se redactó.");
        }

        private static string RepositorioLocal()
        {
            for (DirectoryInfo? d = new(AppContext.BaseDirectory); d is not null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "EcoAccess.P3.sln"))) return d.FullName;
            throw new InvalidOperationException("La prueba de procesos necesita el checkout y los binarios del proyecto.");
        }

        private static string DirectorioGitLocal()
        {
            string raiz = RepositorioLocal(), marca = Path.Combine(raiz, ".git");
            if (Directory.Exists(marca)) return marca;
            // En un worktree .git es un archivo, no una carpeta. Las evidencias siguen siendo privadas.
            if (File.Exists(marca))
            {
                string referencia = File.ReadAllText(marca).Trim();
                if (referencia.StartsWith("gitdir: ", StringComparison.Ordinal))
                {
                    string ruta = Path.GetFullPath(Path.Combine(raiz, referencia[8..]));
                    if (Directory.Exists(ruta)) return ruta;
                }
            }
            throw new InvalidOperationException("No se encontró el directorio Git privado para las evidencias.");
        }
    }
}
