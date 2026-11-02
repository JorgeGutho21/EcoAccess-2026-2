using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    [TestClass, TestCategory(Categoria), DoNotParallelize]
    public sealed partial class EcopuntosOracleTests : BaseIntegracionOracle
    {
        private readonly List<int> _depositos = new List<int>();
        private readonly List<int> _reglas = new List<int>();
        private static readonly DateTime Inicio = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
        private sealed class Reloj : TimeProvider
        {
            public DateTime Ahora = Inicio;
            public override DateTimeOffset GetUtcNow() => new(Ahora);
        }
        private sealed record Caso(ServicioEcopuntos Servicio, RepositorioEcopuntosOracle Repo, ServicioInteracciones Interacciones,
            RepositorioInteraccionesOracle RepoInteracciones, int Deposito, int Ciudadano, int Administrador,
            int Operario, int Material, int Regla, Reloj Reloj);

        private async Task<Caso> PrepararAsync(bool verificar = true, int? cantidadDeclarada = null, int? cantidadVerificada = null)
        {
            string sufijo = Sufijo();
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            var materiales = new RepositorioMaterialesOracle(Fabrica);
            var canecas = new RepositorioContenedoresOracle(Fabrica);
            var reloj = new Reloj();
            int material = RegistrarMaterial(await materiales.AgregarAsync(new Material("EP" + sufijo, "Material ficticio EP", CategoriaMaterial.Aprovechable, null)));
            int punto = RegistrarPunto(await new RepositorioPuntosInstalacionOracle(Fabrica).AgregarAsync(new PuntoInstalacion("Punto EP " + sufijo, null)));
            var caneca = new Contenedor(punto, "Caneca EP " + sufijo, new GeneradorIdentificadorQrAleatorio().Generar(), Inicio.AddDays(-1));
            caneca.AdmitirMaterial(material);
            int contenedor = RegistrarContenedor(await canecas.AgregarAsync(caneca));
            await canecas.CambiarEstadoAsync(contenedor, EstadoContenedor.EnServicio);
            var ciudadano = new Usuario("Ciudadano EP", "ep.c" + sufijo.ToLowerInvariant() + "@example.invalid", CodigoRol.Ciudadano, Inicio.AddDays(-1));
            ciudadano.RegistrarConsentimiento("prueba-ep", Inicio.AddDays(-1));
            int titular = RegistrarUsuario(await usuarios.AgregarAsync(ciudadano));
            int admin = RegistrarUsuario(await usuarios.AgregarAsync(new Usuario("Administrador EP", "ep.a" + sufijo.ToLowerInvariant() + "@example.invalid", CodigoRol.Administrador, Inicio.AddDays(-1))));
            int operario = RegistrarUsuario(await usuarios.AgregarAsync(new Usuario("Operario EP", "ep.o" + sufijo.ToLowerInvariant() + "@example.invalid", CodigoRol.Operario, Inicio.AddDays(-1))));
            var ri = new RepositorioInteraccionesOracle(Fabrica, reloj);
            var interacciones = new ServicioInteracciones(usuarios, ri, canecas, materiales, reloj);
            var inicio = cantidadDeclarada is { } declarada
                ? await interacciones.IniciarConCantidadAsync(titular, contenedor, CanalInteraccion.Telegram, declarada)
                : await interacciones.IniciarAsync(titular, contenedor, CanalInteraccion.Telegram);
            Assert.IsTrue(inicio.Exitoso, inicio.Mensaje);
            var orden = await interacciones.AutorizarAperturaAsync(titular, inicio.Valor, material, "ep-" + sufijo);
            Assert.IsTrue(orden.Exitoso, orden.Mensaje);
            var simulada = new CanecaSimulada(contenedor, interacciones.RegistrarEventoAsync, reloj);
            await new ServicioDespachoOrdenes(ri, interacciones, simulada, reloj).DespacharAsync(orden.Valor);
            await simulada.EjecutarAsync(orden.Valor);
            int deposito = (await ri.ObtenerDepositoPorOrdenAsync(orden.Valor))!.Id;
            _depositos.Add(deposito);
            if (verificar) Assert.IsTrue((await interacciones.VerificarDepositoAsync(operario, deposito, material, cantidadVerificada: cantidadVerificada)).Exitoso);
            var repo = new RepositorioEcopuntosOracle(Fabrica);
            var servicio = new ServicioEcopuntos(usuarios, ri, materiales, repo, reloj);
            var regla = await servicio.RegistrarReglaAsync(admin, material, 10, "Tarifa ficticia de prueba", Inicio, Inicio.AddDays(1));
            Assert.IsTrue(regla.Exitoso, regla.Mensaje);
            _reglas.Add(regla.Valor);
            return new Caso(servicio, repo, interacciones, ri, deposito, titular, admin, operario, material, regla.Valor, reloj);
        }

        [TestCleanup]
        public async Task LimpiarLibroDePruebaAsync()
        {
            if (Fabrica is null) return;
            using var c = await Fabrica.AbrirAsync(CancellationToken.None);
            foreach (int deposito in _depositos)
            {
                var ids = new List<int>();
                using (var cmd = ComandosOracle.Crear(c, "SELECT id_operacion FROM operacion_ecopuntos WHERE id_deposito=:id ORDER BY id_operacion DESC"))
                {
                    cmd.Agregar("id", OracleDbType.Int32, deposito);
                    using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync()) ids.Add(r.GetInt32(0));
                }
                foreach (int id in ids)
                {
                    await EjecutarAsync(c, "DELETE FROM movimiento_ecopuntos WHERE id_operacion=:id", id);
                    await EjecutarAsync(c, "DELETE FROM operacion_ecopuntos WHERE id_operacion=:id", id);
                }
            }
            foreach (int regla in _reglas) await EjecutarAsync(c, "DELETE FROM regla_ecopuntos WHERE id_regla=:id", regla);
        }
        private static async Task EjecutarAsync(OracleConnection c, string sql, int id)
        {
            using var cmd = ComandosOracle.Crear(c, sql); cmd.Agregar("id", OracleDbType.Int32, id); await cmd.ExecuteNonQueryAsync();
        }
        private static MovimientoEcopuntos Credito(Resultado<IReadOnlyList<MovimientoEcopuntos>> r)
        {
            Assert.IsTrue(r.Exitoso, r.Mensaje);
            return r.Valor.Single(m => m.Tipo == TipoMovimientoEcopuntos.Credito);
        }

        [TestMethod]
        public async Task CreditoConservaEvidenciaYReintentoInclusoDespuesDeVencerLaRegla()
        {
            var e = await PrepararAsync();
            var primero = Credito(await e.Servicio.AcreditarAsync(e.Operario, e.Deposito, e.Regla, "ep-" + Sufijo()));
            string clave = "ep-reintento-" + Sufijo();
            var rechazo = await e.Servicio.AcreditarAsync(e.Operario, e.Deposito, e.Regla, clave);
            Assert.IsFalse(rechazo.Exitoso);
            var operacion = await e.Repo.ObtenerOperacionAsync((await ClaveDeAsync(primero.IdOperacion)));
            e.Reloj.Ahora = Inicio.AddDays(2);
            var repetido = Credito(await e.Servicio.AcreditarAsync(e.Operario, e.Deposito, e.Regla, operacion!.Solicitud.Clave));
            Assert.AreEqual(primero.Id, repetido.Id);
            var saldo = await e.Servicio.ConsultarMiosAsync(e.Ciudadano);
            Assert.AreEqual(10L, saldo.Valor.Saldo);
            Assert.HasCount(1, saldo.Valor.Movimientos);
        }
        private async Task<string> ClaveDeAsync(int id)
        {
            using var c = await Fabrica.AbrirAsync(CancellationToken.None);
            using var cmd = ComandosOracle.Crear(c, "SELECT clave FROM operacion_ecopuntos WHERE id_operacion=:id");
            cmd.Agregar("id", OracleDbType.Int32, id); return (string)(await cmd.ExecuteScalarAsync())!;
        }

        [TestMethod]
        public async Task DeteccionSinVerificacionNoRecibePuntos()
        {
            var e = await PrepararAsync(verificar: false);
            Assert.IsFalse((await e.Servicio.AcreditarAsync(e.Operario, e.Deposito, e.Regla, "ep-" + Sufijo())).Exitoso);
            Assert.AreEqual(0L, (await e.Repo.ConsultarAsync(e.Ciudadano)).Saldo);
        }

        [TestMethod]
        public async Task CorregirRevierteYReemplazaElCreditoSinModificarLaHistoria()
        {
            var e = await PrepararAsync();
            var inicial = Credito(await e.Servicio.AcreditarAsync(e.Operario, e.Deposito, e.Regla, "ep-" + Sufijo()));
            var nueva = await e.Servicio.RegistrarReglaAsync(e.Administrador, e.Material, 25, "Nueva versión ficticia", Inicio);
            _reglas.Add(nueva.Valor);
            string clave = "ep-corregir-" + Sufijo();
            var correccion = await e.Servicio.CorregirAsync(e.Administrador, inicial.Id, nueva.Valor, clave, "Corrección documentada de tarifa.");
            var sucesor = Credito(correccion);
            Assert.HasCount(2, correccion.Valor);
            Assert.AreEqual(-10, correccion.Valor.Single(m => m.Tipo == TipoMovimientoEcopuntos.Reverso).Puntos);
            Assert.AreEqual(inicial.Id, sucesor.IdPredecesor);
            var resumenCorregido = await e.Repo.ConsultarAsync(e.Ciudadano);
            Assert.AreEqual(25L, resumenCorregido.Saldo);
            Assert.AreEqual(35L, resumenCorregido.TotalAcreditado);
            Assert.AreEqual(10L, resumenCorregido.TotalRevertido);
            Assert.AreEqual(inicial, await e.Repo.ObtenerMovimientoAsync(inicial.Id));
            Assert.AreEqual(sucesor.Id, Credito(await e.Servicio.CorregirAsync(e.Administrador, inicial.Id, nueva.Valor, clave, "Corrección documentada de tarifa.")).Id);
            Assert.IsFalse((await e.Servicio.RevertirAsync(e.Administrador, inicial.Id, "otra-" + Sufijo(), "Crédito ya reemplazado.")).Exitoso);
            Assert.IsTrue((await e.Servicio.RevertirAsync(e.Administrador, sucesor.Id, "reverso-" + Sufijo(), "Anulación del crédito activo.")).Exitoso);
            Assert.AreEqual(0L, (await e.Repo.ConsultarAsync(e.Ciudadano)).Saldo);
        }

        [TestMethod]
        public async Task ReversoUnicoNoPuedeVolverAAcreditarseComoInicial()
        {
            var e = await PrepararAsync();
            var inicial = Credito(await e.Servicio.AcreditarAsync(e.Operario, e.Deposito, e.Regla, "ep-" + Sufijo()));
            string clave = "anular-" + Sufijo();
            var reverso = await e.Servicio.RevertirAsync(e.Administrador, inicial.Id, clave, "Depósito anulado en prueba.");
            Assert.IsTrue(reverso.Exitoso, reverso.Mensaje);
            var repetido = await e.Servicio.RevertirAsync(e.Administrador, inicial.Id, clave, "Depósito anulado en prueba.");
            Assert.AreEqual(reverso.Valor.Single().Id, repetido.Valor.Single().Id);
            Assert.IsFalse((await e.Servicio.RevertirAsync(e.Administrador, inicial.Id, "otro-" + Sufijo(), "Segundo reverso.")).Exitoso);
            Assert.IsFalse((await e.Servicio.AcreditarAsync(e.Operario, e.Deposito, e.Regla, "nuevo-" + Sufijo())).Exitoso);
            Assert.AreEqual(0L, (await e.Repo.ConsultarAsync(e.Ciudadano)).Saldo);
        }

        [TestMethod]
        public async Task MismaClaveConOtroContenidoNoReutilizaNiExponeLaOperacion()
        {
            var e = await PrepararAsync(); string clave = "ep-" + Sufijo();
            Credito(await e.Servicio.AcreditarAsync(e.Operario, e.Deposito, e.Regla, clave));
            Assert.AreEqual(MotivoRechazo.Duplicado, (await e.Servicio.AcreditarAsync(e.Administrador, e.Deposito, e.Regla, clave)).Motivo);
            Assert.IsFalse((await e.Servicio.AcreditarAsync(e.Ciudadano, e.Deposito, e.Regla, clave)).Exitoso);
        }

        [TestMethod]
        public async Task ReglaInexistenteFuturaODeOtroMaterialNoAcredita()
        {
            var e = await PrepararAsync();
            Assert.IsFalse((await e.Servicio.AcreditarAsync(e.Operario, e.Deposito, int.MaxValue, "ep-" + Sufijo())).Exitoso);
            var futura = await e.Servicio.RegistrarReglaAsync(e.Administrador, e.Material, 20, "Futura", Inicio.AddDays(1)); _reglas.Add(futura.Valor);
            Assert.IsFalse((await e.Servicio.AcreditarAsync(e.Operario, e.Deposito, futura.Valor, "ep-" + Sufijo())).Exitoso);
            int otro = RegistrarMaterial(await new RepositorioMaterialesOracle(Fabrica).AgregarAsync(
                new Material("EP" + Sufijo(), "Otro material EP", CategoriaMaterial.Aprovechable, null)));
            var ajena = await e.Servicio.RegistrarReglaAsync(e.Administrador, otro, 20, "Otro material", Inicio); _reglas.Add(ajena.Valor);
            Assert.IsFalse((await e.Servicio.AcreditarAsync(e.Operario, e.Deposito, ajena.Valor, "ep-" + Sufijo())).Exitoso);
            Assert.IsFalse((await e.Servicio.RegistrarReglaAsync(e.Operario, e.Material, 20, "Sin permiso", Inicio)).Exitoso);
            Assert.AreEqual(0L, (await e.Repo.ConsultarAsync(e.Ciudadano)).Saldo);
        }

        [TestMethod]
        public async Task CreditosConcurrentesEsperanElDepositoYAsignanUnaSolaVez()
        {
            var e = await PrepararAsync();
            using var otra = await SesionAjenaOracle.AbrirAsync(Fabrica);
            await otra.EjecutarAsync("SELECT id_deposito FROM deposito WHERE id_deposito=:id FOR UPDATE", e.Deposito);
            var a = e.Servicio.AcreditarAsync(e.Operario, e.Deposito, e.Regla, "concurrente-a-" + Sufijo());
            var b = e.Servicio.AcreditarAsync(e.Administrador, e.Deposito, e.Regla, "concurrente-b-" + Sufijo());
            await SesionAjenaOracle.AfirmarQueEsperaAsync(a);
            await SesionAjenaOracle.AfirmarQueEsperaAsync(b);
            await otra.ConfirmarAsync();
            var resultados = await Task.WhenAll(a, b);
            Assert.AreEqual(1, resultados.Count(r => r.Exitoso));
            Assert.AreEqual(10L, (await e.Repo.ConsultarAsync(e.Ciudadano)).Saldo);
        }

        [TestMethod]
        public async Task RevocacionDeActorDuranteLaEsperaImpideElCredito()
        {
            var e = await PrepararAsync();
            using var otra = await SesionAjenaOracle.AbrirAsync(Fabrica);
            await otra.EjecutarAsync("SELECT id_usuario FROM usuario WHERE id_usuario=:id FOR UPDATE", e.Operario);
            var pendiente = e.Servicio.AcreditarAsync(e.Operario, e.Deposito, e.Regla, "ep-" + Sufijo());
            await SesionAjenaOracle.AfirmarQueEsperaAsync(pendiente);
            await otra.EjecutarAsync("UPDATE usuario SET activo=0 WHERE id_usuario=:id", e.Operario);
            await otra.ConfirmarAsync();
            Assert.IsFalse((await pendiente).Exitoso);
            Assert.AreEqual(0L, (await e.Repo.ConsultarAsync(e.Ciudadano)).Saldo);
        }

        [TestMethod]
        public async Task CorreccionYReversoConcurrentesSoloConsumenUnaVezElCreditoActivo()
        {
            var e = await PrepararAsync();
            var inicial = Credito(await e.Servicio.AcreditarAsync(e.Operario, e.Deposito, e.Regla, "ep-" + Sufijo()));
            var nueva = await e.Servicio.RegistrarReglaAsync(e.Administrador, e.Material, 25, "Versión concurrente", Inicio);
            _reglas.Add(nueva.Valor);
            using var otra = await SesionAjenaOracle.AbrirAsync(Fabrica);
            await otra.EjecutarAsync("SELECT id_deposito FROM deposito WHERE id_deposito=:id FOR UPDATE", e.Deposito);
            var a = e.Servicio.CorregirAsync(e.Administrador, inicial.Id, nueva.Valor, "corr-" + Sufijo(), "Corrección concurrente.");
            var b = e.Servicio.RevertirAsync(e.Administrador, inicial.Id, "rev-" + Sufijo(), "Reverso concurrente.");
            await SesionAjenaOracle.AfirmarQueEsperaAsync(a);
            await SesionAjenaOracle.AfirmarQueEsperaAsync(b);
            await otra.ConfirmarAsync();
            var resultados = await Task.WhenAll(a, b);
            Assert.AreEqual(1, resultados.Count(r => r.Exitoso));
            long saldo = (await e.Repo.ConsultarAsync(e.Ciudadano)).Saldo;
            Assert.IsTrue(saldo is 0 or 25);
        }

        [TestMethod]
        public async Task FalloEnCreditoSucesorRevierteLaOperacionCompleta()
        {
            var e = await PrepararAsync();
            var inicial = Credito(await e.Servicio.AcreditarAsync(e.Operario, e.Deposito, e.Regla, "ep-" + Sufijo()));
            string trigger = "EP_FALLO_" + Sufijo();
            using var c = await Fabrica.AbrirAsync(CancellationToken.None);
            // Inyección de fallo solo para este titular ficticio. El trigger se retira en finally.
            using (var cmd = ComandosOracle.Crear(c, $"CREATE TRIGGER {trigger} BEFORE INSERT ON movimiento_ecopuntos FOR EACH ROW " +
                $"BEGIN IF :NEW.id_usuario={e.Ciudadano} AND :NEW.id_predecesor IS NOT NULL THEN " +
                "RAISE_APPLICATION_ERROR(-20991,'Fallo controlado al insertar sucesor'); END IF; END;"))
                await cmd.ExecuteNonQueryAsync();
            string clave = "corr-fallo-" + Sufijo();
            try
            {
                var error = await Assert.ThrowsExactlyAsync<OracleException>(() => e.Servicio.CorregirAsync(e.Administrador, inicial.Id,
                    e.Regla, clave, "Corrección con fallo inducido."));
                Assert.AreEqual(20991, error.Number);
            }
            finally
            {
                using var cmd = ComandosOracle.Crear(c, $"DROP TRIGGER {trigger}"); await cmd.ExecuteNonQueryAsync();
            }
            Assert.IsNull(await e.Repo.ObtenerOperacionAsync(clave));
            var resumen = await e.Repo.ConsultarAsync(e.Ciudadano);
            Assert.AreEqual(10L, resumen.Saldo);
            Assert.HasCount(1, resumen.Movimientos);
            Assert.IsTrue((await e.Servicio.CorregirAsync(e.Administrador, inicial.Id, e.Regla, clave, "Corrección con fallo inducido.")).Exitoso);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(1000001)]
        public async Task RestriccionesOracleImpidenTarifasFueraDelRango(int puntos)
        {
            var e = await PrepararAsync();
            using var c = await Fabrica.AbrirAsync(CancellationToken.None);
            using var cmd = ComandosOracle.Crear(c, "INSERT INTO regla_ecopuntos(id_material,puntos,descripcion,desde_utc,id_creador) " +
                "VALUES(:material,:puntos,'Tarifa inválida de prueba',SYS_EXTRACT_UTC(SYSTIMESTAMP),:actor)");
            cmd.Agregar("material", OracleDbType.Int32, e.Material); cmd.Agregar("puntos", OracleDbType.Int32, puntos); cmd.Agregar("actor", OracleDbType.Int32, e.Administrador);
            var error = await Assert.ThrowsExactlyAsync<OracleException>(() => cmd.ExecuteNonQueryAsync());
            Assert.AreEqual(2290, error.Number);
        }

        [TestMethod]
        public async Task OperarioNoPuedeAcreditarseSuPropioDepositoNiAnularCreditos()
        {
            var e = await PrepararAsync();
            await new RepositorioUsuariosOracle(Fabrica).AgregarRolAsync(e.Ciudadano, CodigoRol.Operario);
            Assert.IsFalse((await e.Servicio.AcreditarAsync(e.Ciudadano, e.Deposito, e.Regla, "propio-" + Sufijo())).Exitoso);
            var credito = Credito(await e.Servicio.AcreditarAsync(e.Operario, e.Deposito, e.Regla, "ep-" + Sufijo()));
            Assert.IsFalse((await e.Servicio.RevertirAsync(e.Operario, credito.Id, "sinpermiso-" + Sufijo(), "Intento sin rol admin")).Exitoso);
            Assert.AreEqual(10L, (await e.Repo.ConsultarAsync(e.Ciudadano)).Saldo);
        }

        [TestMethod]
        public async Task SaldoIncluyeTodoElLibroAunqueElHistorialLimiteCincuentaFilas()
        {
            var e = await PrepararAsync();
            var credito = Credito(await e.Servicio.AcreditarAsync(e.Operario, e.Deposito, e.Regla, "ep-" + Sufijo()));
            for (int i = 0; i < 26; i++)
                credito = Credito(await e.Servicio.CorregirAsync(e.Administrador, credito.Id, e.Regla, "corr-" + Sufijo(), "Ensayo de paginación del libro."));
            var resumen = await e.Repo.ConsultarAsync(e.Ciudadano);
            Assert.HasCount(50, resumen.Movimientos);
            Assert.AreEqual(10L, resumen.Saldo);
            Assert.AreEqual(270L, resumen.TotalAcreditado);
            Assert.AreEqual(260L, resumen.TotalRevertido);
            Assert.AreNotEqual(resumen.Saldo, (long)resumen.Movimientos.Sum(m => m.Puntos));
        }
    }
}
