using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Vision35;
using EcoAccess.P3.Logica.Vision35.Expo;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>
    /// Modo ExpoSoftware en Telegram: una foto → una sola inferencia local (9 materiales) → validación C# → canecas →
    /// apertura simultánea 7/14/21 s en la estación (panel local, único dueño del ESP32) → registro en Oracle Cloud.
    /// Solo abre para chats autorizados o con el QR de la estación escaneado, y con el control físico activado en la estación.
    /// </summary>
    public sealed partial class ServicioConversacionTelegram
    {
        public const string PrefijoExpo = "expo:";

        /// <summary>Estación Expo (panel + Oracle). Si es null, el bot conserva el recorrido de siempre.</summary>
        public ServicioEstacionExpo? EstacionExpo { get; set; }

        /// <summary>Reconocedor de los 9 materiales (Ollama local, modelo cargado).</summary>
        public ReconocedorExpo9? ReconocedorExpo { get; set; }

        /// <summary>Envía un mensaje fuera del turno (fin del ciclo de apertura). Lo conecta el adaptador de Telegram.</summary>
        public Func<RespuestaBot, CancellationToken, Task>? EnviarDiferido { get; set; }

        private readonly Dictionary<string, (long Usuario, IReadOnlyList<CategoriaMaterial> Canecas, ResultadoExpo9? Reconocimiento, DateTime HastaUtc)> _propuestasExpo = new(StringComparer.Ordinal);

        private bool ModoExpo => EstacionExpo is not null && ReconocedorExpo is not null;

        private bool PuedeAbrirFisico(MensajeEntrante m) =>
            (CoordinadorEstacion?.EsChatAutorizado(m.ChatId, m.TelegramUserId) ?? false) || CanecaConPresencia(m.TelegramUserId) is not null;

        /// <summary>Sesión de estación que abre el QR fijo: durante este tiempo la persona envía fotos y abre sin reescanear.</summary>
        public static readonly TimeSpan VigenciaSesionEstacion = TimeSpan.FromMinutes(15);

        /// <summary>Cuánto espera una foto ya identificada a que la persona escanee el QR para abrirse sola.</summary>
        public static readonly TimeSpan VigenciaPropuestaExpo = TimeSpan.FromMinutes(5);

        /// <summary>Aperturas por persona desde Telegram: evita abusos con el QR permanente (4 por minuto).</summary>
        private readonly ILimitadorUso _aperturasPorUsuario = new LimitadorUsoEnMemoria(4, TimeSpan.FromMinutes(1));

        private bool PermitirApertura(MensajeEntrante m) =>
            _aperturasPorUsuario.Permitir("abrir:" + m.TelegramUserId.ToString(CultureInfo.InvariantCulture), Ahora);

        /// <summary>Usar la estación renueva la sesión del QR: no se pide otro escaneo mientras la persona siga activa.</summary>
        private void RenovarSesionEstacion(long usuario)
        {
            if (CanecaConPresencia(usuario) is int caneca) RegistrarPresencia(usuario, caneca, VigenciaSesionEstacion);
        }

        /// <summary>
        /// QR fijo de la estación (t.me/bot?start=identificador) en Modo Expo: autoriza a la persona por una sesión temporal
        /// sin pedir permiso a un supervisor. Si ya había enviado una foto identificada sin dudas y sigue vigente, la abre en
        /// ese momento (una sola vez, con clave idempotente). Escanear sin residuo identificado NUNCA abre nada.
        /// Devuelve null si el parámetro no es una caneca registrada (sigue el recorrido de siempre).
        /// </summary>
        private async Task<RespuestaBot?> QrEstacionExpoAsync(MensajeEntrante mensaje, int? usuario, string parametro, CancellationToken ct)
        {
            if (!ModoExpo || parametro.Length > 64) return null;
            Contenedor? caneca = await _contenedores.ObtenerPorIdentificadorQrAsync(parametro, ct).ConfigureAwait(false);
            if (caneca is null) return null;
            if (!caneca.EstaEnServicio)
                return ConMenu(Responder(mensaje, "La estación no está en servicio en este momento. Puedes consultar en qué caneca va cada residuo."));
            RegistrarPresencia(mensaje.TelegramUserId, caneca.Id, VigenciaSesionEstacion);
            RegistrarReconocimiento?.Invoke($"Expo: QR de la estación escaneado (caneca {caneca.Id}); sesión de {VigenciaSesionEstacion.TotalMinutes:0} min");

            var pendiente = TomarUltimaPropuesta(mensaje.TelegramUserId);
            if (pendiente is { } p && p.Canecas.Count > 0 && p.Reconocimiento is { PuedeAbrirAutomaticamente: true })
            {
                if (!PermitirApertura(mensaje))
                    return ConMenu(Responder(mensaje, "Se alcanzó el límite de aperturas. Espera un minuto y vuelve a intentarlo."));
                var res = await EstacionExpo!.EjecutarAsync("qr:" + p.Token, CanalEstacion.TELEGRAM, TipoOperacionEstacion.RECONOCIMIENTO_AUTOMATICO,
                    p.Canecas, usuario, SolicitanteTelegram(mensaje, usuario), p.Reconocimiento, ct, esperarRegistro: false).ConfigureAwait(false);
                ProgramarFinDeCiclo(mensaje, res, usuario, p.Reconocimiento);
                return new RespuestaBot(mensaje.ChatId, res.AperturaEnviada ? TextoApertura(res) : TextoClasificacion(p.Reconocimiento) + "\n\n" + TextoApertura(res), BotonesTrasApertura(res))
                    { Imagen = res.AperturaEnviada ? ImagenCanecas(res.Canecas) : null };
            }
            if (pendiente is { } dudosa && dudosa.Reconocimiento is { } recDudoso)
            {
                // La foto tenía algo dudoso: nunca se abre sola; se ofrece confirmar con un toque (la sesión ya está activa).
                string token = GuardarPropuesta(mensaje.TelegramUserId, dudosa.Canecas, recDudoso);
                var botones = new List<BotonBot>();
                if (dudosa.Canecas.Count > 0)
                    botones.Add(new BotonBot($"✅ Abrir {string.Join(" + ", dudosa.Canecas.Select(c => MotorReglasMultirresiduo.NombreCortoCaneca(c).ToLowerInvariant()))}", PrefijoExpo + "abrir:" + token));
                botones.Add(new BotonBot("🗑️ Elegir canecas", PrefijoMenu + "canecas"));
                return new RespuestaBot(mensaje.ChatId, "✅ Estación vinculada.\n\n" + recDudoso.Resumen() + "\n\nConfirma antes de abrir: hay un objeto que no reconocí con seguridad.", botones);
            }
            return new RespuestaBot(mensaje.ChatId,
                $"✅ Estación vinculada. Durante {VigenciaSesionEstacion.TotalMinutes:0} minutos puedes enviarme fotos de tus residuos y abriré la caneca correcta sin volver a escanear.",
                new[] { new BotonBot("📸 Enviar una foto", PrefijoMenu + "enviarfoto"), new BotonBot("🗑️ Elegir canecas", PrefijoMenu + "canecas"), BotonMenu });
        }

        /// <summary>La foto más reciente de la persona que sigue esperando apertura (se retira: un solo uso).</summary>
        private (string Token, IReadOnlyList<CategoriaMaterial> Canecas, ResultadoExpo9? Reconocimiento)? TomarUltimaPropuesta(long usuario)
        {
            lock (_propuestasExpo)
            {
                var vigentes = _propuestasExpo.Where(x => x.Value.Usuario == usuario && x.Value.HastaUtc > Ahora).OrderByDescending(x => x.Value.HastaUtc).ToList();
                if (vigentes.Count == 0) return null;
                foreach (var v in vigentes) _propuestasExpo.Remove(v.Key);   // una sola apertura aunque haya varias fotos pendientes
                var p = vigentes[0];
                return (p.Key, p.Value.Canecas, p.Value.Reconocimiento);
            }
        }

        private async Task<RespuestaBot> FotoExpoAsync(MensajeEntrante mensaje, int? usuario, CancellationToken ct)
        {
            if (_descargarFoto is null || Compat.IsNullOrWhiteSpace(mensaje.ArchivoFoto) || mensaje.ArchivoFoto!.Length > 512)
                return Responder(mensaje, "La referencia de fotografía no es válida.");
            if (!_fotosPorUsuario.Permitir("foto:" + mensaje.TelegramUserId.ToString(CultureInfo.InvariantCulture), Ahora) || !_fotosGlobales.Permitir("fotos", Ahora))
                return Responder(mensaje, "Se alcanzó el límite de fotografías. Espera un minuto o abre la caneca manualmente.");
            var reloj = System.Diagnostics.Stopwatch.StartNew();
            ImagenResiduo foto;
            try
            {
                using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct); limite.CancelAfter(TimeSpan.FromSeconds(15));
                foto = await _descargarFoto(mensaje.ArchivoFoto!, limite.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception error)
            {
                RegistrarReconocimiento?.Invoke($"Expo: descarga fallida ({error.GetType().Name})");
                return new RespuestaBot(mensaje.ChatId, "No pude descargar la foto. Envíala otra vez o abre la caneca manualmente.", BotonesCanecasExpo());
            }
            long descargaMs = reloj.ElapsedMilliseconds;
            ResultadoExpo9 r = await ReconocedorExpo!.ReconocerAsync(foto.Contenido, ct).ConfigureAwait(false);
            RegistrarReconocimiento?.Invoke(FormattableString.Invariant(
                $"Expo: descarga={descargaMs}ms preproceso={r.Medicion.PreprocesoMs}ms inferencia={r.Medicion.InferenciaMs}ms validacion={r.Medicion.ValidacionMs}ms cache={r.Medicion.DesdeCache} estado={r.Estado} canecas={ClienteEstacionPanel.Letras(r.Canecas)}"));
            string resumen = r.Resumen();
            if (r.Estado == EstadoReconocimientoExpo.Error)
                return new RespuestaBot(mensaje.ChatId, resumen, BotonesCanecasExpo());
            if (r.Estado is EstadoReconocimientoExpo.GestionEspecial or EstadoReconocimientoExpo.SinMaterialesExpo)
                return new RespuestaBot(mensaje.ChatId, resumen, new[] { new BotonBot("🗑️ Abrir caneca manualmente", PrefijoMenu + "canecas"), BotonMenu });

            bool fisico = PuedeAbrirFisico(mensaje);
            string clave = "tg:" + mensaje.UpdateId.ToString(CultureInfo.InvariantCulture);
            if (r.PuedeAbrirAutomaticamente && fisico)
            {
                if (!PermitirApertura(mensaje))
                    return ConMenu(Responder(mensaje, "Se alcanzó el límite de aperturas. Espera un minuto y vuelve a intentarlo."));
                RenovarSesionEstacion(mensaje.TelegramUserId);
                var res = await EstacionExpo!.EjecutarAsync(clave, CanalEstacion.TELEGRAM, TipoOperacionEstacion.RECONOCIMIENTO_AUTOMATICO, r.Canecas, usuario,
                    SolicitanteTelegram(mensaje, usuario), r, ct, esperarRegistro: false).ConfigureAwait(false);
                RegistrarReconocimiento?.Invoke(FormattableString.Invariant($"Expo: orden al ESP32 a los {reloj.ElapsedMilliseconds} ms de iniciar la descarga (descarga {descargaMs} + reconocimiento {r.Medicion.TotalMs} + estación) · apertura={res.AperturaEnviada}"));
                ProgramarFinDeCiclo(mensaje, res, usuario, r);
                return new RespuestaBot(mensaje.ChatId, res.AperturaEnviada ? TextoApertura(res) : TextoClasificacion(r) + "\n\n" + TextoApertura(res), BotonesTrasApertura(res))
                    { Imagen = res.AperturaEnviada ? ImagenCanecas(res.Canecas) : null };
            }

            // Hay algo dudoso (p. ej. papel blanco que puede ser servilleta usada) o la persona no está en la estación:
            // se muestra el resultado y la apertura queda a un toque, nunca automática.
            string token = GuardarPropuesta(mensaje.TelegramUserId, r.Canecas, r);
            var botones = new List<BotonBot>();
            if (!fisico)
            {
                botones.Add(new BotonBot("📱 Escanear el QR de la estación", PrefijoMenu + "qr"));
                botones.Add(BotonMenu);
                // La clasificación educativa se responde igual; solo la apertura física exige estar en la estación.
                string clasif = r.PuedeAbrirAutomaticamente ? TextoClasificacion(r) : resumen;
                return new RespuestaBot(mensaje.ChatId, clasif + "\n\nEscanea el QR de la estación y la caneca se abrirá sola.", botones)
                    { Imagen = r.PuedeAbrirAutomaticamente ? ImagenCanecas(r.Canecas) : null };
            }
            if (r.Canecas.Count > 0)
                botones.Add(new BotonBot($"✅ Abrir {string.Join(" + ", r.Canecas.Select(c => MotorReglasMultirresiduo.NombreCortoCaneca(c).ToLowerInvariant()))}", PrefijoExpo + "abrir:" + token));
            if (r.Desconocidos.Any(EsPapelDudoso) && !r.Canecas.Contains(CategoriaMaterial.NoAprovechable))
                botones.Add(new BotonBot("🧻 Es servilleta o papel higiénico usado (+ negra)", PrefijoExpo + "abrirN:" + token));
            botones.Add(new BotonBot("🗑️ Elegir canecas", PrefijoMenu + "canecas"));
            return new RespuestaBot(mensaje.ChatId, resumen + "\n\nConfirma antes de abrir: hay un objeto que no reconocí con seguridad.", botones);
        }

        /// <summary>Respuesta breve: residuo(s), categoría(s), caneca(s) y tiempo. Ejemplo: «Residuo identificado: botella plástica.»</summary>
        internal static string TextoClasificacion(ResultadoExpo9 r)
        {
            // Una línea por residuo con el color de su caneca: «🟢 Papa detectada.», «⚪ Papel aluminio detectado.»
            var sb = new StringBuilder();
            foreach (var m in r.Materiales)
            {
                string nombre = Capital(m.Material.NombreCorto) + (m.Cantidad > 1 ? " (×" + m.Cantidad.ToString(CultureInfo.InvariantCulture) + ")" : "");
                sb.AppendLine($"{PuntoCaneca(m.Material.Caneca)} {nombre} {(Femenino(m.Material) ? "detectada" : "detectado")}.");
            }
            if (r.Desconocidos.Count > 0) sb.AppendLine("❔ Hay un objeto que no reconocí: no lo deposites en estas canecas.");
            return sb.ToString().TrimEnd();
        }

        internal static string PuntoCaneca(CategoriaMaterial c) => c switch
        {
            CategoriaMaterial.OrganicoAprovechable => "🟢",
            CategoriaMaterial.Aprovechable => "⚪",
            _ => "⚫",
        };

        private static string Capital(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        // Concordancia de «detectado/detectada» según el sustantivo principal del nombre corto.
        private static bool Femenino(MaterialExpo m) => System.Text.RegularExpressions.Regex.IsMatch(m.NombreCorto,
            @"^(papa|manzana|c[aá]scara|servilleta|media|botella|bolsa|envoltura|caja|lata|tapa)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>Frase de las canecas: «Caneca verde», «Canecas verde y blanca», «Las tres canecas».</summary>
        private static string FraseCanecas(IReadOnlyList<CategoriaMaterial> canecas)
        {
            var n = canecas.Select(c => MotorReglasMultirresiduo.NombreCortoCaneca(c).ToLowerInvariant()).ToList();
            return n.Count switch
            {
                1 => "Caneca " + n[0],
                3 => "Las tres canecas",
                _ => "Canecas " + string.Join(" y ", n),
            };
        }

        /// <summary>Imagen oficial de ECO según las canecas: una → ECO de ese color; dos → composición de esos dos ECO
        /// (eco_verde_blanca, eco_verde_negra, eco_blanca_negra); tres → las tres canecas.</summary>
        internal static string? ImagenCanecas(IReadOnlyList<CategoriaMaterial> canecas)
        {
            static string Clave(CategoriaMaterial c) => c switch { CategoriaMaterial.OrganicoAprovechable => "verde", CategoriaMaterial.Aprovechable => "blanca", _ => "negra" };
            static int Orden(CategoriaMaterial c) => c switch { CategoriaMaterial.OrganicoAprovechable => 0, CategoriaMaterial.Aprovechable => 1, _ => 2 };
            var n = canecas.Distinct().OrderBy(Orden).ToList();
            return n.Count switch
            {
                0 => null,
                1 => "eco_" + Clave(n[0]),
                2 => "eco_" + Clave(n[0]) + "_" + Clave(n[1]),
                _ => "eco_canecas",
            };
        }

        private static bool EsPapelDudoso(string nombre) =>
            System.Text.RegularExpressions.Regex.IsMatch(nombre, @"\b(tissue|paper|crumpled|white)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>Botones «expo:…»: confirmar una propuesta, abrir manualmente o declarar el depósito.</summary>
        private async Task<RespuestaBot> BotonExpoAsync(MensajeEntrante mensaje, int? usuario, string dato, CancellationToken ct)
        {
            if (EstacionExpo is null) return Menu(mensaje, MensajesTelegram.NoEntendido);
            string accion = dato.Substring(PrefijoExpo.Length);
            if (accion.StartsWith("dep:", StringComparison.Ordinal))
            {
                string claveOp = accion.Substring(4);
                if (!claveOp.StartsWith("tg:", StringComparison.Ordinal)) return Responder(mensaje, MensajesTelegram.NoEntendido);
                var r0 = new ResultadoOperacionExpo(true, EstacionExpo.Estacion.EsSimulada, false, false, Array.Empty<CategoriaMaterial>(), 0, EstacionExpo.Modalidad, "", claveOp);
                bool ok = await EstacionExpo.MarcarCicloCompletadoAsync(r0, CanalEstacion.TELEGRAM, TipoOperacionEstacion.DISPOSICION_DECLARADA, usuario, SolicitanteTelegram(mensaje, usuario), null, true, ct).ConfigureAwait(false);
                return ConMenu(Responder(mensaje, ok
                    ? "🙌 Gracias. Tu disposición quedó registrada en Oracle como declarada por ti (las tapas no tienen sensores que la verifiquen)."
                    : "🙌 Gracias. Tu disposición quedó guardada en este equipo y se registrará en Oracle cuando vuelva la conexión."));
            }
            if (!PuedeAbrirFisico(mensaje))
                return new RespuestaBot(mensaje.ChatId, "Para abrir las canecas, escanea el QR de la estación.",
                    new[] { new BotonBot("📱 Escanear el QR de la estación", PrefijoMenu + "qr"), BotonMenu });

            IReadOnlyList<CategoriaMaterial> canecas; ResultadoExpo9? rec = null; TipoOperacionEstacion tipo;
            if (accion.StartsWith("man:", StringComparison.Ordinal))
            {
                canecas = DesdeLetras(accion.Substring(4)); tipo = TipoOperacionEstacion.APERTURA_MANUAL;
            }
            else if ((accion.StartsWith("abrir:", StringComparison.Ordinal) || accion.StartsWith("abrirN:", StringComparison.Ordinal))
                && TomarPropuesta(mensaje.TelegramUserId, accion.Substring(accion.IndexOf(':') + 1)) is { } p)
            {
                var lista = p.Canecas.ToList();
                if (accion.StartsWith("abrirN:", StringComparison.Ordinal)) lista.Add(CategoriaMaterial.NoAprovechable);
                canecas = MotorCombinacionesCanecas.CalcularCanecasUnicas(lista); rec = p.Reconocimiento; tipo = TipoOperacionEstacion.RECONOCIMIENTO_AUTOMATICO;
            }
            else return ConMenu(Responder(mensaje, "Esa propuesta ya venció o ya se usó. Envía otra foto o elige las canecas."));

            if (!PermitirApertura(mensaje))
                return ConMenu(Responder(mensaje, "Se alcanzó el límite de aperturas. Espera un minuto y vuelve a intentarlo."));
            RenovarSesionEstacion(mensaje.TelegramUserId);
            string clave = "tg:" + mensaje.UpdateId.ToString(CultureInfo.InvariantCulture);
            var res = await EstacionExpo.EjecutarAsync(clave, CanalEstacion.TELEGRAM, tipo, canecas, usuario, SolicitanteTelegram(mensaje, usuario), rec, ct, esperarRegistro: false).ConfigureAwait(false);
            ProgramarFinDeCiclo(mensaje, res, usuario, rec);
            return new RespuestaBot(mensaje.ChatId, TextoApertura(res), BotonesTrasApertura(res)) { Imagen = res.AperturaEnviada ? ImagenCanecas(res.Canecas) : null };
        }

        /// <summary>/verde, /blanca, /negra y /abrir … en Modo Expo: misma estación, misma política de tiempos, mismo registro.</summary>
        private async Task<RespuestaBot> ComandoCanecasExpoAsync(MensajeEntrante mensaje, int? usuario, IReadOnlyList<CategoriaMaterial> categorias, CancellationToken ct)
        {
            if (!PuedeAbrirFisico(mensaje))
                return ConMenu(Responder(mensaje, "Para abrir las canecas, escanea primero el QR de la estación."));
            string clave = "tg:" + mensaje.UpdateId.ToString(CultureInfo.InvariantCulture);
            var res = await EstacionExpo!.EjecutarAsync(clave, CanalEstacion.TELEGRAM, TipoOperacionEstacion.APERTURA_MANUAL, categorias, usuario, SolicitanteTelegram(mensaje, usuario), null, ct, esperarRegistro: false).ConfigureAwait(false);
            ProgramarFinDeCiclo(mensaje, res, usuario, null);
            return new RespuestaBot(mensaje.ChatId, TextoApertura(res), BotonesTrasApertura(res)) { Imagen = res.AperturaEnviada ? ImagenCanecas(res.Canecas) : null };
        }

        private RespuestaBot MenuCanecasExpo(MensajeEntrante mensaje) =>
            new(mensaje.ChatId, "¿Qué canecas quieres abrir?\nUna caneca: 7 s · dos: 14 s · tres: 21 s (se abren a la vez).", BotonesCanecasExpo()) { Columnas = 2 };

        private static IReadOnlyList<BotonBot> BotonesCanecasExpo() => new[]
        {
            new BotonBot("🟢 Verde", PrefijoExpo + "man:V"), new BotonBot("⚪ Blanca", PrefijoExpo + "man:B"),
            new BotonBot("⚫ Negra", PrefijoExpo + "man:N"), new BotonBot("🟢⚪ Verde + Blanca", PrefijoExpo + "man:VB"),
            new BotonBot("🟢⚫ Verde + Negra", PrefijoExpo + "man:VN"), new BotonBot("⚪⚫ Blanca + Negra", PrefijoExpo + "man:BN"),
            new BotonBot("🟢⚪⚫ Las tres", PrefijoExpo + "man:VBN"), BotonMenu,
        };

        /// <summary>«Botella plástica — blanca» por cada material de la columna MATERIALES («E05 botella plástica, E02 … x2 | …»).</summary>
        internal static IEnumerable<string> LineasHistorial(OperacionEstacion o)
        {
            if (string.IsNullOrEmpty(o.Materiales)) yield break;
            string reconocidos = o.Materiales!.Split('|')[0];
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(reconocidos.Trim(), @"\b(E0[1-9])\b[^,]*?(?:\bx(\d+))?\s*(?=,|$)"))
            {
                if (CatalogoExpo9.PorCodigo(m.Groups[1].Value) is not { } mat) continue;
                string nombre = char.ToUpperInvariant(mat.NombreCorto[0]) + mat.NombreCorto.Substring(1);
                string caneca = MotorReglasMultirresiduo.NombreCortoCaneca(mat.Caneca).ToLowerInvariant();
                yield return $"{nombre}{(m.Groups[2].Success ? " ×" + m.Groups[2].Value : "")} — {caneca}";
            }
        }

        private async Task<RespuestaBot?> HistorialExpoAsync(MensajeEntrante mensaje, int? usuario, CancellationToken ct)
        {
            var oracle = EstacionExpo?.Registro.Oracle;
            if (oracle is null) return null;
            bool supervisor = CoordinadorEstacion?.EsChatAutorizado(mensaje.ChatId, mensaje.TelegramUserId) ?? false;
            if (usuario is null && !supervisor) return null;
            // Una cuenta vinculada ve SOLO sus operaciones (privacidad); un chat de supervisor sin cuenta ve las de la estación.
            bool propio = usuario is int;
            IReadOnlyList<OperacionEstacion> ops;
            try { ops = await oracle.RecientesAsync(usuario, MaximoConsulta, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception) { return Responder(mensaje, "No pude consultar el historial en Oracle en este momento. Intenta de nuevo en un minuto."); }
            if (ops.Count == 0) return ConMenu(Responder(mensaje, propio ? "Todavía no tienes residuos registrados en Oracle. Envía una foto para empezar. 📸" : "Todavía no hay operaciones de la estación registradas en Oracle."));
            var sb = new StringBuilder(propio ? "📜 Tus residuos (Oracle Cloud):\n" : "📜 Últimas operaciones de la estación (Oracle Cloud):\n");
            var zona = ZonaColombia();
            foreach (var o in ops)
            {
                DateTime local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(o.FechaUtc, DateTimeKind.Utc), zona);
                string cuando = local.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                string sufijo = o.Estado switch
                {
                    EstadoOperacionEstacion.DISPOSICION_CONFIRMADA => " · depósito declarado",
                    EstadoOperacionEstacion.ERROR => " · no se abrió",
                    _ => "",
                } + (propio ? "" : " · " + o.Canal);
                var lineas = LineasHistorial(o).ToList();
                if (lineas.Count == 0)
                    sb.AppendLine($"• {(o.Tipo == TipoOperacionEstacion.APERTURA_MANUAL ? "Apertura manual" : Tipo(o.Tipo))} — {(o.Canecas ?? "-").ToLowerInvariant().Replace(",", " + ")} — {cuando}{sufijo}");
                else
                    foreach (string l in lineas) sb.AppendLine($"• {l} — {cuando}{sufijo}");
            }
            int pend = EstacionExpo!.Registro.Pendientes;
            if (pend > 0) sb.AppendLine($"⏳ {pend} operación(es) de este equipo pendiente(s) de sincronizar con Oracle.");
            return Responder(mensaje, sb.ToString().TrimEnd());
        }

        private async Task<string> EstadoEstacionExpoAsync(CancellationToken ct)
        {
            var e = await EstacionExpo!.EstadoAsync(ct).ConfigureAwait(false);
            string modo = e.Disponible && e.Conectado && e.ControlActivo ? "🟢 En servicio: lista para abrir"
                : e.Disponible && e.Conectado ? "🟡 ESP32 conectado; la estación está en pausa por mantenimiento"
                : EstacionExpo.Estacion.EsSimulada ? "⚪ Simulación (este equipo no tiene canecas físicas)" : "🔴 " + e.Texto;
            return $"⚙️ Estación EcoAccess\n{modo}\nPuerto: {e.Puerto ?? "-"} · {e.Firmware ?? "-"}\nApertura: {(EstacionExpo.Modalidad == "simultanea" ? "simultánea" : "secuencial")} · 1 caneca 7 s · 2 canecas 14 s · 3 canecas 21 s\nReconocimiento: {ReconocedorExpo?.Modelo ?? "-"} ({ReconocedorExpo?.LadoPx ?? 0} px), 11 residuos";
        }

        private void ProgramarFinDeCiclo(MensajeEntrante mensaje, ResultadoOperacionExpo res, int? usuario, ResultadoExpo9? rec)
        {
            if (!res.AperturaEnviada || EstacionExpo is null) return;
            var servicio = EstacionExpo; var enviar = EnviarDiferido;
            int espera = res.Modalidad == "simultanea"
                ? res.DuracionMs + (res.Canecas.Count - 1) * 250 + 1500
                : res.Canecas.Count * (res.DuracionMs + 1200) + 500;
            string solicitante = SolicitanteTelegram(mensaje, usuario);
            _ = Task.Run(async () =>
            {
                await Task.Delay(espera).ConfigureAwait(false);
                // Primero termina el registro de la apertura (EN_EJECUCION) para que el cierre nunca llegue antes que la apertura.
                if (res.RegistroEnCurso is { } enCurso) { try { await enCurso.ConfigureAwait(false); } catch (Exception) { } }
                bool oracle = await servicio.MarcarCicloCompletadoAsync(res, CanalEstacion.TELEGRAM,
                    rec is null ? TipoOperacionEstacion.APERTURA_MANUAL : TipoOperacionEstacion.RECONOCIMIENTO_AUTOMATICO, usuario, solicitante, rec, false, CancellationToken.None).ConfigureAwait(false);
                if (enviar is null) return;
                // «Registro guardado correctamente» solo con confirmación real de Oracle (commit de la fila).
                string texto = res.Simulada ? "✅ Operación completada (simulación).\n¿Depositaste tu residuo?"
                    : (res.Canecas.Count > 1 ? "✅ Canecas cerradas." : "✅ Caneca cerrada.") + " ¿Depositaste tu residuo?";
                try { await enviar(new RespuestaBot(mensaje.ChatId, texto, new[] { new BotonBot("🙌 Sí, deposité", PrefijoExpo + "dep:" + res.Clave), new BotonBot("📸 Otra foto", PrefijoMenu + "enviarfoto") }), CancellationToken.None).ConfigureAwait(false); }
                catch (Exception) { /* el aviso final es informativo */ }
            });
        }

        private static string TextoApertura(ResultadoOperacionExpo r)
        {
            if (r.Duplicada) return "Esta solicitud ya se ejecutó: no se repite la apertura.";
            bool varias = r.Canecas.Count > 1;
            if (!r.AperturaEnviada)
            {
                // Sin confirmación del ESP32 nunca se dice «abierta»; se explica la causa real.
                string que = varias ? "Identifiqué tus residuos, pero no se abrieron las canecas" : "Identifiqué tu residuo, pero no se abrió la caneca";
                string m = r.Mensaje ?? "";
                // Solo una pausa manual (mantenimiento o emergencia) produce este rechazo: la estación arranca en servicio.
                if (m.IndexOf("en pausa", StringComparison.OrdinalIgnoreCase) >= 0 || m.IndexOf("no está en servicio", StringComparison.OrdinalIgnoreCase) >= 0
                    || m.IndexOf("control físico está desactivado", StringComparison.OrdinalIgnoreCase) >= 0)
                    return que + ": la estación está en mantenimiento. Inténtalo de nuevo en unos minutos.";
                if (m.IndexOf("No se pudo contactar", StringComparison.OrdinalIgnoreCase) >= 0)
                    return que + ": la estación no está disponible en este momento. Inténtalo de nuevo en unos minutos.";
                if (m.IndexOf("desconectad", StringComparison.OrdinalIgnoreCase) >= 0 || m.IndexOf("USB", StringComparison.Ordinal) >= 0)
                    return que + ": la estación no está disponible en este momento. Inténtalo de nuevo en unos minutos.";
                return que + ". Inténtalo de nuevo en unos minutos o abre la caneca manualmente.";
            }
            string seg = (r.DuracionMs / 1000).ToString(CultureInfo.InvariantCulture);
            string frase = FraseCanecas(r.Canecas);
            if (r.Simulada) return $"🧪 Simulación: {frase.ToLowerInvariant()} (sin movimiento físico).\nTiempo disponible: {seg} segundos.";
            // «Abierta» = el ESP32 confirmó la orden; sin sensor de posición no se afirma la posición física de la tapa.
            return TextoAbierta(r.Canecas, seg);
        }

        /// <summary>
        /// Mensaje corto que acompaña la imagen de Eco tras una apertura CONFIRMADA por el controlador: color de la caneca y
        /// tiempo disponible. Sin material, confianza, Oracle ni hardware (eso queda en OPERACION_ESTACION y en el panel).
        /// </summary>
        internal static string TextoAbierta(IReadOnlyList<CategoriaMaterial> canecas, string seg)
        {
            var n = canecas.Distinct().ToList();
            static string Emoji(CategoriaMaterial c) => c switch { CategoriaMaterial.OrganicoAprovechable => "🟢", CategoriaMaterial.Aprovechable => "⚪", _ => "⚫" };
            static string Color(CategoriaMaterial c) => MotorReglasMultirresiduo.NombreCortoCaneca(c).ToLowerInvariant();
            return n.Count switch
            {
                1 => $"{Emoji(n[0])} Caneca {Color(n[0])} abierta.\nTienes {seg} segundos para desechar tu residuo.",
                2 => $"{Emoji(n[0])}{Emoji(n[1])} Canecas {Color(n[0])} y {Color(n[1])} abiertas.\nTienes {seg} segundos.",
                _ => $"🟢⚪⚫ Las tres canecas están abiertas.\nTienes {seg} segundos.",
            };
        }

        private static IReadOnlyList<BotonBot> BotonesTrasApertura(ResultadoOperacionExpo r) =>
            new[] { new BotonBot("📸 Otra foto", PrefijoMenu + "enviarfoto"), new BotonBot("🗑️ Abrir otra caneca", PrefijoMenu + "canecas"), BotonMenu };

        private string GuardarPropuesta(long usuario, IReadOnlyList<CategoriaMaterial> canecas, ResultadoExpo9? r)
        {
            string token = Guid.NewGuid().ToString("N").Substring(0, 10);
            lock (_propuestasExpo)
            {
                foreach (var k in _propuestasExpo.Where(x => x.Value.HastaUtc <= Ahora).Select(x => x.Key).ToList()) _propuestasExpo.Remove(k);
                if (_propuestasExpo.Count < 512) _propuestasExpo[token] = (usuario, canecas, r, Ahora + VigenciaPropuestaExpo);
            }
            return token;
        }

        private (IReadOnlyList<CategoriaMaterial> Canecas, ResultadoExpo9? Reconocimiento)? TomarPropuesta(long usuario, string token)
        {
            lock (_propuestasExpo)
            {
                if (!_propuestasExpo.TryGetValue(token, out var p) || p.Usuario != usuario || p.HastaUtc <= Ahora) return null;
                _propuestasExpo.Remove(token);   // un solo uso: un doble toque no abre dos veces
                return (p.Canecas, p.Reconocimiento);
            }
        }

        private static IReadOnlyList<CategoriaMaterial> DesdeLetras(string letras) => MotorCombinacionesCanecas.CalcularCanecasUnicas(
            letras.ToUpperInvariant().Where(c => c is 'V' or 'B' or 'N').Select(c => c switch { 'V' => CategoriaMaterial.OrganicoAprovechable, 'B' => CategoriaMaterial.Aprovechable, _ => CategoriaMaterial.NoAprovechable }));

        private static string SolicitanteTelegram(MensajeEntrante m, int? usuario) => usuario is int u ? "usuario:" + u.ToString(CultureInfo.InvariantCulture) : "telegram-invitado";

        private static string Tipo(TipoOperacionEstacion t) => t switch
        {
            TipoOperacionEstacion.RECONOCIMIENTO_AUTOMATICO => "foto",
            TipoOperacionEstacion.APERTURA_MANUAL => "manual",
            TipoOperacionEstacion.DISPOSICION_DECLARADA => "depósito declarado",
            _ => "prueba técnica",
        };

        private static string Estado(EstadoOperacionEstacion e) => e switch
        {
            EstadoOperacionEstacion.CICLO_COMPLETADO => "ciclo completado",
            EstadoOperacionEstacion.DISPOSICION_CONFIRMADA => "depósito declarado",
            EstadoOperacionEstacion.EN_EJECUCION => "en ejecución",
            EstadoOperacionEstacion.ERROR => "no se abrió",
            _ => e.ToString().ToLowerInvariant().Replace('_', ' '),
        };

        private static TimeZoneInfo ZonaColombia()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); }
            catch (Exception) { return TimeZoneInfo.CreateCustomTimeZone("COT", TimeSpan.FromHours(-5), "Colombia", "Colombia"); }
        }
    }
}
