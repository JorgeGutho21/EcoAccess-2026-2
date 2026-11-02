using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Repositorios;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    [TestClass]
    [TestCategory(Categoria)]
    public sealed class RepositorioDiagnosticoOracleTests : BaseIntegracionOracle
    {
        [TestMethod]
        public async Task ConsultaIdentificaEsquemaRealYTablasSinEscribir()
        {
            var repositorio = new RepositorioDiagnosticoOracle(Fabrica);
            var estado = await repositorio.ConsultarAsync();
            Assert.AreEqual(EsquemaEsperado, estado.Esquema);
            Assert.AreEqual(PdbEsperado, estado.Contenedor);
            Assert.IsGreaterThanOrEqualTo(0d, estado.LatenciaMilisegundos);
            Assert.IsTrue(estado.Tablas.Contains("MATERIAL"));
            Assert.IsTrue(estado.Tablas.Contains("MOVIMIENTO_ECOPUNTOS"));
            Assert.AreEqual(0, estado.RestriccionesProblematicas);
            Assert.AreEqual(0, estado.ObjetosInvalidos);
            Assert.AreEqual(0, estado.IndicesInvalidos);
        }

        [TestMethod]
        public async Task CancelacionNoProduceInventarioParcial()
        {
            using var cancelacion = new CancellationTokenSource();
            cancelacion.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                new RepositorioDiagnosticoOracle(Fabrica).ConsultarAsync(cancelacion.Token));
        }
    }
}
