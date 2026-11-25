using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Vision35;
using EcoAccess.P3.Logica.Vision35.Expo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica.Vision35
{
    /// <summary>Modo ExpoSoftware: catálogo de 9, validación determinista, política 5/7/10 s, idempotencia y cola de Oracle.</summary>
    [TestClass]
    public sealed class PruebasModoExpo9Tests
    {
        private static string Json(params (string Nombre, int Cantidad)[] items) =>
            "{\"items\":[" + string.Join(",", items.Select(i => $"{{\"label\":\"{i.Nombre}\",\"count\":{i.Cantidad}}}")) + "]}";

        [TestMethod]
        public void Catalogo_TieneOnceResiduos_5Verdes_4Blancos_2Negros_YCodigosValidos()
        {
            Assert.AreEqual(11, CatalogoExpo9.Todos.Count);
            Assert.AreEqual(5, CatalogoExpo9.Todos.Count(m => m.Caneca == CategoriaMaterial.OrganicoAprovechable));
            Assert.AreEqual(4, CatalogoExpo9.Todos.Count(m => m.Caneca == CategoriaMaterial.Aprovechable));
            Assert.AreEqual(2, CatalogoExpo9.Todos.Count(m => m.Caneca == CategoriaMaterial.NoAprovechable));
            Assert.AreEqual(11, CatalogoExpo9.Todos.Select(m => m.Etiqueta).Distinct().Count(), "etiquetas únicas");
            foreach (var m in CatalogoExpo9.Todos.Where(m => m.Codigo35 is not null))
                Assert.IsNotNull(CatalogoPrioritario35.BuscarPorCodigo(m.Codigo35), m.Codigo35);
            // Reglas del enunciado: sérum de vidrio y aluminio limpio a la BLANCA; pegamento y servilleta a la NEGRA.
            Assert.AreEqual(CategoriaMaterial.Aprovechable, CatalogoExpo9.PorEtiqueta("serum_bottle")!.Caneca);
            Assert.AreEqual(CategoriaMaterial.Aprovechable, CatalogoExpo9.PorEtiqueta("aluminum_foil")!.Caneca);
            Assert.AreEqual(CategoriaMaterial.NoAprovechable, CatalogoExpo9.PorEtiqueta("glue_bottle")!.Caneca);
            Assert.AreEqual(CategoriaMaterial.NoAprovechable, CatalogoExpo9.PorEtiqueta("used_napkin")!.Caneca);
            // El historial antiguo (E01–E09) se sigue leyendo con sus nombres.
            Assert.AreEqual("botella plástica", CatalogoExpo9.PorCodigo("E05")!.NombreCorto);
        }

        [DataTestMethod]
        [DataRow("potato", "M01")]
        [DataRow("half potato", "M01")]
        [DataRow("lime", "M02")]
        [DataRow("lemon", "M02")]
        [DataRow("green_apple", "M03")]
        [DataRow("green apple", "M03")]
        [DataRow("lulo", "M04")]
        [DataRow("orange", "M04")]
        [DataRow("banana peel", "M05")]
        [DataRow("aluminum_foil", "M06")]
        [DataRow("foil-wrapped ball", "M06")]
        [DataRow("serum_bottle", "M07")]
        [DataRow("dropper bottle", "M07")]
        [DataRow("notebook_paper", "M08")]
        [DataRow("graph paper", "M08")]
        [DataRow("kraft_paper", "M09")]
        [DataRow("crumpled cardboard", "M09")]
        [DataRow("glue_bottle", "M10")]
        [DataRow("glue bottle", "M10")]
        [DataRow("used_napkin", "M11")]
        [DataRow("used napkin", "M11")]
        [DataRow("crumpled paper", null)]
        [DataRow("crumpled paper ball", null)]
        [DataRow("chips bag", null)]
        [DataRow("snack wrapper", null)]
        [DataRow("plastic bottle", null)]
        [DataRow("other", null)]
        public void Interpretar_EtiquetaONombreDelModelo(string nombre, string? codigo)
        {
            var (tipo, material) = CatalogoExpo9.Interpretar(nombre);
            if (codigo is null) Assert.AreNotEqual(TipoNombreExpo.Material, tipo, nombre);
            else Assert.AreEqual(codigo, material?.Codigo, nombre);
        }

        [DataTestMethod]
        [DataRow("USB cable")]
        [DataRow("pill blister")]
        [DataRow("razor")]
        [DataRow("broken glass")]
        [DataRow("battery")]
        [DataRow("medicine")]
        [DataRow("chemical bottle")]
        public void Interpretar_GestionEspecial_NuncaEsMaterial(string nombre) =>
            Assert.AreEqual(TipoNombreExpo.GestionEspecial, CatalogoExpo9.Interpretar(nombre).Tipo);

        [DataTestMethod]
        [DataRow("hand")]
        [DataRow("robotic hand")]
        [DataRow("table")]
        [DataRow("laptop")]
        public void Interpretar_ManoMesaYPantalla_SeIgnoran(string nombre) =>
            Assert.AreEqual(TipoNombreExpo.Ignorado, CatalogoExpo9.Interpretar(nombre).Tipo);

        [TestMethod]
        public void Politica_7_14_21_SegunCanecasDistintas()
        {
            Assert.AreEqual(7000, PoliticaTiemposApertura.DuracionMs(1));
            Assert.AreEqual(14000, PoliticaTiemposApertura.DuracionMs(2));
            Assert.AreEqual(21000, PoliticaTiemposApertura.DuracionMs(3));
            Assert.AreEqual(0, PoliticaTiemposApertura.DuracionMs(0));
        }

        [TestMethod]
        public void PapaLimonManzana_AbrenSoloLaVerdeUnaVez_7s()
        {
            var r = ReconocedorExpo9.Interpretar(Json(("potato", 1), ("lime", 1), ("green_apple", 1), ("hand", 1)), "m");
            Assert.AreEqual(EstadoReconocimientoExpo.Reconocido, r.Estado);
            CollectionAssert.AreEqual(new[] { CategoriaMaterial.OrganicoAprovechable }, r.Canecas.ToArray());
            Assert.AreEqual(7000, r.DuracionAperturaMs);
            Assert.AreEqual(3, r.Materiales.Count);
            Assert.IsTrue(r.PuedeAbrirAutomaticamente);
        }

        [TestMethod]
        public void PapaAluminioServilleta_AbrenLasTres_21s_SinImportarElOrden()
        {
            var a = ReconocedorExpo9.Interpretar(Json(("used_napkin", 1), ("aluminum_foil", 1), ("potato", 1)), "m");
            var b = ReconocedorExpo9.Interpretar(Json(("potato", 1), ("used_napkin", 1), ("aluminum_foil", 1)), "m");
            CollectionAssert.AreEqual(a.Canecas.ToArray(), b.Canecas.ToArray());
            Assert.AreEqual(3, a.Canecas.Count);
            Assert.AreEqual(21000, a.DuracionAperturaMs);
            Assert.AreEqual(CombinacionCanecas.C07_VerdeBlancaNegra, a.Combinacion);
        }

        [DataTestMethod]
        [DataRow("lime|aluminum_foil", CombinacionCanecas.C04_VerdeBlanca, 14000)]
        [DataRow("aluminum_foil|glue_bottle", CombinacionCanecas.C06_BlancaNegra, 14000)]
        [DataRow("lulo|used_napkin", CombinacionCanecas.C05_VerdeNegra, 14000)]
        [DataRow("serum_bottle|kraft_paper|notebook_paper", CombinacionCanecas.C02_Blanca, 7000)]
        [DataRow("glue_bottle|used_napkin", CombinacionCanecas.C03_Negra, 7000)]
        [DataRow("banana_peel", CombinacionCanecas.C01_Verde, 7000)]
        public void Combinaciones_DelEnunciado(string nombres, CombinacionCanecas esperada, int ms)
        {
            var r = ReconocedorExpo9.Interpretar(Json(nombres.Split('|').Select(n => (n, 1)).ToArray()), "m");
            Assert.AreEqual(esperada, r.Combinacion);
            Assert.AreEqual(ms, r.DuracionAperturaMs);
        }

        [TestMethod]
        public void PapelBlancoSinDetalle_NoAbreSolo_PeroConservaLoReconocido()
        {
            var r = ReconocedorExpo9.Interpretar(Json(("lime", 1), ("crumpled paper", 1)), "m");
            Assert.AreEqual(EstadoReconocimientoExpo.ParcialConDesconocidos, r.Estado);
            Assert.IsFalse(r.PuedeAbrirAutomaticamente);
            CollectionAssert.AreEqual(new[] { CategoriaMaterial.OrganicoAprovechable }, r.Canecas.ToArray());
        }

        [TestMethod]
        public void Verificaciones_CubrenLosParesQueCambianDeCaneca()
        {
            foreach (var (si, no, pregunta, lado) in ReconocedorExpo9.Verificaciones)
            {
                Assert.IsTrue(no.Split('|').Any(otra => CatalogoExpo9.PorEtiqueta(otra)!.Caneca != CatalogoExpo9.PorEtiqueta(si)!.Caneca),
                    "cada verificación evita al menos un cambio de caneca: " + si + "/" + no);
                Assert.IsTrue(lado is >= 512 and <= 1024);
                StringAssert.EndsWith(pregunta, "?");
            }
            var pares = ReconocedorExpo9.Verificaciones.Select(v => v.Si + "|" + v.No).ToList();
            CollectionAssert.Contains(pares, "serum_bottle|glue_bottle");
            CollectionAssert.Contains(pares, "kraft_paper|notebook_paper|used_napkin");
            CollectionAssert.Contains(pares, "notebook_paper|used_napkin");
        }

        [DataTestMethod]
        [DataRow("tomato", "potato", false)]
        [DataRow("onion", "potato", false)]
        [DataRow("sponge", "aluminum_foil", false)]
        [DataRow("playing card", "card", false)]
        [DataRow("crumpled paper", "other", false)]
        [DataRow("plastic bag", "kraft_paper", false)]
        [DataRow("white plastic bag", "used_napkin", false)]
        [DataRow("plastic bottle", "glue_bottle", false)]
        [DataRow("shampoo bottle", "glue_bottle", false)]
        [DataRow("shampoo bottle", "serum_bottle", false)]
        [DataRow("potato", "potato", true)]
        [DataRow("orange", "lulo", true)]
        [DataRow("dropper bottle", "serum_bottle", true)]
        [DataRow("crumpled paper", "notebook_paper", true)]
        [DataRow("glue bottle", "ASTRO GLUE", true)]
        public void NombreLibreYEtiqueta_SoloSeAceptanSiSonCompatibles(string nombre, string etiqueta, bool acepta)
        {
            var (tipo, _) = CatalogoExpo9.Interpretar(nombre, etiqueta);
            Assert.AreEqual(acepta, tipo == TipoNombreExpo.Material, nombre + " / " + etiqueta);
        }

        [TestMethod]
        public void NombreLibreDeGestionEspecial_MandaSobreLaEtiqueta()
        {
            Assert.AreEqual(TipoNombreExpo.GestionEspecial, CatalogoExpo9.Interpretar("blister pack", "serum_bottle").Tipo);
            Assert.AreEqual(TipoNombreExpo.GestionEspecial, CatalogoExpo9.Interpretar("razor", "aluminum_foil").Tipo);
        }

        [TestMethod]
        public void GestionEspecial_NoAbreNingunaCaneca()
        {
            var r = ReconocedorExpo9.Interpretar(Json(("apple", 1), ("pill blister", 1)), "m");
            Assert.AreEqual(EstadoReconocimientoExpo.GestionEspecial, r.Estado);
            Assert.AreEqual(0, r.Canecas.Count);
            Assert.IsFalse(r.PuedeAbrirAutomaticamente);
        }

        [TestMethod]
        public void JsonCortado_RescataLosObjetosCompletos_YVacioEsError()
        {
            var r = ReconocedorExpo9.Interpretar("{\"items\": [{\"name\": \"banana\", \"count\": 1}, {\"name\": \"bott", "m");
            Assert.AreEqual(1, r.Materiales.Count);
            Assert.AreEqual(EstadoReconocimientoExpo.Error, ReconocedorExpo9.Interpretar("", "m").Estado);
            Assert.AreEqual(EstadoReconocimientoExpo.SinMaterialesExpo, ReconocedorExpo9.Interpretar("{\"items\":[]}", "m").Estado);
        }

        [TestMethod]
        public async Task Servicio_Idempotente_YRegistraConCola()
        {
            string carpeta = Path.Combine(Path.GetTempPath(), "expo-test-" + Guid.NewGuid().ToString("N"));
            var oracle = new RegistroFalso();
            var servicio = new ServicioEstacionExpo(new EstacionExpoSimulada(), new RegistroOperacionesConCola(oracle, carpeta));
            var canecas = new[] { CategoriaMaterial.Aprovechable, CategoriaMaterial.OrganicoAprovechable, CategoriaMaterial.Aprovechable };
            var r1 = await servicio.EjecutarAsync("tg:1", CanalEstacion.TELEGRAM, TipoOperacionEstacion.APERTURA_MANUAL, canecas, 7, "usuario:7");
            var r2 = await servicio.EjecutarAsync("tg:1", CanalEstacion.TELEGRAM, TipoOperacionEstacion.APERTURA_MANUAL, canecas, 7, "usuario:7");
            Assert.IsTrue(r1.AperturaEnviada && r1.Simulada && r1.RegistradaEnOracle);
            Assert.AreEqual(14000, r1.DuracionMs);
            Assert.AreEqual(2, r1.Canecas.Count);
            Assert.IsTrue(r2.Duplicada && !r2.AperturaEnviada);
            Assert.AreEqual(1, oracle.Filas.Count);
            Assert.AreEqual("VERDE,BLANCA", oracle.Filas[0].Canecas);

            oracle.Caido = true;
            var r3 = await servicio.EjecutarAsync("tg:2", CanalEstacion.PC, TipoOperacionEstacion.APERTURA_MANUAL, new[] { CategoriaMaterial.NoAprovechable }, null, "pc");
            Assert.IsTrue(r3.AperturaEnviada && !r3.RegistradaEnOracle, "sin Oracle no se informa un guardado inexistente");
            Assert.AreEqual(1, servicio.Registro.Pendientes);
            oracle.Caido = false;
            Assert.AreEqual(1, await servicio.Registro.SincronizarAsync());
            Assert.AreEqual(0, servicio.Registro.Pendientes);
            Assert.IsTrue(oracle.Filas.Single(f => f.Clave == "tg:2").SincronizadaOffline);
            try { Directory.Delete(carpeta, true); } catch (IOException) { }
        }

        [TestMethod]
        public async Task Servicio_SinEsperarRegistro_RespondeYRegistraEnParalelo()
        {
            var oracle = new RegistroFalso();
            var servicio = new ServicioEstacionExpo(new EstacionExpoSimulada(), new RegistroOperacionesConCola(oracle, Path.Combine(Path.GetTempPath(), "expo-p-" + Guid.NewGuid().ToString("N"))));
            var r = await servicio.EjecutarAsync("tg:9", CanalEstacion.TELEGRAM, TipoOperacionEstacion.APERTURA_MANUAL, new[] { CategoriaMaterial.Aprovechable }, 3, "usuario:3", esperarRegistro: false);
            Assert.IsTrue(r.AperturaEnviada);
            Assert.IsFalse(r.RegistradaEnOracle, "la respuesta inmediata no afirma un registro aún no confirmado");
            Assert.IsNotNull(r.RegistroEnCurso);
            Assert.IsTrue(await r.RegistroEnCurso!);
            Assert.AreEqual("BLANCA", oracle.Filas.Single().Canecas);
        }

        [TestMethod]
        public void Telegram_TextoClasificacion_FormatoBreve_EImagenPorCaneca()
        {
            var una = ReconocedorExpo9.Interpretar(Json(("potato", 1)), "m");
            Assert.AreEqual("🟢 Papa detectada.", EcoAccess.P3.Logica.Servicios.ServicioConversacionTelegram.TextoClasificacion(una).Replace("\r", ""));
            Assert.AreEqual("eco_verde", EcoAccess.P3.Logica.Servicios.ServicioConversacionTelegram.ImagenCanecas(una.Canecas));
            var dos = ReconocedorExpo9.Interpretar(Json(("lime", 1), ("aluminum_foil", 1)), "m");
            Assert.AreEqual("🟢 Limón detectado.\n⚪ Papel aluminio detectado.", EcoAccess.P3.Logica.Servicios.ServicioConversacionTelegram.TextoClasificacion(dos).Replace("\r", ""));
            var tres = ReconocedorExpo9.Interpretar(Json(("potato", 1), ("aluminum_foil", 1), ("used_napkin", 1)), "m");
            string t = EcoAccess.P3.Logica.Servicios.ServicioConversacionTelegram.TextoClasificacion(tres);
            StringAssert.Contains(t, "⚫ Servilleta usada detectada.");
            Assert.AreEqual("eco_canecas", EcoAccess.P3.Logica.Servicios.ServicioConversacionTelegram.ImagenCanecas(tres.Canecas));
            Assert.IsNull(EcoAccess.P3.Logica.Servicios.ServicioConversacionTelegram.ImagenCanecas(Array.Empty<CategoriaMaterial>()));
        }

        [TestMethod]
        public void Telegram_Historial_UnaLineaPorResiduoConSuCaneca()
        {
            var op = new OperacionEstacion("tg:1", DateTime.UtcNow, CanalEstacion.TELEGRAM, TipoOperacionEstacion.RECONOCIMIENTO_AUTOMATICO, 3, "usuario:3",
                "estacion_tres_canecas", "E05 botella plástica, E02 cáscara de banano x2 | no reconocido: cup", "APROVECHABLE,ORGANICO_APROVECHABLE", "VERDE,BLANCA",
                3, 7000, "SIMULTANEA", "Reconocido", EstadoHardwareEstacion.PWM_CONFIRMADO, EstadoOperacionEstacion.CICLO_COMPLETADO);
            var l = EcoAccess.P3.Logica.Servicios.ServicioConversacionTelegram.LineasHistorial(op).ToList();
            Assert.AreEqual(2, l.Count);
            StringAssert.EndsWith(l[0], " — blanca");
            StringAssert.Contains(l[1], "×2");
            StringAssert.EndsWith(l[1], " — verde");
            Assert.AreEqual(0, EcoAccess.P3.Logica.Servicios.ServicioConversacionTelegram.LineasHistorial(op with { Materiales = null }).Count());
        }

        [TestMethod]
        public async Task Servicio_SinCanecas_NoAbreNada()
        {
            var servicio = new ServicioEstacionExpo(new EstacionExpoSimulada(), new RegistroOperacionesConCola(new RegistroFalso(), Path.GetTempPath()));
            var r = await servicio.EjecutarAsync("x", CanalEstacion.PC, TipoOperacionEstacion.APERTURA_MANUAL, Array.Empty<CategoriaMaterial>(), null, "pc");
            Assert.IsFalse(r.AperturaEnviada);
        }

        // ---- Reglas de cero aperturas incorrectas (evaluación consolidada 2026-11-25) ----

        private static string JsonNombre(params (string Nombre, string Etiqueta, int Cantidad)[] items) =>
            "{\"items\":[" + string.Join(",", items.Select(i => $"{{\"name\":\"{i.Nombre}\",\"label\":\"{i.Etiqueta}\",\"count\":{i.Cantidad}}}")) + "]}";

        private static MaterialDetectadoExpo Det(string etiqueta, string nombre = "x", string? duda = null) =>
            new(CatalogoExpo9.PorEtiqueta(etiqueta)!, 1, nombre, duda);

        [DataTestMethod]
        [DataRow("tissue", "used_napkin", true)]
        [DataRow("crumpled_tissue", "used_napkin", true)]
        [DataRow("used napkin", "used_napkin", true)]
        [DataRow("paper towel", "used_napkin", true)]
        [DataRow("crumpled paper", "used_napkin", false)]
        [DataRow("paper", "used_napkin", false)]
        [DataRow("plastic_bag", "used_napkin", false)]
        [DataRow("napkin in plastic wrapper", "used_napkin", false)]
        [DataRow("plastic bag", "notebook_paper", false)]
        [DataRow("plastic_bag", "kraft_paper", false)]
        [DataRow("crumpled paper", "notebook_paper", true)]
        [DataRow("paper bag", "kraft_paper", true)]
        public void Servilleta_ExigePapelSuaveEnElNombre_YRechazaPlastico(string nombre, string etiqueta, bool acepta)
        {
            var (tipo, _) = CatalogoExpo9.Interpretar(nombre, etiqueta);
            Assert.AreEqual(acepta, tipo == TipoNombreExpo.Material, nombre + " / " + etiqueta);
        }

        [DataTestMethod]
        [DataRow("orange", "lulo")]
        [DataRow("orange peel", "lulo")]
        [DataRow("orange", "potato")]
        public void Citricos_NuncaAbrenSolos_QuedanPorConfirmarEnLaVerde(string nombre, string etiqueta)
        {
            var r = ReconocedorExpo9.Interpretar(JsonNombre((nombre, etiqueta, 1)), "m");
            Assert.AreEqual(EstadoReconocimientoExpo.ParcialConDesconocidos, r.Estado);
            Assert.IsFalse(r.PuedeAbrirAutomaticamente);
            Assert.IsTrue(r.Materiales.Single().PorConfirmar);
            CollectionAssert.AreEqual(new[] { CategoriaMaterial.OrganicoAprovechable }, r.Canecas.ToArray(), "la caneca sugerida sigue siendo la verde");
            StringAssert.Contains(r.Resumen(), "por confirmar");
        }

        [DataTestMethod]
        [DataRow("mandarin", "lulo")]
        [DataRow("tangerine", "lulo")]
        [DataRow("mandarin", "lime")]
        public void Mandarinas_NoSonCompatiblesConLulo_YNoAbren(string nombre, string etiqueta)
        {
            var r = ReconocedorExpo9.Interpretar(JsonNombre((nombre, etiqueta, 1)), "m");
            Assert.IsFalse(r.PuedeAbrirAutomaticamente);
            Assert.IsTrue(r.Materiales.All(m => m.PorConfirmar));
        }

        [TestMethod]
        public void Lulo_ConNombreLuloONaranjilla_AbreSolo()
        {
            Assert.IsTrue(ReconocedorExpo9.Interpretar(JsonNombre(("lulo", "lulo", 1)), "m").PuedeAbrirAutomaticamente);
            Assert.IsTrue(ReconocedorExpo9.Interpretar(JsonNombre(("naranjilla (orange fruit)", "lulo", 1)), "m").PuedeAbrirAutomaticamente);
            Assert.IsNull(CatalogoExpo9.MotivoDuda("orange", CatalogoExpo9.PorEtiqueta("aluminum_foil")!), "la regla de cítricos solo aplica a la verde");
        }

        [TestMethod]
        public void Verificacion_NoQueSoloDescarta_DejaElNuevoMaterialPorConfirmar()
        {
            // Papel con etiqueta de cuaderno y «no» a la cuadrícula → servilleta solo por descarte (bolsa plástica, papel liso).
            var servilleta = ReconocedorExpo9.AplicarVerificacion(Det("notebook_paper", "crumpled paper"), "notebook_paper", new[] { "used_napkin" }, false);
            Assert.AreEqual("M11", servilleta.Material.Codigo);
            Assert.IsTrue(servilleta.PorConfirmar);
            // Frasco con «no» al vidrio rosado → pegamento solo por descarte (botella plástica).
            var pegamento = ReconocedorExpo9.AplicarVerificacion(Det("serum_bottle", "dropper bottle"), "serum_bottle", new[] { "glue_bottle" }, false);
            Assert.AreEqual("M10", pegamento.Material.Codigo);
            Assert.IsTrue(pegamento.PorConfirmar);
            // «No» que conserva la etiqueta del modelo (cuaderno: «no es kraft») no añade duda.
            Assert.IsFalse(ReconocedorExpo9.AplicarVerificacion(Det("notebook_paper", "crumpled paper"), "kraft_paper", new[] { "notebook_paper", "used_napkin" }, false).PorConfirmar);
            // «Sí» al kraft sobre algo etiquetado como servilleta: la duda de la servilleta no pasa al kraft.
            var aKraft = ReconocedorExpo9.AplicarVerificacion(Det("used_napkin", "tissue", "¿servilleta?"), "kraft_paper", new[] { "notebook_paper", "used_napkin" }, true);
            Assert.AreEqual("M09", aKraft.Material.Codigo);
            Assert.IsFalse(aKraft.PorConfirmar);
        }

        [DataTestMethod]
        [DataRow("tissue", "used_napkin")]
        [DataRow("used napkin", "used_napkin")]
        [DataRow("napkin", "napkin")]
        public void Servilleta_NuncaAbreSola_SeSugiereLaNegraConConfirmacion(string nombre, string etiqueta)
        {
            // Medido: con un prompt que pide «napkin/tissue», el modelo etiquetó «tissue / used_napkin» una bolsa plástica,
            // papel higiénico, una toallita húmeda y papel liso. Ningún nombre los separa: la servilleta siempre se confirma.
            var r = ReconocedorExpo9.Interpretar(JsonNombre((nombre, etiqueta, 1)), "m");
            Assert.AreEqual("M11", r.Materiales.Single().Material.Codigo);
            Assert.IsTrue(r.Materiales.Single().PorConfirmar);
            Assert.IsFalse(r.PuedeAbrirAutomaticamente);
            CollectionAssert.AreEqual(new[] { CategoriaMaterial.NoAprovechable }, r.Canecas.ToArray());
            // El resto de materiales del catálogo sigue abriendo solo con su etiqueta.
            Assert.IsTrue(ReconocedorExpo9.Interpretar(JsonNombre(("glue bottle", "glue_bottle", 1)), "m").PuedeAbrirAutomaticamente);
        }

        [TestMethod]
        public void Verificacion_SiEsEvidenciaDirecta_YSinRespuestaQuedaPorConfirmar()
        {
            var kraft = ReconocedorExpo9.AplicarVerificacion(Det("notebook_paper", "crumpled paper"), "kraft_paper", new[] { "notebook_paper", "used_napkin" }, true);
            Assert.AreEqual("M09", kraft.Material.Codigo);
            Assert.IsFalse(kraft.PorConfirmar);
            // Kraft descartado («no» → cuaderno por descarte) y luego «sí» a la cuadrícula: el cuaderno queda confirmado.
            var cuaderno = ReconocedorExpo9.AplicarVerificacion(Det("kraft_paper"), "kraft_paper", new[] { "notebook_paper", "used_napkin" }, false);
            Assert.IsTrue(cuaderno.PorConfirmar);
            Assert.IsFalse(ReconocedorExpo9.AplicarVerificacion(cuaderno, "notebook_paper", new[] { "used_napkin" }, true).PorConfirmar);
            Assert.IsTrue(ReconocedorExpo9.AplicarVerificacion(Det("serum_bottle"), "serum_bottle", new[] { "glue_bottle" }, null).PorConfirmar);
        }

        [TestMethod]
        public void Multirresiduo_ParecidosDeCanecasDistintasSinVerificar_NoAbrenSolos()
        {
            var kraftYServilleta = new List<MaterialDetectadoExpo> { Det("kraft_paper"), Det("used_napkin", "tissue"), Det("potato") };
            Assert.IsTrue(ReconocedorExpo9.MarcarParecidosSinVerificar(kraftYServilleta, "kraft_paper", new[] { "notebook_paper", "used_napkin" }));
            Assert.IsTrue(kraftYServilleta[0].PorConfirmar && kraftYServilleta[1].PorConfirmar);
            Assert.IsFalse(kraftYServilleta[2].PorConfirmar, "la papa no es del grupo");
            // Kraft + cuaderno van a la misma caneca (blanca): no hace falta confirmar.
            var mismaCaneca = new List<MaterialDetectadoExpo> { Det("kraft_paper"), Det("notebook_paper") };
            Assert.IsFalse(ReconocedorExpo9.MarcarParecidosSinVerificar(mismaCaneca, "kraft_paper", new[] { "notebook_paper", "used_napkin" }));
        }

        [TestMethod]
        public void UnMaterialPorConfirmar_BloqueaLaAperturaAutomaticaDeTodo()
        {
            var r = ReconocedorExpo9.Interpretar(JsonNombre(("potato", "potato", 1), ("aluminum_foil", "aluminum_foil", 1)), "m");
            Assert.IsTrue(r.PuedeAbrirAutomaticamente);
            var conDuda = r with { Materiales = r.Materiales.Select((m, i) => i == 0 ? m with { Duda = ReconocedorExpo9.DudaNoConfirmado } : m).ToList() };
            Assert.IsFalse(conDuda.PuedeAbrirAutomaticamente);
            var p = ReconocedorExpo9.Confirmaciones.Single(c => c.Etiqueta == "potato");
            StringAssert.EndsWith(p.Pregunta, "?");
        }

        private sealed class RegistroFalso : IRegistroOperacionesEstacion
        {
            public bool Caido;
            public readonly List<OperacionEstacion> Filas = new();
            public Task GuardarAsync(OperacionEstacion op, CancellationToken ct = default)
            {
                if (Caido) throw new InvalidOperationException("Oracle caído");
                Filas.RemoveAll(f => f.Clave == op.Clave); Filas.Add(op); return Task.CompletedTask;
            }
            public Task<IReadOnlyList<OperacionEstacion>> RecientesAsync(int? idUsuario, int maximo, CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<OperacionEstacion>>(Filas.ToList());
        }
    }
}
