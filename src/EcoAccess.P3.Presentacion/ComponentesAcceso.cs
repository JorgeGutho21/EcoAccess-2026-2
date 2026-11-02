using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Krypton.Toolkit;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>Ruta pública reutilizable: explica una opción y expone únicamente su acción visual.</summary>
    internal sealed class TarjetaRutaAcceso : TableLayoutPanel
    {
        internal KryptonButton Accion { get; } = new();

        internal TarjetaRutaAcceso(string titulo, string descripcion, string accion, IconChar icono)
        {
            AutoSize = true;
            ColumnCount = 2;
            RowCount = 3;
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 3; i++) RowStyles.Add(new RowStyle(SizeType.AutoSize));
            BackColor = Color.White;
            Padding = new Padding(24);
            Margin = new Padding(0, 0, 0, 16);
            var simbolo = new IconoEcoAccess { IconChar = icono, IconColor = TemaEcoAccess.Verde, IconSize = 28, Size = new Size(36, 36), BackColor = Color.White, TabStop = false };
            var cabecera = new Label { Text = titulo, AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Texto, Margin = new Padding(0, 0, 0, 8) };
            var detalle = new Label { Text = descripcion, AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 0, 0, 12) };
            Accion.Values.Text = accion;
            Accion.AccessibleName = accion;
            Accion.AutoSize = false;
            Accion.Dock = DockStyle.Top;
            Accion.Height = 42;
            Accion.MinimumSize = new Size(180, 42);
            Accion.Margin = Padding.Empty;
            Accion.TabIndex = 0;
            TemaEcoAccess.EstilizarAccion(Accion);
            Controls.Add(simbolo, 0, 0);
            SetRowSpan(simbolo, 3);
            Controls.Add(cabecera, 1, 0);
            Controls.Add(detalle, 1, 1);
            Controls.Add(Accion, 1, 2);
            SizeChanged += (_, _) =>
            {
                int ancho = Math.Max(200, ClientSize.Width - Padding.Horizontal - 48);
                cabecera.MaximumSize = detalle.MaximumSize = new Size(ancho, 0);
            };
        }
    }

    /// <summary>
    /// Identidad de la bienvenida: logotipo oficial, propósito y Eco (la mascota) sobre blanco, para que el logotipo
    /// conserve sus colores. Es decorativa: no
    /// muestra métricas ni concede acceso.
    /// </summary>
    internal sealed class PanelIdentidadEcoAccess : TableLayoutPanel
    {
        private readonly Font _proposito = new("Segoe UI", 14);
        private readonly Font _lema = new("Segoe UI Semibold", 12);

        internal PanelIdentidadEcoAccess()
        {
            BackColor = TemaEcoAccess.Superficie;
            ForeColor = TemaEcoAccess.Texto;
            ColumnCount = 1;
            RowCount = 6;
            Padding = new Padding(48, 40, 40, 40);
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            for (int i = 0; i < 4; i++) RowStyles.Add(new RowStyle(SizeType.AutoSize));
            RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            var eco = new ImagenRedondeada
            {
                Imagen = RecursosEco.Eco,
                Circular = false,
                RadioEsquina = 24,
                Margin = new Padding(0, 0, 0, 16),
                AccessibleName = "Eco, la mascota de EcoAccess, con el pulgar arriba",
                AccessibleRole = AccessibleRole.Graphic,
            };
            var logo = new LogoEcoAccess { Margin = new Padding(0, 0, 0, 20) };
            var proposito = new Label { Text = "Conectamos tecnología y sostenibilidad para transformar la manera de gestionar nuestros residuos.", Font = _proposito, AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 0, 0, 20) };
            var lema = new Label { Text = "Eco: ¡Cada residuo cuenta!", Font = _lema, AutoSize = true, ForeColor = TemaEcoAccess.VerdeOscuro, Margin = Padding.Empty };
            Controls.Add(logo, 0, 1);
            Controls.Add(proposito, 0, 2);
            Controls.Add(eco, 0, 3);
            Controls.Add(lema, 0, 4);
            SizeChanged += (_, _) =>
            {
                int ancho = Math.Max(180, ClientSize.Width - Padding.Horizontal);
                // Eco ocupa un lugar destacado sin empujar el texto fuera de la vista en pantallas bajas.
                int anchoLogo = Math.Min(ancho, TemaEcoAccess.Px(this, 460));
                logo.Size = new Size(anchoLogo, LogoEcoAccess.AltoPara(anchoLogo));
                int lado = Math.Max(110, Math.Min(ancho, Math.Min(TemaEcoAccess.Px(this, 260), ClientSize.Height - TemaEcoAccess.Px(this, 380))));
                eco.Size = new Size(lado, lado);
                proposito.MaximumSize = lema.MaximumSize = new Size(ancho, 0);
            };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) { _proposito.Dispose(); _lema.Dispose(); }
        }
    }
}
