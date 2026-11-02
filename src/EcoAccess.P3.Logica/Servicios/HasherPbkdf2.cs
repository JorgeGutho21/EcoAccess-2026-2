using System;
using System.Security.Cryptography;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// PBKDF2 con HMAC-SHA-256. Cada contraseña recibe una sal aleatoria de 16 bytes, de modo
    /// que dos cuentas con la misma contraseña tienen hashes distintos, y las iteraciones hacen costoso probar
    /// contraseñas por fuerza bruta si alguien copia la tabla. La comparación usa FixedTimeEquals para que el
    /// tiempo de respuesta no revele cuántos bytes coinciden.
    /// </summary>
    public sealed class HasherPbkdf2 : IHasherContrasenas
    {
        /// <summary>Valor recomendado por OWASP para PBKDF2-HMAC-SHA256.</summary>
        public const int IteracionesRecomendadas = 600_000;

        public int Iteraciones { get; }

        /// <summary>Las pruebas pueden pedir menos iteraciones, pero nunca por debajo del mínimo que acepta la tabla.</summary>
        public HasherPbkdf2(int iteraciones = IteracionesRecomendadas)
        {
            if (iteraciones < CredencialAcceso.IteracionesMinimas)
            {
                throw new ArgumentOutOfRangeException(nameof(iteraciones),
                    $"Se exigen al menos {CredencialAcceso.IteracionesMinimas} iteraciones.");
            }

            Iteraciones = iteraciones;
        }

        public HashContrasena Calcular(string contrasena)
        {
            Compat.ThrowIfNull(contrasena, nameof(contrasena));
            byte[] sal = Compat.RandomGetBytes(CredencialAcceso.LongitudSal);
            return new HashContrasena(Derivar(contrasena, sal, Iteraciones), sal, Iteraciones);
        }

        public bool Verificar(string contrasena, byte[] sal, int iteraciones, byte[] hashEsperado)
        {
            Compat.ThrowIfNull(contrasena, nameof(contrasena));
            Compat.ThrowIfNull(sal, nameof(sal));
            Compat.ThrowIfNull(hashEsperado, nameof(hashEsperado));
            return Compat.FixedTimeEquals(Derivar(contrasena, sal, iteraciones), hashEsperado);
        }

        private static byte[] Derivar(string contrasena, byte[] sal, int iteraciones) =>
            Compat.Pbkdf2(contrasena, sal, iteraciones, HashAlgorithmName.SHA256, CredencialAcceso.LongitudHash);
    }
}
