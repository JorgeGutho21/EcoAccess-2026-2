using System;
using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Vision35;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica.Vision35
{
    [TestClass]
    public sealed class PruebasEscenariosExpoSoftwareFase8Tests
    {
        private readonly MotorReglasMultirresiduo _motor = new();

        private static RespuestaVisionEstructurada Resp(params ObservacionObjetoVisual[] objetos) =>
            new(true, true, CalidadImagenVision.Aceptable, objetos, "test-vision", 10);

        private static ObservacionObjetoVisual Obj(string nombre, string material, EstadoObservableObjeto estado = EstadoObservableObjeto.LimpioYVacio, decimal conf = 0.92m, bool aclarar = false) =>
            new(nombre, material, estado, 1, conf, false, false, false, aclarar);

        [TestMethod]
        public void NueveCombinacionesObligatoriasFase8_ClasificanConExactitudYSeguridad()
        {
            // 1. Manzana -> VERDE
            var r1 = _motor.Evaluar(Resp(Obj("apple", "organic")));
            Assert.AreEqual(CombinacionCanecas.C01_Verde, r1.Combinacion);

            // 2. Papel limpio (incluso arrugado de cuaderno) -> BLANCA
            var r2 = _motor.Evaluar(Resp(Obj("clean crumpled notebook paper", "paper")));
            Assert.AreEqual(CombinacionCanecas.C02_Blanca, r2.Combinacion);

            // 3. Hilo dental usado -> NEGRA
            var r3 = _motor.Evaluar(Resp(Obj("used dental floss", "sanitary", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C03_Negra, r3.Combinacion);

            // 4. Manzana + papel limpio -> VERDE + BLANCA
            var r4 = _motor.Evaluar(Resp(Obj("apple", "organic"), Obj("clean crumpled notebook paper", "paper")));
            Assert.AreEqual(CombinacionCanecas.C04_VerdeBlanca, r4.Combinacion);

            // 5. Manzana + papel limpio + hilo dental usado -> VERDE + BLANCA + NEGRA
            var r5 = _motor.Evaluar(Resp(
                Obj("apple", "organic"),
                Obj("clean crumpled notebook paper", "paper"),
                Obj("used dental floss", "sanitary", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C07_VerdeBlancaNegra, r5.Combinacion);

            // 6. Manzana + papa + cebolla -> VERDE solamente (sin duplicar caneca)
            var r6 = _motor.Evaluar(Resp(Obj("apple", "organic"), Obj("potato", "organic"), Obj("onion", "organic")));
            Assert.AreEqual(CombinacionCanecas.C01_Verde, r6.Combinacion);
            Assert.HasCount(1, r6.CanecasRequeridas);

            // 7. Papa + papel limpio + cable USB -> VERDE + BLANCA + GESTION_ESPECIAL (cable no abre ordinaria)
            var r7 = _motor.Evaluar(Resp(
                Obj("potato", "organic"),
                Obj("clean crumpled notebook paper", "paper"),
                Obj("usb charging cable", "electronic", EstadoObservableObjeto.Desconocido)));
            Assert.AreEqual(CombinacionCanecas.C04_VerdeBlanca, r7.Combinacion);
            Assert.IsTrue(r7.Objetos.Any(o => o.Estado == EstadoClasificacionObjeto.FueraDeCatalogo && o.Motivo.Contains("GESTION_ESPECIAL")));

            // 8. Cable USB solo -> GESTION_ESPECIAL, ninguna caneca ordinaria
            var r8 = _motor.Evaluar(Resp(Obj("usb charging cable", "electronic", EstadoObservableObjeto.Desconocido)));
            Assert.AreEqual(CombinacionCanecas.Ninguna, r8.Combinacion);
            Assert.IsFalse(r8.PuedeAbrirCanecas);

            // 9. Blíster con medicamentos -> manejo farmacéutico especial, ninguna caneca ordinaria
            var r9 = _motor.Evaluar(Resp(Obj("medicine blister pack", "pharmaceutical", EstadoObservableObjeto.Desconocido)));
            Assert.AreEqual(CombinacionCanecas.Ninguna, r9.Combinacion);
            Assert.IsFalse(r9.PuedeAbrirCanecas);
            Assert.IsTrue(r9.Objetos.All(o => o.Motivo.Contains("GESTION_ESPECIAL")));
        }

        [TestMethod]
        public void DudasOImagenDanadaODesconocido_NuncaAbrenCanecaAutomaticamente()
        {
            var danada = _motor.Evaluar(new RespuestaVisionEstructurada(true, false, CalidadImagenVision.CorruptaOInvalida, Array.Empty<ObservacionObjetoVisual>(), "test", 5));
            Assert.IsFalse(danada.PuedeAbrirCanecas);
            Assert.AreEqual(CombinacionCanecas.Ninguna, danada.Combinacion);

            var incierto = _motor.Evaluar(Resp(Obj("clean crumpled notebook paper", "paper", EstadoObservableObjeto.LimpioYVacio, conf: 0.40m, aclarar: true)));
            Assert.IsTrue(incierto.RequiereConfirmacion);
            Assert.IsFalse(incierto.PuedeAbrirCanecas);

            var desconocido = _motor.Evaluar(Resp(Obj("mysterious unknown widget", "unknown", EstadoObservableObjeto.Desconocido)));
            Assert.IsFalse(desconocido.PuedeAbrirCanecas);
            Assert.AreEqual(CombinacionCanecas.Ninguna, desconocido.Combinacion);
        }
    }
}
