using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Catálogo visual de materiales: una tarjeta por residuo depositable con imagen (los que aún no tienen se cuentan como
    /// pendientes de recurso gráfico y siguen en la búsqueda), buscador sin tildes, filtros por caneca y detalle (galería
    /// interna del residuo, caneca, condiciones y ecopuntos). Los contadores salen del catálogo real. Elegir «Ver cómo depositarlo» devuelve el
    /// residuo al inicio, que muestra el destino y, si la caneca está en servicio en Oracle, abre el asistente de depósito.
    /// Consultar el catálogo no otorga puntos.
    /// </summary>
    public sealed class FormCatalogoMateriales : Form
    {
        private readonly CajaTexto _buscar = new() { Font = TemaEcoAccess.Entrada, AccessibleName = "Buscar residuo por nombre" };
        private readonly List<(CategoriaMaterial? Filtro, BotonEco Boton)> _filtros = new(); // el diccionario no admite la clave null de «Todas»
        private readonly RejillaTarjetas _rejilla = new(170) { ColumnasMaximas = 4 };
        private readonly PanelDesplazable _lista = new() { Dock = DockStyle.Fill, Padding = new Padding(20, 6, 12, 16), BackColor = TemaEcoAccess.Fondo };
        private readonly Label _conteo = new() { AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(12, 10, 0, 0) };
        private readonly ImagenAjustada _foto = new() { Dock = DockStyle.Top, AccessibleName = "Foto del residuo" };
        private readonly FlowLayoutPanel _miniaturas = new() { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(0, 6, 0, 6) };
        private readonly Label _nombre = new() { Dock = DockStyle.Top, AutoSize = true, Font = TemaEcoAccess.TituloSeccion, ForeColor = TemaEcoAccess.VerdeOscuro, Padding = new Padding(0, 6, 0, 2) };
        private readonly Label _caneca = new() { Dock = DockStyle.Top, AutoSize = true, Font = TemaEcoAccess.Subtitulo, Padding = new Padding(0, 2, 0, 6) };
        private readonly Label _detalle = new() { Dock = DockStyle.Top, AutoSize = true, ForeColor = TemaEcoAccess.Texto, Padding = new Padding(0, 0, 0, 8) };
        private readonly Label _puntos = new() { Dock = DockStyle.Top, AutoSize = true, ForeColor = TemaEcoAccess.Suave, Font = TemaEcoAccess.Pequena, Padding = new Padding(0, 0, 0, 10) };
        private readonly BotonEco _depositar = new("Ver cómo depositarlo", IconChar.ArrowRight, BotonEco.Estilo.Principal) { Dock = DockStyle.Top };
        private readonly BotonEco _guia = new("Ver todas las fotos de esta caneca", IconChar.Images, BotonEco.Estilo.Enlace) { Dock = DockStyle.Top, Visible = false };
        private CategoriaMaterial? _filtro;
        private ResiduoPrototipo? _actual;

        /// <summary>Residuo que el usuario quiere depositar (null si solo consultó).</summary>
        public ResiduoPrototipo? Elegido { get; private set; }

        public FormCatalogoMateriales(CategoriaMaterial? caneca = null)
        {
            Text = "Catálogo de materiales · EcoAccess";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            MinimizeBox = false;
            KeyPreview = true;
            Size = new Size(TemaEcoAccess.Px(this, 1240), TemaEcoAccess.Px(this, 780));
            MinimumSize = new Size(TemaEcoAccess.Px(this, 820), TemaEcoAccess.Px(this, 560));

            var cabecera = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(20, 16, 20, 4) };
            cabecera.Controls.Add(new Label { Text = "Catálogo de materiales", AutoSize = true, Font = TemaEcoAccess.TituloSeccion, ForeColor = TemaEcoAccess.VerdeOscuro });
            cabecera.Controls.Add(new Label
            {
                Text = TextoResumen(CatalogoFotos.ContarTodo()) + " Busca, filtra y elige uno para ver dónde va.",
                AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(3, 4, 3, 8),
            });
            var herramientas = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = Padding.Empty };
            var caja = new Panel { Size = new Size(TemaEcoAccess.Px(this, 320), TemaEcoAccess.Px(this, 36)), BackColor = Color.White, Padding = new Padding(10, 7, 10, 4), Margin = new Padding(0, 0, 12, 0) };
            _buscar.BorderStyle = BorderStyle.None;
            _buscar.Dock = DockStyle.Fill;
            _buscar.PlaceholderText = "Buscar: botella, cáscara, servilleta…";
            caja.Controls.Add(_buscar);
            herramientas.Controls.Add(caja);
            foreach (var (filtro, texto) in new (CategoriaMaterial?, string)[]
            {
                (null, "Todas"), (CategoriaMaterial.Aprovechable, "Blanca"), (CategoriaMaterial.OrganicoAprovechable, "Verde"), (CategoriaMaterial.NoAprovechable, "Negra"),
            })
            {
                var boton = new BotonEco(texto, filtro is null ? IconChar.LayerGroup : IconChar.TrashCan, BotonEco.Estilo.Secundario) { Margin = new Padding(0, 0, 8, 0), AccessibleName = "Filtrar: " + texto };
                boton.Size = boton.GetPreferredSize(Size.Empty);
                boton.Click += (_, _) => Filtrar(filtro);
                _filtros.Add((filtro, boton));
                herramientas.Controls.Add(boton);
            }
            herramientas.Controls.Add(_conteo);
            cabecera.Controls.Add(herramientas);

            // Detalle a la derecha.
            var detalle = new PanelDesplazable { Dock = DockStyle.Right, Width = TemaEcoAccess.Px(this, 400), Padding = new Padding(16, 8, 20, 16), BackColor = Color.White };
            _foto.Height = TemaEcoAccess.Px(this, 300);
            _depositar.Height = TemaEcoAccess.Px(this, 44);
            _depositar.Font = TemaEcoAccess.Subtitulo;
            _guia.Height = TemaEcoAccess.Px(this, 36);
            _depositar.Click += (_, _) => { if (_actual is not null) { Elegido = _actual; DialogResult = DialogResult.OK; } };
            _guia.Click += (_, _) => MostrarGuia();
            foreach (Control c in new Control[] { _guia, _depositar, _puntos, _detalle, _caneca, _nombre, _miniaturas, _foto }) detalle.Controls.Add(c);
            detalle.SizeChanged += (_, _) =>
            {
                int ancho = Math.Max(120, detalle.ClientSize.Width - detalle.Padding.Horizontal - 4);
                foreach (Label l in new[] { _nombre, _caneca, _detalle, _puntos }) l.MaximumSize = new Size(ancho, 0);
            };

            _lista.Controls.Add(_rejilla);
            _lista.ClientSizeChanged += (_, _) => _rejilla.Adaptar(Math.Max(TemaEcoAccess.Px(this, 300), _lista.ClientSize.Width - _lista.Padding.Horizontal));
            Controls.Add(_lista);
            Controls.Add(detalle);
            Controls.Add(cabecera);

            _buscar.TextChanged += (_, _) => Llenar();
            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
            Shown += (_, _) => { Filtrar(caneca); _buscar.Focus(); };
        }

        private void Filtrar(CategoriaMaterial? caneca)
        {
            _filtro = caneca;
            foreach (var (filtro, boton) in _filtros)
            {
                boton.Tipo = filtro == caneca ? BotonEco.Estilo.Principal : BotonEco.Estilo.Secundario;
                boton.Invalidate();
            }
            _guia.Visible = caneca is not null;
            Llenar();
        }

        private void Llenar()
        {
            string texto = CatalogoPrototipo.Normalizar(_buscar.Text);
            var residuos = CatalogoPrototipo.Todos
                .Where(CatalogoPrototipo.TieneImagen)
                .Where(r => _filtro is null || r.Caneca == _filtro)
                .Where(r => texto.Length == 0
                    || CatalogoPrototipo.Normalizar(r.Nombre).Contains(texto)
                    || r.Sinonimos.Any(s => CatalogoPrototipo.Normalizar(s).Contains(texto)))
                .ToList();
            _rejilla.SuspendLayout();
            foreach (Control c in _rejilla.Controls.Cast<Control>().ToList()) c.Dispose();
            _rejilla.Controls.Clear();
            foreach (ResiduoPrototipo r in residuos)
            {
                var tarjeta = new TarjetaResiduo(r) { Seleccionada = r == _actual };
                tarjeta.Click += (_, _) => Mostrar(r);
                _rejilla.Controls.Add(tarjeta);
            }
            _rejilla.ResumeLayout(true);
            _rejilla.Adaptar(Math.Max(TemaEcoAccess.Px(this, 300), _lista.ClientSize.Width - _lista.Padding.Horizontal));
            _conteo.Text = residuos.Count == 0
                ? "Sin resultados. Si es una pila, un medicamento o un aceite, no va en ninguna de las tres canecas."
                : residuos.Count == 1 ? "1 residuo" : $"{residuos.Count} residuos";
            if (_actual is null || !residuos.Contains(_actual)) Mostrar(residuos.FirstOrDefault());
        }

        private void Mostrar(ResiduoPrototipo? residuo)
        {
            _actual = residuo;
            foreach (TarjetaResiduo t in _rejilla.Controls.OfType<TarjetaResiduo>()) t.Seleccionada = t.Residuo == residuo;
            foreach (Control c in _miniaturas.Controls.Cast<Control>().ToList()) c.Dispose();
            _miniaturas.Controls.Clear();
            _depositar.Enabled = residuo is not null;
            if (residuo is null)
            {
                _foto.Imagen = null;
                _nombre.Text = "Elige un residuo";
                _caneca.Text = _detalle.Text = _puntos.Text = string.Empty;
                return;
            }
            var imagenes = CatalogoPrototipo.Imagenes(residuo);
            _foto.Imagen = imagenes.Count > 0 ? Imagen(imagenes[0]) : null;
            if (imagenes.Count > 1)
            {
                int lado = TemaEcoAccess.Px(this, 56);
                foreach (string clave in imagenes)
                {
                    var mini = new ImagenAjustada { Imagen = Imagen(clave), Size = new Size(lado, lado), Margin = new Padding(0, 0, 6, 6), Cursor = Cursors.Hand, AccessibleName = "Otra foto de " + residuo.Nombre };
                    mini.Click += (_, _) => _foto.Imagen = mini.Imagen;
                    _miniaturas.Controls.Add(mini);
                }
            }
            var info = CatalogoPrototipo.Canecas.First(c => c.Caneca == residuo.Caneca);
            _nombre.Text = residuo.Nombre;
            _caneca.Text = $"{info.Nombre} · {info.Descripcion}";
            _caneca.ForeColor = residuo.Caneca switch
            {
                CategoriaMaterial.OrganicoAprovechable => TemaEcoAccess.Verde,
                CategoriaMaterial.NoAprovechable => TemaEcoAccess.Texto,
                _ => TemaEcoAccess.VerdeOscuro,
            };
            _detalle.Text = $"{residuo.Descripcion}\n\nCondiciones: {residuo.Instrucciones}";
            int puntos = MatrizEcopuntosPropuesta.DeResiduo(residuo);
            _puntos.Text = puntos > 0
                ? $"Ecopuntos: {puntos} por unidad (matriz {MatrizEcopuntosPropuesta.Version}). Solo se acreditan con una cuenta, después de un depósito confirmado y según la regla vigente en Oracle."
                : "Este residuo no da ecopuntos.";
        }

        /// <summary>Resumen con los totales por caneca y generales (residuos únicos, con imagen y pendientes).</summary>
        internal static string TextoResumen(ContadorGeneral t)
        {
            string canecas = string.Join(" · ", t.Canecas.Select(c => $"{CatalogoPrototipo.NombreCaneca(c.Caneca)}: {c.ConImagen}"));
            string pendientes = t.SinImagen == 0 ? string.Empty : $" ({t.SinImagen} más esperan imagen)";
            return $"{t.ConImagen} residuos con imagen{pendientes} y {t.Fotos} fotos. {canecas}.";
        }

        private static Image? Imagen(string clave) => RecursosEco.Imagen("residuos/" + clave.Replace('.', '/') + ".jpg");

        private void MostrarGuia()
        {
            if (_filtro is not { } caneca) return;
            using var ventana = new FormGaleriaCaneca(caneca);
            ventana.ShowDialog(this);
        }
    }
}
