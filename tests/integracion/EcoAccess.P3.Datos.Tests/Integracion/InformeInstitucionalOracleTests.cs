using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    [TestClass, TestCategory(Categoria), DoNotParallelize]
    public sealed class InformeInstitucionalOracleTests : BaseIntegracionOracle
    {
        [TestMethod]
        public async Task VistasSoloLecturaEIndicadoresAgregadosSeConsultanSinDatosPersonales()
        {
            using var c = await Fabrica.AbrirAsync(CancellationToken.None);
            using var cmd = ComandosOracle.Crear(c, "SELECT COUNT(*) FROM user_views WHERE view_name IN ('EA_R_DEPOSITOS','EA_R_PUNTOS','EA_R_OPINIONES','EA_R_BASE_DEPOSITOS','EA_R_BASE_PUNTOS','EA_R_SOLICITUDES') AND read_only='Y'");
            Assert.AreEqual(6, ConversionOracle.Entero((await cmd.ExecuteScalarAsync())!));
            var repo = new RepositorioInformeInstitucionalOracle(Fabrica);
            var f = new FiltroInforme(new(2026, 10, 1, 5, 0, 0, DateTimeKind.Utc), new(2026, 11, 2, 5, 0, 0, DateTimeKind.Utc));
            var datos = await repo.ConsultarAsync(f);
            Assert.IsTrue(datos.Depositos >= datos.Verificados && datos.Ciudadanos <= datos.Depositos);
            foreach (var agrupacion in new[] { AgrupacionDepositos.Dia, AgrupacionDepositos.Material, AgrupacionDepositos.Contenedor, AgrupacionDepositos.Punto })
                Assert.AreEqual(datos.Depositos, (await repo.AgruparAsync(f, agrupacion)).Sum(x => (long)x.Depositos));
            var vacio = await repo.ConsultarAsync(f with { IdContenedor = int.MaxValue });
            Assert.AreEqual(0, vacio.Depositos); Assert.AreEqual(0, vacio.Ciudadanos);
        }
    }
}
