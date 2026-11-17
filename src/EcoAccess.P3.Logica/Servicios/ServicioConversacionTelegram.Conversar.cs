using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Texto libre: qué quiere decir la persona y qué responderle. Solo elige mensajes y consulta servicios existentes; nada de
    /// lo que se escribe aquí abre una caneca, cambia puntos o permisos, ni sale del equipo.
    /// </summary>
    public sealed partial class ServicioConversacionTelegram
    {
        private enum TemaReciente { Ecopuntos }

        private static readonly TimeSpan VigenciaContexto = TimeSpan.FromMinutes(10);
        private readonly Dictionary<long, (CategoriaComentario Categoria, DateTime HastaUtc)> _comentariosEsperados = new();
        private readonly Dictionary<long, (TemaReciente Tema, DateTime HastaUtc)> _temasRecientes = new();

        private static readonly BotonBot BotonVincular = new("👤 Vincular cuenta", PrefijoMenu + "vincular");
        private static readonly BotonBot BotonEcopuntos = new("🪙 Mis ecopuntos", PrefijoMenu + "/ecopuntos");
        private static readonly BotonBot BotonActividad = new("📊 Mi actividad", PrefijoMenu + "/estadisticas");
        private static readonly BotonBot BotonOtraFoto = new("📸 Otra foto", PrefijoMenu + "otrafoto");

        public static readonly IReadOnlyList<BotonBot> BotonesQueja = new BotonBot[]
        {
            new("📸 No reconoció mi foto", PrefijoMenu + "queja:foto"),
            new("⏳ Tardó demasiado", PrefijoMenu + "queja:demora"),
            new("🔓 Problema con la caneca", PrefijoMenu + "queja:caneca"),
            new("✍️ Otro problema", PrefijoMenu + "queja:otro"),
            new("🏠 Menú", PrefijoMenu + "inicio"),
        };

        public static readonly IReadOnlyList<BotonBot> BotonesVincular = new BotonBot[]
        {
            new("📝 Crear cuenta", PrefijoMenu + "crear"),
            new("🔗 Ya tengo cuenta", PrefijoMenu + "vincular"),
            new("🌱 Continuar como invitado", PrefijoMenu + "invitado"),
        };

        public static readonly IReadOnlyList<BotonBot> BotonesCuentaVinculada = new BotonBot[] { new("🪙 Mis ecopuntos", PrefijoMenu + "/ecopuntos"), new("📊 Mi actividad", PrefijoMenu + "/estadisticas"), new("🏠 Menú", PrefijoMenu + "inicio") };

        private void EsperarComentario(long usuario, CategoriaComentario categoria)
        {
            lock (_comentariosEsperados)
            {
                foreach (long id in _comentariosEsperados.Where(x => x.Value.HastaUtc <= Ahora).Select(x => x.Key).ToArray()) _comentariosEsperados.Remove(id);
                if (_comentariosEsperados.Count < 1024 || _comentariosEsperados.ContainsKey(usuario)) _comentariosEsperados[usuario] = (categoria, Ahora + VigenciaContexto);
            }
        }

        private CategoriaComentario? TomarComentarioEsperado(long usuario)
        {
            lock (_comentariosEsperados)
                return _comentariosEsperados.Remove(usuario, out var esperado) && esperado.HastaUtc > Ahora ? esperado.Categoria : null;
        }

        private void RecordarTema(long usuario, TemaReciente tema)
        {
            lock (_temasRecientes)
            {
                foreach (long id in _temasRecientes.Where(x => x.Value.HastaUtc <= Ahora).Select(x => x.Key).ToArray()) _temasRecientes.Remove(id);
                if (_temasRecientes.Count < 1024 || _temasRecientes.ContainsKey(usuario)) _temasRecientes[usuario] = (tema, Ahora + VigenciaContexto);
            }
        }

        private bool TemaVigente(long usuario, TemaReciente tema)
        {
            lock (_temasRecientes)
                return _temasRecientes.TryGetValue(usuario, out var reciente) && reciente.Tema == tema && reciente.HastaUtc > Ahora;
        }

        /// <summary>Guardar un comentario nunca debe impedir responderle a la persona: si Oracle falla, queda constancia en el registro.</summary>
        private async Task AnotarAsync(MensajeEntrante mensaje, ConversacionTelegram conversacion, int? usuario, TipoComentario tipo, CategoriaComentario categoria, string? texto, CancellationToken ct)
        {
            if (_comentarios is null) return;
            try { await _comentarios.RegistrarAsync(mensaje.TelegramUserId, mensaje.UpdateId, usuario, tipo, categoria, texto, conversacion.IdInteraccion, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception error) { RegistrarReconocimiento?.Invoke($"Comentario no guardado ({error.GetType().Name})"); }
        }

        private async Task<RespuestaBot> ConversarAsync(MensajeEntrante mensaje, ConversacionTelegram conversacion, int? usuario, CancellationToken ct)
        {
            IntencionTexto intencion = IntencionesTelegram.Clasificar(mensaje.Texto);
            bool delicado = intencion is IntencionTexto.Urgencia or IntencionTexto.Sexual or IntencionTexto.Violencia;
            if (TomarComentarioEsperado(mensaje.TelegramUserId) is { } categoria && !delicado && intencion is not (IntencionTexto.Cancelar or IntencionTexto.Menu))
            {
                // Caso real del 12/11/2026: tras la valoración, «era plástico y se abrió la negra» quedaba como comentario genérico.
                // Una corrección o un problema con la foto o la caneca se guarda como INCIDENCIA, aunque se esperara un comentario.
                bool incidencia = intencion is IntencionTexto.Correccion or IntencionTexto.ProblemaFoto or IntencionTexto.ProblemaCaneca;
                if (incidencia)
                    categoria = intencion == IntencionTexto.ProblemaCaneca || IntencionesTelegram.MencionaCaneca(mensaje.Texto)
                        ? CategoriaComentario.Caneca : CategoriaComentario.Reconocimiento;
                await AnotarAsync(mensaje, conversacion, usuario, incidencia ? TipoComentario.ProblemaTecnico : TipoComentario.Comentario, categoria,
                    intencion == IntencionTexto.Insulto ? null : mensaje.Texto, ct);
                if (incidencia) return new RespuestaBot(mensaje.ChatId, MensajesConversacion.IncidenciaRegistrada, new[] { BotonOtraFoto, BotonMenuCorto });
                return new RespuestaBot(mensaje.ChatId, MensajesConversacion.ComentarioRecibido, new[] { BotonOtraFoto, BotonMenuCorto });
            }

            switch (intencion)
            {
                case IntencionTexto.Urgencia: return Responder(mensaje, MensajesConversacion.Urgencia);
                case IntencionTexto.Sexual: return new RespuestaBot(mensaje.ChatId, MensajesConversacion.Sexual, new[] { BotonOtraFoto, BotonMenuCorto });
                case IntencionTexto.Violencia: return new RespuestaBot(mensaje.ChatId, MensajesConversacion.Violencia, new[] { BotonOtraFoto, BotonMenuCorto });
                case IntencionTexto.Insulto:
                    await AnotarAsync(mensaje, conversacion, usuario, TipoComentario.Queja, CategoriaComentario.General, null, ct);
                    return new RespuestaBot(mensaje.ChatId, MensajesConversacion.Insulto, BotonesQueja);
                case IntencionTexto.Saludo: return Saludo(mensaje, usuario);
                case IntencionTexto.Correccion:
                    // Incidencia verificable (COMENTARIO_CIUDADANO ligado a la interacción: predicción, orden y caneca en Oracle). No se
                    // entrena con ella: queda «Nuevo» para revisión humana.
                    await AnotarAsync(mensaje, conversacion, usuario, TipoComentario.ProblemaTecnico,
                        IntencionesTelegram.MencionaCaneca(mensaje.Texto) ? CategoriaComentario.Caneca : CategoriaComentario.Reconocimiento, mensaje.Texto, ct);
                    return conversacion.Estado == EstadoConversacion.OrdenEmitida ? await CorregirTrasOrdenAsync(mensaje, conversacion, ct) : Corregir(mensaje, conversacion);
                case IntencionTexto.OtraFoto: return new RespuestaBot(mensaje.ChatId, MensajesTelegram.OtraFoto, BotonesSaludo);
                case IntencionTexto.Cancelar: return ConMenu(await CancelarAsync(mensaje, conversacion, usuario, ct));
                case IntencionTexto.Ayuda: return Menu(mensaje, MensajesTelegram.Ayuda);
                case IntencionTexto.Menu: return Menu(mensaje, MensajesTelegram.Menu);
                case IntencionTexto.Despedida: return new RespuestaBot(mensaje.ChatId, MensajesConversacion.Despedida, BotonesSaludo);
                case IntencionTexto.Agradecimiento: return new RespuestaBot(mensaje.ChatId, MensajesConversacion.DeNada, BotonesSaludo);
                case IntencionTexto.Casual: return new RespuestaBot(mensaje.ChatId, MensajesConversacion.Casual, BotonesSaludo);
                case IntencionTexto.TemaFutbol: return new RespuestaBot(mensaje.ChatId, MensajesConversacion.Futbol, BotonesSaludo);
                case IntencionTexto.TemaTecnologia: return new RespuestaBot(mensaje.ChatId, MensajesConversacion.Tecnologia, BotonesSaludo);
                case IntencionTexto.TemaEstudio: return new RespuestaBot(mensaje.ChatId, MensajesConversacion.Estudio, BotonesSaludo);
                case IntencionTexto.FueraDeTema: return new RespuestaBot(mensaje.ChatId, MensajesConversacion.FueraDeTema, BotonesSaludo);
                case IntencionTexto.ConsultaAmbiental: return ConMenu(Responder(mensaje, MensajesTelegram.Educacion));
                case IntencionTexto.ConsultaResiduo:
                    return await BuscarMaterialAsync(mensaje with { Texto = IntencionesTelegram.ResiduoConsultado(mensaje.Texto) }, conversacion, usuario, ct);
                case IntencionTexto.Ecopuntos: return await SaldoAsync(mensaje, usuario, ct);
                case IntencionTexto.CuantosTengo:
                    return TemaVigente(mensaje.TelegramUserId, TemaReciente.Ecopuntos)
                        ? await EcopuntosSemanaAsync(mensaje, usuario, ct)
                        : new RespuestaBot(mensaje.ChatId, MensajesConversacion.CuantosDeQue, new[] { BotonEcopuntos, BotonActividad, BotonMenuCorto });
                case IntencionTexto.Actividad:
                    return usuario is null ? new RespuestaBot(mensaje.ChatId, MensajesConversacion.ActividadSinCuenta, new[] { BotonVincular, BotonMenuCorto })
                        : ConMenu(await EstadisticasAsync(mensaje, usuario, ct));
                case IntencionTexto.Cuenta: return await OfrecerVinculoAsync(mensaje, usuario);
                case IntencionTexto.YaTengoCuenta: return await VincularAsync(mensaje, usuario, ct);
                case IntencionTexto.Repetir: return await RepetirAsync(mensaje, conversacion, usuario, ct);
                case IntencionTexto.ProblemaCaneca:
                    await AnotarAsync(mensaje, conversacion, usuario, TipoComentario.ProblemaTecnico, CategoriaComentario.Caneca, mensaje.Texto, ct);
                    return await RevisarCanecaAsync(mensaje, conversacion, usuario, ct);
                case IntencionTexto.ProblemaFoto:
                    await AnotarAsync(mensaje, conversacion, usuario, TipoComentario.ProblemaTecnico, CategoriaComentario.Reconocimiento, mensaje.Texto, ct);
                    return new RespuestaBot(mensaje.ChatId, MensajesConversacion.ProblemaFoto, new[] { BotonOtraFoto, new BotonBot("✍️ Reportar el problema", PrefijoMenu + "queja:foto"), BotonMenuCorto });
                case IntencionTexto.Demora:
                    await AnotarAsync(mensaje, conversacion, usuario, TipoComentario.Queja, CategoriaComentario.Demora, mensaje.Texto, ct);
                    return new RespuestaBot(mensaje.ChatId, MensajesConversacion.Demora, new[] { BotonOtraFoto, BotonMenuCorto });
                case IntencionTexto.Queja:
                    await AnotarAsync(mensaje, conversacion, usuario, TipoComentario.Queja, CategoriaComentario.General, mensaje.Texto, ct);
                    return new RespuestaBot(mensaje.ChatId, MensajesConversacion.Queja, BotonesQueja);
                case IntencionTexto.QuejaExperiencia:
                    await AnotarAsync(mensaje, conversacion, usuario, TipoComentario.Queja, CategoriaComentario.General, mensaje.Texto, ct);
                    return new RespuestaBot(mensaje.ChatId, MensajesConversacion.QuejaExperiencia, BotonesQueja);
                case IntencionTexto.Sugerencia:
                    await AnotarAsync(mensaje, conversacion, usuario, TipoComentario.Sugerencia, CategoriaComentario.General, mensaje.Texto, ct);
                    return new RespuestaBot(mensaje.ChatId, MensajesConversacion.Sugerencia, new[] { BotonMenuCorto });
                case IntencionTexto.Elogio:
                    await AnotarAsync(mensaje, conversacion, usuario, TipoComentario.Elogio, CategoriaComentario.General, mensaje.Texto, ct);
                    return new RespuestaBot(mensaje.ChatId, MensajesConversacion.Elogio, BotonesSaludo);
                case IntencionTexto.ElogioEcoAccess:
                    await AnotarAsync(mensaje, conversacion, usuario, TipoComentario.Elogio, CategoriaComentario.General, mensaje.Texto, ct);
                    return new RespuestaBot(mensaje.ChatId, MensajesConversacion.ElogioEcoAccess, BotonesSaludo);
                default:
                    return await BuscarMaterialAsync(mensaje, conversacion, usuario, ct);
            }
        }

        private static RespuestaBot Saludo(MensajeEntrante mensaje, int? usuario) =>
            new(mensaje.ChatId, usuario is null ? MensajesTelegram.SaludoBreve : MensajesConversacion.SaludoDeNuevo, BotonesSaludo);

        /// <summary>El saldo sale de Oracle en cada consulta; si no hay vínculo no se consulta nada.</summary>
        private async Task<RespuestaBot> SaldoAsync(MensajeEntrante mensaje, int? usuario, CancellationToken ct)
        {
            if (usuario is null) return new RespuestaBot(mensaje.ChatId, MensajesConversacion.EcopuntosSinCuenta, new[] { BotonVincular, BotonMenuCorto });
            if (_ecopuntos is null) return ConMenu(Responder(mensaje, MensajesTelegram.EcopuntosPendiente));
            var consulta = await _ecopuntos.ConsultarMiosAsync(usuario.Value, ct);
            if (!consulta.Exitoso) return ConMenu(Responder(mensaje, consulta.Mensaje!));
            RecordarTema(mensaje.TelegramUserId, TemaReciente.Ecopuntos);
            return new RespuestaBot(mensaje.ChatId, MensajesConversacion.Saldo(consulta.Valor.Saldo), new[] { BotonActividad, BotonMenuCorto });
        }

        /// <summary>«¿Cuántos tengo esta semana?» tras consultar ecopuntos: suma los movimientos reales de los últimos 7 días.</summary>
        private async Task<RespuestaBot> EcopuntosSemanaAsync(MensajeEntrante mensaje, int? usuario, CancellationToken ct)
        {
            if (usuario is null) return new RespuestaBot(mensaje.ChatId, MensajesConversacion.EcopuntosSinCuenta, new[] { BotonVincular, BotonMenuCorto });
            if (_ecopuntos is null) return ConMenu(Responder(mensaje, MensajesTelegram.EcopuntosPendiente));
            var consulta = await _ecopuntos.ConsultarMiosAsync(usuario.Value, ct);
            if (!consulta.Exitoso) return ConMenu(Responder(mensaje, consulta.Mensaje!));
            DateTime desde = Ahora.AddDays(-7);
            long semana = consulta.Valor.Movimientos.Where(m => m.FechaUtc >= desde).Sum(m => (long)m.Puntos);
            return new RespuestaBot(mensaje.ChatId, MensajesConversacion.SaldoSemana(semana, consulta.Valor.Saldo), new[] { BotonActividad, BotonMenuCorto });
        }

        /// <summary>No afirma que la caneca falló: dice lo que Oracle registra de la última solicitud de este chat.</summary>
        private async Task<RespuestaBot> RevisarCanecaAsync(MensajeEntrante mensaje, ConversacionTelegram conversacion, int? usuario, CancellationToken ct)
        {
            var botones = new[] { BotonOtraFoto, new BotonBot("✍️ Contar qué pasó", PrefijoMenu + "comentar:caneca"), BotonMenuCorto };
            if (conversacion.IdInteraccion is null || conversacion.IdMaterialPropuesto is null)
                return new RespuestaBot(mensaje.ChatId, MensajesConversacion.CanecaSinSolicitud, botones);
            var consulta = await ConsultarSolicitudAsync(conversacion, usuario, ct);
            return new RespuestaBot(mensaje.ChatId, consulta.Exitoso
                ? MensajesConversacion.CanecaRevisada + "\n" + MensajesTelegram.EstadoOperacion(consulta.Valor, Ahora)
                : MensajesConversacion.CanecaSinSolicitud, botones);
        }

        /// <summary>«Intenta otra vez»: repite una foto que no abrió; una que ya abrió no se repite, porque sería una segunda orden.</summary>
        private async Task<RespuestaBot> RepetirAsync(MensajeEntrante mensaje, ConversacionTelegram conversacion, int? usuario, CancellationToken ct)
        {
            string? foto = null;
            lock (_ultimasFotos)
                if (_ultimasFotos.TryGetValue(mensaje.TelegramUserId, out var ultima) && ultima.Interaccion == conversacion.IdInteraccion && ultima.HastaUtc > Ahora) foto = ultima.Archivo;
            if (foto is null || conversacion.Estado == EstadoConversacion.OrdenEmitida || !_flujoRapido)
                return new RespuestaBot(mensaje.ChatId, MensajesConversacion.NadaQueRepetir, BotonesSaludo);
            var resultado = await FotoRapidaAsync(mensaje with { ArchivoFoto = foto, Texto = null }, conversacion, usuario, ct, false, false);
            return resultado.Respuesta;
        }

        /// <summary>Primer paso de /vincular: breve, sin emitir ningún enlace hasta que la persona lo pida.</summary>
        private Task<RespuestaBot> OfrecerVinculoAsync(MensajeEntrante mensaje, int? usuario) => Task.FromResult(usuario is not null
            ? new RespuestaBot(mensaje.ChatId, MensajesTelegram.YaVinculada, BotonesCuentaVinculada)
            : new RespuestaBot(mensaje.ChatId, MensajesConversacion.OfertaVinculo, BotonesVincular));

        private async Task<RespuestaBot> AtenderBotonQuejaAsync(MensajeEntrante mensaje, ConversacionTelegram conversacion, int? usuario, string opcion, CancellationToken ct)
        {
            switch (opcion)
            {
                case "foto":
                    await AnotarAsync(mensaje, conversacion, usuario, TipoComentario.ProblemaTecnico, CategoriaComentario.Reconocimiento, null, ct);
                    return new RespuestaBot(mensaje.ChatId, MensajesConversacion.ProblemaFotoAnotado, new[] { BotonOtraFoto, new BotonBot("✍️ Contar qué pasó", PrefijoMenu + "comentar:foto"), BotonMenuCorto });
                case "demora":
                    await AnotarAsync(mensaje, conversacion, usuario, TipoComentario.Queja, CategoriaComentario.Demora, null, ct);
                    return new RespuestaBot(mensaje.ChatId, MensajesConversacion.Demora, new[] { BotonOtraFoto, BotonMenuCorto });
                case "caneca":
                    await AnotarAsync(mensaje, conversacion, usuario, TipoComentario.ProblemaTecnico, CategoriaComentario.Caneca, null, ct);
                    return await RevisarCanecaAsync(mensaje, conversacion, usuario, ct);
                default:
                    EsperarComentario(mensaje.TelegramUserId, CategoriaComentario.Otro);
                    return new RespuestaBot(mensaje.ChatId, MensajesConversacion.PideComentario, new[] { BotonMenuCorto });
            }
        }

        private RespuestaBot PedirComentario(MensajeEntrante mensaje, string categoria)
        {
            EsperarComentario(mensaje.TelegramUserId, categoria switch
            {
                "valoracion" => CategoriaComentario.Valoracion,
                "foto" => CategoriaComentario.Reconocimiento,
                "caneca" => CategoriaComentario.Caneca,
                _ => CategoriaComentario.Otro,
            });
            return new RespuestaBot(mensaje.ChatId, MensajesConversacion.PideComentario, new[] { BotonMenuCorto });
        }
    }

    public static class MensajesConversacion
    {
        public const string SaludoDeNuevo = "🌱 ¡Hola de nuevo! Soy Eco. Envíame una foto de tu residuo. 📸";
        public const string Despedida = "💚 ¡Hasta pronto! Pequeñas acciones, grandes cambios.";
        public const string DeNada = "💚 ¡Con gusto! ¿Tienes otro residuo? Envíame una foto. 📸";
        public const string Casual = "🌱 ¡Listo para ayudarte a cuidar el planeta! ¿Qué necesitas?";
        public const string Futbol = "⚽ ¡El fútbol mueve pasiones! Yo me especializo en cuidar el planeta. ¿Te ayudo con EcoAccess?";
        public const string Tecnologia = "💻 La tecnología es muy útil. Cuando deja de funcionar, es importante gestionar correctamente sus residuos electrónicos.";
        public const string Estudio = "💻 ¡Excelente! EcoAccess también utiliza programación para promover el cuidado ambiental.";
        public const string FueraDeTema = "🌱 De eso no tengo información: yo me especializo en EcoAccess y en cuidar el planeta. ¿Te ayudo con un residuo?";
        public const string NoComprendido = "🌱 No entendí bien. Puedes enviarme una foto de tu residuo o escribir su nombre, por ejemplo «botella».";

        public const string Queja = "💚 Lamento que tu experiencia no haya sido buena. ¿Qué podemos mejorar?";
        public const string QuejaExperiencia = "💚 Gracias por decírmelo. Tu opinión nos ayuda a mejorar. ¿Qué ocurrió?";
        public const string ProblemaFoto = "📸 Entiendo. ¿Quieres intentar con otra foto o reportar el problema?";
        public const string ProblemaFotoAnotado = "📸 Gracias por avisarnos, ya quedó anotado. Una foto de cerca y con buena luz suele ayudar.";
        public const string Demora = "⏳ Gracias por avisarnos, ya quedó anotado. La respuesta depende del computador que atiende a Eco.";
        public const string CanecaRevisada = "⚠️ Revisemos lo ocurrido. Esto es lo que está registrado de tu última solicitud:";
        public const string CanecaSinSolicitud = "⚠️ Revisemos lo ocurrido. No encuentro una solicitud reciente en este chat, así que no puedo confirmar qué pasó. Envíame una foto y lo intentamos de nuevo.";
        public const string Sugerencia = "💚 ¡Gracias por tu idea! Ya quedó anotada para el equipo de EcoAccess.";
        public const string Elogio = "💚 ¡Gracias! Nos alegra que EcoAccess te haya resultado útil.";
        public const string ElogioEcoAccess = "💚 ¡Gracias! Pequeñas acciones, grandes cambios.";
        public const string PideComentario = "✍️ Cuéntame qué ocurrió en un solo mensaje, sin datos personales.";
        public const string ComentarioRecibido = "💚 Gracias, ya quedó anotado. Tu opinión nos ayuda a mejorar.";
        public const string IncidenciaRegistrada = "🛠️ Registré una incidencia con la clasificación o la caneca de esta operación para que el equipo la revise. Gracias por avisar: así corregimos a Eco.";
        public const string ValoracionNegativa = "💚 Gracias por tu opinión. ¿Quieres contarnos qué ocurrió?";

        public const string Sexual = "🌱 Este espacio está destinado a EcoAccess y la educación ambiental. ¿Te ayudo a identificar un residuo?";
        public const string Violencia = "🌱 EcoAccess está enfocado en el cuidado ambiental y el uso responsable de sus contenedores.";
        public const string Insulto = "💚 Si algo falló, puedes contármelo para ayudarnos a mejorar.";
        public const string Urgencia = "💚 Lamento que estés pasando por esto, y me importa que estés bien. Soy un bot y no puedo ayudarte como lo haría una persona. Si estás en peligro, llama ahora a la línea de emergencias 123 y busca a alguien de confianza que pueda acompañarte.";

        public const string OfertaVinculo = "👤 ¿Quieres guardar tus depósitos y consultar tus ecopuntos?";
        public const string VinculoListo = "✅ ¡Tu cuenta quedó vinculada! Ya puedes consultar tus ecopuntos y actividad.";
        public const string EcopuntosSinCuenta = "🪙 Para consultar tus ecopuntos, primero vincula tu cuenta.";
        public const string ActividadSinCuenta = "📊 Para consultar tu actividad, primero vincula tu cuenta.";
        public const string CuantosDeQue = "🤔 ¿Te refieres a tus ecopuntos o a tus depósitos?";
        public const string NadaQueRepetir = "📸 No tengo una foto pendiente que pueda repetir. Envíame una nueva.";

        public static string Saldo(long saldo) => $"🪙 Tienes {saldo} ecopuntos.";
        public static string SaldoSemana(long semana, long saldo) => $"🪙 En los últimos 7 días sumaste {semana} ecopuntos. Tu saldo es de {saldo}.";
    }
}
