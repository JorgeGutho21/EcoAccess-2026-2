<#
.SYNOPSIS
    Convierte las etiquetas a ojo del banco de imágenes en manifiestos de entrenamiento, validación y prueba, separados por
    AUTOR (una sesión fotográfica nunca queda en dos conjuntos), más el manifiesto de fotos con varios residuos.
.DESCRIPTION
    etiquetas.txt, una línea por foto:
      objeto único:  archivo|claves aceptadas separadas por coma|ContextoObjeto|admisible(true/false)|grupo
      varios:        archivo|destinos (Blanca,Negra,Verde,Posconsumo)|número de residuos|-|multiobjeto
    El conjunto sale del SHA-256 del autor (o del título si no hay autor): 0-69 entrenamiento, 70-84 validación, 85-99
    prueba. Escribe los manifiestos dentro de la carpeta de fotos (las rutas son relativas a ella) y banco.csv con todo.
#>
param([Parameter(Mandatory)][string] $Banco)
$ErrorActionPreference = 'Stop'
$fotos = Join-Path $Banco 'fotos'
$fuentes = @{}; foreach ($f in (Get-Content (Join-Path $fotos 'fuentes.json') -Raw | ConvertFrom-Json)) { $fuentes[$f.Archivo] = $f }
$sha = [System.Security.Cryptography.SHA256]::Create()
function Conjunto([string] $clave) {
    $h = $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($clave.ToLowerInvariant()))
    $n = [BitConverter]::ToUInt32($h, 0) % 100
    if ($n -lt 70) { 'entrenamiento' } elseif ($n -lt 85) { 'validacion' } else { 'prueba' }
}
$filas = foreach ($linea in Get-Content (Join-Path $Banco 'etiquetas.txt') -Encoding utf8) {
    if ($linea -match '^\s*(#|$)') { continue }
    $p = $linea.Split('|')
    $f = $fuentes[$p[0]]; if (-not $f) { throw "Sin fuente: $($p[0])" }
    $ruta = Join-Path $fotos $p[0]
    $hash = (Get-FileHash $ruta -Algorithm SHA256).Hash.ToLowerInvariant()
    $autor = if ($f.PSObject.Properties['Autor'] -and $f.Autor) { $f.Autor } else { $f.Titulo }
    [pscustomobject]@{ Archivo = $p[0]; Sha256 = $hash; Aceptadas = $p[1]; Contexto = $p[2]; Admisible = $p[3]; Grupo = $p[4]
        Conjunto = (Conjunto $autor); Autor = $autor; Titulo = $f.Titulo; Licencia = $f.Licencia; Url = $f.Url; Busqueda = $f.Busqueda }
}
$filas | Export-Csv (Join-Path $Banco 'banco.csv') -NoTypeInformation -Encoding utf8
foreach ($c in 'entrenamiento', 'validacion', 'prueba') {
    $muestras = @($filas | Where-Object { $_.Conjunto -eq $c -and $_.Grupo -ne 'multiobjeto' } | ForEach-Object {
        [ordered]@{ Archivo = $_.Archivo; Sha256 = $_.Sha256; Clase = $_.Grupo; Aceptadas = @($_.Aceptadas.Split(',')); Contexto = $_.Contexto; Admisible = [bool]::Parse($_.Admisible) } })
    [ordered]@{ Nombre = "Banco de la misión nocturna, conjunto $c (Wikimedia Commons, etiquetado antes de ejecutar el modelo)"
        Licencia = 'Cada foto conserva su licencia CC0/CC BY/CC BY-SA/dominio público; ver banco.csv'; Conjunto = "banco330-$c"; Muestras = $muestras } |
        ConvertTo-Json -Depth 5 | Set-Content (Join-Path $fotos "manifiesto_$c.json") -Encoding utf8
    "{0}: {1} fotos de un objeto" -f $c, $muestras.Count
}
$varios = @($filas | Where-Object Grupo -eq 'multiobjeto' | ForEach-Object {
    [ordered]@{ Archivo = $_.Archivo; Sha256 = $_.Sha256; Destinos = @($_.Aceptadas.Split(',')); Objetos = [int]$_.Contexto; Conjunto = $_.Conjunto } })
[ordered]@{ Nombre = 'Fotos con varios residuos (misión nocturna)'; Licencia = 'ver banco.csv'; Muestras = $varios } |
    ConvertTo-Json -Depth 5 | Set-Content (Join-Path $fotos 'manifiesto_multiobjeto.json') -Encoding utf8
"multiobjeto: $($varios.Count) fotos"
$filas | Group-Object Grupo | ForEach-Object { "{0,-14} {1,4}" -f $_.Name, $_.Count }
