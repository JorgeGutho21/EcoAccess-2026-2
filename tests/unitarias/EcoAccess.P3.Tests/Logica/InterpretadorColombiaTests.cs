using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// Nombres reales de LFM3-VL en la evaluación de productos de Colombia y residuos comunes (Wikimedia Commons, 10/11/2026):
    /// el modelo nombraba bien y el intérprete elegía otra clave. Incluye casos que no deben cambiar.
    /// </summary>
    [TestClass]
    public class InterpretadorColombiaTests
    {
        [TestMethod]
        [DataRow("Egg carton.", "cardboard")]
        [DataRow("Egg cartons stacked.", "cardboard")]
        [DataRow("Egg carton empty.", "cardboard")]
        [DataRow("Cardboard tray.", "cardboard")]
        [DataRow("Battery pile heap.", "battery")]
        [DataRow("Batteries pile.", "battery")]
        [DataRow("Battery box.", "battery")]
        [DataRow("Broken eggshell.", "food_scraps")]
        [DataRow("Egg shell fragment.", "food_scraps")]
        [DataRow("Eggshells and soil.", "food_scraps")]
        [DataRow("Chip bag.", "snack_wrapper")]
        [DataRow("Chips bag", "snack_wrapper")]
        [DataRow("Medical mask", "face_mask")]
        [DataRow("Disposable face mask.", "face_mask")]
        [DataRow("Blue surgical mask.", "face_mask")]
        [DataRow("Mask.", "face_mask")]
        [DataRow("Tissue paper.", "used_tissue")]
        // Tercera ronda (10/11/2026): el modelo nombró bien y la segunda lectura ofreció «metal».
        [DataRow("Military officer saluting.", "people")]
        [DataRow("Soldiers saluting", "people")]
        [DataRow("Police officer", "people")]
        [DataRow("Fan.", "appliance")]
        [DataRow("Electric fan", "appliance")]
        [DataRow("Ceiling fan", "appliance")]
        [DataRow("Air conditioner.", "appliance")]
        [DataRow("Air conditioning unit", "appliance")]
        [DataRow("AC unit on wall", "appliance")]
        public void ElNombreCorrectoLlegaALaClaveCorrecta(string nombre, string esperada)
        {
            Assert.AreEqual(esperada, InterpretadorNombreObjeto.Clave(nombre, out _));
            Assert.IsNotNull(CatalogoObjetos.Buscar(esperada), "Una clave del intérprete debe existir en el catálogo, o el bot no sabe qué responder.");
        }

        [TestMethod]
        public void UnTapabocasEsSanitarioYNuncaVaALaCanecaAprovechable()
        {
            var tapabocas = CatalogoObjetos.Buscar("face_mask")!;
            Assert.AreEqual(ContextoObjeto.ResiduoSanitario, tapabocas.Contexto);
            Assert.AreNotEqual(ContextoObjeto.ResiduoAprovechable, CatalogoObjetos.Buscar("used_tissue")!.Contexto);
        }

        [TestMethod]
        [DataRow("Milk carton", "drink_carton")]
        [DataRow("Juice carton", "drink_carton")]
        [DataRow("Cardboard box", "cardboard")]
        [DataRow("Plastic tray", "plastic_container")]
        [DataRow("Plastic bag", "plastic_bag")]
        [DataRow("Shopping bag", "plastic_bag")]
        [DataRow("Fried egg", "served_food")]
        [DataRow("Boiled eggs", "served_food")]
        [DataRow("Pile of trash", CatalogoObjetos.VariosObjetos)]
        [DataRow("Beer bottle", "glass_bottle")]
        [DataRow("Computer fan", "electronic_other")]
        [DataRow("Computer fan with wires", "electronic_other")]
        [DataRow("AC adapter", "charger")]
        [DataRow("Metal can", "can")]
        [DataRow("Aluminum foil", "metal_item")]
        public void LoQueYaFuncionabaNoCambia(string nombre, string esperada) =>
            Assert.AreEqual(esperada, InterpretadorNombreObjeto.Clave(nombre, out _));
    }
}
