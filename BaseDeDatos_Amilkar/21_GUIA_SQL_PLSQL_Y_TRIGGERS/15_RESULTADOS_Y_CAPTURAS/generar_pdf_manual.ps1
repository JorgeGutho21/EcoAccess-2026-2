<#
  Genera los PDF de la carpeta 21 (A4 horizontal, Times New Roman, negro sobre blanco):
    - 00_MANUAL_COMPLETO_SQL_PLSQL.pdf: 00_LEEME_PRIMERO + los LEEME de 01 a 16, la guía de la carpeta 12
      y, al final de cada tema, la salida REAL de Oracle si existe en 15_RESULTADOS_Y_CAPTURAS.
    - 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS/GUIA_USUARIOS_Y_RESIDUOS.pdf
    - 16_GUIA_ERRORES_FRECUENTES/LEEME.pdf (guía de errores para imprimir aparte)
  Markdown -> HTML (ConvertFrom-Markdown, PowerShell 7) -> PDF con Microsoft Edge sin ventana.
  Uso: pwsh -File 21_GUIA_SQL_PLSQL_Y_TRIGGERS\15_RESULTADOS_Y_CAPTURAS\generar_pdf_manual.ps1
#>
$ErrorActionPreference = 'Stop'
$base = Split-Path -Parent $PSScriptRoot
$res = $PSScriptRoot
$edge = @('C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe', 'C:\Program Files\Microsoft\Edge\Application\msedge.exe') | Where-Object { Test-Path $_ } | Select-Object -First 1
$css = @'
@page { size: 297mm 210mm; margin: 15mm 17mm }
body { font-family: 'Times New Roman', Times, serif; font-size: 12pt; color: #000; background: #fff; line-height: 1.38 }
h1 { font-size: 18pt; margin: 0 0 8pt } h2 { font-size: 14pt; margin: 14pt 0 6pt; break-after: avoid } h3 { font-size: 12.5pt; break-after: avoid }
table { border-collapse: collapse; width: 100%; font-size: 10.5pt; margin: 6pt 0 } tr { break-inside: avoid }
th, td { border: 0.6pt solid #000; padding: 3pt 5pt; text-align: left; vertical-align: top; overflow-wrap: anywhere }
code { font-family: Consolas, monospace; font-size: 10pt } pre { border: 0.6pt solid #000; padding: 6pt; white-space: pre-wrap; font-size: 9pt; line-height: 1.2 }
blockquote { border-left: 2pt solid #000; margin: 6pt 0; padding: 2pt 8pt }
img { display: block; max-width: 100%; max-height: 135mm; margin: 6pt auto; border: 0.6pt solid #000; break-inside: avoid }
.salto { break-before: page } .portada { text-align: center; padding-top: 45mm } .portada h1 { font-size: 24pt }
'@
function Pdf([string]$carpeta, [string]$html, [string]$titulo, [string]$pdf) {
  $doc = "<!doctype html><html lang='es'><head><meta charset='utf-8'><title>$titulo</title><style>$css</style></head><body>$html</body></html>"
  $tmp = Join-Path $carpeta ('_tmp_' + [IO.Path]::GetFileNameWithoutExtension($pdf) + '.html')
  [IO.File]::WriteAllText($tmp, $doc, (New-Object Text.UTF8Encoding $false))
  if (Test-Path $pdf) { Remove-Item $pdf -Force }
  $null = & $edge --headless=new --disable-gpu --no-pdf-header-footer --allow-file-access-from-files --virtual-time-budget=20000 "--print-to-pdf=$pdf" ([Uri]$tmp).AbsoluteUri 2>&1
  $t0 = Get-Date; while (-not (Test-Path $pdf) -and ((Get-Date) - $t0).TotalSeconds -lt 90) { Start-Sleep -Milliseconds 300 }
  Start-Sleep -Milliseconds 500; Remove-Item $tmp -Force
  "$([IO.Path]::GetFileName($pdf)) ($([int]((Get-Item $pdf).Length/1KB)) KB)"
}
function Html($md) { (ConvertFrom-Markdown -Path $md).Html }
function Rebase($h, $sub) {
  [regex]::Replace($h, 'src="([^"]+)"', { param($m) $u = $m.Groups[1].Value; if ($u -match '^(https?:|data:)') { $m.Value } else { 'src="' + $sub + '/' + $u + '"' } })
}
function Esc($s) { [Net.WebUtility]::HtmlEncode($s) }

# Salida real de cada tema (si existe)
$salidas = @{
  '01' = 'salida_01_primeros_comandos.txt'; '02' = 'salida_02_select_basico.txt'; '03' = 'salida_03_where_join_group_by.txt'
  '04' = 'salida_04_bloques_y_variables.txt'; '05' = 'salida_05_cursores.txt'; '06' = 'salida_06_procedimientos.txt'
  '07' = 'salida_07_funciones.txt'; '08' = 'salida_08_paquetes.txt'; '09' = 'salida_09_triggers_consulta.txt'
  '10' = 'salida_10_consultar_practicas.txt'; '11' = 'salida_11_secuencias_y_pk.txt'; '13' = 'salida_13_ecopuntos_y_auditoria.txt'
  '14' = 'salida_14_soluciones.txt'
}
$portada = @"
<div class='portada'><h1>Manual completo de SQL, PL/SQL y disparadores sobre EcoAccess</h1>
<p>Universidad Popular del Cesar · Base de Datos · Profesor Amílkar</p>
<p>Integrantes: Jorge Alfonso Gutiérrez Thomas, Heyder Hedrey Canchila de Ángel y Saray Marcela Cadena Palomino</p>
<p>Esquema ECOACCESS_BD · localhost:1521/FREEPDB1 · 19 tablas, 173 atributos, 38 FK, 4 disparadores</p></div>
"@
$todo = $portada + "<section class='salto'>$(Html (Join-Path $base '00_LEEME_PRIMERO.md'))</section>"
foreach ($d in (Get-ChildItem $base -Directory | Where-Object Name -match '^\d\d_' | Sort-Object Name)) {
  $n = $d.Name.Substring(0, 2)
  $md = if ($n -eq '12') { Join-Path $d.FullName 'GUIA_USUARIOS_Y_RESIDUOS.md' } else { Join-Path $d.FullName 'LEEME.md' }
  if (-not (Test-Path $md)) { continue }
  $h = Rebase (Html $md) $d.Name
  if ($salidas.ContainsKey($n)) {
    $f = Join-Path $res $salidas[$n]
    if (Test-Path $f) {
      $txt = (([IO.File]::ReadAllText($f) -split "`r?`n") | ForEach-Object { ($_ -replace ' {4,}', '  ') -replace '-{20,}', ('-' * 20) }) -join "`n"
      if ($txt.Length -gt 12000) { $txt = $txt.Substring(0, 12000) + "`n… (continúa en 15_RESULTADOS_Y_CAPTURAS/$($salidas[$n]))" }
      $h += "<h2>Salida real en Oracle ($($salidas[$n]))</h2><pre>$(Esc $txt)</pre>"
    }
  }
  $todo += "<section class='salto'>$h</section>"
}
Pdf $base $todo '00_MANUAL_COMPLETO_SQL_PLSQL' (Join-Path $base '00_MANUAL_COMPLETO_SQL_PLSQL.pdf')
$g = Join-Path $base '12_USUARIOS_Y_RESIDUOS_DEPOSITADOS\GUIA_USUARIOS_Y_RESIDUOS.md'
Pdf (Split-Path $g) (Html $g) 'GUIA_USUARIOS_Y_RESIDUOS' ([IO.Path]::ChangeExtension($g, '.pdf'))
$e = Join-Path $base '16_GUIA_ERRORES_FRECUENTES\LEEME.md'
Pdf (Split-Path $e) (Html $e) 'GUIA_ERRORES_FRECUENTES' (Join-Path (Split-Path $e) 'GUIA_ERRORES_FRECUENTES.pdf')
