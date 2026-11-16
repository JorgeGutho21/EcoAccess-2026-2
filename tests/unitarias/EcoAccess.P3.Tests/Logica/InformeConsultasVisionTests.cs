using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// El tablero institucional muestra las fotos atendidas por Echo aparte de los depósitos. Solo administración, y si la
    /// base no tiene V023 lo dice (resultado vacío) en lugar de inventar ceros.
    /// </summary>
    [TestClass]
    public sealed class InformeConsultasVisionTests
    {
        private static readonly DateTime Desde = new(2026, 10, 12, 5, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Hasta = new(2026, 11, 11, 5, 0, 0, DateTimeKind.Utc);

        private sealed class SinInforme : IRepositorioInformeInstitucional
        {
            public Task<IndicadoresInforme> ConsultarAsync(FiltroInforme filtro, CancellationToken ct = default) => throw new NotSupportedException();
            public Task<IReadOnlyList<FilaAgrupada>> AgruparAsync(FiltroInforme filtro, AgrupacionDepositos agrupacion, CancellationToken ct = default) => throw new NotSupportedException();
        }

        private sealed class Consultas : IRepositorioConsultasVision
        {
            public ResumenPeriodoConsultasVision? Respuesta { get; set; } = new(5, 3, 2, 2, 1200);
            public int Llamadas { get; private set; }
            public Task GuardarAsync(ConsultaVisionRegistrada consulta, CancellationToken ct = default) => Task.CompletedTask;
            public Task<ResumenConsultasVision> ResumirDeUsuarioAsync(int idUsuario, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default) => throw new NotSupportedException();
            public Task<ResumenPeriodoConsultasVision?> ResumirPeriodoAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
            { Llamadas++; return Task.FromResult(Respuesta); }
        }

        private static RepositorioUsuariosEnMemoria ConAdministrador()
        {
            var usuarios = new RepositorioUsuariosEnMemoria();
            usuarios.Agregar(1, true, CodigoRol.Administrador);
            usuarios.AgregarCiudadano(2);
            return usuarios;
        }

        [TestMethod]
        public async Task ElAdministradorVeElAgregadoDelPeriodo()
        {
            var consultas = new Consultas();
            var r = await new ServicioInformeInstitucional(ConAdministrador(), new SinInforme(), consultas).ConsultarConsultasVisionAsync(1, Desde, Hasta);
            Assert.IsTrue(r.Exitoso);
            Assert.AreEqual(5, r.Valor!.Total);
            Assert.AreEqual(1, consultas.Llamadas);
        }

        [TestMethod]
        public async Task UnCiudadanoNoVeElAgregadoInstitucionalNiSeConsultaOracle()
        {
            var consultas = new Consultas();
            var r = await new ServicioInformeInstitucional(ConAdministrador(), new SinInforme(), consultas).ConsultarConsultasVisionAsync(2, Desde, Hasta);
            Assert.IsFalse(r.Exitoso);
            Assert.AreEqual(0, consultas.Llamadas);
        }

        [TestMethod]
        public async Task SinV023ElResultadoEsVacioYNoCeros()
        {
            var consultas = new Consultas { Respuesta = null };
            var r = await new ServicioInformeInstitucional(ConAdministrador(), new SinInforme(), consultas).ConsultarConsultasVisionAsync(1, Desde, Hasta);
            Assert.IsTrue(r.Exitoso);
            Assert.IsNull(r.Valor);
            var sinRepositorio = await new ServicioInformeInstitucional(ConAdministrador(), new SinInforme()).ConsultarConsultasVisionAsync(1, Desde, Hasta);
            Assert.IsTrue(sinRepositorio.Exitoso);
            Assert.IsNull(sinRepositorio.Valor);
        }

        [TestMethod]
        public async Task RechazaPeriodosInvalidosSinConsultar()
        {
            var consultas = new Consultas();
            var servicio = new ServicioInformeInstitucional(ConAdministrador(), new SinInforme(), consultas);
            Assert.IsFalse((await servicio.ConsultarConsultasVisionAsync(1, Hasta, Desde)).Exitoso);
            Assert.IsFalse((await servicio.ConsultarConsultasVisionAsync(1, Desde, Desde.AddDays(367))).Exitoso);
            Assert.IsFalse((await servicio.ConsultarConsultasVisionAsync(1, DateTime.SpecifyKind(Desde, DateTimeKind.Local), Hasta)).Exitoso);
            Assert.AreEqual(0, consultas.Llamadas);
        }
    }
}
