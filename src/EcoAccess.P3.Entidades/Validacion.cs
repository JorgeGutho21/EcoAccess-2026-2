using System;

namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Comprobaciones que comparten varias entidades. Es interna porque es un detalle de
    /// implementación de esta capa: las demás capas solo ven el efecto, una excepción clara.
    /// Todas lanzan ArgumentException o una de sus derivadas (ArgumentOutOfRangeException),
    /// porque señalan un dato de entrada inválido.
    /// </summary>
    internal static class Validacion
    {
        public static string TextoRequerido(string? valor, string nombreParametro, int longitudMaxima, int longitudMinima = 1)
        {
            if (Compat.IsNullOrWhiteSpace(valor))
            {
                throw new ArgumentException("El valor es obligatorio.", nombreParametro);
            }

            string limpio = valor.Trim();
            if (limpio.Length < longitudMinima || limpio.Length > longitudMaxima)
            {
                throw new ArgumentException(
                    $"El valor debe tener entre {longitudMinima} y {longitudMaxima} caracteres.", nombreParametro);
            }

            return limpio;
        }

        /// <summary>Un texto vacío o solo con espacios se guarda como null, para no tener dos formas de «no hay dato».</summary>
        public static string? TextoOpcional(string? valor, string nombreParametro, int longitudMaxima)
        {
            return Compat.IsNullOrWhiteSpace(valor) ? null : TextoRequerido(valor, nombreParametro, longitudMaxima);
        }

        /// <summary>
        /// Exige UTC para que todas las fechas de eventos sean comparables entre equipos.
        /// Una fecha Local o Unspecified suele indicar que alguien usó DateTime.Now o leyó de Oracle sin SpecifyKind.
        /// </summary>
        public static DateTime FechaUtc(DateTime fecha, string nombreParametro)
        {
            if (fecha.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("La fecha debe expresarse en UTC (DateTimeKind.Utc).", nombreParametro);
            }

            return fecha;
        }

        /// <summary>Los identificadores de registros guardados son positivos; 0 se reserva para «todavía no guardado».</summary>
        public static int IdPositivo(int id, string nombreParametro)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nombreParametro, "El identificador de un registro guardado debe ser positivo.");
            }

            return id;
        }

        /// <summary>
        /// Un enum en C# acepta cualquier número con una conversión, por ejemplo (CodigoRol)99.
        /// Esta comprobación impide que un valor fuera de la lista aprobada entre a una entidad.
        /// </summary>
        public static TEnum EnumDefinido<TEnum>(TEnum valor, string nombreParametro) where TEnum : struct, Enum
        {
            if (!Compat.IsDefined(valor))
            {
                throw new ArgumentOutOfRangeException(nombreParametro, $"El valor no corresponde a un {typeof(TEnum).Name} aprobado.");
            }

            return valor;
        }
    }
}
