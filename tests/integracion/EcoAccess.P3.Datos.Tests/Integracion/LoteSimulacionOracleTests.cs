using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Datos.Simulacion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{
    /// <summary>
    /// Fase 8 contra Oracle real, SOLO en el esquema aislado ECOACCESS_P3_PRUEBAS*: el generador crea 50 ciudadanos y
    /// 100 depósitos sintéticos, repetirlo no duplica nada y las vistas de informes los excluyen por defecto. En otra base
    /// la prueba queda inconclusa: nunca genera datos en la nube por sí sola.
    /// </summary>
    [TestClass]
    [TestCategory(Categoria)]
    public sealed class LoteSimulacionOracleTests : BaseIntegracionOracle
    {
        private const string Lote = "SIM-PRUEBA-INTEGRACION";

        private async Task<string> EsquemaAsync()
        {
            using OracleConnection c = await Fabrica.AbrirAsync(CancellationToken.None);
            using OracleCommand cmd = ComandosOracle.Crear(c, "SELECT USER FROM dual");
            return (string)(await cmd.ExecuteScalarAsync())!;
        }

        private async Task<int> ContarLoteAsync(string sql)
        {
            using OracleConnection c = await Fabrica.AbrirAsync(CancellationToken.None);
            using OracleCommand cmd = ComandosOracle.Crear(c, sql);
            cmd.Agregar("lote", OracleDbType.Varchar2, Lote);
            return ConversionOracle.Entero(await cmd.ExecuteScalarAsync() ?? 0);
        }

        [TestMethod]
        [Timeout(600_000, CooperativeCancellation = true)]
        public async Task GeneraCincuentaCiudadanosYCienDepositos_EsIdempotente_YQuedaFueraDeLosInformes()
        {
            if (!(await EsquemaAsync()).StartsWith("ECOACCESS_P3_PRUEBAS", StringComparison.Ordinal))
                Assert.Inconclusive("El generador solo se prueba en el esquema aislado ECOACCESS_P3_PRUEBAS*.");
            if (!await new RepositorioLoteSimulacionOracle(Fabrica).TablaDisponibleAsync())
                Assert.Inconclusive("Esta base no tiene V025: prueba no ejecutada.");

            var generador = new GeneradorLoteSimulacion(Fabrica);
            DateTime fin = new(2026, 11, 10, 17, 0, 0, DateTimeKind.Utc);
            ResultadoLoteSimulacion primero = await generador.GenerarAsync(Lote, fin, 20261110);
            ResultadoLoteSimulacion segundo = await generador.GenerarAsync(Lote, fin, 20261110);

            Assert.IsEmpty(primero.Rechazos, string.Join("; ", primero.Rechazos));
            Assert.AreEqual(0, segundo.CiudadanosNuevos, "La segunda ejecución no crea ciudadanos.");
            Assert.AreEqual(0, segundo.RegistrosNuevos, "La segunda ejecución no crea depósitos.");
            Assert.AreEqual(GeneradorLoteSimulacion.Registros, segundo.Existentes);

            Assert.AreEqual(50, await ContarLoteAsync("SELECT COUNT(*) FROM registro_simulacion WHERE id_lote=:lote AND tabla='USUARIO' AND clave LIKE 'ciudadano-%'"));
            Assert.AreEqual(100, await ContarLoteAsync("SELECT COUNT(*) FROM vista_depositos_estado WHERE id_lote=:lote AND es_simulacion=1 AND es_ensayo=1"));
            Assert.AreEqual(0, await ContarLoteAsync("SELECT COUNT(*) FROM vista_depositos_estado WHERE id_lote=:lote AND peso_medido_kg IS NOT NULL"),
                "Un depósito sintético nunca tiene peso medido.");
            Assert.AreEqual(100, await ContarLoteAsync("SELECT COUNT(*) FROM vista_depositos_estado WHERE id_lote=:lote AND estado_registro IN ('DECLARADA_SIN_SENSOR','DETECTADA_POR_SENSOR','VERIFICADA')"));
            Assert.AreEqual(1, await ContarLoteAsync("SELECT COUNT(*) FROM vista_lotes_simulacion WHERE id_lote=:lote AND estado='COMPLETO' AND ciudadanos=50 AND registros=100"));
            // Las vistas de informes reales (V015, es_ensayo=0) no cuentan nada de la estación de simulación.
            Assert.AreEqual(0, await ContarLoteAsync(
                "SELECT COUNT(*) FROM ea_r_base_depositos b WHERE b.es_ensayo=0 AND b.id_contenedor IN " +
                "(SELECT id_registro FROM registro_simulacion WHERE id_lote=:lote AND tabla='CONTENEDOR')"));
        }
    }
}
