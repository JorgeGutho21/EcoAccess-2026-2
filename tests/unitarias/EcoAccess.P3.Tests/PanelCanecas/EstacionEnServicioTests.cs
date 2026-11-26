using System;
using System.IO;
using System.Text.RegularExpressions;
using EcoAccess.P3.Logica.Vision35.Expo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.PanelCanecas
{
    /// <summary>
    /// Estación Oficial EcoAccess en servicio por defecto (solo una pausa manual la detiene) y un único servicio central de
    /// hardware al que los equipos de los compañeros llegan por la red. Se leen las fuentes como texto: no se conecta a
    /// ningún panel ni mueve el ESP32.
    /// </summary>
    [TestClass]
    public sealed class EstacionEnServicioTests
    {
        private static string _html = "", _programa = "";

        [ClassInitialize]
        public static void Cargar(TestContext _)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "EcoAccess.P3.sln"))) dir = dir.Parent;
            Assert.IsNotNull(dir, "No se encontró la raíz del repositorio.");
            string panel = Path.Combine(dir.FullName, "src", "EcoAccess.P3.PanelCanecas");
            _html = File.ReadAllText(Path.Combine(panel, "wwwroot", "index.html"));
            _programa = File.ReadAllText(Path.Combine(panel, "Program.cs"));
        }

        [TestMethod]
        public void Panel_ArrancaEnServicio_YSoloUnaPausaManualLoDetiene()
        {
            // Causa real de «la estación está en pausa» tras cada inicio: la activación caducaba a las 23:59.
            StringAssert.DoesNotMatch(_programa, new Regex("FinDeJornadaUtc|_servicioHastaUtc"));
            StringAssert.Contains(_programa, "private static bool EnServicio => _pausaMotivo is null;");
            // DETENER TODO deja la estación en pausa persistente (no vuelve sola al reiniciar).
            StringAssert.Contains(_programa, "CambiarServicio(false, \"DETENER TODO (emergencia)\")");
            // La pausa solo se pone o quita desde el equipo anfitrión.
            StringAssert.Matches(_programa, new Regex(@"ruta == ""/api/estacion/servicio""\)\s*\{\s*if \(!local\)"));
        }

        [TestMethod]
        public void Pagina_NoTieneCadenasJavaScriptPartidasEnVariasLineas()
        {
            // Un confirm("…") o prompt("…") con saltos de línea reales rompe TODO el script de la página (SyntaxError).
            foreach (Match m in Regex.Matches(_html, @"(?:confirm|prompt)\(""([^""]*)"""))
                Assert.IsFalse(m.Groups[1].Value.Contains("\n"), "Cadena partida en la página: " + m.Value.Substring(0, Math.Min(60, m.Value.Length)));
        }

        [TestMethod]
        public void Pagina_MuestraPausaManualYReanudar()
        {
            StringAssert.Contains(_html, "PONER EN PAUSA");
            StringAssert.Contains(_html, "REANUDAR SERVICIO");
            StringAssert.DoesNotMatch(_html, new Regex("servicioHasta"));
        }

        [TestMethod]
        public void Anfitrion_ArchivoCompletaLoQueFaltaYLasVariablesMandan()
        {
            var lineas = new[] { "# comentario", "url=http://192.168.1.20:8090", "clave=abc123def456" };
            var (url, clave) = AnfitrionEstacion.Interpretar(lineas, null, null);
            Assert.AreEqual("http://192.168.1.20:8090", url);
            Assert.AreEqual("abc123def456", clave);

            (url, clave) = AnfitrionEstacion.Interpretar(lineas, "http://10.0.0.5:8090", null);
            Assert.AreEqual("http://10.0.0.5:8090", url, "La variable ECOACCESS_PANEL_URL manda sobre el archivo.");
            Assert.AreEqual("abc123def456", clave);
        }

        [TestMethod]
        public void Anfitrion_DistingueElPanelLocalDelDeLaRed()
        {
            Assert.IsFalse(AnfitrionEstacion.EsRemoto("http://127.0.0.1:8090"));
            Assert.IsFalse(AnfitrionEstacion.EsRemoto("http://localhost:8090"));
            Assert.IsTrue(AnfitrionEstacion.EsRemoto("http://192.168.1.20:8090"));
            Assert.IsTrue(AnfitrionEstacion.EsRemoto("http://pc-jorge:8090"));
        }
    }
}
