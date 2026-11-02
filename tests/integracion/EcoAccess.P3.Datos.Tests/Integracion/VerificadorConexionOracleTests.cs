using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Oracle;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    /// <summary>
    /// La configuración inicial confía en este verificador: una conexión real y completa debe darse por correcta, y una
    /// credencial rechazada no debe confundirse con Oracle caído. La credencial incorrecta usa un usuario inexistente,
    /// para no sumar intentos fallidos a la cuenta real del esquema.
    /// </summary>
    [TestClass]
    public sealed class VerificadorConexionOracleTests : BaseIntegracionOracle
    {
        [TestMethod]
        public async Task ConexionConfigurada_EsCorrectaYTieneLasTablasDeEcoAccess()
        {
            Assert.AreEqual(EstadoConexionOracle.Correcta, await VerificadorConexionOracle.ComprobarAsync(LeerCadena()));
        }

        [TestMethod]
        public async Task UsuarioInexistente_SeInformaComoCredencialRechazada()
        {
            var partes = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = LeerCadena()! };
            partes["User Id"] = "ECOACCESS_PRUEBA_UI_NO_EXISTE";
            partes["Password"] = "contrasena-de-prueba-incorrecta";

            Assert.AreEqual(EstadoConexionOracle.CredencialesInvalidas, await VerificadorConexionOracle.ComprobarAsync(partes.ConnectionString));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("esto no es una cadena")]
        public async Task CadenaAusenteOMalFormada_NoAbreConexion(string? cadena)
        {
            Assert.AreEqual(EstadoConexionOracle.CadenaInvalida, await VerificadorConexionOracle.ComprobarAsync(cadena));
        }

        [TestMethod]
        public async Task PuertoSinOracle_SeInformaComoServicioNoDisponible()
        {
            var partes = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = LeerCadena()! };
            partes["Data Source"] = "localhost:1599/PRUEBAP3";
            partes["Connection Timeout"] = "5";

            Assert.AreEqual(EstadoConexionOracle.ServicioNoDisponible, await VerificadorConexionOracle.ComprobarAsync(partes.ConnectionString));
        }
    }
}
