using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Campo de formulario: etiqueta arriba, caja redondeada con icono, ojo para mostrar u ocultar la contraseña y un
    /// mensaje debajo (ayuda en gris, error en rojo). Envuelve un <see cref="TextBox"/> existente para no cambiar la
    /// lógica de los formularios. El error se anuncia también en el nombre accesible del campo.
    /// </summary>
    internal sealed class CampoEco : TableLayoutPanel
    {
        private readonly Label _etiqueta = new() { AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Texto, Margin = new Padding(0, 0, 0, 6) };
        private readonly CajaCampo _caja;
        private readonly Label _mensaje = new() { AutoSize = true, Font = TemaEcoAccess.Pequena, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 5, 0, 0) };
        private readonly string _nombre;
        private string _ayuda = "";

        public CampoEco(string etiqueta, IconChar icono, TextBox campo, bool contrasena = false)
        {
            _nombre = etiqueta.Replace("&", "");
            Campo = campo;
            ColumnCount = 1;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = TemaEcoAccess.Superficie;
            Margin = new Padding(0, 0, 0, 14);
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _etiqueta.Text = etiqueta;
            _caja = new CajaCampo(icono, campo, contrasena) { Dock = DockStyle.Top, Margin = Padding.Empty };
            _caja.Height = TemaEcoAccess.Px(this, 46);
            Controls.Add(_etiqueta);
            Controls.Add(_caja);
            Controls.Add(_mensaje);
            _mensaje.Visible = false;
            campo.AccessibleName = _nombre;
            SizeChanged += (_, _) => _mensaje.MaximumSize = new Size(Math.Max(120, Width), 0);
        }

        internal TextBox Campo { get; }

        /// <summary>Texto de ayuda permanente bajo el campo (por ejemplo, el largo de la contraseña).</summary>
        internal void Ayuda(string texto)
        {
            _ayuda = texto;
            MostrarError(null);
        }

        /// <summary>Muestra el error del campo, o la ayuda si es null. El borde también cambia: no depende solo del color.</summary>
        internal void MostrarError(string? error)
        {
            _caja.ConError = error is not null;
            _mensaje.Text = error ?? _ayuda;
            _mensaje.Visible = _mensaje.Text.Length > 0;
            _mensaje.ForeColor = error is null ? TemaEcoAccess.Suave : TemaEcoAccess.Error;
            Campo.AccessibleDescription = error ?? _ayuda;
        }

        internal void MostrarValido(string texto)
        {
            _caja.ConError = false;
            _mensaje.Text = texto;
            _mensaje.Visible = true;
            _mensaje.ForeColor = TemaEcoAccess.VerdeOscuro;
        }

        /// <summary>Vuelve a ocultar la contraseña (por ejemplo, después de registrar o de un intento fallido).</summary>
        internal void OcultarContrasena() => _caja.Mostrar(false);

        /// <summary>Caja redondeada que aloja el TextBox sin borde, el icono y el botón de mostrar contraseña.</summary>
        private sealed class CajaCampo : Panel
        {
            private readonly IconChar _icono;
            private readonly TextBox _campo;
            private readonly Button? _ojo;
            private bool _visible;
            private bool _conError;

            public CajaCampo(IconChar icono, TextBox campo, bool contrasena)
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                _icono = icono;
                _campo = campo;
                BackColor = TemaEcoAccess.Superficie;
                campo.BorderStyle = BorderStyle.None;
                campo.Font = TemaEcoAccess.Entrada;
                campo.BackColor = TemaEcoAccess.Superficie;
                campo.ForeColor = TemaEcoAccess.Texto;
                campo.Dock = DockStyle.None;
                campo.Anchor = AnchorStyles.Left | AnchorStyles.Top;
                campo.Margin = Padding.Empty;
                campo.GotFocus += (_, _) => Invalidate();
                campo.LostFocus += (_, _) => Invalidate();
                Controls.Add(campo);
                Cursor = Cursors.IBeam;
                Click += (_, _) => campo.Focus();
                if (contrasena)
                {
                    campo.UseSystemPasswordChar = true;
                    _ojo = new Button { Tag = TemaEcoAccess.SinEstilo, FlatStyle = FlatStyle.Flat, BackColor = TemaEcoAccess.Superficie, TabStop = true, Cursor = Cursors.Hand };
                    _ojo.FlatAppearance.BorderSize = 0;
                    _ojo.FlatAppearance.MouseOverBackColor = TemaEcoAccess.VerdeSuave;
                    _ojo.Click += (_, _) => { Mostrar(!_visible); campo.Focus(); };
                    Controls.Add(_ojo);
                    Mostrar(false);
                }
            }

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            internal bool ConError
            {
                get => _conError;
                set { _conError = value; Invalidate(); }
            }

            internal void Mostrar(bool visible)
            {
                _visible = visible;
                _campo.UseSystemPasswordChar = !visible;
                if (_ojo is null) return;
                _ojo.Image?.Dispose();
                _ojo.Image = (visible ? IconChar.EyeSlash : IconChar.Eye).ToBitmap(IconFont.Solid, TemaEcoAccess.Px(this, 18), TemaEcoAccess.Suave);
                _ojo.AccessibleName = visible ? "Ocultar contraseña" : "Mostrar contraseña";
            }

            protected override void OnLayout(LayoutEventArgs levent)
            {
                base.OnLayout(levent);
                int izquierda = TemaEcoAccess.Px(this, 44), ojo = _ojo is null ? 0 : TemaEcoAccess.Px(this, 40);
                int alto = _campo.PreferredHeight;
                _campo.SetBounds(izquierda, (Height - alto) / 2, Math.Max(10, Width - izquierda - ojo - TemaEcoAccess.Px(this, 12)), alto);
                _ojo?.SetBounds(Width - ojo - TemaEcoAccess.Px(this, 4), TemaEcoAccess.Px(this, 4), ojo, Height - TemaEcoAccess.Px(this, 8));
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Parent?.BackColor ?? TemaEcoAccess.Superficie);
                bool foco = _campo.Focused;
                Color borde = _conError ? TemaEcoAccess.Error : foco ? TemaEcoAccess.Verde : TemaEcoAccess.Borde;
                using (var ruta = RecursosEco.Redondeado(new RectangleF(1, 1, Width - 3, Height - 3), TemaEcoAccess.Px(this, 10)))
                using (var relleno = new SolidBrush(TemaEcoAccess.Superficie))
                using (var pluma = new Pen(borde, foco || _conError ? TemaEcoAccess.Px(this, 2) : 1))
                {
                    g.FillPath(relleno, ruta);
                    g.DrawPath(pluma, ruta);
                }
                Bitmap icono = CacheIconos.Obtener(_icono, TemaEcoAccess.Px(this, 17), _conError ? TemaEcoAccess.Error : foco ? TemaEcoAccess.Verde : TemaEcoAccess.Suave);
                g.DrawImage(icono, TemaEcoAccess.Px(this, 15), (Height - icono.Height) / 2);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) _ojo?.Image?.Dispose();
                base.Dispose(disposing);
            }
        }
    }

    /// <summary>
    /// Medidor orientativo de la contraseña: cuatro segmentos y una palabra. Solo orienta; la regla obligatoria
    /// (entre 12 y 128 caracteres) la impone la lógica de negocio al registrar.
    /// </summary>
    internal sealed class MedidorContrasena : Control
    {
        private int _nivel;
        private string _texto = "";

        public MedidorContrasena()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = TemaEcoAccess.Superficie;
            Font = TemaEcoAccess.Pequena;
            TabStop = false;
            AccessibleRole = AccessibleRole.Indicator;
        }

        /// <summary>
        /// 0 vacía, 1 no cumple la política ciudadana (6 a 20), 2 aceptable, 3 buena, 4 fuerte: largo y variedad. Solo
        /// orienta; los símbolos no son obligatorios.
        /// </summary>
        internal static int Nivel(string clave)
        {
            if (clave.Length == 0) return 0;
            if (Logica.Servicios.PoliticaContrasenaCiudadano.Validar(clave) is not null) return 1;
            int variedad = new[] { clave.Any(char.IsLower), clave.Any(char.IsUpper), clave.Any(char.IsDigit), clave.Any(c => !char.IsLetterOrDigit(c)) }.Count(x => x);
            return clave.Length >= 12 && variedad >= 3 ? 4 : variedad >= 3 || clave.Length >= 12 ? 3 : 2;
        }

        internal void Evaluar(string clave)
        {
            _nivel = Nivel(clave);
            _texto = _nivel switch { 0 => "", 1 => $"No válida: usa entre {Logica.Servicios.PoliticaContrasenaCiudadano.LongitudMinima} y {Logica.Servicios.PoliticaContrasenaCiudadano.LongitudMaxima} caracteres poco comunes", 2 => "Aceptable", 3 => "Buena", _ => "Fuerte" };
            AccessibleName = _nivel == 0 ? "Fortaleza de la contraseña" : $"Fortaleza de la contraseña: {_texto}";
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color tono = _nivel switch { 1 => TemaEcoAccess.Error, 2 => TemaEcoAccess.Advertencia, _ => TemaEcoAccess.VerdeOscuro };
            int separacion = TemaEcoAccess.Px(this, 6), alto = TemaEcoAccess.Px(this, 6);
            float ancho = (Width - 3 * separacion) / 4f;
            for (int i = 0; i < 4; i++)
            {
                using var ruta = RecursosEco.Redondeado(new RectangleF(i * (ancho + separacion), 0, ancho, alto), alto / 2f);
                using var pincel = new SolidBrush(i < _nivel ? tono : TemaEcoAccess.Borde);
                g.FillPath(pincel, ruta);
            }
            TextRenderer.DrawText(g, _texto, Font, new Point(0, alto + TemaEcoAccess.Px(this, 4)), _nivel == 0 ? TemaEcoAccess.Suave : tono);
        }
    }
}
