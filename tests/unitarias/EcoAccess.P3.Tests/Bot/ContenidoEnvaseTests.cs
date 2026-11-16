using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Logica.Contratos;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Bot
{
    /// <summary>Comprobación de contenido (sí/no por envase) antes de abrir la blanca: químicos, medicamentos, aerosoles y vasos no abren.</summary>
    [TestClass]
    public sealed class ContenidoEnvaseTests
    {
        private static PropuestaVision Con(string clave, string codigo) =>
            new(TipoPropuestaVision.MaterialIdentificado, codigo, 0.9m, ClaveObjeto: clave);

        [TestMethod]
        [DataRow("plastic_bottle", "PET", "chemical_container")]
        [DataRow("plastic_container", "PLASTICO_SIN_ID", "chemical_container")]
        [DataRow("cardboard", "CARTON", "medicine")]
        [DataRow("paper", "PAPEL", "medicine")]
        [DataRow("can", "LATA_ALUMINIO", "aerosol")]
        [DataRow("glass_bottle", "VIDRIO", "drinking_glass")]
        public void UnSiCambiaLaClaveYQuitaElMaterial(string clave, string codigo, string riesgo)
        {
            Assert.IsNotNull(VocabularioVision.PreguntaContenido(clave));
            PropuestaVision p = VocabularioVision.SegunContenido(Con(clave, codigo), "{\"respuesta\":\"yes\"}");
            Assert.AreEqual(riesgo, p.ClaveObjeto);
            Assert.IsNull(p.CodigoMaterial);
            Assert.IsNull(p.ConfianzaDeclarada);
        }

        [TestMethod]
        public void UnNoConservaLaPropuesta() =>
            Assert.AreEqual("PET", VocabularioVision.SegunContenido(Con("plastic_bottle", "PET"), "{\"respuesta\":\"no\"}").CodigoMaterial);

        [TestMethod]
        [DataRow("{\"respuesta\":\"tal vez\"}")]
        [DataRow("no es json")]
        [DataRow("{}")]
        public void UnaRespuestaInvalidaEsDudaYNoAbre(string json)
        {
            PropuestaVision p = VocabularioVision.SegunContenido(Con("can", "LATA_ALUMINIO"), json);
            Assert.AreEqual(TipoPropuestaVision.Incierta, p.Tipo);
            Assert.IsNull(p.CodigoMaterial);
        }

        /// <summary>Cierre V1: pilas de botón leídas como metal o lata (banco Colombia, 4 de 6) pasan a la ruta de pilas.</summary>
        [TestMethod]
        [DataRow("metal_item", "METAL")]
        [DataRow("can", "LATA_ALUMINIO")]
        [DataRow("metal_lid", "METAL")]
        public void UnaPilaLeidaComoMetalVaALaRutaDePilas(string clave, string codigo)
        {
            Assert.IsTrue(VocabularioVision.NecesitaVerPila(clave));
            PropuestaVision p = VocabularioVision.SegunPila(Con(clave, codigo), "{\"respuesta\":\"yes\"}");
            Assert.AreEqual("battery", p.ClaveObjeto);
            Assert.IsNull(p.CodigoMaterial, "Una pila nunca lleva material de caneca.");
            Assert.AreEqual("LATA_ALUMINIO", VocabularioVision.SegunPila(Con("can", "LATA_ALUMINIO"), "{\"respuesta\":\"no\"}").CodigoMaterial);
            Assert.AreEqual(TipoPropuestaVision.Incierta, VocabularioVision.SegunPila(Con(clave, codigo), "{}").Tipo);
        }

        [TestMethod]
        [DataRow("plastic_bottle")]
        [DataRow("cardboard")]
        [DataRow("battery")]
        public void LaPreguntaDePilaSoloSeHaceAnteLoMetalico(string clave) => Assert.IsFalse(VocabularioVision.NecesitaVerPila(clave));

        [TestMethod]
        public void UnaCuchillaLeidaComoMetalVaACortopunzantes()
        {
            PropuestaVision p = VocabularioVision.SegunCortante(Con("metal_item", "METAL"), "{\"respuesta\":\"yes\"}");
            Assert.AreEqual("disposable_razor", p.ClaveObjeto);
            Assert.IsNull(p.CodigoMaterial);
            Assert.AreEqual("METAL", VocabularioVision.SegunCortante(Con("metal_item", "METAL"), "{\"respuesta\":\"no\"}").CodigoMaterial);
            Assert.IsFalse(VocabularioVision.NecesitaVerCortante("can"));
        }

        /// <summary>Cierre V1: un rollo o un paquete de papel nuevo no es un residuo sanitario y no abre la negra.</summary>
        [TestMethod]
        public void ElPapelSanitarioNuevoNoAbreLaNegra()
        {
            var papel = new PropuestaVision(TipoPropuestaVision.SinResiduo, "SERVILLETA_USADA", 0.9m, ClaveObjeto: "used_tissue");
            Assert.AreSame(papel, VocabularioVision.SegunUsado(papel, "{\"respuesta\":\"yes\"}"));
            PropuestaVision nuevo = VocabularioVision.SegunUsado(papel, "{\"respuesta\":\"no\"}");
            Assert.AreEqual("napkin_clean", nuevo.ClaveObjeto);
            Assert.IsNull(nuevo.CodigoMaterial);
            Assert.AreEqual(TipoPropuestaVision.Incierta, VocabularioVision.SegunUsado(papel, "no es json").Tipo);
            Assert.IsFalse(VocabularioVision.NecesitaVerUsado("plastic_bag"));
        }

        [TestMethod]
        public void LoQueNoPodiaAbrirNoSeConsulta()
        {
            Assert.IsNull(VocabularioVision.PreguntaContenido("used_tissue"));
            var servilleta = new PropuestaVision(TipoPropuestaVision.SinResiduo, null, null, ClaveObjeto: "used_tissue");
            Assert.AreSame(servilleta, VocabularioVision.SegunContenido(servilleta, "{\"respuesta\":\"yes\"}"));
        }
    }
}
