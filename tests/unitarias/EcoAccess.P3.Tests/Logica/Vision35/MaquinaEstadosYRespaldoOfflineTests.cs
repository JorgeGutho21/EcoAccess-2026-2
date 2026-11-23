using System;
using System.Collections.Generic;
using System.IO;
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
    public sealed class MaquinaEstadosYRespaldoOfflineTests
    {
        private static readonly byte[] JpegValido = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x05, 0x06 };

        [TestMethod]
        public void MaquinaEstados_RechazaTransicionesInvalidasYRegistraAuditoria()
        {
            var m = new MaquinaEstadosDeposito();
            Assert.AreEqual(EstadoSesionDeposito.INACTIVO, m.EstadoActual);

            // Saltar directo de INACTIVO a ABRIENDO debe fallar.
            Assert.ThrowsExactly<InvalidOperationException>(() => m.Transicionar(EstadoSesionDeposito.ABRIENDO, "salto ilegal"));
            Assert.AreEqual(EstadoSesionDeposito.INACTIVO, m.EstadoActual);

            m.Transicionar(EstadoSesionDeposito.CAPTURANDO, "foto");
            m.Transicionar(EstadoSesionDeposito.ANALIZANDO, "ollama");
            m.Transicionar(EstadoSesionDeposito.REQUIERE_CONFIRMACION, "duda");
            m.Transicionar(EstadoSesionDeposito.CLASIFICADO, "confirmado");
            m.Transicionar(EstadoSesionDeposito.PREPARADO_PARA_APERTURA, "plan");
            m.Transicionar(EstadoSesionDeposito.ABRIENDO, "cmd");
            m.Transicionar(EstadoSesionDeposito.ESPERANDO_DEPOSITO, "tapa abierta");
            m.Transicionar(EstadoSesionDeposito.CERRANDO, "tapa cerrando");
            m.Transicionar(EstadoSesionDeposito.FINALIZADO, "listo");

            Assert.AreEqual(EstadoSesionDeposito.FINALIZADO, m.EstadoActual);
            Assert.AreEqual(9, m.Historial.Count);
        }

        [TestMethod]
        public async Task FlujoExtremoAExtremo_TresCanecas_SecuencialSimuladoYColaOfflineSinDuplicados()
        {
            // 1) Proveedor devuelve 3 residuos (VERDE + BLANCA + NEGRA)
            var respuesta = new RespuestaVisionEstructurada(
                true, true, CalidadImagenVision.Aceptable,
                new[]
                {
                    new ObservacionObjetoVisual("banana peel", "organic", EstadoObservableObjeto.LimpioYVacio, 1, 0.95m),
                    new ObservacionObjetoVisual("plastic water bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio, 2, 0.91m),
                    new ObservacionObjetoVisual("used napkin", "sanitary", EstadoObservableObjeto.Contaminado, 1, 0.90m),
                },
                "lfm3-vl:latest", 1800);

            var sim = new SimuladorEstacionTresCanecas(
                new Dictionary<int, DestinoResiduo>
                {
                    [1093] = DestinoResiduo.Blanca,
                    [1400] = DestinoResiduo.Verde,
                    [1401] = DestinoResiduo.Negra,
                },
                TimeProvider.System, TimeSpan.Zero);

            var gestor = new GestorSesionMultirresiduo(new ProveedorStub(respuesta), new MotorReglasMultirresiduo(), sim);

            // 2) Captura -> análisis -> clasificación
            var analisis = await gestor.AnalizarFotografiaAsync(JpegValido);
            Assert.AreEqual(EstadoGlobalAnalisis.Clasificado, analisis.EstadoGlobal);
            Assert.AreEqual(CombinacionCanecas.C07_VerdeBlancaNegra, analisis.Combinacion);
            Assert.AreEqual(4, analisis.TotalObjetosDetectados);
            Assert.AreEqual(3, analisis.CanecasRequeridas.Count);

            // 3) Apertura secuencial guiada simulada + prevención de duplicados por reintento
            var estacion = new[]
            {
                new CanecaEstacion(1093, "Blanca", CategoriaMaterial.Aprovechable, true, null, null),
                new CanecaEstacion(1400, "Verde", CategoriaMaterial.OrganicoAprovechable, true, null, null),
                new CanecaEstacion(1401, "Negra", CategoriaMaterial.NoAprovechable, true, null, null),
            };
            var aperturas = await gestor.EjecutarAperturaSecuencialAsync("e2e-001", estacion, permitirSoloSimulador: true);
            Assert.AreEqual(3, aperturas.Count);
            Assert.IsTrue(aperturas.All(a => a.AperturaConfirmada && a.CierreConfirmado && a.EsSimulado));
            Assert.AreEqual(EstadoSesionDeposito.FINALIZADO, gestor.Maquina.EstadoActual);

            // 4) Respaldo local ante desconexión temporal de Oracle + sincronización sin duplicados
            string archivoTemp = Path.Combine(Path.GetTempPath(), "ecoaccess_cola_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var cola = new ColaLocalOperaciones(archivoTemp);
                var op = new OperacionPendienteLocal(
                    "e2e-001", DateTime.UtcNow, "Visual",
                    analisis.Objetos.Select(o => o.Material35!.Codigo).ToArray(),
                    analisis.CanecasRequeridas.Select(MotorReglasMultirresiduo.NombreCortoCaneca).ToArray(),
                    aperturas.Select(a => MotorReglasMultirresiduo.NombreCortoCaneca(a.Caneca)).ToArray(),
                    EstadoOperacionLocal.AperturaConfirmadaSimulada,
                    "Prueba E2E simulada");

                Assert.IsTrue(cola.Registrar(op));
                Assert.IsFalse(cola.Registrar(op), "Idempotencia: no debe duplicar la misma clave.");

                // Primer intento falla (Oracle desconectado)
                var sync1 = await cola.SincronizarPendientesAsync((_, _) => Task.FromResult(false));
                Assert.AreEqual(1, sync1.PendientesRestantes);

                // Segundo intento recupera conexión
                int enviosOracle = 0;
                var sync2 = await cola.SincronizarPendientesAsync((_, _) => { enviosOracle++; return Task.FromResult(true); });
                Assert.AreEqual(1, sync2.SincronizadasAhora);
                Assert.AreEqual(0, sync2.PendientesRestantes);

                // Tercer intento: 0 duplicados enviados
                var sync3 = await cola.SincronizarPendientesAsync((_, _) => { enviosOracle++; return Task.FromResult(true); });
                Assert.AreEqual(0, sync3.SincronizadasAhora);
                Assert.AreEqual(1, enviosOracle);
            }
            finally
            {
                if (File.Exists(archivoTemp)) File.Delete(archivoTemp);
            }
        }

        [TestMethod]
        public async Task EjecutorEvaluacion35_NoMarcaAprobadaEvaluacionIncompletaYFiltraCorrectamente()
        {
            string dirTemp = Path.Combine(Path.GetTempPath(), "eco_eval35_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dirTemp);
            try
            {
                string rutaImg = Path.Combine(dirTemp, "banano.jpg");
                File.WriteAllBytes(rutaImg, JpegValido);
                var entrada = new EntradaDataset35(
                    "CASE_01", "banano.jpg", Compat.ToHexString(Compat.Sha256(JpegValido)),
                    ProcedenciaImagen.RealCatalogo, TipoEscenarioDataset.Individual, "C01_Verde",
                    new[] { new AnotacionObjeto35("V01", "Cáscara de banano", "VERDE") });

                var prov = new ProveedorStub(new RespuestaVisionEstructurada(
                    true, true, CalidadImagenVision.Aceptable,
                    new[] { new ObservacionObjetoVisual("banana peel", "organic", EstadoObservableObjeto.LimpioYVacio, 1, 0.94m) },
                    "sim", 120));

                var ejecutor = new EjecutorEvaluacion35(prov);
                var informe = await ejecutor.EjecutarEntradasAsync(new[] { entrada }, dirTemp);

                Assert.AreEqual(1, informe.TotalEjecutado);
                Assert.AreEqual(1, informe.TotalAprobado);
                Assert.AreEqual(0, informe.FalsasAperturas);
                // Regla de Sección 12: 1 caso ejecutado != 3.500 -> NO APROBADO (EVALUACIÓN INCOMPLETA)
                StringAssert.Contains(informe.CriteriosAceptacion[0].Estado, "NO APROBADO");
            }
            finally
            {
                if (Directory.Exists(dirTemp)) Directory.Delete(dirTemp, true);
            }
        }

        private sealed class ProveedorStub : IProveedorVisionMultirresiduo
        {
            private readonly RespuestaVisionEstructurada _r;
            public ProveedorStub(RespuestaVisionEstructurada r) => _r = r;
            public string NombreProveedor => "stub-e2e";
            public Task<RespuestaVisionEstructurada> ObservarAsync(byte[] bytesImagen, CancellationToken ct = default) => Task.FromResult(_r);
        }
    }
}
