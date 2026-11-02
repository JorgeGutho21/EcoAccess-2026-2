using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Bot;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class ProcesadorUpdatesTests
    {
        private static MensajeEntrante Texto(long updateId, string texto, bool privado = true) =>
            new(updateId, 700, privado ? 700 : -100, privado, texto, null);

        private static Task<IReadOnlyList<RespuestaBot>> Eco(MensajeEntrante mensaje, CancellationToken _) =>
            Task.FromResult<IReadOnlyList<RespuestaBot>>(new RespuestaBot[] { new RespuestaBot(mensaje.ChatId, "eco " + (mensaje.Texto ?? mensaje.DatoBoton)) });

        [TestMethod]
        public async Task Mensaje_EnviaLasRespuestasDeLaLogica()
        {
            var cliente = new ClienteTelegramSimulado();
            var procesador = new ProcesadorUpdates(Eco, cliente, _ => { });

            await procesador.ProcesarAsync(new UpdateRecibido(1, Texto(1, "hola"), null), CancellationToken.None);

            Assert.AreEqual("eco hola", cliente.Enviadas.Single().Texto);
            Assert.IsEmpty(cliente.BotonesRespondidos);
        }

        [TestMethod]
        public async Task Boton_SeContestaAntesDeAtender()
        {
            var cliente = new ClienteTelegramSimulado();
            int botonesAlAtender = -1;
            var procesador = new ProcesadorUpdates((m, ct) =>
            {
                botonesAlAtender = cliente.BotonesRespondidos.Count;
                return Eco(m, ct);
            }, cliente, _ => { });

            await procesador.ProcesarAsync(
                new UpdateRecibido(2, new MensajeEntrante(2, 700, 700, true, null, "conf:si"), "cb-1"), CancellationToken.None);

            Assert.AreEqual(1, botonesAlAtender);
            Assert.AreEqual("cb-1", cliente.BotonesRespondidos.Single());
            Assert.AreEqual("eco conf:si", cliente.Enviadas.Single().Texto);
        }

        [TestMethod]
        public async Task SinMensaje_NoLlamaALaLogica()
        {
            var cliente = new ClienteTelegramSimulado();
            bool llamada = false;
            var procesador = new ProcesadorUpdates((m, ct) => { llamada = true; return Eco(m, ct); }, cliente, _ => { });

            await procesador.ProcesarAsync(new UpdateRecibido(3, null, "cb-2"), CancellationToken.None);

            Assert.IsFalse(llamada);
            Assert.IsEmpty(cliente.Enviadas);
            Assert.AreEqual("cb-2", cliente.BotonesRespondidos.Single());
        }

        [TestMethod]
        public async Task FalloDeLaLogica_ResponderErrorGenericoSinDetalles()
        {
            var cliente = new ClienteTelegramSimulado();
            var registro = new List<string>();
            var procesador = new ProcesadorUpdates(
                (_, _) => throw new InvalidOperationException("ORA-12541 detalle interno con datos"), cliente, registro.Add);

            await procesador.ProcesarAsync(new UpdateRecibido(4, Texto(4, "hola"), null), CancellationToken.None);

            Assert.AreEqual(ProcesadorUpdates.ErrorGenerico, cliente.Enviadas.Single().Texto);
            Assert.AreEqual(700, cliente.Enviadas.Single().ChatId);
            string linea = registro.Single(l => !l.StartsWith("Tiempo", StringComparison.Ordinal));
            StringAssert.Contains(linea, nameof(InvalidOperationException));
            Assert.DoesNotContain("ORA-12541", linea);
        }

        [TestMethod]
        public async Task FalloDeLaLogicaEnGrupo_NoResponde()
        {
            var cliente = new ClienteTelegramSimulado();
            var procesador = new ProcesadorUpdates((_, _) => throw new InvalidOperationException(), cliente, _ => { });

            await procesador.ProcesarAsync(new UpdateRecibido(5, Texto(5, "hola", privado: false), null), CancellationToken.None);

            Assert.IsEmpty(cliente.Enviadas);
        }

        [TestMethod]
        public async Task FalloAlEnviar_SeRegistraYNoEscapa()
        {
            var cliente = new ClienteTelegramSimulado { FallarEnvios = true };
            var registro = new List<string>();
            var procesador = new ProcesadorUpdates(Eco, cliente, registro.Add);

            await procesador.ProcesarAsync(new UpdateRecibido(6, Texto(6, "hola"), null), CancellationToken.None);

            StringAssert.Contains(registro.Single(l => !l.StartsWith("Tiempo", StringComparison.Ordinal)), "Update 6");
        }

        [TestMethod]
        public async Task CancelacionPropia_SePropaga()
        {
            using var cancelacion = new CancellationTokenSource();
            var procesador = new ProcesadorUpdates((_, ct) =>
            {
                cancelacion.Cancel();
                ct.ThrowIfCancellationRequested();
                return Task.FromResult<IReadOnlyList<RespuestaBot>>(Array.Empty<RespuestaBot>());
            }, new ClienteTelegramSimulado(), _ => { });

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => procesador.ProcesarAsync(new UpdateRecibido(7, Texto(7, "hola"), null), cancelacion.Token));
        }

        [TestMethod]
        public async Task CancelacionAjena_SeTrataComoFallo()
        {
            var cliente = new ClienteTelegramSimulado();
            var procesador = new ProcesadorUpdates((_, _) => throw new TaskCanceledException("tiempo de Oracle"), cliente, _ => { });

            await procesador.ProcesarAsync(new UpdateRecibido(8, Texto(8, "hola"), null), CancellationToken.None);

            Assert.AreEqual(ProcesadorUpdates.ErrorGenerico, cliente.Enviadas.Single().Texto);
        }

        [TestMethod]
        public async Task CallbackDeFotoNoAnunciaAnalisisAntesDeValidarConsentimiento()
        {
            var cliente = new ClienteTelegramSimulado();
            int enviadasAlAtender = -1;
            var procesador = new ProcesadorUpdates((m, ct) => { enviadasAlAtender = cliente.Enviadas.Count; return Eco(m, ct); }, cliente, _ => { });
            var consentir = new MensajeEntrante(7, 700, 700, true, null, ServicioConversacionTelegram.PrefijoConsentirFoto + "abc");

            await procesador.ProcesarAsync(new UpdateRecibido(7, consentir, "boton-7"), CancellationToken.None);

            Assert.AreEqual(0, enviadasAlAtender, "El adaptador no sabe todavía si el consentimiento es válido.");
            Assert.HasCount(1, cliente.Enviadas, "Solo se envía la respuesta del servicio.");
        }
    }
}
