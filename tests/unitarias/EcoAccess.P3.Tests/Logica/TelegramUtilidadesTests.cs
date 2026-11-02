using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{

    [TestClass]
    public sealed class TelegramUtilidadesTests
    {
        private static readonly DateTime Ahora = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        [TestMethod]
        public void GeneradorProduceBase64UrlDe43CaracteresDistintos()
        {
            var generador = new GeneradorTokensAleatorio();
            var tokens = Enumerable.Range(0, 50).Select(_ => generador.Generar()).ToList();

            Assert.IsTrue(tokens.All(t => t.Length == 43 && t.All(c => Compat.IsAsciiLetterOrDigit(c) || c is '-' or '_')));
            Assert.AreEqual(tokens.Count, tokens.Distinct().Count());
        }

        [TestMethod]
        public void LimitadorCortaAlMaximoYSeReiniciaConLaVentana()
        {
            var limitador = new LimitadorUsoEnMemoria(3, TimeSpan.FromMinutes(1));

            Assert.IsTrue(limitador.Permitir("a", Ahora));
            Assert.IsTrue(limitador.Permitir("a", Ahora));
            Assert.IsTrue(limitador.Permitir("a", Ahora));
            Assert.IsFalse(limitador.Permitir("a", Ahora.AddSeconds(30)));
            Assert.IsTrue(limitador.Permitir("b", Ahora), "Cada clave lleva su propia cuenta.");
            Assert.IsTrue(limitador.Permitir("a", Ahora.AddMinutes(1)));
        }

        [TestMethod]
        public void LimitadorPodaLasVentanasVencidasCuandoHayDemasiadasClaves()
        {
            var limitador = new LimitadorUsoEnMemoria(1, TimeSpan.FromMinutes(1));
            DateTime despues = Ahora.AddMinutes(2);
            for (int i = 0; i < LimitadorUsoEnMemoria.ClavesAntesDePodar; i++)
            {
                limitador.Permitir($"clave-{i}", Ahora);
            }

            Assert.IsTrue(limitador.Permitir("vigente", despues));
            Assert.IsGreaterThan(LimitadorUsoEnMemoria.ClavesAntesDePodar, limitador.Claves);

            // La poda se revisa cada cierto número de llamadas; basta con hacer más llamadas que ese intervalo.
            for (int i = 0; i < 1024 && limitador.Claves > 2; i++)
            {
                limitador.Permitir("nueva", despues);
            }

            Assert.AreEqual(2, limitador.Claves, "Solo quedan las ventanas vigentes.");
            Assert.IsFalse(limitador.Permitir("vigente", despues), "Podar no reinicia una ventana vigente que no se volvió a usar.");
        }

        [TestMethod]
        public void LimitadorRechazaConfiguracionInvalida()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new LimitadorUsoEnMemoria(0, TimeSpan.FromMinutes(1)));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new LimitadorUsoEnMemoria(1, TimeSpan.Zero));
        }

        [TestMethod]
        public void EnlaceDeCanecaUsaElParametroStart()
        {
            Uri enlace = EnlaceTelegram.ParaCaneca("EcoAccessBot", "Ab_9-xyz");
            Assert.AreEqual("https://t.me/EcoAccessBot?start=Ab_9-xyz", enlace.AbsoluteUri);
        }

        [TestMethod]
        [DataRow("eco", "abc")]
        [DataRow("EcoAccess", "abc")]
        [DataRow("EcoAccessBot", "con espacio")]
        [DataRow("EcoAccessBot", "")]
        [DataRow("EcoAccessBot\n", "abc")]
        [DataRow("EcoAccessBot", "abc\n")]
        public void EnlaceDeCanecaRechazaBotOIdentificadorInvalidos(string bot, string qr) =>
            Assert.ThrowsExactly<ArgumentException>(() => EnlaceTelegram.ParaCaneca(bot, qr));

        [TestMethod]
        public void EnlaceDeRegistroExigeHttpsYEscapaElToken()
        {
            Uri enlace = EnlaceTelegram.ParaRegistro(new Uri("https://ecoaccess.example/registro"), "a b");
            Assert.AreEqual("https://ecoaccess.example/registro?t=a%20b", enlace.AbsoluteUri);
            Assert.ThrowsExactly<ArgumentException>(() => EnlaceTelegram.ParaRegistro(new Uri("http://ecoaccess.example/registro"), "x"));
        }

        [TestMethod]
        public void StartSeparaElParametro()
        {
            Assert.IsTrue(EnlaceTelegram.EsStart("/start QR-1", out string? p1));
            Assert.AreEqual("QR-1", p1);
            Assert.IsTrue(EnlaceTelegram.EsStart("/start@EcoAccessBot", out string? p2));
            Assert.IsNull(p2);
            Assert.IsTrue(EnlaceTelegram.EsStart("/start <script>", out string? p3));
            Assert.IsNull(p3, "Un parámetro con caracteres no admitidos se descarta.");
            Assert.IsFalse(EnlaceTelegram.EsStart("/ayuda", out _));
            Assert.IsFalse(EnlaceTelegram.EsStart(null, out _));
        }

        [TestMethod]
        public void ComandoQuitaElNombreDelBotYPasaAMinusculas()
        {
            Assert.AreEqual("/cancelar", EnlaceTelegram.Comando("/CANCELAR@EcoAccessBot ya"));
            Assert.IsNull(EnlaceTelegram.Comando("hola"));
            Assert.IsNull(EnlaceTelegram.Comando("  "));
        }
    }
}
