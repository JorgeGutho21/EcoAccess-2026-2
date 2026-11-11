using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// Fase 3: flujo Eco contra una estación de tres tapas simuladas (sin firmware ni motores). Cada orden abre solo la
    /// tapa de su caneca, una caneca sin sensor deja DECLARADA_SIN_SENSOR y los eventos repetidos no duplican nada.
    /// </summary>
    [TestClass]
    public sealed class EstacionSimuladaTests
    {
        private const int Ciudadano = 3;
        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioMaterialesEnMemoria _materiales = new();
        private readonly RepositorioContenedoresEnMemoria _contenedores = new();
        private readonly RepositorioInteraccionesEnMemoria _repositorio = new();
        private readonly RelojAjustable _reloj = new(new DateTimeOffset(2026, 11, 10, 15, 0, 0, TimeSpan.Zero));
        private readonly ServicioInteracciones _servicio;
        private readonly Material _pet, _comida, _servilleta;
        private readonly Contenedor _blanca, _verde, _negra;
        private readonly CanecaSimulada _tapaBlanca, _tapaVerde, _tapaNegra;
        private readonly EstacionSimulada _estacion;
        private readonly ServicioDespachoOrdenes _despacho;

        public EstacionSimuladaTests()
        {
            _usuarios.AgregarCiudadano(Ciudadano);
            _materiales.Contenedores = _contenedores;
            _contenedores.Materiales = _materiales;
            _pet = _materiales.Sembrar("PET");
            _comida = _materiales.Sembrar("ORGANICO", "Restos de comida", CategoriaMaterial.OrganicoAprovechable);
            _servilleta = _materiales.Sembrar("SERVILLETA", "Servilleta usada", CategoriaMaterial.NoAprovechable);
            _blanca = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _pet.Id);
            _verde = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _comida.Id);
            _negra = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _servilleta.Id);
            _repositorio.Contenedores = _contenedores;
            _repositorio.Materiales = _materiales;
            _servicio = new ServicioInteracciones(_usuarios, _repositorio, _contenedores, _materiales, _reloj);
            _tapaBlanca = new CanecaSimulada(_blanca.Id, _servicio.RegistrarEventoAsync, _reloj);
            _tapaVerde = new CanecaSimulada(_verde.Id, _servicio.RegistrarEventoAsync, _reloj);
            _tapaNegra = new CanecaSimulada(_negra.Id, _servicio.RegistrarEventoAsync, _reloj) { Escenario = EscenarioCaneca.SinSensor };
            _estacion = new EstacionSimulada(new[]
            {
                (_blanca.Id, "esp32-piloto-blanca", _tapaBlanca),
                (_verde.Id, "esp32-piloto-verde", _tapaVerde),
                (_negra.Id, "esp32-piloto-negra", _tapaNegra),
            });
            _despacho = new ServicioDespachoOrdenes(_repositorio, _servicio, _estacion, _reloj);
        }

        private async Task<int> DepositarAsync(Contenedor caneca, Material material, string clave)
        {
            var inicio = await _servicio.IniciarAsync(Ciudadano, caneca.Id, CanalInteraccion.Simulador);
            Assert.IsTrue(inicio.Exitoso, inicio.ToString());
            var orden = await _servicio.AutorizarAperturaAsync(Ciudadano, inicio.Valor, material.Id, clave);
            Assert.IsTrue(orden.Exitoso, orden.ToString());
            Assert.AreEqual(ResultadoDespacho.Enviada, await _despacho.DespacharAsync(orden.Valor));
            var eventos = await _estacion.EjecutarAsync(orden.Valor);
            Assert.IsTrue(eventos.All(e => e.Exitoso), string.Join("; ", eventos));
            return orden.Valor;
        }

        [TestMethod]
        public async Task CadaOrdenAbreSoloLaTapaDeSuCaneca()
        {
            await DepositarAsync(_blanca, _pet, "estacion-blanca");
            await DepositarAsync(_verde, _comida, "estacion-verde");

            Assert.HasCount(1, _tapaBlanca.ComandosRecibidos);
            Assert.HasCount(1, _tapaVerde.ComandosRecibidos);
            Assert.IsEmpty(_tapaNegra.ComandosRecibidos, "Una orden para otra caneca no llega a la negra.");
            Assert.AreEqual(_blanca.Id, _tapaBlanca.ComandosRecibidos.Single().IdContenedor);
            Assert.AreEqual("esp32-piloto-verde", _estacion.DispositivoDe(_verde.Id));
        }

        [TestMethod]
        public async Task UnaOrdenParaOtroContenedorNoSeEntrega()
        {
            var ajena = new ComandoApertura(77, 99_999, "clave-ajena", _reloj.Ahora.UtcDateTime.AddMinutes(1));
            Assert.IsFalse(await _estacion.EnviarAperturaAsync(ajena));
            Assert.IsTrue(new[] { _tapaBlanca, _tapaVerde, _tapaNegra }.All(t => t.ComandosRecibidos.Count == 0));
        }

        [TestMethod]
        public async Task LaCanecaSinSensorDejaDeclaradaSinSensorSinPesoNiVerificacion()
        {
            int orden = await DepositarAsync(_negra, _servilleta, "estacion-negra");
            Deposito d = (await _repositorio.ObtenerDepositoPorOrdenAsync(orden))!;
            Assert.IsTrue(d.SinSensor);
            Assert.AreEqual(EstadoRegistroDeposito.DeclaradaSinSensor, d.EstadoRegistro);
            Assert.AreEqual("DECLARADA_SIN_SENSOR", EstadosRegistroDeposito.Codigo(d.EstadoRegistro));
            Assert.IsNull(d.PesoMedidoKg, "Sin sensor no hay pesaje real.");
        }

        [TestMethod]
        public async Task ConSensorElDepositoQuedaDetectadoYNoDeclarado()
        {
            int orden = await DepositarAsync(_blanca, _pet, "estacion-sensor");
            Deposito d = (await _repositorio.ObtenerDepositoPorOrdenAsync(orden))!;
            Assert.AreEqual(EstadoRegistroDeposito.DetectadaPorSensor, d.EstadoRegistro);
        }

        [TestMethod]
        public async Task EjecutarDosVecesLaMismaOrdenNoCreaOtroDeposito()
        {
            int orden = await DepositarAsync(_negra, _servilleta, "estacion-repetida");
            Assert.IsEmpty(await _estacion.EjecutarAsync(orden), "La misma clave de idempotencia no abre dos veces.");
            Assert.HasCount(1, _repositorio.Depositos);
        }

        [TestMethod]
        public void LaVentanaLogicaEsDeDiezSegundos() => Assert.AreEqual(10, MensajesTelegram.VentanaDepositoSegundos);

        [TestMethod]
        public void UnaEstacionNoAceptaDosTapasConElMismoEsp32()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new EstacionSimulada(new[]
            {
                (_blanca.Id, "esp32-x", _tapaBlanca),
                (_verde.Id, "esp32-x", _tapaVerde),
            }));
        }
    }
}
