using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Configuración → Apariencia → Seleccionar interfaz. Muestra solo los diseños permitidos para el perfil (que salió del
    /// rol validado por Oracle), con vista previa, «Aplicar interfaz» y «Restablecer diseño predeterminado». Cambiar de
    /// diseño solo guarda una preferencia local: no toca Oracle, ecopuntos, Telegram ni las canecas.
    /// </summary>
    public sealed class FormSeleccionInterfaz : Form
    {
        private readonly SesionActiva? _sesion;
        private readonly PerfilApariencia _perfil;
        private readonly string _actual;
        private readonly PreferenciasApariencia _preferencias = AlmacenApariencia.Leer();
        private readonly Label _estado = new() { Dock = DockStyle.Fill, ForeColor = TemaEcoAccess.VerdeOscuro, TextAlign = ContentAlignment.MiddleLeft };

        /// <summary>true si se aplicó o restableció un diseño de panel: quien abrió el selector debe reabrirse con él.</summary>
        public bool Aplicado { get; private set; }

        public FormSeleccionInterfaz(SesionActiva? sesion, PerfilApariencia perfil, string disenoActual)
        {
            _sesion = sesion;
            _perfil = perfil;
            _actual = disenoActual;
            Text = "Configuración · Apariencia · Seleccionar interfaz";
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1000, 640);
            MinimumSize = new Size(820, 560);
            MinimizeBox = false;

            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(TemaEcoAccess.Px(this, 20)) };
            raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 32)));
            raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 52)));
            raiz.Controls.Add(new Label
            {
                Text = "Elige cómo se ve EcoAccess en este equipo. Solo cambia la presentación: tus datos, tu rol, tus ecopuntos, el bot de Telegram y las canecas siguen igual.",
                AutoSize = true, MaximumSize = new Size(TemaEcoAccess.Px(this, 940), 0), Font = new Font("Segoe UI", 11), ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 0, 0, 12),
            }, 0, 0);

            var tarjetas = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1 };
            var disenos = CatalogoApariencias.DisponiblesPara(_perfil);
            tarjetas.ColumnCount = disenos.Count;
            foreach (DisenoInterfaz diseno in disenos)
            {
                tarjetas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / disenos.Count));
                tarjetas.Controls.Add(Tarjeta(diseno));
            }
            raiz.Controls.Add(tarjetas, 0, 1);
            raiz.Controls.Add(_estado, 0, 2);

            var pie = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var cerrar = Boton("Cerrar", IconChar.Xmark, principal: false);
            cerrar.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };
            var restablecer = Boton("Restablecer diseño predeterminado", IconChar.RotateLeft, principal: false);
            restablecer.Click += (_, _) => Restablecer();
            pie.Controls.Add(cerrar);
            pie.Controls.Add(restablecer);
            raiz.Controls.Add(pie, 0, 3);
            Controls.Add(raiz);
            CancelButton = cerrar;
        }

        private Control Tarjeta(DisenoInterfaz diseno)
        {
            bool enUso = diseno.EsModoEstacion ? _preferencias.EsEstacion : string.Equals(diseno.Id, _actual, StringComparison.OrdinalIgnoreCase);
            var tarjeta = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, BackColor = TemaEcoAccess.Superficie, Margin = new Padding(0, 0, TemaEcoAccess.Px(this, 14), 0), Padding = new Padding(TemaEcoAccess.Px(this, 12)) };
            tarjeta.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tarjeta.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tarjeta.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tarjeta.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tarjeta.RowStyles.Add(new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 96)));
            tarjeta.Paint += (_, e) => { using var borde = new Pen(enUso ? TemaEcoAccess.Verde : TemaEcoAccess.Borde, enUso ? 3 : 1); e.Graphics.DrawRectangle(borde, 1, 1, tarjeta.Width - 3, tarjeta.Height - 3); };

            Image? imagen = VistaPrevia(diseno.Id);
            tarjeta.Controls.Add(imagen is null
                ? new IconPictureBox { IconChar = diseno.EsModoEstacion ? IconChar.HandPointer : IconChar.Desktop, IconColor = TemaEcoAccess.VerdeOscuro, BackColor = TemaEcoAccess.VerdeSuave, IconSize = TemaEcoAccess.Px(this, 72), SizeMode = PictureBoxSizeMode.CenterImage, Dock = DockStyle.Fill }
                : new PictureBox { Image = imagen, SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, AccessibleName = "Vista previa de " + diseno.Nombre }, 0, 0);
            tarjeta.Controls.Add(new Label { Text = diseno.Nombre, AutoSize = true, Font = TemaEcoAccess.TituloSeccion, ForeColor = TemaEcoAccess.Texto, Margin = new Padding(0, 10, 0, 2) }, 0, 1);
            string marcas = (diseno.EsPredeterminado ? "Predeterminada" : "") + (enUso ? (diseno.EsPredeterminado ? " · " : "") + (diseno.EsModoEstacion ? "Activa en este equipo" : "En uso") : "");
            tarjeta.Controls.Add(new Label { Text = marcas, AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.VerdeOscuro }, 0, 2);
            tarjeta.Controls.Add(new Label { Text = diseno.Descripcion, AutoSize = true, MaximumSize = new Size(TemaEcoAccess.Px(this, 280), 0), ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 4, 0, 4) }, 0, 3);

            var acciones = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            acciones.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            acciones.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            var previa = Boton("Vista previa", IconChar.Eye, principal: false);
            previa.Dock = DockStyle.Fill;
            previa.Enabled = imagen is not null;
            previa.Click += (_, _) => MostrarVistaPrevia(diseno, imagen!);
            string textoAplicar = diseno.EsModoEstacion ? (_preferencias.EsEstacion ? "Quitar modo estación" : "Usar este equipo como estación") : enUso ? "En uso" : "Aplicar interfaz";
            var aplicar = Boton(textoAplicar, IconChar.Check, principal: true);
            aplicar.Dock = DockStyle.Fill;
            aplicar.Enabled = diseno.EsModoEstacion || !enUso;
            aplicar.Click += (_, _) => Aplicar(diseno);
            acciones.Controls.Add(previa, 0, 0);
            acciones.Controls.Add(aplicar, 0, 1);
            tarjeta.Controls.Add(acciones, 0, 4);
            return tarjeta;
        }

        private void Aplicar(DisenoInterfaz diseno)
        {
            // El perfil viene del rol validado; aquí se vuelve a comprobar para no depender de que el botón estuviera visible.
            if (!diseno.PermitidoPara(_perfil)) { _estado.ForeColor = TemaEcoAccess.Error; _estado.Text = "Ese diseño no está disponible para tu perfil."; return; }
            if (diseno.EsModoEstacion)
            {
                bool activar = !_preferencias.EsEstacion;
                if (activar && MessageBox.Show(this, "Al abrir EcoAccess en este equipo aparecerá directamente la pantalla táctil pública (sin menús ni opciones administrativas).\n\n" +
                    "Para volver al escritorio, abre EcoAccess con el argumento --escritorio o pulsa Esc en la estación y entra como supervisor.",
                    "Modo estación", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK) return;
                _preferencias.CambiarModoEstacion(_perfil, activar);
                Guardar(activar ? "Este equipo abrirá la estación táctil la próxima vez." : "Modo estación quitado: este equipo abrirá el escritorio.");
                return;
            }
            _preferencias.Elegir(_sesion, diseno.Id);
            Guardar("Interfaz aplicada: " + diseno.Nombre + ".");
            Aplicado = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Restablecer()
        {
            _preferencias.Restablecer(_sesion);
            Guardar("Se restableció " + CatalogoApariencias.Predeterminado.Nombre + ".");
            if (!string.Equals(_actual, CatalogoApariencias.Predeterminado.Id, StringComparison.OrdinalIgnoreCase))
            {
                Aplicado = true;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void Guardar(string mensaje)
        {
            bool guardado = AlmacenApariencia.Guardar(_preferencias);
            _estado.ForeColor = guardado ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Advertencia;
            _estado.Text = guardado ? mensaje : mensaje + " No se pudo guardar en este equipo: se aplicará solo en esta sesión.";
        }

        private void MostrarVistaPrevia(DisenoInterfaz diseno, Image imagen)
        {
            using var ventana = new Form { Text = "Vista previa · " + diseno.Nombre, StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(1100, 640), BackColor = Color.Black, MinimizeBox = false };
            ventana.Controls.Add(new PictureBox { Image = imagen, SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill });
            ventana.ShowDialog(this);
        }

        private IconButton Boton(string texto, IconChar icono, bool principal)
        {
            var boton = new IconButton
            {
                Text = "  " + texto, IconChar = icono, IconSize = TemaEcoAccess.Px(this, 20), TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleCenter, TextAlign = ContentAlignment.MiddleCenter, AutoSize = true, MinimumSize = new Size(0, TemaEcoAccess.Px(this, 40)),
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = TemaEcoAccess.Subtitulo, Margin = new Padding(0, 3, TemaEcoAccess.Px(this, 8), 3),
                BackColor = principal ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Superficie, ForeColor = principal ? Color.White : TemaEcoAccess.VerdeOscuro,
                IconColor = principal ? Color.White : TemaEcoAccess.VerdeOscuro,
            };
            boton.FlatAppearance.BorderColor = principal ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Verde;
            return boton;
        }

        /// <summary>Captura real del diseño (assets/interfaces), incrustada en el ejecutable. Si falta, la tarjeta muestra un icono.</summary>
        private static Image? VistaPrevia(string id)
        {
            using Stream? flujo = typeof(FormSeleccionInterfaz).Assembly.GetManifestResourceStream("EcoAccess.Interfaz." + id + ".png");
            if (flujo is null) return null;
            using Image leida = Image.FromStream(flujo);
            return new Bitmap(leida);
        }
    }
}
