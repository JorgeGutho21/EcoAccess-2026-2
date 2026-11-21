using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>Fotografías de las carpetas CANECA BLANCA, VERDE y NEGRA integradas al catálogo del prototipo.</summary>
    [TestClass]
    public sealed class CatalogoFotosTests
    {
        private static string CanecaDe(CategoriaMaterial c) => c switch
        {
            CategoriaMaterial.OrganicoAprovechable => CatalogoPrototipo.Verde,
            CategoriaMaterial.Aprovechable => CatalogoPrototipo.Blanca,
            _ => CatalogoPrototipo.Negra,
        };

        [TestMethod]
        public void CadaFotoPerteneceAUnResiduoDelCatalogoYASuMismaCaneca()
        {
            Assert.AreEqual(85, CatalogoFotos.Total, "Fotos integradas según docs/evidencias/catalogo_fotos/inventario_fotos.csv.");
            foreach (var (residuo, caneca, clave) in CatalogoFotos.Todas)
            {
                var r = CatalogoPrototipo.Buscar(residuo);
                Assert.IsNotNull(r, residuo);
                Assert.AreEqual(CanecaDe(r!.Caneca), caneca, clave);
                StringAssert.StartsWith(clave, caneca + ".fotos." + residuo + "-");
            }
        }

        [TestMethod]
        public void LosResiduosSinIlustracionTomanSuPrimeraFoto()
        {
            var carton = CatalogoPrototipo.Buscar("carton-limpio")!;
            Assert.AreEqual(CatalogoFotos.De("carton-limpio")[0], carton.Imagen);
            Assert.AreEqual(6, CatalogoPrototipo.Imagenes(carton).Count);
            // Los 15 del stand conservan su ilustración y suman sus fotos después, sin repetir.
            var banano = CatalogoPrototipo.Buscar("cascara-banano")!;
            Assert.AreEqual("verde.banano", banano.Imagen);
            CollectionAssert.AreEqual(new[] { "verde.banano" }.Concat(CatalogoFotos.De("cascara-banano")).ToList(), CatalogoPrototipo.Imagenes(banano).ToList());
            Assert.AreEqual(string.Empty, CatalogoPrototipo.Buscar("panal-usado")!.Imagen, "Sin foto: la interfaz usa el icono.");
        }

        [TestMethod]
        public void CadaClaveTieneSuArchivoEnAssets()
        {
            string? dir = AppDomain.CurrentDomain.BaseDirectory;
            while (dir is not null && !Directory.Exists(Path.Combine(dir, "assets", "imagenes"))) dir = Path.GetDirectoryName(dir);
            if (dir is null) Assert.Inconclusive("No se encontró la carpeta assets del repositorio.");
            var claves = CatalogoFotos.Todas.Select(f => f.Clave)
                .Concat(new[] { CategoriaMaterial.Aprovechable, CategoriaMaterial.OrganicoAprovechable, CategoriaMaterial.NoAprovechable }.SelectMany(CatalogoFotos.Guias))
                .Concat(new[] { CategoriaMaterial.Aprovechable, CategoriaMaterial.NoAprovechable }.SelectMany(c => CatalogoFotos.Pendientes(c).Select(p => p.Clave)));
            foreach (string clave in claves)
                Assert.IsTrue(File.Exists(Path.Combine(dir!, "assets", "imagenes", "residuos", clave.Replace('.', Path.DirectorySeparatorChar) + ".jpg")), clave);
            Assert.AreEqual(1, CatalogoFotos.Guias(CategoriaMaterial.Aprovechable).Count);
            Assert.AreEqual(2, CatalogoFotos.Guias(CategoriaMaterial.OrganicoAprovechable).Count);
        }

        [TestMethod]
        public void LaGaleriaDeCadaCanecaMuestraTodasSusFotosSinPerderNinguna()
        {
            // 101 archivos: 85 de residuos + 3 guías + 12 por clasificar + 1 duplicado exacto (no se repite).
            Assert.AreEqual(100, CatalogoFotos.TotalVisibles);
            Assert.AreEqual(27, CatalogoFotos.DeCaneca(CategoriaMaterial.Aprovechable).Count);
            Assert.AreEqual(33, CatalogoFotos.DeCaneca(CategoriaMaterial.OrganicoAprovechable).Count);
            Assert.AreEqual(40, CatalogoFotos.DeCaneca(CategoriaMaterial.NoAprovechable).Count);
            foreach (var c in new[] { CategoriaMaterial.Aprovechable, CategoriaMaterial.OrganicoAprovechable, CategoriaMaterial.NoAprovechable })
                foreach (FotoCaneca f in CatalogoFotos.DeCaneca(c))
                {
                    // Lo que está por clasificar nunca se ofrece como residuo depositable.
                    Assert.AreEqual(f.Tipo == TipoFoto.Residuo, f.ResiduoId is not null, f.Clave);
                    if (f.Tipo == TipoFoto.PorClasificar) Assert.IsFalse(string.IsNullOrWhiteSpace(f.Nota), f.Clave);
                }
        }
    }
}
