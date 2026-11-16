using System;
using System.IO;
using System.Linq;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Presentacion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{

    [TestClass]
    public sealed class ComparacionPeriodosTests
    {
        [TestMethod]
        public void ConBaseAnterior_CalculaLaVariacion()
        {
            FilaComparacion fila = ComparacionPeriodos.Fila("Depósitos", 15, 10);
            Assert.AreEqual(5, fila.Diferencia);
            Assert.AreEqual("+50 %", fila.Variacion);
            Assert.AreEqual("-25 %", ComparacionPeriodos.Variacion(3, 4));
        }

        [TestMethod]
        public void SinBaseAnterior_NoInventaUnPorcentaje()
        {
            Assert.AreEqual(ComparacionPeriodos.SinBase, ComparacionPeriodos.Variacion(4, 0));
            Assert.AreEqual("Sin cambio", ComparacionPeriodos.Variacion(0, 0));
        }

        [TestMethod]
        public void ReversosNegativos_CalculanSobreElValorAbsoluto()
        {
            Assert.AreEqual("-100 %", ComparacionPeriodos.Variacion(-4, -2));
        }
    }

    [TestClass]
    public sealed class ProyeccionesEcoAccessTests
    {
        [TestMethod]
        public void LosBloquesSonLosPedidosYNoDeclaranInstalaciones()
        {
            CollectionAssert.AreEqual(new[] { "Bloque A", "Bloque B", "Bloque C", "Bloque H", "Bloque I", "Bloque P", "Comedor universitario" },
                ProyeccionesEcoAccess.Bloques.Select(b => b.Nombre).ToArray());
            StringAssert.Contains(ProyeccionesEcoAccess.AvisoBloques, "Proyección futura");
            StringAssert.Contains(ProyeccionesEcoAccess.CantidadCanecas, "pendiente de planificación");
            foreach (BloqueProyectado bloque in ProyeccionesEcoAccess.Bloques)
            {
                string texto = (bloque.Objetivo + bloque.Integracion).ToUpperInvariant();
                Assert.IsFalse(texto.Contains("INSTALADA") || texto.Contains("YA FUNCIONA"), bloque.Nombre);
                Assert.IsFalse(texto.Any(char.IsDigit), "Sin cifras inventadas: " + bloque.Nombre);
            }
        }

        [TestMethod]
        public void LasRecompensasNoSePresentanComoDisponibles()
        {
            ElementoEvolucion recompensas = ProyeccionesEcoAccess.Evolucion.Single(e => e.Titulo.StartsWith("Recompensas", StringComparison.Ordinal));
            Assert.AreEqual(EstadoProyeccion.Futura, recompensas.Estado);
            Assert.AreEqual(EstadoProyeccion.EnDesarrollo, ProyeccionesEcoAccess.Evolucion.Single(e => e.Titulo.Contains("ESP32")).Estado);
            Assert.AreEqual(EstadoProyeccion.EnDesarrollo, ProyeccionesEcoAccess.Evolucion.Single(e => e.Titulo.Contains("Power BI")).Estado);
        }

        [TestMethod]
        public void LosResiduosEspecialesNoSeOfrecenComoCanecaOrdinaria()
        {
            CanecaProyectada especiales = ProyeccionesEcoAccess.CanecasFuturas.Single(c => c.Categoria == EcoAccess.P3.Entidades.CategoriaMaterial.Peligroso);
            StringAssert.Contains(especiales.Descripcion, "No son residuos ordinarios");
        }
    }

    [TestClass]
    public sealed class AccesoPowerBiTests
    {
        private static readonly string[] Programas = { @"C:\pf\PBIDesktop.exe" };
        private const string Raiz = @"C:\eco";

        [TestMethod]
        public void SinProgramaNiInforme_LoExplicaYNoIntentaAbrir()
        {
            var estado = AccesoPowerBi.Consultar(Raiz, Programas, _ => false);
            Assert.IsFalse(estado.SePuedeAbrir);
            StringAssert.Contains(AccesoPowerBi.Explicar(estado), "no está instalado");
        }

        [TestMethod]
        public void ConProgramaPeroSinInforme_NoFingeUnInforme()
        {
            var estado = AccesoPowerBi.Consultar(Raiz, Programas, ruta => ruta == Programas[0]);
            Assert.IsFalse(estado.SePuedeAbrir);
            StringAssert.Contains(AccesoPowerBi.Explicar(estado), "Todavía no existe");
        }

        [TestMethod]
        public void ConAmbos_SePuedeAbrir()
        {
            string informe = Path.Combine(Raiz, AccesoPowerBi.RutaInforme);
            var estado = AccesoPowerBi.Consultar(Raiz, Programas, ruta => ruta == Programas[0] || ruta == informe);
            Assert.IsTrue(estado.SePuedeAbrir);
            Assert.AreEqual(informe, estado.Informe);
        }

        [TestMethod]
        public void ConoceLasInstalacionesHabituales()
        {
            string[] rutas = AccesoPowerBi.ProgramasConocidos(@"C:\PF", @"C:\PF86", @"C:\Local").ToArray();
            Assert.AreEqual(3, rutas.Length);
            StringAssert.EndsWith(rutas[0], "PBIDesktop.exe");
            StringAssert.EndsWith(rutas[2], "PBIDesktopStore.exe");
        }
    }
}
