using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Bot.Transportes;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class ControlParadaLocalTests
    {
        [TestMethod]
        public async Task ArchivoDeSesionSolicitaCancelacionSinEliminarloNiMatarProcesos()
        {
            string padre = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework");
            string carpeta = Path.Combine(padre, "pruebas-parada-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(carpeta);
            try
            {
                using var parar = new CancellationTokenSource(); string archivo = Path.Combine(carpeta, "simulador.parar");
                using var control = new ControlParadaLocal(parar, archivo);
                Assert.IsFalse(parar.IsCancellationRequested);
                File.WriteAllText(archivo, "parar");
                // Mide que la señal llega, no su latencia: el temporizador usa el grupo de hilos, que otras pruebas pueden ocupar.
                await Task.Delay(Timeout.InfiniteTimeSpan, parar.Token).ContinueWith(_ => { }, TaskScheduler.Default).WaitAsync(TimeSpan.FromSeconds(30));
                Assert.IsTrue(parar.IsCancellationRequested); Assert.IsTrue(File.Exists(archivo));
            }
            finally
            {
                string absoluta = Path.GetFullPath(carpeta);
                if (Path.GetDirectoryName(absoluta) != Compat.TrimEndingDirectorySeparator(Path.GetFullPath(padre)) || !Path.GetFileName(absoluta).StartsWith("pruebas-parada-", StringComparison.Ordinal))
                    throw new InvalidOperationException("Ruta inesperada del fixture de parada.");
                Directory.Delete(absoluta, true);
            }
        }
    }
}
