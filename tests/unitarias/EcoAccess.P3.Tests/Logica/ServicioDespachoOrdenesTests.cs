using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    [TestClass]
    public sealed class ServicioDespachoOrdenesTests
    {
        private const int Ciudadano = 3;
        private static readonly DateTimeOffset Inicio = new(2026, 3, 10, 8, 30, 0, TimeSpan.Zero);

        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioMaterialesEnMemoria _materiales = new();
        private readonly RepositorioContenedoresEnMemoria _contenedores = new();
        private readonly RepositorioInteraccionesEnMemoria _repositorio = new();
        private readonly RelojAjustable _reloj = new(Inicio);
        private readonly ServicioInteracciones _servicio;
        private readonly CanalPrueba _canal = new();
        private readonly Material _pet;
        private readonly Contenedor _caneca;

        public ServicioDespachoOrdenesTests()
        {
            _usuarios.AgregarCiudadano(Ciudadano);
            _materiales.Contenedores = _contenedores;
            _contenedores.Materiales = _materiales;
            _pet = _materiales.Sembrar("PET");
            _caneca = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _pet.Id);
            _repositorio.Contenedores = _contenedores;
            _repositorio.Materiales = _materiales;
            _servicio = new ServicioInteracciones(_usuarios, _repositorio, _contenedores, _materiales, _reloj);
        }

        /// <summary>Canal de prueba: hace lo que diga <see cref="Responder"/> y cuenta las llamadas.</summary>
        private sealed class CanalPrueba : ICanalDispositivo
        {
            private int _llamadas;

            public Func<ComandoApertura, CancellationToken, Task<bool>> Responder { get; set; } = (_, _) => Task.FromResult(true);

            public List<ComandoApertura> Comandos { get; } = new List<ComandoApertura>();

            public int Llamadas => Volatile.Read(ref _llamadas);

            public string Canal => "prueba";

            public Task<bool> EnviarAperturaAsync(ComandoApertura comando, CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref _llamadas);
                lock (Comandos)
                {
                    Comandos.Add(comando);
                }

                return Responder(comando, cancellationToken);
            }
        }

        private ServicioDespachoOrdenes Despacho(TimeSpan? tiempoMaximo = null) =>
            new(_repositorio, _servicio, _canal, _reloj, tiempoMaximo);

        private async Task<int> OrdenAsync(string clave = "clave-0001")
        {
            var inicio = await _servicio.IniciarAsync(Ciudadano, _caneca.Id, CanalInteraccion.Simulador);
            Assert.IsTrue(inicio.Exitoso, inicio.ToString());
            var orden = await _servicio.AutorizarAperturaAsync(Ciudadano, inicio.Valor, _pet.Id, clave);
            Assert.IsTrue(orden.Exitoso, orden.ToString());
            return orden.Valor;
        }

        private EstadoOrdenApertura Estado(int idOrden) => _repositorio.LeerOrden(idOrden)!.Estado;

        [TestMethod]
        public async Task DespacharEntregaElComandoYDejaLaOrdenEnviada()
        {
            int idOrden = await OrdenAsync();
            OrdenApertura emitida = _repositorio.LeerOrden(idOrden)!;

            Assert.AreEqual(ResultadoDespacho.Enviada, await Despacho().DespacharAsync(idOrden));

            Assert.AreEqual(EstadoOrdenApertura.Enviada, Estado(idOrden));
            ComandoApertura comando = _canal.Comandos.Single();
            Assert.AreEqual(new ComandoApertura(idOrden, _caneca.Id, "clave-0001", emitida.FechaVencimientoUtc), comando);
        }

        [TestMethod]
        public async Task EntregarElComandoNoAbreLaCanecaNiCreaDeposito()
        {
            int idOrden = await OrdenAsync();

            await Despacho().DespacharAsync(idOrden);

            Assert.AreEqual(EstadoOrdenApertura.Enviada, Estado(idOrden), "entregar no es abrir");
            Assert.IsEmpty(_repositorio.Depositos);
            Assert.AreEqual(EstadoInteraccion.Autorizada, _repositorio.LeerInteraccion(_repositorio.LeerOrden(idOrden)!.IdInteraccion)!.Estado);
        }

        [TestMethod]
        public async Task CanalQueLanzaDejaLaOrdenEmitidaParaReintentar()
        {
            int idOrden = await OrdenAsync();
            _canal.Responder = (_, _) => throw new IOException("puerto serie cerrado");

            Assert.AreEqual(ResultadoDespacho.NoEntregada, await Despacho().DespacharAsync(idOrden));
            Assert.AreEqual(EstadoOrdenApertura.Emitida, Estado(idOrden));
        }

        [TestMethod]
        public async Task CanalQueFallaDeFormaAsincronaDejaLaOrdenEmitida()
        {
            int idOrden = await OrdenAsync();
            _canal.Responder = async (_, _) =>
            {
                await Task.Yield();
                throw new HttpRequestException("sin red local");
            };

            Assert.AreEqual(ResultadoDespacho.NoEntregada, await Despacho().DespacharAsync(idOrden));
            Assert.AreEqual(EstadoOrdenApertura.Emitida, Estado(idOrden));
        }

        [TestMethod]
        public async Task CanalQueNoEntregaYLuegoSiPermiteReintentar()
        {
            int idOrden = await OrdenAsync();
            _canal.Responder = (_, _) => Task.FromResult(false);
            ServicioDespachoOrdenes despacho = Despacho();

            Assert.AreEqual(ResultadoDespacho.NoEntregada, await despacho.DespacharAsync(idOrden));
            _canal.Responder = (_, _) => Task.FromResult(true);
            Assert.AreEqual(ResultadoDespacho.Enviada, await despacho.DespacharAsync(idOrden));

            Assert.AreEqual(2, _canal.Llamadas);
            Assert.HasCount(1, _canal.Comandos.Select(c => c.ClaveIdempotencia).Distinct().ToArray(), "el reintento lleva la misma clave");
            Assert.AreEqual(EstadoOrdenApertura.Enviada, Estado(idOrden));
        }

        [TestMethod]
        public async Task CanalQueIgnoraElTokenNoCuelgaElDespacho()
        {
            int idOrden = await OrdenAsync();
            var nunca = new TaskCompletionSource<bool>();
            _canal.Responder = (_, _) => nunca.Task;

            Assert.AreEqual(ResultadoDespacho.NoEntregada, await Despacho(TimeSpan.FromMilliseconds(50)).DespacharAsync(idOrden));
            Assert.AreEqual(EstadoOrdenApertura.Emitida, Estado(idOrden));
        }

        [TestMethod]
        public async Task CanalQueRespetaElTokenRecibeLaCancelacionPorTiempo()
        {
            int idOrden = await OrdenAsync();
            CancellationToken recibido = default;
            _canal.Responder = async (_, ct) =>
            {
                recibido = ct;
                await Task.Delay(Timeout.Infinite, ct);
                return true;
            };

            Assert.AreEqual(ResultadoDespacho.NoEntregada, await Despacho(TimeSpan.FromMilliseconds(50)).DespacharAsync(idOrden));
            Assert.IsTrue(recibido.IsCancellationRequested, "el canal recibió la cancelación por tiempo");
        }

        [TestMethod]
        public async Task CancelacionPreviaNoLlamaAlCanal()
        {
            int idOrden = await OrdenAsync();
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(() => Despacho().DespacharAsync(idOrden, cts.Token));
            Assert.AreEqual(0, _canal.Llamadas);
            Assert.AreEqual(EstadoOrdenApertura.Emitida, Estado(idOrden));
        }

        [TestMethod]
        public async Task CancelacionDuranteElEnvioSePropagaAunqueElCanalLanceOtraExcepcion()
        {
            int idOrden = await OrdenAsync();
            using var cts = new CancellationTokenSource();
            _canal.Responder = (_, _) =>
            {
                // Cancel síncrono: la excepción del canal llega después de la cancelación, sin carrera.
                cts.Cancel();
                return Task.FromException<bool>(new HttpRequestException("conexión abortada"));
            };

            var ex = await Assert.ThrowsAsync<OperationCanceledException>(() => Despacho().DespacharAsync(idOrden, cts.Token));
            Assert.IsInstanceOfType<HttpRequestException>(ex.InnerException);
            Assert.AreEqual(EstadoOrdenApertura.Emitida, Estado(idOrden));
        }

        [TestMethod]
        public async Task OrdenVencidaNoSeEnvia()
        {
            int idOrden = await OrdenAsync();
            _reloj.Avanzar(ServicioInteracciones.VigenciaOrden + TimeSpan.FromSeconds(1));

            Assert.AreEqual(ResultadoDespacho.Vencida, await Despacho().DespacharAsync(idOrden));
            Assert.AreEqual(0, _canal.Llamadas);
        }

        [TestMethod]
        public async Task OrdenQueVenceMientrasSeEntregaQuedaVencida()
        {
            int idOrden = await OrdenAsync();
            _canal.Responder = (_, _) =>
            {
                _reloj.Avanzar(ServicioInteracciones.VigenciaOrden + TimeSpan.FromSeconds(1));
                return Task.FromResult(true);
            };

            Assert.AreEqual(ResultadoDespacho.Vencida, await Despacho().DespacharAsync(idOrden));
            Assert.AreEqual(EstadoOrdenApertura.Emitida, Estado(idOrden), "MarcarEnviada rechaza la orden vencida");
        }

        [TestMethod]
        public async Task OrdenQueElBarridoDejoVencidaSeInformaVencida()
        {
            int idOrden = await OrdenAsync();
            _reloj.Avanzar(ServicioInteracciones.VigenciaOrden + TimeSpan.FromSeconds(1));
            Assert.AreEqual(1, await _servicio.VencerOrdenesAsync());
            Assert.AreEqual(EstadoOrdenApertura.Vencida, Estado(idOrden));

            Assert.AreEqual(ResultadoDespacho.Vencida, await Despacho().DespacharAsync(idOrden));
            Assert.AreEqual(0, _canal.Llamadas);
        }

        [TestMethod]
        public async Task CancelacionDespuesDeEntregarIgualMarcaLaOrdenEnviada()
        {
            int idOrden = await OrdenAsync();
            using var cts = new CancellationTokenSource();
            _canal.Responder = (_, _) =>
            {
                // El comando ya salió hacia la caneca cuando quien llama cancela.
                cts.Cancel();
                return Task.FromResult(true);
            };

            Assert.AreEqual(ResultadoDespacho.Enviada, await Despacho().DespacharAsync(idOrden, cts.Token));
            Assert.AreEqual(EstadoOrdenApertura.Enviada, Estado(idOrden));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(999)]
        public async Task OrdenInexistenteNoSePermite(int idOrden)
        {
            Assert.AreEqual(ResultadoDespacho.NoPermitida, await Despacho().DespacharAsync(idOrden));
            Assert.AreEqual(0, _canal.Llamadas);
        }

        [TestMethod]
        public async Task OrdenTerminadaNoSePermite()
        {
            int idOrden = await OrdenAsync();
            await Despacho().DespacharAsync(idOrden);
            var fallo = await _servicio.RegistrarEventoAsync(new EventoRecibido(_caneca.Id, "e-1", TipoEventoDispositivo.Fallo, idOrden, null));
            Assert.IsTrue(fallo.Exitoso, fallo.ToString());
            Assert.AreEqual(EstadoOrdenApertura.Fallida, Estado(idOrden));

            Assert.AreEqual(ResultadoDespacho.NoPermitida, await Despacho().DespacharAsync(idOrden));
            Assert.AreEqual(1, _canal.Llamadas);
        }

        [TestMethod]
        public async Task OrdenYaEnviadaNoSeReenvia()
        {
            int idOrden = await OrdenAsync();
            ServicioDespachoOrdenes despacho = Despacho();
            await despacho.DespacharAsync(idOrden);

            Assert.AreEqual(ResultadoDespacho.YaEnviada, await despacho.DespacharAsync(idOrden));
            Assert.AreEqual(1, _canal.Llamadas);
        }

        [TestMethod]
        public async Task AcuseQueLlegaAntesDeMarcarEnviadaSeInformaComoYaEnviada()
        {
            int idOrden = await OrdenAsync();
            _canal.Responder = async (comando, ct) =>
            {
                // La caneca responde tan rápido que el acuse llega antes de que el servicio marque la orden.
                var acuse = await _servicio.RegistrarEventoAsync(
                    new EventoRecibido(comando.IdContenedor, "e-1", TipoEventoDispositivo.Acuse, comando.IdOrden, null), ct);
                Assert.AreEqual(ResultadoEvento.Aplicado, acuse.Valor.Resultado, acuse.Valor.MotivoRevision);
                return true;
            };

            Assert.AreEqual(ResultadoDespacho.YaEnviada, await Despacho().DespacharAsync(idOrden));
            Assert.AreEqual(EstadoOrdenApertura.Acusada, Estado(idOrden));
        }

        [TestMethod]
        public void ConstructorRechazaDependenciasNulasYTiempoNoPositivo()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ServicioDespachoOrdenes(null!, _servicio, _canal, _reloj));
            Assert.ThrowsExactly<ArgumentNullException>(() => new ServicioDespachoOrdenes(_repositorio, null!, _canal, _reloj));
            Assert.ThrowsExactly<ArgumentNullException>(() => new ServicioDespachoOrdenes(_repositorio, _servicio, null!, _reloj));
            Assert.ThrowsExactly<ArgumentNullException>(() => new ServicioDespachoOrdenes(_repositorio, _servicio, _canal, null!));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Despacho(TimeSpan.Zero));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Despacho(TimeSpan.FromSeconds(-1)));
            Assert.AreEqual("prueba", Despacho().Canal);
            Assert.AreEqual(TimeSpan.FromSeconds(5), ServicioDespachoOrdenes.TiempoMaximoPredeterminado);
        }
    }
}
