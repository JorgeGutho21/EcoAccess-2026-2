using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Tests.Entidades
{

    [TestClass]
    public sealed class AnalisisImagenTests
    {
        [TestMethod]
        [DataRow("modelo con texto libre")]
        [DataRow("https://sitio/clave")]
        [DataRow("modelo\nsalto")]
        public void TrazaRechazaTextoLibreEnIdentificador(string modelo) =>
            Assert.ThrowsExactly<ArgumentException>(() => new AnalisisImagen("foto:1", 1, "doble", modelo, "no-publicada", 7, null, DateTime.UtcNow));

        [TestMethod]
        public void TrazaDistingueSugerenciaYConfirmacionSinPesoNiCredenciales()
        {
            var fecha = DateTime.UtcNow;
            var a = new AnalisisImagen("foto:1", 1, "doble", "sin-red", "no-publicada", 1, 2, fecha, 3, fecha.AddSeconds(1));
            Assert.AreEqual(2, a.IdMaterialSugerido);
            Assert.AreEqual(3, a.IdMaterialConfirmado);
            Assert.ThrowsExactly<ArgumentException>(() => new AnalisisImagen("foto:2", 1, "doble", "sin-red", "no-publicada", 1, null, fecha, 3));
            Assert.ThrowsExactly<ArgumentException>(() => new AnalisisImagen("foto:2", 1, "doble", "sin-red", "no-publicada", 1, null, fecha, 3, fecha.AddSeconds(-1)));
        }
    }
}
