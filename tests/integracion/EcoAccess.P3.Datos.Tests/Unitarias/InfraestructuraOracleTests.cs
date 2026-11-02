using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Oracle;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace EcoAccess.P3.Datos.Tests.Unitarias
{

    [TestClass]
    public sealed class InfraestructuraOracleTests
    {
        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        public void FabricaConexiones_CadenaVacia_Lanza(string cadena)
        {
            Assert.ThrowsExactly<ArgumentException>(() => new FabricaConexionesOracle(cadena));
        }

        [TestMethod]
        public void ExigirUnaFila_CeroFilas_LanzaConEntidadEId()
        {
            var error = Assert.ThrowsExactly<InvalidOperationException>(() => ComandosOracle.ExigirUnaFila(0, "material", 9));

            StringAssert.Contains(error.Message, "material");
            StringAssert.Contains(error.Message, "9");
        }

        [TestMethod]
        public void ExigirUnaFila_UnaFila_NoLanza()
        {
            ComandosOracle.ExigirUnaFila(1, "material", 9);
        }

        // LeerId se prueba sin conexión asignando Value a mano; que ODP.NET entregue OracleDecimal en un
        // RETURNING real solo lo confirman las pruebas de integración.
        [TestMethod]
        public void LeerId_OracleDecimal_DevuelveEntero()
        {
            var parametro = new OracleParameter("id", OracleDbType.Decimal) { Value = new OracleDecimal(42) };

            Assert.AreEqual(42, ComandosOracle.LeerId(parametro));
        }

        [TestMethod]
        public void LeerId_TipoNet_DevuelveEntero()
        {
            var parametro = new OracleParameter("id", OracleDbType.Decimal) { Value = 42m };

            Assert.AreEqual(42, ComandosOracle.LeerId(parametro));
        }

        [TestMethod]
        public void LeerId_SinValor_Lanza()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                ComandosOracle.LeerId(new OracleParameter("id", OracleDbType.Decimal) { Value = DBNull.Value }));
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                ComandosOracle.LeerId(new OracleParameter("id", OracleDbType.Decimal) { Value = OracleDecimal.Null }));
        }
    }
}
