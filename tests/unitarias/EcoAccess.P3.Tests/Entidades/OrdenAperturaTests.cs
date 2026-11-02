using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Tests.Entidades
{

    [TestClass]
    public class OrdenAperturaTests
    {
        private static readonly DateTime Emision = new(2026, 10, 24, 15, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Vencimiento = Emision.AddMinutes(2);
        private const string Clave = "clave-0001";

        private static OrdenApertura CrearOrden() => new(1, 2, Clave, Emision, Vencimiento);

        [TestMethod]
        public void Constructor_DatosValidos_CreaEmitidaVigente()
        {
            OrdenApertura orden = CrearOrden();

            Assert.IsTrue(orden.EsNuevo);
            Assert.AreEqual(EstadoOrdenApertura.Emitida, orden.Estado);
            Assert.IsTrue(orden.EsVigente);
            Assert.IsNull(orden.FechaCierreUtc);
        }

        [TestMethod]
        [DataRow("corta")]
        [DataRow("   ")]
        public void Constructor_ClaveInvalida_Lanza(string clave)
        {
            Assert.ThrowsExactly<ArgumentException>(() => new OrdenApertura(1, 2, clave, Emision, Vencimiento));
        }

        [TestMethod]
        public void Constructor_ClaveDemasiadoLarga_Lanza()
        {
            string clave = new('k', OrdenApertura.LongitudMaximaClave + 1);

            Assert.ThrowsExactly<ArgumentException>(() => new OrdenApertura(1, 2, clave, Emision, Vencimiento));
        }

        [TestMethod]
        public void Constructor_VencimientoNoPosterior_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new OrdenApertura(1, 2, Clave, Emision, Emision));
        }

        [TestMethod]
        public void Avances_EnOrden_LleganADetectada()
        {
            OrdenApertura orden = CrearOrden();

            orden.MarcarEnviada();
            orden.RegistrarAcuse();
            orden.RegistrarApertura();
            orden.RegistrarDeteccion();

            Assert.AreEqual(EstadoOrdenApertura.Detectada, orden.Estado);
            Assert.IsTrue(orden.EsVigente);
        }

        [TestMethod]
        public void Avances_SaltandoEstados_SePermiten()
        {
            OrdenApertura orden = CrearOrden();

            orden.RegistrarApertura();

            Assert.AreEqual(EstadoOrdenApertura.Abierta, orden.Estado);
        }

        [TestMethod]
        public void Avances_Retroceso_Lanza()
        {
            OrdenApertura orden = CrearOrden();
            orden.RegistrarApertura();

            Assert.ThrowsExactly<InvalidOperationException>(orden.RegistrarAcuse);
        }

        [TestMethod]
        public void Avances_MismoEstado_Lanza()
        {
            OrdenApertura orden = CrearOrden();
            orden.MarcarEnviada();

            Assert.ThrowsExactly<InvalidOperationException>(orden.MarcarEnviada);
        }

        [TestMethod]
        public void Cerrar_Vigente_TerminaConFecha()
        {
            OrdenApertura orden = CrearOrden();

            orden.Cerrar(Emision.AddMinutes(1));

            Assert.AreEqual(EstadoOrdenApertura.Cerrada, orden.Estado);
            Assert.IsFalse(orden.EsVigente);
            Assert.AreEqual(Emision.AddMinutes(1), orden.FechaCierreUtc);
            Assert.IsFalse(orden.PuedeAvanzarA(EstadoOrdenApertura.Detectada));
        }

        [TestMethod]
        public void Terminar_YaTerminada_Lanza()
        {
            OrdenApertura orden = CrearOrden();
            orden.Fallar(Emision);

            Assert.ThrowsExactly<InvalidOperationException>(() => orden.Vencer(Vencimiento));
            Assert.ThrowsExactly<InvalidOperationException>(orden.MarcarEnviada);
        }

        [TestMethod]
        public void Terminar_AntesDeLaEmision_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() => CrearOrden().Cerrar(Emision.AddSeconds(-1)));
        }

        [TestMethod]
        public void EstaVencida_SegunElVencimiento()
        {
            OrdenApertura orden = CrearOrden();

            Assert.IsFalse(orden.EstaVencida(Vencimiento.AddTicks(-1)));
            Assert.IsTrue(orden.EstaVencida(Vencimiento));
            orden.Vencer(Vencimiento);
            Assert.IsFalse(orden.EstaVencida(Vencimiento.AddHours(1)));
        }

        [TestMethod]
        public void Reconstruccion_TerminadaConFecha_ConservaLosDatos()
        {
            var orden = new OrdenApertura(9, 1, 2, Clave, EstadoOrdenApertura.Vencida, Emision, Vencimiento, Vencimiento);

            Assert.AreEqual(9, orden.Id);
            Assert.AreEqual(EstadoOrdenApertura.Vencida, orden.Estado);
        }

        [TestMethod]
        public void Reconstruccion_VigenteConFechaDeCierre_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new OrdenApertura(9, 1, 2, Clave, EstadoOrdenApertura.Abierta, Emision, Vencimiento, Vencimiento));
        }

        [TestMethod]
        public void Reconstruccion_TerminadaSinFecha_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new OrdenApertura(9, 1, 2, Clave, EstadoOrdenApertura.Cerrada, Emision, Vencimiento, null));
        }
    }
}
