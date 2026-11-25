using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.PanelCanecas
{
    /// <summary>
    /// Reglas de seguridad de la página del centro de control (src/EcoAccess.P3.PanelCanecas/wwwroot/index.html), leída
    /// como texto: no se ejecuta ni se conecta a ningún panel ni al ESP32.
    /// </summary>
    [TestClass]
    public sealed class PaginaPanelCanecasTests
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

        /// <summary>Rutas que atiende Program.cs (contrato vigente del panel; la página no puede inventar otras).</summary>
        private static readonly (string Metodo, Regex Ruta)[] Contrato =
        {
            ("GET", new Regex(@"^/api/estacion/estado$")), ("GET", new Regex(@"^/api/diagnostico$")), ("GET", new Regex(@"^/api/historial$")),
            ("POST", new Regex(@"^/api/estacion/(buscar|probar-comunicacion|detener|abrir-grupo)$")),
            ("POST", new Regex(@"^/api/sesion/(habilitar|terminar)$")),
            ("POST", new Regex(@"^/api/canecas/[VBN]/(detener|abrir|cerrar|probar|posicion)$")),
            ("PUT", new Regex(@"^/api/canecas/[VBN]/calibracion$")),
            ("POST", new Regex(@"^/api/(canecas/[VBN]/)?calibracion/(guardar|descartar)$")),
            ("POST", new Regex(@"^/api/pruebas/(secuencial|combinaciones|c01-c07)$")),
        };

        [TestMethod]
        public void CadaLlamadaDeLaPagina_UsaUnaRutaYMetodoDelContratoDelPanel()
        {
            var llamadas = Regex.Matches(_html, @"(?:api|hacer)\(""(GET|POST|PUT)"",\s*[`""](/api/[^`""]+)[`""]").Cast<Match>()
                .Select(m => (Metodo: m.Groups[1].Value, Ruta: m.Groups[2].Value.Replace("${c}", "V").Replace("${a}", "abrir"))).ToList();
            Assert.IsGreaterThanOrEqualTo(15, llamadas.Count, "Se esperaban las llamadas de la página.");
            foreach (var (metodo, ruta) in llamadas)
                Assert.IsTrue(Contrato.Any(c => c.Metodo == metodo && c.Ruta.IsMatch(ruta)), $"{metodo} {ruta} no existe en el panel.");
            // Y el contrato sigue en Program.cs.
            foreach (string ruta in new[] { "/api/estacion/estado", "/api/diagnostico", "/api/historial", "/api/estacion/detener", "/api/sesion/habilitar", "/api/estacion/abrir-grupo" })
                StringAssert.Contains(_programa, ruta);
        }

        [TestMethod]
        public void AlCargar_SoloSeHacenLecturasGet()
        {
            // La última línea del script es el arranque de la página.
            string inicio = _html.Split('\n').Last(l => l.TrimStart().StartsWith("construir();", StringComparison.Ordinal));
            foreach (string llamada in inicio.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0))
                Assert.IsTrue(Regex.IsMatch(llamada, @"^(construir\(\)|modo\(modalidad\)|pintarOrden\(\)|refrescar\(\)|setInterval\(refrescar, 1000\)|alAbrirDiag\(\))$"), "Llamada inesperada al cargar: " + llamada);
            string refrescar = Regex.Match(_html, @"async function refrescar\(\) \{.*?\n\}", RegexOptions.Singleline).Value;
            string verificar = Regex.Match(_html, @"async function verificar\(\) \{.*?\n\}", RegexOptions.Singleline).Value;
            foreach (string cuerpo in new[] { refrescar, verificar })
            {
                Assert.IsFalse(string.IsNullOrEmpty(cuerpo));
                Assert.DoesNotContain("\"POST\"", cuerpo); Assert.DoesNotContain("\"PUT\"", cuerpo);
            }
        }

        [TestMethod]
        public void DetenerTodo_EstaEnLaBarraFijaYNoExigeSesion()
        {
            string barra = Regex.Match(_html, @"<header class=""barra"">.*?</header>", RegexOptions.Singleline).Value;
            StringAssert.Contains(barra, "id=\"btnDetenerTodo\"");
            StringAssert.Contains(barra, "DETENER TODO");
            StringAssert.Matches(_html, new Regex(@"\.barra \{ position:sticky; top:0;"));
            string accion = _html.Split('\n').Single(l => l.Contains("$(\"btnDetenerTodo\").onclick ="));
            Assert.DoesNotContain("control()", accion, "DETENER TODO no puede depender de la sesión supervisada.");
        }

        [TestMethod]
        public void TextosDeSesionSupervisadaYEstadoComandado_SeConservan()
        {
            StringAssert.Contains(_html, "ACTIVAR CONTROL");
            StringAssert.Contains(_html, "Requiere sesión supervisada");
            StringAssert.Contains(_html, "La sesión supervisada queda activa 2 horas.");
            StringAssert.Contains(_html, "Comandado, no medido");
            StringAssert.Contains(_html, "DETENER por software no reemplaza cortar la alimentación");
            // Los movimientos comprueban la sesión antes de enviar la orden.
            StringAssert.Matches(_html, new Regex(@"function mover\(c, a\) \{ if \(!control\(\)\) return aviso\(motivo\(\), true\);"));
        }

        [TestMethod]
        public void SoloRecursosLocalesYFuentesDeGoogle()
        {
            foreach (Match m in Regex.Matches(_html, @"(?:src|href)=""(https?://[^""]+)"""))
                StringAssert.StartsWith(m.Groups[1].Value, "https://fonts.g", "Recurso externo no permitido: " + m.Groups[1].Value);
            Assert.DoesNotContain("<script src", _html);
        }
    }
}
