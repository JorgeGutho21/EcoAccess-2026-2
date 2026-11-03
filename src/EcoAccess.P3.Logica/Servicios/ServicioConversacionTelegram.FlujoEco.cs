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
