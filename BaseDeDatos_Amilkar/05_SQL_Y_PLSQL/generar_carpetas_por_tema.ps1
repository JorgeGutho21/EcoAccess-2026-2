<#
  Reparte el banco verificado de 309 ejercicios (08_CONSULTAS_Y_EJERCICIOS) en carpetas por tema.
  Para cada tema escribe:
    ejercicios.sql   el código, con el objetivo y la explicación de cada ejercicio como comentario
    RESULTADOS.md    el estado de la última ejecución real y la salida de Oracle de cada ejercicio
  No inventa resultados: copia la salida guardada en 16_EVIDENCIAS_Y_PRUEBAS/salidas/17_banco_ejercicios.txt
  y el estado de 08_CONSULTAS_Y_EJERCICIOS/resultados_ejercicios.md (los genera generar_banco.ps1).
  Uso, desde la raíz: pwsh -File 05_SQL_Y_PLSQL\generar_carpetas_por_tema.ps1
#>
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$utf8 = New-Object Text.UTF8Encoding $false
$banco = Join-Path $raiz '08_CONSULTAS_Y_EJERCICIOS'

# 1. Ejercicios (mismo orden y numeración que generar_banco.ps1)
$texto = [IO.File]::ReadAllText("$banco\ejercicios_fuente.txt", $utf8) + "`n@@@`n" + [IO.File]::ReadAllText("$banco\ejercicios_fuente_2.txt", $utf8)
$orden = 0
$crudos = foreach ($b in ($texto -split '(?m)^@@@\s*$' | Where-Object { $_.Trim() })) {
  $l = $b.Trim() -split "`r?`n"; $c = $l[0] -replace '^###\s*', '' -split '\|'; $orden++
  [pscustomobject]@{ Orden = $orden; NumTema = [int]($c[1] -replace '^(\d+)\..*', '$1'); Esquema = $c[0]; Tema = $c[1]; Titulo = $c[2]; Objetivo = $c[3]; Explicacion = $c[4]; Sql = ($l[1..($l.Count - 1)] -join "`n").TrimEnd() }
}
$n = 0; $ej = foreach ($e in ($crudos | Sort-Object NumTema, Orden)) { $n++; $e | Add-Member Id ('E{0:000}' -f $n) -PassThru }
if ($n -ne 309) { throw "Se esperaban 309 ejercicios y hay $n" }

# 2. Salidas y estados reales
$sal = @{}
$txt = [IO.File]::ReadAllText((Join-Path $raiz '16_EVIDENCIAS_Y_PRUEBAS\salidas\17_banco_ejercicios.txt'), $utf8)
foreach ($m in [regex]::Matches($txt, '(?ms)^===== (E\d{3}) =====\r?\n(.*?)(?=^===== E\d{3} =====|\z)')) { $sal[$m.Groups[1].Value] = $m.Groups[2].Value.TrimEnd() }
$est = @{}
foreach ($l in [IO.File]::ReadAllLines("$banco\resultados_ejercicios.md", $utf8)) { if ($l -match '^\| (E\d{3}) \|.*\| ([^|]+) \|$') { $est[$Matches[1]] = $Matches[2].Trim() } }

# 3. Temas -> carpetas (títulos que separan PL/SQL básico y subprogramas)
function Carpeta($e) {
  switch ($e.NumTema) {
    { $_ -in 1, 5, 11, 13 } { return '01_FUNDAMENTOS_SQL' }
    { $_ -in 2, 3, 4 }      { return '02_SELECT_WHERE_ORDER_BY' }
    7                       { return '03_JOIN' }
    { $_ -in 6, 10 }        { return '04_GROUP_BY_HAVING_Y_ANALITICAS' }
    { $_ -in 8, 9 }         { return '05_SUBCONSULTAS_Y_CONJUNTOS' }
    14 { if ("$($e.Titulo) $($e.Explicacion)" -match '%TYPE|%ROWTYPE|IF|CASE|variable|constante|ELSIF') { return '07_VARIABLES_Y_CONDICIONALES' } else { return '06_BLOQUES_ANONIMOS_Y_CICLOS' } }
    15                      { return '08_CURSORES' }
    17 {
      $t = "$($e.Titulo) $($e.Explicacion) $($e.Sql)"
      if ($t -match 'REFCURSOR') { return '11_SYS_REFCURSOR' }
      if ($t -match '(?i)paquete|PACKAGE|pkg_') { return '12_PAQUETES' }
      if ($t -match '(?i)funci[oó]n|FUNCTION|RETURN') { return '10_FUNCIONES' }
      return '09_PROCEDIMIENTOS' }
    12                      { return '13_TRANSACCIONES_Y_DML' }
    16                      { return '14_EXCEPCIONES' }
    18                      { return '15_DISPARADORES_EJERCICIOS' }
    19                      { return '16_ESQUEMA_HR_PRACTICAS' }
  }
}
$porCarpeta = $ej | Group-Object { Carpeta $_ } | Sort-Object Name
$indice = New-Object Text.StringBuilder
foreach ($g in $porCarpeta) {
  $dir = Join-Path $PSScriptRoot $g.Name; New-Item -ItemType Directory -Force $dir | Out-Null
  $usuario = if ($g.Group[0].Esquema -eq 'PRAC') { 'ECOACCESS_PRACTICAS' } else { 'ECOACCESS_BD' }
  $sb = New-Object Text.StringBuilder; $md = New-Object Text.StringBuilder
  [void]$sb.AppendLine("-- $($g.Name) — $($g.Count) ejercicios del banco verificado (conexión: $usuario)")
  [void]$sb.AppendLine('-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.')
  [void]$sb.AppendLine('-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.')
  [void]$sb.AppendLine('SET SERVEROUTPUT ON SIZE UNLIMITED')
  [void]$md.AppendLine("# $($g.Name -replace '^\d+_', '' -replace '_', ' ')"); [void]$md.AppendLine('')
  [void]$md.AppendLine("Ejercicios: $($g.Count) · conexión: ``$usuario`` · temas del banco: $((($g.Group.Tema | Sort-Object -Unique) -join '; ')).")
  [void]$md.AppendLine(''); [void]$md.AppendLine('Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.'); [void]$md.AppendLine('')
  [void]$md.AppendLine('| Id | Ejercicio | Estado |'); [void]$md.AppendLine('|---|---|---|')
  foreach ($e in $g.Group) { [void]$md.AppendLine("| $($e.Id) | $($e.Titulo) | $($est[$e.Id]) |") }
  foreach ($e in $g.Group) {
    [void]$sb.AppendLine(''); [void]$sb.AppendLine("-- ===== $($e.Id) · $($e.Tema) · $($e.Titulo)"); [void]$sb.AppendLine("-- Objetivo: $($e.Objetivo)"); [void]$sb.AppendLine("-- Explicación: $($e.Explicacion)"); [void]$sb.AppendLine($e.Sql)
    [void]$md.AppendLine(''); [void]$md.AppendLine("## $($e.Id) · $($e.Titulo)"); [void]$md.AppendLine(''); [void]$md.AppendLine("**Objetivo:** $($e.Objetivo)  "); [void]$md.AppendLine("**Explicación:** $($e.Explicacion)  "); [void]$md.AppendLine("**Estado:** $($est[$e.Id])"); [void]$md.AppendLine('')
    [void]$md.AppendLine('```sql'); [void]$md.AppendLine($e.Sql); [void]$md.AppendLine('```'); [void]$md.AppendLine(''); [void]$md.AppendLine('Salida real:'); [void]$md.AppendLine('')
    $s = if ($sal.ContainsKey($e.Id)) { $sal[$e.Id] } else { '(sin salida guardada)' }
    $ls = $s -split "`r?`n"; if ($ls.Count -gt 40) { $s = (($ls[0..39]) -join "`n") + "`n… (salida recortada; completa en 16_EVIDENCIAS_Y_PRUEBAS/salidas/17_banco_ejercicios.txt)" }
    [void]$md.AppendLine('```text'); [void]$md.AppendLine($s); [void]$md.AppendLine('```')
  }
  [IO.File]::WriteAllText("$dir\ejercicios.sql", $sb.ToString(), $utf8)
  [IO.File]::WriteAllText("$dir\RESULTADOS.md", $md.ToString(), $utf8)
  $ok = @($g.Group | Where-Object { $est[$_.Id] -like 'OK*' }).Count
  [void]$indice.AppendLine("| ``$($g.Name)`` | $($g.Count) | $ok | $((($g.Group.Tema | Sort-Object -Unique) -join '; ')) |")
}
$total = @($ej | Where-Object { $est[$_.Id] -like 'OK*' }).Count
"Carpetas: $(@($porCarpeta).Count) · ejercicios: $($ej.Count) · con estado OK: $total"
[IO.File]::WriteAllText((Join-Path $PSScriptRoot '_indice_temas.md'), $indice.ToString(), $utf8)
