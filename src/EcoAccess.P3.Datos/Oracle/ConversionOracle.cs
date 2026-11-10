using System;
using System.Globalization;

namespace EcoAccess.P3.Datos.Oracle
{

    /// <summary>
    /// Conversiones entre los tipos de Oracle y los de las entidades. Son funciones puras, sin conexión,
    /// para poder probarlas en cualquier equipo.
    /// </summary>
    internal static class ConversionOracle
    {
        /// <summary>Lee un NUMBER como entero. ODP.NET entrega NUMBER(10) como decimal o long según el caso.</summary>
        public static int Entero(object valor) => Convert.ToInt32(valor, CultureInfo.InvariantCulture);

        /// <summary>Lee un NUMBER(1) con CHECK (0, 1). Cualquier otro valor indica datos corruptos.</summary>
        public static bool Logico(object valor) => Entero(valor) switch
        {
            0 => false,
            1 => true,
            int otro => throw new InvalidOperationException($"Valor lógico inválido en la base de datos: {otro}."),
        };

        public static int DesdeLogico(bool valor) => valor ? 1 : 0;

        /// <summary>Texto opcional: DBNull se convierte en null.</summary>
        public static string? TextoOpcional(object valor) => valor is DBNull ? null : (string)valor;

        /// <summary>
        /// Oracle TIMESTAMP no guarda la zona horaria y ODP.NET devuelve <see cref="DateTimeKind.Unspecified"/>.
        /// El esquema guarda siempre UTC, así que se marca como tal sin mover la hora.
        /// </summary>
        public static DateTime FechaUtc(object valor) => DateTime.SpecifyKind((DateTime)valor, DateTimeKind.Utc);

        public static DateTime? FechaUtcOpcional(object valor) => valor is DBNull ? null : FechaUtc(valor);

        /// <summary>Antes de guardar: una fecha que no esté en UTC es un error de programación, no se convierte.</summary>
        public static DateTime ParaColumnaUtc(DateTime fecha) => fecha.Kind == DateTimeKind.Utc
            ? fecha
            : throw new ArgumentException("La fecha debe estar en UTC.", nameof(fecha));

        public static object ParaColumnaUtcOpcional(DateTime? fecha) =>
            fecha is null ? DBNull.Value : ParaColumnaUtc(fecha.Value);

        public static object ParaColumnaOpcional(string? texto) => texto is null ? DBNull.Value : texto;
    }
}
