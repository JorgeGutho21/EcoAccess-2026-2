<#
  Genera y VERIFICA el banco de ejercicios:
   1) Lee ejercicios_fuente.txt (bloques «### esquema|tema|título|objetivo|explicación» … «@@@»).
   2) Escribe banco_ejercicios_BD.sql y banco_ejercicios_PRACTICAS.sql (numerados E001…).
   3) Los ejecuta en Oracle (run_sql.ps1) y guarda la salida REAL de cada ejercicio.
   4) Marca cada ejercicio: OK, OK (error esperado) o FALLA.
   5) Escribe resultados_ejercicios.md y ejercicios.html (para la guía).
  Uso: pwsh -File generar_banco.ps1 -Ejecutor ..\18_ENTREGA_Y_RESPALDOS\herramientas\ejecutar_sql.ps1  (lo llama INSTALAR_Y_PROBAR.ps1)
#>
param([Parameter(Mandatory)] [string]$Ejecutor)
$ErrorActionPreference = 'Stop'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
$utf8 = New-Object Text.UTF8Encoding $false
# Fuentes: ejercicios_fuente.txt (primera tanda) y ejercicios_fuente_2.txt (ampliación). Se ordenan por número
# de tema conservando el orden de cada fuente (orden estable) y luego se numeran E001…
$texto = [IO.File]::ReadAllText("$aqui\ejercicios_fuente.txt", $utf8)
if (Test-Path "$aqui\ejercicios_fuente_2.txt") { $texto += "`n@@@`n" + [IO.File]::ReadAllText("$aqui\ejercicios_fuente_2.txt", $utf8) }
$bloques = $texto -split '(?m)^@@@\s*$' | Where-Object { $_.Trim() -ne '' }
$crudos = New-Object Collections.Generic.List[object]; $orden = 0
foreach ($b in $bloques) {
  $lineas = $b.Trim() -split "`r?`n"
  $cab = $lineas[0] -replace '^###\s*', '' -split '\|'
  $orden++
  $crudos.Add([pscustomobject]@{ Id = ''; Orden = $orden; NumTema = [int]($cab[1] -replace '^(\d+)\..*', '$1'); Esquema = $cab[0]; Tema = $cab[1]; Titulo = $cab[2]; Objetivo = $cab[3];
    Explicacion = $cab[4]; Sql = ($lineas[1..($lineas.Count - 1)] -join "`n").TrimEnd(); Esperado = ($cab[2] + $cab[3] + $cab[4]) -match 'ERROR ESPERADO|ORA-\d{5}' })
}
$ej = New-Object Collections.Generic.List[object]; $n = 0
foreach ($e in ($crudos | Sort-Object NumTema, Orden)) { $n++; $e.Id = 'E{0:000}' -f $n; $ej.Add($e) }
$salidas = @{}
foreach ($esq in 'BD', 'PRAC') {
  $usuario = if ($esq -eq 'BD') { 'ECOACCESS_BD' } else { 'ECOACCESS_PRACTICAS' }
  $nombre = if ($esq -eq 'BD') { 'banco_ejercicios_BD.sql' } else { 'banco_ejercicios_PRACTICAS.sql' }
  $sb = New-Object Text.StringBuilder
  [void]$sb.AppendLine("-- Banco de ejercicios EcoAccess BD — conexión: $usuario")
  [void]$sb.AppendLine('-- GENERADO por generar_banco.ps1 desde ejercicios_fuente.txt. Cada ejercicio se puede ejecutar')
  [void]$sb.AppendLine('-- solo: selecciónalo y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.')
  [void]$sb.AppendLine('SET SERVEROUTPUT ON SIZE UNLIMITED'); [void]$sb.AppendLine('SET LINESIZE 200'); [void]$sb.AppendLine('SET PAGESIZE 50'); [void]$sb.AppendLine('SET FEEDBACK ON')
  foreach ($e in ($ej | Where-Object Esquema -eq $esq)) {
    [void]$sb.AppendLine(''); [void]$sb.AppendLine("PROMPT <<<$($e.Id)>>>")
    [void]$sb.AppendLine("-- $($e.Id) · $($e.Tema) · $($e.Titulo)"); [void]$sb.AppendLine("-- Objetivo: $($e.Objetivo)"); [void]$sb.AppendLine("-- Explicación: $($e.Explicacion)")
    [void]$sb.AppendLine($e.Sql); [void]$sb.AppendLine("PROMPT <<<FIN>>>")
  }
  [void]$sb.AppendLine('ROLLBACK;')
  [IO.File]::WriteAllText("$aqui\$nombre", $sb.ToString(), $utf8)
  $sal = & $Ejecutor -Usuario $usuario -Scripts "$aqui\$nombre"
  foreach ($m in [regex]::Matches($sal, '<<<(E\d{3})>>>(.*?)<<<FIN>>>', 'Singleline')) { $salidas[$m.Groups[1].Value] = $m.Groups[2].Value.Trim("`r`n ") }
}
$ok = 0; $esp = 0; $fal = 0
$md = New-Object Text.StringBuilder; $html = New-Object Text.StringBuilder
[void]$md.AppendLine('# Resultados del banco de ejercicios (ejecución real en Oracle)'); [void]$md.AppendLine('')
[void]$md.AppendLine("Ejecutado el $(Get-Date -Format 'yyyy-MM-dd HH:mm') en Oracle AI Database 26ai Free (localhost:1521/FREEPDB1). Datos SINTÉTICOS."); [void]$md.AppendLine('')
[void]$md.AppendLine('| Id | Tema | Ejercicio | Esquema | Estado |'); [void]$md.AppendLine('|---|---|---|---|---|')
$temaActual = ''
foreach ($e in $ej) {
  $s = if ($salidas.ContainsKey($e.Id)) { $salidas[$e.Id] } else { '(sin salida capturada)' }
  $hayError = $s -match 'ERROR en l[ií]nea|SP2-\d|PLS-\d'
  $estado = if (-not $salidas.ContainsKey($e.Id)) { 'FALLA'; $fal++ } elseif ($hayError -and $e.Esperado) { 'OK (error esperado)'; $esp++ } elseif ($hayError) { 'FALLA'; $fal++ } else { 'OK'; $ok++ }
  [void]$md.AppendLine("| $($e.Id) | $($e.Tema) | $($e.Titulo) | $($e.Esquema) | $estado |")
  if ($e.Tema -ne $temaActual) { [void]$html.AppendLine("<h3>$([Net.WebUtility]::HtmlEncode($e.Tema))</h3>"); $temaActual = $e.Tema }
  $sCorta = ($s -split "`r?`n" | Where-Object { $_.Trim() -ne '' } | Select-Object -First 18) -join "`n"
  [void]$html.AppendLine("<div class='ej'><p class='ejt'><b>$($e.Id) · $([Net.WebUtility]::HtmlEncode($e.Titulo))</b> <span class='est'>[$estado · conexión $(if ($e.Esquema -eq 'BD') {'ECOACCESS_BD'} else {'ECOACCESS_PRACTICAS'})]</span></p>")
  [void]$html.AppendLine("<p><b>Objetivo:</b> $([Net.WebUtility]::HtmlEncode($e.Objetivo))<br/><b>Idea clave:</b> $([Net.WebUtility]::HtmlEncode($e.Explicacion))</p>")
  [void]$html.AppendLine("<pre class='sql'>$([Net.WebUtility]::HtmlEncode($e.Sql))</pre><p class='res'>Resultado obtenido al ejecutarlo:</p><pre class='out'>$([Net.WebUtility]::HtmlEncode($sCorta))</pre></div>")
}
[void]$md.Insert(0, "")
$resumen = "TOTAL: $($ej.Count) ejercicios · OK: $ok · OK con error esperado: $esp · FALLAN: $fal"
[void]$md.AppendLine(''); [void]$md.AppendLine("**$resumen**")
[IO.File]::WriteAllText("$aqui\resultados_ejercicios.md", $md.ToString(), $utf8)
[IO.File]::WriteAllText("$aqui\ejercicios.html", $html.ToString(), $utf8)
$todo = ($salidas.GetEnumerator() | Sort-Object Key | ForEach-Object { "===== $($_.Key) =====`n$($_.Value)" }) -join "`n`n"
[IO.File]::WriteAllText("$aqui\..\16_EVIDENCIAS_Y_PRUEBAS\salidas\17_banco_ejercicios.txt", $todo, $utf8)
$resumen
$ej | Where-Object { $salidas.ContainsKey($_.Id) -and ($salidas[$_.Id] -match 'ERROR en l[ií]nea|SP2-\d|PLS-\d') -and -not $_.Esperado } | ForEach-Object { "FALLA $($_.Id) $($_.Titulo): " + (($salidas[$_.Id] -split "`n" | Select-String 'ORA-|SP2-|PLS-' | Select-Object -First 2) -join ' / ') }
