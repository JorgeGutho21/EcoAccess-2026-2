using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    [TestClass, TestCategory(Categoria), DoNotParallelize]
    public sealed class InformeInstitucionalOracleTests : BaseIntegracionOracle
    {
        [TestMethod]
        public async Task VistasSoloLecturaEIndicadoresAgregadosSeConsultanSinDatosPersonales()
        {
            using var c = await Fabrica.AbrirAsync(CancellationToken.None);
            using var cmd = ComandosOracle.Crear(c, "SELECT COUNT(*) FROM user_views WHERE view_name IN ('EA_R_DEPOSITOS','EA_R_PUNTOS','EA_R_OPINIONES','EA_R_BASE_DEPOSITOS','EA_R_BASE_PUNTOS','EA_R_SOLICITUDES') AND read_only='Y'");
            Assert.AreEqual(6, ConversionOracle.Entero((await cmd.ExecuteScalarAsync())!));
            var repo = new RepositorioInformeInstitucionalOracle(Fabrica);
            var f = new FiltroInforme(new(2026, 10, 1, 5, 0, 0, DateTimeKind.Utc), new(2026, 11, 2, 5, 0, 0, DateTimeKind.Utc));
            var datos = await repo.ConsultarAsync(f);
            Assert.IsTrue(datos.Depositos >= datos.Verificados && datos.Ciudadanos <= datos.Depositos);
            foreach (var agrupacion in new[] { AgrupacionDepositos.Dia, AgrupacionDepositos.Material, AgrupacionDepositos.Contenedor, AgrupacionDepositos.Punto })
                Assert.AreEqual(datos.Depositos, (await repo.AgruparAsync(f, agrupacion)).Sum(x => (long)x.Depositos));
            var vacio = await repo.ConsultarAsync(f with { IdContenedor = int.MaxValue });
            Assert.AreEqual(0, vacio.Depositos); Assert.AreEqual(0, vacio.Ciudadanos);
        }

        /// <summary>
        /// Mide las seis consultas del tablero institucional (período actual, período anterior y cuatro agrupaciones)
        /// en modo operativo y experimental. Solo lectura. Ubica la demora de 16,7 s medida en la ventana: si estas
        /// consultas son rápidas, la demora está fuera del repositorio.
        /// </summary>
        [TestMethod]
        public async Task ConsultasDelTableroInstitucional_RespondenEnMenosDeCincoSegundos()
        {
            var repo = new RepositorioInformeInstitucionalOracle(Fabrica);
            var desde = new DateTime(2026, 10, 12, 5, 0, 0, DateTimeKind.Utc);
            var hasta = new DateTime(2026, 11, 12, 5, 0, 0, DateTimeKind.Utc);
            foreach (bool ensayo in new[] { false, true })
            {
                var f = new FiltroInforme(desde, hasta, SoloDemostracion: ensayo);
                var reloj = System.Diagnostics.Stopwatch.StartNew();
                var actual = await repo.ConsultarAsync(f);
                long indicadores = reloj.ElapsedMilliseconds;
                await repo.ConsultarAsync(f with { DesdeUtc = desde - (hasta - desde), HastaUtc = desde });
                foreach (var agrupacion in new[] { AgrupacionDepositos.Dia, AgrupacionDepositos.Material, AgrupacionDepositos.Contenedor, AgrupacionDepositos.Punto })
                    await repo.AgruparAsync(f, agrupacion);
                reloj.Stop();
                Console.WriteLine($"MEDICION ensayo={ensayo}: depositos={actual.Depositos}, indicadores {indicadores} ms, tablero completo {reloj.ElapsedMilliseconds} ms");
                Assert.IsLessThan(5000L, reloj.ElapsedMilliseconds, $"El tablero (ensayo={ensayo}) tardó {reloj.ElapsedMilliseconds} ms.");
            }
        }
    }
}
