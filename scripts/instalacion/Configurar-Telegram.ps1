<#
.SYNOPSIS
    Guarda de forma privada el token del bot oficial, tras comprobar con Telegram que corresponde a @EcoAccessP3_JG_bot.
.DESCRIPTION
    Pide el token sin mostrarlo. Solo debe configurarse en el equipo que atenderá el bot: Telegram admite un consumidor.
#>
[CmdletBinding()]
param([string] $IdSecretos = 'ecoaccess-p3-framework', [string] $NombreEsperado = 'EcoAccessP3_JG_bot')
$ErrorActionPreference = 'Stop'
$seguro = Read-Host 'Token de BotFather' -AsSecureString
$puntero = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($seguro)
try { $token = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($puntero) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($puntero) }
if ($token -cnotmatch '\A[0-9]{5,}:[A-Za-z0-9_-]{20,}\z') { throw 'El token no tiene el formato de BotFather.' }
try { $identidad = Invoke-RestMethod "https://api.telegram.org/bot$token/getMe" -TimeoutSec 15 }
catch { throw 'Telegram no aceptó el token. No se guardó nada.' }
if ($identidad.result.username -ine $NombreEsperado) { throw "El token no pertenece a @$NombreEsperado. No se guardó nada." }
$archivo = Join-Path $env:APPDATA "Microsoft\UserSecrets\$IdSecretos\secrets.json"
$secretos = if (Test-Path $archivo) { Get-Content $archivo -Raw -Encoding UTF8 | ConvertFrom-Json } else { New-Object psobject }
$secretos | Add-Member -NotePropertyName 'Telegram:TokenBot' -NotePropertyValue $token -Force
$secretos | Add-Member -NotePropertyName 'Telegram:NombreBot' -NotePropertyValue $NombreEsperado -Force
New-Item -ItemType Directory -Force (Split-Path -Parent $archivo) | Out-Null
[IO.File]::WriteAllText($archivo, ($secretos | ConvertTo-Json), (New-Object Text.UTF8Encoding($false)))
Remove-Variable token, secretos
Write-Output "Bot @$NombreEsperado verificado y guardado en el perfil privado."
