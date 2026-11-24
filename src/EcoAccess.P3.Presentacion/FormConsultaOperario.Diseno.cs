using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Disposición Eco de «Consultar» del Operario (skills eco-ui-operario y eco-ui-supervisor): el mismo
    /// <see cref="BannerModulo"/> de los demás módulos, aviso de solo lectura, pestañas tipo píldora y tablas pulidas con
    /// pastillas. Solo presentación: reutiliza las tablas, la caja de búsqueda y la etiqueta de estado que llena el formulario;
    /// la búsqueda y la carga siguen en <c>FormConsultaOperario.cs</c>.
    /// </summary>
    public sealed partial class FormConsultaOperario
    {
        private void Construir()
        {
            var banner = new BannerModulo("Consultar ciudadanos y canecas",
                "Busca ciudadanos por nombre o correo y revisa el estado de las canecas y el catálogo de materiales habilitados.",
                IconChar.MagnifyingGlass) { Estado = "Consultando ciudadanos, canecas y materiales…" };
            _estado.TextChanged += (_, _) => banner.Estado = _estado.Text;
            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.MagnifyingGlass, "Consultar ciudadanos y canecas",
                "Busca ciudadanos por nombre o correo y revisa el estado de las canecas y el catálogo de materiales habilitados.", banner);

            ModuloEco.Fila(raiz, new AvisoEco(DisenoFormularios.Texto("Para crear cuentas, cambiar canecas o editar materiales se necesita el área Supervisor."),
                "Consulta de solo lectura", IconChar.Lock) { Margin = new Padding(0, 0, 0, 14) });

            // Cada página tiene su barra de búsqueda (la misma pieza): Ciudadanos consulta Oracle como siempre; Canecas y
            // Materiales filtran la lista que el formulario ya cargó completa (no hay otra consulta ni cambia la lógica).
            _buscar.Dock = DockStyle.None;
            Control barraCiudadanos = BarraBusqueda("Nombre o correo", "Escribe un nombre o un correo y pulsa Enter", _buscar, "Buscar ciudadanos",
                async () => await EjecutarAsync(BuscarCiudadanosAsync));
            var filtroCanecas = new FiltroTabla<FilaCaneca>(_canecas, "caneca", "canecas",
                (c, t) => Contiene(c.Nombre, t) || Contiene(c.Estado, t) || c.Id.ToString() == t);
            var filtroMateriales = new FiltroTabla<FilaMaterial>(_catalogo, "material", "materiales",
                (m, t) => Contiene(m.Nombre, t) || Contiene(m.Codigo, t) || Contiene(m.Categoria, t));
            Control barraCanecas = BarraBusqueda("Nombre, código o estado de la caneca", "Ej. verde, 1400 o fuera de servicio", filtroCanecas.Caja, "Buscar canecas", filtroCanecas.Aplicar);
            Control barraMateriales = BarraBusqueda("Nombre, código o categoría del material", "Ej. PET, vidrio o peligroso", filtroMateriales.Caja, "Buscar materiales", filtroMateriales.Aplicar);

            var paginas = new PaginasSinPestanas { Dock = DockStyle.Fill, AccessibleName = "Consulta del operario" };
            paginas.TabPages.Add(Pagina("Ciudadanos", _ciudadanos, barraCiudadanos, null));
            paginas.TabPages.Add(Pagina("Canecas", _canecas, barraCanecas, filtroCanecas.Resumen));
            paginas.TabPages.Add(Pagina("Materiales", _catalogo, barraMateriales, filtroMateriales.Resumen));
            var marco = new Panel { Height = TemaEcoAccess.Px(this, 480), BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            marco.Controls.Add(paginas);

            var resultados = ModuloEco.Seccion("Resultados de la consulta", icono: IconChar.Table);
            resultados.Agregar(new SelectorPestanas(paginas, IconChar.Users, IconChar.TrashCan, IconChar.Leaf) { Dock = DockStyle.Fill });
            resultados.Agregar(marco);
            ModuloEco.Fila(raiz, resultados);
            ModuloEco.Fila(raiz, ModuloEco.Mensaje(_estado));

            ModuloEco.TerminarDesplazable(this, raiz, 640);
            PulirTablasConsulta();
        }

        /// <summary>Página blanca: la barra de búsqueda arriba, la tabla al resto y, si hay, el resumen del filtro debajo.</summary>
        private static TabPage Pagina(string titulo, DataGridView tabla, Control busqueda, Label? resumen)
        {
            var pagina = new TabPage(titulo) { BackColor = TemaEcoAccess.Superficie, Padding = Padding.Empty, UseVisualStyleBackColor = false };
            var columna = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            columna.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            ModuloEco.Fila(columna, busqueda);
            ModuloEco.Fila(columna, tabla, llenar: true);
            tabla.Margin = new Padding(0, 4, 0, 2);
            if (resumen is not null) ModuloEco.Fila(columna, ModuloEco.Mensaje(resumen, pequeno: true));
            pagina.Controls.Add(columna);
            return pagina;
        }

        /// <summary>
        /// Barra de búsqueda reutilizable de las tres páginas: campo con lupa y texto guía, y botón principal «Buscar».
        /// Enter en la caja hace lo mismo que el botón.
        /// </summary>
        private static Control BarraBusqueda(string etiqueta, string guia, TextBox caja, string nombreBoton, Action buscar)
        {
            var boton = new Button { Text = "Buscar", AccessibleName = nombreBoton };
            TemaEcoAccess.EstilizarBoton(boton, true);
            boton.Click += (_, _) => buscar();
            caja.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; buscar(); } };
            DisenoGestion.TextoGuia(caja, guia);
            var campo = ModuloEco.Campo(etiqueta, IconChar.MagnifyingGlass, caja);
            campo.Dock = DockStyle.Fill;
            var barra = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            barra.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            barra.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            barra.Controls.Add(campo, 0, 0);
            barra.Controls.Add(ModuloEco.JuntoACampos(ModuloEco.Boton(boton, IconChar.MagnifyingGlass)), 1, 0);
            return barra;
        }

        /// <summary>Coincidencia sin distinguir mayúsculas ni tildes («peligroso» encuentra «Peligroso», «vehiculo» encuentra «vehículo»).</summary>
        private static bool Contiene(string? texto, string buscado) =>
            texto is not null && CulturaConsulta.CompareInfo.IndexOf(texto, buscado, System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace) >= 0;

        private static readonly System.Globalization.CultureInfo CulturaConsulta = System.Globalization.CultureInfo.GetCultureInfo("es-CO");

        /// <summary>
        /// Filtro de una tabla sobre la lista que el formulario ya cargó (presentación: no consulta Oracle). Guarda la lista
        /// completa cada vez que el formulario la cambia, muestra solo las filas que coinciden y conserva los encabezados que
        /// el formulario puso. Una sola clase genérica para Canecas y Materiales.
        /// </summary>
        private sealed class FiltroTabla<T>
        {
            private readonly DataGridView _tabla;
            private readonly Func<T, string, bool> _coincide;
            private readonly string _singular, _plural;
            private System.Collections.Generic.List<T> _todas = new();
            private bool _aplicando;

            internal FiltroTabla(DataGridView tabla, string singular, string plural, Func<T, string, bool> coincide)
            {
                _tabla = tabla; _singular = singular; _plural = plural; _coincide = coincide;
                Caja = new TextBox { MaxLength = 120, AccessibleName = $"Buscar {plural}" };
                Resumen = new Label { UseMnemonic = false, ForeColor = TemaEcoAccess.Suave };
                tabla.DataSourceChanged += (_, _) =>
                {
                    if (_aplicando || tabla.DataSource is not System.Collections.Generic.List<T> lista) return;
                    _todas = lista;
                    Aplicar();
                };
            }

            internal TextBox Caja { get; }
            internal Label Resumen { get; }

            internal void Aplicar()
            {
                string texto = Caja.Text.Trim();
                var lista = texto.Length == 0 ? _todas : _todas.Where(x => _coincide(x, texto)).ToList();
                if (!ReferenceEquals(_tabla.DataSource, lista))
                {
                    var titulos = _tabla.Columns.Cast<DataGridViewColumn>().ToDictionary(c => c.Name, c => c.HeaderText);
                    _aplicando = true;
                    try { _tabla.DataSource = lista; }
                    finally { _aplicando = false; }
                    foreach (var par in titulos) if (_tabla.Columns[par.Key] is { } columna) columna.HeaderText = par.Value;
                }
                string total = $"{_todas.Count} {(_todas.Count == 1 ? _singular : _plural)}";
                Resumen.Text = texto.Length == 0 ? $"{total}. Escribe arriba para filtrar."
                    : lista.Count == 0 ? $"Ninguna coincidencia con «{texto}» entre {total}."
                    : $"Mostrando {lista.Count} de {total} que coinciden con «{texto}».";
            }
        }

        private void PulirTablasConsulta()
        {
            DisenoGestion.PulirTabla(_ciudadanos, c => c switch { "Id" => 95, "Nombre" => 190, "Correo" => 200, "Estado" => 110, "Registro" => 105, _ => 0 },
                "Escribe un nombre o un correo y pulsa «Buscar» para ver ciudadanos.", IconChar.Users);
            DisenoGestion.PulirTabla(_canecas, c => c switch { "Id" => 100, "Nombre" => 280, "Estado" => 190, "Materiales" => 190, _ => 0 },
                "No hay canecas registradas para mostrar.", IconChar.TrashCan);
            DisenoGestion.PulirTabla(_catalogo, c => c switch { "Codigo" => 170, "Nombre" => 260, "Categoria" => 260, _ => 0 },
                "No hay materiales habilitados en el catálogo.", IconChar.Leaf);
            // Peso de cada columna al repartir el ancho: el código es corto y no debe ocupar lo mismo que el nombre.
            foreach (var (tabla, pesos) in new (DataGridView, (string Columna, float Peso)[])[]
            {
                (_ciudadanos, new[] { ("Id", 55f), ("Nombre", 160f), ("Correo", 160f), ("Estado", 75f), ("Registro", 90f) }),
                (_canecas, new[] { ("Id", 40f), ("Nombre", 170f), ("Estado", 90f), ("Materiales", 90f) }),
                (_catalogo, new[] { ("Codigo", 60f), ("Nombre", 150f), ("Categoria", 120f) }),
            })
                tabla.DataBindingComplete += (_, _) =>
                {
                    foreach (var (columna, peso) in pesos)
                        if (tabla.Columns[columna] is { } c) c.FillWeight = peso;
                };
            _ciudadanos.CellPainting += PintarCeldaConsulta;
            _canecas.CellPainting += PintarCeldaConsulta;
            _catalogo.CellPainting += PintarCeldaConsulta;
        }

        private void PintarCeldaConsulta(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (sender is not DataGridView tabla || e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics is null || e.Value is null) return;
            string columna = tabla.Columns[e.ColumnIndex].Name;
            bool elegida = (e.State & DataGridViewElementStates.Selected) != 0;
            Graphics g = e.Graphics;
            int px(int n) => TemaEcoAccess.Px(tabla, n);
            Rectangle zona(int izquierda) => Rectangle.FromLTRB(e.CellBounds.Left + px(izquierda), e.CellBounds.Top, e.CellBounds.Right - px(4), e.CellBounds.Bottom);
            const TextFormatFlags Linea = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

            if (tabla == _ciudadanos && columna == "Nombre" && e.Value is string nombre)
            {
                // Iniciales en un círculo verde suave, como en Usuarios y roles.
                e.PaintBackground(e.CellBounds, elegida);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int lado = px(30), x = e.CellBounds.Left + px(10), y = e.CellBounds.Top + (e.CellBounds.Height - lado) / 2;
                using (var fondo = new SolidBrush(TemaEcoAccess.VerdeSuave)) g.FillEllipse(fondo, x, y, lado, lado);
                string iniciales = string.Concat(nombre.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));
                TextRenderer.DrawText(g, iniciales, DibujoDashboard.Semibold(tabla, 11f), new Rectangle(x, y, lado, lado), TemaEcoAccess.VerdeOscuro,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, nombre, DibujoDashboard.Semibold(tabla, 13.5f), zona(10 + 30 + 10), elegida ? e.CellStyle!.SelectionForeColor : TemaEcoAccess.Texto, Linea);
            }
            else if (tabla == _ciudadanos && columna == "Estado" && e.Value is string cuenta)
            {
                e.PaintBackground(e.CellBounds, elegida);
                DisenoGestion.PastillaEstado(g, e.CellBounds, cuenta, cuenta == "Activa", tabla);
            }
            else if (tabla == _canecas && columna == "Nombre" && e.Value is string caneca)
            {
                e.PaintBackground(e.CellBounds, elegida);
                Bitmap icono = CacheIconos.Obtener(IconChar.TrashCan, px(16), TemaEcoAccess.Verde);
                g.DrawImage(icono, e.CellBounds.Left + px(10), e.CellBounds.Top + (e.CellBounds.Height - icono.Height) / 2);
                TextRenderer.DrawText(g, caneca, DibujoDashboard.Semibold(tabla, 13.5f), zona(34), elegida ? e.CellStyle!.SelectionForeColor : TemaEcoAccess.Texto, Linea);
            }
            else if (tabla == _canecas && columna == "Estado" && e.Value is string estado)
            {
                // En servicio en verde, fuera de servicio en ámbar y dada de baja en rojo (skill eco-ui-operario).
                e.PaintBackground(e.CellBounds, elegida);
                if (estado == "En servicio") DisenoGestion.PastillaEstado(g, e.CellBounds, estado, true, tabla);
                else if (estado == "Fuera de servicio")
                    DisenoGestion.Pastilla(g, e.CellBounds, estado, TemaEcoAccess.Advertencia, TemaEcoAccess.AdvertenciaSuave, TemaEcoAccess.Borde, IconChar.CirclePause, DibujoDashboard.Pendiente, tabla);
                else
                    DisenoGestion.Pastilla(g, e.CellBounds, estado, TemaEcoAccess.Error, TemaEcoAccess.ErrorSuave, TemaEcoAccess.Borde, IconChar.Ban, TemaEcoAccess.Error, tabla);
            }
            else if (tabla == _canecas && columna == "Materiales" && e.Value is int cantidad)
            {
                e.PaintBackground(e.CellBounds, elegida);
                DisenoGestion.Pastilla(g, e.CellBounds, cantidad == 1 ? "1 material" : $"{cantidad} materiales", TemaEcoAccess.VerdeOscuro, TemaEcoAccess.VerdeSuave,
                    TemaEcoAccess.VerdeHoja, IconChar.Leaf, TemaEcoAccess.Verde, tabla);
            }
            else if (tabla == _catalogo && columna == "Codigo" && e.Value is string codigo)
            {
                e.PaintBackground(e.CellBounds, elegida);
                TextRenderer.DrawText(g, codigo, DibujoDashboard.Semibold(tabla, 13.5f), zona(10), elegida ? e.CellStyle!.SelectionForeColor : TemaEcoAccess.VerdeOscuro, Linea);
            }
            else if (tabla == _catalogo && columna == "Categoria" && e.Value is string categoria
                && TextosCategoria.Opciones.FirstOrDefault(o => o.Texto == categoria) is { } opcion)
            {
                e.PaintBackground(e.CellBounds, elegida);
                var (tono, fondo, icono, _) = EstiloCategoria.Para(opcion.Valor);
                DisenoGestion.Pastilla(g, e.CellBounds, categoria, tono, fondo, TemaEcoAccess.Borde, icono, tono, tabla);
            }
            else return;
            e.Handled = true;
        }
    }
}
