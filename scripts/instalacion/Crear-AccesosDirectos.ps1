<#
.SYNOPSIS
    Crea los accesos directos oficiales de ECOACCESS en el escritorio de Windows (también si está redirigido a OneDrive).

.DESCRIPTION
    EstacionPrincipal: exactamente CUATRO accesos.
      · ECOACCESS P3               aplicación de escritorio (Release instalada). En la estación prepara antes el bot, el
                                   panel y la pasarela sin duplicarlos y sin abrir tapas.
      · ECOACCESS ESP32            panel local http://localhost:8090 (lo inicia si hace falta; no mueve motores al abrir).
      · ECOACCESS DOS CLICS MANUAL interfaz ciudadana para elegir caneca, escribir o hablar (/dos-clics/).
      · ECOACCESS WEB              plataforma oficial (académica + uso ciudadano + EcoAccess Challenge integrado).
    EstacionCompanero: los mismos sin ECOACCESS ESP32 (tres), porque ese equipo no tiene la estación.

    Cualquier otro acceso del escritorio relacionado con EcoAccess (por nombre o por destino) se MUEVE a
    %LOCALAPPDATA%\EcoAccessP3\respaldo_accesos_directos; no se borra. Los accesos de otros programas no se tocan.
    Volver a ejecutarlo deja siempre el mismo conjunto: nunca uno de más ni uno de menos.
    Lo llama Instalar-EcoAccess.ps1; también se puede ejecutar solo para regenerar los accesos.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\instalacion\Crear-AccesosDirectos.ps1 -Perfil EstacionPrincipal
#>
[CmdletBinding()]
param(
    [string]$Destino = (Join-Path $env:LOCALAPPDATA 'Programs\EcoAccess P3'),
    [ValidateSet('EstacionPrincipal', 'EstacionCompanero')]
    [string]$Perfil = 'EstacionCompanero',
    # Raíz del repositorio (estación principal instalada desde el código). Vacía en el paquete distribuible.
    [string]$Raiz,
    [string]$Escritorio = [Environment]::GetFolderPath('Desktop'),
    [string]$UrlWeb = 'https://jorgegutho21.github.io/EcoAccess-2026-2/',
    [string]$UrlDosClics = 'https://jorgegutho21.github.io/EcoAccess-2026-2/dos-clics/'
)
$ErrorActionPreference = 'Stop'
$resultados = [System.Collections.Generic.List[object]]::new()
function Anotar([string]$Prueba, [bool]$Correcta, [string]$Detalle) {
    $resultados.Add([pscustomobject]@{ Prueba = $Prueba; Resultado = $(if ($Correcta) { 'OK' } else { 'FALTA' }); Detalle = $Detalle })
}

$nombreExe = 'EcoAccess.P3.Presentacion.exe'
$exe = Join-Path $Destino $nombreExe
$conhost = Join-Path $env:SystemRoot 'System32\conhost.exe'
$explorer = Join-Path $env:SystemRoot 'explorer.exe'
function Icono([string]$archivo) {
    foreach ($c in @((Join-Path $Destino $archivo), (Join-Path $Destino 'EcoAccess.ico'))) { if (Test-Path -LiteralPath $c) { return "$c,0" } }
    return "$exe,0"
}

# Conjunto oficial según el perfil.
$oficiales = [ordered]@{ 'ECOACCESS P3.lnk' = $true }
if ($Perfil -eq 'EstacionPrincipal') { $oficiales['ECOACCESS ESP32.lnk'] = $true }
$oficiales['ECOACCESS DOS CLICS MANUAL.lnk'] = $true
$oficiales['ECOACCESS WEB.lnk'] = $true

$shell = New-Object -ComObject WScript.Shell
$respaldo = Join-Path $env:LOCALAPPDATA 'EcoAccessP3\respaldo_accesos_directos'
foreach ($viejo in Get-ChildItem -LiteralPath $Escritorio -Filter '*.lnk' -ErrorAction SilentlyContinue) {
    # El nombre se compara en mayúsculas exactas: «EcoAccess P3.lnk» (antiguo) también se respalda y se recrea bien escrito.
    if ($oficiales.Contains($viejo.Name) -and $viejo.Name -ceq ($oficiales.Keys | Where-Object { $_ -ieq $viejo.Name })) { continue }
    $destinoViejo = $shell.CreateShortcut($viejo.FullName)
    if ($viejo.BaseName -match '(?i)eco\s*access' -or "$($destinoViejo.TargetPath) $($destinoViejo.Arguments)" -match '(?i)ecoaccess') {
        New-Item -ItemType Directory -Force -Path $respaldo | Out-Null
        Move-Item -LiteralPath $viejo.FullName -Destination (Join-Path $respaldo $viejo.Name) -Force
        Write-Host "Acceso directo anterior '$($viejo.BaseName)' respaldado en $respaldo" -ForegroundColor DarkYellow
    }
}

# 1. ECOACCESS P3
$lanzador = Join-Path $Destino 'Abrir-EcoAccess.ps1'
New-Item -ItemType Directory -Force -Path $Destino | Out-Null
$contenido = @"
# Lanzador oficial generado por Crear-AccesosDirectos.ps1 ($Perfil).
# Inicia directamente EcoAccess P3 desde la instalacion oficial sin abrir canecas fisicas ni duplicar bots.
`$exe = '$exe'
if (-not (Test-Path -LiteralPath `$exe)) {
    throw "No se encontro EcoAccess P3 en `$exe"
}
Start-Process -FilePath `$exe -WorkingDirectory '$Destino'
"@
[IO.File]::WriteAllText($lanzador, $contenido, (New-Object Text.UTF8Encoding($true)))
$lnk = Join-Path $Escritorio 'ECOACCESS P3.lnk'
$acceso = $shell.CreateShortcut($lnk)
$lanzadorEstacion = if ($Raiz) { Join-Path $Raiz 'scripts\desarrollo\Abrir-EcoAccess.ps1' } else { $null }
if ($Perfil -eq 'EstacionPrincipal' -and $lanzadorEstacion -and (Test-Path -LiteralPath $lanzadorEstacion) -and (Test-Path -LiteralPath $conhost)) {
    $acceso.TargetPath = $conhost
    $acceso.Arguments = "--headless powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$lanzadorEstacion`""
    $esperado = $conhost
} else {
    $acceso.TargetPath = $exe
    $acceso.Arguments = ''
    $esperado = $exe
}
$acceso.WorkingDirectory = $Destino
$acceso.IconLocation = Icono 'EcoAccess.ico'
$acceso.Description = "ECOACCESS P3 ($Perfil): aplicacion principal con Oracle Cloud, vision local, Telegram y tres canecas"
$acceso.Save()
Anotar 'Acceso directo ECOACCESS P3' ($shell.CreateShortcut($lnk).TargetPath -eq $esperado) "$lnk -> $(Split-Path -Leaf $esperado)"

# 2. ECOACCESS ESP32 (solo estación principal)
if ($Perfil -eq 'EstacionPrincipal') {
    $lanzadorPanel = if ($Raiz) { Join-Path $Raiz 'scripts\desarrollo\Abrir-PanelCanecas.ps1' } else { $null }
    $lnkPanel = Join-Path $Escritorio 'ECOACCESS ESP32.lnk'
    $panelLnk = $shell.CreateShortcut($lnkPanel)
    if ($lanzadorPanel -and (Test-Path -LiteralPath $lanzadorPanel) -and (Test-Path -LiteralPath $conhost)) {
        $panelLnk.TargetPath = $conhost
        $panelLnk.Arguments = "--headless powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$lanzadorPanel`""
        $panelLnk.WorkingDirectory = $Raiz
    } else {
        # Sin el repositorio: abre el panel si ya está en marcha (el arranque automático lo inicia).
        $panelLnk.TargetPath = $explorer
        $panelLnk.Arguments = 'http://localhost:8090'
    }
    $panelLnk.IconLocation = Icono 'EcoAccess_ESP32.ico'
    $panelLnk.Description = 'ECOACCESS ESP32: centro de control local de las tres canecas (http://localhost:8090)'
    $panelLnk.Save()
    Anotar 'Acceso directo ECOACCESS ESP32' (Test-Path -LiteralPath $lnkPanel) $lnkPanel
}

# 3. ECOACCESS DOS CLICS MANUAL y 4. ECOACCESS WEB (navegador predeterminado)
foreach ($w in @(
        @{ N = 'ECOACCESS DOS CLICS MANUAL'; U = $UrlDosClics; I = 'EcoAccess_DOS_CLICS.ico'; D = 'ECOACCESS DOS CLICS MANUAL: elige una caneca, escribe o di tu residuo' },
        @{ N = 'ECOACCESS WEB'; U = $UrlWeb; I = 'EcoAccess_WEB.ico'; D = 'ECOACCESS WEB: plataforma oficial (proyecto, tecnologia, uso ciudadano y EcoAccess Challenge)' })) {
    $ruta = Join-Path $Escritorio "$($w.N).lnk"
    $l = $shell.CreateShortcut($ruta)
    $l.TargetPath = $explorer
    $l.Arguments = $w.U
    $l.IconLocation = Icono $w.I
    $l.Description = "$($w.D) ($($w.U))"
    $l.Save()
    Anotar "Acceso directo $($w.N)" ($shell.CreateShortcut($ruta).Arguments -eq $w.U) "$ruta -> $($w.U)"
}

# Comprobación final: exactamente el conjunto oficial.
$presentes = @(Get-ChildItem -LiteralPath $Escritorio -Filter '*.lnk' | Where-Object { $_.BaseName -match '(?i)eco\s*access' } | ForEach-Object Name)
$exacto = ($presentes.Count -eq $oficiales.Count) -and -not ($oficiales.Keys | Where-Object { $presentes -cnotcontains $_ })
Anotar "Exactamente $($oficiales.Count) accesos de ECOACCESS" $exacto ($presentes -join ', ')
$resultados
