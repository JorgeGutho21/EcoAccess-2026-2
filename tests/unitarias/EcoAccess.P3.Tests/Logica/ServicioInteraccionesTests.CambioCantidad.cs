using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioInteraccionesTests
    {
        [TestMethod]
        public async Task DeclararCantidad_ConservaSolicitudYSugerenciaSinOrdenNiCredito()
        {
            int id = await IniciarAsync();
            Assert.IsTrue((await _servicio.RegistrarSugerenciaAsync(Ciudadano, id, _pet.Id, .8m)).Exitoso);
            Assert.IsTrue((await _servicio.DeclararCantidadAsync(Ciudadano, id, 3)).Exitoso);
            Assert.IsTrue((await _servicio.DeclararCantidadAsync(Ciudadano, id, 3)).Exitoso);
            var i = _repositorio.LeerInteraccion(id)!;
            Assert.AreEqual(3, i.CantidadDeclarada); Assert.AreEqual(_pet.Id, i.IdMaterialSugerido);
            Assert.AreEqual(EstadoInteraccion.Iniciada, i.Estado); Assert.IsEmpty(_repositorio.Ordenes); Assert.IsEmpty(_repositorio.Depositos);
        }
        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(1001)]
        public async Task CambiarCantidadInvalida_NoReemplazaValorAnterior(int cantidad)
        {
            int id = (await _servicio.IniciarConCantidadAsync(Ciudadano, _caneca.Id, CanalInteraccion.Simulador, 3)).Valor;
            Assert.IsFalse((await _servicio.DeclararCantidadAsync(Ciudadano, id, cantidad)).Exitoso);
            Assert.AreEqual(3, _repositorio.LeerInteraccion(id)!.CantidadDeclarada);
        }
        [TestMethod]
        public async Task DeclararCantidad_NoPermiteOtraCuentaNiCuentaSinConsentimientoNiOrdenEmitida()
        {
            int id = await IniciarAsync();
            Assert.IsFalse((await _servicio.DeclararCantidadAsync(OtroCiudadano, id, 3)).Exitoso);
            Assert.IsFalse((await _servicio.DeclararCantidadAsync(SinConsentimiento, id, 3)).Exitoso);
            Assert.IsNull(_repositorio.LeerInteraccion(id)!.CantidadDeclarada);
            Assert.IsTrue((await _servicio.DeclararCantidadAsync(Ciudadano, id, 3)).Exitoso);
            Assert.IsFalse((await _servicio.AutorizarDeclaracionAsync(Ciudadano, id, _pet.Id, 4, Clave)).Exitoso);
            int orden = (await _servicio.AutorizarDeclaracionAsync(Ciudadano, id, _pet.Id, 3, Clave)).Valor;
            Assert.IsFalse((await _servicio.DeclararCantidadAsync(Ciudadano, id, 4)).Exitoso);
            Assert.AreEqual(orden, (await _servicio.AutorizarDeclaracionAsync(Ciudadano, id, _pet.Id, 3, Clave)).Valor);
            Assert.HasCount(1, _repositorio.Ordenes);
        }
        [TestMethod]
        public async Task CantidadCambiaEntreLecturaYEmision_NoDejaOrdenOculta()
        {
            int id = (await _servicio.IniciarConCantidadAsync(Ciudadano, _caneca.Id, CanalInteraccion.Simulador, 3)).Valor;
            _repositorio.AlEscribir = () => Assert.IsTrue(_repositorio.CambiarCantidadDeclaradaAsync(Ciudadano, id, 3, 4).GetAwaiter().GetResult());
            Assert.IsFalse((await _servicio.AutorizarDeclaracionAsync(Ciudadano, id, _pet.Id, 3, Clave)).Exitoso);
            Assert.AreEqual(4, _repositorio.LeerInteraccion(id)!.CantidadDeclarada);
            Assert.IsEmpty(_repositorio.Ordenes);
        }
        [TestMethod]
        public async Task RegistrarSugerenciaConcurrente_NoPisaDeclaracionNueva()
        {
            int id = (await _servicio.IniciarConCantidadAsync(Ciudadano, _caneca.Id, CanalInteraccion.Simulador, 3)).Valor;
            _repositorio.AlEscribir = () => _repositorio.CambiarCantidadDeclaradaAsync(Ciudadano, id, 3, 4).GetAwaiter().GetResult();
            Assert.IsTrue((await _servicio.RegistrarSugerenciaAsync(Ciudadano, id, _pet.Id, .8m)).Exitoso);
            Assert.AreEqual(4, _repositorio.LeerInteraccion(id)!.CantidadDeclarada);
        }
    }
}
