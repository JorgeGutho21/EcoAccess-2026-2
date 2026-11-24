using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
    /// Verifica los 30 casos esenciales T001–T030 de la matriz multirresiduo (Sección 8),
    /// las 7 combinaciones C01–C07, deduplicación de canecas, validación JSON estricta y seguridad de hardware.
    /// </summary>
    [TestClass]
    public sealed class MatrizMultirresiduoT001aT030Tests
    {
        private static readonly byte[] JpegValido = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x02, 0x03 };
        private static readonly MotorReglasMultirresiduo Motor = new();

        private static RespuestaVisionEstructurada Obs(params ObservacionObjetoVisual[] objetos) =>
            new(true, objetos.Length > 0, CalidadImagenVision.Aceptable, objetos, "test-vl", 150);

        private static ObservacionObjetoVisual Item(string nombre, string material, EstadoObservableObjeto estado = EstadoObservableObjeto.LimpioYVacio,
            int cantidad = 1, decimal confianza = 0.92m, bool etiqueta = false, bool pantalla = false, bool reutilizable = false, bool aclarar = false) =>
            new(nombre, material, estado, cantidad, confianza, etiqueta, pantalla, reutilizable, aclarar);

        // ─── T001–T003: Una categoría (C01, C02, C03) ─────────────────────────────
        [TestMethod]
        public void T001_CascaraDeBanano_SoloVerde()
        {
            var r = Motor.Evaluar(Obs(Item("banana peel", "organic")));
            Assert.AreEqual(CombinacionCanecas.C01_Verde, r.Combinacion);
            Assert.IsTrue(r.PuedeAbrirCanecas);
            Assert.AreEqual("V01", r.Objetos[0].Material35!.Codigo);
        }

        [TestMethod]
        public void T002_BotellaPlasticaLimpia_SoloBlanca()
        {
            var r = Motor.Evaluar(Obs(Item("clean plastic water bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio)));
            Assert.AreEqual(CombinacionCanecas.C02_Blanca, r.Combinacion);
            Assert.IsTrue(r.PuedeAbrirCanecas);
            Assert.AreEqual("B01", r.Objetos[0].Material35!.Codigo);
        }

        [TestMethod]
        public void T003_ServilletaUsada_SoloNegra()
        {
            var r = Motor.Evaluar(Obs(Item("used napkin", "sanitary", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C03_Negra, r.Combinacion);
            Assert.IsTrue(r.PuedeAbrirCanecas);
            Assert.AreEqual("N01", r.Objetos[0].Material35!.Codigo);
        }

        // ─── T004–T006: C04 VERDE + BLANCA ───────────────────────────────────────
        [TestMethod]
        public void T004_CascaraLimonYBotellaPlastica_VerdeYBlanca()
        {
            var r = Motor.Evaluar(Obs(Item("lemon peel", "organic"), Item("plastic bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio)));
            Assert.AreEqual(CombinacionCanecas.C04_VerdeBlanca, r.Combinacion);
            Assert.AreEqual(2, r.CanecasRequeridas.Count);
        }

        [TestMethod]
        public void T005_CascaraPapaYCartonLimpio_VerdeYBlanca()
        {
            var r = Motor.Evaluar(Obs(Item("potato peel", "organic"), Item("clean cardboard", "cardboard", EstadoObservableObjeto.LimpioYVacio)));
            Assert.AreEqual(CombinacionCanecas.C04_VerdeBlanca, r.Combinacion);
        }

        [TestMethod]
        public void T006_HojasSecasYBotellaVidrio_VerdeYBlanca()
        {
            var r = Motor.Evaluar(Obs(Item("dry leaves", "organic"), Item("empty glass bottle", "glass", EstadoObservableObjeto.LimpioYVacio)));
            Assert.AreEqual(CombinacionCanecas.C04_VerdeBlanca, r.Combinacion);
        }

        // ─── T007–T008: C05 VERDE + NEGRA ────────────────────────────────────────
        [TestMethod]
        public void T007_CascaraAguacateYColillaApagada_VerdeYNegra()
        {
            var r = Motor.Evaluar(Obs(Item("avocado peel", "organic"), Item("cigarette butt", "mixed", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C05_VerdeNegra, r.Combinacion);
        }

        [TestMethod]
        public void T008_RestosTomateYServilletaUsada_VerdeYNegra()
        {
            var r = Motor.Evaluar(Obs(Item("tomato scraps", "organic"), Item("used napkin", "sanitary", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C05_VerdeNegra, r.Combinacion);
        }

        // ─── T009–T011: C06 BLANCA + NEGRA ───────────────────────────────────────
        [TestMethod]
        public void T009_CartonLimpioYCartonConGrasa_BlancaYNegra()
        {
            var r = Motor.Evaluar(Obs(
                Item("clean cardboard", "cardboard", EstadoObservableObjeto.LimpioYVacio),
                Item("greasy cardboard", "cardboard", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C06_BlancaNegra, r.Combinacion);
            Assert.AreEqual("B09", r.Objetos[0].Material35!.Codigo);
            Assert.AreEqual("N03", r.Objetos[1].Material35!.Codigo);
        }

        [TestMethod]
        public void T010_BotellaPlasticaYPitilloUsado_BlancaYNegra()
        {
            var r = Motor.Evaluar(Obs(
                Item("plastic bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio),
                Item("used plastic straw", "plastic_rigid", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C06_BlancaNegra, r.Combinacion);
        }

        [TestMethod]
        public void T011_CajaJugoVaciaYBandejaIcoporSucia_BlancaYNegra()
        {
            var r = Motor.Evaluar(Obs(
                Item("juice box", "carton_multilayer", EstadoObservableObjeto.LimpioYVacio),
                Item("dirty styrofoam tray", "styrofoam", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C06_BlancaNegra, r.Combinacion);
        }

        // ─── T012–T016: C07 LAS TRES CANECAS ─────────────────────────────────────
        [TestMethod]
        public void T012_BananoBotellaYServilleta_LasTresCanecas()
        {
            var r = Motor.Evaluar(Obs(
                Item("banana peel", "organic"),
                Item("plastic water bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio),
                Item("used napkin", "sanitary", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C07_VerdeBlancaNegra, r.Combinacion);
            Assert.AreEqual(3, r.CanecasRequeridas.Count);
            Assert.IsTrue(r.PuedeAbrirCanecas);
        }

        [TestMethod]
        public void T013_MedioLimonFrascoVidrioYColilla_LasTresCanecas()
        {
            var r = Motor.Evaluar(Obs(
                Item("half lemon", "organic"),
                Item("clean glass jar", "glass", EstadoObservableObjeto.LimpioYVacio),
                Item("cigarette butt", "mixed", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C07_VerdeBlancaNegra, r.Combinacion);
        }

        [TestMethod]
        public void T014_CascaraPapaCajaJugoYCartonGrasa_LasTresCanecas()
        {
            var r = Motor.Evaluar(Obs(
                Item("potato peel", "organic"),
                Item("juice box", "carton_multilayer", EstadoObservableObjeto.LimpioYVacio),
                Item("greasy pizza box", "cardboard", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C07_VerdeBlancaNegra, r.Combinacion);
        }

        [TestMethod]
        public void T015_HojasSecasPapelLimpioYPitilloUsado_LasTresCanecas()
        {
            var r = Motor.Evaluar(Obs(
                Item("dry leaves", "organic"),
                Item("crumpled paper", "paper", EstadoObservableObjeto.LimpioYVacio),
                Item("plastic straw", "plastic_rigid", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C07_VerdeBlancaNegra, r.Combinacion);
        }

        [TestMethod]
        public void T016_KiwiBotellaYBandejaIcoporSucia_LasTresCanecas()
        {
            var r = Motor.Evaluar(Obs(
                Item("half kiwi", "organic"),
                Item("plastic soda bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio),
                Item("styrofoam tray", "styrofoam", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C07_VerdeBlancaNegra, r.Combinacion);
        }

        // ─── T017–T019 + Regla de 5 botellas = 1 caneca BLANCA ───────────────────
        [TestMethod]
        public void T017_TresCascarasOrganicas_SoloUnaCanecaVerde()
        {
            var r = Motor.Evaluar(Obs(Item("banana peel", "organic"), Item("orange peel", "organic"), Item("avocado peel", "organic")));
            Assert.AreEqual(CombinacionCanecas.C01_Verde, r.Combinacion);
            Assert.AreEqual(1, r.CanecasRequeridas.Count);
            Assert.AreEqual(3, r.TotalObjetosDetectados);
        }

        [TestMethod]
        public void T018_CincoBotellasPlasticasLimpias_SoloUnaCanecaBlanca()
        {
            var r = Motor.Evaluar(Obs(Item("plastic water bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio, cantidad: 5)));
            Assert.AreEqual(CombinacionCanecas.C02_Blanca, r.Combinacion);
            Assert.AreEqual(1, r.CanecasRequeridas.Count);
            Assert.AreEqual(5, r.TotalObjetosDetectados);
        }

        [TestMethod]
        public void T019_TresResiduosSanitariosSimulados_SoloUnaCanecaNegra()
        {
            var r = Motor.Evaluar(Obs(
                Item("used toilet paper", "sanitary", EstadoObservableObjeto.Contaminado),
                Item("used wet wipe", "sanitary", EstadoObservableObjeto.Contaminado),
                Item("used face mask", "sanitary", EstadoObservableObjeto.Contaminado)));
            Assert.AreEqual(CombinacionCanecas.C03_Negra, r.Combinacion);
            Assert.AreEqual(1, r.CanecasRequeridas.Count);
        }

        // ─── T020–T030: Seguridad, borde, fallo y anti-duplicación ───────────────
        [TestMethod]
        public void T020_FotografiaVacia_NingunaCaneca()
        {
            var r = Motor.Evaluar(new RespuestaVisionEstructurada(true, false, CalidadImagenVision.Vacia, Array.Empty<ObservacionObjetoVisual>(), "test", 50));
            Assert.AreEqual(EstadoGlobalAnalisis.SinResiduos, r.EstadoGlobal);
            Assert.AreEqual(CombinacionCanecas.Ninguna, r.Combinacion);
            Assert.IsFalse(r.PuedeAbrirCanecas);
        }

        [TestMethod]
        public void T021_FotografiaBorrosa_SolicitarRepeticion()
        {
            var r = Motor.Evaluar(new RespuestaVisionEstructurada(true, false, CalidadImagenVision.Borrosa, Array.Empty<ObservacionObjetoVisual>(), "test", 50));
            Assert.AreEqual(EstadoGlobalAnalisis.RequiereRepeticion, r.EstadoGlobal);
            Assert.IsFalse(r.PuedeAbrirCanecas);
        }

        [TestMethod]
        public void T022_BotellaConLiquidoDesconocido_SolicitarConfirmacionSinAbrir()
        {
            var r = Motor.Evaluar(Obs(Item("plastic bottle", "plastic_pet", EstadoObservableObjeto.ConLiquido)));
            Assert.AreEqual(EstadoGlobalAnalisis.RequiereConfirmacion, r.EstadoGlobal);
            Assert.IsTrue(r.RequiereConfirmacion);
            Assert.IsFalse(r.PuedeAbrirCanecas);
        }

        [TestMethod]
        public void T023_EtiquetaReciclajeImpresa_NoSeCuentaComoObjetoAdicional()
        {
            var r = Motor.Evaluar(Obs(
                Item("banana peel", "organic"),
                Item("printed recycling label", "paper", etiqueta: true)));
            Assert.AreEqual(1, r.TotalObjetosDetectados);
            Assert.AreEqual(CombinacionCanecas.C01_Verde, r.Combinacion);
        }

        [TestMethod]
        public void T024_CanecaEnPantalla_NoConfundirConResiduo()
        {
            var r = Motor.Evaluar(Obs(Item("recycling bin on phone screen", "unknown", pantalla: true)));
            Assert.AreEqual(EstadoGlobalAnalisis.SinResiduos, r.EstadoGlobal);
            Assert.IsFalse(r.PuedeAbrirCanecas);
        }

        [TestMethod]
        public void ReglaEspecial_CubetaDeHieloReutilizable_NoSeClasificaComoBasura()
        {
            var r = Motor.Evaluar(Obs(Item("plastic ice cube tray", "plastic_rigid", EstadoObservableObjeto.Reutilizable, reutilizable: true)));
            Assert.AreEqual(EstadoGlobalAnalisis.SinResiduos, r.EstadoGlobal);
            Assert.IsFalse(r.PuedeAbrirCanecas);
            Assert.AreEqual(EstadoClasificacionObjeto.NoEsResiduo, r.Objetos[0].Estado);
        }

        [TestMethod]
        public void Regresion_PeliculaPlasticaB13SiempreRequiereConfirmacion_EvitaFalsaAperturaBlanca()
        {
            var r = Motor.Evaluar(Obs(Item("clean plastic film", "plastic_film", EstadoObservableObjeto.LimpioYVacio)));
            Assert.AreEqual(EstadoGlobalAnalisis.RequiereConfirmacion, r.EstadoGlobal);
            Assert.IsFalse(r.PuedeAbrirCanecas);
            Assert.AreEqual("B13", r.Objetos[0].Material35!.Codigo);
        }

        [TestMethod]
        public void Regresion_BotellaConEtiquetaComercialNoSeDescartaComoSticker()
        {
            var obs = new RespuestaVisionEstructurada(
                true, HayResiduos: true, CalidadImagenVision.Aceptable,
                new[] { Item("plastic water bottle", "plastic_pet", EstadoObservableObjeto.LimpioYVacio, etiqueta: true) },
                "lfm3-vl:latest", 4000);
            var r = Motor.Evaluar(obs);
            Assert.AreEqual(CombinacionCanecas.C02_Blanca, r.Combinacion);
            Assert.AreEqual("B01", r.Objetos[0].Material35!.Codigo);
            Assert.IsTrue(r.PuedeAbrirCanecas);
        }

        [TestMethod]
        public void Regresion_JsonConObjetosYHayResiduosFalse_SeEvaluaPorMotorDeReglas()
        {
            const string json = "{\"hay_residuos\":false,\"calidad_imagen\":\"aceptable\",\"objetos\":[{\"nombre\":\"plastic water bottle\",\"material\":\"plastic_pet\",\"estado\":\"limpio_y_vacio\",\"cantidad\":1,\"confianza\":0.9}]}";
            var obs = ValidadorJsonVision.ValidarRespuestaJson(json, "lfm3-vl:latest", 100);
            Assert.IsTrue(obs.JsonValido);
            Assert.IsTrue(obs.HayResiduos);
            var r = Motor.Evaluar(obs);
            Assert.AreEqual(CombinacionCanecas.C02_Blanca, r.Combinacion);
        }

        [TestMethod]
        public void Regresion_ColillaDanadaVaANegra_MientrasVidrioRotoEsFueraDeCatalogo()
        {
            var colilla = Motor.Evaluar(Obs(Item("cigarette", "mixed", EstadoObservableObjeto.DanadoORoto)));
            Assert.AreEqual(CombinacionCanecas.C03_Negra, colilla.Combinacion);
            Assert.AreEqual("N04", colilla.Objetos[0].Material35!.Codigo);

            var vidrioRoto = Motor.Evaluar(Obs(Item("glass bottle", "glass", EstadoObservableObjeto.DanadoORoto)));
            Assert.IsFalse(vidrioRoto.PuedeAbrirCanecas);
            Assert.AreEqual(EstadoClasificacionObjeto.FueraDeCatalogo, vidrioRoto.Objetos[0].Estado);
        }

        [TestMethod]
        public async Task T025_DosFotografiasConsecutivasDelMismoDeposito_NoDuplicanAperturas()
        {
            var prov = new ProveedorFijo(Obs(Item("banana peel", "organic")));
            var gestor = new GestorSesionMultirresiduo(prov, Motor);

            var r1 = await gestor.AnalizarFotografiaAsync(JpegValido);
            Assert.AreEqual(EstadoGlobalAnalisis.Clasificado, r1.EstadoGlobal);
            gestor.CancelarDeposito("fin prueba 1");

            var r2 = await gestor.AnalizarFotografiaAsync(JpegValido);
            Assert.AreEqual("DUPLICADO_CONSECUTIVO", r2.ErrorTecnico);
            Assert.IsFalse(r2.PuedeAbrirCanecas);
        }

        [TestMethod]
        public async Task T026_OllamaDesconectado_ModoSeguroSinAbrir()
        {
            using var http = new HttpClient(new HandlerFalloRed());
            using var cliente = new ClienteVisionOllamaMultirresiduo("http://127.0.0.1:19999", "lfm3-vl:latest", TimeSpan.FromSeconds(2), httpClient: http);
            var gestor = new GestorSesionMultirresiduo(cliente, Motor);

            var r = await gestor.AnalizarFotografiaAsync(JpegValido);
            Assert.AreEqual(EstadoGlobalAnalisis.ModoSeguroError, r.EstadoGlobal);
            Assert.IsFalse(r.PuedeAbrirCanecas);
            Assert.AreEqual(EstadoSesionDeposito.ERROR_SEGURO, gestor.Maquina.EstadoActual);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("{json roto")]
        [DataRow("{\"hay_residuos\":true}")]
        [DataRow("{\"hay_residuos\":true,\"calidad_imagen\":\"invalida\",\"objetos\":[]}")]
        [DataRow("{\"hay_residuos\":true,\"calidad_imagen\":\"aceptable\",\"objetos\":[{\"nombre\":\"botella\",\"estado\":\"estado_inventado\",\"cantidad\":1,\"confianza\":0.9}]}")]
        public void T027_JsonInvalido_NoAbreCanecas(string jsonMalo)
        {
            var obs = ValidadorJsonVision.ValidarRespuestaJson(jsonMalo, "test", 10);
            Assert.IsFalse(obs.JsonValido);
            var res = Motor.Evaluar(obs);
            Assert.AreEqual(EstadoGlobalAnalisis.ModoSeguroError, res.EstadoGlobal);
            Assert.IsFalse(res.PuedeAbrirCanecas);
            Assert.AreEqual(0, res.CanecasRequeridas.Count);
        }

        [TestMethod]
        public async Task T028_TiempoDeRespuestaExcedido_CancelaYOfreceReintento()
        {
            using var http = new HttpClient(new HandlerLento());
            using var cliente = new ClienteVisionOllamaMultirresiduo("http://127.0.0.1:11434", "lfm3-vl:latest", TimeSpan.FromMilliseconds(80), httpClient: http);
            var resp = await cliente.ObservarAsync(JpegValido);
            Assert.IsFalse(resp.JsonValido);
            StringAssert.Contains(resp.ErrorValidacion, "Tiempo máximo");
            var dictamen = Motor.Evaluar(resp);
            Assert.IsFalse(dictamen.PuedeAbrirCanecas);
        }

        [TestMethod]
        public async Task T029_HardwareDesconectado_NoSimulaAperturaRealExitosa()
        {
            var prov = new ProveedorFijo(Obs(Item("banana peel", "organic")));
            var ejecutorDesconectado = new EjecutorDesconectadoSimulado();
            var gestor = new GestorSesionMultirresiduo(prov, Motor, ejecutorDesconectado);

            await gestor.AnalizarFotografiaAsync(JpegValido);
            var estacion = new[] { new CanecaEstacion(1400, "Verde", CategoriaMaterial.OrganicoAprovechable, true, null, null) };
            var aperturas = await gestor.EjecutarAperturaSecuencialAsync("op-t029", estacion, permitirSoloSimulador: true);

            Assert.AreEqual(1, aperturas.Count);
            Assert.IsTrue(aperturas[0].ComandoEnviado);
            Assert.IsFalse(aperturas[0].AperturaConfirmada, "Nunca simular apertura confirmada cuando hardware está desconectado.");
            Assert.AreEqual(EstadoEjecucionApertura.Desconectada, aperturas[0].EstadoControlador);
            Assert.AreEqual(EstadoSesionDeposito.ERROR_SEGURO, gestor.Maquina.EstadoActual);
        }

        [TestMethod]
        public async Task T030_UsuarioCancelaDeposito_NingunaAperturaNueva()
        {
            var prov = new ProveedorFijo(Obs(Item("banana peel", "organic")));
            var sim = new SimuladorEstacionTresCanecas(
                new Dictionary<int, DestinoResiduo>
                {
                    [1093] = DestinoResiduo.Blanca,
                    [1400] = DestinoResiduo.Verde,
                    [1401] = DestinoResiduo.Negra,
                },
                TimeProvider.System, TimeSpan.Zero);
            var gestor = new GestorSesionMultirresiduo(prov, Motor, sim);

            await gestor.AnalizarFotografiaAsync(JpegValido);
            gestor.CancelarDeposito("Usuario canceló");

            Assert.AreEqual(EstadoSesionDeposito.INACTIVO, gestor.Maquina.EstadoActual);
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                gestor.EjecutarAperturaSecuencialAsync("op-t030", Array.Empty<CanecaEstacion>()));
        }

        private sealed class ProveedorFijo : IProveedorVisionMultirresiduo
        {
            private readonly RespuestaVisionEstructurada _resp;
            public ProveedorFijo(RespuestaVisionEstructurada resp) => _resp = resp;
            public string NombreProveedor => "simulado-fijo";
            public Task<RespuestaVisionEstructurada> ObservarAsync(byte[] bytesImagen, CancellationToken ct = default) => Task.FromResult(_resp);
        }

        private sealed class HandlerFalloRed : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                throw new HttpRequestException("Conexión rechazada (Ollama desconectado).");
        }

        private sealed class HandlerLento : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(5000, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
            }
        }

        private sealed class EjecutorDesconectadoSimulado : IEjecutorAperturas
        {
            public bool EsSimulado => true;
            public Task<ResultadoEjecucionApertura> AbrirAsync(ComandoApertura comando, CancellationToken cancellationToken = default) =>
                Task.FromResult(new ResultadoEjecucionApertura(EstadoEjecucionApertura.Desconectada, Array.Empty<TipoEventoDispositivo>()));
        }
    }
}
