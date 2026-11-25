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
    /// apertura simultánea 5/7/10 s en la estación (panel local, único dueño del ESP32) → registro en Oracle Cloud.
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
                var res = await EstacionExpo!.EjecutarAsync(clave, CanalEstacion.TELEGRAM, TipoOperacionEstacion.RECONOCIMIENTO_AUTOMATICO, r.Canecas, usuario,
                    SolicitanteTelegram(mensaje, usuario), r, ct).ConfigureAwait(false);
                RegistrarReconocimiento?.Invoke(FormattableString.Invariant($"Expo: total hasta la orden al ESP32 = {reloj.ElapsedMilliseconds} ms · apertura={res.AperturaEnviada} oracle={res.RegistradaEnOracle}"));
                ProgramarFinDeCiclo(mensaje, res, usuario, r);
                return new RespuestaBot(mensaje.ChatId, resumen + "\n\n" + TextoApertura(res), BotonesTrasApertura(res));
            }

            // Hay algo dudoso (p. ej. papel blanco que puede ser servilleta usada) o la persona no está en la estación:
            // se muestra el resultado y la apertura queda a un toque, nunca automática.
            string token = GuardarPropuesta(mensaje.TelegramUserId, r.Canecas, r);
            var botones = new List<BotonBot>();
            if (!fisico)
            {
                botones.Add(new BotonBot("📱 Escanear el QR de la estación", PrefijoMenu + "qr"));
                botones.Add(BotonMenu);
                return new RespuestaBot(mensaje.ChatId, resumen + "\n\nPara abrir las canecas, escanea el QR de la estación o pide a un supervisor que autorice este chat.", botones);
            }
            if (r.Canecas.Count > 0)
                botones.Add(new BotonBot($"✅ Abrir {string.Join(" + ", r.Canecas.Select(c => MotorReglasMultirresiduo.NombreCortoCaneca(c).ToLowerInvariant()))}", PrefijoExpo + "abrir:" + token));
            if (r.Desconocidos.Any(EsPapelDudoso) && !r.Canecas.Contains(CategoriaMaterial.NoAprovechable))
                botones.Add(new BotonBot("🧻 Es servilleta o papel higiénico usado (+ negra)", PrefijoExpo + "abrirN:" + token));
            botones.Add(new BotonBot("🗑️ Elegir canecas", PrefijoMenu + "canecas"));
            return new RespuestaBot(mensaje.ChatId, resumen + "\n\nConfirma antes de abrir: hay un objeto que no reconocí con seguridad.", botones);
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
                return new RespuestaBot(mensaje.ChatId, "Para abrir las canecas, escanea el QR de la estación o pide a un supervisor que autorice este chat.",
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

            string clave = "tg:" + mensaje.UpdateId.ToString(CultureInfo.InvariantCulture);
            var res = await EstacionExpo.EjecutarAsync(clave, CanalEstacion.TELEGRAM, tipo, canecas, usuario, SolicitanteTelegram(mensaje, usuario), rec, ct).ConfigureAwait(false);
            ProgramarFinDeCiclo(mensaje, res, usuario, rec);
            return new RespuestaBot(mensaje.ChatId, TextoApertura(res), BotonesTrasApertura(res));
        }

        /// <summary>/verde, /blanca, /negra y /abrir … en Modo Expo: misma estación, misma política de tiempos, mismo registro.</summary>
        private async Task<RespuestaBot> ComandoCanecasExpoAsync(MensajeEntrante mensaje, int? usuario, IReadOnlyList<CategoriaMaterial> categorias, CancellationToken ct)
        {
            if (!PuedeAbrirFisico(mensaje))
                return ConMenu(Responder(mensaje, "Operación denegada: este chat no está autorizado para abrir canecas. Escanea el QR de la estación o pide autorización al supervisor."));
            string clave = "tg:" + mensaje.UpdateId.ToString(CultureInfo.InvariantCulture);
            var res = await EstacionExpo!.EjecutarAsync(clave, CanalEstacion.TELEGRAM, TipoOperacionEstacion.APERTURA_MANUAL, categorias, usuario, SolicitanteTelegram(mensaje, usuario), null, ct).ConfigureAwait(false);
            ProgramarFinDeCiclo(mensaje, res, usuario, null);
            return new RespuestaBot(mensaje.ChatId, TextoApertura(res), BotonesTrasApertura(res));
        }

        private RespuestaBot MenuCanecasExpo(MensajeEntrante mensaje) =>
            new(mensaje.ChatId, "¿Qué canecas quieres abrir?\nUna caneca: 5 s · dos: 7 s · tres: 10 s (se abren a la vez).", BotonesCanecasExpo()) { Columnas = 2 };

        private static IReadOnlyList<BotonBot> BotonesCanecasExpo() => new[]
        {
            new BotonBot("🟢 Verde", PrefijoExpo + "man:V"), new BotonBot("⚪ Blanca", PrefijoExpo + "man:B"),
            new BotonBot("⚫ Negra", PrefijoExpo + "man:N"), new BotonBot("🟢⚪ Verde + Blanca", PrefijoExpo + "man:VB"),
            new BotonBot("🟢⚫ Verde + Negra", PrefijoExpo + "man:VN"), new BotonBot("⚪⚫ Blanca + Negra", PrefijoExpo + "man:BN"),
            new BotonBot("🟢⚪⚫ Las tres", PrefijoExpo + "man:VBN"), BotonMenu,
        };

        private async Task<RespuestaBot?> HistorialExpoAsync(MensajeEntrante mensaje, int? usuario, CancellationToken ct)
        {
            var oracle = EstacionExpo?.Registro.Oracle;
            if (oracle is null) return null;
            bool supervisor = CoordinadorEstacion?.EsChatAutorizado(mensaje.ChatId, mensaje.TelegramUserId) ?? false;
            if (usuario is null && !supervisor) return null;
            IReadOnlyList<OperacionEstacion> ops;
            try { ops = await oracle.RecientesAsync(usuario is int u && !supervisor ? u : (int?)null, MaximoConsulta, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception) { return Responder(mensaje, "No pude consultar el historial en Oracle en este momento. Intenta de nuevo en un minuto."); }
            if (ops.Count == 0) return Responder(mensaje, "Todavía no hay operaciones de la estación registradas en Oracle.");
            var sb = new StringBuilder(supervisor ? "📜 Últimas operaciones de la estación (Oracle Cloud):\n" : "📜 Tus últimas operaciones (Oracle Cloud):\n");
            var zona = ZonaColombia();
            foreach (var o in ops)
            {
                DateTime local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(o.FechaUtc, DateTimeKind.Utc), zona);
                sb.AppendLine($"• {local:dd/MM HH:mm} · {o.Canal} · {Tipo(o.Tipo)} · {(o.Canecas ?? "-").ToLowerInvariant().Replace(",", " + ")} · {o.DuracionMs / 1000} s · {Estado(o.Estado)}"
                    + (string.IsNullOrEmpty(o.Materiales) ? "" : $"\n   {o.Materiales}"));
            }
            int pend = EstacionExpo!.Registro.Pendientes;
            if (pend > 0) sb.AppendLine($"⏳ {pend} operación(es) de este equipo pendiente(s) de sincronizar con Oracle.");
            return Responder(mensaje, sb.ToString().TrimEnd());
        }

        private async Task<string> EstadoEstacionExpoAsync(CancellationToken ct)
        {
            var e = await EstacionExpo!.EstadoAsync(ct).ConfigureAwait(false);
            string modo = e.Disponible && e.Conectado && e.ControlActivo ? "🟢 Lista para abrir (control físico activo)"
                : e.Disponible && e.Conectado ? "🟡 ESP32 conectado; el control físico está desactivado en la estación"
                : EstacionExpo.Estacion.EsSimulada ? "⚪ Simulación (este equipo no tiene canecas físicas)" : "🔴 " + e.Texto;
            return $"⚙️ Estación EcoAccess\n{modo}\nPuerto: {e.Puerto ?? "-"} · {e.Firmware ?? "-"}\nApertura: {(EstacionExpo.Modalidad == "simultanea" ? "simultánea" : "secuencial")} · 1 caneca 5 s · 2 canecas 7 s · 3 canecas 10 s\nReconocimiento: {ReconocedorExpo?.Modelo ?? "-"} ({ReconocedorExpo?.LadoPx ?? 0} px), 9 materiales";
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
                bool oracle = await servicio.MarcarCicloCompletadoAsync(res, CanalEstacion.TELEGRAM,
                    rec is null ? TipoOperacionEstacion.APERTURA_MANUAL : TipoOperacionEstacion.RECONOCIMIENTO_AUTOMATICO, usuario, solicitante, rec, false, CancellationToken.None).ConfigureAwait(false);
                if (enviar is null) return;
                string texto = $"✅ Ciclo terminado: el ESP32 ordenó cerrar {string.Join(", ", res.Canecas.Select(c => MotorReglasMultirresiduo.NombreCortoCaneca(c).ToLowerInvariant()))}."
                    + (res.Simulada ? " (simulación)" : "")
                    + (oracle ? "\n🗄️ Registro guardado en Oracle Cloud." : "\n⏳ Registro pendiente de sincronización con Oracle (se enviará solo).")
                    + "\n¿Depositaste tu residuo?";
                try { await enviar(new RespuestaBot(mensaje.ChatId, texto, new[] { new BotonBot("🙌 Sí, deposité", PrefijoExpo + "dep:" + res.Clave), new BotonBot("📸 Otra foto", PrefijoMenu + "enviarfoto") }), CancellationToken.None).ConfigureAwait(false); }
                catch (Exception) { /* el aviso final es informativo */ }
            });
        }

        private static string TextoApertura(ResultadoOperacionExpo r)
        {
            if (r.Duplicada) return "Esta solicitud ya se ejecutó: no se repite la apertura.";
            if (!r.AperturaEnviada) return "⚠️ No se abrió ninguna caneca: " + r.Mensaje + (r.RegistradaEnOracle ? "\n🗄️ El intento quedó registrado en Oracle." : "");
            string nombres = string.Join(", ", r.Canecas.Select(c => MotorReglasMultirresiduo.NombreCortoCaneca(c).ToLowerInvariant()));
            string modo = r.Canecas.Count == 1 ? "" : r.Modalidad == "simultanea" ? " a la vez" : " una tras otra";
            return (r.Simulada ? "🧪 Simulación: " : "🔓 ") + $"Abriendo {nombres}{modo} durante {r.DuracionMs / 1000} segundos."
                + (r.Simulada ? "" : "\n(El ESP32 confirmó la orden; la apertura física se comprueba mirando la tapa.)")
                + (r.RegistradaEnOracle ? "\n🗄️ Operación registrada en Oracle Cloud." : "\n⏳ Registro pendiente de sincronización con Oracle.");
        }

        private static IReadOnlyList<BotonBot> BotonesTrasApertura(ResultadoOperacionExpo r) =>
            new[] { new BotonBot("📸 Otra foto", PrefijoMenu + "enviarfoto"), new BotonBot("🗑️ Abrir otra caneca", PrefijoMenu + "canecas"), BotonMenu };

        private string GuardarPropuesta(long usuario, IReadOnlyList<CategoriaMaterial> canecas, ResultadoExpo9? r)
        {
            string token = Guid.NewGuid().ToString("N").Substring(0, 10);
            lock (_propuestasExpo)
            {
                foreach (var k in _propuestasExpo.Where(x => x.Value.HastaUtc <= Ahora).Select(x => x.Key).ToList()) _propuestasExpo.Remove(k);
                if (_propuestasExpo.Count < 512) _propuestasExpo[token] = (usuario, canecas, r, Ahora.AddMinutes(3));
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
