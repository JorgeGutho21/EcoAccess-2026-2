<#
.SYNOPSIS
    Abre ECOACCESS DOS CLICS desde la copia local del repositorio (la versión de esta rama), sin esperar a GitHub Pages.
.DESCRIPTION
    Sirve web\ciudadano en http://localhost:5173 (origen ya permitido por la pasarela) con un servidor estático mínimo
    de PowerShell (HttpListener, solo localhost, sin permisos de administrador ni Python) y abre /dos-clics/ en el
    navegador predeterminado. Si el servidor ya está activo, solo abre la página. El servidor queda oculto en segundo
    plano hasta cerrar sesión; para detenerlo: Abrir-DosClics-Local.ps1 -Detener.
    El acceso directo «ECOACCESS DOS CLICS» de este equipo puede apuntar aquí mientras la página pública no se actualiza.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Abrir-DosClics-Local.ps1
#>
param([int] $Puerto = 5173, [string] $Ruta = 'dos-clics/', [switch] $Servir, [switch] $Detener)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$sitio = [IO.Path]::GetFullPath((Join-Path $raiz 'web\ciudadano'))
$prefijo = "http://localhost:$Puerto/"

function Test-Activo {
    try { return (Invoke-WebRequest ($prefijo + 'dos-clics/') -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200 } catch { return $false }
}

if ($Detener) {
    try { Invoke-WebRequest ($prefijo + '__detener') -UseBasicParsing -TimeoutSec 2 | Out-Null } catch { }
    Write-Host 'Servidor local de DOS CLICS detenido.'; return
}

if ($Servir) {
    $tipos = @{ '.html' = 'text/html; charset=utf-8'; '.js' = 'text/javascript; charset=utf-8'; '.mjs' = 'text/javascript; charset=utf-8';
        '.css' = 'text/css; charset=utf-8'; '.json' = 'application/json; charset=utf-8'; '.webmanifest' = 'application/manifest+json';
        '.webp' = 'image/webp'; '.png' = 'image/png'; '.jpg' = 'image/jpeg'; '.svg' = 'image/svg+xml'; '.ico' = 'image/x-icon'; '.txt' = 'text/plain; charset=utf-8' }
    $l = New-Object Net.HttpListener; $l.Prefixes.Add($prefijo); $l.Start()
    try {
        while ($l.IsListening) {
            $c = $l.GetContext(); $r = $c.Response
            try {
                $p = [Uri]::UnescapeDataString($c.Request.Url.AbsolutePath)
                if ($p -eq '/__detener') { $r.StatusCode = 200; $r.Close(); break }
                if ($p.EndsWith('/')) { $p += 'index.html' }
                $f = [IO.Path]::GetFullPath((Join-Path $sitio $p.TrimStart('/')))
                if (-not $f.StartsWith($sitio + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { $r.StatusCode = 403 }
                elseif (Test-Path -LiteralPath $f -PathType Container) { $r.StatusCode = 301; $r.RedirectLocation = $c.Request.Url.AbsolutePath + '/' }
                elseif (Test-Path -LiteralPath $f -PathType Leaf) {
                    $b = [IO.File]::ReadAllBytes($f); $t = $tipos[[IO.Path]::GetExtension($f).ToLowerInvariant()]
                    $r.ContentType = $(if ($t) { $t } else { 'application/octet-stream' }); $r.Headers['Cache-Control'] = 'no-store'
                    $r.ContentLength64 = $b.Length; $r.OutputStream.Write($b, 0, $b.Length)
                } else { $r.StatusCode = 404 }
            } catch { $r.StatusCode = 500 } finally { try { $r.Close() } catch { } }
        }
    } finally { $l.Close() }
    return
}

if (-not (Test-Activo)) {
    Start-Process -FilePath 'powershell.exe' -WindowStyle Hidden -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"", '-Servir', '-Puerto', $Puerto)
    for ($i = 0; $i -lt 20 -and -not (Test-Activo); $i++) { Start-Sleep -Milliseconds 300 }
    if (-not (Test-Activo)) { throw "No arrancó el servidor local en $prefijo (¿puerto ocupado?)." }
}
Start-Process ($prefijo + $Ruta)
