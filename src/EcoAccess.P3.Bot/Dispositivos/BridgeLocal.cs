using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot.Dispositivos
{

    /// <summary>Recupera órdenes de Oracle y recibe eventos autenticados; nunca acciona hardware.</summary>
    public sealed class BridgeLocal
    {
        private readonly ConfiguracionBridge _configuracion;
        private readonly CredencialDispositivo _credencial;
        private readonly IRepositorioInteracciones _repositorio;
        private readonly ServicioInteracciones _servicio;
        private readonly TimeProvider _reloj;
        private readonly Action<string> _registrar;

        /// <summary>Plazo de un ciclo de consulta: más que el límite de cada sentencia (30 s) para no cortar un ciclo normal lento.</summary>
        internal static readonly TimeSpan TiempoMaximoCiclo = TimeSpan.FromSeconds(75);

        public BridgeLocal(ConfiguracionBridge configuracion, CredencialDispositivo credencial, IRepositorioInteracciones repositorio, ServicioInteracciones servicio, TimeProvider reloj, Action<string> registrar)
        {
            _configuracion = configuracion;
            _credencial = credencial;
            _repositorio = repositorio;
            _servicio = servicio;
            _reloj = reloj;
            _registrar = registrar;
        }

        public async Task EjecutarAsync(CancellationToken ct)
        {
            CicloBridge.ValidarBinding(_configuracion, _credencial);
            using var sesion = new SesionBridgeLocal(_configuracion, "bridge");
            var protocolo = new ProtocoloDispositivoHmac(_reloj, (id, v) => id == _credencial.Dispositivo && v == _credencial.VersionClave ? _credencial : null);
            using var canal = new CanalDispositivoHttp(_configuracion.Comandos, protocolo, _credencial);
            var despacho = new ServicioDespachoOrdenes(_repositorio, _servicio, canal, _reloj);
            var receptor = new ReceptorEventosDispositivo(protocolo, _servicio.RegistrarEventoAsync);
            using var servidor = new ServidorMensajesDispositivoHttp(_configuracion.Eventos, async (paquete, cancelar) =>
            {
                if (!protocolo.ValidarEvento(paquete).Exitoso) return RespuestaHttpDispositivo.Rechazar();
                var resultado = await receptor.RecibirAsync(paquete, cancelar);
                if (resultado.Exitoso) _registrar($"Evento {resultado.Valor.IdEvento} persistido: {resultado.Valor.Resultado}; repetido={resultado.Valor.Repetido}.");
                return new RespuestaHttpDispositivo(200, protocolo.FirmarRecepcion(_credencial, paquete, resultado.Exitoso,
                    resultado.Exitoso ? resultado.Valor : null));
            }, (_configuracion.Vigencia, ConsultarVigenciaAsync));
            _registrar("Preparando bridge local; esperando mensajes autenticados. Esto no acredita un ESP32 conectado.");
            await CicloBridge.EjecutarAsync(servidor.EjecutarAsync, ConsultarAsync, ct);

            async Task<RespuestaHttpDispositivo> ConsultarVigenciaAsync(byte[] paquete, CancellationToken cancelar)
            {
                var comando = protocolo.ValidarConsultaVigencia(paquete);
                if (!comando.Exitoso) return RespuestaHttpDispositivo.Rechazar();
                bool autorizado = await _servicio.SigueAutorizadoComandoAsync(comando.Valor, cancelar);
                return new(200, protocolo.FirmarRecepcion(_credencial, paquete, autorizado));
            }

            async Task ConsultarAsync(CancellationToken cancelar)
            {
                while (!cancelar.IsCancellationRequested)
                {
                    // Vigilancia: si Oracle no responde en el plazo (conexión caída sin aviso), el proceso termina en vez de
                    // quedar congelado con órdenes sin atender; el supervisor de la estación lo vuelve a iniciar. El bridge no
                    // acciona hardware: salir no deja ninguna tapa a medias, y el diario y las reservas son durables.
                    Task ciclo = CicloAsync(cancelar);
                    if (await Task.WhenAny(ciclo, Task.Delay(TiempoMaximoCiclo, cancelar)) != ciclo)
                    {
                        if (cancelar.IsCancellationRequested) break;
                        _registrar($"Oracle no respondió en {TiempoMaximoCiclo.TotalSeconds:N0} s: se reinicia el bridge para abrir una conexión nueva.");
                        Environment.Exit(3);
                    }
                    if (ciclo.IsCanceled) break;
                    await Task.Delay(TimeSpan.FromSeconds(1), cancelar);
                }
            }

            async Task CicloAsync(CancellationToken cancelar)
            {
                try
                {
                    await _servicio.VencerOrdenesAsync(cancelar, _configuracion.IdContenedor);
                    var orden = await _repositorio.ObtenerOrdenVigenteAsync(_configuracion.IdContenedor, cancelar);
                    if (orden is { Estado: EstadoOrdenApertura.Emitida })
                    {
                        var comando = new ComandoApertura(orden.Id, orden.IdContenedor, orden.ClaveIdempotencia, orden.FechaVencimientoUtc);
                        if (await _servicio.SigueAutorizadoComandoAsync(comando, cancelar))
                            _registrar($"Orden {orden.Id}: {await despacho.DespacharAsync(orden.Id, cancelar)}; entrega no significa apertura.");
                    }
                }
                catch (OperationCanceledException) when (cancelar.IsCancellationRequested) { throw; }
                catch (Exception) { _registrar("No se completó la consulta/entrega. Se conserva la orden; no se confirma actuación."); }
            }
        }
    }

    /// <summary>Adaptador HTTP del simulador existente, con buzón durable, reserva única y salida ordenada.</summary>
    public sealed class DispositivoSimuladoHttp
    {
        private readonly ConfiguracionBridge _configuracion;
        private readonly CredencialDispositivo _credencial;
        private readonly TimeProvider _reloj;
        private readonly Action<string> _registrar;
        private readonly EscenarioCaneca _escenario;

        private readonly TimeSpan _ventanaDeposito;

        public DispositivoSimuladoHttp(ConfiguracionBridge configuracion, CredencialDispositivo credencial, TimeProvider reloj, Action<string> registrar, EscenarioCaneca escenario = EscenarioCaneca.Normal, TimeSpan ventanaDeposito = default)
        {
            _configuracion = configuracion;
            _credencial = credencial;
            _reloj = reloj;
            _registrar = registrar;
            _escenario = escenario;
        _ventanaDeposito = ventanaDeposito;
        }

        public Task EjecutarAsync(CancellationToken ct) =>
            new DispositivoHttpConColas(_configuracion, _credencial, _reloj, _registrar, _escenario, ventanaDeposito: _ventanaDeposito).EjecutarAsync(ct);
    }

    /// <summary>Una sola infraestructura de recepción, vigencia, diario y eventos para simulador y USB.</summary>
    internal sealed class DispositivoHttpConColas
    {
        private readonly ConfiguracionBridge _configuracion;
        private readonly CredencialDispositivo _credencial;
        private readonly TimeProvider _reloj;
        private readonly Action<string> _registrar;
        private readonly EscenarioCaneca _escenario;
        private readonly Func<ComandoApertura, Func<EventoRecibido, CancellationToken, Task<Resultado<EventoRegistrado>>>, CancellationToken, Task>? _actuar;
        private readonly TimeSpan _ventanaDeposito;

        public DispositivoHttpConColas(ConfiguracionBridge configuracion, CredencialDispositivo credencial, TimeProvider reloj, Action<string> registrar, EscenarioCaneca escenario = EscenarioCaneca.Normal, Func<ComandoApertura, Func<EventoRecibido, CancellationToken, Task<Resultado<EventoRegistrado>>>, CancellationToken, Task>? actuar = null, TimeSpan ventanaDeposito = default)
        {
            _configuracion = configuracion;
            _credencial = credencial;
            _reloj = reloj;
            _registrar = registrar;
            _escenario = escenario;
            _actuar = actuar;
            _ventanaDeposito = ventanaDeposito;
        }

        public async Task EjecutarAsync(CancellationToken ct)
        {
            CicloBridge.ValidarBinding(_configuracion, _credencial);
            using var sesion = new SesionBridgeLocal(_configuracion, "simulador");
            var protocolo = new ProtocoloDispositivoHmac(_reloj, (id, v) => id == _credencial.Dispositivo && v == _credencial.VersionClave ? _credencial : null);
            var entradas = new ColaPaquetesDispositivo(Path.Combine(sesion.Directorio, "comandos"));
            var salidas = new ColaPaquetesDispositivo(Path.Combine(sesion.Directorio, "eventos"));
            bool HayCapacidadDeEventos() => salidas.Pendientes <= ColaPaquetesDispositivo.MaximoPendientes - 8
                && salidas.EnRevision < ColaPaquetesDispositivo.MaximoPendientes;
            using var canalEventos = new CanalDispositivoHttp(_configuracion.Eventos, protocolo, _credencial);
            using var autoridad = new CanalDispositivoHttp(_configuracion.Vigencia, protocolo, _credencial);
            var simulada = new CanecaSimulada(_configuracion.IdContenedor, GuardarEventoAsync, _reloj) { Escenario = _escenario, VentanaDeposito = _ventanaDeposito };
            var ejecutor = new EjecutorComandosAutenticados(protocolo,
                new RegistroEjecucionArchivo(Path.Combine(sesion.Directorio, "reservas")), autoridad.ConsultarVigenciaAsync,
                async (comando, cancelar) =>
                {
                    if (_actuar is not null)
                    {
                        await _actuar(comando, GuardarEventoAsync, cancelar);
                        return;
                    }
                    if (!await simulada.EnviarAperturaAsync(comando, cancelar)) throw new InvalidOperationException("El simulador no aceptó el comando.");
                    await simulada.EjecutarAsync(comando.IdOrden, cancelar);
                });
            using var servidor = new ServidorMensajesDispositivoHttp(_configuracion.Comandos, (paquete, cancelar) =>
            {
                var comando = protocolo.ValidarComando(paquete);
                if (!comando.Exitoso) return Task.FromResult(RespuestaHttpDispositivo.Rechazar());
                // Recibir no autoriza actuar: la consulta remota ocurre antes de la reserva durable.
                bool aceptar = HayCapacidadDeEventos() && entradas.Agregar(paquete);
                return Task.FromResult(new RespuestaHttpDispositivo(200, protocolo.FirmarRecepcion(_credencial, paquete, aceptar)));
            });
            _registrar(_actuar is null ? "Preparando dispositivo SIMULADO. No hay sensores ni actuadores físicos conectados." :
                "Preparando adaptador USB. Un movimiento PWM no confirma apertura ni ingreso; no se simularán sensores.");
            await CicloBridge.EjecutarAsync(servidor.EjecutarAsync, TrabajarAsync, ct);

            Task<Resultado<EventoRegistrado>> GuardarEventoAsync(EventoRecibido evento, CancellationToken _)
            {
                // No enviar aquí: una caída de red no debe adelantar Apertura a un Acuse aún pendiente.
                if (!salidas.Agregar(protocolo.FirmarEvento(_credencial, evento))) throw new IOException("La cola de eventos no admite más mensajes.");
                return Task.FromResult(Resultado<EventoRegistrado>.Rechazo(MotivoRechazo.EstadoNoPermitido,
                    "Evento simulado en cola durable; aún no confirmado en Oracle."));
            }

            async Task TrabajarAsync(CancellationToken cancelar)
            {
                while (!cancelar.IsCancellationRequested)
                {
                    try
                    {
                        await EnviarEventosAsync(cancelar);
                        var pendiente = entradas.Primero();
                        if (pendiente is not null && HayCapacidadDeEventos())
                        {
                            try
                            {
                                // La actuación dura toda la ventana de depósito: sus eventos no pueden esperar a que termine.
                                var ejecucion = ejecutor.ProcesarAsync(pendiente.Datos, cancelar);
                                while (!ejecucion.IsCompleted)
                                {
                                    try { await EnviarEventosAsync(cancelar); }
                                    catch (Exception error) when (!(error is OperationCanceledException)) { _registrar("Evento pendiente de reenvío; se conserva en la cola local."); }
                                    await Task.WhenAny(ejecucion, Task.Delay(250, cancelar));
                                }
                                var resultado = await ejecucion;
                                if (resultado == ResultadoComandoDispositivo.AccionCompletada) entradas.Confirmar(pendiente);
                                else if (resultado != ResultadoComandoDispositivo.AutorizacionNoDisponible) entradas.ConservarParaRevision(pendiente);
                                _registrar($"Comando de dispositivo: {resultado}; apertura/depósito se acreditan únicamente por los eventos persistidos.");
                            }
                            catch (OperationCanceledException) when (cancelar.IsCancellationRequested) { throw; }
                            catch (Exception)
                            {
                                entradas.ConservarParaRevision(pendiente);
                                _registrar("Ejecución incierta conservada para revisión; no se repite la actuación.");
                            }
                        }
                        await EnviarEventosAsync(cancelar);
                    }
                    catch (OperationCanceledException) when (cancelar.IsCancellationRequested) { break; }
                    catch (Exception) { _registrar("No se completó el ciclo local. Se conservan paquetes y reservas sin reabrir."); }
                    await Task.Delay(TimeSpan.FromMilliseconds(250), cancelar);
                }
            }

            async Task EnviarEventosAsync(CancellationToken cancelar)
            {
                while (!cancelar.IsCancellationRequested && salidas.Primero() is { } pendiente)
                {
                    if (!protocolo.ValidarEvento(pendiente.Datos).Exitoso)
                    {
                        salidas.ConservarParaRevision(pendiente);
                        _registrar("Evento no vigente/no válido conservado para conciliación; no se refirma ni se cambia su hora.");
                        continue;
                    }
                    var recibo = await canalEventos.EnviarPaqueteAsync(pendiente.Datos, cancelar);
                    if (!recibo.Exitoso) break;
                    if (recibo.Valor.Aceptado && recibo.Valor.Registro is { IdEvento: > 0 }) salidas.Confirmar(pendiente);
                    else if (!recibo.Valor.Aceptado) salidas.ConservarParaRevision(pendiente);
                    else break; // Recibir bytes no demuestra que Oracle los haya registrado.
                }
            }
        }
    }

    internal static class CicloBridge
    {
        internal static void ValidarBinding(ConfiguracionBridge configuracion, CredencialDispositivo credencial)
        {
            if (!credencial.Habilitada || credencial.Dispositivo != configuracion.Dispositivo
                || credencial.VersionClave != configuracion.VersionClave || credencial.IdContenedor != configuracion.IdContenedor)
                throw new ArgumentException("La credencial no corresponde al binding del bridge.");
        }

        internal static async Task EjecutarAsync(Func<CancellationToken, Task> escuchar, Func<CancellationToken, Task> trabajar, CancellationToken ct)
        {
            using var parar = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Task servidor = escuchar(parar.Token);
            if (servidor.IsFaulted) await servidor;
            Task trabajador = trabajar(parar.Token);
            try
            {
                await Task.WhenAny(servidor, trabajador);
                parar.Cancel();
                await Task.WhenAll(servidor, trabajador);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            finally { parar.Cancel(); }
        }
    }
}
