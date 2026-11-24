using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Consulta de solo lectura del área Operario: ciudadanos (búsqueda autorizada por <see cref="ServicioUsuarios.BuscarCiudadanosAsync"/>),
    /// las canecas con su estado y el catálogo de materiales. No crea, edita ni deshabilita nada: eso es del Supervisor.
    /// </summary>
    public sealed partial class FormConsultaOperario : Form
    {
        private readonly ServicioUsuarios _usuarios;
        private readonly ServicioContenedores _contenedores;
        private readonly ServicioMateriales _materiales;
        private readonly int _idActor;
        private readonly TextBox _buscar = new() { Dock = DockStyle.Fill, MaxLength = 120, AccessibleName = "Buscar ciudadano por nombre o correo" };
        private readonly DataGridView _ciudadanos = Tabla("Ciudadanos encontrados");
        private readonly DataGridView _canecas = Tabla("Canecas registradas");
        private readonly DataGridView _catalogo = Tabla("Materiales habilitados");
        private readonly Label _estado = new() { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
        private bool _ocupado;

        public FormConsultaOperario(ServicioUsuarios usuarios, ServicioContenedores contenedores, ServicioMateriales materiales, int idActor)
        {
            _usuarios = usuarios ?? throw new ArgumentNullException(nameof(usuarios));
            _contenedores = contenedores ?? throw new ArgumentNullException(nameof(contenedores));
            _materiales = materiales ?? throw new ArgumentNullException(nameof(materiales));
            _idActor = idActor;
            Text = "Consulta del operario";
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(640, 420);
            Construir(); // Disposición Eco: FormConsultaOperario.Diseno.cs.
            Load += async (_, _) => await EjecutarAsync(CargarTodoAsync);
            FormClosing += (_, e) => e.Cancel = _ocupado;
        }

        private static DataGridView Tabla(string nombre)
        {
            var t = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AccessibleName = nombre,
            };
            TemaEcoAccess.EstilizarTabla(t);
            return t;
        }

        private async Task CargarTodoAsync()
        {
            await BuscarCiudadanosAsync();
            IReadOnlyList<Contenedor> canecas = await _contenedores.ListarAsync();
            _canecas.DataSource = canecas.OrderBy(c => c.Id).Select(c => new FilaCaneca(c.Id, c.Nombre, TextoEstado(c.Estado), c.MaterialesAdmitidos.Count)).ToList();
            Encabezados(_canecas, ("Id", "Código"), ("Nombre", "Caneca"), ("Estado", "Estado"), ("Materiales", "Materiales admitidos"));
            IReadOnlyList<Material> materiales = await _materiales.ListarAsync(soloHabilitados: true);
            _catalogo.DataSource = materiales.OrderBy(m => m.Nombre).Select(m => new FilaMaterial(m.Codigo, m.Nombre, TextosCategoria.Texto(m.Categoria))).ToList();
            Encabezados(_catalogo, ("Codigo", "Código"), ("Nombre", "Material"), ("Categoria", "Categoría"));
        }

        private async Task BuscarCiudadanosAsync()
        {
            Resultado<PaginaUsuarios> r = await _usuarios.BuscarCiudadanosAsync(_idActor, _buscar.Text);
            if (!r.Exitoso) { Mostrar(r.Mensaje ?? "No tienes permiso para consultar ciudadanos.", true); return; }
            _ciudadanos.DataSource = r.Valor.Filas.Select(u => new FilaCiudadano(u.Id, u.Nombre, u.CorreoElectronico, u.Activo ? "Activa" : "Inactiva",
                HoraColombia.DesdeUtc(u.FechaRegistroUtc).ToString("yyyy-MM-dd"))).ToList();
            Encabezados(_ciudadanos, ("Id", "Código"), ("Nombre", "Nombre"), ("Correo", "Correo"), ("Estado", "Cuenta"), ("Registro", "Registro"));
            Mostrar($"{r.Valor.Filas.Count} ciudadanos{(r.Valor.HaySiguiente ? " (hay más: precisa la búsqueda)" : "")}.", false);
        }

        private static void Encabezados(DataGridView tabla, params (string Columna, string Texto)[] nombres)
        {
            foreach (var (columna, texto) in nombres) if (tabla.Columns[columna] is { } c) c.HeaderText = texto;
        }

        private static string TextoEstado(EstadoContenedor estado) => estado switch
        {
            EstadoContenedor.EnServicio => "En servicio",
            EstadoContenedor.FueraDeServicio => "Fuera de servicio",
            EstadoContenedor.DadoDeBaja => "Dada de baja",
            _ => "Desconocido",
        };

        private async Task EjecutarAsync(Func<Task> accion)
        {
            if (_ocupado) return;
            _ocupado = true;
            UseWaitCursor = true;
            try { await accion(); }
            catch (Exception) { Mostrar("No se pudo consultar Oracle. Revisa la conexión e inténtalo de nuevo.", true); }
            finally { _ocupado = false; UseWaitCursor = false; }
        }

        private void Mostrar(string texto, bool error)
        {
            _estado.ForeColor = error ? TemaEcoAccess.Error : TemaEcoAccess.VerdeOscuro;
            _estado.Text = texto;
        }

        private sealed record FilaCiudadano(int Id, string Nombre, string Correo, string Estado, string Registro);
        private sealed record FilaCaneca(int Id, string Nombre, string Estado, int Materiales);
        private sealed record FilaMaterial(string Codigo, string Nombre, string Categoria);
    }
}
