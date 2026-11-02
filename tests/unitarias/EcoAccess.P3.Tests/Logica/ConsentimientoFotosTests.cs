using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioConversacionTelegramTests
    {
        [TestMethod]
        public async Task FotoRemotaNoSeDescargaHastaConsentirUnaVezYNoGeneraOrden()
        {
            int descargas = 0;
            var servicio = ConVision(TipoPropuestaVision.MaterialIdentificado, (_, _) =>
                { descargas++; return Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png")); }, consentimiento: true);
            await PrepararConAsync(servicio);
            var aviso = Unica(await EnviarFotoAsync(servicio)); string boton = aviso.Botones[0].Dato;
            Assert.AreEqual(0, descargas); Assert.AreEqual(MensajesTelegram.AvisoConsentimientoFoto, aviso.Texto);
            await EnviarConAsync(servicio, boton: boton, cuenta: Invitado); Assert.AreEqual(0, descargas);
            var propuesta = Unica(await EnviarConAsync(servicio, boton: boton)); Assert.AreEqual(1, descargas);
            Assert.AreEqual(ServicioConversacionTelegram.ConfirmarSi, propuesta.Botones[0].Dato);
            await EnviarConAsync(servicio, boton: boton); Assert.AreEqual(1, descargas);
            Assert.IsEmpty(_interacciones.Ordenes); Assert.IsEmpty(_interacciones.Depositos);
        }

        [TestMethod]
        [DataRow("tiempo")]
        [DataRow("mensaje")]
        [DataRow("nueva-foto")]
        [DataRow("reinicio")]
        public async Task ConsentimientoViejoNoDescargaTrasCambioOVencimiento(string cambio)
        {
            int descargas = 0;
            Task<ImagenResiduo> Descargar(string _, CancellationToken __)
            { descargas++; return Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png")); }
            var servicio = ConVision(TipoPropuestaVision.MaterialIdentificado, Descargar, consentimiento: true);
            await PrepararConAsync(servicio); string boton = Unica(await EnviarFotoAsync(servicio)).Botones[0].Dato;
            switch (cambio)
            {
                case "tiempo": _reloj.Avanzar(TimeSpan.FromMinutes(2)); break;
                case "mensaje": await EnviarConAsync(servicio, "/ayuda"); break;
                case "nueva-foto": await EnviarFotoAsync(servicio); break;
                case "reinicio": servicio = ConVision(TipoPropuestaVision.MaterialIdentificado, Descargar, consentimiento: true); break;
            }
            await EnviarConAsync(servicio, boton: boton);
            Assert.AreEqual(0, descargas); Assert.IsEmpty(_interacciones.Ordenes);
        }

        private sealed class ClasificadorSinCuota : IClasificadorMaterial
        {
            public string Proveedor => "cuota-ficticia";
            public Task<PropuestaVision> ClasificarAsync(ImagenResiduo foto, IReadOnlyList<EcoAccess.P3.Entidades.Material> catalogo, CancellationToken ct = default) =>
                Task.FromException<PropuestaVision>(new CuotaVisionAgotadaException());
        }

        [TestMethod]
        public async Task CuotaAgotadaSeInformaYSusBotonesAnterioresNoAbren()
        {
            var clasificacion = new ServicioClasificacionImagen(new ClasificadorSinCuota(), _materiales, new ServicioCatalogoAmbiental(_materiales, _contenedores));
            var servicio = new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj, vision: clasificacion,
                descargarFoto: (_, _) => Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png")));
            await PrepararConAsync(servicio); var r = Unica(await EnviarFotoAsync(servicio));
            StringAssert.Contains(r.Texto, "cuota gratuita"); Assert.IsFalse(r.Botones.Any(b => b.Dato == ServicioConversacionTelegram.ConfirmarSi));
            Assert.IsTrue(r.Botones.Any(b => b.Dato == ServicioConversacionTelegram.PrefijoMenu + "/manual"));
            await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi);
            Assert.IsEmpty(_interacciones.Ordenes); Assert.IsEmpty(_interacciones.Depositos);
        }
    }
}
