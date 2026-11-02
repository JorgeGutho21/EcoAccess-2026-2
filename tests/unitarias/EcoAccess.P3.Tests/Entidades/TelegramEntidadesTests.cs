using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Tests.Entidades
{

    [TestClass]
    public sealed class TelegramEntidadesTests
    {
        private static readonly DateTime Ahora = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        private static readonly string Hash = new('a', TokenVinculacion.LongitudHash);

        [TestMethod]
        public void VinculoActivoHastaLaBaja()
        {
            Assert.IsTrue(new VinculoTelegram(1, 2, 3, Ahora).Activo);
            Assert.IsFalse(new VinculoTelegram(1, 2, 3, Ahora, Ahora.AddDays(1)).Activo);
        }

        [TestMethod]
        public void VinculoRechazaIdTelegramNoPositivoYBajaAnterior()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new VinculoTelegram(1, 2, 0, Ahora));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new VinculoTelegram(1, 2, -5, Ahora));
            Assert.ThrowsExactly<ArgumentException>(() => new VinculoTelegram(1, 2, 3, Ahora, Ahora.AddSeconds(-1)));
        }

        [TestMethod]
        public void TokenNuevoVenceEnDiezMinutos()
        {
            var token = new TokenVinculacion(Hash, 10, 10, Ahora);

            Assert.AreEqual(Ahora + TokenVinculacion.Vigencia, token.VenceUtc);
            Assert.IsTrue(token.EstaVigente(Ahora.AddMinutes(9)));
            Assert.IsFalse(token.EstaVigente(token.VenceUtc));
        }

        [TestMethod]
        public void TokenUsadoNoEstaVigente()
        {
            var token = new TokenVinculacion(Hash, 10, 10, Ahora, Ahora.AddMinutes(10), Ahora.AddMinutes(1));
            Assert.IsFalse(token.EstaVigente(Ahora.AddMinutes(2)));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("ABCDEF")]
        [DataRow("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
        [DataRow("gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg")]
        public void TokenRechazaHashQueNoEsHexMinusculoDe64(string hash) =>
            Assert.ThrowsExactly<ArgumentException>(() => new TokenVinculacion(hash, 10, 10, Ahora));

        [TestMethod]
        public void TokenRechazaChatCeroYVencimientoNoPosterior()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TokenVinculacion(Hash, 10, 0, Ahora));
            Assert.ThrowsExactly<ArgumentException>(() => new TokenVinculacion(Hash, 10, 10, Ahora, Ahora, null));
        }

        [TestMethod]
        public void ConversacionRecorreElFlujoCompleto()
        {
            var c = new ConversacionTelegram(7, 7, Ahora);
            Assert.AreEqual(EstadoConversacion.SinCaneca, c.Estado);

            c.IdentificarCaneca(4, 9, Ahora);
            c.ProponerMaterial(2, Ahora);
            c.ProponerMaterial(3, Ahora);
            Assert.AreEqual(EstadoConversacion.EsperandoConfirmacion, c.Estado);
            Assert.AreEqual(3, c.IdMaterialPropuesto);

            c.MarcarOrdenEmitida(Ahora.AddMinutes(1));
            Assert.AreEqual(EstadoConversacion.OrdenEmitida, c.Estado);
            Assert.AreEqual(Ahora.AddMinutes(1), c.FechaActualizacionUtc);

            c.Reiniciar(Ahora);
            Assert.AreEqual(EstadoConversacion.SinCaneca, c.Estado);
            Assert.IsNull(c.IdContenedor);
            Assert.IsNull(c.IdInteraccion);
        }

        [TestMethod]
        public void InvitadoNoPuedeProponerMaterial()
        {
            var c = new ConversacionTelegram(7, 7, Ahora);
            c.IdentificarCaneca(4, null, Ahora);
            Assert.ThrowsExactly<InvalidOperationException>(() => c.ProponerMaterial(2, Ahora));
        }

        [TestMethod]
        public void NoSeEmiteOrdenSinConfirmar()
        {
            var c = new ConversacionTelegram(7, 7, Ahora);
            c.IdentificarCaneca(4, 9, Ahora);
            Assert.ThrowsExactly<InvalidOperationException>(() => c.MarcarOrdenEmitida(Ahora));
        }

        [TestMethod]
        public void ReconstruccionExigeDatosCoherentesConElEstado()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new ConversacionTelegram(7, 7, EstadoConversacion.SinCaneca, 4, null, null, Ahora, 0));
            Assert.ThrowsExactly<ArgumentException>(() =>
                new ConversacionTelegram(7, 7, EstadoConversacion.EsperandoConfirmacion, 4, 9, null, Ahora, 0));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                new ConversacionTelegram(7, 7, EstadoConversacion.SinCaneca, null, null, null, Ahora, -1));

            var c = new ConversacionTelegram(7, 7, EstadoConversacion.OrdenEmitida, 4, 9, 2, Ahora, 3);
            Assert.AreEqual(3, c.Version);
        }
    }
}
