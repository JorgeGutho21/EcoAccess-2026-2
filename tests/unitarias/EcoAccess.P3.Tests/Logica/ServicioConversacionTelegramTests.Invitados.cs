using System;
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
        public async Task Invitado_NoPuedeConsultarAutorizarNiSugerirInteraccionDeOtraCuenta()
        {
            await IniciarInvitadoPorQrAsync();
            int id = _conversaciones.Leer(Invitado)!.IdInteraccion!.Value;
            var invitados = new ServicioInvitadosTelegram(_conversaciones, _servicioInteracciones, _reloj);
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await invitados.AutorizarAsync(Vinculado, id, _pet.Id, "ajena")).Motivo);
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await invitados.ConsultarAsync(Vinculado, id, "ajena")).Motivo);
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await invitados.RegistrarSugerenciaAsync(Vinculado, id, _pet.Id, .9m)).Motivo);
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await invitados.CancelarAsync(Vinculado, id, "ajena")).Motivo);
            Assert.AreEqual(EstadoInteraccion.Iniciada, _interacciones.LeerInteraccion(id)!.Estado);
        }

        [TestMethod]
        public async Task Invitado_ContratoEscritorioNoAutorizaLaInteraccionTelegram()
        {
            await IniciarInvitadoPorQrAsync();
            int id = _conversaciones.Leer(Invitado)!.IdInteraccion!.Value;
            Assert.AreEqual(MotivoRechazo.SinPermiso,
                (await _servicioInteracciones.AutorizarAperturaVisitanteAsync(id, _pet.Id, "cruzada")).Motivo);
        }

        [TestMethod]
        public async Task Invitado_SesionVencidaNoConsumeAnalisisNiAutoriza()
        {
            await IniciarInvitadoPorQrAsync();
            int id = _conversaciones.Leer(Invitado)!.IdInteraccion!.Value;
            _reloj.Avanzar(ServicioConversacionTelegram.VigenciaConversacion + TimeSpan.FromSeconds(1));
            var invitados = new ServicioInvitadosTelegram(_conversaciones, _servicioInteracciones, _reloj);
            Assert.IsFalse((await invitados.ValidarAsync(Invitado, id)).Exitoso);
            Assert.IsFalse((await invitados.AutorizarAsync(Invitado, id, _pet.Id, "vencida")).Exitoso);
            Assert.IsTrue((await invitados.CancelarAsync(Invitado, id, "Vencida")).Exitoso);
        }

        [TestMethod]
        public async Task Invitado_NuevoQrCancelaSolicitudAnteriorSinReasignarUsuario()
        {
            await IniciarInvitadoPorQrAsync();
            int anterior = _conversaciones.Leer(Invitado)!.IdInteraccion!.Value;
            await IniciarInvitadoPorQrAsync();
            Assert.AreEqual(EstadoInteraccion.Rechazada, _interacciones.LeerInteraccion(anterior)!.Estado);
            Assert.IsNull(_interacciones.LeerInteraccion(anterior)!.IdUsuario);
            var invitados = new ServicioInvitadosTelegram(_conversaciones, _servicioInteracciones, _reloj);
            Assert.IsFalse((await invitados.AutorizarAsync(Invitado, anterior, _pet.Id, "anterior")).Exitoso);
        }

        [TestMethod]
        public async Task Invitado_CancelacionNoCreaOrdenYPermiteReiniciar()
        {
            await IniciarInvitadoPorQrAsync();
            int anterior = _conversaciones.Leer(Invitado)!.IdInteraccion!.Value;
            await TextoAsync("/cancelar", Invitado);
            Assert.AreEqual(EstadoInteraccion.Rechazada, _interacciones.LeerInteraccion(anterior)!.Estado);
            Assert.AreEqual(EstadoConversacion.SinCaneca, _conversaciones.Leer(Invitado)!.Estado);
        }
        [TestMethod]
        public async Task Invitado_VincularDuranteSolicitudConservaTitularNuloYPermiteConfirmar()
        {
            await IniciarInvitadoPorQrAsync();
            int id = _conversaciones.Leer(Invitado)!.IdInteraccion!.Value;
            _vinculos.Sembrar(Ciudadano, Invitado);
            await BotonAsync(MaterialPet, Invitado);
            await BotonAsync(ServicioConversacionTelegram.ConfirmarSi, Invitado);
            Assert.AreEqual(EstadoConversacion.OrdenEmitida, _conversaciones.Leer(Invitado)!.Estado);
            Assert.IsNull(_interacciones.LeerInteraccion(id)!.IdUsuario);
            StringAssert.Contains(Unica(await TextoAsync("/estado", Invitado)).Texto, "sin ecopuntos");
            Assert.AreEqual(MensajesTelegram.YaVinculada, Unica(await TextoAsync("/vincular", Invitado)).Texto);
        }
        [TestMethod]
        public async Task Invitado_VincularYNuevoQrIniciaComoCiudadanoSinReasignarLaAnterior()
        {
            await IniciarInvitadoPorQrAsync();
            int anterior = _conversaciones.Leer(Invitado)!.IdInteraccion!.Value;
            _vinculos.Sembrar(Ciudadano, Invitado);
            await IniciarInvitadoPorQrAsync();
            int actual = _conversaciones.Leer(Invitado)!.IdInteraccion!.Value;
            Assert.AreNotEqual(anterior, actual);
            Assert.IsNull(_interacciones.LeerInteraccion(anterior)!.IdUsuario);
            Assert.AreEqual(EstadoInteraccion.Rechazada, _interacciones.LeerInteraccion(anterior)!.Estado);
            Assert.AreEqual(Ciudadano, _interacciones.LeerInteraccion(actual)!.IdUsuario);
        }

        [TestMethod]
        public async Task Invitado_VincularYDesvincularCancelaSuSolicitudSinPerderlaAbierta()
        {
            await IniciarInvitadoPorQrAsync();
            int anterior = _conversaciones.Leer(Invitado)!.IdInteraccion!.Value;
            _vinculos.Sembrar(Ciudadano, Invitado);
            await TextoAsync("/desvincular", Invitado);
            Assert.AreEqual(EstadoInteraccion.Rechazada, _interacciones.LeerInteraccion(anterior)!.Estado);
            Assert.IsNull(_interacciones.LeerInteraccion(anterior)!.IdUsuario);
        }
    }
}
