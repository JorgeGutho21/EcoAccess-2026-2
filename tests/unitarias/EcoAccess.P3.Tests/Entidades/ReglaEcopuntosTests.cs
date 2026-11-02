using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Tests.Entidades
{

    [TestClass]
    public sealed class ReglaEcopuntosTests
    {
        private static readonly DateTime Inicio = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
        [TestMethod]
        public void VigenciaIncluyeInicioYExcluyeFinal()
        {
            var regla = new ReglaEcopuntos(1, 2, 10, "Tarifa de prueba", Inicio, Inicio.AddHours(1), 3);
            Assert.IsFalse(regla.EstaVigente(Inicio.AddTicks(-1)));
            Assert.IsTrue(regla.EstaVigente(Inicio));
            Assert.IsFalse(regla.EstaVigente(Inicio.AddHours(1)));
        }
        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(1000001)]
        public void NoSeAceptanTarifasSinCreditoValido(int puntos) =>
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ReglaEcopuntos(0, 2, puntos, "Prueba", Inicio, null, 3));
        [TestMethod]
        public void SeExigenFechasUtcYUnIntervaloPositivo()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new ReglaEcopuntos(0, 2, 1, "Prueba", DateTime.SpecifyKind(Inicio, DateTimeKind.Local), null, 3));
            Assert.ThrowsExactly<ArgumentException>(() => new ReglaEcopuntos(0, 2, 1, "Prueba", Inicio, Inicio, 3));
        }
    }
}
