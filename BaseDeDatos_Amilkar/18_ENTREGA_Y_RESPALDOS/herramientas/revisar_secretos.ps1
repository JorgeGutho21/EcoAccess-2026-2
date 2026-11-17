<#
  Revisa una carpeta (o los archivos preparados para un commit) antes de publicarlos.
  Falla si encuentra:
    - una contraseña real de _LOCAL_NO_PUBLICAR\credenciales_locales.env (se comparan sin mostrarlas);
    - archivos prohibidos: .env, wallets de Oracle, tnsnames/sqlnet, respaldos .DMP, ZIP;
    - patrones de secretos: claves privadas, tokens de bot de Telegram, «Password=» con valor en cadenas de conexión.
  Uso:
    pwsh -File revisar_secretos.ps1 -Carpeta <ruta>            revisa todos los archivos de la carpeta
    pwsh -File revisar_secretos.ps1 -Repositorio <ruta del clon> revisa solo lo preparado con git add (git diff --cached)
  Nunca imprime el valor de una contraseña; solo el archivo donde aparece.
#>
param([string]$Carpeta, [string]$Repositorio)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$env_archivo = Join-Path $raiz '_LOCAL_NO_PUBLICAR\credenciales_locales.env'
$claves = @(); if (Test-Path $env_archivo) { $claves = @(Get-Content $env_archivo -Encoding UTF8 | Where-Object { $_ -match '^[A-Z_]+=(.+)$' } | ForEach-Object { ($_ -split '=', 2)[1] } | Where-Object { $_.Length -ge 6 }) }

if ($Repositorio) {
  Push-Location $Repositorio
  try { $lista = @(git diff --cached --name-only --diff-filter=ACMR) | ForEach-Object { Join-Path $Repositorio $_ } } finally { Pop-Location }
} elseif ($Carpeta) {
  $lista = Get-ChildItem $Carpeta -Recurse -File -Force | Where-Object { $_.FullName -notmatch '\\_LOCAL_NO_PUBLICAR\\|\\\.git\\' } | ForEach-Object FullName
} else { throw 'Indica -Carpeta o -Repositorio.' }

$prohibidos = '(^|\\)(\.env|cwallet\.sso|ewallet\.p12|tnsnames\.ora|sqlnet\.ora|credenciales[^\\]*)$|\.(dmp|zip|pfx|p12|key)$|_LOCAL_NO_PUBLICAR'
$patrones = @(
  @{ N = 'clave privada'; R = '-----BEGIN [A-Z ]*PRIVATE KEY-----' }
  @{ N = 'token de bot de Telegram'; R = '\b\d{8,10}:AA[A-Za-z0-9_-]{30,}\b' }
  @{ N = 'contraseña escrita en una cadena de conexión'; R = '(?i)[";''](?:[^"'']*;)?\s*(password|pwd)\s*=\s*(?![\$\{<…*x])[^;"''\s]{4,}' }
)
$texto = '\.(sql|ps1|md|txt|csv|html|cs|csproj|json|m|dax|bim|log|xml|pbir|pbism|pbip|sln|config|yml|ini)$'
$hallazgos = New-Object Collections.Generic.List[string]
foreach ($f in $lista) {
  if (-not (Test-Path $f)) { continue }
  if ($f -match $prohibidos) { $hallazgos.Add("ARCHIVO PROHIBIDO: $f"); continue }
  if ($f -notmatch $texto) { continue }
  $t = [IO.File]::ReadAllText($f)
  foreach ($c in $claves) { if ($t.Contains($c)) { $hallazgos.Add("CONTRASEÑA REAL encontrada en: $f") } }
  foreach ($p in $patrones) { if ([regex]::IsMatch($t, $p.R)) { $hallazgos.Add("POSIBLE $($p.N) en: $f") } }
}
"Archivos revisados: $(@($lista).Count) · contraseñas reales comparadas: $($claves.Count) · hallazgos: $($hallazgos.Count)"
$hallazgos | ForEach-Object { $_ }
if ($hallazgos.Count) { exit 1 }
