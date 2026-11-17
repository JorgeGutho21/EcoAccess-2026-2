<#
  Comprueba que 00_ABRIR_DIAGRAMAS.html funciona sin Internet:
   1) todos sus enlaces (href) e imágenes (src) son relativos y existen;
   2) lo mismo en una COPIA de la carpeta en otra ubicación temporal;
   3) Edge abre la copia y guarda una imagen de la página.
  Uso: pwsh -File 19_MODELOS_VISUALES\09_GUIAS_Y_VERIFICACIONES\verificar_galeria.ps1
#>
$ErrorActionPreference = 'Stop'
$vis = Split-Path -Parent $PSScriptRoot
function Revisar([string]$carpeta) {
  $html = [IO.File]::ReadAllText((Join-Path $carpeta '00_ABRIR_DIAGRAMAS.html'))
  $refs = [regex]::Matches($html, '(?:href|src)=[''"]([^''"#]+)[''"]') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
  $absolutas = @($refs | Where-Object { $_ -match '^(https?:|file:|[A-Za-z]:\\|/)' })
  $rotas = @($refs | Where-Object { $_ -notmatch '^(https?:|file:)' } | Where-Object { -not (Test-Path -LiteralPath (Join-Path $carpeta ([Uri]::UnescapeDataString($_) -replace '/', '\'))) })
  [pscustomobject]@{ Carpeta = $carpeta; Enlaces = $refs.Count; Absolutas = $absolutas.Count; Rotas = $rotas.Count; ListaRotas = ($rotas -join ', ') }
}
$r1 = Revisar $vis
$copia = Join-Path ([IO.Path]::GetTempPath()) ('copia_galeria_' + (Get-Date -Format 'yyyyMMdd_HHmmss'))
Copy-Item $vis $copia -Recurse
$r2 = Revisar $copia
$edge = @('C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe', 'C:\Program Files\Microsoft\Edge\Application\msedge.exe') | Where-Object { Test-Path $_ } | Select-Object -First 1
$png = Join-Path $vis '08_CAPTURAS_REALES\GALERIA_HTML\galeria_abierta_desde_copia_temporal.png'
New-Item -ItemType Directory -Force (Split-Path $png) | Out-Null
$null = & $edge --headless=new --disable-gpu --hide-scrollbars --window-size=1600,2400 --virtual-time-budget=15000 "--screenshot=$png" ([Uri](Join-Path $copia '00_ABRIR_DIAGRAMAS.html')).AbsoluteUri 2>&1
$t0 = Get-Date; while (-not (Test-Path $png) -and ((Get-Date) - $t0).TotalSeconds -lt 60) { Start-Sleep -Milliseconds 500 }
Remove-Item $copia -Recurse -Force
$r1, $r2 | Format-Table Carpeta, Enlaces, Absolutas, Rotas -AutoSize | Out-String -Width 200
if ($r1.ListaRotas) { "Rotas en el original: $($r1.ListaRotas)" }
if ($r2.ListaRotas) { "Rotas en la copia: $($r2.ListaRotas)" }
"Imagen de la galería abierta desde la copia: $(Test-Path $png)"
