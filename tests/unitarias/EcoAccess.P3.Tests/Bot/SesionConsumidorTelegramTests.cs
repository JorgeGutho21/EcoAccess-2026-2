using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Bot.Telegram;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class SesionConsumidorTelegramTests
    {
        [TestMethod]
        public void MismaIdentidadNoConsumeDosVecesYReinicioRecuperaBloqueo()
        {
            string carpeta = Path.Combine(Path.GetTempPath(), "ecoaccess-consumidor-" + Guid.NewGuid().ToString("N"));
            try
            {
                using (var uno = new SesionConsumidorTelegram(123456, carpeta))
                {
                    Assert.ThrowsExactly<IOException>(() => new SesionConsumidorTelegram(123456, carpeta));
                    using var otroBot = new SesionConsumidorTelegram(654321, carpeta);
                }
                using var reinicio = new SesionConsumidorTelegram(123456, carpeta);
                Assert.IsTrue(File.Exists(Path.Combine(carpeta, "123456.lock")), "El cierre no borra ni cambia la identidad del bloqueo.");
            }
            finally
            {
                string absoluta = Path.GetFullPath(carpeta);
                if (Path.GetDirectoryName(absoluta) == Compat.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()))
                    && Path.GetFileName(absoluta).StartsWith("ecoaccess-consumidor-", StringComparison.Ordinal)) Directory.Delete(absoluta, true);
            }
        }
    }
}
