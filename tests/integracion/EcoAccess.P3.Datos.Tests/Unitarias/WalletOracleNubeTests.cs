using System;
using System.IO;
using System.Linq;
using EcoAccess.P3.Datos.Oracle;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Unitarias
{
    /// <summary>Reglas del wallet de Oracle Cloud sin conectarse: carpeta, alias y cadena de ODP.NET.</summary>
    [TestClass]
    public sealed class WalletOracleNubeTests
    {
        // Formato real de un wallet de Autonomous Database (host y servicio ficticios).
        private const string Tnsnames =
            "ecoaccess_high = (description= (retry_count=20)(retry_delay=3)(address=(protocol=tcps)(port=1522)(host=adb.sa-bogota-1.oraclecloud.com))(connect_data=(service_name=abc_ecoaccess_high.adb.oraclecloud.com))(security=(ssl_server_dn_match=yes)))\r\n\r\n" +
            "ecoaccess_tp = (description= (retry_count=20)(retry_delay=3)(address=(protocol=tcps)(port=1522)(host=adb.sa-bogota-1.oraclecloud.com))(connect_data=(service_name=abc_ecoaccess_tp.adb.oraclecloud.com))(security=(ssl_server_dn_match=yes)))\r\n";

        private string _carpeta = "";

        [TestInitialize]
        public void Crear()
        {
            _carpeta = Path.Combine(Path.GetTempPath(), "eco_wallet_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_carpeta);
        }

        [TestCleanup]
        public void Borrar()
        {
            if (Directory.Exists(_carpeta)) Directory.Delete(_carpeta, recursive: true);
        }

        private void Completar()
        {
            File.WriteAllText(Path.Combine(_carpeta, "tnsnames.ora"), Tnsnames);
            File.WriteAllText(Path.Combine(_carpeta, "sqlnet.ora"), "SSL_SERVER_DN_MATCH=yes");
            File.WriteAllBytes(Path.Combine(_carpeta, "cwallet.sso"), new byte[] { 1, 2, 3 });
        }

        [TestMethod]
        public void CarpetaInexistente_DevuelveMotivo()
        {
            Assert.IsNull(WalletOracleNube.Abrir(Path.Combine(_carpeta, "no"), out string motivo));
            StringAssert.Contains(motivo, "No existe");
        }

        [TestMethod]
        public void FaltaCwallet_DevuelveMotivoConElArchivo()
        {
            File.WriteAllText(Path.Combine(_carpeta, "tnsnames.ora"), Tnsnames);
            File.WriteAllText(Path.Combine(_carpeta, "sqlnet.ora"), "");
            Assert.IsNull(WalletOracleNube.Abrir(_carpeta, out string motivo));
            StringAssert.Contains(motivo, "cwallet.sso");
        }

        [TestMethod]
        public void WalletCompleto_LeeAliasHostYPuerto()
        {
            Completar();
            WalletOracleNube? wallet = WalletOracleNube.Abrir(_carpeta, out _);
            Assert.IsNotNull(wallet);
            CollectionAssert.AreEquivalent(new[] { "ecoaccess_high", "ecoaccess_tp" }, wallet!.Alias.ToArray());
            Assert.IsTrue(wallet.TryObtenerDestino("ECOACCESS_TP", out string host, out int puerto));
            Assert.AreEqual("adb.sa-bogota-1.oraclecloud.com", host);
            Assert.AreEqual(1522, puerto);
        }

        [TestMethod]
        public void CrearCadena_UsaAliasYWalletYEsReconocidaPorOdpNet()
        {
            Completar();
            WalletOracleNube wallet = WalletOracleNube.Abrir(_carpeta, out _)!;
            string cadena = wallet.CrearCadena("ecoaccess_tp", "ECOACCESS_P3_APP", "Clave-de-prueba-1");

            // ODP.NET debe aceptar Tns_Admin y Wallet_Location; si no, el constructor lanzaría ArgumentException.
            var partes = new OracleConnectionStringBuilder(cadena);
            Assert.AreEqual("ecoaccess_tp", partes.DataSource);
            Assert.AreEqual("ECOACCESS_P3_APP", partes.UserID);
            Assert.IsTrue(WalletOracleNube.UsaWallet(cadena));
            Assert.AreEqual(wallet.Carpeta, WalletOracleNube.CarpetaDe(cadena));
            Assert.AreEqual(("ecoaccess_tp", "ECOACCESS_P3_APP"), WalletOracleNube.Describir(cadena));
        }

        [TestMethod]
        public void CrearCadena_AliasInexistente_Lanza()
        {
            Completar();
            WalletOracleNube wallet = WalletOracleNube.Abrir(_carpeta, out _)!;
            Assert.ThrowsExactly<ArgumentException>(() => wallet.CrearCadena("otro_tp", "U", "x"));
        }

        [TestMethod]
        public void CadenaLocal_NoUsaWallet()
        {
            Assert.IsFalse(WalletOracleNube.UsaWallet("User Id=a;Password=b;Data Source=localhost:1522/PRUEBAP3"));
            Assert.IsFalse(WalletOracleNube.UsaWallet(null));
        }

        [TestMethod]
        public void CompletarCadena_SoloAlias_AgregaLaCarpetaDelWallet()
        {
            // Formato que guarda Configurar-Credenciales.ps1: alias sin carpeta, pensado para TNS_ADMIN.
            Completar();
            WalletOracleNube wallet = WalletOracleNube.Abrir(_carpeta, out _)!;
            string? cadena = wallet.CompletarCadena("User id=ECOACCESS_P3_APP;Password=Clave-de-prueba-1;data source=ecoaccess_tp");

            Assert.IsNotNull(cadena);
            Assert.AreEqual(wallet.Carpeta, WalletOracleNube.CarpetaDe(cadena));
            var partes = new OracleConnectionStringBuilder(cadena);
            Assert.AreEqual("ECOACCESS_P3_APP", partes.UserID);
            Assert.AreEqual("Clave-de-prueba-1", partes.Password);
        }

        [TestMethod]
        public void CompletarCadena_ConWallet_NoCambia()
        {
            Completar();
            WalletOracleNube wallet = WalletOracleNube.Abrir(_carpeta, out _)!;
            string original = wallet.CrearCadena("ecoaccess_tp", "U", "x");
            Assert.AreEqual(original, wallet.CompletarCadena(original));
        }

        [TestMethod]
        public void CompletarCadena_LocalVaciaOAliasAjeno_DevuelveNull()
        {
            Completar();
            WalletOracleNube wallet = WalletOracleNube.Abrir(_carpeta, out _)!;
            Assert.IsNull(wallet.CompletarCadena(null));
            Assert.IsNull(wallet.CompletarCadena("User Id=a;Password=b;Data Source=localhost:1522/PRUEBAP3"));
            Assert.IsNull(wallet.CompletarCadena("User Id=a;Password=b;Data Source=otro_tp"));
        }
    }
}
