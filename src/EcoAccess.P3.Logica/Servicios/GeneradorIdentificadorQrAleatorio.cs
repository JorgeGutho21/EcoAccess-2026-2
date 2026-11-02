using System;
using System.Security.Cryptography;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Genera el identificador del QR con 16 bytes de <see cref="RandomNumberGenerator"/> (128 bits,
    /// generador criptográfico del sistema) codificados en base64url sin relleno: 22 caracteres del
    /// alfabeto A-Z a-z 0-9 - _ que admite Telegram. No se usa <see cref="Random"/> porque es predecible
    /// y el QR es público: alguien podría adivinar el de otra caneca.
    /// </summary>
    public sealed class GeneradorIdentificadorQrAleatorio : IGeneradorIdentificadorQr
    {
        private const int BytesAleatorios = 16;

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
