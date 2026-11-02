using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace EcoAccess.P3.Bot.Vision
{

    /// <summary>Motor de reconocimiento que usa el bot. Comparacion solo aplica a la herramienta de evaluación.</summary>
    public enum ModoVision
    {
        Local = 0,
        Groq = 1,
        Automatico = 2,
        Comparacion = 3
    }

    /// <summary>Parámetros de Groq. Los precios vienen de la página oficial y se pueden actualizar sin compilar.</summary>
    public sealed record ConfiguracionGroq(string ApiKey, string Modelo, TimeSpan TiempoMaximo, int MaximoSolicitudesDia, decimal PresupuestoDiaUsd,
        decimal PrecioEntradaMillon, decimal PrecioSalidaMillon)
    {
        // Sin la clave: un record la imprimiría completa en cualquier registro.
        public override string ToString() => $"Groq {Modelo}";
    }

    /// <summary>Lee Vision:Modo y Vision:Groq:*. Un valor no reconocido cae en Local: ante la duda, ninguna foto sale del equipo.</summary>
    public static class ConfiguracionVision
    {
        public const string ModeloGroq = "qwen/qwen3.8-27b";

        public static ModoVision LeerModo(IConfiguration configuracion, Action<string> registrar)
        {
            string? valor = configuracion["Vision:Modo"];
            if (Compat.IsNullOrWhiteSpace(valor)) return ModoVision.Local;
            if (Enum.TryParse(valor.Trim(), true, out ModoVision modo) && Enum.IsDefined(typeof(ModoVision), modo) && !int.TryParse(valor, out _)) return modo;
            registrar("Vision:Modo no es Local, Groq, Automatico ni Comparacion. Se usa Local.");
            return ModoVision.Local;
        }

        /// <returns>null si falta la clave o está mal formada; el motivo se registra sin mostrar su contenido.</returns>
        public static ConfiguracionGroq? LeerGroq(IConfiguration configuracion, Action<string> registrar)
        {
            string? clave = configuracion["Vision:Groq:ApiKey"];
            if (Compat.IsNullOrWhiteSpace(clave)) clave = Environment.GetEnvironmentVariable("GROQ_API_KEY");
            if (Compat.IsNullOrWhiteSpace(clave)) return null;
            clave = clave.Trim();
            foreach (char c in clave)
            {
                if (char.IsWhiteSpace(c) || char.IsControl(c) || c > 126)
                {
                    registrar("La clave de Groq no tiene un formato válido. Vuelve a configurarla.");
                    return null;
                }
            }

            string? modelo = configuracion["Vision:Groq:Modelo"];
            return new ConfiguracionGroq(clave, Compat.IsNullOrWhiteSpace(modelo) ? ModeloGroq : modelo.Trim(),
                TimeSpan.FromSeconds(Entero(configuracion["Vision:Groq:TiempoMaximoSegundos"], 20, 5, 60)),
                Entero(configuracion["Vision:Groq:MaximoSolicitudesDia"], 200, 1, 1000),
                Numero(configuracion["Vision:Groq:PresupuestoDiaUsd"], 1.00m),
                Numero(configuracion["Vision:Groq:PrecioEntradaMillon"], 0.80m),
                Numero(configuracion["Vision:Groq:PrecioSalidaMillon"], 4.00m));
        }

        private static int Entero(string? valor, int omision, int minimo, int maximo) =>
            int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n >= minimo && n <= maximo ? n : omision;

        private static decimal Numero(string? valor, decimal omision) =>
            decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal n) && n >= 0m ? n : omision;
    }
}
