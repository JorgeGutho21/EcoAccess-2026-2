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
    [TestClass]
    public sealed class PruebasCoordinadorYTelegramComandosTests
    {
        private static readonly IReadOnlyList<CanecaEstacion> Estacion = new[]
        {
            new CanecaEstacion(1093, "Blanca", CategoriaMaterial.Aprovechable, true, null, null),
            new CanecaEstacion(1400, "Verde", CategoriaMaterial.OrganicoAprovechable, true, null, null),
            new CanecaEstacion(1401, "Negra", CategoriaMaterial.NoAprovechable, true, null, null),
        };

        private static SimuladorEstacionTresCanecas Sim() => new(
            new Dictionary<int, DestinoResiduo> { [1093] = DestinoResiduo.Blanca, [1400] = DestinoResiduo.Verde, [1401] = DestinoResiduo.Negra },
            TimeProvider.System, TimeSpan.Zero);

        [TestMethod]
        public void ParseoComandosTelegram_SoportaLas7CombinacionesYRechazaInvalidos()
        {
            var casos = new (string Cmd, CombinacionCanecas Esp)[]
            {
                ("/verde", CombinacionCanecas.C01_Verde),
                ("/blanca", CombinacionCanecas.C02_Blanca),
                ("/negra", CombinacionCanecas.C03_Negra),
                ("/abrir verde blanca", CombinacionCanecas.C04_VerdeBlanca),
                ("/abrir verde negra", CombinacionCanecas.C05_VerdeNegra),
                ("/abrir blanca negra", CombinacionCanecas.C06_BlancaNegra),
                ("/abrir verde blanca negra", CombinacionCanecas.C07_VerdeBlancaNegra),
            };
            foreach (var c in casos)
            {
                Assert.IsTrue(CoordinadorEstacionUnificado.IntentarParsearComandoCanecas(c.Cmd, out var lista), c.Cmd);
                Assert.AreEqual(c.Esp, MotorCombinacionesCanecas.CalcularCombinacion(lista), c.Cmd);
            }
            Assert.IsFalse(CoordinadorEstacionUnificado.IntentarParsearComandoCanecas("/abrir amarilla", out _));
            Assert.IsFalse(CoordinadorEstacionUnificado.IntentarParsearComandoCanecas("hola", out _));
        }

        [TestMethod]
        public async Task CoordinadorUnificado_SeguridadAutorizacionSesionFisicaYConcurrenciaPcTelegram()
        {
            var ejFisicoSimuladoEnTest = new EjecutorFisicoMock();
            var coord = new CoordinadorEstacionUnificado(Sim(), Estacion, ejFisicoSimuladoEnTest, new long[] { 777001 });

            // 1. Usuario no autorizado rechazado
            var deneg = await coord.SolicitarAperturaAsync("tg-noauth-1", OrigenSolicitudEstacion.Telegram, "desconocido",
                new[] { CategoriaMaterial.OrganicoAprovechable }, solicitarModoFisico: false, usuarioAutorizado: coord.EsChatAutorizado(999, 999));
            Assert.IsFalse(deneg.Autorizada);
            Assert.IsFalse(deneg.Exitosa);

            // 2. Modo físico bloqueado si no se habilitó sesión supervisada
            var bloqFis = await coord.SolicitarAperturaAsync("tg-fis-bloq", OrigenSolicitudEstacion.Telegram, "supervisor",
                new[] { CategoriaMaterial.OrganicoAprovechable }, solicitarModoFisico: true, usuarioAutorizado: coord.EsChatAutorizado(777001, 777001));
            Assert.IsFalse(bloqFis.Autorizada);
            StringAssert.Contains(bloqFis.Mensaje, "deshabilitado");

            // 3. Habilitar sesión supervisada y ejecutar concurrencia PC (blanca) + Telegram (verde) sin colisión serial
            coord.HabilitarSesionFisicaSupervisada(TimeSpan.FromMinutes(15), "Jorge Gutierrez");
            Assert.AreEqual(ModoOperacionHardware.HardwareFisicoSupervisado, coord.ModoActual);

            var tTg = coord.SolicitarAperturaAsync("tg-conc-1", OrigenSolicitudEstacion.Telegram, "Jorge",
                new[] { CategoriaMaterial.OrganicoAprovechable }, solicitarModoFisico: true, usuarioAutorizado: true);
            var tPc = coord.SolicitarAperturaAsync("pc-conc-1", OrigenSolicitudEstacion.WindowsPc, "Jorge",
                new[] { CategoriaMaterial.Aprovechable }, solicitarModoFisico: true, usuarioAutorizado: true);
            var ambos = await Task.WhenAll(tTg, tPc);
            Assert.IsTrue(ambos.All(r => r.Exitosa && r.ModoEjecucion == ModoOperacionHardware.HardwareFisicoSupervisado));
            Assert.AreEqual(1, ejFisicoSimuladoEnTest.MaximaConcurrenciaObservada, "El puerto serial nunca debe usarse en paralelo.");

            // 4. Parada inmediata deshabilita el modo físico
            coord.DeshabilitarSesionFisica();
            Assert.AreEqual(ModoOperacionHardware.Simulacion, coord.ModoActual);
        }

        private sealed class EjecutorFisicoMock : IEjecutorAperturas
        {
            private int _activos;
            public int MaximaConcurrenciaObservada { get; private set; }
            public bool EsSimulado => false;

            public async Task<ResultadoEjecucionApertura> AbrirAsync(ComandoApertura comando, CancellationToken ct = default)
            {
                int cur = Interlocked.Increment(ref _activos);
                if (cur > MaximaConcurrenciaObservada) MaximaConcurrenciaObservada = cur;
                await Task.Delay(25, ct);
                Interlocked.Decrement(ref _activos);
                return new ResultadoEjecucionApertura(EstadoEjecucionApertura.Completada, new[] { TipoEventoDispositivo.Apertura, TipoEventoDispositivo.CierreSinSensor });
            }
        }
    }
}