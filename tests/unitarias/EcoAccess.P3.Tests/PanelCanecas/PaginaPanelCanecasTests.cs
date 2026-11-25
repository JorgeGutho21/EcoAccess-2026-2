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
        public void BarraDeServicios_MuestraLosSieteIndicadoresYElEstadoDeLaEstacion()
        {
            string barra = Regex.Match(_html, @"<header class=""barra"">.*?</header>", RegexOptions.Singleline).Value;
            foreach (string id in new[] { "iUsb", "iCom", "iEsp", "iOra", "iBot", "iVis", "iWeb", "estacion", "btnControl" })
                StringAssert.Contains(barra, $"id=\"{id}\"", "Falta en la barra fija: " + id);
            StringAssert.Contains(barra, "ECOACCESS ESP32");
            // La visión (Ollama) se lee del diagnóstico con un GET de solo lectura, nunca con una orden.
            string leer = Regex.Match(_html, @"async function leerDiagnostico\(\) \{.*?\n\}", RegexOptions.Singleline).Value;
            StringAssert.Contains(leer, "api(\"GET\", \"/api/diagnostico\")");
            Assert.DoesNotContain("\"POST\"", leer);
            StringAssert.Contains(Regex.Match(_html, @"async function refrescar\(\) \{.*?\n\}", RegexOptions.Singleline).Value, "leerDiagnostico()");
        }

        [TestMethod]
        public void SinConexion_SeMuestraElProblemaYQueHacer_SinVerdesFalsos()
        {
            StringAssert.Contains(_html, "id=\"problema\"");
            foreach (string estado in new[] { "usb-desconectado", "usb-detectado", "puerto-ocupado", "sin-respuesta" })
                StringAssert.Matches(_html, new Regex("\"" + estado + "\":\\[\"[^\"]+\", \"[^\"]+\"\\]"), "Falta la instrucción para " + estado);
            StringAssert.Contains(_html, "Servicio del panel apagado");
            // Con el servicio caído ningún indicador queda en verde.
            string caido = Regex.Match(_html, @"function pintarCaido\(error\) \{.*?\n\}", RegexOptions.Singleline).Value;
            Assert.DoesNotContain("\"ok\"", caido);
            StringAssert.Contains(caido, "\"iVis\"");
        }

        [TestMethod]
        public void TresCanalesCombinacionesYRestaurarSinValoresDeFabrica()
        {
            StringAssert.Matches(_html, new Regex(@"V:\{ n:""Verde"".*pin:23"));
            StringAssert.Matches(_html, new Regex(@"B:\{ n:""Blanca"".*pin:19"));
            StringAssert.Matches(_html, new Regex(@"N:\{ n:""Negra"".*pin:18"));
            StringAssert.Contains(_html, "const COMBOS = [\"V\",\"B\",\"N\",\"VB\",\"VN\",\"BN\",\"VBN\"];");
            foreach (string capa in new[] { "cuerpo", "boca", "tapa", "tapa-interior" }) StringAssert.Contains(_html, $"capa(c,\"{capa}\")");
            // Restaurar = descartar en el ESP32 (vuelve a lo guardado en su memoria) y releer; nunca envía ángulos propios.
            string restaurar = Regex.Match(_html, @"async function restaurar\(c\) \{.*?\n\}", RegexOptions.Singleline).Value;
            StringAssert.Contains(restaurar, "/calibracion/descartar");
            StringAssert.Contains(restaurar, "api(\"GET\", \"/api/estacion/estado\")");
            Assert.DoesNotContain("PUT", restaurar);
            Assert.IsFalse(Regex.IsMatch(restaurar, @"\b100\b"), "Restaurar no puede usar valores de fábrica.");
            // Guardar se verifica releyendo el ESP32.
            string guardar = Regex.Match(_html, @"async function guardar\(c\) \{.*?\n\}", RegexOptions.Singleline).Value;
            StringAssert.Contains(guardar, "api(\"GET\", \"/api/estacion/estado\")");
            StringAssert.Contains(guardar, "!k.pendiente");
        }

        [TestMethod]
        public void Diseno_FuncionaDesde1024PxDeAncho()
        {
            int minimo = int.Parse(Regex.Match(_html, @"body \{[^}]*min-width:(\d+)px").Groups[1].Value);
            Assert.IsTrue(minimo <= 1024, "El panel debe caber en 1024 px sin desplazamiento horizontal.");
            StringAssert.Contains(_html, "@media (max-width:1180px)");
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
