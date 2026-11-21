using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Tests.Integracion;

namespace EcoAccess.P3.Datos.Tests.Unitarias
{
    /// <summary>
    /// La integración solo corre contra un esquema de pruebas. Estas cadenas son ficticias (sin contraseña) y no se conectan
    /// a nada: comprueban la regla que impide repetir la corrida del 20/11/2026 contra la base de la aplicación.
    /// </summary>
    [TestClass]
    public sealed class DestinoIntegracionTests
    {
        [TestMethod]
        [DataRow("Data Source=localhost:1521/freepdb1;User Id=ECOACCESS_P3_PRUEBAS")]
        [DataRow("Data Source=localhost:1522/PRUEBAP3;User Id=ECOACCESS_P3")]
        [DataRow("Data Source=localhost:1522/pruebap3/;User Id=otro")]
        [DataRow("Data Source=x;User Id=ecoaccess_p3_pruebas")]
        public void AceptaElEsquemaAisladoOElPdbDePruebas(string cadena) =>
            Assert.IsTrue(BaseIntegracionOracle.EsDestinoDePruebas(cadena), cadena);

        [TestMethod]
        [DataRow("Data Source=ecoaccess_tp;User Id=ECOACCESS_P3_APP")]
        [DataRow("Data Source=localhost:1521/FREEPDB1;User Id=ECOACCESS_P3")]
        [DataRow("Data Source=ecoaccess_tp;User Id=ECOACCESS_P3_PRUEBAS_NO")]
        public void RechazaLaBaseDeLaAplicacion(string cadena) =>
            Assert.IsFalse(BaseIntegracionOracle.EsDestinoDePruebas(cadena), cadena);
    }
}
