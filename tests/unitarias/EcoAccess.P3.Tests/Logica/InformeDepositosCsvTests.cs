using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{

    [TestClass]
    public sealed class InformeDepositosCsvTests
    {
        private static readonly DateTime Fecha = new(2026, 10, 26, 15, 0, 0, DateTimeKind.Utc);
        private static string Informe(string caneca, decimal? medido = null, decimal? estimado = null) =>
            InformeDepositosCsv.Generar(new FiltroEstadisticas(Fecha, Fecha.AddDays(1)),
                new PaginaDepositos(new FilaDeposito[] { new FilaDeposito(7, Fecha, 2, caneca, null, "PET", null, medido, estimado) }, 123, 2, 50));

        [TestMethod]
        public void EscapaSeparadorComillasYSaltoDeLineaSinAgregarColumnasFicticias()
        {
            StringAssert.Contains(Informe("Caneca; \"norte\"\r\nsegundo piso"), "\"Caneca; \"\"norte\"\"\r\nsegundo piso\"");
        }

        [TestMethod]
        [DataRow("=HYPERLINK(1)")]
        [DataRow("  +SUM(1)")]
        [DataRow("\t-1+2")]
        [DataRow("\uFEFF@SUM(1)")]
        public void TextoQuePuedeInterpretarseComoFormulaSeNeutraliza(string texto)
        {
            StringAssert.Contains(Informe(texto), "\"'" + texto + "\"");
        }

        [TestMethod]
        public void FechasUtcPesosSeparadosYNulosSonIndependientesDeLaCulturaLocal()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-CO");
                string csv = Informe("Norte", medido: 0.125m);
                StringAssert.Contains(csv, "2026-10-26T15:00:00.0000000Z");
                StringAssert.Contains(csv, "\"0.125\";\"\"\r\n");
                StringAssert.Contains(csv, "\"2\";\"50\";\"123\";\"7\"");
                Assert.DoesNotContain("0,125", csv);
            }
            finally { CultureInfo.CurrentCulture = original; }
        }

        [TestMethod]
        public void PaginaSinResultadosConservaEncabezadoYSinFilasInventadas()
        {
            string csv = InformeDepositosCsv.Generar(new FiltroEstadisticas(Fecha, Fecha.AddDays(1)), new PaginaDepositos(Array.Empty<FilaDeposito>(), 0, 1, 50));
            Assert.HasCount(2, csv.Split("\r\n"));
            StringAssert.Contains(csv, "peso_medido_kg;peso_estimado_kg");
        }
    }
}
