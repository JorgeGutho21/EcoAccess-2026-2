using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioConversacionTelegramTests
    {
        private ServicioConversacionTelegram SinPagina(Uri? pagina = null) => new(
            _conversaciones, _contenedores, _materiales,
            new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
            _servicioInteracciones, _limitador, pagina, _reloj);

        [TestMethod]
        public async Task SinHttpsConsultaPublicaFuncionaPeroVincularNoGeneraToken()
        {
            var servicio = SinPagina();
            foreach (string texto in new[] { "/start", "/ayuda", "PET" })
            {
                var r = await servicio.ProcesarAsync(new(++_update, Invitado, Invitado, true, texto, null));
                Assert.HasCount(1, r); Assert.AreNotEqual(MensajesTelegram.VinculacionNoDisponible, r[0].Texto);
            }
            Assert.AreEqual(MensajesTelegram.VinculacionNoDisponible,
                Unica(await servicio.ProcesarAsync(new(++_update, Invitado, Invitado, true, null, "menu:vincular"))).Texto);
            Assert.IsEmpty(_vinculos.Tokens);
            Assert.AreEqual(MensajesTelegram.YaVinculada,
                Unica(await servicio.ProcesarAsync(new(++_update, Vinculado, Vinculado, true, "/vincular", null))).Texto);
            Assert.IsEmpty(_vinculos.Tokens);
        }

        [TestMethod]
        [DataRow("http://ejemplo.invalid/registro/")]
        [DataRow("https://usuario@ejemplo.invalid/registro/")]
        [DataRow("https://ejemplo.invalid/registro/?secreto=x")]
        public void PaginaInseguraNoPuedeHabilitarVinculacion(string url) =>
            Assert.ThrowsExactly<ArgumentException>(() => SinPagina(new(url)));
    }
}
