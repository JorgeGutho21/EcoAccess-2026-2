using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    public sealed partial class BridgeOracleTests
    {
        [TestMethod]
        public async Task RecorridoInvitadoTelegram_OracleBridgeDepositoVerificadoSinEcopuntos()
        {
            var e = await PrepararAsync();
            _telegram = 7_200_000_000L + Compat.RandomGetInt32(1, int.MaxValue);
            var bot = Conversacion(e);
            string qr = (await new RepositorioContenedoresOracle(Fabrica).ObtenerPorIdAsync(e.Config.IdContenedor))!.IdentificadorQr;
            Hijo bridge = Iniciar(e, "--bridge"), sim = Iniciar(e, "--dispositivo-simulado");
            await ListoAsync(bridge, e.Config.Eventos); await ListoAsync(sim, e.Config.Comandos);
            await EnviarAsync(bot, "/start " + qr);
            await EnviarAsync(bot, boton: "menu:invitado");
            await EnviarAsync(bot, "/manual");
            await EnviarAsync(bot, boton: ServicioConversacionTelegram.PrefijoMaterial + e.Material);
            await Task.WhenAll(EnviarAsync(bot, boton: ServicioConversacionTelegram.ConfirmarSi),
                EnviarAsync(bot, boton: ServicioConversacionTelegram.ConfirmarSi));
            int orden = (await OrdenesDeAsync(e.Config.IdContenedor)).Single();
            await EsperarAsync(async () => (await e.Repo.ObtenerOrdenAsync(orden))?.Estado == EstadoOrdenApertura.Cerrada, bridge, sim);
            var deposito = (await e.Repo.ObtenerDepositoPorOrdenAsync(orden))!; _deposito = deposito.Id;
            var solicitud = (await e.Repo.ObtenerInteraccionAsync((await e.Repo.ObtenerOrdenAsync(orden))!.IdInteraccion))!;
            Assert.IsNull(solicitud.IdUsuario);
            Assert.AreEqual(CanalInteraccion.Telegram, solicitud.Canal);
            Assert.AreEqual(1, await CantidadDepositosAsync(orden));
            Assert.IsTrue((await e.Servicio.VerificarDepositoAsync(e.Operario, deposito.Id, e.Material)).Exitoso);
            var puntos = new ServicioEcopuntos(new RepositorioUsuariosOracle(Fabrica), e.Repo,
                new RepositorioMaterialesOracle(Fabrica), new RepositorioEcopuntosOracle(Fabrica), TimeProvider.System);
            var regla = await puntos.RegistrarReglaAsync(e.Admin, e.Material, 10, "Ensayo invitado sin crédito", DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddHours(1));
            Assert.IsTrue(regla.Exitoso); _regla = regla.Valor;
            Assert.IsFalse((await puntos.AcreditarAsync(e.Operario, deposito.Id, _regla, "invitado-" + _marca)).Exitoso);
            Assert.AreEqual(0L, (await new RepositorioEcopuntosOracle(Fabrica).ConsultarAsync(e.Ciudadano)).Saldo);
            StringAssert.Contains(string.Join("\n", (await EnviarAsync(bot, "/estado")).Select(r => r.Texto)), "sin ecopuntos");
            Assert.IsNull((await e.Repo.ObtenerInteraccionAsync(solicitud.Id))!.IdUsuario);
            RegistrarEvidencia("invitado-telegram", $"interaccion={solicitud.Id} canal=Telegram ciudadano=NULL orden={orden} deposito={deposito.Id} verificado=si saldo=0");
        }
    }
}
