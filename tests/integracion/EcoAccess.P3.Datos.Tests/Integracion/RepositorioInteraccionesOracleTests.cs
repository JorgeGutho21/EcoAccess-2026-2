using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    [TestClass]
    [TestCategory(Categoria)]
    [DoNotParallelize]
    public sealed class RepositorioInteraccionesOracleTests : BaseIntegracionOracle
    {
        private static readonly DateTime Fecha = new(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc);

        [TestMethod]
        public async Task CantidadDeclarada_CasConcurrenteYEmisionConFotoAnteriorNoDejanOrdenOculta()
        {
            Escenario e = await PrepararAsync();
            int id = await e.Repositorio.AgregarInteraccionAsync(new Interaccion(e.Caneca, e.Ciudadano, CanalInteraccion.Telegram, Fecha));
            var segundo = new RepositorioInteraccionesOracle(Fabrica, new RelojOracleAjustable(Fecha.AddMinutes(1)));
            var resultados = await Task.WhenAll(e.Repositorio.CambiarCantidadDeclaradaAsync(e.Ciudadano, id, null, 3),
                segundo.CambiarCantidadDeclaradaAsync(e.Ciudadano, id, null, 4));
            Assert.AreEqual(1, resultados.Count(x => x));
            var anterior = (await e.Repositorio.ObtenerInteraccionAsync(id))!;
            Assert.IsTrue(anterior.CantidadDeclarada is 3 or 4);
            Assert.IsFalse(await segundo.CambiarCantidadDeclaradaAsync(e.Operario, id, anterior.CantidadDeclarada, 5));
            Assert.IsTrue(await segundo.CambiarCantidadDeclaradaAsync(e.Ciudadano, id, anterior.CantidadDeclarada, 5));
            anterior.Autorizar(e.MaterialA);
            var orden = new OrdenApertura(id, e.Caneca, "declaracion-" + Sufijo(), Fecha.AddMinutes(1), Fecha.AddMinutes(3));
            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => e.Repositorio.EmitirOrdenAsync(anterior, orden));
            Assert.IsNull(await segundo.ObtenerOrdenPorClaveAsync(orden.ClaveIdempotencia));
            var actual = (await segundo.ObtenerInteraccionAsync(id))!;
            Assert.AreEqual(EstadoInteraccion.Iniciada, actual.Estado); Assert.AreEqual(5, actual.CantidadDeclarada);
            actual.Autorizar(e.MaterialA);
            int emitida = await segundo.EmitirOrdenAsync(actual, orden);
            Assert.IsGreaterThan(0, emitida); Assert.IsFalse(await e.Repositorio.CambiarCantidadDeclaradaAsync(e.Ciudadano, id, 5, 6));
        }

        [TestMethod]
        public async Task RepositorioNoEmiteOrdenEspecialAunqueConfiguracionLaIncluya()
        {
            Escenario e = await PrepararAsync();
            int especial = RegistrarMaterial(await new RepositorioMaterialesOracle(Fabrica).AgregarAsync(
                new Material("PEL_" + Sufijo(), "Cortopunzante de ensayo", CategoriaMaterial.Peligroso, "No admitido en el piloto.")));
            var canecas = new RepositorioContenedoresOracle(Fabrica);
            var caneca = (await canecas.ObtenerPorIdAsync(e.Caneca))!;
            caneca.AdmitirMaterial(especial);
            await canecas.AdmitirMaterialAsync(e.Caneca, especial);
            var interaccion = new Interaccion(e.Caneca, e.Ciudadano, CanalInteraccion.Simulador, Fecha);
            int id = await e.Repositorio.AgregarInteraccionAsync(interaccion);
            var autorizada = (await e.Repositorio.ObtenerInteraccionAsync(id))!;
            autorizada.Autorizar(especial);
            var orden = new OrdenApertura(id, e.Caneca, "peligro-" + Sufijo(), Fecha.AddMinutes(1), Fecha.AddMinutes(3));
            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => e.Repositorio.EmitirOrdenAsync(autorizada, orden));
            Assert.AreEqual(EstadoInteraccion.Iniciada, (await e.Repositorio.ObtenerInteraccionAsync(id))!.Estado);
            Assert.IsNull(await e.Repositorio.ObtenerOrdenVigenteAsync(e.Caneca));
        }

        [TestMethod]
        public async Task CantidadDeclaradaPersisteSeparadaDelDepositoYDelDatoHistorico()
        {
            Escenario e = await PrepararAsync();
            int id = await e.Repositorio.AgregarInteraccionAsync(new Interaccion(e.Caneca, e.Ciudadano, CanalInteraccion.Simulador, Fecha, 3));
            int sinDato = await e.Repositorio.AgregarInteraccionAsync(new Interaccion(e.Caneca, e.Ciudadano, CanalInteraccion.Simulador, Fecha));
            Assert.AreEqual(3, (await e.Repositorio.ObtenerInteraccionAsync(id))!.CantidadDeclarada);
            Assert.IsNull((await e.Repositorio.ObtenerInteraccionAsync(sinDato))!.CantidadDeclarada);
            Assert.IsEmpty(await e.Repositorio.ListarDepositosPorUsuarioAsync(e.Ciudadano));
            var solicitud = (await e.Repositorio.ObtenerInteraccionAsync(id))!;
            solicitud.RegistrarSugerencia(e.MaterialA, 0.8m);
            await e.Repositorio.ActualizarInteraccionAsync(solicitud, EstadoInteraccion.Iniciada);
            Assert.AreEqual(3, (await e.Repositorio.ObtenerInteraccionAsync(id))!.CantidadDeclarada);
        }

        [TestMethod]
        public async Task Interaccion_AgregarObtenerYListar_ConservaLosDatos()
        {
            Escenario e = await PrepararAsync();
            var nueva = new Interaccion(e.Caneca, e.Ciudadano, CanalInteraccion.Telegram, Fecha);
            nueva.RegistrarSugerencia(e.MaterialA, 0.875m);

            int id = await e.Repositorio.AgregarInteraccionAsync(nueva);
            int otra = await e.Repositorio.AgregarInteraccionAsync(new Interaccion(e.Caneca, e.Ciudadano, CanalInteraccion.Simulador, Fecha.AddMinutes(5)));

            Interaccion leida = (await e.Repositorio.ObtenerInteraccionAsync(id))!;
            Assert.AreEqual(e.Caneca, leida.IdContenedor);
            Assert.AreEqual(e.Ciudadano, leida.IdUsuario);
            Assert.AreEqual(CanalInteraccion.Telegram, leida.Canal);
            Assert.AreEqual(EstadoInteraccion.Iniciada, leida.Estado);
            Assert.AreEqual(e.MaterialA, leida.IdMaterialSugerido);
            Assert.AreEqual(0.875m, leida.ConfianzaSugerida);
            Assert.AreEqual(Fecha, leida.FechaInicioUtc);
            Assert.AreEqual(DateTimeKind.Utc, leida.FechaInicioUtc.Kind);
            Assert.IsNull(leida.FechaCierreUtc);

            CollectionAssert.AreEqual(new[] { otra, id }, (await e.Repositorio.ListarInteraccionesPorContenedorAsync(e.Caneca)).Select(i => i.Id).ToArray());
            CollectionAssert.AreEqual(new[] { otra, id }, (await e.Repositorio.ListarInteraccionesPorUsuarioAsync(e.Ciudadano)).Select(i => i.Id).ToArray());
            Assert.IsNull(await e.Repositorio.ObtenerInteraccionAsync(int.MaxValue));
        }

        [TestMethod]
        public async Task Interaccion_CanecaInexistente_LanzaInvalidOperation()
        {
            var repositorio = new RepositorioInteraccionesOracle(Fabrica);

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                repositorio.AgregarInteraccionAsync(new Interaccion(int.MaxValue, null, CanalInteraccion.Simulador, Fecha)));
        }

        [TestMethod]
        public async Task Interaccion_ActualizarConEstadoCambiado_LanzaCambioConcurrenteYNoEscribe()
        {
            Escenario e = await PrepararAsync();
            int id = await e.Repositorio.AgregarInteraccionAsync(new Interaccion(e.Caneca, e.Ciudadano, CanalInteraccion.Simulador, Fecha));
            Interaccion primera = (await e.Repositorio.ObtenerInteraccionAsync(id))!;
            Interaccion segunda = (await e.Repositorio.ObtenerInteraccionAsync(id))!;
            primera.Rechazar("Material no reconocido", Fecha.AddMinutes(1));
            await e.Repositorio.ActualizarInteraccionAsync(primera, EstadoInteraccion.Iniciada);

            segunda.Rechazar("Otro motivo", Fecha.AddMinutes(2));
            var error = await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() =>
                e.Repositorio.ActualizarInteraccionAsync(segunda, EstadoInteraccion.Iniciada));

            Assert.AreEqual("La interacción cambió de estado.", error.Message);
            Interaccion leida = (await e.Repositorio.ObtenerInteraccionAsync(id))!;
            Assert.AreEqual(EstadoInteraccion.Rechazada, leida.Estado);
            Assert.AreEqual("Material no reconocido", leida.Motivo);
            Assert.AreEqual(Fecha.AddMinutes(1), leida.FechaCierreUtc);
        }

        [TestMethod]
        public async Task Orden_EmitirVisitantePorSimulador_GuardaLaOrdenSinCiudadano()
        {
            Escenario e = await PrepararAsync();
            (Interaccion interaccion, OrdenApertura orden) = await PrepararEmisionAsync(e, e.MaterialA, visitante: true, CanalInteraccion.Simulador);

            int idOrden = await e.Repositorio.EmitirOrdenAsync(interaccion, orden);

            Assert.AreEqual(idOrden, (await e.Repositorio.ObtenerOrdenVigenteAsync(e.Caneca))!.Id);
            Interaccion autorizada = (await e.Repositorio.ObtenerInteraccionAsync(interaccion.Id))!;
            Assert.IsNull(autorizada.IdUsuario);
            Assert.AreEqual(EstadoInteraccion.Autorizada, autorizada.Estado);
        }

        [TestMethod]
        public async Task Orden_EmitirVisitantePorTelegramSinSesion_SeRechazaSinEmitir()
        {
            Escenario e = await PrepararAsync();
            (Interaccion interaccion, OrdenApertura orden) = await PrepararEmisionAsync(e, e.MaterialA, visitante: true, CanalInteraccion.Telegram);

            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => e.Repositorio.EmitirOrdenAsync(interaccion, orden));

            Assert.IsNull(await e.Repositorio.ObtenerOrdenVigenteAsync(e.Caneca));
        }

        [TestMethod]
        public async Task Orden_Emitir_AutorizaLaInteraccionYQuedaVigente()
        {
            Escenario e = await PrepararAsync();
            (Interaccion interaccion, OrdenApertura orden) = await PrepararEmisionAsync(e, e.MaterialA);

            int idOrden = await e.Repositorio.EmitirOrdenAsync(interaccion, orden);

            OrdenApertura vigente = (await e.Repositorio.ObtenerOrdenVigenteAsync(e.Caneca))!;
            Assert.AreEqual(idOrden, vigente.Id);
            Assert.AreEqual(EstadoOrdenApertura.Emitida, vigente.Estado);
            Assert.AreEqual(orden.ClaveIdempotencia, vigente.ClaveIdempotencia);
            Assert.AreEqual(orden.FechaVencimientoUtc, vigente.FechaVencimientoUtc);
            Assert.AreEqual(idOrden, (await e.Repositorio.ObtenerOrdenPorClaveAsync(orden.ClaveIdempotencia))!.Id);
            Interaccion autorizada = (await e.Repositorio.ObtenerInteraccionAsync(interaccion.Id))!;
            Assert.AreEqual(EstadoInteraccion.Autorizada, autorizada.Estado);
            Assert.AreEqual(e.MaterialA, autorizada.IdMaterialConfirmado);
        }

        [TestMethod]
        public async Task Orden_SegundaVigenteEnLaMismaCaneca_LanzaCanecaOcupadaYDejaIniciada()
        {
            Escenario e = await PrepararAsync();
            (Interaccion primera, OrdenApertura orden) = await PrepararEmisionAsync(e, e.MaterialA);
            await e.Repositorio.EmitirOrdenAsync(primera, orden);
            (Interaccion segunda, OrdenApertura otraOrden) = await PrepararEmisionAsync(e, e.MaterialB);

            var error = await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => e.Repositorio.EmitirOrdenAsync(segunda, otraOrden));

            Assert.AreEqual(MensajesInteracciones.CanecaOcupada, error.Message);
            Assert.AreEqual(EstadoInteraccion.Iniciada, (await e.Repositorio.ObtenerInteraccionAsync(segunda.Id))!.Estado);
            Assert.IsNull(await e.Repositorio.ObtenerOrdenPorClaveAsync(otraOrden.ClaveIdempotencia));
        }

        [TestMethod]
        public async Task Orden_ClaveRepetida_LanzaConflictoConNombreDeRestriccion()
        {
            Escenario e = await PrepararAsync();
            (Interaccion primera, OrdenApertura orden) = await PrepararEmisionAsync(e, e.MaterialA);
            await e.Repositorio.EmitirOrdenAsync(primera, orden);
            (Interaccion segunda, _) = await PrepararEmisionAsync(e, e.MaterialA);
            var repetida = new OrdenApertura(segunda.Id, e.Caneca, orden.ClaveIdempotencia, Fecha.AddMinutes(1), Fecha.AddMinutes(3));

            var error = await Assert.ThrowsExactlyAsync<ConflictoUnicidadException>(() => e.Repositorio.EmitirOrdenAsync(segunda, repetida));

            Assert.AreEqual("uq_orden_clave", error.Message);
        }

        [TestMethod]
        public async Task Orden_MaterialQueLaCanecaNoAdmite_LanzaYNoEmite()
        {
            Escenario e = await PrepararAsync();
            var materiales = new RepositorioMaterialesOracle(Fabrica);
            int ajeno = RegistrarMaterial(await materiales.AgregarAsync(new Material("IT_C" + Sufijo(), "Material IT C", CategoriaMaterial.Aprovechable, null)));
            (Interaccion interaccion, OrdenApertura orden) = await PrepararEmisionAsync(e, ajeno);

            var error = await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => e.Repositorio.EmitirOrdenAsync(interaccion, orden));

            Assert.AreEqual(MensajesInteracciones.CanecaNoAdmiteMaterial, error.Message);
            Assert.IsNull(await e.Repositorio.ObtenerOrdenVigenteAsync(e.Caneca));
        }

        [TestMethod]
        public async Task Orden_MaterialDeshabilitado_LanzaConSuNombre()
        {
            Escenario e = await PrepararAsync();
            await new RepositorioMaterialesOracle(Fabrica).DeshabilitarAsync(e.MaterialA);
            (Interaccion interaccion, OrdenApertura orden) = await PrepararEmisionAsync(e, e.MaterialA);

            var error = await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => e.Repositorio.EmitirOrdenAsync(interaccion, orden));

            Assert.AreEqual(MensajesCatalogo.MaterialDeshabilitado("Material IT A"), error.Message);
        }

        [TestMethod]
        public async Task Orden_CanecaFueraDeServicio_LanzaYNoEmite()
        {
            Escenario e = await PrepararAsync();
            await new RepositorioContenedoresOracle(Fabrica).CambiarEstadoAsync(e.Caneca, EstadoContenedor.FueraDeServicio);
            (Interaccion interaccion, OrdenApertura orden) = await PrepararEmisionAsync(e, e.MaterialA);

            var error = await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => e.Repositorio.EmitirOrdenAsync(interaccion, orden));

            Assert.AreEqual(MensajesInteracciones.CanecaNoEnServicio, error.Message);
        }

        /// <summary>La emisión bloquea la caneca: espera a la otra sesión y, cuando la suelta sin cambios, emite.</summary>
        [TestMethod]
        public async Task Orden_EmitirConCanecaRetenida_EsperaYDespuesEmite()
        {
            Escenario e = await PrepararAsync();
            (Interaccion interaccion, OrdenApertura orden) = await PrepararEmisionAsync(e, e.MaterialA);
            using SesionAjenaOracle otra = await SesionAjenaOracle.AbrirAsync(Fabrica);
            await otra.EjecutarAsync("SELECT id_contenedor FROM contenedor WHERE id_contenedor = :id FOR UPDATE", e.Caneca);

            Task<int> operacion = e.Repositorio.EmitirOrdenAsync(interaccion, orden);
            await SesionAjenaOracle.AfirmarQueEsperaAsync(operacion);
            otra.Dispose();

            Assert.AreEqual(await operacion, (await e.Repositorio.ObtenerOrdenVigenteAsync(e.Caneca))!.Id);
        }

        [TestMethod]
        public async Task Evento_DeteccionConDeposito_GuardaTodoYSePuedeVerificarUnaVez()
        {
            Escenario e = await PrepararAsync();
            OrdenApertura orden = await EmitirAsync(e);
            orden.RegistrarDeteccion();
            var evento = new EventoDispositivo(e.Caneca, "det-" + Sufijo(), TipoEventoDispositivo.Deteccion, orden.Id,
                ResultadoEvento.Aplicado, Fecha.AddMinutes(2), Fecha.AddMinutes(2));
            var deposito = new Deposito(orden.Id, e.MaterialA, 0.125m, null, Fecha.AddMinutes(2));

            int idEvento = await e.Repositorio.RegistrarEventoAsync(evento, new CambioOrden(orden, EstadoOrdenApertura.Emitida), deposito);

            Assert.AreEqual(EstadoOrdenApertura.Detectada, (await e.Repositorio.ObtenerOrdenAsync(orden.Id))!.Estado);
            EventoDispositivo leido = (await e.Repositorio.ObtenerEventoAsync(e.Caneca, evento.IdExterno))!;
            Assert.AreEqual(idEvento, leido.Id);
            Assert.AreEqual(TipoEventoDispositivo.Deteccion, leido.Tipo);
            Assert.AreEqual(orden.Id, leido.IdOrden);
            Deposito guardado = (await e.Repositorio.ObtenerDepositoPorOrdenAsync(orden.Id))!;
            Assert.AreEqual(idEvento, guardado.IdEvento);
            Assert.AreEqual(0.125m, guardado.PesoMedidoKg);
            Assert.IsNull(guardado.PesoEstimadoKg);
            Assert.IsFalse(guardado.EstaVerificado);
            Assert.IsTrue((await e.Repositorio.ListarDepositosSinVerificarAsync()).Any(d => d.Id == guardado.Id));
            Assert.IsTrue((await e.Repositorio.ListarDepositosPorUsuarioAsync(e.Ciudadano)).Any(d => d.Id == guardado.Id));
            Assert.IsFalse((await e.Repositorio.ListarDepositosPorUsuarioAsync(e.Operario)).Any(d => d.Id == guardado.Id));

            guardado.Verificar(e.MaterialB, e.Operario, Fecha.AddMinutes(10));
            await e.Repositorio.VerificarDepositoAsync(guardado);
            Deposito otraLectura = (await e.Repositorio.ObtenerDepositoAsync(guardado.Id))!;
            Assert.AreEqual(e.MaterialB, otraLectura.IdMaterialVerificado);
            Assert.AreEqual(e.Operario, otraLectura.IdVerificador);
            Assert.IsFalse((await e.Repositorio.ListarDepositosSinVerificarAsync()).Any(d => d.Id == guardado.Id));

            var repetida = new Deposito(guardado.Id, guardado.IdOrden, guardado.IdEvento, guardado.IdMaterialDeclarado, null, null, null,
                guardado.PesoMedidoKg, null, guardado.FechaDeteccionUtc);
            repetida.Verificar(e.MaterialA, e.Operario, Fecha.AddMinutes(11));
            var error = await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => e.Repositorio.VerificarDepositoAsync(repetida));
            Assert.AreEqual(MensajesInteracciones.DepositoYaVerificado, error.Message);
            Assert.AreEqual(e.MaterialB, (await e.Repositorio.ObtenerDepositoAsync(guardado.Id))!.IdMaterialVerificado);
        }

        [TestMethod]
        public async Task Evento_SegundoDepositoDeLaOrden_LanzaYNoGuardaElEvento()
        {
            Escenario e = await PrepararAsync();
            OrdenApertura orden = await EmitirAsync(e);
            orden.RegistrarDeteccion();
            await e.Repositorio.RegistrarEventoAsync(
                new EventoDispositivo(e.Caneca, "det-" + Sufijo(), TipoEventoDispositivo.Deteccion, orden.Id, ResultadoEvento.Aplicado, null, Fecha.AddMinutes(2)),
                new CambioOrden(orden, EstadoOrdenApertura.Emitida),
                new Deposito(orden.Id, e.MaterialA, null, 0.050m, Fecha.AddMinutes(2)));
            string segundo = "det-" + Sufijo();

            var error = await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => e.Repositorio.RegistrarEventoAsync(
                new EventoDispositivo(e.Caneca, segundo, TipoEventoDispositivo.Deteccion, orden.Id, ResultadoEvento.Aplicado, null, Fecha.AddMinutes(3)),
                null,
                new Deposito(orden.Id, e.MaterialA, null, 0.050m, Fecha.AddMinutes(3))));

            Assert.AreEqual("La orden ya tiene depósito.", error.Message);
            Assert.IsNull(await e.Repositorio.ObtenerEventoAsync(e.Caneca, segundo));
        }

        [TestMethod]
        public async Task Evento_IdExternoRepetido_LanzaConflictoYNoTocaLaOrden()
        {
            Escenario e = await PrepararAsync();
            OrdenApertura orden = await EmitirAsync(e);
            string externo = "ack-" + Sufijo();
            orden.RegistrarAcuse();
            await e.Repositorio.RegistrarEventoAsync(
                new EventoDispositivo(e.Caneca, externo, TipoEventoDispositivo.Acuse, orden.Id, ResultadoEvento.Aplicado, null, Fecha.AddMinutes(1)),
                new CambioOrden(orden, EstadoOrdenApertura.Emitida), null);
            OrdenApertura actual = (await e.Repositorio.ObtenerOrdenAsync(orden.Id))!;
            actual.RegistrarApertura();

            var error = await Assert.ThrowsExactlyAsync<ConflictoUnicidadException>(() => e.Repositorio.RegistrarEventoAsync(
                new EventoDispositivo(e.Caneca, externo, TipoEventoDispositivo.Apertura, orden.Id, ResultadoEvento.Aplicado, null, Fecha.AddMinutes(2)),
                new CambioOrden(actual, EstadoOrdenApertura.Acusada), null));

            Assert.AreEqual("uq_evento_externo", error.Message);
            Assert.AreEqual(EstadoOrdenApertura.Acusada, (await e.Repositorio.ObtenerOrdenAsync(orden.Id))!.Estado);
        }

        [TestMethod]
        public async Task Evento_OrdenCambiadaPorOtro_LanzaYNoGuardaElEvento()
        {
            Escenario e = await PrepararAsync();
            OrdenApertura orden = await EmitirAsync(e);
            OrdenApertura copia = (await e.Repositorio.ObtenerOrdenAsync(orden.Id))!;
            orden.MarcarEnviada();
            await e.Repositorio.ActualizarOrdenAsync(new CambioOrden(orden, EstadoOrdenApertura.Emitida));
            copia.RegistrarAcuse();
            string externo = "ack-" + Sufijo();

            var error = await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => e.Repositorio.RegistrarEventoAsync(
                new EventoDispositivo(e.Caneca, externo, TipoEventoDispositivo.Acuse, orden.Id, ResultadoEvento.Aplicado, null, Fecha.AddMinutes(1)),
                new CambioOrden(copia, EstadoOrdenApertura.Emitida), null));

            Assert.AreEqual("La orden cambió de estado.", error.Message);
            Assert.IsNull(await e.Repositorio.ObtenerEventoAsync(e.Caneca, externo));
            Assert.AreEqual(EstadoOrdenApertura.Enviada, (await e.Repositorio.ObtenerOrdenAsync(orden.Id))!.Estado);
        }

        [TestMethod]
        public async Task Evento_DepositoDeOtraOrden_LanzaArgumentYNoGuardaNada()
        {
            Escenario e = await PrepararAsync();
            OrdenApertura orden = await EmitirAsync(e);
            string externo = "det-" + Sufijo();

            await Assert.ThrowsExactlyAsync<ArgumentException>(() => e.Repositorio.RegistrarEventoAsync(
                new EventoDispositivo(e.Caneca, externo, TipoEventoDispositivo.Deteccion, orden.Id, ResultadoEvento.Aplicado, null, Fecha.AddMinutes(2)),
                null,
                new Deposito(orden.Id + 1, e.MaterialA, null, 0.050m, Fecha.AddMinutes(2))));

            Assert.IsNull(await e.Repositorio.ObtenerEventoAsync(e.Caneca, externo));
        }

        [TestMethod]
        public async Task Interaccion_ActualizarComoAutorizada_LanzaArgumentYNoEscribe()
        {
            Escenario e = await PrepararAsync();
            (Interaccion interaccion, _) = await PrepararEmisionAsync(e, e.MaterialA);

            await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
                e.Repositorio.ActualizarInteraccionAsync(interaccion, EstadoInteraccion.Iniciada));

            Assert.AreEqual(EstadoInteraccion.Iniciada, (await e.Repositorio.ObtenerInteraccionAsync(interaccion.Id))!.Estado);
        }

        [TestMethod]
        public async Task Evento_EnRevisionSinOrden_SeGuardaYSeLista()
        {
            Escenario e = await PrepararAsync();
            var evento = new EventoDispositivo(e.Caneca, "huerfano-" + Sufijo(), TipoEventoDispositivo.Deteccion, null,
                ResultadoEvento.Revision, null, Fecha);

            int id = await e.Repositorio.RegistrarEventoAsync(evento, null, null);

            Assert.IsTrue((await e.Repositorio.ListarEventosEnRevisionAsync()).Any(x => x.Id == id && x.IdOrden is null));
        }

        [TestMethod]
        public async Task Orden_VencidaYCerradaSinDeposito_LiberaLaCaneca()
        {
            Escenario e = await PrepararAsync();
            OrdenApertura orden = await EmitirAsync(e);

            Assert.IsTrue((await e.Repositorio.ListarOrdenesVencidasAsync(orden.FechaVencimientoUtc)).Any(o => o.Id == orden.Id));
            Assert.IsFalse((await e.Repositorio.ListarOrdenesVencidasAsync(orden.FechaVencimientoUtc.AddSeconds(-1))).Any(o => o.Id == orden.Id));

            Interaccion interaccion = (await e.Repositorio.ObtenerInteraccionAsync(orden.IdInteraccion))!;
            orden.Vencer(orden.FechaVencimientoUtc);
            interaccion.CerrarSinDeposito(orden.FechaVencimientoUtc);
            await e.Repositorio.ActualizarOrdenAsync(new CambioOrden(orden, EstadoOrdenApertura.Emitida, interaccion));

            Assert.IsNull(await e.Repositorio.ObtenerOrdenVigenteAsync(e.Caneca));
            Assert.AreEqual(EstadoOrdenApertura.Vencida, (await e.Repositorio.ObtenerOrdenAsync(orden.Id))!.Estado);
            Interaccion cerrada = (await e.Repositorio.ObtenerInteraccionAsync(interaccion.Id))!;
            Assert.AreEqual(EstadoInteraccion.SinDeposito, cerrada.Estado);
            Assert.AreEqual(orden.FechaVencimientoUtc, cerrada.FechaCierreUtc);
            (Interaccion siguiente, OrdenApertura nueva) = await PrepararEmisionAsync(e, e.MaterialB);
            Assert.IsGreaterThan(0, await e.Repositorio.EmitirOrdenAsync(siguiente, nueva));
        }

        [TestMethod]
        public async Task Orden_ActualizarConOrdenRetenidaMasDeDiezSegundos_LanzaCambioConcurrente()
        {
            Escenario e = await PrepararAsync();
            OrdenApertura orden = await EmitirAsync(e);
            using SesionAjenaOracle otra = await SesionAjenaOracle.AbrirAsync(Fabrica);
            await otra.EjecutarAsync("SELECT id_orden FROM orden_apertura WHERE id_orden = :id FOR UPDATE", orden.Id);
            orden.MarcarEnviada();

            var error = await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() =>
                e.Repositorio.ActualizarOrdenAsync(new CambioOrden(orden, EstadoOrdenApertura.Emitida)));

            Assert.AreEqual(Datos.Oracle.TraductorErroresOracle.MensajeEsperaBloqueo, error.Message);
            otra.Dispose();
            Assert.AreEqual(EstadoOrdenApertura.Emitida, (await e.Repositorio.ObtenerOrdenAsync(orden.Id))!.Estado);
        }

        private sealed record Escenario(RepositorioInteraccionesOracle Repositorio, int Caneca, int MaterialA, int MaterialB, int Ciudadano, int Operario);

        private async Task<Escenario> PrepararAsync()
        {
            var materiales = new RepositorioMaterialesOracle(Fabrica);
            var puntos = new RepositorioPuntosInstalacionOracle(Fabrica);
            var contenedores = new RepositorioContenedoresOracle(Fabrica);
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            string sufijo = Sufijo();
            int a = RegistrarMaterial(await materiales.AgregarAsync(new Material("IT_A" + sufijo, "Material IT A", CategoriaMaterial.Aprovechable, null)));
            int b = RegistrarMaterial(await materiales.AgregarAsync(new Material("IT_B" + sufijo, "Material IT B", CategoriaMaterial.Aprovechable, null)));
            int punto = RegistrarPunto(await puntos.AgregarAsync(new PuntoInstalacion("Punto IT " + sufijo, null)));
            var nueva = new Contenedor(punto, "Caneca IT " + sufijo, new GeneradorIdentificadorQrAleatorio().Generar(), Fecha);
            nueva.AdmitirMaterial(a);
            nueva.AdmitirMaterial(b);
            int caneca = RegistrarContenedor(await contenedores.AgregarAsync(nueva));
            await contenedores.CambiarEstadoAsync(caneca, EstadoContenedor.EnServicio);
            var cuenta = new Usuario("Ciudadano IT", $"it.c{sufijo.ToLowerInvariant()}@ejemplo.com", CodigoRol.Ciudadano, Fecha);
            cuenta.RegistrarConsentimiento("prueba-integracion", Fecha);
            int ciudadano = RegistrarUsuario(await usuarios.AgregarAsync(cuenta));
            int operario = RegistrarUsuario(await usuarios.AgregarAsync(
                new Usuario("Operario IT", $"it.o{sufijo.ToLowerInvariant()}@ejemplo.com", CodigoRol.Operario, Fecha)));
            return new Escenario(new RepositorioInteraccionesOracle(Fabrica, new RelojOracleAjustable(Fecha.AddMinutes(1))), caneca, a, b, ciudadano, operario);
        }

        /// <summary>Guarda una interacción iniciada y devuelve su copia autorizada con la orden lista para emitir.</summary>
        private static async Task<(Interaccion Interaccion, OrdenApertura Orden)> PrepararEmisionAsync(Escenario e, int idMaterial,
            bool visitante = false, CanalInteraccion canal = CanalInteraccion.Telegram)
        {
            int id = await e.Repositorio.AgregarInteraccionAsync(new Interaccion(e.Caneca, visitante ? null : e.Ciudadano, canal, Fecha));
            Interaccion interaccion = (await e.Repositorio.ObtenerInteraccionAsync(id))!;
            interaccion.Autorizar(idMaterial);
            var orden = new OrdenApertura(id, e.Caneca, "it-" + Guid.NewGuid().ToString("N"), Fecha.AddMinutes(1), Fecha.AddMinutes(3));
            return (interaccion, orden);
        }

        private static async Task<OrdenApertura> EmitirAsync(Escenario e)
        {
            (Interaccion interaccion, OrdenApertura orden) = await PrepararEmisionAsync(e, e.MaterialA);
            int id = await e.Repositorio.EmitirOrdenAsync(interaccion, orden);
            return (await e.Repositorio.ObtenerOrdenAsync(id))!;
        }
    }
}
