using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using Oracle.ManagedDataAccess.Client;

// EcoAccess P3 · Lote de demostración académica para la feria (ver EcoAccess.P3.DatosFeria.csproj).
//
// Uso:
//   EcoAccess.P3.DatosFeria.exe generar   --perfil pruebas|produccion [--lote SIM-FERIA-2026] [--estacion <id>] [--confirmar-produccion]
//   EcoAccess.P3.DatosFeria.exe verificar --perfil pruebas|produccion [--lote ...] [--informe <archivo.md>]
//   EcoAccess.P3.DatosFeria.exe desactivar|reactivar --perfil ... [--confirmar-produccion]
//
// · «pruebas» = esquema aislado ECOACCESS_P3_PRUEBAS (User Secrets ecoaccess-p3-pruebas, solo localhost).
// · «produccion» = Oracle Cloud (User Secrets ecoaccess-p3-framework); exige --confirmar-produccion para escribir.
// · Idempotente: cada fila creada queda en REGISTRO_SIMULACION con una clave estable; repetir no duplica nada.
// · Las fechas son relativas al momento de la PRIMERA ejecución (guardado en LOTE_SIMULACION.FECHA_CREACION_UTC).
static class Program
{
    const string LotePredeterminado = "SIM-FERIA-2026";
    const string Dominio = "feria.ecoaccess.test";
    const int Semilla = 20261126;
    static readonly TimeSpan Colombia = TimeSpan.FromHours(-5);

    static string _cadena = "";
    static string _perfil = "";
    static string _lote = LotePredeterminado;
    static FabricaConexionesOracle _fabrica = null!;
    static RepositorioLoteSimulacionOracle _manifiesto = null!;
    static readonly Reloj _reloj = new();
    static StreamWriter? _bitacora;

    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        if (args.Length == 0) { Console.Error.WriteLine("Comando: generar | verificar | desactivar | reactivar"); return 2; }
        string comando = args[0];
        _perfil = Opcion(args, "--perfil") ?? "pruebas";
        _lote = Opcion(args, "--lote") ?? LotePredeterminado;
        if (!RepositorioLoteSimulacionOracle.LoteValido(_lote)) { Console.Error.WriteLine("El lote debe tener la forma SIM-XXXX."); return 2; }
        if (!Conectar()) return 2;
        bool escribe = comando is "generar" or "desactivar" or "reactivar";
        if (escribe && _perfil == "produccion" && !args.Contains("--confirmar-produccion"))
        {
            Console.Error.WriteLine("Escribir en Oracle Cloud exige --confirmar-produccion (aprobación explícita del propietario).");
            return 2;
        }
        _fabrica = new FabricaConexionesOracle(_cadena);
        _manifiesto = new RepositorioLoteSimulacionOracle(_fabrica);
        Directory.CreateDirectory(CarpetaPrivada);
        _bitacora = new StreamWriter(Path.Combine(CarpetaPrivada, $"{_lote}.{_perfil}.{comando}.{DateTime.Now:yyyyMMdd-HHmmss}.log"), false, new UTF8Encoding(false)) { AutoFlush = true };
        try
        {
            return comando switch
            {
                "generar" => await Generador.EjecutarAsync(int.TryParse(Opcion(args, "--estacion"), out int e) ? e : (int?)null),
                "verificar" => await Verificador.EjecutarAsync(Opcion(args, "--informe")),
                "desactivar" => await CambiarActivoAsync(false),
                "reactivar" => await CambiarActivoAsync(true),
                _ => 2,
            };
        }
        catch (Exception ex)
        {
            Log("ERROR: " + ex.GetType().Name + ": " + Limpiar(ex.Message));
            return 1;
        }
        finally { _bitacora?.Dispose(); }
    }

    // ------------------------------------------------------------------ infraestructura
    internal static string CarpetaPrivada => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EcoAccessP3", "privado", "feria");
    internal static string ArchivoCredenciales => Path.Combine(CarpetaPrivada, $"{_lote}.{_perfil}.credenciales.tsv");
    internal static string Lote => _lote;
    internal static string Perfil => _perfil;
    internal static FabricaConexionesOracle Fabrica => _fabrica;
    internal static RepositorioLoteSimulacionOracle Manifiesto => _manifiesto;
    internal static Reloj Reloj => _reloj;

    static string? Opcion(string[] a, string n) { int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }

    static bool Conectar()
    {
        string id = _perfil switch { "pruebas" => "ecoaccess-p3-pruebas", "produccion" => "ecoaccess-p3-framework", _ => "" };
        if (id == "") { Console.Error.WriteLine("Perfil desconocido (pruebas | produccion)."); return false; }
        string archivo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", id, "secrets.json");
        if (!File.Exists(archivo)) { Console.Error.WriteLine($"Falta el perfil de conexión {id}."); return false; }
        string json = File.ReadAllText(archivo);
        _cadena = ValorJson(json, "ConnectionStrings:OracleP3") ?? "";
        var b = new OracleConnectionStringBuilder(_cadena);
        if (_perfil == "pruebas" && !Regex.IsMatch(b.DataSource ?? "", @"^(localhost|127\.0\.0\.1):\d+/", RegexOptions.IgnoreCase))
        { Console.Error.WriteLine("El perfil «pruebas» debe apuntar a un Oracle local."); return false; }
        if (b.UserID is "SYS" or "SYSTEM" or "ADMIN") { Console.Error.WriteLine("No se usan cuentas administrativas de Oracle."); return false; }
        string? wallet = ValorJson(json, "EcoAccess:Oracle:CarpetaWallet");
        if (_perfil == "produccion")
        {
            wallet ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".oracle", "wallets", "EcoAccess");
            OracleConfiguration.TnsAdmin = wallet;
            OracleConfiguration.WalletLocation = wallet;
        }
        Console.WriteLine($"Perfil {_perfil}: usuario {b.UserID}, servicio {b.DataSource}. Lote {_lote}.");
        return true;
    }

    static string? ValorJson(string json, string clave)
    {
        var m = Regex.Match(json, "\"" + Regex.Escape(clave) + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
        return m.Success ? Regex.Unescape(m.Groups[1].Value) : null;
    }

    internal static string Limpiar(string? texto)
    {
        string t = texto ?? "";
        var b = new OracleConnectionStringBuilder(_cadena);
        if (!string.IsNullOrEmpty(b.Password)) t = t.Replace(b.Password, "********");
        return t;
    }

    internal static void Log(string texto)
    {
        string linea = $"[{DateTime.Now:HH:mm:ss}] {texto}";
        Console.WriteLine(linea);
        _bitacora?.WriteLine(linea);
    }

    internal static async Task<OracleConnection> AbrirAsync()
    {
        var c = new OracleConnection(_cadena);
        await c.OpenAsync();
        return c;
    }

    internal static async Task<object?> EscalarAsync(string sql, params (string, object?)[] p)
    {
        using var c = await AbrirAsync();
        using var cmd = c.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = sql;
        foreach (var (n, v) in p) cmd.Parameters.Add(n, v ?? DBNull.Value);
        object? r = await cmd.ExecuteScalarAsync();
        return r is DBNull ? null : r;
    }

    internal static async Task<List<object?[]>> FilasAsync(string sql, params (string, object?)[] p)
    {
        using var c = await AbrirAsync();
        using var cmd = c.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = sql;
        foreach (var (n, v) in p) cmd.Parameters.Add(n, v ?? DBNull.Value);
        using var r = await cmd.ExecuteReaderAsync();
        var filas = new List<object?[]>();
        while (await r.ReadAsync())
        {
            var f = new object?[r.FieldCount];
            for (int i = 0; i < r.FieldCount; i++) f[i] = r.IsDBNull(i) ? null : r.GetValue(i);
            filas.Add(f);
        }
        return filas;
    }

    internal static async Task<int> EjecutarAsync(string sql, params (string, object?)[] p)
    {
        using var c = await AbrirAsync();
        using var cmd = c.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = sql;
        foreach (var (n, v) in p) cmd.Parameters.Add(n, v ?? DBNull.Value);
        return await cmd.ExecuteNonQueryAsync();
    }

    internal static DateTime LocalAUtc(DateTime local) => DateTime.SpecifyKind(local - Colombia, DateTimeKind.Utc);
    internal static DateTime UtcALocal(DateTime utc) => utc + Colombia;

    /// <summary>Desactiva (o reactiva) solo las cuentas del manifiesto del lote. Las cuentas reales no se tocan.</summary>
    static async Task<int> CambiarActivoAsync(bool activo)
    {
        int n = await EjecutarAsync(
            "UPDATE usuario SET activo = :a WHERE id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = :l AND tabla = 'USUARIO') " +
            "AND correo_electronico LIKE '%@" + Dominio + "'", ("a", activo ? 1 : 0), ("l", _lote));
        Log($"{(activo ? "Reactivadas" : "Desactivadas")} {n} cuentas del lote {_lote} (dominio @{Dominio}).");
        return 0;
    }

    internal static string DominioCorreo => Dominio;
    internal static int SemillaLote => Semilla;
}

/// <summary>Reloj de la simulación: los servicios oficiales fechan cada paso con él. Nunca adelanta al reloj real.</summary>
sealed class Reloj : TimeProvider
{
    private DateTimeOffset _ahora = DateTimeOffset.UtcNow;
    public DateTime Utc
    {
        get => _ahora.UtcDateTime;
        set
        {
            var tope = DateTime.UtcNow.AddMinutes(-2);
            _ahora = new DateTimeOffset(DateTime.SpecifyKind(value > tope ? tope : value, DateTimeKind.Utc));
        }
    }
    public override DateTimeOffset GetUtcNow() => _ahora;
}

/// <summary>Credenciales del lote (solo en disco local del propietario, nunca en el repositorio).</summary>
sealed record Cuenta(string Clave, CodigoRol Rol, string Correo, string Contrasena, string Nombre, string Perfil);

static class Credenciales
{
    public static Dictionary<string, Cuenta> Leer()
    {
        var d = new Dictionary<string, Cuenta>(StringComparer.Ordinal);
        if (!File.Exists(Program.ArchivoCredenciales)) return d;
        foreach (string l in File.ReadAllLines(Program.ArchivoCredenciales, Encoding.UTF8))
        {
            var p = l.Split('\t');
            if (p.Length < 6 || p[0] == "clave") continue;
            d[p[0]] = new Cuenta(p[0], (CodigoRol)int.Parse(p[1]), p[2], p[3], p[4], p[5]);
        }
        return d;
    }

    public static void Agregar(Cuenta c)
    {
        bool nuevo = !File.Exists(Program.ArchivoCredenciales);
        using var w = new StreamWriter(Program.ArchivoCredenciales, true, new UTF8Encoding(false));
        if (nuevo) w.WriteLine("clave\trol\tcorreo\tcontrasena\tnombre\tperfil");
        w.WriteLine($"{c.Clave}\t{(int)c.Rol}\t{c.Correo}\t{c.Contrasena}\t{c.Nombre}\t{c.Perfil}");
    }

    /// <summary>Contraseña aleatoria criptográfica con mayúscula, minúscula, dígito y símbolo (sin caracteres ambiguos).</summary>
    public static string Generar(int largo)
    {
        const string may = "ABCDEFGHJKLMNPQRSTUVWXYZ", min = "abcdefghijkmnopqrstuvwxyz", dig = "23456789", sim = "#$%*+=?@!";
        string todos = may + min + dig + sim;
        using var rng = RandomNumberGenerator.Create();
        char Toma(string de) { var b = new byte[4]; rng.GetBytes(b); return de[(int)(BitConverter.ToUInt32(b, 0) % (uint)de.Length)]; }
        var c = new List<char> { Toma(may), Toma(min), Toma(dig), Toma(sim) };
        while (c.Count < largo) c.Add(Toma(todos));
        // Mezcla de Fisher-Yates con el mismo generador.
        for (int i = c.Count - 1; i > 0; i--) { var b = new byte[4]; rng.GetBytes(b); int j = (int)(BitConverter.ToUInt32(b, 0) % (uint)(i + 1)); (c[i], c[j]) = (c[j], c[i]); }
        return new string(c.ToArray());
    }
}
