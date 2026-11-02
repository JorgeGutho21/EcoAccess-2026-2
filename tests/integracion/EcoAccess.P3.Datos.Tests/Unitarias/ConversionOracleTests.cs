using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Oracle;

namespace EcoAccess.P3.Datos.Tests.Unitarias
{

    [TestClass]
    public sealed class ConversionOracleTests
    {
        [TestMethod]
        public void Entero_DecimalYLong_DevuelveEntero()
        {
            Assert.AreEqual(42, ConversionOracle.Entero(42m));
            Assert.AreEqual(7, ConversionOracle.Entero(7L));
        }

        [TestMethod]
        public void Logico_CeroYUno_DevuelveFalsoYVerdadero()
        {
            Assert.IsFalse(ConversionOracle.Logico(0m));
            Assert.IsTrue(ConversionOracle.Logico(1m));
        }

        [TestMethod]
        public void Logico_OtroValor_LanzaPorDatosCorruptos()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => ConversionOracle.Logico(2m));
        }

        [TestMethod]
        public void DesdeLogico_DevuelveUnoOCero()
        {
            Assert.AreEqual(1, ConversionOracle.DesdeLogico(true));
            Assert.AreEqual(0, ConversionOracle.DesdeLogico(false));
        }

        [TestMethod]
        public void TextoOpcional_DbNull_DevuelveNull()
        {
            Assert.IsNull(ConversionOracle.TextoOpcional(DBNull.Value));
            Assert.AreEqual("texto", ConversionOracle.TextoOpcional("texto"));
        }

        [TestMethod]
        public void FechaUtc_FechaSinZona_LaMarcaUtcSinMoverLaHora()
        {
            var leida = new DateTime(2026, 3, 1, 8, 30, 0, DateTimeKind.Unspecified);

            DateTime resultado = ConversionOracle.FechaUtc(leida);

            Assert.AreEqual(DateTimeKind.Utc, resultado.Kind);
            Assert.AreEqual(leida.Ticks, resultado.Ticks);
        }

        [TestMethod]
        public void FechaUtcOpcional_DbNull_DevuelveNull()
        {
            Assert.IsNull(ConversionOracle.FechaUtcOpcional(DBNull.Value));
        }

        [TestMethod]
        public void ParaColumnaUtc_FechaUtc_LaDevuelveIgual()
        {
            var fecha = new DateTime(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc);

            Assert.AreEqual(fecha, ConversionOracle.ParaColumnaUtc(fecha));
        }

        [TestMethod]
        [DataRow(DateTimeKind.Local)]
        [DataRow(DateTimeKind.Unspecified)]
        public void ParaColumnaUtc_FechaNoUtc_Lanza(DateTimeKind tipo)
        {
            var fecha = new DateTime(2026, 3, 1, 8, 30, 0, tipo);

            Assert.ThrowsExactly<ArgumentException>(() => ConversionOracle.ParaColumnaUtc(fecha));
        }

        [TestMethod]
        public void Opcionales_Null_DevuelvenDbNull()
        {
            Assert.AreSame(DBNull.Value, ConversionOracle.ParaColumnaUtcOpcional(null));
            Assert.AreSame(DBNull.Value, ConversionOracle.ParaColumnaOpcional(null));
            Assert.AreEqual("v1", ConversionOracle.ParaColumnaOpcional("v1"));
        }
    }
}
