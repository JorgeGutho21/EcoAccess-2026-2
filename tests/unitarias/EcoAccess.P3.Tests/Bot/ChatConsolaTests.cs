using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class ChatConsolaTests
    {
        [TestMethod]
        public async Task Linea_SeConvierteEnMensajePrivado()
        {
            var salida = new StringWriter();
            var chat = new ChatConsola(new StringReader("  /start  \n"), salida, () => { });

            UpdateRecibido update = (await chat.ObtenerUpdatesAsync(0, TimeSpan.Zero, CancellationToken.None)).Single();

            Assert.IsNotNull(update.Mensaje);
            Assert.AreEqual("/start", update.Mensaje.Texto);
            Assert.IsTrue(update.Mensaje.ChatPrivado);
            Assert.AreEqual(ChatConsola.UsuarioSimulado, update.Mensaje.TelegramUserId);
            Assert.AreEqual(update.UpdateId, update.Mensaje.UpdateId);
            Assert.IsGreaterThan(1_000_000_000_000L, update.UpdateId);
        }

        [TestMethod]
        public async Task NumeroDeBoton_PulsaElBotonDelUltimoMensaje()
        {
            var salida = new StringWriter();
            var chat = new ChatConsola(new StringReader("#2\n#3\n"), salida, () => { });
            await chat.EnviarAsync(new RespuestaBot(1, "Elige", new BotonBot[] { new BotonBot("Plástico", "mat:1"), new BotonBot("Vidrio", "mat:2") }),
                CancellationToken.None);

            UpdateRecibido pulsado = (await chat.ObtenerUpdatesAsync(0, TimeSpan.Zero, CancellationToken.None)).Single();
            IReadOnlyList<UpdateRecibido> fueraDeRango = await chat.ObtenerUpdatesAsync(0, TimeSpan.Zero, CancellationToken.None);

            Assert.AreEqual("mat:2", pulsado.Mensaje?.DatoBoton);
            Assert.IsNotNull(pulsado.IdBoton);
            Assert.IsEmpty(fueraDeRango);
            string texto = salida.ToString();
            StringAssert.Contains(texto, "bot> Elige");
            StringAssert.Contains(texto, "[#2] Vidrio");
            StringAssert.Contains(texto, "(no hay un botón con ese número)");
        }

        [TestMethod]
        public async Task UpdatesConsecutivos_TienenIdsCrecientes()
        {
            var chat = new ChatConsola(new StringReader("a\nb\n"), new StringWriter(), () => { });

            long primero = (await chat.ObtenerUpdatesAsync(0, TimeSpan.Zero, CancellationToken.None)).Single().UpdateId;
            long segundo = (await chat.ObtenerUpdatesAsync(0, TimeSpan.Zero, CancellationToken.None)).Single().UpdateId;

            Assert.AreEqual(primero + 1, segundo);
        }

        [TestMethod]
        public async Task FinDeEntrada_AvisaYDevuelveVacio()
        {
            bool terminado = false;
            var chat = new ChatConsola(new StringReader(""), new StringWriter(), () => terminado = true);

            IReadOnlyList<UpdateRecibido> lote = await chat.ObtenerUpdatesAsync(0, TimeSpan.Zero, CancellationToken.None);

            Assert.IsTrue(terminado);
            Assert.IsEmpty(lote);
        }
    }
}
