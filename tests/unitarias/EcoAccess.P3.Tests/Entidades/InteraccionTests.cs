using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Tests.Entidades
{

    [TestClass]
    public class InteraccionTests
    {
        private static readonly DateTime Inicio = new(2026, 10, 24, 15, 0, 0, DateTimeKind.Utc);

        private static Interaccion CrearIniciada() => new(1, 7, CanalInteraccion.Telegram, Inicio);

        private static Interaccion CrearAutorizada()
        {
            Interaccion interaccion = CrearIniciada();
            interaccion.Autorizar(10);
            return interaccion;
        }

        // ---------- creación ----------

        [TestMethod]
        public void Constructor_DatosValidos_CreaIniciadaSinCierre()
        {
            Interaccion interaccion = CrearIniciada();

            Assert.IsTrue(interaccion.EsNuevo);
            Assert.AreEqual(EstadoInteraccion.Iniciada, interaccion.Estado);
            Assert.AreEqual(7, interaccion.IdUsuario);
            Assert.IsNull(interaccion.FechaCierreUtc);
            Assert.IsFalse(interaccion.EstaCerrada);
        }

        [TestMethod]
        public void Constructor_CanalManual_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new Interaccion(1, 7, CanalInteraccion.Manual, Inicio));
        }

        [TestMethod]
        public void Constructor_FechaLocal_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new Interaccion(1, 7, CanalInteraccion.Telegram, DateTime.Now));
        }

        [TestMethod]
        public void AperturaManual_NaceCerradaSinDepositoConOperario()
        {
            Interaccion manual = Interaccion.AperturaManual(1, 3, "  Atasco del sensor ", Inicio);

            Assert.AreEqual(CanalInteraccion.Manual, manual.Canal);
            Assert.AreEqual(EstadoInteraccion.SinDeposito, manual.Estado);
            Assert.AreEqual(3, manual.IdOperario);
            Assert.IsNull(manual.IdUsuario);
            Assert.AreEqual("Atasco del sensor", manual.Motivo);
            Assert.AreEqual(Inicio, manual.FechaCierreUtc);
        }

        [TestMethod]
        public void AperturaManual_SinMotivo_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() => Interaccion.AperturaManual(1, 3, " ", Inicio));
        }

        // ---------- transiciones ----------

        [TestMethod]
        public void RegistrarSugerencia_Iniciada_ReemplazaLaAnterior()
        {
            Interaccion interaccion = CrearIniciada();

            interaccion.RegistrarSugerencia(10, 0.5m);
            interaccion.RegistrarSugerencia(11, 0.875m);

            Assert.AreEqual(11, interaccion.IdMaterialSugerido);
            Assert.AreEqual(0.875m, interaccion.ConfianzaSugerida);
            Assert.AreEqual(EstadoInteraccion.Iniciada, interaccion.Estado);
        }

        [TestMethod]
        [DataRow(-0.001)]
        [DataRow(1.001)]
        [DataRow(0.1234)]
        public void RegistrarSugerencia_ConfianzaInvalida_Lanza(double confianza)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CrearIniciada().RegistrarSugerencia(10, (decimal)confianza));
        }

        [TestMethod]
        public void Autorizar_Iniciada_FijaMaterialConfirmado()
        {
            Interaccion interaccion = CrearAutorizada();

            Assert.AreEqual(EstadoInteraccion.Autorizada, interaccion.Estado);
            Assert.AreEqual(10, interaccion.IdMaterialConfirmado);
        }

        [TestMethod]
        public void Autorizar_YaAutorizada_Lanza()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => CrearAutorizada().Autorizar(11));
        }

        [TestMethod]
        public void Rechazar_Iniciada_CierraConMotivo()
        {
            Interaccion interaccion = CrearIniciada();

            interaccion.Rechazar("No reconocido", Inicio.AddMinutes(1));

            Assert.AreEqual(EstadoInteraccion.Rechazada, interaccion.Estado);
            Assert.AreEqual("No reconocido", interaccion.Motivo);
            Assert.IsTrue(interaccion.EstaCerrada);
        }

        [TestMethod]
        public void Rechazar_Autorizada_Lanza()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => CrearAutorizada().Rechazar("Tarde", Inicio));
        }

        [TestMethod]
        public void CerrarConDeposito_Autorizada_Cierra()
        {
            Interaccion interaccion = CrearAutorizada();

            interaccion.CerrarConDeposito(Inicio.AddMinutes(1));

            Assert.AreEqual(EstadoInteraccion.ConDeposito, interaccion.Estado);
            Assert.AreEqual(Inicio.AddMinutes(1), interaccion.FechaCierreUtc);
        }

        [TestMethod]
        public void CerrarSinDeposito_Iniciada_Lanza()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => CrearIniciada().CerrarSinDeposito(Inicio));
        }

        [TestMethod]
        public void CerrarConDeposito_AntesDelInicio_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() => CrearAutorizada().CerrarConDeposito(Inicio.AddSeconds(-1)));
        }

        // ---------- reconstrucción ----------

        [TestMethod]
        public void Reconstruccion_AutorizadaValida_ConservaLosDatos()
        {
            var interaccion = new Interaccion(5, 1, 7, CanalInteraccion.Simulador, EstadoInteraccion.Autorizada, 11, 0.9m, 10, null, null, Inicio, null);

            Assert.AreEqual(5, interaccion.Id);
            Assert.AreEqual(10, interaccion.IdMaterialConfirmado);
            Assert.AreEqual(0.9m, interaccion.ConfianzaSugerida);
        }

        [TestMethod]
        public void Reconstruccion_SugerenciaSinConfianza_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new Interaccion(5, 1, 7, CanalInteraccion.Telegram, EstadoInteraccion.Iniciada, 11, null, null, null, null, Inicio, null));
        }

        [TestMethod]
        public void Reconstruccion_CerradaSinFecha_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new Interaccion(5, 1, 7, CanalInteraccion.Telegram, EstadoInteraccion.ConDeposito, null, null, 10, null, null, Inicio, null));
        }

        [TestMethod]
        public void Reconstruccion_RechazadaSinMotivo_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new Interaccion(5, 1, 7, CanalInteraccion.Telegram, EstadoInteraccion.Rechazada, null, null, null, null, null, Inicio, Inicio));
        }

        [TestMethod]
        public void Reconstruccion_AutorizadaSinConfirmado_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new Interaccion(5, 1, 7, CanalInteraccion.Telegram, EstadoInteraccion.Autorizada, null, null, null, null, null, Inicio, null));
        }

        [TestMethod]
        public void Reconstruccion_ManualConMaterial_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new Interaccion(5, 1, null, CanalInteraccion.Manual, EstadoInteraccion.SinDeposito, null, null, 10, 3, "Revisión", Inicio, Inicio));
        }

        [TestMethod]
        public void Reconstruccion_AutomaticaConOperario_Lanza()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new Interaccion(5, 1, 7, CanalInteraccion.Telegram, EstadoInteraccion.Iniciada, null, null, null, 3, null, Inicio, null));
        }
    }
}
