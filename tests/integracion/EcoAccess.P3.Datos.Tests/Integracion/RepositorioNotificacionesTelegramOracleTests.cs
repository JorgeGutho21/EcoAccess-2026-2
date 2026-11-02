using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    [TestClass]
    [TestCategory(Categoria)]
    public sealed class RepositorioNotificacionesTelegramOracleTests : BaseIntegracionOracle
    {
        /// <summary>Solo lectura: la consulta compila en Oracle y cada hecho apunta a un Telegram vinculado vigente.</summary>
        [TestMethod]
        public async Task HechosRecientesTienenClaveUnicaYDestinatarioVinculado()
        {
            var hechos = await new RepositorioNotificacionesTelegramOracle(Fabrica).ListarDesdeAsync(DateTime.UtcNow.AddDays(-30));
            Assert.AreEqual(hechos.Count, hechos.Select(h => h.Clave).Distinct().Count(), "Una clave por hecho.");
            foreach (var hecho in hechos)
            {
                Assert.IsGreaterThan(0L, hecho.TelegramUserId);
                Assert.IsTrue(Compat.IsDefined(hecho.Tipo));
                Assert.IsTrue(hecho.Clave.Length > 3 && hecho.Clave[2] == ':');
                if (hecho.Tipo == TipoHechoNotificable.EcopuntosAcreditados) Assert.IsNotNull(hecho.Saldo);
            }

            int vigentes = await ContarAsync("SELECT COUNT(*) FROM vinculo_telegram WHERE fecha_baja_utc IS NULL");
            if (vigentes == 0) Assert.IsEmpty(hechos, "Sin vínculos vigentes no hay destinatarios.");
        }
    }
}
