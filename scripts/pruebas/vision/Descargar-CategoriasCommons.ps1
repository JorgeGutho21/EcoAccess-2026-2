<#
.SYNOPSIS
    Descarga candidatas de CATEGORÍAS de Wikimedia Commons con licencia libre para un banco de evaluación visual.
.DESCRIPTION
    La búsqueda por texto (Buscar-FotosCommons.ps1) devolvió sobre todo grabados, pacas y paisajes (14/11/2026: unas 4
    fotos útiles de 21). Una categoría es mucho más precisa. Este script:
    - lee -Categorias, un JSON { "clase": { "Nombre de categoría": cupo, ... }, ... };
    - toma hasta 500 archivos de cada categoría y los ordena por el SHA-1 de su título: una muestra pseudoaleatoria y
      reproducible, no las primeras por orden alfabético (que suelen ser una misma serie);
    - acepta solo CC0, CC BY, CC BY-SA y dominio público, en JPEG o PNG;
    - excluye los títulos de bancos anteriores (-Excluidos) y los ya descargados;
    - admite como mucho -PorAutor fotos del mismo autor por categoría;
    - guarda título, licencia, autor, URL y categoría en fuentes.json. Es reanudable.
    Solo descarga candidatas: una persona las revisa y etiqueta ANTES de ejecutar el modelo.
.EXAMPLE
    pwsh -File Descargar-CategoriasCommons.ps1 -Destino C:\banco\candidatas -Categorias categorias.json -Excluidos excluidos.txt
#>
param([Parameter(Mandatory)][string] $Destino, [Parameter(Mandatory)][string] $Categorias, [string] $Excluidos, [int] $PorAutor = 3)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $Destino | Out-Null
$agente = @{ 'User-Agent' = 'EcoAccessP3-evaluacion-academica/1.0 (proyecto universitario)' }
$libres = '^(cc0|cc[- ]by|cc[- ]by[- ]sa|public domain|pd)'
$vistos = New-Object 'System.Collections.Generic.HashSet[string]'
if ($Excluidos -and (Test-Path $Excluidos)) { Get-Content $Excluidos -Encoding utf8 | ForEach-Object { [void]$vistos.Add($_) } }
$fuentes = New-Object System.Collections.Generic.List[object]
$registro = Join-Path $Destino 'fuentes.json'
if (Test-Path $registro) { foreach ($f in (Get-Content $registro -Raw -Encoding utf8 | ConvertFrom-Json)) { $fuentes.Add($f); [void]$vistos.Add($f.Titulo) } }
$sha = [System.Security.Cryptography.SHA1]::Create()
function Orden([string] $t) { [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($t))) }
function Pedir([scriptblock] $accion) {
    for ($intento = 1; ; $intento++) {
        try { return & $accion } catch { if ($intento -ge 6 -or "$_" -notmatch 'too many|429|rate|robot|190.|403|timed out|tiempo') { throw }; Start-Sleep -Seconds (30 * $intento) }
    }
}
$config = Get-Content $Categorias -Raw -Encoding utf8 | ConvertFrom-Json
foreach ($clase in $config.PSObject.Properties) {
    foreach ($cat in $clase.Value.PSObject.Properties) {
        $hechas = @($fuentes | Where-Object { $_.Categoria -eq $cat.Name -and $_.Archivo -like "$($clase.Name)-*" }).Count
        if ($hechas -ge [int]$cat.Value) { continue }
        # Hasta 500 miembros de la categoría, en páginas de 50 con su información de licencia.
        $paginas = New-Object System.Collections.Generic.List[object]; $sigue = ''
        for ($vuelta = 0; $vuelta -lt 10; $vuelta++) {
            $url = "https://commons.wikimedia.org/w/api.php?action=query&generator=categorymembers&gcmtitle=$([uri]::EscapeDataString('Category:' + $cat.Name))&gcmtype=file&gcmlimit=50&prop=imageinfo&iiprop=url|extmetadata|mime&iiurlwidth=500&format=json$sigue"
            $datos = Pedir { Invoke-RestMethod -Uri $url -Headers $agente -TimeoutSec 60 }
            Start-Sleep -Seconds 2
            if ($datos.query) { foreach ($p in $datos.query.pages.PSObject.Properties.Value) { $paginas.Add($p) } }
            if (-not $datos.continue -or -not $datos.continue.gcmcontinue) { break }
            $sigue = "&gcmcontinue=$([uri]::EscapeDataString($datos.continue.gcmcontinue))"
        }
        $porAutorCat = @{}
        $n = @($fuentes | Where-Object { $_.Archivo -like "$($clase.Name)-*" }).Count
        foreach ($p in ($paginas | Sort-Object { Orden $_.title })) {
            if ($hechas -ge [int]$cat.Value) { break }
            if (-not $p.imageinfo) { continue }
            $info = $p.imageinfo[0]
            $lic = "$($info.extmetadata.LicenseShortName.value)"
            if ($lic -notmatch $libres -or $info.mime -notin 'image/jpeg', 'image/png' -or -not $info.thumburl) { continue }
            $autor = ("$($info.extmetadata.Artist.value)" -replace '<[^>]+>', '' -replace '\s+', ' ').Trim()
            $llave = if ($autor) { $autor } else { $p.title }
            if ($porAutorCat[$llave] -ge $PorAutor) { continue }
            if (-not $vistos.Add($p.title)) { continue }
            $n++; $hechas++; $porAutorCat[$llave] = 1 + [int]$porAutorCat[$llave]
            $archivo = '{0}-{1:D3}.jpg' -f $clase.Name, $n
            try { Pedir { Invoke-WebRequest -Uri $info.thumburl -Headers $agente -OutFile (Join-Path $Destino $archivo) -TimeoutSec 60 } | Out-Null }
            catch { $n--; $hechas--; continue }
            $fuentes.Add([pscustomobject]@{ Archivo = $archivo; Titulo = $p.title; Licencia = $lic; Autor = $autor; Url = $info.descriptionurl; Categoria = $cat.Name; Busqueda = "Category:$($cat.Name)" })
            $fuentes | ConvertTo-Json -Depth 3 | Set-Content $registro -Encoding utf8
            Start-Sleep -Seconds 3
        }
        "{0:HH:mm:ss} {1} / {2}: {3}" -f (Get-Date), $clase.Name, $cat.Name, $hechas
    }
}
"Candidatas: $($fuentes.Count)"
