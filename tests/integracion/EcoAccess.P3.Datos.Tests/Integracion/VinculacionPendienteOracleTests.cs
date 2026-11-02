using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Bot;
using System.Text;
using System.Text.RegularExpressions;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    // Vinculación de cuentas existentes contra Oracle real (V007). Si el esquema todavía no tiene V007, las pruebas
    // quedan Inconclusive: no se cuentan como superadas. Las filas propias se borran antes que los usuarios.
    [TestClass]
    [TestCategory(Categoria)]
    [DoNotParallelize]
    public sealed class VinculacionPendienteOracleTests : BaseIntegracionOracle
    {
        private static readonly DateTime Fecha = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        private readonly List<long> _telegrams = new List<long>();

        private sealed class RelojFijo : TimeProvider
        {
            private readonly DateTime _ahoraUtc;

            public RelojFijo(DateTime ahoraUtc)
            {
                _ahoraUtc = ahoraUtc;
            }

            public override DateTimeOffset GetUtcNow() => new(_ahoraUtc);
        }

        [TestInitialize]
        public async Task ExigirV007Async()
        {
            if (await ContarAsync("SELECT COUNT(*) FROM user_tables WHERE table_name = 'VINCULACION_PENDIENTE'") == 0)
            {
                Assert.Inconclusive("Este esquema no tiene V007: prueba de vinculación de cuentas existentes no ejecutada.");
            }
        }

        [TestCleanup]
        public async Task LimpiarVinculacionAsync()
        {
            if (Fabrica is null || _telegrams.Count == 0)
            {
                return;
            }

            using OracleConnection conexion = await Fabrica.AbrirAsync(CancellationToken.None);
            foreach (string sql in new[]
            {
                "DELETE FROM vinculacion_pendiente WHERE telegram_user_id = :id",
                "DELETE FROM vinculo_telegram WHERE telegram_user_id = :id",
                "DELETE FROM token_vinculacion WHERE telegram_user_id = :id",
            })
            {
                foreach (long telegram in _telegrams)
                {
                    using OracleCommand comando = ComandosOracle.Crear(conexion, sql);
                    comando.Agregar("id", OracleDbType.Int64, telegram);
                    await comando.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task<(ServicioVinculacionTelegram Servicio, RepositorioVinculosTelegramOracle Repositorio, long Telegram, int Cuenta, string Token)>
            PrepararEscenarioAsync()
        {
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            var ciudadano = new Usuario("Ciudadano vinculación IT", $"it.vinc{Sufijo().ToLowerInvariant()}@ejemplo.com", CodigoRol.Ciudadano, Fecha);
            ciudadano.RegistrarConsentimiento("v1", Fecha);
            int cuenta = RegistrarUsuario(await usuarios.AgregarAsync(ciudadano));

            long telegram = 7_000_000_000L + Compat.RandomGetInt32(1, int.MaxValue);
            _telegrams.Add(telegram);
            var repositorio = new RepositorioVinculosTelegramOracle(Fabrica);
            string token = "token-it-" + Sufijo();
            await repositorio.AgregarTokenAsync(new TokenVinculacion(ServicioVinculacionTelegram.CalcularHash(token), telegram, telegram, Fecha));
            var servicio = new ServicioVinculacionTelegram(usuarios, repositorio, new GeneradorTokensAleatorio(), new RelojFijo(Fecha.AddMinutes(1)));
            return (servicio, repositorio, telegram, cuenta, token);
        }

        [TestMethod]
        public async Task CodigoCorrecto_VinculaConsumeTokenBorraPendienteYNoCambiaRoles()
        {
            var (servicio, _, telegram, cuenta, token) = await PrepararEscenarioAsync();
            int rolesAntes = await ContarAsync($"SELECT COUNT(*) FROM usuario_rol WHERE id_usuario = {cuenta}");

            string codigo = (await servicio.PrepararVinculacionCuentaExistenteAsync(token, cuenta)).Valor!.Codigo;
            Assert.AreEqual(0, await ContarAsync($"SELECT COUNT(*) FROM vinculo_telegram WHERE telegram_user_id = {telegram}"),
                "Preparar no vincula.");
            var r = await servicio.ConfirmarVinculacionAsync(telegram, telegram, codigo);

            Assert.IsTrue(r.Exitoso, r.ToString());
            Assert.AreEqual(cuenta, r.Valor);
            Assert.AreEqual(1, await ContarAsync(
                $"SELECT COUNT(*) FROM vinculo_telegram WHERE telegram_user_id = {telegram} AND id_usuario = {cuenta} AND fecha_baja_utc IS NULL"));
            Assert.AreEqual(1, await ContarAsync(
                $"SELECT COUNT(*) FROM token_vinculacion WHERE telegram_user_id = {telegram} AND fecha_uso_utc IS NOT NULL AND id_usuario_registrado = {cuenta}"));
            Assert.AreEqual(0, await ContarAsync($"SELECT COUNT(*) FROM vinculacion_pendiente WHERE telegram_user_id = {telegram}"));
            Assert.AreEqual(rolesAntes, await ContarAsync($"SELECT COUNT(*) FROM usuario_rol WHERE id_usuario = {cuenta}"));
        }

        [TestMethod]
        public async Task CodigoVerificadoAntesDeReemplazarPendiente_NoPuedeConfirmarLaNueva()
        {
            var (servicio, repositorio, telegram, cuenta, token) = await PrepararEscenarioAsync();
            await servicio.PrepararVinculacionCuentaExistenteAsync(token, cuenta);
            var anterior = (await repositorio.ObtenerVinculacionPendienteAsync(telegram, Fecha.AddMinutes(1)))!;
            await servicio.PrepararVinculacionCuentaExistenteAsync(token, cuenta);
            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => repositorio.ConfirmarVinculacionPendienteAsync(
                anterior, Fecha.AddMinutes(1)));
            Assert.AreEqual(0, await ContarAsync($"SELECT COUNT(*) FROM vinculo_telegram WHERE telegram_user_id = {telegram}"));
            Assert.IsTrue((await servicio.ConsultarTokenAsync(token)).Exitoso);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task PaginaWeb_CuentaNuevaOExistente_PersisteHashYConfirmaSegundoCanalEnOracle(bool nueva)
        {
            var (servicio, _, telegram, cuenta, token) = await PrepararEscenarioAsync();
            const string clave = "vinculacion-ficticia-2026";
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            var credenciales = new RepositorioCredencialesOracle(Fabrica);
            var hasher = new HasherPbkdf2();
            string correo = (await usuarios.ObtenerPorIdAsync(cuenta))!.CorreoElectronico;
            if (!nueva)
            {
                var hash = hasher.Calcular(clave);
                await credenciales.AgregarAsync(new CredencialAcceso(cuenta, hash.Hash, hash.Sal, hash.Iteraciones, Fecha));
            }
            else correo = "it.web." + Sufijo().ToLowerInvariant() + "@example.invalid";
            var reloj = new RelojFijo(Fecha.AddMinutes(1));
            var pagina = new PaginaVinculacion(servicio, new ServicioAutenticacion(credenciales, usuarios, hasher, reloj),
                new ServicioRegistroCiudadanos(new RepositorioRegistroCiudadanosOracle(Fabrica), hasher, reloj),
                new LimitadorUsoEnMemoria(20, TimeSpan.FromMinutes(1)), new LimitadorUsoEnMemoria(20, TimeSpan.FromMinutes(1)), reloj);
            var formulario = await pagina.MostrarAsync("t=" + token);
            string cookie = formulario.Cookie!.Split(';')[0].Split('=')[1];
            string csrf = Regex.Match(formulario.Html, "name=\"csrf\" value=\"([a-f0-9]{64})\"").Groups[1].Value;
            string campos = $"t={token}&csrf={csrf}&correo={Uri.EscapeDataString(correo)}&clave={clave}&modo=" + (nueva ? "crear" : "entrar");
            if (nueva) campos += "&nombre=Ciudadano+Web+Ficticio&confirmacion=" + clave + "&acepta=si";
            var respuesta = await pagina.EnviarAsync("application/x-www-form-urlencoded", Encoding.UTF8.GetBytes(campos), cookie);
            if (nueva)
            {
                // Registrar para la limpieza también si una aserción posterior falla.
                var creadas = await usuarios.ListarAsync();
                var creada = creadas.SingleOrDefault(u => u.CorreoElectronico == correo);
                if (creada is not null) cuenta = RegistrarUsuario(creada.Id);
            }
            Assert.AreEqual(200, respuesta.Codigo);
            string codigo = Regex.Match(respuesta.Html, @"/confirmar ([A-Z2-9]{8})").Groups[1].Value;
            Assert.AreEqual(8, codigo.Length);
            Assert.AreEqual(0, await ContarAsync($"SELECT COUNT(*) FROM vinculo_telegram WHERE telegram_user_id={telegram}"));
            Assert.IsTrue((await servicio.ConfirmarVinculacionAsync(telegram, telegram, codigo)).Exitoso);
            Assert.AreEqual(1, await ContarAsync($"SELECT COUNT(*) FROM vinculo_telegram WHERE telegram_user_id={telegram} AND id_usuario={cuenta}"));
            CollectionAssert.AreEquivalent(new[] { CodigoRol.Ciudadano }, (await usuarios.ObtenerPorIdAsync(cuenta))!.Roles.ToArray());
            var almacenada = (await credenciales.ObtenerPorUsuarioAsync(cuenta))!;
            Assert.IsTrue(hasher.Verificar(clave, almacenada.Sal, almacenada.Iteraciones, almacenada.Hash));
            Assert.DoesNotContain(clave, respuesta.Html);
        }

        [TestMethod]
        public async Task ConfirmacionesSimultaneas_CreanUnSoloVinculo()
        {
            var (servicio, repositorio, telegram, cuenta, token) = await PrepararEscenarioAsync();
            Assert.IsTrue((await servicio.PrepararVinculacionCuentaExistenteAsync(token, cuenta)).Exitoso);
            VinculacionPendiente pendiente = (await repositorio.ObtenerVinculacionPendienteAsync(telegram, Fecha.AddMinutes(1)))!;

            var resultados = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
            {
                try { await repositorio.ConfirmarVinculacionPendienteAsync(pendiente, Fecha.AddMinutes(1)); return true; }
                catch (CambioConcurrenteException) { return false; }
            }));

            Assert.AreEqual(1, resultados.Count(exito => exito), "Una sola transacción debe crear el vínculo.");
            Assert.AreEqual(1, await ContarAsync($"SELECT COUNT(*) FROM vinculo_telegram WHERE telegram_user_id = {telegram}"));
        }

        [TestMethod]
        public async Task PendienteSustituidaPorOtraCuenta_LaConfirmacionDeLaAnteriorNoVincula()
        {
            var (servicio, repositorio, telegram, cuenta, token) = await PrepararEscenarioAsync();
            Assert.IsTrue((await servicio.PrepararVinculacionCuentaExistenteAsync(token, cuenta)).Exitoso);
            VinculacionPendiente leida = (await repositorio.ObtenerVinculacionPendienteAsync(telegram, Fecha.AddMinutes(1)))!;

            var otro = new Usuario("Otro ciudadano vinculación IT", $"it.vinc{Sufijo().ToLowerInvariant()}@ejemplo.com", CodigoRol.Ciudadano, Fecha);
            otro.RegistrarConsentimiento("v1", Fecha);
            int otraCuenta = RegistrarUsuario(await new RepositorioUsuariosOracle(Fabrica).AgregarAsync(otro));
            Assert.IsTrue((await servicio.PrepararVinculacionCuentaExistenteAsync(token, otraCuenta)).Exitoso);

            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(
                () => repositorio.ConfirmarVinculacionPendienteAsync(leida, Fecha.AddMinutes(1)));
            Assert.AreEqual(0, await ContarAsync($"SELECT COUNT(*) FROM vinculo_telegram WHERE telegram_user_id = {telegram}"));
            Assert.AreEqual(1, await ContarAsync(
                $"SELECT COUNT(*) FROM vinculacion_pendiente WHERE telegram_user_id = {telegram} AND id_usuario = {otraCuenta}"));
        }

        [TestMethod]
        public async Task CincoCodigosIncorrectos_AgotanLaPendienteYElCorrectoYaNoVincula()
        {
            var (servicio, _, telegram, cuenta, token) = await PrepararEscenarioAsync();
            string codigo = (await servicio.PrepararVinculacionCuentaExistenteAsync(token, cuenta)).Valor!.Codigo;

            for (int i = 0; i < ServicioVinculacionTelegram.MaximoIntentosCodigo; i++)
            {
                Assert.AreEqual(MotivoRechazo.DatosInvalidos, (await servicio.ConfirmarVinculacionAsync(telegram, telegram, "ZZZZZZZZ")).Motivo);
            }

            Assert.AreEqual(5, await ContarAsync($"SELECT intentos FROM vinculacion_pendiente WHERE telegram_user_id = {telegram}"));
            Assert.IsFalse((await servicio.ConfirmarVinculacionAsync(telegram, telegram, codigo)).Exitoso);
            Assert.AreEqual(0, await ContarAsync($"SELECT COUNT(*) FROM vinculo_telegram WHERE telegram_user_id = {telegram}"));
        }

        [TestMethod]
        public async Task CuentaDesactivadaAntesDeConfirmar_LaTransaccionRechazaSinVincular()
        {
            var (servicio, _, telegram, cuenta, token) = await PrepararEscenarioAsync();
            string codigo = (await servicio.PrepararVinculacionCuentaExistenteAsync(token, cuenta)).Valor!.Codigo;
            await new RepositorioUsuariosOracle(Fabrica).CambiarActivoAsync(cuenta, false);

            var r = await servicio.ConfirmarVinculacionAsync(telegram, telegram, codigo);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            Assert.AreEqual(0, await ContarAsync($"SELECT COUNT(*) FROM vinculo_telegram WHERE telegram_user_id = {telegram}"));
            Assert.AreEqual(0, await ContarAsync(
                $"SELECT COUNT(*) FROM token_vinculacion WHERE telegram_user_id = {telegram} AND fecha_uso_utc IS NOT NULL"));
        }
    }
}
