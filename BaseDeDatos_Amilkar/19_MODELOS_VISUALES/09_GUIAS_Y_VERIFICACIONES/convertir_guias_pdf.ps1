<#
  Convierte las guías Markdown de 19_MODELOS_VISUALES a PDF (A4 horizontal), con sus capturas.
  Markdown -> HTML con ConvertFrom-Markdown (PowerShell 7) -> PDF con Microsoft Edge sin ventana.
  Uso: pwsh -File 19_MODELOS_VISUALES\09_GUIAS_Y_VERIFICACIONES\convertir_guias_pdf.ps1
#>
$ErrorActionPreference = 'Stop'
$vis = Split-Path -Parent $PSScriptRoot
$edge = @('C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe', 'C:\Program Files\Microsoft\Edge\Application\msedge.exe') | Where-Object { Test-Path $_ } | Select-Object -First 1
$guias = @(
  '05_DATA_MODELER\04_ABRIR_DMD\GUIA_ABRIR_DMD_EN_SQL_DEVELOPER.md',
  '05_DATA_MODELER\04_ABRIR_DMD\GUIA_ABRIR_DMD_EN_DATA_MODELER.md',
  '07_DDL_Y_CODIGO\COMO_VER_EL_DDL_Y_GENERAR_UN_MODELO.md',
  '00_LEEME_PRIMERO.md')
$css = @'
@page { size: 297mm 210mm; margin: 18mm 20mm }
body { font-family: 'Times New Roman', Times, serif; font-size: 12pt; color: #000; background: #fff; line-height: 1.4 }
h1 { font-size: 18pt; margin: 0 0 8pt } h2 { font-size: 14pt; margin: 16pt 0 6pt; break-after: avoid } h3 { font-size: 12.5pt }
table { border-collapse: collapse; width: 100%; font-size: 10.5pt; margin: 6pt 0; break-inside: avoid }
th, td { border: 0.6pt solid #000; padding: 3pt 5pt; text-align: left; vertical-align: top; overflow-wrap: anywhere }
code { font-family: Consolas, monospace; font-size: 10pt } pre { border: 0.6pt solid #000; padding: 6pt; white-space: pre-wrap; font-size: 10pt }
img { display: block; max-width: 100%; max-height: 150mm; margin: 6pt auto; border: 0.6pt solid #000; break-inside: avoid }
p:has(> img) { break-inside: avoid }
'@
foreach ($g in $guias) {
  $md = Join-Path $vis $g
  $html = (ConvertFrom-Markdown -Path $md).Html
  $doc = "<!doctype html><html lang='es'><head><meta charset='utf-8'><title>$([IO.Path]::GetFileNameWithoutExtension($md))</title><style>$css</style></head><body>$html</body></html>"
  # El HTML temporal va en la MISMA carpeta que la guía, para que las rutas relativas de las capturas funcionen.
  $tmp = [IO.Path]::ChangeExtension($md, '.tmp.html'); $pdf = [IO.Path]::ChangeExtension($md, '.pdf')
  [IO.File]::WriteAllText($tmp, $doc, (New-Object Text.UTF8Encoding $false))
  if (Test-Path $pdf) { Remove-Item $pdf -Force }
  $null = & $edge --headless=new --disable-gpu --no-pdf-header-footer --allow-file-access-from-files --virtual-time-budget=15000 "--print-to-pdf=$pdf" ([Uri]$tmp).AbsoluteUri 2>&1
  $t0 = Get-Date; while (-not (Test-Path $pdf) -and ((Get-Date) - $t0).TotalSeconds -lt 60) { Start-Sleep -Milliseconds 300 }
  Remove-Item $tmp -Force
  "$g -> $([IO.Path]::GetFileName($pdf)) ($([int]((Get-Item $pdf).Length/1KB)) KB)"
}
