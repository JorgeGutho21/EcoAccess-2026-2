<#
.SYNOPSIS
    Compara LFM3-VL local y Qwen 3.8 en Groq sobre las mismas fotos y solo da por buena una corrida completa y real.
.DESCRIPTION
    Antes de enviar nada comprueba rama, ejecutable, hashes, licencia de cada foto, servicios y presupuesto. Pide
    confirmación, envía UNA foto para leer los límites reales de la cuenta y el consumo por foto, y solo entonces lanza
    la comparación. Solo lee el catálogo de Oracle: no crea solicitudes, órdenes, depósitos ni ecopuntos.
    No imprime claves. Si algo falla, explica la causa, conserva el registro y no deja un comparacion_groq.json.
.PARAMETER Confirmar
    Omite la pregunta de confirmación. Quien lo usa acepta enviar las fotos del manifiesto a Groq.
.PARAMETER SoloValidar
    No ejecuta nada: valida un archivo ya generado e imprime su tabla.
#>
[CmdletBinding()]
param([switch] $Confirmar, [string] $SoloValidar, [string] $Corpus = 'C:\ProgramData\EcoAccessP3Framework\Corpus', [int] $Muestras = 66)
$raiz = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$bot = Join-Path $raiz 'src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe'
$manifiesto = Join-Path $Corpus 'manifiesto_groq.json'
$destino = Join-Path $raiz 'comparacion_groq.json'
$registro = Join-Path $raiz 'comparacion_groq.log'
$archivoUso = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework\Vision\uso-groq.json'
# Publicados por Groq para este modelo el 2 de noviembre de 2026. No se usan para detener: cada cuenta tiene los suyos.
$solicitudesDiaPublicadas = 1000; $tokensMinutoPublicados = 8000

function Paso([string] $t) { Write-Host "`n== $t" -ForegroundColor Cyan }
function Bien([string] $t) { Write-Host "  OK  $t" -ForegroundColor Green }
function Fallar([string] $t) { Write-Host "  DETENIDO: $t" -ForegroundColor Red; Write-Host '  No se generó ningún resultado de comparación.' -ForegroundColor Red; exit 1 }

function Lanzar([string[]] $Argumentos, [string] $Salida, [string] $Errores) {
    return Start-Process -FilePath $bot -ArgumentList $Argumentos -WorkingDirectory $raiz -NoNewWindow -PassThru -RedirectStandardOutput $Salida -RedirectStandardError $Errores
}
function Ejecutar([string[]] $Argumentos) {
    $s = [IO.Path]::GetTempFileName(); $e = [IO.Path]::GetTempFileName()
    try {
        $p = Lanzar $Argumentos $s $e
        $null = $p.Handle; $p.WaitForExit()
        return [pscustomobject]@{ Codigo = $p.ExitCode; Texto = (Get-Content $s -Raw -Encoding UTF8); Avisos = (Get-Content $e -Raw -Encoding UTF8) }
    }
    finally { Remove-Item $s, $e -ErrorAction SilentlyContinue }
}
function LeerUso {
    if (-not (Test-Path $archivoUso)) { return $null }
    $u = Get-Content $archivoUso -Raw | ConvertFrom-Json
    if (([datetime]$u.DiaUtc).ToUniversalTime().Date -ne [datetime]::UtcNow.Date) { return $null }
    return $u
}

function Validar([string] $Archivo) {
    if (-not (Test-Path $Archivo)) { return 'el archivo no existe' }
    try { $d = Get-Content $Archivo -Raw -Encoding UTF8 | ConvertFrom-Json } catch { return 'no es JSON válido: está incompleto o dañado' }
    $m = @($d.Motores)
    if ($m.Count -ne 2) { return "tiene $($m.Count) motor(es) en lugar de 2" }
    if ($m[0].Proveedor -like 'groq:*' -or $m[1].Proveedor -notlike 'groq:*') { return 'los motores no son el local y Groq' }
    foreach ($x in $m) {
        if ($x.Evaluadas -ne $Muestras -or @($x.Muestras).Count -ne $Muestras) { return "$($x.Proveedor) evaluó $($x.Evaluadas) de $Muestras fotos" }
        if ($x.FallosTecnicos -ne 0) { return "$($x.Proveedor) tuvo $($x.FallosTecnicos) fallos técnicos" }
    }
    if ($m[1].TokensEntrada -le 0) { return 'Groq no registró tokens: no hay evidencia de inferencia real' }
    return $null
}

function Mostrar([string] $Archivo) {
    $d = Get-Content $Archivo -Raw -Encoding UTF8 | ConvertFrom-Json
    $l = $d.Motores[0]; $g = $d.Motores[1]
    $filas = @(
        @('Proveedor', $l.Proveedor, $g.Proveedor), @('Fotos', $l.Evaluadas, $g.Evaluadas),
        @('Aciertos', $l.Aciertos, $g.Aciertos), @('Errores', $l.Errores, $g.Errores),
        @('Precisión global', ('{0:P1}' -f $l.PrecisionGlobal), ('{0:P1}' -f $g.PrecisionGlobal)),
        @('Falsas admisiones', $l.FalsasAdmisiones, $g.FalsasAdmisiones), @('Falsos rechazos', $l.FalsosRechazos, $g.FalsosRechazos),
        @('Abstenciones', $l.Abstenciones, $g.Abstenciones),
        @('Latencia media (ms)', $l.LatenciaMediaMs, $g.LatenciaMediaMs), @('Mediana (ms)', $l.LatenciaMedianaMs, $g.LatenciaMedianaMs), @('P95 (ms)', $l.LatenciaP95Ms, $g.LatenciaP95Ms),
        @('Fallos técnicos', $l.FallosTecnicos, $g.FallosTecnicos), @('Reintentos por límite', $l.ReintentosPorLimite, $g.ReintentosPorLimite),
        @('Tokens entrada/salida', '-', "$($g.TokensEntrada)/$($g.TokensSalida)"), @('Costo estimado (USD)', '0', ('{0:N4}' -f $g.CostoEstimadoUsd))
    )
    Write-Host ''
    foreach ($f in $filas) { Write-Host ('{0,-24} {1,-22} {2}' -f $f[0], $f[1], $f[2]) }
    $porFoto = @{}; foreach ($x in $g.Muestras) { $porFoto[$x.Sha256] = $x }
    $ambos = 0; $soloLocal = 0; $soloGroq = 0; $ninguno = 0
    foreach ($x in $l.Muestras) {
        $y = $porFoto[$x.Sha256]; $a = $x.Esperada -eq $x.Obtenida; $b = $y.Esperada -eq $y.Obtenida
        if ($a -and $b) { $ambos++ } elseif ($a) { $soloLocal++ } elseif ($b) { $soloGroq++ } else { $ninguno++ }
    }
    Write-Host "`nFoto a foto: aciertan ambos $ambos, solo local $soloLocal, solo Groq $soloGroq, ninguno $ninguno."
    Write-Host 'Son fotos de internet, no de tu cámara: la tabla describe este conjunto y no garantiza exactitud en la caneca.'
}

if ($SoloValidar) {
    $problema = Validar $SoloValidar
    if ($problema) { Write-Host "NO VÁLIDO: $problema" -ForegroundColor Red; exit 1 }
    Bien 'Comparación completa: dos motores, todas las fotos, sin fallos técnicos y con tokens de Groq registrados.'
    Mostrar $SoloValidar; exit 0
}

Paso '1. Proyecto y ejecutable'
if (-not (Test-Path (Join-Path $raiz 'EcoAccess.P3.sln'))) { Fallar "no se encontró la solución en $raiz" }
$rama = (& git -C $raiz rev-parse --abbrev-ref HEAD).Trim()
if ($rama -ne 'desarrollo/telegram-doble-ia') { Fallar "estás en la rama $rama. Cambia a desarrollo/telegram-doble-ia con: git switch desarrollo/telegram-doble-ia" }
Bien "rama $rama, commit $((& git -C $raiz rev-parse --short HEAD).Trim())"
$nuevo = if (Test-Path $bot) { Get-ChildItem (Join-Path $raiz 'src') -Recurse -Filter *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.LastWriteTime -gt (Get-Item $bot).LastWriteTime } | Select-Object -First 1 } else { $true }
if ($nuevo) {
    Write-Host '  El ejecutable falta o es más antiguo que el código: compilando.'
    & dotnet build (Join-Path $raiz 'EcoAccess.P3.sln') -c Release --nologo -v q | Out-Null
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $bot)) { Fallar 'no compiló. Ejecuta: dotnet build EcoAccess.P3.sln -c Release' }
}
Bien "ejecutable del $((Get-Item $bot).LastWriteTime.ToString('yyyy-MM-dd HH:mm'))"

Paso '2. Fotos, hashes y licencias'
if (-not (Test-Path $manifiesto)) { Fallar "falta $manifiesto" }
$copia = Join-Path $raiz 'docs\vision\resultados\manifiesto_groq.json'
if ((Get-FileHash $manifiesto).Hash -ne (Get-FileHash $copia).Hash) { Fallar 'el manifiesto del corpus no coincide con el del repositorio' }
$lista = @((Get-Content $manifiesto -Raw -Encoding UTF8 | ConvertFrom-Json).Muestras)
if ($lista.Count -ne $Muestras) { Fallar "el manifiesto tiene $($lista.Count) fotos, no $Muestras" }
$fuentes = @{}
foreach ($f in (Get-Content (Join-Path $raiz 'docs\vision\resultados\fuentes_fotos.json') -Raw -Encoding UTF8 | ConvertFrom-Json)) { $fuentes[$f.archivo] = $f.licencia }
$licencias = @{}
foreach ($s in $lista) {
    $ruta = Join-Path $Corpus $s.Archivo
    if (-not (Test-Path $ruta)) { Fallar "falta la foto $($s.Archivo)" }
    if ((Get-FileHash $ruta -Algorithm SHA256).Hash -ne $s.Sha256) { Fallar "la foto $($s.Archivo) cambió: su hash no coincide" }
    $licencia = if ($s.Archivo -like 'trashnet_*') { 'MIT (TrashNet)' } else { $fuentes[$s.Archivo] }
    if ($licencia -notmatch '^(MIT|CC0|CC BY|Public domain)') { Fallar "la foto $($s.Archivo) no tiene una licencia libre registrada" }
    $licencias[$licencia] = 1 + [int]$licencias[$licencia]
}
Bien "$Muestras fotos con hash correcto; licencias: $(($licencias.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join ', ')"

Paso '3. Servicios'
if (Get-Process EcoAccess.P3.Bot -ErrorAction SilentlyContinue) { Fallar 'el bot está en marcha y alteraría las latencias. Deténlo con: powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Detener-Demostracion.ps1' }
$oracle = Ejecutar @('--comprobar-oracle')
if ($oracle.Codigo -ne 0) { Fallar "Oracle no responde: $($oracle.Avisos)" }
Bien ($oracle.Texto -replace ' No se modificaron.*$', '').Trim()
$estado = (Ejecutar @('--estado-vision')).Texto | ConvertFrom-Json
if (-not ($estado.Local.Disponible -and $estado.Local.ModeloInstalado)) { Fallar 'Ollama o lfm3-vl no están disponibles' }
Bien "Ollama $($estado.Local.Version) con $($estado.Local.Modelo)"
$salud = (Ejecutar @('--comprobar-groq')).Texto | ConvertFrom-Json
if ($salud.Estado -ne 'Operativo') { Fallar "Groq: $($salud.Estado). $($salud.Mensaje)" }
Bien "Groq acepta la clave y ofrece $($salud.Modelo)"

Paso '4. Presupuesto configurado y confirmación'
$uso = LeerUso
$usadas = if ($uso) { [int]$uso.Solicitudes } else { 0 }
$gastado = if ($uso) { [decimal]$uso.CostoUsd } else { 0 }
if ($usadas + $Muestras + 6 -gt $estado.Groq.MaximoSolicitudesDia) { Fallar "hoy van $usadas solicitudes y el máximo diario configurado es $($estado.Groq.MaximoSolicitudesDia)" }
Bien "hoy: $usadas solicitudes y USD $gastado. Máximos configurados: $($estado.Groq.MaximoSolicitudesDia) solicitudes y USD $($estado.Groq.PresupuestoDiaUsd)"
Write-Host "`n  Se enviarán a Groq 1 + $Muestras fotos públicas, sin EXIF y a 512 px. Tarda cerca de 30 minutos."
Write-Host '  Costo estimado: unos 0,15 USD si tu cuenta paga. Se recalcula con la primera foto.'
Write-Host '  Groq indica que no retiene las inferencias, salvo registros de hasta 30 días para fiabilidad y abuso.'
if (-not $Confirmar) {
    $respuesta = Read-Host '  ¿Enviar las fotos a Groq? Escribe S para continuar'
    if ($respuesta -notmatch '^[sS]$') { Write-Host '  Cancelado. No se envió ninguna foto.'; exit 0 }
}

Paso '5. Una foto real para medir tokens y leer los límites de tu cuenta'
$antes = if ($uso) { [long]$uso.TokensEntrada + [long]$uso.TokensSalida } else { 0 }
$prueba = Ejecutar @('--comprobar-vision-groq', (Join-Path $Corpus 'commons_carton-1.jpg'))
if ($prueba.Avisos) { Write-Host "  $($prueba.Avisos.Trim())" -ForegroundColor Yellow }
try { $r = $prueba.Texto | ConvertFrom-Json } catch { $r = $null }
if ($prueba.Codigo -ne 0 -or -not $r) {
    $causa = if ($r) { "$($r.Resultado). $($r.Diagnostico.Error)" } else { $prueba.Avisos }
    Fallar "Groq no clasificó la foto de prueba: $causa"
}
Bien "Qwen respondió: $($r.Resultado), material $($r.Material), $($r.Milisegundos) ms"
$uso = LeerUso
$porFoto = if ($uso) { [long]$uso.TokensEntrada + [long]$uso.TokensSalida - $antes } else { 0 }
if ($porFoto -le 0) { $porFoto = 2700; Write-Host '  Groq no informó tokens; se usa una estimación de 2700 por foto.' -ForegroundColor Yellow }
$lim = $r.Limites
if ($lim) { Bien "tu cuenta informa: $($lim.SolicitudesRestantes) de $($lim.SolicitudesDia) solicitudes hoy y $($lim.TokensMinuto) tokens por minuto" }
else { Write-Host '  Groq no envió encabezados de límite; se usan los publicados.' -ForegroundColor Yellow }
$restantes = if ($lim -and $null -ne $lim.SolicitudesRestantes) { [long]$lim.SolicitudesRestantes } else { $solicitudesDiaPublicadas - $usadas - 1 }
$tokensMinuto = if ($lim -and $lim.TokensMinuto) { [long]$lim.TokensMinuto } else { $tokensMinutoPublicados }
if ($restantes -lt $Muestras) { Fallar "a tu cuenta le quedan $restantes solicitudes hoy y hacen falta $Muestras" }
Write-Host '  Si la cuenta agota sus tokens, se guarda lo medido y otra corrida continúa desde ahí.'
$costo = [decimal]$uso.CostoUsd * $Muestras / [math]::Max(1, $usadas + 1)
if ([decimal]$uso.CostoUsd + $costo -gt $estado.Groq.PresupuestoDiaUsd) { Fallar "el costo previsto (USD $costo) supera el presupuesto diario configurado" }
$pausa = [int][math]::Min(120, [math]::Max(20, [math]::Ceiling(60 * $porFoto / ($tokensMinuto * 0.85))))
$env:ECOACCESS_Vision__Groq__PausaEntreFotosSegundos = "$pausa"
Bien "$porFoto tokens por foto; pausa de $pausa s para no pasar de $tokensMinuto por minuto; costo previsto cerca de USD $([math]::Round($costo, 3))"

Paso '6. Comparación: primero el motor local, después Groq. No cierres esta ventana.'
$temporal = "$destino.tmp"
Remove-Item $temporal -ErrorAction SilentlyContinue
$inicio = Get-Date
$p = Lanzar @('--comparar-vision', $manifiesto, "$Muestras") $temporal $registro
$null = $p.Handle
$base = if (LeerUso) { [int](LeerUso).Solicitudes } else { 0 }
while (-not $p.WaitForExit(60000)) {
    $hechas = if (LeerUso) { [int](LeerUso).Solicitudes - $base } else { 0 }
    Write-Host ("  {0:N0} min. Solicitudes enviadas a Groq en esta corrida: {1} de {2}" -f ((Get-Date) - $inicio).TotalMinutes, $hechas, $Muestras)
}
$codigo = $p.ExitCode
if ($codigo -eq 3) {
    Remove-Item Env:ECOACCESS_Vision__Groq__PausaEntreFotosSegundos -ErrorAction SilentlyContinue
    Remove-Item $temporal -ErrorAction SilentlyContinue
    Get-Content $registro -Tail 2 -Encoding UTF8 | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
    Write-Host '  Comparación incompleta, sin resultado. Repite este mismo comando cuando la cuenta recupere sus tokens.' -ForegroundColor Yellow
    exit 3
}
Remove-Item Env:ECOACCESS_Vision__Groq__PausaEntreFotosSegundos -ErrorAction SilentlyContinue
Write-Host "  Terminó con código $codigo tras $([math]::Round(((Get-Date) - $inicio).TotalMinutes, 1)) minutos."

Paso '7. Validación del resultado'
$problema = if ($codigo -ne 0) { "el proceso terminó con código $codigo" } else { Validar $temporal }
if ($problema) {
    $incompleta = "$destino.incompleta"
    if (Test-Path $temporal) { Move-Item $temporal $incompleta -Force }
    Write-Host '  Últimos avisos del proceso:' -ForegroundColor Yellow
    Get-Content $registro -Tail 8 -Encoding UTF8 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "    $_" }
    Fallar "$problema. Registro: comparacion_groq.log. Lo parcial, si existe, queda en comparacion_groq.json.incompleta y no vale como resultado."
}
if (Test-Path $destino) { Move-Item $destino "$destino.$((Get-Date).ToString('yyyyMMdd-HHmmss')).anterior" }
Move-Item $temporal $destino
Bien "comparacion_groq.json completo y válido"
Mostrar $destino
Write-Host "`nAvísale a Claude que terminó: el archivo no contiene claves ni fotos." -ForegroundColor Cyan
