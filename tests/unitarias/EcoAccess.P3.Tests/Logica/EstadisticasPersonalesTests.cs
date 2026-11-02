using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    [TestClass]
    public sealed class EstadisticasPersonalesTests
    {
        private static readonly DateTime Hasta = new(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);
        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioEstadisticasEnMemoria _repositorio = new();

        [TestMethod]
        public async Task HistorialPropioFijaActorYPaginaSinDarPermisosAdministrativos()
        {
            _usuarios.AgregarCiudadano(3);
            var servicio = new ServicioEstadisticas(_usuarios, _repositorio);
            var pagina = await servicio.ListarMiosAsync(3, Hasta.AddDays(-30), Hasta, 2, 10);
            Assert.IsTrue(pagina.Exitoso);
            Assert.AreEqual(3, _repositorio.UltimoFiltro!.IdUsuario);
            Assert.AreEqual((2, 10), _repositorio.UltimaPagina);
            Assert.AreEqual(MotivoRechazo.SinPermiso,
                (await servicio.ListarDepositosAsync(3, _repositorio.UltimoFiltro, 2, 10)).Motivo);
        }

        [TestMethod]
        public async Task HistorialPrivadoInvalidoONoConsentidoNoConsulta()
        {
            _usuarios.AgregarCiudadano(3);
            _usuarios.AgregarCiudadano(4, consentimiento: false);
            var servicio = new ServicioEstadisticas(_usuarios, _repositorio);
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, (await servicio.ListarMiosAsync(3, Hasta.AddDays(-91), Hasta)).Motivo);
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, (await servicio.ListarMiosAsync(3, Hasta.AddDays(-1), Hasta, 0)).Motivo);
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, (await servicio.ListarMiosAsync(3, Hasta.AddDays(-1), Hasta, 1, 101)).Motivo);
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await servicio.ListarMiosAsync(4, Hasta.AddDays(-1), Hasta)).Motivo);
            Assert.AreEqual(0, _repositorio.Llamadas);
        }

        [TestMethod]
        public async Task TodasLasConsultasUsanLaIdentidadDelActor_YPeriodosSeparados()
        {
            _usuarios.AgregarCiudadano(3);
            var servicio = new ServicioEstadisticas(_usuarios, _repositorio);
            var r = await servicio.ObtenerMiasAsync(3, Hasta.AddDays(-30), Hasta);
            Assert.IsTrue(r.Exitoso, r.ToString());
            Assert.HasCount(4, _repositorio.Filtros);
            Assert.IsTrue(_repositorio.Filtros.All(f => f.IdUsuario == 3));
            Assert.AreEqual(Hasta.AddDays(-60), r.Valor.PeriodoAnterior.DesdeUtc);
            Assert.AreEqual(r.Valor.Periodo.DesdeUtc, r.Valor.PeriodoAnterior.HastaUtc);
            Assert.AreEqual(r.Valor.Periodo, _repositorio.Filtros[2]);
            Assert.AreEqual(r.Valor.Periodo, _repositorio.Filtros[3]);
            Assert.AreEqual(MotivoRechazo.SinPermiso,
                (await servicio.ObtenerResumenAsync(3, r.Valor.Periodo)).Motivo);
            Assert.AreEqual(4, _repositorio.Llamadas);
        }

        [TestMethod]
        [DataRow(false, true, MotivoRechazo.ActorInactivo)]
        [DataRow(true, false, MotivoRechazo.SinPermiso)]
        public async Task CuentaInactivaOSinConsentimiento_NoConsulta(bool activo, bool consentimiento, MotivoRechazo motivo)
        {
            _usuarios.AgregarCiudadano(3, consentimiento, activo);
            var r = await new ServicioEstadisticas(_usuarios, _repositorio).ObtenerMiasAsync(3, Hasta.AddDays(-30), Hasta);
            Assert.AreEqual(motivo, r.Motivo);
            Assert.AreEqual(0, _repositorio.Llamadas);
        }

        [TestMethod]
        public async Task FechasInvalidasOLimiteExcedido_NoConsulta()
        {
            _usuarios.AgregarCiudadano(3);
            var servicio = new ServicioEstadisticas(_usuarios, _repositorio);
            foreach (DateTime desde in new[] { Hasta, Hasta.AddDays(-91), DateTime.SpecifyKind(Hasta.AddDays(-1), DateTimeKind.Local), DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc) })
                Assert.AreEqual(MotivoRechazo.DatosInvalidos, (await servicio.ObtenerMiasAsync(3, desde, Hasta)).Motivo);
            Assert.AreEqual(0, _repositorio.Llamadas);
        }

        [TestMethod]
        public async Task CancelacionDurantePrimeraConsulta_NoSigueConMaterialesNiPeriodoAnterior()
        {
            _usuarios.AgregarCiudadano(3);
            using var cancelar = new CancellationTokenSource();
            _repositorio.AlConsultar = cancelar.Cancel;
            var servicio = new ServicioEstadisticas(_usuarios, _repositorio);
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
                servicio.ObtenerMiasAsync(3, Hasta.AddDays(-30), Hasta, cancelar.Token));
            Assert.AreEqual(1, _repositorio.Llamadas);
        }
    }
}
