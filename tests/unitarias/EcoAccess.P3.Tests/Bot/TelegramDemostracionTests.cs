using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioConversacionTelegramTests
    {
        [TestMethod]
        [DataRow(EscenarioCaneca.Duplicados, 1)]
        [DataRow(EscenarioCaneca.AperturaSinDeteccion, 0)]
        public async Task TelegramDemostracion_QrManualConfirmacionYCaneca_NoConfundeAperturaConDeposito(
            EscenarioCaneca escenario, int depositos)
        {
            var caneca = new CanecaSimulada(_caneca.Id, _servicioInteracciones.RegistrarEventoAsync, _reloj) { Escenario = escenario };
            var servicio = ConCanal(caneca);
            await EnviarConAsync(servicio, "/start " + _caneca.IdentificadorQr);
            var foto = Unica(await EnviarFotoAsync(servicio));
            StringAssert.Contains(foto.Texto, "elegir el material manualmente");
            Assert.IsFalse(foto.Botones.Any(b => b.Dato == ServicioConversacionTelegram.ConfirmarSi));
            var manual = Unica(await EnviarConAsync(servicio, "/manual"));
            StringAssert.Contains(manual.Texto, "no es reconocimiento visual");
            Assert.IsTrue(manual.Botones.Any(b => b.Dato == MaterialPet));
            await EnviarConAsync(servicio, boton: MaterialPet);
            await Task.WhenAll(
                EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi),
                EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi));
            Assert.HasCount(1, _interacciones.Ordenes);
            Assert.HasCount(1, caneca.ComandosRecibidos);
            Assert.IsEmpty(_interacciones.Depositos, "El despacho no acredita ingreso físico.");
            int id = _interacciones.Ordenes.Single().Id;
            await caneca.EjecutarAsync(id);
            await caneca.EjecutarAsync(id);
            Assert.HasCount(depositos, _interacciones.Depositos);
            foreach (var deposito in _interacciones.Depositos)
            {
                Assert.IsFalse(deposito.EstaVerificado);
                Assert.IsNull(deposito.PesoMedidoKg);
            }
            var estado = Unica(await EnviarConAsync(servicio, "/estado"));
            StringAssert.Contains(estado.Texto, depositos == 0 ? "Sin depósito registrado" : "material pendiente de verificación");
        }
    }
}
