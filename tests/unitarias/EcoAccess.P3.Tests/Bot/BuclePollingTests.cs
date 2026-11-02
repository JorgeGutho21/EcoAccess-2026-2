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
    public sealed class BuclePollingTests
    {
        private static UpdateRecibido Update(long id, string texto = "hola") =>
            new(id, new MensajeEntrante(id, 700, 700, true, texto, null), null);

        private sealed class Escenario
        {
            public ClienteTelegramSimulado Cliente { get; } = new();
            public CancellationTokenSource Cancelacion { get; } = new();
            public List<long> Atendidos { get; } = new List<long>();
            public List<TimeSpan> Esperas { get; } = new List<TimeSpan>();
            public BuclePolling Bucle { get; }

            public Escenario(Func<MensajeEntrante, Task>? alAtender = null)
            {
                Cliente.AlVaciarse = Cancelacion.Cancel;
                var procesador = new ProcesadorUpdates(async (m, _) =>
                {
                    Atendidos.Add(m.UpdateId);
                    if (alAtender is not null)
                    {
                        await alAtender(m);
                    }

                    return Array.Empty<RespuestaBot>();
                }, Cliente, _ => { });
                Bucle = new BuclePolling(Cliente, procesador, _ => { }, (espera, _) =>
                {
                    Esperas.Add(espera);
                    return Task.CompletedTask;
                });
            }
        }

        [TestMethod]
        public async Task AtiendeEnOrdenYConfirmaConOffset()
        {
            var escenario = new Escenario();
            escenario.Cliente.Encolar(Update(12), Update(10), Update(11));
            escenario.Cliente.Encolar(Update(13));

            FinPolling fin = await escenario.Bucle.EjecutarAsync(escenario.Cancelacion.Token);

            Assert.AreEqual(FinPolling.Cancelado, fin);
            CollectionAssert.AreEqual(new long[] { 10, 11, 12, 13 }, escenario.Atendidos);
            CollectionAssert.AreEqual(new long[] { 0, 13, 14 }, escenario.Cliente.OffsetsPedidos);
            Assert.AreEqual(14, escenario.Bucle.Offset);
        }

        [TestMethod]
        public async Task UpdateRepetidoOAntiguo_NoSeAtiendeDosVeces()
        {
            var escenario = new Escenario();
            escenario.Cliente.Encolar(Update(20), Update(21));
            escenario.Cliente.Encolar(Update(21), Update(19), Update(22));

            await escenario.Bucle.EjecutarAsync(escenario.Cancelacion.Token);

            CollectionAssert.AreEqual(new long[] { 20, 21, 22 }, escenario.Atendidos);
        }

        [TestMethod]
        public async Task Desconexion_EsperaConRetrocesoYSeRecupera()
        {
            var escenario = new Escenario();
            escenario.Cliente.EncolarFallo(new ErrorTelegramException("sin red"));
            escenario.Cliente.EncolarFallo(new ErrorTelegramException("sin red"));
            escenario.Cliente.EncolarFallo(new ErrorTelegramException("sin red"));
            escenario.Cliente.Encolar(Update(30));
            escenario.Cliente.EncolarFallo(new ErrorTelegramException("sin red"));
            escenario.Cliente.Encolar(Update(31));

            await escenario.Bucle.EjecutarAsync(escenario.Cancelacion.Token);

            // Tras un lote exitoso la cuenta de fallos vuelve a empezar.
            CollectionAssert.AreEqual(
                new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1) },
                escenario.Esperas);
            CollectionAssert.AreEqual(new long[] { 30, 31 }, escenario.Atendidos);
        }

        [TestMethod]
        public async Task RetryAfter_SeRespeta()
        {
            var escenario = new Escenario();
            escenario.Cliente.EncolarFallo(new ErrorTelegramException("429", TimeSpan.FromSeconds(17)));

            await escenario.Bucle.EjecutarAsync(escenario.Cancelacion.Token);

            CollectionAssert.AreEqual(new[] { TimeSpan.FromSeconds(17) }, escenario.Esperas);
        }

        [TestMethod]
        public async Task Conflicto409_TerminaSinReintentar()
        {
            var escenario = new Escenario();
            escenario.Cliente.EncolarFallo(new ErrorTelegramException("409", esConflicto: true));
            escenario.Cliente.Encolar(Update(40));

            FinPolling fin = await escenario.Bucle.EjecutarAsync(escenario.Cancelacion.Token);

            Assert.AreEqual(FinPolling.Conflicto, fin);
            Assert.IsEmpty(escenario.Atendidos);
            Assert.IsEmpty(escenario.Esperas);
        }

        [TestMethod]
        public async Task TokenRechazado_TerminaSinReintentar()
        {
            var escenario = new Escenario();
            escenario.Cliente.EncolarFallo(new ErrorTelegramException("401", esTokenRechazado: true));
            escenario.Cliente.Encolar(Update(45));

            FinPolling fin = await escenario.Bucle.EjecutarAsync(escenario.Cancelacion.Token);

            Assert.AreEqual(FinPolling.TokenRechazado, fin);
            Assert.IsEmpty(escenario.Atendidos);
            Assert.IsEmpty(escenario.Esperas);
        }

        [TestMethod]
        public async Task DetenerTerminaLosUpdatesYaConfirmados()
        {
            Escenario? escenario = null;
            escenario = new Escenario(m =>
            {
                if (m.UpdateId == 51)
                {
                    escenario!.Cancelacion.Cancel();
                    escenario.Cancelacion.Token.ThrowIfCancellationRequested();
                }

                return Task.CompletedTask;
            });
            escenario.Cliente.Encolar(Update(50), Update(51), Update(52));

            FinPolling fin = await escenario.Bucle.EjecutarAsync(escenario.Cancelacion.Token);

            Assert.AreEqual(FinPolling.Cancelado, fin);
            // El lote se confirma al despacharlo: update_telegram ya registró cada update y descartaría un reenvío.
            Assert.AreEqual(53, escenario.Bucle.Offset);
            // Ninguno se pierde: 51 falla dentro de su atención (respuesta genérica) y 52 se atiende en el margen.
            CollectionAssert.AreEqual(new long[] { 50, 51, 52 }, escenario.Atendidos);
        }

        [TestMethod]
        public async Task FalloDeUnUpdate_NoDetieneElPolling()
        {
            var escenario = new Escenario(m => m.UpdateId == 60 ? throw new InvalidOperationException() : Task.CompletedTask);
            escenario.Cliente.Encolar(Update(60), Update(61));

            await escenario.Bucle.EjecutarAsync(escenario.Cancelacion.Token);

            CollectionAssert.AreEqual(new long[] { 60, 61 }, escenario.Atendidos);
            Assert.AreEqual(62, escenario.Bucle.Offset);
            Assert.AreEqual(ProcesadorUpdates.ErrorGenerico, escenario.Cliente.Enviadas.Single().Texto);
        }

        [TestMethod]
        [DataRow(1, 1)]
        [DataRow(2, 2)]
        [DataRow(3, 4)]
        [DataRow(6, 32)]
        [DataRow(7, 60)]
        [DataRow(1000, 60)]
        public void Retroceso_DuplicaHastaElMaximo(int fallos, int segundos)
        {
            Assert.AreEqual(TimeSpan.FromSeconds(segundos), BuclePolling.Retroceso(fallos));
        }

        [TestMethod]
        public void Retroceso_SinFallos_Lanza()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BuclePolling.Retroceso(0));
        }

        [TestMethod]
        public async Task UnaFotoLentaNoBloqueaAOtraPersonaYCadaPersonaConservaSuOrden()
        {
            var liberar = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var terminados = new System.Collections.Concurrent.ConcurrentQueue<long>();
            var escenario = new Escenario(async m =>
            {
                if (m.UpdateId == 60) await liberar.Task; // persona 700: análisis lento
                terminados.Enqueue(m.UpdateId);
                if (m.UpdateId == 62) liberar.SetResult(true); // persona 701 termina mientras 700 sigue ocupada
            });
            escenario.Cliente.Encolar(Update(60), Update(61), new UpdateRecibido(62, new MensajeEntrante(62, 701, 701, true, "hola", null), null));

            await escenario.Bucle.EjecutarAsync(escenario.Cancelacion.Token);

            CollectionAssert.AreEqual(new long[] { 62, 60, 61 }, terminados.ToArray(), "701 no espera a 700; 61 espera a 60.");
        }
    }
}
