using System;
using EcoAccess.P3.Entidades;
using QRCoder;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// QR imprimible de una caneca. Contiene solo el enlace público
    /// <see cref="EnlaceTelegram.ParaCaneca"/>: el identificador QR no es secreto ni autoriza nada por sí mismo, porque
    /// el bot exige la cuenta vinculada antes de aceptar cualquier operación. El PNG sale de <see cref="PngByteQRCode"/>,
    /// que no depende de System.Drawing y funciona igual en Linux y en Windows.
    /// </summary>
    public static class CodigoQrCaneca
    {
        /// <summary>Q recupera hasta un 25 % de módulos dañados: una etiqueta rayada o sucia sigue leyéndose.</summary>
        internal const QRCodeGenerator.ECCLevel NivelCorreccion = QRCodeGenerator.ECCLevel.Q;

        /// <summary>Con 10 px por módulo, el QR de un enlace con bot de 12 caracteres (versión 5: 37 módulos y 8 de zona quieta) mide 450 px.</summary>
        public const int PixelesPorModuloPredeterminado = 10;

        /// <summary>Genera el PNG del QR; rechaza la caneca dada de baja y un nombre de bot o identificador no válidos.</summary>
        public static Resultado<byte[]> Png(string nombreBot, Contenedor caneca, int pixelesPorModulo = PixelesPorModuloPredeterminado)
        {
            Compat.ThrowIfNull(caneca, nameof(caneca));
            ValidarPixeles(pixelesPorModulo);
            Resultado<Uri> enlace = Enlace(nombreBot, caneca);
            return enlace.Exitoso
                ? Resultado<byte[]>.Exito(Png(enlace.Valor, pixelesPorModulo))
                : Resultado<byte[]>.DesdeRechazo(enlace);
        }

        /// <summary>PNG de un enlace ya validado por <see cref="Enlace"/>; la GUI lo usa para no armar el enlace dos veces.</summary>
        public static byte[] Png(Uri enlace, int pixelesPorModulo = PixelesPorModuloPredeterminado)
        {
            Compat.ThrowIfNull(enlace, nameof(enlace));
            ValidarPixeles(pixelesPorModulo);
            using QRCodeData datos = Datos(enlace);
            return new PngByteQRCode(datos).GetGraphic(pixelesPorModulo);
        }

        /// <summary>Enlace que va dentro del QR; la GUI lo muestra como texto junto a la imagen.</summary>
        public static Resultado<Uri> Enlace(string nombreBot, Contenedor caneca)
        {
            Compat.ThrowIfNull(caneca, nameof(caneca));
            if (caneca.Estado == EstadoContenedor.DadoDeBaja)
            {
                return Resultado<Uri>.Rechazo(MotivoRechazo.EstadoNoPermitido, "Una caneca dada de baja no lleva QR.");
            }

            try
            {
                return Resultado<Uri>.Exito(EnlaceTelegram.ParaCaneca(nombreBot, caneca.IdentificadorQr));
            }
            catch (ArgumentException error)
            {
                return Resultado<Uri>.Rechazo(MotivoRechazo.DatosInvalidos, error.Message);
            }
        }

        /// <summary>Matriz de módulos con la zona quieta de 4 módulos por lado; las pruebas la decodifican.</summary>
        internal static bool[,] Modulos(Uri enlace)
        {
            using QRCodeData datos = Datos(enlace);
            int lado = datos.ModuleMatrix.Count;
            var modulos = new bool[lado, lado];
            for (int fila = 0; fila < lado; fila++)
            {
                for (int columna = 0; columna < lado; columna++)
                {
                    modulos[fila, columna] = datos.ModuleMatrix[fila][columna];
                }
            }

            return modulos;
        }

        private static void ValidarPixeles(int pixelesPorModulo)
        {
            Compat.ThrowIfLessThan(pixelesPorModulo, 1, nameof(pixelesPorModulo));
            Compat.ThrowIfGreaterThan(pixelesPorModulo, 40, nameof(pixelesPorModulo));
        }

        private static QRCodeData Datos(Uri enlace)
        {
            using var generador = new QRCodeGenerator();
            return generador.CreateQrCode(enlace.AbsoluteUri, NivelCorreccion);
        }
    }
}
