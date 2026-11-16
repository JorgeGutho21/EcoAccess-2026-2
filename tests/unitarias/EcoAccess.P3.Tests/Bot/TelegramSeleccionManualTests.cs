using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioConversacionTelegramTests
    {
        [TestMethod]
        public async Task TelegramManual_SinProveedorRecuperaCatalogoYExigeSeleccionYConfirmacion()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);
            await _servicio.ProcesarAsync(new MensajeEntrante(++_update, Vinculado, Vinculado, true, null, null, "foto"));
            var manual = Unica(await TextoAsync("/manual"));
            StringAssert.Contains(manual.Texto, "no es reconocimiento visual");
            Assert.IsTrue(manual.Botones.Any(b => b.Dato == MaterialPet));
            Assert.IsFalse(manual.Botones.Any(b => b.Dato == ServicioConversacionTelegram.ConfirmarSi));
            await BotonAsync(ServicioConversacionTelegram.ConfirmarSi);
            Assert.IsEmpty(_interacciones.Ordenes);
            await BotonAsync(MaterialPet);
            await BotonAsync(ServicioConversacionTelegram.ConfirmarSi);
            Assert.HasCount(1, _interacciones.Ordenes);
            Assert.IsEmpty(_interacciones.Depositos);
        }

        [TestMethod]
        public async Task TelegramManual_DescartaSeleccionAnteriorYNoDuplicaOrdenEmitida()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);
            await BotonAsync(MaterialPet);
            await TextoAsync("/manual");
            await BotonAsync(ServicioConversacionTelegram.ConfirmarSi);
            Assert.IsEmpty(_interacciones.Ordenes);
            await BotonAsync(MaterialPet);
            await BotonAsync(ServicioConversacionTelegram.ConfirmarSi);
            Assert.AreEqual(MensajesTelegram.CorreccionTrasOrden, Unica(await TextoAsync("/manual")).Texto);
            Assert.HasCount(1, _interacciones.Ordenes);
        }

        [TestMethod]
        public async Task TelegramManual_InvitadoSinQrOVencidoNoObtieneBotonesOperativos()
        {
            Assert.IsEmpty(Unica(await TextoAsync("/manual", Invitado)).Botones);
            Assert.AreEqual(MensajesTelegram.EscaneaPrimero, Unica(await TextoAsync("/manual")).Texto);
            await TextoAsync("/start " + _caneca.IdentificadorQr);
            _reloj.Avanzar(ServicioConversacionTelegram.VigenciaConversacion + TimeSpan.FromSeconds(1));
            Assert.IsEmpty(Unica(await TextoAsync("/manual")).Botones);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        [DataRow(false, true)]
        [DataRow(true, false)]
        public async Task TelegramManual_RevalidaActividadYConsentimiento(bool activa, bool consentimiento)
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);
            _usuarios.AgregarCiudadano(Ciudadano, consentimiento: consentimiento, activo: activa);
            Assert.IsEmpty(Unica(await TextoAsync("/manual")).Botones);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task TelegramFoto_GuiaPublicaNoDescargaNiEmiteConsentimiento()
        {
            int descargas = 0;
            var servicio = ConVision(TipoPropuestaVision.MaterialIdentificado, (_, _) =>
            {
                descargas++;
                return Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png"));
            }, consentimiento: true);
            var respuesta = Unica(await EnviarConAsync(servicio, "/foto", cuenta: Invitado));
            StringAssert.Contains(respuesta.Texto, "/manual");
            Assert.IsFalse(respuesta.Botones.Any(b => b.Dato.StartsWith(ServicioConversacionTelegram.PrefijoConsentirFoto, StringComparison.Ordinal)));
            Assert.AreEqual(0, descargas);
            Assert.IsEmpty(_interacciones.Ordenes);
        }
    }
}
