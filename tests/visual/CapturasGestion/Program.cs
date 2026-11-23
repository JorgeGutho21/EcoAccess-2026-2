using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Presentacion;

// Capturas de las áreas de gestión (Supervisor u Operario) con la ventana principal REAL y cada módulo del menú, para revisar
// la estética con la skill eco-chequeo-estetica. Oracle es inalcanzable a propósito (127.0.0.1:1): no usa credenciales ni datos
// reales. La ventana queda casi entera fuera de la pantalla (1 px dentro, para tomar la escala del monitor) y se dibuja con
// DrawToBitmap, así que no tapa el escritorio.
// Uso: CapturasGestion.exe <carpeta de salida> [supervisor|operario] [ancho alto]
static class Program
{
    static void Bombear(int ms)
    {
        var fin = DateTime.UtcNow.AddMilliseconds(ms);
        while (DateTime.UtcNow < fin) { Application.DoEvents(); Thread.Sleep(15); }
    }

    static IEnumerable<Control> Todos(Control c) { foreach (Control h in c.Controls) { yield return h; foreach (var n in Todos(h)) yield return n; } }

    static void Capturar(Form f, string carpeta, string nombre)
    {
        // Volcado opcional de posiciones (CAPTURAS_VOLCAR=1) para depurar desplazamientos del diseño fijo.
        if (Environment.GetEnvironmentVariable("CAPTURAS_VOLCAR") == "1")
            File.WriteAllLines(Path.Combine(carpeta, nombre + ".txt"), Todos(f).Where(c => c.Visible)
                .Select(c => $"{c.GetType().Name} {c.Name} {c.Bounds} pad {c.Padding} dpi {c.DeviceDpi} en {c.Parent?.GetType().Name} {c.Parent?.Name}"), new UTF8Encoding(false));
        using var bmp = new Bitmap(f.Width, f.Height);
        f.DrawToBitmap(bmp, new Rectangle(Point.Empty, f.Size));
        bmp.Save(Path.Combine(carpeta, nombre + ".png"), ImageFormat.Png);
    }

    static IEnumerable<Form> Dialogos(ServiciosAplicacion servicios, SesionActiva sesion)
    {
        yield return new FormCambioContrasena(servicios.Autenticacion, sesion);
        using var png = new MemoryStream();
        using (var muestra = new Bitmap(64, 64))
        {
            using (Graphics g = Graphics.FromImage(muestra)) { g.Clear(Color.White); g.FillRectangle(Brushes.Black, 8, 8, 20, 20); g.FillRectangle(Brushes.Black, 36, 36, 20, 20); }
            muestra.Save(png, ImageFormat.Png);
        }
        var qr = new FormQrCaneca("Caneca de prueba", new Uri("https://t.me/EcoAccessP3_JG_bot?start=prueba"), png.ToArray());
        TemaEcoAccess_Aplicar(qr);
        yield return qr;
    }

    // Igual que FormContenedores antes de mostrar el QR (el tema es interno; se aplica por reflexión).
    static void TemaEcoAccess_Aplicar(Form formulario) =>
        typeof(FormPrincipal).Assembly.GetType("EcoAccess.P3.Presentacion.TemaEcoAccess")!
            .GetMethod("Aplicar", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { formulario });

    // Cuentas DE EJEMPLO en la tabla de Usuarios (sin Oracle), con el panel de acciones habilitado como tras una consulta
    // autorizada; solo para revisar el diseño. Usa el registro interno FilaUsuario y repite los ajustes de columnas de CargarAsync.
    static bool UsuariosDeEjemplo(Form principal)
    {
        Control? modulo = Todos(principal).FirstOrDefault(c => c.GetType().Name == "FormUsuarios");
        if (modulo is null) return false;
        Type tipo = modulo.GetType();
        const BindingFlags privado = BindingFlags.Instance | BindingFlags.NonPublic;
        var tabla = (DataGridView)tipo.GetField("tablaUsuarios", privado)!.GetValue(modulo)!;
        Type fila = tipo.GetNestedType("FilaUsuario", BindingFlags.NonPublic)!;
        var lista = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(fila))!;
        foreach (var (id, nombre, correo, roles, activo) in new[]
                 {
                     (1, "Ana María Pérez", "ana.perez@unicesar.edu.co", "Ciudadano", true),
                     (2, "Carlos Andrés Gómez", "carlos.gomez@unicesar.edu.co", "Ciudadano, Operario", true),
                     (3, "Daniela Torres", "daniela.torres@unicesar.edu.co", "Supervisor (Administrador del sistema)", true),
                     (4, "Esteban Ruiz", "esteban.ruiz@unicesar.edu.co", "Ciudadano", false),
                     (5, "Fernanda López", "fernanda.lopez@unicesar.edu.co", "Operario", true),
                     (6, "Gabriel Martínez", "gabriel.martinez@unicesar.edu.co", "Ciudadano", true),
                     (7, "Helena Castro", "helena.castro@unicesar.edu.co", "Ciudadano", false),
                 })
            lista.Add(Activator.CreateInstance(fila, id, nombre, correo, roles, activo ? "Activo" : "Inactivo", activo ? "Sí (v2)" : "No", "2026-11-2" + id % 10 + " 08:1" + id + " (COL)"));
        tabla.DataSource = lista;
        tabla.Columns["Id"]!.Visible = false;
        tabla.Columns["Correo"]!.HeaderText = "Correo electrónico";
        tabla.Columns["Registro"]!.HeaderText = "Fecha de registro";
        foreach (DataGridViewColumn columna in tabla.Columns)
            columna.Width = (int)(columna.Name is "Correo" or "Roles" ? 230 * modulo.DeviceDpi / 96f : 155 * modulo.DeviceDpi / 96f);
        tipo.GetField("_datosAutorizados", privado)!.SetValue(modulo, true);
        ((Control)tipo.GetField("panelAcciones", privado)!.GetValue(modulo)!).Enabled = true;
        tabla.Rows[1].Selected = true;
        tabla.CurrentCell = tabla.Rows[1].Cells["Nombre"];
        tipo.GetMethod("ActualizarSeleccionUsuarios", privado)!.Invoke(modulo, null);
        return true;
    }

    // Materiales DE EJEMPLO (sin Oracle), con los ajustes de columnas del propio formulario (PrepararColumnas) y el panel de
    // acciones habilitado como tras una consulta autorizada; solo para revisar el diseño.
    static bool MaterialesDeEjemplo(Form principal)
    {
        Control? modulo = Todos(principal).FirstOrDefault(c => c.GetType().Name == "FormMateriales");
        if (modulo is null) return false;
        Type tipo = modulo.GetType();
        const BindingFlags privado = BindingFlags.Instance | BindingFlags.NonPublic;
        var tabla = (DataGridView)tipo.GetField("tablaMateriales", privado)!.GetValue(modulo)!;
        Type fila = tipo.GetNestedType("FilaMaterial", BindingFlags.NonPublic)!;
        var lista = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(fila))!;
        foreach (var (id, codigo, nombre, categoria, habilitado, descripcion) in new[]
                 {
                     (1, "PET", "Botella plástica PET", "Aprovechable (blanca)", true, "Vacía, limpia y aplastada"),
                     (2, "LATA_ALUMINIO", "Lata de aluminio", "Aprovechable (blanca)", true, "Vacía y aplastada"),
                     (3, "CARTON", "Cartón", "Aprovechable (blanca)", true, "Seco y doblado"),
                     (4, "ORGANICO", "Restos de comida", "Orgánico aprovechable (verde)", true, "Va en la caneca verde, sin empaques"),
                     (5, "SERVILLETA", "Servilleta o papel higiénico usado", "No aprovechable (negra)", true, "Residuo no aprovechable"),
                     (6, "ICOPOR_CONT", "Icopor con restos de comida", "No aprovechable (negra)", false, "Va en la caneca negra"),
                     (7, "MEDICAMENTO", "Medicamento vencido", "Peligroso (ruta especial)", true, "Entrégalo en un punto posconsumo"),
                 })
        {
            string texto = TextoCategoria(categoria);
            lista.Add(Activator.CreateInstance(fila, id, codigo, nombre, texto, habilitado ? "Habilitado" : "Deshabilitado", descripcion));
        }
        tabla.DataSource = lista;
        tipo.GetMethod("PrepararColumnas", privado)!.Invoke(modulo, null);
        tipo.GetField("_lecturaAutorizadaMaterial", privado)!.SetValue(modulo, true);
        ((Control)tipo.GetField("panelAcciones", privado)!.GetValue(modulo)!).Enabled = true;
        tabla.Rows[0].Selected = true;
        tabla.CurrentCell = tabla.Rows[0].Cells["Codigo"];
        tipo.GetMethod("ActualizarSeleccionMaterial", privado)!.Invoke(modulo, null);
        return true;

        // Usa el texto real de cada categoría (TextosCategoria) para que la tabla pinte su pastilla como con datos de Oracle.
        static string TextoCategoria(string aproximado)
        {
            var opciones = (System.Collections.IEnumerable)typeof(FormPrincipal).Assembly.GetType("EcoAccess.P3.Presentacion.TextosCategoria")!
                .GetProperty("Opciones", BindingFlags.Static | BindingFlags.Public)!.GetValue(null)!;
            string clave = aproximado.ToLowerInvariant().Substring(0, Math.Min(4, aproximado.Length));
            foreach (object o in opciones)
            {
                string t = (string)o.GetType().GetProperty("Texto")!.GetValue(o)!;
                if (t.ToLowerInvariant().StartsWith(clave, StringComparison.Ordinal)) return t;
            }
            return aproximado;
        }
    }

    // Canecas DE EJEMPLO (sin Oracle) con los mismos ajustes de columnas que CargarAsync; solo para revisar el diseño.
    static bool CanecasDeEjemplo(Form principal)
    {
        Control? modulo = Todos(principal).FirstOrDefault(c => c.GetType().Name == "FormContenedores");
        if (modulo is null) return false;
        Type tipo = modulo.GetType();
        const BindingFlags privado = BindingFlags.Instance | BindingFlags.NonPublic;
        var tabla = (DataGridView)tipo.GetField("tablaCanecas", privado)!.GetValue(modulo)!;
        Type fila = tipo.GetNestedType("FilaCaneca", BindingFlags.NonPublic)!;
        var lista = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(fila))!;
        foreach (var (id, nombre, punto, estado, materiales) in new[]
                 {
                     (1, "EcoAccess · Caneca verde", "Bloque A · Cafetería", "En servicio", "Restos de comida"),
                     (2, "EcoAccess · Caneca negra", "Bloque A · Cafetería", "En servicio", "Servilletas, Icopor con restos"),
                     (3, "EcoAccess · Caneca blanca", "Biblioteca central", "En servicio", "Botella PET, Cartón, Lata de aluminio"),
                     (4, "EcoAccess — Caneca piloto", "Laboratorio de sistemas", "Fuera de servicio", "Botella PET, Papel de oficina"),
                     (5, "Caneca antigua coliseo", "Coliseo", "Dada de baja", "Botella PET"),
                 })
            lista.Add(Activator.CreateInstance(fila, id, nombre, punto, estado, materiales, $"qr-{id:000}-a7f3c9", $"2{id}/11/2026 08:30"));
        tabla.DataSource = lista;
        tabla.Columns["Id"]!.Visible = false;
        tabla.Columns["IdentificadorQr"]!.HeaderText = "Identificador QR";
        tabla.Columns["Alta"]!.HeaderText = "Alta";
        ((Control)tipo.GetField("panelAcciones", privado)!.GetValue(modulo)!).Enabled = true;
        tabla.CurrentCell = tabla.Rows[2].Cells["Nombre"];
        return true;
    }

    // Puntos de instalación DE EJEMPLO (sin Oracle), con los mismos encabezados y selección que aplica CargarAsync; solo para
    // revisar icono, pastilla de estado, ficha lateral y línea de estado del banner.
    static bool PuntosDeEjemplo(Form principal)
    {
        Control? modulo = Todos(principal).FirstOrDefault(c => c.GetType().Name == "FormPuntos");
        if (modulo is null) return false;
        Type tipo = modulo.GetType();
        const BindingFlags privado = BindingFlags.Instance | BindingFlags.NonPublic;
        var tabla = (DataGridView)tipo.GetField("tablaPuntos", privado)!.GetValue(modulo)!;
        Type fila = tipo.GetNestedType("FilaPunto", BindingFlags.NonPublic)!;
        var lista = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(fila))!;
        foreach (var (id, nombre, referencia, estado) in new[]
                 {
                     (1, "Bloque A · primer piso", "Junto a la cafetería", "Activo"),
                     (2, "Biblioteca central", "Entrada principal", "Activo"),
                     (3, "Laboratorio de sistemas", "Pasillo norte", "Inactivo"),
                     (4, "Coliseo", "Acceso a las graderías", "Activo"),
                 })
            lista.Add(Activator.CreateInstance(fila, id, nombre, referencia, estado));
        tabla.DataSource = lista;
        tabla.Columns["Id"]!.Visible = false;
        tabla.Columns["Referencia"]!.HeaderText = "Referencia de ubicación";
        ((Control)tipo.GetField("panelAcciones", privado)!.GetValue(modulo)!).Enabled = true;
        tabla.CurrentCell = tabla.Rows[1].Cells["Nombre"];
        return true;
    }

    // Auditoría DE EJEMPLO (sin Oracle): filas para revisar las tres páginas, sus tablas pulidas y la pastilla de verificación.
    static bool AuditoriaDeEjemplo(Form principal)
    {
        Control? modulo = Todos(principal).FirstOrDefault(c => c.GetType().Name == "FormInteraccionesDepositos");
        if (modulo is null) return false;
        Type tipo = modulo.GetType();
        const BindingFlags privado = BindingFlags.Instance | BindingFlags.NonPublic;
        var depositos = (DataGridView)tipo.GetField("tablaDepositos", privado)!.GetValue(modulo)!;
        var solicitudes = (DataGridView)tipo.GetField("tablaInteracciones", privado)!.GetValue(modulo)!;
        var incidencias = (DataGridView)tipo.GetField("tablaIncidencias", privado)!.GetValue(modulo)!;
        Type filaDeposito = tipo.GetNestedType("VistaDeposito", BindingFlags.NonPublic)!;
        Type filaInteraccion = tipo.GetNestedType("VistaInteraccion", BindingFlags.NonPublic)!;
        Type filaIncidencia = tipo.GetNestedType("VistaIncidencia", BindingFlags.NonPublic)!;
        var listaDepositos = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(filaDeposito))!;
        foreach (var fila in new[]
                 {
                     (1482, "2026-11-23 09:18:24", "EcoAccess · Caneca verde", "104", "Restos de comida", "Sin verificar", false, "2,300", "2,100"),
                     (1481, "2026-11-23 08:42:11", "EcoAccess · Caneca blanca", "87", "Botella plástica PET", "Botella plástica PET", true, "0,540", "0,500"),
                     (1480, "2026-11-22 16:05:33", "EcoAccess · Caneca negra", "—", "Servilleta usada", "Servilleta usada", true, "—", "0,080"),
                     (1479, "2026-11-22 14:29:02", "EcoAccess · Caneca blanca", "51", "Cartón", "Sin verificar", false, "1,120", "1,000"),
                 })
            listaDepositos.Add(Activator.CreateInstance(filaDeposito, fila.Item1, fila.Item2, fila.Item3, fila.Item4, fila.Item5, fila.Item6, fila.Item7, fila.Item8, fila.Item9));
        depositos.DataSource = listaDepositos;
        foreach (var (nombre, encabezado) in new[] { ("Id", "Depósito"), ("Fecha", "Detectado (Colombia)"), ("Usuario", "Id usuario"), ("Declarado", "Material declarado"), ("Verificado", "Material verificado"), ("KgMedidos", "Kg medidos"), ("KgEstimados", "Kg estimados, no medidos") })
            depositos.Columns[nombre]!.HeaderText = encabezado;
        depositos.CurrentCell = depositos.Rows[1].Cells["Id"];

        var listaSolicitudes = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(filaInteraccion))!;
        foreach (var fila in new[]
                 {
                     (771, "2026-11-23 09:16:51", "104", "Telegram", "Cerrada", "12", "2026-11-23 09:18:25"),
                     (770, "2026-11-23 08:39:13", "87", "QR", "Cerrada", "5", "2026-11-23 08:42:12"),
                 })
            listaSolicitudes.Add(Activator.CreateInstance(filaInteraccion, fila.Item1, fila.Item2, fila.Item3, fila.Item4, fila.Item5, fila.Item6, fila.Item7));
        solicitudes.DataSource = listaSolicitudes;

        var listaIncidencias = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(filaIncidencia))!;
        foreach (var fila in new[]
                 {
                     (308, 7, "771", "Sensor sin confirmación", "2026-11-23 09:17:03", "2026-11-23 09:16:58", "evt-7-203"),
                     (307, 3, "—", "Apertura sin detección", "2026-11-22 14:30:11", "2026-11-22 14:29:10", "evt-3-202"),
                 })
            listaIncidencias.Add(Activator.CreateInstance(filaIncidencia, fila.Item1, fila.Item2, fila.Item3, fila.Item4, fila.Item5, fila.Item6, fila.Item7));
        incidencias.DataSource = listaIncidencias;
        ((Label)tipo.GetField("etiquetaPagina", privado)!.GetValue(modulo)!).Text = "Página 1 de 1 · 4 depósitos · 50 por página";
        ((Label)tipo.GetField("etiquetaInteracciones", privado)!.GetValue(modulo)!).Text = "2 solicitudes en el período; se muestran las más recientes.";
        ((Label)tipo.GetField("etiquetaIncidencias", privado)!.GetValue(modulo)!).Text = "2 eventos en revisión en el período; consulta de auditoría.";
        return true;
    }

    // Estadísticas DE EJEMPLO (sin Oracle): llama a los mismos métodos del formulario (TextoResumen, MostrarAgrupadas,
    // MostrarFallos y MostrarDetalle) con cifras inventadas; solo para revisar tarjetas, gráfico y tablas.
    static bool EstadisticasDeEjemplo(Form principal)
    {
        Control? modulo = Todos(principal).FirstOrDefault(c => c.GetType().Name == "FormEstadisticas");
        if (modulo is null) return false;
        Type tipo = modulo.GetType();
        const BindingFlags privado = BindingFlags.Instance | BindingFlags.NonPublic;
        var resumen = new ResumenActividad(new ConteoInteracciones(115, 11, 0, 94, 7, 3, 16, 0, 99), new ConteoOrdenes(101, 0, 94, 6, 1, 94, 94),
            new ConteoEventos(328, 328, 0, 1), new ResumenDepositos(94, 33, 33, 0, 0m, 92, 9.912m));
        string texto = (string)tipo.GetMethod("TextoResumen", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { resumen })!;
        ((TextBox)tipo.GetField("textoResumen", privado)!.GetValue(modulo)!).Text = texto;
        var grupos = new[] { ("Botella plástica PET", 31, 14), ("Restos de comida", 22, 9), ("Cartón", 15, 4), ("Lata de aluminio", 11, 3), ("Papel de oficina y cuadernos", 8, 2), ("Servilletas", 5, 1), ("Vidrio", 2, 0) }
            .Select((g, k) => new FilaAgrupada(k + 1, null, g.Item1, g.Item2, g.Item3, 0, 0m, g.Item2 - 1, g.Item2 * 0.105m)).ToList();
        tipo.GetMethod("MostrarAgrupadas", privado)!.Invoke(modulo, new object[] { (IReadOnlyList<FilaAgrupada>)grupos });
        var fallos = new List<FilaFallosContenedor> { new(1, "EcoAccess · Caneca verde", 1, 0, 1, 3), new(2, "EcoAccess · Caneca blanca", 0, 0, 0, 2), new(3, "EcoAccess · Caneca negra", 0, 0, 0, 1) };
        tipo.GetMethod("MostrarFallos", privado)!.Invoke(modulo, new object[] { (IReadOnlyList<FilaFallosContenedor>)fallos });
        DateTime ahora = DateTime.UtcNow;
        var filas = new List<FilaDeposito>
        {
            new(1482, ahora.AddMinutes(-20), 1, "EcoAccess · Caneca verde", 104, "Restos de comida", null, null, 0.21m),
            new(1481, ahora.AddMinutes(-58), 3, "EcoAccess · Caneca blanca", 87, "Botella plástica PET", "Botella plástica PET", null, 0.05m),
            new(1480, ahora.AddHours(-3), 2, "EcoAccess · Caneca negra", null, "Servilleta usada", "Servilleta usada", null, 0.01m),
            new(1479, ahora.AddHours(-5), 3, "EcoAccess · Caneca blanca", 51, "Cartón", null, null, 0.12m),
            new(1478, ahora.AddHours(-26), 3, "EcoAccess · Caneca blanca", 33, "Lata de aluminio", "Lata de aluminio", null, 0.02m),
        };
        tipo.GetMethod("MostrarDetalle", privado)!.Invoke(modulo, new object[] { new PaginaDepositos(filas, 94, 1, 50) });
        return true;
    }

    // Valoraciones DE EJEMPLO (sin Oracle): las mismas filas agregadas que devuelve la consulta; solo para revisar el diseño.
    static bool ValoracionesDeEjemplo(Form principal)
    {
        Control? modulo = Todos(principal).FirstOrDefault(c => c.GetType().Name == "FormOpiniones");
        if (modulo is null) return false;
        var tabla = Todos(modulo).OfType<DataGridView>().First(t => t.AccessibleName == "Valoraciones por día");
        DateTime hoy = DateTime.UtcNow.Date;
        var filas = new[] { (4.6m, 9, 2), (4.2m, 12, 3), (3.8m, 6, 1), (4.9m, 4, 0), (2.7m, 3, 2), (4.4m, 15, 5), (4.0m, 8, 1), (3.3m, 5, 2), (4.7m, 11, 4), (4.5m, 7, 1), (1.8m, 2, 1), (4.1m, 10, 3) }
            .Select((f, k) => new TendenciaOpiniones(hoy.AddDays(-2 * k), f.Item2, f.Item1, f.Item3)).ToList();
        tabla.DataSource = filas;
        var aviso = (Label)modulo.GetType().GetField("_avisoValoraciones", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(modulo)!;
        aviso.Text = $"Período: {filas.Sum(f => f.Cantidad):N0} valoraciones · {filas.Sum(f => f.Invitados):N0} de invitados. La tabla muestra promedios diarios.";
        return true;
    }

    // Dashboard institucional DE EJEMPLO (sin Oracle): un informe inventado pasado por el mismo Mostrar() del formulario.
    static bool InstitucionalDeEjemplo(Form principal)
    {
        Control? modulo = Todos(principal).FirstOrDefault(c => c.GetType().Name == "FormDashboardInstitucional");
        if (modulo is null) return false;
        Type tipo = modulo.GetType();
        const BindingFlags privado = BindingFlags.Instance | BindingFlags.NonPublic;
        DateOnly hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5)), primero = hoy.AddDays(-29);
        int[] serie = { 3, 2, 4, 2, 1, 5, 3, 2, 6, 4, 2, 3, 1, 4, 5, 3, 2, 4, 6, 3, 2, 5, 4, 3, 2, 6, 5, 4, 3, 7 };
        var dias = serie.Select((d, k) => new FilaAgrupada(null, primero.AddDays(k), "", d, Math.Max(0, d - 1), 0, 0, 0, 0)).ToList();
        var materiales = new[] { ("Botella plástica PET", 31), ("Restos de comida", 22), ("Cartón", 15), ("Lata de aluminio", 11), ("Papel de oficina", 8) }
            .Select((m, k) => new FilaAgrupada(k + 1, null, m.Item1, m.Item2, m.Item2 / 2, 0, 0, 0, 0)).ToList();
        var canecas = new[] { ("EcoAccess · Caneca verde", 38), ("EcoAccess · Caneca blanca", 34), ("EcoAccess · Caneca negra", 22) }
            .Select((c, k) => new FilaAgrupada(k + 1, null, c.Item1, c.Item2, c.Item2 / 2, 0, 0, 0, 0)).ToList();
        var ubicaciones = new[] { ("Bloque A · Cafetería", 60), ("Biblioteca central", 34) }.Select((u, k) => new FilaAgrupada(k + 1, null, u.Item1, u.Item2, u.Item2 / 2, 0, 0, 0, 0)).ToList();
        var actual = new IndicadoresInforme(94, 47, 31, 520, 495, 92, 4.2m, 115, 63, 25, 2);
        var anterior = new IndicadoresInforme(81, 52, 28, 470, 470, 70, 4.0m, 120, 58, 0, 0);
        tipo.GetField("_datos", privado)!.SetValue(modulo, new InformeInstitucional(actual, anterior, dias, materiales, canecas, ubicaciones));
        tipo.GetField("_desdeConsultado", privado)!.SetValue(modulo, primero);
        tipo.GetField("_hastaConsultado", privado)!.SetValue(modulo, hoy);
        tipo.GetMethod("Mostrar", privado)!.Invoke(modulo, null);
        foreach (var (campo, valor, detalle) in new[] { ("_visionTotal", "42", "18 ciudadanos vinculados"), ("_visionDestino", "35", "objeto reconocido"), ("_visionOtraFoto", "7", "foto borrosa, incierta o con varios objetos"), ("_visionLatencia", "2,4 s", "modelo local") })
        {
            object tarjeta = tipo.GetField(campo, privado)!.GetValue(modulo)!;
            tarjeta.GetType().GetMethod("Mostrar", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(tarjeta, new object[] { valor, detalle });
        }
        ((DataGridView)tipo.GetField("_estadoCanecas", privado)!.GetValue(modulo)!).DataSource = new[]
        {
            new { Caneca = "EcoAccess · Caneca verde", Configuración = "Habilitada en configuración", Conectividad = "Sin telemetría: no se mide desde aquí", Dispositivo = "Pendiente de prueba física" },
            new { Caneca = "EcoAccess · Caneca blanca", Configuración = "Habilitada en configuración", Conectividad = "Sin telemetría: no se mide desde aquí", Dispositivo = "Pendiente de prueba física" },
            new { Caneca = "EcoAccess — Caneca piloto", Configuración = "Deshabilitada", Conectividad = "Sin telemetría: no se mide desde aquí", Dispositivo = "Pendiente de prueba física" },
        }.ToList();
        return true;
    }

    // Sistema y Telegram DE EJEMPLO (sin Oracle): los mismos textos que escribe ConsultarAsync y MostrarServicios.
    static bool SistemaDeEjemplo(Form principal)
    {
        Control? modulo = Todos(principal).FirstOrDefault(c => c.GetType().Name == "FormDiagnosticoOracle");
        if (modulo is null) return false;
        Type tipo = modulo.GetType();
        const BindingFlags privado = BindingFlags.Instance | BindingFlags.NonPublic;
        Label Etiqueta(string campo) => (Label)tipo.GetField(campo, privado)!.GetValue(modulo)!;
        Etiqueta("_estado").Text = "Oracle disponible · sin incidencias estructurales detectadas";
        Etiqueta("_detalle").Text = "Esquema: ECOACCESS_P3_APP · PDB: ECOACCESS_TP · Consulta: 41,7 ms\n" +
            "Tablas: 27 · Restricciones deshabilitadas/no validadas: 0 · Objetos inválidos: 0 · Índices inválidos: 0\n" +
            "Última verificación: 2026-11-23 10:42:18 (Colombia)\n" +
            "La presencia de tablas no acredita por sí sola el historial ni el contenido completo de una migración.";
        Etiqueta("_servicios").Text = "Servicios de la demostración (este equipo):\n" +
            "  ● Bot de Telegram: activo · @EcoAccessP3_JG_bot · PID 8812\n" +
            "  ● Bridge del sensor: activo · COM4 · PID 9120\n" +
            "  ○ Simulador de canecas: no disponible · no iniciado en esta sesión\n" +
            "  ● IA de fotos (Qwen 3.8 en Groq): activo · clave configurada · modo nube · última respuesta hace 3 min\n" +
            "Última operación del bridge: 2026-11-23 10:39:05 · apertura confirmada en EcoAccess · Caneca verde\n" +
            "Errores recientes: ninguno registrado.";
        ((DataGridView)tipo.GetField("_migraciones", privado)!.GetValue(modulo)!).DataSource = new[]
        {
            new { Versión = "V001", Estado = "Estructura presente", Faltantes = "" },
            new { Versión = "V012", Estado = "Estructura presente", Faltantes = "" },
            new { Versión = "V021", Estado = "Estructura presente", Faltantes = "" },
            new { Versión = "V023", Estado = "Estructura incompleta", Faltantes = "CONSULTA_VISION, IX_CONSULTA_VISION_FECHA" },
        }.ToList();
        return true;
    }

    // Consultar del Operario DE EJEMPLO (sin Oracle): las mismas filas internas y encabezados que pone el formulario.
    static bool ConsultarDeEjemplo(Form principal)
    {
        Control? modulo = Todos(principal).FirstOrDefault(c => c.GetType().Name == "FormConsultaOperario");
        if (modulo is null) return false;
        Type tipo = modulo.GetType();
        const BindingFlags privado = BindingFlags.Instance | BindingFlags.NonPublic;
        void Llenar(string campo, string registro, object[][] filas, params (string Columna, string Texto)[] encabezados)
        {
            Type fila = tipo.GetNestedType(registro, BindingFlags.NonPublic)!;
            var lista = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(fila))!;
            foreach (object[] valores in filas) lista.Add(Activator.CreateInstance(fila, valores));
            var tabla = (DataGridView)tipo.GetField(campo, privado)!.GetValue(modulo)!;
            tabla.DataSource = lista;
            foreach (var (columna, texto) in encabezados) tabla.Columns[columna]!.HeaderText = texto;
        }
        Llenar("_ciudadanos", "FilaCiudadano", new[]
        {
            new object[] { 104, "Laura Martínez Díaz", "laura.martinez@unicesar.edu.co", "Activa", "2026-10-02" },
            new object[] { 87, "Andrés Pérez Gómez", "andres.perez@unicesar.edu.co", "Activa", "2026-10-11" },
            new object[] { 51, "Camila Rodríguez", "camila.rodriguez@unicesar.edu.co", "Inactiva", "2026-09-28" },
            new object[] { 33, "José Ángel Núñez", "jose.nunez@unicesar.edu.co", "Activa", "2026-11-04" },
        }, ("Id", "Código"), ("Nombre", "Nombre"), ("Correo", "Correo"), ("Estado", "Cuenta"), ("Registro", "Registro"));
        Llenar("_canecas", "FilaCaneca", new[]
        {
            new object[] { 1, "EcoAccess · Caneca verde", "En servicio", 6 },
            new object[] { 2, "EcoAccess · Caneca negra", "En servicio", 4 },
            new object[] { 3, "EcoAccess · Caneca blanca", "En servicio", 9 },
            new object[] { 4, "EcoAccess — Caneca piloto", "Fuera de servicio", 2 },
            new object[] { 5, "Caneca antigua coliseo", "Dada de baja", 1 },
        }, ("Id", "Código"), ("Nombre", "Caneca"), ("Estado", "Estado"), ("Materiales", "Materiales admitidos"));
        Llenar("_catalogo", "FilaMaterial", new[]
        {
            new object[] { "MAT-PET", "Botella plástica PET", "Aprovechable (blanca)" },
            new object[] { "MAT-ORG", "Restos de comida", "Orgánico aprovechable (verde)" },
            new object[] { "MAT-SRV", "Servilleta usada", "No aprovechable (negra)" },
            new object[] { "MAT-PIL", "Pilas y baterías", "Peligroso (ruta especial)" },
        }, ("Codigo", "Código"), ("Nombre", "Material"), ("Categoria", "Categoría"));
        var estado = (Label)tipo.GetField("_estado", privado)!.GetValue(modulo)!;
        estado.ForeColor = Color.FromArgb(0x10, 0x5C, 0x3B);
        estado.Text = "4 ciudadanos.";
        return true;
    }

    // Ecopuntos DE EJEMPLO (sin Oracle): los mismos textos y columnas que escribe ConsultarAsync del formulario.
    static bool EcopuntosDeEjemplo(Form principal)
    {
        Control? modulo = Todos(principal).FirstOrDefault(c => c.GetType().Name == "FormEcopuntos");
        if (modulo is null) return false;
        Type tipo = modulo.GetType();
        const BindingFlags privado = BindingFlags.Instance | BindingFlags.NonPublic;
        object Campo(string nombre) => tipo.GetField(nombre, privado)!.GetValue(modulo)!;
        ((Label)Campo("_saldo")).Text = $"{185:N0} ecopuntos";
        ((Label)Campo("_totales")).Text = $"Acreditados: {205:N0} · revertidos: {20:N0}";
        DateTime ahora = DateTime.Now;
        var historial = (DataGridView)Campo("_historial");
        historial.DataSource = new[]
        {
            new { CreditoOMovimiento = 412, Deposito = 1482, Regla = 7, Tipo = TipoMovimientoEcopuntos.Credito, Puntos = 25, Fecha = ahora.AddMinutes(-30), Actor = 2, Motivo = "Depósito verificado" },
            new { CreditoOMovimiento = 409, Deposito = 1479, Regla = 5, Tipo = TipoMovimientoEcopuntos.Credito, Puntos = 40, Fecha = ahora.AddHours(-5), Actor = 2, Motivo = "Depósito verificado" },
            new { CreditoOMovimiento = 401, Deposito = 1460, Regla = 5, Tipo = TipoMovimientoEcopuntos.Reverso, Puntos = -20, Fecha = ahora.AddDays(-1), Actor = 1, Motivo = "Material verificado distinto al declarado" },
            new { CreditoOMovimiento = 388, Deposito = 1460, Regla = 5, Tipo = TipoMovimientoEcopuntos.Credito, Puntos = 20, Fecha = ahora.AddDays(-2), Actor = 2, Motivo = "Depósito verificado" },
            new { CreditoOMovimiento = 371, Deposito = 1431, Regla = 3, Tipo = TipoMovimientoEcopuntos.Credito, Puntos = 120, Fecha = ahora.AddDays(-6), Actor = 2, Motivo = "Depósito verificado" },
        }.ToList();
        historial.Visible = true;
        ((Control)Campo("_vacio")).Visible = false;
        ((Label)Campo("_estado")).Text = "Saldo de todo el libro; se muestran los últimos 50 movimientos.";
        return true;
    }

    [STAThread]
    static int Main(string[] args)
    {
        string salida = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "capturas_gestion");
        Directory.CreateDirectory(salida);
        var area = args.Length > 1 && args[1] == "operario" ? AreaAcceso.Operario : AreaAcceso.Supervisor;
        var cliente = args.Length > 3 ? new Size(int.Parse(args[2]), int.Parse(args[3])) : new Size(1366, 728);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var errores = new List<string>();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => errores.Add(e.Exception.ToString());

        var servicios = EcoAccess.P3.Presentacion.Program.ComponerServicios("User Id=capturas;Data Source=127.0.0.1:1/ninguno;Connection Timeout=2", "EcoAccessP3_JG_bot");
        var sesion = new SesionActiva(1, "Supervisora Demo", new[] { CodigoRol.Administrador, CodigoRol.Operario }, DateTime.UtcNow);
        using var principal = new FormPrincipal(servicios, sesion, area) { StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        principal.Location = new Point(1 - principal.Width, 0);
        principal.Show();
        principal.WindowState = FormWindowState.Normal;
        principal.Location = new Point(1 - principal.Width, 0);
        principal.ClientSize = cliente;
        // Toca 1 px del monitor principal: así usa su escala real (fuera de toda pantalla Windows la toma como 100 %).
        principal.Location = new Point(1 - principal.Width, 0);
        Bombear(2500);
        string prefijo = area == AreaAcceso.Supervisor ? "sup" : "ope";
        Capturar(principal, salida, $"{prefijo}_00_Dashboard");
        // El mismo dashboard con datos DE EJEMPLO (las cifras del diseño aprobado) para comparar la disposición con datos; no
        // vienen de Oracle ni se guardan en ningún lado.
        if (Todos(principal).FirstOrDefault(c => c.GetType().Name == "PanelResumenAdministrativo") is { } panel)
        {
            DateOnly hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5)), primero = hoy.AddDays(-29);
            int[] serie = { 3, 2, 1, 2, 1, 2, 1, 2, 1, 1, 2, 1, 1, 2, 1, 1, 2, 1, 2, 1, 2, 9, 2, 1, 1, 2, 1, 4, 3, 22 };
            var dias = serie.Select((d, k) => new FilaAgrupada(null, primero.AddDays(k), "", d, Math.Max(0, d / 2), 0, 0, 0, 0)).ToList();
            var materiales = new[] { ("Botella plástica PET", 38), ("Cartón", 27), ("Lata de aluminio", 22), ("Papel de oficina y cuadernos", 18), ("Restos de alimentos", 16), ("Vidrio", 8), ("Servilletas", 6) }
                .Select((m, k) => new FilaAgrupada(k + 1, null, m.Item1, m.Item2, 0, 0, 0, 0, 0)).ToList();
            DateTime ahora = DateTime.UtcNow;
            var actividad = new[]
            {
                (1382, "Restos de comida", "EcoAccess · Caneca verde"), (1381, "Restos de comida", "EcoAccess · Caneca verde"),
                (1380, "Papel de oficina y cuadernos", "EcoAccess — Caneca piloto"), (1379, "Botella plástica PET", "EcoAccess — Caneca piloto"),
                (1378, "Servilleta o papel higiénico usado", "EcoAccess · Caneca negra"), (1377, "Botella plástica PET", "EcoAccess — Caneca piloto"),
                (1376, "Restos de comida", "EcoAccess · Caneca verde"),
            }.Select((a, k) => new ActividadPanel(a.Item1, ahora.AddMinutes(-k), a.Item2, false, a.Item3)).ToList();
            var resumen = new ResumenPanelAdministrativo(new IndicadoresPanel(9, 6, 12, 135, 58, 156, 0, 0, 0, 80, 0), actividad);
            panel.GetType().GetMethod("MostrarResumen", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(panel, new object[] { resumen, dias, materiales, primero, hoy });
            Bombear(800);
            Capturar(principal, salida, $"{prefijo}_00_Dashboard_datos_de_ejemplo");
        }

        // Barra lateral contraída (CAPTURAS_CONTRAER=1): pulsa el icono «Contraer menú», captura y vuelve a expandirla.
        if (Environment.GetEnvironmentVariable("CAPTURAS_CONTRAER") == "1"
            && Todos(principal).FirstOrDefault(c => c.GetType().Name == "BotonNavegacion" && c.Text == "Contraer menú") is { } contraer)
        {
            MethodInfo clic = typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!;
            clic.Invoke(contraer, new object[] { EventArgs.Empty });
            Bombear(800);
            Capturar(principal, salida, $"{prefijo}_00_Dashboard_menu_contraido");
            clic.Invoke(contraer, new object[] { EventArgs.Empty });
            Bombear(800);
        }

        var botones = Todos(principal).Where(c => c.GetType().Name == "BotonNavegacion" && c.Visible).Select(c => c.Text)
            .Where(t => t is not ("Dashboard" or "Contraer menú" or "Análisis en Power BI" or "Perfil y contraseña" or "Cerrar sesión") && !t.StartsWith("Cambiar", StringComparison.Ordinal))
            .Where(t => Environment.GetEnvironmentVariable("CAPTURAS_SOLO") is not { Length: > 0 } solo || solo.Split('|').Contains(t))
            .Distinct().ToList();
        int i = 1;
        foreach (string texto in botones)
        {
            if (principal.IsDisposed) break;
            Control boton = Todos(principal).First(c => c.GetType().Name == "BotonNavegacion" && c.Text == texto);
            typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(boton, new object[] { EventArgs.Empty });
            Bombear(5000);
            string nombre = $"{prefijo}_{i++:00}_" + new string(texto.Where(char.IsLetterOrDigit).ToArray());
            Capturar(principal, salida, nombre);
            if ((texto == "Usuarios y roles" && UsuariosDeEjemplo(principal)) || (texto == "Materiales" && MaterialesDeEjemplo(principal))
                || (texto == "Canecas y QR" && CanecasDeEjemplo(principal)) || (texto == "Puntos de instalación" && PuntosDeEjemplo(principal))
                || (texto == "Auditar depósitos" && AuditoriaDeEjemplo(principal)) || (texto == "Estadísticas" && EstadisticasDeEjemplo(principal)) || (texto == "Valoraciones" && ValoracionesDeEjemplo(principal)) || (texto == "Dashboard institucional" && InstitucionalDeEjemplo(principal)) || (texto == "Sistema y Telegram" && SistemaDeEjemplo(principal)) || (texto == "Consultar" && ConsultarDeEjemplo(principal)) || (texto is "Ecopuntos" or "Tarifas y ecopuntos" && EcopuntosDeEjemplo(principal)))
            {
                Bombear(800);
                Capturar(principal, salida, nombre + "_datos_de_ejemplo");
            }
            {
                // Recorrido del módulo desplazado (CAPTURAS_DESPLAZAR=1): revisa que todo se alcance con la barra y nada se corte.
                if (Environment.GetEnvironmentVariable("CAPTURAS_DESPLAZAR") == "1"
                    && Todos(principal).OfType<ScrollableControl>().LastOrDefault(c => c.GetType().Name == "PanelDesplazable" && c.Visible && c.VerticalScroll.Visible) is { } desplazable)
                {
                    int paso = Math.Max(100, desplazable.ClientSize.Height * 4 / 5), k = 0;
                    for (int y = paso; k < 8; y += paso)
                    {
                        desplazable.AutoScrollPosition = new Point(0, y);
                        Bombear(500);
                        Capturar(principal, salida, $"{nombre}_desplazado{++k}");
                        if (-desplazable.AutoScrollPosition.Y < y) break;
                    }
                    File.WriteAllText(Path.Combine(salida, nombre + "_desplazamiento.txt"),
                        $"visible {desplazable.ClientSize} minimo {desplazable.AutoScrollMinSize} pantalla {desplazable.DisplayRectangle} modulo {desplazable.Controls.Cast<Control>().FirstOrDefault()?.Bounds}");
                    desplazable.AutoScrollPosition = Point.Empty;
                    Bombear(300);
                }
            }
            int p = 0;
            foreach (TabControl pestanas in Todos(principal).OfType<TabControl>().Where(t => t.Visible).ToList())
                for (int k = 1; k < pestanas.TabCount; k++)
                {
                    pestanas.SelectedIndex = k; Bombear(600);
                    // Con CAPTURAS_DESPLAZAR=1 lleva las pestañas a la vista si el módulo se desplaza (si no, la captura muestra
                    // la parte de arriba del módulo y no la página elegida).
                    if (Environment.GetEnvironmentVariable("CAPTURAS_DESPLAZAR") == "1"
                        && Todos(principal).OfType<ScrollableControl>().LastOrDefault(c => c.GetType().Name == "PanelDesplazable" && c.Visible && c.VerticalScroll.Visible) is { } vista)
                    {
                        int arriba = vista.PointToClient(pestanas.PointToScreen(Point.Empty)).Y - vista.AutoScrollPosition.Y;
                        vista.AutoScrollPosition = new Point(0, Math.Max(0, arriba - 140));
                        Bombear(400);
                    }
                    Capturar(principal, salida, $"{nombre}_pestana{++p}");
                }
        }
        // Diálogos del personal: perfil y contraseña, y el QR de una caneca (con un PNG de prueba, no el QR real).
        if (Environment.GetEnvironmentVariable("CAPTURAS_SOLO") is not { Length: > 0 })
        {
            foreach (Form dialogo in Dialogos(servicios, sesion))
                using (dialogo)
                {
                    dialogo.StartPosition = FormStartPosition.Manual;
                    dialogo.ShowInTaskbar = false;
                    dialogo.Show();
                    dialogo.Location = new Point(1 - dialogo.Width, 0);
                    Bombear(1500);
                    Capturar(dialogo, salida, $"{prefijo}_dialogo_{new string(dialogo.GetType().Name.Where(char.IsLetter).ToArray())}");
                    dialogo.Hide();
                }
        }
        File.WriteAllLines(Path.Combine(salida, "errores.txt"), errores, new UTF8Encoding(false));
        return errores.Count == 0 ? 0 : 1;
    }
}
