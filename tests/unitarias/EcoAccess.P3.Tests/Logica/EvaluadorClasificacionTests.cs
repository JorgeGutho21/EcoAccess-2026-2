using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{

    /// <summary>Comprueba solo la aritmética del evaluador con conteos hechos a mano; no mide ningún clasificador.</summary>
    [TestClass]
    public sealed class EvaluadorClasificacionTests
    {
        [TestMethod]
        public void CalculaExactitudAbstencionYMetricasPorEtiqueta()
        {
            MuestraEvaluacion[] muestras =
            new MuestraEvaluacion[]
    {
        new("PET", "PET"),
        new("PET", "PET"),
        new("PET", "VIDRIO"),
        new("VIDRIO", "VIDRIO"),
        new("PILA", "Incierta"),
        new("SinResiduo", "SinResiduo"),
        new("PILA", "ProveedorNoDisponible"),
        new("ORGANICO", "PET")
    };

            InformeEvaluacion informe = EvaluadorClasificacion.Evaluar(muestras);

            Assert.AreEqual(8, informe.Total);
            Assert.AreEqual(4, informe.Aciertos);
            Assert.AreEqual(0.5m, informe.Exactitud);
            Assert.AreEqual(2, informe.Abstenciones);
            Assert.AreEqual(0.25m, informe.TasaAbstencion);

            MetricaEtiqueta pet = informe.PorEtiqueta.Single(m => m.Etiqueta == "PET");
            Assert.AreEqual((3, 3, 2), (pet.Esperadas, pet.Obtenidas, pet.Aciertos));
            Assert.AreEqual(0.6667m, pet.Precision);
            Assert.AreEqual(0.6667m, pet.Exhaustividad);

            MetricaEtiqueta vidrio = informe.PorEtiqueta.Single(m => m.Etiqueta == "VIDRIO");
            Assert.AreEqual(0.5m, vidrio.Precision);
            Assert.AreEqual(1m, vidrio.Exhaustividad);

            MetricaEtiqueta pila = informe.PorEtiqueta.Single(m => m.Etiqueta == "PILA");
            Assert.IsNull(pila.Precision, "Nunca se obtuvo PILA: la precisión no está definida.");
            Assert.AreEqual(0m, pila.Exhaustividad);

            MetricaEtiqueta incierta = informe.PorEtiqueta.Single(m => m.Etiqueta == "Incierta");
            Assert.IsNull(incierta.Exhaustividad);
            Assert.AreEqual(0m, incierta.Precision);
        }

        [TestMethod]
        public void EtiquetasOrdenadasYSinRepetir()
        {
            InformeEvaluacion informe = EvaluadorClasificacion.Evaluar(new MuestraEvaluacion[] { new("PET", "PET"), new("PET", "LATA_ALUMINIO") });

            CollectionAssert.AreEqual(new[] { "LATA_ALUMINIO", "PET" }, informe.PorEtiqueta.Select(m => m.Etiqueta).ToArray());
        }

        [TestMethod]
        public void RechazaMuestrasVaciasOIncompletas()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => EvaluadorClasificacion.Evaluar(null!));
            Assert.ThrowsExactly<ArgumentException>(() => EvaluadorClasificacion.Evaluar(Array.Empty<MuestraEvaluacion>()));
            Assert.ThrowsExactly<ArgumentException>(() => EvaluadorClasificacion.Evaluar(new MuestraEvaluacion[] { new("PET", " ") }));
            Assert.ThrowsExactly<ArgumentException>(() => EvaluadorClasificacion.Evaluar(new MuestraEvaluacion[] { new("", "PET") }));
            Assert.ThrowsExactly<ArgumentException>(() => EvaluadorClasificacion.Evaluar(new MuestraEvaluacion[] { new("PET", "PET"), null! }));
        }

        [TestMethod]
        public void EtiquetaUsaElCodigoSoloSiSeIdentifico()
        {
            var incierta = new ClasificacionImagen(ResultadoClasificacion.Incierta, null, 0.5m, "simulador", "x");

            Assert.AreEqual("Incierta", EvaluadorClasificacion.Etiqueta(incierta));
            Assert.ThrowsExactly<ArgumentNullException>(() => EvaluadorClasificacion.Etiqueta(null!));
            Assert.ThrowsExactly<ArgumentException>(() => EvaluadorClasificacion.Etiqueta(
                new ClasificacionImagen(ResultadoClasificacion.Identificado, null, 0.9m, "simulador", "x")));
        }
    }
}
