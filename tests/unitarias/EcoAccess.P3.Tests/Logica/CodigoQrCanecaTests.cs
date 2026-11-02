using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Buffers.Binary;
using System.IO.Compression;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using ZXing.Common;
using ZXing.QrCode.Internal;

using System.Linq;

namespace EcoAccess.P3.Tests.Logica
{

    /// <summary>
    /// El QR se comprueba decodificándolo con ZXing.Net, una implementación independiente de QRCoder: la prueba no
    /// confía en que la librería que lo genera también lo lea bien.
    /// </summary>
    [TestClass]
    public sealed class CodigoQrCanecaTests
    {
        private const string Bot = "EcoAccessBot";
        private const string Identificador = "Ab_9-xyzAb_9-xyzAb_9-x";
        private const int ZonaQuieta = 4;

        private static Contenedor Caneca(EstadoContenedor estado = EstadoContenedor.EnServicio) =>
            new(7, 1, "Caneca patio", Identificador, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), estado, new int[] { 1 });

        [TestMethod]
        public void LaMatrizSeDecodificaComoElEnlaceDeLaCaneca()
        {
            Uri enlace = CodigoQrCaneca.Enlace(Bot, Caneca()).Valor;

            Assert.AreEqual($"https://t.me/{Bot}?start={Identificador}", Decodificar(CodigoQrCaneca.Modulos(enlace)));
        }

        [TestMethod]
        public void ElPngTieneFirmaTamanoYLosMismosModulosQueLaMatriz()
        {
            const int pixeles = 6;
            Resultado<byte[]> png = CodigoQrCaneca.Png(Bot, Caneca(), pixeles);
            Assert.IsTrue(png.Exitoso);
            bool[,] modulos = CodigoQrCaneca.Modulos(CodigoQrCaneca.Enlace(Bot, Caneca()).Valor);
            int lado = modulos.GetLength(0);

            bool[,] oscuros = LeerPng(png.Valor, out int ancho, out int alto);

            Assert.AreEqual(lado * pixeles, ancho);
            Assert.AreEqual(ancho, alto);
            for (int fila = 0; fila < lado; fila++)
            {
                for (int columna = 0; columna < lado; columna++)
                {
                    // Se mira el centro de cada módulo.
                    Assert.AreEqual(modulos[fila, columna], oscuros[fila * pixeles + pixeles / 2, columna * pixeles + pixeles / 2]);
                }
            }

            Assert.AreEqual($"https://t.me/{Bot}?start={Identificador}", Decodificar(Muestrear(oscuros, lado, pixeles)));
        }

        [TestMethod]
        public void LaMatrizIncluyeLaZonaQuietaDeCuatroModulos()
        {
            bool[,] modulos = CodigoQrCaneca.Modulos(CodigoQrCaneca.Enlace(Bot, Caneca()).Valor);
            int lado = modulos.GetLength(0);
            for (int i = 0; i < lado; i++)
            {
                for (int borde = 0; borde < ZonaQuieta; borde++)
                {
                    Assert.IsFalse(modulos[borde, i] || modulos[i, borde] || modulos[lado - 1 - borde, i] || modulos[i, lado - 1 - borde]);
                }
            }
        }

        [TestMethod]
        public void ElQrDelBotDeEjemploEsVersionCincoCon450Pixeles()
        {
            Assert.AreEqual(37 + 2 * ZonaQuieta, CodigoQrCaneca.Modulos(CodigoQrCaneca.Enlace(Bot, Caneca()).Valor).GetLength(0));
            LeerPng(CodigoQrCaneca.Png(Bot, Caneca()).Valor, out int ancho, out _);
            Assert.AreEqual(450, ancho);
        }

        [TestMethod]
        public void UnaCanecaFueraDeServicioSigueTeniendoQr() =>
            Assert.IsTrue(CodigoQrCaneca.Png(Bot, Caneca(EstadoContenedor.FueraDeServicio)).Exitoso,
                "El bot es quien avisa que la caneca no está en servicio; la etiqueta no se reimprime.");

        [TestMethod]
        public void UnaCanecaDadaDeBajaNoLlevaQr()
        {
            Resultado<byte[]> png = CodigoQrCaneca.Png(Bot, Caneca(EstadoContenedor.DadoDeBaja));
            Assert.IsFalse(png.Exitoso);
            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, png.Motivo);
            Resultado<Uri> enlace = CodigoQrCaneca.Enlace(Bot, Caneca(EstadoContenedor.DadoDeBaja));
            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, enlace.Motivo, "La GUI arma el enlace primero: también debe rechazarse.");
        }

        [TestMethod]
        public void ElPngDesdeElEnlaceCoincideConElDeLaCaneca()
        {
            Uri enlace = CodigoQrCaneca.Enlace(Bot, Caneca()).Valor;
            CollectionAssert.AreEqual(CodigoQrCaneca.Png(Bot, Caneca()).Valor, CodigoQrCaneca.Png(enlace));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("EcoAccess")]
        [DataRow("Eco Access Bot")]
        public void UnNombreDeBotInvalidoSeRechazaSinExcepcion(string bot)
        {
            Resultado<byte[]> png = CodigoQrCaneca.Png(bot, Caneca());
            Assert.IsFalse(png.Exitoso);
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, png.Motivo);
        }

        [TestMethod]
        public void PixelesPorModuloFueraDeRangoSonUnErrorDeProgramacion()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CodigoQrCaneca.Png(Bot, Caneca(), 0));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CodigoQrCaneca.Png(Bot, Caneca(), 41));
        }

        private static string Decodificar(bool[,] modulos)
        {
            int lado = modulos.GetLength(0) - 2 * ZonaQuieta;
            var matriz = new BitMatrix(lado, lado);
            for (int fila = 0; fila < lado; fila++)
            {
                for (int columna = 0; columna < lado; columna++)
                {
                    matriz[columna, fila] = modulos[fila + ZonaQuieta, columna + ZonaQuieta];
                }
            }

            DecoderResult? resultado = new Decoder().decode(matriz, null);
            Assert.IsNotNull(resultado, "ZXing no pudo decodificar el QR.");
            return resultado.Text;
        }

        private static bool[,] Muestrear(bool[,] oscuros, int lado, int pixeles)
        {
            var modulos = new bool[lado, lado];
            for (int fila = 0; fila < lado; fila++)
            {
                for (int columna = 0; columna < lado; columna++)
                {
                    modulos[fila, columna] = oscuros[fila * pixeles + pixeles / 2, columna * pixeles + pixeles / 2];
                }
            }

            return modulos;
        }

        /// <summary>
        /// Lector PNG mínimo para lo que emite PngByteQRCode: paleta o escala de grises de 1 bit, sin entrelazado y con
        /// filtro 0 en cada fila. Cualquier otra cosa hace fallar la prueba en vez de leerse mal.
        /// </summary>
        private static bool[,] LeerPng(byte[] png, out int ancho, out int alto)
        {
            CollectionAssert.AreEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, png.Take(8).ToArray());
            ancho = alto = 0;
            byte[]? paleta = null;
            int tipoColor = -1;
            using var idat = new MemoryStream();
            for (int pos = 8; pos < png.Length;)
            {
                int largo = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(pos));
                string tipo = System.Text.Encoding.ASCII.GetString(png, pos + 4, 4);
                ReadOnlySpan<byte> datos = png.AsSpan(pos + 8, largo);
                switch (tipo)
                {
                    case "IHDR":
                        ancho = BinaryPrimitives.ReadInt32BigEndian(datos);
                        alto = BinaryPrimitives.ReadInt32BigEndian(datos[4..]);
                        Assert.AreEqual(1, datos[8], "Profundidad de bits");
                        tipoColor = datos[9];
                        Assert.AreEqual(0, datos[12], "Sin entrelazado");
                        break;
                    case "PLTE":
                        paleta = datos.ToArray();
                        break;
                    case "IDAT":
                        idat.Write(datos.ToArray());
                        break;
                }

                pos += 12 + largo;
            }

            idat.Position = 2;
            using var zlib = new DeflateStream(idat, CompressionMode.Decompress);
            using var crudo = new MemoryStream();
            zlib.CopyTo(crudo);
            byte[] filas = crudo.ToArray();
            int bytesFila = 1 + (ancho + 7) / 8;
            Assert.HasCount(bytesFila * alto, filas);
            var oscuros = new bool[alto, ancho];
            for (int y = 0; y < alto; y++)
            {
                Assert.AreEqual(0, filas[y * bytesFila], "Filtro de fila");
                for (int x = 0; x < ancho; x++)
                {
                    int indice = (filas[y * bytesFila + 1 + x / 8] >> (7 - x % 8)) & 1;
                    oscuros[y, x] = tipoColor switch
                    {
                        0 => indice == 0,
                        3 => paleta![indice * 3] < 128,
                        _ => throw new AssertFailedException($"Tipo de color PNG no esperado: {tipoColor}"),
                    };
                }
            }

            return oscuros;
        }
    }
}
