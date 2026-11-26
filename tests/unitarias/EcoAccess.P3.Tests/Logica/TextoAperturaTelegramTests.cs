using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>Mensajes cortos de Telegram tras una apertura confirmada (acompañan la imagen de Eco).</summary>
    [TestClass]
    public sealed class TextoAperturaTelegramTests
    {
        private static string RaizRepositorio()
        {
            var d = new System.IO.DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory);
            while (d is not null && !System.IO.File.Exists(System.IO.Path.Combine(d.FullName, "EcoAccess.P3.sln"))) d = d.Parent;
            return d?.FullName ?? throw new System.InvalidOperationException("No se encontró la raíz del repositorio.");
        }

        [TestMethod]
        public void UnaCaneca_ColorYSieteSegundos()
        {
            Assert.AreEqual("🟢 Caneca verde abierta.\nTienes 7 segundos para desechar tu residuo.", ServicioConversacionTelegram.TextoAbierta(new[] { CategoriaMaterial.OrganicoAprovechable }, "7"));
            Assert.AreEqual("⚪ Caneca blanca abierta.\nTienes 7 segundos para desechar tu residuo.", ServicioConversacionTelegram.TextoAbierta(new[] { CategoriaMaterial.Aprovechable }, "7"));
            Assert.AreEqual("⚫ Caneca negra abierta.\nTienes 7 segundos para desechar tu residuo.", ServicioConversacionTelegram.TextoAbierta(new[] { CategoriaMaterial.NoAprovechable }, "7"));
        }

        [TestMethod]
        public void DosYTresCanecas()
        {
            Assert.AreEqual("🟢⚪ Canecas verde y blanca abiertas.\nTienes 14 segundos.", ServicioConversacionTelegram.TextoAbierta(new[] { CategoriaMaterial.OrganicoAprovechable, CategoriaMaterial.Aprovechable }, "14"));
            Assert.AreEqual("⚪⚫ Canecas blanca y negra abiertas.\nTienes 14 segundos.", ServicioConversacionTelegram.TextoAbierta(new[] { CategoriaMaterial.Aprovechable, CategoriaMaterial.NoAprovechable }, "14"));
            Assert.AreEqual("🟢⚪⚫ Las tres canecas están abiertas.\nTienes 21 segundos.", ServicioConversacionTelegram.TextoAbierta(new[] { CategoriaMaterial.OrganicoAprovechable, CategoriaMaterial.Aprovechable, CategoriaMaterial.NoAprovechable }, "21"));
        }

        [TestMethod]
        public void ImagenDeEcoSegunLasCanecas()
        {
            Assert.AreEqual("eco_verde", ServicioConversacionTelegram.ImagenCanecas(new[] { CategoriaMaterial.OrganicoAprovechable }));
            Assert.AreEqual("eco_blanca", ServicioConversacionTelegram.ImagenCanecas(new[] { CategoriaMaterial.Aprovechable }));
            Assert.AreEqual("eco_negra", ServicioConversacionTelegram.ImagenCanecas(new[] { CategoriaMaterial.NoAprovechable }));
            Assert.AreEqual("eco_blanca_negra", ServicioConversacionTelegram.ImagenCanecas(new[] { CategoriaMaterial.NoAprovechable, CategoriaMaterial.Aprovechable }));
            Assert.AreEqual("eco_verde_blanca", ServicioConversacionTelegram.ImagenCanecas(new[] { CategoriaMaterial.Aprovechable, CategoriaMaterial.OrganicoAprovechable }));
            Assert.AreEqual("eco_verde_negra", ServicioConversacionTelegram.ImagenCanecas(new[] { CategoriaMaterial.OrganicoAprovechable, CategoriaMaterial.NoAprovechable }));
            Assert.AreEqual("eco_canecas", ServicioConversacionTelegram.ImagenCanecas(new[] { CategoriaMaterial.OrganicoAprovechable, CategoriaMaterial.Aprovechable, CategoriaMaterial.NoAprovechable }));
            foreach (var clave in new[] { "eco_verde", "eco_blanca", "eco_negra", "eco_verde_blanca", "eco_verde_negra", "eco_blanca_negra", "eco_canecas" })
                Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(RaizRepositorio(), "assets", "imagenes", "bot", clave + ".jpg")), "Falta la imagen " + clave);
        }
    }
}
