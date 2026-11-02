using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using EcoAccess.P3.Bot.Dispositivos;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class ColaPaquetesDispositivoTests
    {
        private readonly string _directorio = Path.Combine(Path.GetTempPath(), "ecoaccess-cola-test-" + Guid.NewGuid().ToString("N"));

        [TestCleanup]
        public void LimpiarSoloCarpetaCreadaPorEstaPrueba()
        {
            string ruta = Path.GetFullPath(_directorio);
            if (Path.GetDirectoryName(ruta) != Compat.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()))
                || !Path.GetFileName(ruta).StartsWith("ecoaccess-cola-test-", StringComparison.Ordinal)) throw new InvalidOperationException("Ruta de prueba inesperada.");
            if (Directory.Exists(ruta)) Directory.Delete(ruta, recursive: true);
        }

        [TestMethod]
        public void ReinicioConservaOrdenYBytesYAgregarNoDependeDelBufferExterno()
        {
            var cola = new ColaPaquetesDispositivo(_directorio + Path.DirectorySeparatorChar);
            byte[] primero = new byte[] { 1, 2, 3 }; Assert.IsTrue(cola.Agregar(primero)); primero[0] = 99;
            Assert.IsTrue(cola.Agregar(new byte[] { 4, 5 }));
            var reiniciada = new ColaPaquetesDispositivo(_directorio);
            var p = reiniciada.Primero()!; CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, p.Datos);
            reiniciada.Confirmar(p); CollectionAssert.AreEqual(new byte[] { 4, 5 }, reiniciada.Primero()!.Datos);
            Assert.AreEqual(1, reiniciada.Pendientes);
        }

        [TestMethod]
        public void DuplicadosNoCrecenYRevisionNoSeReencolaNiSeBorra()
        {
            var cola = new ColaPaquetesDispositivo(_directorio);
            Assert.IsTrue(cola.Agregar(new byte[] { 1 })); Assert.IsTrue(cola.Agregar(new byte[] { 1 })); Assert.AreEqual(1, cola.Pendientes);
            cola.ConservarParaRevision(cola.Primero()!);
            var reiniciada = new ColaPaquetesDispositivo(_directorio);
            Assert.AreEqual(1, reiniciada.EnRevision); Assert.IsFalse(reiniciada.Agregar(new byte[] { 1 })); Assert.IsNull(reiniciada.Primero());
        }

        [TestMethod]
        public void PaqueteAlteradoFallaCerradoYNombreNoPuedeSalirDelDirectorio()
        {
            var cola = new ColaPaquetesDispositivo(_directorio); cola.Agregar(new byte[] { 1, 2, 3 });
            var p = cola.Primero()!; File.WriteAllBytes(Path.Combine(_directorio, p.Nombre), new byte[] { 9, 8, 7 });
            Assert.ThrowsExactly<InvalidDataException>(() => cola.Primero());
            Assert.IsTrue(File.Exists(Path.Combine(_directorio, p.Nombre)));
            Assert.ThrowsExactly<ArgumentException>(() => cola.Confirmar(new PaquetePendiente("../ajeno.json", Array.Empty<byte>())));
            Assert.ThrowsExactly<ArgumentException>(() => cola.ConservarParaRevision(new PaquetePendiente("../ajeno.json", Array.Empty<byte>())));
        }

        [TestMethod]
        public void CapacidadAgotadaRechazaSinSobrescribir()
        {
            Directory.CreateDirectory(_directorio);
            byte[] dato = new byte[] { 1 }; string hash = Compat.ToHexString(Compat.Sha256(dato));
            for (int i = 1; i <= ColaPaquetesDispositivo.MaximoPendientes; i++)
                File.WriteAllBytes(Path.Combine(_directorio, i.ToString("D19") + "-" + hash + ".json"), dato);
            var cola = new ColaPaquetesDispositivo(_directorio);
            Assert.IsFalse(cola.Agregar(new byte[] { 2 })); Assert.AreEqual(ColaPaquetesDispositivo.MaximoPendientes, cola.Pendientes);
            Assert.IsTrue(cola.Agregar(new byte[] { 1 }), "Repetir un paquete ya guardado no necesita otro espacio.");
        }

        [TestMethod]
        public void CuerpoExcesivoNoSeGuarda()
        {
            var cola = new ColaPaquetesDispositivo(_directorio);
            Assert.ThrowsExactly<ArgumentException>(() => cola.Agregar(new byte[4097])); Assert.AreEqual(0, cola.Pendientes);
        }
    }
}
