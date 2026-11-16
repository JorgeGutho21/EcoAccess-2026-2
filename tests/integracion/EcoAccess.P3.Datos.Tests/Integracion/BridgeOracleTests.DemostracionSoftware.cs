using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using SkiaSharp;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    public sealed partial class BridgeOracleTests
    {
        private sealed class ClasificadorNoDisponible : IClasificadorMaterial
        {
            public string Proveedor => "doble-modelo-no-disponible";
            public Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken ct = default) =>
                Task.FromException<PropuestaVision>(new HttpRequestException("fallo técnico sintético que no debe mostrarse al ciudadano"));
        }

        [TestMethod]
        public async Task DemostracionCiudadanoNuevo_FotoFalla_DeclaraTres_VerificaDos_AcreditaCuatro_YReinicia()
        {
            var e = await PrepararAsync();
            string correo = "br.n" + _marca + "@example.invalid";
            string contrasena = Compat.ToHexString(Compat.RandomGetBytes(8)); // 16 caracteres: política ciudadana de 6 a 20
            var alta = await new ServicioRegistroCiudadanos(new RepositorioRegistroCiudadanosOracle(Fabrica), new HasherPbkdf2(), TimeProvider.System)
                .RegistrarAsync("Ciudadano nuevo de ensayo", correo, contrasena, contrasena, true);
            Assert.IsTrue(alta.Exitoso, alta.Mensaje); RegistrarUsuario(alta.Valor);
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            var sesion = await new ServicioAutenticacion(new RepositorioCredencialesOracle(Fabrica), usuarios, new HasherPbkdf2(), TimeProvider.System)
                .IniciarSesionPublicaAsync(correo, contrasena);
            Assert.IsTrue(sesion.Exitoso, sesion.Mensaje); Assert.AreEqual(alta.Valor, sesion.Valor.IdUsuario);
            e = e with { Ciudadano = alta.Valor };
            await VincularTelegramAsync(e);
            var materiales = new RepositorioMaterialesOracle(Fabrica); var canecas = new RepositorioContenedoresOracle(Fabrica);
            string qr = (await canecas.ObtenerPorIdAsync(e.Config.IdContenedor))!.IdentificadorQr;
            var puntos = new ServicioEcopuntos(usuarios, e.Repo, materiales, new RepositorioEcopuntosOracle(Fabrica), TimeProvider.System);
            var regla = await puntos.RegistrarReglaAsync(e.Admin, e.Material, 2, "Tarifa por unidad del ensayo integral", DateTime.UtcNow.AddMinutes(-1), porUnidad: true);
            Assert.IsTrue(regla.Exitoso, regla.Mensaje); _regla = regla.Valor;
            var vision = new ServicioClasificacionImagen(new ClasificadorNoDisponible(), materiales, new ServicioCatalogoAmbiental(materiales, canecas));
            using var bitmap = new SKBitmap(32, 32); bitmap.Erase(SKColors.Green);
            using var imagen = SKImage.FromBitmap(bitmap); using var png = imagen.Encode(SKEncodedImageFormat.Png, 90);
            var bot = new ServicioConversacionTelegram(new RepositorioConversacionesTelegramOracle(Fabrica), canecas, materiales,
                new ServicioVinculacionTelegram(usuarios, new RepositorioVinculosTelegramOracle(Fabrica), new GeneradorTokensAleatorio(), TimeProvider.System),
                e.Servicio, new LimitadorUsoEnMemoria(100, TimeSpan.FromMinutes(1)), null, TimeProvider.System,
                vision: vision, descargarFoto: (_, _) => Task.FromResult(new ImagenResiduo(png.ToArray(), "image/png")), ecopuntos: puntos, qrDemo: qr);
            Hijo bridge = Iniciar(e, "--bridge"), simulador = Iniciar(e, "--dispositivo-simulado");
            await ListoAsync(bridge, e.Config.Eventos); await ListoAsync(simulador, e.Config.Comandos);
            await EnviarAsync(bot, "/start " + qr);
            await EnviarAsync(bot, "/cantidad 3");
            int solicitud = (await new RepositorioConversacionesTelegramOracle(Fabrica).ObtenerAsync(_telegram))!.IdInteraccion!.Value;
            var fallo = await bot.ProcesarAsync(new MensajeEntrante(NuevoUpdate(), _telegram, _telegram, true, null, null, "imagen-sintetica-del-ensayo"));
            Assert.IsTrue(fallo.SelectMany(r => r.Botones).Any(b => b.Dato == ServicioConversacionTelegram.PrefijoMenu + "/manual"));
            Assert.IsFalse(fallo.Any(r => r.Texto.Contains("fallo técnico sintético")));
            Assert.AreEqual(solicitud, (await new RepositorioConversacionesTelegramOracle(Fabrica).ObtenerAsync(_telegram))!.IdInteraccion);
            Assert.AreEqual(3, (await e.Repo.ObtenerInteraccionAsync(solicitud))!.CantidadDeclarada);
            Assert.AreEqual(CanalInteraccion.TelegramSimulado, (await e.Repo.ObtenerInteraccionAsync(solicitud))!.Canal);
            await EnviarAsync(bot, "/manual"); await EnviarAsync(bot, boton: ServicioConversacionTelegram.PrefijoMaterial + e.Material);
            var estimacion = (await EnviarAsync(bot, "/cantidad 3")).Single();
            StringAssert.Contains(estimacion.Texto, "Puntos potenciales: 6");
            Assert.AreEqual(0L, (await puntos.ConsultarMiosAsync(e.Ciudadano)).Valor.Saldo);
            string confirmar = estimacion.Botones.Single(b => b.Dato.StartsWith(ServicioConversacionTelegram.ConfirmarSi + ":")).Dato;
            await Task.WhenAll(EnviarAsync(bot, boton: confirmar), EnviarAsync(bot, boton: confirmar));
            int orden = (await OrdenesDeAsync(e.Config.IdContenedor)).Single();
            await EsperarAsync(async () => (await e.Repo.ObtenerOrdenAsync(orden))?.Estado == EstadoOrdenApertura.Cerrada, bridge, simulador);
            var deposito = (await e.Repo.ObtenerDepositoPorOrdenAsync(orden))!; _deposito = deposito.Id;
            Assert.IsFalse(deposito.EstaVerificado); Assert.IsNull(deposito.PesoMedidoKg);
            Assert.AreEqual(3, (await e.Repo.ObtenerInteraccionAsync((await e.Repo.ObtenerOrdenAsync(orden))!.IdInteraccion))!.CantidadDeclarada);
            string clave = "demo-unitaria-" + _marca;
            Assert.IsFalse((await puntos.AcreditarAsync(e.Operario, _deposito, _regla, clave)).Exitoso);
            Assert.IsTrue((await e.Servicio.VerificarDepositoAsync(e.Operario, _deposito, e.Material, cantidadVerificada: 2)).Exitoso);
            var duplicados = await Task.WhenAll(puntos.AcreditarAsync(e.Operario, _deposito, _regla, clave), puntos.AcreditarAsync(e.Operario, _deposito, _regla, clave));
            Assert.IsTrue(duplicados.All(r => r.Exitoso));
            Assert.AreEqual(duplicados[0].Valor.Single().Id, duplicados[1].Valor.Single().Id);
            Assert.AreEqual(4L, (await puntos.ConsultarMiosAsync(e.Ciudadano)).Valor.Saldo);
            var reiniciado = Conversacion(e);
            StringAssert.Contains(string.Join("\n", (await EnviarAsync(reiniciado, "/ecopuntos")).Select(r => r.Texto)), "4");
            Assert.HasCount(1, (await e.Servicio.ListarMisDepositosAsync(e.Ciudadano)).Valor);
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
            var periodo = HoraColombia.PeriodoUtc(hoy, hoy);
            var filtro = new FiltroInforme(periodo.DesdeUtc, periodo.HastaUtc, e.Config.IdContenedor, SoloDemostracion: true);
            var informes = new ServicioInformeInstitucional(usuarios, new RepositorioInformeInstitucionalOracle(Fabrica));
            var informe = await informes.ConsultarAsync(e.Admin, filtro);
            Assert.IsTrue(informe.Exitoso, informe.Mensaje);
            var indicadores = informe.Valor.Actual;
            Assert.AreEqual(1L, indicadores.Solicitudes); Assert.AreEqual(1L, indicadores.Depositos);
            Assert.AreEqual(1L, indicadores.Verificados); Assert.AreEqual(1L, indicadores.Ciudadanos);
            Assert.AreEqual(2L, indicadores.UnidadesVerificadas); Assert.AreEqual(0L, indicadores.VerificadosSinConteo);
            Assert.AreEqual(4L, indicadores.Acreditados); Assert.AreEqual(0L, indicadores.Reversos); Assert.AreEqual(4L, indicadores.Netos);
            Assert.AreEqual(1, informe.Valor.Canecas.Single().Depositos);
            var operativo = await informes.ConsultarAsync(e.Admin, filtro with { SoloDemostracion = false });
            Assert.IsTrue(operativo.Exitoso); Assert.AreEqual(0L, operativo.Valor.Actual.Depositos);
            Assert.AreEqual(0L, operativo.Valor.Actual.Solicitudes); Assert.AreEqual(0L, operativo.Valor.Actual.Acreditados);
            Assert.AreEqual(0L, informe.Valor.Anterior.Depositos);
            Assert.IsTrue((await puntos.RevertirAsync(e.Admin, duplicados[0].Valor.Single().Id, "reverso-demo-" + _marca, "Reversión de prueba del informe")).Exitoso);
            var reversado = await informes.ConsultarAsync(e.Admin, filtro);
            Assert.IsTrue(reversado.Exitoso); Assert.AreEqual(4L, reversado.Valor.Actual.Acreditados);
            Assert.AreEqual(-4L, reversado.Valor.Actual.Reversos); Assert.AreEqual(0L, reversado.Valor.Actual.Netos);
            Assert.AreEqual(0L, (await puntos.ConsultarMiosAsync(e.Ciudadano)).Valor.Saldo);
            RegistrarEvidencia("nuevo-cantidad", $"registro y autenticacion correctos; QR y foto fallida con respaldo manual; declaradas=3 potenciales=6 verificadas=2 credito=4 luego reverso=-4 neto=0; orden={orden} deposito={_deposito}; HTTP local firmado, simulador y Telegram doble; sin hardware ni Telegram real");
        }
        [TestMethod]
        public async Task DemostracionInvitado_QrDemoPersisteSimuladorYDepositoSinTitularNiCredito()
        {
            var e = await PrepararAsync();
            _telegram = 7_100_000_000L + Compat.RandomGetInt32(1, int.MaxValue);
            var canecas = new RepositorioContenedoresOracle(Fabrica);
            string qr = (await canecas.ObtenerPorIdAsync(e.Config.IdContenedor))!.IdentificadorQr;
            var bot = new ServicioConversacionTelegram(new RepositorioConversacionesTelegramOracle(Fabrica), canecas,
                new RepositorioMaterialesOracle(Fabrica), new ServicioVinculacionTelegram(new RepositorioUsuariosOracle(Fabrica),
                new RepositorioVinculosTelegramOracle(Fabrica), new GeneradorTokensAleatorio(), TimeProvider.System),
                e.Servicio, new LimitadorUsoEnMemoria(100, TimeSpan.FromMinutes(1)), null, TimeProvider.System, qrDemo: qr);
            Hijo bridge = Iniciar(e, "--bridge"), sim = Iniciar(e, "--dispositivo-simulado");
            await ListoAsync(bridge, e.Config.Eventos); await ListoAsync(sim, e.Config.Comandos);
            await EnviarAsync(bot, "/start " + qr); await EnviarAsync(bot, boton: "menu:invitado");
            int solicitud = (await new RepositorioConversacionesTelegramOracle(Fabrica).ObtenerAsync(_telegram))!.IdInteraccion!.Value;
            Assert.AreEqual(CanalInteraccion.TelegramSimulado, (await e.Repo.ObtenerInteraccionAsync(solicitud))!.Canal);
            Assert.IsFalse((await e.Servicio.AutorizarAperturaVisitanteAsync(solicitud, e.Material, "intrusion-visitante")).Exitoso);
            Assert.IsFalse((await e.Servicio.ConsultarOperacionVisitanteAsync(solicitud, "intrusion-visitante")).Exitoso);
            await EnviarAsync(bot, boton: ServicioConversacionTelegram.PrefijoMaterial + e.Material);
            await EnviarAsync(bot, boton: ServicioConversacionTelegram.ConfirmarSi);
            int orden = (await OrdenesDeAsync(e.Config.IdContenedor)).Single();
            await EsperarAsync(async () => (await e.Repo.ObtenerOrdenAsync(orden))?.Estado == EstadoOrdenApertura.Cerrada, bridge, sim);
            var deposito = (await e.Repo.ObtenerDepositoPorOrdenAsync(orden))!; _deposito = deposito.Id;
            Assert.IsNull((await e.Repo.ObtenerInteraccionAsync(solicitud))!.IdUsuario);
            Assert.IsFalse(deposito.EstaVerificado); Assert.IsNull(deposito.PesoMedidoKg);
            RegistrarEvidencia("invitado-demo", "QR demo invitado, canal Simulador persistido, deposito sin titular, conteo ni peso; sin Telegram real ni hardware");
        }

    }
}
