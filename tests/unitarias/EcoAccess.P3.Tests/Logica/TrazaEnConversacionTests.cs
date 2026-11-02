using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioConversacionTelegramTests
    {
        private sealed class TrazaQueFalla : IRepositorioAnalisisImagen
        {
            public bool Fallar = true;
            public Task GuardarAsync(AnalisisImagen analisis, CancellationToken ct = default) => Task.CompletedTask;
            public Task<AnalisisImagen?> ObtenerUltimoAsync(int interaccion, CancellationToken ct = default) => Task.FromResult<AnalisisImagen?>(null);
            public Task ConfirmarAsync(int interaccion, int material, DateTime fechaUtc, CancellationToken ct = default) =>
                Fallar ? Task.FromException(new IOException("Fallo de traza sintético")) : Task.CompletedTask;
        }

        [TestMethod]
        public async Task TrazaFallaAntesDeEmitir_NoDejaOrdenOcultaYPermiteReintento()
        {
            var traza = new TrazaQueFalla();
            var vinculacion = new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj);
            var bot = new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales, vinculacion,
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj, analisis: traza);
            await EnviarConAsync(bot, "/start " + _caneca.IdentificadorQr);
            await EnviarConAsync(bot, boton: MaterialPet);
            await Assert.ThrowsExactlyAsync<IOException>(() => EnviarConAsync(bot, boton: ServicioConversacionTelegram.ConfirmarSi));
            Assert.IsEmpty(_interacciones.Ordenes);
            Assert.IsEmpty(_interacciones.Depositos);
            traza.Fallar = false;
            await EnviarConAsync(bot, boton: ServicioConversacionTelegram.ConfirmarSi);
            Assert.HasCount(1, _interacciones.Ordenes);
            Assert.IsEmpty(_interacciones.Depositos);
        }
    }
}
