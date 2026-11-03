using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;
using Krypton.Toolkit;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>Presenta agregados autorizados; no ejecuta SQL ni acredita puntos. Funciona sin Power BI.</summary>
    public sealed class FormDashboardInstitucional : Form
    {
        private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

        private sealed record Opcion(int? Id, string Texto, string? Codigo = null, CategoriaMaterial? Categoria = null)
        {
            public override string ToString() => Texto;
        }

        private readonly ServicioInformeInstitucional _servicio;
        private readonly ServicioContenedores? _contenedores;
        private readonly ServicioPuntosInstalacion? _puntos;
        private readonly ServicioMateriales? _materiales;
        private readonly int _actor;
        private readonly DateTimePicker _desde = new() { Width = 135, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-29), AccessibleName = "Fecha inicial" };
        private readonly DateTimePicker _hasta = new() { Width = 135, Format = DateTimePickerFormat.Short, AccessibleName = "Fecha final" };
        private readonly ComboBox _caneca = Lista(190, "Caneca");
        private readonly ComboBox _ubicacion = Lista(190, "Ubicación");
        private readonly ComboBox _material = Lista(190, "Material");
        private readonly ComboBox _categoria = Lista(170, "Categoría");
        private readonly ComboBox _periodo = Lista(100, "Agrupar por");
        private readonly ComboBox _alcance = Lista(220, "Datos");
        private readonly KryptonButton _consultar = new() { Text = "Actualizar datos", AutoSize = true };
        private readonly KryptonButton _limpiar = new() { Text = "Limpiar filtros", AutoSize = true };
        private readonly KryptonButton _powerBi = new() { Text = "Abrir análisis en Power BI", AutoSize = true };
        private readonly Label _alcanceTexto = new() { AutoSize = true, Font = TemaEcoAccess.Subtitulo, Margin = new Padding(0, 4, 0, 8) };
        private readonly Label _estado = new() { AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 8, 0, 8) };
        private readonly RejillaTarjetas _indicadores = new(190) { ColumnasMaximas = 4 };
        private readonly TarjetaDato _solicitudes = new(IconChar.ClipboardList, "Solicitudes");
        private readonly TarjetaDato _depositos = new(IconChar.BoxOpen, "Depósitos registrados");
        private readonly TarjetaDato _verificados = new(IconChar.CircleCheck, "Depósitos verificados");
        private readonly TarjetaDato _unidades = new(IconChar.Cubes, "Unidades verificadas");
        private readonly TarjetaDato _ciudadanos = new(IconChar.Users, "Ciudadanos únicos");
        private readonly TarjetaDato _acreditados = new(IconChar.Coins, "Ecopuntos acreditados");
        private readonly TarjetaDato _reversos = new(IconChar.RotateLeft, "Ecopuntos reversados");
        private readonly TarjetaDato _netos = new(IconChar.ScaleBalanced, "Ecopuntos netos");
        private readonly PanelGraficosResumen _graficos = new();
        private readonly DataGridView _comparacion = Tabla("Comparación con el período anterior");
        private readonly DataGridView _grupos = Tabla("Actividad por caneca y ubicación");
        private readonly DataGridView _estadoCanecas = Tabla("Estado de las canecas");
        private readonly CancellationTokenSource _cierre = new();
        private InformeInstitucional? _datos;
        private bool _ocupado;
        private bool _demostracionConsultada;
        private DateOnly _desdeConsultado;
        private DateOnly _hastaConsultado;

        public FormDashboardInstitucional(ServicioInformeInstitucional servicio, int actor, ServicioContenedores? contenedores = null,
            ServicioPuntosInstalacion? puntos = null, ServicioMateriales? materiales = null, Action? abrirPowerBi = null)
        {
            _servicio = servicio; _actor = actor;
            _contenedores = contenedores; _puntos = puntos; _materiales = materiales;
            Text = "EcoAccess · dashboard institucional";
            Font = TemaEcoAccess.Cuerpo; BackColor = TemaEcoAccess.Fondo; ForeColor = TemaEcoAccess.Texto;
            AutoScaleMode = AutoScaleMode.Dpi; Size = new Size(1200, 850); MinimumSize = new Size(720, 560);

            var scroll = new PanelDesplazable { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = TemaEcoAccess.Fondo };
            var columna = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, BackColor = TemaEcoAccess.Fondo };
            var filtros = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
            _periodo.Items.AddRange(new object[] { "Día", "Semana", "Mes" }); _periodo.SelectedIndex = 0;
            _alcance.Items.AddRange(new object[] { "Datos operativos", "Demostración y simulaciones" }); _alcance.SelectedIndex = 0;
            _caneca.Items.Add(new Opcion(null, "Todas")); _caneca.SelectedIndex = 0;
            _ubicacion.Items.Add(new Opcion(null, "Todas")); _ubicacion.SelectedIndex = 0;
            _material.Items.Add(new Opcion(null, "Todos")); _material.SelectedIndex = 0;
            _categoria.Items.Add(new Opcion(null, "Todas"));
            foreach (CategoriaMaterial categoria in Compat.GetValues<CategoriaMaterial>()) _categoria.Items.Add(new Opcion(null, TextoCategoria(categoria), Categoria: categoria));
            _categoria.SelectedIndex = 0;
            foreach (var item in new (string Texto, Control Control)[] { ("Desde", _desde), ("Hasta", _hasta), ("Caneca", _caneca), ("Ubicación", _ubicacion),
                ("Material", _material), ("Categoría", _categoria), ("Agrupar", _periodo), ("Datos", _alcance) })
            { filtros.Controls.Add(new Label { Text = item.Texto, AutoSize = true, Margin = new Padding(8, 8, 4, 8) }); filtros.Controls.Add(item.Control); }
            TemaEcoAccess.EstilizarAccion(_consultar, principal: true);
            TemaEcoAccess.EstilizarAccion(_limpiar);
            TemaEcoAccess.EstilizarAccion(_powerBi);
            var acciones = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 4, 0, 8) };
            foreach (KryptonButton boton in new[] { _consultar, _limpiar }) { boton.Margin = new Padding(0, 0, 10, 0); acciones.Controls.Add(boton); }
            if (abrirPowerBi is not null)
            {
                _powerBi.Click += (_, _) => abrirPowerBi();
                acciones.Controls.Add(_powerBi);
            }

            foreach (TarjetaDato dato in new[] { _solicitudes, _depositos, _verificados, _unidades, _ciudadanos, _acreditados, _reversos, _netos }) _indicadores.Controls.Add(dato);
            columna.Controls.Add(new Label { Text = "Actividad institucional", Font = TemaEcoAccess.TituloSeccion, AutoSize = true });
            columna.Controls.Add(filtros); columna.Controls.Add(acciones); columna.Controls.Add(_alcanceTexto);
            columna.Controls.Add(_indicadores); columna.Controls.Add(_graficos);
            var marcoComparacion = Marco("Comparación con el período anterior de igual duración", _comparacion, 400);
            var marcoGrupos = Marco("Actividad por caneca y ubicación", _grupos, 260);
            var marcoCanecas = Marco("Estado de las canecas registradas", _estadoCanecas, 200);
            columna.Controls.Add(marcoComparacion); columna.Controls.Add(marcoGrupos); columna.Controls.Add(marcoCanecas);
            columna.Controls.Add(_estado); scroll.Controls.Add(columna); Controls.Add(scroll);
            scroll.ClientSizeChanged += (_, _) =>
            {
                int ancho = Math.Max(TemaEcoAccess.Px(this, 620), scroll.ClientSize.Width - scroll.Padding.Horizontal);
                columna.Width = ancho;
                _graficos.Width = ancho;
                foreach (Control marco in new Control[] { marcoComparacion, marcoGrupos, marcoCanecas }) marco.Width = ancho;
                _indicadores.Adaptar(ancho);
                _estado.MaximumSize = _alcanceTexto.MaximumSize = new Size(ancho, 0);
            };
            _consultar.Click += async (_, _) => await ConsultarAsync();
            _limpiar.Click += async (_, _) => await LimpiarAsync();
            _periodo.SelectedIndexChanged += (_, _) => Mostrar();
            Shown += async (_, _) => { await CargarFiltrosAsync(); await ConsultarAsync(); };
            FormClosed += (_, _) => _cierre.Cancel();
        }

        private static ComboBox Lista(int ancho, string nombre) => new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = ancho, AccessibleName = nombre };

        private static DataGridView Tabla(string nombre)
        {
            var tabla = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AccessibleName = nombre };
            TemaEcoAccess.EstilizarTabla(tabla);
            return tabla;
        }

        private Control Marco(string titulo, DataGridView tabla, int alto)
        {
            var marco = new Panel { Height = TemaEcoAccess.Px(this, alto), Margin = new Padding(0, 8, 0, 8), BackColor = TemaEcoAccess.Superficie };
            marco.Controls.Add(tabla);
            marco.Controls.Add(new Label { Text = titulo, Dock = DockStyle.Top, AutoSize = true, Font = TemaEcoAccess.Subtitulo, Padding = new Padding(8) });
            return marco;
        }

        private static string TextoCategoria(CategoriaMaterial categoria) => categoria switch
        {
            CategoriaMaterial.Aprovechable => "Aprovechable",
            CategoriaMaterial.OrganicoAprovechable => "Orgánico aprovechable",
            CategoriaMaterial.NoAprovechable => "No aprovechable",
            _ => "Peligroso o especial",
        };

        /// <summary>Las listas salen de Oracle: solo ofrecen canecas, ubicaciones y materiales que existen.</summary>
        private async Task CargarFiltrosAsync()
        {
            try
            {
                if (_contenedores is not null)
                {
                    IReadOnlyList<Contenedor> canecas = await _contenedores.ListarAsync(_cierre.Token);
                    foreach (Contenedor caneca in canecas.OrderBy(c => c.Nombre, StringComparer.CurrentCulture)) _caneca.Items.Add(new Opcion(caneca.Id, caneca.Nombre));
                    _estadoCanecas.DataSource = canecas.Select(c => new
                    {
                        Caneca = c.Nombre,
                        Configuración = c.EstaEnServicio ? "Habilitada en configuración" : "Deshabilitada",
                        Conectividad = "Sin telemetría: no se mide desde aquí",
                        Dispositivo = "Pendiente de prueba física",
                    }).ToList();
                }
                if (_puntos is not null)
                    foreach (PuntoInstalacion punto in (await _puntos.ListarAsync(_cierre.Token)).OrderBy(p => p.Nombre, StringComparer.CurrentCulture)) _ubicacion.Items.Add(new Opcion(punto.Id, punto.Nombre));
                if (_materiales is not null)
                    foreach (Material material in (await _materiales.ListarAsync(true, _cierre.Token)).OrderBy(m => m.Nombre, StringComparer.CurrentCulture)) _material.Items.Add(new Opcion(material.Id, material.Nombre, material.Codigo));
            }
            catch (OperationCanceledException) { }
            catch (Exception) when (!IsDisposed) { _estado.Text = "No se pudieron cargar las listas de filtros. Puedes consultar con «Todas»."; }
        }

        private async Task LimpiarAsync()
        {
            _desde.Value = DateTime.Today.AddDays(-29);
            _hasta.Value = DateTime.Today;
            foreach (ComboBox lista in new[] { _caneca, _ubicacion, _material, _categoria, _periodo, _alcance }) lista.SelectedIndex = 0;
            await ConsultarAsync();
        }

        private async Task ConsultarAsync()
        {
            if (_ocupado) return;
            _ocupado = true; _consultar.Enabled = _limpiar.Enabled = false; _datos = null;
            _graficos.Limpiar("Consultando…"); _grupos.DataSource = null; _comparacion.DataSource = null;
            _estado.ForeColor = TemaEcoAccess.Suave; _estado.Text = "Consultando Oracle…";
            foreach (TarjetaDato dato in Tarjetas()) dato.Mostrar("—", "Consultando…");
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(_cierre.Token);
            limite.CancelAfter(TimeSpan.FromSeconds(45));
            try
            {
                var periodo = HoraColombia.PeriodoUtc(DateOnly.FromDateTime(_desde.Value), DateOnly.FromDateTime(_hasta.Value));
                bool demostracion = _alcance.SelectedIndex == 1;
                var r = await _servicio.ConsultarAsync(_actor, new(periodo.DesdeUtc, periodo.HastaUtc,
                    (_caneca.SelectedItem as Opcion)?.Id, (_ubicacion.SelectedItem as Opcion)?.Id, demostracion,
                    (_material.SelectedItem as Opcion)?.Codigo, (_categoria.SelectedItem as Opcion)?.Categoria), limite.Token);
                if (IsDisposed) return;
                if (!r.Exitoso) { MostrarFallo(r.Mensaje ?? "Consulta no disponible."); return; }
                _demostracionConsultada = demostracion;
                _desdeConsultado = DateOnly.FromDateTime(periodo.DesdeUtc.AddHours(-5));
                _hastaConsultado = DateOnly.FromDateTime(periodo.HastaUtc.AddHours(-5)).AddDays(-1);
                _datos = r.Valor; Mostrar();
            }
            catch (OperationCanceledException) { if (!IsDisposed) MostrarFallo("Consulta cancelada o tiempo agotado. Puedes reintentar con «Actualizar datos»."); }
            catch (Exception) { if (!IsDisposed) MostrarFallo("No se pudo consultar el informe. Revisa la conexión con Oracle y reintenta."); }
            finally { _ocupado = false; if (!IsDisposed) _consultar.Enabled = _limpiar.Enabled = true; }
        }

        private IEnumerable<TarjetaDato> Tarjetas() => new[] { _solicitudes, _depositos, _verificados, _unidades, _ciudadanos, _acreditados, _reversos, _netos };

        // Una consulta fallida nunca se muestra como cero.
        private void MostrarFallo(string mensaje)
        {
            foreach (TarjetaDato dato in Tarjetas()) dato.Mostrar("—", "Sin datos verificados.");
            _graficos.Limpiar("Sin datos verificados para mostrar.");
            _alcanceTexto.Text = "Sin datos";
            _estado.ForeColor = TemaEcoAccess.Error;
            _estado.Text = mensaje;
        }

        private void Mostrar()
        {
            if (_datos is not { } d) return;
            var i = d.Actual;
            bool porMaterial = _material.SelectedItem is Opcion { Codigo: not null } || _categoria.SelectedItem is Opcion { Categoria: not null };
            _alcanceTexto.Text = _demostracionConsultada ? "DEMOSTRACIÓN Y SIMULACIONES: no son actividad física." : "DATOS OPERATIVOS, sin marcas de ensayo: no certifican actividad física.";
            string N(long valor) => valor.ToString("N0", Colombia);
            const string soloDepositos = "No se reparte por material: muestra el total del período.";
            _solicitudes.Mostrar(N(i.Solicitudes), porMaterial ? soloDepositos : "Interacciones iniciadas.");
            _depositos.Mostrar(N(i.Depositos), $"Anterior: {N(d.Anterior.Depositos)}.");
            _verificados.Mostrar(N(i.Verificados), $"Anterior: {N(d.Anterior.Verificados)}.");
            _unidades.Mostrar(N(i.UnidadesVerificadas), i.VerificadosSinConteo > 0 ? $"{N(i.VerificadosSinConteo)} verificados históricos sin conteo. No son kilos." : "Son unidades, no kilos.");
            _ciudadanos.Mostrar(N(i.Ciudadanos), "Personas distintas en el período.");
            _acreditados.Mostrar(N(i.Acreditados), porMaterial ? soloDepositos : $"Anterior: {N(d.Anterior.Acreditados)}.");
            _reversos.Mostrar(N(i.Reversos), porMaterial ? soloDepositos : "Anulaciones y correcciones.");
            _netos.Mostrar(N(i.Netos), porMaterial ? soloDepositos : "Acreditados más reversos.");
            var dias = d.Dias;
            if (_periodo.SelectedIndex > 0)
                dias = d.Dias.Where(f => f.Dia is not null).GroupBy(f => _periodo.SelectedIndex == 1
                    ? f.Dia!.Value.AddDays(-(((int)f.Dia.Value.DayOfWeek + 6) % 7)) : new DateOnly(f.Dia!.Value.Year, f.Dia.Value.Month, 1))
                    .Select(g => new FilaAgrupada(null, g.Key, g.Key.ToString("yyyy-MM-dd"), g.Sum(f => f.Depositos), g.Sum(f => f.Verificados), g.Sum(f => f.ConPesoMedido), g.Sum(f => f.KgMedidos), 0, 0)).ToArray();
            _graficos.MostrarInforme(d, dias, _desdeConsultado, _hastaConsultado);
            _comparacion.DataSource = ComparacionPeriodos.Filas(i, d.Anterior)
                .Select(f => new { f.Indicador, Actual = f.Actual, Anterior = f.Anterior, Diferencia = f.Diferencia, Variación = f.Variacion }).ToList();
            _grupos.DataSource = d.Canecas.Select(f => new { Dimensión = "Caneca", Grupo = f.Etiqueta, Depósitos = f.Depositos, Verificados = f.Verificados })
                .Concat(d.Ubicaciones.Select(f => new { Dimensión = "Ubicación", Grupo = f.Etiqueta, Depósitos = f.Depositos, Verificados = f.Verificados })).ToList();
            _estado.ForeColor = TemaEcoAccess.Suave;
            _estado.Text = (_demostracionConsultada ? "Ensayos y simulaciones separados del informe operativo. " : "Se excluyen simulaciones y datos de prueba conocidos; su ausencia no certifica actividad física. ") +
                $"Opiniones globales del período: {N(i.Opiniones)}, promedio {(i.Promedio is { } media ? media.ToString("N2", Colombia) : "sin valoraciones")} de 5; no se atribuyen a una caneca. " +
                "Períodos en hora de Colombia (America/Bogota, UTC−5); las semanas y los meses de los extremos pueden ser parciales. Los bloques de la UPC son proyecciones y no aparecen aquí.";
        }
    }
}
