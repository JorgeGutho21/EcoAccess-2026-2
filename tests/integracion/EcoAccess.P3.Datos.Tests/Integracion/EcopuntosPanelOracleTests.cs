using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Repositorios;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    public sealed partial class EcopuntosOracleTests
    {
        [TestMethod]
        public async Task PanelDistingueCreditosHistoricosDelSaldoTrasUnReverso()
        {
            var e = await PrepararAsync();
            var panel = new RepositorioPanelAdministrativoOracle(Fabrica);
            var antes = (await panel.ConsultarAsync()).Indicadores;
            var credito = Credito(await e.Servicio.AcreditarAsync(e.Operario, e.Deposito, e.Regla, "panel-" + Sufijo()));
            Assert.IsTrue((await e.Servicio.RevertirAsync(e.Administrador, credito.Id, "panel-rev-" + Sufijo(),
                "Reverso del crédito ficticio de prueba del panel.")).Exitoso);
            var despues = (await panel.ConsultarAsync()).Indicadores;
            Assert.AreEqual(antes.SaldoNetoEcopuntos, despues.SaldoNetoEcopuntos);
            Assert.AreEqual(antes.CreditosOtorgados + 10, despues.CreditosOtorgados);
        }
    }
}
