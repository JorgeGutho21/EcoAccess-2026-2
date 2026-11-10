using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{
    /// <summary>
    /// Caso real del 10/11/2026: contra un Oracle local rápido, una solicitud recién creada se declaraba vencida.
    /// Oracle redondeaba la décima de microsegundo hacia arriba y el reloj de .NET Framework (saltos de hasta 15,6 ms)
    /// devolvía el mismo «ahora»: la hora guardada quedaba 100 ns en el futuro. Las fechas se truncan al guardar.
    /// </summary>
    [TestClass]
    public sealed class FechasSinOracleTests
    {
        [TestMethod]
        public void AlGuardarSeTruncaAMicrosegundosYNuncaQuedaDespuesDeLaHoraReal()
        {
            var real = new DateTime(2026, 11, 10, 19, 9, 27, DateTimeKind.Utc).AddTicks(2265209);
            DateTime guardada = ConversionOracle.ParaColumnaUtc(real);
            Assert.AreEqual(0, guardada.Ticks % 10, "Debe caber exacta en TIMESTAMP(6).");
            Assert.IsTrue(guardada <= real);
            Assert.AreEqual(9, (real - guardada).Ticks);
            Assert.AreEqual(DateTimeKind.Utc, guardada.Kind);
        }

        [TestMethod]
        public void UnaFechaQueNoEsUtcSigueSiendoUnErrorDeProgramacion() =>
            Assert.ThrowsExactly<ArgumentException>(() => ConversionOracle.ParaColumnaUtc(new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Local)));
    }

    [TestClass]
    [TestCategory(Categoria)]
    public sealed class FechasOracleTests : BaseIntegracionOracle
    {
        [TestMethod]
        public async Task LaHoraGuardadaYLeidaDeOracleNuncaQuedaDespuesDeLaHoraDeLaAplicacion()
        {
            using OracleConnection conexion = await Fabrica.AbrirAsync(CancellationToken.None);
            for (int i = 0; i < 500; i++)
            {
                DateTime ahora = DateTime.UtcNow;
                using OracleCommand comando = ComandosOracle.Crear(conexion, "SELECT CAST(:fecha AS TIMESTAMP) FROM dual");
                comando.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(ahora));
                DateTime leida = ConversionOracle.FechaUtc(await comando.ExecuteScalarAsync() ?? throw new InvalidOperationException());
                Assert.IsTrue(leida <= ahora, $"Oracle devolvió {leida:O}, posterior a {ahora:O}.");
            }
        }
    }
}
