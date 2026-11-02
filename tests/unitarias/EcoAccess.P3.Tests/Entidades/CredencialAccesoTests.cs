using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Tests.Entidades
{

    [TestClass]
    public sealed class CredencialAccesoTests
    {
        private static readonly DateTime FechaUtc = new(2026, 10, 24, 15, 0, 0, DateTimeKind.Utc);

        private static byte[] Bytes(int longitud, byte valor = 1) => Enumerable.Repeat(valor, longitud).ToArray();

        private static CredencialAcceso Crear(DateTime? bloqueadaHasta = null) =>
            new(7, Bytes(CredencialAcceso.LongitudHash), Bytes(CredencialAcceso.LongitudSal), CredencialAcceso.IteracionesMinimas,
                FechaUtc, intentosFallidos: 2, bloqueadaHasta);

        [TestMethod]
        public void Constructor_DatosValidos_GuardaValores()
        {
            CredencialAcceso credencial = Crear();

            Assert.AreEqual(7, credencial.IdUsuario);
            Assert.AreEqual(CredencialAcceso.IteracionesMinimas, credencial.Iteraciones);
            Assert.AreEqual(2, credencial.IntentosFallidos);
            Assert.AreEqual(FechaUtc, credencial.FechaCambioUtc);
            Assert.IsNull(credencial.BloqueadaHastaUtc);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void Constructor_IdNoPositivo_LanzaExcepcion(int id)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                new CredencialAcceso(id, Bytes(32), Bytes(16), CredencialAcceso.IteracionesMinimas, FechaUtc));
        }

        [TestMethod]
        [DataRow(31, 16)]
        [DataRow(32, 15)]
        [DataRow(0, 16)]
        public void Constructor_LongitudIncorrecta_LanzaExcepcion(int largoHash, int largoSal)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new CredencialAcceso(1, Bytes(largoHash), Bytes(largoSal), CredencialAcceso.IteracionesMinimas, FechaUtc));
        }

        [TestMethod]
        public void Constructor_HashNulo_LanzaExcepcion()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new CredencialAcceso(1, null!, Bytes(16), CredencialAcceso.IteracionesMinimas, FechaUtc));
        }

        [TestMethod]
        public void Constructor_IteracionesBajoElMinimo_LanzaExcepcion()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                new CredencialAcceso(1, Bytes(32), Bytes(16), CredencialAcceso.IteracionesMinimas - 1, FechaUtc));
        }

        [TestMethod]
        public void Constructor_IntentosNegativos_LanzaExcepcion()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                new CredencialAcceso(1, Bytes(32), Bytes(16), CredencialAcceso.IteracionesMinimas, FechaUtc, intentosFallidos: -1));
        }

        [TestMethod]
        public void Constructor_FechaNoUtc_LanzaExcepcion()
        {
            var local = new DateTime(2026, 10, 24, 15, 0, 0, DateTimeKind.Local);

            Assert.ThrowsExactly<ArgumentException>(() =>
                new CredencialAcceso(1, Bytes(32), Bytes(16), CredencialAcceso.IteracionesMinimas, local));
            Assert.ThrowsExactly<ArgumentException>(() =>
                new CredencialAcceso(1, Bytes(32), Bytes(16), CredencialAcceso.IteracionesMinimas, FechaUtc, 0, local));
        }

        [TestMethod]
        public void HashYSal_SonCopias_NoPermitenAlterarLaCredencial()
        {
            byte[] original = Bytes(32, 5);
            var credencial = new CredencialAcceso(1, original, Bytes(16), CredencialAcceso.IteracionesMinimas, FechaUtc);

            original[0] = 99;
            credencial.Hash[1] = 99;
            credencial.Sal[0] = 99;

            CollectionAssert.AreEqual(Bytes(32, 5), credencial.Hash);
            CollectionAssert.AreEqual(Bytes(16), credencial.Sal);
        }

        [TestMethod]
        public void EstaBloqueada_SegunFechaDeBloqueo()
        {
            DateTime hasta = FechaUtc.AddMinutes(15);
            CredencialAcceso bloqueada = Crear(hasta);

            Assert.IsTrue(bloqueada.EstaBloqueada(FechaUtc));
            Assert.IsFalse(bloqueada.EstaBloqueada(hasta));
            Assert.IsFalse(bloqueada.EstaBloqueada(hasta.AddSeconds(1)));
            Assert.IsFalse(Crear().EstaBloqueada(FechaUtc));
        }
    }
}
