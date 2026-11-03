using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Recorrido definitivo de Eco: saludo, foto, apertura y despedida. La foto solo abre con una caneca vinculada; una
    /// caneca física exige además un QR escaneado hace poco, porque una foto por sí sola no demuestra presencia.
    /// </summary>
    public sealed partial class ServicioConversacionTelegram
    {
        /// <summary>Cuánto dura la presencia que da escanear el QR. Vive en memoria: reiniciar el bot pide escanear otra vez.</summary>
        public static readonly TimeSpan VigenciaPresencia = TimeSpan.FromMinutes(5);

        /// <summary>Respuesta vacía: la apertura se anuncia cuando la caneca la confirma, no antes.</summary>
        private const string SinRespuesta = "";

        public static readonly IReadOnlyList<BotonBot> BotonesSaludo = new BotonBot[] { new("☰ Menú", PrefijoMenu + "inicio") };

        public static readonly IReadOnlyList<BotonBot> BotonesDespedida = new BotonBot[]
        {
            new("📸 Depositar otro residuo", PrefijoMenu + "enviarfoto"),
            new("🪙 Mis ecopuntos", PrefijoMenu + "/ecopuntos"),
            new("📊 Mi actividad", PrefijoMenu + "/estadisticas"),
            new("🌎 Explorar EcoAccess", PrefijoMenu + "conoce"),
            new("🌿 Próximamente", PrefijoMenu + "/proximamente"),
            new("☰ Menú completo", PrefijoMenu + "inicio"),
        };

        /// <summary>Estrellas y omitir: valorar es opcional y un toque repetido no crea otra valoración.</summary>
        public static readonly IReadOnlyList<BotonBot> BotonesValoracion =
            Enumerable.Range(1, 5).Select(n => new BotonBot(new string('⭐', n), PrefijoMenu + "opinion:" + n))
                .Append(new BotonBot("Omitir", PrefijoMenu + "omitir")).ToArray();

        public static readonly IReadOnlyList<BotonBot> BotonesObjeto = new BotonBot[]
        {
            new("📸 Otra foto", PrefijoMenu + "otrafoto"),
            new("🏠 Menú", PrefijoMenu + "inicio"),
        };

        /// <summary>Solo clave, contexto y resultado de cada foto, para el registro: sin imagen ni datos de la persona.</summary>
        public Action<string>? RegistrarReconocimiento { get; set; }

        private readonly Dictionary<long, (int Contenedor, DateTime HastaUtc)> _presencias = new();
        private readonly Dictionary<long, (string Frase, DateTime HastaUtc)> _detecciones = new();

        /// <summary>Qué se detectó en la última foto de la persona, para anunciar la apertura; null si eligió el material a mano.</summary>
        public string? FraseDeteccion(long telegramUserId)
        {
            lock (_detecciones)
                return _detecciones.TryGetValue(telegramUserId, out var d) && d.HastaUtc > Ahora ? d.Frase : null;
        }

        private void RecordarDeteccion(long usuario, string? frase)
        {
            lock (_detecciones)
            {
                foreach (long id in _detecciones.Where(x => x.Value.HastaUtc <= Ahora).Select(x => x.Key).ToArray()) _detecciones.Remove(id);
                if (frase is null) _detecciones.Remove(usuario);
                else if (_detecciones.Count < 512 || _detecciones.ContainsKey(usuario)) _detecciones[usuario] = (frase, Ahora.AddMinutes(5));
            }
        }

        private void RegistrarPresencia(long usuario, int contenedor)
        {
            lock (_presencias)
            {
                foreach (long id in _presencias.Where(x => x.Value.HastaUtc <= Ahora).Select(x => x.Key).ToArray()) _presencias.Remove(id);
                if (_presencias.Count < 1024 || _presencias.ContainsKey(usuario)) _presencias[usuario] = (contenedor, Ahora + VigenciaPresencia);
            }
        }

        private int? CanecaConPresencia(long usuario)
        {
            lock (_presencias)
                return _presencias.TryGetValue(usuario, out var p) && p.HastaUtc > Ahora ? p.Contenedor : null;
        }

        /// <summary>La caneca simulada no mueve nada físico: no necesita demostrar presencia.</summary>
        private bool EsSimulada(Contenedor caneca) => _qrDemo is not null && _qrDemo == caneca.IdentificadorQr;

        private async Task<bool> PuedeAtenderAsync(int contenedor, CancellationToken ct)
        {
            if (_estadoCaneca is null) return true;
            try { return await _estadoCaneca.DisponibleAsync(contenedor, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception) { return false; }
        }

        /// <summary>La caneca con la que esta foto puede continuar: la de un QR vigente o la simulada; null si hay que escanear.</summary>
        private async Task<Contenedor?> CanecaParaFotoAsync(long usuario, CancellationToken ct)
        {
            if (CanecaConPresencia(usuario) is int contenedor)
                return await _contenedores.ObtenerPorIdAsync(contenedor, ct) is { EstaEnServicio: true } escaneada ? escaneada : null;
            return await BuscarPilotoDisponibleAsync(ct) is { } piloto && EsSimulada(piloto) ? piloto : null;
        }

        /// <summary>
        /// Una solicitud sin orden que ya venció no se reutiliza ni bloquea la foto siguiente. La vigencia de Oracle cuenta
        /// desde el inicio y no se renueva con los botones: una conversación activa podía llevar una solicitud vencida.
        /// </summary>
        private async Task RetirarSolicitudVencidaAsync(ConversacionTelegram conversacion, int? usuario, CancellationToken ct)
        {
            if (conversacion.IdInteraccion is null || conversacion.Estado is not (EstadoConversacion.CanecaIdentificada or EstadoConversacion.EsperandoConfirmacion)) return;
            if (Ahora - conversacion.FechaActualizacionUtc <= VigenciaConversacion && (await ValidarSolicitudAsync(conversacion, usuario, ct)).Exitoso) return;
            await CancelarPendienteAsync(conversacion, usuario, MotivoCancelacion, ct);
        }

        /// <summary>
        /// Recorrido de una foto: primero qué se ve y qué admite Oracle, y solo después la solicitud y la caneca. Ni una sesión
        /// vencida ni una caneca ocupada impiden responder qué es; ninguna de las dos cosas abre nada por sí sola.
        /// </summary>
        private async Task<(RespuestaBot Respuesta, ConversacionTelegram Conversacion)> FotoRapidaAsync(MensajeEntrante mensaje, ConversacionTelegram conversacion, int? usuario, CancellationToken ct, bool consentida, bool segundaOpinion)
        {
            (RespuestaBot Respuesta, ConversacionTelegram Conversacion) Terminar(RespuestaBot respuesta) => (respuesta, conversacion);
            if (_vision is null || _descargarFoto is null) return Terminar(AlternativaFoto(mensaje, MensajesTelegram.FotoNoDisponible));
            if (Compat.IsNullOrWhiteSpace(mensaje.ArchivoFoto) || mensaje.ArchivoFoto.Length > 512)
                return Terminar(Responder(mensaje, "La referencia de fotografía no es válida."));

            // Una foto nueva invalida la propuesta anterior: un botón viejo no confirma lo que ya no está en pantalla.
            EstadoConversacion estadoLeido = conversacion.Estado;
            await RetirarSolicitudVencidaAsync(conversacion, usuario, ct);
            if (conversacion.Estado == EstadoConversacion.EsperandoConfirmacion && conversacion.IdContenedor is int enCurso)
                conversacion.IdentificarCaneca(enCurso, conversacion.IdInteraccion, Ahora);
            if (conversacion.Estado != estadoLeido)
            {
                try { await _conversaciones.GuardarAsync(conversacion, ct); }
                catch (CambioConcurrenteException) { return Terminar(Responder(mensaje, MensajesTelegram.MensajeSimultaneo)); }
                conversacion = await _conversaciones.ObtenerAsync(mensaje.TelegramUserId, ct)
                    ?? throw new InvalidOperationException("No se pudo recuperar el diálogo preparado para la foto.");
            }

            if (_requiereConsentimientoFoto && !consentida)
            {
                string? nonce = GuardarFotoPendiente(mensaje, conversacion, false, conversacion.Version + 1);
                if (nonce is null) return Terminar(Responder(mensaje, "No hay capacidad para confirmar otra foto. Intenta más tarde."));
                return Terminar(new RespuestaBot(mensaje.ChatId, MensajesTelegram.AvisoConsentimientoFoto, new BotonBot[]
                {
                    new("Confirmo: mayor de 18, sin datos personales; enviar", PrefijoConsentirFoto + nonce),
                    new("Cancelar", ConfirmarNo)
                }));
            }
            // La autorización se consulta en Oracle en cada foto: revocarla surte efecto de inmediato.
            bool externaAutorizada = _politicaExterna != PoliticaVisionExterna.Ninguna && await _consentimientosVision!
                .EstaVigenteAsync(mensaje.TelegramUserId, MensajesTelegram.VersionAvisoExterno, ct);
            if (!externaAutorizada && (_politicaExterna == PoliticaVisionExterna.Principal || segundaOpinion))
                return Terminar(PedirAutorizacionExterna(mensaje, conversacion, segundaOpinion ? MensajesTelegram.AvisoSegundaOpinion : MensajesTelegram.AvisoFotoExterna,
                    segundaOpinion, conversacion.Version + 1));
            if (!_fotosPorUsuario.Permitir("foto:" + mensaje.TelegramUserId.ToString(CultureInfo.InvariantCulture), Ahora)
                || !_fotosGlobales.Permitir("fotos", Ahora))
                return Terminar(Responder(mensaje, "Se alcanzó el límite de fotografías. Espera un minuto o elige el material de la lista."));

            // Un corte breve de la red no debe costarle la foto a la persona: se intenta una segunda vez.
            ImagenResiduo? descargada = null;
            for (int intento = 1; descargada is null; intento++)
            {
                using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
                limite.CancelAfter(TimeSpan.FromSeconds(12));
                try { descargada = await _descargarFoto(mensaje.ArchivoFoto!, limite.Token).WaitAsync(TimeSpan.FromSeconds(12), ct); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception error)
                {
                    limite.Cancel();
                    RegistrarReconocimiento?.Invoke($"Foto: descarga fallida, intento {intento} ({error.GetType().Name})");
                    if (intento == 2) return Terminar(AlternativaFoto(mensaje, "No se pudo descargar una foto válida. Envía otra o elige el material de la lista."));
                }
            }
            ImagenResiduo foto = descargada with { Envio = !externaAutorizada ? EnvioExterno.Prohibido : segundaOpinion ? EnvioExterno.SegundaOpinion : EnvioExterno.Autorizado };

            // La admisión se consulta aunque aún falte el QR: decir qué recibe el piloto no autoriza ninguna apertura.
            Contenedor? presente = await CanecaParaFotoAsync(mensaje.TelegramUserId, ct);
            Contenedor? referencia = presente ?? await BuscarPilotoDisponibleAsync(ct);
            var r = await _vision.ClasificarAsync(foto, referencia?.Id, ct);
            if (!r.Exitoso)
            {
                RegistrarReconocimiento?.Invoke($"Foto: rechazada antes del motor ({r.Motivo})");
                return Terminar(AlternativaFoto(mensaje, r.Mensaje!));
            }
            ClasificacionImagen clasificacion = r.Valor;
            RegistrarReconocimiento?.Invoke($"Foto: objeto={clasificacion.Reconocido?.Clave ?? "-"} contexto={clasificacion.Reconocido?.Contexto.ToString() ?? "-"} resultado={clasificacion.Resultado} corroborada={clasificacion.Corroborada}");
            // Una foto con personas no se conserva ni se ofrece a un servicio externo.
            if (clasificacion.Reconocido?.Contexto != ContextoObjeto.Persona) RecordarFoto(mensaje, conversacion.IdInteraccion);
            if (clasificacion.Resultado == ResultadoClasificacion.AutorizacionExternaRequerida)
                return Terminar(PedirAutorizacionExterna(mensaje, conversacion, clasificacion.Mensaje, false, conversacion.Version + 1));
            if (clasificacion.Resultado == ResultadoClasificacion.ObjetoReconocido)
                return Terminar(new RespuestaBot(mensaje.ChatId, clasificacion.Mensaje, BotonesObjeto));
            if (clasificacion.Resultado is ResultadoClasificacion.ProveedorNoDisponible or ResultadoClasificacion.CuotaGratuitaAgotada)
                return Terminar(AlternativaFoto(mensaje, clasificacion.Mensaje));
            if (clasificacion.Resultado != ResultadoClasificacion.Identificado || clasificacion.Consulta is not { } consulta)
                return Terminar(AlternativaFoto(mensaje, clasificacion.Resultado == ResultadoClasificacion.VariosObjetos ? clasificacion.Mensaje : MensajesTelegram.NoIdentificado));
            if (consulta.Admision is not (AdmisionMaterial.Admitido or AdmisionMaterial.SinCaneca))
                return Terminar(new RespuestaBot(mensaje.ChatId, clasificacion.Mensaje, BotonesObjeto));

            // Hasta aquí fue solo información. Lo que sigue exige caneca, presencia, sesión vigente y disponibilidad.
            if (presente is null)
            {
                RespuestaBot guardada = GuardarFotoHastaQr(mensaje);
                return Terminar(guardada with { Texto = MensajesTelegram.EscaneaParaAbrir(clasificacion.FraseDeteccion) });
            }
            string identificado = clasificacion.FraseDeteccion is null ? "" : $"♻️ Identifiqué {clasificacion.FraseDeteccion}.\n";
            if (await _interacciones.CanecaOcupadaAsync(presente.Id, ct))
                return Terminar(new RespuestaBot(mensaje.ChatId, identificado + MensajesTelegram.CanecaEnUso, BotonesSaludo));
            if (!await PuedeAtenderAsync(presente.Id, ct))
                return Terminar(new RespuestaBot(mensaje.ChatId, identificado + MensajesTelegram.CanecaSinServicio, BotonesSaludo));

            if (conversacion.IdInteraccion is null || conversacion.IdContenedor != presente.Id
                || conversacion.Estado is not (EstadoConversacion.CanecaIdentificada or EstadoConversacion.EsperandoConfirmacion))
            {
                RespuestaBot sesion = await IdentificarCanecaAsync(mensaje, conversacion, usuario, presente.IdentificadorQr, ct);
                if (conversacion.IdInteraccion is null) return Terminar(sesion);
                // La validación del invitado lee la sesión de Oracle: sin guardarla, su primera foto se rechazaba.
                try { await _conversaciones.GuardarAsync(conversacion, ct); }
                catch (CambioConcurrenteException) { return Terminar(Responder(mensaje, MensajesTelegram.MensajeSimultaneo)); }
                conversacion = await _conversaciones.ObtenerAsync(mensaje.TelegramUserId, ct)
                    ?? throw new InvalidOperationException("No se pudo recuperar la sesión de la foto.");
                RecordarFoto(mensaje, conversacion.IdInteraccion);
            }
            var permiso = await ValidarSolicitudAsync(conversacion, usuario, ct);
            if (!permiso.Exitoso) return Terminar(AlternativaFoto(mensaje, permiso.Mensaje!));

            if (_analisis is not null)
            {
                string[] identidad = clasificacion.Proveedor.Split(':', 2);
                await _analisis.GuardarAsync(new AnalisisImagen($"telegram:{conversacion.IdInteraccion}:foto:{conversacion.Version}",
                    conversacion.IdInteraccion!.Value, identidad[0], identidad.Length == 2 ? identidad[1] : "no-informado",
                    "no-publicada", (int)clasificacion.Resultado, consulta.Material.Id, Ahora), ct);
            }
            var sugerencia = usuario is int titular
                ? await _interacciones.RegistrarSugerenciaAsync(titular, conversacion.IdInteraccion!.Value, consulta.Material.Id, clasificacion.Confianza!.Value, ct)
                : await _invitados.RegistrarSugerenciaAsync(mensaje.TelegramUserId, conversacion.IdInteraccion!.Value, consulta.Material.Id, clasificacion.Confianza!.Value, ct);
            if (!sugerencia.Exitoso) return Terminar(Responder(mensaje, sugerencia.Mensaje!));
            conversacion.ProponerMaterial(consulta.Material.Id, Ahora);
            if (usuario is int declarante)
            {
                var unidad = await _interacciones.DeclararCantidadAsync(declarante, conversacion.IdInteraccion!.Value, CantidadPorFoto, ct);
                if (!unidad.Exitoso) return Terminar(Responder(mensaje, unidad.Mensaje!));
            }
            RecordarDeteccion(mensaje.TelegramUserId, clasificacion.FraseDeteccion);
            // Reconocido, admitido y con evidencia suficiente: la foto ya es la solicitud. El aviso sale cuando la caneca confirme.
            if (PoliticaAperturaAutomatica.Permite(clasificacion))
            {
                RespuestaBot abierta = await ConfirmarAsync(mensaje, conversacion, usuario, ct, usuario is null ? null : CantidadPorFoto);
                bool enCamino = conversacion.Estado == EstadoConversacion.OrdenEmitida
                    && (abierta.Texto == MensajesTelegram.OrdenEmitida || abierta.Texto == MensajesTelegram.Despacho(ResultadoDespacho.Enviada));
                // Esa foto ya ordenó una apertura: «intenta otra vez» no puede producir otra.
                if (enCamino) lock (_ultimasFotos) { _ultimasFotos.Remove(mensaje.TelegramUserId); }
                return Terminar(enCamino ? new RespuestaBot(mensaje.ChatId, SinRespuesta) : abierta);
            }
            string confirmar = usuario is null ? ConfirmarSi
                : FormattableString.Invariant($"{ConfirmarSi}:{conversacion.IdInteraccion}:{consulta.Material.Id}:{CantidadPorFoto}");
            return Terminar(new RespuestaBot(mensaje.ChatId, clasificacion.Mensaje, new BotonBot[]
            {
                new("✅ Sí, continuar", confirmar),
                new("✏️ Corregir material", ConfirmarNo)
            }));
        }

        /// <summary>Guarda solo la referencia de Telegram, sin descargarla: el QR retoma esta foto automáticamente.</summary>
        private RespuestaBot GuardarFotoHastaQr(MensajeEntrante mensaje)
        {
            if (Compat.IsNullOrWhiteSpace(mensaje.ArchivoFoto) || mensaje.ArchivoFoto.Length > 512)
                return Responder(mensaje, MensajesTelegram.EscaneaPrimero);
            lock (_fotosSinCaneca)
            {
                foreach (long id in _fotosSinCaneca.Where(x => x.Value.HastaUtc <= Ahora).Select(x => x.Key).ToArray()) _fotosSinCaneca.Remove(id);
                if (_fotosSinCaneca.Count >= 512 && !_fotosSinCaneca.ContainsKey(mensaje.TelegramUserId))
                    return Responder(mensaje, MensajesTelegram.EscaneaPrimero);
                _fotosSinCaneca[mensaje.TelegramUserId] = (mensaje.ArchivoFoto, Ahora.AddMinutes(10));
            }
            return new RespuestaBot(mensaje.ChatId, MensajesTelegram.FotoGuardadaSinCaneca, BotonesSaludo);
        }

        private string? TomarFotoGuardada(long usuario)
        {
            lock (_fotosSinCaneca)
                return _fotosSinCaneca.Remove(usuario, out var previa) && previa.HastaUtc > Ahora ? previa.Archivo : null;
        }

        /// <summary>
        /// «Te equivocaste» con la orden ya emitida: no genera otra orden. Queda constancia si la traza está activa y se
        /// ofrece empezar una operación nueva, que pasa por todas las validaciones.
        /// </summary>
        private async Task<RespuestaBot> CorregirTrasOrdenAsync(MensajeEntrante mensaje, ConversacionTelegram conversacion, CancellationToken ct)
        {
            if (_analisis is not null && conversacion.IdInteraccion is int interaccion)
            {
                try
                {
                    await _analisis.GuardarAsync(new AnalisisImagen($"telegram:{interaccion}:correccion", interaccion, "persona", "correccion",
                        "no-publicada", (int)ResultadoClasificacion.Incierta, conversacion.IdMaterialPropuesto, Ahora), ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception) { /* Repetir la corrección no vuelve a registrarla ni debe impedir la respuesta. */ }
            }
            RecordarDeteccion(mensaje.TelegramUserId, null);
            return new RespuestaBot(mensaje.ChatId, MensajesTelegram.CorreccionTrasOrden, new BotonBot[]
            {
                new("📸 Otra foto", PrefijoMenu + "otrafoto"),
                new("🏠 Menú", PrefijoMenu + "inicio"),
            });
        }

        private RespuestaBot Proximamente(MensajeEntrante mensaje, string? seccion)
        {
            if (seccion is null)
                return new RespuestaBot(mensaje.ChatId, MensajesTelegram.ProximamenteInicio, MensajesTelegram.SeccionesProximamente
                    .Select(s => new BotonBot(s.Boton, PrefijoMenu + "prox:" + s.Clave)).Append(BotonMenuCorto).ToArray());
            var elegida = MensajesTelegram.SeccionesProximamente.FirstOrDefault(s => s.Clave == seccion);
            return elegida.Clave is null
                ? Menu(mensaje, MensajesTelegram.NoEntendido)
                : new RespuestaBot(mensaje.ChatId, elegida.Texto, new[] { new BotonBot("◀️ Próximamente", PrefijoMenu + "/proximamente"), BotonMenuCorto });
        }

        private static readonly BotonBot BotonMenuCorto = new("🏠 Menú", PrefijoMenu + "inicio");
    }
}
