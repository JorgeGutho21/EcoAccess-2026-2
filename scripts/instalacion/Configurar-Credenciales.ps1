<#
.SYNOPSIS
    Guarda una sola vez, en el perfil privado de Windows, las credenciales y el motor visual de EcoAccess.
.DESCRIPTION
    Pide los secretos sin mostrarlos, los comprueba antes de guardarlos y nunca los imprime. Una entrada vacía conserva
    el valor actual. Sin parámetros pregunta por todo. Ejecútalo en tu propia ventana de PowerShell, no desde otra aplicación.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Credenciales.ps1 -Groq
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Credenciales.ps1 -Modo Automatico
#>
[CmdletBinding()]
param(
    [switch] $Telegram,
    [switch] $Groq,
    [switch] $Oracle,
    [ValidateSet('Local', 'Groq', 'Automatico')] [string] $Modo,
    [string] $IdSecretos = 'ecoaccess-p3-framework',
    [string] $NombreBot = 'EcoAccessP3_JG_bot',
    [string] $ModeloGroq = 'qwen/qwen3.8-27b'
)
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$archivo = Join-Path $env:APPDATA "Microsoft\UserSecrets\$IdSecretos\secrets.json"
$todo = -not ($Telegram -or $Groq -or $Oracle -or $Modo)

function Leer-Secreto([string] $Pregunta) {
    $seguro = Read-Host $Pregunta -AsSecureString
    $puntero = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($seguro)
    try { return ([Runtime.InteropServices.Marshal]::PtrToStringBSTR($puntero)).Trim() }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($puntero) }
}

function Guardar([hashtable] $Valores) {
    $secretos = if (Test-Path -LiteralPath $archivo) { Get-Content -LiteralPath $archivo -Raw -Encoding UTF8 | ConvertFrom-Json } else { New-Object psobject }
    foreach ($par in $Valores.GetEnumerator()) { $secretos | Add-Member -NotePropertyName $par.Key -NotePropertyValue $par.Value -Force }
    New-Item -ItemType Directory -Force (Split-Path -Parent $archivo) | Out-Null
    [IO.File]::WriteAllText($archivo, ($secretos | ConvertTo-Json), (New-Object Text.UTF8Encoding($false)))
}

function Tiene([string] $Clave) {
    if (-not (Test-Path -LiteralPath $archivo)) { return $false }
    return [bool](Get-Content -LiteralPath $archivo -Raw -Encoding UTF8 | ConvertFrom-Json).$Clave
}

if ($Telegram -or $todo) {
    $estado = if (Tiene 'Telegram:TokenBot') { 'hay uno guardado; Enter lo conserva' } else { 'BotFather > /mybots > API Token' }
    $token = Leer-Secreto "Token del bot ($estado)"
    if ($token) {
        if ($token -cnotmatch '\A[0-9]{5,}:[A-Za-z0-9_-]{20,}\z') { throw 'El token no tiene el formato de BotFather. No se guardó nada.' }
        try { $identidad = Invoke-RestMethod "https://api.telegram.org/bot$token/getMe" -TimeoutSec 15 }
        catch { throw 'Telegram no aceptó el token o no hay conexión. No se guardó nada.' }
        if ($identidad.result.username -ine $NombreBot) { throw "El token no pertenece a @$NombreBot. No se guardó nada." }
        Guardar @{ 'Telegram:TokenBot' = $token; 'Telegram:NombreBot' = $NombreBot }
        Write-Output "Telegram: @$NombreBot verificado con getMe y guardado."
    }
    else { Write-Output 'Telegram: sin cambios.' }
    Remove-Variable token
}

if ($Groq -or $todo) {
    $estado = if (Tiene 'Vision:Groq:ApiKey') { 'hay una guardada; Enter la conserva' } else { 'console.groq.com/keys > Create API Key' }
    $clave = Leer-Secreto "Clave de Groq ($estado)"
    if ($clave) {
        try { $modelos = Invoke-RestMethod 'https://api.groq.com/openai/v1/models' -Headers @{ Authorization = "Bearer $clave" } -TimeoutSec 20 }
        catch {
            $codigo = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
            throw "Groq no aceptó la clave o no hay conexión (HTTP $codigo). No se guardó nada."
        }
        if (-not ($modelos.data | Where-Object { $_.id -ceq $ModeloGroq })) { throw "La clave es válida, pero esta cuenta no ofrece $ModeloGroq. No se guardó nada ni se eligió otro modelo." }
        Guardar @{ 'Vision:Groq:ApiKey' = $clave; 'Vision:Groq:Modelo' = $ModeloGroq }
        Write-Output "Groq: clave aceptada y modelo $ModeloGroq disponible. No se envió ninguna imagen."
    }
    else { Write-Output 'Groq: sin cambios.' }
    Remove-Variable clave
}

if ($Oracle -or $todo) {
    $estado = if (Tiene 'ConnectionStrings:OracleP3') { 'hay una guardada; Enter la conserva' } else { 'User Id=...;Password=...;Data Source=localhost:1522/PRUEBAP3' }
    $cadena = Leer-Secreto "Conexión Oracle ($estado)"
    if ($cadena) {
        $bot = Join-Path $raiz 'src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe'
        if (-not (Test-Path $bot)) { throw 'Compila primero: dotnet build EcoAccess.P3.sln -c Release' }
        $env:ECOACCESS_ConnectionStrings__OracleP3 = $cadena
        try { & $bot --comprobar-oracle 2>$null | Out-Null; $correcto = $LASTEXITCODE -eq 0 }
        finally { Remove-Item Env:ECOACCESS_ConnectionStrings__OracleP3 }
        if (-not $correcto) { throw 'Oracle no respondió con esa conexión. No se guardó nada.' }
        Guardar @{ 'ConnectionStrings:OracleP3' = $cadena }
        Write-Output 'Oracle: conexión comprobada y guardada.'
    }
    else { Write-Output 'Oracle: sin cambios.' }
    Remove-Variable cadena
}

if ($todo -and -not $Modo) {
    $respuesta = Read-Host 'Motor visual: Local, Groq o Automatico (Enter conserva el actual)'
    if ($respuesta) {
        if ($respuesta -notin 'Local', 'Groq', 'Automatico') { throw 'Elige Local, Groq o Automatico.' }
        $Modo = $respuesta
    }
}
if ($Modo) {
    if ($Modo -ne 'Local' -and -not (Tiene 'Vision:Groq:ApiKey')) { throw "El modo $Modo necesita la clave de Groq: ejecuta este script con -Groq." }
    # Ollama solo escucha en este equipo: el motor local nunca envía la foto a otro servidor.
    $valores = @{ 'Vision:Modo' = $Modo }
    if ($Modo -ne 'Groq') { $valores['Vision:Ollama:ServidorSinNubeVerificado'] = 'true' }
    Guardar $valores
    Write-Output "Motor visual: $Modo. Reinicia el bot para aplicarlo."
    if ($Modo -ne 'Local') { Write-Output 'Las fotos solo saldrán a Groq cuando cada persona lo autorice en Telegram; puede retirarlo con /revocarfotos.' }
}
Write-Output 'Comprueba el resultado con scripts\desarrollo\Verificar-EcoAccess-Telegram.ps1'
