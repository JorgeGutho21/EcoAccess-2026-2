using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioConversacionTelegramTests
    {
        private ServicioConversacionTelegram ConCanal(ICanalDispositivo canal, TimeSpan? tiempo = null) => new(
            _conversaciones, _contenedores, _materiales,
            new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
            _servicioInteracciones, _limitador, PaginaRegistro, _reloj,
            new ServicioDespachoOrdenes(_interacciones, _servicioInteracciones, canal, _reloj, tiempo));

        private Task<IReadOnlyList<RespuestaBot>> EnviarConAsync(ServicioConversacionTelegram servicio,
            string? texto = null, string? boton = null, long cuenta = Vinculado, CancellationToken ct = default) =>
            servicio.ProcesarAsync(new MensajeEntrante(Interlocked.Increment(ref _update), cuenta, cuenta, true, texto, boton), ct);

        private async Task PrepararConAsync(ServicioConversacionTelegram servicio)
        {
            await EnviarConAsync(servicio, "/start " + _caneca.IdentificadorQr);
            await EnviarConAsync(servicio, boton: MaterialPet);
        }

        [TestMethod]
        [DataRow(EscenarioCaneca.Normal, EstadoOrdenApertura.Cerrada, 1)]
        [DataRow(EscenarioCaneca.SinAcuse, EstadoOrdenApertura.Enviada, 0)]
        [DataRow(EscenarioCaneca.AcuseSinApertura, EstadoOrdenApertura.Acusada, 0)]
        [DataRow(EscenarioCaneca.AperturaSinDeteccion, EstadoOrdenApertura.Cerrada, 0)]
        [DataRow(EscenarioCaneca.Fallo, EstadoOrdenApertura.Fallida, 0)]
        [DataRow(EscenarioCaneca.Duplicados, EstadoOrdenApertura.Cerrada, 1)]
        [DataRow(EscenarioCaneca.FueraDeOrden, EstadoOrdenApertura.Cerrada, 0)]
        [DataRow(EscenarioCaneca.Desconectada, EstadoOrdenApertura.Emitida, 0)]
        public async Task BotDespachaYLosEventosDeterminanElDeposito(EscenarioCaneca escenario, EstadoOrdenApertura estado, int depositos)
        {
            var caneca = new CanecaSimulada(_caneca.Id, _servicioInteracciones.RegistrarEventoAsync, _reloj) { Escenario = escenario };
            var servicio = ConCanal(caneca);
            await PrepararConAsync(servicio);

            string respuesta = Unica(await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi)).Texto;
            Assert.AreEqual(MensajesTelegram.Despacho(escenario == EscenarioCaneca.Desconectada
                ? ResultadoDespacho.NoEntregada : ResultadoDespacho.Enviada), respuesta);
            Assert.IsEmpty(_interacciones.Depositos, "Entregar nunca prueba ingreso.");
            int orden = _interacciones.Ordenes.Single().Id;

            await caneca.EjecutarAsync(orden);
            await caneca.EjecutarAsync(orden);

            Assert.AreEqual(estado, _interacciones.LeerOrden(orden)!.Estado);
            Assert.HasCount(depositos, _interacciones.Depositos);
            foreach (var deposito in _interacciones.Depositos)
            {
                Assert.IsFalse(deposito.EstaVerificado);
                Assert.IsNull(deposito.PesoMedidoKg);
            }
            string consulta = Unica(await EnviarConAsync(servicio, "/estado")).Texto;
            StringAssert.Contains(consulta, estado.ToString());
            StringAssert.Contains(consulta, depositos == 0 ? "Sin depósito registrado" : "material pendiente de verificación");
        }

        [TestMethod]
        public async Task ReconectarReintentaLaMismaOrdenYUnComandoRepetidoNoDepositaDosVeces()
        {
            var caneca = new CanecaSimulada(_caneca.Id, _servicioInteracciones.RegistrarEventoAsync, _reloj)
            { Escenario = EscenarioCaneca.Desconectada };
            var servicio = ConCanal(caneca);
            await PrepararConAsync(servicio);
            var fallo = Unica(await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi));
            Assert.AreEqual(ServicioConversacionTelegram.ConfirmarSi, fallo.Botones[0].Dato);
            caneca.Escenario = EscenarioCaneca.Normal;

            await EnviarConAsync(servicio, boton: fallo.Botones[0].Dato);
            await EnviarConAsync(servicio, boton: fallo.Botones[0].Dato);
            Assert.HasCount(1, _interacciones.Ordenes);
            Assert.HasCount(1, caneca.ComandosRecibidos);
            await caneca.EjecutarAsync(_interacciones.Ordenes.Single().Id);
            Assert.HasCount(1, _interacciones.Depositos);
        }

        [TestMethod]
        public async Task ReintentoDespuesDelVencimientoNoEntregaNiInventaDeposito()
        {
            var caneca = new CanecaSimulada(_caneca.Id, _servicioInteracciones.RegistrarEventoAsync, _reloj)
            { Escenario = EscenarioCaneca.Desconectada };
            var servicio = ConCanal(caneca);
            await PrepararConAsync(servicio);
            await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi);
            _reloj.Avanzar(ServicioInteracciones.VigenciaOrden + TimeSpan.FromSeconds(1));
            caneca.Escenario = EscenarioCaneca.Normal;

            Assert.AreEqual(MensajesTelegram.Despacho(ResultadoDespacho.Vencida),
                Unica(await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi)).Texto);
            StringAssert.Contains(Unica(await EnviarConAsync(servicio, "/estado")).Texto, "vencida por tiempo");
            Assert.IsEmpty(caneca.ComandosRecibidos);
            Assert.IsEmpty(_interacciones.Depositos);
        }

        [TestMethod]
        public async Task CancelarTrasEmitirNoPrometeRevocarElComando()
        {
            var caneca = new CanecaSimulada(_caneca.Id, _servicioInteracciones.RegistrarEventoAsync, _reloj);
            var servicio = ConCanal(caneca);
            await PrepararConAsync(servicio);
            await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi);
            Assert.AreEqual(MensajesTelegram.DialogoCerrado, Unica(await EnviarConAsync(servicio, "/cancelar")).Texto);
            Assert.AreEqual(EstadoOrdenApertura.Enviada, _interacciones.Ordenes.Single().Estado);
            Assert.IsEmpty(_interacciones.Depositos);
        }

        [TestMethod]
        public async Task ConsultaDeEstadoExigeVinculoYTitularidadAunqueSeConozcaLaClave()
        {
            var caneca = new CanecaSimulada(_caneca.Id, _servicioInteracciones.RegistrarEventoAsync, _reloj);
            var servicio = ConCanal(caneca);
            await PrepararConAsync(servicio);
            await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi);
            Assert.AreEqual(MensajesTelegram.SinVinculoConsulta, Unica(await EnviarConAsync(servicio, "/estado", cuenta: Invitado)).Texto);
            _usuarios.AgregarCiudadano(4);
            var orden = _interacciones.Ordenes.Single();
            var ajena = await _servicioInteracciones.ConsultarOperacionAsync(4, orden.IdInteraccion, orden.ClaveIdempotencia);
            Assert.IsFalse(ajena.Exitoso);
            Assert.AreEqual(MotivoRechazo.SinPermiso, ajena.Motivo);
        }

        [TestMethod]
        public async Task FalloDePersistenciaDespuesDeEntregarSeRecuperaConLaMismaOrden()
        {
            var caneca = new CanecaSimulada(_caneca.Id, _servicioInteracciones.RegistrarEventoAsync, _reloj);
            bool fallar = true;
            var servicio = ConCanal(new CanalDelFlujo(async (comando, ct) =>
            {
                bool entregado = await caneca.EnviarAperturaAsync(comando, ct);
                if (fallar)
                {
                    _interacciones.AlEscribir = () => throw new IOException("Fallo de persistencia simulado, no es Oracle real.");
                }
                return entregado;
            }));
            await PrepararConAsync(servicio);
            await Assert.ThrowsExactlyAsync<IOException>(() => EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi));
            Assert.IsEmpty(_interacciones.Depositos);
            fallar = false;

            await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi);
            Assert.HasCount(1, _interacciones.Ordenes);
            Assert.HasCount(1, caneca.ComandosRecibidos);
            await caneca.EjecutarAsync(_interacciones.Ordenes.Single().Id);
            Assert.HasCount(1, _interacciones.Depositos);
        }

        [TestMethod]
        public async Task CanalQueNoRespondeNoCreaDepositoYPermiteReintentar()
        {
            var pendiente = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var servicio = ConCanal(new CanalDelFlujo((_, _) => pendiente.Task), TimeSpan.FromMilliseconds(25));
            await PrepararConAsync(servicio);
            var respuesta = Unica(await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi));
            Assert.AreEqual(MensajesTelegram.Despacho(ResultadoDespacho.NoEntregada), respuesta.Texto);
            Assert.AreEqual(ServicioConversacionTelegram.ConfirmarSi, respuesta.Botones[0].Dato, "El primer botón reintenta la misma orden.");
            Assert.IsEmpty(_interacciones.Depositos);
            pendiente.SetResult(false);
        }

        [TestMethod]
        public async Task ConfirmacionesSolapadasConservanUnaOrdenYUnaSecuenciaDeEventos()
        {
            var caneca = new CanecaSimulada(_caneca.Id, _servicioInteracciones.RegistrarEventoAsync, _reloj);
            var entro = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var liberar = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int llamadas = 0;
            var servicio = ConCanal(new CanalDelFlujo(async (comando, ct) =>
            {
                llamadas++;
                entro.SetResult(true);
                await liberar.Task.WaitAsync(TimeSpan.FromSeconds(2), ct);
                return await caneca.EnviarAperturaAsync(comando, ct);
            }));
            await PrepararConAsync(servicio);
            var primera = EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi);
            await entro.Task.WaitAsync(TimeSpan.FromSeconds(1));
            var segunda = EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi);
            Assert.IsFalse(segunda.IsCompleted, "La misma cuenta espera a que termine la confirmación anterior.");
            Assert.AreEqual(1, llamadas);
            liberar.SetResult(true);
            await Task.WhenAll(primera, segunda);
            Assert.AreEqual(1, llamadas, "La confirmación que esperó observa la entrega persistida y no reenvía.");
            Assert.HasCount(1, _interacciones.Ordenes);
            Assert.HasCount(1, caneca.ComandosRecibidos);
            await Task.WhenAll(caneca.EjecutarAsync(_interacciones.Ordenes.Single().Id),
                caneca.EjecutarAsync(_interacciones.Ordenes.Single().Id));
            Assert.HasCount(4, _interacciones.Eventos);
            Assert.HasCount(1, _interacciones.Depositos);
        }

        [TestMethod]
        public async Task ReiniciarSimuladorConOtraOrdenNoReutilizaIdentificadoresExternos()
        {
            var primera = new CanecaSimulada(_caneca.Id, _servicioInteracciones.RegistrarEventoAsync, _reloj);
            var servicio = ConCanal(primera);
            await PrepararConAsync(servicio);
            await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi);
            await primera.EjecutarAsync(_interacciones.Ordenes.Single().Id);
            var segunda = new CanecaSimulada(_caneca.Id, _servicioInteracciones.RegistrarEventoAsync, _reloj);
            servicio = ConCanal(segunda);
            await PrepararConAsync(servicio);
            await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi);
            await segunda.EjecutarAsync(segunda.ComandosRecibidos.Single().IdOrden);
            Assert.HasCount(8, _interacciones.Eventos);
            Assert.HasCount(2, _interacciones.Depositos);
        }

        [TestMethod]
        public async Task CancelarTrasFalloAlGuardarDialogoReconoceLaOrdenPersistida()
        {
            var caneca = new CanecaSimulada(_caneca.Id, _servicioInteracciones.RegistrarEventoAsync, _reloj);
            var servicio = ConCanal(caneca);
            await PrepararConAsync(servicio);
            _conversaciones.AlEscribir = () => throw new IOException("Fallo simulado al guardar diálogo.");
            await Assert.ThrowsExactlyAsync<IOException>(() => EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi));

            Assert.AreEqual(MensajesTelegram.DialogoCerrado, Unica(await EnviarConAsync(servicio, "/cancelar")).Texto);
            Assert.AreEqual(EstadoOrdenApertura.Enviada, _interacciones.Ordenes.Single().Estado);
            Assert.IsEmpty(_interacciones.Depositos);
        }

        [TestMethod]
        public async Task CancelacionAntesDeConfirmarNoEntregaComandos()
        {
            var caneca = new CanecaSimulada(_caneca.Id, _servicioInteracciones.RegistrarEventoAsync, _reloj);
            var servicio = ConCanal(caneca);
            await PrepararConAsync(servicio);
            Assert.AreEqual(MensajesTelegram.Cancelado, Unica(await EnviarConAsync(servicio, "/cancelar")).Texto);
            await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi);
            Assert.IsEmpty(caneca.ComandosRecibidos);
            Assert.IsEmpty(_interacciones.Ordenes);
            Assert.IsEmpty(_interacciones.Depositos);
        }

        [TestMethod]
        public async Task CancelacionPreviaDelSimuladorNoConsumeLaEjecucion()
        {
            var caneca = new CanecaSimulada(_caneca.Id, _servicioInteracciones.RegistrarEventoAsync, _reloj);
            var servicio = ConCanal(caneca);
            await PrepararConAsync(servicio);
            await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi);
            using var cancelacion = new CancellationTokenSource();
            cancelacion.Cancel();
            int orden = _interacciones.Ordenes.Single().Id;
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => caneca.EjecutarAsync(orden, cancelacion.Token));
            Assert.IsEmpty(_interacciones.Eventos);
            await caneca.EjecutarAsync(orden);
            Assert.HasCount(1, _interacciones.Depositos);
        }

        [TestMethod]
        public async Task FalloAlRegistrarAperturaNoGeneraDepositoNiRepiteLaSecuencia()
        {
            var caneca = new CanecaSimulada(_caneca.Id, async (evento, ct) =>
            {
                var respuesta = await _servicioInteracciones.RegistrarEventoAsync(evento, ct);
                if (evento.Tipo == TipoEventoDispositivo.Apertura) throw new IOException("Respuesta de apertura perdida.");
                return respuesta;
            }, _reloj);
            var servicio = ConCanal(caneca);
            await PrepararConAsync(servicio);
            await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi);
            int orden = _interacciones.Ordenes.Single().Id;
            await Assert.ThrowsExactlyAsync<IOException>(() => caneca.EjecutarAsync(orden));
            Assert.IsEmpty(await caneca.EjecutarAsync(orden));
            Assert.AreEqual(EstadoOrdenApertura.Abierta, _interacciones.Ordenes.Single().Estado);
            Assert.HasCount(2, _interacciones.Eventos);
            Assert.IsEmpty(_interacciones.Depositos);
        }

        [TestMethod]
        public async Task OtraPersonaNoObtieneUnaSegundaOrdenMientrasLaCanecaEstaOcupada()
        {
            _usuarios.AgregarCiudadano(4);
            _vinculos.Sembrar(4, Invitado);
            var caneca = new CanecaSimulada(_caneca.Id, _servicioInteracciones.RegistrarEventoAsync, _reloj);
            var servicio = ConCanal(caneca);
            await PrepararConAsync(servicio);
            await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi);
            await EnviarConAsync(servicio, "/start " + _caneca.IdentificadorQr, cuenta: Invitado);
            await EnviarConAsync(servicio, boton: MaterialPet, cuenta: Invitado);
            await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi, cuenta: Invitado);
            Assert.HasCount(1, _interacciones.Ordenes);
            Assert.HasCount(1, caneca.ComandosRecibidos);
            Assert.IsEmpty(_interacciones.Depositos);
        }

        [TestMethod]
        public async Task CancelacionQuePierdeCarreraConConfirmacionNoAfirmaRechazo()
        {
            var caneca = new CanecaSimulada(_caneca.Id, _servicioInteracciones.RegistrarEventoAsync, _reloj);
            var servicio = ConCanal(caneca);
            await PrepararConAsync(servicio);
            int interaccion = IdInteraccionActual;
            _interacciones.AlEscribir = () =>
            {
                var orden = _servicioInteracciones.AutorizarAperturaAsync(Ciudadano, interaccion, _pet.Id,
                    $"telegram:{interaccion}:{_pet.Id}").GetAwaiter().GetResult();
                Assert.IsTrue(orden.Exitoso);
            };

            Assert.AreEqual(MensajesTelegram.CancelacionNoConfirmada, Unica(await EnviarConAsync(servicio, "/cancelar")).Texto);
            Assert.HasCount(1, _interacciones.Ordenes);
            Assert.IsEmpty(_interacciones.Depositos);
        }

        private sealed class CanalDelFlujo : ICanalDispositivo
        {
            private readonly Func<ComandoApertura, CancellationToken, Task<bool>> _enviar;

            public CanalDelFlujo(Func<ComandoApertura, CancellationToken, Task<bool>> enviar)
            {
                _enviar = enviar;
            }

            public string Canal => "doble del flujo";
            public Task<bool> EnviarAperturaAsync(ComandoApertura comando, CancellationToken cancellationToken = default) => _enviar(comando, cancellationToken);
        }
    }
}
