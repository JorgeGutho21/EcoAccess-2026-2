using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Bot.Transportes;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class CopiadorRegistroSeguroTests
    {
        [TestMethod]
        public async Task DiscoFallidoNoImpideDrenarLaSalidaDelProceso()
        {
            using var entrada = new MemoryStream(new byte[50000]); using var salida = new DiscoFallido();
            Assert.IsFalse(await CopiadorRegistroSeguro.CopiarAsync(entrada, salida));
            Assert.AreEqual(entrada.Length, entrada.Position, "El hijo no se retiene por un pipe lleno cuando falla el disco.");
        }
        [TestMethod]
        public async Task CadaBloqueQuedaLegibleMientrasElProcesoSigueActivo()
        {
            string ruta = Path.Combine(Path.GetTempPath(), "copiador-" + Guid.NewGuid().ToString("N") + ".log");
            try
            {
                var tubo = new System.IO.Pipes.AnonymousPipeServerStream(System.IO.Pipes.PipeDirection.In);
                using var escritor = new System.IO.Pipes.AnonymousPipeClientStream(System.IO.Pipes.PipeDirection.Out, tubo.ClientSafePipeHandle);
                using var salida = new FileStream(ruta, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                Task<bool> copia = CopiadorRegistroSeguro.CopiarAsync(tubo, salida);
                escritor.Write(Encoding.UTF8.GetBytes("Identidad comprobada\n")); escritor.Flush();
                string leido = string.Empty;
                for (int i = 0; i < 100 && leido.Length == 0; i++)
                {
                    await Task.Delay(20);
                    using var lector = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    leido = new StreamReader(lector).ReadToEnd();
                }
                Assert.AreEqual("Identidad comprobada\n", leido, "Una línea corta no espera al cierre del proceso.");
                escritor.Dispose();
                Assert.IsTrue(await copia);
                tubo.Dispose();
            }
            finally { File.Delete(ruta); }
        }
        private sealed class DiscoFallido : MemoryStream
        {
            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                Task.FromException(new IOException("Disco ficticio lleno."));
        }
    }
}
