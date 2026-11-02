using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Tests.Entidades
{

    [TestClass]
    public class DepositoTests
    {
        private static readonly DateTime Deteccion = new(2026, 10, 24, 15, 0, 0, DateTimeKind.Utc);

        private static Deposito CrearDeposito() => new(4, 10, 0.25m, null, Deteccion);

        [TestMethod]
        public void Constructor_DatosValidos_CreaSinVerificar()
        {
            Deposito deposito = CrearDeposito();

            Assert.IsTrue(deposito.EsNuevo);
            Assert.AreEqual(0, deposito.IdEvento);
            Assert.AreEqual(0.25m, deposito.PesoMedidoKg);
            Assert.IsNull(deposito.PesoEstimadoKg);
            Assert.IsFalse(deposito.EstaVerificado);
        }

        [TestMethod]
        public void Constructor_SinPesos_SePermite()
        {
            var deposito = new Deposito(4, 10, null, null, Deteccion);

            Assert.IsNull(deposito.PesoMedidoKg);
            Assert.IsNull(deposito.PesoEstimadoKg);
        }

        [TestMethod]
        [DataRow(0.0)]
        [DataRow(-1.0)]
        [DataRow(1000.0)]
        [DataRow(0.0005)]
        public void Constructor_PesoInvalido_Lanza(double peso)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Deposito(4, 10, (decimal)peso, null, Deteccion));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Deposito(4, 10, null, (decimal)peso, Deteccion));
        }

        [TestMethod]
        public void Constructor_PesoMaximo_SePermite()
        {
            Assert.AreEqual(999.999m, new Deposito(4, 10, 999.999m, null, Deteccion).PesoMedidoKg);
        }

        [TestMethod]
        public void Verificar_PrimeraVez_GuardaMaterialVerificadorYFecha()
        {
            Deposito deposito = CrearDeposito();

            deposito.Verificar(11, 3, Deteccion.AddHours(1));

            Assert.IsTrue(deposito.EstaVerificado);
            Assert.AreEqual(11, deposito.IdMaterialVerificado);
            Assert.AreEqual(3, deposito.IdVerificador);
            Assert.AreEqual(Deteccion.AddHours(1), deposito.FechaVerificacionUtc);
        }

        [TestMethod]
        public void Verificar_DosVeces_Lanza()
        {
            Deposito deposito = CrearDeposito();
            deposito.Verificar(11, 3, Deteccion);

            Assert.ThrowsExactly<InvalidOperationException>(() => deposito.Verificar(10, 3, Deteccion));
        }

        [TestMethod]
        public void Verificar_AntesDeLaDeteccion_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() => CrearDeposito().Verificar(11, 3, Deteccion.AddSeconds(-1)));
        }

        [TestMethod]
        public void Reconstruccion_Verificado_ConservaLosDatos()
        {
            var deposito = new Deposito(6, 4, 8, 10, 11, 3, Deteccion, null, 0.3m, Deteccion);

            Assert.AreEqual(6, deposito.Id);
            Assert.AreEqual(8, deposito.IdEvento);
            Assert.IsTrue(deposito.EstaVerificado);
            Assert.AreEqual(0.3m, deposito.PesoEstimadoKg);
        }

        [TestMethod]
        public void Reconstruccion_VerificacionIncompleta_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new Deposito(6, 4, 8, 10, 11, null, null, 0.3m, null, Deteccion));
        }

        [TestMethod]
        public void Reconstruccion_SinEvento_Lanza()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Deposito(6, 4, 0, 10, null, null, null, 0.3m, null, Deteccion));
        }
    }
}
