using System;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Disposición Eco de «Ecopuntos» (Operario) y «Tarifas y ecopuntos» (Supervisor): el mismo <see cref="BannerModulo"/>
    /// de los demás módulos, saldo en fichas, historial con pastillas y las operaciones en secciones con icono. Solo
    /// presentación: reutiliza los controles y los botones que llaman a la lógica del formulario; las fichas del saldo se
    /// leen de los textos que el formulario ya escribe (si no se reconocen, quedan en «—»).
    /// </summary>
    public sealed partial class FormEcopuntos
    {
        private static readonly CultureInfo CulturaEcopuntos = CultureInfo.GetCultureInfo("es-CO");
        private readonly RejillaIndicadores _fichasSaldo = new();

        private void ConstruirDiseno(bool administracion, bool vistaCiudadana, ServicioEstadisticas? estadisticas, int actor)
        {
            string titulo = administracion ? "Tarifas y ecopuntos" : vistaCiudadana ? "Mis ecopuntos" : "Ecopuntos de los ciudadanos";
            string descripcion = administracion
                ? "Consulta el saldo de un ciudadano, acredita depósitos verificados y administra las tarifas de puntos por material."
                : vistaCiudadana ? "Tu saldo y los movimientos de tus ecopuntos."
                : "Consulta el saldo de un ciudadano y acredita los depósitos que ya verificaste.";
            var banner = new BannerModulo(titulo, descripcion, IconChar.Coins) { Estado = _estado.Text };
            _estado.TextChanged += (_, _) => banner.Estado = _estado.Text;
            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.Coins, titulo, descripcion, banner);

            // Saldo: la consulta del ciudadano y tres fichas (saldo, acreditados y revertidos).
            var saldo = ModuloEco.Seccion(vistaCiudadana ? "Mi saldo" : "Saldo del ciudadano",
                vistaCiudadana ? null : "Escribe el ID del ciudadano y consulta su saldo e historial.", icono: IconChar.Wallet);
            if (vistaCiudadana)
            {
                var propias = ModuloEco.Botones(Boton("Actualizar mi saldo", ConsultarAsync, IconChar.ArrowsRotate));
                if (estadisticas is not null)
                    propias.Controls.Add(Boton("Mi actividad diaria y semanal", () =>
                    {
                        using var actividad = new FormEstadisticasPersonales(estadisticas, actor);
                        actividad.ShowDialog(this);
                        return Task.CompletedTask;
                    }, IconChar.ChartColumn));
                saldo.Agregar(propias);
            }
            else saldo.Agregar(ModuloEco.FilaCampos(
                ModuloEco.Campo("Ciudadano (ID)", IconChar.User, _ciudadano, ancho: 240),
                ModuloEco.JuntoACampos(Boton("Consultar saldo e historial", ConsultarAsync, IconChar.MagnifyingGlass))));
            saldo.Agregar(_fichasSaldo);
            _saldo.TextChanged += (_, _) => MostrarFichasSaldo();
            _totales.TextChanged += (_, _) => MostrarFichasSaldo();
            Disposed += (_, _) => { _saldo.Dispose(); _totales.Dispose(); };
            MostrarFichasSaldo();
            ModuloEco.Fila(raiz, saldo);

            // Historial con estado vacío explicativo (el formulario alterna la tabla y el aviso).
            var historial = ModuloEco.Seccion("Historial de movimientos", "Hora de Colombia. Se muestran los últimos 50 movimientos.", icono: IconChar.ClockRotateLeft);
            var area = new Panel { Height = TemaEcoAccess.Px(this, 360), BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            TemaEcoAccess.EstilizarTabla(_historial);
            area.Controls.Add(_historial);
            area.Controls.Add(_vacio);
            _vacio.Dock = DockStyle.Fill;
            _vacio.Mostrar("Consulta un ciudadano para ver su saldo e historial. Los ecopuntos se acreditan solo después de verificar un depósito.", false);
            _historial.Visible = false;
            historial.Agregar(area);
            ModuloEco.Fila(raiz, historial);
            PulirHistorial();

            var operacion = administracion
                ? ModuloEco.Seccion("Acreditar, anular o corregir", "Usa el ID del depósito verificado o del crédito activo y la versión de la regla.", icono: IconChar.HandHoldingDollar)
                : ModuloEco.Seccion("Acreditar un depósito", "Usa el ID del depósito verificado y la versión de la regla.", icono: IconChar.HandHoldingDollar);
            operacion.Agregar(new AvisoEco(DisenoFormularios.Texto("Los ecopuntos se acreditan solo después de verificar un depósito. Cada operación queda en el historial: el libro nunca se modifica ni se borra."),
                "Antes de operar", IconChar.CircleInfo) { Margin = new Padding(0, 0, 0, 14) });
            var anular = ModuloEco.JuntoACampos(Boton("Anular crédito", () => OperarAsync(TipoOperacionEcopuntos.Revertir), IconChar.Ban)); anular.Visible = administracion;
            var corregir = ModuloEco.JuntoACampos(Boton("Corregir con nueva regla", () => OperarAsync(TipoOperacionEcopuntos.Corregir), IconChar.PenToSquare)); corregir.Visible = administracion;
            operacion.Agregar(ModuloEco.FilaCampos(
                ModuloEco.Campo("Depósito (ID)", IconChar.BoxOpen, _deposito, ancho: 200),
                ModuloEco.Campo("Regla", IconChar.ListCheck, _regla, ancho: 460),
                ModuloEco.JuntoACampos(Boton("Acreditar depósito", () => OperarAsync(TipoOperacionEcopuntos.Acreditar), IconChar.CircleCheck))));
            // Anular y corregir son del Supervisor: sin sus botones, el Operario no necesita el crédito ni el motivo.
            var filaAnular = ModuloEco.FilaCampos(
                ModuloEco.Campo("Crédito activo (ID)", IconChar.Coins, _credito, ancho: 200),
                ModuloEco.Campo("Motivo", IconChar.AlignLeft, _motivo, ancho: 460),
                anular, corregir);
            filaAnular.Visible = administracion;
            operacion.Agregar(filaAnular);
            DisenoGestion.TextoGuia(_motivo, "Explica por qué se anula o se corrige");
            operacion.Visible = !vistaCiudadana;
            ModuloEco.Fila(raiz, operacion);

            var reglas = ModuloEco.Seccion("Reglas de puntos por material", "Cada cambio crea una versión nueva desde ahora; los movimientos anteriores conservan su tarifa.", icono: IconChar.Sliders);
            _porUnidad.Font = TemaEcoAccess.Subtitulo;
            _porUnidad.Margin = new Padding(0, TemaEcoAccess.Subtitulo.Height + 18, 14, 14);
            reglas.Agregar(ModuloEco.FilaCampos(
                ModuloEco.Campo("Material", IconChar.Leaf, _material, ancho: 280),
                ModuloEco.Campo("Tarifa en puntos", IconChar.Coins, _puntos, ancho: 200),
                _porUnidad,
                ModuloEco.Campo("Descripción de la nueva versión", IconChar.AlignLeft, _descripcion, ancho: 380),
                ModuloEco.JuntoACampos(Boton("Crear regla desde ahora", CrearReglaAsync, IconChar.Plus))));
            DisenoGestion.TextoGuia(_descripcion, "Ej. Tarifa de inicio de semestre");
            reglas.Visible = administracion;
            ModuloEco.Fila(raiz, reglas);
            _estado.MaximumSize = Size.Empty;
            ModuloEco.Fila(raiz, ModuloEco.Mensaje(_estado));

            // Se desplaza dentro de su propio panel: con 1 240 px de alto mínimo, a escala 125 % el final quedaba cortado.
            ModuloEco.TerminarDesplazable(this, raiz);
        }

        /// <summary>Fichas leídas de «N ecopuntos» y «Acreditados: X · revertidos: Y» (los textos que escribe el formulario).</summary>
        private void MostrarFichasSaldo()
        {
            Match saldo = Regex.Match(_saldo.Text, @"^([-−]?[\d.,]+)\s+ecopuntos");
            Match totales = Regex.Match(_totales.Text, @"Acreditados:\s*([\d.,]+)\s*·\s*revertidos:\s*([\d.,]+)");
            bool leido = saldo.Success && totales.Success;
            _fichasSaldo.Mostrar(new[]
            {
                new Indicador("Saldo actual", IconChar.Coins, leido ? saldo.Groups[1].Value : "—", leido ? "ecopuntos disponibles" : "Consulta un ciudadano para verlo."),
                new Indicador("Acreditados", IconChar.CirclePlus, leido ? totales.Groups[1].Value : "—", "Por depósitos verificados."),
                new Indicador("Revertidos", IconChar.RotateLeft, leido ? totales.Groups[2].Value : "—", "Anulaciones y correcciones."),
            });
        }

        /// <summary>Encabezados en español, fecha legible y pastillas para el tipo y los puntos del historial.</summary>
        private void PulirHistorial()
        {
            _historial.AllowUserToResizeRows = false;
            _historial.RowHeadersVisible = false;
            DisenoGestion.PulirTabla(_historial, c => c switch
            {
                "CreditoOMovimiento" => 120, "Deposito" => 110, "Regla" => 90, "Tipo" => 140, "Puntos" => 110, "Fecha" => 160, "Actor" => 100, "Motivo" => 220, _ => 0,
            }, "Este ciudadano todavía no tiene movimientos.", IconChar.ClockRotateLeft);
            _historial.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _historial.DataBindingComplete += (_, _) =>
            {
                foreach (var (nombre, texto) in new[] { ("CreditoOMovimiento", "Movimiento"), ("Deposito", "Depósito"), ("Regla", "Regla"), ("Tipo", "Tipo"),
                             ("Puntos", "Puntos"), ("Fecha", "Fecha (Colombia)"), ("Actor", "Registró (ID)"), ("Motivo", "Motivo") })
                    if (_historial.Columns[nombre] is { } columna) columna.HeaderText = texto;
                if (_historial.Columns["Fecha"] is { } fecha) fecha.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            };
            _historial.CellPainting += PintarCeldaHistorial;
        }

        private void PintarCeldaHistorial(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics is null || e.Value is null) return;
            string columna = _historial.Columns[e.ColumnIndex].Name;
            bool elegida = (e.State & DataGridViewElementStates.Selected) != 0;
            if (columna == "Tipo" && e.Value is TipoMovimientoEcopuntos tipo)
            {
                e.PaintBackground(e.CellBounds, elegida);
                if (tipo == TipoMovimientoEcopuntos.Credito)
                    DisenoGestion.Pastilla(e.Graphics, e.CellBounds, "Crédito", TemaEcoAccess.VerdeOscuro, TemaEcoAccess.VerdeSuave, TemaEcoAccess.VerdeHoja, IconChar.CirclePlus, TemaEcoAccess.Verde, _historial);
                else
                    DisenoGestion.Pastilla(e.Graphics, e.CellBounds, "Reverso", TemaEcoAccess.Advertencia, TemaEcoAccess.AdvertenciaSuave, TemaEcoAccess.Borde, IconChar.RotateLeft, DibujoDashboard.Pendiente, _historial);
            }
            else if (columna == "Puntos" && e.Value is int puntos)
            {
                e.PaintBackground(e.CellBounds, elegida);
                string texto = puntos > 0 ? "+" + puntos.ToString("N0", CulturaEcopuntos) : puntos < 0 ? "−" + Math.Abs(puntos).ToString("N0", CulturaEcopuntos) : "0";
                Color tono = elegida ? e.CellStyle!.SelectionForeColor : puntos >= 0 ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Advertencia;
                TextRenderer.DrawText(e.Graphics, texto, DibujoDashboard.Semibold(_historial, 14f),
                    Rectangle.FromLTRB(e.CellBounds.Left + TemaEcoAccess.Px(_historial, 10), e.CellBounds.Top, e.CellBounds.Right - TemaEcoAccess.Px(_historial, 4), e.CellBounds.Bottom),
                    tono, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
            }
            else return;
            e.Handled = true;
        }
    }
}
