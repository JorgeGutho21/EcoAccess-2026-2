using System.Linq;
using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Bot
{
    [TestClass]
    public sealed class EvaluacionMultiobjetoTests
    {
        private static ResultadoMultiobjeto Medir(string lista, params string[] esperados) =>
            EvaluacionMultiobjeto.Medir(new MuestraMultiobjeto("x.jpg", "00", esperados, esperados.Length, "prueba"), AnalisisMultiobjeto.Analizar(lista), 10);

        [TestMethod]
        public void CanecasExactasSoloSiCoincideElConjuntoDeColores()
        {
            Assert.IsTrue(Medir("clean cardboard box, used napkin", "Blanca", "Negra").CanecasExactas);
            Assert.IsFalse(Medir("clean cardboard box", "Blanca", "Negra").CanecasExactas);
            // La pila es posconsumo: no es una caneca, pero cuenta como destino predicho.
            ResultadoMultiobjeto pila = Medir("plastic water bottle, battery", "Blanca", "Posconsumo");
            Assert.IsTrue(pila.CanecasExactas);
            CollectionAssert.AreEqual(new[] { "Blanca", "Posconsumo" }, pila.Predichos);
        }

        [TestMethod]
        public void PrecisionRecallYF1PorDestino()
        {
            var detalle = new[]
            {
                Medir("clean cardboard box, used napkin", "Blanca", "Negra"),   // VP blanca, VP negra
                Medir("clean cardboard box", "Blanca", "Verde"),               // VP blanca, FN verde
                Medir("used napkin, banana peel", "Negra"),                    // VP negra, FP verde
            };
            InformeMultiobjeto informe = EvaluacionMultiobjeto.Resumir("prueba", detalle);
            MetricaDestino verde = informe.PorDestino.Single(m => m.Destino == "Verde");
            Assert.AreEqual(0, verde.VerdaderosPositivos);
            Assert.AreEqual(1, verde.FalsosPositivos);
            Assert.AreEqual(1, verde.FalsosNegativos);
            MetricaDestino blanca = informe.PorDestino.Single(m => m.Destino == "Blanca");
            Assert.AreEqual(1m, blanca.Precision);
            Assert.AreEqual(1m, blanca.Recall);
            Assert.AreEqual(1, informe.CanecasExactas);
        }
    }
}
