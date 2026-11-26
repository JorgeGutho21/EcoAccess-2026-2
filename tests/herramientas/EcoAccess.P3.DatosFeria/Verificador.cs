using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;

/// <summary>
/// Comprueba el lote con los servicios oficiales (inicio de sesión, áreas por rol, historial, saldo) y con consultas SQL
/// independientes de integridad. Solo si TODAS las cuentas inician sesión escribe los TXT del escritorio.
/// </summary>
static class Verificador
{
    static readonly List<(string Grupo, string Prueba, bool Ok, string Detalle)> R = new();
    static void Anotar(string g, string p, bool ok, string d) { R.Add((g, p, ok, d)); Program.Log($"{(ok ? "OK   " : "FALLA")} {g} · {p}: {d}"); }

    const string EnLote = "IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = :l AND tabla = '{0}')";
    static string De(string tabla) => string.Format(EnLote, tabla);

    public static async Task<int> EjecutarAsync(string? informe)
    {
        var usuarios = new RepositorioUsuariosOracle(Program.Fabrica);
        var materiales = new RepositorioMaterialesOracle(Program.Fabrica);
        var contenedores = new RepositorioContenedoresOracle(Program.Fabrica);
        var interacciones = new RepositorioInteraccionesOracle(Program.Fabrica);
        var auth = new ServicioAutenticacion(new RepositorioCredencialesOracle(Program.Fabrica), usuarios, new HasherPbkdf2(), TimeProvider.System);
        var servicio = new ServicioInteracciones(usuarios, interacciones, contenedores, materiales, TimeProvider.System);
        var eco = new ServicioEcopuntos(usuarios, interacciones, materiales, new RepositorioEcopuntosOracle(Program.Fabrica), TimeProvider.System);
        var cuentasSrv = new ServicioUsuarios(usuarios, TimeProvider.System);
        var cred = Credenciales.Leer();
        var manifiesto = (await Program.FilasAsync("SELECT clave, id_registro FROM registro_simulacion WHERE id_lote = :l AND tabla = 'USUARIO'", ("l", Program.Lote)))
            .ToDictionary(f => (string)f[0]!, f => Convert.ToInt32(f[1]));
        var p = ("l", (object?)Program.Lote);

        // ---------------------------------------------------------------- autenticación de TODAS las cuentas
        var verificadas = new Dictionary<string, (Cuenta C, int Id)>();
        foreach (var c in cred.Values.OrderBy(c => c.Clave))
        {
            if (!manifiesto.TryGetValue(c.Clave, out int id)) { Anotar("Autenticación", c.Clave, false, "no está en el manifiesto del lote"); continue; }
            var s = await auth.IniciarSesionPublicaAsync(c.Correo, c.Contrasena);
            if (!s.Exitoso || s.Valor.IdUsuario != id) { Anotar("Autenticación", c.Clave, false, $"inicio de sesión rechazado: {s.Mensaje}"); continue; }
            var area = c.Rol switch { CodigoRol.Ciudadano => AreaAcceso.Ciudadano, CodigoRol.Operario => AreaAcceso.Operario, _ => AreaAcceso.Supervisor };
            bool propia = AreasAcceso.Elegir(s.Valor, area).Exitoso;
            var ajena = c.Rol switch { CodigoRol.Ciudadano => AreaAcceso.Operario, CodigoRol.Operario => AreaAcceso.Supervisor, _ => (AreaAcceso?)null };
            bool bloqueada = ajena is null || !AreasAcceso.Elegir(s.Valor, ajena.Value).Exitoso;
            bool rolesOk = s.Valor.Roles.Count == 1 && s.Valor.TieneRol(c.Rol);
            // Entrada de escritorio (Windows Forms): solo Operario y Supervisor; un ciudadano debe ser rechazado.
            var gui = await auth.IniciarSesionAsync(c.Correo, c.Contrasena);
            bool guiOk = c.Rol == CodigoRol.Ciudadano ? !gui.Exitoso : gui.Exitoso;
            bool ok = propia && bloqueada && rolesOk && guiOk;
            Anotar("Autenticación", c.Clave, ok, $"{c.Correo}: sesión {s.Valor.IdUsuario}, rol {AreasAcceso.NombreRol(c.Rol)}, área propia {(propia ? "sí" : "NO")}, " +
                $"área ajena {(ajena is null ? "n/a" : bloqueada ? "bloqueada" : "PERMITIDA")}, escritorio {(gui.Exitoso ? "permitido" : "rechazado")}");
            if (ok) verificadas[c.Clave] = (c, id);
        }
        // Contraseña incorrecta: debe rechazarse (una sola cuenta, para no bloquearla; luego se inicia sesión bien).
        if (cred.TryGetValue("ciu-02", out var muestra))
        {
            var mal = await auth.IniciarSesionPublicaAsync(muestra.Correo, muestra.Contrasena + "x");
            var bien = await auth.IniciarSesionPublicaAsync(muestra.Correo, muestra.Contrasena);
            Anotar("Autenticación", "contraseña incorrecta", !mal.Exitoso && bien.Exitoso, mal.Exitoso ? "se aceptó una contraseña incorrecta" : "rechazada; la correcta sigue funcionando");
        }

        // ---------------------------------------------------------------- módulos por rol (servicios oficiales)
        foreach (var (clave, v) in verificadas.Where(v => v.Value.C.Rol == CodigoRol.Ciudadano))
        {
            var dep = await servicio.ListarMisDepositosAsync(v.Id);
            var mios = await eco.ConsultarMiosAsync(v.Id);
            long sql = Convert.ToInt64(await Program.EscalarAsync("SELECT NVL(SUM(puntos),0) FROM movimiento_ecopuntos WHERE id_usuario = :u", ("u", v.Id)) ?? 0m);
            long pkg = Convert.ToInt64(await Program.EscalarAsync("SELECT pkg_ecopuntos.saldo(:u) FROM dual", ("u", v.Id)) ?? 0m);
            bool ok = dep.Exitoso && mios.Exitoso && mios.Valor.Saldo == sql && sql == pkg && sql >= 0;
            if (!ok || clave is "ciu-01" or "ciu-21" or "ciu-40")
                Anotar("Ciudadano", clave, ok, $"historial {(dep.Exitoso ? dep.Valor.Count.ToString() : "ERROR")} depósitos; saldo app {(mios.Exitoso ? mios.Valor.Saldo.ToString() : "ERROR")} = SQL {sql} = PKG {pkg}");
        }
        Anotar("Ciudadano", "historial y saldo de los 50", verificadas.Count(v => v.Value.C.Rol == CodigoRol.Ciudadano) == 50 && !R.Any(r => r.Grupo == "Ciudadano" && !r.Ok), "app = SQL = PKG_ECOPUNTOS.saldo, ninguno negativo");
        if (verificadas.Values.FirstOrDefault(v => v.C.Rol == CodigoRol.Operario) is { C: not null } op)
        {
            var pend = await servicio.ListarDepositosSinVerificarAsync(op.Id);
            Anotar("Operario", op.C.Clave + " · depósitos por verificar", pend.Exitoso && pend.Valor.Count > 0, pend.Exitoso ? $"{pend.Valor.Count} pendientes visibles" : pend.Mensaje ?? "");
            var sinPermiso = await cuentasSrv.ListarAsync(op.Id);
            Anotar("Operario", op.C.Clave + " · gestión de usuarios", !sinPermiso.Exitoso, sinPermiso.Exitoso ? "PERMITIDA (no debería)" : "rechazada (solo Supervisor)");
        }
        if (verificadas.Values.FirstOrDefault(v => v.C.Clave == "sup-01") is { C: not null } sup)
        {
            var lista = await cuentasSrv.ListarAsync(sup.Id);
            Anotar("Supervisor", "sup-01 · gestión de usuarios", lista.Exitoso, lista.Exitoso ? $"{lista.Valor.Count} cuentas visibles" : lista.Mensaje ?? "");
        }
        if (verificadas.Values.FirstOrDefault(v => v.C.Rol == CodigoRol.Ciudadano) is { C: not null } ciu)
        {
            var sinPermiso = await servicio.ListarDepositosSinVerificarAsync(ciu.Id);
            Anotar("Ciudadano", ciu.C.Clave + " · lista de verificación del operario", !sinPermiso.Exitoso, sinPermiso.Exitoso ? "PERMITIDA (no debería)" : "rechazada");
        }

        // ---------------------------------------------------------------- integridad (SQL independiente)
        async Task Cero(string prueba, string sql)
        {
            int n = Convert.ToInt32(await Program.EscalarAsync(sql, p) ?? 0m);
            Anotar("Integridad", prueba, n == 0, n == 0 ? "0 casos" : $"{n} casos");
        }
        await Cero("correos duplicados", "SELECT COUNT(*) FROM (SELECT LOWER(correo_electronico) FROM usuario GROUP BY LOWER(correo_electronico) HAVING COUNT(*) > 1)");
        await Cero("cuentas del lote sin rol", $"SELECT COUNT(*) FROM usuario u WHERE u.id_usuario {De("USUARIO")} AND NOT EXISTS (SELECT 1 FROM usuario_rol r WHERE r.id_usuario = u.id_usuario)");
        await Cero("cuentas del lote sin credencial", $"SELECT COUNT(*) FROM usuario u WHERE u.id_usuario {De("USUARIO")} AND NOT EXISTS (SELECT 1 FROM credencial_acceso c WHERE c.id_usuario = u.id_usuario)");
        await Cero("depósitos sin orden, visita o evento", $"SELECT COUNT(*) FROM deposito d WHERE d.id_deposito {De("DEPOSITO")} AND NOT EXISTS (SELECT 1 FROM orden_apertura o JOIN interaccion i ON i.id_interaccion = o.id_interaccion JOIN evento_dispositivo e ON e.id_orden = o.id_orden WHERE o.id_orden = d.id_orden AND e.id_evento = d.id_evento)");
        await Cero("caneca que no admite el material", $"SELECT COUNT(*) FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden WHERE d.id_deposito {De("DEPOSITO")} AND NOT EXISTS (SELECT 1 FROM contenedor_material cm WHERE cm.id_contenedor = o.id_contenedor AND cm.id_material = NVL(d.id_material_verificado, d.id_material_declarado))");
        await Cero("categoría del material distinta de la caneca", $"SELECT COUNT(*) FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN configuracion_caneca k ON k.id_contenedor = o.id_contenedor JOIN material m ON m.id_material = d.id_material_declarado WHERE d.id_deposito {De("DEPOSITO")} AND m.categoria <> k.categoria");
        await Cero("depósitos fuera de las tres canecas oficiales", $"SELECT COUNT(*) FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN contenedor c ON c.id_contenedor = o.id_contenedor WHERE d.id_deposito {De("DEPOSITO")} AND c.id_punto_instalacion <> (SELECT MIN(c2.id_punto_instalacion) FROM registro_simulacion rs JOIN deposito d2 ON d2.id_deposito = rs.id_registro JOIN orden_apertura o2 ON o2.id_orden = d2.id_orden JOIN contenedor c2 ON c2.id_contenedor = o2.id_contenedor WHERE rs.id_lote = :l AND rs.tabla = 'DEPOSITO')");
        await Cero("saldos negativos", "SELECT COUNT(*) FROM (SELECT id_usuario FROM movimiento_ecopuntos GROUP BY id_usuario HAVING SUM(puntos) < 0)");
        await Cero("más de un crédito inicial por depósito", "SELECT COUNT(*) FROM (SELECT id_deposito FROM movimiento_ecopuntos WHERE tipo = 1 AND id_predecesor IS NULL GROUP BY id_deposito HAVING COUNT(*) > 1)");
        await Cero("créditos de depósitos no verificados", "SELECT COUNT(*) FROM movimiento_ecopuntos m JOIN deposito d ON d.id_deposito = m.id_deposito WHERE m.tipo = 1 AND d.id_material_verificado IS NULL");
        await Cero("créditos de depósitos rechazados", "SELECT COUNT(*) FROM movimiento_ecopuntos m WHERE EXISTS (SELECT 1 FROM auditoria_deposito a WHERE a.id_deposito = m.id_deposito AND a.operacion = 'RECHAZO')");
        await Cero("créditos a invitados (sin titular)", "SELECT COUNT(*) FROM movimiento_ecopuntos m JOIN deposito d ON d.id_deposito = m.id_deposito JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion WHERE i.id_usuario IS NULL");
        await Cero("crédito distinto de tarifa × cantidad", "SELECT COUNT(*) FROM movimiento_ecopuntos m JOIN regla_ecopuntos r ON r.id_regla = m.id_regla JOIN deposito d ON d.id_deposito = m.id_deposito WHERE m.tipo = 1 AND m.puntos <> CASE r.por_unidad WHEN 1 THEN r.puntos * d.cantidad_verificada ELSE r.puntos END");
        await Cero("operaciones anteriores al registro del titular", $"SELECT COUNT(*) FROM interaccion i JOIN usuario u ON u.id_usuario = i.id_usuario WHERE i.id_interaccion {De("INTERACCION")} AND i.fecha_inicio_utc < u.fecha_registro_utc");
        await Cero("fechas futuras en el lote", $"SELECT COUNT(*) FROM interaccion i WHERE i.id_interaccion {De("INTERACCION")} AND i.fecha_inicio_utc > SYS_EXTRACT_UTC(SYSTIMESTAMP)");
        await Cero("verificación anterior a la detección", $"SELECT COUNT(*) FROM deposito d WHERE d.id_deposito {De("DEPOSITO")} AND d.fecha_verificacion_utc < d.fecha_deteccion_utc");
        await Cero("órdenes del lote sin cerrar", $"SELECT COUNT(*) FROM orden_apertura o WHERE o.id_interaccion {De("INTERACCION")} AND o.estado BETWEEN 1 AND 5");
        await Cero("supervisores del lote sin actividad", $"SELECT COUNT(*) FROM registro_simulacion rs WHERE rs.id_lote = :l AND rs.tabla = 'USUARIO' AND rs.clave LIKE 'sup-%' AND NOT EXISTS (SELECT 1 FROM deposito d WHERE d.id_verificador = rs.id_registro) AND NOT EXISTS (SELECT 1 FROM operacion_ecopuntos o WHERE o.id_actor = rs.id_registro) AND NOT EXISTS (SELECT 1 FROM auditoria_deposito a WHERE a.id_actor = rs.id_registro)");
        await Cero("operarios del lote sin actividad", $"SELECT COUNT(*) FROM registro_simulacion rs WHERE rs.id_lote = :l AND rs.tabla = 'USUARIO' AND rs.clave LIKE 'op-%' AND NOT EXISTS (SELECT 1 FROM deposito d WHERE d.id_verificador = rs.id_registro) AND NOT EXISTS (SELECT 1 FROM interaccion i WHERE i.id_operario = rs.id_registro)");
        await Cero("depósitos del lote sin trazabilidad (fuera de la vista de estado)", $"SELECT COUNT(*) FROM deposito d WHERE d.id_deposito {De("DEPOSITO")} AND NOT EXISTS (SELECT 1 FROM vista_depositos_estado v WHERE v.id_deposito = d.id_deposito AND v.es_simulacion = 1 AND v.es_ensayo = 1)");
        await Cero("saldo de la vista distinto del libro", "SELECT COUNT(*) FROM (SELECT u.id_usuario, NVL(SUM(m.puntos),0) s FROM usuario u LEFT JOIN movimiento_ecopuntos m ON m.id_usuario = u.id_usuario GROUP BY u.id_usuario) x WHERE x.s <> pkg_ecopuntos.saldo(x.id_usuario)");
        await Cero("FK deshabilitadas u objetos inválidos", "SELECT (SELECT COUNT(*) FROM user_constraints WHERE constraint_type = 'R' AND (status <> 'ENABLED' OR validated <> 'VALIDATED')) + (SELECT COUNT(*) FROM user_objects WHERE status <> 'VALID' AND object_name NOT LIKE 'BIN$%') FROM dual");

        // ---------------------------------------------------------------- cifras del lote
        var sb = new StringBuilder();
        sb.AppendLine($"# Lote {Program.Lote} · verificación ({Program.Perfil}) · {DateTime.Now:yyyy-MM-dd HH:mm}").AppendLine();
        async Task Tabla(string titulo, string sql)
        {
            sb.AppendLine($"## {titulo}").AppendLine();
            var filas = await Program.FilasAsync(sql, p);
            foreach (var f in filas) sb.AppendLine("| " + string.Join(" | ", f.Select(v => v is DateTime d ? d.ToString("yyyy-MM-dd") : Convert.ToString(v))) + " |");
            sb.AppendLine();
        }
        await Tabla("Cuentas por rol", $"SELECT r.descripcion, COUNT(*) FROM usuario u JOIN usuario_rol ur ON ur.id_usuario = u.id_usuario JOIN rol r ON r.id_rol = ur.id_rol WHERE u.id_usuario {De("USUARIO")} GROUP BY r.descripcion ORDER BY 1");
        await Tabla("Depósitos por caneca", $"SELECT color_caneca, COUNT(*), ROUND(100*RATIO_TO_REPORT(COUNT(*)) OVER (), 1) FROM vista_depositos_estado WHERE id_lote = :l GROUP BY color_caneca ORDER BY 2 DESC");
        await Tabla("Depósitos por material", "SELECT material, color_caneca, COUNT(*) FROM vista_depositos_estado WHERE id_lote = :l GROUP BY material, color_caneca ORDER BY 3 DESC");
        await Tabla("Visitas por canal (1 Telegram, 2 Manual, 3 Simulador/invitado, 5 Asistido)", $"SELECT canal, COUNT(*) FROM interaccion WHERE id_interaccion {De("INTERACCION")} GROUP BY canal ORDER BY 1");
        await Tabla("Depósitos por tramo de antigüedad (días)", $"SELECT CASE WHEN d <= 7 THEN '00-07' WHEN d <= 14 THEN '08-14' WHEN d <= 21 THEN '15-21' WHEN d <= 30 THEN '22-30' WHEN d <= 60 THEN '31-60' ELSE '61-90' END, COUNT(*) FROM (SELECT TRUNC(CAST(SYS_EXTRACT_UTC(SYSTIMESTAMP) AS DATE)) - TRUNC(CAST(fecha_deteccion_utc AS DATE)) d FROM deposito WHERE id_deposito {De("DEPOSITO")}) GROUP BY CASE WHEN d <= 7 THEN '00-07' WHEN d <= 14 THEN '08-14' WHEN d <= 21 THEN '15-21' WHEN d <= 30 THEN '22-30' WHEN d <= 60 THEN '31-60' ELSE '61-90' END ORDER BY 1");
        await Tabla("Estado de los depósitos (PKG_DEPOSITO.estado: 0 pendiente, 1 verificado, 2 rechazado)", $"SELECT pkg_deposito.estado(id_deposito), COUNT(*) FROM deposito WHERE id_deposito {De("DEPOSITO")} GROUP BY pkg_deposito.estado(id_deposito) ORDER BY 1");
        await Tabla("Movimientos de ecopuntos (1 crédito, 2 reverso)", $"SELECT tipo, COUNT(*), SUM(puntos) FROM movimiento_ecopuntos WHERE id_deposito {De("DEPOSITO")} GROUP BY tipo ORDER BY 1");
        await Tabla("Ciudadanos del lote por saldo", $"SELECT CASE WHEN s = 0 THEN '0' WHEN s < 30 THEN '1-29' WHEN s < 120 THEN '30-119' ELSE '120+' END, COUNT(*) FROM (SELECT u.id_usuario, NVL(SUM(m.puntos),0) s FROM usuario u LEFT JOIN movimiento_ecopuntos m ON m.id_usuario = u.id_usuario WHERE u.id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = :l AND tabla='USUARIO' AND clave LIKE 'ciu-%') GROUP BY u.id_usuario) GROUP BY CASE WHEN s = 0 THEN '0' WHEN s < 30 THEN '1-29' WHEN s < 120 THEN '30-119' ELSE '120+' END ORDER BY 1");
        await Tabla("Separación: estadísticas reales (es_ensayo = 0) frente a demostración (es_ensayo = 1)", "SELECT es_ensayo, COUNT(*) FROM vista_depositos_estado GROUP BY es_ensayo ORDER BY 1");

        // ---------------------------------------------------------------- escenarios
        var esc = await EscenariosAsync(manifiesto);
        sb.AppendLine("## Escenarios").AppendLine();
        foreach (var e in esc) sb.AppendLine($"- **{e.Titulo}**: `{e.Clave}` ({(cred.TryGetValue(e.Clave, out var cc) ? cc.Correo : "?")}) — {e.Dato}");
        sb.AppendLine().AppendLine("## Pruebas").AppendLine();
        foreach (var r in R) sb.AppendLine($"| {(r.Ok ? "OK" : "FALLA")} | {r.Grupo} | {r.Prueba} | {r.Detalle.Replace("|", "/")} |");
        int fallas = R.Count(r => !r.Ok);
        sb.AppendLine().AppendLine($"**Resultado: {R.Count - fallas} de {R.Count} comprobaciones correctas.**");
        string destino = informe ?? Path.Combine(Program.CarpetaPrivada, $"{Program.Lote}.{Program.Perfil}.verificacion.md");
        File.WriteAllText(destino, sb.ToString(), new UTF8Encoding(false));
        Program.Log($"Informe (sin contraseñas): {destino}");

        // ---------------------------------------------------------------- TXT del escritorio (solo producción y todo verificado)
        bool todas = verificadas.Count == cred.Count && cred.Count == 64;
        if (Program.Perfil == "produccion" && todas && fallas == 0)
        {
            string escritorio = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            EscribirCredenciales(Path.Combine(escritorio, "ECOACCESS_CREDENCIALES_FERIA_2026.txt"), verificadas.Values.Select(v => v.C).ToList());
            EscribirGuia(Path.Combine(escritorio, "ECOACCESS_GUIA_DEMOSTRACION_FERIA.txt"), esc, cred);
            Program.Log($"TXT escritos en {escritorio}.");
        }
        else Program.Log($"TXT del escritorio NO escritos (perfil {Program.Perfil}, cuentas verificadas {verificadas.Count}/{cred.Count}, fallas {fallas}).");
        Program.Log($"Resultado: {R.Count - fallas}/{R.Count} comprobaciones correctas.");
        return fallas == 0 ? 0 : 1;
    }

    internal sealed record Escenario(string Titulo, string Clave, string Dato, string Modulo);

    static async Task<List<Escenario>> EscenariosAsync(Dictionary<string, int> m)
    {
        var p = ("l", (object?)Program.Lote);
        async Task<(string Clave, string Dato)> Max(string sql, string formato)
        {
            var f = (await Program.FilasAsync(sql, p)).FirstOrDefault();
            return f is null ? ("?", "sin datos") : ((string)f[0]!, string.Format(formato, f.Skip(1).ToArray()));
        }
        const string Ciu = "SELECT rs.clave, rs.id_registro FROM registro_simulacion rs WHERE rs.id_lote = :l AND rs.tabla = 'USUARIO' AND rs.clave LIKE 'ciu-%'";
        var activo = await Max($"SELECT c.clave, COUNT(*) FROM ({Ciu}) c JOIN interaccion i ON i.id_usuario = c.id_registro JOIN orden_apertura o ON o.id_interaccion = i.id_interaccion JOIN deposito d ON d.id_orden = o.id_orden GROUP BY c.clave ORDER BY 2 DESC, 1", "{0} depósitos en varias semanas");
        var puntos = await Max($"SELECT c.clave, SUM(mv.puntos) FROM ({Ciu}) c JOIN movimiento_ecopuntos mv ON mv.id_usuario = c.id_registro GROUP BY c.clave ORDER BY 2 DESC, 1", "{0} ecopuntos");
        var hace2 = await Max($"SELECT c.clave, COUNT(*) FROM ({Ciu}) c JOIN interaccion i ON i.id_usuario = c.id_registro JOIN orden_apertura o ON o.id_interaccion = i.id_interaccion JOIN deposito d ON d.id_orden = o.id_orden WHERE c.clave BETWEEN 'ciu-21' AND 'ciu-35' AND d.fecha_deteccion_utc BETWEEN SYS_EXTRACT_UTC(SYSTIMESTAMP) - 15 AND SYS_EXTRACT_UTC(SYSTIMESTAMP) - 13 GROUP BY c.clave ORDER BY 2 DESC, 1", "{0} depósitos hace dos semanas (13 a 15 días)");
        var hace3 = await Max($"SELECT c.clave, COUNT(*) FROM ({Ciu}) c JOIN interaccion i ON i.id_usuario = c.id_registro JOIN orden_apertura o ON o.id_interaccion = i.id_interaccion JOIN deposito d ON d.id_orden = o.id_orden WHERE d.fecha_deteccion_utc BETWEEN SYS_EXTRACT_UTC(SYSTIMESTAMP) - 22 AND SYS_EXTRACT_UTC(SYSTIMESTAMP) - 20 GROUP BY c.clave ORDER BY 2 DESC, 1", "{0} depósitos hace tres semanas (20 a 22 días)");
        var pend = await Max($"SELECT c.clave, COUNT(*) FROM ({Ciu}) c JOIN interaccion i ON i.id_usuario = c.id_registro JOIN orden_apertura o ON o.id_interaccion = i.id_interaccion JOIN deposito d ON d.id_orden = o.id_orden WHERE c.clave = 'ciu-21' AND pkg_deposito.estado(d.id_deposito) = 0 GROUP BY c.clave", "{0} depósitos pendientes de verificación y acreditación");
        var frec = await Max($"SELECT c.clave, COUNT(*) FROM ({Ciu}) c JOIN movimiento_ecopuntos mv ON mv.id_usuario = c.id_registro WHERE c.clave BETWEEN 'ciu-22' AND 'ciu-35' GROUP BY c.clave ORDER BY 2 DESC, 1", "{0} movimientos de ecopuntos e historial semanal");
        var nuevo = await Max($"SELECT c.clave, COUNT(d.id_deposito) FROM ({Ciu}) c LEFT JOIN interaccion i ON i.id_usuario = c.id_registro LEFT JOIN orden_apertura o ON o.id_interaccion = i.id_interaccion LEFT JOIN deposito d ON d.id_orden = o.id_orden WHERE c.clave = 'ciu-01' GROUP BY c.clave", "registrado esta semana; {0} depósito (el primero, pendiente)");
        var oper = await Max("SELECT rs.clave, COUNT(d.id_deposito) FROM registro_simulacion rs LEFT JOIN deposito d ON d.id_verificador = rs.id_registro WHERE rs.id_lote = :l AND rs.tabla = 'USUARIO' AND rs.clave LIKE 'op-%' GROUP BY rs.clave ORDER BY 2 DESC, 1", "{0} depósitos verificados; ve la cola compartida de pendientes");
        var sup = await Max("SELECT rs.clave, (SELECT COUNT(*) FROM deposito d WHERE d.id_verificador = rs.id_registro) + (SELECT COUNT(*) FROM operacion_ecopuntos o WHERE o.id_actor = rs.id_registro) FROM registro_simulacion rs WHERE rs.id_lote = :l AND rs.tabla = 'USUARIO' AND rs.clave LIKE 'sup-%' ORDER BY 2 DESC, 1", "{0} verificaciones y operaciones de ecopuntos");
        return new List<Escenario>
        {
            new("1. Ciudadano recién registrado con su primer depósito", nuevo.Clave, nuevo.Dato, "Inicio ciudadano → Mis depósitos (estado «pendiente»); en vivo puede hacer otro por Telegram con el QR."),
            new("2. Ciudadano frecuente: historial y ecopuntos acumulados", frec.Clave, frec.Dato, "Mis ecopuntos (saldo y movimientos) y Mis estadísticas."),
            new("3. Ciudadano destacado (mayor actividad)", activo.Clave, activo.Dato, "Mis estadísticas: gráfico por semanas, por caneca y por material."),
            new("3b. Ciudadano con más ecopuntos", puntos.Clave, puntos.Dato, "Mis ecopuntos: saldo y créditos por material verificado."),
            new("4. Ciudadano con depósitos pendientes de acreditación", pend.Clave, pend.Dato, "Mis depósitos: varios «pendientes»; el operario los verifica y acredita en vivo."),
            new("5 y 6. Operario: verificar y acreditar por el flujo oficial", oper.Clave, oper.Dato, "Interacciones y depósitos → pendientes → Verificar; luego Ecopuntos → Acreditar."),
            new("7. Supervisor: actividad de operarios y estadísticas", sup.Clave, sup.Dato, "Dashboard institucional, Estadísticas, Usuarios (aclarar que la actividad @feria.ecoaccess.test es sintética)."),
            new("8. Administrador de demostración: gráficos generales", "adm-01", "creó las cuentas del personal del lote", "Dashboard institucional, Estadísticas, Usuarios, Estado de la estación."),
            new("9. Invitado desde canal público", "invitado", "30 depósitos de invitados (sin cuenta, sin ecopuntos)", "En vivo: ECOACCESS DOS CLICS o WEB desde el acceso del anfitrión; no requiere credenciales."),
            new("10. Ciudadano con depósitos de hace dos semanas", hace2.Clave, hace2.Dato, "Mis depósitos, filtrando por fecha."),
            new("10b. Ciudadano con depósitos de hace tres semanas", hace3.Clave, hace3.Dato, "Mis depósitos, filtrando por fecha."),
        };
    }

    static void EscribirCredenciales(string ruta, List<Cuenta> cuentas)
    {
        var sb = new StringBuilder();
        void Seccion(string titulo, IEnumerable<Cuenta> lista)
        {
            sb.AppendLine("==================================================").AppendLine(titulo).AppendLine("==================================================").AppendLine();
            int i = 0;
            foreach (var c in lista) { i++; sb.AppendLine($"{i:00}.").AppendLine($"Correo: {c.Correo}").AppendLine($"Contraseña: {c.Contrasena}").AppendLine(); }
        }
        Seccion("CIUDADANOS", cuentas.Where(c => c.Clave.StartsWith("ciu-")).OrderBy(c => c.Clave));
        Seccion("OPERARIOS", cuentas.Where(c => c.Clave.StartsWith("op-")).OrderBy(c => c.Clave));
        Seccion("SUPERVISORES", cuentas.Where(c => c.Clave.StartsWith("sup-")).OrderBy(c => c.Clave));
        Seccion("ADMINISTRADORES DE DEMOSTRACIÓN", cuentas.Where(c => c.Clave.StartsWith("adm-")).OrderBy(c => c.Clave));
        File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(false));
    }

    static void EscribirGuia(string ruta, List<Escenario> esc, Dictionary<string, Cuenta> cred)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ECOACCESS · GUÍA DE DEMOSTRACIÓN · FERIA 2026").AppendLine($"Lote de demostración: {Program.Lote} (datos sintéticos de @feria.ecoaccess.test)").AppendLine();
        sb.AppendLine("Las contraseñas están en ECOACCESS_CREDENCIALES_FERIA_2026.txt. Importante: el Dashboard institucional y las estadísticas suman el lote con la actividad real (no hay filtro de demostración; ver docs/feria-2026/PENDIENTES.md, P1 y P2). Al presentar, aclara que esas cifras son sintéticas.").AppendLine();
        foreach (var e in esc)
        {
            sb.AppendLine("==================================================").AppendLine(e.Titulo).AppendLine("==================================================");
            sb.AppendLine($"Cuenta: {(cred.TryGetValue(e.Clave, out var c) ? c.Correo : e.Clave)}");
            sb.AppendLine($"Qué abrir: {e.Modulo}");
            sb.AppendLine($"Qué se ve: {e.Dato}").AppendLine();
        }
        File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(false));
    }
}
