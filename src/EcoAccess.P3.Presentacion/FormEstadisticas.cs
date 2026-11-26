using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;
using System.Text;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Estadísticas de uso para operarios y administradores (CU-15). Solo consulta: arma el filtro con los
    /// días de Colombia elegidos, pide a <see cref="ServicioEstadisticas"/> el resumen, la agrupación, las incidencias
    /// y una página del detalle, y los muestra. Los permisos y la validez del filtro los decide el servicio.
    /// </summary>
    /// <remarks>
    /// Cada categoría se muestra aparte (una orden emitida no es una apertura confirmada ni un depósito) y los
    /// kilogramos medidos nunca se suman con los estimados (RN-12). Los usuarios aparecen solo por id (RN-13).
    /// </remarks>
    public partial class FormEstadisticas : Form
    {
        private const int TamanoPagina = 50;
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");

        private readonly ServicioEstadisticas _estadisticas;
        private readonly ServicioContenedores _contenedores;
        private readonly ServicioMateriales _materiales;
        private readonly int _idActor;
        private FiltroEstadisticas? _filtroConsultado;
        private int _pagina = 1;
        private int _totalPaginas;
        private bool _ocupado;

        public FormEstadisticas(ServicioEstadisticas estadisticas, ServicioContenedores contenedores, ServicioMateriales materiales, int idActor)
        {
            _estadisticas = estadisticas ?? throw new ArgumentNullException(nameof(estadisticas));
            _contenedores = contenedores ?? throw new ArgumentNullException(nameof(contenedores));
            _materiales = materiales ?? throw new ArgumentNullException(nameof(materiales));
            _idActor = idActor;
            InitializeComponent();

            DateOnly hoy = DateOnly.FromDateTime(HoraColombia.DesdeUtc(DateTime.UtcNow));
            fechaDesde.Value = hoy.AddDays(-29).ToDateTime(TimeOnly.MinValue);
            fechaHasta.Value = hoy.ToDateTime(TimeOnly.MinValue);

            foreach (ComboBox combo in new[] { comboCaneca, comboMaterial, comboVerificacion, comboCriterio, comboAgrupacion })
            {
                combo.DisplayMember = nameof(Opcion<int>.Texto);
            }

            comboVerificacion.DataSource = new List<Opcion<EstadoVerificacion>>
            {
                new(EstadoVerificacion.Todos, "Todos"),
                new(EstadoVerificacion.SinVerificar, "Sin verificar"),
                new(EstadoVerificacion.Verificado, "Verificados"),
            };
            comboCriterio.DataSource = new List<Opcion<CriterioMaterial>>
            {
                new(CriterioMaterial.Declarado, "Declarado por el ciudadano"),
                new(CriterioMaterial.Verificado, "Verificado por un operario"),
            };
            comboAgrupacion.DataSource = new List<Opcion<AgrupacionDepositos>>
            {
                new(AgrupacionDepositos.Material, "Material"),
                new(AgrupacionDepositos.Contenedor, "Caneca"),
                new(AgrupacionDepositos.Punto, "Punto de instalación"),
                new(AgrupacionDepositos.Dia, "Día (Colombia)"),
                new(AgrupacionDepositos.Usuario, "Usuario (solo id)"),
            };
            var exportar = new Button
            {
                Text = "Exportar página CSV",
                AccessibleName = "Exportar la página consultada de depósitos",
                Location = new Point(820, 5),
                Size = new Size(220, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TabIndex = 3,
            };
            exportar.Click += async (_, _) => await ExportarPaginaAsync();
            panelPaginas.Controls.Add(exportar);
            // Operaciones de la estación (web, Telegram y PC; invitados y registrados) desde OPERACION_ESTACION.
            var estacion = new Button
            {
                Text = "Operaciones de la estación",
                AccessibleName = "Abrir las estadísticas de la estación de tres canecas",
                Location = new Point(580, 5),
                Size = new Size(230, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TabIndex = 4,
            };
            estacion.Click += (_, _) => { using var f = new FormEstadisticasEstacion(); f.ShowDialog(this); };
            panelPaginas.Controls.Add(estacion);
            ConstruirDiseno();
            FormClosing += (_, e) => e.Cancel = _ocupado;
        }

        private async void FormEstadisticas_Load(object? sender, EventArgs e) => await EjecutarAsync(async () =>
        {
            // Las listas incluyen canecas dadas de baja y materiales deshabilitados: sus depósitos siguen en la historia.
            IReadOnlyList<Contenedor> canecas = await _contenedores.ListarAsync();
            IReadOnlyList<Material> materiales = await _materiales.ListarAsync(soloHabilitados: false);
            comboCaneca.DataSource = ConTodas(canecas.OrderBy(c => c.Nombre).Select(c => new Opcion<int?>(c.Id, c.Nombre)), "Todas");
            comboMaterial.DataSource = ConTodas(materiales.OrderBy(m => m.Nombre).Select(m => new Opcion<int?>(m.Id, m.Nombre)), "Todos");
            // Consultar empieza desactivado (Designer): con los combos vacíos «sin filtro» se confundiría con «todas».
            botonConsultar.Enabled = true;
            await ConsultarAsync();
        });

        private async void BotonConsultar_Click(object? sender, EventArgs e) => await EjecutarAsync(ConsultarAsync);

        private async Task ExportarPaginaAsync()
        {
            if (_ocupado) return;
            if (_filtroConsultado is null)
            {
                MostrarEstado("Consulte un período antes de exportar una página.", esError: true);
                return;
            }
            FiltroEstadisticas filtro = _filtroConsultado;
            int pagina = _pagina;
            using var dialogo = new SaveFileDialog
            {
                Filter = "CSV de depósitos (*.csv)|*.csv",
                DefaultExt = "csv",
                AddExtension = true,
                FileName = $"depositos-pagina-{pagina}.csv",
                OverwritePrompt = true,
                Title = "Exportar solo esta página, no todo el período",
            };
            if (dialogo.ShowDialog(this) != DialogResult.OK) return;
            await EjecutarAsync(async () =>
            {
                // Volver a consultar exige permisos actuales, incluso si el rol cambió con la ventana abierta.
                var resultado = await _estadisticas.ListarDepositosAsync(_idActor, filtro, pagina, TamanoPagina);
                if (!Informar(resultado)) return;
                string contenido = InformeDepositosCsv.Generar(filtro, resultado.Valor);
                try
                {
                    await Compat.WriteAllTextAsync(dialogo.FileName, contenido, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                {
                    MostrarEstado("No se pudo guardar el CSV. Revise los permisos y el espacio de la carpeta elegida.", esError: true);
                    return;
                }
                MostrarEstado($"Exportada la página {pagina}: {resultado.Valor.Filas.Count} depósitos. " +
                    "El archivo no representa todo el período; la consulta se actualizó al exportar.", esError: false);
            });
        }


        private async void BotonAnterior_Click(object? sender, EventArgs e) => await CambiarPaginaAsync(_pagina - 1);

        private async void BotonSiguiente_Click(object? sender, EventArgs e) => await CambiarPaginaAsync(_pagina + 1);

        private async Task CambiarPaginaAsync(int pagina) => await EjecutarAsync(async () =>
        {
            if (_filtroConsultado is null || pagina < 1 || pagina > _totalPaginas)
            {
                return;
            }

            Resultado<PaginaDepositos> resultado = await PedirPaginaAsync(_filtroConsultado, pagina);
            if (Informar(resultado))
            {
                MostrarDetalle(resultado.Valor);
                MostrarEstado("Consulta realizada.", esError: false);
            }
        });

        // Pide las cuatro consultas y solo cambia la pantalla si todas se aceptaron: así nunca se ve el resumen de un
        // filtro con la paginación de otro.
        private async Task ConsultarAsync()
        {
            FiltroEstadisticas? filtro = ArmarFiltro();
            if (filtro is null)
            {
                return;
            }

            AgrupacionDepositos agrupacion = Valor<AgrupacionDepositos>(comboAgrupacion);
            Resultado<ResumenActividad> resumen = await _estadisticas.ObtenerResumenAsync(_idActor, filtro);
            if (!Informar(resumen))
            {
                return;
            }

            Resultado<IReadOnlyList<FilaAgrupada>> agrupadas = await _estadisticas.AgruparDepositosAsync(_idActor, filtro, agrupacion);
            Resultado<IReadOnlyList<FilaFallosContenedor>> fallos = await _estadisticas.ListarFallosPorContenedorAsync(_idActor, filtro);
            if (!Informar(agrupadas) || !Informar(fallos))
            {
                return;
            }

            Resultado<PaginaDepositos> detalle = await PedirPaginaAsync(filtro, pagina: 1);
            if (!Informar(detalle))
            {
                return;
            }

            textoResumen.Text = TextoResumen(resumen.Valor);
            MostrarAgrupadas(agrupadas.Valor);
            MostrarFallos(fallos.Valor);
            _filtroConsultado = filtro;
            MostrarDetalle(detalle.Valor);
            MostrarEstado("Consulta realizada.", esError: false);
        }

        // Si entre dos clics se borraron depósitos y la página pedida ya no existe, pide la última que queda.
        private async Task<Resultado<PaginaDepositos>> PedirPaginaAsync(FiltroEstadisticas filtro, int pagina)
        {
            Resultado<PaginaDepositos> resultado = await _estadisticas.ListarDepositosAsync(_idActor, filtro, pagina, TamanoPagina);
            if (resultado.Exitoso && resultado.Valor.TotalPaginas > 0 && pagina > resultado.Valor.TotalPaginas)
            {
                resultado = await _estadisticas.ListarDepositosAsync(_idActor, filtro, resultado.Valor.TotalPaginas, TamanoPagina);
            }

            return resultado;
        }

        private void MostrarDetalle(PaginaDepositos datos)
        {
            _pagina = datos.Pagina;
            _totalPaginas = datos.TotalPaginas;
            tablaDepositos.DataSource = datos.Filas.Select(FilaVistaDeposito.Desde).ToList();
            Encabezados(tablaDepositos,
                (nameof(FilaVistaDeposito.Id), "Depósito"),
                (nameof(FilaVistaDeposito.Detectado), "Detectado (hora Colombia)"),
                (nameof(FilaVistaDeposito.IdUsuario), "Id usuario"),
                (nameof(FilaVistaDeposito.Declarado), "Material declarado"),
                (nameof(FilaVistaDeposito.Verificado), "Material verificado"),
                (nameof(FilaVistaDeposito.KgMedidos), "Kg medidos"),
                (nameof(FilaVistaDeposito.KgEstimados), "Kg estimados"));
            etiquetaPagina.Text = _totalPaginas == 0 ? "Sin depósitos" : $"Página {_pagina} de {_totalPaginas} ({datos.Total} depósitos)";
            botonAnterior.Enabled = _pagina > 1;
            botonSiguiente.Enabled = _pagina < _totalPaginas;
        }

        // Convierte los días elegidos en el período UTC semiabierto que espera el servicio.
        private FiltroEstadisticas? ArmarFiltro()
        {
            DateOnly primerDia = DateOnly.FromDateTime(fechaDesde.Value);
            DateOnly ultimoDia = DateOnly.FromDateTime(fechaHasta.Value);
            if (ultimoDia < primerDia)
            {
                MostrarEstado("Error: el día final no puede ser anterior al inicial.", esError: true);
                return null;
            }

            (DateTime desdeUtc, DateTime hastaUtc) = HoraColombia.PeriodoUtc(primerDia, ultimoDia);
            int? idUsuario = numeroUsuario.Value == 0 ? null : (int)numeroUsuario.Value;
            return new FiltroEstadisticas(
                desdeUtc,
                hastaUtc,
                (comboCaneca.SelectedItem as Opcion<int?>)?.Valor,
                (comboMaterial.SelectedItem as Opcion<int?>)?.Valor,
                idUsuario,
                Valor<EstadoVerificacion>(comboVerificacion),
                Valor<CriterioMaterial>(comboCriterio));
        }

        private static string TextoResumen(ResumenActividad r)
        {
            ConteoInteracciones i = r.Interacciones;
            ConteoOrdenes o = r.Ordenes;
            ConteoEventos e = r.Eventos;
            ResumenDepositos d = r.Depositos;
            return string.Join(Environment.NewLine,
                $"Interacciones (solicitudes): {i.Total}. Iniciadas {i.Iniciadas}, autorizadas {i.Autorizadas}, con depósito {i.ConDeposito}, " +
                $"sin depósito {i.SinDeposito}, rechazadas {i.Rechazadas}. Por Telegram {i.PorTelegram}, manuales {i.Manuales}, simulador {i.PorSimulador}.",
                $"Órdenes de apertura emitidas: {o.Total}. Vigentes {o.Vigentes}, cerradas {o.Cerradas}, vencidas {o.Vencidas}, fallidas {o.Fallidas}. " +
                $"Apertura confirmada por el sensor: {o.ConAperturaConfirmada}; sin confirmar: {o.SinAperturaConfirmada}. Con depósito: {o.ConDeposito}.",
                $"Eventos de las canecas: {e.Total}. Aplicados {e.Aplicados}, en revisión {e.EnRevision} (aplicados + en revisión = total); fallos {e.Fallos}, que pueden estar aplicados o en revisión.",
                $"Depósitos detectados: {d.Total}. Verificados {d.Verificados} (coinciden {d.Coincidentes}, discrepan {d.Discrepantes}), sin verificar {d.SinVerificar}.",
                $"Peso medido con báscula: {Kg(d.KgMedidos)} kg en {d.ConPesoMedido} depósitos. " +
                $"Peso estimado (no es una medición): {Kg(d.KgEstimados)} kg en {d.ConPesoEstimado} depósitos.",
                "El material y la verificación solo filtran depósitos; la caneca, el usuario y las fechas filtran todo.");
        }

        private void MostrarAgrupadas(IReadOnlyList<FilaAgrupada> filas)
        {
            tablaAgrupada.DataSource = filas.Select(f => new FilaVistaAgrupada(
                f.Etiqueta,
                f.Depositos,
                f.Verificados,
                f.ConPesoMedido,
                Kg(f.KgMedidos),
                f.ConPesoEstimado,
                Kg(f.KgEstimados))).ToList();
            Encabezados(tablaAgrupada,
                (nameof(FilaVistaAgrupada.Grupo), ((Opcion<AgrupacionDepositos>)comboAgrupacion.SelectedItem!).Texto),
                (nameof(FilaVistaAgrupada.ConPesoMedido), "Con peso medido"),
                (nameof(FilaVistaAgrupada.KgMedidos), "Kg medidos"),
                (nameof(FilaVistaAgrupada.ConPesoEstimado), "Con peso estimado"),
                (nameof(FilaVistaAgrupada.KgEstimados), "Kg estimados"));
        }

        private void MostrarFallos(IReadOnlyList<FilaFallosContenedor> filas)
        {
            tablaFallos.DataSource = filas.Select(f => new FilaVistaFallos(
                f.Contenedor, f.EventosFallo, f.EventosEnRevision, f.OrdenesFallidas, f.OrdenesVencidas, f.Incidencias)).ToList();
            Encabezados(tablaFallos,
                (nameof(FilaVistaFallos.EventosFallo), "Eventos de fallo"),
                (nameof(FilaVistaFallos.EventosEnRevision), "En revisión (sin fallos)"),
                (nameof(FilaVistaFallos.OrdenesFallidas), "Órdenes fallidas"),
                (nameof(FilaVistaFallos.OrdenesVencidas), "Órdenes vencidas"));
        }

        private static string Kg(decimal kilogramos) => kilogramos.ToString("0.000", Cultura);

        private static List<Opcion<int?>> ConTodas(IEnumerable<Opcion<int?>> opciones, string textoTodas) =>
            new Opcion<int?>[] { new Opcion<int?>(null, textoTodas) }.Concat(opciones).ToList();

        private static T Valor<T>(ComboBox combo) => ((Opcion<T>)combo.SelectedItem!).Valor;

        private static void Encabezados(DataGridView tabla, params (string Columna, string Texto)[] encabezados)
        {
            foreach ((string columna, string texto) in encabezados)
            {
                if (tabla.Columns[columna] is { } c)
                {
                    c.HeaderText = texto;
                }
            }
        }

        // Igual que en las demás pantallas: bloquea la ventana durante la consulta y no muestra el texto del proveedor.
        private async Task EjecutarAsync(Func<Task> accion)
        {
            if (_ocupado)
            {
                return;
            }

            _ocupado = true;
            UseWaitCursor = true;
            panelFiltros.Enabled = false;
            pestanas.Enabled = false;
            try
            {
                await accion();
            }
            catch (Exception)
            {
                MostrarEstado("Error: no se pudo completar la consulta. Revise que Oracle esté en marcha e inténtelo de nuevo.", esError: true);
            }
            finally
            {
                _ocupado = false;
                panelFiltros.Enabled = true;
                pestanas.Enabled = true;
                UseWaitCursor = false;
            }
        }

        private bool Informar(Resultado resultado)
        {
            if (!resultado.Exitoso)
            {
                MostrarEstado($"Error: {resultado.Mensaje ?? "no se pudo completar la consulta."}", esError: true);
            }

            return resultado.Exitoso;
        }

        private void MostrarEstado(string mensaje, bool esError)
        {
            etiquetaEstado.ForeColor = esError ? TemaEcoAccess.Error : TemaEcoAccess.VerdeOscuro;
            etiquetaEstado.Text = mensaje;
        }

        private sealed record Opcion<T>(T Valor, string Texto);

        private sealed record FilaVistaAgrupada(string Grupo, int Depositos, int Verificados, int ConPesoMedido, string KgMedidos, int ConPesoEstimado, string KgEstimados);

        private sealed record FilaVistaFallos(string Caneca, int EventosFallo, int EventosEnRevision, int OrdenesFallidas, int OrdenesVencidas, int Incidencias);

        private sealed record FilaVistaDeposito(int Id, string Detectado, string Caneca, string IdUsuario, string Declarado, string Verificado, string KgMedidos, string KgEstimados)
        {
            public static FilaVistaDeposito Desde(FilaDeposito f) => new(
                f.IdDeposito,
                HoraColombia.DesdeUtc(f.FechaDeteccionUtc).ToString("yyyy-MM-dd HH:mm", Cultura),
                f.Contenedor,
                f.IdUsuario?.ToString(Cultura) ?? "—",
                f.MaterialDeclarado,
                f.MaterialVerificado ?? "Sin verificar",
                f.PesoMedidoKg is { } medido ? Kg(medido) : "—",
                f.PesoEstimadoKg is { } estimado ? Kg(estimado) : "—");
        }
    }
}
