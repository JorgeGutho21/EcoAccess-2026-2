using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class MapeoObjetoCatalogoLocalTests
    {
        private static readonly Material[] Catalogo = new Material[]
    {
        new(1,"PAPEL","Papel",CategoriaMaterial.Aprovechable,null,true),
        new(2,"CARTON","Cartón",CategoriaMaterial.Aprovechable,null,true),
        new(3,"VIDRIO","Vidrio",CategoriaMaterial.Aprovechable,null,true),
        new(4,"PET","Botella PET",CategoriaMaterial.Aprovechable,null,true)
    };

        [TestMethod]
        [DataRow(ObjetoVisible.Papel, "PAPEL")]
        [DataRow(ObjetoVisible.Carton, "CARTON")]
        [DataRow(ObjetoVisible.EnvaseVidrio, "VIDRIO")]
        public void ObjetoConEquivalenciaExplicita_ConservaConfianzaSinInventarQuimica(ObjetoVisible objeto, string codigo)
        {
            var p = MapeoObjetoCatalogoLocal.Normalizar(new(TipoPropuestaVision.ObjetoDesconocido, null, .7m, Objeto: objeto), Catalogo);
            Assert.AreEqual(codigo, p.CodigoMaterial);
            Assert.AreEqual(.7m, p.ConfianzaDeclarada);
            Assert.AreEqual(TipoPropuestaVision.MaterialIdentificado, p.Tipo);
        }

        [TestMethod]
        [DataRow(ObjetoVisible.BotellaPlastica)]
        [DataRow(ObjetoVisible.Lata)]
        [DataRow(ObjetoVisible.BolsaPlastica)]
        [DataRow(ObjetoVisible.PapelCometa)]
        [DataRow(ObjetoVisible.Caja)]
        public void ObjetoGenericoOEspecial_NoSeConvierteEnMaterial(ObjetoVisible objeto)
        {
            var p = new PropuestaVision(TipoPropuestaVision.ObjetoDesconocido, null, .95m, Objeto: objeto);
            Assert.AreSame(p, MapeoObjetoCatalogoLocal.Normalizar(p, Catalogo));
        }

        [TestMethod]
        [DataRow(TipoPropuestaVision.VariosObjetos)]
        [DataRow(TipoPropuestaVision.Incierta)]
        [DataRow(TipoPropuestaVision.ImagenBorrosa)]
        [DataRow(TipoPropuestaVision.SinResiduo)]
        public void AbstencionesDeSeguridad_NoSePromueven(TipoPropuestaVision tipo)
        {
            var p = new PropuestaVision(tipo, null, .99m, Objeto: ObjetoVisible.Papel);
            Assert.AreSame(p, MapeoObjetoCatalogoLocal.Normalizar(p, Catalogo));
        }

        [TestMethod]
        public void MaterialAusenteDeshabilitadoOCategoriaIncompatible_NoSePropone()
        {
            var p = new PropuestaVision(TipoPropuestaVision.ObjetoDesconocido, null, .95m, Objeto: ObjetoVisible.Papel);
            Assert.AreSame(p, MapeoObjetoCatalogoLocal.Normalizar(p, Array.Empty<Material>()));
            Assert.AreSame(p, MapeoObjetoCatalogoLocal.Normalizar(p, new Material[] { new(1, "PAPEL", "Papel", CategoriaMaterial.Aprovechable, null, false) }));
            Assert.AreSame(p, MapeoObjetoCatalogoLocal.Normalizar(p, new Material[] { new(1, "PAPEL", "Papel", CategoriaMaterial.Peligroso, null, true) }));
        }
    }
}
