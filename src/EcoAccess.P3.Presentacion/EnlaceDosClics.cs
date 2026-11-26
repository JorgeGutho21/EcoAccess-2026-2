using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Enlace de DOS CLICS vinculado a esta estación. La dirección del túnel HTTPS (trycloudflare) cambia cada vez que se
    /// reinicia y el permiso de anfitrión vence a las 12 h, así que no se fija en el código: se pide a la pasarela local
    /// (<c>POST /enlace-anfitrion</c>), que devuelve el túnel vigente y renueva el permiso si hace falta. Antes de abrirlo se
    /// comprueba que el túnel responda desde internet; si no, el login abre DOS CLICS sin vincular y lo avisa.
    /// </summary>
    internal static class EnlaceDosClics
    {
        internal const string SitioWeb = "https://jorgegutho21.github.io/EcoAccess-2026-2/";
        internal const string PaginaSinVincular = SitioWeb + "dos-clics/";
        internal const string Pasarela = "http://127.0.0.1:8093/enlace-anfitrion";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(6) };

        /// <summary>Enlace vinculado y con un túnel que responde, o null si la pasarela o el túnel no están disponibles.</summary>
        internal static async Task<string?> ObtenerAsync()
        {
            try
            {
                using var solicitud = new HttpRequestMessage(HttpMethod.Post, Pasarela)
                {
                    Content = new StringContent("{\"destino\":\"dos-clics\"}", Encoding.UTF8, "application/json"),
                };
                solicitud.Headers.Add("X-EcoAccess-Operador", "1");
                using var respuesta = await Http.SendAsync(solicitud).ConfigureAwait(false);
                if (!respuesta.IsSuccessStatusCode) return null;
                using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false));
                string? enlace = documento.RootElement.TryGetProperty("enlace", out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() : null;
                string? tunel = TunelDe(enlace);
                return tunel is not null && await TunelRespondeAsync(tunel).ConfigureAwait(false) ? enlace : null;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException) { return null; }
        }

        /// <summary>
        /// Dirección del túnel (parámetro <c>e</c> del fragmento) si el enlace es de DOS CLICS en el sitio oficial y el túnel es
        /// https; null en cualquier otro caso (así nunca se abre un enlace ajeno).
        /// </summary>
        internal static string? TunelDe(string? enlace)
        {
            if (enlace is null || !enlace.StartsWith(PaginaSinVincular + "#", StringComparison.Ordinal)) return null;
            foreach (string parte in enlace.Substring(PaginaSinVincular.Length + 1).Split('&'))
            {
                if (!parte.StartsWith("e=", StringComparison.Ordinal)) continue;
                string tunel = Uri.UnescapeDataString(parte.Substring(2));
                return Uri.TryCreate(tunel, UriKind.Absolute, out var u) && u.Scheme == Uri.UriSchemeHttps ? tunel.TrimEnd('/') : null;
            }
            return null;
        }

        /// <summary>Cualquier respuesta HTTP (también 401 sin permiso) prueba que el túnel llega a la pasarela.</summary>
        private static async Task<bool> TunelRespondeAsync(string tunel)
        {
            try { using var r = await Http.GetAsync(tunel + "/api/v1/estado").ConfigureAwait(false); return true; }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { return false; }
        }
    }
}
