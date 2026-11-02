using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioInteraccionesTests
    {
        [TestMethod]
        public async Task SolicitudAlLimiteDeQuinceMinutosNoClasificaNiEmiteOrden()
        {
            int id = await IniciarAsync();
            _reloj.Avanzar(PoliticaVigenciaSolicitud.Vigencia);
            Assert.IsFalse((await _servicio.ValidarSolicitudActivaAsync(Ciudadano, id)).Exitoso);
            var resultado = await _servicio.AutorizarAperturaAsync(Ciudadano, id, _pet.Id, "vencida-0001");
            Assert.IsFalse(resultado.Exitoso);
            Assert.AreEqual(MensajesInteracciones.SolicitudVencida, resultado.Mensaje);
            Assert.IsNull(await _repositorio.ObtenerOrdenPorClaveAsync("vencida-0001"));
            Assert.AreEqual(EstadoInteraccion.Iniciada, _repositorio.LeerInteraccion(id)!.Estado);
        }

        [TestMethod]
        public async Task SolicitudJustoAntesDelLimiteSeAutorizaYReintentoConservaOrdenVigente()
        {
            int id = await IniciarAsync();
            _reloj.Avanzar(PoliticaVigenciaSolicitud.Vigencia - TimeSpan.FromTicks(1));
            var primera = await _servicio.AutorizarAperturaAsync(Ciudadano, id, _pet.Id, "borde-0001");
            Assert.IsTrue(primera.Exitoso, primera.Mensaje);
            _reloj.Avanzar(TimeSpan.FromSeconds(10));
            var repetida = await _servicio.AutorizarAperturaAsync(Ciudadano, id, _pet.Id, "borde-0001");
            Assert.IsTrue(repetida.Exitoso, repetida.Mensaje);
            Assert.AreEqual(primera.Valor, repetida.Valor);
        }

        [TestMethod]
        public void PoliticaNoAceptaFechasFuturasNiHorasLocales()
        {
            DateTime ahora = Inicio.UtcDateTime;
            Assert.IsFalse(PoliticaVigenciaSolicitud.EstaVigente(ahora.AddTicks(1), ahora));
            Assert.IsFalse(PoliticaVigenciaSolicitud.EstaVigente(DateTime.SpecifyKind(ahora, DateTimeKind.Unspecified), ahora));
            Assert.IsTrue(PoliticaVigenciaSolicitud.EstaVigente(ahora, ahora));
        }
    }

    public sealed partial class ServicioConversacionTelegramTests
    {
        [TestMethod]
        public async Task FotoDeSolicitudVencidaNoSeDescargaAunqueDialogoSeMantengaActivo()
        {
            int descargas = 0;
            var servicio = ConVision(TipoPropuestaVision.Incierta, (_, _) =>
            {
                descargas++;
                return Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png"));
            });
            await EnviarConAsync(servicio, "/start " + _caneca.IdentificadorQr);
            _reloj.Avanzar(TimeSpan.FromMinutes(10));
            await EnviarConAsync(servicio, "/separar");
            _reloj.Avanzar(TimeSpan.FromMinutes(5));
            await EnviarFotoAsync(servicio);
            Assert.AreEqual(0, descargas);
            Assert.IsEmpty(_interacciones.Ordenes);
        }
    }
}
