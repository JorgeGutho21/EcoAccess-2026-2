using System;
using System.Collections.Generic;
using System.IO;
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

    // Recorrido integral QR → conversación → orden en Oracle → bridge → caneca simulada → sensor → depósito → ecopuntos.
    // Bridge y caneca son procesos separados con HTTP local firmado (HMAC). Telegram es el transporte simulado: los
    // mensajes entran como MensajeEntrante a ServicioConversacionTelegram, el mismo servicio que usa el bot real, sin
    // despacho en proceso; la orden solo llega a la caneca a través del bridge. No es Telegram real ni hardware.
    public sealed partial class BridgeOracleTests
    {
        private long _telegram;
        private readonly List<long> _updates = new List<long>();

        private ServicioConversacionTelegram Conversacion(Caso e)
        {
            var usuarios = new RepositorioUsuariosOracle(Fabrica); var materiales = new RepositorioMaterialesOracle(Fabrica);
            var canecas = new RepositorioContenedoresOracle(Fabrica);
            return new ServicioConversacionTelegram(new RepositorioConversacionesTelegramOracle(Fabrica), canecas, materiales,
                new ServicioVinculacionTelegram(usuarios, new RepositorioVinculosTelegramOracle(Fabrica), new GeneradorTokensAleatorio(), TimeProvider.System),
                e.Servicio, new LimitadorUsoEnMemoria(100, TimeSpan.FromMinutes(1)), null, TimeProvider.System,
                ecopuntos: new ServicioEcopuntos(usuarios, e.Repo, materiales, new RepositorioEcopuntosOracle(Fabrica), TimeProvider.System),
                estadisticas: new ServicioEstadisticas(usuarios, new RepositorioEstadisticasOracle(Fabrica)));
        }

        private async Task VincularTelegramAsync(Caso e)
        {
            _telegram = 7_100_000_000L + Compat.RandomGetInt32(1, int.MaxValue);
            using var c = await Fabrica.AbrirAsync(CancellationToken.None);
            using var cmd = ComandosOracle.Crear(c, "INSERT INTO vinculo_telegram (id_usuario, telegram_user_id, fecha_vinculo_utc) VALUES (:u, :t, :f)");
            cmd.Agregar("u", OracleDbType.Int32, e.Ciudadano); cmd.Agregar("t", OracleDbType.Int64, _telegram);
            cmd.Agregar("f", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(DateTime.UtcNow));
            await cmd.ExecuteNonQueryAsync();
        }

        private long NuevoUpdate() { long id = 9_000_000_000L + Compat.RandomGetInt32(1, int.MaxValue); lock (_updates) _updates.Add(id); return id; }

        private Task<IReadOnlyList<RespuestaBot>> EnviarAsync(ServicioConversacionTelegram bot, string? texto = null, string? boton = null, long? update = null) =>
            bot.ProcesarAsync(new MensajeEntrante(update ?? NuevoUpdate(), _telegram, _telegram, true, texto, boton));

        private async Task<int[]> OrdenesDeAsync(int contenedor)
        {
            using var c = await Fabrica.AbrirAsync(CancellationToken.None);
            using var cmd = ComandosOracle.Crear(c, "SELECT id_orden FROM orden_apertura WHERE id_contenedor=:c ORDER BY id_orden");
            cmd.Agregar("c", OracleDbType.Int32, contenedor);
            var ids = new List<int>(); using var r = await cmd.ExecuteReaderAsync(); while (await r.ReadAsync()) ids.Add(r.GetInt32(0));
            return ids.ToArray();
        }

        private async Task<int> ContarDeAsync(string sql, int id)
        {
            using var c = await Fabrica.AbrirAsync(CancellationToken.None);
            using var cmd = ComandosOracle.Crear(c, sql); cmd.Agregar("id", OracleDbType.Int32, id);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private async Task LimpiarTelegramAsync(OracleConnection c)
        {
            if (_telegram > 0)
                foreach (string sql in new[] { "DELETE FROM conversacion_telegram WHERE telegram_user_id=:id", "DELETE FROM vinculo_telegram WHERE telegram_user_id=:id" })
                { using var cmd = ComandosOracle.Crear(c, sql); cmd.Agregar("id", OracleDbType.Int64, _telegram); await cmd.ExecuteNonQueryAsync(); }
            if (_segundoTelegram > 0)
                foreach (string sql in new[] { "DELETE FROM conversacion_telegram WHERE telegram_user_id=:id", "DELETE FROM vinculo_telegram WHERE telegram_user_id=:id" })
                { using var cmd = ComandosOracle.Crear(c, sql); cmd.Agregar("id", OracleDbType.Int64, _segundoTelegram); await cmd.ExecuteNonQueryAsync(); }
            foreach (long update in _updates)
            { using var cmd = ComandosOracle.Crear(c, "DELETE FROM update_telegram WHERE update_id=:id"); cmd.Agregar("id", OracleDbType.Int64, update); await cmd.ExecuteNonQueryAsync(); }
        }

        private void RegistrarEvidencia(string escenario, string texto)
        {
            string archivo = Path.Combine(DirectorioGitLocal(), "evidencia-diseno-20261026", "recorrido-integral-" + escenario + ".txt");
            Directory.CreateDirectory(Path.GetDirectoryName(archivo)!);
            File.WriteAllText(archivo, $"{DateTime.UtcNow:O} PDB={PdbEsperado} conservado={ConservarDemo}\n{texto}\n");
        }

        [TestMethod]
        public async Task RecorridoA_MaterialAdmitido_QrConversacionBridgeCanecaDepositoVerificacionYEcopuntos()
        {
            var e = await PrepararAsync(); await VincularTelegramAsync(e); var bot = Conversacion(e);
            string qr = (await new RepositorioContenedoresOracle(Fabrica).ObtenerPorIdAsync(e.Config.IdContenedor))!.IdentificadorQr;
            Hijo bridge = Iniciar(e, "--bridge"), sim = Iniciar(e, "--dispositivo-simulado");
            await ListoAsync(bridge, e.Config.Eventos); await ListoAsync(sim, e.Config.Comandos);

            var inicio = await EnviarAsync(bot, "/start " + qr);
            string botonMaterial = ServicioConversacionTelegram.PrefijoMaterial + e.Material;
            if (!inicio.SelectMany(r => r.Botones).Any(b => b.Dato == botonMaterial)) inicio = await EnviarAsync(bot, "/manual");
            Assert.IsTrue(inicio.SelectMany(r => r.Botones).Any(b => b.Dato == botonMaterial), "La caneca escaneada ofrece su material admitido.");
            var eleccion = await EnviarAsync(bot, boton: botonMaterial);
            Assert.IsTrue(eleccion.SelectMany(r => r.Botones).Any(b => b.Dato == ServicioConversacionTelegram.ConfirmarSi), "Pide confirmación antes de abrir.");
            Assert.IsEmpty(await OrdenesDeAsync(e.Config.IdContenedor), "Elegir material no abre la caneca.");

            // Mensajes repetidos: el mismo update dos veces y dos confirmaciones simultáneas emiten una sola orden.
            long repetido = NuevoUpdate();
            await Task.WhenAll(EnviarAsync(bot, boton: ServicioConversacionTelegram.ConfirmarSi, update: repetido),
                EnviarAsync(bot, boton: ServicioConversacionTelegram.ConfirmarSi));
            await EnviarAsync(bot, boton: ServicioConversacionTelegram.ConfirmarSi, update: repetido);
            int orden = (await OrdenesDeAsync(e.Config.IdContenedor)).Single();

            await EsperarAsync(async () => (await e.Repo.ObtenerOrdenAsync(orden))?.Estado == EstadoOrdenApertura.Cerrada, bridge, sim);
            var deposito = (await e.Repo.ObtenerDepositoPorOrdenAsync(orden))!; _deposito = deposito.Id;
            Assert.IsFalse(deposito.EstaVerificado, "El sensor registra el ingreso, no verifica el material.");
            Assert.AreEqual(4, await ContarDeAsync("SELECT COUNT(DISTINCT tipo) FROM evento_dispositivo WHERE id_orden=:id", orden),
                "Acuse, apertura, detección y cierre persistidos.");
            StringAssert.Contains(string.Join("\n", (await EnviarAsync(bot, "/estado")).Select(r => r.Texto)), "pendiente de verificación");

            var puntos = new ServicioEcopuntos(new RepositorioUsuariosOracle(Fabrica), e.Repo, new RepositorioMaterialesOracle(Fabrica),
                new RepositorioEcopuntosOracle(Fabrica), TimeProvider.System);
            var regla = await puntos.RegistrarReglaAsync(e.Admin, e.Material, 10, "Tarifa ficticia de demostración integral", DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddHours(1));
            Assert.IsTrue(regla.Exitoso, regla.Mensaje); _regla = regla.Valor;
            Assert.IsFalse((await puntos.AcreditarAsync(e.Operario, _deposito, _regla, "demo-" + _marca)).Exitoso, "Sin verificación no hay ecopuntos.");
            // Nueva composición, como el escritorio: no comparte la conversación ni las entidades del bot.
            var escritorio = new ServicioInteracciones(new RepositorioUsuariosOracle(Fabrica), new RepositorioInteraccionesOracle(Fabrica),
                new RepositorioContenedoresOracle(Fabrica), new RepositorioMaterialesOracle(Fabrica), TimeProvider.System);
            Assert.IsTrue((await escritorio.ListarMisDepositosAsync(e.Ciudadano)).Valor.Any(d => d.Id == _deposito));
            Assert.IsTrue((await escritorio.VerificarDepositoAsync(e.Operario, _deposito, e.Material)).Exitoso);
            Assert.IsTrue((await puntos.AcreditarAsync(e.Operario, _deposito, _regla, "demo-" + _marca)).Exitoso);
            Assert.IsTrue((await puntos.AcreditarAsync(e.Operario, _deposito, _regla, "demo-" + _marca)).Exitoso, "El reintento devuelve el mismo crédito.");
            Assert.AreEqual(10L, (await new RepositorioEcopuntosOracle(Fabrica).ConsultarAsync(e.Ciudadano)).Saldo);
            StringAssert.Contains(string.Join("\n", (await EnviarAsync(bot, "/ecopuntos")).Select(r => r.Texto)), "10");

            // Otra instancia recupera identidad y estadísticas desde Oracle, sin recordar sesión en memoria.
            var reiniciado = Conversacion(e);
            var resumen = (await EnviarAsync(reiniciado, "/estadisticas")).Single().Estadisticas;
            Assert.IsNotNull(resumen);
            Assert.AreEqual(e.Ciudadano, resumen.Periodo.IdUsuario);
            Assert.AreEqual(1, resumen.Actual.Depositos.Total);
            Assert.AreEqual(1, resumen.Actual.Depositos.Verificados);
            Assert.IsTrue((await escritorio.ListarMisDepositosAsync(e.Ciudadano)).Valor.Single(d => d.Id == _deposito).EstaVerificado);
            var estadisticasEscritorio = new ServicioEstadisticas(new RepositorioUsuariosOracle(Fabrica), new RepositorioEstadisticasOracle(Fabrica));
            var paginaPropia = await estadisticasEscritorio.ListarMiosAsync(e.Ciudadano, resumen!.Periodo.DesdeUtc, resumen.Periodo.HastaUtc, 1, 10);
            Assert.IsTrue(paginaPropia.Exitoso);
            Assert.IsTrue(paginaPropia.Valor.Filas.Any(d => d.IdDeposito == _deposito));
            Assert.IsTrue(paginaPropia.Valor.Filas.All(d => d.IdUsuario == e.Ciudadano));

            RegistrarEvidencia("a", $"ciudadano={e.Ciudadano} caneca={e.Config.IdContenedor} material={e.Material} orden={orden} deposito={_deposito} regla={_regla} saldo=10");
        }

        [TestMethod]
        public async Task RecorridoB_MaterialNoAdmitido_SeRechazaSinOrdenDepositoNiEcopuntos()
        {
            var e = await PrepararAsync(); await VincularTelegramAsync(e); var bot = Conversacion(e);
            int ajeno = RegistrarMaterial(await new RepositorioMaterialesOracle(Fabrica).AgregarAsync(
                new Material("BN_" + _marca[..12].ToUpperInvariant(), "Material ficticio no admitido", CategoriaMaterial.NoAprovechable, null)));
            string qr = (await new RepositorioContenedoresOracle(Fabrica).ObtenerPorIdAsync(e.Config.IdContenedor))!.IdentificadorQr;
            Hijo bridge = Iniciar(e, "--bridge"), sim = Iniciar(e, "--dispositivo-simulado");
            await ListoAsync(bridge, e.Config.Eventos); await ListoAsync(sim, e.Config.Comandos);

            var inicio = await EnviarAsync(bot, "/start " + qr);
            Assert.IsFalse(inicio.SelectMany(r => r.Botones).Any(b => b.Dato == ServicioConversacionTelegram.PrefijoMaterial + ajeno), "No se ofrece.");
            // Botón fabricado con un material que esta caneca no admite.
            var rechazo = await EnviarAsync(bot, boton: ServicioConversacionTelegram.PrefijoMaterial + ajeno);
            Assert.IsFalse(rechazo.SelectMany(r => r.Botones).Any(b => b.Dato == ServicioConversacionTelegram.ConfirmarSi), "No pide confirmar lo inadmisible.");
            Assert.IsTrue(rechazo.Any(r => r.Texto.Length > 0), "El bot informa al ciudadano.");
            await EnviarAsync(bot, boton: ServicioConversacionTelegram.ConfirmarSi);
            await Task.Delay(1500);

            Assert.IsEmpty(await OrdenesDeAsync(e.Config.IdContenedor), "Sin orden de apertura.");
            Assert.AreEqual(0, await ContarDeAsync("SELECT COUNT(*) FROM evento_dispositivo WHERE id_contenedor=:id", e.Config.IdContenedor));
            Assert.AreEqual(0L, (await new RepositorioEcopuntosOracle(Fabrica).ConsultarAsync(e.Ciudadano)).Saldo);
            RegistrarEvidencia("b", $"ciudadano={e.Ciudadano} caneca={e.Config.IdContenedor} material_no_admitido={ajeno} ordenes=0 eventos=0 saldo=0 respuesta={rechazo.FirstOrDefault()?.Texto}");
        }

        [TestMethod]
        public async Task RecorridoD_EventosDuplicadosYReinicioDeProcesos_UnaOrdenUnDepositoYEstadosConsistentes()
        {
            var e = await PrepararAsync(); await VincularTelegramAsync(e); var bot = Conversacion(e);
            string qr = (await new RepositorioContenedoresOracle(Fabrica).ObtenerPorIdAsync(e.Config.IdContenedor))!.IdentificadorQr;
            Hijo bridge = Iniciar(e, "--bridge"), sim = Iniciar(e, "--dispositivo-simulado", escenario: EscenarioCaneca.Duplicados);
            await ListoAsync(bridge, e.Config.Eventos); await ListoAsync(sim, e.Config.Comandos);

            string botonMaterial = ServicioConversacionTelegram.PrefijoMaterial + e.Material;
            if (!(await EnviarAsync(bot, "/start " + qr)).SelectMany(r => r.Botones).Any(b => b.Dato == botonMaterial)) await EnviarAsync(bot, "/manual");
            await EnviarAsync(bot, boton: botonMaterial);
            // Tres confirmaciones simultáneas con updates distintos: una sola orden.
            await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => EnviarAsync(bot, boton: ServicioConversacionTelegram.ConfirmarSi)));
            int orden = (await OrdenesDeAsync(e.Config.IdContenedor)).Single();
            await EsperarAsync(async () => (await e.Repo.ObtenerOrdenAsync(orden))?.Estado == EstadoOrdenApertura.Cerrada, bridge, sim);
            int deposito = (await e.Repo.ObtenerDepositoPorOrdenAsync(orden))!.Id; _deposito = deposito;

            // Reinicio abrupto de ambos procesos con el mismo diario: nada se reenvía ni se duplica.
            await CerrarAsync(sim); await CerrarAsync(bridge);
            bridge = Iniciar(e, "--bridge"); sim = Iniciar(e, "--dispositivo-simulado", escenario: EscenarioCaneca.Duplicados);
            await ListoAsync(bridge, e.Config.Eventos); await ListoAsync(sim, e.Config.Comandos);
            await Task.Delay(2000);
            await EnviarAsync(bot, boton: ServicioConversacionTelegram.ConfirmarSi);

            Assert.HasCount(1, await OrdenesDeAsync(e.Config.IdContenedor), "Ni el reinicio ni otra confirmación crean otra orden.");
            Assert.AreEqual(4, await ContarDeAsync("SELECT COUNT(*) FROM evento_dispositivo WHERE id_orden=:id", orden),
                "Cada evento duplicado se guardó una sola vez.");
            Assert.AreEqual(1, await CantidadDepositosAsync(orden));
            Assert.AreEqual(deposito, (await e.Repo.ObtenerDepositoPorOrdenAsync(orden))!.Id);
            Assert.AreEqual(EstadoOrdenApertura.Cerrada, (await e.Repo.ObtenerOrdenAsync(orden))!.Estado);
            Assert.AreEqual(0L, (await new RepositorioEcopuntosOracle(Fabrica).ConsultarAsync(e.Ciudadano)).Saldo, "Sin verificación no hay ecopuntos.");
            RegistrarEvidencia("d", $"ciudadano={e.Ciudadano} caneca={e.Config.IdContenedor} orden={orden} eventos=4 deposito={deposito} estado=Cerrada saldo=0 reinicios=1");
        }

        [TestMethod]
        public async Task RecorridoC_AperturaSinDeteccion_CierraSinDepositoNiEcopuntos()
        {
            var e = await PrepararAsync(); await VincularTelegramAsync(e); var bot = Conversacion(e);
            string qr = (await new RepositorioContenedoresOracle(Fabrica).ObtenerPorIdAsync(e.Config.IdContenedor))!.IdentificadorQr;
            Hijo bridge = Iniciar(e, "--bridge"), sim = Iniciar(e, "--dispositivo-simulado", escenario: EscenarioCaneca.AperturaSinDeteccion);
            await ListoAsync(bridge, e.Config.Eventos); await ListoAsync(sim, e.Config.Comandos);

            string botonMaterial = ServicioConversacionTelegram.PrefijoMaterial + e.Material;
            if (!(await EnviarAsync(bot, "/start " + qr)).SelectMany(r => r.Botones).Any(b => b.Dato == botonMaterial)) await EnviarAsync(bot, "/manual");
            await EnviarAsync(bot, boton: botonMaterial);
            await EnviarAsync(bot, boton: ServicioConversacionTelegram.ConfirmarSi);
            int orden = (await OrdenesDeAsync(e.Config.IdContenedor)).Single();

            await EsperarAsync(async () => (await e.Repo.ObtenerOrdenAsync(orden))?.Estado is EstadoOrdenApertura.Cerrada or EstadoOrdenApertura.Fallida, bridge, sim);
            Assert.IsNull(await e.Repo.ObtenerDepositoPorOrdenAsync(orden), "Abrir y cerrar sin detección no es un depósito.");
            Assert.AreEqual(0L, (await new RepositorioEcopuntosOracle(Fabrica).ConsultarAsync(e.Ciudadano)).Saldo);
            StringAssert.Contains(string.Join("\n", (await EnviarAsync(bot, "/estado")).Select(r => r.Texto)), "Sin depósito registrado");
            var estado = (await e.Repo.ObtenerOrdenAsync(orden))!.Estado;
            RegistrarEvidencia("c", $"ciudadano={e.Ciudadano} caneca={e.Config.IdContenedor} orden={orden} estado={estado} depositos=0 saldo=0");
        }
    }
}
