using System;
using System.Linq;
using EcoAccess.P3.Bot;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class DecisionSupervisorTests
    {
        private static readonly DateTime Ahora = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

        private static AccionSupervisor Decidir(bool vivo, TimeSpan? sinLatido = null, bool red = false, string? lineas = null, int reinicios = 0) =>
            DecisionSupervisor.Decidir(vivo, sinLatido, red, lineas, Enumerable.Range(1, reinicios).Select(i => Ahora.AddMinutes(-i)).ToArray(), Ahora);

        [TestMethod]
        public void UnServicioSanoNoSeToca() => Assert.AreEqual(AccionSupervisor.Nada, Decidir(true, TimeSpan.FromSeconds(30)));

        [TestMethod]
        public void UnServicioQueTerminoSeReinicia() => Assert.AreEqual(AccionSupervisor.Reiniciar, Decidir(false));

        [TestMethod]
        public void UnBotQueDejoDePedirMensajesSeReinicia() => Assert.AreEqual(AccionSupervisor.Reiniciar, Decidir(true, TimeSpan.FromMinutes(5)));

        [TestMethod]
        public void SinInternetNoSeReinicia_ElBucleYaReintenta() => Assert.AreEqual(AccionSupervisor.Nada, Decidir(true, TimeSpan.FromMinutes(30), red: true));

        [TestMethod]
        [DataRow("Webhook activo incompatible con polling.")]
        [DataRow("otra instancia puede estar activa")]
        [DataRow("Telegram respondió 409")]
        [DataRow("Telegram rechazó el token")]
        public void UnConflictoOTokenRechazadoNoSeInsiste_Evita2Consumidores(string lineas) =>
            Assert.AreEqual(AccionSupervisor.Detener, Decidir(false, lineas: lineas));

        [TestMethod]
        public void HayUnLimiteDeReinicios()
        {
            Assert.AreEqual(AccionSupervisor.Reiniciar, Decidir(false, reinicios: DecisionSupervisor.MaximoReinicios - 1));
            Assert.AreEqual(AccionSupervisor.Detener, Decidir(false, reinicios: DecisionSupervisor.MaximoReinicios));
        }

        [TestMethod]
        public void LosReiniciosAntiguosNoCuentan()
        {
            var viejos = Enumerable.Range(0, 10).Select(i => Ahora.AddHours(-2)).ToArray();
            Assert.AreEqual(AccionSupervisor.Reiniciar, DecisionSupervisor.Decidir(false, null, false, null, viejos, Ahora));
        }
    }
}
