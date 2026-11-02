using System;
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
    public sealed class ConsultaUsuariosTests
    {
        [TestMethod]
        public async Task RevalidaPermisoDespuesDeUnaConsultaAutorizada()
        {
            var repo = new RepositorioUsuariosEnMemoria();
            repo.Agregar(1, true, CodigoRol.Administrador, CodigoRol.Ciudadano);
            var servicio = new ServicioUsuarios(repo, TimeProvider.System);
            Assert.IsTrue((await servicio.ConsultarPaginaAsync(1, new())).Exitoso);
            repo.CambiarGuardado(1, u => u.QuitarRol(CodigoRol.Administrador));
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await servicio.ConsultarPaginaAsync(1, new())).Motivo);
            Assert.AreEqual(1, repo.ConsultasPagina, "Una lectura rechazada no debe consultar la página con datos personales.");
        }

        [TestMethod]
        public async Task CuentaInactivaNoConsultaDatosPersonales()
        {
            var repo = new RepositorioUsuariosEnMemoria();
            repo.Agregar(1, false, CodigoRol.Administrador);
            var servicio = new ServicioUsuarios(repo, TimeProvider.System);
            Assert.AreEqual(MotivoRechazo.ActorInactivo, (await servicio.ConsultarPaginaAsync(1, new())).Motivo);
            Assert.AreEqual(0, repo.ConsultasPagina);
        }

        [TestMethod]
        public async Task CancelacionPreviaNoLlegaALaConsulta()
        {
            var repo = new RepositorioUsuariosEnMemoria();
            var servicio = new ServicioUsuarios(repo, TimeProvider.System);
            using var cancelar = new CancellationTokenSource();
            cancelar.Cancel();
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => servicio.ConsultarPaginaAsync(1, new(), cancelar.Token));
            Assert.AreEqual(0, repo.ConsultasPagina);
        }

        [TestMethod]
        public void FiltroRechazaLimitesYRolInvalidoAntesDeAccederAlRepositorio()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FiltroUsuarios(pagina: 0));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FiltroUsuarios(tamanoPagina: 101));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FiltroUsuarios(tamanoPagina: 0));
            Assert.ThrowsExactly<ArgumentException>(() => new FiltroUsuarios(rol: (CodigoRol)999));
            Assert.ThrowsExactly<ArgumentException>(() => new FiltroUsuarios(new string('x', 255)));
            var filtro = new FiltroUsuarios("  PRUEBA_%  ", pagina: int.MaxValue, tamanoPagina: 100);
            Assert.AreEqual("prueba_%", filtro.Texto);
            Assert.AreEqual(214748364600L, filtro.Desplazamiento, "El desplazamiento no debe desbordar Int32.");
        }
    }
}
