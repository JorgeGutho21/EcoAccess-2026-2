using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using EcoAccess.P3.Bot;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class ReceptorWebhookTests
    {
        private const string Secreto = "secreto_de_prueba-0123456789";
        private const string UpdateValido =
            @"{""update_id"":77,""message"":{""from"":{""id"":700},""chat"":{""id"":700,""type"":""private""},""text"":""/help""}}";

        private static (ReceptorWebhook Receptor, List<long> Atendidos, ClienteTelegramSimulado Cliente) Crear()
        {
            var atendidos = new List<long>();
            var cliente = new ClienteTelegramSimulado();
            var procesador = new ProcesadorUpdates((m, _) =>
            {
                atendidos.Add(m.UpdateId);
                return Task.FromResult<IReadOnlyList<RespuestaBot>>(new RespuestaBot[] { new RespuestaBot(m.ChatId, "ok") });
            }, cliente, _ => { });
            return (new ReceptorWebhook(procesador, Secreto), atendidos, cliente);
        }

        private static byte[] Bytes(string texto) => Encoding.UTF8.GetBytes(texto);

        [TestMethod]
        public async Task SecretoCorrecto_AtiendeYDevuelve200()
        {
            (ReceptorWebhook receptor, List<long> atendidos, ClienteTelegramSimulado cliente) = Crear();

            int estado = await receptor.RecibirAsync(Secreto, Bytes(UpdateValido), CancellationToken.None);

            Assert.AreEqual(200, estado);
            CollectionAssert.AreEqual(new long[] { 77 }, atendidos);
            Assert.HasCount(1, cliente.Enviadas);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("secreto_de_prueba-012345678")]
        [DataRow("secreto_de_prueba-0123456789x")]
        [DataRow("SECRETO_DE_PRUEBA-0123456789")]
        public async Task SecretoAusenteODistinto_Devuelve401SinAtender(string? recibido)
        {
            (ReceptorWebhook receptor, List<long> atendidos, _) = Crear();

            int estado = await receptor.RecibirAsync(recibido, Bytes(UpdateValido), CancellationToken.None);

            Assert.AreEqual(401, estado);
            Assert.IsEmpty(atendidos);
        }

        [TestMethod]
        [DataRow("no es json")]
        [DataRow(@"{""message"":{}}")]
        [DataRow("")]
        public async Task CuerpoIlegible_Devuelve400(string cuerpo)
        {
            (ReceptorWebhook receptor, List<long> atendidos, _) = Crear();

            Assert.AreEqual(400, await receptor.RecibirAsync(Secreto, Bytes(cuerpo), CancellationToken.None));
            Assert.IsEmpty(atendidos);
        }

        [TestMethod]
        public async Task CuerpoDemasiadoGrande_Devuelve413()
        {
            (ReceptorWebhook receptor, List<long> atendidos, _) = Crear();

            int estado = await receptor.RecibirAsync(Secreto, new byte[ReceptorWebhook.TamanoMaximo + 1], CancellationToken.None);

            Assert.AreEqual(413, estado);
            Assert.IsEmpty(atendidos);
        }

        [TestMethod]
        public async Task UpdateSinMensajeUtil_Devuelve200()
        {
            (ReceptorWebhook receptor, List<long> atendidos, _) = Crear();

            int estado = await receptor.RecibirAsync(Secreto, Bytes(@"{""update_id"":78,""edited_message"":{}}"), CancellationToken.None);

            Assert.AreEqual(200, estado);
            Assert.IsEmpty(atendidos);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("corto")]
        [DataRow("con espacios dentro del secreto")]
        [DataRow("secreto+con/caracteres=raros")]
        public void SecretoMalFormado_SeRechaza(string? secreto)
        {
            Assert.IsFalse(ReceptorWebhook.SecretoValido(secreto));
            var procesador = new ProcesadorUpdates((_, _) => Task.FromResult<IReadOnlyList<RespuestaBot>>(Array.Empty<RespuestaBot>()), new ClienteTelegramSimulado(), _ => { });
            Assert.ThrowsExactly<ArgumentException>(() => new ReceptorWebhook(procesador, secreto!));
        }

        [TestMethod]
        public void SecretoLargoValido_SeAcepta()
        {
            Assert.IsTrue(ReceptorWebhook.SecretoValido(new string('a', 256)));
            Assert.IsFalse(ReceptorWebhook.SecretoValido(new string('a', 257)));
        }
    }
}
