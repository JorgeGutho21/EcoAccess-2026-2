using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Etiqueta QR de una caneca: la muestra, la guarda como PNG y la imprime. El PNG llega ya
    /// generado por <see cref="EcoAccess.P3.Logica.Servicios.CodigoQrCaneca"/>; esta pantalla no arma el enlace ni
    /// decide si la caneca lleva QR. El enlace es público: no contiene contraseñas ni tokens.
    /// </summary>
    public partial class FormQrCaneca : Form
    {
        /// <summary>Lado del QR impreso en centésimas de pulgada (unos 6 cm): se lee bien a un brazo de distancia.</summary>
        private const int LadoImpreso = 236;

        /// <summary>Alto reservado bajo el QR para el nombre de la caneca y la instrucción.</summary>
        private const int AltoTexto = 80;

        private readonly string _nombreCaneca;
        private readonly byte[] _png;
        private readonly Image _imagen;
        private readonly Font _fuenteCaneca;

        public FormQrCaneca(string nombreCaneca, Uri enlace, byte[] png, bool vistaCiudadana = false)
        {
            Compat.ThrowIfNull(enlace, nameof(enlace));
            _nombreCaneca = nombreCaneca ?? throw new ArgumentNullException(nameof(nombreCaneca));
            _png = png ?? throw new ArgumentNullException(nameof(png));
            // Antes de InitializeComponent: si el PNG no se pudiera leer, no queda un formulario a medio crear.
            // Image.FromStream exige que el flujo siga abierto; la copia en Bitmap se independiza de él.
            using (var flujo = new MemoryStream(_png))
            using (Image leida = Image.FromStream(flujo))
            {
                _imagen = new Bitmap(leida);
            }

            InitializeComponent();
            _fuenteCaneca = new Font(Font.FontFamily, 12F, FontStyle.Bold);
            etiquetaCaneca.Font = _fuenteCaneca;
            imagenQr.Image = _imagen;
            etiquetaCaneca.Text = _nombreCaneca;
            textoEnlace.Text = enlace.AbsoluteUri;
            Text = $"QR — {_nombreCaneca}";
            AgregarAccionesEnlace(enlace);
            if (vistaCiudadana) textoEnlace.Visible = false;
            Disposed += (_, _) =>
            {
                _imagen.Dispose();
                _fuenteCaneca.Dispose();
            };
        }

        /// <summary>
        /// Copiar el enlace (para enviarlo al celular) y abrirlo en Telegram desde este equipo. Solo se abren enlaces
        /// https://t.me/, los mismos que codifica el QR.
        /// </summary>
        private void AgregarAccionesEnlace(Uri enlace)
        {
            var copiar = new Button { Text = "Copiar enlace", Location = new Point(12, 586), Size = new Size(225, 32), TabIndex = 4, UseVisualStyleBackColor = true };
            var abrir = new Button { Text = "Abrir en Telegram", Location = new Point(243, 586), Size = new Size(225, 32), TabIndex = 5, UseVisualStyleBackColor = true };
            bool esTelegram = enlace.Scheme == Uri.UriSchemeHttps && enlace.Host == "t.me";
            abrir.Enabled = esTelegram;
            copiar.Click += (_, _) =>
            {
                try { Clipboard.SetText(enlace.AbsoluteUri); MostrarEstado("Enlace copiado.", esError: false); }
                catch (System.Runtime.InteropServices.ExternalException) { MostrarEstado("Error: el portapapeles está ocupado. Intente de nuevo.", esError: true); }
            };
            abrir.Click += (_, _) =>
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(enlace.AbsoluteUri) { UseShellExecute = true })?.Dispose(); }
                catch (Exception error) when (error is Win32Exception or InvalidOperationException) { MostrarEstado("Error: no se pudo abrir el navegador.", esError: true); }
            };
            etiquetaEstado.Top = 624;
            ClientSize = new Size(ClientSize.Width, 658);
            Controls.Add(copiar);
            Controls.Add(abrir);
        }

        private void BotonGuardar_Click(object? sender, EventArgs e)
        {
            using var dialogo = new SaveFileDialog
            {
                Filter = "Imagen PNG (*.png)|*.png",
                FileName = $"qr-{NombreArchivo(_nombreCaneca)}.png",
                OverwritePrompt = true,
            };
            if (dialogo.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                File.WriteAllBytes(dialogo.FileName, _png);
                MostrarEstado("PNG guardado.", esError: false);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                MostrarEstado("Error: no se pudo guardar el archivo. Elija otra carpeta.", esError: true);
            }
        }

        private void BotonImprimir_Click(object? sender, EventArgs e)
        {
            using var documento = new PrintDocument { DocumentName = Text };
            documento.PrintPage += ImprimirPagina;
            using var dialogo = new PrintDialog { Document = documento, UseEXDialog = true };
            if (dialogo.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                documento.Print();
                MostrarEstado("Enviado a la impresora.", esError: false);
            }
            catch (InvalidPrinterException)
            {
                MostrarEstado("Error: la impresora elegida no está disponible.", esError: true);
            }
            catch (Exception error) when (error is Win32Exception or InvalidOperationException)
            {
                MostrarEstado("Error: no se pudo imprimir. Revise la impresora e inténtelo de nuevo.", esError: true);
            }
        }

        /// <summary>QR centrado arriba y nombre de la caneca debajo; sin suavizado para que los módulos queden nítidos.</summary>
        private void ImprimirPagina(object? sender, PrintPageEventArgs e)
        {
            Graphics lienzo = e.Graphics!;
            Rectangle margen = e.MarginBounds;
            int lado = Math.Max(1, Math.Min(LadoImpreso, Math.Min(margen.Width, margen.Height - AltoTexto)));
            var destino = new Rectangle(margen.Left + (margen.Width - lado) / 2, margen.Top, lado, lado);
            lienzo.InterpolationMode = InterpolationMode.NearestNeighbor;
            lienzo.PixelOffsetMode = PixelOffsetMode.Half;
            lienzo.DrawImage(_imagen, destino);
            using var fuente = new Font(Font.FontFamily, 14F, FontStyle.Bold);
            using var formato = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            var texto = new RectangleF(margen.Left, destino.Bottom + 10, margen.Width, AltoTexto - 10);
            lienzo.DrawString($"{_nombreCaneca}\nEscanea con Telegram", fuente, Brushes.Black, texto, formato);
            e.HasMorePages = false;
        }

        private void MostrarEstado(string mensaje, bool esError)
        {
            etiquetaEstado.ForeColor = esError ? Color.DarkRed : Color.DarkGreen;
            etiquetaEstado.Text = mensaje;
        }

        /// <summary>El nombre lo escribió una persona: se quitan los caracteres que Windows no admite en un archivo.</summary>
        internal static string NombreArchivo(string nombre)
        {
            char[] invalidos = Path.GetInvalidFileNameChars().Concat(new char[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' }).ToArray();
            string limpio = new string(nombre.Select(c => invalidos.Contains(c) || char.IsControl(c) ? '-' : c).ToArray()).Trim(' ', '.');
            return limpio.Length == 0 ? "caneca" : limpio;
        }
    }
}
