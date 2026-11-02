using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    [TestClass]
    [TestCategory(Categoria)]
    [DoNotParallelize]
    public sealed class MaterialesPaginaOracleTests : BaseIntegracionOracle
    {
        [TestMethod]
        public async Task OrientacionPersistidaYBusquedaPorSinonimoNoAutorizaEspeciales()
        {
            var repo = new RepositorioMaterialesOracle(Fabrica);
            string marca = Sufijo();
            int id = RegistrarMaterial(await repo.AgregarAsync(new Material("ALI_" + marca, "Entrada especial " + marca,
                CategoriaMaterial.Peligroso, "Consulta a tu EPS o IPS.", "lancéta " + marca,
                "No mezclar con medicamentos.", "Ruta de cortopunzantes.")));
            var leida = (await repo.ObtenerPorIdAsync(id))!;
            Assert.AreEqual("lancéta " + marca, leida.Sinonimos);
            Assert.AreEqual("No mezclar con medicamentos.", leida.Precauciones);
            Assert.AreEqual("Ruta de cortopunzantes.", leida.RutaSeparacion);
            var servicio = new EcoAccess.P3.Logica.Servicios.ServicioCatalogoAmbiental(repo, new RepositorioContenedoresOracle(Fabrica));
            Assert.AreEqual(id, (await servicio.ConsultarPaginaPublicaAsync("lanceta " + marca)).Valor.Filas.Single().Id);
            Assert.AreEqual(id, (await servicio.BuscarAsync(marca)).Single(m => m.Id == id).Id);
        }

        [TestMethod]
        public async Task CatalogoPublicoPaginaEnOracle_OcultaFixturesYBuscaTildesSinCambiarFilas()
        {
            var repo = new RepositorioMaterialesOracle(Fabrica); string marca = Sufijo();
            for (int i = 0; i < 12; i++) RegistrarMaterial(await repo.AgregarAsync(new Material("PUB_" + marca + i.ToString("D2"),
                "Cartón " + marca + " " + i, CategoriaMaterial.Aprovechable, null)));
            int privado = RegistrarMaterial(await repo.AgregarAsync(new Material("BR_" + marca, "Material ficticio bridge " + marca, CategoriaMaterial.Aprovechable, null)));
            var servicio = new EcoAccess.P3.Logica.Servicios.ServicioCatalogoAmbiental(repo, new RepositorioContenedoresOracle(Fabrica));
            var reloj = System.Diagnostics.Stopwatch.StartNew();
            var primera = await servicio.ConsultarPaginaPublicaAsync("carton " + marca, 1);
            var segunda = await servicio.ConsultarPaginaPublicaAsync("carton " + marca, 2);
            Assert.IsTrue(primera.Exitoso); Assert.HasCount(10, primera.Valor.Filas); Assert.IsTrue(primera.Valor.HaySiguiente);
            Assert.HasCount(2, segunda.Valor.Filas); Assert.IsFalse(segunda.Valor.HaySiguiente);
            Assert.IsFalse(primera.Valor.Filas.Select(m => m.Id).Intersect(segunda.Valor.Filas.Select(m => m.Id)).Any());
            Assert.IsEmpty((await servicio.ConsultarPaginaPublicaAsync("bridge " + marca)).Valor.Filas);
            Assert.IsNotNull(await repo.ObtenerPorIdAsync(privado));
            Assert.IsTrue((await repo.ObtenerPorIdAsync(privado))!.Habilitado);
            Console.WriteLine($"Consulta pública real: páginas10+2, fixture oculto conservado, {reloj.ElapsedMilliseconds}ms; sin medir renderizado GUI.");
        }

        [TestMethod]
        public async Task PaginaYEstadosNoRecortanElCatalogoCompleto()
        {
            var repo = new RepositorioMaterialesOracle(Fabrica);
            string marca = Sufijo();
            var ids = new List<int>();
            for (int i = 0; i < 27; i++)
                ids.Add(RegistrarMaterial(await repo.AgregarAsync(new Material("PG" + marca + i.ToString("D2"),
                    "Material ficticio " + marca, CategoriaMaterial.Aprovechable, null))));
            var primera = await repo.ConsultarPaginaAsync(new FiltroMateriales(marca));
            Assert.HasCount(25, primera.Filas);
            Assert.IsTrue(primera.HaySiguiente);
            var segunda = await repo.ConsultarPaginaAsync(new FiltroMateriales(marca, pagina: 2));
            Assert.HasCount(2, segunda.Filas);
            Assert.IsFalse(segunda.HaySiguiente);
            Assert.IsFalse(primera.Filas.Select(m => m.Id).Intersect(segunda.Filas.Select(m => m.Id)).Any());
            var completo = (await repo.ListarAsync(true)).Where(m => ids.Contains(m.Id)).ToArray();
            Assert.HasCount(27, completo);
            await repo.DeshabilitarAsync(ids[0]);
            Assert.IsEmpty((await repo.ConsultarPaginaAsync(new FiltroMateriales("PG" + marca + "00", habilitado: true))).Filas);
            Assert.AreEqual(ids[0], (await repo.ConsultarPaginaAsync(new FiltroMateriales("PG" + marca + "00", habilitado: false))).Filas.Single().Id);
        }

        [TestMethod]
        public async Task BusquedaLiteralYCategoriaSonParametros()
        {
            var repo = new RepositorioMaterialesOracle(Fabrica);
            string marca = Sufijo();
            int id = RegistrarMaterial(await repo.AgregarAsync(new Material("LT" + marca,
                "Prueba " + marca + " %_'", CategoriaMaterial.Peligroso, "Ficticio, solo para prueba de consulta.")));
            RegistrarMaterial(await repo.AgregarAsync(new Material("OT" + marca, "Prueba " + marca + " normal",
                CategoriaMaterial.Aprovechable, null)));
            Assert.AreEqual(id, (await repo.ConsultarPaginaAsync(new FiltroMateriales(marca + " %_'"))).Filas.Single().Id);
            Assert.AreEqual(id, (await repo.ConsultarPaginaAsync(new FiltroMateriales(marca, categoria: CategoriaMaterial.Peligroso))).Filas.Single().Id);
            Assert.IsEmpty((await repo.ConsultarPaginaAsync(new FiltroMateriales("' OR 1=1 --"))).Filas);
        }
    }
}
