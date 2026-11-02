using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioConversacionTelegramTests
    {
        [TestMethod]
        public async Task EcoFaqPasaPorDeduplicacionRealDeLaBll()
        {
            var eco = new AsistenteEco(_servicio.ProcesarAsync);
            var m = new MensajeEntrante(++_update, Invitado, Invitado, true, "¿Qué es EcoAccess?", null);
            StringAssert.Contains((await eco.ProcesarAsync(m)).Single().Texto, "EcoAccess");
            Assert.IsEmpty(await eco.ProcesarAsync(m));
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task EcoConsultaPrivadaDelInvitadoConservaLaRespuestaDePermisos()
        {
            var eco = new AsistenteEco(_servicio.ProcesarAsync);
            var m = new MensajeEntrante(++_update, Invitado, Invitado, true, "Mis depósitos", null);
            Assert.AreEqual(MensajesTelegram.SinVinculoConsulta, (await eco.ProcesarAsync(m)).Single().Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task EcoFotoRepetidaONonceAjenoNoRepiteProgresoNiDescarga()
        {
            int avisos = 0, descargas = 0;
            AsistenteEco? eco = null;
            var servicio = ConVision(TipoPropuestaVision.MaterialIdentificado, async (_, ct) =>
            {
                await eco!.NotificarAnalisisFotoAsync(ct);
                descargas++;
                return new ImagenResiduo(FotoDelDoble, "image/png");
            }, consentimiento: true);
            eco = new(servicio.ProcesarAsync, (_, _) => { avisos++; return Task.CompletedTask; });
            await PrepararConAsync(servicio);
            var foto = new MensajeEntrante(++_update, Vinculado, Vinculado, true, null, null, "foto-sintetica");
            string consentimiento = (await eco.ProcesarAsync(foto)).Single().Botones[0].Dato;
            Assert.AreEqual(0, avisos);
            var ajeno = new MensajeEntrante(++_update, Invitado, Invitado, true, null, consentimiento);
            await eco.ProcesarAsync(ajeno);
            Assert.AreEqual(0, avisos);
            var valido = new MensajeEntrante(++_update, Vinculado, Vinculado, true, null, consentimiento);
            await Task.WhenAll(eco.ProcesarAsync(valido), eco.ProcesarAsync(valido));
            await eco.ProcesarAsync(valido with { UpdateId = ++_update });
            Assert.AreEqual(1, avisos);
            Assert.AreEqual(1, descargas);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task EcoConsentimientoVencidoNoDescargaNiAnunciaAnalisis()
        {
            int avisos = 0;
            var servicio = ConVision(TipoPropuestaVision.MaterialIdentificado,
                (_, _) => throw new AssertFailedException("No se puede descargar con consentimiento vencido."), consentimiento: true);
            var eco = new AsistenteEco(servicio.ProcesarAsync, (_, _) => { avisos++; return Task.CompletedTask; });
            await PrepararConAsync(servicio);
            var foto = new MensajeEntrante(++_update, Vinculado, Vinculado, true, null, null, "foto-sintetica");
            string boton = (await eco.ProcesarAsync(foto)).Single().Botones[0].Dato;
            _reloj.Avanzar(TimeSpan.FromMinutes(3));
            await eco.ProcesarAsync(foto with { UpdateId = ++_update, ArchivoFoto = null, DatoBoton = boton });
            Assert.AreEqual(0, avisos);
            Assert.IsEmpty(_interacciones.Ordenes);
        }
    }
}
