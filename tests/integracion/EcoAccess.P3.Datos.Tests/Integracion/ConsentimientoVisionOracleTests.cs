using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Repositorios;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    // Autorización de fotos externas contra Oracle real (V018).
    [TestClass]
    [TestCategory(Categoria)]
    [DoNotParallelize]
    public sealed class ConsentimientoVisionOracleTests : BaseIntegracionOracle
    {
        private static readonly DateTime Fecha = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        private readonly long _telegram = 8_000_000_000L + new Random().Next(1, 900_000_000);

        [TestCleanup]
        public async Task LimpiarConsentimientoAsync()
        {
            if (Fabrica is null) return;
            using OracleConnection conexion = await Fabrica.AbrirAsync(CancellationToken.None);
            using var comando = conexion.CreateCommand();
            comando.BindByName = true;
            comando.CommandText = "DELETE FROM consentimiento_vision_externa WHERE telegram_user_id = :id";
            comando.Parameters.Add("id", OracleDbType.Int64).Value = _telegram;
            await comando.ExecuteNonQueryAsync();
        }

        [TestMethod]
        public async Task SinRegistro_NoEstaVigente()
        {
            var repositorio = new RepositorioConsentimientoVisionOracle(Fabrica);
            Assert.IsFalse(await repositorio.EstaVigenteAsync(_telegram, "v1"));
            Assert.IsFalse(await repositorio.RevocarAsync(_telegram, Fecha), "Revocar sin autorización no cambia nada.");
        }

        [TestMethod]
        public async Task Registrar_QuedaVigenteSoloParaEsaVersion()
        {
            var repositorio = new RepositorioConsentimientoVisionOracle(Fabrica);
            await repositorio.RegistrarAsync(_telegram, "v1", Fecha);
            Assert.IsTrue(await repositorio.EstaVigenteAsync(_telegram, "v1"));
            Assert.IsFalse(await repositorio.EstaVigenteAsync(_telegram, "v2"), "Un aviso nuevo exige autorizar otra vez.");
        }

        [TestMethod]
        public async Task Revocar_LaDejaSinEfectoYSePuedeVolverAOtorgar()
        {
            var repositorio = new RepositorioConsentimientoVisionOracle(Fabrica);
            await repositorio.RegistrarAsync(_telegram, "v1", Fecha);

            Assert.IsTrue(await repositorio.RevocarAsync(_telegram, Fecha.AddMinutes(1)));
            Assert.IsFalse(await repositorio.EstaVigenteAsync(_telegram, "v1"));
            Assert.IsFalse(await repositorio.RevocarAsync(_telegram, Fecha.AddMinutes(2)), "No hay nada más que revocar.");

            await repositorio.RegistrarAsync(_telegram, "v1", Fecha.AddMinutes(3));
            Assert.IsTrue(await repositorio.EstaVigenteAsync(_telegram, "v1"));
        }

        [TestMethod]
        public async Task SobreviveAUnaConexionNueva_ComoTrasReiniciarElBot()
        {
            await new RepositorioConsentimientoVisionOracle(Fabrica).RegistrarAsync(_telegram, "v1", Fecha);
            Assert.IsTrue(await new RepositorioConsentimientoVisionOracle(Fabrica).EstaVigenteAsync(_telegram, "v1"));
        }
    }
}
