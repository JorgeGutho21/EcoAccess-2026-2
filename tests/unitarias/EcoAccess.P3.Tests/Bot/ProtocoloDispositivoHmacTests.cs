using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EcoAccess.P3.Bot.Dispositivos;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class ProtocoloDispositivoHmacTests
    {
        private static readonly DateTimeOffset Inicio = new(2026, 10, 26, 15, 0, 0, TimeSpan.Zero);
        // Material sintético de pruebas, sin relación con un dispositivo o secreto real.
        private static readonly byte[] Clave = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private readonly RelojAjustable _reloj = new(Inicio);
        private CredencialDispositivo Credencial(string dispositivo = "demo_1", int version = 1, int caneca = 1, bool habilitada = true) =>
            new(dispositivo, version, caneca, Clave, habilitada);
        private ProtocoloDispositivoHmac Protocolo(CredencialDispositivo c) => new(_reloj,
            (d, v) => d == c.Dispositivo && v == c.VersionClave ? c : null);
        private ComandoApertura Comando(int orden = 1, string nonce = "nonce-demo-0001", int caneca = 1) =>
            new(orden, caneca, nonce, _reloj.Ahora.UtcDateTime.AddMinutes(2));
        private EventoRecibido Evento(TipoEventoDispositivo tipo = TipoEventoDispositivo.Acuse, string nonce = "evento-demo-0001", int caneca = 1) =>
            new(caneca, nonce, tipo, 1, _reloj.Ahora.UtcDateTime);

        private static byte[] FirmarJson(string cuerpo, string tipo = "evento", string dispositivo = "demo_1", int version = 1, byte[]? clave = null)
        {
            string datos = Convert.ToBase64String(Encoding.UTF8.GetBytes(cuerpo));
            byte[] material = Encoding.UTF8.GetBytes(FormattableString.Invariant($"ecoaccess-p3/v1\n{dispositivo}\n{version}\n{tipo}\n{datos}"));
            string firma = Convert.ToBase64String(Compat.HmacSha256(clave ?? Clave, material));
            return JsonSerializer.SerializeToUtf8Bytes(new SobreDispositivo(1, dispositivo, version, tipo, datos, firma), Json);
        }
        private static byte[] FirmarCuerpo<T>(T cuerpo, string tipo = "evento") => FirmarJson(JsonSerializer.Serialize(cuerpo, Json), tipo);
        private static byte[] CambiarSobre(byte[] paquete, string nombre, JsonNode? valor)
        {
            var json = JsonNode.Parse(paquete)!.AsObject();
            json[nombre] = valor;
            return Encoding.UTF8.GetBytes(json.ToJsonString());
        }

        [TestMethod]
        public void FirmaDeComandoCoincideConRepresentacionIndependienteYNoConOrdenDelJsonExterior()
        {
            using var c = Credencial();
            var p = Protocolo(c);
            var comando = Comando();
            byte[] esperado = FirmarCuerpo(new ComandoFirmable(comando, Inicio.UtcDateTime), "comando");
            CollectionAssert.AreEqual(esperado, p.FirmarComando(c, comando));
            Assert.AreEqual(comando, p.ValidarComando(esperado).Valor);
            var sobre = JsonNode.Parse(esperado)!.AsObject();
            string reordenado = "{\n" + string.Join(",\n", sobre.Reverse().Select(x => JsonSerializer.Serialize(x.Key) + ": " + x.Value!.ToJsonString())) + "\n}";
            Assert.AreEqual(comando, p.ValidarComando(Encoding.UTF8.GetBytes(reordenado)).Valor);
        }

        [TestMethod]
        public void VectorPublicadoCalculadoConPythonCoincideConFirmaLiteralYSeValida()
        {
            using var c = Credencial("demo-esp32", caneca: 7);
            var p = Protocolo(c);
            var comando = new ComandoApertura(42, 7, "demo-nonce-0001", Inicio.UtcDateTime.AddMinutes(2));
            const string datos = "eyJjb21hbmRvIjp7ImlkT3JkZW4iOjQyLCJpZENvbnRlbmVkb3IiOjcsImNsYXZlSWRlbXBvdGVuY2lhIjoiZGVtby1ub25jZS0wMDAxIiwidmVuY2VVdGMiOiIyMDI2LTEwLTI2VDE1OjAyOjAwWiJ9LCJlbWl0aWRvVXRjIjoiMjAyNi0xMC0yNlQxNTowMDowMFoifQ==";
            const string firma = "byboWCyeM3Uo8bO595FPh/uVGee8U1s7+UJKuD9OVmk=";
            var generado = JsonSerializer.Deserialize<SobreDispositivo>(p.FirmarComando(c, comando), Json)!;
            Assert.AreEqual(datos, generado.Datos);
            Assert.AreEqual(firma, generado.Firma);
            byte[] fijo = JsonSerializer.SerializeToUtf8Bytes(new SobreDispositivo(1, "demo-esp32", 1, "comando", datos, firma), Json);
            Assert.AreEqual(comando, p.ValidarComando(fijo).Valor);
        }

        [TestMethod]
        public void FirmaEventoConRelojQueAvanzaUnTickNoGeneraVigenciaMayorQueElMaximo()
        {
            using var c = Credencial();
            var reloj = new RelojPorTicks(Inicio);
            var p = new ProtocoloDispositivoHmac(reloj, (_, _) => c);
            byte[] paquete = p.FirmarEvento(c, Evento());
            Assert.IsTrue(p.ValidarEvento(paquete).Exitoso);
            var sobre = JsonSerializer.Deserialize<SobreDispositivo>(paquete, Json)!;
            var cuerpo = JsonSerializer.Deserialize<EventoFirmable>(Convert.FromBase64String(sobre.Datos), Json)!;
            Assert.AreEqual(ProtocoloDispositivoHmac.VigenciaMaxima, cuerpo.VenceUtc - cuerpo.EmitidoUtc);
        }

        [TestMethod]
        public void ClaveSeCopiaYNoDependeDeMutacionesDelArrayQueRecibioElConstructor()
        {
            byte[] origen = Clave.ToArray();
            using var c = new CredencialDispositivo("demo_1", 1, 1, origen);
            Array.Clear(origen, 0, origen.Length);
            Assert.IsTrue(Protocolo(c).ValidarComando(FirmarCuerpo(new ComandoFirmable(Comando(), Inicio.UtcDateTime), "comando")).Exitoso);
        }

        [TestMethod]
        [DataRow("version")]
        [DataRow("dispositivo")]
        [DataRow("versionClave")]
        [DataRow("tipo")]
        [DataRow("datos")]
        [DataRow("firma")]
        [DataRow("campoDesconocido")]
        public void CambiarCualquierParteDelSobreImpideAutenticar(string campo)
        {
            using var c = Credencial();
            var p = Protocolo(c);
            byte[] paquete = p.FirmarEvento(c, Evento());
            JsonNode nuevo = campo is "version" or "versionClave" ? JsonValue.Create(2)! : JsonValue.Create("alterado")!;
            Assert.IsFalse(p.ValidarEvento(CambiarSobre(paquete, campo, nuevo)).Exitoso);
        }

        [TestMethod]
        public void ClaveDeOtroDispositivoBindingAjenoDeshabilitadoOEliminadoNoAutentican()
        {
            using var c = Credencial();
            byte[] paquete = Protocolo(c).FirmarEvento(c, Evento());
            using var otraClave = new CredencialDispositivo("demo_1", 1, 1, Enumerable.Repeat((byte)99, 32).ToArray());
            Assert.IsFalse(Protocolo(otraClave).ValidarEvento(paquete).Exitoso);
            using var otraCaneca = Credencial(caneca: 2);
            Assert.IsFalse(Protocolo(otraCaneca).ValidarEvento(paquete).Exitoso);
            using var otroDispositivo = Credencial("demo_2");
            Assert.IsFalse(new ProtocoloDispositivoHmac(_reloj, (_, _) => otroDispositivo).ValidarEvento(paquete).Exitoso);
            using var deshabilitada = Credencial(habilitada: false);
            Assert.IsFalse(Protocolo(deshabilitada).ValidarEvento(paquete).Exitoso);
            Assert.IsFalse(new ProtocoloDispositivoHmac(_reloj, (_, _) => null).ValidarEvento(paquete).Exitoso);
            c.Dispose();
            Assert.IsFalse(Protocolo(c).ValidarEvento(paquete).Exitoso);
        }

        [TestMethod]
        public void RotacionAdmiteVersionResueltaYRechazaLaAnteriorRevocada()
        {
            using var anterior = Credencial();
            using var nueva = Credencial(version: 2);
            byte[] viejo = Protocolo(anterior).FirmarEvento(anterior, Evento());
            byte[] nuevo = Protocolo(nueva).FirmarEvento(nueva, Evento());
            var receptor = Protocolo(nueva);
            Assert.IsFalse(receptor.ValidarEvento(viejo).Exitoso);
            Assert.IsTrue(receptor.ValidarEvento(nuevo).Exitoso);
        }

        [TestMethod]
        public void SeparacionDeDominioImpideInterpretarEventoComoComandoYViceversa()
        {
            using var c = Credencial();
            var p = Protocolo(c);
            Assert.IsFalse(p.ValidarComando(p.FirmarEvento(c, Evento())).Exitoso);
            Assert.IsFalse(p.ValidarEvento(p.FirmarComando(c, Comando())).Exitoso);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("null")]
        [DataRow("[]")]
        [DataRow("{}")]
        [DataRow("{invalido}")]
        [DataRow("{\"version\":1,\"version\":1}")]
        public void JsonExteriorInvalidoOAmbiguoEsRechazoSeguro(string json)
        {
            using var c = Credencial();
            var p = Protocolo(c);
            Assert.IsFalse(p.ValidarEvento(Encoding.UTF8.GetBytes(json)).Exitoso);
            Assert.IsFalse(p.ValidarComando(Encoding.UTF8.GetBytes(json)).Exitoso);
        }

        [TestMethod]
        public void DuplicadosExteriorConFirmaValidaSeRechazan()
        {
            using var c = Credencial();
            var p = Protocolo(c);
            string json = Encoding.UTF8.GetString(p.FirmarEvento(c, Evento()));
            Assert.IsFalse(p.ValidarEvento(Encoding.UTF8.GetBytes(json.Insert(1, "\"version\":1,"))).Exitoso);
        }

        [TestMethod]
        public void CuerpoFirmadoDuplicadoDesconocidoONuloSeRechaza()
        {
            using var c = Credencial();
            var p = Protocolo(c);
            string valido = JsonSerializer.Serialize(new EventoFirmable(Evento(), Inicio.UtcDateTime, Inicio.UtcDateTime.AddMinutes(2)), Json);
            foreach (string cuerpo in new[] { valido.Insert(1, "\"extra\":1,"), valido.Replace("\"idContenedor\":1", "\"idContenedor\":1,\"idContenedor\":2"), "null", "{}", "{\"evento\":null}" })
                Assert.IsFalse(p.ValidarEvento(FirmarJson(cuerpo)).Exitoso, cuerpo);
            Assert.IsFalse(p.ValidarComando(FirmarJson("{\"comando\":null}", "comando")).Exitoso);
        }

        [TestMethod]
        public void PaqueteNuloVacioOSuperiorAlLimiteSeRechazaSinResolverCredencial()
        {
            int llamadas = 0;
            var p = new ProtocoloDispositivoHmac(_reloj, (_, _) => { llamadas++; return null; });
            foreach (byte[] bytes in new[] { null!, Array.Empty<byte>(), new byte[ProtocoloDispositivoHmac.MaximoPaquete + 1] })
            {
                Assert.IsFalse(p.ValidarEvento(bytes).Exitoso);
                Assert.IsFalse(p.ValidarComando(bytes).Exitoso);
            }
            Assert.AreEqual(0, llamadas);
        }

        [TestMethod]
        public void EventosYComandosVencidosNoSeAceptanAunqueLaFirmaSigaCorrecta()
        {
            using var c = Credencial();
            var p = Protocolo(c);
            byte[] evento = p.FirmarEvento(c, Evento());
            byte[] comando = p.FirmarComando(c, Comando());
            _reloj.Avanzar(TimeSpan.FromMinutes(2) + TimeSpan.FromTicks(1));
            Assert.IsFalse(p.ValidarEvento(evento).Exitoso);
            Assert.IsFalse(p.ValidarComando(comando).Exitoso);
            Assert.ThrowsExactly<ArgumentException>(() => p.FirmarComando(c, Comando() with { VenceUtc = Inicio.UtcDateTime }));
        }

        [TestMethod]
        [DataRow(30, true)]
        [DataRow(31, false)]
        [DataRow(-1, true)]
        public void DesfaseDeEmisionFuturaEstaAcotado(int segundos, bool esperado)
        {
            using var c = Credencial();
            DateTime emitido = Inicio.UtcDateTime.AddSeconds(segundos);
            var e = Evento() with { FechaDispositivoUtc = emitido };
            byte[] evento = FirmarCuerpo(new EventoFirmable(e, emitido, emitido.AddSeconds(60)));
            Assert.AreEqual(esperado, Protocolo(c).ValidarEvento(evento).Exitoso);
            byte[] comando = FirmarCuerpo(new ComandoFirmable(Comando() with { VenceUtc = Inicio.UtcDateTime.AddMinutes(1) }, emitido), "comando");
            Assert.AreEqual(esperado, Protocolo(c).ValidarComando(comando).Exitoso);
        }

        [TestMethod]
        public void VentanasExcesivasFechasSinUtcYDatosDeEventoInvalidosSonRechazados()
        {
            using var c = Credencial();
            var p = Protocolo(c);
            var cuerpo = new EventoFirmable(Evento(), Inicio.UtcDateTime, Inicio.UtcDateTime.AddMinutes(2));
            foreach (var invalido in new[] {
                cuerpo with { VenceUtc = Inicio.UtcDateTime.AddMinutes(3) },
                cuerpo with { EmitidoUtc = DateTime.SpecifyKind(Inicio.UtcDateTime, DateTimeKind.Unspecified) },
                cuerpo with { VenceUtc = Inicio.UtcDateTime },
                cuerpo with { Evento = Evento() with { IdOrden = null } },
                cuerpo with { Evento = Evento() with { IdOrden = 0 } },
                cuerpo with { Evento = Evento() with { IdContenedor = 2 } },
                cuerpo with { Evento = Evento() with { IdExterno = new string('x',65) } },
                cuerpo with { Evento = Evento() with { IdExterno = " " } },
                cuerpo with { Evento = Evento() with { Tipo = (TipoEventoDispositivo)999 } },
                cuerpo with { Evento = Evento() with { FechaDispositivoUtc = Inicio.UtcDateTime.AddMinutes(-3) } },
                cuerpo with { Evento = Evento() with { FechaDispositivoUtc = Inicio.UtcDateTime.AddSeconds(31) } } })
                Assert.IsFalse(p.ValidarEvento(FirmarCuerpo(invalido)).Exitoso, invalido.ToString());
            Assert.ThrowsExactly<ArgumentException>(() => p.FirmarEvento(c, Evento(caneca: 2)));
            Assert.ThrowsExactly<ArgumentException>(() => p.FirmarComando(c, Comando(caneca: 2)));
            Assert.ThrowsExactly<ArgumentException>(() => p.FirmarComando(c, Comando() with { VenceUtc = Inicio.UtcDateTime.AddMinutes(3) }));
        }

        [TestMethod]
        public async Task ReceptorNoInvocaNegocioSiFallaFirmaYPropagaCancelacion()
        {
            using var c = Credencial();
            var p = Protocolo(c);
            int recibidos = 0;
            var receptor = new ReceptorEventosDispositivo(p, (_, ct) => { ct.ThrowIfCancellationRequested(); recibidos++; return Task.FromResult(Resultado<EventoRegistrado>.Exito(new(1, ResultadoEvento.Aplicado, false))); });
            Assert.IsFalse((await receptor.RecibirAsync(new byte[] { 1, 2, 3 })).Exitoso);
            Assert.AreEqual(0, recibidos);
            byte[] valido = p.FirmarEvento(c, Evento());
            Assert.IsTrue((await receptor.RecibirAsync(valido)).Exitoso);
            using var cancelacion = new CancellationTokenSource();
            cancelacion.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => receptor.RecibirAsync(valido, cancelacion.Token));
            Assert.AreEqual(1, recibidos);
        }

        private sealed class RelojPorTicks : TimeProvider
        {
            private readonly DateTimeOffset _inicio;

            public RelojPorTicks(DateTimeOffset inicio)
            {
                _inicio = inicio;
            }

            private long _ticks;
            public override DateTimeOffset GetUtcNow() => _inicio.AddTicks(Interlocked.Increment(ref _ticks));
        }
    }

    [TestClass]
    public sealed class EjecucionDispositivoSeguraTests
    {
        private static readonly DateTimeOffset Inicio = new(2026, 10, 26, 15, 0, 0, TimeSpan.Zero);
        private readonly RelojAjustable _reloj = new(Inicio);
        private readonly string _directorio = Path.Combine(Path.GetTempPath(), "ecoaccess-prueba-dispositivo-" + Guid.NewGuid().ToString("N"));
        private CredencialDispositivo _credencial = null!;
        private ProtocoloDispositivoHmac _protocolo = null!;
        private ComandoApertura Comando(int orden = 1, string nonce = "nonce-0001") => new(orden, 1, nonce, _reloj.Ahora.UtcDateTime.AddMinutes(2));
        [TestInitialize]
        public void Preparar()
        {
            _credencial = new CredencialDispositivo("caneca_demo", 1, 1, Enumerable.Range(0, 32).Select(i => (byte)i).ToArray());
            _protocolo = new ProtocoloDispositivoHmac(_reloj, (d, v) => d == _credencial.Dispositivo && v == _credencial.VersionClave ? _credencial : null);
        }
        [TestCleanup]
        public void Limpiar()
        {
            _credencial.Dispose();
            // Solo elimina el directorio temporal único que creó esta prueba, nunca un diario del proyecto.
            if (Directory.Exists(_directorio)) Directory.Delete(_directorio, recursive: true);
        }
        private EjecutorComandosAutenticados Ejecutor(Func<ComandoApertura, CancellationToken, Task> actuar,
            Func<ComandoApertura, CancellationToken, Task<bool>>? autorizar = null) =>
            new(_protocolo, new RegistroEjecucionArchivo(_directorio), autorizar ?? ((_, _) => Task.FromResult(true)), actuar);

        [TestMethod]
        public async Task FirmaInvalidaOAutorizacionRevocadaNoLleganAReservaNiAccion()
        {
            int consultas = 0, acciones = 0;
            var ejecutor = Ejecutor((_, _) => { acciones++; return Task.CompletedTask; }, (_, _) => { consultas++; return Task.FromResult(false); });
            Assert.AreEqual(ResultadoComandoDispositivo.Rechazado, await ejecutor.ProcesarAsync(new byte[] { 1, 2, 3 }));
            Assert.AreEqual(0, consultas);
            Assert.AreEqual(ResultadoComandoDispositivo.NoAutorizado, await ejecutor.ProcesarAsync(_protocolo.FirmarComando(_credencial, Comando())));
            Assert.AreEqual(1, consultas);
            Assert.AreEqual(0, acciones);
            Assert.IsEmpty(Directory.GetFiles(_directorio));
        }

        [TestMethod]
        public async Task ConsultaCaidaNoActuaYPermiteReintentarTrasRecuperarConexion()
        {
            int acciones = 0;
            byte[] paquete = _protocolo.FirmarComando(_credencial, Comando());
            var sinConexion = Ejecutor((_, _) => { acciones++; return Task.CompletedTask; }, (_, _) => throw new IOException("fallo ficticio"));
            Assert.AreEqual(ResultadoComandoDispositivo.AutorizacionNoDisponible, await sinConexion.ProcesarAsync(paquete));
            Assert.IsEmpty(Directory.GetFiles(_directorio));
            var recuperado = Ejecutor((_, _) => { acciones++; return Task.CompletedTask; });
            Assert.AreEqual(ResultadoComandoDispositivo.AccionCompletada, await recuperado.ProcesarAsync(paquete));
            Assert.AreEqual(1, acciones);
        }

        [TestMethod]
        public async Task SiVenceDuranteLaConsultaNoSeReservaNiSeActua()
        {
            byte[] paquete = _protocolo.FirmarComando(_credencial, Comando());
            var ejecutor = Ejecutor((_, _) => throw new AssertFailedException("No debe actuar"), (_, _) => { _reloj.Avanzar(TimeSpan.FromMinutes(3)); return Task.FromResult(true); });
            Assert.AreEqual(ResultadoComandoDispositivo.Rechazado, await ejecutor.ProcesarAsync(paquete));
            Assert.IsEmpty(Directory.GetFiles(_directorio));
        }

        [TestMethod]
        public async Task BufferExteriorMutadoMientrasSeAutorizaNoCambiaElComandoPrivadoNiSuVencimiento()
        {
            var original = Comando() with { VenceUtc = Inicio.UtcDateTime.AddSeconds(1) };
            byte[] paquete = Enumerable.Repeat((byte)32, ProtocoloDispositivoHmac.MaximoPaquete).ToArray();
            _protocolo.FirmarComando(_credencial, original).CopyTo(paquete, 0);
            byte[] reemplazo = _protocolo.FirmarComando(_credencial, Comando());
            var ejecutor = Ejecutor((_, _) => throw new AssertFailedException("El comando original venció"), (_, _) =>
            {
                for (int p = 0; p < paquete.Length; p++) paquete[p] = 32;
                reemplazo.CopyTo(paquete, 0);
                _reloj.Avanzar(TimeSpan.FromSeconds(2));
                return Task.FromResult(true);
            });
            Assert.AreEqual(ResultadoComandoDispositivo.Rechazado, await ejecutor.ProcesarAsync(paquete));
            Assert.IsEmpty(Directory.GetFiles(_directorio));
        }

        [TestMethod]
        public async Task VencimientoDuranteEscrituraDurableConservaReservaPeroNoActua()
        {
            byte[] paquete = _protocolo.FirmarComando(_credencial, Comando() with { VenceUtc = Inicio.UtcDateTime.AddSeconds(1) });
            var durable = new RegistroEjecucionArchivo(_directorio);
            var registro = new RegistroConAccion(comando =>
            {
                bool resultado = durable.IntentarReservar(comando);
                _reloj.Avanzar(TimeSpan.FromSeconds(2));
                return resultado;
            });
            var ejecutor = new EjecutorComandosAutenticados(_protocolo, registro, (_, _) => Task.FromResult(true),
                (_, _) => throw new AssertFailedException("Ya venció"));
            Assert.AreEqual(ResultadoComandoDispositivo.Rechazado, await ejecutor.ProcesarAsync(paquete));
            Assert.IsFalse(new RegistroEjecucionArchivo(_directorio).IntentarReservar(Comando()));
        }

        private sealed class RegistroConAccion : IRegistroEjecucionDispositivo
        {
            private readonly Func<ComandoApertura, bool> _reservar;

            public RegistroConAccion(Func<ComandoApertura, bool> reservar)
            {
                _reservar = reservar;
            }

            public bool IntentarReservar(ComandoApertura comando) => _reservar(comando);
        }

        [TestMethod]
        public async Task ReplayOrdenConOtraClaveYNonceEnOtraOrdenNoVuelvenAActuarTrasReinicio()
        {
            int acciones = 0;
            Task Accion(ComandoApertura _, CancellationToken ct) { ct.ThrowIfCancellationRequested(); acciones++; return Task.CompletedTask; }
            byte[] paquete = _protocolo.FirmarComando(_credencial, Comando());
            Assert.AreEqual(ResultadoComandoDispositivo.AccionCompletada, await Ejecutor(Accion).ProcesarAsync(paquete));
            var reiniciado = Ejecutor(Accion);
            Assert.AreEqual(ResultadoComandoDispositivo.YaReservadoOIncierto, await reiniciado.ProcesarAsync(paquete));
            Assert.AreEqual(ResultadoComandoDispositivo.YaReservadoOIncierto, await reiniciado.ProcesarAsync(_protocolo.FirmarComando(_credencial, Comando(nonce: "otro-nonce"))));
            Assert.AreEqual(ResultadoComandoDispositivo.YaReservadoOIncierto, await reiniciado.ProcesarAsync(_protocolo.FirmarComando(_credencial, Comando(2))));
            Assert.AreEqual(1, acciones);
        }

        [TestMethod]
        public async Task ExcepcionTrasReservaConservaLaIncertidumbreYNoReabre()
        {
            int acciones = 0;
            byte[] paquete = _protocolo.FirmarComando(_credencial, Comando());
            await Assert.ThrowsExactlyAsync<IOException>(() => Ejecutor((_, _) => { acciones++; throw new IOException("fallo ficticio después de reservar"); }).ProcesarAsync(paquete));
            Assert.AreEqual(ResultadoComandoDispositivo.YaReservadoOIncierto, await Ejecutor((_, _) => { acciones++; return Task.CompletedTask; }).ProcesarAsync(paquete));
            Assert.AreEqual(1, acciones);
        }

        [TestMethod]
        public async Task CancelacionPrevioAReservaNoConsumeOperacionYPosteriorNoPermiteRepetir()
        {
            byte[] paquete = _protocolo.FirmarComando(_credencial, Comando());
            using var antes = new CancellationTokenSource();
            antes.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => Ejecutor((_, _) => throw new AssertFailedException("No debe actuar")).ProcesarAsync(paquete, antes.Token));
            Assert.IsEmpty(Directory.GetFiles(_directorio));
            using var durante = new CancellationTokenSource();
            await Assert.ThrowsAsync<OperationCanceledException>(() => Ejecutor((_, ct) => { durante.Cancel(); ct.ThrowIfCancellationRequested(); return Task.CompletedTask; }).ProcesarAsync(paquete, durante.Token));
            Assert.AreEqual(ResultadoComandoDispositivo.YaReservadoOIncierto, await Ejecutor((_, _) => throw new AssertFailedException("No debe actuar")).ProcesarAsync(paquete));
        }

        [TestMethod]
        public async Task ReservasConcurrentesDeProcesosLogicosDistintosSoloPermitenUnaActuacion()
        {
            int acciones = 0;
            byte[] paquete = _protocolo.FirmarComando(_credencial, Comando());
            var inicio = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<ResultadoComandoDispositivo>[] tareas = Enumerable.Range(0, 12).Select(_ => Task.Run(async () =>
            {
                var ejecutor = Ejecutor((_, _) => { Interlocked.Increment(ref acciones); return Task.CompletedTask; });
                await inicio.Task;
                return await ejecutor.ProcesarAsync(paquete);
            })).ToArray();
            inicio.SetResult(true);
            var resultados = await Task.WhenAll(tareas);
            Assert.AreEqual(1, acciones);
            Assert.AreEqual(1, resultados.Count(r => r == ResultadoComandoDispositivo.AccionCompletada));
            Assert.AreEqual(11, resultados.Count(r => r == ResultadoComandoDispositivo.YaReservadoOIncierto));
        }

        [TestMethod]
        public void FalloDeSegundaMarcaOAlmacenamientoNoPermiteReapertura()
        {
            var registro = new RegistroEjecucionArchivo(_directorio);
            Assert.IsTrue(registro.IntentarReservar(Comando()));
            Assert.IsFalse(registro.IntentarReservar(Comando(2))); // nonce ya reservado, pero orden2 queda marcada
            Assert.IsFalse(new RegistroEjecucionArchivo(_directorio).IntentarReservar(Comando(2, "nuevo-nonce")));
            string bloqueado = Path.Combine(_directorio, "orden-1-3");
            Directory.CreateDirectory(bloqueado);
            Assert.IsFalse(registro.IntentarReservar(Comando(3, "nonce-tercero")));
        }

        [TestMethod]
        public async Task FlujoAutenticadoSimulaSensorDeduplicaEventoYReinicioNoGeneraSegundoDeposito()
        {
            const int ciudadano = 3;
            var usuarios = new RepositorioUsuariosEnMemoria();
            var materiales = new RepositorioMaterialesEnMemoria();
            var contenedores = new RepositorioContenedoresEnMemoria();
            var repositorio = new RepositorioInteraccionesEnMemoria();
            usuarios.AgregarCiudadano(ciudadano);
            materiales.Contenedores = contenedores;
            contenedores.Materiales = materiales;
            var material = materiales.Sembrar("PET");
            var caneca = contenedores.Sembrar(1, EstadoContenedor.EnServicio, material.Id);
            Assert.AreEqual(1, caneca.Id);
            repositorio.Contenedores = contenedores;
            repositorio.Materiales = materiales;
            var servicio = new ServicioInteracciones(usuarios, repositorio, contenedores, materiales, _reloj);
            var receptor = new ReceptorEventosDispositivo(_protocolo, servicio.RegistrarEventoAsync);
            var paquetes = new List<byte[]>();
            Task<Resultado<EventoRegistrado>> EnviarEvento(EventoRecibido evento, CancellationToken ct)
            {
                byte[] firmado = _protocolo.FirmarEvento(_credencial, evento);
                paquetes.Add(firmado);
                return receptor.RecibirAsync(firmado, ct);
            }
            var simulada = new CanecaSimulada(caneca.Id, EnviarEvento, _reloj, new RegistroEjecucionArchivo(_directorio)) { Escenario = EscenarioCaneca.Desconectada };
            int interaccion = (await servicio.IniciarAsync(ciudadano, caneca.Id, CanalInteraccion.Simulador)).Valor;
            int orden = (await servicio.AutorizarAperturaAsync(ciudadano, interaccion, material.Id, "nonce-demo-flujo")).Valor;
            var despacho = new ServicioDespachoOrdenes(repositorio, servicio, simulada, _reloj);
            Assert.AreEqual(ResultadoDespacho.NoEntregada, await despacho.DespacharAsync(orden));
            simulada.Escenario = EscenarioCaneca.Duplicados;
            Assert.AreEqual(ResultadoDespacho.Enviada, await despacho.DespacharAsync(orden));
            var respuestas = await simulada.EjecutarAsync(orden);
            Assert.HasCount(8, respuestas);
            Assert.AreEqual(4, respuestas.Count(r => r.Valor.Repetido));
            Assert.HasCount(1, repositorio.Depositos);
            Assert.IsNull(repositorio.Depositos.Single().IdMaterialVerificado);
            Assert.IsNull(repositorio.Depositos.Single().PesoMedidoKg);
            Assert.AreEqual(EstadoOrdenApertura.Cerrada, repositorio.LeerOrden(orden)!.Estado);
            Assert.IsTrue((await receptor.RecibirAsync(paquetes[4])).Valor.Repetido);
            var nuevoEventoTerminal = new EventoRecibido(caneca.Id, "deteccion-terminal-nueva", TipoEventoDispositivo.Deteccion, orden, _reloj.Ahora.UtcDateTime);
            Assert.AreEqual(ResultadoEvento.Revision, (await receptor.RecibirAsync(_protocolo.FirmarEvento(_credencial, nuevoEventoTerminal))).Valor.Resultado);
            Assert.HasCount(1, repositorio.Depositos);
            var reiniciada = new CanecaSimulada(caneca.Id, EnviarEvento, _reloj, new RegistroEjecucionArchivo(_directorio));
            var comando = simulada.ComandosRecibidos.Single();
            Assert.IsTrue(await reiniciada.EnviarAperturaAsync(comando));
            Assert.IsEmpty(await reiniciada.EjecutarAsync(orden));
            Assert.HasCount(1, repositorio.Depositos);
            Assert.AreEqual(ResultadoDespacho.NoPermitida, await despacho.DespacharAsync(orden));
        }

        [TestMethod]
        public async Task SimuladorRechazaComandoVencidoOConflictivoSinAceptarPayloadNuevo()
        {
            var simulada = new CanecaSimulada(1, (_, _) => throw new InvalidOperationException("No emitir"), _reloj);
            var comando = Comando();
            Assert.IsTrue(await simulada.EnviarAperturaAsync(comando));
            Assert.IsTrue(await simulada.EnviarAperturaAsync(comando));
            Assert.IsFalse(await simulada.EnviarAperturaAsync(comando with { ClaveIdempotencia = "otra-clave" }));
            Assert.IsFalse(await simulada.EnviarAperturaAsync(comando with { VenceUtc = Inicio.UtcDateTime.AddMinutes(1) }));
            Assert.IsFalse(await simulada.EnviarAperturaAsync(comando with { IdOrden = 2, VenceUtc = Inicio.UtcDateTime.AddTicks(-1) }));
            Assert.HasCount(1, simulada.ComandosRecibidos);
        }
    }
}
