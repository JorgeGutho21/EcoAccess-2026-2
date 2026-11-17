<#
.SYNOPSIS
    Instala EcoAccess P3 (aplicación de escritorio) en este equipo y crea el acceso directo «EcoAccess P3» con el logo.

.DESCRIPTION
    Sirve para el responsable del proyecto y para los compañeros, sin rutas propias de ningún PC:
      1. Toma la aplicación compilada: la compila desde el repositorio (desarrolladores, requiere el SDK de .NET) o
         usa el paquete publicado si este script se ejecuta desde la carpeta descomprimida del paquete.
      2. Copia la aplicación a %LOCALAPPDATA%\Programs\EcoAccess P3 (fuera del repositorio: un «git pull» no la bloquea).
      3. Instala el wallet de Oracle Cloud (opcional, -Wallet) en %LOCALAPPDATA%\EcoAccessP3\wallet, la carpeta que
         la aplicación busca por sí sola. El wallet nunca se copia al repositorio.
      4. Crea en el escritorio un único acceso directo «EcoAccess P3» que abre el ejecutable sin consola.
      5. Ejecuta pruebas de humo: ejecutable, logo, wallet y alias ecoaccess_tp.
    No pide, muestra ni guarda contraseñas: la primera vez, la aplicación pide la contraseña del esquema en su
    pantalla de configuración, la comprueba con Oracle y la guarda solo en el perfil de Windows (User Secrets).
    Docker no es necesario: la base oficial es Oracle Cloud.

.PARAMETER Wallet
    Ruta del Wallet_*.zip (o de la carpeta descomprimida) de Oracle Cloud que entrega el responsable del proyecto.

.PARAMETER Destino
    Carpeta de instalación. Por defecto %LOCALAPPDATA%\Programs\EcoAccess P3.

.PARAMETER SinCompilar
    No compila; usa la última compilación Release existente en el repositorio.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\instalacion\Instalar-EcoAccess.ps1 -Wallet "$HOME\Downloads\Wallet_ecoaccess.zip"
#>
[CmdletBinding()]
param(
    [string]$Wallet,
    [string]$Destino = (Join-Path $env:LOCALAPPDATA 'Programs\EcoAccess P3'),
    [switch]$SinCompilar
)
$ErrorActionPreference = 'Stop'
$nombreExe = 'EcoAccess.P3.Presentacion.exe'
$resultados = [System.Collections.Generic.List[object]]::new()
function Anotar([string]$Prueba, [bool]$Correcta, [string]$Detalle) {
    $resultados.Add([pscustomobject]@{ Prueba = $Prueba; Resultado = $(if ($Correcta) { 'OK' } else { 'FALTA' }); Detalle = $Detalle })
}

# 1. Origen de la aplicación: paquete publicado (exe junto al script) o repositorio.
$paquete = Join-Path $PSScriptRoot 'aplicacion'
if (Test-Path -LiteralPath (Join-Path $paquete $nombreExe)) {
    $origen = $paquete
    Write-Host 'Instalando desde el paquete publicado.' -ForegroundColor Cyan
} else {
    $raiz = Resolve-Path (Join-Path $PSScriptRoot '..\..')
    $proyecto = Join-Path $raiz 'src\EcoAccess.P3.Presentacion\EcoAccess.P3.Presentacion.csproj'
    $origen = Join-Path $raiz 'src\EcoAccess.P3.Presentacion\bin\Release\net48'
    if (-not $SinCompilar) {
        if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
            throw 'Falta el SDK de .NET (https://dotnet.microsoft.com/download). Instálalo o usa el paquete publicado de EcoAccess.'
        }
        Write-Host 'Restaurando paquetes NuGet y compilando EcoAccess (Release)…' -ForegroundColor Cyan
        Push-Location -LiteralPath $raiz
        try {
            & dotnet build $proyecto -c Release -nologo -v quiet
            if ($LASTEXITCODE -ne 0) { throw "La compilación falló (código $LASTEXITCODE). Revisa los errores de arriba." }
        } finally { Pop-Location }
    }
}
if (-not (Test-Path -LiteralPath (Join-Path $origen $nombreExe))) { throw "No se encontró $nombreExe en $origen." }

# 2. Copia a la carpeta de instalación (cierra antes la aplicación si está abierta desde allí).
$abierta = Get-Process -Name 'EcoAccess.P3.Presentacion' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($Destino, [StringComparison]::OrdinalIgnoreCase) }
if ($abierta) { throw 'EcoAccess está abierto desde la carpeta de instalación. Ciérralo y vuelve a ejecutar el instalador.' }
New-Item -ItemType Directory -Force -Path $Destino | Out-Null
& robocopy.exe $origen $Destino /MIR /NFL /NDL /NJH /NJS /NP /R:2 /W:1 | Out-Null
if ($LASTEXITCODE -ge 8) { throw "No se pudo copiar la aplicación (robocopy $LASTEXITCODE)." }
$exe = Join-Path $Destino $nombreExe
Anotar 'Aplicación instalada' (Test-Path -LiteralPath $exe) $Destino

# 3. Wallet de Oracle Cloud (opcional): se instala fuera del repositorio.
$carpetaWallet = Join-Path $env:LOCALAPPDATA 'EcoAccessP3\wallet'
if ($Wallet) {
    if (-not (Test-Path -LiteralPath $Wallet)) { throw "No existe el wallet indicado: $Wallet" }
    New-Item -ItemType Directory -Force -Path $carpetaWallet | Out-Null
    if ((Get-Item -LiteralPath $Wallet).PSIsContainer) {
        Copy-Item -Path (Join-Path $Wallet '*') -Destination $carpetaWallet -Recurse -Force
    } else {
        Expand-Archive -LiteralPath $Wallet -DestinationPath $carpetaWallet -Force
    }
    # El sqlnet.ora del zip apunta a «?/network/admin»: ODP.NET necesita la ruta real de la carpeta del wallet.
    $sqlnet = Join-Path $carpetaWallet 'sqlnet.ora'
    if (Test-Path -LiteralPath $sqlnet) {
        $texto = Get-Content -LiteralPath $sqlnet -Raw
        $corregido = [regex]::Replace($texto, 'DIRECTORY\s*=\s*"[^"]*"', 'DIRECTORY="' + $carpetaWallet + '"')
        if ($corregido -ne $texto) {
            if (-not (Test-Path -LiteralPath "$sqlnet.original")) { Copy-Item -LiteralPath $sqlnet -Destination "$sqlnet.original" }
            Set-Content -LiteralPath $sqlnet -Value $corregido -Encoding ASCII -NoNewline
        }
    }
    # Solo el usuario actual puede leer el wallet.
    & icacls.exe $carpetaWallet /inheritance:r /grant:r "$($env:USERNAME):(OI)(CI)F" | Out-Null
}
$candidatas = @($env:TNS_ADMIN, [Environment]::GetEnvironmentVariable('TNS_ADMIN', 'User'), $carpetaWallet,
    (Join-Path $env:USERPROFILE '.oracle\wallets\EcoAccess')) | Where-Object { $_ }
$walletEncontrado = $candidatas | Where-Object { Test-Path -LiteralPath (Join-Path $_ 'tnsnames.ora') } | Select-Object -First 1
Anotar 'Wallet de Oracle Cloud' ([bool]$walletEncontrado) $(if ($walletEncontrado) { $walletEncontrado } else { 'pide el Wallet_*.zip y repite con -Wallet <ruta>' })
$alias = $walletEncontrado -and (Select-String -LiteralPath (Join-Path $walletEncontrado 'tnsnames.ora') -Pattern '^\s*ecoaccess_tp\s*=' -Quiet)
Anotar 'Alias ecoaccess_tp' ([bool]$alias) 'tnsnames.ora del wallet'

# 4. Acceso directo único «EcoAccess P3» en el escritorio (sin consola: apunta al ejecutable).
$escritorio = [Environment]::GetFolderPath('Desktop')
$shell = New-Object -ComObject WScript.Shell
$respaldo = Join-Path $env:LOCALAPPDATA 'EcoAccessP3\respaldo_accesos_directos'
foreach ($viejo in Get-ChildItem -LiteralPath $escritorio -Filter '*.lnk' -ErrorAction SilentlyContinue) {
    if ($viejo.Name -eq 'EcoAccess P3.lnk') { continue }
    $destinoViejo = $shell.CreateShortcut($viejo.FullName)
    if ($viejo.BaseName -match 'EcoAccess' -or "$($destinoViejo.TargetPath) $($destinoViejo.Arguments)" -match 'EcoAccess') {
        # No se borra: se guarda una copia para poder restaurarlo.
        New-Item -ItemType Directory -Force -Path $respaldo | Out-Null
        Move-Item -LiteralPath $viejo.FullName -Destination (Join-Path $respaldo $viejo.Name) -Force
        Write-Host "Acceso directo anterior «$($viejo.BaseName)» movido a $respaldo" -ForegroundColor DarkYellow
    }
}
$lnk = Join-Path $escritorio 'EcoAccess P3.lnk'
$acceso = $shell.CreateShortcut($lnk)
$acceso.TargetPath = $exe
$acceso.WorkingDirectory = $Destino
$acceso.IconLocation = "$exe,0"
$acceso.Description = 'EcoAccess P3: gestión de residuos con Oracle Cloud, Telegram y tres canecas'
$acceso.Save()
$creado = $shell.CreateShortcut($lnk)
Anotar 'Acceso directo «EcoAccess P3»' ($creado.TargetPath -eq $exe) $lnk

# 5. Logo embebido en el ejecutable.
Add-Type -AssemblyName System.Drawing
$icono = [System.Drawing.Icon]::ExtractAssociatedIcon($exe)
Anotar 'Logo en el ejecutable' ($null -ne $icono) 'icono de assets\identidad\EcoAccess.ico'

$resultados | Format-Table -AutoSize
if ($resultados.Resultado -contains 'FALTA') {
    Write-Host 'Instalación con pendientes: revisa las filas FALTA. Sin wallet, EcoAccess abre su pantalla de configuración y explica qué falta.' -ForegroundColor Yellow
} else {
    Write-Host 'EcoAccess P3 quedó instalado. Ábrelo desde el acceso directo «EcoAccess P3» del escritorio.' -ForegroundColor Green
}
