using System;
using System.Collections.Generic;
using System.Linq;
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
    public sealed class CanecaSimuladaTests
    {
        private const int Ciudadano = 3;
        private static readonly DateTimeOffset Inicio = new(2026, 3, 10, 8, 30, 0, TimeSpan.Zero);

        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioMaterialesEnMemoria _materiales = new();
        private readonly RepositorioContenedoresEnMemoria _contenedores = new();
        private readonly RepositorioInteraccionesEnMemoria _repositorio = new();
        private readonly RelojAjustable _reloj = new(Inicio);
        private readonly ServicioInteracciones _servicio;
        private readonly CanecaSimulada _simulada;
        private readonly ServicioDespachoOrdenes _despacho;
        private readonly Material _pet;
        private readonly Contenedor _caneca;

        public CanecaSimuladaTests()
        {
            _usuarios.AgregarCiudadano(Ciudadano);
            _materiales.Contenedores = _contenedores;
            _contenedores.Materiales = _materiales;
            _pet = _materiales.Sembrar("PET");
            _caneca = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _pet.Id);
            _repositorio.Contenedores = _contenedores;
            _repositorio.Materiales = _materiales;
            _servicio = new ServicioInteracciones(_usuarios, _repositorio, _contenedores, _materiales, _reloj);
            _simulada = new CanecaSimulada(_caneca.Id, _servicio.RegistrarEventoAsync, _reloj);
            _despacho = new ServicioDespachoOrdenes(_repositorio, _servicio, _simulada, _reloj);
        }

        private async Task<int> OrdenDespachadaAsync(string clave = "clave-0001")
        {
            var inicio = await _servicio.IniciarAsync(Ciudadano, _caneca.Id, CanalInteraccion.Simulador);
            Assert.IsTrue(inicio.Exitoso, inicio.ToString());
            var orden = await _servicio.AutorizarAperturaAsync(Ciudadano, inicio.Valor, _pet.Id, clave);
            Assert.IsTrue(orden.Exitoso, orden.ToString());
            Assert.AreEqual(ResultadoDespacho.Enviada, await _despacho.DespacharAsync(orden.Valor));
            return orden.Valor;
        }

        private EstadoOrdenApertura Estado(int idOrden) => _repositorio.LeerOrden(idOrden)!.Estado;

        private EstadoInteraccion EstadoDeInteraccion(int idOrden) =>
            _repositorio.LeerInteraccion(_repositorio.LeerOrden(idOrden)!.IdInteraccion)!.Estado;

        private static ResultadoEvento[] Resultados(IReadOnlyList<Resultado<EventoRegistrado>> respuestas)
        {
            Assert.IsTrue(respuestas.All(r => r.Exitoso), string.Join("; ", respuestas));
            return respuestas.Select(r => r.Valor.Resultado).ToArray();
        }

        [TestMethod]
        public async Task RecepcionEnElInstanteExactoDelVencimientoNoEncola()
        {
            var comando = new ComandoApertura(1, _caneca.Id, "clave-limite", _reloj.Ahora.UtcDateTime);
            Assert.IsFalse(await _simulada.EnviarAperturaAsync(comando));
            Assert.IsEmpty(_simulada.ComandosRecibidos);
        }

        [TestMethod]
        public async Task EjecucionEnElInstanteExactoDelVencimientoNoGeneraEventos()
        {
            int idOrden = await OrdenDespachadaAsync();
            _reloj.Avanzar(ServicioInteracciones.VigenciaOrden);
            Assert.IsEmpty(await _simulada.EjecutarAsync(idOrden));
            Assert.AreEqual(EstadoOrdenApertura.Enviada, Estado(idOrden));
            Assert.IsNull(await _repositorio.ObtenerDepositoPorOrdenAsync(idOrden));
        }

        [TestMethod]
        public async Task ConsultaAutoritativaNoRearmaUnaOrdenAcusadaAbiertaODetectada()
        {
            int idOrden = await OrdenDespachadaAsync();
            var o = _repositorio.LeerOrden(idOrden)!;
            var comando = new ComandoApertura(o.Id, o.IdContenedor, o.ClaveIdempotencia, o.FechaVencimientoUtc);
            Assert.IsTrue(await _servicio.SigueAutorizadoComandoAsync(comando));
            foreach (var tipo in new[] { TipoEventoDispositivo.Acuse, TipoEventoDispositivo.Apertura, TipoEventoDispositivo.Deteccion })
            {
                Assert.IsTrue((await _servicio.RegistrarEventoAsync(new EventoRecibido(_caneca.Id, "gate-" + tipo, tipo, idOrden, _reloj.Ahora.UtcDateTime))).Exitoso);
                Assert.IsFalse(await _servicio.SigueAutorizadoComandoAsync(comando));
            }
        }

        [TestMethod]
        public async Task ConsultaAutoritativaRechazaContenidoDistintoYVencimientoExacto()
        {
            int idOrden = await OrdenDespachadaAsync(); var o = _repositorio.LeerOrden(idOrden)!;
            var comando = new ComandoApertura(o.Id, o.IdContenedor, o.ClaveIdempotencia, o.FechaVencimientoUtc);
            foreach (var distinto in new[]{comando with{IdOrden=999},comando with{IdContenedor=999},
                comando with{ClaveIdempotencia="otra-clave"},comando with{VenceUtc=comando.VenceUtc.AddTicks(1)},
                comando with{VenceUtc=DateTime.SpecifyKind(comando.VenceUtc,DateTimeKind.Unspecified)}})
                Assert.IsFalse(await _servicio.SigueAutorizadoComandoAsync(distinto));
            using var ct = new CancellationTokenSource(); ct.Cancel();
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => _servicio.SigueAutorizadoComandoAsync(comando, ct.Token));
            _reloj.Avanzar(ServicioInteracciones.VigenciaOrden);
            Assert.IsFalse(await _servicio.SigueAutorizadoComandoAsync(comando));
        }

        [TestMethod]
        [DataRow("inactivo")]
        [DataRow("sin-rol")]
        [DataRow("sin-consentimiento")]
        [DataRow("fuera-servicio")]
        [DataRow("material-deshabilitado")]
        [DataRow("material-no-admitido")]
        public async Task ConsultaAutoritativaRevalidaCambiosPosteriores(string cambio)
        {
            int idOrden = await OrdenDespachadaAsync(); var o = _repositorio.LeerOrden(idOrden)!;
            var comando = new ComandoApertura(o.Id, o.IdContenedor, o.ClaveIdempotencia, o.FechaVencimientoUtc);
            Assert.IsTrue(await _servicio.SigueAutorizadoComandoAsync(comando));
            var otro = _materiales.Sembrar("OTRO"); await _contenedores.AdmitirMaterialAsync(_caneca.Id, otro.Id);
            switch (cambio)
            {
                case "inactivo": _usuarios.CambiarGuardado(Ciudadano, u => u.Desactivar()); break;
                case "sin-rol": _usuarios.CambiarGuardado(Ciudadano, u => { u.AsignarRol(CodigoRol.Operario); u.QuitarRol(CodigoRol.Ciudadano); }); break;
                case "sin-consentimiento": _usuarios.CambiarGuardado(Ciudadano, u => u.RevocarConsentimiento()); break;
                case "fuera-servicio": await _contenedores.CambiarEstadoAsync(_caneca.Id, EstadoContenedor.FueraDeServicio); break;
                case "material-deshabilitado": await _materiales.DeshabilitarAsync(_pet.Id); break;
                case "material-no-admitido": await _contenedores.QuitarMaterialAsync(_caneca.Id, _pet.Id); break;
            }
            Assert.IsFalse(await _servicio.SigueAutorizadoComandoAsync(comando));
            Assert.IsEmpty(_repositorio.Eventos); Assert.IsEmpty(_repositorio.Depositos);
        }

        [TestMethod]
        public async Task BarridoDelBridgeSoloVenceSuContenedorConfigurado()
        {
            int primera = await OrdenDespachadaAsync();
            var otra = _contenedores.Sembrar(2, EstadoContenedor.EnServicio, _pet.Id);
            var inicio = await _servicio.IniciarAsync(Ciudadano, otra.Id, CanalInteraccion.Simulador);
            var segunda = await _servicio.AutorizarAperturaAsync(Ciudadano, inicio.Valor, _pet.Id, "otra-caneca");
            Assert.IsTrue(segunda.Exitoso);
            _reloj.Avanzar(ServicioInteracciones.VigenciaOrden);
            Assert.AreEqual(1, await _servicio.VencerOrdenesAsync(idContenedor: _caneca.Id));
            Assert.AreEqual(EstadoOrdenApertura.Vencida, Estado(primera));
            Assert.AreEqual(EstadoOrdenApertura.Emitida, Estado(segunda.Valor));
        }

        [TestMethod]
        public async Task RecibirElComandoNoEmiteEventos()
        {
            int idOrden = await OrdenDespachadaAsync();

            Assert.HasCount(1, _simulada.ComandosRecibidos);
            Assert.IsEmpty(_repositorio.Eventos);
            Assert.AreEqual(EstadoOrdenApertura.Enviada, Estado(idOrden));
        }

        [TestMethod]
        public async Task EscenarioNormalCreaUnDepositoSinVerificarConElMaterialDeLaPersona()
        {
            _simulada.PesoMedidoKg = 0.250m;
            int idOrden = await OrdenDespachadaAsync();

            var respuestas = await _simulada.EjecutarAsync(idOrden);

            CollectionAssert.AreEqual(Enumerable.Repeat(ResultadoEvento.Aplicado, 4).ToArray(), Resultados(respuestas));
            Deposito deposito = _repositorio.Depositos.Single();
            Assert.AreEqual(idOrden, deposito.IdOrden);
            Assert.AreEqual(_pet.Id, deposito.IdMaterialDeclarado, "el material lo declaró la persona, no la caneca");
            Assert.IsNull(deposito.IdMaterialVerificado, "la caneca no verifica el material");
            Assert.AreEqual(0.250m, deposito.PesoMedidoKg);
            Assert.IsNull(deposito.PesoEstimadoKg);
            Assert.AreEqual(EstadoOrdenApertura.Cerrada, Estado(idOrden));
            Assert.AreEqual(EstadoInteraccion.ConDeposito, EstadoDeInteraccion(idOrden));
        }

        [TestMethod]
        public async Task SinBasculaElDepositoNoTienePesoMedido()
        {
            int idOrden = await OrdenDespachadaAsync();

            await _simulada.EjecutarAsync(idOrden);

            Assert.IsNull(_repositorio.Depositos.Single().PesoMedidoKg);
        }

        [TestMethod]
        public async Task SinAcuseLaOrdenSigueEnviadaYNoHayDeposito()
        {
            _simulada.Escenario = EscenarioCaneca.SinAcuse;
            int idOrden = await OrdenDespachadaAsync();

            Assert.IsEmpty((await _simulada.EjecutarAsync(idOrden)));
            Assert.AreEqual(EstadoOrdenApertura.Enviada, Estado(idOrden));
            Assert.IsEmpty(_repositorio.Depositos);
        }

        [TestMethod]
        public async Task AcuseSinAperturaNoCreaDeposito()
        {
            _simulada.Escenario = EscenarioCaneca.AcuseSinApertura;
            int idOrden = await OrdenDespachadaAsync();

            CollectionAssert.AreEqual(new[] { ResultadoEvento.Aplicado }, Resultados(await _simulada.EjecutarAsync(idOrden)));
            Assert.AreEqual(EstadoOrdenApertura.Acusada, Estado(idOrden));
            Assert.IsEmpty(_repositorio.Depositos);
        }

        [TestMethod]
        public async Task AperturaSinDeteccionCierraSinDeposito()
        {
            _simulada.Escenario = EscenarioCaneca.AperturaSinDeteccion;
            int idOrden = await OrdenDespachadaAsync();

            Resultados(await _simulada.EjecutarAsync(idOrden));

            Assert.AreEqual(EstadoOrdenApertura.Cerrada, Estado(idOrden));
            Assert.AreEqual(EstadoInteraccion.SinDeposito, EstadoDeInteraccion(idOrden));
            Assert.IsEmpty(_repositorio.Depositos);
        }

        [TestMethod]
        public async Task FalloDelDispositivoCierraSinDeposito()
        {
            _simulada.Escenario = EscenarioCaneca.Fallo;
            int idOrden = await OrdenDespachadaAsync();

            Resultados(await _simulada.EjecutarAsync(idOrden));

            Assert.AreEqual(EstadoOrdenApertura.Fallida, Estado(idOrden));
            Assert.AreEqual(EstadoInteraccion.SinDeposito, EstadoDeInteraccion(idOrden));
            Assert.IsEmpty(_repositorio.Depositos);
        }

        [TestMethod]
        public async Task EventosDuplicadosSeMarcanRepetidosYNoDuplicanElDeposito()
        {
            _simulada.Escenario = EscenarioCaneca.Duplicados;
            int idOrden = await OrdenDespachadaAsync();

            var respuestas = await _simulada.EjecutarAsync(idOrden);

            Assert.HasCount(8, respuestas);
            for (int i = 0; i < respuestas.Count; i += 2)
            {
                Assert.IsFalse(respuestas[i].Valor.Repetido);
                Assert.IsTrue(respuestas[i + 1].Valor.Repetido);
                Assert.AreEqual(respuestas[i].Valor.IdEvento, respuestas[i + 1].Valor.IdEvento);
            }

            Assert.HasCount(1, _repositorio.Depositos);
            Assert.HasCount(4, _repositorio.Eventos);
        }

        [TestMethod]
        public async Task DeteccionAntesDeLaAperturaQuedaEnRevisionSinDeposito()
        {
            _simulada.Escenario = EscenarioCaneca.FueraDeOrden;
            int idOrden = await OrdenDespachadaAsync();

            var respuestas = await _simulada.EjecutarAsync(idOrden);

            CollectionAssert.AreEqual(
                new[] { ResultadoEvento.Aplicado, ResultadoEvento.Revision, ResultadoEvento.Aplicado, ResultadoEvento.Aplicado },
                Resultados(respuestas));
            Assert.IsNotNull(respuestas[1].Valor.MotivoRevision);
            Assert.IsEmpty(_repositorio.Depositos);
            Assert.AreEqual(EstadoInteraccion.SinDeposito, EstadoDeInteraccion(idOrden));
        }

        [TestMethod]
        public async Task CanecaDesconectadaNoRecibeElComando()
        {
            _simulada.Escenario = EscenarioCaneca.Desconectada;
            var inicio = await _servicio.IniciarAsync(Ciudadano, _caneca.Id, CanalInteraccion.Simulador);
            int idOrden = (await _servicio.AutorizarAperturaAsync(Ciudadano, inicio.Valor, _pet.Id, "clave-0001")).Valor;

            Assert.AreEqual(ResultadoDespacho.NoEntregada, await _despacho.DespacharAsync(idOrden));
            Assert.IsEmpty(_simulada.ComandosRecibidos);
            Assert.IsEmpty((await _simulada.EjecutarAsync(idOrden)));
            Assert.AreEqual(EstadoOrdenApertura.Emitida, Estado(idOrden));
        }

        [TestMethod]
        public async Task SinComandoLaCanecaNoSeAbre()
        {
            var inicio = await _servicio.IniciarAsync(Ciudadano, _caneca.Id, CanalInteraccion.Simulador);
            int idOrden = (await _servicio.AutorizarAperturaAsync(Ciudadano, inicio.Valor, _pet.Id, "clave-0001")).Valor;

            Assert.IsEmpty((await _simulada.EjecutarAsync(idOrden)));
            Assert.IsEmpty(_repositorio.Eventos);
        }

        [TestMethod]
        public async Task LaMismaOrdenNoSeEjecutaDosVeces()
        {
            int idOrden = await OrdenDespachadaAsync();
            await _simulada.EjecutarAsync(idOrden);

            Assert.IsEmpty((await _simulada.EjecutarAsync(idOrden)));
            Assert.HasCount(4, _repositorio.Eventos);
            Assert.HasCount(1, _repositorio.Depositos);
        }

        [TestMethod]
        public async Task ComandoVencidoNoAbreLaCaneca()
        {
            int idOrden = await OrdenDespachadaAsync();
            _reloj.Avanzar(ServicioInteracciones.VigenciaOrden + TimeSpan.FromSeconds(1));

            Assert.IsEmpty((await _simulada.EjecutarAsync(idOrden)));
            Assert.IsEmpty(_repositorio.Eventos);
        }

        [TestMethod]
        public async Task ComandoDeOtraCanecaSeRechaza()
        {
            var comando = new ComandoApertura(1, _caneca.Id + 1, "clave-0001", _reloj.Ahora.UtcDateTime.AddMinutes(2));

            Assert.IsFalse(await _simulada.EnviarAperturaAsync(comando));
            Assert.IsEmpty(_simulada.ComandosRecibidos);
        }

        [TestMethod]
        public async Task ComandosRecibidosConservanElOrdenDeLlegadaSinRepetidos()
        {
            DateTime vence = _reloj.Ahora.UtcDateTime.AddMinutes(2);
            foreach (int id in new[] { 30, 10, 20, 10 })
            {
                Assert.IsTrue(await _simulada.EnviarAperturaAsync(new ComandoApertura(id, _caneca.Id, $"clave-{id:0000}", vence)));
            }

            CollectionAssert.AreEqual(new[] { 30, 10, 20 }, _simulada.ComandosRecibidos.Select(c => c.IdOrden).ToArray());
        }

        [TestMethod]
        public async Task IdentificadoresExternosSonDeterministasYUnicos()
        {
            int primera = await OrdenDespachadaAsync("clave-0001");
            await _simulada.EjecutarAsync(primera);
            int segunda = await OrdenDespachadaAsync("clave-0002");
            await _simulada.EjecutarAsync(segunda);

            string[] ids = _repositorio.Eventos.Select(e => e.IdExterno).ToArray();
            CollectionAssert.AreEquivalent(new[] { primera, segunda }.SelectMany(orden =>
                new[] { TipoEventoDispositivo.Acuse, TipoEventoDispositivo.Apertura, TipoEventoDispositivo.Deteccion, TipoEventoDispositivo.Cierre }
                    .Select(tipo => $"sim-{_caneca.Id}-{orden}-{(int)tipo}")).ToArray(), ids);
            Assert.HasCount(2, _repositorio.Depositos);
        }

        [TestMethod]
        public void ConstructorValidaArgumentos()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CanecaSimulada(0, _servicio.RegistrarEventoAsync, _reloj));
            Assert.ThrowsExactly<ArgumentNullException>(() => new CanecaSimulada(1, null!, _reloj));
            Assert.ThrowsExactly<ArgumentNullException>(() => new CanecaSimulada(1, _servicio.RegistrarEventoAsync, null!));
            Assert.AreEqual("simulador", _simulada.Canal);
        }
    }
}
