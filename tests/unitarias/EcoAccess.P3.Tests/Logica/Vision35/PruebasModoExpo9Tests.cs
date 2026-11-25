using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Vision35;
using EcoAccess.P3.Logica.Vision35.Expo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica.Vision35
{
    /// <summary>Modo ExpoSoftware: catálogo de 9, validación determinista, política 5/7/10 s, idempotencia y cola de Oracle.</summary>
    [TestClass]
    public sealed class PruebasModoExpo9Tests
    {
        private static string Json(params (string Nombre, int Cantidad)[] items) =>
            "{\"items\":[" + string.Join(",", items.Select(i => $"{{\"name\":\"{i.Nombre}\",\"count\":{i.Cantidad}}}")) + "]}";

        [TestMethod]
        public void Catalogo_TieneNueveMaterialesTresPorCaneca_YReutilizaCodigos()
        {
            Assert.AreEqual(9, CatalogoExpo9.Todos.Count);
            foreach (var c in new[] { CategoriaMaterial.OrganicoAprovechable, CategoriaMaterial.Aprovechable, CategoriaMaterial.NoAprovechable })
                Assert.AreEqual(3, CatalogoExpo9.Todos.Count(m => m.Caneca == c));
            // Solo la media manzana no tiene código en el catálogo de 35; las demás reutilizan el existente.
            Assert.AreEqual(1, CatalogoExpo9.Todos.Count(m => m.Codigo35 is null));
            foreach (var m in CatalogoExpo9.Todos.Where(m => m.Codigo35 is not null))
                Assert.IsNotNull(CatalogoPrioritario35.BuscarPorCodigo(m.Codigo35), m.Codigo35);
        }

        [DataTestMethod]
        [DataRow("half apple", "E01")]
        [DataRow("banana peel", "E02")]
        [DataRow("half potato", "E03")]
        [DataRow("brown paper bag", "E04")]
        [DataRow("crumpled cardboard", "E04")]
        [DataRow("plastic water bottle", "E05")]
        [DataRow("glass jar", "E06")]
        [DataRow("glass bottle", "E06")]
        [DataRow("toilet paper", "E07")]
        [DataRow("used napkin", "E08")]
        [DataRow("chips bag", "E09")]
        [DataRow("crumpled foil wrapper", null)]
        [DataRow("aluminum foil", null)]
        [DataRow("tissue paper", null)]
        [DataRow("crumpled paper", null)]
        [DataRow("lime", null)]
        [DataRow("juice box", null)]
        public void Interpretar_NombreDelModelo(string nombre, string? codigo)
        {
            var (tipo, material) = CatalogoExpo9.Interpretar(nombre);
            if (codigo is null) Assert.AreNotEqual(TipoNombreExpo.Material, tipo, nombre);
            else Assert.AreEqual(codigo, material?.Codigo, nombre);
        }

        [DataTestMethod]
        [DataRow("USB cable")]
        [DataRow("pill blister")]
        [DataRow("razor")]
        [DataRow("broken glass")]
        [DataRow("battery")]
        public void Interpretar_GestionEspecial_NuncaEsMaterial(string nombre) =>
            Assert.AreEqual(TipoNombreExpo.GestionEspecial, CatalogoExpo9.Interpretar(nombre).Tipo);

        [DataTestMethod]
        [DataRow("hand")]
        [DataRow("robotic hand")]
        [DataRow("table")]
        public void Interpretar_ManoYMesa_SeIgnoran(string nombre) =>
            Assert.AreEqual(TipoNombreExpo.Ignorado, CatalogoExpo9.Interpretar(nombre).Tipo);

        [TestMethod]
        public void Politica_SegunCanecasDistintas()
        {
            Assert.AreEqual(5000, PoliticaTiemposApertura.DuracionMs(1));
            Assert.AreEqual(7000, PoliticaTiemposApertura.DuracionMs(2));
            Assert.AreEqual(10000, PoliticaTiemposApertura.DuracionMs(3));
            Assert.AreEqual(0, PoliticaTiemposApertura.DuracionMs(0));
        }

        [TestMethod]
        public void TresOrganicos_AbrenSoloLaVerdeUnaVez_5s()
        {
            var r = ReconocedorExpo9.Interpretar(Json(("apple", 1), ("banana peel", 1), ("potato", 2), ("hand", 1)), "m");
            Assert.AreEqual(EstadoReconocimientoExpo.Reconocido, r.Estado);
            CollectionAssert.AreEqual(new[] { CategoriaMaterial.OrganicoAprovechable }, r.Canecas.ToArray());
            Assert.AreEqual(5000, r.DuracionAperturaMs);
            Assert.AreEqual(4, r.Materiales.Sum(m => m.Cantidad));
            Assert.IsTrue(r.PuedeAbrirAutomaticamente);
        }

        [TestMethod]
        public void ManzanaBotellaServilleta_AbrenLasTres_10s_SinImportarElOrden()
        {
            var a = ReconocedorExpo9.Interpretar(Json(("used napkin", 1), ("plastic bottle", 1), ("half apple", 1)), "m");
            var b = ReconocedorExpo9.Interpretar(Json(("half apple", 1), ("used napkin", 1), ("plastic bottle", 1)), "m");
            CollectionAssert.AreEqual(a.Canecas.ToArray(), b.Canecas.ToArray());
            Assert.AreEqual(3, a.Canecas.Count);
            Assert.AreEqual(10000, a.DuracionAperturaMs);
            Assert.AreEqual(CombinacionCanecas.C07_VerdeBlancaNegra, a.Combinacion);
        }

        [DataTestMethod]
        [DataRow("apple|plastic bottle", CombinacionCanecas.C04_VerdeBlanca, 7000)]
        [DataRow("potato|used napkin", CombinacionCanecas.C05_VerdeNegra, 7000)]
        [DataRow("glass jar|chips bag", CombinacionCanecas.C06_BlancaNegra, 7000)]
        [DataRow("plastic bottle|glass jar", CombinacionCanecas.C02_Blanca, 5000)]
        [DataRow("toilet paper|used napkin", CombinacionCanecas.C03_Negra, 5000)]
        public void Combinaciones_DelEnunciado(string nombres, CombinacionCanecas esperada, int ms)
        {
            var r = ReconocedorExpo9.Interpretar(Json(nombres.Split('|').Select(n => (n, 1)).ToArray()), "m");
            Assert.AreEqual(esperada, r.Combinacion);
            Assert.AreEqual(ms, r.DuracionAperturaMs);
        }

        [TestMethod]
        public void PapelBlancoDudoso_NoAbreSolo_PeroConservaLoReconocido()
        {
            var r = ReconocedorExpo9.Interpretar(Json(("apple", 1), ("tissue paper", 1)), "m");
            Assert.AreEqual(EstadoReconocimientoExpo.ParcialConDesconocidos, r.Estado);
            Assert.IsFalse(r.PuedeAbrirAutomaticamente);
            CollectionAssert.AreEqual(new[] { CategoriaMaterial.OrganicoAprovechable }, r.Canecas.ToArray());
        }

        [TestMethod]
        public void GestionEspecial_NoAbreNingunaCaneca()
        {
            var r = ReconocedorExpo9.Interpretar(Json(("apple", 1), ("pill blister", 1)), "m");
            Assert.AreEqual(EstadoReconocimientoExpo.GestionEspecial, r.Estado);
            Assert.AreEqual(0, r.Canecas.Count);
            Assert.IsFalse(r.PuedeAbrirAutomaticamente);
        }

        [TestMethod]
        public void JsonCortado_RescataLosObjetosCompletos_YVacioEsError()
        {
            var r = ReconocedorExpo9.Interpretar("{\"items\": [{\"name\": \"banana\", \"count\": 1}, {\"name\": \"bott", "m");
            Assert.AreEqual(1, r.Materiales.Count);
            Assert.AreEqual(EstadoReconocimientoExpo.Error, ReconocedorExpo9.Interpretar("", "m").Estado);
            Assert.AreEqual(EstadoReconocimientoExpo.SinMaterialesExpo, ReconocedorExpo9.Interpretar("{\"items\":[]}", "m").Estado);
        }

        [TestMethod]
        public async Task Servicio_Idempotente_YRegistraConCola()
        {
            string carpeta = Path.Combine(Path.GetTempPath(), "expo-test-" + Guid.NewGuid().ToString("N"));
            var oracle = new RegistroFalso();
            var servicio = new ServicioEstacionExpo(new EstacionExpoSimulada(), new RegistroOperacionesConCola(oracle, carpeta));
            var canecas = new[] { CategoriaMaterial.Aprovechable, CategoriaMaterial.OrganicoAprovechable, CategoriaMaterial.Aprovechable };
            var r1 = await servicio.EjecutarAsync("tg:1", CanalEstacion.TELEGRAM, TipoOperacionEstacion.APERTURA_MANUAL, canecas, 7, "usuario:7");
            var r2 = await servicio.EjecutarAsync("tg:1", CanalEstacion.TELEGRAM, TipoOperacionEstacion.APERTURA_MANUAL, canecas, 7, "usuario:7");
            Assert.IsTrue(r1.AperturaEnviada && r1.Simulada && r1.RegistradaEnOracle);
            Assert.AreEqual(7000, r1.DuracionMs);
            Assert.AreEqual(2, r1.Canecas.Count);
            Assert.IsTrue(r2.Duplicada && !r2.AperturaEnviada);
            Assert.AreEqual(1, oracle.Filas.Count);
            Assert.AreEqual("VERDE,BLANCA", oracle.Filas[0].Canecas);

            oracle.Caido = true;
            var r3 = await servicio.EjecutarAsync("tg:2", CanalEstacion.PC, TipoOperacionEstacion.APERTURA_MANUAL, new[] { CategoriaMaterial.NoAprovechable }, null, "pc");
            Assert.IsTrue(r3.AperturaEnviada && !r3.RegistradaEnOracle, "sin Oracle no se informa un guardado inexistente");
            Assert.AreEqual(1, servicio.Registro.Pendientes);
            oracle.Caido = false;
            Assert.AreEqual(1, await servicio.Registro.SincronizarAsync());
            Assert.AreEqual(0, servicio.Registro.Pendientes);
            Assert.IsTrue(oracle.Filas.Single(f => f.Clave == "tg:2").SincronizadaOffline);
            try { Directory.Delete(carpeta, true); } catch (IOException) { }
        }

        [TestMethod]
        public async Task Servicio_SinCanecas_NoAbreNada()
        {
            var servicio = new ServicioEstacionExpo(new EstacionExpoSimulada(), new RegistroOperacionesConCola(new RegistroFalso(), Path.GetTempPath()));
            var r = await servicio.EjecutarAsync("x", CanalEstacion.PC, TipoOperacionEstacion.APERTURA_MANUAL, Array.Empty<CategoriaMaterial>(), null, "pc");
            Assert.IsFalse(r.AperturaEnviada);
        }

        private sealed class RegistroFalso : IRegistroOperacionesEstacion
        {
            public bool Caido;
            public readonly List<OperacionEstacion> Filas = new();
            public Task GuardarAsync(OperacionEstacion op, CancellationToken ct = default)
            {
                if (Caido) throw new InvalidOperationException("Oracle caído");
                Filas.RemoveAll(f => f.Clave == op.Clave); Filas.Add(op); return Task.CompletedTask;
            }
            public Task<IReadOnlyList<OperacionEstacion>> RecientesAsync(int? idUsuario, int maximo, CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<OperacionEstacion>>(Filas.ToList());
        }
    }
}
