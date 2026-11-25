using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using EcoAccess.P3.Bot.Dispositivos;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Arquitectura
{
    /// <summary>
    /// Reglas del tercer corte (ADO.NET + DAO/Repository + 3 capas + App.config) comprobadas sobre el código real.
    /// Ver docs/academico/MATRIZ_CUMPLIMIENTO_TERCER_CORTE.md.
    /// </summary>
    [TestClass]
    public class PruebasTercerCorte
    {
        [TestMethod]
        public void AppConfig_DelProyectoDeInicio_TieneLaCadenaSinContrasena()
        {
            string ruta = Path.Combine(Raiz(), "src", "EcoAccess.P3.Presentacion", "App.config");
            Assert.IsTrue(File.Exists(ruta), "El proyecto de inicio debe tener App.config.");
            XElement? entrada = XDocument.Load(ruta).Descendants("connectionStrings").Elements("add")
                .FirstOrDefault(e => (string?)e.Attribute("name") == CadenaConexionOracle.Nombre);
            Assert.IsNotNull(entrada, "Falta <add name=\"OracleP3\"> en connectionStrings.");
            string cadena = (string?)entrada!.Attribute("connectionString") ?? "";
            Assert.IsFalse(CadenaConexionOracle.TieneContrasena(cadena), "App.config se versiona: no puede llevar contraseña.");
            Assert.AreEqual("ECOACCESS_P3_APP", CadenaConexionOracle.Usuario(cadena));
            Assert.AreEqual("Oracle.ManagedDataAccess.Client", (string?)entrada.Attribute("providerName"));
            StringAssert.Contains(File.ReadAllText(ruta), "Version=v4.8");
        }

        [TestMethod]
        public void Program_LeeAppConfigConConfigurationManager()
        {
            string program = File.ReadAllText(Path.Combine(Raiz(), "src", "EcoAccess.P3.Presentacion", "Program.cs"));
            StringAssert.Contains(program, "ConfigurationManager.ConnectionStrings[CadenaConexionOracle.Nombre]");
            StringAssert.Contains(program, "CadenaConexionOracle.Resolver(");
        }

        [TestMethod]
        public void Resolver_PrefiereLaCadenaPrivadaYRechazaContrasenasEnAppConfig()
        {
            const string publica = "Data Source=ecoaccess_tp;User Id=ECOACCESS_P3_APP";
            const string privada = "Data Source=ecoaccess_tp;User Id=ECOACCESS_P3_APP;Password=valor-de-prueba";
            Assert.AreEqual(privada, CadenaConexionOracle.Resolver(privada, publica));
            Assert.AreEqual(publica, CadenaConexionOracle.Resolver(null, publica));
            Assert.AreEqual(publica, CadenaConexionOracle.Resolver("  ", publica));
            Assert.IsNull(CadenaConexionOracle.Resolver(null, privada), "Una contraseña en App.config no se usa.");
            Assert.IsNull(CadenaConexionOracle.Resolver(null, null));
        }

        [TestMethod]
        public void CadenaConexion_LeeDestinoUsuarioYContrasenaSinExcepciones()
        {
            Assert.AreEqual("ecoaccess_tp", CadenaConexionOracle.Destino("Data Source=ecoaccess_tp;User Id=X"));
            Assert.AreEqual(CadenaConexionOracle.DestinoPredeterminado, CadenaConexionOracle.Destino(null));
            Assert.AreEqual(CadenaConexionOracle.DestinoPredeterminado, CadenaConexionOracle.Destino("esto no es una cadena="));
            Assert.IsTrue(CadenaConexionOracle.TieneContrasena("User Id=X;PWD=y"));
            Assert.IsFalse(CadenaConexionOracle.TieneContrasena("User Id=X;Password="));
            Assert.IsNull(CadenaConexionOracle.Usuario("Data Source=a"));
        }

        [TestMethod]
        public void Presentacion_NoEscribeSqlNiUsaTiposAdoNet()
        {
            // Regla de oro del curso: el formulario nunca toca la conexión. Program.cs es la raíz de composición.
            var sql = new Regex(@"\b(OracleConnection|OracleCommand|OracleDataReader|SqlConnection|ExecuteReader|ExecuteNonQuery)\b|""\s*(SELECT|INSERT|UPDATE|DELETE)\s", RegexOptions.IgnoreCase);
            string[] infractores = Archivos("src", "EcoAccess.P3.Presentacion")
                .Where(r => Path.GetFileName(r) != "Program.cs" && sql.IsMatch(File.ReadAllText(r)))
                .Select(Path.GetFileName).ToArray()!;
            Assert.IsEmpty(infractores, "SQL o ADO.NET en Presentacion: " + string.Join(", ", infractores));
        }

        [TestMethod]
        public void Datos_CreaComandosSoloConBindByNameYSinConcatenarValores()
        {
            string[] archivos = Archivos("src", "EcoAccess.P3.Datos");
            Assert.IsEmpty(archivos.Where(r => File.ReadAllText(r).Contains("new OracleCommand(")).Select(Path.GetFileName).ToArray(),
                "Los comandos se crean con ComandosOracle.Crear (BindByName = true).");
            // Interpolar valores en el SQL abriría la puerta a la inyección; los valores van en parámetros :nombre.
            var interpolado = new Regex(@"CommandText\s*=\s*\$""");
            Assert.IsEmpty(archivos.Where(r => interpolado.IsMatch(File.ReadAllText(r))).Select(Path.GetFileName).ToArray());
            StringAssert.Contains(File.ReadAllText(archivos.Single(r => Path.GetFileName(r) == "ComandosOracle.cs")), "comando.BindByName = true;");
        }

        [TestMethod]
        public void VentanaDelSimulador_UsaLaFuenteUnicaDeSieteSegundos()
        {
            Assert.AreEqual(7, MensajesTelegram.VentanaDepositoSegundos);
            Assert.AreEqual(MensajesTelegram.VentanaDepositoSegundos, AplicacionBridge.VentanaSimulada(null));
            Assert.AreEqual(MensajesTelegram.VentanaDepositoSegundos, AplicacionBridge.VentanaSimulada("no-numero"));
            Assert.AreEqual(0, AplicacionBridge.VentanaSimulada("0"));
            Assert.AreEqual(60, AplicacionBridge.VentanaSimulada("600"));
            string firmware = File.ReadAllText(Path.Combine(Raiz(), "hardware", "servosecoacces", "configuracion_ejemplo.h"));
            StringAssert.Contains(firmware, $"ABIERTO_MS = {MensajesTelegram.VentanaDepositoSegundos * 1000}");
        }

        [TestMethod]
        public void Documentacion_NoPrometeVeinteSegundosDeApertura()
        {
            var veinte = new Regex(@"(abiert[ao]|apertura|ventana|depositar)[^\n|]{0,40}\b20 (s|seg)", RegexOptions.IgnoreCase);
            string[] candidatos = new[] { Path.Combine(Raiz(), "README.md"), Path.Combine(Raiz(), "hardware", "README.md") }
                .Concat(Directory.GetFiles(Path.Combine(Raiz(), "docs", "academico"), "*.md")).Where(File.Exists).ToArray();
            string[] con20 = candidatos.Where(r => veinte.IsMatch(File.ReadAllText(r))).Select(Path.GetFileName).ToArray()!;
            Assert.IsEmpty(con20, "Documentos que aún dicen 20 s: " + string.Join(", ", con20));
        }

        private static string[] Archivos(params string[] carpeta) =>
            Directory.GetFiles(Path.Combine(new[] { Raiz() }.Concat(carpeta).ToArray()), "*.cs", SearchOption.AllDirectories)
                .Where(r => !r.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                            && !r.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)).ToArray();

        private static string Raiz()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "EcoAccess.P3.sln"))) dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("No se encontró EcoAccess.P3.sln.");
        }
    }
}
