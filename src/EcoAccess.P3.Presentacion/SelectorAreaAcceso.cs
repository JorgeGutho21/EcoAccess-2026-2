using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;
using EcoAccess.P3.Logica;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Tres tarjetas (Ciudadano, Operario, Supervisor) que se comportan como un grupo de opciones.
    /// Elegir una NO concede permisos: <see cref="AreasAcceso.Elegir"/> compara la elección con los roles reales
    /// de la cuenta después de comprobar la contraseña.
    /// </summary>
    internal sealed class SelectorAreaAcceso : TableLayoutPanel
    {
        internal static readonly IReadOnlyDictionary<AreaAcceso, (IconChar Icono, string Resumen)> Descripciones =
            new Dictionary<AreaAcceso, (IconChar, string)>
            {
                [AreaAcceso.Ciudadano] = (IconChar.User, "Tu cuenta personal: depósitos, historial, ecopuntos, Telegram e identificación de residuos."),
                [AreaAcceso.Operario] = (IconChar.ClipboardCheck, "Consulta ciudadanos, canecas y materiales; revisa y verifica depósitos; estadísticas, dashboards y Power BI."),
                [AreaAcceso.Supervisor] = (IconChar.UserShield, "Administrador del sistema: usuarios y roles, materiales, canecas, QR, puntos, tarifas, diagnóstico y auditoría."),
            };

        private readonly List<TarjetaArea> _tarjetas = new();
        internal AreaAcceso Seleccionada { get; private set; } = AreaAcceso.Ciudadano;
        internal event EventHandler? Cambio;

        internal SelectorAreaAcceso()
        {
            AutoSize = true;
            Dock = DockStyle.Top;
            ColumnCount = 3;
            RowCount = 1;
            Margin = new Padding(0, 0, 0, 12);
            AccessibleName = "Tipo de acceso";
            AccessibleRole = AccessibleRole.Grouping;
            for (int i = 0; i < 3; i++) ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            int columna = 0;
            foreach (var area in new[] { AreaAcceso.Ciudadano, AreaAcceso.Operario, AreaAcceso.Supervisor })
            {
                var tarjeta = new TarjetaArea(area, Descripciones[area].Icono) { Dock = DockStyle.Fill, TabIndex = columna };
                tarjeta.Elegida += (_, _) => Seleccionar(area);
                _tarjetas.Add(tarjeta);
                Controls.Add(tarjeta, columna++, 0);
            }
            Seleccionar(AreaAcceso.Ciudadano);
        }

        internal void Seleccionar(AreaAcceso area)
        {
            Seleccionada = area;
            foreach (var t in _tarjetas) t.Marcada = t.Area == area;
            Cambio?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Tarjeta con comportamiento de botón de opción: clic, Espacio o Intro la eligen; flechas recorren el grupo.</summary>
        private sealed class TarjetaArea : Panel
        {
            private readonly IconoEcoAccess _icono;
            private readonly Label _titulo;
            private bool _marcada;
            internal AreaAcceso Area { get; }
            internal event EventHandler? Elegida;

            internal TarjetaArea(AreaAcceso area, IconChar icono)
            {
                Area = area;
                TabStop = true;
                Cursor = Cursors.Hand;
                Margin = new Padding(0, 0, 8, 0);
                Padding = new Padding(4, 10, 4, 10);
                MinimumSize = new Size(0, 74);
                Height = 74;
                AccessibleRole = AccessibleRole.RadioButton;
                AccessibleName = $"Acceso {AreasAcceso.NombreArea(area)}";
                AccessibleDescription = Descripciones[area].Resumen;
                _icono = new IconoEcoAccess { IconChar = icono, IconSize = 24, Size = new Size(28, 28), Dock = DockStyle.Top, BackColor = Color.Transparent, TabStop = false };
                _titulo = new Label { Text = AreasAcceso.NombreArea(area), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 10), Height = 26, BackColor = Color.Transparent };
                Controls.Add(_titulo);
                Controls.Add(_icono);
                foreach (Control c in new Control[] { this, _icono, _titulo }) c.Click += (_, _) => { Focus(); Elegida?.Invoke(this, EventArgs.Empty); };
                GotFocus += (_, _) => Invalidate();
                LostFocus += (_, _) => Invalidate();
            }

            internal bool Marcada
            {
                get => _marcada;
                set
                {
                    _marcada = value;
                    BackColor = value ? TemaEcoAccess.VerdeSuave : Color.White;
                    _titulo.ForeColor = value ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Texto;
                    _icono.IconColor = value ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Suave;
                    AccessibleDescription = (value ? "Seleccionado. " : "") + Descripciones[Area].Resumen;
                    Invalidate();
                }
            }

            protected override bool IsInputKey(Keys keyData) => keyData is Keys.Left or Keys.Right || base.IsInputKey(keyData);

            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);
                if (e.KeyCode is Keys.Space or Keys.Enter) { Elegida?.Invoke(this, EventArgs.Empty); e.Handled = true; }
                else if (e.KeyCode is Keys.Left or Keys.Right && Parent is SelectorAreaAcceso grupo)
                {
                    var tarjetas = grupo._tarjetas;
                    int i = tarjetas.IndexOf(this) + (e.KeyCode == Keys.Right ? 1 : -1);
                    var destino = tarjetas[(i + tarjetas.Count) % tarjetas.Count];
                    destino.Focus();
                    grupo.Seleccionar(destino.Area);
                    e.Handled = true;
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Color borde = _marcada ? TemaEcoAccess.Verde : TemaEcoAccess.Borde;
                int grosor = _marcada || Focused ? 2 : 1;
                using var lapiz = new Pen(Focused && !_marcada ? TemaEcoAccess.Info : borde, grosor);
                e.Graphics.DrawRectangle(lapiz, grosor / 2, grosor / 2, Width - grosor, Height - grosor);
            }
        }
    }
}
