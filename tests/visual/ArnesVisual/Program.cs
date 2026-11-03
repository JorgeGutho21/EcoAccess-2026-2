using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Presentacion;
using Oracle.ManagedDataAccess.Client;

// Abre cada modulo real contra una CLON temporal de la base y registra excepciones, dialogos y textos de error.
static class Arnes
{
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumThreadWindows(uint id, EnumProc p, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr h, EnumProc p, IntPtr l);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder b, int n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder b, int n);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);

    static readonly List<string> Eventos = new List<string>();
    static uint _hilo;

    static void VigilarDialogos(object? _)
    {
        EnumThreadWindows(_hilo, (h, l) =>
        {
            var c = new StringBuilder(64); GetClassName(h, c, 64);
            if (c.ToString() != "#32770") return true;
            var texto = new StringBuilder();
            EnumChildWindows(h, (hh, ll) => { var t = new StringBuilder(600); GetWindowText(hh, t, 600); if (t.Length > 0) texto.Append(t).Append(" | "); return true; }, IntPtr.Zero);
            lock (Eventos) Eventos.Add("DIALOGO: " + texto);
            PostMessage(h, 0x0010, IntPtr.Zero, IntPtr.Zero);
            return true;
        }, IntPtr.Zero);
    }

    static IEnumerable<Control> Todos(Control c) { foreach (Control h in c.Controls) { yield return h; foreach (var n in Todos(h)) yield return n; } }

    // DoEvents retira el contexto de Windows Forms; con Application.Run, como en la aplicacion real, permanece.
    static SynchronizationContext? _contexto;
    static void Bombear() { Application.DoEvents(); SynchronizationContext.SetSynchronizationContext(_contexto); }
    static string _ultimaMedida = "";
    static string _salidaGlobal = "";

    // Desplaza como una rueda de raton y mide cuanto tarda en repintarse cada paso.
    static string MedirScroll(List<Control> controles)
    {
        var panel = controles.OfType<ScrollableControl>().Where(c => c.AutoScroll && c.VerticalScroll.Visible && c.Visible).OrderByDescending(c => c.VerticalScroll.Maximum).FirstOrDefault();
        if (panel is null) return "scroll=no aplica";
        var tiempos = new List<double>();
        int maximo = panel.VerticalScroll.Maximum - panel.ClientSize.Height;
        int paso = 120;
        for (int vuelta = 0; vuelta < 2; vuelta++)
            foreach (int y in Enumerable.Range(0, Math.Min(40, maximo / paso + 1)).Select(i => i * paso).Concat(Enumerable.Range(0, Math.Min(40, maximo / paso + 1)).Select(i => Math.Max(0, maximo - i * paso))))
            {
                var c = System.Diagnostics.Stopwatch.StartNew();
                panel.AutoScrollPosition = new Point(0, y);
                panel.Update();
                Bombear();
                tiempos.Add(c.Elapsed.TotalMilliseconds);
            }
        panel.AutoScrollPosition = Point.Empty;
        tiempos.Sort();
        return $"scroll pasos={tiempos.Count} media={tiempos.Average():0.0}ms p95={tiempos[(int)(tiempos.Count * 0.95)]:0.0}ms max={tiempos.Last():0.0}ms alto={panel.VerticalScroll.Maximum}px dpi={panel.DeviceDpi}";
    }

    static object? Campo(object o, string nombre) => o.GetType().GetField(nombre, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(o);
    static void Esperar(int ms) { var fin = DateTime.UtcNow.AddMilliseconds(ms); while (DateTime.UtcNow < fin) { Bombear(); Thread.Sleep(20); } }
    static void Tecla(Control c, Keys k) => typeof(Control).GetMethod("OnKeyDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(c, new object[] { new KeyEventArgs(k) });
    static string TextoTarjeta(object o) => (string)o.GetType().GetField("_valor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(o)!;

    static void Interactuar(string nombre, Form f, List<string> r)
    {
        if (nombre == "registro_manual")
        {
            var caja = (Control)Campo(f, "_buscarResiduo")!; var lista = (ListBox)Campo(f, "_residuos")!; var estado = (Label)Campo(f, "_estadoBusqueda")!; var orient = (Label)Campo(f, "_orientacion")!; var abrir = (Control)Campo(f, "_abrir")!;
            int total = lista.Items.Count;
            foreach (string q in new[] { "botella", "carton", "cartón", "pilas", "plastico", "vidrio", "lata", "papel", "xyzqw" })
            {
                caja.Text = q; var c = System.Diagnostics.Stopwatch.StartNew(); Tecla(caja, Keys.Enter); Esperar(900);
                r.Add($"    enter «{q}»: {lista.Items.Count} resultados · {estado.Text}");
            }
            caja.Text = "botella"; Tecla(caja, Keys.Enter); Esperar(800);
            Tecla(caja, Keys.Down); Esperar(200);
            r.Add($"    flecha abajo: seleccion={lista.SelectedIndex} · abrir={abrir.Enabled} · {orient.Text}");
            r.Add($"    sin busqueda habia {total} materiales");
        }
        if (nombre == "dashboard_institucional")
        {
            string Kpi() => string.Join(",", new[] { "_solicitudes", "_depositos", "_verificados", "_acreditados", "_netos" }.Select(n => TextoTarjeta(Campo(f, n)!)));
            var alcance = (ComboBox)Campo(f, "_alcance")!; var consultar = (Control)Campo(f, "_consultar")!; var limpiar = (Control)Campo(f, "_limpiar")!;
            var material = (ComboBox)Campo(f, "_material")!; var categoria = (ComboBox)Campo(f, "_categoria")!; var caneca = (ComboBox)Campo(f, "_caneca")!; var grid = (DataGridView)Campo(f, "_comparacion")!;
            void Clic(Control b) { typeof(Control).GetMethod("OnClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(b, new object[] { EventArgs.Empty }); var c = System.Diagnostics.Stopwatch.StartNew(); do { Esperar(50); } while ((bool)Campo(f, "_ocupado")! && c.ElapsedMilliseconds < 60000); r.Add($"    consulta {c.ElapsedMilliseconds} ms"); File.AppendAllLines(Path.Combine(Path.GetDirectoryName(Path.Combine(_salidaGlobal, "x"))!, "avance.txt"), new[] { DateTime.Now.ToString("HH:mm:ss") + " consulta " + c.ElapsedMilliseconds + " " + Kpi() }); }
            { var c0 = System.Diagnostics.Stopwatch.StartNew(); do { Esperar(50); } while ((bool)Campo(f, "_ocupado")! && c0.ElapsedMilliseconds < 60000); }
            r.Add($"    operativos: {Kpi()} · filtros caneca={caneca.Items.Count} material={material.Items.Count}");
            alcance.SelectedIndex = 1; Clic(consultar); r.Add($"    demostracion: {Kpi()} · comparacion filas={grid.RowCount} primera={(grid.RowCount > 0 ? string.Join("|", grid.Rows[1].Cells.Cast<DataGridViewCell>().Select(c => c.FormattedValue)) : "")}");
            for (int i = 1; i < material.Items.Count; i++) if (material.Items[i].ToString()!.Contains("PET")) { material.SelectedIndex = i; break; }
            Clic(consultar); r.Add($"    demostracion material={material.Text}: {Kpi()}");
            material.SelectedIndex = 0;
            for (int i = 1; i < material.Items.Count; i++) if (material.Items[i].ToString()!.StartsWith("Vidrio")) { material.SelectedIndex = i; break; }
            Clic(consultar); r.Add($"    demostracion material={material.Text}: {Kpi()}");
            material.SelectedIndex = 0; categoria.SelectedIndex = 1; Clic(consultar); r.Add($"    demostracion categoria={categoria.Text}: {Kpi()}");
            categoria.SelectedIndex = 3; Clic(consultar); r.Add($"    demostracion categoria={categoria.Text}: {Kpi()}");
            categoria.SelectedIndex = 0; if (caneca.Items.Count > 1) { for (int i = 1; i < caneca.Items.Count; i++) if (caneca.Items[i].ToString()!.Contains("DEMO")) caneca.SelectedIndex = i; Clic(consultar); r.Add($"    demostracion caneca={caneca.Text}: {Kpi()}"); }
            alcance.SelectedIndex = 1; caneca.SelectedIndex = 0; Clic(consultar);
            Clic(limpiar); r.Add($"    limpiar: alcance={alcance.Text} caneca={caneca.Text} material={material.Text}: {Kpi()}");
            alcance.SelectedIndex = 1; Clic(consultar);
        }
        if (nombre == "explorar_invitado" || nombre == "explorar_ciudadano")
        {
            var busq = Campo(f, "_busqueda")!; var caja = (Control)busq.GetType().GetProperty("Campo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!.GetValue(busq)!; var estado = (Label)Campo(f, "_estadoBusqueda")!; var prox = (Control)Campo(f, "_proxima")!; var ant = (Control)Campo(f, "_anterior")!;
            r.Add($"    inicial: {estado.Text} · anterior={ant.Enabled} siguiente={prox.Enabled}");
            void Clic(Control b) { typeof(Control).GetMethod("OnClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(b, new object[] { EventArgs.Empty }); Esperar(1200); }
            caja!.Text = "botella"; Tecla(caja, Keys.Enter); Esperar(1200);
            r.Add($"    enter botella: {estado.Text} · anterior={ant.Enabled} siguiente={prox.Enabled}");
            caja.Text = ""; Tecla(caja, Keys.Enter); Esperar(500); r.Add($"    enter vacio: {estado.Text}");
            typeof(FormExplorarEcoAccess).GetMethod("ListarCatalogoAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(f, null); Esperar(1200);
            r.Add($"    catalogo: {estado.Text} · anterior={ant.Enabled} siguiente={prox.Enabled}");
            if (prox.Enabled) { Clic(prox); r.Add($"    siguiente: {estado.Text} · anterior={ant.Enabled} siguiente={prox.Enabled}"); Clic(ant); r.Add($"    anterior: {estado.Text} · anterior={ant.Enabled} siguiente={prox.Enabled}"); }
            if (nombre == "explorar_ciudadano")
            {
                var act = (Control)Campo(f, "_actualizarEstado")!; var lab = (Label)Campo(f, "_estadoActualizacion")!;
                Clic(act); r.Add($"    actualizar mi estado: {lab.Text} · saldo={TextoTarjeta(Campo(f, "_saldo")!)} ultima={TextoTarjeta(Campo(f, "_ultima")!)}");
            }
        }
        if (nombre == "principal_admin")
        {
            foreach (Control b in Todos(f).Where(c => c.GetType().Name == "BotonNavegacion" && c.Visible).ToList())
            {
                if (new[] { "Cerrar sesión", "Perfil y contraseña", "Contraer menú", "Análisis en Power BI" }.Contains(b.Text)) continue;
                var c = System.Diagnostics.Stopwatch.StartNew();
                typeof(Control).GetMethod("OnClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(b, new object[] { EventArgs.Empty });
                Esperar(1800);
                r.Add($"    navega «{b.Text}»: titulo={((Label)Campo(f, "_tituloModulo")!).Text}");
            }
            typeof(Control).GetMethod("OnClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(Todos(f).First(c => c.GetType().Name == "BotonNavegacion" && c.Text == "Dashboard"), new object[] { EventArgs.Empty });
            Esperar(2000);
        }
    }

    [STAThread]
    static void Main(string[] args)
    {
        string salida = args[0];
        string cadena = Environment.GetEnvironmentVariable("ECOARNES_CADENA") ?? throw new InvalidOperationException("sin cadena");
        if (!cadena.Contains("1523")) throw new InvalidOperationException("Solo se ejecuta contra la clon temporal en el puerto 1523.");
        Directory.CreateDirectory(salida); _salidaGlobal = salida;
        _hilo = GetCurrentThreadId();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => { lock (Eventos) Eventos.Add("EXCEPCION UI: " + e.Exception.ToString().Replace("\r\n", " | ")); };
        AppDomain.CurrentDomain.UnhandledException += (_, e) => { lock (Eventos) Eventos.Add("EXCEPCION: " + e.ExceptionObject); };
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) => { lock (Eventos) Eventos.Add("TAREA: " + e.Exception.GetBaseException().GetType().Name + ": " + e.Exception.GetBaseException().Message); e.SetObserved(); };
        string diario = Path.Combine(salida, "primera-oportunidad.txt");
        AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
        {
            if (e.Exception is OperationCanceledException or System.Threading.Tasks.TaskCanceledException) return;
            try { File.AppendAllText(diario, DateTime.Now.ToString("HH:mm:ss.fff") + " " + e.Exception.GetType().Name + ": " + e.Exception.Message + " || " + string.Join(" | ", new System.Diagnostics.StackTrace(1).GetFrames().Take(14).Select(x => x.GetMethod()?.DeclaringType?.Name + "." + x.GetMethod()?.Name)) + "\n"); } catch (Exception) { }
        };
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); _contexto = new WindowsFormsSynchronizationContext(); SynchronizationContext.SetSynchronizationContext(_contexto);

        var fabrica = new FabricaConexionesOracle(cadena);
        var usuarios = new RepositorioUsuariosOracle(fabrica);
        var materiales = new RepositorioMaterialesOracle(fabrica);
        var puntos = new RepositorioPuntosInstalacionOracle(fabrica);
        var contenedores = new RepositorioContenedoresOracle(fabrica);
        var s = new ServiciosAplicacion(
            new ServicioMateriales(usuarios, materiales, contenedores),
            new ServicioAutenticacion(new RepositorioCredencialesOracle(fabrica), usuarios, new HasherPbkdf2(), TimeProvider.System),
            new ServicioPuntosInstalacion(usuarios, puntos, contenedores),
            new ServicioContenedores(usuarios, contenedores, puntos, materiales, new GeneradorIdentificadorQrAleatorio(), TimeProvider.System),
            new ServicioUsuarios(usuarios, TimeProvider.System),
            new ServicioInteracciones(usuarios, new RepositorioInteraccionesOracle(fabrica), contenedores, materiales, TimeProvider.System),
            new ServicioEstadisticas(usuarios, new RepositorioEstadisticasOracle(fabrica)),
            "EcoAccessP3_JG_bot",
            new ServicioEcopuntos(usuarios, new RepositorioInteraccionesOracle(fabrica), materiales, new RepositorioEcopuntosOracle(fabrica), TimeProvider.System),
            new ServicioDiagnosticoOracle(usuarios, new RepositorioDiagnosticoOracle(fabrica), TimeProvider.System),
            new ServicioPanelAdministrativo(usuarios, new RepositorioPanelAdministrativoOracle(fabrica)),
            new ServicioRegistroCiudadanos(new RepositorioRegistroCiudadanosOracle(fabrica), new HasherPbkdf2(), TimeProvider.System),
            new ServicioCatalogoAmbiental(materiales, contenedores),
            new ServicioOpiniones(usuarios, new RepositorioOpinionesOracle(fabrica), TimeProvider.System),
            new ServicioInformeInstitucional(usuarios, new RepositorioInformeInstitucionalOracle(fabrica)));

        int admin, ciudadano;
        using (var c = new OracleConnection(cadena))
        {
            c.Open();
            int Uno(string sql) { using var q = c.CreateCommand(); q.CommandText = sql; return Convert.ToInt32(q.ExecuteScalar()); }
            admin = Uno("SELECT MIN(ur.id_usuario) FROM usuario_rol ur JOIN rol r ON r.id_rol=ur.id_rol WHERE r.codigo=" + (int)CodigoRol.Administrador);
            ciudadano = Uno("SELECT MIN(id_usuario) FROM interaccion WHERE id_usuario IS NOT NULL AND canal=5");
        }
        var sesionAdmin = new SesionActiva(admin, "Administrador de prueba", new[] { CodigoRol.Administrador }, DateTime.UtcNow);
        var sesionCiudadano = new SesionActiva(ciudadano, "Ciudadano de prueba", new[] { CodigoRol.Ciudadano }, DateTime.UtcNow);

        var modulos = new List<(string, Func<Form>)>
        {
            ("principal_admin", () => new FormPrincipal(s, sesionAdmin)),
            ("dashboard_institucional", () => new FormDashboardInstitucional(s.InformeInstitucional!, admin, s.Contenedores, s.Puntos, s.Materiales, () => { })),
            ("estadisticas", () => new FormEstadisticas(s.Estadisticas, s.Contenedores, s.Materiales, admin)),
            ("estadisticas_personales", () => new FormEstadisticasPersonales(s.Estadisticas, ciudadano)),
            ("depositos_interacciones", () => new FormInteraccionesDepositos(s.Interacciones, s.Estadisticas, s.Contenedores, s.Materiales, admin)),
            ("ecopuntos_admin", () => new FormEcopuntos(s.Ecopuntos, s.Materiales, admin, true)),
            ("ecopuntos_ciudadano", () => new FormEcopuntos(s.Ecopuntos, s.Materiales, ciudadano, false, true, s.Estadisticas)),
            ("diagnostico", () => new FormDiagnosticoOracle(s.DiagnosticoOracle, admin)),
            ("materiales", () => new FormMateriales(s.Materiales, admin)),
            ("puntos", () => new FormPuntos(s.Puntos, admin)),
            ("canecas", () => new FormContenedores(s.Contenedores, s.Puntos, s.Materiales, admin, s.NombreBotTelegram)),
            ("usuarios", () => new FormUsuarios(s.Usuarios, s.Autenticacion, admin)),
            ("opiniones_admin", () => new FormOpiniones(s.Opiniones!, admin, administracion: true)),
            ("registro_manual", () => new FormRegistroManual(s, admin)),
            ("proximamente", () => new FormProximamente()),
            ("mis_ecopuntos", () => new FormMisEcopuntos(s.Ecopuntos, s.Estadisticas, ciudadano)),
            ("explorar_invitado", () => new FormExplorarEcoAccess(s, null)),
            ("explorar_ciudadano", () => new FormExplorarEcoAccess(s, sesionCiudadano)),
            ("principal_ciudadano", () => new FormPrincipal(s, sesionCiudadano)),
        };

        var error = new Regex("error|no se pudo|fall[oó]|excepci|ORA-|invalid|no est[aá] disponible|sin permiso|no tiene permiso|acceso denegado", RegexOptions.IgnoreCase);
        using var vigia = new System.Threading.Timer(VigilarDialogos, null, 500, 500);
        var resumen = new List<string>();
        foreach (var (nombre, crear) in modulos)
        {
            if (args.Length > 1 && !args[1].Split(',').Contains(nombre)) continue;
            lock (Eventos) Eventos.Clear();
            string estado = "abre";
            int filas = 0, tablas = 0; var marcas = new List<string>();
            try
            {
                using Form f = crear();
                f.StartPosition = FormStartPosition.Manual;
                f.Location = new Point(-4000, 0);
                f.WindowState = FormWindowState.Normal;
                f.Size = new Size(1366, 768);
                f.Show();
                var fin = DateTime.UtcNow.AddSeconds(9);
                while (DateTime.UtcNow < fin) { Bombear(); Thread.Sleep(40); }
                Interactuar(nombre, f, resumen);
                var controles = Todos(f).ToList();
                _ultimaMedida = MedirScroll(controles) + $"; memoria={GC.GetTotalMemory(false) / (1024 * 1024)}MB";
                var textos = controles.Where(c => c.Visible && !string.IsNullOrWhiteSpace(c.Text)).Select(c => c.GetType().Name + ": " + c.Text.Replace("\r", " ").Replace("\n", " / ")).Distinct().ToList();
                foreach (var g in controles.OfType<DataGridView>())
                {
                    tablas++; filas += g.RowCount;
                    textos.Add("TABLA " + g.Name + " filas=" + g.RowCount + " cols=" + string.Join(",", g.Columns.Cast<DataGridViewColumn>().Where(k => k.Visible).Select(k => k.HeaderText)));
                    foreach (DataGridViewRow r in g.Rows.Cast<DataGridViewRow>().Take(6)) textos.Add("  fila: " + string.Join(" | ", r.Cells.Cast<DataGridViewCell>().Where(k => k.Visible).Select(k => k.FormattedValue)));
                }
                marcas = textos.Where(t => error.IsMatch(t) && !t.StartsWith("  fila")).ToList();
                File.WriteAllLines(Path.Combine(salida, nombre + ".txt"), textos);
                using var bmp = new Bitmap(f.Width, f.Height);
                f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                bmp.Save(Path.Combine(salida, nombre + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                f.Close();
                var cierre = DateTime.UtcNow.AddMilliseconds(400);
                while (DateTime.UtcNow < cierre) { Bombear(); Thread.Sleep(20); }
            }
            catch (Exception e) { estado = "FALLA: " + e.GetType().Name + ": " + e.Message; }
            GC.Collect(); GC.WaitForPendingFinalizers();
            List<string> ev; lock (Eventos) ev = Eventos.ToList();
            resumen.Add($"{nombre}: {estado}; tablas={tablas} filas={filas}; eventos={ev.Count}; marcas={marcas.Count}; {_ultimaMedida}");
            resumen.AddRange(ev.Distinct().Take(6).Select(x => "    " + (x.Length > 2400 ? x.Substring(0, 2400) : x)));
            resumen.AddRange(marcas.Take(5).Select(x => "    marca> " + (x.Length > 220 ? x.Substring(0, 220) : x)));
            File.WriteAllLines(Path.Combine(salida, "resumen.txt"), resumen);
        }
        File.AppendAllLines(Path.Combine(salida, "resumen.txt"), new[] { "FIN" });
    }
}
