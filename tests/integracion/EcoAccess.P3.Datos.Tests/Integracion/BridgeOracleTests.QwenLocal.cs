using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
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
        private sealed class QwenObservado : IClasificadorMaterial
        {
            private readonly IClasificadorMaterial _real;

            public QwenObservado(IClasificadorMaterial real)
            {
                _real = real;
            }

            public string Proveedor => _real.Proveedor;
            public PropuestaVision? Ultima { get; private set; }
            public async Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken ct = default)
            { Ultima = await _real.ClasificarAsync(imagen, catalogo, ct); return Ultima; }
        }

        [TestMethod, TestCategory("VisionLocalReal")]
        public async Task FotoQwenReal_VinculaContratoSeguro_ConsultaCatalogo_DeclaraYConfirma_HttpSimulado_Persiste()
        {
            if (Environment.GetEnvironmentVariable("ECOACCESS_PROBAR_QWEN_INTEGRADO") != "1")
                Assert.Inconclusive("Requiere ejecución explícita de Qwen local y corpus MIT; no equivale a Telegram real.");
            using var diagnostico = new DiagnosticoOllamaLocal();
            Assert.IsTrue((await diagnostico.ComprobarAsync()).ListoParaInferencia);
            var e = await PrepararAsync();
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            string correo = "br.qwen" + _marca + "@example.invalid";
            string claveCiudadano = Compat.ToHexString(Compat.RandomGetBytes(24));
            var alta = await new ServicioRegistroCiudadanos(new RepositorioRegistroCiudadanosOracle(Fabrica), new HasherPbkdf2(), TimeProvider.System)
                .RegistrarAsync("Ciudadano ficticio de Qwen", correo, claveCiudadano, claveCiudadano, true);
            Assert.IsTrue(alta.Exitoso, alta.Mensaje); RegistrarUsuario(alta.Valor);
            var autenticacion = new ServicioAutenticacion(new RepositorioCredencialesOracle(Fabrica), usuarios, new HasherPbkdf2(), TimeProvider.System);
            var sesion = await autenticacion.IniciarSesionPublicaAsync(correo, claveCiudadano);
            Assert.IsTrue(sesion.Exitoso, sesion.Mensaje); Assert.AreEqual(alta.Valor, sesion.Valor.IdUsuario);
            e = e with { Ciudadano = alta.Valor };
            var materiales = new RepositorioMaterialesOracle(Fabrica);
            var canecas = new RepositorioContenedoresOracle(Fabrica);
            var papel = (await materiales.ListarAsync(true)).Single(m => m.Codigo == "PAPEL");
            var servicioCanecas = new ServicioContenedores(usuarios, canecas, new RepositorioPuntosInstalacionOracle(Fabrica),
                materiales, new GeneradorIdentificadorQrAleatorio(), TimeProvider.System);
            Assert.IsTrue((await servicioCanecas.AdmitirMaterialAsync(e.Admin, e.Config.IdContenedor, papel.Id)).Exitoso);
            _telegram = 7_100_000_000L + Compat.RandomGetInt32(1, int.MaxValue);
            _telegramDemo = _telegram; // También limpiar tokens y pendientes del contrato seguro de este ensayo.
            var vinculacion = new ServicioVinculacionTelegram(usuarios, new RepositorioVinculosTelegramOracle(Fabrica), new GeneradorTokensAleatorio(), TimeProvider.System);
            var solicitud = await vinculacion.SolicitarAsync(_telegram, _telegram);
            Assert.IsTrue(solicitud.Exitoso);
            var pendiente = await vinculacion.PrepararVinculacionCuentaExistenteAsync(solicitud.Valor.Token, e.Ciudadano);
            Assert.IsTrue(pendiente.Exitoso);
            Assert.IsTrue((await vinculacion.ConfirmarVinculacionAsync(_telegram, _telegram, pendiente.Valor.Codigo)).Exitoso);
            string ruta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Modelos", "trashnet-6fa2b87", "paper", "paper495.jpg");
            byte[] foto = await Compat.ReadAllBytesAsync(ruta);
            string hash = Compat.ToHexString(Compat.Sha256(foto));
            Assert.AreEqual("00C4D3AE3FB39A268AF2E2AB7D567D64A6275A88EA04EA6E34E727570A3C541D", hash);
            var puntos = new ServicioEcopuntos(usuarios, e.Repo, materiales, new RepositorioEcopuntosOracle(Fabrica), TimeProvider.System);
            var reglas = await puntos.ListarReglasAsync(e.Admin); Assert.IsTrue(reglas.Exitoso);
            var regla = reglas.Valor.Where(r => r.IdMaterial == papel.Id && r.EstaVigente(DateTime.UtcNow))
                .OrderByDescending(r => r.DesdeUtc).ThenByDescending(r => r.Id).FirstOrDefault();
            Assert.IsNotNull(regla, "Preparar el piloto aislado y su tarifa ficticia antes de esta prueba.");
            Assert.IsTrue(regla.PorUnidad, "Esta regresión compara cantidad declarada y verificada.");
            // Regla existente prestada: no asignar _regla ni borrarla al limpiar los fixtures.
            using var qwen = new ClasificadorOllamaLocal();
            var observado = new QwenObservado(qwen);
            var vision = new ServicioClasificacionImagen(observado, materiales, new ServicioCatalogoAmbiental(materiales, canecas),
                ClasificadorOllamaLocal.TiempoMaximoInferencia + TimeSpan.FromSeconds(10));
            string qr = (await canecas.ObtenerPorIdAsync(e.Config.IdContenedor))!.IdentificadorQr;
            var bot = new ServicioConversacionTelegram(new RepositorioConversacionesTelegramOracle(Fabrica), canecas, materiales, vinculacion,
                e.Servicio, new LimitadorUsoEnMemoria(100, TimeSpan.FromMinutes(1)), null, TimeProvider.System,
                ecopuntos: puntos, vision: vision, descargarFoto: (_, _) => Task.FromResult(new ImagenResiduo(foto, "image/jpeg")), qrDemo: qr);
            Hijo bridge = Iniciar(e, "--bridge"), sim = Iniciar(e, "--dispositivo-simulado");
            await ListoAsync(bridge, e.Config.Eventos); await ListoAsync(sim, e.Config.Comandos);
            Assert.IsNotEmpty(await EnviarAsync(bot, "/materiales"));
            await EnviarAsync(bot, "/start " + qr); await EnviarAsync(bot, "/cantidad 3");
            int interaccion = (await new RepositorioConversacionesTelegramOracle(Fabrica).ObtenerAsync(_telegram))!.IdInteraccion!.Value;
            var respuesta = await bot.ProcesarAsync(new MensajeEntrante(NuevoUpdate(), _telegram, _telegram, true, null, null, "foto-publica-mit"));
            Assert.IsNotNull(observado.Ultima, "La integración no completó la inferencia local real.");
            Assert.IsNotEmpty(respuesta); Assert.IsEmpty(await OrdenesDeAsync(e.Config.IdContenedor));
            Assert.AreEqual(3, (await e.Repo.ObtenerInteraccionAsync(interaccion))!.CantidadDeclarada);
            // La persona revisa la sugerencia y elige papel manualmente: no exige precisión perfecta.
            await EnviarAsync(bot, "/manual");
            await EnviarAsync(bot, boton: ServicioConversacionTelegram.PrefijoMaterial + papel.Id);
            var confirmacion = await EnviarAsync(bot, "/potenciales");
            int potencial = regla.CalcularPuntos(3), acreditado = regla.CalcularPuntos(2);
            StringAssert.Contains(string.Join("\n", confirmacion.Select(r => r.Texto)), "Puntos potenciales: " + potencial);
            Assert.AreEqual(0L, (await puntos.ConsultarMiosAsync(e.Ciudadano)).Valor.Saldo);
            string boton = confirmacion.SelectMany(r => r.Botones).Single(b => b.Dato.StartsWith(ServicioConversacionTelegram.ConfirmarSi + ":")).Dato;
            await EnviarAsync(bot, boton: boton); await EnviarAsync(bot, boton: boton);
            int orden = (await OrdenesDeAsync(e.Config.IdContenedor)).Single();
            await EsperarAsync(async () => (await e.Repo.ObtenerOrdenAsync(orden))?.Estado == EstadoOrdenApertura.Cerrada, bridge, sim);
            var deposito = (await e.Repo.ObtenerDepositoPorOrdenAsync(orden))!; _deposito = deposito.Id;
            Assert.IsNull(deposito.PesoMedidoKg); Assert.IsFalse(deposito.EstaVerificado);
            string claveCredito = "qwen-integrado-" + _marca;
            Assert.IsFalse((await puntos.AcreditarAsync(e.Operario, _deposito, regla.Id, claveCredito)).Exitoso);
            Assert.IsTrue((await e.Servicio.VerificarDepositoAsync(e.Operario, _deposito, papel.Id, cantidadVerificada: 2)).Exitoso);
            var creditos = await Task.WhenAll(puntos.AcreditarAsync(e.Operario, _deposito, regla.Id, claveCredito),
                puntos.AcreditarAsync(e.Operario, _deposito, regla.Id, claveCredito));
            Assert.IsTrue(creditos.All(c => c.Exitoso));
            Assert.AreEqual(creditos[0].Valor.Single().Id, creditos[1].Valor.Single().Id);
            Assert.AreEqual((long)acreditado, (await puntos.ConsultarMiosAsync(e.Ciudadano)).Valor.Saldo);
            var reiniciado = Conversacion(e);
            string saldoTexto = string.Join("\n", (await EnviarAsync(reiniciado, "/ecopuntos")).Select(r => r.Texto));
            StringAssert.Contains(saldoTexto, $"Saldo: {acreditado} ecopuntos.");
            StringAssert.Contains(saldoTexto, $"depósito {_deposito}, regla {regla.Id}");
            string historial = string.Join("\n", (await EnviarAsync(reiniciado, "/depositos")).Select(r => r.Texto));
            StringAssert.Contains(historial, $"{papel.Nombre}, verificado, sin peso");
            Assert.HasCount(1, (await e.Servicio.ListarMisDepositosAsync(e.Ciudadano)).Valor);
            RegistrarEvidencia("qwen-integrado", JsonSerializer.Serialize(new
            {
                Modelo = ClasificadorOllamaLocal.Modelo,
                FotoSha256 = hash,
                Propuesta = observado.Ultima.Tipo.ToString(),
                observado.Ultima.CodigoMaterial,
                observado.Ultima.ConfianzaDeclarada,
                MaterialConfirmado = "PAPEL",
                CantidadDeclarada = 3,
                CantidadVerificada = 2,
                PuntosPotenciales = potencial,
                PuntosAcreditados = acreditado,
                CreditoDuplicado = false,
                RegistroAutenticacion = true,
                Vinculacion = "contrato seguro local con Oracle",
                TelegramReal = false,
                Orden = orden,
                Deposito = _deposito,
                Simulacion = true,
                Hardware = false,
                Limite = "Regresión integral con Qwen real y crédito; no evaluación independiente ni Telegram oficial"
            }));
        }
    }
}
