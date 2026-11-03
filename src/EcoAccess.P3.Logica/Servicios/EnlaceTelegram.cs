using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Enlaces profundos del bot. El QR de la caneca contiene https://t.me/{bot}?start={identificador}:
    /// al escanearlo, Telegram abre el chat y envía «/start {identificador}». El identificador ya es base64url de
    /// 22 caracteres, dentro de lo que admite el parámetro start (1 a 64 caracteres de A-Z a-z 0-9 _ -).
    /// </summary>
    public static partial class EnlaceTelegram
    {
        public const string NombreBotPiloto = "EcoAccessP3_JG_bot";
        private const string PrefijoStart = "/start";

        /// <summary>Enlace que se imprime en el QR de la caneca.</summary>
        public static Uri ParaCaneca(string nombreBot, string identificadorQr)
        {
            if (Compat.IsNullOrEmpty(nombreBot) || !NombreBotValido().IsMatch(nombreBot))
            {
                throw new ArgumentException("El nombre del bot debe tener 5 a 32 caracteres A-Z a-z 0-9 _ y terminar en «bot».", nameof(nombreBot));
            }

            if (Compat.IsNullOrEmpty(identificadorQr) || !ParametroStartValido().IsMatch(identificadorQr))
            {
                throw new ArgumentException("El identificador no cabe en el parámetro start de Telegram.", nameof(identificadorQr));
            }

            return new Uri($"https://t.me/{nombreBot}?start={identificadorQr}");
        }

        /// <summary>Parámetro de /start que confirma una vinculación: el enlace vuelve al bot sin copiar el código a mano.</summary>
        public const string PrefijoConfirmar = "vincular-";

        /// <summary>Abre el chat del bot con el código ya puesto. Solo surte efecto en la cuenta de Telegram que pidió el enlace.</summary>
        public static Uri ParaConfirmar(string codigo) => new Uri($"https://t.me/{NombreBotPiloto}?start={PrefijoConfirmar}{Uri.EscapeDataString(codigo)}");

        /// <summary>Enlace de la página de registro con el token en la consulta.</summary>
        public static Uri ParaRegistro(Uri paginaRegistro, string token)
        {
            Compat.ThrowIfNull(paginaRegistro, nameof(paginaRegistro));
            if (paginaRegistro.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException("La página de registro debe servirse por HTTPS.", nameof(paginaRegistro));
            }

            var constructor = new UriBuilder(paginaRegistro) { Query = "t=" + Uri.EscapeDataString(token) };
            return constructor.Uri;
        }

        /// <summary>
        /// Separa «/start» y su parámetro. Devuelve false si el texto no es /start; el parámetro es null si vino solo.
        /// Telegram puede agregar «@nombre_bot» al comando.
        /// </summary>
        public static bool EsStart(string? texto, out string? parametro)
        {
            parametro = null;
            if (texto is null)
            {
                return false;
            }

            string[] partes = texto.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
            if (partes.Length == 0 || Comando(partes[0]) != PrefijoStart)
            {
                return false;
            }

            parametro = partes.Length == 2 && ParametroStartValido().IsMatch(partes[1]) ? partes[1] : null;
            return true;
        }

        /// <summary>Comando en minúsculas sin «@nombre_bot», o null si el texto no empieza con «/».</summary>
        public static string? Comando(string? texto)
        {
            if (Compat.IsNullOrWhiteSpace(texto) || texto.TrimStart()[0] != '/')
            {
                return null;
            }

            string primera = texto.Trim().Split(' ', 2)[0];
            int arroba = primera.IndexOf('@');
            return (arroba >= 0 ? primera[..arroba] : primera).ToLowerInvariant();
        }

        private static readonly Regex PatronNombreBotValido = new Regex("^[A-Za-z0-9_]{2,29}[Bb][Oo][Tt]\\z");
        private static Regex NombreBotValido() => PatronNombreBotValido;

        private static readonly Regex PatronParametroStartValido = new Regex("^[A-Za-z0-9_-]{1,64}\\z");
        private static Regex ParametroStartValido() => PatronParametroStartValido;
    }
}
