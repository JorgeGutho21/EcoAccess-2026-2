using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using EcoAccess.P3.Bot.Dispositivos;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Bot
{
    /// <summary>
    /// Estación de tres canecas con un bridge por caneca: antes de ordenar una apertura, el bot comprueba que escuche el
    /// bridge de ESA caneca. Antes solo se comprobaban los enlaces de la blanca, y una orden para la verde con su bridge
    /// caído se emitía igual y terminaba vencida.
    /// </summary>
    [TestClass]
    public sealed class EstadoCanecaLocalTests
    {
        /// <summary>En .NET Framework 4.8 TcpListener no es IDisposable: este envoltorio lo detiene al salir.</summary>
        private sealed class Oyente : IDisposable
        {
            private readonly TcpListener _l = new(IPAddress.Loopback, 0);
            public Oyente() => _l.Start();
            public IPEndPoint LocalEndpoint => (IPEndPoint)_l.LocalEndpoint;
            public void Dispose() => _l.Stop();
        }

        private static Oyente Escuchar() => new();

        private static int PuertoLibre()
        {
            using var l = Escuchar();
            return l.LocalEndpoint.Port;
        }

        private static Uri Url(int puerto, string ruta) => new($"http://127.0.0.1:{puerto}/{ruta}/");

        [TestMethod]
        public async Task CadaCanecaCompruebaSuPropioBridge()
        {
            using var blancaEventos = Escuchar();
            using var blancaComandos = Escuchar();
            int Puerto(Oyente l) => l.LocalEndpoint.Port;
            var porCaneca = new Dictionary<int, Uri[]>
            {
                [1093] = new[] { Url(Puerto(blancaEventos), "eventos"), Url(Puerto(blancaComandos), "comandos") },
                [1400] = new[] { Url(PuertoLibre(), "eventos"), Url(PuertoLibre(), "comandos") },   // bridge de la verde caído
            };
            int consultasFallo = 0;
            var estado = new EstadoCanecaLocal((_, _, _) => { consultasFallo++; return Task.FromResult(false); }, porCaneca,
                Url(Puerto(blancaEventos), "eventos"), Url(Puerto(blancaComandos), "comandos"));

            Assert.IsTrue(await estado.DisponibleAsync(1093));
            Assert.IsFalse(await estado.DisponibleAsync(1400), "La verde no está disponible aunque el bridge de la blanca escuche.");
            Assert.IsTrue(await estado.DisponibleAsync(1401), "Sin enlaces propios, la caneca usa los predeterminados.");
            Assert.AreEqual(2, consultasFallo, "Con el bridge caído ni siquiera se consulta Oracle.");
        }

        [TestMethod]
        public async Task FalloFisicoReciente_BloqueaSoloEsaCaneca()
        {
            using var eventos = Escuchar();
            using var comandos = Escuchar();
            var enlaces = new[] { Url(eventos.LocalEndpoint.Port, "eventos"), Url(comandos.LocalEndpoint.Port, "comandos") };
            var estado = new EstadoCanecaLocal((id, _, _) => Task.FromResult(id == 1401), new Dictionary<int, Uri[]> { [1401] = enlaces, [1400] = enlaces }, enlaces);

            Assert.IsFalse(await estado.DisponibleAsync(1401));
            Assert.IsTrue(await estado.DisponibleAsync(1400));
        }

        [TestMethod]
        public async Task EnlaceNoLocal_NuncaSeSondea()
        {
            var estado = new EstadoCanecaLocal((_, _, _) => Task.FromResult(false),
                new Dictionary<int, Uri[]> { [1093] = new[] { new Uri("http://192.0.2.10:8182/eventos/") } });
            Assert.IsFalse(await estado.DisponibleAsync(1093));
        }
    }
}
