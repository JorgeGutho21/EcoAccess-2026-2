using System.Linq;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>Residuos depositables del piloto: los 15 del stand más la ampliación, tres canecas y nada fuera de alcance clasificado.</summary>
    [TestClass]
    public sealed class CatalogoPrototipoTests
    {
        private static readonly string[] Quince =
        {
            "cascara-banano", "cascara-naranja", "hojas-secas", "mitad-kiwi", "cascara-mandarina", "mitad-limon", "botella-agua", "botella-gaseosa",
            "papel-cuaderno", "caja-jugo", "vasos-plasticos", "papel-higienico", "servilletas-usadas", "vaso-contaminado", "plato-icopor",
        };

        /// <summary>Ampliación del 20/11/2026: 13 verdes, 12 blancos y 13 negros. Los 15 originales siguen intactos.</summary>
        [TestMethod]
        public void TieneLosQuinceOriginalesMasLaAmpliacion_TreceVerdesDoceBlancosTreceNegros()
        {
            Assert.AreEqual(38, CatalogoPrototipo.Todos.Count);
            Assert.AreEqual(13, CatalogoPrototipo.DeCaneca(CategoriaMaterial.OrganicoAprovechable).Count);
            Assert.AreEqual(12, CatalogoPrototipo.DeCaneca(CategoriaMaterial.Aprovechable).Count);
            Assert.AreEqual(13, CatalogoPrototipo.DeCaneca(CategoriaMaterial.NoAprovechable).Count);
            foreach (string id in Quince) Assert.IsNotNull(CatalogoPrototipo.Buscar(id), id);
        }

        /// <summary>Solo códigos que existen en los scripts de Oracle con la categoría de su caneca (1 blanca, 2 verde, 3 negra).</summary>
        [TestMethod]
        public void CadaMaterialPerteneceALaCategoriaDeSuCanecaEnLosScriptsDeOracle()
        {
            var blanca = new[] { "PET", "PEAD", "PAPEL", "CARTON", "VIDRIO", "LATA_ALUMINIO", "MULTICAPA", "PLASTICO_RIGIDO" };
            var verde = new[] { "ORGANICO" };
            var negra = new[] { "SERVILLETA_USADA", "PLASTICO_SIN_ID", "ICOPOR_CONTAMINADO", "CONDON", "TAMPON", "TOALLA_HIGIENICA", "PANAL", "COLILLA" };
            foreach (ResiduoPrototipo r in CatalogoPrototipo.Todos)
            {
                string[] validos = r.Caneca switch
                {
                    CategoriaMaterial.Aprovechable => blanca,
                    CategoriaMaterial.OrganicoAprovechable => verde,
                    _ => negra,
                };
                CollectionAssert.Contains(validos, r.CodigoMaterial, r.Id);
            }
        }

        [TestMethod]
        public void SoloUsaLasTresCanecas_SinCuartaCategoriaEspecialOPeligrosa()
        {
            CollectionAssert.AreEquivalent(
                new[] { CategoriaMaterial.OrganicoAprovechable, CategoriaMaterial.Aprovechable, CategoriaMaterial.NoAprovechable },
                CatalogoPrototipo.Todos.Select(r => r.Caneca).Distinct().ToArray());
            Assert.AreEqual(3, CatalogoPrototipo.Canecas.Count);
        }

        [TestMethod]
        public void CadaResiduoTieneIdEstableUnicoYDatosCompletos()
        {
            Assert.AreEqual(CatalogoPrototipo.Todos.Count, CatalogoPrototipo.Todos.Select(r => r.Id).Distinct().Count());
            foreach (string id in Quince) Assert.IsFalse(string.IsNullOrWhiteSpace(CatalogoPrototipo.Buscar(id)!.Imagen), id);
            foreach (ResiduoPrototipo r in CatalogoPrototipo.Todos)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(r.Nombre), r.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(r.Descripcion), r.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(r.Instrucciones), r.Id);
                Assert.IsTrue(r.Sinonimos.Count > 0, r.Id);
                Assert.IsTrue(r.Disponible, r.Id);
            }
        }

        /// <summary>Cada residuo se registra con un material que su caneca ya admite en Oracle (consultado el 20/11/2026).</summary>
        [TestMethod]
        [DataRow("cascara-banano", "ORGANICO")]
        [DataRow("hojas-secas", "ORGANICO")]
        [DataRow("botella-agua", "PET")]
        [DataRow("papel-cuaderno", "PAPEL")]
        [DataRow("caja-jugo", "MULTICAPA")]
        [DataRow("vasos-plasticos", "PLASTICO_RIGIDO")]
        [DataRow("papel-higienico", "SERVILLETA_USADA")]
        [DataRow("vaso-contaminado", "PLASTICO_SIN_ID")]
        [DataRow("plato-icopor", "ICOPOR_CONTAMINADO")]
        public void CadaResiduoUsaElMaterialDeSuCaneca(string id, string codigo) =>
            Assert.AreEqual(codigo, CatalogoPrototipo.Buscar(id)!.CodigoMaterial);

        [TestMethod]
        [DataRow("CÁSCARA DE BANANO", "cascara-banano")]
        [DataRow("cascara de platano", "cascara-banano")]
        [DataRow("limón", "mitad-limon")]
        [DataRow("Kiwi", "mitad-kiwi")]
        [DataRow("hojarasca", "hojas-secas")]
        [DataRow("PET", "botella-agua")]
        [DataRow("botella de agua", "botella-agua")]
        [DataRow("gaseosa", "botella-gaseosa")]
        [DataRow("hoja arrugada", "papel-cuaderno")]
        [DataRow("papel arrugado", "papel-cuaderno")]
        [DataRow("Hit", "caja-jugo")]
        [DataRow("tetrapak", "caja-jugo")]
        [DataRow("servilleta sucia", "servilletas-usadas")]
        [DataRow("papel higiénico", "papel-higienico")]
        [DataRow("icopor", "plato-icopor")]
        [DataRow("vaso sucio", "vaso-contaminado")]
        public void BuscaSinMayusculasNiTildesYConSinonimos(string texto, string esperado)
        {
            BusquedaPrototipo b = CatalogoPrototipo.Consultar(texto);
            Assert.IsFalse(b.FueraDeCatalogo, texto);
            Assert.AreEqual(esperado, b.Residuos.First().Id, texto);
        }

        [TestMethod]
        public void BusquedasAmpliasDevuelvenTodasLasOpcionesRelacionadas()
        {
            CollectionAssert.IsSubsetOf(new[] { "cascara-banano", "cascara-naranja", "cascara-mandarina" },
                CatalogoPrototipo.Consultar("cáscara").Residuos.Select(r => r.Id).ToArray());
            CollectionAssert.IsSubsetOf(new[] { "botella-agua", "botella-gaseosa" },
                CatalogoPrototipo.Consultar("botella").Residuos.Select(r => r.Id).ToArray());
            // «Vaso» muestra el reciclable limpio (blanca) y el contaminado (negra): la persona elige según su estado.
            CollectionAssert.IsSubsetOf(new[] { "vasos-plasticos", "vaso-contaminado" },
                CatalogoPrototipo.Consultar("vaso").Residuos.Select(r => r.Id).ToArray());
        }

        [TestMethod]
        [DataRow("pila")]
        [DataRow("Pilas AA")]
        [DataRow("batería de celular")]
        [DataRow("medicamento vencido")]
        [DataRow("aceite usado")]
        [DataRow("jeringa")]
        public void LoEspecialOPeligrosoNoSeAsignaANingunaCaneca(string texto)
        {
            BusquedaPrototipo b = CatalogoPrototipo.Consultar(texto);
            Assert.IsTrue(b.FueraDeCatalogo, texto);
            Assert.IsEmpty(b.Residuos, "No se clasifica falsamente en verde, blanca o negra.");
        }

        [TestMethod]
        public void TextoVacioMuestraTodosLosDepositables_YDesconocidoNoInventaResultado()
        {
            Assert.AreEqual(CatalogoPrototipo.Todos.Count, CatalogoPrototipo.Consultar("  ").Residuos.Count);
            BusquedaPrototipo nada = CatalogoPrototipo.Consultar("helicóptero");
            Assert.IsEmpty(nada.Residuos);
            Assert.IsFalse(nada.FueraDeCatalogo);
        }

        /// <summary>Las ilustraciones van incrustadas desde assets/imagenes: si falta una, la tarjeta quedaría sin imagen.</summary>
        [TestMethod]
        public void CadaResiduoTieneSuIlustracionEnElRepositorio()
        {
            var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
            while (dir is not null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "EcoAccess.P3.sln"))) dir = dir.Parent;
            Assert.IsNotNull(dir, "No se encontró la raíz del repositorio.");
            foreach (ResiduoPrototipo r in CatalogoPrototipo.Todos.Where(r => r.Imagen.Length > 0))
            {
                string ruta = System.IO.Path.Combine(dir!.FullName, "assets", "imagenes", "residuos", r.Imagen.Replace('.', System.IO.Path.DirectorySeparatorChar) + ".jpg");
                Assert.IsTrue(System.IO.File.Exists(ruta), $"Falta la ilustración de {r.Id}: {ruta}");
            }
        }

        [TestMethod]
        public void NormalizarQuitaTildesSignosYEspaciosRepetidos() =>
            Assert.AreEqual("cascara de limon", CatalogoPrototipo.Normalizar("  ¡Cáscara   de LIMÓN! "));
    }
}
