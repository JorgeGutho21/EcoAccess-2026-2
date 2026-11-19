<#
.SYNOPSIS
  Ejecuta las pruebas de base de datos de EcoAccess y guarda la evidencia en docs/database/evidencias.

.DESCRIPTION
  A conexión · B estructura · C compilación · D consultas a las vistas · E lógica PL/SQL sin escrituras
  · F flujo completo de depósito y ecopuntos (escribe y termina con ROLLBACK)            → solo con -IncluirEscritura
  · I seguridad del repositorio (sin wallets, .env ni contraseñas versionadas)

  A–E son de solo lectura y se pueden ejecutar contra la base compartida de Oracle Cloud.
  F inserta datos de prueba (prefijo IT_V027_) y los deshace con ROLLBACK; el script se NIEGA a ejecutarla si la
  conexión no es un esquema aislado en localhost.

.EXAMPLE
  pwsh -File database\scripts\Ejecutar-PruebasBD.ps1                                   # Oracle Cloud, A–E e I
.EXAMPLE
  pwsh -File database\scripts\Ejecutar-PruebasBD.ps1 -Secreto ecoaccess-p3-pruebas -IncluirEscritura   # esquema aislado, A–F e I
#>
param(
    [string]$Secreto = 'ecoaccess-p3-framework',
    [switch]$IncluirEscritura,
    [string]$Salida
)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
. (Join-Path $PSScriptRoot 'ComunOracle.ps1')
$conexion = Get-EcoConexion -Secreto $Secreto
if ($IncluirEscritura -and -not $conexion.Local) {
    throw 'La prueba F escribe datos (y los deshace con ROLLBACK). Solo se permite en un esquema aislado de localhost, nunca en Oracle Cloud.'
}
$fecha = Get-Date
if (-not $Salida) {
    $Salida = Join-Path $raiz ("docs\database\evidencias\pruebas_{0}_{1}.md" -f $conexion.Usuario.ToLower(), $fecha.ToString('yyyyMMdd_HHmm'))
}
New-Item -ItemType Directory -Force (Split-Path $Salida) | Out-Null

$pruebas = [ordered]@{
    'A' = @('Conexión', 'database\tests\A_conexion.sql')
    'B' = @('Estructura', 'database\tests\B_estructura.sql')
    'C' = @('Compilación', 'database\tests\C_compilacion.sql')
    'D' = @('Consultas a las vistas', 'database\tests\D_consultas_vistas.sql')
    'E' = @('Lógica PL/SQL sin escrituras', 'database\tests\E_plsql_sin_escritura.sql')
}
if ($IncluirEscritura) { $pruebas['F'] = @('Flujo de depósito y ecopuntos (ROLLBACK)', 'database\05_pruebas\V027_pruebas_capa_academica.sql') }

$filas = New-Object Collections.Generic.List[object]
$detalle = New-Object Text.StringBuilder
foreach ($id in $pruebas.Keys) {
    $nombre, $archivo = $pruebas[$id]
    $sql = Get-Content (Join-Path $raiz $archivo) -Raw -Encoding UTF8
    $r = Invoke-EcoSqlPlus -Conexion $conexion -Sql $sql
    $texto = $r.Texto.Trim()
    $m = [regex]::Match($texto, 'RESULTADO \w+:\s*(\d+) correctas, (\d+) fallidas(?:, (\d+) omitidas)?')
    $errores = [regex]::Matches($texto, '(ORA|PLS|SP2)-\d{4,5}[^\r\n]*') | Where-Object { $_.Value -notmatch 'ORA-?20\d{3}' } | ForEach-Object { $_.Value } | Select-Object -Unique
    if (-not $m.Success) { $estado = 'FALLIDA'; $resumen = 'no terminó (ver detalle)' }
    else {
        $bien = [int]$m.Groups[1].Value; $mal = [int]$m.Groups[2].Value; $omit = if ($m.Groups[3].Success) { [int]$m.Groups[3].Value } else { 0 }
        $estado = if ($mal -eq 0 -and -not $errores) { 'APROBADA' } else { 'FALLIDA' }
        $resumen = "$bien correctas, $mal fallidas" + $(if ($omit) { ", $omit omitidas" } else { '' })
    }
    $filas.Add([pscustomobject]@{ Id = $id; Nombre = $nombre; Estado = $estado; Resumen = $resumen; Archivo = $archivo })
    Write-Host ("{0} {1,-45} {2,-9} {3}" -f $id, $nombre, $estado, $resumen) -ForegroundColor $(if ($estado -eq 'APROBADA') { 'Green' } else { 'Red' })
    [void]$detalle.Append("### $id · $nombre`n`nArchivo: ``$($archivo -replace '\\', '/')```n`n``````text`n$texto`n```````n`n")
}

# I. Seguridad: nada secreto versionado.
Push-Location $raiz
try {
    $versionados = git ls-files 2>$null
    $sospechosos = @($versionados | Where-Object { $_ -match '(?i)(cwallet\.sso|ewallet\.(p12|pem)|keystore\.jks|truststore\.jks|Wallet_.*\.zip|(^|/)\.env$|credenciales_locales\.env|secrets\.json$)' })
    $conClave = @($versionados | Where-Object { $_ -notmatch '\.(png|jpg|jpeg|gif|pdf|docx|xlsx|pptx|zip|dll|exe|pbix|ico|ttf|woff2?)$' -and (Test-Path $_) } |
        Where-Object { [IO.File]::ReadAllText((Join-Path $raiz $_)).Contains($conexion.Clave) })
    $ignorados = @('x/cwallet.sso', 'x/ewallet.p12', 'x/Wallet_EcoAccess.zip', 'x/.env', 'x/secrets.json' | Where-Object { git check-ignore -q $_ 2>$null; $LASTEXITCODE -ne 0 })
} finally { Pop-Location }
$okI = ($sospechosos.Count -eq 0) -and ($conClave.Count -eq 0) -and ($ignorados.Count -eq 0)
$filas.Add([pscustomobject]@{ Id = 'I'; Nombre = 'Seguridad del repositorio'; Estado = $(if ($okI) { 'APROBADA' } else { 'FALLIDA' })
    Resumen = "archivos secretos versionados: $($sospechosos.Count); archivos que contienen la contraseña de esta conexión: $($conClave.Count); patrones de wallet sin ignorar: $($ignorados.Count)"; Archivo = '.gitignore' })
Write-Host ("I {0,-45} {1,-9} {2}" -f 'Seguridad del repositorio', $filas[-1].Estado, $filas[-1].Resumen) -ForegroundColor $(if ($okI) { 'Green' } else { 'Red' })
if ($sospechosos) { [void]$detalle.Append("### I · Archivos sospechosos`n`n" + (($sospechosos | ForEach-Object { "- ``$_``" }) -join "`n") + "`n`n") }
$conexion.Clave = $null

$destino = if ($conexion.TnsAdmin) { "Oracle Cloud (alias $($conexion.Origen))" } else { "Oracle local ($($conexion.Origen))" }
$md = New-Object Text.StringBuilder
[void]$md.Append("# Evidencia de pruebas de base de datos · $($conexion.Usuario)`n`n")
[void]$md.Append("- Fecha: $($fecha.ToString('yyyy-MM-dd HH:mm')) (hora del equipo)`n- Base: $destino`n- Esquema: ``$($conexion.Usuario)```n")
[void]$md.Append("- Generado por ``database/scripts/Ejecutar-PruebasBD.ps1``$(if ($IncluirEscritura) { ' -IncluirEscritura' })`n- La contraseña no aparece en este archivo: se envía a SQL*Plus por la entrada estándar.`n`n")
[void]$md.Append("| Prueba | Resultado | Evidencia | Observaciones |`n|---|---|---|---|`n")
foreach ($f in $filas) { [void]$md.Append("| $($f.Id) · $($f.Nombre) | **$($f.Estado)** | ``$($f.Archivo -replace '\\', '/')`` | $($f.Resumen) |`n") }
if (-not $IncluirEscritura) { [void]$md.Append("| F · Flujo de depósito y ecopuntos | **NO EJECUTADA** | ``database/05_pruebas/V027_pruebas_capa_academica.sql`` | escribe datos: solo en el esquema aislado (``-Secreto ecoaccess-p3-pruebas -IncluirEscritura``) |`n") }
[void]$md.Append("`n## Detalle`n`n" + $detalle.ToString())
[IO.File]::WriteAllText($Salida, ($md.ToString() -replace "`r", ''), (New-Object Text.UTF8Encoding $false))
Write-Host "Evidencia: $Salida"
if ($filas | Where-Object Estado -ne 'APROBADA') { exit 1 }
