using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// Fotografías de las carpetas CANECA BLANCA, VERDE y NEGRA integradas al catálogo: láminas recortadas panel por
    /// panel, sin guías completas, sin duplicados, sin tarjetas vacías y con contadores calculados del catálogo real.
    /// </summary>
    [TestClass]
    public sealed class CatalogoFotosTests
    {
        private static readonly CategoriaMaterial[] Tres = { CategoriaMaterial.OrganicoAprovechable, CategoriaMaterial.Aprovechable, CategoriaMaterial.NoAprovechable };

        private static string CanecaDe(CategoriaMaterial c) => c switch
        {
            CategoriaMaterial.OrganicoAprovechable => CatalogoPrototipo.Verde,
            CategoriaMaterial.Aprovechable => CatalogoPrototipo.Blanca,
            _ => CatalogoPrototipo.Negra,
        };

        private static string Raiz()
        {
            string? dir = AppDomain.CurrentDomain.BaseDirectory;
            while (dir is not null && !Directory.Exists(Path.Combine(dir, "assets", "imagenes"))) dir = Path.GetDirectoryName(dir);
            if (dir is null) Assert.Inconclusive("No se encontró la carpeta assets del repositorio.");
            return dir!;
        }

        private static string Archivo(string clave) => Path.Combine(Raiz(), "assets", "imagenes", "residuos", clave.Replace('.', Path.DirectorySeparatorChar) + ".jpg");

        /// <summary>Filas de recortes_laminas.csv (sin comillas en esos campos: nombres de residuo y números).</summary>
        private static List<Dictionary<string, string>> Recortes()
        {
            string ruta = Path.Combine(Raiz(), "docs", "evidencias", "catalogo_fotos", "recortes_laminas.csv");
            string[] lineas = File.ReadAllLines(ruta);
            string[] cabecera = lineas[0].TrimStart('﻿').Split(',');
            var filas = new List<Dictionary<string, string>>();
            foreach (string linea in lineas.Skip(1).Where(l => l.Length > 0))
            {
                // La lámina puede llevar comas entre comillas: se toman las columnas desde la derecha.
                string[] partes = linea.Split(',');
                var fila = new Dictionary<string, string>();
                int extra = partes.Length - cabecera.Length;
                for (int i = 0; i < cabecera.Length; i++)
                    fila[cabecera[i]] = i < 2 ? partes[i] : partes[i + extra];
                filas.Add(fila);
            }
            return filas;
        }

        [TestMethod]
        public void CadaFotoPerteneceAUnResiduoDelCatalogoYASuMismaCaneca()
        {
            Assert.AreEqual(112, CatalogoFotos.Total, "Fotos integradas según docs/evidencias/catalogo_fotos/inventario_fotos.csv y recortes_laminas.csv.");
            foreach (var (residuo, caneca, clave) in CatalogoFotos.Todas)
            {
                var r = CatalogoPrototipo.Buscar(residuo);
                Assert.IsNotNull(r, residuo);
                Assert.AreEqual(CanecaDe(r!.Caneca), caneca, clave);
                StringAssert.StartsWith(clave, caneca + ".fotos." + residuo + "-");
                Assert.IsFalse(clave.Contains(':') || clave.Contains('\\') || clave.Contains('/'), "Sin rutas del equipo: " + clave);
            }
            Assert.AreEqual(CatalogoFotos.Total, CatalogoFotos.Todas.Select(f => f.Clave).Distinct().Count(), "Cada clave es única.");
        }

        [TestMethod]
        public void LosResiduosSinIlustracionTomanSuPrimeraFotoYGuardanSuGaleria()
        {
            var carton = CatalogoPrototipo.Buscar("carton-limpio")!;
            Assert.AreEqual(CatalogoFotos.De("carton-limpio")[0], carton.Imagen);
            Assert.AreEqual(3, CatalogoPrototipo.Imagenes(carton).Count, "Dos fotos y el panel «caja de cartón corrugado».");
            var banano = CatalogoPrototipo.Buscar("cascara-banano")!;
            Assert.AreEqual("verde.banano", banano.Imagen);
            CollectionAssert.AreEqual(new[] { "verde.banano" }.Concat(CatalogoFotos.De("cascara-banano")).ToList(), CatalogoPrototipo.Imagenes(banano).ToList());
            Assert.AreEqual(string.Empty, CatalogoPrototipo.Buscar("panal-usado")!.Imagen, "Sin foto: queda pendiente de recurso gráfico.");
        }

        [TestMethod]
        public void CadaClaveTieneSuArchivoCuadradoSinDeformarYSinRecursosDeGuias()
        {
            var claves = CatalogoFotos.Todas.Select(f => f.Clave).Concat(Tres.SelectMany(c => CatalogoFotos.Pendientes(c).Select(p => p.Clave)));
            foreach (string clave in claves)
            {
                string archivo = Archivo(clave);
                Assert.IsTrue(File.Exists(archivo), clave);
                using var imagen = Image.FromFile(archivo);
                Assert.AreEqual(360, imagen.Width, clave);
                Assert.AreEqual(imagen.Width, imagen.Height, "El recorte se centra en un cuadrado sin estirar: " + clave);
            }
            foreach (string caneca in new[] { "verde", "blanca", "negra" })
                Assert.IsFalse(Directory.Exists(Path.Combine(Raiz(), "assets", "imagenes", "residuos", caneca, "guias")), "Las láminas completas no se publican: " + caneca);
        }

        [TestMethod]
        public void LaGaleriaDeCadaCanecaSoloTieneFotosDeResiduosSinRepetir()
        {
            foreach (CategoriaMaterial c in Tres)
            {
                var fotos = CatalogoFotos.DeCaneca(c);
                Assert.IsTrue(fotos.Count > 0, c.ToString());
                Assert.AreEqual(fotos.Count, fotos.Select(f => f.Clave).Distinct().Count(), "Sin fotos repetidas: " + c);
                foreach (FotoCaneca f in fotos)
                {
                    Assert.AreEqual(TipoFoto.Residuo, f.Tipo, f.Clave);
                    Assert.IsNotNull(f.ResiduoId, f.Clave);
                    Assert.AreNotEqual("Guía visual", f.Titulo, "Ninguna lámina aparece como residuo.");
                    Assert.AreEqual(c, CatalogoPrototipo.Buscar(f.ResiduoId)!.Caneca, f.Clave);
                }
            }
            Assert.IsFalse(CatalogoPrototipo.Todos.Any(r => CatalogoPrototipo.Normalizar(r.Nombre).Contains("guia visual")));
        }

        [TestMethod]
        public void LasLaminasSeRecortanEnTodosSusPanelesConElResiduoDeSuEtiqueta()
        {
            var recortes = Recortes();
            Assert.AreEqual(27, recortes.Count, "10 paneles de papel y cartón, 11 de compostables y 6 de orgánicos.");
            Assert.IsTrue(recortes.All(r => r["estado"] == "integrada"), "Ningún panel rechazado ni repetido.");
            var papel = recortes.Where(r => r["carpeta"] == "CANECA BLANCA").Select(r => r["residuo"]).ToList();
            CollectionAssert.AreEqual(new[] { "periodico-usado", "revista-usada", "folletos", "cartulina-limpia", "carton-limpio",
                "caja-cereal", "cubeta-huevos", "tubo-carton", "caja-jugo", "caja-leche" }, papel);
            foreach (var r in recortes)
            {
                Assert.IsNotNull(CatalogoPrototipo.Buscar(r["residuo"]), r["residuo"]);
                int x0 = int.Parse(r["x0"]), y0 = int.Parse(r["y0"]), x1 = int.Parse(r["x1"]), y1 = int.Parse(r["y1"]);
                Assert.IsTrue(x1 > x0 && y1 > y0, "Coordenadas registradas: " + r["recurso"]);
                Assert.IsTrue(File.Exists(Path.Combine(Raiz(), r["recurso"].Replace('/', Path.DirectorySeparatorChar))), r["recurso"]);
            }
            // Los paneles de una misma fila no se superponen (un recorte no contiene dos productos).
            foreach (var lamina in recortes.GroupBy(r => r["lamina"]))
            {
                var cajas = lamina.Select(r => new Rectangle(int.Parse(r["x0"]), int.Parse(r["y0"]), int.Parse(r["x1"]) - int.Parse(r["x0"]), int.Parse(r["y1"]) - int.Parse(r["y0"]))).ToList();
                for (int i = 0; i < cajas.Count; i++)
                    for (int j = i + 1; j < cajas.Count; j++)
                        Assert.IsFalse(cajas[i].IntersectsWith(cajas[j]), $"{lamina.Key}: paneles {i + 1} y {j + 1} se superponen.");
            }
        }

        [TestMethod]
        public void ElInventarioCuentaLasCarpetasOriginales()
        {
            var verde = CatalogoFotos.Inventario(CategoriaMaterial.OrganicoAprovechable);
            var blanca = CatalogoFotos.Inventario(CategoriaMaterial.Aprovechable);
            var negra = CatalogoFotos.Inventario(CategoriaMaterial.NoAprovechable);
            Assert.AreEqual(101, verde.Originales + blanca.Originales + negra.Originales);
            Assert.AreEqual((2, 17, 17), (verde.Laminas, verde.Paneles, verde.Recortes));
            Assert.AreEqual((1, 10, 10), (blanca.Laminas, blanca.Paneles, blanca.Recortes));
            Assert.AreEqual(1, blanca.Duplicadas, "El frasco de vidrio repetido byte a byte.");
            Assert.AreEqual(0, verde.Invalidas + blanca.Invalidas + negra.Invalidas);
            Assert.AreEqual(12, blanca.Pendientes + negra.Pendientes + verde.Pendientes);
            // Cada archivo original termina en exactamente un estado.
            foreach (var i in new[] { verde, blanca, negra })
                Assert.AreEqual(i.Originales, i.Laminas + i.Individuales + i.Duplicadas + i.Invalidas + i.Pendientes, i.Caneca);
        }

        [TestMethod]
        public void LosContadoresSonResiduosUnicosConImagenYCoincidenConLasTarjetas()
        {
            var total = CatalogoFotos.ContarTodo();
            var porCaneca = total.Canecas.ToDictionary(c => c.Caneca);
            Assert.AreEqual(21, porCaneca[CategoriaMaterial.OrganicoAprovechable].ConImagen);
            Assert.AreEqual(18, porCaneca[CategoriaMaterial.Aprovechable].ConImagen);
            Assert.AreEqual(10, porCaneca[CategoriaMaterial.NoAprovechable].ConImagen);
            Assert.AreEqual(54, total.ResiduosUnicos);
            Assert.AreEqual(49, total.ConImagen);
            Assert.AreEqual(5, total.SinImagen);
            Assert.AreEqual(total.ConImagen, total.Canecas.Sum(c => c.ConImagen));
            Assert.AreEqual(CatalogoFotos.Total + 15, total.Fotos, "Fotos de las galerías: las 112 integradas más las 15 ilustraciones del stand.");
            foreach (CategoriaMaterial c in Tres)
            {
                var visibles = CatalogoPrototipo.VisiblesDe(c);
                Assert.AreEqual(porCaneca[c].ConImagen, visibles.Count, "Tarjetas = contador: " + c);
                Assert.AreEqual(visibles.Count, visibles.Select(r => r.Id).Distinct().Count());
                Assert.AreEqual(visibles.Count, visibles.Select(r => CatalogoPrototipo.Normalizar(r.Nombre)).Distinct().Count(), "Sin nombres repetidos: " + c);
                Assert.IsTrue(visibles.All(CatalogoPrototipo.TieneImagen), "Ninguna tarjeta vacía: " + c);
                Assert.IsTrue(visibles.All(r => File.Exists(Archivo(r.Imagen))), "Toda imagen visible existe: " + c);
                CollectionAssert.AreEqual(visibles.OrderBy(r => CatalogoPrototipo.Normalizar(r.Nombre), StringComparer.Ordinal).ToList(), visibles.ToList(), "Orden estable por nombre.");
                Assert.AreEqual(porCaneca[c].Residuos, porCaneca[c].ConImagen + porCaneca[c].SinImagen);
            }
        }

        [TestMethod]
        public void LosNuevosResiduosUsanCodigosDeOracleExistentesYSusPuntos()
        {
            var esperados = new Dictionary<string, (string Codigo, int Puntos)>
            {
                ["periodico-usado"] = ("PAPEL", 5), ["revista-usada"] = ("PAPEL", 5), ["folletos"] = ("PAPEL", 5),
                ["cartulina-limpia"] = ("CARTON", 7), ["caja-cereal"] = ("CARTON", 7), ["cubeta-huevos"] = ("CARTON", 7), ["tubo-carton"] = ("CARTON", 7),
                ["cascara-limon"] = ("ORGANICO", 2), ["cascara-aguacate"] = ("ORGANICO", 2), ["fruta-danada"] = ("ORGANICO", 2),
                ["cascara-papa"] = ("ORGANICO", 2), ["cascara-yuca"] = ("ORGANICO", 2), ["cascara-platano-verde"] = ("ORGANICO", 2),
                ["cascara-zanahoria"] = ("ORGANICO", 2), ["restos-tomate"] = ("ORGANICO", 2), ["hojas-lechuga"] = ("ORGANICO", 2),
            };
            foreach (var (id, (codigo, puntos)) in esperados.Select(p => (p.Key, p.Value)))
            {
                var r = CatalogoPrototipo.Buscar(id);
                Assert.IsNotNull(r, id);
                Assert.AreEqual(codigo, r!.CodigoMaterial, id);
                Assert.AreEqual(puntos, MatrizEcopuntosPropuesta.DeResiduo(r), id);
                Assert.IsNotNull(CatalogoResiduos.Buscar(id), "El depósito encuentra el residuo: " + id);
            }
            Assert.IsTrue(CatalogoPrototipo.VisiblesDe(CategoriaMaterial.NoAprovechable).All(r => MatrizEcopuntosPropuesta.DeResiduo(r) == 0), "La negra da 0.");
        }

        [DataTestMethod]
        [DataRow(4, 1600, 4, 1)]
        [DataRow(7, 1600, 5, 2)]
        [DataRow(10, 1600, 5, 2)]
        [DataRow(13, 1600, 5, 3)]
        [DataRow(20, 1600, 5, 4)]
        [DataRow(13, 700, 3, 5)]
        [DataRow(13, 300, 1, 13)]
        [DataRow(1, 1600, 1, 1)]
        public void LaRejillaNoReservaCasillasVacias(int tarjetas, int ancho, int columnas, int filas)
        {
            int c = DistribucionRejilla.Columnas(tarjetas, ancho, 210, 16, 5);
            Assert.AreEqual(columnas, c);
            Assert.AreEqual(filas, DistribucionRejilla.Filas(tarjetas, c));
            Assert.IsTrue(c * DistribucionRejilla.Filas(tarjetas, c) - tarjetas < c, "Como mucho una fila incompleta.");
        }

        [TestMethod]
        public void UnaRejillaVaciaNoOcupaAlto()
        {
            Assert.AreEqual(0, DistribucionRejilla.Filas(0, 4));
            Assert.AreEqual(0, DistribucionRejilla.Alto(0, 4, 300, 16));
            Assert.AreEqual(2 * 300 + 16, DistribucionRejilla.Alto(7, 4, 300, 16));
        }
    }
}
