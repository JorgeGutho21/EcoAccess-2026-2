using System;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Conversión entre UTC, como se guarda todo en Oracle, y la hora civil de Colombia, que es la que ve la persona.
    /// Colombia usa UTC−5 todo el año, sin horario de verano, así que basta un desfase fijo y no se depende de la
    /// zona horaria instalada en el equipo.
    /// </summary>
    public static class HoraColombia
    {
        /// <summary>Diferencia de Colombia respecto a UTC.</summary>
        public static readonly TimeSpan Desfase = TimeSpan.FromHours(-5);

        /// <summary>Instante UTC en que empieza el día civil indicado en Colombia (00:00 de Bogotá = 05:00 UTC).</summary>
        public static DateTime InicioDelDiaUtc(DateOnly dia) =>
            DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue) - Desfase, DateTimeKind.Utc);

        /// <summary>
        /// Período semiabierto [inicio, fin) en UTC que cubre del primer al último día de Colombia, ambos incluidos.
        /// El fin es el comienzo del día siguiente al último, así un registro de las 23:59 del último día cuenta.
        /// </summary>
        /// <exception cref="ArgumentException">Si el último día es anterior al primero.</exception>
        public static (DateTime DesdeUtc, DateTime HastaUtc) PeriodoUtc(DateOnly primerDia, DateOnly ultimoDia)
        {
            if (ultimoDia < primerDia)
            {
                throw new ArgumentException("El último día no puede ser anterior al primero.", nameof(ultimoDia));
            }

            return (InicioDelDiaUtc(primerDia), InicioDelDiaUtc(ultimoDia.AddDays(1)));
        }

        /// <summary>Hora de Colombia de un instante UTC, para mostrarla en pantalla.</summary>
        /// <exception cref="ArgumentException">Si la fecha no está marcada como UTC.</exception>
        public static DateTime DesdeUtc(DateTime instanteUtc)
        {
            if (instanteUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("La fecha debe estar en UTC.", nameof(instanteUtc));
            }

            return DateTime.SpecifyKind(instanteUtc + Desfase, DateTimeKind.Unspecified);
        }
    }
}
