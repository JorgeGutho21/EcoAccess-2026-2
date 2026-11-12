<#
.SYNOPSIS
    Métricas por ruta de disposición a partir del informe de --evaluar-objetos: matriz de confusión (ruta esperada x ruta
    predicha), precisión, recall y F1 por ruta, y la lista de errores con objeto, predicción y clase correcta.
.DESCRIPTION
    La ruta esperada es la de la PRIMERA clave aceptada de cada muestra; la predicha, la de la clave que respondió Eco
    («(abstención)» si no hubo clave). Rutas.csv sale del código compilado (CatalogoObjetos + MatrizDecisionResiduos).
#>
param([Parameter(Mandatory)][string] $Informe, [Parameter(Mandatory)][string] $Manifiesto, [Parameter(Mandatory)][string] $Rutas, [string] $Titulo = 'Resultados', [string] $Salida)
$ErrorActionPreference = 'Stop'
$ruta = @{}; foreach ($r in Import-Csv $Rutas) { $ruta[$r.Clave] = $r.Ruta }
$texto = Get-Content $Informe -Raw
$json = $texto.Substring($texto.IndexOf('{'))
$inf = $json | ConvertFrom-Json
$muestras = @{}; foreach ($m in (Get-Content $Manifiesto -Raw | ConvertFrom-Json).Muestras) { $muestras[$m.Archivo] = $m }
$filas = foreach ($d in $inf.Detalle) {
    $m = $muestras[$d.Archivo]
    $esperada = $ruta[$m.Aceptadas[0]]
    $predicha = if ($d.Clave) { $ruta[$d.Clave] } else { '(abstención)' }
    # Correcta por ruta: cualquier clave aceptada que lleve a la misma ruta que la predicha.
    $rutasAceptadas = @($m.Aceptadas | ForEach-Object { $ruta[$_] } | Select-Object -Unique)
    [pscustomobject]@{ Archivo = $d.Archivo; Grupo = $m.Clase; Esperada = $esperada; Predicha = $predicha; ClaveOk = [bool]($m.Aceptadas -contains $d.Clave)
        RutaOk = $rutasAceptadas -contains $predicha; Clave = $d.Clave; Aceptadas = ($m.Aceptadas -join ','); Abriria = [bool]$d.Abriria; Admisible = [bool]$m.Admisible }
}
$n = @($filas).Count
$o = New-Object System.Text.StringBuilder
[void]$o.AppendLine("### $Titulo")
[void]$o.AppendLine('')
[void]$o.AppendLine("| Métrica | Valor |"); [void]$o.AppendLine('|---|---|')
[void]$o.AppendLine("| Muestras | $n |")
[void]$o.AppendLine(("| Clave exacta correcta | {0} ({1:P1}) |" -f @($filas | ? ClaveOk).Count, (@($filas | ? ClaveOk).Count / [math]::Max(1,$n))))
[void]$o.AppendLine(("| Ruta de disposición correcta | {0} ({1:P1}) |" -f @($filas | ? RutaOk).Count, (@($filas | ? RutaOk).Count / [math]::Max(1,$n))))
[void]$o.AppendLine("| Falsas admisiones (abriría sin deber) | $($inf.FalsasAdmisiones) |")
[void]$o.AppendLine("| Ofertas indebidas | $($inf.OfertasIndebidas) |")
[void]$o.AppendLine("| Abstenciones | $($inf.Abstenciones) |")
[void]$o.AppendLine("| Fallos técnicos | $($inf.FallosTecnicos) |")
[void]$o.AppendLine("| Latencia mediana / p95 | $($inf.MedianaMs) ms / $($inf.P95Ms) ms |")
$listaRutas = @($filas | ForEach-Object { $_.Esperada; $_.Predicha } | Select-Object -Unique | Sort-Object)
[void]$o.AppendLine(''); [void]$o.AppendLine('**Precisión, recall y F1 por ruta** (sobre la ruta exacta de la primera clave aceptada):'); [void]$o.AppendLine('')
[void]$o.AppendLine('| Ruta | VP | FP | FN | Precisión | Recall | F1 |'); [void]$o.AppendLine('|---|---:|---:|---:|---:|---:|---:|')
foreach ($r in $listaRutas) {
    $vp = @($filas | ? { $_.Esperada -eq $r -and $_.Predicha -eq $r }).Count
    $fp = @($filas | ? { $_.Esperada -ne $r -and $_.Predicha -eq $r }).Count
    $fn = @($filas | ? { $_.Esperada -eq $r -and $_.Predicha -ne $r }).Count
    $p = if ($vp + $fp) { $vp / ($vp + $fp) } else { 0 }; $rc = if ($vp + $fn) { $vp / ($vp + $fn) } else { 0 }
    $f1 = if ($p + $rc) { 2 * $p * $rc / ($p + $rc) } else { 0 }
    [void]$o.AppendLine(("| {0} | {1} | {2} | {3} | {4:N2} | {5:N2} | {6:N2} |" -f $r, $vp, $fp, $fn, $p, $rc, $f1))
}
[void]$o.AppendLine(''); [void]$o.AppendLine('**Matriz de confusión** (filas: ruta esperada; columnas: ruta predicha):'); [void]$o.AppendLine('')
[void]$o.AppendLine('| Esperada \ Predicha | ' + ($listaRutas -join ' | ') + ' |'); [void]$o.AppendLine('|---|' + (($listaRutas | % { '---:' }) -join '|') + '|')
foreach ($e in $listaRutas) {
    $celdas = foreach ($p in $listaRutas) { @($filas | ? { $_.Esperada -eq $e -and $_.Predicha -eq $p }).Count }
    if (($celdas | Measure-Object -Sum).Sum -gt 0) { [void]$o.AppendLine("| $e | " + ($celdas -join ' | ') + ' |') }
}
[void]$o.AppendLine(''); [void]$o.AppendLine('**Errores de ruta** (objeto, predicción, clase correcta):'); [void]$o.AppendLine('')
[void]$o.AppendLine('| Foto | Grupo | Predicción (clave → ruta) | Correcto | ¿Abriría? |'); [void]$o.AppendLine('|---|---|---|---|---|')
foreach ($f in ($filas | ? { -not $_.RutaOk } | Sort-Object Grupo, Archivo)) {
    [void]$o.AppendLine("| $($f.Archivo) | $($f.Grupo) | $(if ($f.Clave) { "``$($f.Clave)`` → $($f.Predicha)" } else { 'abstención' }) | ``$($f.Aceptadas)`` → $($f.Esperada) | $(if ($f.Abriria) { '**sí**' } else { 'no' }) |")
}
[void]$o.AppendLine(""); [void]$o.AppendLine("**Falsas admisiones** (Eco abriría sola sin deber):"); [void]$o.AppendLine("")
foreach ($f in ($filas | ? { $_.Abriria -and -not $_.Admisible })) { [void]$o.AppendLine(('- {0} ({1}): Eco dijo `{2}`; correcto `{3}`.' -f $f.Archivo, $f.Grupo, $f.Clave, $f.Aceptadas)) }
if (-not ($filas | ? { $_.Abriria -and -not $_.Admisible })) { [void]$o.AppendLine("- Ninguna.") }
if ($Salida) { Set-Content $Salida $o.ToString() -Encoding utf8 } else { $o.ToString() }
