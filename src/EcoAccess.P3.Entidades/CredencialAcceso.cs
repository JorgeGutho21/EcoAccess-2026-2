using System;

namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Secreto con el que una cuenta inicia sesión en la GUI. Nunca contiene la
    /// contraseña: solo su hash PBKDF2, la sal y las iteraciones con que se calculó, más el contador de
    /// intentos fallidos y el bloqueo temporal. Es inmutable: cada cambio lo escribe el repositorio con un
    /// método puntual (por ejemplo, el incremento atómico de intentos), no reemplazando el objeto completo.
    /// </summary>
    public sealed class CredencialAcceso
    {
        public const int LongitudHash = 32;
        public const int LongitudSal = 16;
        public const int IteracionesMinimas = 100_000;
        public const int LongitudMinimaContrasena = 12;
        public const int LongitudMaximaContrasena = 128;

        private readonly byte[] _hash;
        private readonly byte[] _sal;

        public int IdUsuario { get; }
        public int Iteraciones { get; }
        public int IntentosFallidos { get; }
        public DateTime? BloqueadaHastaUtc { get; }
        public DateTime FechaCambioUtc { get; }

        /// <summary>Copia del hash: quien la reciba no puede alterar la credencial.</summary>
        public byte[] Hash => (byte[])_hash.Clone();

        /// <summary>Copia de la sal, por la misma razón que <see cref="Hash"/>.</summary>
        public byte[] Sal => (byte[])_sal.Clone();

        /// <summary>
        /// Crea o reconstruye una credencial. Lo usan el servicio (con intentos en 0 y sin bloqueo) y la capa
        /// de datos; en ambos casos se validan las longitudes, para que una fila corrupta no pase por válida.
        /// </summary>
        public CredencialAcceso(
            int idUsuario,
            byte[] hash,
            byte[] sal,
            int iteraciones,
            DateTime fechaCambioUtc,
            int intentosFallidos = 0,
            DateTime? bloqueadaHastaUtc = null)
        {
            IdUsuario = Validacion.IdPositivo(idUsuario, nameof(idUsuario));
            _hash = CopiarConLongitud(hash, LongitudHash, nameof(hash));
            _sal = CopiarConLongitud(sal, LongitudSal, nameof(sal));

            if (iteraciones < IteracionesMinimas)
            {
                throw new ArgumentOutOfRangeException(nameof(iteraciones), $"Se exigen al menos {IteracionesMinimas} iteraciones.");
            }

            if (intentosFallidos < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intentosFallidos), "Los intentos fallidos no pueden ser negativos.");
            }

            Iteraciones = iteraciones;
            IntentosFallidos = intentosFallidos;
            FechaCambioUtc = Validacion.FechaUtc(fechaCambioUtc, nameof(fechaCambioUtc));
            BloqueadaHastaUtc = bloqueadaHastaUtc is { } hasta ? Validacion.FechaUtc(hasta, nameof(bloqueadaHastaUtc)) : null;
        }

        /// <summary>Indica si el bloqueo temporal sigue vigente en ese instante.</summary>
        public bool EstaBloqueada(DateTime ahoraUtc) =>
            BloqueadaHastaUtc is { } hasta && Validacion.FechaUtc(ahoraUtc, nameof(ahoraUtc)) < hasta;

        private static byte[] CopiarConLongitud(byte[]? valor, int longitud, string nombreParametro)
        {
            Compat.ThrowIfNull(valor, nombreParametro);
            if (valor.Length != longitud)
            {
                throw new ArgumentException($"Debe tener exactamente {longitud} bytes.", nombreParametro);
            }

            return (byte[])valor.Clone();
        }
    }
}
