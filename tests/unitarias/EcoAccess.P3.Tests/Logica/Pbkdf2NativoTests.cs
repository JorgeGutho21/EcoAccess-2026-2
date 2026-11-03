using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{

    /// <summary>La versión nativa es solo más rápida: si cambiara el resultado, nadie podría volver a iniciar sesión.</summary>
    [TestClass]
    public sealed class Pbkdf2NativoTests
    {
        [TestMethod]
        public void CoincideConElVectorPublicado()
        {
            // RFC 7914, sección 11: PBKDF2-HMAC-SHA-256, «passwd», «salt», 1 iteración, 64 bytes (primeros 16).
            byte[] r = Compat.Pbkdf2(Encoding.UTF8.GetBytes("passwd"), Encoding.UTF8.GetBytes("salt"), 1, HashAlgorithmName.SHA256, 64);

            Assert.AreEqual("55AC046E56E3089FEC1691C22544B605", Compat.ToHexString(r).Substring(0, 32));
        }

        [TestMethod]
        [DataRow(1000, 32)]
        [DataRow(4096, 64)]
        public void CoincideConLaVersionAdministrada(int iteraciones, int longitud)
        {
            byte[] clave = Encoding.UTF8.GetBytes("contraseña de prueba"), sal = Compat.RandomGetBytes(16);

            CollectionAssert.AreEqual(Compat.Pbkdf2Administrado(clave, sal, iteraciones, HashAlgorithmName.SHA256, longitud),
                Compat.Pbkdf2(clave, sal, iteraciones, HashAlgorithmName.SHA256, longitud));
        }

        [TestMethod]
        public void UnHashExistenteSigueVerificando()
        {
            var hasher = new HasherPbkdf2();
            byte[] sal = Compat.RandomGetBytes(16);
            byte[] guardado = Compat.Pbkdf2Administrado(Encoding.UTF8.GetBytes("ciudadano-ficticio-2026"), sal, 100_000, HashAlgorithmName.SHA256, EcoAccess.P3.Entidades.CredencialAcceso.LongitudHash);

            Assert.IsTrue(hasher.Verificar("ciudadano-ficticio-2026", sal, 100_000, guardado));
            Assert.IsFalse(hasher.Verificar("otra", sal, 100_000, guardado));
        }

        [TestMethod]
        public void SeImprimeCuantoTardaConLasIteracionesReales()
        {
            byte[] clave = Encoding.UTF8.GetBytes("clave"), sal = Compat.RandomGetBytes(16);
            var nativo = Stopwatch.StartNew();
            Compat.Pbkdf2(clave, sal, HasherPbkdf2.IteracionesRecomendadas, HashAlgorithmName.SHA256, 32);
            nativo.Stop();
            var administrado = Stopwatch.StartNew();
            Compat.Pbkdf2Administrado(clave, sal, HasherPbkdf2.IteracionesRecomendadas, HashAlgorithmName.SHA256, 32);
            administrado.Stop();

            Console.WriteLine($"PBKDF2 600000 iteraciones: nativo {nativo.ElapsedMilliseconds} ms, administrado {administrado.ElapsedMilliseconds} ms");
            Assert.IsTrue(nativo.ElapsedMilliseconds <= administrado.ElapsedMilliseconds);
        }
    }
}
