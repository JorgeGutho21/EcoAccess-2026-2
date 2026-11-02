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
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    // Vinculación y conversaciones de Telegram contra Oracle real (V004). Las filas de las tablas de Telegram se
    // borran aquí, antes que la limpieza de la base borre los usuarios a los que apuntan.
    [TestClass]
    [TestCategory(Categoria)]
    [DoNotParallelize]
    public sealed class RepositoriosTelegramOracleTests : BaseIntegracionOracle
    {
        private static readonly DateTime Fecha = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        private readonly List<long> _telegrams = new List<long>();
        private readonly List<long> _updates = new List<long>();

        [TestCleanup]
        public async Task LimpiarTelegramAsync()
        {
            if (Fabrica is null)
            {
                return;
            }

            // Hijos antes que padres; un paso fallido no impide los demás y al final se informan juntos.
            var errores = new List<Exception>();
            using OracleConnection conexion = await Fabrica.AbrirAsync(CancellationToken.None);
            await BorrarAsync(conexion, "DELETE FROM conversacion_telegram WHERE telegram_user_id = :id", _telegrams, errores);
            await BorrarAsync(conexion, "DELETE FROM vinculo_telegram WHERE telegram_user_id = :id", _telegrams, errores);
            await BorrarAsync(conexion, "DELETE FROM token_vinculacion WHERE telegram_user_id = :id", _telegrams, errores);
            await BorrarAsync(conexion, "DELETE FROM update_telegram WHERE update_id = :id", _updates, errores);
            if (errores.Count > 0)
            {
                throw new AggregateException("La limpieza de las tablas de Telegram falló en algún paso.", errores);
            }
        }

        [TestMethod]
        public async Task Token_SeGuardaYSeLee_ElHashRepetidoChocaConLaLlave()
        {
            var repositorio = new RepositorioVinculosTelegramOracle(Fabrica);
            long telegram = NuevoTelegram();
            var token = new TokenVinculacion(NuevoHash(), telegram, telegram, Fecha);

            await repositorio.AgregarTokenAsync(token);
            TokenVinculacion? leido = await repositorio.ObtenerTokenAsync(token.HashToken);

            Assert.IsNotNull(leido);
            Assert.AreEqual(telegram, leido.TelegramUserId);
            Assert.AreEqual(Fecha, leido.CreacionUtc);
            Assert.AreEqual(Fecha + TokenVinculacion.Vigencia, leido.VenceUtc);
            Assert.IsNull(leido.UsoUtc);
            Assert.IsNull(await repositorio.ObtenerTokenAsync(NuevoHash()));
            var error = await Assert.ThrowsExactlyAsync<ConflictoUnicidadException>(() => repositorio.AgregarTokenAsync(token));
            Assert.AreEqual("pk_token_vinculacion", error.Message);
        }

        [TestMethod]
        public async Task Registro_CreaCiudadanoVinculaYConsumeElToken_UnSegundoUsoFalla()
        {
            var repositorio = new RepositorioVinculosTelegramOracle(Fabrica);
            long telegram = NuevoTelegram();
            var token = new TokenVinculacion(NuevoHash(), telegram, telegram, Fecha);
            await repositorio.AgregarTokenAsync(token);
            DateTime ahora = Fecha.AddMinutes(2);

            int id = RegistrarUsuario(await repositorio.RegistrarCiudadanoYVincularAsync(NuevoCiudadano(ahora), token.HashToken, telegram, ahora));

            Usuario? usuario = await new RepositorioUsuariosOracle(Fabrica).ObtenerPorIdAsync(id);
            Assert.IsNotNull(usuario);
            Assert.IsTrue(usuario.Roles.Contains(CodigoRol.Ciudadano));
            Assert.IsTrue(usuario.TieneConsentimiento);
            Assert.AreEqual(id, (await repositorio.ObtenerVinculoActivoPorTelegramAsync(telegram))?.IdUsuario);
            Assert.AreEqual(telegram, (await repositorio.ObtenerVinculoActivoPorUsuarioAsync(id))?.TelegramUserId);
            Assert.AreEqual(ahora, (await repositorio.ObtenerTokenAsync(token.HashToken))?.UsoUtc);
            Assert.AreEqual(1, await ContarAsync($"SELECT COUNT(*) FROM token_vinculacion WHERE hash_token = '{token.HashToken}' AND id_usuario_registrado = {id}"));

            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() =>
                repositorio.RegistrarCiudadanoYVincularAsync(NuevoCiudadano(ahora), token.HashToken, telegram, ahora));
        }

        [TestMethod]
        public async Task Registro_TokenVencidoAjenoOInexistente_NoCreaCuenta()
        {
            var repositorio = new RepositorioVinculosTelegramOracle(Fabrica);
            long telegram = NuevoTelegram();
            var token = new TokenVinculacion(NuevoHash(), telegram, telegram, Fecha);
            await repositorio.AgregarTokenAsync(token);
            Usuario vencido = NuevoCiudadano(Fecha);
            Usuario ajeno = NuevoCiudadano(Fecha);

            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() =>
                repositorio.RegistrarCiudadanoYVincularAsync(vencido, token.HashToken, telegram, token.VenceUtc));
            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() =>
                repositorio.RegistrarCiudadanoYVincularAsync(ajeno, token.HashToken, telegram + 1, Fecha.AddMinutes(1)));
            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() =>
                repositorio.RegistrarCiudadanoYVincularAsync(ajeno, NuevoHash(), telegram, Fecha.AddMinutes(1)));

            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() =>
                repositorio.RegistrarCiudadanoYVincularAsync(ajeno, token.HashToken, telegram, Fecha.AddMinutes(-1)));

            Assert.AreEqual(0, await ContarAsync(
                $"SELECT COUNT(*) FROM usuario WHERE correo_electronico IN ('{vencido.CorreoElectronico}', '{ajeno.CorreoElectronico}')"));
            Assert.IsNull((await repositorio.ObtenerTokenAsync(token.HashToken))?.UsoUtc);
        }

        [TestMethod]
        public async Task Registro_DosEnviosSimultaneosDelMismoToken_SoloUnoCreaCuenta()
        {
            var repositorio = new RepositorioVinculosTelegramOracle(Fabrica);
            long telegram = NuevoTelegram();
            var token = new TokenVinculacion(NuevoHash(), telegram, telegram, Fecha);
            await repositorio.AgregarTokenAsync(token);
            DateTime ahora = Fecha.AddMinutes(1);
            Usuario[] candidatos = new Usuario[] { NuevoCiudadano(ahora), NuevoCiudadano(ahora) };

            Task<int>[] intentos = candidatos
                .Select(c => Task.Run(() => repositorio.RegistrarCiudadanoYVincularAsync(c, token.HashToken, telegram, ahora)))
                .ToArray();
            try
            {
                await Task.WhenAll(intentos);
            }
            catch (CambioConcurrenteException)
            {
                // Se esperaba que uno de los dos fallara; abajo se comprueba cuál.
            }

            Task<int>[] exitosos = intentos.Where(t => t.Status == TaskStatus.RanToCompletion).ToArray();
            foreach (Task<int> exito in exitosos)
            {
                RegistrarUsuario(exito.Result);
            }

            Assert.HasCount(1, exitosos);
            Assert.IsInstanceOfType<CambioConcurrenteException>(intentos.Single(t => t.IsFaulted).Exception?.InnerException);
            Assert.AreEqual(1, await ContarAsync(
                $"SELECT COUNT(*) FROM usuario WHERE correo_electronico IN ('{candidatos[0].CorreoElectronico}', '{candidatos[1].CorreoElectronico}')"));
            Assert.AreEqual(1, await ContarAsync($"SELECT COUNT(*) FROM vinculo_telegram WHERE telegram_user_id = {telegram}"));
        }

        [TestMethod]
        public async Task Registro_CuentaDeTelegramYaVinculada_DeshaceTodoYDejaElTokenSinUsar()
        {
            var repositorio = new RepositorioVinculosTelegramOracle(Fabrica);
            long telegram = NuevoTelegram();
            var primero = new TokenVinculacion(NuevoHash(), telegram, telegram, Fecha);
            var segundo = new TokenVinculacion(NuevoHash(), telegram, telegram, Fecha.AddMinutes(1));
            await repositorio.AgregarTokenAsync(primero);
            await repositorio.AgregarTokenAsync(segundo);
            RegistrarUsuario(await repositorio.RegistrarCiudadanoYVincularAsync(NuevoCiudadano(Fecha), primero.HashToken, telegram, Fecha.AddMinutes(1)));
            Usuario otro = NuevoCiudadano(Fecha);

            var error = await Assert.ThrowsExactlyAsync<ConflictoUnicidadException>(() =>
                repositorio.RegistrarCiudadanoYVincularAsync(otro, segundo.HashToken, telegram, Fecha.AddMinutes(2)));

            Assert.AreEqual("uq_vinculo_telegram_activo", error.Message);
            Assert.AreEqual(0, await ContarAsync($"SELECT COUNT(*) FROM usuario WHERE correo_electronico = '{otro.CorreoElectronico}'"));
            Assert.IsNull((await repositorio.ObtenerTokenAsync(segundo.HashToken))?.UsoUtc);
        }

        [TestMethod]
        public async Task Baja_ConservaElVinculoInactivoYPermiteVolverARegistrarse()
        {
            var repositorio = new RepositorioVinculosTelegramOracle(Fabrica);
            long telegram = NuevoTelegram();
            var primero = new TokenVinculacion(NuevoHash(), telegram, telegram, Fecha);
            await repositorio.AgregarTokenAsync(primero);
            int id = RegistrarUsuario(await repositorio.RegistrarCiudadanoYVincularAsync(NuevoCiudadano(Fecha), primero.HashToken, telegram, Fecha.AddMinutes(1)));

            await repositorio.DarDeBajaAsync(id, Fecha.AddMinutes(3));

            Assert.IsNull(await repositorio.ObtenerVinculoActivoPorTelegramAsync(telegram));
            Assert.IsNull(await repositorio.ObtenerVinculoActivoPorUsuarioAsync(id));
            Assert.AreEqual(1, await ContarAsync($"SELECT COUNT(*) FROM vinculo_telegram WHERE id_usuario = {id} AND fecha_baja_utc IS NOT NULL"));

            var segundo = new TokenVinculacion(NuevoHash(), telegram, telegram, Fecha.AddMinutes(4));
            await repositorio.AgregarTokenAsync(segundo);
            int nuevo = RegistrarUsuario(await repositorio.RegistrarCiudadanoYVincularAsync(NuevoCiudadano(Fecha), segundo.HashToken, telegram, Fecha.AddMinutes(5)));
            Assert.AreEqual(nuevo, (await repositorio.ObtenerVinculoActivoPorTelegramAsync(telegram))?.IdUsuario);
        }

        [TestMethod]
        public async Task Conversacion_VersionOptimista_RechazaEscriturasViejasYDuplicadas()
        {
            var repositorio = new RepositorioConversacionesTelegramOracle(Fabrica);
            long telegram = NuevoTelegram();

            Assert.IsNull(await repositorio.ObtenerAsync(telegram));
            await repositorio.GuardarAsync(new ConversacionTelegram(telegram, telegram, Fecha));
            ConversacionTelegram? leida = await repositorio.ObtenerAsync(telegram);
            Assert.IsNotNull(leida);
            Assert.AreEqual(1, leida.Version);
            Assert.AreEqual(EstadoConversacion.SinCaneca, leida.Estado);

            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() =>
                repositorio.GuardarAsync(new ConversacionTelegram(telegram, telegram, Fecha)));

            await repositorio.GuardarAsync(leida);
            Assert.AreEqual(2, (await repositorio.ObtenerAsync(telegram))?.Version);
            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => repositorio.GuardarAsync(leida));
        }

        [TestMethod]
        public async Task Conversacion_CanecaIdentificada_ExigeUnContenedorQueExista()
        {
            var repositorio = new RepositorioConversacionesTelegramOracle(Fabrica);
            long telegram = NuevoTelegram();
            await repositorio.GuardarAsync(new ConversacionTelegram(telegram, telegram, Fecha));
            int caneca = await CrearContenedorAsync();

            var inexistente = new ConversacionTelegram(telegram, telegram, EstadoConversacion.CanecaIdentificada, int.MaxValue, null, null, Fecha, 1);
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => repositorio.GuardarAsync(inexistente));

            await repositorio.GuardarAsync(new ConversacionTelegram(telegram, telegram, EstadoConversacion.CanecaIdentificada, caneca, null, null, Fecha.AddMinutes(1), 1));
            ConversacionTelegram? leida = await repositorio.ObtenerAsync(telegram);
            Assert.AreEqual(EstadoConversacion.CanecaIdentificada, leida?.Estado);
            Assert.AreEqual(caneca, leida?.IdContenedor);
            Assert.AreEqual(Fecha.AddMinutes(1), leida?.FechaActualizacionUtc);
        }

        [TestMethod]
        public async Task Update_SoloSeRegistraLaPrimeraVez()
        {
            var repositorio = new RepositorioConversacionesTelegramOracle(Fabrica);
            long update = 9_000_000_000L + Compat.RandomShared.NextInt64(1_000_000_000L);
            _updates.Add(update);

            Assert.IsTrue(await repositorio.RegistrarUpdateAsync(update, Fecha));
            Assert.IsFalse(await repositorio.RegistrarUpdateAsync(update, Fecha.AddSeconds(5)));
        }

        private long NuevoTelegram()
        {
            long id = 9_000_000_000L + Compat.RandomShared.NextInt64(1_000_000_000L);
            _telegrams.Add(id);
            return id;
        }

        private static async Task BorrarAsync(OracleConnection conexion, string sql, List<long> ids, List<Exception> errores)
        {
            foreach (long id in ids)
            {
                try
                {
                    using OracleCommand comando = ComandosOracle.Crear(conexion, sql);
                    comando.Agregar("id", OracleDbType.Int64, id);
                    await comando.ExecuteNonQueryAsync();
                }
                catch (OracleException error)
                {
                    errores.Add(error);
                }
            }
        }

        private static string NuevoHash() => Compat.ToHexStringLower(Compat.RandomGetBytes(32));

        private static Usuario NuevoCiudadano(DateTime fecha)
        {
            var usuario = new Usuario("Ciudadano Telegram IT", $"it.tg{Sufijo().ToLowerInvariant()}@ejemplo.com", CodigoRol.Ciudadano, fecha);
            usuario.RegistrarConsentimiento("v1", fecha);
            return usuario;
        }

        private async Task<int> CrearContenedorAsync()
        {
            int punto = RegistrarPunto(await new RepositorioPuntosInstalacionOracle(Fabrica).AgregarAsync(new PuntoInstalacion("Punto TG " + Sufijo(), null)));
            return RegistrarContenedor(await new RepositorioContenedoresOracle(Fabrica).AgregarAsync(
                new Contenedor(punto, "Caneca TG " + Sufijo(), new GeneradorIdentificadorQrAleatorio().Generar(), Fecha)));
        }
    }
}
