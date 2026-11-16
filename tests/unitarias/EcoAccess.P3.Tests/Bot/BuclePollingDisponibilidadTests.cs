using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Bot;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Bot
{

    /// <summary>El bot no debe quedar en silencio por un fallo pasajero. Con un cliente falso: no es Telegram real.</summary>
    [TestClass]
    public sealed class BuclePollingDisponibilidadTests
    {
        private sealed class ClienteConGuion : IClienteTelegram
        {
            private readonly Queue<Func<IReadOnlyList<UpdateRecibido>>> _pasos = new();
            public CancellationTokenSource Fin { get; } = new();
            public void Agregar(Func<IReadOnlyList<UpdateRecibido>> paso) => _pasos.Enqueue(paso);
            public Task<IReadOnlyList<UpdateRecibido>> ObtenerUpdatesAsync(long offset, TimeSpan espera, CancellationToken ct)
            {
                if (_pasos.Count == 0) { Fin.Cancel(); throw new OperationCanceledException(ct); }
                return Task.FromResult(_pasos.Dequeue()());
            }
            public Task EnviarAsync(RespuestaBot respuesta, CancellationToken ct) => Task.CompletedTask;
            public Task ResponderBotonAsync(string idBoton, CancellationToken ct) => Task.CompletedTask;
        }

        private static UpdateRecibido Update(long id) => new(id, new MensajeEntrante(id, 700, 700, true, "/start", null), null);

        private static (BuclePolling Bucle, List<long> Atendidos, List<TimeSpan> Esperas, List<EstadoPolling> Latidos) Crear(ClienteConGuion cliente)
        {
            var atendidos = new List<long>();
            var esperas = new List<TimeSpan>();
            var latidos = new List<EstadoPolling>();
            var procesador = new ProcesadorUpdates((m, _) => { atendidos.Add(m.UpdateId); return Task.FromResult<IReadOnlyList<RespuestaBot>>(Array.Empty<RespuestaBot>()); }, cliente, _ => { });
            var bucle = new BuclePolling(cliente, procesador, _ => { }, (e, _) => { esperas.Add(e); return Task.CompletedTask; }, latidos.Add);
            return (bucle, atendidos, esperas, latidos);
        }

        [TestMethod]
        public async Task UnFalloInesperadoNoTerminaElBucleYSeSiguenAtendiendoMensajes()
        {
            var cliente = new ClienteConGuion();
            cliente.Agregar(() => throw new InvalidOperationException("fallo no previsto"));
            cliente.Agregar(() => new[] { Update(5) });
            var (bucle, atendidos, esperas, _) = Crear(cliente);

            FinPolling fin = await bucle.EjecutarAsync(cliente.Fin.Token);

            Assert.AreEqual(FinPolling.Cancelado, fin);
            CollectionAssert.AreEqual(new long[] { 5 }, atendidos);
            Assert.HasCount(1, esperas);
        }

        [TestMethod]
        public async Task UnaDesconexionSeReflejaEnElLatidoYSeRecupera()
        {
            var cliente = new ClienteConGuion();
            cliente.Agregar(() => throw new ErrorTelegramException("No se pudo conectar con Telegram (getUpdates)."));
            cliente.Agregar(() => new[] { Update(9) });
            var (bucle, atendidos, _, latidos) = Crear(cliente);

            await bucle.EjecutarAsync(cliente.Fin.Token);

            Assert.AreEqual(1, latidos[0].FallosSeguidos);
            StringAssert.Contains(latidos[0].UltimoError, "conectar");
            EstadoPolling ultimo = latidos[latidos.Count - 1];
            Assert.AreEqual(0, ultimo.FallosSeguidos);
            Assert.IsNull(ultimo.UltimoError);
            Assert.AreEqual(9, ultimo.UltimoUpdateId);
            Assert.AreEqual(1, ultimo.Atendidos);
            CollectionAssert.AreEqual(new long[] { 9 }, atendidos);
        }

        [TestMethod]
        public void ElLatidoNoFallaSiNoPuedeEscribirYNoGuardaDatosDePersonas()
        {
            var estado = new EstadoPolling(DateTime.UtcNow, DateTime.UtcNow, 1, 1, 0, null, 1, DateTime.UtcNow);
            estado.Guardar(Path.Combine(Path.GetTempPath(), new string('x', 300), "e.json"));

            string archivo = Path.Combine(Path.GetTempPath(), "latido-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                estado.Guardar(archivo);
                string texto = File.ReadAllText(archivo);
                StringAssert.Contains(texto, "UltimoContactoUtc");
                Assert.IsFalse(texto.Contains("Token") || texto.Contains("Texto") || texto.Contains("Chat"));
            }
            finally { File.Delete(archivo); }
        }
    }
}
