using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioInteraccionesTests
    {
        [TestMethod]
        public async Task TarifaUnitaria_DeclararTresEstimaSeis_VerificarDosAcreditaCuatroUnaVez()
        {
            var (servicio, libro, _) = PrepararEcopuntos();
            int regla = (await servicio.RegistrarReglaAsync(Admin, _pet.Id, 2, "Por unidad de ensayo", AhoraUtc, porUnidad: true)).Valor;
            var potencial = await servicio.ConsultarPotencialesAsync(Ciudadano, _pet.Id, 3);
            Assert.IsTrue(potencial.Exitoso); Assert.AreEqual(6, potencial.Valor.PuntosPotenciales);
            Assert.IsEmpty(libro.Movimientos);

            int solicitud = (await _servicio.IniciarConCantidadAsync(Ciudadano, _caneca.Id, CanalInteraccion.Simulador, 3)).Valor;
            int orden = (await _servicio.AutorizarAperturaAsync(Ciudadano, solicitud, _pet.Id, Clave)).Valor;
            Assert.IsTrue((await _servicio.MarcarEnviadaAsync(orden)).Exitoso);
            foreach (var tipo in new[] { TipoEventoDispositivo.Acuse, TipoEventoDispositivo.Apertura, TipoEventoDispositivo.Deteccion })
                Assert.AreEqual(ResultadoEvento.Aplicado, (await EventoAsync("unidad-" + tipo, tipo, orden)).Valor.Resultado);
            int deposito = _repositorio.Depositos.Single().Id;
            Assert.IsFalse((await servicio.AcreditarAsync(Admin, deposito, regla, "aun-sin-verificar")).Exitoso);
            Assert.IsTrue((await _servicio.VerificarDepositoAsync(Operario, deposito, _pet.Id, cantidadVerificada: 2)).Exitoso);
            Assert.AreEqual(3, _repositorio.LeerInteraccion(solicitud)!.CantidadDeclarada);
            var credito = await servicio.AcreditarAsync(Admin, deposito, regla, "credito-unitario");
            Assert.IsTrue(credito.Exitoso); Assert.AreEqual(4, credito.Valor.Single().Puntos);
            Assert.AreEqual(4, (await servicio.AcreditarAsync(Admin, deposito, regla, "credito-unitario")).Valor.Single().Puntos);
            Assert.HasCount(1, libro.Movimientos);
        }

        [TestMethod]
        public async Task TarifaUnitaria_MaterialVerificadoSinConteoNoPermiteCreditoNiAtribuirDeclaracion()
        {
            var (servicio, libro, _) = PrepararEcopuntos();
            int regla = (await servicio.RegistrarReglaAsync(Admin, _pet.Id, 2, "Por unidad", AhoraUtc, porUnidad: true)).Valor;
            int deposito = await DepositoVerificadoAsync();
            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await servicio.AcreditarAsync(Admin, deposito, regla, "no-conteo")).Motivo);
            Assert.IsEmpty(libro.Movimientos);
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await servicio.RegistrarReglaAsync(Ciudadano, _pet.Id, 2, "No autorizado", AhoraUtc, porUnidad: true)).Motivo);
        }

        [TestMethod]
        public void TarifaUnitaria_LimitaTotalYCantidadSinCambiarTarifaHistorica()
        {
            var unidad = new ReglaEcopuntos(1, _pet.Id, 1_000_000, "Ensayo", AhoraUtc, null, Admin, true);
            Assert.ThrowsExactly<InvalidOperationException>(() => unidad.CalcularPuntos(null));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => unidad.CalcularPuntos(0));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => unidad.CalcularPuntos(1001));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => unidad.CalcularPuntos(2));
            Assert.AreEqual(10, new ReglaEcopuntos(1, _pet.Id, 10, "Histórica", AhoraUtc, null, Admin).CalcularPuntos(null));
        }
    }
}
