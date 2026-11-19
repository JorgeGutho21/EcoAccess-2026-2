using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// Pantalla táctil de la estación (observación del profesor Patiño, 17/11/2026). Los materiales son los habilitados
    /// en Oracle Cloud el 18/11/2026 (código, nombre y categoría tal como están en MATERIAL).
    /// </summary>
    [TestClass]
    public sealed class GuiaEstacionTactilTests
    {
        private static readonly (string Codigo, string Nombre, CategoriaMaterial Categoria, GrupoEstacion Grupo)[] CatalogoReal =
        {
            ("BOLSA_PLASTICA", "Bolsa plástica", CategoriaMaterial.Aprovechable, GrupoEstacion.Plastico),
            ("VIDRIO", "Botella o frasco de vidrio", CategoriaMaterial.Aprovechable, GrupoEstacion.VidrioMetal),
            ("PET", "Botella plástica PET", CategoriaMaterial.Aprovechable, GrupoEstacion.Plastico),
            ("CARTON", "Cartón", CategoriaMaterial.Aprovechable, GrupoEstacion.PapelCarton),
            ("MULTICAPA", "Envase multicapa", CategoriaMaterial.Aprovechable, GrupoEstacion.PapelCarton),
            ("PEAD", "Envase PEAD identificado", CategoriaMaterial.Aprovechable, GrupoEstacion.Plastico),
            ("PLASTICO_RIGIDO", "Envase plástico rígido", CategoriaMaterial.Aprovechable, GrupoEstacion.Plastico),
            ("LATA_ALUMINIO", "Lata de aluminio", CategoriaMaterial.Aprovechable, GrupoEstacion.VidrioMetal),
            ("METAL", "Metal o chatarra menor", CategoriaMaterial.Aprovechable, GrupoEstacion.VidrioMetal),
            ("PAPEL", "Papel de oficina y cuadernos", CategoriaMaterial.Aprovechable, GrupoEstacion.PapelCarton),
            ("PLASTICO_FLEXIBLE", "Plástico flexible", CategoriaMaterial.Aprovechable, GrupoEstacion.Plastico),
            ("RESTOS_ALIMENTOS", "Restos de alimentos", CategoriaMaterial.OrganicoAprovechable, GrupoEstacion.Comida),
            ("ORGANICO", "Restos de comida", CategoriaMaterial.OrganicoAprovechable, GrupoEstacion.Comida),
            ("CHANCLETAS", "Chancletas deterioradas", CategoriaMaterial.NoAprovechable, GrupoEstacion.Otros),
            ("COLILLA", "Colilla de cigarrillo", CategoriaMaterial.NoAprovechable, GrupoEstacion.Otros),
            ("CONDON", "Condón usado doméstico", CategoriaMaterial.NoAprovechable, GrupoEstacion.Otros),
            ("PLASTICO_SIN_ID", "Envase plástico sin identificar", CategoriaMaterial.NoAprovechable, GrupoEstacion.Plastico),
            ("ICOPOR_CONTAMINADO", "Icopor con restos de comida", CategoriaMaterial.NoAprovechable, GrupoEstacion.Plastico),
            ("MADERA", "Madera", CategoriaMaterial.NoAprovechable, GrupoEstacion.Otros),
            ("OBJETO_COMPUESTO", "Objeto compuesto", CategoriaMaterial.NoAprovechable, GrupoEstacion.Otros),
            ("PANAL", "Pañal usado doméstico", CategoriaMaterial.NoAprovechable, GrupoEstacion.Otros),
            ("ROPA", "Ropa deteriorada", CategoriaMaterial.NoAprovechable, GrupoEstacion.Otros),
            ("SERVILLETA_USADA", "Servilleta o papel higiénico usado", CategoriaMaterial.NoAprovechable, GrupoEstacion.PapelCarton),
            ("TAMPON", "Tampón usado doméstico", CategoriaMaterial.NoAprovechable, GrupoEstacion.Otros),
            ("TOALLA_HIGIENICA", "Toalla higiénica usada doméstica", CategoriaMaterial.NoAprovechable, GrupoEstacion.Otros),
            ("ZAPATOS", "Zapatos deteriorados", CategoriaMaterial.NoAprovechable, GrupoEstacion.Otros),
            ("ACEITE", "Aceite usado", CategoriaMaterial.Peligroso, GrupoEstacion.Especiales),
            ("ANALGESICO", "Analgésico o medicamento vencido", CategoriaMaterial.Peligroso, GrupoEstacion.Especiales),
            ("RAEE", "Aparato eléctrico o electrónico", CategoriaMaterial.Peligroso, GrupoEstacion.Especiales),
            ("BATERIA", "Batería usada", CategoriaMaterial.Peligroso, GrupoEstacion.Especiales),
            ("BLISTER", "Blíster de medicamentos", CategoriaMaterial.Peligroso, GrupoEstacion.Especiales),
            ("BOMBILLO", "Bombillo", CategoriaMaterial.Peligroso, GrupoEstacion.Especiales),
            ("CABLE", "Cable eléctrico en desuso", CategoriaMaterial.Peligroso, GrupoEstacion.Especiales),
            ("CARGADOR", "Cargador en desuso", CategoriaMaterial.Peligroso, GrupoEstacion.Especiales),
            ("CORTOPUNZANTE", "Elemento cortopunzante", CategoriaMaterial.Peligroso, GrupoEstacion.Especiales),
            ("ENVASE_CONTAMINADO", "Envase contaminado", CategoriaMaterial.Peligroso, GrupoEstacion.Especiales),
            ("JERINGA", "Jeringa o aguja usada", CategoriaMaterial.Peligroso, GrupoEstacion.Especiales),
            ("MEDICAMENTO", "Medicamento vencido", CategoriaMaterial.Peligroso, GrupoEstacion.Especiales),
            ("PILA", "Pila o batería", CategoriaMaterial.Peligroso, GrupoEstacion.Especiales),
            ("ESPECIAL", "Residuo especial sin identificar", CategoriaMaterial.Peligroso, GrupoEstacion.Especiales),
        };

        private static List<Material> Catalogo() =>
            CatalogoReal.Select((m, i) => new Material(i + 1, m.Codigo, m.Nombre, m.Categoria, null, true)).ToList();

        [TestMethod]
        public void CadaMaterialRealQuedaEnSuGrupo()
        {
            List<Material> catalogo = Catalogo();
            for (int i = 0; i < catalogo.Count; i++)
                Assert.AreEqual(CatalogoReal[i].Grupo, GuiaEstacionTactil.GrupoDe(catalogo[i]), CatalogoReal[i].Nombre);
        }

        [TestMethod]
        public void NingunMaterialSePierdeYNingunGrupoQuedaVacio()
        {
            List<Material> catalogo = Catalogo();
            var porGrupo = GuiaEstacionTactil.Grupos.ToDictionary(g => g, g => GuiaEstacionTactil.MaterialesDe(g, catalogo));
            Assert.AreEqual(catalogo.Count, porGrupo.Values.Sum(l => l.Count), "Cada material aparece en un solo grupo.");
            Assert.IsTrue(porGrupo.Values.All(l => l.Count > 0), "Ningún botón lleva a una pantalla vacía.");
        }

        [TestMethod]
        public void UnMaterialNuevoDesconocidoApareceEnOtros()
        {
            var nuevo = new Material(99, "ESPONJA", "Esponja de cocina", CategoriaMaterial.NoAprovechable, null, true);
            Assert.AreEqual(GrupoEstacion.Otros, GuiaEstacionTactil.GrupoDe(nuevo));
        }

        [TestMethod]
        public void LaCanecaSaleDeLaCategoriaDeOracleYNoDelGrupo()
        {
            List<Material> catalogo = Catalogo();
            Assert.AreEqual(DestinoResiduo.Blanca, GuiaEstacionTactil.Orientar(catalogo.Single(m => m.Codigo == "PAPEL")).Destino);
            Assert.AreEqual(DestinoResiduo.Negra, GuiaEstacionTactil.Orientar(catalogo.Single(m => m.Codigo == "SERVILLETA_USADA")).Destino,
                "Mismo grupo que el papel, pero va en la negra.");
            Assert.AreEqual(DestinoResiduo.Verde, GuiaEstacionTactil.Orientar(catalogo.Single(m => m.Codigo == "ORGANICO")).Destino);
        }

        [TestMethod]
        public void LoEspecialNoVaEnNingunaDeLasTresCanecas()
        {
            foreach (Material especial in Catalogo().Where(m => m.Categoria == CategoriaMaterial.Peligroso))
            {
                OrientacionEstacion destino = GuiaEstacionTactil.Orientar(especial);
                Assert.IsFalse(destino.VaEnCaneca, especial.Nombre);
                Assert.AreEqual(DestinoResiduo.Posconsumo, destino.Destino);
                Assert.AreEqual("No va en estas canecas", destino.Titulo);
            }
        }

        [TestMethod]
        public void LaIndicacionPrefiereLaRutaGuardadaEnOracle()
        {
            var pila = new Material(1, "PILA", "Pila o batería", CategoriaMaterial.Peligroso, null, true,
                rutaSeparacion: "Punto posconsumo de pilas");
            Assert.AreEqual("Punto posconsumo de pilas", GuiaEstacionTactil.Orientar(pila).Indicacion);
        }
    }
}
