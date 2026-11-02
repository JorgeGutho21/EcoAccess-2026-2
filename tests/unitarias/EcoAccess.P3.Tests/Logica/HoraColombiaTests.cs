using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{

    [TestClass]
    public sealed class HoraColombiaTests
    {
        [TestMethod]
        public void InicioDelDiaUtc_MedianocheDeBogota_EsCincoDeLaMananaUtc()
        {
            DateTime inicio = HoraColombia.InicioDelDiaUtc(new DateOnly(2026, 10, 25));

            Assert.AreEqual(new DateTime(2026, 10, 25, 5, 0, 0, DateTimeKind.Utc), inicio);
            Assert.AreEqual(DateTimeKind.Utc, inicio.Kind);
        }

        [TestMethod]
        public void PeriodoUtc_UnSoloDia_CubreVeinticuatroHoras()
        {
            var dia = new DateOnly(2026, 10, 25);

            (DateTime desde, DateTime hasta) = HoraColombia.PeriodoUtc(dia, dia);

            Assert.AreEqual(new DateTime(2026, 10, 25, 5, 0, 0, DateTimeKind.Utc), desde);
            Assert.AreEqual(new DateTime(2026, 10, 26, 5, 0, 0, DateTimeKind.Utc), hasta);
        }

        [TestMethod]
        public void PeriodoUtc_FinDeMes_IncluyeElUltimoDiaCompleto()
        {
            (DateTime desde, DateTime hasta) = HoraColombia.PeriodoUtc(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

            Assert.AreEqual(new DateTime(2026, 10, 1, 5, 0, 0, DateTimeKind.Utc), desde);
            Assert.AreEqual(new DateTime(2026, 11, 1, 5, 0, 0, DateTimeKind.Utc), hasta);
        }

        [TestMethod]
        public void PeriodoUtc_UltimoDiaAnterior_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                HoraColombia.PeriodoUtc(new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 1)));
        }

        [TestMethod]
        public void DesdeUtc_MadrugadaUtc_EsElDiaAnteriorEnColombia()
        {
            DateTime local = HoraColombia.DesdeUtc(new DateTime(2026, 10, 26, 4, 30, 0, DateTimeKind.Utc));

            Assert.AreEqual(new DateTime(2026, 10, 25, 23, 30, 0), local);
            Assert.AreEqual(DateTimeKind.Unspecified, local.Kind);
        }

        [TestMethod]
        public void DesdeUtc_FechaNoUtc_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() => HoraColombia.DesdeUtc(new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Local)));
        }

        [TestMethod]
        public void DesdeUtc_BordeDeLasCincoUtc_CambiaDeDiaEnColombia()
        {
            DateTime antes = HoraColombia.DesdeUtc(new DateTime(2026, 10, 26, 4, 59, 59, DateTimeKind.Utc));
            DateTime justo = HoraColombia.DesdeUtc(new DateTime(2026, 10, 26, 5, 0, 0, DateTimeKind.Utc));

            Assert.AreEqual(new DateOnly(2026, 10, 25), DateOnly.FromDateTime(antes));
            Assert.AreEqual(new DateOnly(2026, 10, 26), DateOnly.FromDateTime(justo));
            Assert.AreEqual(TimeOnly.MinValue, TimeOnly.FromDateTime(justo));
        }
    }
}
