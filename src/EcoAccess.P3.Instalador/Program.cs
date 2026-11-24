using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Instalador;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

// Instalador de la base de EcoAccess en Oracle Cloud. Uso (desde la raíz del repositorio):
//   dotnet run --project src/EcoAccess.P3.Instalador -- plan                  muestra versiones aplicadas y pendientes
//   dotnet run --project src/EcoAccess.P3.Instalador -- instalar [--con-demo] aplica solo las pendientes
//   dotnet run --project src/EcoAccess.P3.Instalador -- guardar-conexion      comprueba y guarda la cadena del esquema
//   dotnet run --project src/EcoAccess.P3.Instalador -- preparar-esquema      (ADMIN) crea ECOACCESS_P3_APP y los roles
//   dotnet run --project src/EcoAccess.P3.Instalador -- crear-integrante      (ADMIN) cuenta personal ECO_<NOMBRE> o ECO_DEV_*
//   dotnet run --project src/EcoAccess.P3.Instalador -- auditar               solo lectura: base, esquema, objetos y permisos
//   dotnet run --project src/EcoAccess.P3.Instalador -- auditar-admin         (ADMIN) solo lectura: cuentas ECO*, roles y privilegios
//   dotnet run --project src/EcoAccess.P3.Instalador -- comprobar-cuenta --usuario ECO_JORGE   permisos de un integrante
//   dotnet run --project src/EcoAccess.P3.Instalador -- simular-permisos      (dueño) muestra los GRANT/REVOKE sin ejecutarlos
//   dotnet run --project src/EcoAccess.P3.Instalador -- aplicar-permisos      (dueño) ejecuta 05_conceder_permisos_roles.sql
//   dotnet run --project src/EcoAccess.P3.Instalador -- preparar-prueba-colaborativa           (dueño) tabla de prueba aislada
//   dotnet run --project src/EcoAccess.P3.Instalador -- prueba-colaborativa --usuario ECO_JORGE  escribe y lee la base compartida
//   dotnet run --project src/EcoAccess.P3.Instalador -- marcar V0XX --confirmar  registra una versión verificada a mano
// Opciones: --wallet <carpeta>  --alias <ecoaccess_tp>  --usuario <ECOACCESS_P3_APP>
// Las contraseñas se escriben en esta consola sin mostrarse; nunca se pasan como argumentos ni se imprimen.

Console.OutputEncoding = new UTF8Encoding(false);
const string IdSecretos = "ecoaccess-p3-framework";
Opciones opciones = Opciones.Leer(args);
string? raizEncontrada = BuscarRaiz();
if (raizEncontrada is null)
{
    Console.Error.WriteLine("No se encontró EcoAccess.P3.sln. Ejecuta el instalador desde la carpeta del repositorio.");
    return 2;
}

string raiz = raizEncontrada;
IConfiguration configuracion = new ConfigurationBuilder().AddUserSecrets(IdSecretos).AddEnvironmentVariables("ECOACCESS_").Build();
string carpetaWallet = opciones.Wallet ?? WalletOracleNube.CarpetaDe(configuracion.GetConnectionString("OracleP3"))
    ?? configuracion["EcoAccess:Oracle:CarpetaWallet"] ?? CadenaConexionOracle.CarpetaWallet() ?? WalletOracleNube.CarpetaPredeterminada;
string alias = opciones.Alias ?? configuracion["EcoAccess:Oracle:Alias"] ?? WalletOracleNube.AliasPredeterminado;

WalletOracleNube? walletAbierto = WalletOracleNube.Abrir(carpetaWallet, out string motivo);
if (walletAbierto is null)
{
    Console.Error.WriteLine("Wallet no disponible. " + motivo);
    Console.Error.WriteLine("Descárgalo en Oracle Cloud (base EcoAccess → Database connection → Download wallet) y descomprímelo allí, o usa --wallet <carpeta>.");
    return 3;
}

WalletOracleNube wallet = walletAbierto;
if (!wallet.TryObtenerDestino(alias, out string host, out int puerto))
{
    Console.Error.WriteLine($"El wallet no tiene el alias «{alias}». Disponibles: {string.Join(", ", wallet.Alias)}");
    return 3;
}

Console.WriteLine($"EcoAccess · instalador Oracle Cloud · wallet {wallet.Carpeta} · servicio {alias} ({host}:{puerto})");
using var cancelacion = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancelacion.Cancel(); };
CancellationToken ct = cancelacion.Token;

try
{
    return opciones.Comando switch
    {
        "plan" => await PlanAsync(),
        "instalar" => await InstalarAsync(),
        "guardar-conexion" => await GuardarConexionAsync(),
        "preparar-esquema" => await PrepararEsquemaAsync(),
        "crear-integrante" => await CrearIntegranteAsync(),
        "marcar" => await MarcarAsync(),
        "auditar" => await AuditarAsync(),
        "auditar-admin" => await AuditarAdminAsync(),
        "comprobar-cuenta" => await ComprobarCuentaAsync(),
        "preparar-prueba-colaborativa" => await PrepararPruebaColaborativaAsync(),
        "simular-permisos" => await SimularPermisosAsync(),
        "aplicar-permisos" => await AplicarPermisosAsync(),
        "prueba-colaborativa" => await PruebaColaborativaAsync(),
        _ => Ayuda(),
    };
}
catch (OracleException error)
{
    // Solo el número y la primera línea: el texto del proveedor no incluye contraseñas, pero puede ser largo.
    Console.Error.WriteLine($"Oracle respondió ORA-{error.Number:00000}: {error.Message.Split('\n')[0]}");
    if (error.Number == 1017) Console.Error.WriteLine("Usuario o contraseña incorrectos. Varios intentos fallidos bloquean la cuenta.");
    if (error.Number == 28001) Console.Error.WriteLine("La contraseña está expirada (es la temporal): cámbiala primero en SQL Developer con «Test» y repite.");
    if (error.Number == 28000) Console.Error.WriteLine("La cuenta está bloqueada: pide al propietario «ALTER USER <cuenta> ACCOUNT UNLOCK».");
    if (error.Number is 12506 or 12541 or 12170 or 12545) Console.Error.WriteLine("No se llegó a la base: revisa que esté «Available» en Oracle Cloud y la red.");
    return 4;
}

async Task<OracleConnection> AbrirAsync(string cadena)
{
    var conexion = new OracleConnection(cadena);
    await conexion.OpenAsync(ct);
    using OracleCommand comando = conexion.CreateCommand();
    comando.CommandText =
        "SELECT SYS_CONTEXT('USERENV','SESSION_USER'), SYS_CONTEXT('USERENV','CON_NAME'), " +
        "(SELECT banner FROM v$version WHERE ROWNUM = 1), TO_CHAR(SYS_EXTRACT_UTC(SYSTIMESTAMP), 'YYYY-MM-DD HH24:MI:SS') FROM dual";
    using var lector = await comando.ExecuteReaderAsync(ct);
    await lector.ReadAsync(ct);
    Console.WriteLine($"Conectado como {lector.GetString(0)} en {lector.GetString(1)} · {lector.GetString(2)} · {lector.GetString(3)} UTC");
    return conexion;
}

Task<OracleConnection> ConectarEsquemaAsync()
{
    // La cadena guardada puede traer su wallet (guardar-conexion) o solo el alias (Configurar-Credenciales.ps1 + TNS_ADMIN).
    string? cadena = opciones.Usuario is null ? wallet.CompletarCadena(configuracion.GetConnectionString("OracleP3")) : null;
    if (string.IsNullOrWhiteSpace(cadena))
    {
        string usuario = opciones.Usuario ?? "ECOACCESS_P3_APP";
        cadena = wallet.CrearCadena(alias, usuario, LeerClave($"Contraseña de {usuario}: "));
    }

    return AbrirAsync(cadena!);
}

Task<OracleConnection> ConectarAdminAsync() =>
    AbrirAsync(wallet.CrearCadena(alias, "ADMIN", LeerClave("Contraseña de ADMIN (base EcoAccess): ")));

async Task<int> PlanAsync()
{
    using OracleConnection conexion = await ConectarEsquemaAsync();
    var ejecutor = new EjecutorScriptsOracle(conexion, Console.Out);
    if (!await ejecutor.EjecutarAsync(Ruta("database/00_administracion/03_registro_migraciones.sql"), ct)) return 5;
    Dictionary<string, string> huellas = await ejecutor.LeerHuellasAsync(ct);
    var aplicadas = new HashSet<string>(huellas.Keys, StringComparer.OrdinalIgnoreCase);
    Console.WriteLine();
    foreach (PasoInstalacion paso in PlanInstalacion.Pasos)
    {
        string estado = aplicadas.Contains(paso.Version) ? "aplicada " : paso.Opcional ? "opcional " : "PENDIENTE";
        string aviso = huellas.TryGetValue(paso.Version, out string? h) && h != paso.Huella(raiz) ? "  (el script cambió después de aplicarse)" : "";
        Console.WriteLine($"  {paso.Version}  {estado}  {paso.Descripcion}{aviso}");
    }

    string? orden = PlanInstalacion.ValidarOrden(aplicadas);
    if (orden is not null) Console.WriteLine("\n" + orden);
    return 0;
}

async Task<int> InstalarAsync()
{
    using OracleConnection conexion = await ConectarEsquemaAsync();
    var ejecutor = new EjecutorScriptsOracle(conexion, Console.Out);
    Console.WriteLine("\n[registro] database/00_administracion/03_registro_migraciones.sql");
    if (!await ejecutor.EjecutarAsync(Ruta("database/00_administracion/03_registro_migraciones.sql"), ct)) return 5;

    HashSet<string> aplicadas = await ejecutor.LeerAplicadasAsync(ct);
    string? problema = PlanInstalacion.ValidarOrden(aplicadas);
    if (problema is not null) { Console.Error.WriteLine(problema); return 6; }
    if (aplicadas.Count == 0 && await ContarTablasDominioAsync(conexion) > 0)
    {
        Console.Error.WriteLine("El esquema ya tiene tablas de EcoAccess pero ninguna versión registrada. No se ejecutó nada.");
        Console.Error.WriteLine("Revisa el esquema y registra lo que ya existe con «marcar V0XX --confirmar» antes de instalar.");
        return 6;
    }

    IReadOnlyList<PasoInstalacion> pendientes = PlanInstalacion.Pendientes(aplicadas, opciones.ConDemo);
    if (pendientes.Count == 0) Console.WriteLine("\nNo hay versiones pendientes.");
    foreach (PasoInstalacion paso in pendientes)
    {
        Console.WriteLine($"\n[{paso.Version}] {paso.Descripcion} — {paso.Archivo}");
        var reloj = Stopwatch.StartNew();
        if (!await ejecutor.EjecutarAsync(Ruta(paso.Archivo), ct))
        {
            Console.Error.WriteLine($"\n{paso.Version} se detuvo y NO quedó registrada. Las versiones anteriores sí están registradas.");
            Console.Error.WriteLine("Revisa el mensaje ORA- de arriba; al corregirlo, vuelve a ejecutar «instalar»: continuará desde aquí.");
            return 7;
        }

        await ejecutor.RegistrarAsync(paso, paso.Huella(raiz), "EJECUTADA", reloj.ElapsedMilliseconds, ct);
        Console.WriteLine($"   ✓ {paso.Version} registrada ({reloj.ElapsedMilliseconds} ms)");
    }

    Console.WriteLine("\n[permisos] database/00_administracion/05_conceder_permisos_roles.sql");
    if (!await ejecutor.EjecutarAsync(Ruta("database/00_administracion/05_conceder_permisos_roles.sql"), ct))
        Console.WriteLine("   (los roles se pueden conceder después; no afecta a la aplicación)");
    Console.WriteLine("\nInstalación terminada. Comprueba el resultado con «plan» y en SQL Developer.");
    return 0;
}

// Comprueba la contraseña del esquema con una conexión real y solo entonces la guarda en los User Secrets
// del perfil de Windows (fuera del repositorio), igual que la configuración inicial de Windows Forms.
async Task<int> GuardarConexionAsync()
{
    string usuario = opciones.Usuario ?? "ECOACCESS_P3_APP";
    string cadena = wallet.CrearCadena(alias, usuario, LeerClave($"Contraseña de {usuario}: "));
    using (await AbrirAsync(cadena)) { }

    GuardarSecretos(new Dictionary<string, string>
    {
        ["ConnectionStrings:OracleP3"] = cadena,
        ["EcoAccess:Oracle:CarpetaWallet"] = wallet.Carpeta,
        ["EcoAccess:Oracle:Alias"] = alias,
    });
    Console.WriteLine($"Conexión comprobada y guardada en los User Secrets «{IdSecretos}» de este perfil de Windows.");
    return 0;
}

async Task<int> PrepararEsquemaAsync()
{
    using OracleConnection conexion = await ConectarAdminAsync();
    string clave = LeerClave("Nueva contraseña para ECOACCESS_P3_APP (12-30, mayúscula, minúscula y número): ");
    if (clave != LeerClave("Repítela: ")) { Console.Error.WriteLine("Las contraseñas no coinciden. No se hizo nada."); return 8; }
    var ejecutor = new EjecutorScriptsOracle(conexion, Console.Out);
    bool correcto = await ejecutor.EjecutarAsync(Ruta("database/00_administracion/04_crear_esquema_oracle_cloud.sql"), ct,
        new Dictionary<string, string> { ["clave_esquema"] = clave });
    return correcto ? 0 : 5;
}

async Task<int> CrearIntegranteAsync()
{
    using OracleConnection conexion = await ConectarAdminAsync();
    Console.Write("Usuario (por ejemplo ECO_JORGE): ");
    string usuario = Console.ReadLine()?.Trim() ?? "";
    Console.Write("Rol (CONSULTA o EDICION): ");
    string rol = Console.ReadLine()?.Trim() ?? "";
    string clave = LeerClave("Contraseña temporal (el integrante la cambiará al entrar): ");
    var ejecutor = new EjecutorScriptsOracle(conexion, Console.Out);
    bool correcto = await ejecutor.EjecutarAsync(Ruta("database/00_administracion/06_crear_cuenta_integrante.sql"), ct,
        new Dictionary<string, string> { ["usuario_integrante"] = usuario, ["clave_integrante"] = clave, ["rol_integrante"] = rol });
    return correcto ? 0 : 5;
}

async Task<int> AuditarAsync()
{
    using OracleConnection conexion = await ConectarEsquemaAsync();
    await Auditoria.MostrarAsync(conexion, Auditoria.Identidad.Concat(Auditoria.Esquema), Console.Out, ct);
    return 0;
}

async Task<int> AuditarAdminAsync()
{
    using OracleConnection conexion = await ConectarAdminAsync();
    await Auditoria.MostrarAsync(conexion, Auditoria.Identidad.Take(2).Concat(Auditoria.Administrador), Console.Out, ct);
    return 0;
}

async Task<int> ComprobarCuentaAsync()
{
    if (opciones.Usuario is null) { Console.Error.WriteLine("Uso: comprobar-cuenta --usuario ECO_<NOMBRE>"); return 2; }
    using OracleConnection conexion = await ConectarEsquemaAsync();
    await Auditoria.MostrarAsync(conexion, Auditoria.Identidad, Console.Out, ct);
    Console.WriteLine();
    Console.WriteLine("== Pruebas de permisos (lo autorizado funciona, lo administrativo se rechaza)");
    int fallas = await Auditoria.ComprobarIntegranteAsync(conexion, Console.Out, ct);
    Console.WriteLine(fallas == 0 ? "Permisos correctos." : $"{fallas} prueba(s) para revisar.");
    return fallas == 0 ? 0 : 6;
}

async Task<int> PrepararPruebaColaborativaAsync()
{
    if (opciones.Usuario is not null) { Console.Error.WriteLine("Este comando lo ejecuta el dueño del esquema (sin --usuario)."); return 2; }
    using OracleConnection conexion = await ConectarEsquemaAsync();
    var ejecutor = new EjecutorScriptsOracle(conexion, Console.Out);
    bool correcto = await ejecutor.EjecutarAsync(Ruta("database/00_administracion/07_prueba_colaborativa.sql"), ct)
        && await ejecutor.EjecutarAsync(Ruta("database/00_administracion/05_conceder_permisos_roles.sql"), ct);
    return correcto ? 0 : 5;
}

// Ejecuta 05_conceder_permisos_roles.sql cambiando cada EXECUTE IMMEDIATE por DBMS_OUTPUT: compila el mismo bloque contra la
// base real y lista exactamente los GRANT/REVOKE que haría, sin cambiar ningún permiso.
async Task<int> SimularPermisosAsync()
{
    if (opciones.Usuario is not null) { Console.Error.WriteLine("Este comando lo ejecuta el dueño del esquema (sin --usuario)."); return 2; }
    string original = File.ReadAllText(Ruta("database/00_administracion/05_conceder_permisos_roles.sql"));
    string simulado = System.Text.RegularExpressions.Regex.Replace(original, @"EXECUTE IMMEDIATE\s+(.+?);\s*$",
        "DBMS_OUTPUT.PUT_LINE('   ' || $1);", System.Text.RegularExpressions.RegexOptions.Multiline);
    if (simulado.Contains("EXECUTE IMMEDIATE")) { Console.Error.WriteLine("No se pudo convertir todo el script a simulación; no se ejecutó nada."); return 5; }
    string temporal = Path.Combine(Path.GetTempPath(), "ecoaccess_simulacion_permisos.sql");
    File.WriteAllText(temporal, simulado, new UTF8Encoding(false));
    try
    {
        using OracleConnection conexion = await ConectarEsquemaAsync();
        Console.WriteLine("SIMULACIÓN: estas son las sentencias que ejecutaría el script (no se cambió ningún permiso).");
        var ejecutor = new EjecutorScriptsOracle(conexion, Console.Out);
        return await ejecutor.EjecutarAsync(temporal, ct) ? 0 : 5;
    }
    finally { File.Delete(temporal); }
}

async Task<int> AplicarPermisosAsync()
{
    if (opciones.Usuario is not null) { Console.Error.WriteLine("Este comando lo ejecuta el dueño del esquema (sin --usuario)."); return 2; }
    using OracleConnection conexion = await ConectarEsquemaAsync();
    var ejecutor = new EjecutorScriptsOracle(conexion, Console.Out);
    return await ejecutor.EjecutarAsync(Ruta("database/00_administracion/05_conceder_permisos_roles.sql"), ct) ? 0 : 5;
}

async Task<int> PruebaColaborativaAsync()
{
    if (opciones.Usuario is null) { Console.Error.WriteLine("Uso: prueba-colaborativa --usuario ECO_<NOMBRE>"); return 2; }
    using OracleConnection conexion = await ConectarEsquemaAsync();
    using (OracleCommand insertar = conexion.CreateCommand())
    {
        // Solo la tabla aislada PRUEBA_COLABORATIVA: no toca usuarios, depósitos ni ecopuntos reales.
        // La tabla pone el usuario de Oracle, la fecha y el nombre de la base; no se guarda nada del equipo.
        insertar.CommandText = "INSERT INTO ecoaccess_p3_app.prueba_colaborativa (nota) VALUES ('prueba colaborativa') RETURNING id_prueba INTO :id";
        OracleParameter id = insertar.Parameters.Add("id", OracleDbType.Int32, System.Data.ParameterDirection.Output);
        await insertar.ExecuteNonQueryAsync(ct);
        using OracleCommand confirmar = conexion.CreateCommand();
        confirmar.CommandText = "COMMIT";
        await confirmar.ExecuteNonQueryAsync(ct);
        Console.WriteLine($"Registro de prueba creado con id {id.Value}. Pide a otro integrante que ejecute este mismo comando y compare los id.");
    }
    await Auditoria.MostrarAsync(conexion, new[]
    {
        ("Últimos registros de todos los integrantes (misma base compartida)",
         "SELECT * FROM (SELECT id_prueba, integrante, TO_CHAR(creado_utc,'YYYY-MM-DD HH24:MI:SS') creado_utc, base FROM ecoaccess_p3_app.prueba_colaborativa ORDER BY id_prueba DESC) WHERE ROWNUM <= 10"),
    }, Console.Out, ct);
    return 0;
}

async Task<int> MarcarAsync()
{
    PasoInstalacion? paso = PlanInstalacion.Pasos.FirstOrDefault(p => p.Version.Equals(opciones.Argumento, StringComparison.OrdinalIgnoreCase));
    if (paso is null || !opciones.Confirmar)
    {
        Console.Error.WriteLine("Uso: marcar V0XX --confirmar  (solo después de comprobar en SQL Developer que esa versión ya existe).");
        return 2;
    }

    using OracleConnection conexion = await ConectarEsquemaAsync();
    var ejecutor = new EjecutorScriptsOracle(conexion, Console.Out);
    if (!await ejecutor.EjecutarAsync(Ruta("database/00_administracion/03_registro_migraciones.sql"), ct)) return 5;
    await ejecutor.RegistrarAsync(paso, paso.Huella(raiz), "MARCADA", 0, ct);
    Console.WriteLine($"{paso.Version} registrada como MARCADA (verificada por una persona, no ejecutada por el instalador).");
    return 0;
}

async Task<int> ContarTablasDominioAsync(OracleConnection conexion)
{
    using OracleCommand comando = conexion.CreateCommand();
    comando.CommandText = "SELECT COUNT(*) FROM user_tables WHERE table_name IN ('USUARIO','MATERIAL','CONTENEDOR','ROL')";
    return Convert.ToInt32(await comando.ExecuteScalarAsync(ct));
}

void GuardarSecretos(IReadOnlyDictionary<string, string> valores)
{
    string ruta = Microsoft.Extensions.Configuration.UserSecrets.PathHelper.GetSecretsPathFromSecretsId(IdSecretos);
    Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
    JsonObject json = File.Exists(ruta) && JsonNode.Parse(File.ReadAllText(ruta)) is JsonObject existente ? existente : new JsonObject();
    foreach (KeyValuePair<string, string> par in valores) json[par.Key] = par.Value;
    string temporal = ruta + ".tmp";
    File.WriteAllText(temporal, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    if (File.Exists(ruta)) File.Delete(ruta);
    File.Move(temporal, ruta);
}

string Ruta(string relativa) => Path.Combine(raiz, relativa.Replace('/', Path.DirectorySeparatorChar));

static int Ayuda()
{
    Console.WriteLine("Comandos: plan | instalar [--con-demo] | guardar-conexion | preparar-esquema | crear-integrante | marcar V0XX --confirmar");
    Console.WriteLine("          auditar | auditar-admin | comprobar-cuenta --usuario ECO_X | preparar-prueba-colaborativa | prueba-colaborativa --usuario ECO_X");
    Console.WriteLine("          simular-permisos | aplicar-permisos");
    Console.WriteLine("Opciones: --wallet <carpeta> --alias <alias> --usuario <usuario>");
    return 1;
}

static string? BuscarRaiz()
{
    foreach (string inicio in new[] { Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory })
    {
        for (DirectoryInfo? carpeta = new DirectoryInfo(inicio); carpeta is not null; carpeta = carpeta.Parent)
            if (File.Exists(Path.Combine(carpeta.FullName, "EcoAccess.P3.sln"))) return carpeta.FullName;
    }

    return null;
}

/// <summary>Lee una contraseña sin mostrarla (asteriscos). Exige una consola interactiva.</summary>
static string LeerClave(string mensaje)
{
    if (Console.IsInputRedirected)
        throw new InvalidOperationException("Las contraseñas solo se escriben en una consola interactiva.");
    while (true)
    {
        Console.Write(mensaje);
        var clave = new StringBuilder();
        while (true)
        {
            ConsoleKeyInfo tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Enter) break;
            if (tecla.Key == ConsoleKey.Backspace)
            {
                if (clave.Length > 0) { clave.Length--; Console.Write("\b \b"); }
                continue;
            }

            if (!char.IsControl(tecla.KeyChar)) { clave.Append(tecla.KeyChar); Console.Write('*'); }
        }

        Console.WriteLine();
        // Una contraseña vacía (Enter accidental o pegado fallido) no se envía: Oracle la rechaza y cuenta como intento.
        if (clave.Length > 0) return clave.ToString();
        Console.WriteLine("No se escribió nada. Vuelve a intentarlo (clic derecho o Ctrl+V para pegar).");
    }
}

/// <summary>Argumentos de la línea de comandos, sin dependencias externas.</summary>
internal sealed record Opciones(string Comando, string? Argumento, string? Wallet, string? Alias, string? Usuario, bool ConDemo, bool Confirmar)
{
    public static Opciones Leer(string[] args)
    {
        string comando = args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal) ? args[0].ToLowerInvariant() : "plan";
        string? argumento = args.Length > 1 && !args[1].StartsWith("--", StringComparison.Ordinal) ? args[1] : null;
        string? Valor(string nombre)
        {
            int i = Array.FindIndex(args, a => a.Equals(nombre, StringComparison.OrdinalIgnoreCase));
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        bool Bandera(string nombre) => args.Any(a => a.Equals(nombre, StringComparison.OrdinalIgnoreCase));
        return new Opciones(comando, argumento, Valor("--wallet"), Valor("--alias"), Valor("--usuario"), Bandera("--con-demo"), Bandera("--confirmar"));
    }
}
