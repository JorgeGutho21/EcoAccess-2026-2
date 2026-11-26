using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;

/// <summary>
/// Crea el lote con los servicios oficiales. Todas las decisiones aleatorias salen de una semilla fija y se toman antes
/// de escribir, así que repetir la ejecución recorre el mismo plan y omite lo que ya existe en el manifiesto.
/// </summary>
static class Generador
{
    // Perfiles de los 50 ciudadanos (sección 4 del encargo).
    internal const string Nuevo = "nuevo", Ocasional = "ocasional", Frecuente = "frecuente", Destacado = "destacado", Inactivo = "inactivo";

    static readonly string[] Nombres =
    {
        "Valentina", "Santiago", "Isabella", "Sebastián", "Mariana", "Mateo", "Gabriela", "Nicolás", "Daniela", "Samuel",
        "Camila", "Alejandro", "Sofía", "Juan David", "Laura", "Andrés Felipe", "Paula Andrea", "Julián", "Natalia", "Esteban",
        "Luisa Fernanda", "Cristian", "María José", "Kevin", "Valeria", "Jhon Fredy", "Ana Lucía", "Brayan", "Manuela", "Carlos Andrés",
        "Juliana", "Miguel Ángel", "Sara", "Diego Alejandro", "Ximena", "Yeison", "Karen", "Óscar Iván", "Daniela Sofía", "Luis Eduardo",
        "Melissa", "Jesús David", "Angie", "Duván", "Tatiana", "Wilmer", "Lina Marcela", "Fabián", "Yuliana", "Rafael",
        "Adriana", "Hernán", "Mónica", "Germán", "Claudia", "Álvaro", "Sandra", "Ricardo", "Diana Carolina", "Jairo",
    };
    static readonly string[] Apellidos =
    {
        "Quintero", "Maestre", "Ariza", "Mendoza", "Daza", "Oñate", "Fuentes", "Castro", "Pumarejo", "Araújo", "Villazón", "Gutiérrez",
        "Romero", "Zuleta", "Cuello", "Baquero", "Molina", "Peralta", "Montero", "Arzuaga", "Lacouture", "Calderón", "Orozco", "Rangel",
        "Vega", "Pineda", "Suárez", "Barros", "Carrillo", "Ospino", "Hinojosa", "Mejía", "Rincón", "Salcedo", "Torres", "Durán",
        "Escorcia", "Galván", "Herrera", "Jiménez", "Lozano", "Martínez", "Navarro", "Ortega", "Palencia", "Ruiz", "Sierra", "Ustariz",
    };

    // Mezcla de materiales por caneca (códigos reales del catálogo; solo se usan los que la caneca admite).
    static readonly (int Categoria, string Codigo, int Peso, int MaxUnidades)[] Mezcla =
    {
        (2, "ORGANICO", 55, 2), (2, "RESTOS_ALIMENTOS", 45, 2),
        (1, "PET", 24, 4), (1, "CARTON", 16, 3), (1, "PAPEL", 18, 5), (1, "VIDRIO", 10, 2), (1, "LATA_ALUMINIO", 12, 4),
        (1, "MULTICAPA", 9, 3), (1, "METAL", 4, 2), (1, "PLASTICO_RIGIDO", 4, 2), (1, "BOLSA_PLASTICA", 3, 2),
        (3, "SERVILLETA_USADA", 46, 3), (3, "ICOPOR_CONTAMINADO", 18, 2), (3, "PLASTICO_SIN_ID", 14, 2), (3, "COLILLA", 10, 4),
        (3, "OBJETO_COMPUESTO", 6, 1), (3, "MADERA", 6, 1),
    };

    static readonly string[] MotivosRechazo =
    {
        "Material contaminado con restos de comida: no corresponde a la caneca declarada.",
        "El residuo depositado no coincide con el material declarado.",
        "Depósito vacío: la tapa se abrió pero no se observó residuo.",
        "Envase con líquido: debe vaciarse antes de reciclar.",
    };
    static readonly string[] MotivosManual =
    {
        "Apertura manual para vaciar la caneca y cambiar la bolsa.",
        "Apertura manual: limpieza programada de la estación.",
        "Apertura manual para revisar el estado de la tapa.",
        "Apertura manual: separación en la fuente con estudiantes del curso.",
        "Apertura manual: inspección de contaminación cruzada.",
    };

    sealed record Evento(int K, DateTime Utc, bool Invitado, int Categoria, string Codigo, int Unidades, bool Asistido,
        double Tiro, double TiroCredito, int Verificador, int Acreditador);

    internal sealed class Contexto
    {
        public RepositorioUsuariosOracle Usuarios = null!;
        public RepositorioMaterialesOracle Materiales = null!;
        public RepositorioContenedoresOracle Contenedores = null!;
        public RepositorioInteraccionesOracle Interacciones = null!;
        public RepositorioCredencialesOracle CredencialesRepo = null!;
        public RepositorioEcopuntosOracle Ecopuntos = null!;
        public ServicioInteracciones Servicio = null!;
        public ServicioUsuarios Cuentas = null!;
        public ServicioAutenticacion Autenticacion = null!;
        public ServicioRegistroCiudadanos Registro = null!;
        public ServicioEcopuntos ServicioEcopuntos = null!;
        public Dictionary<int, int> Canecas = new();                // categoría -> id_contenedor
        public Dictionary<string, Material> Catalogo = new(StringComparer.Ordinal);
        public HashSet<(int, int)> Admite = new();                  // (contenedor, material)
        public List<ReglaEcopuntos> Reglas = new();
    }

    public static async Task<int> EjecutarAsync(int? estacion)
    {
        var rel = Program.Reloj;
        var x = new Contexto
        {
            Usuarios = new RepositorioUsuariosOracle(Program.Fabrica),
            Materiales = new RepositorioMaterialesOracle(Program.Fabrica),
            Contenedores = new RepositorioContenedoresOracle(Program.Fabrica),
            Interacciones = new RepositorioInteraccionesOracle(Program.Fabrica, rel),
            CredencialesRepo = new RepositorioCredencialesOracle(Program.Fabrica),
            Ecopuntos = new RepositorioEcopuntosOracle(Program.Fabrica),
        };
        x.Servicio = new ServicioInteracciones(x.Usuarios, x.Interacciones, x.Contenedores, x.Materiales, rel);
        x.Cuentas = new ServicioUsuarios(x.Usuarios, rel);
        x.Autenticacion = new ServicioAutenticacion(x.CredencialesRepo, x.Usuarios, new HasherPbkdf2(), rel);
        x.Registro = new ServicioRegistroCiudadanos(new RepositorioRegistroCiudadanosOracle(Program.Fabrica), new HasherPbkdf2(), rel);
        x.ServicioEcopuntos = new ServicioEcopuntos(x.Usuarios, x.Interacciones, x.Materiales, x.Ecopuntos, rel);

        // 0. Ninguna otra pieza debe leer órdenes de Oracle para mover hardware (bridges HMAC del modo anterior).
        var bridges = System.Diagnostics.Process.GetProcessesByName("EcoAccess.P3.Bot")
            .Count(p => { try { return LineaComando(p.Id).Contains("--bridge"); } catch { return false; } });
        if (bridges > 0) throw new InvalidOperationException("Hay un bridge del ESP32 que lee órdenes de Oracle: deténlo antes de generar el lote.");

        // 1. Lote y fecha de referencia (la de la primera ejecución).
        if (await Program.EscalarAsync("SELECT COUNT(*) FROM lote_simulacion WHERE id_lote = :l", ("l", Program.Lote)) is decimal n0 && n0 == 0)
            await Program.Manifiesto.AsegurarLoteAsync(Program.Lote,
                "Demostración académica Feria 2026: cuentas ficticias, depósitos sintéticos de 90 días y ecopuntos (no son operaciones físicas)",
                Program.SemillaLote, DateTime.UtcNow, default);
        DateTime fin = DateTime.SpecifyKind((DateTime)(await Program.EscalarAsync("SELECT fecha_creacion_utc FROM lote_simulacion WHERE id_lote = :l", ("l", Program.Lote)))!, DateTimeKind.Utc);
        Program.Log($"Fecha de referencia del lote: {Program.UtcALocal(fin):yyyy-MM-dd HH:mm} (hora de Colombia).");

        // 2. Estación oficial y sus tres canecas (sin crear estaciones ni canecas).
        int idEstacion = estacion ?? Convert.ToInt32(await Program.EscalarAsync(
            "SELECT MIN(id_punto_instalacion) FROM punto_instalacion WHERE nombre = 'Estación Oficial EcoAccess'") ?? 0);
        if (idEstacion == 0) throw new InvalidOperationException("No encuentro la Estación Oficial EcoAccess (usa --estacion <id>).");
        foreach (var f in await Program.FilasAsync(
            "SELECT k.categoria, c.id_contenedor, c.estado FROM contenedor c JOIN configuracion_caneca k ON k.id_contenedor = c.id_contenedor " +
            "WHERE c.id_punto_instalacion = :p", ("p", idEstacion)))
        {
            int cat = Convert.ToInt32(f[0]), id = Convert.ToInt32(f[1]), est = Convert.ToInt32(f[2]);
            if (est != 1) throw new InvalidOperationException($"La caneca {id} (categoría {cat}) no está en servicio; no se cambia su estado.");
            x.Canecas[cat] = id;
        }
        if (x.Canecas.Count != 3) throw new InvalidOperationException($"La estación {idEstacion} no tiene exactamente blanca, verde y negra configuradas.");
        Program.Log($"Estación {idEstacion}: blanca {x.Canecas[1]}, verde {x.Canecas[2]}, negra {x.Canecas[3]}.");
        foreach (var m in await x.Materiales.ListarAsync(true, default)) x.Catalogo[m.Codigo] = m;
        foreach (var f in await Program.FilasAsync("SELECT id_contenedor, id_material FROM contenedor_material WHERE id_contenedor IN (:a, :b, :c)",
                     ("a", x.Canecas[1]), ("b", x.Canecas[2]), ("c", x.Canecas[3])))
            x.Admite.Add((Convert.ToInt32(f[0]), Convert.ToInt32(f[1])));
        x.Reglas = (await x.Ecopuntos.ListarReglasAsync(default)).ToList();

        // Las órdenes vencidas que quedaron sin cerrar bloquean la caneca (UQ_ORDEN_VIGENTE): se vencen con el reloj REAL,
        // exactamente como lo hace cualquier solicitud nueva de la aplicación.
        var real = new ServicioInteracciones(x.Usuarios, new RepositorioInteraccionesOracle(Program.Fabrica), x.Contenedores, x.Materiales, TimeProvider.System);
        foreach (int c in x.Canecas.Values) { int v = await real.VencerOrdenesAsync(default, c); if (v > 0) Program.Log($"Caneca {c}: {v} orden(es) vencida(s) cerrada(s) con el reloj real."); }

        // 3. Cuentas.
        var azar = new Random(Program.SemillaLote);
        var guardadas = Credenciales.Leer();
        var cuentas = await CuentasAsync(x, fin, azar, guardadas);
        var ids = cuentas.ToDictionary(c => c.Key, c => c.Value.Id);
        int[] operarios = Enumerable.Range(1, 8).Select(i => ids[$"op-{i:00}"]).ToArray();
        int[] supervisores = Enumerable.Range(1, 4).Select(i => ids[$"sup-{i:00}"]).Concat(new[] { ids["adm-01"], ids["adm-02"] }).ToArray();

        // 4. Plan de depósitos (650) con la distribución pedida por tramos de días.
        var plan = Planificar(fin, azar, x);
        var ciudadanos = Enumerable.Range(1, 50).Select(i => (Clave: $"ciu-{i:00}", cuentas[$"ciu-{i:00}"].Id, cuentas[$"ciu-{i:00}"].Perfil, cuentas[$"ciu-{i:00}"].Registro)).ToArray();
        var asignado = Asignar(plan, ciudadanos, fin, azar);

        // 5. Registros manuales del operario (canal Manual, sin depósito): 40 en los 90 días.
        var manuales = Enumerable.Range(1, 40).Select(i =>
        {
            DateTime t = Hora(Program.UtcALocal(fin).Date.AddDays(-azar.Next(1, 88)), azar, fin);
            return (K: i, Utc: t, Operario: operarios[azar.Next(operarios.Length)], Categoria: 1 + azar.Next(3), Motivo: MotivosManual[azar.Next(MotivosManual.Length)]);
        }).ToList();

        int creados = 0, omitidos = 0, fallos = 0, verificados = 0, rechazados = 0, creditos = 0, invitados = 0;
        var pendientesCredito = new List<(int Deposito, int Regla, DateTime Verificado, int Acreditador, string Clave)>();
        var escenarioPendientes = ids["ciu-21"];   // ESCENARIO 4: ciudadano frecuente con depósitos pendientes de acreditación

        foreach (var ev in plan.OrderBy(e => e.Utc))
        {
            string clave = $"dep-{ev.K:000}";
            if (await Program.Manifiesto.BuscarAsync(Program.Lote, clave, default) is not null) { omitidos++; continue; }
            if (await Program.Manifiesto.BuscarAsync(Program.Lote, "int-" + ev.K.ToString("000"), default) is not null)
            { Program.Log($"{clave}: quedó incompleto en una ejecución anterior; se omite (su visita está en el manifiesto)."); omitidos++; continue; }
            int caneca = x.Canecas[ev.Categoria];
            Material material = x.Catalogo[ev.Codigo];
            int? titular = ev.Invitado ? null : asignado[ev.K];
            rel.Utc = ev.Utc;
            Resultado<int> inicio;
            int? operadorAsistido = null;
            if (titular is null) inicio = await x.Servicio.IniciarVisitanteAsync(caneca, default);
            else if (ev.Asistido)
            {
                operadorAsistido = operarios[ev.Verificador % operarios.Length];
                inicio = await x.Servicio.RegistrarAsistidoAsync(operadorAsistido.Value, titular.Value, caneca, material.Id, default);
            }
            else inicio = await x.Servicio.IniciarConCantidadAsync(titular.Value, caneca, CanalInteraccion.Telegram, ev.Unidades, default);
            if (!inicio.Exitoso) { Program.Log($"{clave}: inicio rechazado: {inicio.Mensaje}"); fallos++; continue; }

            int idOrden, idInteraccion;
            if (ev.Asistido && titular is not null)
            {
                idOrden = inicio.Valor;
                idInteraccion = Convert.ToInt32(await Program.EscalarAsync("SELECT id_interaccion FROM orden_apertura WHERE id_orden = :o", ("o", idOrden)));
            }
            else
            {
                idInteraccion = inicio.Valor;
                await Program.Manifiesto.RegistrarAsync(Program.Lote, "INTERACCION", idInteraccion, "int-" + ev.K.ToString("000"), default);
                var orden = titular is null
                    ? await x.Servicio.AutorizarAperturaVisitanteAsync(idInteraccion, material.Id, $"{Program.Lote}-{clave}", default)
                    : await x.Servicio.AutorizarAperturaAsync(titular.Value, idInteraccion, material.Id, $"{Program.Lote}-{clave}", default);
                if (!orden.Exitoso) { Program.Log($"{clave}: autorización rechazada: {orden.Mensaje}"); fallos++; continue; }
                idOrden = orden.Valor;
            }
            if (ev.Asistido && titular is not null)
                await Program.Manifiesto.RegistrarAsync(Program.Lote, "INTERACCION", idInteraccion, "int-" + ev.K.ToString("000"), default);

            // Tapa simulada en memoria (nunca el panel ni el ESP32). Sin báscula: el depósito queda «declarado sin sensor».
            var tapa = new CanecaSimulada(caneca, x.Servicio.RegistrarEventoAsync, rel) { Escenario = EscenarioCaneca.SinSensor };
            var despacho = new ServicioDespachoOrdenes(x.Interacciones, x.Servicio, tapa, rel);
            if (await despacho.DespacharAsync(idOrden, default) != ResultadoDespacho.Enviada) { Program.Log($"{clave}: la orden no se despachó."); fallos++; continue; }
            rel.Utc = ev.Utc.AddSeconds(8);
            var eventos = await tapa.EjecutarAsync(idOrden, default);
            if (eventos.Any(e => !e.Exitoso)) { Program.Log($"{clave}: evento rechazado: {eventos.First(e => !e.Exitoso).Mensaje}"); fallos++; continue; }
            Deposito? deposito = await x.Interacciones.ObtenerDepositoPorOrdenAsync(idOrden, default);
            if (deposito is null) { Program.Log($"{clave}: sin depósito."); fallos++; continue; }
            await Program.Manifiesto.RegistrarAsync(Program.Lote, "DEPOSITO", deposito.Id, clave, default);
            creados++;
            if (titular is null) invitados++;

            // Verificación, rechazo o pendiente, según la antigüedad del depósito.
            double dias = (fin - ev.Utc).TotalDays;
            double pVerif = dias < 2 ? 0.30 : dias < 14 ? 0.66 : 0.86;
            double pRech = dias < 2 ? 0.04 : 0.08;
            bool pendienteForzado = (titular == escenarioPendientes && dias < 15) || (titular == ids["ciu-01"]);
            int verificador = ev.Verificador < 90 ? operarios[ev.Verificador % operarios.Length] : supervisores[ev.Verificador % supervisores.Length];
            DateTime cuando = Siguiente(ev.Utc.AddHours(1 + ev.Tiro * 30), fin);
            if (pendienteForzado || cuando >= fin.AddMinutes(-10)) continue;
            if (ev.Tiro < pRech)
            {
                await Program.EjecutarAsync("BEGIN pkg_deposito.rechazar(:d, :v, :m); END;",
                    ("d", deposito.Id), ("v", verificador), ("m", MotivosRechazo[ev.K % MotivosRechazo.Length]));
                rechazados++;
                continue;
            }
            if (ev.Tiro >= pRech + pVerif) continue;   // queda pendiente de verificación
            rel.Utc = cuando;
            var ver = await x.Servicio.VerificarDepositoAsync(verificador, deposito.Id, material.Id, default, ev.Unidades);
            if (!ver.Exitoso) { Program.Log($"{clave}: verificación rechazada: {ver.Mensaje}"); continue; }
            verificados++;

            // Ecopuntos: solo con tarifa vigente del material verificado y titular con cuenta (reglas oficiales).
            if (titular is null) continue;
            var regla = x.Reglas.Where(r => r.IdMaterial == material.Id).OrderByDescending(r => r.DesdeUtc).FirstOrDefault();
            if (regla is null) continue;
            bool dejarPendiente = dias < 10 && ev.TiroCredito < 0.18;
            if (dejarPendiente) continue;
            DateTime alAcreditar = regla.EstaVigente(cuando) ? Siguiente(cuando.AddMinutes(20 + ev.TiroCredito * 300), fin)
                // Verificado antes de existir la tarifa: el operario acredita el rezago después de su aprobación (11/11).
                : Siguiente(regla.DesdeUtc.AddHours(16 + ev.TiroCredito * 70), fin);
            if (alAcreditar >= fin.AddMinutes(-5) || !regla.EstaVigente(alAcreditar)) continue;
            pendientesCredito.Add((deposito.Id, regla.Id, alAcreditar, operarios[ev.Acreditador % operarios.Length], $"{Program.Lote}-cred-{clave}"));
            if (creados % 25 == 0) Program.Log($"Avance: {creados} depósitos creados ({omitidos} ya existían, {fallos} rechazados por las reglas).");
        }

        // 6. Acreditaciones en orden cronológico con el servicio oficial (clave idempotente por depósito).
        foreach (var p in pendientesCredito.OrderBy(p => p.Verificado))
        {
            rel.Utc = p.Verificado;
            var r = await x.ServicioEcopuntos.AcreditarAsync(p.Acreditador, p.Deposito, p.Regla, p.Clave, default);
            if (r.Exitoso) creditos++; else Program.Log($"Crédito {p.Clave}: {r.Mensaje}");
        }

        // 7. Reversos de supervisión (deducciones con motivo), sobre créditos del lote de hace más de una semana.
        int reversos = 0;
        var candidatos = await Program.FilasAsync(
            "SELECT m.id_movimiento, MAX(o.fecha_utc) FROM movimiento_ecopuntos m JOIN operacion_ecopuntos o ON o.id_operacion = m.id_operacion " +
            "JOIN registro_simulacion rs ON rs.tabla = 'DEPOSITO' AND rs.id_registro = m.id_deposito AND rs.id_lote = :l " +
            "WHERE m.tipo = 1 AND NOT EXISTS (SELECT 1 FROM movimiento_ecopuntos r WHERE r.id_credito_revertido = m.id_movimiento) " +
            "AND o.fecha_utc < :tope GROUP BY m.id_movimiento ORDER BY m.id_movimiento", ("l", Program.Lote), ("tope", fin.AddDays(-7)));
        var yaRevertidos = Convert.ToInt32(await Program.EscalarAsync(
            "SELECT COUNT(*) FROM operacion_ecopuntos WHERE clave LIKE :c", ("c", Program.Lote + "-rev-%")) ?? 0);
        for (int i = 0; i < candidatos.Count && yaRevertidos + reversos < 5; i += Math.Max(1, candidatos.Count / 5))
        {
            int credito = Convert.ToInt32(candidatos[i][0]);
            rel.Utc = Siguiente(((DateTime)candidatos[i][1]!).AddDays(2), fin);
            var r = await x.ServicioEcopuntos.RevertirAsync(supervisores[i % 4], credito, $"{Program.Lote}-rev-{credito}",
                "Revisión de supervisión: el material verificado estaba contaminado; se anulan los ecopuntos.", default);
            if (r.Exitoso) reversos++; else Program.Log($"Reverso {credito}: {r.Mensaje}");
        }

        // 8. Registros manuales del operario.
        int manualesNuevos = 0;
        foreach (var m in manuales)
        {
            string clave = $"man-{m.K:00}";
            if (await Program.Manifiesto.BuscarAsync(Program.Lote, clave, default) is not null) continue;
            rel.Utc = m.Utc;
            var r = await x.Servicio.RegistrarAperturaManualAsync(m.Operario, x.Canecas[m.Categoria], m.Motivo, default);
            if (!r.Exitoso) { Program.Log($"{clave}: {r.Mensaje}"); continue; }
            await Program.Manifiesto.RegistrarAsync(Program.Lote, "INTERACCION", r.Valor, clave, default);
            manualesNuevos++;
        }

        await Program.Manifiesto.CompletarAsync(Program.Lote, default);
        Program.Log($"Lote {Program.Lote}: {creados} depósitos nuevos ({invitados} de invitados), {omitidos} ya existían, {fallos} rechazados por las reglas; " +
                    $"{verificados} verificados, {rechazados} rechazados por operarios, {creditos} créditos, {reversos} reversos, {manualesNuevos} registros manuales.");
        return fallos > 30 ? 1 : 0;
    }

    // ------------------------------------------------------------------ cuentas
    internal sealed record Creada(int Id, string Perfil, DateTime Registro);

    static async Task<Dictionary<string, Creada>> CuentasAsync(Contexto x, DateTime fin, Random azar, Dictionary<string, Cuenta> guardadas)
    {
        var res = new Dictionary<string, Creada>(StringComparer.Ordinal);
        var usados = new HashSet<string>(StringComparer.Ordinal);
        // Lista fija de personas (nombre, apellido, perfil, días desde el registro): se calcula siempre igual.
        var personas = new List<(string Clave, CodigoRol Rol, string Nombre, string Perfil, DateTime Registro)>();
        string NombreUnico()
        {
            while (true)
            {
                string n = $"{Nombres[azar.Next(Nombres.Length)]} {Apellidos[azar.Next(Apellidos.Length)]} {Apellidos[azar.Next(Apellidos.Length)]}";
                if (usados.Add(n)) return n;
            }
        }
        DateTime Dia(double hace) => Hora(Program.UtcALocal(fin.AddDays(-hace)).Date, azar, fin);
        personas.Add(("adm-01", CodigoRol.Administrador, NombreUnico(), "administrador", Dia(104)));
        personas.Add(("adm-02", CodigoRol.Administrador, NombreUnico(), "administrador", Dia(103)));
        for (int i = 1; i <= 4; i++) personas.Add(($"sup-{i:00}", CodigoRol.Administrador, NombreUnico(), "supervisor", Dia(102)));
        for (int i = 1; i <= 8; i++) personas.Add(($"op-{i:00}", CodigoRol.Operario, NombreUnico(), "operario", Dia(101)));
        for (int i = 1; i <= 50; i++)
        {
            string perfil = i <= 8 ? Nuevo : i <= 20 ? Ocasional : i <= 35 ? Frecuente : i <= 45 ? Destacado : Inactivo;
            double hace = perfil switch
            {
                Nuevo => 0.6 + azar.NextDouble() * 5.5,
                Ocasional => 20 + azar.NextDouble() * 70,
                _ => 92 + azar.NextDouble() * 6,
            };
            personas.Add(($"ciu-{i:00}", CodigoRol.Ciudadano, NombreUnico(), perfil, Dia(hace)));
        }

        int? actor = null;
        foreach (var p in personas)
        {
            int? existente = await Program.Manifiesto.BuscarAsync(Program.Lote, p.Clave, default);
            DateTime registro = p.Registro;
            if (existente is { } id)
            {
                if (!guardadas.ContainsKey(p.Clave)) Program.Log($"AVISO: {p.Clave} existe pero no está en el archivo local de credenciales.");
                registro = DateTime.SpecifyKind((DateTime)(await Program.EscalarAsync("SELECT fecha_registro_utc FROM usuario WHERE id_usuario = :u", ("u", id)))!, DateTimeKind.Utc);
                res[p.Clave] = new Creada(id, p.Perfil, registro);
                if (p.Clave == "adm-01") actor = id;
                continue;
            }
            string usuario = Correo(p.Nombre, p.Clave);
            string contrasena = Credenciales.Generar(p.Rol == CodigoRol.Ciudadano ? 14 : 16);
            Program.Reloj.Utc = registro;
            int nuevo;
            if (p.Rol == CodigoRol.Ciudadano)
            {
                var r = await x.Registro.RegistrarAsync(p.Nombre, usuario, contrasena, contrasena, true, default);
                if (!r.Exitoso) throw new InvalidOperationException($"{p.Clave}: registro rechazado: {r.Mensaje}");
                nuevo = r.Valor;
            }
            else if (actor is null)
            {
                // Primera cuenta del lote (administrador de demostración): mismas inserciones y mismo hash que el servicio
                // oficial; las demás cuentas del personal las crea esta cuenta con ServicioUsuarios.
                nuevo = await x.Usuarios.AgregarAsync(new Usuario(p.Nombre, usuario, p.Rol, registro), default);
                var h = new HasherPbkdf2().Calcular(contrasena);
                await x.CredencialesRepo.AgregarAsync(new CredencialAcceso(nuevo, h.Hash, h.Sal, h.Iteraciones, registro), default);
            }
            else
            {
                var r = await x.Cuentas.RegistrarAsync(actor.Value, p.Nombre, usuario, p.Rol, default);
                if (!r.Exitoso) throw new InvalidOperationException($"{p.Clave}: alta rechazada: {r.Mensaje}");
                nuevo = r.Valor;
                var c = await x.Autenticacion.EstablecerContrasenaInicialAsync(actor.Value, nuevo, contrasena, default);
                if (!c.Exitoso) throw new InvalidOperationException($"{p.Clave}: contraseña rechazada: {c.Mensaje}");
            }
            await Program.Manifiesto.RegistrarAsync(Program.Lote, "USUARIO", nuevo, p.Clave, default);
            Credenciales.Agregar(new Cuenta(p.Clave, p.Rol, usuario, contrasena, p.Nombre, p.Perfil));
            if (p.Clave == "adm-01") actor = nuevo;
            res[p.Clave] = new Creada(nuevo, p.Perfil, registro);
            Program.Log($"Cuenta {p.Clave} ({p.Perfil}) creada: id {nuevo}.");
        }
        return res;
    }

    static string Correo(string nombre, string clave)
    {
        string Ascii(string s)
        {
            var b = new StringBuilder();
            foreach (char ch in s.Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) b.Append(ch);
            return b.ToString().ToLowerInvariant();
        }
        var partes = nombre.Split(' ');
        string primero = Ascii(partes[0]), apellido = Ascii(partes[partes.Length - 2]);
        return $"{primero}.{apellido}.{clave.Replace("-", "")}@{Program.DominioCorreo}";
    }

    // ------------------------------------------------------------------ plan temporal
    static List<Evento> Planificar(DateTime fin, Random azar, Contexto x)
    {
        // (desde, hasta, cantidad) en días hacia atrás; 650 en total.
        var tramos = new (int Desde, int Hasta, int Cantidad)[] { (0, 7, 100), (8, 14, 110), (15, 21, 95), (22, 30, 95), (31, 60, 150), (61, 90, 100) };
        var materiales = Mezcla.Where(m => x.Catalogo.TryGetValue(m.Codigo, out var mat) && x.Admite.Contains((x.Canecas[m.Categoria], mat.Id))).ToArray();
        var lista = new List<Evento>();
        int k = 0;
        foreach (var t in tramos)
        {
            for (int i = 0; i < t.Cantidad; i++)
            {
                // Días fijos que piden los escenarios (hoy, ayer, hace 3 días, 1, 2 y 3 semanas, 1, 2 y 3 meses).
                int dia = i switch
                {
                    0 when t.Desde == 0 => 0, 1 when t.Desde == 0 => 1, 2 when t.Desde == 0 => 3, 3 when t.Desde == 0 => 7,
                    0 when t.Desde == 8 => 14, 0 when t.Desde == 15 => 21, 0 when t.Desde == 22 => 30, 0 when t.Desde == 31 => 60, 0 when t.Desde == 61 => 90,
                    _ => t.Desde == 0 ? 1 + azar.Next(7) : t.Desde + azar.Next(t.Hasta - t.Desde + 1),
                };
                DateTime utc = Hora(Program.UtcALocal(fin).Date.AddDays(-dia), azar, fin);
                int cat = Tirar(azar, new[] { (1, 35), (2, 45), (3, 20) });
                var opciones = materiales.Where(m => m.Categoria == cat).ToArray();
                var mat = opciones[Tirar(azar, opciones.Select((m, j) => (j, m.Peso)).ToArray())];
                k++;
                lista.Add(new Evento(k, utc, Invitado: azar.NextDouble() < 0.0462, cat, mat.Codigo, 1 + azar.Next(mat.MaxUnidades),
                    Asistido: azar.NextDouble() < 0.28, Tiro: azar.NextDouble(), TiroCredito: azar.NextDouble(), Verificador: azar.Next(100), Acreditador: azar.Next(100)));
            }
        }
        // Exactamente 30 invitados: se ajusta con la misma semilla.
        var inv = lista.Where(e => e.Invitado).ToList();
        while (inv.Count > 30) { var e = inv[azar.Next(inv.Count)]; inv.Remove(e); lista[e.K - 1] = e with { Invitado = false }; }
        var noInv = lista.Where(e => !e.Invitado).ToList();
        while (inv.Count < 30) { var e = noInv[azar.Next(noInv.Count)]; noInv.Remove(e); var n = e with { Invitado = true, Asistido = false }; lista[e.K - 1] = n; inv.Add(n); }
        return lista.Select(e => e.Invitado ? e with { Asistido = false } : e).ToList();
    }

    /// <summary>Asigna cada depósito a un ciudadano según su perfil, sin operaciones anteriores a su registro.</summary>
    static Dictionary<int, int?> Asignar(List<Evento> plan, (string Clave, int Id, string Perfil, DateTime Registro)[] ciudadanos, DateTime fin, Random azar)
    {
        var res = new Dictionary<int, int?>();
        var conteo = ciudadanos.ToDictionary(c => c.Id, _ => 0);
        // Los nuevos 01 a 04 tienen exactamente su primer depósito (01 pendiente, para el escenario 1); 05 a 08, ninguno.
        var reservados = new HashSet<int>();
        foreach (var c in ciudadanos.Where(c => c.Perfil == Nuevo).Take(4))
        {
            var e = plan.Where(p => !p.Invitado && !reservados.Contains(p.K) && p.Utc > c.Registro.AddHours(2)).OrderBy(p => p.Utc).FirstOrDefault();
            if (e is null) continue;
            reservados.Add(e.K); res[e.K] = c.Id; conteo[c.Id]++;
        }
        foreach (var e in plan.Where(p => !p.Invitado && !reservados.Contains(p.K)))
        {
            double antig = (fin - e.Utc).TotalDays;
            var cand = ciudadanos.Where(c => c.Perfil != Nuevo && e.Utc > c.Registro.AddHours(2) && (c.Perfil != Inactivo || antig > 21))
                .Select(c => (c.Id, Peso: c.Perfil switch
                {
                    Destacado => 12.0,
                    Frecuente => 5.0,
                    Inactivo => 4.0,
                    _ => conteo[c.Id] >= 6 ? 0.2 : 1.3,
                })).ToArray();
            double total = cand.Sum(c => c.Peso), tiro = azar.NextDouble() * total;
            int elegido = cand[cand.Length - 1].Id;
            foreach (var c in cand) { if (tiro < c.Peso) { elegido = c.Id; break; } tiro -= c.Peso; }
            res[e.K] = elegido; conteo[elegido]++;
        }
        return res;
    }

    /// <summary>Hora local universitaria (7:00 a 19:59), más actividad entre semana y al final de la mañana y de la tarde.</summary>
    internal static DateTime Hora(DateTime diaLocal, Random azar, DateTime fin)
    {
        var horas = new (int, int)[] { (7, 3), (8, 6), (9, 8), (10, 10), (11, 10), (12, 7), (13, 5), (14, 8), (15, 9), (16, 8), (17, 6), (18, 4), (19, 2) };
        DateTime local = diaLocal.Date;
        int h = Tirar(azar, horas);
        var t = Program.LocalAUtc(local.AddHours(h).AddMinutes(azar.Next(60)).AddSeconds(azar.Next(60)));
        if (t >= fin.AddMinutes(-15))
        {
            // Hoy, antes de la hora de referencia: un momento real entre la medianoche local y la referencia.
            DateTime inicio = Program.LocalAUtc(Program.UtcALocal(fin).Date);
            double rango = Math.Max(5, (fin.AddMinutes(-20) - inicio).TotalMinutes);
            t = inicio.AddMinutes(azar.NextDouble() * rango);
            if (t >= fin.AddMinutes(-15)) t = fin.AddMinutes(-30);
        }
        return t;
    }

    /// <summary>Lleva un instante a la jornada laboral siguiente (7:00 a 18:00 local) sin pasar la referencia.</summary>
    static DateTime Siguiente(DateTime utc, DateTime fin)
    {
        DateTime l = Program.UtcALocal(utc);
        if (l.Hour < 7) l = l.Date.AddHours(7).AddMinutes(l.Minute);
        else if (l.Hour >= 18) l = l.Date.AddDays(1).AddHours(7).AddMinutes(l.Minute);
        var r = Program.LocalAUtc(l);
        return r < fin ? r : fin.AddMinutes(-3);
    }

    static int Tirar(Random azar, (int Valor, int Peso)[] opciones)
    {
        int total = opciones.Sum(o => o.Peso), t = azar.Next(total);
        foreach (var o in opciones) { if (t < o.Peso) return o.Valor; t -= o.Peso; }
        return opciones[opciones.Length - 1].Valor;
    }

    static string LineaComando(int pid)
    {
        using var s = new System.Management.ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {pid}");
        foreach (System.Management.ManagementObject o in s.Get()) return Convert.ToString(o["CommandLine"]) ?? "";
        return "";
    }
}
