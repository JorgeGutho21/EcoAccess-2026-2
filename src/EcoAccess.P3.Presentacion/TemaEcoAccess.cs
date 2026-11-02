using System;
using System.Drawing;
using System.Windows.Forms;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Sistema de diseño Eco: paleta, tipografía y estilos compartidos. No modifica posiciones, eventos ni archivos
    /// Designer. Contrastes medidos (WCAG): texto #1E293B/fondo 13,7:1; #105C3B/blanco 8:1; #64748B/blanco 4,8:1.
    /// El verde de marca #168A55 (4,4:1 sobre blanco) se reserva para iconos, gráficos y cifras grandes.
    /// </summary>
    internal static class TemaEcoAccess
    {
        // Superficies
        internal static readonly Color Fondo = Color.FromArgb(0xF6, 0xF8, 0xF7);
        internal static readonly Color Superficie = Color.White;
        internal static readonly Color Borde = Color.FromArgb(0xE2, 0xE8, 0xE5);

        // Marca
        internal static readonly Color Verde = Color.FromArgb(0x16, 0x8A, 0x55);
        internal static readonly Color VerdeOscuro = Color.FromArgb(0x10, 0x5C, 0x3B);
        internal static readonly Color VerdeSuave = Color.FromArgb(0xE8, 0xF6, 0xEE);
        internal static readonly Color VerdeHoja = Color.FromArgb(0xB7, 0xE4, 0xCB);
        internal static readonly Color Hover = Color.FromArgb(0x0C, 0x4A, 0x2F);

        // Texto
        internal static readonly Color Texto = Color.FromArgb(0x1E, 0x29, 0x3B);
        internal static readonly Color Suave = Color.FromArgb(0x55, 0x62, 0x74);

        // Estados (con fondo suave para pastillas)
        internal static readonly Color Error = Color.FromArgb(0xB4, 0x23, 0x18);
        internal static readonly Color ErrorSuave = Color.FromArgb(0xFD, 0xEC, 0xEA);
        internal static readonly Color Advertencia = Color.FromArgb(0x92, 0x5A, 0x07);
        internal static readonly Color AdvertenciaSuave = Color.FromArgb(0xFE, 0xF4, 0xE2);
        internal static readonly Color Info = Color.FromArgb(0x1D, 0x4E, 0xD8);
        internal static readonly Color InfoSuave = Color.FromArgb(0xE8, 0xEF, 0xFD);

        /// <summary>Compatibilidad con pantallas anteriores: la navegación ahora es clara.</summary>
        internal static readonly Color Navegacion = Superficie;

        // Tipografía Segoe UI con escalones claros (no se usan tamaños menores de 9 pt).
        internal static readonly Font Titulo = new("Segoe UI Semibold", 20);
        internal static readonly Font TituloSeccion = new("Segoe UI Semibold", 14);
        internal static readonly Font Subtitulo = new("Segoe UI Semibold", 11);
        internal static readonly Font Valor = new("Segoe UI Semibold", 24);
        internal static readonly Font Entrada = new("Segoe UI", 11);
        internal static readonly Font Cuerpo = new("Segoe UI", 10);
        internal static readonly Font Pequena = new("Segoe UI", 9);

        internal const int Radio = 12;

        internal static int Px(Control control, int unidades) => (int)Math.Round(unidades * control.DeviceDpi / 96d);

        internal static void EstilizarAccion(Krypton.Toolkit.KryptonButton boton, bool principal = false)
        {
            Color fondo = principal ? VerdeOscuro : Superficie;
            Color texto = principal ? Color.White : VerdeOscuro;
            boton.StateCommon.Back.Color1 = boton.StateCommon.Back.Color2 = fondo;
            boton.StateCommon.Border.Color1 = boton.StateCommon.Border.Color2 = principal ? VerdeOscuro : Borde;
            boton.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            boton.StateCommon.Border.Rounding = 10;
            boton.StateCommon.Content.ShortText.Color1 = texto;
            boton.StateCommon.Content.ShortText.Font = Subtitulo;
            boton.StateCommon.Content.Padding = new Padding(14, 6, 14, 6);
            boton.OverrideDefault.Back.Color1 = boton.OverrideDefault.Back.Color2 = fondo;
            boton.OverrideDefault.Border.Color1 = principal ? VerdeOscuro : Borde;
            boton.OverrideDefault.Content.ShortText.Color1 = texto;
            boton.StateTracking.Back.Color1 = boton.StateTracking.Back.Color2 = principal ? Hover : VerdeSuave;
            boton.StatePressed.Back.Color1 = boton.StatePressed.Back.Color2 = principal ? Hover : VerdeHoja;
            boton.StateDisabled.Back.Color1 = boton.StateDisabled.Back.Color2 = Borde;
            boton.StateDisabled.Content.ShortText.Color1 = Suave;
            // Foco visible para teclado: anillo de 2 px en el verde de marca.
            boton.OverrideFocus.Border.Color1 = Verde;
            boton.OverrideFocus.Border.Width = 2;
        }

        /// <summary>Marca (en Tag) de los botones con estilo propio, como el ojo de la contraseña.</summary>
        internal const string SinEstilo = "sin-estilo";

        internal static void Aplicar(Control control)
        {
            if (control is Form formulario)
            {
                formulario.BackColor = Fondo;
                formulario.ForeColor = Texto;
            }
            if (control is Button boton && !Equals(boton.Tag, SinEstilo)) EstilizarBoton(boton,
                boton.Name.Contains("Registrar", StringComparison.OrdinalIgnoreCase)
                || boton.Name.Contains("Entrar", StringComparison.OrdinalIgnoreCase)
                || boton.Name.Contains("Guardar", StringComparison.OrdinalIgnoreCase));
            // Solo color: cambiar fuentes o tamaños recortaría los diseños fijos de los formularios del Designer.
            if (control is GroupBox grupo) grupo.ForeColor = Texto;
            if (control is DataGridView tabla) EstilizarTabla(tabla);
            foreach (Control hijo in control.Controls) Aplicar(hijo);
        }

        /// <summary>Tabla limpia: encabezado suave, filas altas, solo separadores horizontales y selección en verde suave.</summary>
        internal static void EstilizarTabla(DataGridView tabla)
        {
            tabla.BackgroundColor = Superficie;
            tabla.BorderStyle = BorderStyle.None;
            tabla.GridColor = Borde;
            tabla.EnableHeadersVisualStyles = false;
            tabla.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            tabla.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            tabla.ColumnHeadersHeight = Px(tabla, 40);
            tabla.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0xF1, 0xF5, 0xF3);
            tabla.ColumnHeadersDefaultCellStyle.ForeColor = Suave;
            tabla.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5f);
            tabla.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            tabla.ColumnHeadersDefaultCellStyle.SelectionBackColor = tabla.ColumnHeadersDefaultCellStyle.BackColor;
            tabla.ColumnHeadersDefaultCellStyle.SelectionForeColor = Suave;
            tabla.DefaultCellStyle.BackColor = Superficie;
            tabla.DefaultCellStyle.ForeColor = Texto;
            tabla.DefaultCellStyle.Font = Cuerpo;
            tabla.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            tabla.DefaultCellStyle.SelectionBackColor = VerdeSuave;
            tabla.DefaultCellStyle.SelectionForeColor = Texto;
            tabla.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(0xFA, 0xFC, 0xFB);
            tabla.RowTemplate.Height = Px(tabla, 38);
            tabla.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            tabla.RowHeadersVisible = false;
        }

        internal static void EstilizarBoton(Button boton, bool principal = false)
        {
            boton.FlatStyle = FlatStyle.Flat;
            boton.FlatAppearance.BorderSize = 1;
            boton.FlatAppearance.BorderColor = principal ? VerdeOscuro : Borde;
            boton.BackColor = principal ? VerdeOscuro : Superficie;
            boton.ForeColor = principal ? Color.White : VerdeOscuro;
            boton.Cursor = Cursors.Hand;
            boton.UseVisualStyleBackColor = false;
            boton.FlatAppearance.MouseOverBackColor = principal ? Hover : VerdeSuave;
            boton.FlatAppearance.MouseDownBackColor = principal ? Hover : VerdeHoja;
            if (Compat.IsNullOrWhiteSpace(boton.AccessibleName)) boton.AccessibleName = boton.Text.Replace("&", "");
        }
    }
}
