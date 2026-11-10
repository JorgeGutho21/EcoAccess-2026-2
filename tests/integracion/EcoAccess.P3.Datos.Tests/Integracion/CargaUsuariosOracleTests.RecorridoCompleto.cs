using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{
    /// <summary>
    /// Recorrido completo de un ciudadano nuevo con la misma composición que el bot en producción (fase de software):
    /// página de registro HTTP en proceso, Oracle Cloud, modelo visual local real y QR oficial leído desde su PNG.
    /// Telegram se sustituye por actualizaciones inyectadas: NO es una prueba desde un teléfono ni por la red de Telegram.
    /// </summary>
    public sealed partial class CargaUsuariosOracleTests
    {
        // Lata de TrashNet (MIT) del conjunto final, reconocida como lata en la última evaluación contra la piloto 1093.
        private const string FotoLata = @"C:\ProgramData\EcoAccessP3\Vision\p3-final\metal-metal310.jpg";
        private const string Sha256FotoLata = "10bff8c5d8e0dc1bdd5785f57de5e4a0b69913813781b10cf5ecf09b42bbe6b9";

        private ServicioConversacionTelegram EcoComoProduccion(Entorno e, ServicioClasificacionImagen? vision, byte[]? foto,
            IRepositorioConsultasVision? consultas)
        {
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            var materiales = new RepositorioMaterialesOracle(Fabrica);
            var contenedores = new RepositorioContenedoresOracle(Fabrica);
            var interacciones = new RepositorioInteraccionesOracle(Fabrica);
            var eco = new ServicioConversacionTelegram(new RepositorioConversacionesTelegramOracle(Fabrica), contenedores, materiales, e.Vinculacion,
                new ServicioInteracciones(usuarios, interacciones, contenedores, materiales, TimeProvider.System),
                new LimitadorUsoEnMemoria(20, TimeSpan.FromMinutes(1)), PaginaPublica, TimeProvider.System,
                ecopuntos: new ServicioEcopuntos(usuarios, interacciones, materiales, new RepositorioEcopuntosOracle(Fabrica), TimeProvider.System),
                vision: vision,
                descargarFoto: foto is null ? null : (_, _) => Task.FromResult(new ImagenResiduo(foto, "image/jpeg")),
                analisis: new RepositorioAnalisisImagenOracle(Fabrica),
                estadisticas: new ServicioEstadisticas(usuarios, new RepositorioEstadisticasOracle(Fabrica)),
                flujoRapido: vision is not null)
            {
                SinAperturaFisica = true,
                ConsultasVision = consultas,
            };
            return eco;
        }

        private static string RaizRepositorio()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "EcoAccess.P3.sln"))) dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("No se encontró la raíz del repositorio.");
        }

        [TestMethod, TestCategory("VisionLocalReal")]
        public async Task CiudadanoNuevo_QrOficial_Foto_Destino_Historial_Estadisticas_Y_Reinicio()
        {
            if (Environment.GetEnvironmentVariable("ECOACCESS_PROBAR_VISION_INTEGRADA") != "1")
                Assert.Inconclusive("Requiere el modelo local (Ollama) y ECOACCESS_PROBAR_VISION_INTEGRADA=1.");
            if (!File.Exists(FotoLata)) Assert.Inconclusive("Falta la foto de TrashNet del conjunto final.");
            byte[] foto = File.ReadAllBytes(FotoLata);
            Assert.AreEqual(Sha256FotoLata, Compat.ToHexString(Compat.Sha256(foto)).ToLowerInvariant(), "La foto de la prueba cambió.");
            using var diagnostico = new DiagnosticoOllamaLocal();
            if (!(await diagnostico.ComprobarAsync()).ListoParaInferencia) Assert.Inconclusive("Ollama no está listo.");

            var pasos = new Dictionary<string, long>();
            var reloj = Stopwatch.StartNew();
            long Marca(string paso) { long ms = reloj.ElapsedMilliseconds; pasos[paso] = ms; reloj.Restart(); return ms; }

            using var e = new Entorno(Fabrica);
            int interaccionesAntes = await ContarAsync("SELECT COUNT(*) FROM interaccion");
            int ordenesAntes = await ContarAsync("SELECT COUNT(*) FROM orden_apertura");
            int depositosAntes = await ContarAsync("SELECT COUNT(*) FROM deposito");

            // 1. Cuenta nueva: /vincular → página HTTPS (aquí HTTP en proceso) → código → vuelta al bot → saldo → /start.
            long cuenta = NuevaCuentaTelegram();
            int id = RegistrarUsuario(await RecorridoNuevoAsync(e, cuenta, Correo()));
            Marca("registro_y_vinculacion");

            // 2. QR oficial leído desde el PNG del repositorio, sin regenerarlo.
            string qrTexto;
            using (var imagen = new Bitmap(Path.Combine(RaizRepositorio(), "assets", "identidad", "QR_Caneca_piloto.png")))
                qrTexto = new ZXing.BarcodeReader().Decode(imagen)?.Text ?? throw new AssertFailedException("No se pudo leer el QR oficial.");
            string inicio = new Uri(qrTexto).Query.TrimStart('?').Split('&').Single(p => p.StartsWith("start=", StringComparison.Ordinal)).Substring(6);
            Assert.AreEqual(EnlaceTelegram.IdentificadorQrOficial, inicio);

            // 3. Echo con la composición de producción y el modelo local real.
            bool hayTabla = await new RepositorioConsultasVisionOracle(Fabrica).TablaDisponibleAsync();
            var consultas = hayTabla ? new RepositorioConsultasVisionOracle(Fabrica) : null;
            var materiales = new RepositorioMaterialesOracle(Fabrica);
            using var local = new ClasificadorOllamaLocal();
            var vision = new ServicioClasificacionImagen(local, materiales, new ServicioCatalogoAmbiental(materiales, new RepositorioContenedoresOracle(Fabrica)),
                ClasificadorOllamaLocal.TiempoMaximoInferencia + TimeSpan.FromSeconds(10));
            var eco = EcoComoProduccion(e, vision, foto, consultas);

            string qr = (await EnviarConAsync(eco, cuenta, "/start " + inicio)).Texto;
            StringAssert.Contains(qr, ServicioCatalogoAmbiental.NombreCanecaPiloto);
            StringAssert.Contains(qr, MensajesTelegram.AperturaPendiente);
            Marca("qr_oficial");

            long updateFoto = Interlocked.Increment(ref _siguienteUpdate);
            _updates.Add(updateFoto);
            var cronometro = Stopwatch.StartNew();
            RespuestaBot destino = (await eco.ProcesarAsync(new MensajeEntrante(updateFoto, cuenta, cuenta, true, null, null, "foto-publica-mit"))).Single();
            long latenciaFoto = cronometro.ElapsedMilliseconds;
            Marca("foto_a_destino");
            StringAssert.Contains(destino.Texto, "Caneca blanca", "La lata va a la caneca de aprovechables: " + destino.Texto);
            StringAssert.Contains(destino.Texto, MensajesTelegram.AperturaPendiente);
            Assert.DoesNotContain("abierta", destino.Texto, "No se afirma una apertura que no ocurrió.");

            // 4. Historial, ecopuntos y estadísticas personales: una consulta no es un depósito ni suma puntos.
            StringAssert.Contains((await EnviarConAsync(eco, cuenta, "/depositos")).Texto, MensajesTelegram.SinDepositos);
            Assert.AreEqual(MensajesConversacion.Saldo(0), (await EnviarConAsync(eco, cuenta, "¿Cuántos ecopuntos tengo?")).Texto);
            string actividad = (await EnviarConAsync(eco, cuenta, "/estadisticas")).Texto;
            StringAssert.Contains(actividad, "Tu actividad");
            if (hayTabla) StringAssert.Contains(actividad, "Fotos consultadas a Echo: 1 (con destino: 1;");
            Marca("historial_ecopuntos_estadisticas");

            // 5. Oracle: ni solicitudes ni órdenes ni depósitos nuevos; la consulta sí queda (si la tabla existe).
            Assert.AreEqual(interaccionesAntes, await ContarAsync("SELECT COUNT(*) FROM interaccion"), "El modo software no crea solicitudes.");
            Assert.AreEqual(ordenesAntes, await ContarAsync("SELECT COUNT(*) FROM orden_apertura"), "El modo software no emite órdenes.");
            Assert.AreEqual(depositosAntes, await ContarAsync("SELECT COUNT(*) FROM deposito"), "Una clasificación no es un depósito.");
            if (hayTabla)
            {
                Assert.AreEqual(1, await ContarAsync($"SELECT COUNT(*) FROM consulta_vision WHERE clave='telegram:{updateFoto}' AND id_usuario={id} AND modo=1"));
                _consultasVision.Add("telegram:" + updateFoto);
            }

            // 6. Reinicio: otro proceso que arranca de cero reconoce a la persona por Oracle y no duplica su cuenta.
            using var otroProceso = new Entorno(Fabrica);
            var reiniciado = EcoComoProduccion(otroProceso, vision, foto, consultas);
            Assert.AreEqual(MensajesConversacion.SaludoDeNuevo, (await EnviarConAsync(reiniciado, cuenta, "/start")).Texto);
            Assert.AreEqual(id, await e.Vinculacion.ObtenerUsuarioVinculadoAsync(cuenta));
            Marca("reinicio");
            await VerificarPersistenciaAsync(new[] { (await new RepositorioUsuariosOracle(Fabrica).ObtenerPorIdAsync(id))!.CorreoElectronico }, new long[] { cuenta });

            string? carpeta = Environment.GetEnvironmentVariable("ECOACCESS_E2E_SALIDA");
            if (!string.IsNullOrWhiteSpace(carpeta))
            {
                Directory.CreateDirectory(carpeta);
                File.WriteAllText(Path.Combine(carpeta, "recorrido_completo.json"), JsonSerializer.Serialize(new
                {
                    FechaUtc = DateTime.UtcNow,
                    Modelo = ClasificadorOllamaLocal.Modelo,
                    FotoSha256 = Sha256FotoLata,
                    TelegramReal = false,
                    PaginaRegistro = "HTTP en proceso con el mismo servidor de producción (sin Funnel)",
                    Oracle = "Oracle Cloud real",
                    ConsultaVisionPersistida = hayTabla,
                    Destino = destino.Texto,
                    LatenciaFotoMs = latenciaFoto,
                    PasosMs = pasos,
                }, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
            }
        }

        private readonly List<string> _consultasVision = new();

        private async Task<RespuestaBot> EnviarConAsync(ServicioConversacionTelegram eco, long cuenta, string texto)
        {
            long update = Interlocked.Increment(ref _siguienteUpdate);
            _updates.Add(update);
            RespuestaBot r = (await eco.ProcesarAsync(new MensajeEntrante(update, cuenta, cuenta, true, texto, null))).Single();
            Assert.AreEqual(cuenta, r.ChatId);
            return r;
        }

        /// <summary>Lo llama la limpieza de la clase antes de borrar los usuarios (la consulta los referencia).</summary>
        private async Task QuitarConsultasDeVisionAsync(OracleConnection conexion)
        {
            foreach (string clave in _consultasVision)
            {
                using OracleCommand comando = ComandosOracle.Crear(conexion, "DELETE FROM consulta_vision WHERE clave=:clave");
                comando.Agregar("clave", OracleDbType.Varchar2, clave);
                await comando.ExecuteNonQueryAsync();
            }
        }
    }
}
