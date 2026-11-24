using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Lleva la estética Eco (skill eco-estilo-base) a los controles estándar de los formularios del Designer, sin cambiar
    /// sus eventos ni su lógica: cajas de texto redondeadas con foco verde, listas desplegables planas, botones redondeados
    /// (principal, secundario y de peligro), grupos como tarjetas blancas y pestañas con subrayado verde. Lo usa
    /// <see cref="TemaEcoAccess.Aplicar"/>; cada control se estiliza una sola vez aunque se llame varias veces.
    /// </summary>
    internal static class EstiloControlesEco
    {
        /// <summary>Suave: relleno verde suave sin borde marcado (acciones frecuentes de una tarjeta lateral, como en el diseño de Usuarios).</summary>
        internal enum TipoBoton { Principal, Secundario, Peligro, Suave }

        private static readonly ConditionalWeakTable<Control, object> Estilizados = new();
        private static readonly Dictionary<float, Font> Semibold = new();

        /// <summary>Devuelve false si el control ya tenía el estilo (llamadas repetidas a Aplicar).</summary>
        private static bool Marcar(Control control)
        {
            if (Estilizados.TryGetValue(control, out _)) return false;
            Estilizados.Add(control, new object());
            return true;
        }

        /// <summary>Marca un control que ya armó <see cref="ModuloEco"/>, para que <see cref="TemaEcoAccess.Aplicar"/> no lo vuelva a tocar.</summary>
        internal static void MarcarEstilizado(Control control) => Marcar(control);

        private sealed class IconoGuardado { internal IconChar Icono; }
        private static readonly ConditionalWeakTable<Button, IconoGuardado> IconosBoton = new();

        /// <summary>Icono FontAwesome del botón: se dibuja a la escala real de la pantalla y del color del texto en cada estado.</summary>
        internal static void IconoBoton(Button boton, IconChar icono)
        {
            IconosBoton.Remove(boton);
            IconosBoton.Add(boton, new IconoGuardado { Icono = icono });
            boton.Invalidate();
        }

        private static IconChar IconoDe(Button boton) => IconosBoton.TryGetValue(boton, out IconoGuardado? g) ? g.Icono : IconChar.None;

        /// <summary>Verdadero si el módulo ya eligió un icono para el botón (los ayudantes genéricos no lo reemplazan).</summary>
        internal static bool TieneIcono(Button boton) => IconoDe(boton) != IconChar.None;

        internal static Font FuenteSemibold(Font base_)
        {
            float tamano = base_.SizeInPoints;
            if (!Semibold.TryGetValue(tamano, out Font? fuente)) Semibold[tamano] = fuente = new Font("Segoe UI Semibold", tamano);
            return fuente;
        }

        /// <summary>Color real detrás del control: sube por los contenedores mientras el fondo sea transparente.</summary>
        internal static Color FondoDe(Control? control)
        {
            for (Control? c = control; c is not null; c = c.Parent)
                if (c.BackColor.A == 255) return c.BackColor;
            return TemaEcoAccess.Fondo;
        }

        private static GraphicsPath Redondeado(RectangleF r, float radio) => RecursosEco.Redondeado(r, Math.Max(1f, radio));

        // ShowFocusCues y ShowKeyboardCues son protegidas: se leen por reflexión para respetar la preferencia de Windows.
        private static readonly PropertyInfo? PropiedadFoco = typeof(Control).GetProperty("ShowFocusCues", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo? PropiedadTeclado = typeof(Control).GetProperty("ShowKeyboardCues", BindingFlags.Instance | BindingFlags.NonPublic);
        private static bool PistasFoco(Control c) => PropiedadFoco is null || (bool)PropiedadFoco.GetValue(c, null)!;
        private static bool PistasTeclado(Control c) => PropiedadTeclado is null || (bool)PropiedadTeclado.GetValue(c, null)!;

        // ───────────────────────────── Botones ─────────────────────────────

        private static readonly string[] Principales = { "Acreditar", "Registrar", "Entrar", "Guardar", "Aceptar", "Consultar", "Buscar", "Verificar", "Admitir", "Asignar", "Establecer", "Crear", "Enviar", "Aplicar" };
        private static readonly string[] Peligrosas = { "Anular", "DarDeBaja", "Dar de baja", "Deshabilitar", "Desactivar", "Quitar", "FueraDeServicio", "Fuera de servicio", "Eliminar", "Rechazar", "Borrar" };

        /// <summary>Tipo según el nombre o el texto del botón: así los formularios del Designer no cambian.</summary>
        internal static TipoBoton Clasificar(Button boton)
        {
            string clave = boton.Name + " " + boton.Text.Replace("&", "");
            foreach (string p in Peligrosas) if (clave.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0) return TipoBoton.Peligro;
            foreach (string p in Principales) if (clave.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0) return TipoBoton.Principal;
            return TipoBoton.Secundario;
        }

        internal static void Boton(Button boton, TipoBoton tipo)
        {
            if (!Marcar(boton)) return;
            bool encima = false, pulsado = false;
            boton.FlatStyle = FlatStyle.Flat;
            boton.FlatAppearance.BorderSize = 0;
            boton.UseVisualStyleBackColor = false;
            boton.Cursor = Cursors.Hand;
            boton.MouseEnter += (_, _) => { encima = true; boton.Invalidate(); };
            boton.MouseLeave += (_, _) => { encima = pulsado = false; boton.Invalidate(); };
            boton.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { pulsado = true; boton.Invalidate(); } };
            boton.MouseUp += (_, _) => { pulsado = false; boton.Invalidate(); };
            boton.GotFocus += (_, _) => boton.Invalidate();
            boton.LostFocus += (_, _) => boton.Invalidate();
            boton.EnabledChanged += (_, _) => boton.Invalidate();
            boton.Paint += (_, e) => PintarBoton(boton, e.Graphics, tipo, encima, pulsado);
            // Un botón que se ajusta a su texto reserva el ancho del texto en semibold, también si después hereda otra fuente.
            void Reservar()
            {
                // Con fuente semibold (botones de ModuloEco) el propio AutoSize ya mide bien; el icono va en el Padding.
                if (!boton.AutoSize || boton.Text.Length == 0 || boton.Font.Name.Contains("Semibold")) return;
                int icono = boton.Image?.Width ?? (IconoDe(boton) == IconChar.None ? 0 : TemaEcoAccess.Px(boton, 24));
                int ancho = TextRenderer.MeasureText(boton.Text, FuenteSemibold(boton.Font)).Width + TemaEcoAccess.Px(boton, 24) + boton.Padding.Horizontal + icono;
                if (ancho > boton.MinimumSize.Width) boton.MinimumSize = new Size(ancho, boton.MinimumSize.Height);
            }
            boton.FontChanged += (_, _) => Reservar();
            boton.TextChanged += (_, _) => Reservar();
            Reservar();
        }

        private static void PintarBoton(Button b, Graphics g, TipoBoton tipo, bool encima, bool pulsado)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(FondoDe(b.Parent));
            Color fondo, borde, texto;
            switch (tipo)
            {
                case TipoBoton.Principal:
                    fondo = !b.Enabled ? TemaEcoAccess.Borde : pulsado || encima ? TemaEcoAccess.Hover : TemaEcoAccess.VerdeOscuro;
                    borde = fondo;
                    texto = b.Enabled ? Color.White : TemaEcoAccess.Suave;
                    break;
                case TipoBoton.Suave:
                    fondo = !b.Enabled ? TemaEcoAccess.Fondo : pulsado ? TemaEcoAccess.VerdeHoja : encima ? TemaEcoAccess.VerdeHoja : TemaEcoAccess.VerdeSuave;
                    borde = b.Enabled && encima ? TemaEcoAccess.Verde : fondo;
                    texto = b.Enabled ? TemaEcoAccess.Texto : TemaEcoAccess.Suave;
                    break;
                case TipoBoton.Peligro:
                    fondo = !b.Enabled ? TemaEcoAccess.Fondo : pulsado || encima ? TemaEcoAccess.ErrorSuave : TemaEcoAccess.Superficie;
                    borde = b.Enabled && encima ? TemaEcoAccess.Error : TemaEcoAccess.Borde;
                    texto = b.Enabled ? TemaEcoAccess.Error : TemaEcoAccess.Suave;
                    break;
                default:
                    fondo = !b.Enabled ? TemaEcoAccess.Fondo : pulsado ? TemaEcoAccess.VerdeHoja : encima ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.Superficie;
                    borde = b.Enabled && encima ? TemaEcoAccess.Verde : TemaEcoAccess.Borde;
                    texto = b.Enabled ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Suave;
                    break;
            }
            var area = new RectangleF(0.5f, 0.5f, b.Width - 1.5f, b.Height - 1.5f);
            float radio = Math.Min(TemaEcoAccess.Px(b, 8), area.Height / 2f);
            using (var ruta = Redondeado(area, radio))
            {
                using (var pincel = new SolidBrush(fondo)) g.FillPath(pincel, ruta);
                using (var lapiz = new Pen(borde)) g.DrawPath(lapiz, ruta);
            }
            if (b.Focused && PistasFoco(b))
            {
                using var ruta = Redondeado(RectangleF.Inflate(area, -1.5f, -1.5f), radio - 1);
                using var foco = new Pen(tipo == TipoBoton.Principal ? TemaEcoAccess.VerdeHoja : TemaEcoAccess.Verde, 2);
                g.DrawPath(foco, ruta);
            }
            var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
            if (!PistasTeclado(b)) flags |= TextFormatFlags.HidePrefix;
            int margen = TemaEcoAccess.Px(b, 6);
            var zona = new Rectangle(margen, 0, Math.Max(1, b.Width - 2 * margen), b.Height);
            // Semibold en una línea como en el resto de la app; si en el ancho fijo del Designer no cabe, la regular en una línea, y
            // solo si tampoco cabe, en varias líneas (nunca cortado a la mitad).
            Image? imagen = b.Image ?? (IconoDe(b) is var ic && ic != IconChar.None ? CacheIconos.Obtener(ic, TemaEcoAccess.Px(b, 16), texto) : null);
            int libre = zona.Width - (imagen is null ? 0 : imagen.Width + TemaEcoAccess.Px(b, 8));
            bool Cabe(Font f) => TextRenderer.MeasureText(g, b.Text, f, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width <= libre;
            Font fuente = FuenteSemibold(b.Font);
            if (!Cabe(fuente)) fuente = b.Font;
            flags |= Cabe(fuente) ? TextFormatFlags.SingleLine : TextFormatFlags.WordBreak;
            if (imagen is not null)
            {
                int separacion = b.Text.Length > 0 ? TemaEcoAccess.Px(b, 8) : 0;
                Size medida = TextRenderer.MeasureText(g, b.Text, fuente, new Size(zona.Width - imagen.Width - separacion, b.Height), flags);
                int x = Math.Max(margen, (b.Width - imagen.Width - separacion - medida.Width) / 2);
                g.DrawImage(imagen, x, (b.Height - imagen.Height) / 2);
                zona = new Rectangle(x + imagen.Width + separacion, 0, Math.Max(1, b.Width - x - imagen.Width - separacion - margen), b.Height);
                flags = (flags & ~TextFormatFlags.HorizontalCenter) | TextFormatFlags.Left;
            }
            TextRenderer.DrawText(g, b.Text, fuente, zona, texto, flags);
        }

        // ───────────────────────────── Cajas de texto ─────────────────────────────

        /// <summary>
        /// Envuelve un TextBox del Designer en un marco redondeado que ocupa su mismo lugar (posición, anclaje, celda u orden).
        /// Los formularios siguen usando el mismo TextBox: solo cambia quién dibuja el borde.
        /// </summary>
        internal static void Caja(TextBox caja)
        {
            if (caja.BorderStyle == BorderStyle.None || caja.Parent is null or MarcoEntrada or UpDownBase || !Marcar(caja)) return;
            Envolver(caja, caja.Multiline);
        }

        /// <summary>Selector numérico: mismo marco que las cajas de texto; las flechas quedan dentro, sin borde 3D.</summary>
        internal static void Numero(UpDownBase numero)
        {
            if (numero.BorderStyle == BorderStyle.None || numero.Parent is null or MarcoEntrada || !Marcar(numero)) return;
            Envolver(numero, false);
        }

        private static void Envolver(Control caja, bool multilinea)
        {
            Control padre = caja.Parent!;
            int indice = padre.Controls.GetChildIndex(caja);
            TableLayoutPanelCellPosition? celda = padre is TableLayoutPanel t ? t.GetCellPosition(caja) : null;
            int columnas = padre is TableLayoutPanel t1 ? t1.GetColumnSpan(caja) : 1, filas = padre is TableLayoutPanel t2 ? t2.GetRowSpan(caja) : 1;

            int altoOriginal = caja.Height;
            // Alto proporcional al de la propia caja (no a los DPI: antes de mostrarse, el formulario aún no sabe en qué monitor está).
            int alto = multilinea ? altoOriginal : Math.Max(altoOriginal, MarcoEntrada.AltoPara(caja));
            // En una fila de alto fijo de una tabla, el marco no puede crecer más que la celda.
            if (padre is TableLayoutPanel tabla && celda is { } c && c.Row >= 0 && c.Row < tabla.RowStyles.Count && tabla.RowStyles[c.Row].SizeType == SizeType.Absolute)
                alto = altoOriginal;
            var marco = new MarcoEntrada(caja)
            {
                AltoEnPixeles = true,
                Name = caja.Name + "Marco",
                Anchor = caja.Anchor,
                Dock = caja.Dock,
                Margin = caja.Margin,
                TabIndex = caja.TabIndex,
                // Centrado sobre la posición original: queda alineado con su etiqueta; el ajuste del diseño fijo baja las filas siguientes.
                Bounds = new Rectangle(caja.Left, Math.Max(0, caja.Top - (alto - altoOriginal) / 2), caja.Width, alto),
                MinimumSize = Size.Empty,
            };
            padre.SuspendLayout();
            marco.Encerrar();
            if (padre is TableLayoutPanel destino && celda is { } posicion)
            {
                destino.Controls.Add(marco, posicion.Column, posicion.Row);
                destino.SetColumnSpan(marco, columnas);
                destino.SetRowSpan(marco, filas);
            }
            else
            {
                padre.Controls.Add(marco);
                padre.Controls.SetChildIndex(marco, indice);
            }
            padre.ResumeLayout(true);
        }

        // ───────────────────────────── Listas desplegables ─────────────────────────────

        internal static void Lista(ComboBox lista)
        {
            if (!Marcar(lista)) return;
            lista.FlatStyle = FlatStyle.Flat;
            lista.BackColor = TemaEcoAccess.Superficie;
            lista.ForeColor = TemaEcoAccess.Texto;
            if (lista.DropDownStyle != ComboBoxStyle.DropDownList) return;
            // La parte visible mide lo mismo que el texto de una caja (así el marco queda del alto de las cajas de texto); las
            // opciones de la lista desplegada son más altas y con selección verde suave.
            lista.DrawMode = DrawMode.OwnerDrawVariable;
            void Alto() => lista.ItemHeight = lista.Font.Height + 1;
            lista.FontChanged += (_, _) => Alto();
            lista.MeasureItem += (_, e) => e.ItemHeight = lista.Font.Height * 3 / 2 + 1;
            Alto();
            lista.DrawItem += (_, e) =>
            {
                if (e.Index < 0) return;
                bool elegida = (e.State & DrawItemState.Selected) != 0;
                using (var fondo = new SolidBrush(elegida ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.Superficie)) e.Graphics.FillRectangle(fondo, e.Bounds);
                var texto = Rectangle.FromLTRB(e.Bounds.Left + TemaEcoAccess.Px(lista, 8), e.Bounds.Top, e.Bounds.Right - 2, e.Bounds.Bottom);
                TextRenderer.DrawText(e.Graphics, lista.GetItemText(lista.Items[e.Index]), lista.Font, texto, elegida ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Texto,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            };
            _ = new PintorLista(lista);
        }

        // ───────────────────────────── Grupos, pestañas, tablas y casillas ─────────────────────────────

        /// <summary>El GroupBox se dibuja como tarjeta blanca redondeada con el título en verde oscuro.</summary>
        internal static void Tarjeta(GroupBox grupo)
        {
            if (!Marcar(grupo)) return;
            grupo.BackColor = TemaEcoAccess.Superficie;
            grupo.ForeColor = TemaEcoAccess.Texto;
            grupo.Paint += (_, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(FondoDe(grupo.Parent));
                var area = new RectangleF(0.5f, 0.5f, grupo.Width - 1.5f, grupo.Height - 1.5f);
                using (var ruta = Redondeado(area, TemaEcoAccess.Px(grupo, 10)))
                {
                    using (var relleno = new SolidBrush(TemaEcoAccess.Superficie)) g.FillPath(relleno, ruta);
                    using (var borde = new Pen(TemaEcoAccess.Borde)) g.DrawPath(borde, ruta);
                }
                TextRenderer.DrawText(g, grupo.Text, FuenteSemibold(grupo.Font), new Point(TemaEcoAccess.Px(grupo, 10), TemaEcoAccess.Px(grupo, 2)),
                    grupo.Enabled ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Suave, TextFormatFlags.NoPrefix);
            };
        }

        internal static void Pestanas(TabControl pestanas)
        {
            if (!Marcar(pestanas)) return;
            pestanas.Padding = new Point(TemaEcoAccess.Px(pestanas, 12), TemaEcoAccess.Px(pestanas, 6));
            foreach (TabPage pagina in pestanas.TabPages)
            {
                pagina.UseVisualStyleBackColor = false;
                pagina.BackColor = TemaEcoAccess.Superficie;
            }
            pestanas.ControlAdded += (_, e) => { if (e.Control is TabPage p) { p.UseVisualStyleBackColor = false; p.BackColor = TemaEcoAccess.Superficie; } };
            _ = new PintorPestanas(pestanas);
        }

        /// <summary>Tabla dentro de un borde fino; el resto del estilo lo pone <see cref="TemaEcoAccess.EstilizarTabla"/>.</summary>
        internal static void MarcoTabla(DataGridView tabla)
        {
            if (!Marcar(tabla)) return;
            tabla.Paint += (_, e) =>
            {
                using var borde = new Pen(TemaEcoAccess.Borde);
                e.Graphics.DrawRectangle(borde, 0, 0, tabla.Width - 1, tabla.Height - 1);
            };
        }

        internal static void Casilla(ButtonBase casilla)
        {
            if (!Marcar(casilla)) return;
            casilla.FlatStyle = FlatStyle.Flat;
            casilla.FlatAppearance.BorderColor = TemaEcoAccess.Suave;
            casilla.FlatAppearance.CheckedBackColor = TemaEcoAccess.VerdeSuave;
            casilla.FlatAppearance.MouseOverBackColor = TemaEcoAccess.VerdeSuave;
            casilla.ForeColor = TemaEcoAccess.Texto;
            casilla.Cursor = Cursors.Hand;
        }

        internal static void ListaSimple(ListBox lista)
        {
            if (!Marcar(lista)) return;
            lista.BackColor = TemaEcoAccess.Superficie;
            lista.ForeColor = TemaEcoAccess.Texto;
            if (lista.Parent is not { } padre) { lista.BorderStyle = BorderStyle.FixedSingle; return; }
            // Sin el borde negro de Windows: un borde fino y redondeado del tono Borde, dibujado por el contenedor alrededor.
            lista.BorderStyle = BorderStyle.None;
            int aire = TemaEcoAccess.Px(lista, 2);
            padre.Paint += (_, e) =>
            {
                if (!lista.Visible || lista.Parent != padre) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = Rectangle.Inflate(lista.Bounds, aire, aire);
                using var ruta = Redondeado(new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height - 1), TemaEcoAccess.Px(lista, 6));
                using (var relleno = new SolidBrush(TemaEcoAccess.Superficie)) e.Graphics.FillPath(relleno, ruta);
                using var borde = new Pen(lista.Focused ? TemaEcoAccess.Verde : TemaEcoAccess.Borde);
                e.Graphics.DrawPath(borde, ruta);
            };
            void Repintar() => padre.Invalidate(Rectangle.Inflate(lista.Bounds, aire + 2, aire + 2));
            lista.GotFocus += (_, _) => Repintar();
            lista.LostFocus += (_, _) => Repintar();
            lista.LocationChanged += (_, _) => padre.Invalidate();
            lista.SizeChanged += (_, _) => padre.Invalidate();
            lista.VisibleChanged += (_, _) => padre.Invalidate();
        }

        /// <summary>
        /// Selector de fecha: Windows lo dibuja (el campo enfocado se sigue resaltando al editar con el teclado) y encima se
        /// ponen el borde redondeado y el botón del calendario con el icono Eco.
        /// </summary>
        internal static void Fecha(DateTimePicker fecha)
        {
            if (!Marcar(fecha)) return;
            fecha.CalendarTitleBackColor = TemaEcoAccess.VerdeOscuro;
            fecha.CalendarTitleForeColor = Color.White;
            fecha.CalendarTrailingForeColor = TemaEcoAccess.Suave;
            _ = new PintorFecha(fecha);
        }

        // ───────────────────────────── Pintores nativos ─────────────────────────────

        private const int WmPaint = 0x000F, WmEraseBkgnd = 0x0014, WmPrintClient = 0x0318;

        [StructLayout(LayoutKind.Sequential)]
        private struct PaintStruct
        {
            public IntPtr Hdc; public bool Erase; public int Left, Top, Right, Bottom; public bool Restore, IncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] Reserved;
        }

        [DllImport("user32.dll")] private static extern IntPtr BeginPaint(IntPtr ventana, ref PaintStruct ps);
        [DllImport("user32.dll")] private static extern bool EndPaint(IntPtr ventana, ref PaintStruct ps);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindowEx(IntPtr padre, IntPtr despues, string clase, string? titulo);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr ventana, out Rect rect);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr ventana);
        [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }

        /// <summary>
        /// Dibuja por completo un control nativo (lista desplegable o pestañas) en WM_PAINT y en WM_PRINTCLIENT (capturas),
        /// con doble búfer. El resto de mensajes (teclado, ratón, accesibilidad) siguen en el control original.
        /// </summary>
        private abstract class PintorNativo : NativeWindow
        {
            protected readonly Control Control;

            protected PintorNativo(Control control)
            {
                Control = control;
                control.HandleCreated += (_, _) => AssignHandle(control.Handle);
                control.HandleDestroyed += (_, _) => ReleaseHandle();
                if (control.IsHandleCreated) AssignHandle(control.Handle);
            }

            protected override void WndProc(ref Message m)
            {
                switch (m.Msg)
                {
                    case WmEraseBkgnd:
                        m.Result = (IntPtr)1;
                        return;
                    case WmPaint:
                        var ps = new PaintStruct();
                        IntPtr hdc = BeginPaint(m.HWnd, ref ps);
                        try { using Graphics g = Graphics.FromHdc(hdc); PintarConBufer(g); }
                        finally { EndPaint(m.HWnd, ref ps); }
                        m.Result = IntPtr.Zero;
                        return;
                    case WmPrintClient:
                        using (Graphics g = Graphics.FromHdc(m.WParam)) PintarConBufer(g);
                        m.Result = IntPtr.Zero;
                        return;
                }
                base.WndProc(ref m);
            }

            private void PintarConBufer(Graphics destino)
            {
                var area = new Rectangle(Point.Empty, Control.ClientSize);
                if (area.Width <= 0 || area.Height <= 0) return;
                Recortar(destino);
                using BufferedGraphics bufer = BufferedGraphicsManager.Current.Allocate(destino, area);
                Graphics g = bufer.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Pintar(g);
                bufer.Render(destino);
            }

            protected virtual void Recortar(Graphics g) { }
            protected abstract void Pintar(Graphics g);
        }

        /// <summary>Retoque sobre el dibujo nativo del selector de fecha (no lo sustituye, para no perder la edición por campos).</summary>
        private sealed class PintorFecha : NativeWindow
        {
            private readonly DateTimePicker _fecha;

            public PintorFecha(DateTimePicker fecha)
            {
                _fecha = fecha;
                fecha.HandleCreated += (_, _) => AssignHandle(fecha.Handle);
                fecha.HandleDestroyed += (_, _) => ReleaseHandle();
                if (fecha.IsHandleCreated) AssignHandle(fecha.Handle);
                foreach (string evento in new[] { "GotFocus", "LostFocus", "ValueChanged", "EnabledChanged", "CloseUp", "DropDown" })
                    typeof(DateTimePicker).GetEvent(evento)!.AddEventHandler(fecha, new EventHandler((_, _) => fecha.Invalidate()));
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                if (m.Msg == WmPaint)
                    using (Graphics g = Graphics.FromHwnd(m.HWnd)) Retocar(g);
                else if (m.Msg == WmPrintClient)
                    using (Graphics g = Graphics.FromHdc(m.WParam)) Retocar(g);
            }

            private void Retocar(Graphics g)
            {
                int ancho = _fecha.ClientSize.Width, alto = _fecha.ClientSize.Height;
                if (ancho <= 0 || alto <= 0) return;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                bool enMarco = _fecha.Parent is MarcoEntrada;
                Color relleno = enMarco ? ((MarcoEntrada)_fecha.Parent!).Relleno : _fecha.Enabled ? TemaEcoAccess.Superficie : TemaEcoAccess.Fondo;
                bool activa = _fecha.Focused;
                // Tapa el borde nativo con el relleno y las esquinas con el color del contenedor.
                using (var banda = new Pen(relleno, 3)) g.DrawRectangle(banda, 1, 1, ancho - 3, alto - 3);
                var area = new RectangleF(0.5f, 0.5f, ancho - 1.5f, alto - 1.5f);
                using var ruta = Redondeado(area, Math.Min(TemaEcoAccess.Px(_fecha, 8), alto / 2f));
                if (!enMarco)
                    using (var fuera = new Region(new Rectangle(0, 0, ancho, alto)))
                    using (var fondo = new SolidBrush(FondoDe(_fecha.Parent)))
                    {
                        fuera.Exclude(ruta);
                        g.FillRegion(fondo, fuera);
                    }
                // Botón del calendario: mismo relleno e icono de calendario en gris (verde con el foco).
                int lado = TemaEcoAccess.Px(_fecha, 34); // cubre el botón nativo y su icono
                var boton = new Rectangle(ancho - lado - 3, 2, lado + 1, alto - 4);
                using (var pincel = new SolidBrush(relleno)) g.FillRectangle(pincel, boton);
                // Dentro de un marco con icono de calendario a la izquierda, a la derecha va la flecha (igual que las listas).
                Bitmap icono = CacheIconos.Obtener(enMarco ? IconChar.ChevronDown : IconChar.CalendarDays, TemaEcoAccess.Px(_fecha, enMarco ? 12 : 13), !_fecha.Enabled ? TemaEcoAccess.Borde : activa ? TemaEcoAccess.Verde : TemaEcoAccess.Suave);
                g.DrawImage(icono, boton.X + (boton.Width - icono.Width) / 2, (alto - icono.Height) / 2);
                if (enMarco) return; // el marco dibuja el borde y el foco
                using var borde = new Pen(activa ? TemaEcoAccess.Verde : TemaEcoAccess.Borde, activa ? 2 : 1);
                g.DrawPath(borde, ruta);
            }
        }

        private sealed class PintorLista : PintorNativo
        {
            private bool _encima;

            public PintorLista(ComboBox lista) : base(lista)
            {
                lista.MouseEnter += (_, _) => { _encima = true; lista.Invalidate(); };
                lista.MouseLeave += (_, _) => { _encima = false; lista.Invalidate(); };
                foreach (string evento in new[] { "GotFocus", "LostFocus", "SelectedIndexChanged", "DropDown", "DropDownClosed", "EnabledChanged", "TextChanged" })
                    typeof(ComboBox).GetEvent(evento)!.AddEventHandler(lista, new EventHandler((_, _) => lista.Invalidate()));
            }

            protected override void Pintar(Graphics g)
            {
                var lista = (ComboBox)Control;
                bool activa = lista.Focused || lista.DroppedDown;
                // Dentro de un MarcoEntrada el borde y el icono los dibuja el marco: aquí solo el relleno, el texto y la flecha.
                bool enMarco = lista.Parent is MarcoEntrada;
                if (enMarco) g.Clear(((MarcoEntrada)lista.Parent!).Relleno);
                else
                {
                    g.Clear(FondoDe(lista.Parent));
                    var area = new RectangleF(0.5f, 0.5f, lista.Width - 1.5f, lista.Height - 1.5f);
                    using var ruta = Redondeado(area, TemaEcoAccess.Px(lista, 8));
                    using (var relleno = new SolidBrush(lista.Enabled ? TemaEcoAccess.Superficie : TemaEcoAccess.Fondo)) g.FillPath(relleno, ruta);
                    using var borde = new Pen(activa || (_encima && lista.Enabled) ? TemaEcoAccess.Verde : TemaEcoAccess.Borde, activa ? 2 : 1);
                    g.DrawPath(borde, ruta);
                }
                int chevron = TemaEcoAccess.Px(lista, 12);
                Bitmap flecha = CacheIconos.Obtener(IconChar.ChevronDown, chevron, lista.Enabled ? (activa ? TemaEcoAccess.Verde : TemaEcoAccess.Suave) : TemaEcoAccess.Borde);
                int derecha = lista.Width - TemaEcoAccess.Px(lista, 12) - flecha.Width;
                g.DrawImage(flecha, derecha, (lista.Height - flecha.Height) / 2);
                var texto = Rectangle.FromLTRB(TemaEcoAccess.Px(lista, enMarco ? 4 : 10), 0, derecha - TemaEcoAccess.Px(lista, 6), lista.Height);
                TextRenderer.DrawText(g, lista.Text, lista.Font, texto, lista.Enabled ? TemaEcoAccess.Texto : TemaEcoAccess.Suave,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            }
        }

        private sealed class PintorPestanas : PintorNativo
        {
            private int _encima = -1;

            public PintorPestanas(TabControl pestanas) : base(pestanas)
            {
                pestanas.MouseMove += (_, e) =>
                {
                    int nueva = -1;
                    for (int i = 0; i < pestanas.TabCount; i++) if (pestanas.GetTabRect(i).Contains(e.Location)) nueva = i;
                    if (nueva != _encima) { _encima = nueva; pestanas.Invalidate(false); }
                };
                pestanas.MouseLeave += (_, _) => { _encima = -1; pestanas.Invalidate(false); };
                pestanas.SelectedIndexChanged += (_, _) => pestanas.Invalidate(false);
            }

            /// <summary>No pinta encima de la página visible ni de las flechas de desplazamiento (son ventanas hijas).</summary>
            protected override void Recortar(Graphics g)
            {
                var pestanas = (TabControl)Control;
                if (pestanas.SelectedTab is { } pagina) g.ExcludeClip(pagina.Bounds);
                IntPtr flechas = FindWindowEx(pestanas.Handle, IntPtr.Zero, "msctls_updown32", null);
                if (flechas != IntPtr.Zero && IsWindowVisible(flechas) && GetWindowRect(flechas, out Rect r))
                    g.ExcludeClip(pestanas.RectangleToClient(Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom)));
            }

            protected override void Pintar(Graphics g)
            {
                var pestanas = (TabControl)Control;
                g.Clear(FondoDe(pestanas.Parent));
                int franja = pestanas.TabCount > 0 ? pestanas.GetTabRect(0).Bottom : pestanas.DisplayRectangle.Top - 4;
                // Cuerpo de la página: tarjeta blanca con borde fino bajo la franja de pestañas.
                var cuerpo = new RectangleF(0.5f, franja + 0.5f, pestanas.Width - 1.5f, pestanas.Height - franja - 1.5f);
                using (var relleno = new SolidBrush(TemaEcoAccess.Superficie)) g.FillRectangle(relleno, cuerpo);
                using (var borde = new Pen(TemaEcoAccess.Borde)) g.DrawRectangle(borde, cuerpo.X, cuerpo.Y, cuerpo.Width, cuerpo.Height);
                int grosor = Math.Max(2, TemaEcoAccess.Px(pestanas, 3));
                for (int i = 0; i < pestanas.TabCount; i++)
                {
                    Rectangle r = pestanas.GetTabRect(i);
                    bool elegida = i == pestanas.SelectedIndex;
                    if (!elegida && i == _encima)
                    {
                        using var ruta = Redondeado(new RectangleF(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 3), TemaEcoAccess.Px(pestanas, 6));
                        using var hover = new SolidBrush(TemaEcoAccess.VerdeSuave);
                        g.FillPath(hover, ruta);
                    }
                    if (elegida)
                        using (var linea = new SolidBrush(TemaEcoAccess.Verde))
                            g.FillRectangle(linea, r.X + TemaEcoAccess.Px(pestanas, 4), franja - grosor + 1, r.Width - TemaEcoAccess.Px(pestanas, 8), grosor);
                    TextRenderer.DrawText(g, pestanas.TabPages[i].Text, elegida ? FuenteSemibold(pestanas.Font) : pestanas.Font, r,
                        !pestanas.Enabled ? TemaEcoAccess.Suave : elegida ? TemaEcoAccess.VerdeOscuro : i == _encima ? TemaEcoAccess.Texto : TemaEcoAccess.Suave,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
                }
                if (pestanas.Focused && PistasFoco(pestanas) && pestanas.SelectedIndex >= 0)
                {
                    Rectangle r = pestanas.GetTabRect(pestanas.SelectedIndex);
                    using var ruta = Redondeado(new RectangleF(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 5), TemaEcoAccess.Px(pestanas, 6));
                    using var foco = new Pen(TemaEcoAccess.Verde, 2);
                    g.DrawPath(foco, ruta);
                }
            }
        }
    }

    /// <summary>
    /// Caja de entrada Eco: marco blanco redondeado, icono opcional a la izquierda (como el inicio de sesión), borde de 2 px
    /// verde con el foco y fondo gris si está deshabilitada o es de solo lectura. Aloja un TextBox, un NumericUpDown, una lista
    /// desplegable o un selector de fecha: el control sigue siendo el mismo objeto con sus eventos; solo cambia quién dibuja el borde.
    /// </summary>
    internal sealed class MarcoEntrada : Panel
    {
        private static readonly MethodInfo? EstadoControl = typeof(Control).GetMethod("GetState", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(int) }, null);
        private const int EstadoVisible = 0x2;
        private readonly Control _caja;

        public MarcoEntrada(Control caja, IconChar icono = IconChar.None)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            _caja = caja;
            Icono = icono;
            Cursor = caja is ComboBox or DateTimePicker ? Cursors.Hand : Cursors.IBeam;
            Click += (_, _) => _caja.Focus();
        }

        internal IconChar Icono { get; }

        /// <summary>
        /// Verdadero cuando el marco envuelve una caja de un formulario del Designer ya medida en píxeles de pantalla: el escalado
        /// por fuente no debe volver a agrandarlo. Falso en los módulos construidos por código, que escalan por DPI.
        /// </summary>
        internal bool AltoEnPixeles { get; set; }

        private bool Multilinea => _caja is TextBoxBase { Multiline: true };

        /// <summary>Mueve el control dentro del marco (lo quita de su contenedor original) y conecta los avisos de foco y estado.</summary>
        internal void Encerrar()
        {
            bool visible = VisiblePropio(_caja);
            if (_caja is TextBox texto) texto.BorderStyle = BorderStyle.None;
            if (_caja is UpDownBase numero) numero.BorderStyle = BorderStyle.None;
            _caja.Dock = DockStyle.None;
            _caja.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            _caja.Margin = Padding.Empty;
            _caja.ForeColor = TemaEcoAccess.Texto;
            Controls.Add(_caja);
            Visible = visible;
            ActualizarFondo();
            _caja.Enter += (_, _) => Invalidate();
            _caja.Leave += (_, _) => Invalidate();
            _caja.GotFocus += (_, _) => Invalidate();
            _caja.LostFocus += (_, _) => Invalidate();
            _caja.EnabledChanged += (_, _) => ActualizarFondo();
            if (_caja is TextBoxBase t) t.ReadOnlyChanged += (_, _) => ActualizarFondo();
            // Si el formulario oculta o muestra el control, el marco lo acompaña (se compara la visibilidad propia, no la heredada).
            _caja.VisibleChanged += (_, _) =>
            {
                bool propio = VisiblePropio(_caja);
                if (VisiblePropio(this) != propio) Visible = propio;
            };
        }

        private static bool VisiblePropio(Control c) => EstadoControl is null ? c.Visible : (bool)EstadoControl.Invoke(c, new object[] { EstadoVisible })!;

        internal bool Apagada => !_caja.Enabled || _caja is TextBoxBase { ReadOnly: true } || _caja is UpDownBase { ReadOnly: true };

        /// <summary>Relleno actual del marco (lo usan los pintores de la lista y la fecha cuando están dentro).</summary>
        internal Color Relleno => Apagada ? TemaEcoAccess.Fondo : TemaEcoAccess.Superficie;

        private void ActualizarFondo()
        {
            BackColor = Relleno;
            if (_caja is not ComboBox) _caja.BackColor = Relleno;
            _caja.Invalidate();
            Invalidate();
        }

        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            base.ScaleControl(factor, AltoEnPixeles && !Multilinea ? specified & ~BoundsSpecified.Height : specified);
            AjustarAlto();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            AjustarAlto();
        }

        private void AjustarAlto()
        {
            if (Multilinea) return;
            int alto = AltoPara(_caja);
            if (alto > Height) Height = alto;
        }

        private static int Natural(Control caja) => caja switch { TextBox t => t.PreferredHeight, UpDownBase u => u.PreferredHeight, _ => caja.Height };

        /// <summary>Alto mínimo del marco de una línea: el alto natural del control más un tercio de aire (25 px → 34 px a 100 %).</summary>
        internal static int AltoPara(Control caja) => Natural(caja) * 4 / 3 + 1;

        private int Izquierda => Icono == IconChar.None ? TemaEcoAccess.Px(this, 10) : TemaEcoAccess.Px(this, _caja is TextBoxBase ? 44 : 40);

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            int izquierda = Izquierda, lado = TemaEcoAccess.Px(this, 10);
            if (Multilinea)
            {
                int arriba = TemaEcoAccess.Px(this, 9);
                _caja.SetBounds(izquierda, arriba, Math.Max(10, Width - izquierda - lado), Math.Max(10, Height - 2 * arriba));
                return;
            }
            int alto = Natural(_caja);
            // El selector numérico, la lista y la fecha dejan su flecha cerca del borde derecho del marco.
            int derecha = _caja is TextBoxBase ? lado : TemaEcoAccess.Px(this, 4);
            _caja.SetBounds(izquierda, Math.Max(0, (Height - alto) / 2), Math.Max(10, Width - izquierda - derecha), alto);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(EstiloControlesEco.FondoDe(Parent));
            bool foco = _caja.ContainsFocus;
            var area = new RectangleF(1, 1, Width - 3, Height - 3);
            using var ruta = RecursosEco.Redondeado(area, Math.Min(TemaEcoAccess.Px(this, 10), area.Height / 2f));
            using (var relleno = new SolidBrush(Relleno)) g.FillPath(relleno, ruta);
            using var borde = new Pen(foco ? TemaEcoAccess.Verde : TemaEcoAccess.Borde, foco ? TemaEcoAccess.Px(this, 2) : 1);
            g.DrawPath(borde, ruta);
            if (Icono == IconChar.None) return;
            Bitmap icono = CacheIconos.Obtener(Icono, TemaEcoAccess.Px(this, 17), !_caja.Enabled ? TemaEcoAccess.Borde : foco ? TemaEcoAccess.Verde : TemaEcoAccess.Suave);
            int y = Multilinea ? TemaEcoAccess.Px(this, 13) : (Height - icono.Height) / 2;
            g.DrawImage(icono, TemaEcoAccess.Px(this, 15), y);
        }
    }

    /// <summary>Icono FontAwesome dentro de un círculo o un cuadro redondeado: sustituye a las ilustraciones en las áreas de gestión.</summary>
    internal sealed class InsigniaIcono : Control
    {
        public InsigniaIcono(IconChar icono, Color tono, Color fondo, bool circular = true)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            Icono = icono; Tono = tono; Relleno = fondo; Circular = circular;
            BackColor = Color.Transparent;
            TabStop = false;
            AccessibleRole = AccessibleRole.Graphic;
        }

        internal IconChar Icono { get; }
        internal Color Tono { get; }
        internal Color Relleno { get; }
        internal bool Circular { get; }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int lado = Math.Min(Width, Height);
            var area = new RectangleF((Width - lado) / 2f, (Height - lado) / 2f, lado - 1, lado - 1);
            using (var pincel = new SolidBrush(Relleno))
            {
                if (Circular) g.FillEllipse(pincel, area);
                else using (var ruta = RecursosEco.Redondeado(area, lado / 5f)) g.FillPath(pincel, ruta);
            }
            Bitmap imagen = CacheIconos.Obtener(Icono, (int)(lado * 0.46f), Tono);
            g.DrawImage(imagen, (Width - imagen.Width) / 2f, (Height - imagen.Height) / 2f);
        }
    }
}
