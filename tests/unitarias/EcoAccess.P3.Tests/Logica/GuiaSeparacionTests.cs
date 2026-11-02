using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{

    [TestClass]
    public sealed class GuiaSeparacionTests
    {
        [TestMethod]
        [DataRow(CategoriaMaterial.Aprovechable, "blanca")]
        [DataRow(CategoriaMaterial.OrganicoAprovechable, "verde")]
        [DataRow(CategoriaMaterial.NoAprovechable, "negra")]
        [DataRow(CategoriaMaterial.Peligroso, null)]
        public void CadaCategoriaTieneSuColorDeLaResolucion2184(CategoriaMaterial categoria, string? color)
        {
            IndicacionSeparacion indicacion = GuiaSeparacion.Para(categoria);

            Assert.AreEqual(categoria, indicacion.Categoria);
            Assert.AreEqual(color, indicacion.ColorCaneca);
            Assert.IsFalse(Compat.IsNullOrWhiteSpace(indicacion.Indicacion));
        }

        [TestMethod]
        public void TodasCubreCadaCategoriaEnElOrdenDelEnum() =>
            CollectionAssert.AreEqual(Compat.GetValues<CategoriaMaterial>(), GuiaSeparacion.Todas.Select(i => i.Categoria).ToArray());

        [TestMethod]
        public void UnaCategoriaIndefinidaEsUnErrorDeProgramacion() =>
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => GuiaSeparacion.Para((CategoriaMaterial)99));
    }
}
