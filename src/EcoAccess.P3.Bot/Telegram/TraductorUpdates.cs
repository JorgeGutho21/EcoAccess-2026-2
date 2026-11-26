using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot.Telegram
{

    /// <summary>
    /// Convierte el JSON de un Update de la Bot API en <see cref="UpdateRecibido"/>. Lo usan el polling y el webhook.
    /// Solo se leen los campos que el bot necesita; lo demás se ignora. Un JSON sin update_id se rechaza porque sin
    /// él no hay control de duplicados.
    /// </summary>
    public static class TraductorUpdates
    {
        /// <summary>Lado mayor mínimo de la variante de foto que se descarga.</summary>
        public const int LadoSuficienteFoto = 640;

        private static bool TieneUpdateId(JsonElement update) =>
            update.ValueKind == JsonValueKind.Object
            && update.TryGetProperty("update_id", out JsonElement id)
            && id.ValueKind == JsonValueKind.Number
            && id.TryGetInt64(out long valor) && valor >= 0 && valor < long.MaxValue;

        /// <summary>Traduce un update o lanza <see cref="JsonException"/> si falta update_id.</summary>
        public static UpdateRecibido Traducir(JsonElement update)
        {
            if (update.ValueKind != JsonValueKind.Object
                || !update.TryGetProperty("update_id", out JsonElement id)
                || id.ValueKind != JsonValueKind.Number
                || !id.TryGetInt64(out long updateId) || updateId < 0 || updateId == long.MaxValue)
            {
                throw new JsonException("El update no trae update_id.");
            }

            if (update.TryGetProperty("message", out JsonElement mensaje))
            {
                return new UpdateRecibido(updateId, DesdeMensaje(updateId, mensaje), null);
            }

            if (update.TryGetProperty("callback_query", out JsonElement boton))
            {
                string? idBoton = Texto(boton, "id");
                return new UpdateRecibido(updateId, DesdeBoton(updateId, boton), idBoton);
            }

            return new UpdateRecibido(updateId, null, null);
        }

        /// <summary>
        /// Traduce la respuesta de getUpdates ({"ok":true,"result":[...]}). Un elemento sin update_id se omite en
        /// lugar de rechazar el lote: si no, ese elemento bloquearía el polling para siempre. Los demás siguen
        /// avanzando el offset.
        /// </summary>
        public static IReadOnlyList<UpdateRecibido> TraducirLista(JsonElement resultado)
        {
            if (resultado.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException("result no es una lista.");
            }

            return resultado.EnumerateArray().Where(TieneUpdateId).Select(Traducir).ToList();
        }

        private static MensajeEntrante? DesdeMensaje(long updateId, JsonElement mensaje)
        {
            if (!Largo(mensaje, "from", "id", out long usuario) || !Chat(mensaje, out long chat, out bool privado))
            {
                return null;
            }

            string? texto = Texto(mensaje, "text");
            string? foto = null;
            if (mensaje.TryGetProperty("photo", out var fotos) && fotos.ValueKind == JsonValueKind.Array)
            {
                // Se descarga la variante más pequeña cuyo lado mayor llega a 640 px (Telegram ofrece ~90/320/800/1280 px):
                // el reconocedor la reduce a 512 px de todos modos y la descarga es varias veces más corta que la de 1280 px.
                long areaMaxima = -1, areaSuficiente = long.MaxValue; string? suficiente = null;
                foreach (var variante in fotos.EnumerateArray())
                {
                    if (variante.ValueKind != JsonValueKind.Object) continue;
                    string? archivo = Texto(variante, "file_id");
                    if (archivo is null || archivo.Length > 512 || archivo.Any(char.IsControl)) continue;
                    if (variante.TryGetProperty("file_size", out var size) && (size.ValueKind != JsonValueKind.Number || !size.TryGetInt64(out long bytes) || bytes < 0 || bytes > ServicioClasificacionImagen.MaximoBytes)) continue;
                    long area = variante.TryGetProperty("width", out var w) && w.ValueKind == JsonValueKind.Number && w.TryGetInt32(out int ancho)
                        && variante.TryGetProperty("height", out var h) && h.ValueKind == JsonValueKind.Number && h.TryGetInt32(out int alto) && ancho > 0 && alto > 0
                        ? (long)ancho * alto : 0;
                    if (area >= areaMaxima) { areaMaxima = area; foto = archivo; }
                    int lado = variante.TryGetProperty("width", out var w2) && w2.TryGetInt32(out int a2) && variante.TryGetProperty("height", out var h2) && h2.TryGetInt32(out int b2) ? System.Math.Max(a2, b2) : 0;
                    if (lado >= LadoSuficienteFoto && area < areaSuficiente) { areaSuficiente = area; suficiente = archivo; }
                }
                if (suficiente is not null) foto = suficiente;
            }
            bool noCompatible = false;
            // Una imagen enviada «como archivo» es una foto más; cualquier otro adjunto recibe una explicación en vez de silencio.
            if (foto is null && mensaje.TryGetProperty("document", out var documento) && documento.ValueKind == JsonValueKind.Object)
            {
                string? archivo = Texto(documento, "file_id");
                bool imagen = Texto(documento, "mime_type") is "image/jpeg" or "image/png" or "image/webp";
                bool cabe = !documento.TryGetProperty("file_size", out var tamano) || (tamano.ValueKind == JsonValueKind.Number && tamano.TryGetInt64(out long bytes) && bytes is >= 0 and <= ServicioClasificacionImagen.MaximoBytes);
                if (imagen && cabe && archivo is { Length: <= 512 } && !archivo.Any(char.IsControl)) foto = archivo;
                else noCompatible = true;
            }
            else if (foto is null && (TieneAdjunto(mensaje) || mensaje.TryGetProperty("photo", out _))) noCompatible = true;
            return texto is null && foto is null && !noCompatible ? null
                : new MensajeEntrante(updateId, usuario, chat, privado, texto, null, foto, noCompatible && texto is null);
        }

        private static bool TieneAdjunto(JsonElement mensaje)
        {
            // Un sticker no es un intento de enviar un residuo: sigue sin respuesta.
            foreach (string tipo in new[] { "video", "video_note", "audio", "voice", "animation" })
                if (mensaje.TryGetProperty(tipo, out _)) return true;
            return false;
        }

        private static MensajeEntrante? DesdeBoton(long updateId, JsonElement boton)
        {
            // Un botón de un mensaje inline o muy viejo llega sin «message»: no se sabe el chat y se descarta.
            if (!Largo(boton, "from", "id", out long usuario)
                || !boton.TryGetProperty("message", out JsonElement mensaje)
                || !Chat(mensaje, out long chat, out bool privado))
            {
                return null;
            }

            string? dato = Texto(boton, "data");
            return dato is null ? null : new MensajeEntrante(updateId, usuario, chat, privado, null, dato);
        }

        private static bool Chat(JsonElement mensaje, out long chat, out bool privado)
        {
            privado = false;
            if (!Largo(mensaje, "chat", "id", out chat))
            {
                return false;
            }

            privado = mensaje.GetProperty("chat").TryGetProperty("type", out JsonElement tipo)
                && tipo.ValueKind == JsonValueKind.String
                && tipo.GetString() == "private";
            return true;
        }

        private static bool Largo(JsonElement padre, string objeto, string campo, out long valor)
        {
            valor = 0;
            return padre.ValueKind == JsonValueKind.Object
                && padre.TryGetProperty(objeto, out JsonElement hijo)
                && hijo.ValueKind == JsonValueKind.Object
                && hijo.TryGetProperty(campo, out JsonElement numero)
                && numero.ValueKind == JsonValueKind.Number
                && numero.TryGetInt64(out valor);
        }

        private static string? Texto(JsonElement padre, string campo) =>
            padre.ValueKind == JsonValueKind.Object
                && padre.TryGetProperty(campo, out JsonElement valor) && valor.ValueKind == JsonValueKind.String
                ? valor.GetString()
                : null;
    }
}
