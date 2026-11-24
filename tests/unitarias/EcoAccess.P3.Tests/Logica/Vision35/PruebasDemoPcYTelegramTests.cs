using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Logica.Vision35;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica.Vision35
{
    /// <summary>
    /// Valida el modo demostración PC (SimuladorEstacionTresCanecas) para las 7 combinaciones C01–C07,
    /// rechazo de órdenes vencidas, idempotencia de duplicados y coherencia/concurrencia entre PC y Telegram.
    /// Todas estas pruebas operan estrictamente en simulación (EsSimulado == true; 0 activaciones físicas).
    /// </summary>
    [TestClass]
    public sealed class PruebasDemoPcYTelegramTests
    {
        private static readonly IReadOnlyList<CanecaEstacion> EstacionDemo = new[]
        {
            new CanecaEstacion(1093, "Blanca", CategoriaMaterial.Aprovechable, true, null, null),
            new CanecaEstacion(1400, "Verde", CategoriaMaterial.OrganicoAprovechable, true, null, null),
            new CanecaEstacion(1401, "Negra", CategoriaMaterial.NoAprovechable, true, null, null),
        };

        private static SimuladorEstacionTresCanecas CrearSimulador() => new(
            new Dictionary<int, DestinoResiduo>
            {
                [1093] = DestinoResiduo.Blanca,
                [1400] = DestinoResiduo.Verde,
                [1401] = DestinoResiduo.Negra,
            },
            TimeProvider.System,
            TimeSpan.Zero);

        private static byte[] FotoUnica(byte seed) =>
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, seed, (byte)(seed + 1) };

        [TestMethod]
        public async Task DemoPc_SieteCombinacionesC01aC07_EjecutanAperturaYCierreSimuladosYRestablecenEstado()
        {
            var casos = new (CombinacionCanecas Comb, int CanecasEsperadas, ObservacionObjetoVisual[] Objetos)[]
            {
                (CombinacionCanecas.C01_Verde, 1, new[] { new ObservacionObjetoVisual("green apple", "organic", EstadoObservableObjeto.LimpioYVacio, 1, 0.95m) }),
                (CombinacionCanecas.C02_Blanca, 1, new[] { new ObservacionObjetoVisual("plastic water bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio, 1, 0.95m) }),
                (CombinacionCanecas.C03_Negra, 1, new[] { new ObservacionObjetoVisual("used napkin", "sanitary", EstadoObservableObjeto.Contaminado, 1, 0.95m) }),
                (CombinacionCanecas.C04_VerdeBlanca, 2, new[]
                {
                    new ObservacionObjetoVisual("green apple", "organic", EstadoObservableObjeto.LimpioYVacio, 1, 0.95m),
                    new ObservacionObjetoVisual("clean cardboard box", "cardboard", EstadoObservableObjeto.LimpioYVacio, 1, 0.93m),
                }),
                (CombinacionCanecas.C05_VerdeNegra, 2, new[]
                {
                    new ObservacionObjetoVisual("potato peel", "organic", EstadoObservableObjeto.LimpioYVacio, 1, 0.94m),
                    new ObservacionObjetoVisual("used dental floss", "sanitary", EstadoObservableObjeto.Contaminado, 1, 0.91m),
                }),
                (CombinacionCanecas.C06_BlancaNegra, 2, new[]
                {
                    new ObservacionObjetoVisual("clean paper", "paper", EstadoObservableObjeto.LimpioYVacio, 1, 0.94m),
                    new ObservacionObjetoVisual("used dental floss", "sanitary", EstadoObservableObjeto.Contaminado, 1, 0.91m),
                }),
                (CombinacionCanecas.C07_VerdeBlancaNegra, 3, new[]
                {
                    new ObservacionObjetoVisual("green apple", "organic", EstadoObservableObjeto.LimpioYVacio, 1, 0.95m),
                    new ObservacionObjetoVisual("clean paper", "paper", EstadoObservableObjeto.LimpioYVacio, 1, 0.94m),
                    new ObservacionObjetoVisual("used dental floss", "sanitary", EstadoObservableObjeto.Contaminado, 1, 0.91m),
                }),
            };

            var sim = CrearSimulador();
            byte seed = 10;
            foreach (var c in casos)
            {
                seed++;
                var resp = new RespuestaVisionEstructurada(true, true, CalidadImagenVision.Aceptable, c.Objetos, "lfm3-vl:latest", 250);
                var gestor = new GestorSesionMultirresiduo(new ProveedorFijoDemo(resp), new MotorReglasMultirresiduo(), sim);

                var dictamen = await gestor.AnalizarFotografiaAsync(FotoUnica(seed));
                Assert.AreEqual(c.Comb, dictamen.Combinacion, $"Combinación fallida en {c.Comb}");
                Assert.IsTrue(dictamen.PuedeAbrirCanecas);

                var aperturas = await gestor.EjecutarAperturaSecuencialAsync($"demo-pc-{c.Comb}", EstacionDemo, permitirSoloSimulador: true);
                Assert.AreEqual(c.CanecasEsperadas, aperturas.Count);
                Assert.IsTrue(aperturas.All(a => a.ComandoEnviado && a.AperturaConfirmada && a.CierreConfirmado && a.EsSimulado));
                Assert.AreEqual(EstadoSesionDeposito.FINALIZADO, gestor.Maquina.EstadoActual);

                // Prevención de activación duplicada al reintentar el mismo idOperacion
                gestor.Maquina.Transicionar(EstadoSesionDeposito.INACTIVO, "reintento");
                await gestor.AnalizarFotografiaAsync(FotoUnica((byte)(seed + 100)));
                var dup = await gestor.EjecutarAperturaSecuencialAsync($"demo-pc-{c.Comb}", EstacionDemo, permitirSoloSimulador: true);
                Assert.AreEqual(0, dup.Count, "Un reintento con el mismo idOperacion no debe abrir nuevamente.");
            }
        }

        [TestMethod]
        public async Task SimuladorDemo_RechazaOrdenVencidaYDuplicada_YCoordinaConcurrenciaPcYTelegram()
        {
            var sim = CrearSimulador();

            // 1. Orden vencida debe rechazarse sin apertura
            var vencida = new ComandoApertura(9001, 1400, "orden-vencida-1", DateTime.UtcNow.AddSeconds(-30));
            var resVencida = await sim.AbrirAsync(vencida);
            Assert.AreEqual(EstadoEjecucionApertura.Rechazada, resVencida.Estado);
            Assert.AreEqual(0, resVencida.Eventos.Count);

            // 2. Dos canales (PC y Telegram) enviando la misma clave de idempotencia concurrentemente: solo 1 se ejecuta
            var cmdPc = new ComandoApertura(9002, 1093, "clave-compartida-pc-tg", DateTime.UtcNow.AddMinutes(2));
            var cmdTg = new ComandoApertura(9003, 1093, "clave-compartida-pc-tg", DateTime.UtcNow.AddMinutes(2));
            var resultados = await Task.WhenAll(sim.AbrirAsync(cmdPc), sim.AbrirAsync(cmdTg));
            Assert.AreEqual(1, resultados.Count(r => r.Estado == EstadoEjecucionApertura.Completada), "Exactamente una ejecución exitosa.");
            Assert.AreEqual(1, resultados.Count(r => r.Estado == EstadoEjecucionApertura.Duplicada), "La segunda debe bloquearse como Duplicada.");

            // 3. Coherencia PC vs Telegram: ambos canales usando el mismo MotorReglasMultirresiduo obtienen idénticas canecas
            var motorCompartido = new MotorReglasMultirresiduo();
            var escena = new RespuestaVisionEstructurada(true, true, CalidadImagenVision.Aceptable, new[]
            {
                new ObservacionObjetoVisual("potato peel", "organic", EstadoObservableObjeto.LimpioYVacio, 1, 0.94m),
                new ObservacionObjetoVisual("clean paper", "paper", EstadoObservableObjeto.LimpioYVacio, 1, 0.92m),
                new ObservacionObjetoVisual("usb cable", "electronic", EstadoObservableObjeto.LimpioYVacio, 1, 0.91m),
            }, "lfm3-vl:latest", 400);

            var resultadoPc = motorCompartido.Evaluar(escena);
            var resultadoTelegram = motorCompartido.Evaluar(escena);
            Assert.AreEqual(resultadoPc.Combinacion, resultadoTelegram.Combinacion);
            Assert.AreEqual(CombinacionCanecas.C04_VerdeBlanca, resultadoPc.Combinacion);
            CollectionAssert.AreEqual(resultadoPc.CanecasRequeridas.ToList(), resultadoTelegram.CanecasRequeridas.ToList());
        }

        private sealed class ProveedorFijoDemo : IProveedorVisionMultirresiduo
        {
            private readonly RespuestaVisionEstructurada _r;
            public ProveedorFijoDemo(RespuestaVisionEstructurada r) => _r = r;
            public string NombreProveedor => _r.ModeloUtilizado;
            public Task<RespuestaVisionEstructurada> ObservarAsync(byte[] bytesImagen, CancellationToken ct = default) => Task.FromResult(_r);
        }
    }
}