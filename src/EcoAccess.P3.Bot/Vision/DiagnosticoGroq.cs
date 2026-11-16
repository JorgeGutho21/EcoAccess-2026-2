using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Bot.Transportes;

namespace EcoAccess.P3.Bot.Vision
{

    public enum EstadoGroq
    {
        Operativo,
        NoConfigurado,
        CredencialRechazada,
        ModeloNoDisponible,
        NoDisponible
    }

    public sealed record SaludGroq(EstadoGroq Estado, string Modelo, string Mensaje);

    /// <summary>Solo lista modelos con la clave: no envía imágenes ni genera costo de inferencia. No imprime la clave.</summary>
    public static class DiagnosticoGroq
    {
        public static async Task<SaludGroq> ComprobarAsync(ConfiguracionGroq? configuracion, HttpClient? http = null, CancellationToken ct = default)
        {
            if (configuracion is null)
                return new(EstadoGroq.NoConfigurado, ConfiguracionVision.ModeloGroq, "Falta la clave de Groq. Ejecuta scripts\\instalacion\\Configurar-Credenciales.ps1.");
            bool propio = http is null;
            http ??= new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
            try
            {
                using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
                limite.CancelAfter(TimeSpan.FromSeconds(15));
                using var peticion = new HttpRequestMessage(HttpMethod.Get, ClasificadorGroq.Servidor + "models");
                peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuracion.ApiKey);
                using HttpResponseMessage respuesta = await http.SendAsync(peticion, HttpCompletionOption.ResponseHeadersRead, limite.Token);
                if (respuesta.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    return new(EstadoGroq.CredencialRechazada, configuracion.Modelo, $"Groq rechazó la clave (HTTP {(int)respuesta.StatusCode}).");
                if (!respuesta.IsSuccessStatusCode)
                    return new(EstadoGroq.NoDisponible, configuracion.Modelo, $"Groq respondió HTTP {(int)respuesta.StatusCode}.");
                using JsonDocument documento = JsonDocument.Parse(await LecturaAcotada.DesdeHttpAsync(respuesta.Content, 512 * 1024, limite.Token));
                bool existe = documento.RootElement.GetProperty("data").EnumerateArray()
                    .Any(m => m.GetProperty("id").GetString() == configuracion.Modelo);
                return existe
                    ? new(EstadoGroq.Operativo, configuracion.Modelo, "La clave es válida y el modelo está disponible. No se envió ninguna imagen.")
                    : new(EstadoGroq.ModeloNoDisponible, configuracion.Modelo, "La clave es válida, pero la cuenta no tiene ese modelo. No se sustituyó por otro.");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception error) when (error is HttpRequestException or OperationCanceledException or JsonException or InvalidDataException or InvalidOperationException or System.Collections.Generic.KeyNotFoundException)
            {
                return new(EstadoGroq.NoDisponible, configuracion.Modelo, $"No se pudo comprobar Groq ({error.GetType().Name}).");
            }
            finally { if (propio) http.Dispose(); }
        }
    }
}
