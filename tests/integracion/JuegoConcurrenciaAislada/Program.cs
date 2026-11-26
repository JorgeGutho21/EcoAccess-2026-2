// Arnés de concurrencia y seguridad del juego contra PKG_ECO_JUEGO en el ESQUEMA AISLADO local (nunca producción).
// Cubre las dos rondas: banco 1 (7 preguntas, BANCO_ID nulo, como la ronda oficial abierta) y banco 2 (10 preguntas, V034).
// Para demostrar que la ronda de 7 no cambió con V035, compila en el esquema aislado una copia del paquete de V032
// (PKG_ECO_JUEGO_V032, tomada de R035) y compara las respuestas JSON de ambos paquetes sobre los mismos intentos.
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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

const string NUEVO = "pkg_eco_juego", V032 = "pkg_eco_juego_v032";

// ---------- utilidades ----------
(string json, int http) LlamarEn(OracleConnection c, string paquete, string proc, params object?[] entradas)
{
    using var cmd = c.CreateCommand();
    cmd.CommandText = paquete + "." + proc; cmd.CommandType = System.Data.CommandType.StoredProcedure; cmd.BindByName = false;
    foreach (var e in entradas)
        cmd.Parameters.Add(e is string s && s.StartsWith("{") ? new OracleParameter { OracleDbType = OracleDbType.Clob, Value = s }
                                                               : new OracleParameter { OracleDbType = OracleDbType.Varchar2, Value = e ?? DBNull.Value });
    var pj = cmd.Parameters.Add("j", OracleDbType.Clob, System.Data.ParameterDirection.Output);
    var ph = cmd.Parameters.Add("h", OracleDbType.Decimal, System.Data.ParameterDirection.Output);
    cmd.ExecuteNonQuery();
    string json = ((OracleClob)pj.Value).IsNull ? "" : ((OracleClob)pj.Value).Value;
    return (json, (int)((OracleDecimal)ph.Value).Value);
}
(string json, int http) Llamar(OracleConnection c, string proc, params object?[] entradas) => LlamarEn(c, NUEVO, proc, entradas);
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
string? ErrorOracle(string sql) { try { Ejecutar(sql); return null; } catch (OracleException e) { return "ORA-" + e.Number.ToString("D5"); } }
static string Sesion() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
static double Pct(List<double> v, double p) { if (v.Count == 0) return 0; var s = v.OrderBy(x => x).ToList(); int i = (int)Math.Ceiling(p / 100 * s.Count) - 1; return s[Math.Clamp(i, 0, s.Count - 1)]; }
long Ses(string consulta, string sesion) => Convert.ToInt64(Escalar(consulta.Replace("#S", "LOWER(RAWTOHEX(STANDARD_HASH(:s,'SHA256')))"), ("s", sesion)));

// ---------- paquete de referencia (V032 exacto, solo en el esquema aislado) ----------
string Raiz()
{
    foreach (var inicio in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        for (var d = new DirectoryInfo(inicio); d is not null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "EcoAccess.P3.sln"))) return d.FullName;
    throw new Exception("No se encontró la raíz del repositorio");
}
{
    string r035 = File.ReadAllText(Path.Combine(Raiz(), "database", "06_migraciones", "reversion", "R035__juego_paquete_bancos.sql")).Replace("\r\n", "\n");
    var bloques = Regex.Split(r035, @"^/\s*$", RegexOptions.Multiline)
        .Where(x => x.Contains("CREATE OR REPLACE PACKAGE")).Select(x => x[x.IndexOf("CREATE OR REPLACE PACKAGE", StringComparison.Ordinal)..].TrimEnd())
        .Select(x => Regex.Replace(x, @"\bpkg_eco_juego\b", V032)).ToList();
    if (bloques.Count != 2) throw new Exception("R035 no contiene la especificación y el cuerpo del paquete");
    foreach (var bloque in bloques) Ejecutar(bloque);
    if (Convert.ToString(Escalar("SELECT status FROM user_objects WHERE object_name = 'PKG_ECO_JUEGO_V032' AND object_type = 'PACKAGE BODY'")) != "VALID")
        throw new Exception("El paquete de referencia de V032 no compiló");
}

// Clave de respuestas SOLO en el esquema aislado: la opción de menor id de cada pregunta es la correcta (en los dos bancos).
Ejecutar("UPDATE eco_juego_opciones o SET es_correcta = CASE WHEN o.id = (SELECT MIN(x.id) FROM eco_juego_opciones x WHERE x.pregunta_id = o.pregunta_id) THEN 'S' ELSE 'N' END");
var correctas = new HashSet<long>();
using (var c = new OracleConnection(cadena)) { c.Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT id FROM eco_juego_opciones WHERE es_correcta='S'"; using var r = cmd.ExecuteReader(); while (r.Read()) correctas.Add(r.GetInt64(0)); }

// Los límites por origen (400 registros y 3000 respuestas cada 10 min) cuentan a todo el arnés como un solo origen:
// se reinician entre fases para que dos rondas completas no los agoten. Solo en el esquema aislado.
void ReiniciarLimites() => Ejecutar("DELETE FROM eco_juego_limites WHERE clave LIKE 'reg|%' OR clave LIKE 'resp|%'");
ReiniciarLimites();

string NuevaRonda(string etiqueta, int? banco = null)
{
    string codigo = $"AISLADO-{etiqueta}-{DateTime.UtcNow:HHmmssfff}";
    Ejecutar("INSERT INTO eco_juego_competencias (codigo, nombre, estado, es_prueba, abierta_en, banco_id) VALUES (:c, :n, 'ABIERTA', 'S', SYSTIMESTAMP, :b)",
        ("c", codigo), ("n", "Prueba aislada " + etiqueta), ("b", banco.HasValue ? banco.Value : DBNull.Value));
    return codigo;
}
void Finalizar(string ronda) => Ejecutar("UPDATE eco_juego_competencias SET estado='FINALIZADA', finalizada_en=SYSTIMESTAMP WHERE codigo=:c", ("c", ronda));
string Registro(string nombre, string alias, string sesion, string ronda) =>
    $"{{\"nombre\":\"{nombre}\",\"alias\":\"{alias}\",\"acepta\":true,\"sesion\":\"{sesion}\",\"dispositivo\":\"{Sesion()}\",\"ronda\":\"{ronda}\"}}";

// Clave de administración temporal SOLO si el esquema aislado no tiene ninguna (se retira al terminar).
string? claveAdmin = null;
if (Convert.ToInt64(Escalar("SELECT COUNT(*) FROM eco_juego_configuracion WHERE clave = 'ADMIN_CLAVE_SHA256'")) == 0)
{
    claveAdmin = Sesion() + Sesion();
    Ejecutar("INSERT INTO eco_juego_configuracion (clave, valor) VALUES ('ADMIN_CLAVE_SHA256', LOWER(RAWTOHEX(STANDARD_HASH(:k,'SHA256'))))", ("k", claveAdmin));
}

int ok = 0, fallo = 0;
void Ver(bool cond, string texto) { if (cond) ok++; else fallo++; Log((cond ? "OK    " : "FALLO ") + texto); }
try
{
// ---------- pruebas funcionales y de seguridad (ronda de 7, como antes) ----------
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
    long nResp = Ses("SELECT COUNT(*) FROM eco_juego_respuestas r JOIN eco_juego_intentos i ON i.id=r.intento_id WHERE i.sesion_sha256 = #S", s1);
    long opGuardada = Ses("SELECT r.opcion_id FROM eco_juego_respuestas r JOIN eco_juego_intentos i ON i.id=r.intento_id WHERE i.sesion_sha256 = #S", s1);
    Ver(r1.http == 200 && r1b.http == 200 && nResp == 1 && opGuardada == op1, "respuesta inmutable: el reenvío con otra opción no la cambia ni duplica");
    var resultado = Llamar(c, "revision", s1);
    Ver(resultado.http != 200, $"revisión de respuestas bloqueada mientras el concurso está activo ({resultado.http})");
    string hash = Convert.ToString(Escalar("SELECT sesion_sha256 FROM eco_juego_intentos i WHERE i.sesion_sha256 = LOWER(RAWTOHEX(STANDARD_HASH(:s,'SHA256')))", ("s", s1)))!;
    long enClaro = Convert.ToInt64(Escalar("SELECT COUNT(*) FROM eco_juego_intentos WHERE sesion_sha256 = :s", ("s", s1)));
    Ver(hash.Length == 64 && enClaro == 0, "Oracle guarda solo el SHA-256 de la sesión, nunca el token en claro");
    var adminSin = Llamar(c, "administrar", "clave-incorrecta-de-prueba-0000", "competencias", "{}");
    Ver(adminSin.http is 401 or 403 or 429 or 503, $"administración con clave incorrecta rechazada ({adminSin.http})");
    Finalizar(ronda);
    var tarde = Llamar(c, "responder", s1, $"{{\"pregunta\":2,\"opcion\":{opAjena}}}");
    long nTras = Ses("SELECT COUNT(*) FROM eco_juego_respuestas r JOIN eco_juego_intentos i ON i.id=r.intento_id WHERE i.sesion_sha256 = #S", s1);
    Ver(nTras == 1 && JsonNode.Parse(tarde.json)!["estado"]!.GetValue<string>() == "FINALIZADO", "con el concurso finalizado no se aceptan respuestas y el intento se cierra");
    var nuevo = Llamar(c, "registrar", $"{{\"nombre\":\"Tarde Prueba\",\"alias\":\"tarde1\",\"acepta\":true,\"sesion\":\"{Sesion()}\",\"ronda\":\"{ronda}\"}}");
    Ver(nuevo.http == 409, $"registro con el concurso finalizado → 409 ({nuevo.http})");
}

// ---------- ronda de 7: V035 responde exactamente lo mismo que el paquete de V032 ----------
{
    static string SinReloj(string j) => Regex.Replace(Regex.Replace(j, "\"transcurridoMs\":[0-9.]+,?", ""), "\"actualizado\":\"[^\"]*\"", "");
    int comparaciones = 0; var distintas = new List<string>();
    void Igual(string etiqueta, string a, string b) { comparaciones++; if (SinReloj(a) != SinReloj(b)) distintas.Add(etiqueta); }
    string ronda = NuevaRonda("EQ7");
    using var c = new OracleConnection(cadena); c.Open();
    Igual("estado_publico", LlamarEn(c, NUEVO, "estado_publico", ronda).json, LlamarEn(c, V032, "estado_publico", ronda).json);
    // A: registro con V035 y reintento con V032; respuestas alternadas entre paquetes y reintentos cruzados.
    string sa = Sesion(), sb = Sesion(), sc = Sesion(), sd = Sesion();
    var ra = LlamarEn(c, NUEVO, "registrar", Registro("Ana Equivalente", "eqana", sa, ronda));
    var ra2 = LlamarEn(c, V032, "registrar", Registro("Ana Equivalente", "eqana", sa, ronda));
    Igual("registro (201 vs reintento 200)", ra.json, ra2.json);
    Igual("inicio", LlamarEn(c, NUEVO, "iniciar", sa).json, LlamarEn(c, V032, "iniciar", sa).json);
    for (int q = 1; q <= 7; q++)
    {
        string crudo = LlamarEn(c, NUEVO, "consultar", sa).json;
        var est = JsonNode.Parse(crudo)!;
        Igual($"consultar pregunta {q}", crudo, LlamarEn(c, V032, "consultar", sa).json);
        var ops = est["pregunta"]!["opciones"]!.AsArray().Select(x => x!["id"]!.GetValue<long>()).ToList();
        long op = q % 3 == 0 ? ops.First(x => !correctas.Contains(x)) : ops.First(correctas.Contains);
        var (uno, otro) = q % 2 == 1 ? (V032, NUEVO) : (NUEVO, V032);
        var r = LlamarEn(c, uno, "responder", sa, $"{{\"pregunta\":{q},\"opcion\":{op}}}");
        var dup = LlamarEn(c, otro, "responder", sa, $"{{\"pregunta\":{q},\"opcion\":{ops.First(x => x != op)}}}");
        Igual($"respuesta {q} y reintento cruzado", r.json, dup.json);
        Igual($"pregunta futura {q + 2}", LlamarEn(c, NUEVO, "responder", sa, $"{{\"pregunta\":{q + 2},\"opcion\":{op}}}").json,
                                          LlamarEn(c, V032, "responder", sa, $"{{\"pregunta\":{q + 2},\"opcion\":{op}}}").json);
    }
    Igual("pregunta 8 inexistente", LlamarEn(c, NUEVO, "responder", sa, "{\"pregunta\":8,\"opcion\":101}").json, LlamarEn(c, V032, "responder", sa, "{\"pregunta\":8,\"opcion\":101}").json);
    Igual("resultado final", LlamarEn(c, NUEVO, "consultar", sa).json, LlamarEn(c, V032, "consultar", sa).json);
    // B y C: tres respuestas y cierre por fin del concurso, uno con cada paquete. D: solo registrado.
    foreach (var (s, alias, paquete) in new[] { (sb, "eqbeto", V032), (sc, "eqcarla", NUEVO) })
    {
        LlamarEn(c, paquete, "registrar", Registro("Persona " + alias[2..], alias, s, ronda)); LlamarEn(c, paquete, "iniciar", s);
        for (int q = 1; q <= 3; q++)
        {
            var ops = JsonNode.Parse(LlamarEn(c, paquete, "consultar", s).json)!["pregunta"]!["opciones"]!.AsArray().Select(x => x!["id"]!.GetValue<long>()).ToList();
            LlamarEn(c, paquete == NUEVO ? V032 : NUEVO, "responder", s, $"{{\"pregunta\":{q},\"opcion\":{(q == 2 ? ops.First(x => !correctas.Contains(x)) : ops.First(correctas.Contains))}}}");
        }
    }
    LlamarEn(c, NUEVO, "registrar", Registro("Diana Registrada", "eqdiana", sd, ronda));
    Igual("clasificación en curso", LlamarEn(c, NUEVO, "clasificacion", ronda).json, LlamarEn(c, V032, "clasificacion", ronda).json);
    Finalizar(ronda);
    LlamarEn(c, V032, "consultar", sb);    // cierra con cerrar_intento de V032
    LlamarEn(c, NUEVO, "consultar", sc);   // cierra con cerrar_intento de V035
    // Se comparan después de los dos cierres: cada cierre cambia posiciones y el número de participantes finalizados.
    Igual("cierre por fin de concurso (V032)", LlamarEn(c, V032, "consultar", sb).json, LlamarEn(c, NUEVO, "consultar", sb).json);
    Igual("cierre por fin de concurso (V035)", LlamarEn(c, V032, "consultar", sc).json, LlamarEn(c, NUEVO, "consultar", sc).json);
    Igual("solo registrado tras el fin", LlamarEn(c, NUEVO, "consultar", sd).json, LlamarEn(c, V032, "consultar", sd).json);
    long cierres = Convert.ToInt64(Escalar("SELECT COUNT(*) FROM eco_juego_intentos i JOIN eco_juego_competencias k ON k.id=i.competencia_id WHERE k.codigo=:c AND i.estado='FINALIZADO' AND i.aciertos + i.errores = 7 AND i.pregunta_actual = 8 AND i.puntaje = i.aciertos*100", ("c", ronda)));
    Igual("clasificación definitiva", LlamarEn(c, NUEVO, "clasificacion", ronda).json, LlamarEn(c, V032, "clasificacion", ronda).json);
    // Revisión: V032 recorre TODAS las preguntas de la tabla (tras V034 también las 10 nuevas, orden 11..20); V035 solo
    // las del banco. Las 7 primeras deben coincidir byte a byte.
    var revN = JsonNode.Parse(LlamarEn(c, NUEVO, "revision", sa).json)!["revision"]!.AsArray();
    var revV = JsonNode.Parse(LlamarEn(c, V032, "revision", sa).json)!["revision"]!.AsArray();
    comparaciones++;
    if (revN.Count != 7 || !revN.Select(x => x!.ToJsonString()).SequenceEqual(revV.Take(7).Select(x => x!.ToJsonString()))) distintas.Add("revisión");
    if (claveAdmin is not null)
    {
        string id = Convert.ToString(Escalar("SELECT id FROM eco_juego_competencias WHERE codigo=:c", ("c", ronda)))!;
        Igual("panel: participantes", LlamarEn(c, NUEVO, "administrar", claveAdmin, "participantes", $"{{\"competencia\":{id}}}").json,
                                      LlamarEn(c, V032, "administrar", claveAdmin, "participantes", $"{{\"competencia\":{id}}}").json);
        Igual("panel: exportación CSV", LlamarEn(c, NUEVO, "exportar", claveAdmin, id).json, LlamarEn(c, V032, "exportar", claveAdmin, id).json);
    }
    Ver(distintas.Count == 0, $"ronda de 7: V035 = V032 en {comparaciones} comparaciones de JSON/CSV (registro, inicio, 7 respuestas, reintentos, errores, cierre, clasificación, revisión{(claveAdmin is null ? "" : ", panel y CSV")}) {string.Join(" | ", distintas)}");
    Ver(cierres == 3, $"ronda de 7: puntaje = aciertos×100, aciertos+errores = 7 y pregunta_actual = 8 en los 3 cierres ({cierres}/3)");
    long banco1 = Convert.ToInt64(Escalar("SELECT COUNT(*) FROM eco_juego_banco_preguntas WHERE banco_id = 1 AND orden = pregunta_id AND orden BETWEEN 1 AND 7"));
    long sinBanco = Convert.ToInt64(Escalar("SELECT COUNT(*) FROM eco_juego_competencias WHERE codigo = :c AND banco_id IS NULL", ("c", ronda)));
    Ver(banco1 == 7 && sinBanco == 1, "ronda de 7: BANCO_ID nulo usa el banco 1 = preguntas 1..7 en su orden original");
}

// ---------- ronda de 10 (banco 2) ----------
{
    string ronda = NuevaRonda("SEG10", 2);
    using var c = new OracleConnection(cadena); c.Open();
    var est = JsonNode.Parse(Llamar(c, "estado_publico", ronda).json)!;
    Ver(est["preguntas"]!.GetValue<int>() == 10 && est["puntajeMaximo"]!.GetValue<int>() == 1000, $"ronda de 10: estado público con 10 preguntas y 1000 puntos ({est["preguntas"]}, {est["puntajeMaximo"]})");
    string s1 = Sesion(), s2 = Sesion();
    Llamar(c, "registrar", Registro("Diez Perfecto", "diez1", s1, ronda));
    var ini = JsonNode.Parse(Llamar(c, "iniciar", s1).json)!;
    var ids = ini["pregunta"]!["opciones"]!.AsArray().Select(x => x!["id"]!.GetValue<long>()).ToList();
    Ver(ini["pregunta"]!["total"]!.GetValue<int>() == 10 && ini["pregunta"]!["orden"]!.GetValue<int>() == 1 && ids.Count == 4 && ids.All(x => x is >= 1011 and <= 1014),
        "ronda de 10: la pregunta 1 es la 101 (propósito de EcoAccess) con 4 opciones y total 10");
    Ver(!ini.ToJsonString().Contains("correct", StringComparison.OrdinalIgnoreCase), "ronda de 10: la pregunta no revela la respuesta");
    var p11 = Llamar(c, "responder", s1, "{\"pregunta\":11,\"opcion\":1011}");
    var opDe7 = Llamar(c, "responder", s1, "{\"pregunta\":1,\"opcion\":101}");
    var futura = Llamar(c, "responder", s1, "{\"pregunta\":4,\"opcion\":1041}");
    Ver(p11.http == 400 && opDe7.http == 400 && futura.http == 409, $"ronda de 10: pregunta 11 → 400, opción de la ronda de 7 → 400, pregunta futura → 409 ({p11.http}, {opDe7.http}, {futura.http})");
    var temas = new List<string>();
    JsonNode? fin = null;
    for (int q = 1; q <= 10; q++)
    {
        var e = JsonNode.Parse(Llamar(c, "consultar", s1).json)!;
        temas.Add(e["pregunta"]!["tema"]!.GetValue<string>());
        long op = e["pregunta"]!["opciones"]!.AsArray().Select(x => x!["id"]!.GetValue<long>()).First(correctas.Contains);
        fin = JsonNode.Parse(Llamar(c, "responder", s1, $"{{\"pregunta\":{q},\"opcion\":{op}}}").json);
        Llamar(c, "responder", s1, $"{{\"pregunta\":{q},\"opcion\":{op + 1}}}");   // reintento con otra opción: no cambia nada
    }
    var res = fin!["resultado"]!;
    Ver(fin["estado"]!.GetValue<string>() == "FINALIZADO" && res["puntaje"]!.GetValue<int>() == 1000 && res["aciertos"]!.GetValue<int>() == 10 && res["errores"]!.GetValue<int>() == 0 && fin["respondidas"]!.GetValue<int>() == 10,
        $"ronda de 10: 10 aciertos → 1000 puntos, 0 errores, 10 respondidas ({res["puntaje"]})");
    Ver(Ses("SELECT COUNT(*) FROM eco_juego_respuestas r JOIN eco_juego_intentos i ON i.id=r.intento_id WHERE i.sesion_sha256 = #S", s1) == 10
        && Ses("SELECT pregunta_actual FROM eco_juego_intentos WHERE sesion_sha256 = #S", s1) == 11, "ronda de 10: 10 respuestas inmutables y pregunta_actual = 11");
    Ver(string.Join(",", temas) == "EcoAccess,Separación,ESP32,Servomotores,SOLID,Grafos,Oracle Cloud,Wallet,Arquitectura web,Política de apertura",
        $"ronda de 10: temas en orden ({string.Join(", ", temas)})");
    var repetida = JsonNode.Parse(Llamar(c, "responder", s1, "{\"pregunta\":10,\"opcion\":1104}").json)!;
    Ver(repetida["resultado"]!["puntaje"]!.GetValue<int>() == 1000, "ronda de 10: repetir el envío final no cambia el resultado");
    // Cierre por fin del concurso con 4 respuestas: lo no respondido cuenta como error (10 − aciertos).
    Llamar(c, "registrar", Registro("Diez Parcial", "diez2", s2, ronda)); Llamar(c, "iniciar", s2);
    for (int q = 1; q <= 4; q++)
    {
        var ops = JsonNode.Parse(Llamar(c, "consultar", s2).json)!["pregunta"]!["opciones"]!.AsArray().Select(x => x!["id"]!.GetValue<long>()).ToList();
        Llamar(c, "responder", s2, $"{{\"pregunta\":{q},\"opcion\":{(q == 4 ? ops.First(x => !correctas.Contains(x)) : ops.First(correctas.Contains))}}}");
    }
    Ver(Llamar(c, "revision", s1).http == 403, "ronda de 10: sin revisión con el concurso activo");
    Finalizar(ronda);
    var parcial = JsonNode.Parse(Llamar(c, "consultar", s2).json)!["resultado"]!;
    Ver(parcial["aciertos"]!.GetValue<int>() == 3 && parcial["errores"]!.GetValue<int>() == 7 && parcial["puntaje"]!.GetValue<int>() == 300
        && Ses("SELECT pregunta_actual FROM eco_juego_intentos WHERE sesion_sha256 = #S", s2) == 11,
        $"ronda de 10: cierre por fin del concurso = 3 aciertos, 7 errores, 300 puntos ({parcial["aciertos"]}/{parcial["errores"]}/{parcial["puntaje"]})");
    var rev = JsonNode.Parse(Llamar(c, "revision", s1).json)!["revision"]!.AsArray();
    Ver(rev.Count == 10 && rev.Select(x => x!["orden"]!.GetValue<int>()).SequenceEqual(Enumerable.Range(1, 10))
        && rev.All(x => x!["acierto"]!.GetValue<bool>() && x["correcta"]!.GetValue<string>() == x["elegida"]!.GetValue<string>() && x["explicacion"]!.GetValue<string>().Length > 60),
        $"ronda de 10: revisión educativa con 10 preguntas en orden 1..10 y su explicación ({rev.Count})");
    var podio = JsonNode.Parse(Llamar(c, "clasificacion", ronda).json)!["podio"]!.AsArray();
    Ver(podio.Count == 2 && podio[0]!["alias"]!.GetValue<string>() == "diez1" && podio[0]!["puntaje"]!.GetValue<int>() == 1000, "ronda de 10: clasificación con 1000 puntos en el primer puesto");
    // Integridad: el banco de una competencia abierta/finalizada y el contenido de un banco en uso no cambian.
    string? e1 = ErrorOracle($"UPDATE eco_juego_competencias SET banco_id = 1 WHERE codigo = '{ronda}'");
    string? e2 = ErrorOracle("UPDATE eco_juego_banco_preguntas SET pregunta_id = pregunta_id WHERE banco_id = 2 AND orden = 1");
    string? e3 = ErrorOracle("UPDATE eco_juego_intentos SET puntaje = 1100, aciertos = 11 WHERE id = (SELECT MAX(id) FROM eco_juego_intentos)");
    Ver(e1 == "ORA-20431" && e2 == "ORA-20432", $"ronda de 10: no se cambia el banco de una competencia ya abierta ni un banco en uso ({e1}, {e2})");
    Ver(e3 == "ORA-02290", $"CHECK de puntaje: más de 1000 puntos se rechaza ({e3})");
}

// ---------- panel: crear rondas con banco ----------
if (claveAdmin is not null)
{
    using var c = new OracleConnection(cadena); c.Open();
    var diez = JsonNode.Parse(Llamar(c, "administrar", claveAdmin, "crear", "{\"nombre\":\"Arnés diez\",\"prueba\":true,\"banco\":\"EXPO2026-10\"}").json)!;
    var siete = JsonNode.Parse(Llamar(c, "administrar", claveAdmin, "crear", "{\"nombre\":\"Arnés siete\",\"prueba\":true}").json)!;
    var mala = Llamar(c, "administrar", claveAdmin, "crear", "{\"nombre\":\"Arnés malo\",\"prueba\":true,\"banco\":\"NO-EXISTE\"}");
    var lista = JsonNode.Parse(Llamar(c, "administrar", claveAdmin, "competencias", "{}").json)!["competencias"]!.AsArray();
    var l10 = lista.First(x => x!["id"]!.GetValue<long>() == diez["id"]!.GetValue<long>())!;
    var l7 = lista.First(x => x!["id"]!.GetValue<long>() == siete["id"]!.GetValue<long>())!;
    long nulo = Convert.ToInt64(Escalar("SELECT COUNT(*) FROM eco_juego_competencias WHERE id = :i AND banco_id IS NULL", ("i", siete["id"]!.GetValue<long>())));
    Ver(diez["preguntas"]!.GetValue<int>() == 10 && siete["preguntas"]!.GetValue<int>() == 7 && nulo == 1 && mala.http == 400
        && l10["preguntas"]!.GetValue<int>() == 10 && l10["banco"]!.GetValue<string>() == "EXPO2026-10" && l7["preguntas"]!.GetValue<int>() == 7,
        $"panel: «crear» con banco EXPO2026-10 → 10 preguntas; sin banco → 7 (BANCO_ID nulo); banco inexistente → 400 ({mala.http})");
    foreach (var k in new[] { diez, siete })
        Llamar(c, "administrar", claveAdmin, "eliminar_prueba", $"{{\"competencia\":{k["id"]}}}");
}
else Log("AVISO  el esquema aislado ya tiene clave de administración: se omiten las comparaciones del panel");

// ---------- concurrencia: ronda de 7 y ronda de 10 ----------
foreach (var (banco, total) in new[] { ((int?)null, 7), ((int?)2, 10) })
foreach (int n in tamaños)
{
    ReiniciarLimites();
    string ronda = NuevaRonda($"R{total}N{n}", banco);
    var lat = new ConcurrentDictionary<string, ConcurrentBag<double>>();
    var errores = new ConcurrentBag<string>();
    var finales = new ConcurrentDictionary<string, (int aciertos, string sesion)>();
    var reloj = Stopwatch.StartNew();
    var barrera = new Barrier(n);
    var hilos = Enumerable.Range(0, n).Select(k => Task.Factory.StartNew(() =>
    {
        var rnd = new Random(k * 7919 + n + total);
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
            string alias = $"j{total}n{n}x{k:D3}";
            T(c, "registrar", $"{{\"nombre\":\"Jugador Prueba\",\"alias\":\"{alias}\",\"acepta\":true,\"sesion\":\"{ses}\",\"dispositivo\":\"{Sesion()}\",\"ronda\":\"{ronda}\"}}".Replace("Jugador Prueba", $"Jugador Prueba{new string((char)('a' + k % 26), 1)}{(char)('a' + k / 26 % 26)}{(char)('a' + k / 676 % 26)}"));
            var e = T(c, "iniciar", ses);
            for (int q = 1; q <= total; q++)
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
    long incoherentes = Convert.ToInt64(Escalar("SELECT COUNT(*) FROM eco_juego_intentos i JOIN eco_juego_competencias c ON c.id=i.competencia_id WHERE c.codigo=:c AND (i.puntaje <> i.aciertos*100 OR i.aciertos + i.errores <> :t OR i.pregunta_actual <> :t + 1 OR i.aciertos <> (SELECT COUNT(*) FROM eco_juego_respuestas r WHERE r.intento_id=i.id AND r.es_correcta='S') OR i.tiempo_ms IS NULL OR i.tiempo_ms <= 0)", ("c", ronda), ("t", total)));
    // Aciertos esperados (calculados por el cliente) contra los calculados por el servidor
    int discrep = 0;
    foreach (var kv in finales)
        if (Ses("SELECT aciertos FROM eco_juego_intentos WHERE sesion_sha256 = #S", kv.Key) != kv.Value.aciertos) discrep++;
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
    string et = $"R{total}·{n}";
    Log($"--- ronda de {total} · {n} jugadores concurrentes · {ronda} · {reloj.Elapsed.TotalSeconds:F1} s");
    foreach (var op in new[] { "registrar", "iniciar", "consultar", "responder" })
        if (lat.TryGetValue(op, out var l)) { var v = l.ToList(); Log($"    {op,-10} n={v.Count,5} media={v.Average(),7:F1} ms  mediana={Pct(v, 50),7:F1}  p95={Pct(v, 95),7:F1}  p99={Pct(v, 99),7:F1}  máx={v.Max(),7:F1}"); }
    if (todas.Count > 0) Log($"    total      n={todas.Count,5} media={todas.Average(),7:F1} ms  mediana={Pct(todas, 50),7:F1}  p95={Pct(todas, 95),7:F1}  p99={Pct(todas, 99),7:F1}");
    Ver(errores.IsEmpty, $"{et}: errores HTTP/excepciones = {errores.Count} {string.Join(" | ", errores.Take(3))}");
    Ver(part == n && fin == n, $"{et}: participantes {part}/{n} y partidas finalizadas {fin}/{n}");
    Ver(resp == n * total && dup == 0, $"{et}: respuestas {resp}/{n * total}, duplicadas {dup} (con reintentos de red simulados)");
    Ver(incoherentes == 0 && discrep == 0, $"{et}: puntaje, aciertos+errores = {total} y aciertos del servidor coherentes ({incoherentes} incoherentes, {discrep} distintos del cálculo independiente)");
    Ver(podio.SequenceEqual(esperado), $"{et}: top 5 público = orden aciertos↓, tiempo↑, fin↑, id↑ ({string.Join(",", podio)})");
}
}
finally
{
    if (claveAdmin is not null) Ejecutar("DELETE FROM eco_juego_configuracion WHERE clave = 'ADMIN_CLAVE_SHA256'");
    ReiniciarLimites();
    Ejecutar("DROP PACKAGE pkg_eco_juego_v032");   // la copia de referencia no queda en el esquema
}
Log($"RESUMEN: {ok} correctas, {fallo} fallidas");
File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "resultado.txt"), salida);
return fallo == 0 ? 0 : 1;
