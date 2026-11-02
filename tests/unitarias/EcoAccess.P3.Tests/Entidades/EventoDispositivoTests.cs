using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Tests.Entidades
{

    [TestClass]
    public class EventoDispositivoTests
    {
        private static readonly DateTime Recepcion = new(2026, 10, 24, 15, 0, 0, DateTimeKind.Utc);

        [TestMethod]
        public void Constructor_AplicadoConOrden_CreaEventoNuevo()
        {
            var evento = new EventoDispositivo(1, " e-1 ", TipoEventoDispositivo.Acuse, 5, ResultadoEvento.Aplicado, null, Recepcion);

            Assert.IsTrue(evento.EsNuevo);
            Assert.AreEqual("e-1", evento.IdExterno);
            Assert.AreEqual(5, evento.IdOrden);
            Assert.IsNull(evento.FechaDispositivoUtc);
        }

        [TestMethod]
        public void Constructor_AplicadoSinOrden_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new EventoDispositivo(1, "e-1", TipoEventoDispositivo.Acuse, null, ResultadoEvento.Aplicado, null, Recepcion));
        }

        [TestMethod]
        public void Constructor_RevisionSinOrden_SePermite()
        {
            var evento = new EventoDispositivo(1, "e-1", TipoEventoDispositivo.Deteccion, null, ResultadoEvento.Revision, Recepcion, Recepcion);

            Assert.AreEqual(ResultadoEvento.Revision, evento.Resultado);
            Assert.AreEqual(Recepcion, evento.FechaDispositivoUtc);
        }

        [TestMethod]
        public void Constructor_IdExternoDemasiadoLargo_Lanza()
        {
            string largo = new('x', EventoDispositivo.LongitudMaximaIdExterno + 1);

            Assert.ThrowsExactly<ArgumentException>(() =>
                new EventoDispositivo(1, largo, TipoEventoDispositivo.Acuse, null, ResultadoEvento.Revision, null, Recepcion));
        }

        [TestMethod]
        public void Constructor_TipoNoDefinido_Lanza()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                new EventoDispositivo(1, "e-1", (TipoEventoDispositivo)99, null, ResultadoEvento.Revision, null, Recepcion));
        }

        [TestMethod]
        public void Constructor_FechaDispositivoLocal_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new EventoDispositivo(1, "e-1", TipoEventoDispositivo.Acuse, null, ResultadoEvento.Revision, DateTime.Now, Recepcion));
        }

        [TestMethod]
        public void Reconstruccion_ConId_LoConserva()
        {
            var evento = new EventoDispositivo(8, 1, "e-1", TipoEventoDispositivo.Cierre, 5, ResultadoEvento.Aplicado, null, Recepcion);

            Assert.AreEqual(8, evento.Id);
            Assert.IsFalse(evento.EsNuevo);
        }
    }
}
