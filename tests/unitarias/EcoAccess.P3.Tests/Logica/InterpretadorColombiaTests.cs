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
        public void ElNombreCorrectoLlegaALaClaveCorrecta(string nombre, string esperada) =>
            Assert.AreEqual(esperada, InterpretadorNombreObjeto.Clave(nombre, out _));

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
        public void LoQueYaFuncionabaNoCambia(string nombre, string esperada) =>
            Assert.AreEqual(esperada, InterpretadorNombreObjeto.Clave(nombre, out _));
    }
}
