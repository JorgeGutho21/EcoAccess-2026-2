using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Tests.Entidades
{

    [TestClass]
    public class PuntoInstalacionTests
    {
        [TestMethod]
        public void Constructor_DatosValidos_CreaPuntoNuevoActivo()
        {
            var punto = new PuntoInstalacion(" Bloque A ", " Primer piso, junto a la cafetería ");

            Assert.AreEqual(0, punto.Id);
            Assert.IsTrue(punto.EsNuevo);
            Assert.AreEqual("Bloque A", punto.Nombre);
            Assert.AreEqual("Primer piso, junto a la cafetería", punto.ReferenciaUbicacion);
            Assert.IsTrue(punto.Activo);
        }

        [TestMethod]
        public void Constructor_SinReferencia_LaGuardaComoNull()
        {
            var punto = new PuntoInstalacion("Bloque A", "  ");

            Assert.IsNull(punto.ReferenciaUbicacion);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("A")]
        public void Constructor_NombreVacioOMuyCorto_LanzaArgumentException(string? nombre)
        {
            Assert.ThrowsExactly<ArgumentException>(() => new PuntoInstalacion(nombre!, null));
        }

        [TestMethod]
        public void Constructor_NombreYReferenciaEnElMaximo_LosAcepta()
        {
            string nombre = new('n', PuntoInstalacion.LongitudMaximaNombre);
            string referencia = new('r', PuntoInstalacion.LongitudMaximaReferencia);

            var punto = new PuntoInstalacion(nombre, referencia);

            Assert.AreEqual(nombre, punto.Nombre);
            Assert.AreEqual(referencia, punto.ReferenciaUbicacion);
        }

        [TestMethod]
        public void Constructor_ReferenciaMasLargaQueElMaximo_LanzaArgumentException()
        {
            string larga = new('x', PuntoInstalacion.LongitudMaximaReferencia + 1);

            Assert.ThrowsExactly<ArgumentException>(() => new PuntoInstalacion("Bloque A", larga));
        }

        [TestMethod]
        public void ConstructorReconstruccion_DatosValidos_ConservaIdYEstado()
        {
            var punto = new PuntoInstalacion(2, "Bloque B", null, activo: false);

            Assert.AreEqual(2, punto.Id);
            Assert.IsFalse(punto.Activo);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void ConstructorReconstruccion_IdNoPositivo_LanzaArgumentOutOfRangeException(int id)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PuntoInstalacion(id, "Bloque B", null, true));
        }

        [TestMethod]
        public void DesactivarYActivar_CambianElEstado()
        {
            var punto = new PuntoInstalacion("Bloque A", null);

            punto.Desactivar();
            Assert.IsFalse(punto.Activo);

            punto.Activar();
            Assert.IsTrue(punto.Activo);
        }
    }
}
