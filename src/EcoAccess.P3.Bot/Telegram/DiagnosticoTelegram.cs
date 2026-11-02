using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace EcoAccess.P3.Bot.Telegram
{

    public static class DiagnosticoTelegram
    {
        public static async Task<int> EjecutarAsync(IConfiguration configuracion, Action<string> registrar, CancellationToken ct = default)
        {
            string? token = configuracion["Telegram:TokenBot"], nombre = configuracion["Telegram:NombreBot"];
            if (!ClienteTelegramHttp.TokenValido(token) || Compat.IsNullOrWhiteSpace(nombre))
            { registrar("Falta configurar Telegram:TokenBot válido y Telegram:NombreBot en User Secrets ecoaccess-p3. No los compartas por chat."); return 1; }
            try
            {
                using var cliente = new ClienteTelegramHttp(token!);
                var estado = await cliente.ComprobarConfiguracionAsync(nombre, ct);
                registrar($"Identidad de Telegram comprobada: @{estado.NombreBot}; actualizaciones pendientes: {estado.ActualizacionesPendientes}.");
                registrar(estado.WebhookActivo ? "Webhook activo: no iniciar polling; revisar su configuración. No se modificó ni se descartaron updates." :
                    "Sin webhook activo. Diagnóstico sin consumo de updates; no acredita una conversación desde el teléfono.");
                return estado.WebhookActivo ? 3 : 0;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (ErrorTelegramException e) { registrar(e.Message); return e.EsTokenRechazado ? 4 : 1; }
            catch (Exception) { registrar("No se pudo comprobar la configuración de Telegram. No se consumieron actualizaciones."); return 1; }
        }
    }
}
