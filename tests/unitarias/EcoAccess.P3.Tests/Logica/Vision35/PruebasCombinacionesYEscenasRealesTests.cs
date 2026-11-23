using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Vision35;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica.Vision35
{
    /// <summary>
    /// Validaciones matemáticas exhaustivas (FASE 8 / Sección 9: 595 pares, 6.545 tríos = 7.140 sin repetición;
    /// 630 pares y 7.770 tríos con repetición), escenas obligatorias A–I incluyendo GESTION_ESPECIAL (cable USB),
    /// independencia del orden, FALTA IMAGEN y escalado seguro de fotografías de alta resolución.
    /// </summary>
    [TestClass]
    public sealed class PruebasCombinacionesYEscenasRealesTests
    {
        private static readonly MotorReglasMultirresiduo Motor = new();

        private static RespuestaVisionEstructurada Obs(params ObservacionObjetoVisual[] objs) =>
            new(true, objs.Length > 0, CalidadImagenVision.Aceptable, objs, "test-vl", 100);

        private static ObservacionObjetoVisual Item(string nombre, string material,
            EstadoObservableObjeto estado = EstadoObservableObjeto.LimpioYVacio, int cantidad = 1, decimal conf = 0.93m) =>
            new(nombre, material, estado, cantidad, conf);

        [TestMethod]
        public void UniversoCombinatorioCatalogo35_CumpleExactamente595Pares6545TriosYConRepeticion()
        {
            ResumenCombinatorioCatalogo r = MotorCombinacionesCanecas.EvaluarUniversoCombinatorio(CatalogoPrioritario35.Todos);

            Assert.AreEqual(35, r.TotalMateriales);
            Assert.AreEqual(10, r.TotalVerde);
            Assert.AreEqual(13, r.TotalBlanca);
            Assert.AreEqual(12, r.TotalNegra);

            // Sin repetición: C(35,2)=595 (189 una caneca + 406 dos canecas)
            Assert.AreEqual(595, r.ParesSinRepeticion);
            Assert.AreEqual(189, r.ParesUnaCaneca);
            Assert.AreEqual(406, r.ParesDosCanecas);

            // Sin repetición: C(35,3)=6545 (626 una + 4359 dos + 1560 tres canecas)
            Assert.AreEqual(6545, r.TriosSinRepeticion);
            Assert.AreEqual(626, r.TriosUnaCaneca);
            Assert.AreEqual(4359, r.TriosDosCanecas);
            Assert.AreEqual(1560, r.TriosTresCanecas);
            Assert.AreEqual(7140, r.TotalSinRepeticionParesYTrios);

            // Con repetición: C(36,2)=630 pares y C(37,3)=7770 tríos
            Assert.AreEqual(630, r.ParesConRepeticion);
            Assert.AreEqual(7770, r.TriosConRepeticion);
        }

        [TestMethod]
        public void EscenasObligatorias_A_Hasta_I_SeleccionanExactamenteLasCanecasRequeridas()
        {
            // Escena A: Manzana + papa -> solo VERDE
            var escA = Motor.Evaluar(Obs(Item("green apple", "organic"), Item("potato peel", "organic")));
            Assert.AreEqual(CombinacionCanecas.C01_Verde, escA.Combinacion);
            Assert.IsTrue(escA.PuedeAbrirCanecas);

            // Escena B: Manzana + cartón limpio -> VERDE + BLANCA
            var escB = Motor.Evaluar(Obs(Item("green apple", "organic"), Item("clean cardboard box", "cardboard")));
            Assert.AreEqual(CombinacionCanecas.C04_VerdeBlanca, escB.Combinacion);
            Assert.IsTrue(escB.PuedeAbrirCanecas);

            // Escena C: Papa + hilo dental usado -> VERDE + NEGRA
            var escC = Motor.Evaluar(Obs(Item("potato peel", "organic"), Item("used dental floss", "sanitary", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C05_VerdeNegra, escC.Combinacion);
            Assert.IsTrue(escC.PuedeAbrirCanecas);

            // Escena D: Cartón/papel limpio + hilo dental usado -> BLANCA + NEGRA
            var escD = Motor.Evaluar(Obs(Item("clean paper", "paper"), Item("used dental floss", "sanitary", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C06_BlancaNegra, escD.Combinacion);
            Assert.IsTrue(escD.PuedeAbrirCanecas);

            // Escena E: Manzana + cartón limpio + hilo dental usado -> VERDE + BLANCA + NEGRA
            var escE = Motor.Evaluar(Obs(
                Item("green apple", "organic"),
                Item("clean cardboard", "cardboard"),
                Item("used dental floss", "sanitary", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C07_VerdeBlancaNegra, escE.Combinacion);
            Assert.IsTrue(escE.PuedeAbrirCanecas);

            // Escena F: Papa + papel limpio + cable USB -> VERDE + BLANCA + GESTION_ESPECIAL (SIN tercera caneca)
            var escF = Motor.Evaluar(Obs(
                Item("potato peel", "organic"),
                Item("clean paper", "paper"),
                Item("usb cable", "electronic")));
            Assert.AreEqual(CombinacionCanecas.C04_VerdeBlanca, escF.Combinacion);
            Assert.AreEqual(2, escF.CanecasRequeridas.Count);
            Assert.IsTrue(escF.PuedeAbrirCanecas);
            Assert.IsTrue(escF.Objetos.Any(o => o.Estado == EstadoClasificacionObjeto.FueraDeCatalogo && o.CanecaAsignada is null && o.Motivo.Contains("GESTION_ESPECIAL")));
            StringAssert.Contains(escF.MensajeUsuario, "GESTION_ESPECIAL");

            // Escena G: Manzana + papa + cebolla -> solamente VERDE
            var escG = Motor.Evaluar(Obs(Item("green apple", "organic"), Item("potato peel", "organic"), Item("red onion", "organic")));
            Assert.AreEqual(CombinacionCanecas.C01_Verde, escG.Combinacion);
            Assert.AreEqual(1, escG.CanecasRequeridas.Count);

            // Escena H: Manzana + cartón limpio + papel limpio -> VERDE + BLANCA
            var escH = Motor.Evaluar(Obs(Item("green apple", "organic"), Item("clean cardboard", "cardboard"), Item("notebook paper", "paper")));
            Assert.AreEqual(CombinacionCanecas.C04_VerdeBlanca, escH.Combinacion);
            Assert.AreEqual(2, escH.CanecasRequeridas.Count);

            // Escena I: Papel sucio + servilleta usada + hilo dental -> solamente NEGRA
            var escI = Motor.Evaluar(Obs(
                Item("dirty paper", "paper", EstadoObservableObjeto.Contaminado),
                Item("used napkin", "sanitary", EstadoObservableObjeto.Contaminado),
                Item("used dental floss", "sanitary", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C03_Negra, escI.Combinacion);
            Assert.AreEqual(1, escI.CanecasRequeridas.Count);
        }

        [TestMethod]
        public void IndependenciaDelOrdenYRepeticion_TodasLasPermutacionesDanIdenticoResultado()
        {
            var a = Item("green apple", "organic");
            var b = Item("clean cardboard", "cardboard");
            var c = Item("used dental floss", "sanitary", EstadoObservableObjeto.Contaminado);

            var permutaciones = new[]
            {
                new[] { a, b, c }, new[] { a, c, b },
                new[] { b, a, c }, new[] { b, c, a },
                new[] { c, a, b }, new[] { c, b, a },
            };

            foreach (var perm in permutaciones)
            {
                var r = Motor.Evaluar(Obs(perm));
                Assert.AreEqual(CombinacionCanecas.C07_VerdeBlancaNegra, r.Combinacion);
                Assert.AreEqual(3, r.CanecasRequeridas.Count);
            }

            // Repetición: dos manzanas y una papa -> 1 sola caneca VERDE (jamás 3 aperturas)
            var rep = Motor.Evaluar(Obs(Item("green apple", "organic", cantidad: 2), Item("potato peel", "organic", cantidad: 1)));
            Assert.AreEqual(CombinacionCanecas.C01_Verde, rep.Combinacion);
            Assert.AreEqual(1, rep.CanecasRequeridas.Count);
            Assert.AreEqual(3, rep.TotalObjetosDetectados);
        }

        [TestMethod]
        public void MaterialSinFotografia_ReportaExplicitamenteFaltaImagenSinErrores()
        {
            var sinFoto = CatalogoPrioritario35.BuscarPorCodigo("N10")!;
            Assert.AreEqual("FALTA IMAGEN", CatalogoPrioritario35.EstadoImagenDe(sinFoto));
            var conFoto = CatalogoPrioritario35.BuscarPorCodigo("V01")!;
            Assert.AreEqual("DISPONIBLE", CatalogoPrioritario35.EstadoImagenDe(conFoto));
        }

        [TestMethod]
        public void EscaladoSeguroParaOllama_ReduceFoto1200x1600AMaximo768PxEnMemoria()
        {
            using var bmp = new Bitmap(1200, 1600);
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Jpeg);
            byte[] original = ms.ToArray();

            byte[] optimizada = ClienteVisionOllamaMultirresiduo.PrepararBytesParaOllama(original, 768);
            using var msOut = new MemoryStream(optimizada);
            using var imgOut = Image.FromStream(msOut);

            Assert.AreEqual(768, Math.Max(imgOut.Width, imgOut.Height));
            Assert.AreEqual(576, imgOut.Width);
            Assert.AreEqual(768, imgOut.Height);
        }

        [TestMethod]
        public void RegresionFotosReales20261123_CebollaCitricoCableUsbYTissueClasificanCorrectamente()
        {
            // Cebolla morada y cascara de cebolla -> VERDE (nunca BLANCA ni NEGRA)
            var cebolla = Motor.Evaluar(Obs(Item("red onion", "organic")));
            Assert.AreEqual(CombinacionCanecas.C01_Verde, cebolla.Combinacion);
            var cascaraCebolla = Motor.Evaluar(Obs(Item("red onion skin", "organic")));
            Assert.AreEqual(CombinacionCanecas.C01_Verde, cascaraCebolla.Combinacion);
            var citrico = Motor.Evaluar(Obs(Item("citrus fruit half", "organic")));
            Assert.AreEqual(CombinacionCanecas.C01_Verde, citrico.Combinacion);

            // Cable USB marcado reutilizable o charging cable -> GESTION_ESPECIAL (nunca caneca ordinaria ni NoEsResiduo)
            var usbReut = Motor.Evaluar(Obs(new ObservacionObjetoVisual("USB cable", "electronic", EstadoObservableObjeto.LimpioYVacio, 1, 0.9m, EsReutilizable: true)));
            Assert.AreEqual(CombinacionCanecas.Ninguna, usbReut.Combinacion);
            Assert.AreEqual(EstadoClasificacionObjeto.FueraDeCatalogo, usbReut.Objetos[0].Estado);
            StringAssert.Contains(usbReut.Objetos[0].Motivo, "GESTION_ESPECIAL");

            var cableYCaja = Motor.Evaluar(Obs(Item("charging cable", "electronic"), Item("clean cardboard box", "cardboard")));
            Assert.AreEqual(CombinacionCanecas.C02_Blanca, cableYCaja.Combinacion);
            Assert.IsTrue(cableYCaja.Objetos.Any(o => o.Estado == EstadoClasificacionObjeto.FueraDeCatalogo && o.Motivo.Contains("GESTION_ESPECIAL")));

            // Tissue paper arrugado -> N01 NEGRA (no papel de cuaderno limpio B08)
            var tissue = Motor.Evaluar(Obs(Item("crumpled tissue paper", "paper")));
            Assert.AreEqual(CombinacionCanecas.C03_Negra, tissue.Combinacion);
            Assert.AreEqual("N01", tissue.Objetos[0].Material35?.Codigo);
        }
    }
}