using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// Nombres reales de LFM3-VL sobre el conjunto de ajuste TrashNet (no el de evaluación final): el envase metálico gana
    /// sobre la comida de su etiqueta y el papel aluminio sobre la palabra «wrapper».
    /// </summary>
    [TestClass]
    public class InterpretadorLatasTests
    {
        [TestMethod]
        [DataRow("Canned fruit syrup.", "can")]
        [DataRow("Canned food.", "can")]
        [DataRow("Canned food label.", "can")]
        [DataRow("Canned food product.", "can")]
        [DataRow("Canned beverage.", "can")]
        [DataRow("Olive oil can.", "can")]
        [DataRow("Crushed can.", "can")]
        [DataRow("Foil wrapper.", "metal_item")]
        [DataRow("Foil wrapped item.", "metal_item")]
        [DataRow("Aluminum foil tray.", "metal_item")]
        public void EnvasesMetalicosSeReconocenComoMetal(string nombre, string esperada) =>
            Assert.AreEqual(esperada, InterpretadorNombreObjeto.Clave(nombre, out _));

        [TestMethod]
        [DataRow("Plate of rice and chicken", "served_food")]
        [DataRow("Fruit salad bowl", "served_food")]
        [DataRow("Chip packet wrapper", "snack_wrapper")]
        [DataRow("Candy wrapper", "snack_wrapper")]
        public void LaComidaYLosEmpaquesSinMetalNoCambian(string nombre, string esperada) =>
            Assert.AreEqual(esperada, InterpretadorNombreObjeto.Clave(nombre, out _));
    }
}
