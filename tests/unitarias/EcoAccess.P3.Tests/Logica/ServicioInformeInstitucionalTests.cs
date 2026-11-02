using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    [TestClass]
    public sealed class ServicioInformeInstitucionalTests
    {
        private sealed class Datos : IRepositorioInformeInstitucional
        {
            public List<FiltroInforme> Consultas { get; } = new List<FiltroInforme>();
            public List<AgrupacionDepositos> Agrupaciones { get; } = new List<AgrupacionDepositos>();
            public Task<IndicadoresInforme> ConsultarAsync(FiltroInforme filtro, CancellationToken ct = default)
            { Consultas.Add(filtro); return Task.FromResult(new IndicadoresInforme(2, 1, 1, 4, 2, 3, 4m)); }
            public Task<IReadOnlyList<FilaAgrupada>> AgruparAsync(FiltroInforme filtro, AgrupacionDepositos agrupacion, CancellationToken ct = default)
            { Agrupaciones.Add(agrupacion); Consultas.Add(filtro); return Task.FromResult<IReadOnlyList<FilaAgrupada>>(Array.Empty<FilaAgrupada>()); }
        }
        private static readonly FiltroInforme Filtro = new(new(2026, 10, 1, 5, 0, 0, DateTimeKind.Utc), new(2026, 11, 1, 5, 0, 0, DateTimeKind.Utc), 7, 8, true);

        [TestMethod]
        public async Task RevalidaAdministradorAntesDeConsultarDatosInstitucionales()
        {
            var usuarios = new RepositorioUsuariosEnMemoria(); usuarios.AgregarCiudadano(1);
            var repo = new Datos(); var servicio = new ServicioInformeInstitucional(usuarios, repo);
            Assert.IsFalse((await servicio.ConsultarAsync(1, Filtro)).Exitoso); Assert.IsEmpty(repo.Consultas);
        }
        [TestMethod]
        public async Task ComparaMismaDuracionYConservaCanecaUbicacionEnTodasLasConsultas()
        {
            var usuarios = new RepositorioUsuariosEnMemoria(); usuarios.Agregar(1, true, CodigoRol.Administrador);
            var repo = new Datos(); var r = await new ServicioInformeInstitucional(usuarios, repo).ConsultarAsync(1, Filtro);
            Assert.IsTrue(r.Exitoso); Assert.HasCount(6, repo.Consultas);
            Assert.AreEqual(Filtro.DesdeUtc, repo.Consultas[1].HastaUtc);
            Assert.AreEqual(Filtro.HastaUtc - Filtro.DesdeUtc, repo.Consultas[1].HastaUtc - repo.Consultas[1].DesdeUtc);
            Assert.IsTrue(repo.Consultas.All(f => f.IdContenedor == 7 && f.IdUbicacion == 8 && f.SoloDemostracion));
            CollectionAssert.AreEquivalent(new[] { AgrupacionDepositos.Dia, AgrupacionDepositos.Material, AgrupacionDepositos.Contenedor, AgrupacionDepositos.Punto }, repo.Agrupaciones);
        }
        [TestMethod]
        public async Task RechazaPeriodosExcesivosFronterasParcialesYFiltrosInvalidosSinConsultar()
        {
            var usuarios = new RepositorioUsuariosEnMemoria(); usuarios.Agregar(1, true, CodigoRol.Administrador);
            var repo = new Datos(); var servicio = new ServicioInformeInstitucional(usuarios, repo);
            foreach (var filtro in new[] { Filtro with { HastaUtc = Filtro.DesdeUtc.AddDays(367) }, Filtro with { DesdeUtc = Filtro.DesdeUtc.AddHours(1) }, Filtro with { IdContenedor = 0 }, Filtro with { IdUbicacion = -1 } })
                Assert.IsFalse((await servicio.ConsultarAsync(1, filtro)).Exitoso);
            Assert.IsEmpty(repo.Consultas);
        }
    }
}
