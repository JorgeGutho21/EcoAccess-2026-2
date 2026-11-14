<#
  Prueba de concurrencia de pkg_ecopuntos.registrar_regla con dos sesiones reales de SQL*Plus.

  1. Abre la sesión A (04_SCRIPTS_ORACLE\20_prueba_concurrencia_tarifas.sql): registra la primera tarifa
     de PLASTICO_RIGIDO y retiene la transacción -Retener segundos sin confirmarla.
  2. Dos segundos después abre la sesión B, que intenta registrar la primera tarifa del mismo material.
  3. Compara las horas: la prueba PASA solo si B terminó su registro DESPUÉS de que A terminó
     (B esperó el bloqueo de la fila de MATERIAL). Si B registró mientras A seguía abierta,
     las dos tarifas habrían quedado abiertas a la vez: FALLA.

  Las dos sesiones hacen ROLLBACK: no quedan datos nuevos. No imprime contraseñas.
  Uso:  pwsh -File prueba_concurrencia_tarifas.ps1 [-Retener 6] [-Salida <archivo.txt>]
  Código de salida: 0 si pasa, 1 si falla.
#>
param([int]$Retener = 6, [string]$Salida)
$ErrorActionPreference = 'Stop'
$comun = Join-Path $PSScriptRoot 'comun.ps1'
. $comun
$script = Join-Path $script:Raiz '04_SCRIPTS_ORACLE\20_prueba_concurrencia_tarifas.sql'
[void](Initialize-EcoOracle)
[void](Get-EcoClave 'ECOACCESS_BD' -Obligatoria)   # deja la clave en el entorno del proceso para los dos hilos

$sesion = {
    param($comun, $script, $papel, $retener)
    . $comun
    (Invoke-EcoSql -Usuario 'ECOACCESS_BD' -Scripts @($script) -Respuestas @($papel, "$retener")).Texto
}
$a = Start-ThreadJob -ScriptBlock $sesion -ArgumentList $comun, $script, 'A', $Retener
Start-Sleep -Seconds 2
$b = Start-ThreadJob -ScriptBlock $sesion -ArgumentList $comun, $script, 'B', $Retener
$textoA = Receive-Job $a -Wait -AutoRemoveJob | Out-String
$textoB = Receive-Job $b -Wait -AutoRemoveJob | Out-String

function Valor([string]$texto, [string]$clave) { $m = [regex]::Match($texto, "$clave=([^\r\n]+)"); if ($m.Success) { $m.Groups[1].Value.Trim() } }
function Hora([string]$h) { if ($h) { [TimeSpan]::ParseExact($h, 'hh\:mm\:ss\.fff', $null) } }
$finA = Hora (Valor $textoA 'FIN_A');  $regA = Hora (Valor $textoA 'REGISTRO_A')
$iniB = Hora (Valor $textoB 'INICIO_B'); $regB = Hora (Valor $textoB 'REGISTRO_B')
$errores = @((Valor $textoA 'ERROR_A'), (Valor $textoB 'ERROR_B')) | Where-Object { $_ }

$lineas = New-Object Collections.Generic.List[string]
$lineas.Add("Prueba de concurrencia de registrar_regla · $(Get-Date -Format 'yyyy-MM-dd HH:mm') · A retiene $Retener s")
$lineas.Add("Sesión A: reglas previas $(Valor $textoA 'REGLAS_PREVIAS_A') · inicio $(Valor $textoA 'INICIO_A') · registró $(Valor $textoA 'REGISTRO_A') · abiertas que ve $(Valor $textoA 'ABIERTAS_VISTAS_A') · terminó (ROLLBACK) $(Valor $textoA 'FIN_A')")
$lineas.Add("Sesión B: reglas previas $(Valor $textoB 'REGLAS_PREVIAS_B') · inicio $(Valor $textoB 'INICIO_B') · registró $(Valor $textoB 'REGISTRO_B') · abiertas que ve $(Valor $textoB 'ABIERTAS_VISTAS_B') · terminó (ROLLBACK) $(Valor $textoB 'FIN_B')")
if ($errores) { $lineas.Add("Errores: $($errores -join ' | ')") }

$pasa = $false
if ($finA -and $regB -and $iniB -and -not $errores) {
    $espera = ($regB - $iniB).TotalSeconds
    if ($regB -ge $finA) {
        $pasa = $true
        $lineas.Add(("PASA  B esperó {0:N1} s y registró solo después de que A terminó: nunca hubo dos tarifas abiertas a la vez." -f $espera))
    } else {
        $lineas.Add(("FALLA B registró a los {0:N1} s, mientras A seguía con su tarifa abierta sin confirmar: dos tarifas abiertas a la vez." -f $espera))
    }
} else {
    $lineas.Add('FALLA no se pudieron leer las horas de las dos sesiones.')
}
$lineas.Add('Las dos sesiones hicieron ROLLBACK: la base quedó igual.')
$lineas.Add(''); $lineas.Add('----- Salida de la sesión A -----'); $lineas.Add($textoA.Trim())
$lineas.Add(''); $lineas.Add('----- Salida de la sesión B -----'); $lineas.Add($textoB.Trim())
$texto = $lineas -join [Environment]::NewLine
if ($Salida) { [IO.File]::WriteAllText($Salida, $texto, (New-Object Text.UTF8Encoding $false)) }
$lineas[0..4] | Where-Object { $_ } | ForEach-Object { $_ }
if (-not $pasa) { exit 1 }
