using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Catálogo visual de una caneca (o resultados de una búsqueda): UNA tarjeta por residuo único con su imagen, nombre,
    /// caneca y ecopuntos; al elegir una se cierra. La misma ventana sirve a las tres canecas: solo cambia el contenido.
    /// La cuadrícula calcula sus columnas con <see cref="DistribucionRejilla"/> (nunca casillas vacías) y la ventana toma
    /// el alto de su contenido hasta el 88 % de la pantalla; más allá, desplazamiento vertical. Con caneca, solo muestra
    /// residuos con imagen (los demás siguen en la búsqueda y en el catálogo de datos) y un buscador si hay más de seis.
    /// </summary>
    public sealed class FormResiduosCaneca : Form
    {
        private const int AnchoTarjeta = 210, AnchoMaximoTarjeta = 280, Separacion = 16, MaximoColumnas = 5, Margen = 28;
        private readonly List<TarjetaResiduo> _tarjetas = new();
        private readonly RejillaTarjetas _rejilla = new(AnchoTarjeta);
        private readonly Label _contador = new() { AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(3, 4, 3, 0) };
        private readonly Label _vacio = new() { AutoSize = true, ForeColor = TemaEcoAccess.Suave, Visible = false, Margin = new Padding(0, 8, 0, 8) };
        private readonly PanelDesplazable _desplazable = new() { Dock = DockStyle.Fill, Padding = new Padding(Margen, 8, Margen, 20), BackColor = TemaEcoAccess.Fondo };
        private readonly FlowLayoutPanel _cabecera = new() { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(Margen, 20, Margen, 8), BackColor = TemaEcoAccess.Fondo };
        private readonly int _total;

        public ResiduoPrototipo? Elegido { get; private set; }

        /// <summary>Tarjetas que se están mostrando (después del filtro de búsqueda).</summary>
        internal int TarjetasVisibles => _rejilla.Controls.Count;

        public FormResiduosCaneca(string titulo, string subtitulo, IReadOnlyList<ResiduoPrototipo> residuos, CategoriaMaterial? caneca = null)
        {
            // Una tarjeta por residuo: sin repetidos y, en el catálogo de una caneca, solo los que tienen imagen.
            var lista = residuos.GroupBy(r => r.Id, StringComparer.Ordinal).Select(g => g.First())
                                .Where(r => caneca is null || CatalogoPrototipo.TieneImagen(r)).ToList();
            _total = lista.Count;
            Text = titulo + " · EcoAccess";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            MinimizeBox = false;
            KeyPreview = true;
            MinimumSize = new Size(TemaEcoAccess.Px(this, 420), TemaEcoAccess.Px(this, 360));

            _cabecera.Controls.Add(new Label { Text = titulo, AutoSize = true, Font = TemaEcoAccess.TituloSeccion, ForeColor = TemaEcoAccess.VerdeOscuro });
            _cabecera.Controls.Add(new Label { Text = subtitulo, AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(3, 4, 3, 0) });
            _cabecera.Controls.Add(_contador);
            if (_total > 6)
            {
                var buscar = new TextBox { Width = TemaEcoAccess.Px(this, 320), Margin = new Padding(3, 10, 3, 2), AccessibleName = "Buscar en esta caneca" };
                CajaTexto.Sugerir(buscar, "Buscar en esta caneca…");
                buscar.TextChanged += (_, _) => Filtrar(buscar.Text);
                _cabecera.Controls.Add(buscar);
            }

            foreach (ResiduoPrototipo r in lista)
            {
                var tarjeta = new TarjetaResiduo(r);
                tarjeta.Click += (_, _) => { Elegido = r; DialogResult = DialogResult.OK; };
                _tarjetas.Add(tarjeta);
                _rejilla.Controls.Add(tarjeta);
            }
            _vacio.Text = _total == 0
                ? "Esta caneca aún no tiene residuos con imagen. Búscalos por su nombre en la pantalla principal."
                : "Ningún residuo de esta caneca coincide con la búsqueda.";
            var contenido = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty, BackColor = TemaEcoAccess.Fondo };
            contenido.Controls.Add(_vacio);
            contenido.Controls.Add(_rejilla);
            _desplazable.Controls.Add(contenido);
            _desplazable.ClientSizeChanged += (_, _) => Ajustar();
            Controls.Add(_desplazable);
            Controls.Add(_cabecera);
            ActualizarContador();
            Load += (_, _) => DimensionarVentana();
            Shown += (_, _) =>
            {
                Ajustar();
                AjustarAltoAlContenido(contenido);
                _rejilla.Controls.OfType<TarjetaResiduo>().FirstOrDefault()?.Focus();
            };
            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        }

        private void Filtrar(string texto)
        {
            string consulta = CatalogoPrototipo.Normalizar(texto);
            // Se quitan de la rejilla (no se ocultan): así no quedan huecos ni márgenes calculados para tarjetas invisibles.
            _rejilla.SuspendLayout();
            _rejilla.Controls.Clear();
            _rejilla.Controls.AddRange(_tarjetas.Where(t => consulta.Length == 0
                || CatalogoPrototipo.Normalizar(t.Residuo.Nombre).Contains(consulta)
                || t.Residuo.Sinonimos.Any(s => CatalogoPrototipo.Normalizar(s).Contains(consulta))).ToArray<Control>());
            _rejilla.ResumeLayout(true);
            ActualizarContador();
            Ajustar();
        }

        private void ActualizarContador()
        {
            int visibles = TarjetasVisibles;
            _contador.Text = visibles == _total
                ? (_total == 1 ? "1 residuo disponible" : $"{_total} residuos disponibles")
                : $"Mostrando {visibles} de {_total} residuos";
            _vacio.Visible = visibles == 0;
            _rejilla.Visible = visibles > 0;
        }

        /// <summary>Ancho para las columnas que caben en pantalla y alto justo para sus filas (con tope y desplazamiento).</summary>
        private void DimensionarVentana()
        {
            Rectangle pantalla = Screen.FromControl(Owner ?? (Control)this).WorkingArea;
            int separacion = TemaEcoAccess.Px(this, Separacion), ancho = TemaEcoAccess.Px(this, AnchoTarjeta);
            int margen = _desplazable.Padding.Horizontal + SystemInformation.VerticalScrollBarWidth;
            int columnas = DistribucionRejilla.Columnas(Math.Max(1, _total), (int)(pantalla.Width * 0.9) - margen, ancho, separacion, MaximoColumnas);
            int anchoContenido = columnas * ancho + (columnas - 1) * separacion;
            int altoTarjeta = _tarjetas.Count == 0 ? 0 : _tarjetas.Max(t => t.MedirAlto(ancho));
            int altoRejilla = _total == 0 ? _vacio.PreferredHeight + TemaEcoAccess.Px(this, 24)
                : DistribucionRejilla.Alto(_total, columnas, altoTarjeta, separacion);
            _cabecera.MaximumSize = new Size(anchoContenido + _cabecera.Padding.Horizontal, 0);
            int altoCabecera = _cabecera.GetPreferredSize(new Size(anchoContenido + _cabecera.Padding.Horizontal, 0)).Height;
            var cliente = new Size(
                Math.Max(anchoContenido + margen, _cabecera.PreferredSize.Width),
                altoCabecera + _desplazable.Padding.Vertical + altoRejilla + separacion);
            Size marco = SizeFromClientSize(cliente);
            Size = new Size(Math.Min(marco.Width, (int)(pantalla.Width * 0.95)), Math.Min(marco.Height, (int)(pantalla.Height * 0.88)));
            Location = new Point(pantalla.Left + (pantalla.Width - Width) / 2, pantalla.Top + (pantalla.Height - Height) / 2);
        }

        /// <summary>
        /// Con las tarjetas ya medidas a su ancho real, la ventana toma el alto exacto del contenido (sin franja vacía
        /// debajo), entre el mínimo y el 88 % de la pantalla. Solo al abrir: buscar no hace saltar la ventana.
        /// </summary>
        private void AjustarAltoAlContenido(Control contenido)
        {
            if (WindowState != FormWindowState.Normal) return;
            Rectangle pantalla = Screen.FromControl(this).WorkingArea;
            int deseado = contenido.PreferredSize.Height + _desplazable.Padding.Vertical;
            int alto = Math.Max(MinimumSize.Height, Math.Min(Height + deseado - _desplazable.ClientSize.Height, (int)(pantalla.Height * 0.88)));
            if (alto == Height) return;
            Height = alto;
            Top = Math.Max(pantalla.Top, pantalla.Top + (pantalla.Height - Height) / 2);
            Ajustar();
        }

        private void Ajustar()
        {
            int ancho = Math.Max(TemaEcoAccess.Px(this, 220), _desplazable.ClientSize.Width - _desplazable.Padding.Horizontal);
            int separacion = TemaEcoAccess.Px(this, Separacion);
            int columnas = DistribucionRejilla.Columnas(Math.Max(1, TarjetasVisibles), ancho, TemaEcoAccess.Px(this, AnchoTarjeta), separacion, MaximoColumnas);
            // Tarjetas de 210 a 280 px: con pocas, la rejilla no se estira a todo el ancho y queda centrada.
            int anchoRejilla = Math.Min(ancho, columnas * TemaEcoAccess.Px(this, AnchoMaximoTarjeta) + (columnas - 1) * separacion);
            _rejilla.ColumnasMaximas = columnas;
            _rejilla.Adaptar(anchoRejilla);
            // Las tarjetas de una misma fila toman el alto de la más alta: filas parejas, sin escalones.
            var enOrden = _rejilla.Controls.OfType<TarjetaResiduo>().ToList();
            _rejilla.SuspendLayout();
            for (int i = 0; i < enOrden.Count; i += columnas)
            {
                var fila = enOrden.Skip(i).Take(columnas).ToList();
                int alto = fila.Max(t => t.Height);
                foreach (TarjetaResiduo t in fila) t.Height = alto;
            }
            _rejilla.ResumeLayout(true);
            _rejilla.Margin = new Padding(Math.Max(0, (ancho - anchoRejilla) / 2), 0, 0, 0);
            _vacio.MaximumSize = new Size(ancho, 0);
            foreach (Label l in _cabecera.Controls.OfType<Label>()) l.MaximumSize = new Size(Math.Max(100, _cabecera.ClientSize.Width - _cabecera.Padding.Horizontal), 0);
        }
    }
}
