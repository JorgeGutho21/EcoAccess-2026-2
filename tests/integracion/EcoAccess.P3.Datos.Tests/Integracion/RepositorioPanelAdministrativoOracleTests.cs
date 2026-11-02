using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    [TestClass]
    [TestCategory(Categoria)]
    [DoNotParallelize]
    public sealed class RepositorioPanelAdministrativoOracleTests : BaseIntegracionOracle
    {
        [TestMethod]
        public async Task MaterialCreadoYDeshabilitadoSeReflejaEnIndicadorSinDatosDecorativos()
        {
            var panel = new RepositorioPanelAdministrativoOracle(Fabrica);
            var materiales = new RepositorioMaterialesOracle(Fabrica);
            var antes = await panel.ConsultarAsync();
            int id = RegistrarMaterial(await materiales.AgregarAsync(new Material("P" + Sufijo(), "Material panel ficticio", CategoriaMaterial.Aprovechable, null)));
            var despues = await panel.ConsultarAsync();
            Assert.AreEqual(antes.Indicadores.MaterialesHabilitados + 1, despues.Indicadores.MaterialesHabilitados);
            await materiales.DeshabilitarAsync(id);
            Assert.AreEqual(antes.Indicadores.MaterialesHabilitados, (await panel.ConsultarAsync()).Indicadores.MaterialesHabilitados);
            Assert.IsLessThanOrEqualTo(8, despues.Actividad.Count);
            Assert.IsLessThanOrEqualTo(despues.Indicadores.Depositos, despues.Indicadores.Verificados);
        }

        [TestMethod]
        public async Task CancelacionPreviaNoDevuelveResumenInventado()
        {
            using var cancelacion = new CancellationTokenSource();
            cancelacion.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                new RepositorioPanelAdministrativoOracle(Fabrica).ConsultarAsync(cancelacion.Token));
        }
    }
}
