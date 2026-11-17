<#
  Convierte los SVG de las carpetas de diagramas a PNG (alta resolución, escala 2x) y PDF (una sola página
  del tamaño exacto del diagrama, vectorial) usando Microsoft Edge en modo sin ventana.
  Uso: pwsh -File convertir_exportaciones.ps1 [-Filtro 'MR_*']
#>
param([string]$Filtro = '*')
$ErrorActionPreference = 'Stop'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
# Los diagramas están repartidos en 01_MER, 02_MEER, 03, 04 y 06; se recorre cada carpeta.
$carpetas = '01_MER', '02_MEER', '03_MODELO_RELACIONAL_GENERAL', '04_MODELO_RELACIONAL_POR_MODULOS', '06_FLUJO_DE_DATOS' | ForEach-Object { Join-Path (Split-Path $aqui) $_ }
$edge = 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
$tmp = Join-Path $env:TEMP 'eco_svg_conv'; New-Item -ItemType Directory -Force $tmp | Out-Null
foreach ($svg in Get-ChildItem $carpetas -Filter "$Filtro.svg") { $exp = $svg.DirectoryName
  $txt = [IO.File]::ReadAllText($svg.FullName)
  if ($txt -notmatch "width='(\d+(\.\d+)?)' height='(\d+(\.\d+)?)'") { Write-Warning "sin tamaño: $($svg.Name)"; continue }
  $w = [math]::Ceiling([double]$Matches[1]); $h = [math]::Ceiling([double]$Matches[3])
  $html = "<!doctype html><html><head><meta charset='utf-8'><style>@page{size:${w}px ${h}px;margin:0}html,body{margin:0;padding:0;background:#fff}svg{display:block}</style></head><body>$txt</body></html>"
  $hf = Join-Path $tmp ($svg.BaseName + '.html'); [IO.File]::WriteAllText($hf, $html, (New-Object Text.UTF8Encoding $false))
  $png = Join-Path $exp ($svg.BaseName + '.png'); $pdf = Join-Path $exp ($svg.BaseName + '.pdf')
  Remove-Item $png, $pdf -ErrorAction SilentlyContinue
  $perfil = Join-Path $tmp 'perfil'
  $url = "file:///$($hf -replace '\\','/')"
  Start-Process -FilePath $edge -Wait -WindowStyle Hidden -ArgumentList @('--headless=new', '--disable-gpu', '--hide-scrollbars', "--user-data-dir=`"$perfil-png`"", '--force-device-scale-factor=2', "--window-size=$w,$h", "--screenshot=`"$png`"", "`"$url`"")
  Start-Process -FilePath $edge -Wait -WindowStyle Hidden -ArgumentList @('--headless=new', '--disable-gpu', "--user-data-dir=`"$perfil-pdf`"", '--no-pdf-header-footer', "--print-to-pdf=`"$pdf`"", "`"$url`"")
  $i = 0; while (-not ((Test-Path $png) -and (Test-Path $pdf)) -and $i -lt 40) { Start-Sleep -Milliseconds 250; $i++ }
  '{0,-40} PNG {1,8:N0} B  PDF {2,8:N0} B  ({3}x{4})' -f $svg.BaseName, (Get-Item $png).Length, (Get-Item $pdf).Length, $w, $h
}
