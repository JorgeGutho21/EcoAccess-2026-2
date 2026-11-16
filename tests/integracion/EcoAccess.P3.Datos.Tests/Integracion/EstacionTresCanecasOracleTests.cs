using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{
    /// <summary>
    /// V024 contra Oracle real, solo lectura. El QR oficial lleva a una estación con una caneca de cada color y la
    /// caneca piloto es la blanca. En una base sin V024 la prueba queda inconclusa: no se declara aprobada.
    /// </summary>
    [TestClass]
    [TestCategory(Categoria)]
    public sealed class EstacionTresCanecasOracleTests : BaseIntegracionOracle
    {
        private const string QrOficial = "EcoAccessCanecaPiloto1";

        [TestMethod]
        public async Task ElQrOficialLlevaAUnaEstacionConBlancaVerdeYNegra()
        {
            int? piloto;
            using (OracleConnection conexion = await Fabrica.AbrirAsync(CancellationToken.None))
            using (OracleCommand comando = ComandosOracle.Crear(conexion, "SELECT MIN(id_contenedor) FROM contenedor WHERE identificador_qr=:qr"))
            {
                comando.Agregar("qr", OracleDbType.Varchar2, QrOficial);
                object? valor = await comando.ExecuteScalarAsync();
                piloto = valor is null or System.DBNull ? null : ConversionOracle.Entero(valor);
            }
            if (piloto is null) Assert.Inconclusive("Esta base no tiene la caneca del QR oficial.");

            var estacion = await new RepositorioEstacionesOracle(Fabrica).ListarEstacionDeAsync(piloto.Value);
            if (estacion.Count == 0) Assert.Inconclusive("Esta base no tiene V024 (estación piloto sin configurar): prueba no ejecutada.");

            Assert.AreEqual(CategoriaMaterial.Aprovechable, estacion.Single(c => c.IdContenedor == piloto).Categoria, "La caneca del QR oficial es la blanca.");
            CollectionAssert.AreEquivalent(new[] { CategoriaMaterial.Aprovechable, CategoriaMaterial.OrganicoAprovechable, CategoriaMaterial.NoAprovechable },
                estacion.Select(c => c.Categoria).ToArray());
            Assert.AreEqual(3, estacion.Select(c => c.IdentificadorDispositivo).Distinct().Count(), "Cada tapa tiene su propio ESP32.");
            foreach (DestinoResiduo d in new[] { DestinoResiduo.Blanca, DestinoResiduo.Verde, DestinoResiduo.Negra })
                Assert.IsNotNull(ResolutorDestino.Resolver(d, estacion).Caneca, d.ToString());
            Assert.IsNull(ResolutorDestino.Resolver(DestinoResiduo.Posconsumo, estacion).Caneca);
        }

        [TestMethod]
        public async Task UnaCanecaInexistenteNoTieneEstacion() =>
            Assert.IsEmpty(await new RepositorioEstacionesOracle(Fabrica).ListarEstacionDeAsync(int.MaxValue));
    }
}
