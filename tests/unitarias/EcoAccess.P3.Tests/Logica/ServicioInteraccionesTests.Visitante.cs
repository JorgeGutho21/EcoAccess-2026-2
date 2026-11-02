using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioInteraccionesTests
    {
        // Visitante: deposita en la caneca DEMO sin cuenta y sin ecopuntos

        [TestMethod]
        public async Task IniciarVisitante_CreaInteraccionSinUsuarioPorSimulador()
        {
            var r = await _servicio.IniciarVisitanteAsync(_caneca.Id);

            Assert.IsTrue(r.Exitoso, r.ToString());
            Interaccion guardada = _repositorio.LeerInteraccion(r.Valor)!;
            Assert.IsNull(guardada.IdUsuario);
            Assert.AreEqual(CanalInteraccion.Simulador, guardada.Canal);
            Assert.AreEqual(EstadoInteraccion.Iniciada, guardada.Estado);
        }

        [TestMethod]
        public async Task AutorizarVisitante_MaterialAdmitido_EmiteOrdenYSePuedeConsultar()
        {
            int id = (await _servicio.IniciarVisitanteAsync(_caneca.Id)).Valor;

            var orden = await _servicio.AutorizarAperturaVisitanteAsync(id, _pet.Id, Clave);
            var consulta = await _servicio.ConsultarOperacionVisitanteAsync(id, Clave);

            Assert.IsTrue(orden.Exitoso, orden.ToString());
            Assert.IsTrue(consulta.Exitoso, consulta.ToString());
            Assert.AreEqual(orden.Valor, consulta.Valor.Orden.Id);
            Assert.IsNull(consulta.Valor.Deposito);
        }

        [TestMethod]
        public async Task AutorizarVisitante_MaterialNoAdmitido_NoEmiteOrden()
        {
            int id = (await _servicio.IniciarVisitanteAsync(_caneca.Id)).Valor;

            var r = await _servicio.AutorizarAperturaVisitanteAsync(id, _vidrio.Id, Clave);

            Assert.IsFalse(r.Exitoso);
            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
        }

        [TestMethod]
        public async Task Visitante_NoAlcanzaSolicitudesDeUnCiudadano()
        {
            int delCiudadano = await IniciarAsync();

            var autorizar = await _servicio.AutorizarAperturaVisitanteAsync(delCiudadano, _pet.Id, Clave);
            var consultar = await _servicio.ConsultarOperacionVisitanteAsync(delCiudadano, Clave);

            Assert.AreEqual(MotivoRechazo.SinPermiso, autorizar.Motivo);
            Assert.AreEqual(MotivoRechazo.SinPermiso, consultar.Motivo);
        }

        [TestMethod]
        public async Task Ciudadano_NoAlcanzaSolicitudesDeUnVisitante()
        {
            int delVisitante = (await _servicio.IniciarVisitanteAsync(_caneca.Id)).Valor;

            var r = await _servicio.AutorizarAperturaAsync(Ciudadano, delVisitante, _pet.Id, Clave);

            Assert.AreEqual(MotivoRechazo.SinPermiso, r.Motivo);
        }

        [TestMethod]
        public async Task Visitante_OrdenVigente_ElBridgePuedeEntregarla()
        {
            int id = (await _servicio.IniciarVisitanteAsync(_caneca.Id)).Valor;
            await _servicio.AutorizarAperturaVisitanteAsync(id, _pet.Id, Clave);
            OrdenApertura orden = (await _servicio.ConsultarOperacionVisitanteAsync(id, Clave)).Valor.Orden;

            bool autorizado = await _servicio.SigueAutorizadoComandoAsync(
                new ComandoApertura(orden.Id, orden.IdContenedor, orden.ClaveIdempotencia, orden.FechaVencimientoUtc));

            Assert.IsTrue(autorizado);
        }

        [TestMethod]
        public async Task Visitante_DeteccionCreaDepositoSinCiudadano()
        {
            int id = (await _servicio.IniciarVisitanteAsync(_caneca.Id)).Valor;
            int idOrden = (await _servicio.AutorizarAperturaVisitanteAsync(id, _pet.Id, Clave)).Valor;
            Assert.IsTrue((await _servicio.MarcarEnviadaAsync(idOrden)).Exitoso);
            await EventoAsync("v-1", TipoEventoDispositivo.Acuse, idOrden);
            await EventoAsync("v-2", TipoEventoDispositivo.Apertura, idOrden);
            await EventoAsync("v-3", TipoEventoDispositivo.Deteccion, idOrden);

            var consulta = await _servicio.ConsultarOperacionVisitanteAsync(id, Clave);

            Assert.IsNotNull(consulta.Valor.Deposito);
            Assert.IsNull(_repositorio.LeerInteraccion(id)!.IdUsuario);
        }
    }
}
