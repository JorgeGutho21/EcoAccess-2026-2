using System.IO;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>Fase de software: Eco informa el destino ambiental sin afirmar aperturas que no existen.</summary>
    [TestClass]
    public class ModoSoftwareTests
    {
        [TestMethod]
        [DataRow(CategoriaMaterial.Aprovechable, "PET", "Botella plástica PET", "Botella plástica PET.\nCaneca blanca.\nModo demostración: apertura física pendiente.")]
        [DataRow(CategoriaMaterial.OrganicoAprovechable, "CASCARA", "Cáscara de fruta", "Cáscara de fruta.\nCaneca verde.\nModo demostración: apertura física pendiente.")]
        [DataRow(CategoriaMaterial.NoAprovechable, "SERVILLETA", "Servilleta usada", "Servilleta usada.\nCaneca negra.\nModo demostración: apertura física pendiente.")]
        public void ElDestinoSeInformaSinAfirmarApertura(CategoriaMaterial categoria, string codigo, string nombre, string esperado)
        {
            var material = new Material(codigo, nombre, categoria, null);
            var consulta = new ConsultaMaterial(material, GuiaSeparacion.Para(categoria), AdmisionMaterial.SinCaneca);
            string texto = MensajesTelegram.DestinoModoSoftware(consulta);
            Assert.AreEqual(esperado, texto);
            Assert.IsFalse(texto.Contains("abierta") || texto.Contains("segundos"), "En modo software nunca se anuncia una apertura.");
        }

        [TestMethod]
        public void ElBotActivaElModoSoftwareSinDispositivoConfigurado()
        {
            string program = File.ReadAllText(Path.Combine(Raiz(), "src", "EcoAccess.P3.Bot", "Program.cs"));
            StringAssert.Contains(program, "conversacion.SinAperturaFisica = despacho is null");
            string flujo = File.ReadAllText(Path.Combine(Raiz(), "src", "EcoAccess.P3.Logica", "Servicios", "ServicioConversacionTelegram.FlujoEco.cs"));
            // La salida de modo software va antes de abrir sesión de caneca, solicitud u orden.
            Assert.IsLessThan(flujo.IndexOf("IdentificarCanecaAsync(mensaje, conversacion, usuario, presente.IdentificadorQr", System.StringComparison.Ordinal),
                flujo.IndexOf("if (SinAperturaFisica)", System.StringComparison.Ordinal));
        }

        private static string Raiz()
        {
            var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "EcoAccess.P3.sln"))) dir = dir.Parent;
            return dir!.FullName;
        }
    }
}
