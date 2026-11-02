using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot.Telegram
{

    /// <summary>Presentación de Eco sobre el diálogo existente; no consulta DAL ni autoriza operaciones.</summary>
    public sealed class AsistenteEco
    {
        private const string ConsultaInformativa = "/eco_informacion";
        private readonly Func<MensajeEntrante, CancellationToken, Task<IReadOnlyList<RespuestaBot>>> _atender;
        private readonly Func<RespuestaBot, CancellationToken, Task>? _enviarProgreso;
        private readonly AsyncLocal<ContextoFoto?> _contexto = new();
        private sealed class ContextoFoto
        {
            public ContextoFoto(long chat)
            {
                Chat = chat;
            }

            internal long Chat { get; }
            internal int Avisada;
        }

        public AsistenteEco(Func<MensajeEntrante, CancellationToken, Task<IReadOnlyList<RespuestaBot>>> atender,
            Func<RespuestaBot, CancellationToken, Task>? enviarProgreso = null)
        {
            _atender = atender ?? throw new ArgumentNullException(nameof(atender));
            _enviarProgreso = enviarProgreso;
        }

        public async Task<IReadOnlyList<RespuestaBot>> ProcesarAsync(MensajeEntrante mensaje, CancellationToken ct = default)
        {
            Compat.ThrowIfNull(mensaje, nameof(mensaje));
            if (!mensaje.ChatPrivado || mensaje.ChatId != mensaje.TelegramUserId || mensaje.TelegramUserId <= 0) return Array.Empty<RespuestaBot>();
            IntencionEco? intencion = mensaje.ArchivoFoto is null && mensaje.DatoBoton is null
                ? BaseConocimientoEco.Resolver(mensaje.Texto == "/acerca" ? "acerca" : mensaje.Texto) : null;
            if (mensaje.DatoBoton == "eco:privacidad" && mensaje.ArchivoFoto is null)
                intencion = new(null, BaseConocimientoEco.Privacidad);
            MensajeEntrante entrada = intencion is null ? mensaje : mensaje with
            {
                Texto = intencion.Boton is null ? intencion.Comando ?? ConsultaInformativa : null,
                DatoBoton = intencion.Boton
            };
            ContextoFoto? anterior = _contexto.Value;
            _contexto.Value = new(mensaje.ChatId);
            try
            {
                // La BLL conserva orden por conversación, deduplicación, límites, permisos y persistencia.
                IReadOnlyList<RespuestaBot> respuestas = await _atender(entrada, ct).ConfigureAwait(false);
                return respuestas.Select(r => Presentar(intencion?.Respuesta is { } texto && r.Texto == MensajesTelegram.NoEntendido
                    ? r with { Texto = texto, Botones = new BotonBot[] { new("🏠 Menú", "menu:inicio") } } : r)).ToArray();
            }
            finally { _contexto.Value = anterior; }
        }

        /// <summary>Se llama desde la descarga que la BLL permite después de validar el consentimiento.</summary>
        public async Task NotificarAnalisisFotoAsync(CancellationToken ct)
        {
            if (_contexto.Value is not { } contexto || _enviarProgreso is null || Interlocked.Exchange(ref contexto.Avisada, 1) != 0) return;
            try { await _enviarProgreso(new(contexto.Chat, MensajesTelegram.FotoRecibida), ct).ConfigureAwait(false); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception) { /* La falta de un aviso no vuelve a enviar ni clasificar la fotografía. */ }
        }

        private static RespuestaBot Presentar(RespuestaBot respuesta)
        {
            string texto = respuesta.Texto == MensajesTelegram.GuiaFoto
                ? "📸 Envíame una foto de tu residuo.\nSi prefieres, elige el material con /manual. 🌱" : respuesta.Texto;
            bool confirmaOperacion = respuesta.Botones.Any(b => b.Dato == ServicioConversacionTelegram.ConfirmarSi);
            var botones = respuesta.Botones.Select(b => b with
            {
                Texto = b.Dato switch
                {
                    ServicioConversacionTelegram.ConfirmarSi => "✅ Sí, es correcto",
                    ServicioConversacionTelegram.ConfirmarNo when confirmaOperacion => "✏️ Cambiar material",
                    _ when b.Dato.StartsWith(ServicioConversacionTelegram.PrefijoConsentirFoto, StringComparison.Ordinal) => "Soy mayor; analizar",
                    _ when b.Dato.StartsWith(ServicioConversacionTelegram.PrefijoAutorizarExterna, StringComparison.Ordinal) => "Soy mayor; autorizo el envío",
                    "menu:/estado" => "📍 Mi operación",
                    "menu:/demo" => "🧪 Caneca DEMO",
                    "menu:inicio" => "🏠 Menú",
                    _ => b.Texto
                }
            }).ToList();
            if (respuesta.Texto == MensajesTelegram.GuiaFoto && !botones.Any(b => b.Dato == "eco:privacidad"))
                botones.Add(new("🔒 Privacidad", "eco:privacidad"));
            if (botones.Any(b => b.Dato == ServicioConversacionTelegram.ConfirmarSi) && !botones.Any(b => b.Dato == "menu:inicio"))
                botones.Add(new("🏠 Menú", "menu:inicio"));
            return respuesta with { Texto = texto, Botones = botones };
        }
    }
}
