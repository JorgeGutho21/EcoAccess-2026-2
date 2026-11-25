<#
.SYNOPSIS
    Autoriza UN identificador numérico de Telegram, ya verificado por el supervisor, para abrir las canecas desde
    Telegram sin escanear el QR de la estación. No desactiva ningún control de acceso.
.DESCRIPTION
    Mecanismo real (no cambia con este script):
      1. La lista vive en %ProgramData%\EcoAccessP3Framework\Bridge\telegram-autorizados.txt, un identificador por línea.
      2. scripts\desarrollo\Iniciar-EstacionExpo.ps1 la lee al iniciar el bot (solo líneas de 4 a 19 dígitos) y la pasa
         al proceso en la variable ECOACCESS_TELEGRAM_CHATS_AUTORIZADOS.
      3. CoordinadorEstacionUnificado.EsChatAutorizado(chat, usuario) acepta el chat o el usuario de esa lista; los demás
         solo abren tras escanear el QR de la estación. En cualquier caso el panel sigue exigiendo la sesión supervisada
         (ACTIVAR CONTROL) para mover un motor.
    Este script solo AÑADE un identificador que tú indicas (nunca lo toma de un mensaje ni de un archivo), comprueba su
    forma, pide confirmación y no reinicia nada. La lista se aplica la próxima vez que arranque el bot: al iniciar sesión
    (tarea programada) o con «Iniciar-EstacionExpo.ps1 -ReiniciarBot».

    Cómo obtener el identificador verificado: la persona escribe /miid al bot desde SU cuenta y te muestra la respuesta
    en persona; compruébalo en su teléfono antes de autorizarlo.
.PARAMETER Identificador
    ID numérico de usuario de Telegram (4 a 19 dígitos, sin signo). Los chats de grupo (negativos) no se admiten.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\arranque\Autorizar-CuentaTelegram.ps1 -Identificador 123456789 -WhatIf
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\arranque\Autorizar-CuentaTelegram.ps1 -Identificador 123456789
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)] [ValidatePattern('^\d{4,19}$')] [string] $Identificador
)
$ErrorActionPreference = 'Stop'
if ([decimal] $Identificador -gt [decimal] [long]::MaxValue -or [long] $Identificador -le 0) { throw 'El identificador no es un ID de usuario de Telegram válido.' }

$base = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework'
$archivo = Join-Path $base 'Bridge\telegram-autorizados.txt'
$actuales = @(if (Test-Path -LiteralPath $archivo) { Get-Content -LiteralPath $archivo | ForEach-Object { $_.Trim() } | Where-Object { $_ -match '^\d{4,19}$' } })
if ($actuales -contains $Identificador) { Write-Host "Ese identificador ya estaba autorizado ($($actuales.Count) en total). No se cambió nada."; exit 0 }

$oculto = $Identificador.Substring(0, 2) + ('•' * ($Identificador.Length - 4)) + $Identificador.Substring($Identificador.Length - 2)
if ($PSCmdlet.ShouldProcess("Telegram $oculto", "Autorizar para abrir canecas (lista con $($actuales.Count) cuentas)")) {
    New-Item -ItemType Directory -Force (Split-Path $archivo) | Out-Null
    # ASCII: el lanzador lee la lista con Get-Content y una marca BOM invalidaría la primera línea.
    Add-Content -LiteralPath $archivo -Value $Identificador -Encoding ASCII
    $log = Join-Path $base 'Sesiones\arranque-ecoaccess.log'
    New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
    Add-Content -LiteralPath $log -Value "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Autorización Telegram: se añadió $oculto (total $($actuales.Count + 1)) por $env:USERNAME." -Encoding UTF8
    Write-Host "Autorizado $oculto. Total: $($actuales.Count + 1)."
    Write-Host 'Se aplica cuando el bot vuelva a iniciar (próximo inicio de sesión, o ahora con:'
    Write-Host '  powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-EstacionExpo.ps1 -ReiniciarBot)'
    Write-Host 'Para revocar: borra la línea del archivo y reinicia el bot del mismo modo.'
}
