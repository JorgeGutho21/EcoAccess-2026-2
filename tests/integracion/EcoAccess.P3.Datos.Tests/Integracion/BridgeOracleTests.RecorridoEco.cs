using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Datos.Tests.Integracion
{
    public sealed partial class BridgeOracleTests
    {
        private sealed class RegistroAvisosPrueba : IRegistroNotificacionesEnviadas
        {
            public EstadoNotificaciones? Cargar() => new EstadoNotificaciones(new Dictionary<string, DateTime>(), new Dictionary<string, int>());
            public void Guardar(EstadoNotificaciones estado) { }
        }

        /// <summary>
        /// Recorrido completo del ciudadano con el modelo visual real, Oracle y el bridge con simulador: registro, sesión,
        /// foto, apertura automática, orden, ventana de 7 segundos, cierre, avisos, verificación, ecopuntos, historial,
        /// estadísticas y valoración. No usa Telegram real ni hardware, y así queda registrado en la evidencia.
        /// </summary>
        [TestMethod, TestCategory("VisionLocalReal")]
        public async Task RecorridoCompletoDeEco_FotoRealConfirmacionVentanaVerificacionYValoracion()
        {
            if (Environment.GetEnvironmentVariable("ECOACCESS_PROBAR_VISION_INTEGRADA") != "1")
                Assert.Inconclusive("Requiere ejecución explícita del modelo local y corpus; no equivale a Telegram real.");
            using var diagnostico = new DiagnosticoOllamaLocal();
            Assert.IsTrue((await diagnostico.ComprobarAsync()).ListoParaInferencia, "Ollama o el modelo visual no están disponibles.");

            var e = await PrepararAsync();
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            var materiales = new RepositorioMaterialesOracle(Fabrica);
            var canecas = new RepositorioContenedoresOracle(Fabrica);

            // 1 y 2. Registro de ciudadano e inicio de sesión.
            string correo = "br.eco" + _marca + "@example.invalid";
            string clave = Compat.ToHexString(Compat.RandomGetBytes(8)); // 16 caracteres: política ciudadana de 6 a 20
            var alta = await new ServicioRegistroCiudadanos(new RepositorioRegistroCiudadanosOracle(Fabrica), new HasherPbkdf2(), TimeProvider.System)
                .RegistrarAsync("Ciudadano ficticio del recorrido", correo, clave, clave, true);
            Assert.IsTrue(alta.Exitoso, alta.Mensaje); RegistrarUsuario(alta.Valor);
            var sesion = await new ServicioAutenticacion(new RepositorioCredencialesOracle(Fabrica), usuarios, new HasherPbkdf2(), TimeProvider.System)
                .IniciarSesionPublicaAsync(correo, clave);
            Assert.IsTrue(sesion.Exitoso, sesion.Mensaje);
            e = e with { Ciudadano = alta.Valor };
            await VincularTelegramAsync(e);

            // Foto real de cartón: material admitido en esta caneca, con tarifa por unidad.
            var carton = (await materiales.ListarAsync(true)).Single(m => m.Codigo == "CARTON");
            var servicioCanecas = new ServicioContenedores(usuarios, canecas, new RepositorioPuntosInstalacionOracle(Fabrica),
                materiales, new GeneradorIdentificadorQrAleatorio(), TimeProvider.System);
            Assert.IsTrue((await servicioCanecas.AdmitirMaterialAsync(e.Admin, e.Config.IdContenedor, carton.Id)).Exitoso);
            var puntos = new ServicioEcopuntos(usuarios, e.Repo, materiales, new RepositorioEcopuntosOracle(Fabrica), TimeProvider.System);
            var regla = (await puntos.ListarReglasAsync(e.Admin)).Valor.FirstOrDefault(r => r.IdMaterial == carton.Id && r.EstaVigente(DateTime.UtcNow));
            int idRegla;
            if (regla is null)
            {
                var nueva = await puntos.RegistrarReglaAsync(e.Admin, carton.Id, 2, "Tarifa ficticia del recorrido", DateTime.UtcNow.AddMinutes(-1), porUnidad: true);
                Assert.IsTrue(nueva.Exitoso, nueva.Mensaje); _regla = nueva.Valor; idRegla = nueva.Valor;
            }
            else idRegla = regla.Id;
            string ruta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Modelos", "trashnet-6fa2b87", "cardboard", "cardboard100.jpg");
            byte[] foto = await Compat.ReadAllBytesAsync(ruta);

            using var clasificador = new ClasificadorOllamaLocal();
            var observado = new VisionLocalObservado(clasificador);
            var vision = new ServicioClasificacionImagen(observado, materiales, new ServicioCatalogoAmbiental(materiales, canecas),
                ClasificadorOllamaLocal.TiempoMaximoInferencia + TimeSpan.FromSeconds(10));
            string qr = (await canecas.ObtenerPorIdAsync(e.Config.IdContenedor))!.IdentificadorQr;
            var bot = new ServicioConversacionTelegram(new RepositorioConversacionesTelegramOracle(Fabrica), canecas, materiales,
                new ServicioVinculacionTelegram(usuarios, new RepositorioVinculosTelegramOracle(Fabrica), new GeneradorTokensAleatorio(), TimeProvider.System),
                e.Servicio, new LimitadorUsoEnMemoria(100, TimeSpan.FromMinutes(1)), null, TimeProvider.System,
                ecopuntos: puntos, vision: vision, descargarFoto: (_, _) => Task.FromResult(new ImagenResiduo(foto, "image/jpeg")), qrDemo: qr,
                estadisticas: new ServicioEstadisticas(usuarios, new RepositorioEstadisticasOracle(Fabrica)),
                opiniones: new ServicioOpiniones(usuarios, new RepositorioOpinionesOracle(Fabrica), TimeProvider.System),
                flujoRapido: true);

            _ventanaSimulada = MensajesTelegram.VentanaDepositoSegundos;
            Hijo bridge = Iniciar(e, "--bridge"), simulador = Iniciar(e, "--dispositivo-simulado");
            await ListoAsync(bridge, e.Config.Eventos); await ListoAsync(simulador, e.Config.Comandos);

            // 3. Inicio del chatbot desde el QR.
            var saludo = (await EnviarAsync(bot, "/start " + qr)).Single();
            StringAssert.StartsWith(saludo.Texto, MensajesTelegram.SaludoEco);

            // 4 a 9. Foto, reconocimiento real, reglas y orden única. La confianza no decide: decide el par objeto-material.
            var tiempo = Stopwatch.StartNew();
            var identificacion = await bot.ProcesarAsync(new MensajeEntrante(NuevoUpdate(), _telegram, _telegram, true, null, null, "foto-corpus"));
            double segundosFoto = tiempo.Elapsed.TotalSeconds;
            Assert.AreEqual("CARTON", observado.Ultima!.CodigoMaterial);
            bool automatica = observado.Ultima.Objeto is { } visto && PoliticaAperturaAutomatica.Coinciden("CARTON", visto) && observado.Ultima.Estado != EstadoObservable.Contaminado;
            if (automatica) Assert.IsEmpty(identificacion, "La foto ya es la solicitud: no pregunta nada.");
            else
            {
                StringAssert.StartsWith(identificacion.Single().Texto, "♻️ Parece");
                Assert.IsEmpty(await OrdenesDeAsync(e.Config.IdContenedor), "Sin coincidencia, identificar no abre la caneca.");
                string confirmar = identificacion.Single().Botones[0].Dato;
                await Task.WhenAll(EnviarAsync(bot, boton: confirmar), EnviarAsync(bot, boton: confirmar));
            }
            int orden = (await OrdenesDeAsync(e.Config.IdContenedor)).Single();
            var interaccion = (await e.Repo.ObtenerInteraccionAsync((await e.Repo.ObtenerOrdenAsync(orden))!.IdInteraccion))!;
            Assert.AreEqual(1, interaccion.CantidadDeclarada);
            StringAssert.Contains((await EnviarAsync(bot, "Te equivocaste")).Single().Texto, "no se repite");
            Assert.AreEqual(orden, (await OrdenesDeAsync(e.Config.IdContenedor)).Single(), "Corregir no genera otra orden.");

            // 10, 11 y 12. Apertura simulada, ventana de 7 segundos y cierre.
            await EsperarAsync(async () => (await e.Repo.ObtenerOrdenAsync(orden))?.Estado == EstadoOrdenApertura.Abierta, bridge, simulador);
            double segundosHastaApertura = tiempo.Elapsed.TotalSeconds;
            var ventana = Stopwatch.StartNew();
            // El aviso de apertura se lee mientras la tapa está abierta, como lo hace el bot: dice los segundos que de verdad quedan.
            // (Leído después del cierre, el bot avisa que llegó tarde: «ya se está cerrando». Cierre V1, 15/11/2026.)
            var avisos = new List<RespuestaBot>();
            var notificaciones = new ServicioNotificacionesTelegram(new RepositorioNotificacionesTelegramOracle(Fabrica), new RegistroAvisosPrueba(), TimeProvider.System, bot.FraseDeteccion);
            await notificaciones.RevisarAsync((r, _) => { if (r.ChatId == _telegram) avisos.Add(r); return Task.CompletedTask; }, _ => { }, default);
            string abierta = avisos.Single(a => a.Texto.Contains("Apertura simulada")).Texto;
            StringAssert.Matches(abierta, new System.Text.RegularExpressions.Regex(@"Tienes ([3-9]|10) segundos"));
            StringAssert.Contains(abierta, "sin movimiento físico");
            Assert.DoesNotContain("¡Caneca abierta!", abierta, "Con la estación simulada no se afirma que una tapa se abrió.");
            await EsperarAsync(async () => (await e.Repo.ObtenerOrdenAsync(orden))?.Estado == EstadoOrdenApertura.Cerrada, bridge, simulador);
            double segundosAbierta = ventana.Elapsed.TotalSeconds;
            Assert.IsGreaterThan(8.0, segundosAbierta, "La apertura debe registrarse antes de que termine la ventana.");
            Assert.IsLessThan(14.0, segundosAbierta, "La ventana ya no es de 20 segundos.");

            // 13. Registro en Oracle y avisos: agradecer al cerrar y valoración opcional, sin afirmar que entró un residuo.
            var deposito = (await e.Repo.ObtenerDepositoPorOrdenAsync(orden))!; _deposito = deposito.Id;
            Assert.IsFalse(deposito.EstaVerificado);
            await notificaciones.RevisarAsync((r, _) => { if (r.ChatId == _telegram) avisos.Add(r); return Task.CompletedTask; }, _ => { }, default);
            Assert.HasCount(1, avisos.Where(a => a.Texto.Contains("Apertura simulada")), "La apertura se avisa una sola vez.");
            var cierre = avisos.Single(a => a.Texto.StartsWith(MensajesTelegram.Agradecimiento));
            StringAssert.Contains(cierre.Texto, MensajesTelegram.PreguntaExperiencia);
            Assert.HasCount(6, cierre.Botones, "Cinco estrellas y omitir.");
            Assert.IsFalse(await new RepositorioFallosCanecaOracle(Fabrica).TieneFalloRecienteAsync(e.Config.IdContenedor, DateTime.UtcNow.AddMinutes(-15)));

            // 14 y 15. Verificación independiente y ecopuntos sin duplicar.
            Assert.IsFalse((await puntos.AcreditarAsync(e.Operario, _deposito, idRegla, "eco-" + _marca)).Exitoso, "Sin verificar no hay puntos.");
            Assert.IsTrue((await e.Servicio.VerificarDepositoAsync(e.Operario, _deposito, carton.Id, cantidadVerificada: 1)).Exitoso);
            var creditos = await Task.WhenAll(puntos.AcreditarAsync(e.Operario, _deposito, idRegla, "eco-" + _marca),
                puntos.AcreditarAsync(e.Operario, _deposito, idRegla, "eco-" + _marca));
            Assert.IsTrue(creditos.All(c => c.Exitoso));
            Assert.AreEqual(creditos[0].Valor.Single().Id, creditos[1].Valor.Single().Id);
            long saldo = (await puntos.ConsultarMiosAsync(e.Ciudadano)).Valor.Saldo;
            Assert.IsGreaterThan(0L, saldo);

            // 16, 17 y 18. Historial, estadísticas y valoración.
            StringAssert.Contains(string.Join("\n", (await EnviarAsync(bot, "/depositos")).Select(r => r.Texto)), carton.Nombre);
            Assert.IsNotEmpty((await EnviarAsync(bot, "/estadisticas")).Single().Texto);
            var valoracion = await EnviarAsync(bot, boton: ServicioConversacionTelegram.PrefijoMenu + "opinion:5");
            Assert.AreEqual(MensajesTelegram.Despedida, valoracion.Single().Texto);

            RegistrarEvidencia("recorrido-completo", JsonSerializer.Serialize(new
            {
                Modelo = ClasificadorOllamaLocal.Modelo,
                Propuesta = observado.Ultima.CodigoMaterial,
                AperturaAutomatica = automatica,
                SegundosFotoHastaOrden = Math.Round(segundosFoto, 2),
                SegundosFotoHastaApertura = Math.Round(segundosHastaApertura, 2),
                SegundosAbierta = Math.Round(segundosAbierta, 1),
                Orden = orden,
                Deposito = _deposito,
                Saldo = saldo,
                TelegramReal = false,
                Simulacion = true,
                Hardware = false,
            }));
        }
    }
}
