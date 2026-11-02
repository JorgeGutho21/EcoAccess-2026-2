using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Mensaje de una caneca ya autenticado por el adaptador (RN-20).</summary>
    /// <param name="IdOrden">Orden que informa el dispositivo; si falta, se usa la orden vigente de la caneca.</param>
    /// <param name="PesoMedidoKg">Peso de la báscula; null si no se midió (RN-12).</param>
    /// <param name="PesoEstimadoKg">Estimación aparte, nunca como peso real.</param>
    public sealed record EventoRecibido(
        int IdContenedor,
        string IdExterno,
        TipoEventoDispositivo Tipo,
        int? IdOrden,
        DateTime? FechaDispositivoUtc,
        decimal? PesoMedidoKg = null,
        decimal? PesoEstimadoKg = null);

    /// <summary>Respuesta al dispositivo: el evento guardado y si cambió algo.</summary>
    /// <param name="Repetido">True si el dispositivo ya había enviado ese identificador; no se procesó otra vez.</param>
    /// <param name="MotivoRevision">Por qué quedó en revisión; null si se aplicó o si es repetido.</param>
    public sealed record EventoRegistrado(int IdEvento, ResultadoEvento Resultado, bool Repetido, string? MotivoRevision = null);

    /// <summary>Hechos persistidos de una operación propia; el depósito no implica verificación del material.</summary>
    public sealed record OperacionApertura(OrdenApertura Orden, Deposito? Deposito);

    /// <summary>
    /// Casos de uso de la apertura de canecas: el ciudadano solicita, recibe una sugerencia, confirma el material y la
    /// caneca recibe una orden; los eventos del dispositivo la avanzan y la detección crea el depósito (CU-05 a CU-09).
    /// También la apertura manual de un operario (CU-17), el vencimiento de órdenes y la verificación del material.
    /// </summary>
    /// <remarks>
    /// El servicio decide los cambios con una lectura previa y el repositorio los guarda solo si nada cambió desde
    /// entonces. Así un evento duplicado o tardío nunca abre dos veces ni crea dos depósitos (RN-07, RN-09, RN-17).
    /// Los métodos de dispositivo y de vencimiento no reciben actor: los llama el adaptador o una tarea del sistema.
    /// </remarks>
    public sealed partial class ServicioInteracciones
    {
        /// <summary>Ventana de una orden: abrir, depositar y cerrar deben ocurrir antes de que venza.</summary>
        public static readonly TimeSpan VigenciaOrden = TimeSpan.FromMinutes(2);

        private readonly ControlAcceso _acceso;
        private readonly IRepositorioInteracciones _interacciones;
        private readonly IRepositorioContenedores _contenedores;
        private readonly IRepositorioMateriales _materiales;
        private readonly TimeProvider _reloj;

        public ServicioInteracciones(
            IRepositorioUsuarios usuarios,
            IRepositorioInteracciones interacciones,
            IRepositorioContenedores contenedores,
            IRepositorioMateriales materiales,
            TimeProvider reloj)
        {
            _acceso = new ControlAcceso(usuarios);
            _interacciones = interacciones ?? throw new ArgumentNullException(nameof(interacciones));
            _contenedores = contenedores ?? throw new ArgumentNullException(nameof(contenedores));
            _materiales = materiales ?? throw new ArgumentNullException(nameof(materiales));
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        }

        private DateTime Ahora => _reloj.GetUtcNow().UtcDateTime;

        /// <summary>Revalida titularidad, habilitación y estado antes de consumir recursos de clasificación externos.</summary>
        public async Task<Resultado> ValidarSolicitudActivaAsync(int idActor, int idInteraccion, CancellationToken ct = default)
        {
            var (_, rechazo) = await CargarPropiaIniciadaAsync(idActor, idInteraccion, ct);
            return rechazo ?? Resultado.Exito();
        }

        /// <summary>Consulta una orden por su clave, comprobando la titularidad de la interacción antes de leerla.</summary>
        public Task<Resultado<OperacionApertura>> ConsultarOperacionAsync(
            int idActor, int idInteraccion, string claveIdempotencia, CancellationToken cancellationToken = default) =>
            ConsultarOperacionDeAsync(idActor, idInteraccion, claveIdempotencia, cancellationToken);

        /// <summary>Consulta la operación de un visitante: quien la creó conserva el id y la clave de la orden.</summary>
        public Task<Resultado<OperacionApertura>> ConsultarOperacionVisitanteAsync(
            int idInteraccion, string claveIdempotencia, CancellationToken cancellationToken = default) =>
            ConsultarOperacionDeAsync(null, idInteraccion, claveIdempotencia, cancellationToken);

        private async Task<Resultado<OperacionApertura>> ConsultarOperacionDeAsync(
            int? idActor, int idInteraccion, string claveIdempotencia, CancellationToken cancellationToken,
            CanalInteraccion canalInvitado = CanalInteraccion.Simulador)
        {
            var (_, rechazo) = await CargarPropiaAsync(idActor, idInteraccion, cancellationToken, canalInvitado);
            if (rechazo is not null)
            {
                return Resultado<OperacionApertura>.DesdeRechazo(rechazo);
            }

            OrdenApertura? orden = await _interacciones.ObtenerOrdenPorClaveAsync(claveIdempotencia, cancellationToken);
            if (orden is null || orden.IdInteraccion != idInteraccion)
            {
                return Resultado<OperacionApertura>.Rechazo(MotivoRechazo.NoEncontrado, "No hay una orden para esa solicitud.");
            }

            Deposito? deposito = await _interacciones.ObtenerDepositoPorOrdenAsync(orden.Id, cancellationToken);
            return Resultado<OperacionApertura>.Exito(new OperacionApertura(orden, deposito));
        }

        /// <summary>El ciudadano solicita abrir una caneca en servicio (CU-05). Devuelve el id de la interacción.</summary>
        public async Task<Resultado<int>> IniciarAsync(
            int idActor,
            int idContenedor,
            CanalInteraccion canal,
            CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirCiudadanoHabilitadoAsync(idActor, cancellationToken) is { } rechazo)
            {
                return Resultado<int>.DesdeRechazo(rechazo);
            }

            return await IniciarDeAsync(idActor, idContenedor, canal, cancellationToken);
        }

        /// <summary>
        /// Un visitante sin cuenta pide usar la caneca DEMO desde la aplicación de escritorio. La interacción queda sin
        /// usuario (id_usuario NULL, canal Simulador): se registra y se traza igual, pero nunca acumula ecopuntos.
        /// </summary>
        public Task<Resultado<int>> IniciarVisitanteAsync(int idContenedor, CancellationToken cancellationToken = default) =>
            IniciarDeAsync(null, idContenedor, CanalInteraccion.Simulador, cancellationToken);

        /// <summary>Solicitud ciudadana con cantidad declarada. No confirma ingreso ni modifica el libro de ecopuntos.</summary>
        public async Task<Resultado<int>> IniciarConCantidadAsync(int idActor, int idContenedor, CanalInteraccion canal,
            int cantidadDeclarada, CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirCiudadanoHabilitadoAsync(idActor, cancellationToken) is { } rechazo)
                return Resultado<int>.DesdeRechazo(rechazo);
            return await IniciarDeAsync(idActor, idContenedor, canal, cancellationToken, cantidadDeclarada);
        }

        private async Task<Resultado<int>> IniciarDeAsync(
            int? idActor, int idContenedor, CanalInteraccion canal, CancellationToken cancellationToken, int? cantidadDeclarada = null)
        {
            Contenedor? caneca = await _contenedores.ObtenerPorIdAsync(idContenedor, cancellationToken);
            if (caneca is null)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.NoEncontrado, "No existe esa caneca.");
            }

            if (!caneca.EstaEnServicio)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesInteracciones.CanecaNoEnServicio);
            }

            Interaccion interaccion;
            try
            {
                interaccion = new Interaccion(idContenedor, idActor, canal, Ahora, cantidadDeclarada);
            }
            catch (ArgumentException error)
            {
                return MensajesValidacion.Rechazo<int>(error);
            }

            return Resultado<int>.Exito(await _interacciones.AgregarInteraccionAsync(interaccion, cancellationToken));
        }

        /// <summary>Guarda la propuesta del clasificador; solo orienta, no autoriza.</summary>
        public Task<Resultado> RegistrarSugerenciaAsync(int idActor, int idInteraccion, int idMaterial, decimal confianza, CancellationToken cancellationToken = default) =>
            RegistrarSugerenciaDeAsync(idActor, idInteraccion, idMaterial, confianza, cancellationToken);

        private async Task<Resultado> RegistrarSugerenciaDeAsync(int? idActor, int idInteraccion, int idMaterial, decimal confianza,
            CancellationToken cancellationToken, CanalInteraccion canalInvitado = CanalInteraccion.Simulador)
        {
            (Interaccion? interaccion, Resultado? rechazo) = await CargarPropiaIniciadaAsync(idActor, idInteraccion, cancellationToken, canalInvitado: canalInvitado);
            if (rechazo is not null)
            {
                return rechazo;
            }

            if (await _materiales.ObtenerPorIdAsync(idMaterial, cancellationToken) is null)
            {
                return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No existe ese material.");
            }

            try
            {
                interaccion!.RegistrarSugerencia(idMaterial, confianza);
            }
            catch (ArgumentException error)
            {
                return MensajesValidacion.Rechazo(error);
            }

            return await GuardarInteraccionAsync(interaccion, cancellationToken);
        }

        /// <summary>
        /// El ciudadano confirma el material y se emite la orden de apertura (CU-06, RN-14). La clave la pone quien
        /// llama y la repite al reintentar: la misma clave devuelve la misma orden sin emitir otra (RN-07).
        /// </summary>
        public Task<Resultado<int>> AutorizarAperturaAsync(
            int idActor,
            int idInteraccion,
            int idMaterialConfirmado,
            string claveIdempotencia,
            CancellationToken cancellationToken = default) =>
            AutorizarAperturaDeAsync(idActor, idInteraccion, idMaterialConfirmado, claveIdempotencia, cancellationToken);

        /// <summary>El visitante confirma el material de su solicitud; mismas reglas de caneca, material y clave.</summary>
        public Task<Resultado<int>> AutorizarAperturaVisitanteAsync(
            int idInteraccion,
            int idMaterialConfirmado,
            string claveIdempotencia,
            CancellationToken cancellationToken = default) =>
            AutorizarAperturaDeAsync(null, idInteraccion, idMaterialConfirmado, claveIdempotencia, cancellationToken);

        private async Task<Resultado<int>> AutorizarAperturaDeAsync(
            int? idActor,
            int idInteraccion,
            int idMaterialConfirmado,
            string claveIdempotencia,
            CancellationToken cancellationToken, CanalInteraccion canalInvitado = CanalInteraccion.Simulador,
            bool exigirDeclaracion = false, int? cantidadEsperada = null)
        {
            (Interaccion? interaccion, Resultado? rechazo) = await CargarPropiaAsync(idActor, idInteraccion, cancellationToken, canalInvitado);
            if (rechazo is not null)
            {
                return Resultado<int>.DesdeRechazo(rechazo);
            }

            if (Compat.IsNullOrWhiteSpace(claveIdempotencia))
            {
                return Resultado<int>.Rechazo(MotivoRechazo.DatosInvalidos, "claveIdempotencia: El valor es obligatorio.");
            }

            string clave = claveIdempotencia.Trim();
            if (exigirDeclaracion && interaccion!.CantidadDeclarada != cantidadEsperada)
                return Resultado<int>.Rechazo(MotivoRechazo.EstadoNoPermitido,
                    "La cantidad declarada cambió. Consulta los puntos potenciales y confirma la declaración actual.");
            if (await _interacciones.ObtenerOrdenPorClaveAsync(clave, cancellationToken) is { } existente)
            {
                return OrdenRepetida(existente, idInteraccion);
            }

            if (!PoliticaVigenciaSolicitud.EstaVigente(interaccion!.FechaInicioUtc, Ahora))
            {
                return Resultado<int>.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesInteracciones.SolicitudVencida);
            }

            if (interaccion!.Estado != EstadoInteraccion.Iniciada)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesInteracciones.InteraccionNoIniciada);
            }

            if (await ComprobarAperturaAsync(interaccion.IdContenedor, idMaterialConfirmado, cancellationToken) is { } noAbre)
            {
                return Resultado<int>.DesdeRechazo(noAbre);
            }

            DateTime ahora = Ahora;
            OrdenApertura orden;
            try
            {
                orden = new OrdenApertura(idInteraccion, interaccion.IdContenedor, clave, ahora, ahora + VigenciaOrden);
            }
            catch (ArgumentException error)
            {
                return MensajesValidacion.Rechazo<int>(error);
            }

            interaccion.Autorizar(idMaterialConfirmado);
            try
            {
                return Resultado<int>.Exito(await _interacciones.EmitirOrdenAsync(interaccion, orden, cancellationToken));
            }
            catch (ConflictoUnicidadException)
            {
                // Otro reintento con la misma clave ganó la carrera.
                OrdenApertura? ganadora = await _interacciones.ObtenerOrdenPorClaveAsync(clave, cancellationToken);
                return ganadora is null
                    ? Resultado<int>.Rechazo(MotivoRechazo.Duplicado, "La clave de la orden ya se usó.")
                    : OrdenRepetida(ganadora, idInteraccion);
            }
            catch (CambioConcurrenteException error)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.EstadoNoPermitido, error.Message);
            }
        }

        /// <summary>El ciudadano desiste o el material no es admitido: la solicitud termina sin orden.</summary>
        public Task<Resultado> RechazarAsync(int idActor, int idInteraccion, string motivo, CancellationToken cancellationToken = default) =>
            RechazarDeAsync(idActor, idInteraccion, motivo, cancellationToken);

        private async Task<Resultado> RechazarDeAsync(int? idActor, int idInteraccion, string motivo, CancellationToken cancellationToken,
            CanalInteraccion canalInvitado = CanalInteraccion.Simulador)
        {
            (Interaccion? interaccion, Resultado? rechazo) = await CargarPropiaIniciadaAsync(idActor, idInteraccion, cancellationToken, exigirVigencia: false, canalInvitado: canalInvitado);
            if (rechazo is not null)
            {
                return rechazo;
            }

            try
            {
                interaccion!.Rechazar(motivo, Ahora);
            }
            catch (ArgumentException error)
            {
                return MensajesValidacion.Rechazo(error);
            }

            return await GuardarInteraccionAsync(interaccion, cancellationToken);
        }

        /// <summary>Consulta autoritativa antes de actuar: autenticar un paquete no sustituye estas reglas.</summary>
        public async Task<bool> SigueAutorizadoComandoAsync(ComandoApertura comando, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(comando, nameof(comando));
            cancellationToken.ThrowIfCancellationRequested();
            if (comando.IdOrden <= 0 || comando.IdContenedor <= 0 || comando.VenceUtc.Kind != DateTimeKind.Utc) return false;
            bool Coincide(OrdenApertura? o) => o is not null && o.Id == comando.IdOrden && o.IdContenedor == comando.IdContenedor
                && o.ClaveIdempotencia == comando.ClaveIdempotencia && o.FechaVencimientoUtc == comando.VenceUtc
                && !o.EstaVencida(Ahora) && o.Estado is EstadoOrdenApertura.Emitida or EstadoOrdenApertura.Enviada;
            var orden = await _interacciones.ObtenerOrdenAsync(comando.IdOrden, cancellationToken);
            if (!Coincide(orden)) return false;
            var interaccion = await _interacciones.ObtenerInteraccionAsync(orden!.IdInteraccion, cancellationToken);
            if (interaccion is not { Estado: EstadoInteraccion.Autorizada, IdMaterialConfirmado: int material }
                || interaccion.IdContenedor != comando.IdContenedor) return false;
            // Visitante: sin ciudadano que revalidar, pero solo por el canal Simulador (caneca DEMO).
            if (interaccion.IdUsuario is int ciudadano)
            {
                if (await _acceso.ExigirCiudadanoHabilitadoAsync(ciudadano, cancellationToken) is not null) return false;
            }
            else if (interaccion.Canal is not (CanalInteraccion.Simulador or CanalInteraccion.Telegram or CanalInteraccion.TelegramSimulado)) return false;
            var caneca = await _contenedores.ObtenerPorIdAsync(comando.IdContenedor, cancellationToken);
            if (caneca is not { Estado: EstadoContenedor.EnServicio } || !caneca.AdmiteMaterial(material)) return false;
            if (await _materiales.ObtenerPorIdAsync(material, cancellationToken) is not { Habilitado: true }) return false;
            // Las consultas previas pudieron tardar o recibir eventos; no rearmar una orden ya acusada/abierta.
            cancellationToken.ThrowIfCancellationRequested();
            return Coincide(await _interacciones.ObtenerOrdenAsync(comando.IdOrden, cancellationToken));
        }

        /// <summary>
        /// El adaptador avisa que entregó la orden a la caneca. Una orden vencida no se reenvía (RN-07); repetir el
        /// aviso de una orden ya enviada no es un error.
        /// </summary>
        public async Task<Resultado> MarcarEnviadaAsync(int idOrden, CancellationToken cancellationToken = default)
        {
            OrdenApertura? orden = await _interacciones.ObtenerOrdenAsync(idOrden, cancellationToken);
            if (orden is null)
            {
                return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No existe esa orden.");
            }

            if (orden.EstaVencida(Ahora))
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, "La orden venció; no se reenvía (RN-07).");
            }

            if (orden.Estado == EstadoOrdenApertura.Enviada)
            {
                return Resultado.Exito();
            }

            if (orden.Estado != EstadoOrdenApertura.Emitida)
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, $"La orden está en estado {orden.Estado}; no se envía otra vez.");
            }

            orden.MarcarEnviada();
            return await GuardarOrdenAsync(new CambioOrden(orden, EstadoOrdenApertura.Emitida), cancellationToken);
        }

        /// <summary>
        /// Procesa un mensaje de la caneca. Siempre se guarda para auditar; si no corresponde a una orden vigente,
        /// está fuera de la ventana o no es un avance válido, queda en revisión sin cambiar nada (RN-08, RN-09).
        /// Una retransmisión con el mismo identificador externo devuelve el evento ya guardado.
        /// </summary>
        public async Task<Resultado<EventoRegistrado>> RegistrarEventoAsync(
            EventoRecibido recibido,
            CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(recibido, nameof(recibido));
            string idExterno = recibido.IdExterno?.Trim() ?? string.Empty;
            if (idExterno.Length is 0 or > EventoDispositivo.LongitudMaximaIdExterno)
            {
                return Resultado<EventoRegistrado>.Rechazo(
                    MotivoRechazo.DatosInvalidos,
                    $"idExterno: El valor debe tener entre 1 y {EventoDispositivo.LongitudMaximaIdExterno} caracteres.");
            }

            if (!Compat.IsDefined(recibido.Tipo))
            {
                return Resultado<EventoRegistrado>.Rechazo(MotivoRechazo.DatosInvalidos, "tipo: El tipo de evento no existe.");
            }

            if (recibido.FechaDispositivoUtc is { Kind: not DateTimeKind.Utc })
            {
                return Resultado<EventoRegistrado>.Rechazo(MotivoRechazo.DatosInvalidos, "fechaDispositivoUtc: La fecha debe estar en UTC.");
            }

            if (await _contenedores.ObtenerPorIdAsync(recibido.IdContenedor, cancellationToken) is null)
            {
                return Resultado<EventoRegistrado>.Rechazo(MotivoRechazo.NoEncontrado, "No existe esa caneca.");
            }

            if (await _interacciones.ObtenerEventoAsync(recibido.IdContenedor, idExterno, cancellationToken) is { } previo)
            {
                return Resultado<EventoRegistrado>.Exito(new EventoRegistrado(previo.Id, previo.Resultado, Repetido: true));
            }

            DateTime ahora = Ahora;
            Decision decision = await DecidirAsync(recibido, ahora, cancellationToken);
            try
            {
                return Resultado<EventoRegistrado>.Exito(await GuardarEventoAsync(recibido, idExterno, decision, ahora, cancellationToken));
            }
            catch (CambioConcurrenteException)
            {
                // Otro evento cambió la orden entre la lectura y la escritura: se decide una vez más con el estado nuevo.
            }

            decision = await DecidirAsync(recibido, ahora, cancellationToken);
            try
            {
                return Resultado<EventoRegistrado>.Exito(await GuardarEventoAsync(recibido, idExterno, decision, ahora, cancellationToken));
            }
            catch (CambioConcurrenteException)
            {
                // Si vuelve a cambiar, el evento queda para revisión en lugar de reintentar sin fin.
                var revision = Decision.Revision(decision.IdOrden, "La orden cambió mientras se procesaba el evento.");
                return Resultado<EventoRegistrado>.Exito(await GuardarEventoAsync(recibido, idExterno, revision, ahora, cancellationToken));
            }
        }

        /// <summary>
        /// Termina las órdenes vigentes cuyo vencimiento llegó y cierra sin depósito las interacciones que esperaban.
        /// Lo llama una tarea periódica; devuelve cuántas órdenes venció. Una orden que otro evento cambió se omite.
        /// </summary>
        public async Task<int> VencerOrdenesAsync(CancellationToken cancellationToken = default, int? idContenedor = null)
        {
            if (idContenedor is <= 0) throw new ArgumentOutOfRangeException(nameof(idContenedor));
            DateTime ahora = Ahora;
            int vencidas = 0;
            IReadOnlyList<OrdenApertura> candidatas;
            if (idContenedor is int id)
            {
                var actual = await _interacciones.ObtenerOrdenVigenteAsync(id, cancellationToken);
                candidatas = actual is not null && actual.EstaVencida(ahora) ? new OrdenApertura[] { actual } : Array.Empty<OrdenApertura>();
            }
            else candidatas = await _interacciones.ListarOrdenesVencidasAsync(ahora, cancellationToken);
            foreach (OrdenApertura orden in candidatas)
            {
                EstadoOrdenApertura anterior = orden.Estado;
                Interaccion interaccion = await _interacciones.ObtenerInteraccionAsync(orden.IdInteraccion, cancellationToken)
                    ?? throw new InvalidOperationException($"La orden {orden.Id} apunta a una interacción inexistente.");
                orden.Vencer(ahora);
                Interaccion? cerrada = CerrarSinDepositoSiEspera(interaccion, ahora);
                try
                {
                    await _interacciones.ActualizarOrdenAsync(new CambioOrden(orden, anterior, cerrada), cancellationToken);
                    vencidas++;
                }
                catch (CambioConcurrenteException)
                {
                    // Un evento la avanzó o la cerró mientras tanto; la próxima pasada la revisa de nuevo.
                }
            }

            return vencidas;
        }

        /// <summary>
        /// Un operario abre la caneca a mano por mantenimiento o soporte (CU-17). Queda registrada, cerrada y sin
        /// depósito automático. No se permite mientras la caneca atiende una orden, para no confundir sus eventos.
        /// </summary>
        public async Task<Resultado<int>> RegistrarAperturaManualAsync(
            int idActor,
            int idContenedor,
            string motivo,
            CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirAlgunRolAsync(idActor, cancellationToken, CodigoRol.Operario, CodigoRol.Administrador) is { } rechazo)
            {
                return Resultado<int>.DesdeRechazo(rechazo);
            }

            Contenedor? caneca = await _contenedores.ObtenerPorIdAsync(idContenedor, cancellationToken);
            if (caneca is null)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.NoEncontrado, "No existe esa caneca.");
            }

            if (caneca.Estado == EstadoContenedor.DadoDeBaja)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesCatalogo.CanecaDadaDeBaja);
            }

            if (await _interacciones.ObtenerOrdenVigenteAsync(idContenedor, cancellationToken) is not null)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesInteracciones.CanecaOcupada);
            }

            Interaccion manual;
            try
            {
                manual = Interaccion.AperturaManual(idContenedor, idActor, motivo, Ahora);
            }
            catch (ArgumentException error)
            {
                return MensajesValidacion.Rechazo<int>(error);
            }

            return Resultado<int>.Exito(await _interacciones.AgregarInteraccionAsync(manual, cancellationToken));
        }

        /// <summary>
        /// Un operador atiende a un ciudadano desde el escritorio: crea la solicitud a nombre del ciudadano y autoriza con
        /// las mismas reglas de caneca, material y exclusión por contenedor que cualquier otro canal.
        /// </summary>
        public async Task<Resultado<int>> RegistrarAsistidoAsync(int idOperador, int idCiudadano, int idContenedor, int idMaterial, CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirAlgunRolAsync(idOperador, cancellationToken, CodigoRol.Operario, CodigoRol.Administrador) is { } rechazo)
            {
                return Resultado<int>.DesdeRechazo(rechazo);
            }

            Resultado<int> inicio = await IniciarAsync(idCiudadano, idContenedor, CanalInteraccion.Asistido, cancellationToken);
            if (!inicio.Exitoso)
            {
                return inicio;
            }

            return await AutorizarAperturaAsync(idCiudadano, inicio.Valor, idMaterial,
                FormattableString.Invariant($"asistido:{inicio.Valor}:{idMaterial}"), cancellationToken);
        }

        /// <summary>Estado de una orden y de su depósito, para que el operador siga la operación que atiende.</summary>
        public async Task<Resultado<OperacionApertura>> ConsultarOperacionAsync(int idActor, int idOrden, CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirAlgunRolAsync(idActor, cancellationToken, CodigoRol.Operario, CodigoRol.Administrador) is { } rechazo)
            {
                return Resultado<OperacionApertura>.DesdeRechazo(rechazo);
            }

            OrdenApertura? orden = await _interacciones.ObtenerOrdenAsync(idOrden, cancellationToken);
            return orden is null
                ? Resultado<OperacionApertura>.Rechazo(MotivoRechazo.NoEncontrado, "No existe esa orden.")
                : Resultado<OperacionApertura>.Exito(new OperacionApertura(orden, await _interacciones.ObtenerDepositoPorOrdenAsync(idOrden, cancellationToken)));
        }

        /// <summary>
        /// Un operario o administrador fija el material real de un depósito (CU-10, RN-11). Nadie verifica su propio
        /// depósito (RN-21).
        /// </summary>
        public async Task<Resultado> VerificarDepositoAsync(
            int idActor,
            int idDeposito,
            int idMaterialVerificado,
            CancellationToken cancellationToken = default,
            int? cantidadVerificada = null)
        {
            if (await _acceso.ExigirAlgunRolAsync(idActor, cancellationToken, CodigoRol.Operario, CodigoRol.Administrador) is { } rechazo)
            {
                return rechazo;
            }

            Deposito? deposito = await _interacciones.ObtenerDepositoAsync(idDeposito, cancellationToken);
            if (deposito is null)
            {
                return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No existe ese depósito.");
            }

            if (deposito.EstaVerificado)
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesInteracciones.DepositoYaVerificado);
            }

            OrdenApertura orden = await _interacciones.ObtenerOrdenAsync(deposito.IdOrden, cancellationToken)
                ?? throw new InvalidOperationException($"El depósito {idDeposito} apunta a una orden inexistente.");
            Interaccion interaccion = await _interacciones.ObtenerInteraccionAsync(orden.IdInteraccion, cancellationToken)
                ?? throw new InvalidOperationException($"La orden {orden.Id} apunta a una interacción inexistente.");
            if (interaccion.IdUsuario == idActor)
            {
                return Resultado.Rechazo(MotivoRechazo.SinPermiso, "Nadie puede verificar su propio depósito (RN-21).");
            }

            if (await _materiales.ObtenerPorIdAsync(idMaterialVerificado, cancellationToken) is null)
            {
                return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No existe ese material.");
            }

            try
            {
                deposito.Verificar(idMaterialVerificado, idActor, Ahora, cantidadVerificada);
            }
            catch (ArgumentException error)
            {
                return MensajesValidacion.Rechazo(error);
            }

            try
            {
                await _interacciones.VerificarDepositoAsync(deposito, cancellationToken);
                return Resultado.Exito();
            }
            catch (CambioConcurrenteException error)
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, error.Message);
            }
        }

        public async Task<Resultado<Interaccion>> ObtenerAsync(int idActor, int idInteraccion, CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirAlgunRolAsync(idActor, cancellationToken, CodigoRol.Ciudadano, CodigoRol.Operario, CodigoRol.Administrador) is { } rechazo)
            {
                return Resultado<Interaccion>.DesdeRechazo(rechazo);
            }

            Interaccion? interaccion = await _interacciones.ObtenerInteraccionAsync(idInteraccion, cancellationToken);
            if (interaccion is null)
            {
                return Resultado<Interaccion>.Rechazo(MotivoRechazo.NoEncontrado, "No existe esa solicitud.");
            }

            // Un ciudadano solo ve las suyas; el personal ve todas.
            if (interaccion.IdUsuario != idActor
                && await _acceso.ExigirAlgunRolAsync(idActor, cancellationToken, CodigoRol.Operario, CodigoRol.Administrador) is { } ajena)
            {
                return Resultado<Interaccion>.DesdeRechazo(ajena);
            }

            return Resultado<Interaccion>.Exito(interaccion);
        }

        /// <summary>Solicitudes del propio ciudadano, de la más reciente a la más antigua.</summary>
        public async Task<Resultado<IReadOnlyList<Interaccion>>> ListarMiasAsync(int idActor, CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Ciudadano, cancellationToken) is { } rechazo)
            {
                return Resultado<IReadOnlyList<Interaccion>>.DesdeRechazo(rechazo);
            }

            return Resultado<IReadOnlyList<Interaccion>>.Exito(
                await _interacciones.ListarInteraccionesPorUsuarioAsync(idActor, cancellationToken));
        }

        /// <summary>Depósitos del propio ciudadano, del más reciente al más antiguo.</summary>
        public async Task<Resultado<IReadOnlyList<Deposito>>> ListarMisDepositosAsync(int idActor, CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Ciudadano, cancellationToken) is { } rechazo)
            {
                return Resultado<IReadOnlyList<Deposito>>.DesdeRechazo(rechazo);
            }

            return Resultado<IReadOnlyList<Deposito>>.Exito(
                await _interacciones.ListarDepositosPorUsuarioAsync(idActor, cancellationToken));
        }

        public Task<Resultado<IReadOnlyList<Interaccion>>> ListarPorContenedorAsync(
            int idActor,
            int idContenedor,
            CancellationToken cancellationToken = default) =>
            ListarComoPersonalAsync(idActor, () => _interacciones.ListarInteraccionesPorContenedorAsync(idContenedor, cancellationToken), cancellationToken);

        public Task<Resultado<IReadOnlyList<EventoDispositivo>>> ListarEventosEnRevisionAsync(
            int idActor,
            CancellationToken cancellationToken = default) =>
            ListarComoPersonalAsync(idActor, () => _interacciones.ListarEventosEnRevisionAsync(cancellationToken), cancellationToken);

        public Task<Resultado<IReadOnlyList<Deposito>>> ListarDepositosSinVerificarAsync(
            int idActor,
            CancellationToken cancellationToken = default) =>
            ListarComoPersonalAsync(idActor, () => _interacciones.ListarDepositosSinVerificarAsync(cancellationToken), cancellationToken);

        private async Task<Resultado<IReadOnlyList<T>>> ListarComoPersonalAsync<T>(
            int idActor,
            Func<Task<IReadOnlyList<T>>> listar,
            CancellationToken cancellationToken)
        {
            if (await _acceso.ExigirAlgunRolAsync(idActor, cancellationToken, CodigoRol.Operario, CodigoRol.Administrador) is { } rechazo)
            {
                return Resultado<IReadOnlyList<T>>.DesdeRechazo(rechazo);
            }

            return Resultado<IReadOnlyList<T>>.Exito(await listar());
        }

        /// <summary>
        /// Ciudadano habilitado y dueño de la interacción. Con <paramref name="idActor"/> null es un visitante: solo
        /// alcanza solicitudes sin usuario hechas por el canal Simulador, nunca las de un ciudadano ni las de Telegram.
        /// </summary>
        private async Task<(Interaccion? Interaccion, Resultado? Rechazo)> CargarPropiaAsync(
            int? idActor,
            int idInteraccion,
            CancellationToken cancellationToken, CanalInteraccion canalInvitado = CanalInteraccion.Simulador)
        {
            if (idActor is { } ciudadano && await _acceso.ExigirCiudadanoHabilitadoAsync(ciudadano, cancellationToken) is { } rechazo)
            {
                return (null, rechazo);
            }

            Interaccion? interaccion = await _interacciones.ObtenerInteraccionAsync(idInteraccion, cancellationToken);
            if (interaccion is null)
            {
                return (null, Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No existe esa solicitud."));
            }

            if (interaccion.IdUsuario != idActor || (idActor is null && interaccion.Canal != canalInvitado))
            {
                return (null, Resultado.Rechazo(MotivoRechazo.SinPermiso, "La solicitud pertenece a otra persona."));
            }

            return (interaccion, null);
        }

        private async Task<(Interaccion? Interaccion, Resultado? Rechazo)> CargarPropiaIniciadaAsync(
            int? idActor,
            int idInteraccion,
            CancellationToken cancellationToken, bool exigirVigencia = true, CanalInteraccion canalInvitado = CanalInteraccion.Simulador)
        {
            (Interaccion? interaccion, Resultado? rechazo) = await CargarPropiaAsync(idActor, idInteraccion, cancellationToken, canalInvitado);
            if (rechazo is null && interaccion!.Estado != EstadoInteraccion.Iniciada)
            {
                return (null, Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesInteracciones.InteraccionNoIniciada));
            }

            if (rechazo is null && exigirVigencia && !PoliticaVigenciaSolicitud.EstaVigente(interaccion!.FechaInicioUtc, Ahora))
            {
                return (null, Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesInteracciones.SolicitudVencida));
            }
            return (interaccion, rechazo);
        }

        /// <summary>Lectura previa de RN-05, RN-06 y RN-16; el repositorio las repite con bloqueo al emitir.</summary>
        private async Task<Resultado?> ComprobarAperturaAsync(int idContenedor, int idMaterial, CancellationToken cancellationToken)
        {
            Contenedor caneca = await _contenedores.ObtenerPorIdAsync(idContenedor, cancellationToken)
                ?? throw new InvalidOperationException($"La interacción apunta a la caneca {idContenedor}, que no existe.");
            if (!caneca.EstaEnServicio)
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesInteracciones.CanecaNoEnServicio);
            }

            Material? material = await _materiales.ObtenerPorIdAsync(idMaterial, cancellationToken);
            if (material is null)
            {
                return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No existe ese material.");
            }

            if (!material.Habilitado)
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesCatalogo.MaterialDeshabilitado(material.Nombre));
            }

            if (material.Categoria == CategoriaMaterial.Peligroso)
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, "La caneca piloto no admite residuos de gestión especial. Consulta su ruta indicada.");

            if (!caneca.AdmiteMaterial(idMaterial))
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesInteracciones.CanecaNoAdmiteMaterial);
            }

            if (await _interacciones.ObtenerOrdenVigenteAsync(idContenedor, cancellationToken) is not null)
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesInteracciones.CanecaOcupada);
            }

            return null;
        }

        /// <summary>
        /// Repetir la clave devuelve la misma orden solo mientras siga vigente: una orden vencida o cerrada no debe
        /// anunciarse otra vez como apertura en curso.
        /// </summary>
        private Resultado<int> OrdenRepetida(OrdenApertura existente, int idInteraccion)
        {
            if (existente.IdInteraccion != idInteraccion)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.Duplicado, "La clave de la orden ya se usó en otra solicitud.");
            }

            return existente.EsVigente && !existente.EstaVencida(Ahora)
                ? Resultado<int>.Exito(existente.Id)
                : Resultado<int>.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesInteracciones.OrdenNoVigente);
        }

        private async Task<Resultado> GuardarInteraccionAsync(Interaccion interaccion, CancellationToken cancellationToken)
        {
            try
            {
                await _interacciones.ActualizarInteraccionAsync(interaccion, EstadoInteraccion.Iniciada, cancellationToken);
                return Resultado.Exito();
            }
            catch (CambioConcurrenteException error)
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, error.Message);
            }
        }

        private async Task<Resultado> GuardarOrdenAsync(CambioOrden cambio, CancellationToken cancellationToken)
        {
            try
            {
                await _interacciones.ActualizarOrdenAsync(cambio, cancellationToken);
                return Resultado.Exito();
            }
            catch (CambioConcurrenteException error)
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, error.Message);
            }
        }

        /// <summary>Cierre o fallo antes de detectar: la interacción que esperaba termina sin depósito.</summary>
        private static Interaccion? CerrarSinDepositoSiEspera(Interaccion interaccion, DateTime ahora)
        {
            if (interaccion.Estado != EstadoInteraccion.Autorizada)
            {
                return null;
            }

            interaccion.CerrarSinDeposito(ahora);
            return interaccion;
        }

        private async Task<EventoRegistrado> GuardarEventoAsync(
            EventoRecibido recibido,
            string idExterno,
            Decision decision,
            DateTime ahora,
            CancellationToken cancellationToken)
        {
            ResultadoEvento resultado = decision.MotivoRevision is null ? ResultadoEvento.Aplicado : ResultadoEvento.Revision;
            var evento = new EventoDispositivo(
                recibido.IdContenedor, idExterno, recibido.Tipo, decision.IdOrden, resultado, recibido.FechaDispositivoUtc, ahora);
            try
            {
                int id = await _interacciones.RegistrarEventoAsync(evento, decision.Cambio, decision.Deposito, cancellationToken);
                return new EventoRegistrado(id, resultado, Repetido: false, decision.MotivoRevision);
            }
            catch (ConflictoUnicidadException)
            {
                // La misma retransmisión llegó dos veces a la vez; la otra la guardó.
                EventoDispositivo previo = await _interacciones.ObtenerEventoAsync(recibido.IdContenedor, idExterno, cancellationToken)
                    ?? throw new InvalidOperationException("El evento se rechazó por repetido pero no se encuentra.");
                return new EventoRegistrado(previo.Id, previo.Resultado, Repetido: true);
            }
        }

        /// <summary>Qué hacer con un evento: aplicarlo con un cambio (y quizá un depósito) o dejarlo en revisión.</summary>
        private sealed record Decision(int? IdOrden, CambioOrden? Cambio, Deposito? Deposito, string? MotivoRevision)
        {
            public static Decision Revision(int? idOrden, string motivo) => new(idOrden, null, null, motivo);
        }

        private async Task<Decision> DecidirAsync(EventoRecibido recibido, DateTime ahora, CancellationToken cancellationToken)
        {
            OrdenApertura? orden = recibido.IdOrden is { } idOrden
                ? await _interacciones.ObtenerOrdenAsync(idOrden, cancellationToken)
                : await _interacciones.ObtenerOrdenVigenteAsync(recibido.IdContenedor, cancellationToken);
            if (orden is null)
            {
                return Decision.Revision(null, "No hay una orden vigente para ese evento.");
            }

            if (orden.IdContenedor != recibido.IdContenedor)
            {
                return Decision.Revision(null, "La orden pertenece a otra caneca.");
            }

            if (!orden.EsVigente)
            {
                return Decision.Revision(orden.Id, $"La orden ya terminó en estado {orden.Estado}.");
            }

            // Abrir y depositar solo cuentan dentro de la ventana (RN-08); cerrar o fallar se registra mientras siga vigente.
            bool requiereVentana = recibido.Tipo is TipoEventoDispositivo.Acuse or TipoEventoDispositivo.Apertura or TipoEventoDispositivo.Deteccion;
            if (requiereVentana && orden.EstaVencida(ahora))
            {
                return Decision.Revision(orden.Id, "El evento llegó después del vencimiento de la orden.");
            }

            Interaccion interaccion = await _interacciones.ObtenerInteraccionAsync(orden.IdInteraccion, cancellationToken)
                ?? throw new InvalidOperationException($"La orden {orden.Id} apunta a una interacción inexistente.");
            EstadoOrdenApertura anterior = orden.Estado;

            switch (recibido.Tipo)
            {
                case TipoEventoDispositivo.Acuse when orden.PuedeAvanzarA(EstadoOrdenApertura.Acusada):
                    orden.RegistrarAcuse();
                    return new Decision(orden.Id, new CambioOrden(orden, anterior), null, null);

                case TipoEventoDispositivo.Apertura when orden.PuedeAvanzarA(EstadoOrdenApertura.Abierta):
                    orden.RegistrarApertura();
                    return new Decision(orden.Id, new CambioOrden(orden, anterior), null, null);

                // Solo se detecta con la tapa abierta: la apertura física y la detección son evidencias distintas.
                case TipoEventoDispositivo.Deteccion
                    when orden.Estado == EstadoOrdenApertura.Abierta && interaccion.Estado == EstadoInteraccion.Autorizada:
                    Deposito deposito;
                    try
                    {
                        deposito = new Deposito(orden.Id, interaccion.IdMaterialConfirmado!.Value, recibido.PesoMedidoKg, recibido.PesoEstimadoKg, ahora);
                    }
                    catch (ArgumentException)
                    {
                        // Un sensor descalibrado no debe perder la evidencia: el evento queda para que un operario lo revise.
                        return Decision.Revision(orden.Id, "El peso informado no es válido.");
                    }

                    orden.RegistrarDeteccion();
                    interaccion.CerrarConDeposito(ahora);
                    return new Decision(orden.Id, new CambioOrden(orden, anterior, interaccion), deposito, null);

                // Sin sensor no hay detección: se conserva como pendiente y solo un operario puede verificarla.

                case TipoEventoDispositivo.CierreSinSensor
                    when orden.Estado == EstadoOrdenApertura.Abierta && interaccion.Estado == EstadoInteraccion.Autorizada:
                    var pendiente = new Deposito(orden.Id, interaccion.IdMaterialConfirmado!.Value, null, null, ahora, sinSensor: true);
                    orden.Cerrar(ahora);
                    interaccion.CerrarConDeposito(ahora);
                    return new Decision(orden.Id, new CambioOrden(orden, anterior, interaccion), pendiente, null);


                case TipoEventoDispositivo.Cierre:
                case TipoEventoDispositivo.CierreSinSensor:
                    orden.Cerrar(ahora);
                    return new Decision(orden.Id, new CambioOrden(orden, anterior, CerrarSinDepositoSiEspera(interaccion, ahora)), null, null);

                case TipoEventoDispositivo.Fallo:
                    orden.Fallar(ahora);
                    return new Decision(orden.Id, new CambioOrden(orden, anterior, CerrarSinDepositoSiEspera(interaccion, ahora)), null, null);

                default:
                    return Decision.Revision(orden.Id, $"Un evento {recibido.Tipo} no es un avance válido desde {orden.Estado}.");
            }
        }
    }
}
