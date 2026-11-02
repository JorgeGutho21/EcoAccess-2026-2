using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    public sealed partial class BridgeOracleTests
    {
        private long _segundoTelegram;

        [TestMethod]
        public async Task RecorridoDosCiudadanosConcurrentes_UnaCanecaSinMezclarOrdenDepositoOPuntos()
        {
            var e = await PrepararAsync(); await VincularTelegramAsync(e);
            var usuarios = new RepositorioUsuariosOracle(Fabrica); var ahora = DateTime.UtcNow;
            var segundo = new Usuario("Segundo ciudadano ficticio", "segundo." + _marca + "@example.invalid", CodigoRol.Ciudadano, ahora);
            segundo.RegistrarConsentimiento("prueba-concurrencia", ahora);
            int idSegundo = RegistrarUsuario(await usuarios.AgregarAsync(segundo));
            _segundoTelegram = 7_300_000_000L + Compat.RandomGetInt32(1, int.MaxValue);
            using (var c = await Fabrica.AbrirAsync(CancellationToken.None))
            using (var cmd = ComandosOracle.Crear(c, "INSERT INTO vinculo_telegram(id_usuario,telegram_user_id,fecha_vinculo_utc) VALUES (:u,:t,:f)"))
            {
                cmd.Agregar("u", OracleDbType.Int32, idSegundo); cmd.Agregar("t", OracleDbType.Int64, _segundoTelegram);
                cmd.Agregar("f", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(ahora)); await cmd.ExecuteNonQueryAsync();
            }
            var bot = Conversacion(e);
            Task<IReadOnlyList<RespuestaBot>> EnviarSegundo(string? texto = null, string? boton = null) =>
                bot.ProcesarAsync(new(NuevoUpdate(), _segundoTelegram, _segundoTelegram, true, texto, boton));
            string qr = (await new RepositorioContenedoresOracle(Fabrica).ObtenerPorIdAsync(e.Config.IdContenedor))!.IdentificadorQr;
            await Task.WhenAll(EnviarAsync(bot, "/start " + qr), EnviarSegundo("/start " + qr));
            await Task.WhenAll(EnviarAsync(bot, boton: ServicioConversacionTelegram.PrefijoMaterial + e.Material),
                EnviarSegundo(boton: ServicioConversacionTelegram.PrefijoMaterial + e.Material));
            var repoConversaciones = new RepositorioConversacionesTelegramOracle(Fabrica);
            int primera = (await repoConversaciones.ObtenerAsync(_telegram))!.IdInteraccion!.Value;
            int segunda = (await repoConversaciones.ObtenerAsync(_segundoTelegram))!.IdInteraccion!.Value;
            Assert.AreNotEqual(primera, segunda);
            await Task.WhenAll(EnviarAsync(bot, boton: ServicioConversacionTelegram.ConfirmarSi), EnviarSegundo(boton: ServicioConversacionTelegram.ConfirmarSi));
            int orden = (await OrdenesDeAsync(e.Config.IdContenedor)).Single();
            var solicitud = (await e.Repo.ObtenerInteraccionAsync((await e.Repo.ObtenerOrdenAsync(orden))!.IdInteraccion))!;
            int ganador = solicitud.Id == primera ? e.Ciudadano : idSegundo;
            Assert.AreEqual(ganador, solicitud.IdUsuario);
            Hijo bridge = Iniciar(e, "--bridge"), sim = Iniciar(e, "--dispositivo-simulado");
            await ListoAsync(bridge, e.Config.Eventos); await ListoAsync(sim, e.Config.Comandos);
            await EsperarAsync(async () => (await e.Repo.ObtenerOrdenAsync(orden))?.Estado == EstadoOrdenApertura.Cerrada, bridge, sim);
            _deposito = (await e.Repo.ObtenerDepositoPorOrdenAsync(orden))!.Id;
            Assert.AreEqual(1, await CantidadDepositosAsync(orden));
            Assert.IsTrue((await e.Servicio.VerificarDepositoAsync(e.Operario, _deposito, e.Material)).Exitoso);
            var repoPuntos = new RepositorioEcopuntosOracle(Fabrica);
            var puntos = new ServicioEcopuntos(usuarios, e.Repo, new RepositorioMaterialesOracle(Fabrica), repoPuntos, TimeProvider.System);
            var regla = await puntos.RegistrarReglaAsync(e.Admin, e.Material, 10, "Ensayo concurrencia", DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddHours(1));
            Assert.IsTrue(regla.Exitoso); _regla = regla.Valor;
            string clave = "concurrente-" + _marca;
            Assert.IsTrue((await puntos.AcreditarAsync(e.Operario, _deposito, _regla, clave)).Exitoso);
            Assert.IsTrue((await puntos.AcreditarAsync(e.Operario, _deposito, _regla, clave)).Exitoso);
            Assert.AreEqual(10L, (await repoPuntos.ConsultarAsync(ganador)).Saldo);
            Assert.AreEqual(0L, (await repoPuntos.ConsultarAsync(ganador == e.Ciudadano ? idSegundo : e.Ciudadano)).Saldo);
            RegistrarEvidencia("dos-ciudadanos", $"interacciones={primera},{segunda} orden={orden} deposito={_deposito} titular={ganador} credito=10 otro_saldo=0");
        }

        [TestMethod]
        public async Task RecorridoCanecaSinRespuesta_NoInventaDepositoYRecuperaSinDuplicar()
        {
            var e = await PrepararAsync(); await VincularTelegramAsync(e); var bot = Conversacion(e);
            Hijo bridge = Iniciar(e, "--bridge"); await ListoAsync(bridge, e.Config.Eventos);
            string qr = (await new RepositorioContenedoresOracle(Fabrica).ObtenerPorIdAsync(e.Config.IdContenedor))!.IdentificadorQr;
            await EnviarAsync(bot, "/start " + qr);
            await EnviarAsync(bot, boton: ServicioConversacionTelegram.PrefijoMaterial + e.Material);
            await EnviarAsync(bot, boton: ServicioConversacionTelegram.ConfirmarSi);
            int orden = (await OrdenesDeAsync(e.Config.IdContenedor)).Single();
            await Task.Delay(2000);
            Assert.IsNull(await e.Repo.ObtenerDepositoPorOrdenAsync(orden));
            Assert.AreEqual(0L, (await new RepositorioEcopuntosOracle(Fabrica).ConsultarAsync(e.Ciudadano)).Saldo);
            Assert.IsTrue((await e.Repo.ObtenerOrdenAsync(orden))!.Estado is EstadoOrdenApertura.Emitida or EstadoOrdenApertura.Enviada);
            Hijo sim = Iniciar(e, "--dispositivo-simulado"); await ListoAsync(sim, e.Config.Comandos);
            await EsperarAsync(async () => (await e.Repo.ObtenerOrdenAsync(orden))?.Estado == EstadoOrdenApertura.Cerrada, bridge, sim);
            _deposito = (await e.Repo.ObtenerDepositoPorOrdenAsync(orden))!.Id;
            Assert.HasCount(1, await OrdenesDeAsync(e.Config.IdContenedor)); Assert.AreEqual(1, await CantidadDepositosAsync(orden));
            Assert.IsFalse((await e.Repo.ObtenerDepositoPorOrdenAsync(orden))!.EstaVerificado);
            RegistrarEvidencia("caneca-no-responde", $"orden={orden} sin_dispositivo_deposito=0 saldo=0 recuperacion=si deposito={_deposito} ordenes=1");
        }
    }
}
