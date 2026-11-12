<#
.SYNOPSIS
    Descarga candidatas de Wikimedia Commons con licencia libre para una ronda de evaluación visual (equivalente en
    PowerShell de buscar_fotos_commons.py, para equipos sin Python).
.DESCRIPTION
    Solo descarga candidatas y guarda su procedencia (título, licencia, URL y búsqueda) en fuentes.json. Una persona debe
    revisarlas y etiquetarlas ANTES de ejecutar el modelo. Excluye los títulos de rondas anteriores (-Excluidos), para que
    la ronda sea independiente. Solo acepta CC0, CC BY, CC BY-SA y dominio público.
.EXAMPLE
    pwsh -File Buscar-FotosCommons.ps1 -Destino C:\corpus\ronda5 -Busquedas busquedas.json -Excluidos excluidos.txt
#>
param([Parameter(Mandatory)][string] $Destino, [Parameter(Mandatory)][string] $Busquedas, [string] $Excluidos, [int] $PorBusqueda = 4)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $Destino | Out-Null
$agente = @{ 'User-Agent' = 'EcoAccessP3-evaluacion-academica/1.0 (proyecto universitario)' }
$libres = '^(cc0|cc[- ]by|cc[- ]by[- ]sa|public domain|pd)'
$vistos = New-Object 'System.Collections.Generic.HashSet[string]'
if ($Excluidos -and (Test-Path $Excluidos)) { Get-Content $Excluidos | % { [void]$vistos.Add($_) } }
$fuentes = New-Object System.Collections.Generic.List[object]
$registro = Join-Path $Destino "fuentes.json"
# Reanudable: conserva lo ya descargado y no repite títulos.
if (Test-Path $registro) { foreach ($f in (Get-Content $registro -Raw | ConvertFrom-Json)) { $fuentes.Add($f); [void]$vistos.Add($f.Titulo) } }
# Respeta los límites de Wikimedia: pausas entre peticiones y reintentos con espera creciente ante «too many requests».
function Pedir([scriptblock] $accion) {
    for ($intento = 1; ; $intento++) {
        try { return & $accion } catch { if ($intento -ge 6 -or "$_" -notmatch "too many|429|rate|robot|190.|403") { throw }; Start-Sleep -Seconds (30 * $intento) }
    }
}
$clases = Get-Content $Busquedas -Raw | ConvertFrom-Json
foreach ($clase in $clases.PSObject.Properties) {
    $n = @($fuentes | Where-Object { $_.Archivo -like "$($clase.Name)-*" }).Count
    if ($n -gt 0) { continue }   # clase ya descargada en una corrida anterior
    foreach ($termino in $clase.Value) {
        $q = [uri]::EscapeDataString("$termino filetype:bitmap")
        $url = "https://commons.wikimedia.org/w/api.php?action=query&generator=search&gsrsearch=$q&gsrnamespace=6&gsrlimit=15&prop=imageinfo&iiprop=url|extmetadata|mime&iiurlwidth=800&format=json"
        $datos = Pedir { Invoke-RestMethod -Uri $url -Headers $agente -TimeoutSec 60 }
        Start-Sleep -Seconds 3
        if (-not $datos.query) { continue }
        $tomadas = 0
        foreach ($p in ($datos.query.pages.PSObject.Properties.Value | Sort-Object index)) {
            if ($tomadas -ge $PorBusqueda) { break }
            $info = $p.imageinfo[0]
            $lic = "$($info.extmetadata.LicenseShortName.value)"
            if ($lic -notmatch $libres -or $info.mime -notin 'image/jpeg', 'image/png' -or -not $vistos.Add($p.title)) { continue }
            $n++; $tomadas++
            $archivo = '{0}-{1:D2}.jpg' -f $clase.Name, $n
            Pedir { Invoke-WebRequest -Uri $info.thumburl -Headers $agente -OutFile (Join-Path $Destino $archivo) -TimeoutSec 60 } | Out-Null
            # El autor permite separar entrenamiento, validación y prueba sin que una misma sesión fotográfica quede en dos conjuntos.
            $autor = ("$($info.extmetadata.Artist.value)" -replace '<[^>]+>', '' -replace '\s+', ' ').Trim()
            $fuentes.Add([pscustomobject]@{ Archivo = $archivo; Titulo = $p.title; Licencia = $lic; Autor = $autor; Url = $info.descriptionurl; Busqueda = $termino })
            $fuentes | ConvertTo-Json -Depth 3 | Set-Content $registro -Encoding utf8
            Start-Sleep -Seconds 4
        }
    }
}
$fuentes | ConvertTo-Json -Depth 3 | Set-Content (Join-Path $Destino 'fuentes.json') -Encoding utf8
"Candidatas: $($fuentes.Count)"
