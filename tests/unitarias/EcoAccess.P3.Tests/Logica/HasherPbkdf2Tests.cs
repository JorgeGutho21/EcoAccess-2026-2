using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{

    // Se usa el mínimo de iteraciones para que las pruebas sean rápidas; la aplicación usa las recomendadas.
    [TestClass]
    public sealed class HasherPbkdf2Tests
    {
        private readonly HasherPbkdf2 _hasher = new(CredencialAcceso.IteracionesMinimas);

        [TestMethod]
        public void Constructor_PorDefecto_UsaIteracionesRecomendadas()
        {
            Assert.AreEqual(HasherPbkdf2.IteracionesRecomendadas, new HasherPbkdf2().Iteraciones);
        }

        [TestMethod]
        public void Constructor_IteracionesBajoElMinimo_LanzaExcepcion()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HasherPbkdf2(CredencialAcceso.IteracionesMinimas - 1));
        }

        [TestMethod]
        public void Calcular_DevuelveLongitudesDeLaCredencial()
        {
            HashContrasena calculado = _hasher.Calcular("contraseña segura");

            Assert.HasCount(CredencialAcceso.LongitudHash, calculado.Hash);
            Assert.HasCount(CredencialAcceso.LongitudSal, calculado.Sal);
            Assert.AreEqual(CredencialAcceso.IteracionesMinimas, calculado.Iteraciones);
        }

        [TestMethod]
        public void Calcular_DosVeces_UsaSalesDistintasYHashesDistintos()
        {
            HashContrasena a = _hasher.Calcular("contraseña segura");
            HashContrasena b = _hasher.Calcular("contraseña segura");

            CollectionAssert.AreNotEqual(a.Sal, b.Sal);
            CollectionAssert.AreNotEqual(a.Hash, b.Hash);
        }

        [TestMethod]
        public void Verificar_MismaContrasenaYSal_DevuelveTrue()
        {
            HashContrasena calculado = _hasher.Calcular("contraseña segura");

            Assert.IsTrue(_hasher.Verificar("contraseña segura", calculado.Sal, calculado.Iteraciones, calculado.Hash));
        }

        [TestMethod]
        public void Verificar_OtraContrasena_DevuelveFalse()
        {
            HashContrasena calculado = _hasher.Calcular("contraseña segura");

            Assert.IsFalse(_hasher.Verificar("Contraseña segura", calculado.Sal, calculado.Iteraciones, calculado.Hash));
        }

        [TestMethod]
        public void Verificar_HashConUnByteCambiado_DevuelveFalse()
        {
            HashContrasena calculado = _hasher.Calcular("contraseña segura");
            byte[] alterado = calculado.Hash;
            alterado[^1] ^= 1;

            Assert.IsFalse(_hasher.Verificar("contraseña segura", calculado.Sal, calculado.Iteraciones, alterado));
        }

        [TestMethod]
        public void Verificar_RespetaIteracionesGuardadas_AunqueElHasherUseOtras()
        {
            HashContrasena calculado = _hasher.Calcular("contraseña segura");
            var otro = new HasherPbkdf2(CredencialAcceso.IteracionesMinimas + 1);

            Assert.IsTrue(otro.Verificar("contraseña segura", calculado.Sal, calculado.Iteraciones, calculado.Hash));
        }
    }
}
