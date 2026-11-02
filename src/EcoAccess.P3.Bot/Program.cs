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
// La página de registro solo inicia si existe Telegram:PaginaRegistro HTTPS; la consulta pública no la requiere.
// El supervisor lee tuberías UTF-8 también cuando Windows ejecuta el proceso sin consola.
Console.OutputEncoding = new System.Text.UTF8Encoding(false);
string modo = args.Length == 0 ? "--polling" : args[0];
if (modo == "--simulado") Console.InputEncoding = new System.Text.UTF8Encoding(false);
if (modo == "--estado-vision-local")
{
    if (args.Length != 1) return 2;
    using var diagnosticoLocal = new DiagnosticoOllamaLocal();
    var salud = await diagnosticoLocal.ComprobarAsync();
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(salud));
    return salud.ListoParaInferencia ? 0 : 1;
}
bool evaluar = modo == "--evaluar-vision";
bool comparar = modo == "--comparar-vision";
bool comprobarVision = modo == "--comprobar-vision-local";
bool comprobarVisionGroq = modo == "--comprobar-vision-groq";
int maximoEvaluacion = 0;
if (evaluar || comparar ? args.Length != 3 || !int.TryParse(args[2], out maximoEvaluacion) || maximoEvaluacion is < 1 or > EvaluacionVisionArchivos.MaximoSolicitudes
    : comprobarVision || comprobarVisionGroq ? args.Length != 2
    : modo is not ("--polling" or "--simulado" or "--webhook" or "--bridge" or "--dispositivo-simulado" or "--dispositivo-serial" or "--comprobar-telegram" or "--comprobar-oracle" or "--comprobar-groq" or "--estado-vision") || args.Length > 1)
{
    Console.Error.WriteLine("Uso: EcoAccess.P3.Bot [--polling | --simulado | --webhook | --bridge | --dispositivo-simulado | --dispositivo-serial | --comprobar-telegram | --comprobar-oracle | --comprobar-groq | --estado-vision] o --evaluar-vision <manifiesto.json> <máximo-1-a-200> o --comparar-vision <manifiesto.json> <máximo-1-a-200> o --comprobar-vision-local <imagen> o --comprobar-vision-groq <imagen>");
    return 2;
}

IConfiguration configuracion;
try
{
    var constructorConfiguracion = new ConfigurationBuilder();
    string? elegido = Environment.GetEnvironmentVariable("ECOACCESS_ARCHIVO_BRIDGE");
    string archivoBridge = elegido ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Bridge", "bridge.local.json");
    if (elegido is not null && (!Compat.IsPathFullyQualified(archivoBridge) || !File.Exists(archivoBridge)))
        throw new ArgumentException("Archivo local de configuración no disponible.");
    if (File.Exists(archivoBridge))
    {
        ConfiguracionBridge.ValidarRutaPrivada(Path.GetDirectoryName(archivoBridge)!);
        constructorConfiguracion.AddJsonFile(archivoBridge, optional: false, reloadOnChange: false);
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
var usuarios = new RepositorioUsuariosOracle(fabrica);
var contenedores = new RepositorioContenedoresOracle(fabrica);
var materiales = new RepositorioMaterialesOracle(fabrica);
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
            // 30 solicitudes por minuto es el límite publicado: se deja margen entre fotos.
            var informeGroq = await EvaluacionVisionArchivos.EjecutarAsync(args[1], maximoEvaluacion, remoto, catalogoEvaluacion, contenedores, TimeProvider.System,
                tiempoMaximo: remoto.TiempoMaximo + TimeSpan.FromSeconds(5), envio: EcoAccess.P3.Logica.Contratos.EnvioExterno.Autorizado, pausaEntreMuestras: TimeSpan.FromSeconds(2.2));
            var despues = uso.Actual;
            motores.Add(MetricasVision.Calcular(informeGroq, catalogoEvaluacion, despues.TokensEntrada - antes.TokensEntrada, despues.TokensSalida - antes.TokensSalida, despues.CostoUsd - antes.CostoUsd));
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
if (idSimulado is not null)
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
if (int.TryParse(configuracion["Bridge:IdContenedor"], out int idPilotoLocal)
    && configuracion["Bridge:EscenarioSimulado"] is not null
    && await contenedores.ObtenerPorIdAsync(idPilotoLocal) is { } pilotoLocal
    && pilotoLocal.Nombre == ServicioCatalogoAmbiental.NombreCanecaPiloto)
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
    analisis: configuracion["Vision:RegistrarTraza"] == "true" ? new RepositorioAnalisisImagenOracle(fabrica) : null,
    estadisticas: new ServicioEstadisticas(usuarios, new RepositorioEstadisticasOracle(fabrica)),
    opiniones: new ServicioOpiniones(usuarios, new RepositorioOpinionesOracle(fabrica), TimeProvider.System),
    flujoRapido: proveedorVision is not null,
    consentimientosVision: new RepositorioConsentimientoVisionOracle(fabrica),
    politicaExterna: proveedorVision?.Politica ?? EcoAccess.P3.Logica.Contratos.PoliticaVisionExterna.Ninguna);
eco = new AsistenteEco(conversacion.ProcesarAsync, (respuesta, ct) =>
    clienteFotos is null ? Task.CompletedTask : clienteFotos.EnviarAsync(respuesta, ct));

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
    { Registrar("Consulta pública disponible; vinculación sin página HTTPS configurada, no se emitirán enlaces nuevos."); return true; }
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
        // En Windows, un prefijo que no sea localhost necesita «netsh http add urlacl» (ver 11_TELEGRAM.md §5).
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
            TimeProvider.System);
        using var finAvisos = CancellationTokenSource.CreateLinkedTokenSource(cancelacion.Token);
        Task avisos = AvisarAsync(notificaciones, cliente, finAvisos.Token);
        Registrar("Bot en polling con avisos automáticos. Ctrl+C para detener.");
        FinPolling fin = await new BuclePolling(cliente, procesador, Registrar).EjecutarAsync(cancelacion.Token);
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
            try { await Task.Delay(TimeSpan.FromSeconds(Math.Min(60, 2 * (fallosSeguidos + 1))), ct); }
            catch (OperationCanceledException) { break; }
        }
    }
}
