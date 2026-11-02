using System;
using System.Security.Cryptography;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Genera el token de registro con 32 bytes de <see cref="RandomNumberGenerator"/> (256 bits) en base64url sin
    /// relleno: 43 caracteres que caben en un enlace sin escapar. El token viaja una sola vez al chat privado; en la
    /// base solo se guarda su hash SHA-256, así que una copia de la tabla no permite completar registros ajenos.
    /// </summary>
    public sealed class GeneradorTokensAleatorio : IGeneradorTokens
    {
        private const int BytesAleatorios = 32;

        public string Generar()
        {
            var bytes = Compat.RandomGetBytes(BytesAleatorios);

            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
