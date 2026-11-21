using System;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>
    /// Cada cuánto se vuelven a leer de Oracle los datos del ciudadano (ecopuntos, depósitos, actividad y disponibilidad de
    /// la estación) en Windows Forms. No hay notificaciones del servidor: se consulta periódicamente (15 s por defecto,
    /// configurable entre 10 y 120 s con la clave <c>EcoAccess.IntervaloActualizacionSegundos</c> de App.config), al volver
    /// a la ventana y justo después de un depósito. Si Oracle no responde, el siguiente intento se espacia (doble cada vez,
    /// hasta 2 minutos) para no insistir sin control. Después de <see cref="Vigencia"/> sin una lectura correcta, los datos
    /// en pantalla se marcan como desactualizados.
    /// </summary>
    public static class PoliticaSincronizacion
    {
        public const int SegundosPorDefecto = 15, SegundosMinimos = 10, SegundosMaximos = 120;

        public static TimeSpan Intervalo { get; private set; } = TimeSpan.FromSeconds(SegundosPorDefecto);
        public static TimeSpan Maximo { get; } = TimeSpan.FromMinutes(2);

        /// <summary>Datos leídos hace más de esto (dos ciclos y medio) se presentan como desactualizados.</summary>
        public static TimeSpan Vigencia => TimeSpan.FromSeconds(Intervalo.TotalSeconds * 2.5);

        /// <summary>Fija el intervalo desde la configuración. Un valor vacío, inválido o fuera de rango deja el anterior acotado.</summary>
        public static TimeSpan Configurar(string? segundos)
        {
            if (int.TryParse(segundos?.Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int s))
                Intervalo = TimeSpan.FromSeconds(Math.Max(SegundosMinimos, Math.Min(SegundosMaximos, s)));
            return Intervalo;
        }

        /// <summary>Espera antes del siguiente intento tras <paramref name="fallosSeguidos"/> fallos consecutivos.</summary>
        public static TimeSpan EsperaTrasFallos(int fallosSeguidos)
        {
            double segundos = Intervalo.TotalSeconds * Math.Pow(2, Math.Max(0, Math.Min(fallosSeguidos, 6)));
            return TimeSpan.FromSeconds(Math.Min(segundos, Maximo.TotalSeconds));
        }

        /// <summary>True si la última lectura correcta es demasiado vieja para presentarla como actual.</summary>
        public static bool EstaDesactualizado(DateTime ultimaLecturaUtc, DateTime ahoraUtc) =>
            ultimaLecturaUtc == DateTime.MinValue || ahoraUtc - ultimaLecturaUtc > Vigencia;
    }
}
