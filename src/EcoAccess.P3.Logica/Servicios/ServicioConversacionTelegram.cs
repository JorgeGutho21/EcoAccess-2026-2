using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Mensaje que el adaptador del bot recibió de Telegram, ya reducido a lo que el servicio necesita.
    /// </summary>
    /// <param name="UpdateId">Identificador del update de Telegram; sirve para descartar reenvíos.</param>
    /// <param name="ChatPrivado">True si el chat es privado; los grupos se ignoran.</param>
    /// <param name="Texto">Texto escrito o comando; null si fue un botón.</param>
    /// <param name="DatoBoton">Dato del botón pulsado (callback_data); null si fue texto.</param>
    public sealed record MensajeEntrante(long UpdateId, long TelegramUserId, long ChatId, bool ChatPrivado, string? Texto, string? DatoBoton,
        string? ArchivoFoto = null);

    /// <summary>Botón en línea: el texto que ve la persona y el dato que vuelve al pulsarlo.</summary>
    public sealed record BotonBot(string Texto, string Dato);

    /// <summary>Respuesta que el adaptador envía al chat.</summary>
    public sealed record RespuestaBot(long ChatId, string Texto, IReadOnlyList<BotonBot> Botones)
    {
        public RespuestaBot(long chatId, string texto) : this(chatId, texto, Array.Empty<BotonBot>()) { }

        /// <summary>Botones por fila en el teclado de Telegram (el menú usa dos para caber en un celular).</summary>
        public int Columnas { get; init; } = 1;
        /// <summary>Datos ya autorizados para que el adaptador renderice la gráfica; la lógica no conoce PNG ni Telegram HTTP.</summary>
        public EstadisticasPersonales? Estadisticas { get; init; }
    }

    /// <summary>
    /// Diálogo del bot de Telegram: escanear el QR, elegir el material, confirmar y emitir la orden.
    /// No toca Telegram ni la caneca: recibe un <see cref="MensajeEntrante"/>, aplica las reglas con los servicios de
    /// vinculación e interacciones y devuelve las respuestas. Así se prueba entero sin red.
    /// </summary>
    /// <remarks>
    /// Cada mensaje se procesa en este orden: se descartan los grupos, los updates repetidos (Telegram reenvía si no
    /// recibe confirmación) y el exceso de mensajes; luego se aplica la transición y se guarda la conversación con
    /// versión optimista. Los datos de los botones vienen del cliente y pueden alterarse, así que el material se
    /// vuelve a comprobar contra la caneca. La orden la emite <see cref="ServicioInteracciones"/>, que valida de nuevo
    /// RN-05, RN-06 y RN-16; la clave de idempotencia es fija por interacción y material, de modo que pulsar
    /// «Sí» dos veces devuelve la misma orden (RN-07). El vínculo con Telegram cumple el contacto verificado de RN-02.
    /// </remarks>
    public sealed partial class ServicioConversacionTelegram
    {
        /// <summary>Tras este tiempo sin mensajes, un botón viejo ya no actúa sobre la solicitud.</summary>
        public static readonly TimeSpan VigenciaConversacion = PoliticaVigenciaSolicitud.Vigencia;

        public const string PrefijoMaterial = "mat:";
        public const string ConfirmarSi = "conf:si";
        public const string ConfirmarNo = "conf:no";
        public const string PrefijoConsentirFoto = "foto:consentir:";

        /// <summary>Navegación del menú: solo traduce a un comando existente, nunca autoriza nada por sí misma.</summary>
        public const string PrefijoMenu = "menu:";
        public const string FotoPrevia = "foto:previa";

        /// <summary>Menú principal; cada opción reutiliza el comando equivalente.</summary>
        public static readonly IReadOnlyList<BotonBot> MenuPrincipal =
        new BotonBot[]
    {
        new("📦 Registrar residuo", PrefijoMenu + "depositar"),
        new("📊 Mi actividad", PrefijoMenu + "/estadisticas"),
        new("🧾 Mi historial", PrefijoMenu + "/depositos"),
        new("🌱 Mis ecopuntos", PrefijoMenu + "/ecopuntos"),
        new("🌎 Educación ambiental", PrefijoMenu + "/categorias"),
        new("🌿 Próximamente", PrefijoMenu + "/proximamente"),
        new("ℹ️ Acerca de EcoAccess", PrefijoMenu + "/acerca"),
        new("⭐ Calificar experiencia", PrefijoMenu + "/calificar")
    };

        private static readonly BotonBot BotonMenu = new("🏠 Menú principal", PrefijoMenu + "inicio");
        private static readonly BotonBot BotonCancelar = new("✖️ Cancelar", PrefijoMenu + "/cancelar");
        private static readonly BotonBot BotonEstado = new("📍 Consultar estado", PrefijoMenu + "/estado");

        /// <summary>Botón de consulta del catálogo: solo informa, no cambia la solicitud en curso.</summary>
        public const string PrefijoInfo = "info:";

        /// <summary>Cuántas operaciones o depósitos muestra una consulta por chat.</summary>
        public const int MaximoConsulta = 5;

        private const string MotivoCancelacion = "Cancelada por el ciudadano en Telegram.";
        private const string MotivoReemplazo = "Reemplazada por un nuevo escaneo en Telegram.";

        private readonly IRepositorioConversacionesTelegram _conversaciones;
        private readonly IRepositorioContenedores _contenedores;
        private readonly IRepositorioMateriales _materiales;
        private readonly ServicioVinculacionTelegram _vinculacion;
        private readonly ServicioInvitadosTelegram _invitados;
        private readonly IRepositorioAnalisisImagen? _analisis;
        private readonly ServicioInteracciones _interacciones;
        private readonly ServicioCatalogoAmbiental _catalogo;
        private readonly ILimitadorUso _limitador;
        private readonly Uri? _paginaRegistro;
        private readonly TimeProvider _reloj;
        private readonly ServicioDespachoOrdenes? _despacho;
        private readonly ServicioEcopuntos? _ecopuntos;
        private readonly ServicioEstadisticas? _estadisticas;
        private readonly ServicioOpiniones? _opiniones;
        private readonly ServicioClasificacionImagen? _vision;
        private readonly Func<string, CancellationToken, Task<ImagenResiduo>>? _descargarFoto;
        private readonly string? _avisoFotografias;
        private readonly bool _requiereConsentimientoFoto;

        /// <summary>
        /// Con una identificación suficiente de un material admitido, la lógica autoriza sin más pasos. La visión solo
        /// propone: la regla de admisión, el estado de la caneca y la exclusión por contenedor siguen decidiendo aquí.
        /// </summary>
        private readonly bool _aperturaDirecta;
        private readonly string? _qrDemo;
        private readonly Dictionary<long, FotoPendiente> _fotosPendientes = new Dictionary<long, FotoPendiente>();
        private sealed record FotoPendiente(string Archivo, string Nonce, int Interaccion, long Chat, int Version, DateTime HastaUtc);
        // Foto recibida antes del QR: solo la referencia de Telegram, sin descargar, durante 10 minutos.
        private readonly Dictionary<long, (string Archivo, DateTime HastaUtc)> _fotosSinCaneca = new Dictionary<long, (string Archivo, DateTime HastaUtc)>();
        private readonly ILimitadorUso _fotosPorUsuario = new LimitadorUsoEnMemoria(3, TimeSpan.FromMinutes(1));
        private readonly ILimitadorUso _fotosGlobales = new LimitadorUsoEnMemoria(20, TimeSpan.FromMinutes(1));
        // Bandas acotadas: una misma cuenta se procesa en orden, sin un diccionario creciente de usuarios.
        private readonly SemaphoreSlim[] _turnos = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

        public ServicioConversacionTelegram(
            IRepositorioConversacionesTelegram conversaciones,
            IRepositorioContenedores contenedores,
            IRepositorioMateriales materiales,
            ServicioVinculacionTelegram vinculacion,
            ServicioInteracciones interacciones,
            ILimitadorUso limitador,
            Uri? paginaRegistro,
            TimeProvider reloj,
            ServicioDespachoOrdenes? despacho = null,
            ServicioEcopuntos? ecopuntos = null,
            ServicioClasificacionImagen? vision = null,
            Func<string, CancellationToken, Task<ImagenResiduo>>? descargarFoto = null,
            string? avisoFotografias = null,
            bool requiereConsentimientoFoto = false,
            string? qrDemo = null,
            IRepositorioAnalisisImagen? analisis = null,
            ServicioEstadisticas? estadisticas = null,
            ServicioOpiniones? opiniones = null,
            bool aperturaDirecta = false)
        {
            _conversaciones = conversaciones ?? throw new ArgumentNullException(nameof(conversaciones));
            _contenedores = contenedores ?? throw new ArgumentNullException(nameof(contenedores));
            _materiales = materiales ?? throw new ArgumentNullException(nameof(materiales));
            _vinculacion = vinculacion ?? throw new ArgumentNullException(nameof(vinculacion));
            _interacciones = interacciones ?? throw new ArgumentNullException(nameof(interacciones));
            // El catálogo usa los mismos repositorios y no tiene estado propio: se arma aquí para no cambiar la firma pública.
            _catalogo = new ServicioCatalogoAmbiental(materiales, contenedores);
            _limitador = limitador ?? throw new ArgumentNullException(nameof(limitador));
            if (paginaRegistro is not null && (!paginaRegistro.IsAbsoluteUri || paginaRegistro.Scheme != Uri.UriSchemeHttps
                || paginaRegistro.UserInfo.Length != 0 || paginaRegistro.Query.Length != 0 || paginaRegistro.Fragment.Length != 0))
                throw new ArgumentException("La página de vinculación debe ser HTTPS sin credenciales ni parámetros.", nameof(paginaRegistro));
            _paginaRegistro = paginaRegistro;
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
            _invitados = new ServicioInvitadosTelegram(conversaciones, interacciones, reloj);
            _analisis = analisis;
            _despacho = despacho;
            _ecopuntos = ecopuntos;
            _estadisticas = estadisticas;
            _opiniones = opiniones;
            _vision = vision;
            _descargarFoto = descargarFoto;
            _avisoFotografias = avisoFotografias;
            _requiereConsentimientoFoto = requiereConsentimientoFoto;
            _aperturaDirecta = aperturaDirecta;
            // Solo el entorno de pruebas lo configura (Telegram:QrDemo); nunca un identificador de producción.
            _qrDemo = Compat.IsNullOrWhiteSpace(qrDemo) ? null : qrDemo.Trim();
        }

        private DateTime Ahora => _reloj.GetUtcNow().UtcDateTime;

        /// <summary>Procesa un mensaje y devuelve lo que hay que responder (vacío si se descarta).</summary>
        public async Task<IReadOnlyList<RespuestaBot>> ProcesarAsync(MensajeEntrante mensaje, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(mensaje, nameof(mensaje));

            if (!mensaje.ChatPrivado || mensaje.ChatId != mensaje.TelegramUserId || mensaje.TelegramUserId <= 0)
            {
                return Array.Empty<RespuestaBot>();
            }

            SemaphoreSlim turno = _turnos[(int)(mensaje.TelegramUserId % _turnos.Length)];
            await turno.WaitAsync(cancellationToken);
            try { return await ProcesarEnOrdenAsync(mensaje, cancellationToken); }
            finally { turno.Release(); }
        }

        private async Task<IReadOnlyList<RespuestaBot>> ProcesarEnOrdenAsync(MensajeEntrante mensaje, CancellationToken cancellationToken)
        {

            if (!await _conversaciones.RegistrarUpdateAsync(mensaje.UpdateId, Ahora, cancellationToken))
            {
                return Array.Empty<RespuestaBot>();
            }

            if (!_limitador.Permitir("tg:" + mensaje.TelegramUserId.ToString(CultureInfo.InvariantCulture), Ahora))
            {
                return new RespuestaBot[] { Responder(mensaje, MensajesTelegram.DemasiadosMensajes) };
            }

            ConversacionTelegram conversacion = await _conversaciones.ObtenerAsync(mensaje.TelegramUserId, cancellationToken)
                ?? new ConversacionTelegram(mensaje.TelegramUserId, mensaje.ChatId, Ahora);
            int? idUsuario = await _vinculacion.ObtenerUsuarioVinculadoAsync(mensaje.TelegramUserId, cancellationToken);
            // Vincular una cuenta no reasigna una solicitud invitada ni rompe su continuidad.
            if (conversacion.IdInteraccion is int solicitud && await _invitados.EsSesionInvitadaAsync(mensaje.TelegramUserId, solicitud, cancellationToken))
                idUsuario = null;

            bool fotoConsentida = false;
            if (mensaje.DatoBoton?.StartsWith(PrefijoConsentirFoto, StringComparison.Ordinal) == true)
            {
                FotoPendiente? pendiente;
                lock (_fotosPendientes)
                {
                    _fotosPendientes.Remove(mensaje.TelegramUserId, out pendiente);
                }
                if (pendiente is null || pendiente.Nonce != mensaje.DatoBoton[PrefijoConsentirFoto.Length..]
                    || pendiente.Chat != mensaje.ChatId || pendiente.Interaccion != conversacion.IdInteraccion
                    || pendiente.Version != conversacion.Version || Ahora >= pendiente.HastaUtc)
                    return new RespuestaBot[] { Responder(mensaje, "La confirmación de la foto venció o cambió la operación. Envía de nuevo una foto de un residuo sin datos personales.") };
                // Consumir una sola vez antes de cualquier descarga/red. Reiniciar exige confirmar de nuevo.
                mensaje = mensaje with { ArchivoFoto = pendiente.Archivo, DatoBoton = null }; fotoConsentida = true;
            }
            else if (mensaje.DatoBoton == FotoPrevia)
            {
                (string Archivo, DateTime HastaUtc) previa;
                bool hay;
                lock (_fotosSinCaneca) { hay = _fotosSinCaneca.Remove(mensaje.TelegramUserId, out previa); }
                if (!hay || Ahora >= previa.HastaUtc)
                    return new RespuestaBot[] { Responder(mensaje, "Esa foto ya venció. " + MensajesTelegram.OtraFoto) };
                // Sigue el camino normal de una foto nueva: consentimiento, límites y validación de la caneca.
                mensaje = mensaje with { ArchivoFoto = previa.Archivo, DatoBoton = null };
            }
            else if (mensaje.ArchivoFoto is null)
            { lock (_fotosPendientes) { _fotosPendientes.Remove(mensaje.TelegramUserId); } }

            RespuestaBot respuesta;
            if (mensaje.ArchivoFoto is not null)
            {
                var foto = await FotoAsync(mensaje, conversacion, idUsuario, cancellationToken, fotoConsentida);
                respuesta = foto.Respuesta;
                conversacion = foto.Conversacion;
            }
            else
            {
                respuesta = await DespacharAsync(mensaje, conversacion, idUsuario, cancellationToken);
            }

            try
            {
                await _conversaciones.GuardarAsync(conversacion, cancellationToken);
            }
            catch (CambioConcurrenteException)
            {
                return new RespuestaBot[] { Responder(mensaje, MensajesTelegram.MensajeSimultaneo) };
            }

            return new RespuestaBot[] { respuesta };
        }

        private async Task<(RespuestaBot Respuesta, ConversacionTelegram Conversacion)> FotoAsync(MensajeEntrante mensaje, ConversacionTelegram conversacion, int? usuario, CancellationToken ct, bool consentida = false)
        {
            (RespuestaBot Respuesta, ConversacionTelegram Conversacion) Terminar(RespuestaBot respuesta) => (respuesta, conversacion);
            if (conversacion.IdInteraccion is null || conversacion.IdContenedor is null
                || conversacion.Estado is not (EstadoConversacion.CanecaIdentificada or EstadoConversacion.EsperandoConfirmacion)
                || Ahora - conversacion.FechaActualizacionUtc > VigenciaConversacion)
            {
                if (_vision is null || Compat.IsNullOrWhiteSpace(mensaje.ArchivoFoto) || mensaje.ArchivoFoto.Length > 512)
                    return Terminar(Responder(mensaje, MensajesTelegram.EscaneaPrimero));
                lock (_fotosSinCaneca)
                {
                    foreach (long id in _fotosSinCaneca.Where(x => x.Value.HastaUtc <= Ahora).Select(x => x.Key).ToArray()) _fotosSinCaneca.Remove(id);
                    if (_fotosSinCaneca.Count >= 512 && !_fotosSinCaneca.ContainsKey(mensaje.TelegramUserId))
                        return Terminar(Responder(mensaje, MensajesTelegram.EscaneaPrimero));
                    _fotosSinCaneca[mensaje.TelegramUserId] = (mensaje.ArchivoFoto, Ahora.AddMinutes(10));
                }
                return Terminar(new RespuestaBot(mensaje.ChatId, MensajesTelegram.FotoGuardadaSinCaneca, new BotonBot[] { BotonMenu }));
            }
            var permiso = await ValidarSolicitudAsync(conversacion, usuario, ct);
            if (!permiso.Exitoso) return Terminar(Responder(mensaje, permiso.Mensaje!));
            if (await _contenedores.ObtenerPorIdAsync(conversacion.IdContenedor.Value, ct) is not { EstaEnServicio: true })
                return Terminar(Responder(mensaje, MensajesTelegram.CanecaNoDisponible));
            // Una foto nueva invalida la propuesta anterior: un botón viejo no confirma una sugerencia incierta.
            conversacion.IdentificarCaneca(conversacion.IdContenedor.Value, conversacion.IdInteraccion, Ahora);
            if (_vision is null || _descargarFoto is null)
                return Terminar(AlternativaFoto(mensaje, MensajesTelegram.FotoNoDisponible));
            if (_requiereConsentimientoFoto && !consentida)
            {
                if (Compat.IsNullOrWhiteSpace(mensaje.ArchivoFoto) || mensaje.ArchivoFoto.Length > 512)
                    return Terminar(Responder(mensaje, "La referencia de fotografía no es válida."));
                string nonce = Guid.NewGuid().ToString("N");
                lock (_fotosPendientes)
                {
                    foreach (long id in _fotosPendientes.Where(x => x.Value.HastaUtc <= Ahora).Select(x => x.Key).ToArray()) _fotosPendientes.Remove(id);
                    if (_fotosPendientes.Count >= 512 && !_fotosPendientes.ContainsKey(mensaje.TelegramUserId))
                        return Terminar(Responder(mensaje, "No hay capacidad para confirmar otra foto. Intenta más tarde."));
                    _fotosPendientes[mensaje.TelegramUserId] = new(mensaje.ArchivoFoto, nonce, conversacion.IdInteraccion!.Value,
                        mensaje.ChatId, conversacion.Version + 1, Ahora.AddMinutes(2));
                }
                return Terminar(new RespuestaBot(mensaje.ChatId, MensajesTelegram.AvisoConsentimientoFoto,
                    new BotonBot[]
    {
        new BotonBot("Confirmo: mayor de 18, sin datos personales; enviar",PrefijoConsentirFoto+nonce),
        new BotonBot("Cancelar",ConfirmarNo)
    }));
            }
            if (!_fotosPorUsuario.Permitir("foto:" + mensaje.TelegramUserId.ToString(CultureInfo.InvariantCulture), Ahora)
                || !_fotosGlobales.Permitir("fotos", Ahora))
                return Terminar(Responder(mensaje, "Se alcanzó el límite de fotografías. Espera un minuto o elige el material de la lista."));
            // La invalidación se confirma ANTES de la red. Un fallo posterior no revive el botón anterior.
            try { await _conversaciones.GuardarAsync(conversacion, ct); }
            catch (CambioConcurrenteException) { return Terminar(Responder(mensaje, MensajesTelegram.MensajeSimultaneo)); }
            conversacion = await _conversaciones.ObtenerAsync(mensaje.TelegramUserId, ct)
                ?? throw new InvalidOperationException("No se pudo recuperar el diálogo preparado para la foto.");
            ImagenResiduo foto;
            using (var limite = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                limite.CancelAfter(TimeSpan.FromSeconds(15));
                try { foto = await _descargarFoto(mensaje.ArchivoFoto!, limite.Token).WaitAsync(TimeSpan.FromSeconds(15), ct); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception)
                {
                    limite.Cancel();
                    return Terminar(Responder(mensaje, "No se pudo descargar una foto válida. Envía otra o elige el material de la lista."));
                }
            }
            var r = await _vision.ClasificarAsync(foto, conversacion.IdContenedor, ct);
            if (!r.Exitoso) return Terminar(Responder(mensaje, r.Mensaje!));
            var clasificacion = r.Valor;
            if (_analisis is not null)
            {
                string[] identidad = clasificacion.Proveedor.Split(':', 2);
                await _analisis.GuardarAsync(new AnalisisImagen($"telegram:{conversacion.IdInteraccion}:foto:{conversacion.Version}",
                    conversacion.IdInteraccion!.Value, identidad[0], identidad.Length == 2 ? identidad[1] : "no-informado",
                    "no-publicada", (int)clasificacion.Resultado, clasificacion.Consulta?.Material.Id, Ahora), ct);
            }
            if (clasificacion.Resultado != ResultadoClasificacion.Identificado || clasificacion.Consulta is not { } consulta
                || consulta.Admision != AdmisionMaterial.Admitido || consulta.Material.Categoria == CategoriaMaterial.Peligroso)
                return Terminar(AlternativaFoto(mensaje, clasificacion.Mensaje));
            var sugerencia = usuario is int titular
                ? await _interacciones.RegistrarSugerenciaAsync(titular, conversacion.IdInteraccion!.Value, consulta.Material.Id, clasificacion.Confianza!.Value, ct)
                : await _invitados.RegistrarSugerenciaAsync(mensaje.TelegramUserId, conversacion.IdInteraccion!.Value, consulta.Material.Id, clasificacion.Confianza!.Value, ct);
            if (!sugerencia.Exitoso) return Terminar(Responder(mensaje, sugerencia.Mensaje!));
            conversacion.ProponerMaterial(consulta.Material.Id, Ahora);
            if (_aperturaDirecta)
            {
                var apertura = await ConfirmarAsync(mensaje, conversacion, usuario, ct);
                // Sin evidencia todavía: «abierta» solo se dice cuando el dispositivo lo informa y Oracle lo registra.
                return Terminar(conversacion.Estado == EstadoConversacion.OrdenEmitida && !apertura.Botones.Any(b => b.Dato == ConfirmarSi)
                    ? new RespuestaBot(mensaje.ChatId, MensajesTelegram.Identificado(consulta.Material.Nombre))
                    : apertura);
            }
            var confirmacion = await MostrarDeclaracionAsync(mensaje, conversacion, usuario, consulta.Material, ct);
            return Terminar(confirmacion with { Texto = clasificacion.Mensaje + "\n" + confirmacion.Texto });
        }

        private async Task<RespuestaBot> DespacharAsync(
            MensajeEntrante mensaje,
            ConversacionTelegram conversacion,
            int? idUsuario,
            CancellationToken cancellationToken)
        {
            if (mensaje.DatoBoton is { } dato)
            {
                // La consulta del catálogo no actúa sobre la solicitud, así que un botón viejo no la cancela.
                if (dato.StartsWith(PrefijoInfo, StringComparison.Ordinal))
                {
                    return await InformarMaterialAsync(mensaje, conversacion, idUsuario, dato[PrefijoInfo.Length..], cancellationToken);
                }

                if (dato.StartsWith(PrefijoMenu, StringComparison.Ordinal))
                {
                    return await NavegarAsync(mensaje, conversacion, idUsuario, dato[PrefijoMenu.Length..], cancellationToken);
                }

                if (conversacion.Estado != EstadoConversacion.SinCaneca && Ahora - conversacion.FechaActualizacionUtc > VigenciaConversacion)
                {
                    await CancelarPendienteAsync(conversacion, idUsuario, MotivoCancelacion, cancellationToken);
                    return Responder(mensaje, MensajesTelegram.ConversacionVencida);
                }

                return dato switch
                {
                    _ when dato.StartsWith(ConfirmarSi + ":", StringComparison.Ordinal) =>
                        await ConfirmarDeclaracionAsync(mensaje, conversacion, idUsuario, dato, cancellationToken),
                    ConfirmarSi => await ConfirmarAsync(mensaje, conversacion, idUsuario, cancellationToken),
                    ConfirmarNo => await VolverAMaterialesAsync(mensaje, conversacion, cancellationToken),
                    _ when dato.StartsWith(PrefijoMaterial, StringComparison.Ordinal) =>
                        await ElegirMaterialAsync(mensaje, conversacion, idUsuario, dato[PrefijoMaterial.Length..], cancellationToken),
                    _ => Responder(mensaje, MensajesTelegram.NoEntendido),
                };
            }

            if (EnlaceTelegram.EsStart(mensaje.Texto, out string? parametro))
            {
                return parametro is null
                    ? idUsuario is null && conversacion.Version == 0
                        ? BienvenidaSinCuenta(mensaje)
                        : Menu(mensaje, MensajesTelegram.Bienvenida)
                    : await IdentificarCanecaAsync(mensaje, conversacion, idUsuario, parametro, cancellationToken);
            }

            return await EjecutarComandoAsync(mensaje, conversacion, idUsuario, EnlaceTelegram.Comando(mensaje.Texto), cancellationToken);
        }

        /// <summary>Un botón del menú equivale a escribir el comando; las acciones conservan todas sus validaciones.</summary>
        private async Task<RespuestaBot> NavegarAsync(
            MensajeEntrante mensaje, ConversacionTelegram conversacion, int? idUsuario, string opcion, CancellationToken cancellationToken)
        {
            switch (opcion)
            {
                case { } opinion when opinion.StartsWith("opinion:", StringComparison.Ordinal):
                    return await CalificarAsync(mensaje, opinion[8..], true, cancellationToken);
                case "crear":
                case "vincular":
                    return ConMenu(await VincularAsync(mensaje,
                    await _vinculacion.ObtenerUsuarioVinculadoAsync(mensaje.TelegramUserId, cancellationToken), cancellationToken));
                case "invitado":
                    if (await _vinculacion.ObtenerUsuarioVinculadoAsync(mensaje.TelegramUserId, cancellationToken) is not null)
                        return Menu(mensaje, "Tu cuenta sigue vinculada. Consulta «Mi cuenta» si quieres desvincularla; este botón no cambia tu identidad ni las operaciones anteriores.");
                    if (conversacion.IdInteraccion is null && conversacion.IdContenedor is int pendiente
                        && Ahora - conversacion.FechaActualizacionUtc <= VigenciaConversacion)
                    {
                        var canecaPendiente = await _contenedores.ObtenerPorIdAsync(pendiente, cancellationToken);
                        if (canecaPendiente is not null)
                            return await IdentificarCanecaAsync(mensaje, conversacion, null, canecaPendiente.IdentificadorQr, cancellationToken);
                    }
                    return Menu(mensaje, "Continúas como invitado, sin ecopuntos ni historial personal. Escanea el QR de la caneca para depositar o consulta los materiales.");
                case "inicio": return Menu(mensaje, MensajesTelegram.Menu);
                case "cuenta": return ConMenu(Responder(mensaje, MensajesTelegram.MiCuenta(await _vinculacion.ObtenerUsuarioVinculadoAsync(mensaje.TelegramUserId, cancellationToken) is not null)));
                case "otrafoto": return new RespuestaBot(mensaje.ChatId, MensajesTelegram.OtraFoto, new BotonBot[] { BotonCancelar });
                case "qr": return ConMenu(Responder(mensaje, MensajesTelegram.EscanearQr));
                case "piloto-iniciar":
                    var pilotoActual = await BuscarPilotoDisponibleAsync(cancellationToken);
                    return pilotoActual is null ? ConMenu(Responder(mensaje, "La caneca piloto no está disponible en este momento."))
                        : await IdentificarCanecaAsync(mensaje, conversacion, idUsuario, pilotoActual.IdentificadorQr, cancellationToken);
                case "demo-iniciar":
                    return _qrDemo is null
                        ? ConMenu(Responder(mensaje, MensajesTelegram.DemoNoDisponible))
                        : await IdentificarCanecaAsync(mensaje, conversacion, idUsuario, _qrDemo, cancellationToken);
                case "depositar":
                    // Si el QR sigue vigente, se continúa donde quedó en vez de pedir otro escaneo.
                    if (conversacion.IdInteraccion is not null && conversacion.IdContenedor is { } idCaneca
                        && conversacion.Estado is EstadoConversacion.CanecaIdentificada or EstadoConversacion.EsperandoConfirmacion
                        && Ahora - conversacion.FechaActualizacionUtc <= VigenciaConversacion
                        && await _contenedores.ObtenerPorIdAsync(idCaneca, cancellationToken) is { EstaEnServicio: true } caneca)
                    {
                        var botones = BotonesMateriales(await MaterialesDeAsync(caneca, cancellationToken));
                        botones.Add(BotonCancelar);
                        return new RespuestaBot(mensaje.ChatId, MensajesTelegram.ElegirMaterial, botones);
                    }
                    var piloto = await BuscarPilotoDisponibleAsync(cancellationToken);
                    return piloto is null ? ConMenu(Responder(mensaje, MensajesTelegram.ComoDepositar))
                        : new RespuestaBot(mensaje.ChatId, "Puedes registrar tu residuo en «EcoAccess — Caneca piloto». Su identificación no autentica tu cuenta ni autoriza una apertura.",
                            new BotonBot[] { new BotonBot("Continuar en la caneca piloto", PrefijoMenu + "piloto-iniciar"), BotonMenu });
                case { } comando when comando.StartsWith('/') && !comando.Contains(' '):
                    return await EjecutarComandoAsync(mensaje, conversacion, idUsuario, comando, cancellationToken);
                default: return Menu(mensaje, MensajesTelegram.NoEntendido);
            }
        }

        private async Task<RespuestaBot> EjecutarComandoAsync(
            MensajeEntrante mensaje, ConversacionTelegram conversacion, int? idUsuario, string? comando, CancellationToken cancellationToken)
        {
            return comando switch
            {
                "/menu" or "/inicio" => Menu(mensaje, MensajesTelegram.Menu),
                "/proximamente" => ConMenu(Responder(mensaje, "🌿 Próximamente en EcoAccess. Son proyecciones, todavía no son funciones ni instalaciones activas.\n\n" +
                    string.Join("\n\n", ProyeccionesEcoAccess.Todas.Select(t => t.Titulo + "\n" + t.Descripcion)) + "\n\n" + ProyeccionesEcoAccess.MensajeInstitucional)),
                "/acerca" => ConMenu(Responder(mensaje, MensajesTelegram.AcercaDe)),
                "/calificar" => await CalificarAsync(mensaje, null, false, cancellationToken),
                "/demo" => _qrDemo is null
                    ? ConMenu(Responder(mensaje, MensajesTelegram.DemoNoDisponible))
                    : new RespuestaBot(mensaje.ChatId, MensajesTelegram.Demo,
                        new BotonBot[] { new BotonBot("▶️ Empezar con la caneca DEMO", PrefijoMenu + "demo-iniciar"), BotonMenu }),
                "/ayuda" or "/help" => Menu(mensaje, MensajesTelegram.Ayuda),
                "/foto" => ConMenu(Responder(mensaje, MensajesTelegram.GuiaFoto)),
                "/manual" => await SeleccionarManualmenteAsync(mensaje, conversacion, idUsuario, cancellationToken),
                "/cantidad" => await DeclararCantidadAsync(mensaje, conversacion, idUsuario, cancellationToken),
                "/potenciales" => await PotencialesAsync(mensaje, conversacion, idUsuario, cancellationToken),
                "/confirmar" => await ConfirmarVinculoAsync(mensaje, cancellationToken),
                "/vincular" => await VincularAsync(mensaje, await _vinculacion.ObtenerUsuarioVinculadoAsync(mensaje.TelegramUserId, cancellationToken), cancellationToken),
                "/cancelar" => ConMenu(await CancelarAsync(mensaje, conversacion, idUsuario, cancellationToken)),
                "/desvincular" => ConMenu(await DesvincularAsync(mensaje, conversacion, await _vinculacion.ObtenerUsuarioVinculadoAsync(mensaje.TelegramUserId, cancellationToken), cancellationToken)),
                "/operaciones" => ConMenu(await OperacionesAsync(mensaje, await _vinculacion.ObtenerUsuarioVinculadoAsync(mensaje.TelegramUserId, cancellationToken), cancellationToken)),
                "/depositos" => ConMenu(await DepositosAsync(mensaje, await _vinculacion.ObtenerUsuarioVinculadoAsync(mensaje.TelegramUserId, cancellationToken), cancellationToken)),
                "/estado" => (await EstadoAsync(mensaje, conversacion, idUsuario, cancellationToken)) with { Botones = new BotonBot[] { BotonEstado, BotonMenu } },
                "/ecopuntos" => ConMenu(await EcopuntosAsync(mensaje, await _vinculacion.ObtenerUsuarioVinculadoAsync(mensaje.TelegramUserId, cancellationToken), cancellationToken)),
                "/estadisticas" or "/mismateriales" or "/evolucion" => ConMenu(await EstadisticasAsync(mensaje,
                    await _vinculacion.ObtenerUsuarioVinculadoAsync(mensaje.TelegramUserId, cancellationToken), cancellationToken)),
                "/separar" or "/categorias" => ConMenu(Responder(mensaje, MensajesTelegram.Separacion(GuiaSeparacion.Todas))),
                "/materiales" => await MaterialesAsync(mensaje, cancellationToken),
                null when !Compat.IsNullOrWhiteSpace(mensaje.Texto) => await BuscarMaterialAsync(mensaje, conversacion, idUsuario, cancellationToken),
                _ => Menu(mensaje, MensajesTelegram.NoEntendido),
            };
        }

        private async Task<Contenedor?> BuscarPilotoDisponibleAsync(CancellationToken ct)
        {
            if (_qrDemo is not null)
            {
                var configurada = await _contenedores.ObtenerPorIdentificadorQrAsync(_qrDemo, ct);
                return configurada is { EstaEnServicio: true } ? configurada : null;
            }
            // Sin QR previo solo se ofrece el piloto explícitamente nombrado, nunca el primer fixture de la base.
            var pilotos = (await _contenedores.ListarAsync(ct))
                .Where(c => c.Nombre == ServicioCatalogoAmbiental.NombreCanecaPiloto && c.EstaEnServicio).Take(2).ToArray();
            return pilotos.Length == 1 ? pilotos[0] : null;
        }

        private async Task<RespuestaBot> ConfirmarVinculoAsync(MensajeEntrante mensaje, CancellationToken ct)
        {
            string[] partes = (mensaje.Texto ?? "").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length != 2 || partes[1].Length > 32)
                return Responder(mensaje, "Escribe /confirmar seguido del código mostrado en la página HTTPS. No envíes tu contraseña.");
            var resultado = await _vinculacion.ConfirmarVinculacionAsync(mensaje.TelegramUserId, mensaje.ChatId, partes[1], ct);
            return Responder(mensaje, resultado.Exitoso
                ? "Telegram quedó vinculado a tu cuenta de EcoAccess. Escanea el QR de la caneca para continuar."
                : resultado.Mensaje!);
        }

        private static RespuestaBot BienvenidaSinCuenta(MensajeEntrante mensaje) => new(mensaje.ChatId,
            "Bienvenido a EcoAccess. Puedes crear una cuenta o vincular la que ya tienes mediante nuestra página segura. Nunca envíes contraseñas por Telegram.\n\n" +
            "Puedes continuar sin registrarte. Sin embargo, no acumularás ecopuntos ni tendrás un historial personal. Si creas una cuenta, podrás consultar tus depósitos y beneficios.",
            new BotonBot[]
    {
        new("Crear cuenta", PrefijoMenu + "crear"),
        new("Vincular cuenta existente", PrefijoMenu + "vincular"),
        new("Continuar como invitado", PrefijoMenu + "invitado")
    });

        private async Task<RespuestaBot> EstadisticasAsync(MensajeEntrante mensaje, int? usuario, CancellationToken ct)
        {
            if (usuario is null) return Responder(mensaje, MensajesTelegram.SinVinculoConsulta);
            if (_estadisticas is null) return Responder(mensaje, "Las estadísticas personales no están disponibles en este entorno.");
            DateTime hasta = Ahora;
            var resultado = await _estadisticas.ObtenerMiasAsync(usuario.Value, hasta.AddDays(-30), hasta, ct);
            if (!resultado.Exitoso) return Responder(mensaje, resultado.Mensaje!);
            var datos = resultado.Valor;
            var texto = new System.Text.StringBuilder("📊 Tu actividad de los últimos 30 días (UTC)\n");
            texto.AppendLine($"Solicitudes: {datos.Actual.Interacciones.Total}");
            texto.AppendLine($"Depósitos: {datos.Actual.Depositos.Total}; verificados: {datos.Actual.Depositos.Verificados}");
            texto.AppendLine($"30 días anteriores: {datos.Anterior.Depositos.Total} depósitos, {datos.Anterior.Depositos.Verificados} verificados");
            texto.AppendLine("Materiales declarados (confirmados por ti):");
            foreach (var fila in datos.Materiales.Take(10))
                texto.AppendLine($"• {fila.Etiqueta}: {fila.Depositos} depósitos, {fila.Verificados} verificados");
            if (datos.Materiales.Count == 0) texto.AppendLine("No hay depósitos en este período.");
            texto.AppendLine("Evolución reciente, días de Colombia:");
            foreach (var fila in datos.Evolucion.OrderByDescending(f => f.Dia).Take(7))
                texto.AppendLine($"• {fila.Dia:yyyy-MM-dd}: {fila.Depositos} depósitos");
            texto.Append(datos.Actual.Depositos.ConPesoMedido > 0
                ? $"Peso medido: {datos.Actual.Depositos.KgMedidos.ToString("0.###", CultureInfo.InvariantCulture)} kg; {datos.Actual.Depositos.ConPesoMedido} depósitos con medición."
                : "No hay medición real de peso en este período.");
            texto.Append("\nConsulta /ecopuntos para tu saldo. Los conteos se actualizan durante la actividad y pueden variar entre consultas.");
            return Responder(mensaje, texto.ToString()) with { Estadisticas = datos };
        }

        /// <summary>Recupera el catálogo privado tras un fallo de fotos, sin depender de un proveedor visual.</summary>
        private async Task<RespuestaBot> SeleccionarManualmenteAsync(
            MensajeEntrante mensaje, ConversacionTelegram conversacion, int? idUsuario, CancellationToken ct)
        {
            if (conversacion.Estado == EstadoConversacion.OrdenEmitida)
                return Responder(mensaje, MensajesTelegram.OrdenYaEmitida);
            if (conversacion.IdInteraccion is not { } interaccion || conversacion.IdContenedor is not { } contenedor
                || Ahora - conversacion.FechaActualizacionUtc > VigenciaConversacion)
                return Responder(mensaje, MensajesTelegram.EscaneaPrimero);
            var permiso = await ValidarSolicitudAsync(conversacion, idUsuario, ct);
            if (!permiso.Exitoso) return Responder(mensaje, permiso.Mensaje!);
            if (await _contenedores.ObtenerPorIdAsync(contenedor, ct) is not { EstaEnServicio: true } caneca)
                return Responder(mensaje, MensajesTelegram.CanecaNoDisponible);
            var materiales = await MaterialesDeAsync(caneca, ct);
            // Descarta la selección anterior: un botón de confirmación viejo no confirma otro material.
            conversacion.IdentificarCaneca(contenedor, interaccion, Ahora);
            return new RespuestaBot(mensaje.ChatId,
                materiales.Count == 0 ? MensajesTelegram.SinMateriales : MensajesTelegram.SeleccionManual,
                BotonesMateriales(materiales));
        }

        /// <summary>Máximo de botones del catálogo público; Telegram admite teclados más grandes, pero el chat se vuelve ilegible.</summary>
        public const int MaximoBotonesCatalogo = 30;

        /// <summary>
        /// Comando público /materiales: muestra el catálogo validado con un botón de información por material.
        /// No exige cuenta ni caneca y no cambia la solicitud en curso; elegir para depositar sigue requiriendo el QR.
        /// </summary>
        private async Task<RespuestaBot> MaterialesAsync(MensajeEntrante mensaje, CancellationToken cancellationToken)
        {
            IReadOnlyList<Material> catalogo = await _catalogo.ListarHabilitadosAsync(cancellationToken);
            if (catalogo.Count == 0)
            {
                return Responder(mensaje, MensajesTelegram.CatalogoVacio);
            }

            IReadOnlyList<Material> visibles = catalogo.Take(MaximoBotonesCatalogo).ToList();
            return new RespuestaBot(mensaje.ChatId, MensajesTelegram.Catalogo(visibles, catalogo.Count),
                visibles.Select(m => new BotonBot(m.Nombre, PrefijoInfo + m.Id.ToString(CultureInfo.InvariantCulture))).ToList());
        }

        /// <summary>
        /// El texto libre se busca en el catálogo: con una coincidencia se explica el material, con varias se ofrecen
        /// botones para elegir. El texto nunca se interpreta como orden: solo se compara contra el catálogo.
        /// </summary>
        private async Task<RespuestaBot> BuscarMaterialAsync(
            MensajeEntrante mensaje,
            ConversacionTelegram conversacion,
            int? idUsuario,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Material> encontrados = await _catalogo.BuscarAsync(mensaje.Texto, cancellationToken);
            return encontrados.Count switch
            {
                0 => Responder(mensaje, MensajesTelegram.MaterialNoEncontrado),
                1 => await InformarAsync(mensaje, conversacion, idUsuario, encontrados[0].Id, cancellationToken),
                _ => new RespuestaBot(mensaje.ChatId, MensajesTelegram.VariosMateriales,
                    encontrados.Select(m => new BotonBot(m.Nombre, PrefijoInfo + m.Id.ToString(CultureInfo.InvariantCulture))).ToList()),
            };
        }

        private async Task<RespuestaBot> InformarMaterialAsync(
            MensajeEntrante mensaje,
            ConversacionTelegram conversacion,
            int? idUsuario,
            string textoId,
            CancellationToken cancellationToken) =>
            int.TryParse(textoId, NumberStyles.None, CultureInfo.InvariantCulture, out int idMaterial)
                ? await InformarAsync(mensaje, conversacion, idUsuario, idMaterial, cancellationToken)
                : Responder(mensaje, MensajesTelegram.MaterialNoEncontrado);

        /// <summary>
        /// Explica cómo se separa el material y, si hay una caneca identificada y vigente, si esa caneca lo recibe.
        /// Si la recibe y la persona tiene una solicitud abierta, ofrece el botón de material del flujo normal, que
        /// vuelve a validar todo; la consulta en sí no cambia la conversación.
        /// </summary>
        private async Task<RespuestaBot> InformarAsync(
            MensajeEntrante mensaje,
            ConversacionTelegram conversacion,
            int? idUsuario,
            int idMaterial,
            CancellationToken cancellationToken)
        {
            bool canecaVigente = conversacion.Estado != EstadoConversacion.SinCaneca
                && Ahora - conversacion.FechaActualizacionUtc <= VigenciaConversacion;
            int? idContenedor = canecaVigente ? conversacion.IdContenedor : null;

            Resultado<ConsultaMaterial> consulta = await _catalogo.ConsultarAsync(idMaterial, idContenedor, cancellationToken);
            if (!consulta.Exitoso)
            {
                return Responder(mensaje, MensajesTelegram.MaterialNoEncontrado);
            }

            string texto = MensajesTelegram.InformacionMaterial(consulta.Valor, consulta.Valor.Caneca?.Nombre);
            if (consulta.Valor.Admision != AdmisionMaterial.Admitido)
            {
                return Responder(mensaje, texto);
            }

            if (idUsuario is null && conversacion.IdInteraccion is null)
            {
                return Responder(mensaje, texto + "\n\n" + MensajesTelegram.InvitaRegistro);
            }

            return conversacion.IdInteraccion is not null
                && (conversacion.Estado is EstadoConversacion.CanecaIdentificada or EstadoConversacion.EsperandoConfirmacion)
                ? new RespuestaBot(mensaje.ChatId, texto, BotonesMateriales(new Material[] { consulta.Valor.Material }))
                : Responder(mensaje, texto);
        }

        private async Task<RespuestaBot> IdentificarCanecaAsync(
            MensajeEntrante mensaje,
            ConversacionTelegram conversacion,
            int? idUsuario,
            string identificadorQr,
            CancellationToken cancellationToken)
        {
            Contenedor? caneca = await _contenedores.ObtenerPorIdentificadorQrAsync(identificadorQr, cancellationToken);
            if (caneca is null)
            {
                return Responder(mensaje, MensajesTelegram.QrDesconocido);
            }

            if (!caneca.EstaEnServicio)
            {
                return Responder(mensaje, MensajesTelegram.CanecaNoDisponible);
            }

            IReadOnlyList<Material> materiales = await MaterialesDeAsync(caneca, cancellationToken);
            if (materiales.Count == 0)
            {
                return Responder(mensaje, MensajesTelegram.SinMateriales);
            }

            string presentacion = MensajesTelegram.CanecaIdentificada(caneca.Nombre, materiales.Select(m => m.Nombre));
            if (_qrDemo == caneca.IdentificadorQr)
                presentacion += "\n\nSimulación de software: estos eventos no demuestran movimiento físico ni ingreso real.";

            // Primera entrada desde el QR: conserva únicamente la caneca mientras la persona elige cómo continuar.
            // No inicia solicitud ni entrega botones de material antes de mostrar el aviso de invitado.
            if (conversacion.Version == 0
                && await _vinculacion.ObtenerUsuarioVinculadoAsync(mensaje.TelegramUserId, cancellationToken) is null)
            {
                conversacion.IdentificarCaneca(caneca.Id, null, Ahora);
                var bienvenida = BienvenidaSinCuenta(mensaje);
                return bienvenida with { Texto = presentacion + "\n\n" + bienvenida.Texto };
            }

            // Un QR nuevo reemplaza la solicitud anterior que no llegó a orden.
            await CancelarPendienteAsync(conversacion, idUsuario, MotivoReemplazo, cancellationToken);

            // El nuevo QR usa el vínculo actual; solo la operación anterior conservaba contexto invitado.
            idUsuario = await _vinculacion.ObtenerUsuarioVinculadoAsync(mensaje.TelegramUserId, cancellationToken);
            // El QR configurado como demo conserva esa procedencia en Oracle, también al entrar desde Telegram.
            CanalInteraccion canal = _qrDemo == caneca.IdentificadorQr ? CanalInteraccion.TelegramSimulado : CanalInteraccion.Telegram;
            Resultado<int> inicio = idUsuario is int titular
                ? await _interacciones.IniciarAsync(titular, caneca.Id, canal, cancellationToken)
                : await _invitados.IniciarAsync(mensaje.TelegramUserId, mensaje.ChatId, caneca.Id, cancellationToken, simulador: canal == CanalInteraccion.TelegramSimulado);
            if (!inicio.Exitoso)
            {
                conversacion.Reiniciar(Ahora);
                return Responder(mensaje, presentacion + "\n\n" + inicio.Mensaje);
            }

            conversacion.IdentificarCaneca(caneca.Id, inicio.Valor, Ahora);
            // Las condiciones completas se muestran al enviar cada foto; aquí basta un recordatorio corto.
            if (_aperturaDirecta && _vision is not null)
            {
                var saludo = new List<BotonBot> { new BotonBot("📝 Elegir material", PrefijoMenu + "/manual") };
                bool hayPrevia;
                lock (_fotosSinCaneca)
                    hayPrevia = _fotosSinCaneca.TryGetValue(mensaje.TelegramUserId, out var guardada) && guardada.HastaUtc > Ahora;
                if (hayPrevia) saludo.Insert(0, new BotonBot("📷 Analizar la foto que enviaste", FotoPrevia));
                string texto = MensajesTelegram.SaludoEco;
                if (_qrDemo == caneca.IdentificadorQr)
                    texto += "\n\nSimulación de software: estos eventos no demuestran movimiento físico ni ingreso real.";
                return new RespuestaBot(mensaje.ChatId, texto, saludo);
            }

            string aviso = _vision is null ? "" : "\n\n" + (_requiereConsentimientoFoto ? MensajesTelegram.SugerenciaFoto : _avisoFotografias ?? MensajesTelegram.SugerenciaFoto);
            var botones = BotonesMateriales(materiales);
            bool fotoPrevia;
            lock (_fotosSinCaneca)
                fotoPrevia = _fotosSinCaneca.TryGetValue(mensaje.TelegramUserId, out var previa) && previa.HastaUtc > Ahora;
            if (fotoPrevia && _vision is not null) botones.Insert(0, new BotonBot("📷 Analizar la foto que enviaste", FotoPrevia));
            botones.Add(BotonCancelar);
            return new RespuestaBot(mensaje.ChatId, presentacion + "\n\n" + MensajesTelegram.ElegirMaterial + aviso, botones);
        }

        private async Task<RespuestaBot> ElegirMaterialAsync(
            MensajeEntrante mensaje,
            ConversacionTelegram conversacion,
            int? idUsuario,
            string textoId,
            CancellationToken cancellationToken)
        {
            if (conversacion.IdInteraccion is null
                || conversacion.Estado is not (EstadoConversacion.CanecaIdentificada or EstadoConversacion.EsperandoConfirmacion))
            {
                return Responder(mensaje, MensajesTelegram.EscaneaPrimero);
            }

            Contenedor? caneca = await _contenedores.ObtenerPorIdAsync(conversacion.IdContenedor!.Value, cancellationToken);
            Material? material = int.TryParse(textoId, NumberStyles.None, CultureInfo.InvariantCulture, out int idMaterial)
                ? await _materiales.ObtenerPorIdAsync(idMaterial, cancellationToken)
                : null;
            if (caneca is null || material is null || !material.Habilitado || material.Categoria == CategoriaMaterial.Peligroso
                || FiltroMateriales.EsDatoDePrueba(material) || !caneca.AdmiteMaterial(material.Id))
            {
                return Responder(mensaje, MensajesTelegram.MaterialNoValido);
            }

            conversacion.ProponerMaterial(material.Id, Ahora);
            return await MostrarDeclaracionAsync(mensaje, conversacion, idUsuario, material, cancellationToken);
        }

        private async Task<RespuestaBot> ConfirmarAsync(
            MensajeEntrante mensaje,
            ConversacionTelegram conversacion,
            int? idUsuario,
            CancellationToken cancellationToken, int? cantidadConfirmada = null)
        {
            if (conversacion.Estado == EstadoConversacion.OrdenEmitida)
            {
                if (_despacho is null)
                {
                    return Responder(mensaje, MensajesTelegram.OrdenYaEmitida);
                }

                var actual = await ConsultarSolicitudAsync(conversacion, idUsuario, cancellationToken);
                return actual.Exitoso
                    ? await EntregarOrdenAsync(mensaje, actual.Valor.Orden.Id, cancellationToken)
                    : Responder(mensaje, actual.Mensaje!);
            }

            if (conversacion.Estado != EstadoConversacion.EsperandoConfirmacion)
            {
                return Responder(mensaje, MensajesTelegram.NadaQueConfirmar);
            }

            int idInteraccion = conversacion.IdInteraccion!.Value;
            int idMaterial = conversacion.IdMaterialPropuesto!.Value;
            string clave = FormattableString.Invariant($"telegram:{idInteraccion}:{idMaterial}");
            if (idUsuario is int actorDeclaracion)
            {
                var declarada = await _interacciones.ConsultarSolicitudPropiaAsync(actorDeclaracion, idInteraccion, cancellationToken);
                if (!declarada.Exitoso) return Responder(mensaje, declarada.Mensaje!);
                if (declarada.Valor.CantidadDeclarada != cantidadConfirmada)
                    return Responder(mensaje, "Esta confirmación corresponde a una cantidad anterior. Consulta /potenciales y utiliza el botón actualizado.");
            }

            if (_analisis is not null)
            {
                var yaEmitida = await ConsultarSolicitudAsync(conversacion, idUsuario, cancellationToken);
                if (!yaEmitida.Exitoso)
                {
                    var permisoTraza = await ValidarSolicitudAsync(conversacion, idUsuario, cancellationToken);
                    if (!permisoTraza.Exitoso) return Responder(mensaje, permisoTraza.Mensaje!);
                    // Antes de emitir: fallo de traza no deja una orden oculta. No es autorización ni depósito.
                    await _analisis.ConfirmarAsync(idInteraccion, idMaterial, Ahora, cancellationToken);
                }
            }
            Resultado<int> orden = idUsuario is int titular
                ? await _interacciones.AutorizarDeclaracionAsync(titular, idInteraccion, idMaterial, cantidadConfirmada, clave, cancellationToken)
                : await _invitados.AutorizarAsync(mensaje.TelegramUserId, idInteraccion, idMaterial, clave, cancellationToken);
            if (!orden.Exitoso)
            {
                if (idUsuario is int actorCambio)
                {
                    var declarada = await _interacciones.ConsultarSolicitudPropiaAsync(actorCambio, idInteraccion, cancellationToken);
                    if (declarada.Exitoso && declarada.Valor.CantidadDeclarada != cantidadConfirmada)
                        return Responder(mensaje, "La cantidad cambió durante la confirmación. Consulta /potenciales; no se emitió otra orden.");
                }
                // La interacción no queda abierta esperando un material que ya no se puede autorizar.
                await CancelarPendienteAsync(conversacion, idUsuario, MotivoCancelacion, cancellationToken);
                return Responder(mensaje, orden.Mensaje + "\n" + MensajesTelegram.EscaneaPrimero);
            }

            conversacion.MarcarOrdenEmitida(Ahora);
            return _despacho is null
                ? Responder(mensaje, MensajesTelegram.OrdenEmitida)
                : await EntregarOrdenAsync(mensaje, orden.Valor, cancellationToken);
        }

        private static string ClaveOrden(ConversacionTelegram conversacion) =>
            FormattableString.Invariant($"telegram:{conversacion.IdInteraccion}:{conversacion.IdMaterialPropuesto}");

        private async Task<RespuestaBot> EcopuntosAsync(MensajeEntrante mensaje, int? idUsuario, CancellationToken ct)
        {
            if (idUsuario is null) return Responder(mensaje, MensajesTelegram.SinVinculoConsulta);
            if (_ecopuntos is null) return Responder(mensaje, MensajesTelegram.EcopuntosPendiente);
            var consulta = await _ecopuntos.ConsultarMiosAsync(idUsuario.Value, ct);
            if (!consulta.Exitoso) return Responder(mensaje, consulta.Mensaje!);
            var resumen = consulta.Valor;
            string historial = string.Join("\n", resumen.Movimientos.Take(MaximoConsulta)
                .Select(m => $"{HoraColombia.DesdeUtc(m.FechaUtc):yyyy-MM-dd HH:mm}: {m.Tipo}, {m.Puntos} puntos, depósito {m.IdDeposito}, regla {m.IdRegla}"));
            return Responder(mensaje, $"Saldo: {resumen.Saldo} ecopuntos.\nAcreditados: {resumen.TotalAcreditado}; revertidos: {resumen.TotalRevertido}.\n" +
                (historial.Length == 0 ? "Sin movimientos." : "Últimos movimientos (hora Colombia):\n" + historial));
        }

        private async Task<RespuestaBot> EntregarOrdenAsync(MensajeEntrante mensaje, int idOrden, CancellationToken cancellationToken)
        {
            ResultadoDespacho resultado = await _despacho!.DespacharAsync(idOrden, cancellationToken);
            return resultado == ResultadoDespacho.NoEntregada
                ? new RespuestaBot(mensaje.ChatId, MensajesTelegram.Despacho(resultado), new BotonBot[] { new BotonBot("Reintentar entrega", ConfirmarSi), BotonEstado })
                : new RespuestaBot(mensaje.ChatId, MensajesTelegram.Despacho(resultado), new BotonBot[] { BotonEstado, BotonMenu });
        }

        private async Task<RespuestaBot> EstadoAsync(MensajeEntrante mensaje, ConversacionTelegram conversacion,
            int? idUsuario, CancellationToken cancellationToken)
        {
            if (idUsuario is null && conversacion.IdInteraccion is null)
                return Responder(mensaje, MensajesTelegram.SinVinculoConsulta);
            if (conversacion.IdInteraccion is null || conversacion.IdMaterialPropuesto is null)
            {
                return Responder(mensaje, "No hay una orden en este diálogo. Consulta /operaciones o /depositos.");
            }

            var consulta = await ConsultarSolicitudAsync(conversacion, idUsuario, cancellationToken);
            return Responder(mensaje, consulta.Exitoso ? MensajesTelegram.EstadoOperacion(consulta.Valor, Ahora) + (idUsuario is null ? "\n\nDepositas como invitado, sin ecopuntos. Vincula tu cuenta para futuras operaciones." : "") : consulta.Mensaje!);
        }

        private async Task<RespuestaBot> VolverAMaterialesAsync(
            MensajeEntrante mensaje,
            ConversacionTelegram conversacion,
            CancellationToken cancellationToken)
        {
            if (conversacion.Estado != EstadoConversacion.EsperandoConfirmacion)
            {
                return Responder(mensaje, MensajesTelegram.NadaQueConfirmar);
            }

            Contenedor? caneca = await _contenedores.ObtenerPorIdAsync(conversacion.IdContenedor!.Value, cancellationToken);
            IReadOnlyList<Material> materiales = caneca is null ? Array.Empty<Material>() : await MaterialesDeAsync(caneca, cancellationToken);
            return new RespuestaBot(mensaje.ChatId, MensajesTelegram.ElegirMaterial, BotonesMateriales(materiales).Concat(new BotonBot[] { BotonCancelar }).ToArray());
        }

        private async Task<RespuestaBot> VincularAsync(MensajeEntrante mensaje, int? idUsuario, CancellationToken cancellationToken)
        {
            if (idUsuario is not null)
            {
                return Responder(mensaje, MensajesTelegram.YaVinculada);
            }

            // No emitir un token que el ciudadano no podría utilizar desde su teléfono.
            if (_paginaRegistro is null) return Responder(mensaje, MensajesTelegram.VinculacionNoDisponible);
            Resultado<EnlaceRegistro> enlace = await _vinculacion.SolicitarAsync(mensaje.TelegramUserId, mensaje.ChatId, cancellationToken);
            return enlace.Exitoso
                ? Responder(mensaje, MensajesTelegram.EnlaceRegistro(EnlaceTelegram.ParaRegistro(_paginaRegistro, enlace.Valor!.Token)))
                : Responder(mensaje, enlace.Mensaje!);
        }

        private async Task<RespuestaBot> CancelarAsync(
            MensajeEntrante mensaje,
            ConversacionTelegram conversacion,
            int? idUsuario,
            CancellationToken cancellationToken)
        {
            if (conversacion.Estado == EstadoConversacion.SinCaneca)
            {
                return Responder(mensaje, MensajesTelegram.NadaQueCancelar);
            }

            bool ordenEmitida = conversacion.Estado == EstadoConversacion.OrdenEmitida;
            if (!ordenEmitida && conversacion.IdInteraccion is not null
                && conversacion.IdMaterialPropuesto is not null)
            {
                // La orden puede existir aunque haya fallado guardar el estado del diálogo después de entregarla.
                var persistida = await ConsultarSolicitudAsync(conversacion, idUsuario, cancellationToken);
                ordenEmitida = persistida.Exitoso;
            }
            bool rechazoConfirmado = await CancelarPendienteAsync(conversacion, idUsuario, MotivoCancelacion, cancellationToken);
            return Responder(mensaje, ordenEmitida ? MensajesTelegram.DialogoCerrado
                : rechazoConfirmado ? MensajesTelegram.Cancelado : MensajesTelegram.CancelacionNoConfirmada);
        }

        private async Task<RespuestaBot> DesvincularAsync(
            MensajeEntrante mensaje,
            ConversacionTelegram conversacion,
            int? idUsuario,
            CancellationToken cancellationToken)
        {
            await CancelarPendienteAsync(conversacion, idUsuario, MotivoCancelacion, cancellationToken);
            Resultado baja = await _vinculacion.DesvincularAsync(mensaje.TelegramUserId, cancellationToken);
            return Responder(mensaje, baja.Exitoso ? MensajesTelegram.Desvinculado : baja.Mensaje!);
        }

        /// <summary>Últimas interacciones del ciudadano vinculado; sin vínculo no se consulta nada.</summary>
        private async Task<RespuestaBot> OperacionesAsync(MensajeEntrante mensaje, int? idUsuario, CancellationToken cancellationToken)
        {
            if (idUsuario is not { } actor)
            {
                return Responder(mensaje, MensajesTelegram.SinVinculoConsulta);
            }

            Resultado<IReadOnlyList<Interaccion>> lista = await _interacciones.ListarMiasAsync(actor, cancellationToken);
            if (!lista.Exitoso)
            {
                return Responder(mensaje, lista.Mensaje!);
            }

            if (lista.Valor!.Count == 0)
            {
                return Responder(mensaje, MensajesTelegram.SinOperaciones);
            }

            IReadOnlyDictionary<int, string> nombres = await NombresMaterialesAsync(cancellationToken);
            return Responder(mensaje, MensajesTelegram.Operaciones(lista.Valor.Take(MaximoConsulta).Select(i =>
                (i.FechaInicioUtc, i.Estado, i.IdMaterialConfirmado is { } m ? nombres.GetValueOrDefault(m) : null))));
        }

        /// <summary>
        /// Últimos depósitos del ciudadano vinculado. El peso medido por el sensor y el estimado se muestran por
        /// separado y con su nombre: nunca se presenta una estimación como medida.
        /// </summary>
        private async Task<RespuestaBot> DepositosAsync(MensajeEntrante mensaje, int? idUsuario, CancellationToken cancellationToken)
        {
            if (idUsuario is not { } actor)
            {
                return Responder(mensaje, MensajesTelegram.SinVinculoConsulta);
            }

            Resultado<IReadOnlyList<Deposito>> lista = await _interacciones.ListarMisDepositosAsync(actor, cancellationToken);
            if (!lista.Exitoso)
            {
                return Responder(mensaje, lista.Mensaje!);
            }

            if (lista.Valor!.Count == 0)
            {
                return Responder(mensaje, MensajesTelegram.SinDepositos);
            }

            IReadOnlyDictionary<int, string> nombres = await NombresMaterialesAsync(cancellationToken);
            return Responder(mensaje, MensajesTelegram.Depositos(lista.Valor.Take(MaximoConsulta).Select(d =>
                (d.FechaDeteccionUtc, nombres.GetValueOrDefault(d.IdMaterialVerificado ?? d.IdMaterialDeclarado) ?? "?",
                 d.EstaVerificado, d.PesoMedidoKg, d.PesoEstimadoKg))));
        }

        private async Task<IReadOnlyDictionary<int, string>> NombresMaterialesAsync(CancellationToken cancellationToken) =>
            (await _materiales.ListarAsync(soloHabilitados: false, cancellationToken)).ToDictionary(m => m.Id, m => m.Nombre);

        /// <summary>
        /// Rechaza la interacción aún sin orden y deja la conversación en el inicio. Una orden ya emitida no se
        /// cancela desde el chat: la cierran el dispositivo o su vencimiento (RN-07).
        /// </summary>
        private async Task<bool> CancelarPendienteAsync(
            ConversacionTelegram conversacion,
            int? idUsuario,
            string motivo,
            CancellationToken cancellationToken)
        {
            bool rechazoConfirmado = true;
            if (conversacion.IdInteraccion is { } idInteraccion
                && conversacion.Estado is EstadoConversacion.CanecaIdentificada or EstadoConversacion.EsperandoConfirmacion)
            {
                // Si ya no estaba iniciada (venció o se cerró), no hay nada que rechazar.
                bool invitada = await _invitados.EsSesionInvitadaAsync(conversacion.TelegramUserId, idInteraccion, cancellationToken);
                rechazoConfirmado = (idUsuario is int actor && !invitada
                    ? await _interacciones.RechazarAsync(actor, idInteraccion, motivo, cancellationToken)
                    : await _invitados.CancelarAsync(conversacion.TelegramUserId, idInteraccion, motivo, cancellationToken)).Exitoso;
            }

            conversacion.Reiniciar(Ahora);
            return rechazoConfirmado;
        }

        private Task<Resultado> ValidarSolicitudAsync(ConversacionTelegram conversacion, int? usuario, CancellationToken ct) =>
            usuario is int titular
                ? _interacciones.ValidarSolicitudActivaAsync(titular, conversacion.IdInteraccion!.Value, ct)
                : _invitados.ValidarAsync(conversacion.TelegramUserId, conversacion.IdInteraccion!.Value, ct);

        private Task<Resultado<OperacionApertura>> ConsultarSolicitudAsync(ConversacionTelegram conversacion, int? usuario, CancellationToken ct) =>
            usuario is int titular
                ? _interacciones.ConsultarOperacionAsync(titular, conversacion.IdInteraccion!.Value, ClaveOrden(conversacion), ct)
                : _invitados.ConsultarAsync(conversacion.TelegramUserId, conversacion.IdInteraccion!.Value, ClaveOrden(conversacion), ct);

        private async Task<IReadOnlyList<Material>> MaterialesDeAsync(Contenedor caneca, CancellationToken cancellationToken) =>
            (await _catalogo.ListarHabilitadosAsync(cancellationToken))
                .Where(m => m.Categoria != CategoriaMaterial.Peligroso && caneca.AdmiteMaterial(m.Id))
                .ToList();

        private static List<BotonBot> BotonesMateriales(IEnumerable<Material> materiales) =>
            materiales.Select(m => new BotonBot(m.Nombre, PrefijoMaterial + m.Id.ToString(CultureInfo.InvariantCulture))).ToList();

        private static RespuestaBot AlternativaFoto(MensajeEntrante mensaje, string texto) =>
            new(mensaje.ChatId, texto, new BotonBot[]
    {
        new("📷 Otra foto", PrefijoMenu + "otrafoto"),
        new("📝 Elegir material", PrefijoMenu + "/manual"),
        BotonMenu
    });

        private static List<BotonBot> BotonesConfirmar(bool conFoto) =>
            conFoto
                ? new List<BotonBot> { new("✅ Sí, es correcto", ConfirmarSi), new("✏️ Cambiar material", ConfirmarNo), new("📷 Otra foto", PrefijoMenu + "otrafoto") }
                : new List<BotonBot> { new("✅ Sí, es correcto", ConfirmarSi), new("✏️ Cambiar material", ConfirmarNo), BotonCancelar };

        private async Task<RespuestaBot> CalificarAsync(MensajeEntrante mensaje, string? puntuacion, bool desdeBoton, CancellationToken ct)
        {
            if (_opiniones is null) return ConMenu(Responder(mensaje, "Las valoraciones no están disponibles en este momento. Intenta más tarde."));
            int? ciudadano = await _vinculacion.ObtenerUsuarioVinculadoAsync(mensaje.TelegramUserId, ct);
            // Comentario y consentimiento se envían juntos; nunca se interpreta el texto libre de otro flujo como opinión.
            string[] partes = mensaje.Texto?.Trim().Split(' ', 4, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            if (desdeBoton || partes.Length >= 3 && partes[2].Equals("acepto", StringComparison.OrdinalIgnoreCase))
            {
                string? valor = desdeBoton ? puntuacion : partes[1];
                if (!int.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out int estrellas) || estrellas is < 1 or > 5)
                    return ConMenu(Responder(mensaje, "Selecciona entre 1 y 5 estrellas."));
                string? comentario = !desdeBoton && partes.Length == 4 ? partes[3] : null;
                if (ciudadano is null && comentario is not null)
                    return ConMenu(Responder(mensaje, "Como invitado puedes enviar estrellas sin comentario. Vincula una cuenta para añadir un comentario."));
                var resultado = ciudadano is { } actor
                    ? await _opiniones.RegistrarCiudadanoAsync(actor, estrellas, comentario, true, ct)
                    : await _opiniones.RegistrarInvitadoAsync("telegram:" + mensaje.TelegramUserId.ToString(CultureInfo.InvariantCulture), estrellas, true, ct);
                return ConMenu(Responder(mensaje, resultado.Exitoso ? "Gracias. Recibimos tu valoración de EcoAccess." : resultado.Mensaje!));
            }
            return new RespuestaBot(mensaje.ChatId,
                "Tu experiencia con EcoAccess es opcional y no concede ecopuntos. Al pulsar una estrella aceptas guardar una valoración diaria " +
                (ciudadano is null ? "con un identificador seudónimo, sin comentario." : "vinculada a tu cuenta. Para añadir un comentario: /calificar 5 acepto tu comentario (máximo 500 caracteres, sin datos personales)."),
                Enumerable.Range(1, 5).Select(n => new BotonBot(n + " ⭐", PrefijoMenu + "opinion:" + n)).Append(BotonMenu).ToArray());
        }

        private static RespuestaBot ConMenu(RespuestaBot respuesta) =>
            respuesta.Botones.Count > 0 ? respuesta : respuesta with { Botones = new BotonBot[] { BotonMenu } };

        private static RespuestaBot Menu(MensajeEntrante mensaje, string texto) =>
            new(mensaje.ChatId, texto, MenuPrincipal) { Columnas = 2 };

        private static RespuestaBot Responder(MensajeEntrante mensaje, string texto) => new(mensaje.ChatId, texto);
    }
}
