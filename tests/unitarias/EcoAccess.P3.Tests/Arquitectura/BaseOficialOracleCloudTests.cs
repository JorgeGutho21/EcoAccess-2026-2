using System;
using System.IO;
using System.Text.RegularExpressions;
using EcoAccess.P3.Datos.Oracle;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Arquitectura
{
    /// <summary>
    /// Unificación: la base oficial es Oracle Cloud (alias TNS del wallet). Docker y Oracle local quedan como entornos
    /// de desarrollo opcionales y no son requisito para abrir la aplicación.
    /// </summary>
    [TestClass]
    public sealed class BaseOficialOracleCloudTests
    {
        private static string Raiz()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "EcoAccess.P3.sln"))) dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("No se encontró la raíz del repositorio.");
        }

        [TestMethod]
        public void ElDestinoPredeterminadoEsElAliasDeOracleCloud() =>
            Assert.AreEqual("ecoaccess_tp", CadenaConexionOracle.DestinoPredeterminado);

        [TestMethod]
        public void AppConfigDelEscritorioApuntaALaNubeSinContrasena()
        {
            string texto = File.ReadAllText(Path.Combine(Raiz(), "src", "EcoAccess.P3.Presentacion", "App.config"));
            Match cadena = Regex.Match(texto, "connectionString=\"([^\"]+)\"");
            Assert.IsTrue(cadena.Success, "App.config debe declarar la cadena pública OracleP3.");
            Assert.AreEqual("ecoaccess_tp", CadenaConexionOracle.Destino(cadena.Groups[1].Value));
            Assert.IsFalse(CadenaConexionOracle.TieneContrasena(cadena.Groups[1].Value), "App.config nunca lleva contraseña.");
        }

        [TestMethod]
        [DataRow("Data Source=localhost:1522/PRUEBAP3;User Id=X")]
        [DataRow("Data Source=localhost:1521/FREEPDB1;User Id=X")]
        public void UnDestinoEzConnectNoNecesitaWallet(string cadena) =>
            Assert.IsTrue(CadenaConexionOracle.PrepararWallet(cadena));

        [TestMethod]
        public void LaPresentacionNoExigeDockerConLaBaseOficial()
        {
            string formulario = File.ReadAllText(Path.Combine(Raiz(), "src", "EcoAccess.P3.Presentacion", "FormActivacionOracle.cs"));
            StringAssert.Contains(formulario, "Wallet de Oracle Cloud");
            StringAssert.Contains(formulario, "if (_puertoLocal == 1522)", "Docker solo se comprueba con el destino local de desarrollo.");
        }

        [TestMethod]
        public void ElLaboratorioDePropiedadesSoloSeAbreConArgumentoExplicito()
        {
            string programa = File.ReadAllText(Path.Combine(Raiz(), "src", "EcoAccess.P3.Presentacion", "Program.cs"));
            StringAssert.Contains(programa, "--laboratorio-propiedades");
            Assert.IsFalse(Regex.IsMatch(programa, @"abrirLaboratorioPropiedades\s*=\s*true"), "El arranque normal no puede abrir el formulario de práctica.");
        }
    }
}
