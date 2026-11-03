using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{

    /// <summary>
    /// Recorrido definitivo: saludo, foto, apertura y despedida. Con motores falsos y una caneca en memoria: no prueba
    /// Telegram, el modelo ni el ESP32 reales. La primera prueba reproduce la foto real rechazada del 3 de noviembre.
    /// </summary>
    public sealed partial class ServicioConversacionTelegramTests
    {
        private sealed class EstadoCanecaFalso : IEstadoCaneca
        {
            public bool Disponible { get; set; } = true;
            public bool Falla { get; set; }
            public Task<bool> DisponibleAsync(int idContenedor, CancellationToken ct = default) =>
                Falla ? throw new InvalidOperationException("sin enlace") : Task.FromResult(Disponible);
        }

        private PropuestaVision BotellaEnPet() => new(TipoPropuestaVision.MaterialIdentificado, _pet.Codigo, 0.95m, Objeto: ObjetoVisible.BotellaPlastica);

        /// <param name="simulada">Hace de la caneca de la prueba la simulada; si no, se comporta como caneca física.</param>
        private ServicioConversacionTelegram Eco(Func<PropuestaVision> propuesta, bool simulada = true, IEstadoCaneca? estado = null)
        {
            var motor = new MotorFalso("ollama-local:modelo", propuesta);
            var selector = new SelectorVision(ModoVision.Local, motor, null, TimeSpan.FromSeconds(30), _ => { });
            return new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj,
                vision: new ServicioClasificacionImagen(selector, _materiales, new ServicioCatalogoAmbiental(_materiales, _contenedores)),
                descargarFoto: (_, _) => Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png")),
                flujoRapido: true, qrDemo: simulada ? _caneca.IdentificadorQr : null, estadoCaneca: estado);
        }

        [TestMethod]
        public async Task PrimeraFotoDeUnInvitadoSinStartAbreLaCanecaSimulada()
        {
            var eco = Eco(BotellaEnPet);

            var respuestas = await EnviarFotoAsync(eco, Invitado);

            Assert.IsEmpty(respuestas, "La apertura se anuncia cuando la caneca la confirma.");
            Assert.HasCount(1, _interacciones.Ordenes, "Antes se rechazaba porque la sesión aún no estaba en Oracle.");
            Assert.AreEqual(EstadoConversacion.OrdenEmitida, _conversaciones.Leer(Invitado)!.Estado);
        }

        [TestMethod]
        [DataRow("/start")]
        [DataRow("Hola")]
        [DataRow("Buenas")]
        public async Task ElSaludoEsUnaFraseConUnSoloBotonDeMenu(string texto)
        {
            RespuestaBot r = Unica(await EnviarConAsync(Eco(BotellaEnPet), texto));

            Assert.AreEqual("🌱 ¡Hola, soy Eco! Envíame una foto de tu residuo. 📸", r.Texto);
            Assert.AreEqual("☰ Menú", r.Botones.Single().Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task CanecaFisicaSinQr_LaFotoNuncaAbreYElQrLaRetoma()
        {
            var motor = Eco(BotellaEnPet, simulada: false);

            RespuestaBot pedir = Unica(await EnviarFotoAsync(motor));

            Assert.AreEqual(MensajesTelegram.EscaneaParaAbrir("una botella plástica"), pedir.Texto);
            Assert.IsEmpty(_interacciones.Ordenes, "Una foto por sí sola no demuestra presencia.");

            var tras = await EnviarConAsync(motor, "/start " + _caneca.IdentificadorQr);

            Assert.IsEmpty(tras);
            Assert.HasCount(1, _interacciones.Ordenes, "El QR retoma la foto guardada.");
        }

        [TestMethod]
        public async Task CanecaFisica_LaPresenciaDelQrVence()
        {
            var eco = Eco(BotellaEnPet, simulada: false);
            await EnviarConAsync(eco, "/start " + _caneca.IdentificadorQr);

            _reloj.Avanzar(ServicioConversacionTelegram.VigenciaPresencia + TimeSpan.FromSeconds(1));
            RespuestaBot r = Unica(await EnviarFotoAsync(eco));

            Assert.AreEqual(MensajesTelegram.EscaneaParaAbrir("una botella plástica"), r.Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task CanecaFisica_ElBotonDelPilotoNoIniciaSinQr()
        {
            await AgregarPilotoDirectoAsync();
            RespuestaBot r = Unica(await EnviarConAsync(Eco(BotellaEnPet, simulada: false), boton: "menu:piloto-iniciar"));

            Assert.AreEqual(MensajesTelegram.EscanearQr, r.Texto);
            Assert.IsNull(_conversaciones.Leer(Vinculado)?.IdInteraccion);
        }

        [TestMethod]
        public async Task MaterialRechazadoNuncaProvocaApertura()
        {
            RespuestaBot r = Unica(await EnviarFotoAsync(Eco(() => new PropuestaVision(TipoPropuestaVision.MaterialIdentificado, _vidrio.Codigo, 0.99m, Objeto: ObjetoVisible.EnvaseVidrio))));

            StringAssert.Contains(r.Texto, "no lo recibe");
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        [DataRow(TipoPropuestaVision.Incierta)]
        [DataRow(TipoPropuestaVision.ImagenBorrosa)]
        [DataRow(TipoPropuestaVision.VariosObjetos)]
        [DataRow(TipoPropuestaVision.SinResiduo)]
        [DataRow(TipoPropuestaVision.ObjetoDesconocido)]
        public async Task ImagenAmbiguaNoAbreYSoloOfreceOtraFotoOElegir(TipoPropuestaVision tipo)
        {
            RespuestaBot r = Unica(await EnviarFotoAsync(Eco(() => new PropuestaVision(tipo, null, null))));

            StringAssert.StartsWith(r.Texto, "🤔");
            CollectionAssert.AreEqual(new[] { "📸 Otra foto", "✏️ Elegir material" }, r.Botones.Select(b => b.Texto).ToArray());
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task SinEvidenciaDeObjeto_LaConfianzaAltaPideUnToque()
        {
            var eco = Eco(() => new PropuestaVision(TipoPropuestaVision.MaterialIdentificado, _pet.Codigo, 1m));

            RespuestaBot r = Unica(await EnviarFotoAsync(eco));

            Assert.IsEmpty(_interacciones.Ordenes, "La confianza declarada no autoriza una apertura.");
            await EnviarConAsync(eco, boton: r.Botones[0].Dato);
            Assert.HasCount(1, _interacciones.Ordenes);
        }

        [TestMethod]
        public async Task EnvaseSucioNoAbreSinUnToque()
        {
            RespuestaBot r = Unica(await EnviarFotoAsync(Eco(() => BotellaEnPet() with { Estado = EstadoObservable.Contaminado })));

            StringAssert.Contains(r.Texto, "sucio");
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task CanecaNoDisponible_DiceQueEsPeroNoOrdena()
        {
            var estado = new EstadoCanecaFalso { Disponible = false };
            RespuestaBot r = Unica(await EnviarFotoAsync(Eco(BotellaEnPet, estado: estado)));

            Assert.AreEqual("♻️ Identifiqué una botella plástica.\n" + MensajesTelegram.CanecaSinServicio, r.Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task SiFallaLaComprobacionDeLaCaneca_NoSeAbre()
        {
            RespuestaBot r = Unica(await EnviarFotoAsync(Eco(BotellaEnPet, estado: new EstadoCanecaFalso { Falla = true })));

            StringAssert.EndsWith(r.Texto, MensajesTelegram.CanecaSinServicio);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task CanecaOcupada_OtraPersonaNoRecibeSegundaOrden()
        {
            var eco = Eco(BotellaEnPet);
            await EnviarFotoAsync(eco);

            RespuestaBot otra = Unica(await EnviarFotoAsync(eco, Invitado));
            RespuestaBot misma = Unica(await EnviarFotoAsync(eco));

            StringAssert.EndsWith(otra.Texto, MensajesTelegram.CanecaEnUso);
            StringAssert.EndsWith(misma.Texto, MensajesTelegram.CanecaEnUso, "Fotos seguidas no generan otra orden.");
            Assert.HasCount(1, _interacciones.Ordenes);
        }

        [TestMethod]
        public async Task DepositosConsecutivos_CadaFotoTieneSuUnicaOrden()
        {
            var eco = Eco(BotellaEnPet);
            await EnviarFotoAsync(eco);
            _reloj.Avanzar(ServicioInteracciones.VigenciaOrden + TimeSpan.FromSeconds(1));
            await _servicioInteracciones.VencerOrdenesAsync();

            await EnviarFotoAsync(eco);

            Assert.HasCount(2, _interacciones.Ordenes);
            Assert.AreEqual(2, _interacciones.Ordenes.Select(o => o.ClaveIdempotencia).Distinct().Count());
        }

        [TestMethod]
        public async Task ElUpdateRepetidoDeUnaFotoNoCreaOtraOrden()
        {
            var eco = Eco(BotellaEnPet);
            await EnviarFotoAsync(eco, update: 7001);
            await EnviarFotoAsync(eco, update: 7001);

            Assert.HasCount(1, _interacciones.Ordenes);
        }

        [TestMethod]
        [DataRow("Te equivocaste")]
        [DataRow("No es eso")]
        [DataRow("Está mal")]
        public async Task CorreccionTrasLaOrdenNoAbreDeNuevo(string frase)
        {
            var eco = Eco(BotellaEnPet);
            await EnviarFotoAsync(eco);

            RespuestaBot r = Unica(await EnviarConAsync(eco, frase));

            Assert.AreEqual(MensajesTelegram.CorreccionTrasOrden, r.Texto);
            Assert.HasCount(1, _interacciones.Ordenes);
            Assert.IsNull(eco.FraseDeteccion(Vinculado), "Lo corregido ya no se anuncia como detectado.");
        }

        [TestMethod]
        public async Task CorreccionAntesDeAbrir_PermiteElegirSinOrden()
        {
            var eco = Eco(() => new PropuestaVision(TipoPropuestaVision.MaterialIdentificado, _pet.Codigo, 1m));
            await EnviarFotoAsync(eco);

            RespuestaBot r = Unica(await EnviarConAsync(eco, "Te equivocaste"));

            Assert.AreEqual(MensajesTelegram.CorregirClasificacion, r.Texto);
            Assert.IsTrue(r.Botones.Any(b => b.Dato == "menu:/manual"));
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        [DataRow("Otra foto")]
        [DataRow("otra fotografía")]
        public async Task OtraFotoNoSeBuscaComoMaterial(string texto)
        {
            Assert.AreEqual(MensajesTelegram.OtraFoto, Unica(await EnviarConAsync(Eco(BotellaEnPet), texto)).Texto);
        }

        [TestMethod]
        public async Task SiLaCanecaNoAdmiteElMaterial_NoSeAbre()
        {
            await _contenedores.AdmitirMaterialAsync(_caneca.Id, _vidrio.Id);
            await _contenedores.QuitarMaterialAsync(_caneca.Id, _pet.Id);

            await EnviarFotoAsync(Eco(BotellaEnPet));

            Assert.IsEmpty(_interacciones.Ordenes);
        }

        // Política de apertura automática: cada caso que queda fuera pide un toque.

        private static ClasificacionImagen Clasificada(string codigo, CategoriaMaterial categoria, ObjetoVisible? objeto,
            EstadoObservable estado = EstadoObservable.NoDeterminado, AdmisionMaterial admision = AdmisionMaterial.Admitido,
            ResultadoClasificacion resultado = ResultadoClasificacion.Identificado)
        {
            var material = new Material(1, codigo, codigo, categoria, "Descripción de prueba.", true);
            return new ClasificacionImagen(resultado, new ConsultaMaterial(material, GuiaSeparacion.Para(categoria), admision, null), 1m, "motor", "texto", objeto, estado);
        }

        [TestMethod]
        [DataRow("LATA_ALUMINIO", ObjetoVisible.Lata, true)]
        [DataRow("VIDRIO", ObjetoVisible.EnvaseVidrio, true)]
        [DataRow("CARTON", ObjetoVisible.Caja, true)]
        [DataRow("PLASTICO_SIN_ID", ObjetoVisible.BotellaPlastica, true)]
        [DataRow("PLASTICO_SIN_ID", ObjetoVisible.EnvasePlasticoRigido, false)]
        [DataRow("LATA_ALUMINIO", ObjetoVisible.Jeringa, false)]
        [DataRow("PAPEL", ObjetoVisible.Papel, false)]
        [DataRow("MULTICAPA", ObjetoVisible.EnvaseCompuesto, false)]
        public void PoliticaExigeCoherenciaEntreObjetoYMaterial(string codigo, ObjetoVisible objeto, bool abre)
        {
            Assert.AreEqual(abre, PoliticaAperturaAutomatica.Permite(Clasificada(codigo, CategoriaMaterial.Aprovechable, objeto)));
        }

        [TestMethod]
        public void PoliticaRechazaTodoLoQueNoSeaAprovechableLimpioYAdmitido()
        {
            Assert.IsFalse(PoliticaAperturaAutomatica.Permite(Clasificada("VIDRIO", CategoriaMaterial.NoAprovechable, ObjetoVisible.EnvaseVidrio)));
            Assert.IsFalse(PoliticaAperturaAutomatica.Permite(Clasificada("VIDRIO", CategoriaMaterial.Aprovechable, ObjetoVisible.EnvaseVidrio, EstadoObservable.Contaminado)));
            Assert.IsFalse(PoliticaAperturaAutomatica.Permite(Clasificada("VIDRIO", CategoriaMaterial.Aprovechable, ObjetoVisible.EnvaseVidrio, admision: AdmisionMaterial.NoAdmitido)));
            Assert.IsFalse(PoliticaAperturaAutomatica.Permite(Clasificada("VIDRIO", CategoriaMaterial.Aprovechable, null)));
            Assert.IsFalse(PoliticaAperturaAutomatica.Permite(Clasificada("VIDRIO", CategoriaMaterial.Aprovechable, ObjetoVisible.EnvaseVidrio, resultado: ResultadoClasificacion.Incierta)));
        }

        // Mensajes y menús definitivos.

        [TestMethod]
        public void LosMensajesDefinitivosSonExactos()
        {
            Assert.AreEqual("♻️ Identifiqué una botella plástica.\n🔓 ¡Caneca abierta! Tienes 10 segundos para depositarla.", MensajesTelegram.CanecaAbierta("una botella plástica", "PET"));
            Assert.AreEqual("♻️ Identifiqué cartón.\n🔓 ¡Caneca abierta! Tienes 10 segundos para depositarlo.", MensajesTelegram.CanecaAbierta("cartón", "Carton"));
            Assert.AreEqual("🔓 ¡Caneca abierta! Tienes 10 segundos para depositar PET.", MensajesTelegram.CanecaAbierta(null, "PET"), "Sin foto no se afirma una detección.");
            Assert.AreEqual("💚 Pequeñas acciones, grandes cambios.\n🌿 ¿Te gustó EcoAccess? ¡Descubre todo lo que puedes hacer!", MensajesTelegram.Despedida);
        }

        [TestMethod]
        public void AlCerrarElCicloAgradeceYPideValoracionOpcional()
        {
            var r = ServicioNotificacionesTelegram.Mensaje(new HechoNotificable("ci:1", TipoHechoNotificable.CicloCerrado, Vinculado, 1, null, "PET", null, null, DateTime.UtcNow));

            Assert.AreEqual("🌱 ¡Gracias por cuidar nuestro planeta! 💚\n⭐ ¿Cómo fue tu experiencia con Eco?", r.Texto);
            CollectionAssert.AreEqual(new[] { "⭐", "⭐⭐", "⭐⭐⭐", "⭐⭐⭐⭐", "⭐⭐⭐⭐⭐", "Omitir" }, r.Botones.Select(b => b.Texto).ToArray());
        }

        [TestMethod]
        public void LaAperturaSimuladaLoDice()
        {
            var r = ServicioNotificacionesTelegram.Mensaje(new HechoNotificable("ev:1", TipoHechoNotificable.Apertura, Vinculado, 1, null, "PET", null, null, DateTime.UtcNow, true, "una botella plástica"));

            StringAssert.StartsWith(r.Texto, "♻️ Identifiqué una botella plástica.");
            StringAssert.Contains(r.Texto, "🧪 Caneca simulada: no hay movimiento físico.");
        }

        [TestMethod]
        public void UnaOrdenVencidaSinAperturaSeAvisa()
        {
            var r = ServicioNotificacionesTelegram.Mensaje(new HechoNotificable("ov:1", TipoHechoNotificable.OrdenVencida, Vinculado, 1, null, "PET", null, null, DateTime.UtcNow));

            StringAssert.Contains(r.Texto, "no se abrió");
        }

        [TestMethod]
        public async Task OmitirMuestraLaDespedidaSinGuardarValoracion()
        {
            var repo = new OpinionesEnMemoria();
            RespuestaBot r = Unica(await ConOpiniones(repo).ProcesarAsync(new MensajeEntrante(++_update, Invitado, Invitado, true, null, "menu:omitir")));

            Assert.AreEqual(MensajesTelegram.Despedida, r.Texto);
            CollectionAssert.AreEqual(ServicioConversacionTelegram.BotonesDespedida.ToList(), r.Botones.ToList());
            Assert.IsEmpty(repo.Filas);
        }

        [TestMethod]
        public async Task PulsarVariasEstrellasGuardaUnaSolaValoracion()
        {
            var repo = new OpinionesEnMemoria();
            var servicio = ConOpiniones(repo);

            RespuestaBot primera = Unica(await servicio.ProcesarAsync(new MensajeEntrante(++_update, Invitado, Invitado, true, null, "menu:opinion:5")));
            RespuestaBot segunda = Unica(await servicio.ProcesarAsync(new MensajeEntrante(++_update, Invitado, Invitado, true, null, "menu:opinion:3")));

            Assert.AreEqual(MensajesTelegram.Despedida, primera.Texto);
            Assert.AreEqual(MensajesTelegram.Despedida, segunda.Texto);
            Assert.HasCount(1, repo.Filas);
        }

        [TestMethod]
        public async Task LaPresentacionConservaLosBotonesDefinitivos()
        {
            var eco = new EcoAccess.P3.Bot.Telegram.AsistenteEco((m, _) => Task.FromResult<System.Collections.Generic.IReadOnlyList<RespuestaBot>>(
                new[] { new RespuestaBot(m.ChatId, "x", ServicioConversacionTelegram.BotonesDespedida), new RespuestaBot(m.ChatId, "y", ServicioConversacionTelegram.BotonesSaludo) }));

            var r = await eco.ProcesarAsync(new MensajeEntrante(1, Vinculado, Vinculado, true, null, "menu:omitir"));

            Assert.AreEqual("☰ Menú completo", r[0].Botones.Last().Texto);
            Assert.AreEqual("☰ Menú", r[1].Botones.Single().Texto);
        }

        [TestMethod]
        public void LaDespedidaOfreceLosBotonesAcordados()
        {
            CollectionAssert.AreEqual(new[] { "📸 Depositar otro residuo", "🪙 Mis ecopuntos", "📊 Mi actividad", "🌎 Explorar EcoAccess", "🌿 Próximamente", "☰ Menú completo" },
                ServicioConversacionTelegram.BotonesDespedida.Select(b => b.Texto).ToArray());
        }

        [TestMethod]
        public void ElMenuTieneLasDiezOpcionesAcordadas()
        {
            CollectionAssert.AreEqual(new[] { "📸 Depositar otro residuo", "🪙 Mis ecopuntos", "📊 Mi actividad", "📜 Mis depósitos", "🔎 Buscar materiales",
                "♻️ Separación inteligente", "👤 Mi cuenta", "🌎 Conoce EcoAccess", "🌿 Próximamente", "🏠 Menú" },
                ServicioConversacionTelegram.MenuPrincipal.Select(b => b.Texto).ToArray());
        }

        [TestMethod]
        public async Task TodasLasOpcionesDelMenuYLaDespedidaResponden()
        {
            var eco = Eco(BotellaEnPet);
            foreach (BotonBot boton in ServicioConversacionTelegram.MenuPrincipal.Concat(ServicioConversacionTelegram.BotonesDespedida))
            {
                RespuestaBot r = Unica(await EnviarConAsync(eco, boton: boton.Dato));
                Assert.AreNotEqual(MensajesTelegram.NoEntendido, r.Texto, boton.Texto);
                Assert.IsGreaterThan(0, r.Texto.Length, boton.Texto);
            }
            Assert.IsEmpty(_interacciones.Ordenes, "Navegar nunca abre la caneca.");
        }

        [TestMethod]
        public async Task ProximamenteAbreCadaSeccionPorSeparadoYNoPresentaLoFuturoComoActivo()
        {
            var eco = Eco(BotellaEnPet);
            RespuestaBot inicio = Unica(await EnviarConAsync(eco, boton: "menu:/proximamente"));

            Assert.AreEqual(MensajesTelegram.ProximamenteInicio, inicio.Texto);
            Assert.HasCount(7, inicio.Botones, "Seis secciones y el menú.");
            foreach (BotonBot seccion in inicio.Botones.Take(6))
            {
                RespuestaBot r = Unica(await EnviarConAsync(eco, boton: seccion.Dato));
                Assert.IsLessThan(500, r.Texto.Length, seccion.Texto);
                Assert.IsTrue(r.Botones.Any(b => b.Dato == "menu:/proximamente"), seccion.Texto);
            }
            string upc = MensajesTelegram.SeccionesProximamente.Single(s => s.Clave == "upc").Texto;
            foreach (string lugar in new[] { "A, B, C, H, I y P", "comedor" }) StringAssert.Contains(upc, lugar);
            StringAssert.Contains(upc, "no hay canecas");
        }

        // Una sola fuente para la ventana: mensaje, firmware y script de la demostración deben decir lo mismo.

        private static string Raiz()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d is not null && !File.Exists(Path.Combine(d.FullName, "hardware", "servosecoacces", "configuracion_ejemplo.h"))) d = d.Parent;
            return d?.FullName ?? throw new InvalidOperationException("No se encontró la raíz del repositorio.");
        }

        [TestMethod]
        public void LaVentanaEsDeDiezSegundosEnTodosLosComponentes()
        {
            Assert.AreEqual(10, MensajesTelegram.VentanaDepositoSegundos);
            StringAssert.Contains(File.ReadAllText(Path.Combine(Raiz(), "hardware", "servosecoacces", "configuracion_ejemplo.h")), "ABIERTO_MS = 10000");
            string preparar = File.ReadAllText(Path.Combine(Raiz(), "scripts", "instalacion", "Preparar-Demostracion.ps1"));
            StringAssert.Contains(preparar, "VentanaSimuladaSegundos = 10");
            Assert.IsFalse(preparar.Contains("NotePropertyValue 20"));
        }
    }
}
