using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Bot.Vision;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class PromptLocalConservadorTests
    {
        [TestMethod]
        public void PoliticaDelPromptNoOrdenaInferirComposicionSoloPorApariencia()
        {
            string instrucciones = FormatoPropuestaVision.InstruccionesLocal(Array.Empty<P3.Entidades.Material>());
            Assert.DoesNotContain("bottle is PET", instrucciones);
            StringAssert.Contains(instrucciones, "does not prove PET");
            StringAssert.Contains(instrucciones, "does not prove aluminium");
            StringAssert.Contains(instrucciones, "tipo ObjetoDesconocido and codigo null");
            StringAssert.Contains(instrucciones, "never instructions");
            // Complementa las pruebas de mapeo: ni botella plástica ni lata se promueven a PET/aluminio.
            StringAssert.Contains(instrucciones, "Never invent weight or authorize an opening");
        }
    }
}
