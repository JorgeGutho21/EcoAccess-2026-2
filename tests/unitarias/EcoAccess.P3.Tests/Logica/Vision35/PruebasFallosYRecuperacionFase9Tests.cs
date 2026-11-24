using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    /// Pruebas de fallos, recuperación (Fase 9) y regresión del Bug #4:
    /// caja de cartón vacía vs cable USB / medicamentos / blíster, Ollama apagado, modelo inexistente,
    /// imagen dañada, >12 MB, respuesta vacía, timeout, objetos ambiguos, ESP32 desconectado/reconectado,
    /// fallo de Oracle y prevención de duplicados por reintento.
    /// </summary>
    [TestClass]
    public sealed class PruebasFallosYRecuperacionFase9Tests
    {
        private static readonly byte[] JpegStub = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x33, 0x44 };
        private static readonly MotorReglasMultirresiduo Motor = new();
        private static readonly IReadOnlyList<CanecaEstacion> Estacion = new[]
        {
            new CanecaEstacion(1093, "Blanca", CategoriaMaterial.Aprovechable, true, null, null),
            new CanecaEstacion(1400, "Verde", CategoriaMaterial.OrganicoAprovechable, true, null, null),
            new CanecaEstacion(1401, "Negra", CategoriaMaterial.NoAprovechable, true, null, null),
        };

        [TestMethod]
        public void Bug4_CajaCartonVaciaDeCableOMedicamento_EsBlanca_MientrasCableYBlisterSonGestionEspecial()
        {
            // Foto #41: cable USB + caja de cartón vacía del cable -> BLANCA + GESTION_ESPECIAL
            var foto41 = Motor.Evaluar(new RespuestaVisionEstructurada(
                true, true, CalidadImagenVision.Aceptable,
                new[]
                {
                    new ObservacionObjetoVisual("charging cable", "electronic", EstadoObservableObjeto.LimpioYVacio, 1, 0.92m),
                    new ObservacionObjetoVisual("Xidoxx charging cable box", "cardboard", EstadoObservableObjeto.LimpioYVacio, 1, 0.90m),
                },
                "lfm3-vl:latest", 4200));

            Assert.AreEqual(CombinacionCanecas.C02_Blanca, foto41.Combinacion);
            Assert.IsTrue(foto41.PuedeAbrirCanecas);
            Assert.AreEqual(1, foto41.CanecasRequeridas.Count);
            Assert.AreEqual(EstadoClasificacionObjeto.FueraDeCatalogo, foto41.Objetos[0].Estado);
            Assert.AreEqual("B09", foto41.Objetos[1].Material35?.Codigo);

            // Caja externa limpia de medicamento (sin blíster ni pastillas) -> BLANCA (B09)
            var cajaMed = Motor.Evaluar(new RespuestaVisionEstructurada(
                true, true, CalidadImagenVision.Aceptable,
                new[] { new ObservacionObjetoVisual("empty medicine cardboard box", "cardboard", EstadoObservableObjeto.LimpioYVacio, 1, 0.91m) },
                "lfm3-vl:latest", 1200));
            Assert.AreEqual(CombinacionCanecas.C02_Blanca, cajaMed.Combinacion);
            Assert.AreEqual("B09", cajaMed.Objetos[0].Material35?.Codigo);

            // Blíster con pastillas y medicamentos vencidos -> SIEMPRE GESTION_ESPECIAL (0 aperturas)
            var blister = Motor.Evaluar(new RespuestaVisionEstructurada(
                true, true, CalidadImagenVision.Aceptable,
                new[] { new ObservacionObjetoVisual("blister pack of pills", "mixed", EstadoObservableObjeto.LimpioYVacio, 1, 0.95m) },
                "lfm3-vl:latest", 1200));
            Assert.IsFalse(blister.PuedeAbrirCanecas);
            Assert.AreEqual(EstadoClasificacionObjeto.FueraDeCatalogo, blister.Objetos[0].Estado);
            StringAssert.Contains(blister.Objetos[0].Motivo, "GESTION_ESPECIAL");

            var vencido = Motor.Evaluar(new RespuestaVisionEstructurada(
                true, true, CalidadImagenVision.Aceptable,
                new[] { new ObservacionObjetoVisual("expired medicine box", "mixed", EstadoObservableObjeto.LimpioYVacio, 1, 0.92m) },
                "lfm3-vl:latest", 1200));
            Assert.IsFalse(vencido.PuedeAbrirCanecas);
            Assert.AreEqual(EstadoClasificacionObjeto.FueraDeCatalogo, vencido.Objetos[0].Estado);
        }

        [TestMethod]
        public async Task Fase9_OllamaApagadoOModeloNoInstalado_RespondeModoSeguroSinExcepcionNoControlada()
        {
            using var httpApagado = new HttpClient(new HandlerSimulado(_ => throw new HttpRequestException("Connection refused")));
            using var cliApagado = new ClienteVisionOllamaMultirresiduo("http://127.0.0.1:11434", "lfm3-vl:latest", TimeSpan.FromSeconds(5), httpClient: httpApagado);
            var obsApagado = await cliApagado.ObservarAsync(JpegStub);
            Assert.IsFalse(obsApagado.JsonValido);
            Assert.IsFalse(Motor.Evaluar(obsApagado).PuedeAbrirCanecas);

            using var http404 = new HttpClient(new HandlerSimulado(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))));
            using var cli404 = new ClienteVisionOllamaMultirresiduo("http://127.0.0.1:11434", "modelo-inexistente:latest", TimeSpan.FromSeconds(5), httpClient: http404);
            var obs404 = await cli404.ObservarAsync(JpegStub);
            Assert.IsFalse(obs404.JsonValido);
            StringAssert.Contains(obs404.ErrorValidacion ?? "", "404");
        }

        [TestMethod]
        public async Task Fase9_RespuestaVaciaYTimeout_BloqueanAperturaYNoCuelgan()
        {
            using var httpVacio = new HttpClient(new HandlerSimulado(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"message\":{\"content\":\"\"}}") })));
            using var cliVacio = new ClienteVisionOllamaMultirresiduo("http://127.0.0.1:11434", "lfm3-vl:latest", TimeSpan.FromSeconds(5), httpClient: httpVacio);
            var obsVacio = await cliVacio.ObservarAsync(JpegStub);
            Assert.IsFalse(obsVacio.JsonValido);
            Assert.IsFalse(Motor.Evaluar(obsVacio).PuedeAbrirCanecas);

            using var httpLento = new HttpClient(new HandlerSimulado(async ct =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }));
            using var cliLento = new ClienteVisionOllamaMultirresiduo("http://127.0.0.1:11434", "lfm3-vl:latest", TimeSpan.FromMilliseconds(150), httpClient: httpLento);
            var obsTimeout = await cliLento.ObservarAsync(JpegStub);
            Assert.IsFalse(obsTimeout.JsonValido);
            StringAssert.Contains(obsTimeout.ErrorValidacion ?? "", "Tiempo máximo");
        }

        [TestMethod]
        public void Fase9_ImagenDanadaDemasiadoGrandeYObjetosAmbiguos_ManajanConSeguridad()
        {
            Assert.IsFalse(ValidadorJsonVision.ValidarFormatoImagen(Array.Empty<byte>(), out _, out _));
            Assert.IsFalse(ValidadorJsonVision.ValidarFormatoImagen(new byte[] { 1, 2, 3, 4, 5 }, out _, out _));

            byte[] gigante = new byte[(12 * 1024 * 1024) + 10];
            gigante[0] = 0xFF; gigante[1] = 0xD8; gigante[2] = 0xFF;
            Assert.IsFalse(ValidadorJsonVision.ValidarFormatoImagen(gigante, out _, out string? errTam));
            StringAssert.Contains(errTam ?? "", "tamaño");

            var ambigua = Motor.Evaluar(new RespuestaVisionEstructurada(
                true, true, CalidadImagenVision.Aceptable,
                new[]
                {
                    new ObservacionObjetoVisual("partially occluded object", "mixed", EstadoObservableObjeto.Desconocido, 1, 0.42m, RequiereAclaracion: true),
                    new ObservacionObjetoVisual("unknown wrapper", "plastic", EstadoObservableObjeto.Desconocido, 1, 0.50m, RequiereAclaracion: true),
                },
                "lfm3-vl:latest", 900));
            Assert.IsFalse(ambigua.PuedeAbrirCanecas);
            Assert.IsTrue(ambigua.RequiereConfirmacion || ambigua.EstadoGlobal == EstadoGlobalAnalisis.NoIdentificado);
        }

        [TestMethod]
        public async Task Fase9_Esp32DesconectadoYRecuperacionTrasReconexion_RegistraFalloHonestoYLuegoOpera()
        {
            var resp = new RespuestaVisionEstructurada(
                true, true, CalidadImagenVision.Aceptable,
                new[] { new ObservacionObjetoVisual("banana peel", "organic", EstadoObservableObjeto.LimpioYVacio, 1, 0.95m) },
                "lfm3-vl:latest", 300);

            var ejecutor = new EjecutorConFalloYReconexion();
            var gestor = new GestorSesionMultirresiduo(new ProveedorStubLocal(resp), Motor, ejecutor);

            await gestor.AnalizarFotografiaAsync(JpegStub);
            var res1 = await gestor.EjecutarAperturaSecuencialAsync("op-esp-1", Estacion, permitirSoloSimulador: true);
            Assert.AreEqual(1, res1.Count);
            Assert.IsFalse(res1[0].AperturaConfirmada);
            Assert.AreEqual(EstadoEjecucionApertura.Desconectada, res1[0].EstadoControlador);
            Assert.AreEqual(EstadoSesionDeposito.ERROR_SEGURO, gestor.Maquina.EstadoActual);

            ejecutor.Conectado = true;
            byte[] jpeg2 = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x02, 0x55, 0x66 };
            await gestor.AnalizarFotografiaAsync(jpeg2);
            var res2 = await gestor.EjecutarAperturaSecuencialAsync("op-esp-2", Estacion, permitirSoloSimulador: true);
            Assert.AreEqual(1, res2.Count);
            Assert.IsTrue(res2[0].AperturaConfirmada);
            Assert.AreEqual(EstadoSesionDeposito.FINALIZADO, gestor.Maquina.EstadoActual);
        }

        private sealed class HandlerSimulado : HttpMessageHandler
        {
            private readonly Func<CancellationToken, Task<HttpResponseMessage>> _fn;
            
            public HandlerSimulado(Func<CancellationToken, Task<HttpResponseMessage>> fn) => _fn = fn;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => _fn(cancellationToken);
        }

        private sealed class ProveedorStubLocal : IProveedorVisionMultirresiduo
        {
            private readonly RespuestaVisionEstructurada _r;
            public ProveedorStubLocal(RespuestaVisionEstructurada r) => _r = r;
            public string NombreProveedor => _r.ModeloUtilizado;
            public Task<RespuestaVisionEstructurada> ObservarAsync(byte[] bytesImagen, CancellationToken ct = default) => Task.FromResult(_r);
        }

        private sealed class EjecutorConFalloYReconexion : IEjecutorAperturas
        {
            public bool EsSimulado => true;
            public bool Conectado { get; set; }
            public Task<ResultadoEjecucionApertura> AbrirAsync(ComandoApertura comando, CancellationToken ct = default) =>
                Task.FromResult(Conectado
                    ? new ResultadoEjecucionApertura(EstadoEjecucionApertura.Completada, new[] { TipoEventoDispositivo.Apertura, TipoEventoDispositivo.Cierre })
                    : new ResultadoEjecucionApertura(EstadoEjecucionApertura.Desconectada, Array.Empty<TipoEventoDispositivo>()));
        }
    }
}