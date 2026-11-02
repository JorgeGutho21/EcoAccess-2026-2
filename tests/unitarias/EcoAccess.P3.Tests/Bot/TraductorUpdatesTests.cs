using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using EcoAccess.P3.Bot.Telegram;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class TraductorUpdatesTests
    {
        private static UpdateRecibido Traducir(string json)
        {
            using JsonDocument documento = JsonDocument.Parse(json);
            return TraductorUpdates.Traducir(documento.RootElement);
        }

        [TestMethod]
        public void MensajeDeTextoPrivado_SeTraduce()
        {
            UpdateRecibido update = Traducir(
                @"{""update_id"":41,""message"":{""message_id"":5,""from"":{""id"":700},""chat"":{""id"":700,""type"":""private""},""text"":""/start""}}");

            Assert.AreEqual(41, update.UpdateId);
            Assert.IsNull(update.IdBoton);
            Assert.IsNotNull(update.Mensaje);
            Assert.AreEqual(41, update.Mensaje.UpdateId);
            Assert.AreEqual(700, update.Mensaje.TelegramUserId);
            Assert.AreEqual(700, update.Mensaje.ChatId);
            Assert.IsTrue(update.Mensaje.ChatPrivado);
            Assert.AreEqual("/start", update.Mensaje.Texto);
            Assert.IsNull(update.Mensaje.DatoBoton);
        }

        [TestMethod]
        public void MensajeDeGrupo_NoEsPrivado()
        {
            UpdateRecibido update = Traducir(
                @"{""update_id"":42,""message"":{""from"":{""id"":700},""chat"":{""id"":-100,""type"":""group""},""text"":""hola""}}");

            Assert.IsNotNull(update.Mensaje);
            Assert.IsFalse(update.Mensaje.ChatPrivado);
            Assert.AreEqual(-100, update.Mensaje.ChatId);
        }

        [TestMethod]
        public void BotonPulsado_TraeDatoEIdDelCallback()
        {
            UpdateRecibido update = Traducir(
                @"{""update_id"":43,""callback_query"":{""id"":""cb-9"",""from"":{""id"":700},""data"":""mat:2"",""message"":{""chat"":{""id"":700,""type"":""private""}}}}");

            Assert.AreEqual("cb-9", update.IdBoton);
            Assert.IsNotNull(update.Mensaje);
            Assert.IsNull(update.Mensaje.Texto);
            Assert.AreEqual("mat:2", update.Mensaje.DatoBoton);
        }

        [TestMethod]
        public void BotonSinMensaje_SeContestaPeroNoSeAtiende()
        {
            UpdateRecibido update = Traducir(@"{""update_id"":44,""callback_query"":{""id"":""cb-1"",""from"":{""id"":700},""data"":""mat:2""}}");

            Assert.AreEqual("cb-1", update.IdBoton);
            Assert.IsNull(update.Mensaje);
        }

        [TestMethod]
        [DataRow(@"{""update_id"":45,""edited_message"":{""from"":{""id"":700},""chat"":{""id"":700,""type"":""private""},""text"":""x""}}")]
        [DataRow(@"{""update_id"":45,""message"":{""from"":{""id"":700},""chat"":{""id"":700,""type"":""private""},""sticker"":{}}}")]
        [DataRow(@"{""update_id"":45,""message"":{""chat"":{""id"":700,""type"":""private""},""text"":""sin remitente""}}")]
        [DataRow(@"{""update_id"":45,""message"":{""from"":{""id"":""700""},""chat"":{""id"":700,""type"":""private""},""text"":""id como texto""}}")]
        public void UpdateQueNoInteresa_ConservaElIdSinMensaje(string json)
        {
            UpdateRecibido update = Traducir(json);

            Assert.AreEqual(45, update.UpdateId);
            Assert.IsNull(update.Mensaje);
        }

        [TestMethod]
        [DataRow(@"{""message"":{}}")]
        [DataRow(@"{""update_id"":""12""}")]
        [DataRow(@"{""update_id"":1.5}")]
        [DataRow(@"[1,2]")]
        public void SinUpdateIdNumerico_LanzaJsonException(string json)
        {
            Assert.ThrowsExactly<JsonException>(() => Traducir(json));
        }

        [TestMethod]
        public void TraducirLista_RequiereUnArreglo()
        {
            using JsonDocument lista = JsonDocument.Parse(@"[{""update_id"":1},{""update_id"":2}]");
            using JsonDocument objeto = JsonDocument.Parse(@"{""update_id"":1}");

            Assert.HasCount(2, TraductorUpdates.TraducirLista(lista.RootElement));
            Assert.ThrowsExactly<JsonException>(() => TraductorUpdates.TraducirLista(objeto.RootElement));
        }

        [TestMethod]
        public void TraducirLista_OmiteElementosSinUpdateIdSinPerderElLote()
        {
            using JsonDocument lista = JsonDocument.Parse(@"[{""update_id"":1},{""sin_id"":true},7,{""update_id"":""3""},{""update_id"":4}]");

            CollectionAssert.AreEqual(
                new long[] { 1, 4 }, TraductorUpdates.TraducirLista(lista.RootElement).Select(u => u.UpdateId).ToArray());
        }
    }
}
