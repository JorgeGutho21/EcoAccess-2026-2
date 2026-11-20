using System;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>
    /// Cada cuánto se vuelven a leer de Oracle los datos del ciudadano (ecopuntos, depósitos, actividad) en Windows Forms.
    /// No hay notificaciones del servidor: se consulta periódicamente, al volver a la ventana y al terminar un depósito.
    /// Si Oracle no responde, el siguiente intento se espacia (30 s, 60 s, 120 s) para no insistir sin control.
    /// </summary>
    public static class PoliticaSincronizacion
    {
        public static TimeSpan Intervalo { get; } = TimeSpan.FromSeconds(30);
        public static TimeSpan Maximo { get; } = TimeSpan.FromMinutes(2);

        /// <summary>Espera antes del siguiente intento tras <paramref name="fallosSeguidos"/> fallos consecutivos.</summary>
        public static TimeSpan EsperaTrasFallos(int fallosSeguidos)
        {
            double segundos = Intervalo.TotalSeconds * Math.Pow(2, Math.Max(0, Math.Min(fallosSeguidos, 6)));
            return TimeSpan.FromSeconds(Math.Min(segundos, Maximo.TotalSeconds));
        }
    }
}
