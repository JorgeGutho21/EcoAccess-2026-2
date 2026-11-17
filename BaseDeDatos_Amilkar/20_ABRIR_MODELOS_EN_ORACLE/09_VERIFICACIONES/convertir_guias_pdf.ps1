<#
  Convierte las guías Markdown de 20_ABRIR_MODELOS_EN_ORACLE a PDF (A4 horizontal), con sus capturas,
  y arma 00_INDICE_PARA_ABRIR_MODELOS.pdf (LEEME + las tres guías + informe de pruebas en un solo PDF).
  Markdown -> HTML con ConvertFrom-Markdown (PowerShell 7) -> PDF con Microsoft Edge sin ventana.
  Uso: pwsh -File 20_ABRIR_MODELOS_EN_ORACLE\09_VERIFICACIONES\convertir_guias_pdf.ps1
#>
$ErrorActionPreference = 'Stop'
$base = Split-Path -Parent $PSScriptRoot
$edge = @('C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe', 'C:\Program Files\Microsoft\Edge\Application\msedge.exe') | Where-Object { Test-Path $_ } | Select-Object -First 1
$css = @'
@page { size: 297mm 210mm; margin: 16mm 18mm }
body { font-family: 'Times New Roman', Times, serif; font-size: 12pt; color: #000; background: #fff; line-height: 1.4 }
h1 { font-size: 18pt; margin: 0 0 8pt } h2 { font-size: 14pt; margin: 16pt 0 6pt; break-after: avoid } h3 { font-size: 12.5pt; break-after: avoid }
table { border-collapse: collapse; width: 100%; font-size: 10.5pt; margin: 6pt 0 }
tr { break-inside: avoid }
th, td { border: 0.6pt solid #000; padding: 3pt 5pt; text-align: left; vertical-align: top; overflow-wrap: anywhere }
td img { max-height: 70mm }
code { font-family: Consolas, monospace; font-size: 10pt } pre { border: 0.6pt solid #000; padding: 6pt; white-space: pre-wrap; font-size: 10pt }
blockquote { border-left: 2pt solid #000; margin: 6pt 0; padding: 2pt 8pt }
img { display: block; max-width: 100%; max-height: 140mm; margin: 6pt auto; border: 0.6pt solid #000; break-inside: avoid }
.salto { break-before: page }
'@
function Convertir([string]$carpeta, [string]$html, [string]$titulo, [string]$pdf) {
  $doc = "<!doctype html><html lang='es'><head><meta charset='utf-8'><title>$titulo</title><style>$css</style></head><body>$html</body></html>"
  # El HTML temporal va en la carpeta indicada para que las rutas relativas de las capturas funcionen.
  $tmp = Join-Path $carpeta ('_tmp_' + [IO.Path]::GetFileNameWithoutExtension($pdf) + '.html')
  [IO.File]::WriteAllText($tmp, $doc, (New-Object Text.UTF8Encoding $false))
  if (Test-Path $pdf) { Remove-Item $pdf -Force }
  $null = & $edge --headless=new --disable-gpu --no-pdf-header-footer --allow-file-access-from-files --virtual-time-budget=20000 "--print-to-pdf=$pdf" ([Uri]$tmp).AbsoluteUri 2>&1
  $t0 = Get-Date; while (-not (Test-Path $pdf) -and ((Get-Date) - $t0).TotalSeconds -lt 90) { Start-Sleep -Milliseconds 300 }
  Start-Sleep -Milliseconds 500; Remove-Item $tmp -Force
  "$([IO.Path]::GetFileName($pdf)) ($([int]((Get-Item $pdf).Length/1KB)) KB)"
}
# Los enlaces a .md se dejan como texto de ruta; en el PDF no son navegables.
function Html($md) { (ConvertFrom-Markdown -Path $md).Html }

$guias = '05_GUIA_SQL_DEVELOPER\GUIA_ABRIR_MODELOS.md', '06_GUIA_DATA_MODELER\GUIA_ABRIR_MODELOS.md', '07_GUIA_IMPORTAR_DDL\GUIA_IMPORTACION_PASO_A_PASO.md'
foreach ($g in $guias) {
  $md = Join-Path $base $g
  Convertir (Split-Path $md) (Html $md) ([IO.Path]::GetFileNameWithoutExtension($md)) ([IO.Path]::ChangeExtension($md, '.pdf'))
}

# Índice único: las rutas de imagen de cada guía son relativas a su carpeta; se reescriben a la raíz de 20.
$partes = @('00_LEEME_PRIMERO.md') + $guias + '09_VERIFICACIONES\INFORME_DE_PRUEBAS.md'
$todo = ''
foreach ($p in $partes) {
  $md = Join-Path $base $p
  $h = Html $md
  $sub = Split-Path $p
  if ($sub) {
    $h = [regex]::Replace($h, 'src="([^"]+)"', { param($m) $u = $m.Groups[1].Value; if ($u -match '^(https?:|data:)') { $m.Value } else { 'src="' + ($sub.Replace('\', '/') + '/' + $u) + '"' } })
  }
  $todo += "<section class='$(if ($todo) { 'salto' })'>$h</section>"
}
Convertir $base $todo '00_INDICE_PARA_ABRIR_MODELOS' (Join-Path $base '00_INDICE_PARA_ABRIR_MODELOS.pdf')
