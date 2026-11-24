using System;
using System.IO;
using System.Linq;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Vision35;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica.Vision35
{
    [TestClass]
    public sealed class CatalogoPrioritario35Tests
    {
        [TestMethod]
        public void CatalogoTieneExactamente35Materiales_10Verdes_13Blancos_12Negros()
        {
            Assert.AreEqual(35, CatalogoPrioritario35.Todos.Count);
            Assert.AreEqual(10, CatalogoPrioritario35.DeCaneca(CategoriaMaterial.OrganicoAprovechable).Count);
            Assert.AreEqual(13, CatalogoPrioritario35.DeCaneca(CategoriaMaterial.Aprovechable).Count);
            Assert.AreEqual(12, CatalogoPrioritario35.DeCaneca(CategoriaMaterial.NoAprovechable).Count);
            Assert.AreEqual(35, CatalogoPrioritario35.Todos.Select(m => m.Codigo).Distinct().Count());
        }

        [TestMethod]
        [DataRow("V01", "VERDE")]
        [DataRow("V10", "VERDE")]
        [DataRow("B01", "BLANCA")]
        [DataRow("B03", "BLANCA")]
        [DataRow("B13", "BLANCA")]
        [DataRow("N01", "NEGRA")]
        [DataRow("N06", "NEGRA")]
        [DataRow("N10", "NEGRA")]
        [DataRow("N11", "NEGRA")]
        [DataRow("N12", "NEGRA")]
        public void CadaCodigoPrioritarioExisteConSuCanecaOficial(string codigo, string canecaEsperada)
        {
            MaterialPrioritario35? m = CatalogoPrioritario35.BuscarPorCodigo(codigo);
            Assert.IsNotNull(m, codigo);
            Assert.AreEqual(canecaEsperada, m!.NombreCaneca);
            Assert.IsFalse(string.IsNullOrWhiteSpace(m.CodigoOracle));
        }

        [TestMethod]
        public void ResiduosSanitariosQuedanMarcadosParaSimulacionSeguraEnStand()
        {
            foreach (string cod in new[] { "N02", "N05", "N11", "N12" })
            {
                MaterialPrioritario35? m = CatalogoPrioritario35.BuscarPorCodigo(cod);
                Assert.IsNotNull(m);
                Assert.IsTrue(m!.EsSanitarioSimuladoEnStand, $"{cod} debe marcarse para representación segura en stand.");
            }
        }

        [TestMethod]
        [DataRow("cáscara de aguacate", "V04")]
        [DataRow("tapa plástica aprovechable", "B03")]
        [DataRow("película plástica limpia", "B13")]
        [DataRow("pitillo usado", "N06")]
        [DataRow("chicle usado", "N10")]
        [DataRow("toallita húmeda usada", "N11")]
        [DataRow("tapabocas desechable usado", "N12")]
        public void BusquedaEscritaEncuentraMaterialYSeDistingueDeDeteccionVisual(string texto, string codigoEsperado)
        {
            ResultadoBusquedaPrioritaria res = CatalogoPrioritario35.BuscarPorTexto(texto);
            Assert.AreEqual(TipoResultadoBusquedaTexto.Encontrado, res.Tipo);
            Assert.IsTrue(res.EsBusquedaTextual);
            Assert.AreEqual(codigoEsperado, res.Materiales.First().Codigo);
        }

        [TestMethod]
        public void CubetaPlasticaParaHieloReutilizableNoSeClasificaComoBasura()
        {
            ResultadoBusquedaPrioritaria res = CatalogoPrioritario35.BuscarPorTexto("cubeta plástica para hielo");
            Assert.AreEqual(TipoResultadoBusquedaTexto.ReutilizableNoResiduo, res.Tipo);
            Assert.AreEqual(0, res.Materiales.Count);
        }

        [TestMethod]
        public void ResiduoPeligrosoEsRechazadoComoFueraDeCatalogo()
        {
            ResultadoBusquedaPrioritaria res = CatalogoPrioritario35.BuscarPorTexto("pila alcalina AA");
            Assert.AreEqual(TipoResultadoBusquedaTexto.FueraDeCatalogoEspecial, res.Tipo);
            Assert.AreEqual(0, res.Materiales.Count);
        }

        [TestMethod]
        public void ExportacionEImportacionJsonConservanLos35MaterialesYRechazanDuplicados()
        {
            string json = CatalogoPrioritario35.ExportarJson();
            DocumentoCatalogo35 doc = CatalogoPrioritario35.CargarDesdeJson(json);
            Assert.AreEqual(CatalogoPrioritario35.Version, doc.Version);
            Assert.AreEqual(35, doc.Materiales.Count);

            string duplicado = json.Replace("\"V02\"", "\"V01\"");
            Assert.ThrowsExactly<InvalidDataException>(() => CatalogoPrioritario35.CargarDesdeJson(duplicado));
        }
    }
}
