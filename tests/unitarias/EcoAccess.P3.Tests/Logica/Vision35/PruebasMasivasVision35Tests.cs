using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// Batería masiva determinista (FASE 7): cubre los 35 materiales individuales, las 7 combinaciones C01–C07,
    /// escenas con 4+ objetos, 10 objetos de la misma categoría = 1 caneca, regresiones del motor de reglas,
    /// seguridad ante discrepancias entre modelos (LFM3-VL vs. Qwen3-VL), modos de apertura secuencial/simultáneo,
    /// honestidad ante ausencia de sensores físicos y recuperación ante fallos de hardware, visión y Oracle.
    /// </summary>
    [TestClass]
    public sealed class PruebasMasivasVision35Tests
    {
        private static readonly byte[] JpegValidoA = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x10, 0x11 };
        private static readonly byte[] JpegValidoB = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x20, 0x22 };
        private static readonly MotorReglasMultirresiduo Motor = new();

        private static readonly IReadOnlyList<CanecaEstacion> EstacionCompleta = new[]
        {
            new CanecaEstacion(1093, "Blanca", CategoriaMaterial.Aprovechable, true, null, null),
            new CanecaEstacion(1400, "Verde", CategoriaMaterial.OrganicoAprovechable, true, null, null),
            new CanecaEstacion(1401, "Negra", CategoriaMaterial.NoAprovechable, true, null, null),
        };

        private static RespuestaVisionEstructurada Obs(string modelo, params ObservacionObjetoVisual[] objetos) =>
            new(true, objetos.Length > 0, CalidadImagenVision.Aceptable, objetos, modelo, 220);

        private static ObservacionObjetoVisual Item(
            string nombre,
            string material,
            EstadoObservableObjeto estado = EstadoObservableObjeto.LimpioYVacio,
            int cantidad = 1,
            decimal confianza = 0.92m,
            bool etiqueta = false,
            bool pantalla = false,
            bool reutilizable = false,
            bool aclarar = false) =>
            new(nombre, material, estado, cantidad, confianza, etiqueta, pantalla, reutilizable, aclarar);

        // ─── 1. Los 35 materiales prioritarios individuales ──────────────────────
        [TestMethod]
        public void TodosLos35MaterialesIndividuales_ClasificanExactamenteEnSuCodigoYCaneca()
        {
            var casos = new (string Codigo, string Nombre, string Material, EstadoObservableObjeto Estado, CategoriaMaterial Caneca, bool EsConfirmacion)[]
            {
                // VERDE (V01..V10)
                ("V01", "banana peel", "organic", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.OrganicoAprovechable, false),
                ("V02", "lemon peel", "organic", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.OrganicoAprovechable, false),
                ("V03", "potato peel", "organic", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.OrganicoAprovechable, false),
                ("V04", "avocado peel", "organic", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.OrganicoAprovechable, false),
                ("V05", "half lemon", "organic", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.OrganicoAprovechable, false),
                ("V06", "tomato scraps", "organic", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.OrganicoAprovechable, false),
                ("V07", "half kiwi", "organic", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.OrganicoAprovechable, false),
                ("V08", "dry leaves", "organic", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.OrganicoAprovechable, false),
                ("V09", "grass clippings", "organic", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.OrganicoAprovechable, false),
                ("V10", "orange peel", "organic", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.OrganicoAprovechable, false),
                // BLANCA (B01..B13)
                ("B01", "plastic water bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.Aprovechable, false),
                ("B02", "plastic soda bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.Aprovechable, false),
                ("B03", "plastic cap", "plastic", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.Aprovechable, false),
                ("B04", "clean plastic cup", "plastic", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.Aprovechable, false),
                ("B05", "clean plastic container", "plastic", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.Aprovechable, false),
                ("B06", "clean glass jar", "glass", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.Aprovechable, false),
                ("B07", "empty glass bottle", "glass", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.Aprovechable, false),
                ("B08", "notebook paper", "paper", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.Aprovechable, false),
                ("B09", "clean cardboard box", "cardboard", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.Aprovechable, false),
                ("B10", "juice box", "tetrapak", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.Aprovechable, false),
                ("B11", "milk carton", "tetrapak", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.Aprovechable, false),
                ("B12", "aluminum can", "metal", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.Aprovechable, false),
                ("B13", "clean plastic film", "plastic", EstadoObservableObjeto.LimpioYVacio, CategoriaMaterial.Aprovechable, true),
                // NEGRA (N01..N12)
                ("N01", "used napkin", "sanitary", EstadoObservableObjeto.Contaminado, CategoriaMaterial.NoAprovechable, false),
                ("N02", "used toilet paper", "sanitary", EstadoObservableObjeto.Contaminado, CategoriaMaterial.NoAprovechable, false),
                ("N03", "greasy pizza box", "cardboard", EstadoObservableObjeto.Contaminado, CategoriaMaterial.NoAprovechable, false),
                ("N04", "cigarette butt", "cigarette", EstadoObservableObjeto.Contaminado, CategoriaMaterial.NoAprovechable, false),
                ("N05", "used condom", "sanitary", EstadoObservableObjeto.Contaminado, CategoriaMaterial.NoAprovechable, false),
                ("N06", "used straw", "plastic", EstadoObservableObjeto.Contaminado, CategoriaMaterial.NoAprovechable, false),
                ("N07", "styrofoam tray", "styrofoam", EstadoObservableObjeto.Contaminado, CategoriaMaterial.NoAprovechable, false),
                ("N08", "snack wrapper", "metallized_wrapper", EstadoObservableObjeto.Contaminado, CategoriaMaterial.NoAprovechable, false),
                ("N09", "dirty plastic cup", "plastic", EstadoObservableObjeto.Contaminado, CategoriaMaterial.NoAprovechable, false),
                ("N10", "chewing gum", "other", EstadoObservableObjeto.Contaminado, CategoriaMaterial.NoAprovechable, false),
                ("N11", "wet wipe", "sanitary", EstadoObservableObjeto.Contaminado, CategoriaMaterial.NoAprovechable, false),
                ("N12", "used face mask", "sanitary", EstadoObservableObjeto.Contaminado, CategoriaMaterial.NoAprovechable, false),
            };

            Assert.AreEqual(35, casos.Length, "Deben probarse exactamente los 35 materiales prioritarios.");
            Assert.AreEqual(35, casos.Select(c => c.Codigo).Distinct().Count(), "Códigos únicos.");

            foreach (var c in casos)
            {
                var r = Motor.Evaluar(Obs("test-vl", Item(c.Nombre, c.Material, c.Estado)));
                Assert.AreEqual(1, r.Objetos.Count, $"Fallo cantidad de objetos en {c.Codigo}");
                Assert.AreEqual(c.Codigo, r.Objetos[0].Material35?.Codigo, $"Código esperado {c.Codigo} para '{c.Nombre}', obtenido '{r.Objetos[0].Material35?.Codigo}'.");
                Assert.AreEqual(c.Caneca, r.Objetos[0].CanecaAsignada, $"Caneca esperada {c.Caneca} para {c.Codigo}.");

                if (c.EsConfirmacion)
                {
                    Assert.IsTrue(r.RequiereConfirmacion, $"{c.Codigo} debe exigir confirmación humana.");
                    Assert.IsFalse(r.PuedeAbrirCanecas, $"{c.Codigo} no debe abrir automáticamente.");
                }
                else
                {
                    Assert.IsTrue(r.PuedeAbrirCanecas, $"{c.Codigo} debe permitir apertura segura.");
                    Assert.AreEqual(1, r.CanecasRequeridas.Count, $"{c.Codigo} debe habilitar exactamente 1 caneca.");
                    Assert.AreEqual(c.Caneca, r.CanecasRequeridas[0]);
                }
            }
        }

        // ─── 2. Diez objetos misma categoría = 1 caneca + mezclas 4+ objetos ─────
        [TestMethod]
        public void DiezObjetosDeLaMismaCategoria_AbrenUnaSolaCanecaSinDuplicados()
        {
            var diezBotellas = Enumerable.Range(1, 10)
                .Select(_ => Item("plastic water bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio))
                .ToArray();

            var r = Motor.Evaluar(Obs("test-vl", diezBotellas));
            Assert.AreEqual(EstadoGlobalAnalisis.Clasificado, r.EstadoGlobal);
            Assert.AreEqual(10, r.TotalObjetosDetectados);
            Assert.AreEqual(1, r.CanecasRequeridas.Count, "10 botellas limpias deben abrir exactamente 1 caneca (BLANCA).");
            Assert.AreEqual(CombinacionCanecas.C02_Blanca, r.Combinacion);
        }

        [TestMethod]
        public void EscenaConSeisObjetosDeTresCategorias_AbreExactamenteTresCanecasSinDuplicar()
        {
            var r = Motor.Evaluar(Obs("test-vl",
                Item("banana peel", "organic"),
                Item("orange peel", "organic"),
                Item("plastic water bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio),
                Item("aluminum can", "metal", EstadoObservableObjeto.LimpioYVacio),
                Item("used napkin", "sanitary", EstadoObservableObjeto.Contaminado),
                Item("cigarette butt", "cigarette", EstadoObservableObjeto.Contaminado)));

            Assert.AreEqual(6, r.TotalObjetosDetectados);
            Assert.AreEqual(3, r.CanecasRequeridas.Count);
            Assert.AreEqual(CombinacionCanecas.C07_VerdeBlancaNegra, r.Combinacion);
            Assert.IsTrue(r.PuedeAbrirCanecas);
        }

        [TestMethod]
        public void ObjetoUnicoAmbiguo_NuncaAbreVariasCanecas()
        {
            var r = Motor.Evaluar(Obs("test-vl",
                Item("unknown container", "plastic", EstadoObservableObjeto.Desconocido, confianza: 0.55m, aclarar: true)));

            Assert.IsFalse(r.PuedeAbrirCanecas);
            Assert.IsTrue(r.RequiereConfirmacion || r.EstadoGlobal == EstadoGlobalAnalisis.NoIdentificado);
            Assert.IsTrue(r.CanecasRequeridas.Count <= 1, "Un objeto único ambiguo jamás debe abrir múltiples canecas.");
        }

        // ─── 3. Regresiones FASE 3 (EsReutilizable, Etiqueta, Vidrio roto) ───────
        [TestMethod]
        public void RegresionFase3_CajaCartonMarcadaReutilizablePorVlm_SigueClasificandoEnBlancaB09()
        {
            var r = Motor.Evaluar(Obs("lfm3-vl:latest",
                Item("cardboard box", "cardboard", EstadoObservableObjeto.LimpioYVacio, reutilizable: true)));

            Assert.AreEqual(CombinacionCanecas.C02_Blanca, r.Combinacion);
            Assert.IsTrue(r.PuedeAbrirCanecas);
            Assert.AreEqual("B09", r.Objetos[0].Material35?.Codigo);
        }

        [TestMethod]
        public void RegresionFase3_CubetaHieloReutilizable_SigueProtegidaComoNoEsResiduo()
        {
            var r = Motor.Evaluar(Obs("lfm3-vl:latest",
                Item("plastic ice cube tray", "plastic", EstadoObservableObjeto.LimpioYVacio, reutilizable: true)));

            Assert.IsFalse(r.PuedeAbrirCanecas);
            Assert.AreEqual(CombinacionCanecas.Ninguna, r.Combinacion);
            Assert.AreEqual(EstadoClasificacionObjeto.NoEsResiduo, r.Objetos[0].Estado);
        }

        [TestMethod]
        public void RegresionFase3_BotellaPlasticaAplastada_EsBlanca_PeroVidrioRotoExigeConfirmacion()
        {
            var plasticoAplastado = Motor.Evaluar(Obs("test-vl",
                Item("crushed plastic bottle", "plastic_pet", EstadoObservableObjeto.DanadoORoto)));
            Assert.AreEqual(CombinacionCanecas.C02_Blanca, plasticoAplastado.Combinacion);
            Assert.IsTrue(plasticoAplastado.PuedeAbrirCanecas);

            var vidrioRoto = Motor.Evaluar(Obs("test-vl",
                Item("broken glass bottle", "glass", EstadoObservableObjeto.DanadoORoto)));
            Assert.IsFalse(vidrioRoto.PuedeAbrirCanecas);
            Assert.AreEqual(0, vidrioRoto.CanecasRequeridas.Count);
            Assert.AreEqual(EstadoClasificacionObjeto.FueraDeCatalogo, vidrioRoto.Objetos[0].Estado);
        }

        // ─── 4. Seguridad de consenso entre dos modelos (FASE 2) ─────────────────
        [TestMethod]
        public void ComparadorModelos_DiscrepanciaEntreLfm3yQwen_BloqueaAperturaAutomatica()
        {
            var dictA = Motor.Evaluar(Obs("lfm3-vl:latest", Item("banana peel", "organic")));
            var dictB = Motor.Evaluar(Obs("qwen3-vl:4b", Item("plastic water bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio)));

            var consenso = ComparadorModelosVision.CombinarConSeguridad(dictA, dictB);

            Assert.IsFalse(consenso.CoincidenCanecas);
            Assert.IsFalse(consenso.PuedeAbrirConSeguridad);
            Assert.IsFalse(consenso.DictamenSeguroFinal.PuedeAbrirCanecas);
            Assert.IsTrue(consenso.DictamenSeguroFinal.RequiereConfirmacion);
            Assert.AreEqual("DISCREPANCIA_MODELOS", consenso.DictamenSeguroFinal.ErrorTecnico);
        }

        [TestMethod]
        public void ComparadorModelos_ConsensoVerificado_AutorizaAperturaSegura()
        {
            var dictA = Motor.Evaluar(Obs("lfm3-vl:latest",
                Item("banana peel", "organic"),
                Item("plastic bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio)));
            var dictB = Motor.Evaluar(Obs("qwen3-vl:4b",
                Item("banana peel", "organic"),
                Item("plastic water bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio)));

            var consenso = ComparadorModelosVision.CombinarConSeguridad(dictA, dictB);

            Assert.IsTrue(consenso.CoincidenCanecas);
            Assert.IsTrue(consenso.PuedeAbrirConSeguridad);
            Assert.IsTrue(consenso.DictamenSeguroFinal.PuedeAbrirCanecas);
            Assert.AreEqual(CombinacionCanecas.C04_VerdeBlanca, consenso.DictamenSeguroFinal.Combinacion);
        }

        // ─── 5. Modos de apertura (Secuencial vs Simultáneo) y honestidad de sensores ─
        [TestMethod]
        public async Task AperturaSimultaneaSupervisada_AbreTresCanecasSinDuplicarYRegistraHonestidadSinSensores()
        {
            var resp = Obs("test-vl",
                Item("banana peel", "organic"),
                Item("plastic water bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio),
                Item("used napkin", "sanitary", EstadoObservableObjeto.Contaminado));

            var sim = new SimuladorEstacionTresCanecas(
                new Dictionary<int, DestinoResiduo>
                {
                    [1093] = DestinoResiduo.Blanca,
                    [1400] = DestinoResiduo.Verde,
                    [1401] = DestinoResiduo.Negra,
                },
                TimeProvider.System, TimeSpan.Zero);

            var gestor = new GestorSesionMultirresiduo(new ProveedorFijo(resp), Motor, sim);
            var analisis = await gestor.AnalizarFotografiaAsync(JpegValidoA);
            Assert.AreEqual(CombinacionCanecas.C07_VerdeBlancaNegra, analisis.Combinacion);

            var aperturas = await gestor.EjecutarAperturaSecuencialAsync(
                "op-sim-001", EstacionCompleta, permitirSoloSimulador: true,
                modoApertura: ModoAperturaCanecas.SimultaneaSupervisada,
                hardwareConSensoresFisicos: false);

            Assert.AreEqual(3, aperturas.Count);
            Assert.IsTrue(aperturas.All(a => a.AperturaConfirmada && a.EsSimulado && !a.TieneSensorFisico && a.ConfirmacionObservacionalRequerida));
            Assert.AreEqual(EstadoSesionDeposito.FINALIZADO, gestor.Maquina.EstadoActual);
        }

        [TestMethod]
        public async Task HardwareFisicoSinSensores_ReportaExplicitamenteFaltaDeSensorYNoPermiteSinSupervision()
        {
            var resp = Obs("test-vl", Item("banana peel", "organic"));
            var ejecutorFisico = new EjecutorControlado(esSimulado: false, EstadoEjecucionApertura.Completada);
            var gestor = new GestorSesionMultirresiduo(new ProveedorFijo(resp), Motor, ejecutorFisico);

            await gestor.AnalizarFotografiaAsync(JpegValidoA);

            // Sin autorización supervisada (permitirSoloSimulador = true) DEBE bloquear el motor físico.
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                gestor.EjecutarAperturaSecuencialAsync("op-fis-bloqueada", EstacionCompleta, permitirSoloSimulador: true));
            Assert.AreEqual(EstadoSesionDeposito.ERROR_SEGURO, gestor.Maquina.EstadoActual);

            // Recuperación tras reinicio seguro y ejecución en MODO 2 supervisado sin sensores físicos
            await gestor.AnalizarFotografiaAsync(JpegValidoB);
            var resultados = await gestor.EjecutarAperturaSecuencialAsync(
                "op-fis-supervisada", EstacionCompleta, permitirSoloSimulador: false,
                modoApertura: ModoAperturaCanecas.SecuencialGuiada,
                hardwareConSensoresFisicos: false);

            Assert.AreEqual(1, resultados.Count);
            Assert.IsFalse(resultados[0].EsSimulado);
            Assert.IsFalse(resultados[0].TieneSensorFisico);
            Assert.IsTrue(resultados[0].ConfirmacionObservacionalRequerida);
            StringAssert.Contains(resultados[0].Mensaje, "sin sensores de posición");
        }

        // ─── 6. Fallos de hardware, modelo, imágenes corruptas y cola Oracle ─────
        [TestMethod]
        public async Task FalloHardwareDuranteAperturaYCierre_MantieneEstadoSeguroYHonesto()
        {
            var resp = Obs("test-vl", Item("used napkin", "sanitary", EstadoObservableObjeto.Contaminado));

            // Caso A: Fallo durante apertura
            var gestorFalloAbrir = new GestorSesionMultirresiduo(
                new ProveedorFijo(resp), Motor, new EjecutorControlado(true, EstadoEjecucionApertura.FalloApertura));
            await gestorFalloAbrir.AnalizarFotografiaAsync(JpegValidoA);
            var resAbrir = await gestorFalloAbrir.EjecutarAperturaSecuencialAsync("op-fallo-abrir", EstacionCompleta, permitirSoloSimulador: true);
            Assert.AreEqual(1, resAbrir.Count);
            Assert.IsFalse(resAbrir[0].AperturaConfirmada);
            Assert.AreEqual(EstadoSesionDeposito.ERROR_SEGURO, gestorFalloAbrir.Maquina.EstadoActual);

            // Caso B: Fallo durante cierre (apertura ocurrió pero cierre falló)
            var gestorFalloCerrar = new GestorSesionMultirresiduo(
                new ProveedorFijo(resp), Motor, new EjecutorControlado(true, EstadoEjecucionApertura.FalloCierre));
            await gestorFalloCerrar.AnalizarFotografiaAsync(JpegValidoB);
            var resCerrar = await gestorFalloCerrar.EjecutarAperturaSecuencialAsync("op-fallo-cerrar", EstacionCompleta, permitirSoloSimulador: true);
            Assert.AreEqual(1, resCerrar.Count);
            Assert.IsTrue(resCerrar[0].AperturaConfirmada);
            Assert.IsFalse(resCerrar[0].CierreConfirmado);
            StringAssert.Contains(resCerrar[0].Mensaje, "fallo al cerrar");
        }

        [TestMethod]
        public async Task CaidaDeModeloEImagenCorrupta_PasanAModoSeguroYSeRecuperanSinReiniciarApp()
        {
            // 1. Imagen corrupta -> ERROR_SEGURO -> 0 aperturas
            var gestor = new GestorSesionMultirresiduo(new ProveedorQueFalla(), Motor);
            var resCorrupta = await gestor.AnalizarFotografiaAsync(new byte[] { 0x00, 0x01, 0x02, 0x03 });
            Assert.AreEqual(EstadoGlobalAnalisis.RequiereRepeticion, resCorrupta.EstadoGlobal);
            Assert.IsFalse(resCorrupta.PuedeAbrirCanecas);
            Assert.AreEqual(EstadoSesionDeposito.ERROR_SEGURO, gestor.Maquina.EstadoActual);

            // 2. Caída HTTP de Ollama -> ModoSeguroError -> 0 aperturas
            var resCaida = await gestor.AnalizarFotografiaAsync(JpegValidoA);
            Assert.AreEqual(EstadoGlobalAnalisis.ModoSeguroError, resCaida.EstadoGlobal);
            Assert.IsFalse(resCaida.PuedeAbrirCanecas);
            Assert.AreEqual(EstadoSesionDeposito.ERROR_SEGURO, gestor.Maquina.EstadoActual);
        }

        [TestMethod]
        public async Task ColaOfflineOracle_ReintentoDuplicadoNoDuplicaNiPierdeRegistros()
        {
            string ruta = Path.Combine(Path.GetTempPath(), "ecoaccess_masiva_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var cola = new ColaLocalOperaciones(ruta);
                var op = new OperacionPendienteLocal(
                    "op-unica-001", DateTime.UtcNow, "qr",
                    new[] { "V01", "B01", "N01" },
                    new[] { "VERDE", "BLANCA", "NEGRA" },
                    new[] { "VERDE", "BLANCA", "NEGRA" },
                    EstadoOperacionLocal.AperturaConfirmadaSimulada,
                    "lfm3-vl:latest | 1200ms");

                Assert.IsTrue(cola.Registrar(op));
                Assert.IsFalse(cola.Registrar(op), "Reintento con mismo IdOperacion no debe duplicar.");

                int intentos = 0;
                // Primera sincronización simula caída de Oracle
                var falloSync = await cola.SincronizarPendientesAsync((_, _) =>
                {
                    intentos++;
                    return Task.FromResult(false);
                });
                Assert.AreEqual(0, falloSync.SincronizadasAhora);
                Assert.AreEqual(1, falloSync.PendientesRestantes);

                // Segunda sincronización simula Oracle restaurado
                var okSync = await cola.SincronizarPendientesAsync((_, _) =>
                {
                    intentos++;
                    return Task.FromResult(true);
                });
                Assert.AreEqual(1, okSync.SincronizadasAhora);
                Assert.AreEqual(0, okSync.PendientesRestantes);
                Assert.AreEqual(2, intentos);
            }
            finally
            {
                if (File.Exists(ruta)) File.Delete(ruta);
            }
        }

        private sealed class ProveedorFijo : IProveedorVisionMultirresiduo
        {
            private readonly RespuestaVisionEstructurada _resp;
            public ProveedorFijo(RespuestaVisionEstructurada resp) => _resp = resp;
            public string NombreProveedor => _resp.ModeloUtilizado;
            public Task<RespuestaVisionEstructurada> ObservarAsync(byte[] bytesImagen, CancellationToken ct = default) => Task.FromResult(_resp);
        }

        private sealed class ProveedorQueFalla : IProveedorVisionMultirresiduo
        {
            public string NombreProveedor => "ollama-caido";
            public Task<RespuestaVisionEstructurada> ObservarAsync(byte[] bytesImagen, CancellationToken ct = default) =>
                throw new HttpRequestException("Conexión rechazada por Ollama.");
        }

        private sealed class EjecutorControlado : IEjecutorAperturas
        {
            private readonly EstadoEjecucionApertura _estado;
            public bool EsSimulado { get; }

            public EjecutorControlado(bool esSimulado, EstadoEjecucionApertura estado)
            {
                EsSimulado = esSimulado;
                _estado = estado;
            }

            public Task<ResultadoEjecucionApertura> AbrirAsync(ComandoApertura comando, CancellationToken ct = default)
            {
                IReadOnlyList<TipoEventoDispositivo> eventos = _estado switch
                {
                    EstadoEjecucionApertura.Completada => new[] { TipoEventoDispositivo.Apertura, TipoEventoDispositivo.CierreSinSensor },
                    EstadoEjecucionApertura.FalloCierre => new[] { TipoEventoDispositivo.Apertura },
                    _ => Array.Empty<TipoEventoDispositivo>(),
                };
                return Task.FromResult(new ResultadoEjecucionApertura(_estado, eventos));
            }
        }
    }
}
