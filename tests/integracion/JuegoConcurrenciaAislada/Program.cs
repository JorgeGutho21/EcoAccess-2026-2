// Arnés de concurrencia y seguridad del juego contra PKG_ECO_JUEGO en el ESQUEMA AISLADO local (nunca producción).
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

var secretos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", "ecoaccess-p3-pruebas", "secrets.json");
string cadena = JsonNode.Parse(File.ReadAllText(secretos))!["ConnectionStrings:OracleP3"]!.GetValue<string>();
var b = new OracleConnectionStringBuilder(cadena);
if (!b.UserID.ToUpperInvariant().EndsWith("_PRUEBAS") || !(b.DataSource.StartsWith("localhost") || b.DataSource.StartsWith("127.0.0.1")))
    throw new Exception("Destino no aislado: se rechaza");
b.MaxPoolSize = 130; b.MinPoolSize = 0; b.ConnectionTimeout = 60;
cadena = b.ConnectionString;

int[] tamaños = args.Length > 0 ? args[0].Split(',').Select(int.Parse).ToArray() : new[] { 10, 25, 50, 100 };
var salida = new List<string>();
void Log(string s) { Console.WriteLine(s); salida.Add(s); }

// ---------- utilidades ----------
(string json, int http) Llamar(OracleConnection c, string proc, params object?[] entradas)
{
    using var cmd = c.CreateCommand();
    cmd.CommandText = "pkg_eco_juego." + proc; cmd.CommandType = System.Data.CommandType.StoredProcedure; cmd.BindByName = false;
    foreach (var e in entradas)
        cmd.Parameters.Add(e is string s && s.StartsWith("{") ? new OracleParameter { OracleDbType = OracleDbType.Clob, Value = s }
                                                               : new OracleParameter { OracleDbType = OracleDbType.Varchar2, Value = e ?? DBNull.Value });
    var pj = cmd.Parameters.Add("j", OracleDbType.Clob, System.Data.ParameterDirection.Output);
    var ph = cmd.Parameters.Add("h", OracleDbType.Decimal, System.Data.ParameterDirection.Output);
    cmd.ExecuteNonQuery();
    string json = ((OracleClob)pj.Value).IsNull ? "" : ((OracleClob)pj.Value).Value;
    return (json, (int)((OracleDecimal)ph.Value).Value);
}
object? Escalar(string sql, params (string, object)[] p)
{
    using var c = new OracleConnection(cadena); c.Open();
    using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.BindByName = true;
    foreach (var (n, v) in p) cmd.Parameters.Add(n, v);
    return cmd.ExecuteScalar();
}
void Ejecutar(string sql, params (string, object)[] p)
{
    using var c = new OracleConnection(cadena); c.Open();
    using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.BindByName = true;
    foreach (var (n, v) in p) cmd.Parameters.Add(n, v);
    cmd.ExecuteNonQuery();
}
static string Sesion() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
static double Pct(List<double> v, double p) { if (v.Count == 0) return 0; var s = v.OrderBy(x => x).ToList(); int i = (int)Math.Ceiling(p / 100 * s.Count) - 1; return s[Math.Clamp(i, 0, s.Count - 1)]; }

// Clave de respuestas SOLO en el esquema aislado: la opción de menor id de cada pregunta es la correcta.
Ejecutar("UPDATE eco_juego_opciones o SET es_correcta = CASE WHEN o.id = (SELECT MIN(x.id) FROM eco_juego_opciones x WHERE x.pregunta_id = o.pregunta_id) THEN 'S' ELSE 'N' END");
var correctas = new HashSet<long>();
using (var c = new OracleConnection(cadena)) { c.Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT id FROM eco_juego_opciones WHERE es_correcta='S'"; using var r = cmd.ExecuteReader(); while (r.Read()) correctas.Add(r.GetInt64(0)); }

string NuevaRonda(string etiqueta)
{
    string codigo = $"AISLADO-{etiqueta}-{DateTime.UtcNow:HHmmssfff}";
    Ejecutar("INSERT INTO eco_juego_competencias (codigo, nombre, estado, es_prueba, abierta_en) VALUES (:c, :n, 'ABIERTA', 'S', SYSTIMESTAMP)", ("c", codigo), ("n", "Prueba aislada " + etiqueta));
    return codigo;
}

// ---------- pruebas funcionales y de seguridad ----------
int ok = 0, fallo = 0;
void Ver(bool cond, string texto) { if (cond) ok++; else fallo++; Log((cond ? "OK    " : "FALLO ") + texto); }
{
    string ronda = NuevaRonda("SEG");
    using var c = new OracleConnection(cadena); c.Open();
    string s1 = Sesion();
    var reg = Llamar(c, "registrar", $"{{\"nombre\":\"Ana Prueba\",\"alias\":\"anaprueba\",\"acepta\":true,\"sesion\":\"{s1}\",\"ronda\":\"{ronda}\"}}");
    Ver(reg.http == 201, $"registro nuevo → 201 ({reg.http})");
    var reg2 = Llamar(c, "registrar", $"{{\"nombre\":\"Ana Prueba\",\"alias\":\"anaprueba\",\"acepta\":true,\"sesion\":\"{s1}\",\"ronda\":\"{ronda}\"}}");
    Ver(reg2.http == 200, $"reintento del mismo registro es idempotente → 200 ({reg2.http})");
    var aliasDup = Llamar(c, "registrar", $"{{\"nombre\":\"Otra Persona\",\"alias\":\"AnaPrueba\",\"acepta\":true,\"sesion\":\"{Sesion()}\",\"ronda\":\"{ronda}\"}}");
    Ver(aliasDup.http == 409, $"alias duplicado (sin distinguir mayúsculas) → 409 ({aliasDup.http})");
    var sinAviso = Llamar(c, "registrar", $"{{\"nombre\":\"Luis Prueba\",\"alias\":\"luis1\",\"acepta\":false,\"sesion\":\"{Sesion()}\",\"ronda\":\"{ronda}\"}}");
    Ver(sinAviso.http == 400, $"sin aceptar el aviso → 400 ({sinAviso.http})");
    var inyeccion = Llamar(c, "registrar", $"{{\"nombre\":\"Robert'); DROP TABLE x;--\",\"alias\":\"<script>\",\"acepta\":true,\"sesion\":\"{Sesion()}\",\"ronda\":\"{ronda}\"}}");
    Ver(inyeccion.http == 400, $"nombre y alias con inyección/HTML → 400 ({inyeccion.http})");
    var malJson = Llamar(c, "registrar", "{no es json");
    Ver(malJson.http == 400, $"JSON inválido → 400 ({malJson.http})");
    var sesionMala = Llamar(c, "consultar", "corta");
    Ver(sesionMala.http == 401, $"sesión con formato inválido → 401 ({sesionMala.http})");
    var sesionAjena = Llamar(c, "consultar", Sesion());
    Ver(sesionAjena.http == 401, $"sesión desconocida → 401 ({sesionAjena.http})");
    var antes = Llamar(c, "responder", s1, "{\"pregunta\":1,\"opcion\":1}");
    Ver(antes.http == 409, $"responder sin iniciar → 409 ({antes.http})");
    var ini = Llamar(c, "iniciar", s1);
    var j = JsonNode.Parse(ini.json)!;
    Ver(ini.http == 200 && j["estado"]!.GetValue<string>() == "EN_CURSO", "iniciar → EN_CURSO con cronómetro del servidor");
    Ver(!ini.json.Contains("correct", StringComparison.OrdinalIgnoreCase) && !ini.json.Contains("es_correcta", StringComparison.OrdinalIgnoreCase),
        "la pregunta enviada al navegador no contiene marca de respuesta correcta");
    var ini2 = Llamar(c, "iniciar", s1);
    Ver(JsonNode.Parse(ini2.json)!["transcurridoMs"]!.GetValue<double>() >= j["transcurridoMs"]!.GetValue<double>(), "reiniciar no reinicia el cronómetro");
    long op1 = j["pregunta"]!["opciones"]![0]!["id"]!.GetValue<long>();
    var saltar = Llamar(c, "responder", s1, $"{{\"pregunta\":3,\"opcion\":{op1}}}");
    Ver(saltar.http == 409, $"responder una pregunta que no toca → 409 ({saltar.http})");
    long opAjena = Convert.ToInt64(Escalar("SELECT MIN(o.id) FROM eco_juego_opciones o JOIN eco_juego_preguntas p ON p.id=o.pregunta_id WHERE p.orden=2"));
    var ajena = Llamar(c, "responder", s1, $"{{\"pregunta\":1,\"opcion\":{opAjena}}}");
    Ver(ajena.http == 400, $"opción de otra pregunta → 400 ({ajena.http})");
    var r1 = Llamar(c, "responder", s1, $"{{\"pregunta\":1,\"opcion\":{op1}}}");
    long otra = j["pregunta"]!["opciones"]![1]!["id"]!.GetValue<long>();
    var r1b = Llamar(c, "responder", s1, $"{{\"pregunta\":1,\"opcion\":{otra}}}");
    long nResp = Convert.ToInt64(Escalar("SELECT COUNT(*) FROM eco_juego_respuestas r JOIN eco_juego_intentos i ON i.id=r.intento_id WHERE i.sesion_sha256 = LOWER(RAWTOHEX(STANDARD_HASH(:s,'SHA256')))", ("s", s1)));
    long opGuardada = Convert.ToInt64(Escalar("SELECT r.opcion_id FROM eco_juego_respuestas r JOIN eco_juego_intentos i ON i.id=r.intento_id WHERE i.sesion_sha256 = LOWER(RAWTOHEX(STANDARD_HASH(:s,'SHA256')))", ("s", s1)));
    Ver(r1.http == 200 && r1b.http == 200 && nResp == 1 && opGuardada == op1, "respuesta inmutable: el reenvío con otra opción no la cambia ni duplica");
    var resultado = Llamar(c, "revision", s1);
    Ver(resultado.http != 200, $"revisión de respuestas bloqueada mientras el concurso está activo ({resultado.http})");
    string hash = Convert.ToString(Escalar("SELECT sesion_sha256 FROM eco_juego_intentos i WHERE i.sesion_sha256 = LOWER(RAWTOHEX(STANDARD_HASH(:s,'SHA256')))", ("s", s1)))!;
    long enClaro = Convert.ToInt64(Escalar("SELECT COUNT(*) FROM eco_juego_intentos WHERE sesion_sha256 = :s", ("s", s1)));
    Ver(hash.Length == 64 && enClaro == 0, "Oracle guarda solo el SHA-256 de la sesión, nunca el token en claro");
    var adminSin = Llamar(c, "administrar", "clave-incorrecta-de-prueba-0000", "competencias", "{}");
    Ver(adminSin.http is 401 or 403 or 503, $"administración con clave incorrecta rechazada ({adminSin.http})");
    Ejecutar("UPDATE eco_juego_competencias SET estado='FINALIZADA', finalizada_en=SYSTIMESTAMP WHERE codigo=:c", ("c", ronda));
    var tarde = Llamar(c, "responder", s1, $"{{\"pregunta\":2,\"opcion\":{opAjena}}}");
    long nTras = Convert.ToInt64(Escalar("SELECT COUNT(*) FROM eco_juego_respuestas r JOIN eco_juego_intentos i ON i.id=r.intento_id WHERE i.sesion_sha256 = LOWER(RAWTOHEX(STANDARD_HASH(:s,'SHA256')))", ("s", s1)));
    Ver(nTras == 1 && JsonNode.Parse(tarde.json)!["estado"]!.GetValue<string>() == "FINALIZADO", "con el concurso finalizado no se aceptan respuestas y el intento se cierra");
    var nuevo = Llamar(c, "registrar", $"{{\"nombre\":\"Tarde Prueba\",\"alias\":\"tarde1\",\"acepta\":true,\"sesion\":\"{Sesion()}\",\"ronda\":\"{ronda}\"}}");
    Ver(nuevo.http == 409, $"registro con el concurso finalizado → 409 ({nuevo.http})");
}

// ---------- concurrencia ----------
foreach (int n in tamaños)
{
    string ronda = NuevaRonda("N" + n);
    var lat = new ConcurrentDictionary<string, ConcurrentBag<double>>();
    var errores = new ConcurrentBag<string>();
    var finales = new ConcurrentDictionary<string, (int aciertos, string sesion)>();
    var reloj = Stopwatch.StartNew();
    var barrera = new Barrier(n);
    var hilos = Enumerable.Range(0, n).Select(k => Task.Factory.StartNew(() =>
    {
        var rnd = new Random(k * 7919 + n);
        string ses = Sesion(); int aciertos = 0;
        (string json, int http) T(OracleConnection c, string op, params object?[] a)
        {
            var sw = Stopwatch.StartNew(); var r = Llamar(c, op, a); sw.Stop();
            lat.GetOrAdd(op, _ => new ConcurrentBag<double>()).Add(sw.Elapsed.TotalMilliseconds);
            if (r.http >= 400) errores.Add($"{op}:{r.http}:{r.json[..Math.Min(120, r.json.Length)]}");
            return r;
        }
        try
        {
            barrera.SignalAndWait(TimeSpan.FromSeconds(120));
            using var c = new OracleConnection(cadena); c.Open();
            string alias = $"jug{n}x{k:D3}";
            T(c, "registrar", $"{{\"nombre\":\"Jugador Prueba\",\"alias\":\"{alias}\",\"acepta\":true,\"sesion\":\"{ses}\",\"dispositivo\":\"{Sesion()}\",\"ronda\":\"{ronda}\"}}".Replace("Jugador Prueba", $"Jugador Prueba{new string((char)('a' + k % 26), 1)}{(char)('a' + k / 26 % 26)}{(char)('a' + k / 676 % 26)}"));
            var e = T(c, "iniciar", ses);
            for (int q = 1; q <= 7; q++)
            {
                var est = JsonNode.Parse(T(c, "consultar", ses).json)!;
                var ops = est["pregunta"]!["opciones"]!.AsArray();
                long op = ops[rnd.Next(ops.Count)]!["id"]!.GetValue<long>();
                if (correctas.Contains(op)) aciertos++;
                Thread.Sleep(rnd.Next(50, 400));
                T(c, "responder", ses, $"{{\"pregunta\":{q},\"opcion\":{op}}}");
                if (rnd.Next(4) == 0) T(c, "responder", ses, $"{{\"pregunta\":{q},\"opcion\":{op}}}"); // reintento duplicado de red
            }
            finales[ses] = (aciertos, alias);
        }
        catch (Exception ex) { errores.Add("excepcion:" + ex.GetType().Name + ":" + ex.Message[..Math.Min(140, ex.Message.Length)]); }
    }, TaskCreationOptions.LongRunning)).ToArray();
    Task.WaitAll(hilos);
    reloj.Stop();

    // Consistencia en la base
    long part = Convert.ToInt64(Escalar("SELECT COUNT(*) FROM eco_juego_participantes p JOIN eco_juego_competencias c ON c.id=p.competencia_id WHERE c.codigo=:c", ("c", ronda)));
    long fin = Convert.ToInt64(Escalar("SELECT COUNT(*) FROM eco_juego_intentos i JOIN eco_juego_competencias c ON c.id=i.competencia_id WHERE c.codigo=:c AND i.estado='FINALIZADO'", ("c", ronda)));
    long resp = Convert.ToInt64(Escalar("SELECT COUNT(*) FROM eco_juego_respuestas r JOIN eco_juego_intentos i ON i.id=r.intento_id JOIN eco_juego_competencias c ON c.id=i.competencia_id WHERE c.codigo=:c", ("c", ronda)));
    long dup = Convert.ToInt64(Escalar("SELECT COUNT(*) FROM (SELECT r.intento_id, r.pregunta_id FROM eco_juego_respuestas r JOIN eco_juego_intentos i ON i.id=r.intento_id JOIN eco_juego_competencias c ON c.id=i.competencia_id WHERE c.codigo=:c GROUP BY r.intento_id, r.pregunta_id HAVING COUNT(*)>1)", ("c", ronda)));
    long incoherentes = Convert.ToInt64(Escalar("SELECT COUNT(*) FROM eco_juego_intentos i JOIN eco_juego_competencias c ON c.id=i.competencia_id WHERE c.codigo=:c AND (i.puntaje <> i.aciertos*100 OR i.aciertos <> (SELECT COUNT(*) FROM eco_juego_respuestas r WHERE r.intento_id=i.id AND r.es_correcta='S') OR i.tiempo_ms IS NULL OR i.tiempo_ms <= 0)", ("c", ronda)));
    // Aciertos esperados (calculados por el cliente) contra los calculados por el servidor
    int discrep = 0;
    foreach (var kv in finales)
    {
        long srv = Convert.ToInt64(Escalar("SELECT aciertos FROM eco_juego_intentos WHERE sesion_sha256 = LOWER(RAWTOHEX(STANDARD_HASH(:s,'SHA256')))", ("s", kv.Key)));
        if (srv != kv.Value.aciertos) discrep++;
    }
    // Orden del ranking: clasificación pública contra orden recalculado (aciertos desc, tiempo asc, fin asc, id asc)
    string clas; using (var c = new OracleConnection(cadena)) { c.Open(); clas = Llamar(c, "clasificacion", ronda).json; }
    var podio = JsonNode.Parse(clas)!["podio"]!.AsArray().Select(x => x!["alias"]!.GetValue<string>()).ToList();
    var esperado = new List<string>();
    using (var c = new OracleConnection(cadena))
    {
        c.Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT p.alias, i.aciertos, i.tiempo_ms, i.fin, i.id FROM eco_juego_intentos i JOIN eco_juego_participantes p ON p.id=i.participante_id JOIN eco_juego_competencias c ON c.id=i.competencia_id WHERE c.codigo=:c AND i.estado='FINALIZADO'";
        cmd.Parameters.Add("c", ronda);
        var filas = new List<(string a, long ac, long t, DateTimeOffset f, long id)>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) filas.Add((r.GetString(0), r.GetInt64(1), r.GetInt64(2), r.GetDateTimeOffset(3), r.GetInt64(4)));
        esperado = filas.OrderByDescending(x => x.ac).ThenBy(x => x.t).ThenBy(x => x.f).ThenBy(x => x.id).Take(5).Select(x => x.a).ToList();
    }
    var todas = lat.Values.SelectMany(x => x).ToList();
    Log($"--- {n} jugadores concurrentes · ronda {ronda} · {reloj.Elapsed.TotalSeconds:F1} s");
    foreach (var op in new[] { "registrar", "iniciar", "consultar", "responder" })
        if (lat.TryGetValue(op, out var l)) { var v = l.ToList(); Log($"    {op,-10} n={v.Count,5} media={v.Average(),7:F1} ms  mediana={Pct(v, 50),7:F1}  p95={Pct(v, 95),7:F1}  p99={Pct(v, 99),7:F1}  máx={v.Max(),7:F1}"); }
    Log($"    total      n={todas.Count,5} media={todas.Average(),7:F1} ms  mediana={Pct(todas, 50),7:F1}  p95={Pct(todas, 95),7:F1}  p99={Pct(todas, 99),7:F1}");
    Ver(errores.IsEmpty, $"{n}: errores HTTP/excepciones = {errores.Count} {string.Join(" | ", errores.Take(3))}");
    Ver(part == n && fin == n, $"{n}: participantes {part}/{n} y partidas finalizadas {fin}/{n}");
    Ver(resp == n * 7 && dup == 0, $"{n}: respuestas {resp}/{n * 7}, duplicadas {dup} (con reintentos de red simulados)");
    Ver(incoherentes == 0 && discrep == 0, $"{n}: puntaje y aciertos del servidor coherentes ({incoherentes} incoherentes, {discrep} distintos del cálculo independiente)");
    Ver(podio.SequenceEqual(esperado), $"{n}: top 5 público = orden aciertos↓, tiempo↑, fin↑, id↑ ({string.Join(",", podio)})");
}
Log($"RESUMEN: {ok} correctas, {fallo} fallidas");
File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "resultado.txt"), salida);
return fallo == 0 ? 0 : 1;
