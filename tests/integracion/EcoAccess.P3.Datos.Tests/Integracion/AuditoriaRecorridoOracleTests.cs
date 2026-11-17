using System;
using System.Linq;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Repositorios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    // Todas las consultas de la auditoría contra el esquema real; no escribe.
    [TestClass]
    [TestCategory(Categoria)]
    [DoNotParallelize]
    public sealed class AuditoriaRecorridoOracleTests : BaseIntegracionOracle
    {
        [TestMethod]
        public async Task ConFechaFutura_NoCuentaNada()
        {
            var a = await new RepositorioAuditoriaRecorridoOracle(Fabrica).ConsultarAsync(DateTime.UtcNow.AddDays(1));
            Assert.IsEmpty(a.InteraccionesPorCanalYEstado);
            Assert.AreEqual(0, a.Depositos);
            Assert.IsTrue(a.SinDuplicados);
        }

        [TestMethod]
        public async Task ConFechaAntigua_LasConsultasCorrenYLasRestriccionesImpidenRepetidos()
        {
            var a = await new RepositorioAuditoriaRecorridoOracle(Fabrica).ConsultarAsync(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            Assert.AreEqual(0, a.InteraccionesConVariasOrdenes);
            Assert.AreEqual(0, a.OrdenesConVariosDepositos);
            Assert.AreEqual(0, a.DepositosConVariosCreditos);
            Assert.IsTrue(a.InteraccionesPorCanalYEstado.All(c => c.Cantidad > 0));
        }
    }
}
