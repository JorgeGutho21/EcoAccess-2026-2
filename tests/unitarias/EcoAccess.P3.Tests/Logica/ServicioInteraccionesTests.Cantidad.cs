using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioInteraccionesTests
    {
        [TestMethod]
        public async Task EspecialSeAutorizaSiLaCanecaLoAdmite()
        {
            var especial = _materiales.Sembrar("JERINGA", "Jeringa", CategoriaMaterial.Peligroso);
            var caneca = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _pet.Id, especial.Id);
            int solicitud = (await _servicio.IniciarAsync(Ciudadano, caneca.Id, CanalInteraccion.Simulador)).Valor;
            Assert.IsTrue((await _servicio.AutorizarAperturaAsync(Ciudadano, solicitud, especial.Id, "especial-admitido")).Exitoso);
            Assert.HasCount(1, _repositorio.Ordenes);
        }
        [TestMethod]
        [DataRow(1)]
        [DataRow(3)]
        [DataRow(1000)]
        public async Task Cantidad_SeConservaAlAutorizarSinCrearDeposito(int cantidad)
        {
            var inicio = await _servicio.IniciarConCantidadAsync(Ciudadano, _caneca.Id, CanalInteraccion.Simulador, cantidad);
            Assert.IsTrue(inicio.Exitoso);
            Assert.AreEqual(cantidad, _repositorio.LeerInteraccion(inicio.Valor)!.CantidadDeclarada);
            Assert.IsTrue((await _servicio.AutorizarAperturaAsync(Ciudadano, inicio.Valor, _pet.Id, Clave)).Exitoso);
            Assert.AreEqual(cantidad, _repositorio.LeerInteraccion(inicio.Valor)!.CantidadDeclarada);
            Assert.IsEmpty(_repositorio.Depositos);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(1001)]
        public async Task Cantidad_InvalidaNoPersiste(int cantidad)
        {
            var resultado = await _servicio.IniciarConCantidadAsync(Ciudadano, _caneca.Id, CanalInteraccion.Simulador, cantidad);
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, resultado.Motivo);
            Assert.IsEmpty(await _repositorio.ListarInteraccionesPorUsuarioAsync(Ciudadano));
        }

        [TestMethod]
        public async Task Cantidad_ExigeCiudadanoConsentidoYNoInventaDatoHistorico()
        {
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await _servicio.IniciarConCantidadAsync(SinConsentimiento, _caneca.Id, CanalInteraccion.Simulador, 3)).Motivo);
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await _servicio.IniciarConCantidadAsync(Operario, _caneca.Id, CanalInteraccion.Simulador, 3)).Motivo);
            Assert.IsNull(_repositorio.LeerInteraccion(await IniciarAsync())!.CantidadDeclarada);
        }
    }
}
