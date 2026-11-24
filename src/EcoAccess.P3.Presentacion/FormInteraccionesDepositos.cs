using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>Consulta de hechos y verificación operativa. La autorización y las transacciones pertenecen a los servicios.</summary>
    public partial class FormInteraccionesDepositos : Form
    {
        private const int TamanoPagina = 50;
        private const int MaximoFilasAuxiliares = 200;
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");
        private readonly ServicioInteracciones _interacciones;
        private readonly ServicioEstadisticas _estadisticas;
        private readonly ServicioContenedores _contenedores;
        private readonly ServicioMateriales _materiales;
        private readonly int _idActor;
        private readonly Dictionary<int, Contenedor> _canecas = new Dictionary<int, Contenedor>();
        private FiltroEstadisticas? _filtroConsultado;
        private int _pagina = 1;
        private int _totalPaginas;
        private bool _ocupado;
        private bool _cargado;
        private CancellationTokenSource? _operacion;
        private readonly CheckBox _conteo = new() { Text = "Conté las unidades físicamente", AutoSize = true };
        private readonly NumericUpDown _cantidadVerificada = new()
        {
            Minimum = 1,
            Maximum = 1000,
            Value = 1,
            Width = 90,
            Enabled = false,
            AccessibleName = "Cantidad observada por el verificador, no cantidad declarada"
        };

        public FormInteraccionesDepositos(ServicioInteracciones interacciones, ServicioEstadisticas estadisticas,
            ServicioContenedores contenedores, ServicioMateriales materiales, int idActor)
        {
            _interacciones = interacciones ?? throw new ArgumentNullException(nameof(interacciones));
            _estadisticas = estadisticas ?? throw new ArgumentNullException(nameof(estadisticas));
            _contenedores = contenedores ?? throw new ArgumentNullException(nameof(contenedores));
            _materiales = materiales ?? throw new ArgumentNullException(nameof(materiales));
            _idActor = idActor;
            InitializeComponent();
            panelDepositos.Controls.Add(_conteo);
            panelDepositos.Controls.Add(_cantidadVerificada);
            panelDepositos.Controls.SetChildIndex(botonVerificar, panelDepositos.Controls.Count - 1);
            _conteo.CheckedChanged += (_, _) => _cantidadVerificada.Enabled = _conteo.Checked;
            DateTime hoy = HoraColombia.DesdeUtc(DateTime.UtcNow).Date;
            fechaDesde.Value = hoy.AddDays(-29);
            fechaHasta.Value = hoy;
            comboCaneca.DisplayMember = nameof(Opcion<int?>.Texto);
            comboMaterial.DisplayMember = nameof(Opcion<int>.Texto);
            comboVerificacion.DisplayMember = nameof(Opcion<EstadoVerificacion>.Texto);
            comboVerificacion.DataSource = new[]
            {
                new Opcion<EstadoVerificacion>(EstadoVerificacion.Todos, "Todos"),
                new Opcion<EstadoVerificacion>(EstadoVerificacion.SinVerificar, "Sin verificar"),
                new Opcion<EstadoVerificacion>(EstadoVerificacion.Verificado, "Verificados")
            };
            Load += Formulario_Load;
            FormClosing += Formulario_FormClosing;
            tablaDepositos.SelectionChanged += (_, _) => { _conteo.Checked = false; _cantidadVerificada.Value = 1; ActualizarAcciones(); };
            ConstruirDiseno();
        }

        private async void Formulario_Load(object? sender, EventArgs e) => await EjecutarAsync(CargarYConsultarAsync);
        private async void BotonConsultar_Click(object? sender, EventArgs e) => await EjecutarAsync(CargarYConsultarAsync);

        private async Task CargarYConsultarAsync(CancellationToken ct)
        {
            if (!_cargado)
            {
                IReadOnlyList<Contenedor> canecas = await _contenedores.ListarAsync(ct);
                IReadOnlyList<Material> materiales = await _materiales.ListarAsync(soloHabilitados: false, cancellationToken: ct);
                ct.ThrowIfCancellationRequested();
                _canecas.Clear();
                foreach (Contenedor c in canecas) _canecas.Add(c.Id, c);
                comboCaneca.DataSource = new[] { new Opcion<int?>(null, "Todas las canecas") }
                    .Concat(canecas.OrderBy(c => c.Nombre).Select(c => new Opcion<int?>(c.Id, c.Nombre))).ToList();
                comboMaterial.DataSource = materiales.OrderBy(m => m.Nombre).Select(m => new Opcion<int>(m.Id,
                    m.Habilitado ? m.Nombre : $"{m.Nombre} (deshabilitado en catálogo)")).ToList();
                _cargado = true;
            }
            await ConsultarAsync(ct);
        }

        private async void BotonAnterior_Click(object? sender, EventArgs e) => await CambiarPaginaAsync(_pagina - 1);
        private async void BotonSiguiente_Click(object? sender, EventArgs e) => await CambiarPaginaAsync(_pagina + 1);
        private void BotonCancelar_Click(object? sender, EventArgs e)
        {
            botonCancelar.Enabled = false;
            _operacion?.Cancel();
        }

        private async Task ConsultarAsync(CancellationToken ct)
        {
            if (!_cargado) return;
            DateOnly desde = DateOnly.FromDateTime(fechaDesde.Value);
            DateOnly hasta = DateOnly.FromDateTime(fechaHasta.Value);
            if (hasta < desde)
            {
                MostrarEstado("El día final no puede ser anterior al inicial.", true);
                return;
            }
            (DateTime desdeUtc, DateTime hastaUtc) = HoraColombia.PeriodoUtc(desde, hasta);
            int? caneca = (comboCaneca.SelectedItem as Opcion<int?>)?.Valor;
            var filtro = new FiltroEstadisticas(desdeUtc, hastaUtc, IdContenedor: caneca,
                Verificacion: ((Opcion<EstadoVerificacion>)comboVerificacion.SelectedItem!).Valor);
            Resultado<PaginaDepositos> depositos = await PedirPaginaAsync(filtro, 1, ct);
            if (!Informar(depositos)) return;
            IReadOnlyList<Interaccion> solicitudes = Array.Empty<Interaccion>();
            if (caneca is { } idCaneca)
            {
                var respuesta = await _interacciones.ListarPorContenedorAsync(_idActor, idCaneca, ct);
                if (!Informar(respuesta)) return;
                solicitudes = respuesta.Valor;
            }
            var eventos = await _interacciones.ListarEventosEnRevisionAsync(_idActor, ct);
            if (!Informar(eventos)) return;
            IReadOnlyList<Contenedor> canecasActualizadas = await _contenedores.ListarAsync(ct);
            ct.ThrowIfCancellationRequested();
            _canecas.Clear();
            foreach (Contenedor c in canecasActualizadas) _canecas.Add(c.Id, c);
            _filtroConsultado = filtro;
            MostrarDepositos(depositos.Valor);
            MostrarSolicitudes(solicitudes.Where(i => i.FechaInicioUtc >= desdeUtc && i.FechaInicioUtc < hastaUtc), caneca);
            MostrarIncidencias(eventos.Valor.Where(e => (!caneca.HasValue || e.IdContenedor == caneca)
                && e.FechaRecepcionUtc >= desdeUtc && e.FechaRecepcionUtc < hastaUtc));
            etiquetaConsulta.Text = $"Datos consultados: {desde:yyyy-MM-dd} a {hasta:yyyy-MM-dd} (Colombia), " +
                $"{(comboCaneca.SelectedItem as Opcion<int?>)?.Texto}. El filtro de verificación solo afecta depósitos.";
            MostrarCaneca(caneca);
            ConsultaCompletada();
        }

        private async Task CambiarPaginaAsync(int pagina) => await EjecutarAsync(async ct =>
        {
            if (_filtroConsultado is null || pagina < 1 || pagina > _totalPaginas) return;
            var resultado = await PedirPaginaAsync(_filtroConsultado, pagina, ct);
            if (!Informar(resultado)) return;
            ct.ThrowIfCancellationRequested();
            MostrarDepositos(resultado.Valor);
            ConsultaCompletada();
        });

        private async Task<Resultado<PaginaDepositos>> PedirPaginaAsync(FiltroEstadisticas filtro, int pagina, CancellationToken ct)
        {
            var resultado = await _estadisticas.ListarDepositosAsync(_idActor, filtro, pagina, TamanoPagina, ct);
            if (resultado.Exitoso && resultado.Valor.TotalPaginas > 0 && pagina > resultado.Valor.TotalPaginas)
                resultado = await _estadisticas.ListarDepositosAsync(_idActor, filtro, resultado.Valor.TotalPaginas, TamanoPagina, ct);
            return resultado;
        }

        private async void BotonVerificar_Click(object? sender, EventArgs e)
        {
            if (_ocupado || tablaDepositos.CurrentRow?.DataBoundItem is not VistaDeposito fila
                || comboMaterial.SelectedItem is not Opcion<int> material) return;
            int? cantidad = _conteo.Checked ? (int)_cantidadVerificada.Value : null;
            if (MessageBox.Show(this,
                $"¿Confirma que observó físicamente el material «{material.Texto}» en el depósito {fila.Id}?\n" +
                (cantidad is { } unidades ? $"Conteo físico independiente: {unidades} unidades.\n" : "No se verificará cantidad; las tarifas unitarias no podrán acreditarse.\n") +
                "Esta verificación quedará registrada y no se cambia desde esta pantalla. No acredita ecopuntos automáticamente.",
                "Confirmar verificación independiente", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            await EjecutarAsync(async _ =>
            {
                // La escritura no se cancela desde la interfaz: un corte no demostraría que Oracle hizo rollback.
                Resultado resultado = await _interacciones.VerificarDepositoAsync(_idActor, fila.Id, material.Valor, CancellationToken.None, cantidadVerificada: cantidad);
                if (!Informar(resultado)) return;
                MostrarEstado($"Verificación del depósito {fila.Id} guardada. Actualizando la consulta…", false);
                if (_filtroConsultado is not null)
                {
                    var actualizados = await PedirPaginaAsync(_filtroConsultado, _pagina, CancellationToken.None);
                    if (!Informar(actualizados)) return;
                    MostrarDepositos(actualizados.Valor);
                    ConsultaCompletada($"Depósito {fila.Id} verificado. No se acreditaron ecopuntos.");
                }
            }, permiteCancelar: false);
        }

        private void MostrarDepositos(PaginaDepositos pagina)
        {
            _pagina = pagina.Pagina;
            _totalPaginas = pagina.TotalPaginas;
            tablaDepositos.DataSource = pagina.Filas.Select(f => new VistaDeposito(f.IdDeposito,
                Fecha(f.FechaDeteccionUtc), f.Contenedor, f.IdUsuario?.ToString(Cultura) ?? "—",
                f.MaterialDeclarado, f.MaterialVerificado ?? "Sin verificar", f.MaterialVerificado is not null,
                f.PesoMedidoKg?.ToString("0.000", Cultura) ?? "—",
                f.PesoEstimadoKg?.ToString("0.000", Cultura) ?? "—")).ToList();
            Encabezados(tablaDepositos, ("Id", "Depósito"), ("Fecha", "Detectado (Colombia)"), ("Usuario", "Id usuario"),
                ("Declarado", "Material declarado"), ("Verificado", "Material verificado"),
                ("KgMedidos", "Kg medidos"), ("KgEstimados", "Kg estimados, no medidos"));
            etiquetaPagina.Text = pagina.Total == 0 ? "Sin depósitos para este filtro" : $"Página {_pagina} de {_totalPaginas} · {pagina.Total} depósitos · {TamanoPagina} por página";
        }

        private void MostrarSolicitudes(IEnumerable<Interaccion> solicitudes, int? caneca)
        {
            var filas = solicitudes.OrderByDescending(i => i.FechaInicioUtc).ThenByDescending(i => i.Id).ToList();
            tablaInteracciones.DataSource = filas.Take(MaximoFilasAuxiliares).Select(i => new VistaInteraccion(i.Id,
                Fecha(i.FechaInicioUtc), i.IdUsuario?.ToString(Cultura) ?? "—", i.Canal.ToString(), i.Estado.ToString(),
                i.IdMaterialConfirmado?.ToString(Cultura) ?? "—", i.FechaCierreUtc is { } cierre ? Fecha(cierre) : "—")).ToList();
            Encabezados(tablaInteracciones, ("Id", "Interacción"), ("Inicio", "Inicio (Colombia)"), ("Usuario", "Id usuario"),
                ("Material", "Id material confirmado"), ("Cierre", "Cierre (Colombia)"));
            etiquetaInteracciones.Text = caneca is null ? "Seleccione una caneca y consulte para ver sus solicitudes." :
                $"{filas.Count} solicitudes en el período; se muestran las {MaximoFilasAuxiliares} más recientes como máximo. Una solicitud no prueba un depósito.";
        }

        private void MostrarIncidencias(IEnumerable<EventoDispositivo> eventos)
        {
            var filas = eventos.OrderByDescending(e => e.FechaRecepcionUtc).ThenByDescending(e => e.Id).ToList();
            tablaIncidencias.DataSource = filas.Take(MaximoFilasAuxiliares).Select(e => new VistaIncidencia(e.Id, e.IdContenedor,
                e.IdOrden?.ToString(Cultura) ?? "—", e.Tipo.ToString(), Fecha(e.FechaRecepcionUtc),
                e.FechaDispositivoUtc is { } fecha ? Fecha(fecha) : "Sin fecha", e.IdExterno)).ToList();
            Encabezados(tablaIncidencias, ("Id", "Evento en revisión"), ("Caneca", "Id caneca"), ("Orden", "Id orden"),
                ("Recibido", "Recepción (Colombia)"), ("Dispositivo", "Fecha informada (Colombia)"), ("Externo", "Identificador externo"));
            etiquetaIncidencias.Text = $"{filas.Count} eventos en revisión en el período; máximo {MaximoFilasAuxiliares} visibles. Consulta de auditoría: no resuelve ni modifica eventos.";
        }

        private void MostrarCaneca(int? id)
        {
            etiquetaCaneca.Text = id is { } valor && _canecas.TryGetValue(valor, out var c)
                ? $"Estado configurado de {c.Nombre}: {c.Estado}. Conexión física y estado del sensor no comprobados."
                : "Seleccione una caneca para consultar su estado configurado. Conexión física y estado del sensor no comprobados.";
        }

        private async Task EjecutarAsync(Func<CancellationToken, Task> accion, bool permiteCancelar = true)
        {
            if (_ocupado) return;
            _ocupado = true;
            _operacion = new CancellationTokenSource();
            UseWaitCursor = true;
            panelFiltros.Enabled = false;
            pestanas.Enabled = false;
            botonCancelar.Enabled = permiteCancelar;
            MostrarEstado(permiteCancelar ? "Consultando… Puede cancelar la consulta." : "Guardando verificación… Espere la confirmación de Oracle.", false);
            try { await accion(_operacion.Token); }
            catch (OperationCanceledException) when (_operacion.IsCancellationRequested)
            {
                MostrarEstado("Consulta cancelada. Los datos anteriores conservan su fecha de consulta.", true);
            }
            catch (Exception)
            {
                if (!permiteCancelar) LimpiarDatos();
                MostrarEstado(permiteCancelar ? "No se pudo completar la consulta. Revise la conexión con Oracle e inténtelo de nuevo." :
                    "No se pudo confirmar el resultado de la verificación. Consulte de nuevo antes de reintentar; la operación pudo guardarse.", true);
            }
            finally
            {
                _operacion.Dispose();
                _operacion = null;
                _ocupado = false;
                panelFiltros.Enabled = true;
                pestanas.Enabled = true;
                botonCancelar.Enabled = false;
                UseWaitCursor = false;
                ActualizarAcciones();
            }
        }

        private bool Informar(Resultado resultado)
        {
            if (resultado.Exitoso) return true;
            LimpiarDatos();
            MostrarEstado(resultado.Mensaje ?? "La operación fue rechazada por el servicio.", true);
            return false;
        }
        private void LimpiarDatos()
        {
            tablaDepositos.DataSource = null;
            tablaInteracciones.DataSource = null;
            tablaIncidencias.DataSource = null;
            _filtroConsultado = null;
            _totalPaginas = 0;
            etiquetaPagina.Text = "Consulte de nuevo para actualizar los datos";
            etiquetaConsulta.Text = "No hay una consulta vigente en pantalla.";
        }
        private void ActualizarAcciones()
        {
            botonConsultar.Enabled = !_ocupado;
            botonAnterior.Enabled = !_ocupado && _pagina > 1 && _filtroConsultado is not null;
            botonSiguiente.Enabled = !_ocupado && _pagina < _totalPaginas && _filtroConsultado is not null;
            botonVerificar.Enabled = !_ocupado && comboMaterial.Items.Count > 0
                && tablaDepositos.CurrentRow?.DataBoundItem is VistaDeposito { VerificacionRegistrada: false };
        }
        private void Formulario_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!_ocupado) return;
            e.Cancel = true;
            MessageBox.Show(this, "Espere que finalice la operación. Puede cancelar una consulta con el botón «Cancelar consulta».",
                "Operación en curso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void ConsultaCompletada(string mensaje = "Consulta realizada.")
        {
            etiquetaUltimaConsulta.Text = $"Última consulta Oracle realizada: {Fecha(DateTime.UtcNow)} (Colombia).";
            MostrarEstado(mensaje + " Esto no comprueba la conexión física del dispositivo.", false);
        }
        private void MostrarEstado(string mensaje, bool error)
        {
            etiquetaEstado.ForeColor = error ? TemaEcoAccess.Error : TemaEcoAccess.VerdeOscuro;
            etiquetaEstado.Text = mensaje;
        }
        private static string Fecha(DateTime utc) => HoraColombia.DesdeUtc(utc).ToString("yyyy-MM-dd HH:mm:ss", Cultura);
        private static void Encabezados(DataGridView tabla, params (string Nombre, string Texto)[] columnas)
        {
            foreach (var (nombre, texto) in columnas)
                if (tabla.Columns[nombre] is { } columna) columna.HeaderText = texto;
        }
        private sealed record Opcion<T>(T Valor, string Texto);
        private sealed record VistaDeposito(int Id, string Fecha, string Caneca, string Usuario, string Declarado, string Verificado, [property: System.ComponentModel.Browsable(false)] bool VerificacionRegistrada, string KgMedidos, string KgEstimados);
        private sealed record VistaInteraccion(int Id, string Inicio, string Usuario, string Canal, string Estado, string Material, string Cierre);
        private sealed record VistaIncidencia(int Id, int Caneca, string Orden, string Tipo, string Recibido, string Dispositivo, string Externo);
    }
}
