// Proyecto EcoAccess P3 - Jorge Gutierrez Thomas
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Bot;
using EcoAccess.P3.Bot.Dispositivos;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Logica.Servicios;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Bot.Transportes;

// Raíz de composición del bot: el único lugar del proyecto que conoce Datos. Los secretos se leen de
// los User Secrets «ecoaccess-p3-framework» o de variables de entorno ECOACCESS_ (ECOACCESS_Telegram__TokenBot…) y nunca
// se escriben en la consola.
//   dotnet run --project src/EcoAccess.P3.Bot               polling con Telegram real
//   dotnet run --project src/EcoAccess.P3.Bot -- --simulado  chat de consola contra Oracle, sin Telegram
//   dotnet run --project src/EcoAccess.P3.Bot -- --webhook   recibe updates por HTTP detrás de un proxy HTTPS
//   dotnet run --project src/EcoAccess.P3.Bot -- --bridge    despacha órdenes y recibe eventos locales autenticados
//   dotnet run --project src/EcoAccess.P3.Bot -- --dispositivo-simulado  adapta CanecaSimulada por HTTP local
//   dotnet run --project src/EcoAccess.P3.Bot -- --estacion-web  recibe aperturas de la estación web y crea las órdenes
// La página de registro solo inicia si existe Telegram:PaginaRegistro HTTPS; la consulta pública no la requiere.
// El supervisor lee tuberías UTF-8 también cuando Windows ejecuta el proceso sin consola.
Console.OutputEncoding = new System.Text.UTF8Encoding(false);
// .NET Framework permite solo 2 conexiones simultáneas por servidor: el largo polling ocupaba una y getFile se quedaba
// esperando hasta vencer («No pude descargar la foto»). Debe fijarse antes de la primera petición HTTP.
System.Net.ServicePointManager.DefaultConnectionLimit = Math.Max(System.Net.ServicePointManager.DefaultConnectionLimit, 16);
string modo = args.Length == 0 ? "--polling" : args[0];
bool pollingSinDispositivo = modo == "--polling" && args.Length == 2 && args[1] == "--sin-dispositivo";
if (pollingSinDispositivo)
    GuardiaHardware.BloquearEsteProceso("sondeo explícito sin dispositivo");
if (modo == "--supervisar-en-segundo-plano") return SupervisorServicios.IniciarEnSegundoPlano();
if (modo == "--supervisar") return await SupervisorServicios.EjecutarAsync();
if (modo == "--simulado") Console.InputEncoding = new System.Text.UTF8Encoding(false);
// Solo el bot, el webhook, los bridges reales y la prueba presencial explícitamente confirmada pueden llegar a una tapa.
// Evaluaciones, diagnósticos y simulaciones quedan bloqueados en el proceso (GuardiaHardware), además de ECOACCESS_SIN_HARDWARE=1.
bool pruebaFisicaConfirmada = modo == "--probar-estacion-fisica"
    && args.Any(a => string.Equals(a, "--confirmar-presencia-supervisada", StringComparison.OrdinalIgnoreCase))
    && !args.Any(a => string.Equals(a, "--simulado", StringComparison.OrdinalIgnoreCase));
if (modo is not ("--polling" or "--webhook" or "--bridge" or "--dispositivo-serial") && !pruebaFisicaConfirmada)
    EcoAccess.P3.Bot.Dispositivos.GuardiaHardware.BloquearEsteProceso($"modo {modo}, que no actúa sobre la estación");
if (modo == "--estado-vision-local")
{
    if (args.Length != 1) return 2;
    using var diagnosticoLocal = new DiagnosticoOllamaLocal();
    var salud = await diagnosticoLocal.ComprobarAsync();
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(salud));
    return salud.ListoParaInferencia ? 0 : 1;
}
if (modo == "--auditar-vision-ollama")
    return await ComandoVision35.AuditarOllamaAsync(args);
if (modo == "--probar-imagen35")
    return await ComandoVision35.ProbarImagenAsync(args);
if (modo == "--evaluar-catalogo35")
    return await ComandoVision35.EvaluarManifiestoAsync(args);
if (modo == "--comparar-modelos35")
    return await ComandoVision35.CompararModelosAsync(args);
if (modo == "--evaluar-expo9")
    return await ComandoExpo9.EvaluarAsync(args);
if (modo == "--comparar-latencia-expo9")
    return await ComandoExpo9.CompararLatenciaAsync(args);
if (modo == "--probar-registro-expo9")
{
    // Raíz de composición: el repositorio de Oracle se crea aquí, con el User Secret indicado (por defecto el esquema aislado).
    int iSecreto = Array.IndexOf(args, "--secreto");
    string idSecreto = iSecreto >= 0 && iSecreto + 1 < args.Length ? args[iSecreto + 1] : "ecoaccess-p3-pruebas";
    string archivoSecreto = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", idSecreto, "secrets.json");
    if (!File.Exists(archivoSecreto)) { Console.Error.WriteLine("No existe el User Secret " + idSecreto); return 2; }
    string cadenaPrueba;
    using (var d = System.Text.Json.JsonDocument.Parse(File.ReadAllText(archivoSecreto))) cadenaPrueba = d.RootElement.GetProperty("ConnectionStrings:OracleP3").GetString()!;
    var repoPrueba = new RepositorioOperacionesEstacionOracle(new FabricaConexionesOracle(cadenaPrueba));
    return await ComandoExpo9.ProbarRegistroAsync(repoPrueba, () => repoPrueba.TablaDisponibleAsync(),
        async () => (await repoPrueba.EstadisticasAsync(DateTime.Now.AddDays(-2), DateTime.Now.AddDays(2)))
            .Select(f => (f.Clave, f.CodigoResiduo, f.Caneca, f.TipoUsuario, f.Metodo, f.DuracionMs)).ToList());
}
if (modo == "--probar-estacion-fisica")
    return await ComandoEstacionFisica.EjecutarAsync(args);
bool resolverCaneca = modo == "--resolver-caneca";
int idPunto = 0;
int idMaterial = 0;
if (resolverCaneca && (args.Length != 3 || !int.TryParse(args[1], out idPunto) || idPunto <= 0
    || !int.TryParse(args[2], out idMaterial) || idMaterial <= 0))
{
    Console.Error.WriteLine("Uso: EcoAccess.P3.Bot --resolver-caneca <id-punto> <id-material>. Usa enteros positivos.");
    return 2;
}

bool evaluar = modo == "--evaluar-vision";
bool comparar = modo == "--comparar-vision";
bool comprobarVision = modo == "--comprobar-vision-local";
bool comprobarVisionGroq = modo == "--comprobar-vision-groq";
bool auditar = modo == "--auditar-recorrido";
bool evaluarObjetos = modo == "--evaluar-objetos";
bool evaluarMultiobjeto = modo == "--evaluar-multiobjeto";
bool generarLote = modo == "--generar-lote-simulacion";
int maximoEvaluacion = 0;
int minutosAuditoria = 0;
if (!resolverCaneca && (generarLote ? args.Length != 2 || !EcoAccess.P3.Datos.Repositorios.RepositorioLoteSimulacionOracle.LoteValido(args[1])
    : auditar ? args.Length != 2 || !int.TryParse(args[1], out minutosAuditoria) || minutosAuditoria is < 1 or > 10080
    : evaluar || comparar ? args.Length != 3 || !int.TryParse(args[2], out maximoEvaluacion) || maximoEvaluacion is < 1 or > EvaluacionVisionArchivos.MaximoSolicitudes
    : comprobarVision || comprobarVisionGroq ? args.Length != 2
    : evaluarObjetos ? args.Length != 3 || args[2] is not ("local" or "groq")
    : evaluarMultiobjeto ? args.Length != 2
    : modo is not ("--polling" or "--simulado" or "--webhook" or "--bridge" or "--dispositivo-simulado" or "--dispositivo-serial" or "--estacion-web" or "--comprobar-telegram" or "--comprobar-oracle" or "--comprobar-groq" or "--estado-vision" or "--restablecer-contrasena") || (args.Length > 1 && !pollingSinDispositivo)))
{
    Console.Error.WriteLine("Uso: EcoAccess.P3.Bot [--polling [--sin-dispositivo] | --simulado | --webhook | --bridge | --dispositivo-simulado | --dispositivo-serial | --estacion-web | --comprobar-telegram | --comprobar-oracle | --comprobar-groq | --estado-vision] o --evaluar-vision <manifiesto.json> <máximo-1-a-200> o --comparar-vision <manifiesto.json> <máximo-1-a-200> o --comprobar-vision-local <imagen> o --comprobar-vision-groq <imagen> o --auditar-recorrido <minutos-1-a-10080> o --resolver-caneca <id-punto> <id-material>");
    return 2;
}

IConfiguration configuracion;
try
{
    var constructorConfiguracion = new ConfigurationBuilder();
    string? elegido = Environment.GetEnvironmentVariable("ECOACCESS_ARCHIVO_BRIDGE");
    string archivoBridge = elegido ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Bridge", "bridge.local.json");
    if (!pollingSinDispositivo && !resolverCaneca)
    {
        if (elegido is not null && (!Compat.IsPathFullyQualified(archivoBridge) || !File.Exists(archivoBridge)))
            throw new ArgumentException("Archivo local de configuración no disponible.");
        if (File.Exists(archivoBridge))
        {
            ConfiguracionBridge.ValidarRutaPrivada(Path.GetDirectoryName(archivoBridge)!);
            constructorConfiguracion.AddJsonFile(archivoBridge, optional: false, reloadOnChange: false);
        }
    }
    // El dispositivo recibe únicamente su configuración/clave: no carga secretos Oracle ni Telegram.
    if (modo is "--dispositivo-simulado" or "--dispositivo-serial")
    {
        const string prefijo = "ECOACCESS_Bridge__";
        var variables = Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>()
            .Where(e => e.Key is string nombre && nombre.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
            .Select(e => new KeyValuePair<string, string?>("Bridge:" + ((string)e.Key)[prefijo.Length..].Replace("__", ":"), (string?)e.Value));
        configuracion = constructorConfiguracion.AddInMemoryCollection(variables).Build();
    }
    else configuracion = constructorConfiguracion.AddUserSecrets(typeof(ProcesadorUpdates).Assembly, optional: true)
        .AddEnvironmentVariables("ECOACCESS_").Build();
}
catch (Exception)
{
    Console.Error.WriteLine("No se pudo leer la configuración local. Revisa el archivo del bridge y los User Secrets; no compartas sus contenidos.");
    return 1;
}

if (modo == "--dispositivo-simulado")
    return await AplicacionBridge.EjecutarSimuladorAsync(configuracion, TimeProvider.System);
if (modo == "--dispositivo-serial")
    return await AplicacionBridge.EjecutarSerialAsync(configuracion, TimeProvider.System);
if (modo == "--comprobar-telegram")
    return await DiagnosticoTelegram.EjecutarAsync(configuracion, Console.WriteLine);
var jsonLegible = new System.Text.Json.JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true };
if (modo == "--comprobar-groq")
{
    var saludGroq = await DiagnosticoGroq.ComprobarAsync(ConfiguracionVision.LeerGroq(configuracion, Console.Error.WriteLine));
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { Estado = saludGroq.Estado.ToString(), saludGroq.Modelo, saludGroq.Mensaje }, jsonLegible));
    return saludGroq.Estado == EstadoGroq.Operativo ? 0 : 1;
}
if (modo == "--estado-vision")
{
    using var diagnosticoEstado = new DiagnosticoOllamaLocal();
    var saludLocal = await diagnosticoEstado.ComprobarAsync();
    string directorioVision = Path.GetDirectoryName(EstadoVisionLocal.Archivo)!;
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
    {
        Modo = ConfiguracionVision.LeerModo(configuracion, Console.Error.WriteLine).ToString(),
        Local = new { saludLocal.Disponible, saludLocal.ModeloInstalado, saludLocal.ModeloResidente, saludLocal.Version, Modelo = ClasificadorOllamaLocal.Modelo, UltimaInferencia = EstadoVisionLocal.Leer() },
        Groq = new
        {
            ClaveConfigurada = ConfiguracionVision.LeerGroq(configuracion, Console.Error.WriteLine) is not null,
            Modelo = configuracion["Vision:Groq:Modelo"] ?? ConfiguracionVision.ModeloGroq,
            MaximoSolicitudesDia = ConfiguracionVision.LeerGroq(configuracion, _ => { })?.MaximoSolicitudesDia,
            PresupuestoDiaUsd = ConfiguracionVision.LeerGroq(configuracion, _ => { })?.PresupuestoDiaUsd,
            UltimaInferencia = EstadoVisionLocal.Leer(Path.Combine(directorioVision, ClasificadorGroq.ArchivoEstado)),
            UsoHoy = UsoGroq.Leer(),
        },
    }, jsonLegible));
    return 0;
}
string? cadena = configuracion.GetConnectionString("OracleP3");
if (Compat.IsNullOrWhiteSpace(cadena))
{
    Console.Error.WriteLine("Falta ConnectionStrings:OracleP3 en los User Secrets «ecoaccess-p3-framework».");
    return 1;
}

FabricaConexionesOracle fabrica;
try
{
    fabrica = new FabricaConexionesOracle(cadena);
}
catch (ArgumentException)
{
    // El texto de la excepción podría citar parte de la cadena: no se muestra.
    Console.Error.WriteLine("La cadena ConnectionStrings:OracleP3 no es válida.");
    return 1;
}

if (resolverCaneca)
{
    try
    {
        using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        int idCaneca = await new RepositorioEstacionesOracle(fabrica)
            .ResolverCanecaDisponibleAsync(idPunto, idMaterial, limite.Token);
        Console.WriteLine($"Punto {idPunto}, material {idMaterial}, caneca {idCaneca}.");
        return 0;
    }
    catch (Oracle.ManagedDataAccess.Client.OracleException error)
    {
        Console.Error.WriteLine($"Oracle no pudo resolver la caneca (ORA-{error.Number:00000}).");
        return 1;
    }
    catch (OperationCanceledException)
    {
        Console.Error.WriteLine("Se agotó el tiempo para consultar la caneca en Oracle.");
        return 1;
    }
    catch (Exception)
    {
        Console.Error.WriteLine("No se pudo obtener la caneca desde Oracle. Revisa la configuración privada.");
        return 1;
    }
}

if (modo == "--comprobar-oracle")
{
    try
    {
        using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var catalogo = await new RepositorioMaterialesOracle(fabrica).ListarAsync(true, limite.Token);
        Console.WriteLine($"Oracle respondió a la consulta del catálogo público: {catalogo.Count} materiales habilitados. No se modificaron datos ni se aplicaron migraciones.");
        return 0;
    }
    catch (Exception) { Console.Error.WriteLine("No se pudo abrir la conexión Oracle. Revisa Docker, servicio y configuración privada."); return 1; }
}
if (generarLote)
{
    // Fase 8: datos experimentales marcados SIMULACION (V025). Idempotente: repetir con el mismo lote no duplica nada.
    try
    {
        var resultado = await new EcoAccess.P3.Datos.Simulacion.GeneradorLoteSimulacion(fabrica, Console.Error.WriteLine)
            .GenerarAsync(args[1], DateTime.UtcNow, 20261110);
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(resultado, jsonLegible));
        return resultado.Rechazos.Count == 0 ? 0 : 1;
    }
    catch (Exception error) { Console.Error.WriteLine("No se pudo generar el lote: " + error.Message); return 1; }
}
if (auditar)
{
    // Solo SELECT y solo conteos: comprueba el recorrido de Telegram sin citar personas ni mensajes.
    try
    {
        var auditoria = await new RepositorioAuditoriaRecorridoOracle(fabrica).ConsultarAsync(DateTime.UtcNow.AddMinutes(-minutosAuditoria));
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(auditoria, jsonLegible));
        return auditoria.SinDuplicados ? 0 : 1;
    }
    catch (Exception error) { Console.Error.WriteLine($"No se pudo consultar Oracle ({error.GetType().Name})."); return 1; }
}
var usuarios = new RepositorioUsuariosOracle(fabrica);
if (modo == "--restablecer-contrasena")
    return await RestablecimientoLocal.EjecutarAsync(
        new ServicioAutenticacion(new RepositorioCredencialesOracle(fabrica), usuarios, new HasherPbkdf2(), TimeProvider.System));
var contenedores = new RepositorioContenedoresOracle(fabrica);
var materiales = new RepositorioMaterialesOracle(fabrica);
if (modo == "--estacion-web")
{
    // Solo escribe en Oracle; la guardia ya bloqueó el hardware en este proceso. Las tapas las mueven los bridges.
    var servicioEstacion = new ServicioInteracciones(usuarios, new RepositorioInteraccionesOracle(fabrica), contenedores, materiales, TimeProvider.System);
    return await EcoAccess.P3.Bot.EstacionWeb.AplicacionEstacionWeb.EjecutarAsync(configuracion, servicioEstacion, contenedores, materiales, TimeProvider.System);
}
if (comprobarVision)
{
    // Inferencia visual real con el modelo local y el catálogo de Oracle; no crea solicitudes, órdenes ni trazas.
    EstadoVisionLocal.Origen = "comprobacion";
    try
    {
        byte[] contenido = await Compat.ReadAllBytesAsync(args[1]);
        string? tipo = ServicioClasificacionImagen.TipoMimeReal(contenido);
        if (tipo is null) { Console.Error.WriteLine("La imagen debe ser JPEG, PNG o WebP de hasta 5 MB."); return 2; }
        using var local = new ClasificadorOllamaLocal(configuracion["Vision:Ollama:MantenerCargado"] ?? "15m");
        var servicio = new ServicioClasificacionImagen(local, materiales, new ServicioCatalogoAmbiental(materiales, contenedores),
            ClasificadorOllamaLocal.TiempoMaximoInferencia + TimeSpan.FromSeconds(10));
        int? demo = configuracion["Telegram:QrDemo"] is { Length: > 0 } qr ? (await contenedores.ObtenerPorIdentificadorQrAsync(qr))?.Id : null;
        var cronometro = System.Diagnostics.Stopwatch.StartNew();
        var r = await servicio.ClasificarAsync(new EcoAccess.P3.Logica.Contratos.ImagenResiduo(contenido, tipo), demo);
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
        {
            Archivo = Path.GetFileName(args[1]),
            Modelo = ClasificadorOllamaLocal.Modelo,
            Resultado = r.Exitoso ? r.Valor.Resultado.ToString() : "Rechazo",
            Material = r.Exitoso ? r.Valor.Consulta?.Material.Codigo : null,
            Admision = r.Exitoso ? r.Valor.Consulta?.Admision.ToString() : null,
            Confianza = r.Exitoso ? r.Valor.Confianza : null,
            Mensaje = r.Exitoso ? r.Valor.Mensaje : r.Mensaje,
            Milisegundos = cronometro.ElapsedMilliseconds,
            Diagnostico = EstadoVisionLocal.Leer()
        }, new System.Text.Json.JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        return r.Exitoso && r.Valor.Resultado != ResultadoClasificacion.ProveedorNoDisponible ? 0 : 1;
    }
    catch (Exception error) { Console.Error.WriteLine($"Comprobación visual local no completada ({error.GetType().Name})."); return 1; }
}
if (evaluarMultiobjeto)
{
    // Fotos con varios residuos (misión nocturna): solo el modelo local; no consulta canecas, no crea solicitudes ni abre nada.
    EstadoVisionLocal.Origen = "comparacion";
    using var localMulti = new ClasificadorOllamaLocal(configuracion["Vision:Ollama:MantenerCargado"] ?? "15m");
    await localMulti.PrecalentarAsync();
    var servicioMulti = new ServicioClasificacionImagen(localMulti, materiales, new ServicioCatalogoAmbiental(materiales, contenedores), ClasificadorOllamaLocal.TiempoMaximoInferencia + TimeSpan.FromSeconds(10));
    var informeMulti = await EvaluacionMultiobjeto.EjecutarAsync(args[1], servicioMulti, localMulti.Proveedor);
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(informeMulti, new System.Text.Json.JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
    return 0;
}
if (evaluarObjetos)
{
    // Solo lee catálogo y admisión. Con «groq» quien ejecuta decide enviar esas fotos: el manifiesto solo debe tener fotos públicas.
    EstadoVisionLocal.Origen = "comparacion";
    try
    {
        var catalogoAmbiental = new ServicioCatalogoAmbiental(materiales, contenedores);
        // La admisión se mide contra la Estación Oficial EcoAccess real (por su nombre) y solo si no existe contra la de Telegram:QrDemo.
        // Antes se prefería QrDemo: una caneca DEMO cuyos materiales y estado cambiaron hizo que dos mediciones del mismo
        // código usaran canecas distintas sin avisarlo. Ahora se informa siempre cuál se usa.
        var canecaEvaluada = (await contenedores.ListarAsync()).FirstOrDefault(c => ServicioCatalogoAmbiental.EsEstacionOficial(c))
            ?? (configuracion["Telegram:QrDemo"] is { Length: > 0 } qrPilotoEvaluado ? await contenedores.ObtenerPorIdentificadorQrAsync(qrPilotoEvaluado) : null);
        int? piloto = canecaEvaluada?.Id;
        Console.Error.WriteLine(canecaEvaluada is null ? "Admisión: sin caneca (solo se mide el reconocimiento)."
            : $"Admisión contra la caneca {canecaEvaluada.Id} «{canecaEvaluada.Nombre}», {(canecaEvaluada.EstaEnServicio ? "en servicio" : "NO en servicio")}.");
        // Solo se reanuda con el mismo código: la huella de Lógica y del Bot cambia en cada versión (compilación determinista).
        // Antes, una medición «después de corregir» reutilizó las respuestas de la versión anterior.
        string huella = typeof(InterpretadorNombreObjeto).Assembly.ManifestModule.ModuleVersionId.ToString("N").Substring(0, 8)
            + typeof(EvaluacionObjetos).Assembly.ManifestModule.ModuleVersionId.ToString("N").Substring(0, 8);
        // La variante de medición también separa el avance: sin esto, la medición «con pregunta de contenido» reanudó las respuestas
        // de la medición «sin pregunta» del mismo código (14/11/2026).
        string variante = Environment.GetEnvironmentVariable("ECOACCESS_VISION_SIN_PREGUNTA_LLENO") == "1" ? ".sin-pregunta-lleno" : "";
        string progreso = args[1] + "." + args[2] + "." + huella + variante + ".progreso";
        InformeObjetos informe;
        if (args[2] == "local")
        {
            using var local = new ClasificadorOllamaLocal(configuracion["Vision:Ollama:MantenerCargado"] ?? "15m");
            await local.PrecalentarAsync();
            var servicio = new ServicioClasificacionImagen(local, materiales, catalogoAmbiental, ClasificadorOllamaLocal.TiempoMaximoInferencia + TimeSpan.FromSeconds(10));
            informe = await EvaluacionObjetos.EjecutarAsync(args[1], servicio, local.Proveedor, piloto, EcoAccess.P3.Logica.Contratos.EnvioExterno.Prohibido, TimeSpan.Zero, progreso);
        }
        else
        {
            var ajustes = ConfiguracionVision.LeerGroq(configuracion, Console.Error.WriteLine);
            if (ajustes is null) { Console.Error.WriteLine("Falta la clave de Groq."); return 1; }
            using var remoto = new ClasificadorGroq(ajustes, new UsoGroq(), Console.Error.WriteLine);
            var servicio = new ServicioClasificacionImagen(remoto, materiales, catalogoAmbiental, remoto.TiempoMaximo + TimeSpan.FromSeconds(5));
            int pausa = int.TryParse(configuracion["Vision:Groq:PausaEntreFotosSegundos"], out int segundos) && segundos is >= 0 and <= 120 ? segundos : 20;
            informe = await EvaluacionObjetos.EjecutarAsync(args[1], servicio, remoto.Proveedor, piloto, EcoAccess.P3.Logica.Contratos.EnvioExterno.Autorizado, TimeSpan.FromSeconds(pausa), progreso);
        }
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(informe, jsonLegible));
        return informe.FallosTecnicos == 0 ? 0 : 1;
    }
    catch (Exception error)
    {
        Console.Error.WriteLine($"La evaluación no se completó ({(error is ArgumentException ? error.Message : error.GetType().Name)}). No se declara un resultado.");
        return 1;
    }
}
if (comprobarVisionGroq)
{
    // Quien ejecuta este comando decide enviar esa imagen a Groq. No crea solicitudes, órdenes ni depósitos.
    EstadoVisionLocal.Origen = "comprobacion";
    var ajustesGroq = ConfiguracionVision.LeerGroq(configuracion, Console.Error.WriteLine);
    if (ajustesGroq is null) { Console.Error.WriteLine("Falta la clave de Groq. Ejecuta scripts\\instalacion\\Configurar-Credenciales.ps1."); return 1; }
    try
    {
        byte[] contenido = await Compat.ReadAllBytesAsync(args[1]);
        string? tipo = ServicioClasificacionImagen.TipoMimeReal(contenido);
        if (tipo is null) { Console.Error.WriteLine("La imagen debe ser JPEG, PNG o WebP de hasta 5 MB."); return 2; }
        using var remoto = new ClasificadorGroq(ajustesGroq, new UsoGroq(), Console.Error.WriteLine);
        var servicio = new ServicioClasificacionImagen(remoto, materiales, new ServicioCatalogoAmbiental(materiales, contenedores), remoto.TiempoMaximo + TimeSpan.FromSeconds(5));
        var cronometro = System.Diagnostics.Stopwatch.StartNew();
        var r = await servicio.ClasificarAsync(new EcoAccess.P3.Logica.Contratos.ImagenResiduo(contenido, tipo, EcoAccess.P3.Logica.Contratos.EnvioExterno.Autorizado));
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
        {
            Archivo = Path.GetFileName(args[1]),
            Modelo = ajustesGroq.Modelo,
            Resultado = r.Exitoso ? r.Valor.Resultado.ToString() : "Rechazo",
            Material = r.Exitoso ? r.Valor.Consulta?.Material.Codigo : null,
            Mensaje = r.Exitoso ? r.Valor.Mensaje : r.Mensaje,
            Milisegundos = cronometro.ElapsedMilliseconds,
            Diagnostico = EstadoVisionLocal.Leer(Path.Combine(Path.GetDirectoryName(EstadoVisionLocal.Archivo)!, ClasificadorGroq.ArchivoEstado)),
            UsoHoy = UsoGroq.Leer(),
            Limites = remoto.UltimosLimites,
        }, jsonLegible));
        return r.Exitoso && r.Valor.Resultado != ResultadoClasificacion.ProveedorNoDisponible ? 0 : 1;
    }
    catch (Exception error) { Console.Error.WriteLine($"Comprobación de Groq no completada ({error.GetType().Name})."); return 1; }
}
if (comparar)
{
    // Solo lee el catálogo: no crea solicitudes, órdenes, depósitos ni ecopuntos.
    EstadoVisionLocal.Origen = "comparacion";
    var ajustesGroq = ConfiguracionVision.LeerGroq(configuracion, Console.Error.WriteLine);
    try
    {
        var catalogoEvaluacion = await materiales.ListarAsync(true);
        var motores = new List<ResumenVision>();
        using (var local = new ClasificadorOllamaLocal(configuracion["Vision:Ollama:MantenerCargado"] ?? "15m"))
        {
            await local.PrecalentarAsync();
            var informeLocal = await EvaluacionVisionArchivos.EjecutarAsync(args[1], maximoEvaluacion, local, catalogoEvaluacion, contenedores, TimeProvider.System,
                tiempoMaximo: ClasificadorOllamaLocal.TiempoMaximoInferencia + TimeSpan.FromSeconds(10));
            motores.Add(MetricasVision.Calcular(informeLocal, catalogoEvaluacion));
        }
        if (ajustesGroq is null) Console.Error.WriteLine("Sin clave de Groq: la comparación solo mide el motor local. No se inventan resultados del otro motor.");
        else
        {
            var uso = new UsoGroq();
            var antes = uso.Actual;
            using var remoto = new ClasificadorGroq(ajustesGroq, uso, Console.Error.WriteLine);
            string rutaProgreso = ProgresoEvaluacion.Ruta(Path.GetDirectoryName(UsoGroq.ArchivoPredeterminado)!, args[1]);
            var progreso = ProgresoEvaluacion.Leer(rutaProgreso, args[1], remoto.Proveedor);
            int yaMedidas = progreso.Muestras.Count;
            void GuardarProgreso()
            {
                var consumo = uso.Actual;
                progreso.TokensEntrada += consumo.TokensEntrada - antes.TokensEntrada;
                progreso.TokensSalida += consumo.TokensSalida - antes.TokensSalida;
                progreso.CostoUsd += consumo.CostoUsd - antes.CostoUsd;
                antes = consumo;
                progreso.Guardar(rutaProgreso);
            }
            // El límite publicado de 8000 tokens por minuto, con unos 2600 por foto, deja una foto cada 20 s.
            int pausaGroq = int.TryParse(configuracion["Vision:Groq:PausaEntreFotosSegundos"], out int pausa) && pausa is >= 0 and <= 120 ? pausa : 20;
            InformeVisionArchivos informeGroq;
            try
            {
                informeGroq = await EvaluacionVisionArchivos.EjecutarAsync(args[1], maximoEvaluacion, remoto, catalogoEvaluacion, contenedores, TimeProvider.System,
                    tiempoMaximo: remoto.TiempoMaximo + TimeSpan.FromSeconds(5), envio: EcoAccess.P3.Logica.Contratos.EnvioExterno.Autorizado, pausaEntreMuestras: TimeSpan.FromSeconds(pausaGroq), esperaProveedor: () => remoto.EsperaPendiente,
                    previos: progreso.Muestras.ToDictionary(m => m.Sha256, StringComparer.OrdinalIgnoreCase),
                    alMedir: detalle => { progreso.Muestras.Add(detalle); GuardarProgreso(); });
            }
            catch (EcoAccess.P3.Logica.Contratos.CuotaVisionAgotadaException)
            {
                GuardarProgreso();
                Console.Error.WriteLine($"Groq pidió una espera larga: {progreso.Muestras.Count} de {maximoEvaluacion} fotos medidas y guardadas ({progreso.Muestras.Count - yaMedidas} en esta corrida). Repite el comando más tarde: continúa desde ahí sin volver a enviarlas.");
                return 3;
            }
            GuardarProgreso();
            motores.Add(MetricasVision.Calcular(informeGroq, catalogoEvaluacion, progreso.TokensEntrada, progreso.TokensSalida, progreso.CostoUsd));
        }
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new ComparacionVisionInforme(DateTime.UtcNow, Path.GetFileName(args[1]), "ver manifiesto", maximoEvaluacion, motores), jsonLegible));
        return motores.All(m => m.FallosTecnicos == 0) ? 0 : 1;
    }
    catch (Exception error)
    {
        // Los mensajes de ArgumentException son los del validador del corpus: no citan imágenes ni credenciales.
        Console.Error.WriteLine($"La comparación no se completó ({(error is ArgumentException ? error.Message : error.GetType().Name)}). No se declara un resultado.");
        return 1;
    }
}
if (modo == "--bridge")
{
    var repositorioBridge = new RepositorioInteraccionesOracle(fabrica);
    var servicioBridge = new ServicioInteracciones(usuarios, repositorioBridge, contenedores, materiales, TimeProvider.System);
    return await AplicacionBridge.EjecutarAsync(modo, configuracion, repositorioBridge, servicioBridge, TimeProvider.System);
}
ModoVision modoVision = ConfiguracionVision.LeerModo(configuracion, Console.Error.WriteLine);
if (modoVision == ModoVision.Comparacion && !evaluar)
    Console.Error.WriteLine("Vision:Modo=Comparacion solo aplica a --comparar-vision; el bot atiende con el motor local.");
SeleccionVisionLocal? motorLocal = modoVision == ModoVision.Groq ? null : SeleccionVisionLocal.Crear(configuracion, Console.Error.WriteLine);
ConfiguracionGroq? ajustesVisionGroq = modoVision is ModoVision.Groq or ModoVision.Automatico ? ConfiguracionVision.LeerGroq(configuracion, Console.Error.WriteLine) : null;
if (modoVision is ModoVision.Groq or ModoVision.Automatico && ajustesVisionGroq is null)
    Console.Error.WriteLine("Falta la clave de Groq: el servicio externo queda desactivado y la selección manual sigue disponible.");
ClasificadorGroq? motorGroq = ajustesVisionGroq is null ? null : new ClasificadorGroq(ajustesVisionGroq, new UsoGroq(), Console.Error.WriteLine);
using SelectorVision? proveedorVision = motorLocal is null && motorGroq is null ? null
    : new SelectorVision(modoVision, motorLocal, motorGroq,
        (motorLocal?.TiempoMaximo ?? TimeSpan.Zero) + (motorGroq?.TiempoMaximo ?? TimeSpan.Zero) + TimeSpan.FromSeconds(5), Console.Error.WriteLine);
if (motorLocal is not null && !evaluar) _ = motorLocal.PrecalentarAsync();
ServicioClasificacionImagen? vision = proveedorVision is null ? null :
    new ServicioClasificacionImagen(proveedorVision, materiales, new ServicioCatalogoAmbiental(materiales, contenedores), proveedorVision.TiempoMaximo);
if (evaluar)
{
    if (motorLocal is null)
    {
        Console.Error.WriteLine("La evaluación requiere el reconocimiento local verificado y un manifiesto de imágenes autorizadas.");
        return 1;
    }
    try
    {
        var informe = await EvaluacionVisionArchivos.EjecutarAsync(args[1], maximoEvaluacion, motorLocal,
            await materiales.ListarAsync(true), contenedores, TimeProvider.System, tiempoMaximo: motorLocal.TiempoMaximo);
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(informe, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        return informe.FallosProveedor == 0 ? 0 : 1;
    }
    catch (Exception)
    {
        Console.Error.WriteLine("La evaluación no se completó. Revisa manifiesto, hashes, rutas, licencia y acceso al proveedor; no se declara un resultado completo.");
        return 1;
    }
}
Uri? paginaRegistro = null;
string? urlRegistro = configuracion["Telegram:PaginaRegistro"];
if (!Compat.IsNullOrWhiteSpace(urlRegistro) && (!Uri.TryCreate(urlRegistro, UriKind.Absolute, out paginaRegistro)
    || paginaRegistro.Scheme != Uri.UriSchemeHttps || paginaRegistro.UserInfo.Length != 0
    || paginaRegistro.Query.Length != 0 || paginaRegistro.Fragment.Length != 0))
{
    Console.Error.WriteLine("Telegram:PaginaRegistro debe ser HTTPS sin credenciales, consulta ni fragmento.");
    return 1;
}
ClienteTelegramHttp? clienteFotos = null;
var repositorioInteracciones = new RepositorioInteraccionesOracle(fabrica);
var interacciones = new ServicioInteracciones(usuarios, repositorioInteracciones, contenedores, materiales, TimeProvider.System);
CanecaSimulada? canecaSimulada = null;
ServicioDespachoOrdenes? despacho = null;
string? idSimulado = configuracion["Dispositivo:IdContenedorSimulado"];
if (!pollingSinDispositivo && idSimulado is not null)
{
    if (modo != "--simulado" || !int.TryParse(idSimulado, out int idContenedorSimulado) || idContenedorSimulado <= 0)
    {
        Console.Error.WriteLine("Dispositivo:IdContenedorSimulado requiere --simulado y un identificador positivo.");
        return 1;
    }

    string directorioSimulador = configuracion["Dispositivo:DirectorioSimulador"]
        ?? Path.Combine(AppContext.BaseDirectory, "estado-simulador");
    canecaSimulada = new CanecaSimulada(idContenedorSimulado, interacciones.RegistrarEventoAsync, TimeProvider.System,
        new RegistroEjecucionArchivo(directorioSimulador));
    despacho = new ServicioDespachoOrdenes(repositorioInteracciones, interacciones, canecaSimulada, TimeProvider.System);
}
var vinculacion = new ServicioVinculacionTelegram(
    usuarios, new RepositorioVinculosTelegramOracle(fabrica), new GeneradorTokensAleatorio(), TimeProvider.System);
AsistenteEco? eco = null;
string? qrPiloto = configuracion["Telegram:QrDemo"];
// El binding local y la base elegida deben identificar el mismo piloto; no se copia un QR de otra base.
if (!pollingSinDispositivo && int.TryParse(configuracion["Bridge:IdContenedor"], out int idPilotoLocal)
    && configuracion["Bridge:EscenarioSimulado"] is not null
    && await contenedores.ObtenerPorIdAsync(idPilotoLocal) is { } pilotoLocal
    && ServicioCatalogoAmbiental.EsEstacionOficial(pilotoLocal))
    qrPiloto = pilotoLocal.IdentificadorQr;
var conversacion = new ServicioConversacionTelegram(
    new RepositorioConversacionesTelegramOracle(fabrica),
    contenedores,
    materiales,
    vinculacion,
    interacciones,
    new LimitadorUsoEnMemoria(20, TimeSpan.FromMinutes(1)),
    paginaRegistro,
    TimeProvider.System,
    despacho,
    new ServicioEcopuntos(usuarios, repositorioInteracciones, materiales, new RepositorioEcopuntosOracle(fabrica), TimeProvider.System),
    vision,
    async (archivo, ct) =>
    {
        if (clienteFotos is null) throw new InvalidOperationException("No hay cliente de fotografías en este modo.");
        if (eco is not null) await eco.NotificarAnalisisFotoAsync(ct);
        return await clienteFotos.DescargarFotoAsync(archivo, ct);
    },
    (proveedorVision?.Politica ?? EcoAccess.P3.Logica.Contratos.PoliticaVisionExterna.Ninguna) switch
    {
        EcoAccess.P3.Logica.Contratos.PoliticaVisionExterna.Principal => "🌐 Analizo tu foto con un servicio externo (Groq), solo si antes lo autorizas. No incluyas personas ni datos personales; la sugerencia no abre la caneca por sí sola.",
        EcoAccess.P3.Logica.Contratos.PoliticaVisionExterna.Respaldo => "🔒 Analizo tu foto en este computador. Solo saldría a un servicio externo (Groq) con tu autorización. No incluyas personas ni datos personales; la sugerencia no abre la caneca por sí sola.",
        _ => "🔒 Analizo tu foto aquí mismo, sin enviarla a otros servicios. No incluyas personas ni datos personales; la sugerencia no abre la caneca por sí sola.",
    },
    qrDemo: qrPiloto,
    // La foto puede abrir sin confirmación: queda traza salvo que se desactive explícitamente.
    analisis: configuracion["Vision:RegistrarTraza"] != "false" ? new RepositorioAnalisisImagenOracle(fabrica) : null,
    estadisticas: new ServicioEstadisticas(usuarios, new RepositorioEstadisticasOracle(fabrica)),
    opiniones: new ServicioOpiniones(usuarios, new RepositorioOpinionesOracle(fabrica), TimeProvider.System),
    flujoRapido: proveedorVision is not null,
    consentimientosVision: new RepositorioConsentimientoVisionOracle(fabrica),
    politicaExterna: proveedorVision?.Politica ?? EcoAccess.P3.Logica.Contratos.PoliticaVisionExterna.Ninguna,
    estadoCaneca: despacho is null && !pollingSinDispositivo ? CrearEstadoCaneca() : null,
    comentarios: new ServicioComentarios(new RepositorioComentariosOracle(fabrica), TimeProvider.System));
conversacion.RegistrarReconocimiento = Console.Error.WriteLine;
// Dónde entregar lo que no va en las canecas (V026). Si la tabla aún no existe en este esquema, se usa la semilla verificada.
conversacion.Orientacion = new ServicioOrientacionEspecial(new EcoAccess.P3.Datos.Repositorios.RepositorioPuntosDisposicionOracle(fabrica));
// Fase de software: sin simulador ni enlaces del bridge no hay por dónde abrir una caneca; se informa solo el destino.
conversacion.SinAperturaFisica = despacho is null && (pollingSinDispositivo
    || (string.IsNullOrWhiteSpace(configuracion["Bridge:Eventos"]) && string.IsNullOrWhiteSpace(configuracion["Bridge:Comandos"])));
if (modo == "--polling" && conversacion.SinAperturaFisica) Console.Error.WriteLine("Modo software: sin dispositivo configurado; Eco orienta el destino y no emite órdenes de apertura.");
// Cada foto atendida se anota en CONSULTA_VISION (V023) para los tableros y «Mi actividad»; sin la migración, Echo sigue igual.
var consultasVision = new RepositorioConsultasVisionOracle(fabrica);
try
{
    if (await consultasVision.TablaDisponibleAsync()) conversacion.ConsultasVision = consultasVision;
    Console.Error.WriteLine(conversacion.ConsultasVision is null
        ? "Consultas de fotos: la base no tiene CONSULTA_VISION (migración V023 pendiente); no se anotan."
        : "Consultas de fotos: se anotan en Oracle (CONSULTA_VISION), sin guardar la foto.");
}
catch (Exception error) { Console.Error.WriteLine("Consultas de fotos: no se pudo comprobar la tabla (" + error.GetType().Name + "); no se anotan."); }
// Estación de tres canecas (V021/V024): la respuesta indica el color y la ubicación; sin configuración, caneca única.
conversacion.Estaciones = new RepositorioEstacionesOracle(fabrica);
// Coordinador unificado PC + Telegram (Fases 4 y 5): serializa aperturas, bloquea chats no autorizados y mantiene modo físico desactivado salvo sesión supervisada.
var simuladorUnificado = new SimuladorEstacionTresCanecas(
    new Dictionary<int, DestinoResiduo>
    {
        [1093] = DestinoResiduo.Blanca,
        [1400] = DestinoResiduo.Verde,
        [1401] = DestinoResiduo.Negra,
    }, TimeProvider.System, TimeSpan.Zero);
// Modo ExpoSoftware (predeterminado; ECOACCESS_MODO_EXPO=0 vuelve al recorrido anterior): el panel local de canecas es el
// único dueño del ESP32, así que este proceso NO abre el puerto serie y pide las aperturas al panel por HTTP local.
bool modoExpo = Environment.GetEnvironmentVariable("ECOACCESS_MODO_EXPO") != "0" && modo is "--polling" or "--webhook" or "--simulado";
using EjecutorEstacionSerialHmac? ejecutorSerialUnificado = (!modoExpo && !pollingSinDispositivo && !conversacion.SinAperturaFisica && !GuardiaHardware.Bloqueado)
    ? EjecutorEstacionSerialHmac.IntentarCargarDesdeProgramData(TimeProvider.System)
    : null;
var coordinadorUnificado = new EcoAccess.P3.Logica.Vision35.CoordinadorEstacionUnificado(
    simuladorUnificado,
    ComandoEstacionFisica.EstacionEstandar,
    ejecutorSerialUnificado,
    reloj: TimeProvider.System);
if (ejecutorSerialUnificado is not null && Environment.GetEnvironmentVariable("ECOACCESS_MODO_FISICO_SUPERVISADO") == "1")
{
    coordinadorUnificado.HabilitarSesionFisicaSupervisada(TimeSpan.FromHours(2), "Supervisor ExpoSoftware", conSensoresFisicos: false);
    Console.Error.WriteLine("Coordinador unificado: MODO FÍSICO SUPERVISADO habilitado (solo chats autorizados, acceso serializado a COM3).");
}
conversacion.CoordinadorEstacion = coordinadorUnificado;
EcoAccess.P3.Logica.Vision35.Expo.RegistroOperacionesConCola? registroExpo = null;
if (modoExpo)
{
    var repoOperaciones = new RepositorioOperacionesEstacionOracle(fabrica);
    // En el anfitrión este proceso también sube a Oracle las aperturas hechas desde la web de control (canal WEB).
    registroExpo = new EcoAccess.P3.Logica.Vision35.Expo.RegistroOperacionesConCola(repoOperaciones, subirOperacionesWeb: true);
    EcoAccess.P3.Logica.Vision35.Expo.IEstacionCanecasExpo estacionExpo = Environment.GetEnvironmentVariable("ECOACCESS_ESTACION") == "simulada"
        ? new EcoAccess.P3.Logica.Vision35.Expo.EstacionExpoSimulada()
        : new EcoAccess.P3.Logica.Vision35.Expo.ClienteEstacionPanel();
    conversacion.EstacionExpo = new EcoAccess.P3.Logica.Vision35.Expo.ServicioEstacionExpo(estacionExpo, registroExpo);
    conversacion.ReconocedorExpo = new EcoAccess.P3.Logica.Vision35.Expo.ReconocedorExpo9();
    // Vinculación de la web ciudadana con la cuenta de Telegram e historial oficial para la página (solo 127.0.0.1;
    // la pasarela ciudadana lo expone con CORS y límites). Requiere la clave de la estación del panel.
    if (modo == "--polling" || modo == "--webhook")
    {
        string archivoClaveEstacion = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Panel", "clave.txt");
        string claveEstacion = File.Exists(archivoClaveEstacion) ? File.ReadAllText(archivoClaveEstacion).Trim() : "";
        if (claveEstacion.Length >= 16)
        {
            var cuentasWeb = new EcoAccess.P3.Bot.Web.ServicioCuentasWeb((id, maximo, ct) => repoOperaciones.RecientesAsync(id, maximo, ct));
            conversacion.ConfirmarVinculoWeb = cuentasWeb.Confirmar;
            cuentasWeb.Iniciar(claveEstacion, texto => Console.Error.WriteLine(texto));
        }
        else Console.Error.WriteLine("Cuentas web: falta la clave de la estación (Panel\\clave.txt); la vinculación web queda desactivada.");
    }
    if (clienteFotos is not null) conversacion.EnviarDiferido = (respuesta, ct) => clienteFotos.EnviarAsync(respuesta, ct);
    try
    {
        bool tabla = await repoOperaciones.TablaDisponibleAsync();
        Console.Error.WriteLine(tabla ? "Modo ExpoSoftware: operaciones registradas en Oracle (OPERACION_ESTACION)."
            : "Modo ExpoSoftware: la base no tiene OPERACION_ESTACION (migración V030 pendiente); las operaciones quedan en la cola local y se sincronizan al aplicarla.");
    }
    catch (Exception error) { Console.Error.WriteLine("Modo ExpoSoftware: no se pudo comprobar OPERACION_ESTACION (" + error.GetType().Name + "); se usa la cola local."); }
    // Precalentamiento: la primera foto no paga la carga en frío del modelo (medido: ~14 s en frío, ~1,6 s cargado).
    var reconocedorExpo = conversacion.ReconocedorExpo;
    _ = Task.Run(async () =>
    {
        long ms = await reconocedorExpo.PrecalentarAsync();
        Console.Error.WriteLine(ms >= 0 ? $"Modelo visual {reconocedorExpo.Modelo} cargado y en memoria ({ms} ms)." : "Ollama no respondió al precalentar; la primera foto tardará más.");
    });
    Console.Error.WriteLine($"Modo ExpoSoftware: 11 residuos, {reconocedorExpo.Modelo} a {reconocedorExpo.LadoPx}px, estación {(estacionExpo.EsSimulada ? "simulada" : estacionExpo is EcoAccess.P3.Logica.Vision35.Expo.ClienteEstacionPanel cp ? (EcoAccess.P3.Logica.Vision35.Expo.AnfitrionEstacion.EsRemoto(cp.Url) ? "anfitrión de red " : "panel local ") + cp.Url : "panel")}, apertura {conversacion.EstacionExpo.Modalidad} 7/14/21 s.");
}
// Aviso inmediato «escribiendo…». En Modo Expo no se envía un mensaje intermedio («Procesando…»): la respuesta definitiva
// llega en ~2 s, y esperar a Telegram para un aviso retrasaba la clasificación ~0,3 s. La indicación no se espera.
eco = new AsistenteEco(conversacion.ProcesarAsync, async (respuesta, ct) =>
{
    if (clienteFotos is null) return;
    if (modoExpo) { _ = clienteFotos.IndicarEscribiendoAsync(respuesta.ChatId, CancellationToken.None).ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted); return; }
    await clienteFotos.IndicarEscribiendoAsync(respuesta.ChatId, ct);
});
if (registroExpo is not null)
{
    var registroSincronizar = registroExpo;
    var oracleExpo = registroExpo.Oracle as RepositorioOperacionesEstacionOracle;
    string pulso = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Estacion", "bot-estado.json");
    _ = Task.Run(async () =>
    {
        while (true)
        {
            bool? oracleOk = null;
            try
            {
                int n = await registroSincronizar.SincronizarAsync();
                if (n > 0) Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}Z] Expo: {n} operación(es) sincronizadas con Oracle.");
                if (oracleExpo is not null) oracleOk = await oracleExpo.TablaDisponibleAsync();
            }
            catch (Exception) { oracleOk = false; }
            // Pulso para el panel local (indicadores «Bot» y «Oracle»): sin datos personales ni secretos.
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(pulso)!);
                File.WriteAllText(pulso, System.Text.Json.JsonSerializer.Serialize(new
                {
                    pid = System.Diagnostics.Process.GetCurrentProcess().Id,
                    utc = DateTime.UtcNow.ToString("o"),
                    oracle = oracleOk,
                    pendientes = Math.Max(0, registroSincronizar.Pendientes) + Math.Max(0, registroSincronizar.PendientesWeb),
                    modelo = conversacion.ReconocedorExpo?.Modelo,
                }));
            }
            catch (Exception) { }
            await Task.Delay(TimeSpan.FromSeconds(30));
        }
    });
}

// Sin enlaces locales configurados no hay por dónde entregar una orden.
EcoAccess.P3.Logica.Contratos.IEstadoCaneca CrearEstadoCaneca()
{
    Uri Enlace(string? valor) => Uri.TryCreate(valor, UriKind.Absolute, out Uri? enlace) ? enlace : new Uri("http://sin-enlace.invalid/");
    var enlaces = new[] { "Bridge:Eventos", "Bridge:Comandos" }.Select(clave => Enlace(configuracion[clave])).ToArray();
    // Estación de tres canecas: Bridge:Canecas:<IdContenedor>:Eventos/Comandos indica el bridge de cada una.
    var porCaneca = new Dictionary<int, Uri[]>();
    foreach (var seccion in configuracion.GetSection("Bridge:Canecas").GetChildren())
        if (int.TryParse(seccion.Key, out int idCaneca) && idCaneca > 0)
            porCaneca[idCaneca] = new[] { Enlace(seccion["Eventos"]), Enlace(seccion["Comandos"]) };
    return new EcoAccess.P3.Bot.Dispositivos.EstadoCanecaLocal(new RepositorioFallosCanecaOracle(fabrica).TieneFalloRecienteAsync, porCaneca, enlaces);
}

using var cancelacion = new CancellationTokenSource();
ControlParadaLocal control;
try { control = new ControlParadaLocal(cancelacion); }
catch (ArgumentException) { Console.Error.WriteLine("Archivo de parada local inválido; revisar supervisor y carpeta privada."); return 1; }
using var controlLocal = control;
Console.CancelKeyPress += (_, evento) =>
{
    evento.Cancel = true;
    cancelacion.Cancel();
};
void Registrar(string texto) => Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}Z] {texto}");

Task paginaEnMarcha = Task.CompletedTask;
SesionConsumidorTelegram? sesionTelegram = null;
bool IniciarPagina()
{
    if (paginaRegistro is null)
    { Registrar("Sin Telegram:PaginaRegistro: /vincular informa que el registro no está disponible. Ver docs/telegram/VINCULACION_HTTPS.md."); return true; }
    string prefijoRegistro = configuracion["Telegram:PrefijoRegistro"] ?? "http://127.0.0.1:8081/registro/";
    var servidorRegistro = new ServidorRegistro(
    new PaginaVinculacion(
        vinculacion,
        new ServicioAutenticacion(new RepositorioCredencialesOracle(fabrica), usuarios, new HasherPbkdf2(), TimeProvider.System),
        new ServicioRegistroCiudadanos(new RepositorioRegistroCiudadanosOracle(fabrica), new HasherPbkdf2(), TimeProvider.System),
        new LimitadorUsoEnMemoria(120, TimeSpan.FromMinutes(1)),
        new LimitadorUsoEnMemoria(10, TimeSpan.FromMinutes(1)),
        TimeProvider.System),
    prefijoRegistro,
    Registrar);
    // HttpListener.Start corre antes del primer await: si el prefijo no se puede abrir, la tarea ya está fallida.
    paginaEnMarcha = servidorRegistro.EjecutarAsync(cancelacion.Token);
    if (paginaEnMarcha.IsFaulted)
    {
        // En Windows, un prefijo con comodín necesita «netsh http add urlacl» (ver docs/telegram/VINCULACION_HTTPS.md).
        Console.Error.WriteLine("No se pudo abrir Telegram:PrefijoRegistro (puerto ocupado, prefijo mal escrito o sin permiso).");
        _ = paginaEnMarcha.Exception;
        return false;
    }

    // Si la página cae más tarde, el bot sigue atendiendo Telegram, pero queda constancia en el registro.
    _ = paginaEnMarcha.ContinueWith(
        t => Registrar($"La página de registro se detuvo ({t.Exception?.InnerException?.GetType().Name})."),
        CancellationToken.None,
        TaskContinuationOptions.OnlyOnFaulted,
        TaskScheduler.Default);
    return true;
}

try { return await EjecutarModoAsync(); }
catch (OperationCanceledException) when (cancelacion.IsCancellationRequested) { return 0; }
finally
{
    cancelacion.Cancel();
    try { await paginaEnMarcha.ConfigureAwait(false); }
    catch (OperationCanceledException) when (cancelacion.IsCancellationRequested) { }
    catch (Exception) { Registrar("La página de vinculación finalizó con un error; revisar su disponibilidad."); }
    finally { sesionTelegram?.Dispose(); }
}

async Task<int> EjecutarModoAsync()
{
    if (modo == "--simulado")
    {
        if (!IniciarPagina()) return 1;
        var chat = new ChatConsola(Console.In, Console.Out, cancelacion.Cancel);
        Console.WriteLine($"Chat simulado con la cuenta de Telegram ficticia {ChatConsola.UsuarioSimulado}. «#n» pulsa un botón; Ctrl+D termina.");
        await new BuclePolling(chat, new ProcesadorUpdates(ProcesarSimuladoAsync, chat, Registrar), Registrar)
            .EjecutarAsync(cancelacion.Token);
        return 0;
    }

    async Task<IReadOnlyList<RespuestaBot>> ProcesarSimuladoAsync(MensajeEntrante mensaje, CancellationToken ct)
    {
        var respuestas = (await eco.ProcesarAsync(mensaje, ct)).ToList();
        if (canecaSimulada is not null)
        {
            // Paso explícito de la demo: entregar el comando nunca genera eventos por sí solo.
            foreach (var comando in canecaSimulada.ComandosRecibidos)
            {
                var eventos = await canecaSimulada.EjecutarAsync(comando.IdOrden, ct);
                if (eventos.Count > 0)
                {
                    respuestas.Add(new RespuestaBot(mensaje.ChatId,
                        "Se ejecutó la caneca simulada, sin hardware real. Consulta /estado para ver el resultado persistido."));
                }
            }
        }

        return respuestas;
    }

    string? token = configuracion["Telegram:TokenBot"];
    if (Compat.IsNullOrWhiteSpace(token))
    {
        Console.Error.WriteLine("Falta Telegram:TokenBot en los User Secrets «ecoaccess-p3-framework». Usa --simulado para probar sin Telegram.");
        return 1;
    }

    ClienteTelegramHttp cliente;
    try
    {
        cliente = new ClienteTelegramHttp(token);
        clienteFotos = cliente;
    }
    catch (ArgumentException)
    {
        Console.Error.WriteLine("Telegram:TokenBot no tiene el formato de un token de bot.");
        return 1;
    }

    var idBot = default(long);
    using (cliente)
    {
        try
        {
            var estado = await cliente.ComprobarConfiguracionAsync(configuracion["Telegram:NombreBot"] ?? string.Empty, cancelacion.Token);
            if (modo == "--polling" && estado.WebhookActivo)
            { Registrar("Webhook activo incompatible con polling. No se modificó ni se consumieron updates."); return 3; }
            sesionTelegram = new SesionConsumidorTelegram(estado.IdBot); idBot = estado.IdBot;
            Registrar($"Identidad comprobada: @{estado.NombreBot}. Consumidor local exclusivo.");
        }
        catch (ErrorTelegramException e) { Registrar(e.Message); return e.EsTokenRechazado ? 4 : 1; }
        catch (ArgumentException) { Registrar("Configura Telegram:NombreBot válido, sin @."); return 1; }
        catch (IOException) { Registrar("No se pudo obtener el bloqueo local del bot: otra instancia puede estar activa. No se consumieron updates."); return 3; }
        catch (UnauthorizedAccessException) { Registrar("Sin acceso al directorio del bloqueo local del bot. No se consumieron updates."); return 1; }
        if (!IniciarPagina()) return 1;
        var procesador = new ProcesadorUpdates(eco.ProcesarAsync, cliente, Registrar);
        if (modo == "--webhook")
        {
            string? secreto = configuracion["Telegram:SecretoWebhook"];
            string prefijo = configuracion["Telegram:PrefijoWebhook"] ?? "http://127.0.0.1:8080/telegram/";
            if (!ReceptorWebhook.SecretoValido(secreto))
            {
                Console.Error.WriteLine("Falta Telegram:SecretoWebhook o no cumple el formato (16-256 caracteres A-Z, a-z, 0-9, _ o -).");
                return 1;
            }

            await new ServidorWebhook(new ReceptorWebhook(procesador, secreto!), prefijo, Registrar).EjecutarAsync(cancelacion.Token);
            return 0;
        }

        // Avisos de cambios de estado: leen hechos ya registrados en Oracle; una falla aquí no detiene el polling.
        var notificaciones = new ServicioNotificacionesTelegram(new RepositorioNotificacionesTelegramOracle(fabrica),
            new RegistroNotificacionesArchivo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EcoAccessP3Framework", "Telegram", "notificaciones-" + idBot.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".json")),
            TimeProvider.System, conversacion.FraseDeteccion);
        using var finAvisos = CancellationTokenSource.CreateLinkedTokenSource(cancelacion.Token);
        Task avisos = AvisarAsync(notificaciones, cliente, finAvisos.Token);
        Registrar("Bot en polling con avisos automáticos. Ctrl+C para detener.");
        FinPolling fin = await new BuclePolling(cliente, procesador, Registrar, alLatir: estado => estado.Guardar()).EjecutarAsync(cancelacion.Token);
        finAvisos.Cancel();
        await avisos;
        return fin switch { FinPolling.Conflicto => 3, FinPolling.TokenRechazado => 4, _ => 0 };
    }
    async Task AvisarAsync(ServicioNotificacionesTelegram servicio, ClienteTelegramHttp telegram, CancellationToken ct)
    {
        async Task EnviarAvisoAsync(RespuestaBot respuesta, CancellationToken c)
        {
            try { await telegram.EnviarAsync(respuesta, c); }
            catch (ErrorTelegramException error) when (error.EsDestinoNoDisponible) { throw new DestinoNotificacionNoDisponibleException(error); }
        }

        int fallosSeguidos = 0;
        while (!ct.IsCancellationRequested)
        {
            try { await servicio.RevisarAsync(EnviarAvisoAsync, Registrar, ct); fallosSeguidos = 0; }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception error)
            {
                // Oracle caído o red intermitente: se registra y se reintenta con espera creciente, sin perder avisos.
                fallosSeguidos++;
                if (fallosSeguidos == 1 || fallosSeguidos % 20 == 0)
                    Registrar("Avisos automáticos en pausa (" + error.GetType().Name + "); se reintenta sin perder los pendientes.");
            }
            // Cada segundo: el aviso de apertura llega dentro de los 7 segundos de depósito.
            try { await Task.Delay(TimeSpan.FromSeconds(fallosSeguidos == 0 ? 1 : Math.Min(60, 2 * (fallosSeguidos + 1))), ct); }
            catch (OperationCanceledException) { break; }
        }
    }
}
