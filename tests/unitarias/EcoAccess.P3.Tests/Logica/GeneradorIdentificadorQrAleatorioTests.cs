using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{

    [TestClass]
    public sealed class GeneradorIdentificadorQrAleatorioTests
    {
        [TestMethod]
        public void Generar_Siempre_Devuelve22CaracteresBase64Url()
        {
            string qr = new GeneradorIdentificadorQrAleatorio().Generar();

            Assert.AreEqual(22, qr.Length);
            Assert.IsTrue(qr.All(c => Compat.IsAsciiLetterOrDigit(c) || c is '-' or '_'), qr);
        }

        [TestMethod]
        public void Generar_Siempre_EsAceptadoPorContenedor()
        {
            string qr = new GeneradorIdentificadorQrAleatorio().Generar();

            var caneca = new Contenedor(1, "Caneca de prueba", qr, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.AreEqual(qr, caneca.IdentificadorQr);
        }

        [TestMethod]
        public void Generar_MilVeces_NoRepiteIdentificadores()
        {
            var generador = new GeneradorIdentificadorQrAleatorio();

            var generados = Enumerable.Range(0, 1000).Select(_ => generador.Generar()).ToHashSet();

            Assert.HasCount(1000, generados);
        }
    }
}
