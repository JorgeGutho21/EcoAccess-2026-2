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
    public sealed class ServicioPanelAdministrativoTests
    {
        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly PanelEnMemoria _panel = new();

        public ServicioPanelAdministrativoTests()
        {
            _usuarios.Agregar(1, true, CodigoRol.Administrador);
            _usuarios.Agregar(2, true, CodigoRol.Operario);
            _usuarios.Agregar(3, true, CodigoRol.Ciudadano);
            _usuarios.Agregar(4, false, CodigoRol.Administrador);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        public async Task RolOperativoRecibeIndicadoresRealesDelPuerto(int actor)
        {
            var resultado = await new ServicioPanelAdministrativo(_usuarios, _panel).ConsultarAsync(actor);
            Assert.IsTrue(resultado.Exitoso);
            Assert.AreSame(_panel.Resumen, resultado.Valor);
        }

        [TestMethod]
        [DataRow(0, MotivoRechazo.ActorNoEncontrado)]
        [DataRow(99, MotivoRechazo.ActorNoEncontrado)]
        [DataRow(3, MotivoRechazo.SinPermiso)]
        [DataRow(4, MotivoRechazo.ActorInactivo)]
        public async Task CuentaNoAutorizadaNoConsultaIndicadores(int actor, MotivoRechazo motivo)
        {
            var resultado = await new ServicioPanelAdministrativo(_usuarios, _panel).ConsultarAsync(actor);
            Assert.AreEqual(motivo, resultado.Motivo);
            Assert.AreEqual(0, _panel.Llamadas);
        }

        [TestMethod]
        public async Task RevocacionDeRolImpideActualizarLaSesionRecordada()
        {
            var servicio = new ServicioPanelAdministrativo(_usuarios, _panel);
            Assert.IsTrue((await servicio.ConsultarAsync(1)).Exitoso);
            _usuarios.Agregar(1, true, CodigoRol.Ciudadano);
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await servicio.ConsultarAsync(1)).Motivo);
            Assert.AreEqual(1, _panel.Llamadas);
        }

        [TestMethod]
        public async Task CancelacionPreviaNoConsultaNiProduceCeros()
        {
            using var cancelacion = new CancellationTokenSource();
            cancelacion.Cancel();
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
                new ServicioPanelAdministrativo(_usuarios, _panel).ConsultarAsync(1, cancelacion.Token));
            Assert.AreEqual(0, _panel.Llamadas);
        }

        private sealed class PanelEnMemoria : IRepositorioPanelAdministrativo
        {
            internal int Llamadas { get; private set; }
            internal ResumenPanelAdministrativo Resumen { get; } = new(new(4, 1, 3, 7, 2, 9, 1, 15, 20), Array.Empty<ActividadPanel>());
            public Task<ResumenPanelAdministrativo> ConsultarAsync(CancellationToken cancellationToken = default)
            {
                Llamadas++;
                return Task.FromResult(Resumen);
            }
        }
    }
}
