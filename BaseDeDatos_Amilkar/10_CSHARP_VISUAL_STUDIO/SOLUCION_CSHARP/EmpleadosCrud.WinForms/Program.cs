using EcoAccessBD.Datos;
using Oracle.ManagedDataAccess.Client;

namespace EmpleadosCrud.WinForms;

/// <summary>
/// Ventana de la tarea «CRUD de empleado SIN DELETE»: Insertar, Consultar y Actualizar.
/// No hay botón Eliminar a propósito. La conexión sale de las variables de entorno
/// PRACTICAS_USUARIO / PRACTICAS_CLAVE (ver la guía de C#); nunca del código.
///
/// Modo demostración (evidencia para el informe):
///   EmpleadosCrud.WinForms.exe --demo C:\carpeta\de\capturas
/// abre la ventana fuera de la pantalla y sin quitar el foco, pulsa los botones con PerformClick()
/// (el mismo código que un clic real) y guarda una imagen de la ventana después de cada paso.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length >= 2 && args[0] == "--demo") return FormEmpleados.Demostrar(args[1]);
        Application.Run(new FormEmpleados());
        return 0;
    }
}

internal sealed class FormEmpleados : Form
{
    private readonly DataGridView _tabla = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
    private readonly TextBox _id = new() { ReadOnly = true, Width = 70 };
    private readonly TextBox _nombre = new() { Width = 220 };
    private readonly TextBox _email = new() { Width = 120 };
    private readonly NumericUpDown _salario = new() { Maximum = 1_000_000, DecimalPlaces = 2, Width = 100 };
    private readonly NumericUpDown _depto = new() { Maximum = 1000, Value = 60, Width = 70 };
    private readonly Label _estado = new() { Dock = DockStyle.Bottom, Height = 28, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0) };
    private readonly Button _bConsultar = new() { Text = "Consultar", AutoSize = true };
    private readonly Button _bInsertar = new() { Text = "Insertar", AutoSize = true };
    private readonly Button _bActualizar = new() { Text = "Actualizar", AutoSize = true };
    private RepositorioEmpleados? _repo;
    private bool _cargando;
    private readonly bool _sinActivar;

    public FormEmpleados(bool sinActivar = false)
    {
        _sinActivar = sinActivar;
        Text = "EcoAccess BD · Práctica CRUD de empleado (sin DELETE)";
        Width = 1000; Height = 600; Font = new Font("Segoe UI", 10);
        var barra = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(6) };
        void Campo(string t, Control c) { barra.Controls.Add(new Label { Text = t, AutoSize = true, Padding = new Padding(0, 6, 0, 0) }); barra.Controls.Add(c); }
        Campo("Id", _id); Campo("Nombre", _nombre); Campo("Email", _email); Campo("Salario", _salario); Campo("Depto", _depto);
        barra.Controls.AddRange([_bConsultar, _bInsertar, _bActualizar]);
        Controls.Add(_tabla); Controls.Add(barra); Controls.Add(_estado);

        _bConsultar.Click += (_, _) => Ejecutar(() => { Recargar(null); return $"{_tabla.Rows.Count} empleados del departamento {_depto.Value}."; });
        _bInsertar.Click += (_, _) => Ejecutar(() =>
        {
            int id = Repo().Insertar(_nombre.Text, _email.Text, _salario.Value, (int)_depto.Value);
            Recargar(id);
            return $"Insertado con id {id} (COMMIT).";
        });
        _bActualizar.Click += (_, _) => Ejecutar(() =>
        {
            int id = int.Parse(_id.Text);
            Repo().Actualizar(id, _nombre.Text, _salario.Value, (int)_depto.Value);
            Recargar(id);
            return $"Empleado {id} actualizado (COMMIT).";
        });
        _tabla.SelectionChanged += (_, _) =>
        {
            if (_cargando) return;   // al recargar la tabla no se pisa el empleado que se está editando
            if (_tabla.CurrentRow?.DataBoundItem is System.Data.DataRowView f)
            {
                _id.Text = f["ID_EMPLEADO"].ToString(); _nombre.Text = f["NOMBRE"].ToString(); _email.Text = f["EMAIL"].ToString();
                _salario.Value = Convert.ToDecimal(f["SALARIO"]);
            }
        };
    }

    protected override bool ShowWithoutActivation => _sinActivar;

    /// <summary>Recarga la tabla y, si se indica, deja seleccionado ese empleado (corrige que la recarga
    /// cambiara el Id del formulario por el de la primera fila).</summary>
    private void Recargar(int? seleccionar)
    {
        _cargando = true;
        try
        {
            _tabla.DataSource = Repo().Consultar((int)_depto.Value);
            if (seleccionar is int id)
            {
                foreach (DataGridViewRow fila in _tabla.Rows)
                    if (fila.DataBoundItem is System.Data.DataRowView f && Convert.ToInt32(f["ID_EMPLEADO"]) == id)
                    { _tabla.CurrentCell = fila.Cells[0]; fila.Selected = true; break; }
            }
        }
        finally { _cargando = false; }
        if (seleccionar is int id2) _id.Text = id2.ToString();
    }

    private RepositorioEmpleados Repo() => _repo ??= new RepositorioEmpleados(Conexion.Crear("PRACTICAS"));

    private void Ejecutar(Func<string> accion)
    {
        try { _estado.Text = accion(); _estado.ForeColor = Color.DarkGreen; }
        catch (OracleException ex) { _estado.Text = $"Oracle ORA-{ex.Number:00000}: {ex.Message.Split('\n')[0]}"; _estado.ForeColor = Color.DarkRed; }
        catch (Exception ex) { _estado.Text = ex.Message; _estado.ForeColor = Color.DarkRed; }
    }

    /// <summary>Recorre los botones como lo haría una persona y guarda una imagen tras cada paso.</summary>
    internal static int Demostrar(string carpeta)
    {
        Directory.CreateDirectory(carpeta);
        var registro = new List<string>();
        using var f = new FormEmpleados(sinActivar: true) { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false };
        f.Show();
        Application.DoEvents();
        void Paso(string archivo, string descripcion, Action accion)
        {
            accion();
            Application.DoEvents();
            using var bmp = new Bitmap(f.Width, f.Height);
            f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
            bmp.Save(Path.Combine(carpeta, archivo), System.Drawing.Imaging.ImageFormat.Png);
            registro.Add($"{archivo} | {descripcion} | estado: {f._estado.Text} | color: {(f._estado.ForeColor == Color.DarkRed ? "ROJO (error)" : "VERDE (correcto)")}");
        }
        string sello = DateTime.Now.ToString("HHmmss");
        Paso("01_consultar.png", "Consultar el departamento 60 (SYS_REFCURSOR)", () => { f._depto.Value = 60; f._bConsultar.PerformClick(); });
        Paso("02_insertar.png", "Insertar un empleado nuevo (secuencia + COMMIT)", () =>
        {
            f._nombre.Text = "Empleado demo ventana " + sello; f._email.Text = "DEMO" + sello; f._salario.Value = 3200; f._bInsertar.PerformClick();
        });
        string idNuevo = f._id.Text;
        Paso("03_actualizar.png", $"Actualizar el salario del empleado {idNuevo} a 3500", () => { f._salario.Value = 3500; f._bActualizar.PerformClick(); });
        Paso("04_error_oracle.png", "Actualizar un id que no existe (debe mostrar ORA-20002 en rojo)", () => { f._id.Text = "999999"; f._bActualizar.PerformClick(); });
        bool sinEliminar = !f.Controls.OfType<FlowLayoutPanel>().SelectMany(p => p.Controls.OfType<Button>()).Any(b => b.Text.Contains("Elimin", StringComparison.OrdinalIgnoreCase));
        registro.Add($"La ventana no tiene botón Eliminar: {sinEliminar}");
        f.Close();
        File.WriteAllLines(Path.Combine(carpeta, "registro_demo.txt"), registro);
        foreach (var l in registro) Console.WriteLine(l);
        return registro.Take(3).All(l => l.Contains("VERDE")) && registro[3].Contains("ORA-20002") && sinEliminar ? 0 : 1;
    }
}
