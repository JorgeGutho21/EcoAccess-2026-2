using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// El QR oficial de la estación es definitivo: estas pruebas fallan si el archivo cambia o si deja de abrir el bot
    /// con el identificador esperado. Se decodifica con ZXing.Net, independiente del generador del proyecto.
    /// </summary>
    [TestClass]
    public class QrOficialTests
    {
        private static string Ruta() => Path.Combine(Raiz(), "assets", "identidad", "QR_Caneca_piloto.png");

        [TestMethod]
        public void ElArchivoOficialNoCambio()
        {
            using var sha = SHA256.Create();
            string hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Ruta()))).Replace("-", "").ToLowerInvariant();
            Assert.AreEqual(EnlaceTelegram.Sha256QrOficial, hash, "El QR oficial no debe regenerarse ni reemplazarse.");
        }

        [TestMethod]
        public void ElQrOficialAbreElBotConElIdentificadorDeLaEstacion()
        {
            using var imagen = new Bitmap(Ruta());
            var resultado = new ZXing.BarcodeReader().Decode(imagen);
            Assert.IsNotNull(resultado, "ZXing no pudo leer el QR oficial.");
            Assert.AreEqual(EnlaceTelegram.ParaCaneca(EnlaceTelegram.NombreBotPiloto, EnlaceTelegram.IdentificadorQrOficial).ToString(), resultado.Text);
        }

        [TestMethod]
        public void LaPresentacionIncrustaElArchivoOficialYNoGeneraOtro()
        {
            string csproj = File.ReadAllText(Path.Combine(Raiz(), "src", "EcoAccess.P3.Presentacion", "EcoAccess.P3.Presentacion.csproj"));
            StringAssert.Contains(csproj, @"assets\identidad\QR_Caneca_piloto.png");
            string explorar = File.ReadAllText(Path.Combine(Raiz(), "src", "EcoAccess.P3.Presentacion", "FormExplorarEcoAccess.cs"));
            StringAssert.Contains(explorar, "RecursosEco.QrOficialPng()");
            Assert.IsFalse(explorar.Contains("CodigoQrCaneca.Png("), "La vista ciudadana no debe generar un QR distinto del oficial.");
        }

        private static string Raiz()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "EcoAccess.P3.sln"))) dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("No se encontró EcoAccess.P3.sln.");
        }
    }
}
