using System.Linq;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// Catálogo unificado del escritorio: residuos del piloto (depositables) más la matriz de ECO (orientación), con búsqueda
    /// tolerante. La regla de seguridad que se prueba en todos los casos: la búsqueda flexible sugiere; solo un residuo del
    /// piloto puede pedir la apertura, y nada especial, condicional o aproximado abre una caneca por sí solo.
    /// </summary>
    [TestClass]
    public sealed class CatalogoResiduosTests
    {
        private static ResiduoCatalogo? Primero(string texto) => CatalogoResiduos.Consultar(texto).Principal;

        [TestMethod]
        public void IncluyeTodosLosDelPilotoYLaMatrizDeEco_SinIdentificadoresDuplicados()
        {
            Assert.AreEqual(CatalogoResiduos.Todos.Count, CatalogoResiduos.Todos.Select(r => r.Id).Distinct().Count());
            foreach (ResiduoPrototipo p in CatalogoPrototipo.Todos)
                Assert.IsTrue(CatalogoResiduos.Buscar(p.Id)!.PermiteApertura, p.Id);
            Assert.IsTrue(CatalogoResiduos.Todos.Count >= 200, $"Se esperaban al menos 200 residuos y hay {CatalogoResiduos.Todos.Count}.");
            Assert.IsTrue(CatalogoResiduos.ManejoEspecial.Count >= 40, "Pilas, RAEE, medicamentos, bombillos, peligrosos, cortopunzantes y aceite.");
        }

        [TestMethod]
        public void SoloLosDelPilotoPermitenApertura_YTodosTienenUnaDeLasTresCanecas()
        {
            foreach (ResiduoCatalogo r in CatalogoResiduos.Todos)
            {
                if (r.PermiteApertura)
                {
                    Assert.IsNotNull(r.Prototipo, r.Id);
                    Assert.IsNotNull(r.Caneca, r.Id);
                    Assert.AreEqual(r.Prototipo!.Caneca, r.Caneca, r.Id);
                }
                else Assert.IsNull(r.Prototipo, r.Id);
                if (r.RequiereManejoEspecial || r.DependeDeSuEstado) Assert.IsFalse(r.PermiteApertura, r.Id);
            }
        }

        [TestMethod]
        public void CadaResiduoTieneNombreInstruccionesYFuente_LaImagenEsOpcional()
        {
            foreach (ResiduoCatalogo r in CatalogoResiduos.Todos)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(r.Nombre), r.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(r.Instrucciones), r.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(r.Fuente), r.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(r.Material), r.Id);
            }
            Assert.IsTrue(CatalogoResiduos.Todos.Count(r => r.Imagen is null) > 150, "La mayoría no tiene ilustración y aun así se puede buscar.");
        }

        /// <summary>Botella, botellita, botella de agua, PET, envase de agua y plástico de bebida llevan a una botella PET de la blanca.</summary>
        [TestMethod]
        [DataRow("Botella")]
        [DataRow("botellita")]
        [DataRow("botella de agua")]
        [DataRow("Botella PET")]
        [DataRow("envase de agua")]
        [DataRow("plástico de bebida")]
        [DataRow("BOTELLAS")]
        public void LasVariantesDeBotellaLlevanAUnaBotellaPetDeLaBlanca(string texto)
        {
            ResiduoCatalogo r = Primero(texto)!;
            Assert.IsNotNull(r, texto);
            Assert.AreEqual(CategoriaMaterial.Aprovechable, r.Caneca, texto);
            Assert.AreEqual("PET", r.Material, texto);
            Assert.IsTrue(r.PermiteApertura, texto);
        }

        [TestMethod]
        [DataRow("cascara de banano", "cascara-banano")]
        [DataRow("guineo", "cascara-banano")]
        [DataRow("CÁSCARA DE NARANJA", "cascara-naranja")]
        [DataRow("hojas secas", "hojas-secas")]
        [DataRow("restos de poda", "poda-jardin")]
        [DataRow("cascaras de huevo", "cascaras-alimentos")]
        [DataRow("carton", "carton-limpio")]
        [DataRow("lata de gaseosa", "lata-aluminio")]
        [DataRow("frasco de vidrio", "vidrio")]
        [DataRow("servilleta", "servilletas-usadas")]
        [DataRow("papel higiénico", "papel-higienico")]
        [DataRow("condón", "condon-usado")]
        [DataRow("pañal", "panal-usado")]
        [DataRow("caja de pizza", "papel-contaminado")]
        [DataRow("paquete de papas", "envolturas")]
        public void NombresSinonimosYNombresPopularesDanElResiduoEsperado(string texto, string id) =>
            Assert.AreEqual(id, Primero(texto)?.Id, texto);

        [TestMethod]
        [DataRow("servileta", "servilletas-usadas")]
        [DataRow("votella de agua", "botella-agua")]
        [DataRow("carrton", "carton-limpio")]
        [DataRow("cascra de banano", "cascara-banano")]
        public void ToleraErroresDeEscrituraRazonables_PeroSoloComoSugerencia(string texto, string id)
        {
            ResultadoBusquedaResiduo b = CatalogoResiduos.Consultar(texto);
            Assert.AreEqual(id, b.Principal?.Id, texto);
        }

        [TestMethod]
        public void UnaCoincidenciaSoloAproximadaSeMarcaParaConfirmar()
        {
            ResultadoBusquedaResiduo b = CatalogoResiduos.Consultar("servileta");
            Assert.IsTrue(b.EsAproximada);
            StringAssert.Contains(b.Mensaje, "¿Quisiste decir");
            Assert.IsFalse(CatalogoResiduos.Consultar("servilleta").EsAproximada);
        }

        [TestMethod]
        [DataRow("pila")]
        [DataRow("Pilas AA")]
        [DataRow("batería de celular")]
        [DataRow("medicamento vencido")]
        [DataRow("pastillas")]
        [DataRow("aceite de cocina")]
        [DataRow("jeringa")]
        [DataRow("bombillo")]
        [DataRow("celular")]
        [DataRow("cloro")]
        [DataRow("termómetro de mercurio")]
        public void LoEspecialSeEncuentra_PeroNuncaAbreNingunaCaneca(string texto)
        {
            ResultadoBusquedaResiduo b = CatalogoResiduos.Consultar(texto);
            Assert.IsFalse(b.SinResultados, texto);
            ResiduoCatalogo r = b.Principal!;
            Assert.IsTrue(r.RequiereManejoEspecial, $"{texto} → {r.Nombre} ({r.Ruta})");
            Assert.IsNull(r.Caneca, texto);
            Assert.IsFalse(r.PermiteApertura, texto);
        }

        [TestMethod]
        public void VasoEsAmbiguo_LimpioALaBlancaYSucioALaNegra()
        {
            ResultadoBusquedaResiduo b = CatalogoResiduos.Consultar("vaso");
            var ids = b.Coincidencias.Select(c => c.Residuo.Id).ToList();
            CollectionAssert.IsSubsetOf(new[] { "vasos-plasticos", "vaso-contaminado" }, ids);
            Assert.IsTrue(b.EsAmbigua, "Se pregunta por su estado en vez de asumir una caneca.");
        }

        [TestMethod]
        public void PapelEsAmbiguo_LimpioALaBlancaYUsadoALaNegra()
        {
            ResultadoBusquedaResiduo b = CatalogoResiduos.Consultar("papel");
            Assert.AreEqual(CategoriaMaterial.Aprovechable, b.Principal!.Caneca);
            Assert.IsTrue(b.Alternativas.Any(r => r.Caneca == CategoriaMaterial.NoAprovechable));
        }

        [TestMethod]
        public void BotellaNoEsAmbigua_TodasSusVariantesDepositablesVanALaBlanca() =>
            Assert.IsFalse(CatalogoResiduos.Consultar("botella").Alternativas.Any(r => r.PermiteApertura));

        [TestMethod]
        public void UnDiminutivoAparenteNoConfundeMedicamentosConComida()
        {
            Assert.AreEqual(RutaDisposicion.Medicamentos, Primero("pastilla")!.Ruta);
            Assert.AreEqual("colilla", Primero("colilla")!.Id);
        }

        [TestMethod]
        public void LoQueDependeDeSuEstadoAdvierteYNoAbre()
        {
            ResiduoCatalogo r = CatalogoResiduos.Buscar("obj-cream_jar")!;
            Assert.IsTrue(r.DependeDeSuEstado);
            Assert.IsFalse(r.PermiteApertura);
            Assert.IsNull(r.Caneca);
        }

        [TestMethod]
        [DataRow("helicóptero")]
        [DataRow("xyzzy")]
        [DataRow("   ")]
        public void SinResultadosNoInventaUnaClasificacion(string texto)
        {
            ResultadoBusquedaResiduo b = CatalogoResiduos.Consultar(texto);
            Assert.IsTrue(b.SinResultados, texto);
            Assert.IsNull(b.Principal);
        }

        [TestMethod]
        public void LaPalabraAMedioEscribirSugiere()
        {
            var sugerencias = CatalogoResiduos.Sugerir("bote");
            Assert.IsTrue(sugerencias.Count > 0);
            StringAssert.Contains(CatalogoPrototipo.Normalizar(sugerencias[0].Nombre), "botella");
            Assert.IsTrue(sugerencias.Count(r => CatalogoPrototipo.Normalizar(r.Nombre + " " + string.Join(" ", r.Sinonimos)).Contains("botel")) >= 3);
            Assert.AreEqual(0, CatalogoResiduos.Sugerir("b").Count, "Una letra no sugiere nada.");
        }

        [TestMethod]
        public void LosDelPilotoVanPrimeroCuandoCompitenConLaOrientacion()
        {
            Assert.IsTrue(Primero("lata")!.PermiteApertura);
            Assert.IsTrue(Primero("icopor")!.PermiteApertura);
            Assert.IsTrue(Primero("papel")!.PermiteApertura);
        }

        [TestMethod]
        public void LasTresCanecasTienenResiduosDepositables()
        {
            foreach (CategoriaMaterial c in new[] { CategoriaMaterial.OrganicoAprovechable, CategoriaMaterial.Aprovechable, CategoriaMaterial.NoAprovechable })
            {
                Assert.IsTrue(CatalogoResiduos.DepositablesDe(c).Count >= 12, c.ToString());
                Assert.IsTrue(CatalogoResiduos.DeCaneca(c).Count > CatalogoResiduos.DepositablesDe(c).Count, c.ToString());
            }
        }

        /// <summary>
        /// Coherencia escritorio ↔ Telegram: el bot clasifica el texto con la tabla MATERIAL de Oracle (la caneca sale de la
        /// categoría del material) y el escritorio registra cada depósito con un código de esa tabla. Se leen los scripts SQL
        /// del repositorio: cada código del piloto debe existir allí con la categoría de su caneca (1 blanca, 2 verde, 3 negra).
        /// </summary>
        [TestMethod]
        public void CadaResiduoDepositableCoincideConLaCategoriaDeSuMaterialEnLosScriptsDeOracle()
        {
            var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
            while (dir is not null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "EcoAccess.P3.sln"))) dir = dir.Parent;
            Assert.IsNotNull(dir);
            string datos = System.IO.Path.Combine(dir!.FullName, "database", "03_datos_prueba");
            var categorias = new System.Collections.Generic.Dictionary<string, int>();
            foreach (string archivo in new[] { "01_catalogos.sql", "03_catalogo_ambiental_piloto.sql" })
            {
                string sql = System.IO.File.ReadAllText(System.IO.Path.Combine(datos, archivo));
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(sql,
                             @"(?:agregar\(|SELECT\s+)'([A-Z_]+)'(?:\s+AS\s+codigo)?\s*,\s*'[^']*'(?:\s+AS\s+nombre)?\s*,\s*([123])"))
                    categorias[m.Groups[1].Value] = int.Parse(m.Groups[2].Value);
            }
            Assert.IsTrue(categorias.Count >= 15, $"Solo se leyeron {categorias.Count} materiales de los scripts.");
            foreach (ResiduoCatalogo r in CatalogoResiduos.Todos.Where(x => x.PermiteApertura))
            {
                Assert.IsTrue(categorias.TryGetValue(r.Material, out int categoria), $"{r.Id}: el material {r.Material} no está en los scripts de Oracle.");
                int esperada = r.Caneca switch { CategoriaMaterial.Aprovechable => 1, CategoriaMaterial.OrganicoAprovechable => 2, _ => 3 };
                Assert.AreEqual(esperada, categoria, $"{r.Id}: {r.Material} tiene categoría {categoria} en Oracle y va a la {r.Caneca}.");
            }
        }

        [TestMethod]
        [DataRow("botellita", "botell")]
        [DataRow("botellas", "botell")]
        [DataRow("limones", "limon")]
        [DataRow("hojita", "hoj")]
        [DataRow("pet", "pet")]
        public void LaRaizQuitaDiminutivoPluralYVocalFinal(string palabra, string raiz) =>
            Assert.AreEqual(raiz, CatalogoResiduos.Raiz(palabra));

        [TestMethod]
        public void LaDistanciaCuentaTransposiciones()
        {
            Assert.AreEqual(1, CatalogoResiduos.Distancia("servileta", "servilleta"));
            Assert.AreEqual(1, CatalogoResiduos.Distancia("cartno", "carton"));
            Assert.IsFalse(CatalogoResiduos.Parecido("pila", "pala"), "Las palabras cortas deben coincidir exactamente.");
        }
    }
}
