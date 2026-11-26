using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Presentacion;

namespace EcoAccess.P3.Tests.Presentacion
{
    [TestClass]
    public sealed class EnlaceDosClicsTests
    {
        private const string Pagina = "https://jorgegutho21.github.io/EcoAccess-2026-2/dos-clics/";

        [TestMethod]
        public void TunelDe_EnlaceDeLaPasarela_DevuelveElTunel() =>
            Assert.AreEqual("https://abc-def.trycloudflare.com",
                EnlaceDosClics.TunelDe(Pagina + "#e=https%3A%2F%2Fabc-def.trycloudflare.com&t=PERMISO"));

        [TestMethod]
        public void TunelDe_ParametroDespuesDelPermiso_TambienSeLee() =>
            Assert.AreEqual("https://x.trycloudflare.com", EnlaceDosClics.TunelDe(Pagina + "#t=P&e=https%3A%2F%2Fx.trycloudflare.com%2F"));

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("https://otro-sitio.example/dos-clics/#e=https%3A%2F%2Fx.trycloudflare.com&t=P")]
        [DataRow(Pagina + "#e=http%3A%2F%2Finseguro.example&t=P")]
        [DataRow(Pagina + "#t=P")]
        [DataRow(Pagina)]
        public void TunelDe_EnlaceAjenoOIncompleto_EsNull(string? enlace) => Assert.IsNull(EnlaceDosClics.TunelDe(enlace));

        [TestMethod]
        public void PaginaSinVincular_EsDosClicsDelSitioOficial() => Assert.AreEqual(Pagina, EnlaceDosClics.PaginaSinVincular);
    }
}
