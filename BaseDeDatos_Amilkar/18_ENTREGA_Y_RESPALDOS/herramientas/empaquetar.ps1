<#
  Empaqueta la entrega académica:
   1. Escribe 18_ENTREGA_Y_RESPALDOS\MANIFIESTO_FINAL.md (inventario con rutas relativas, tamaño y SHA-256 corto) a partir de
      00_EMPEZAR_AQUI\ESTADO_DEL_PROYECTO.md (tabla de estado VERIFICADO / PARCIAL / PENDIENTE).
   2. Crea ..\BaseDeDatos_Amilkar_ENTREGA_<fecha>.zip junto a la carpeta del proyecto, EXCLUYENDO:
      _LOCAL_NO_PUBLICAR, respaldos (.DMP, .zip), intermedios regenerables, bin/obj de C#, .vs y temporales (~$*, *.tmp).
   3. Reabre el ZIP, lista su contenido y busca dentro de cada archivo de texto las contraseñas reales de
      _LOCAL_NO_PUBLICAR\credenciales_locales.env (sin imprimirlas). Si encuentra una, borra el ZIP y falla.
  Uso desde la raíz: pwsh -File 18_ENTREGA_Y_RESPALDOS\herramientas\empaquetar.ps1
#>
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$utf8 = New-Object Text.UTF8Encoding $false
$excluir = '^(_LOCAL_NO_PUBLICAR|_VERSIONES_ANTERIORES_LOCALES|\.git)(\\|$)|\\(bin|obj|\.vs)\\|\\_ensambladas\\|(^|\\)~\$|\.tmp$|\.cache$|\.(dmp|zip)$'
$archivos = Get-ChildItem $raiz -Recurse -File -Force | ForEach-Object { $_ | Add-Member -NotePropertyName Rel -NotePropertyValue $_.FullName.Substring($raiz.Length + 1) -PassThru } |
  Where-Object { $_.Rel -notmatch $excluir -and $_.Rel -ne '18_ENTREGA_Y_RESPALDOS\MANIFIESTO_FINAL.md' } | Sort-Object Rel

# ---------- 1. Manifiesto
$sb = New-Object Text.StringBuilder
[void]$sb.AppendLine('# MANIFIESTO FINAL — EcoAccess BD — Profesor Amílkar')
[void]$sb.AppendLine('')
[void]$sb.AppendLine("Generado: $(Get-Date -Format 'yyyy-MM-dd HH:mm') · archivos en la entrega: $($archivos.Count + 1)")
[void]$sb.AppendLine('')
[void]$sb.Append([IO.File]::ReadAllText((Join-Path $raiz '00_EMPEZAR_AQUI\ESTADO_DEL_PROYECTO.md'), $utf8))
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## Inventario de archivos (rutas relativas)')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('| Ruta | Bytes | SHA-256 (16) |')
[void]$sb.AppendLine('|---|---:|---|')
foreach ($a in $archivos) { [void]$sb.AppendLine("| ``$($a.Rel -replace '\\','/')`` | $($a.Length) | $((Get-FileHash $a.FullName).Hash.Substring(0,16)) |") }
[IO.File]::WriteAllText((Join-Path $raiz '18_ENTREGA_Y_RESPALDOS\MANIFIESTO_FINAL.md'), $sb.ToString(), $utf8)

# ---------- 2. ZIP
$zip = Join-Path (Split-Path $raiz) ("BaseDeDatos_Amilkar_ENTREGA_{0}.zip" -f (Get-Date -Format 'yyyyMMdd_HHmm'))
if (Test-Path $zip) { Remove-Item $zip }
$z = [IO.Compression.ZipFile]::Open($zip, 'Create')
try {
  foreach ($a in @($archivos) + @(Get-Item (Join-Path $raiz '18_ENTREGA_Y_RESPALDOS\MANIFIESTO_FINAL.md') | Add-Member -NotePropertyName Rel -NotePropertyValue '18_ENTREGA_Y_RESPALDOS\MANIFIESTO_FINAL.md' -PassThru)) {
    [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($z, $a.FullName, ('BaseDeDatos_Amilkar/' + ($a.Rel -replace '\\', '/')), 'Optimal')
  }
} finally { $z.Dispose() }

# ---------- 3. Validación del ZIP
$claves = @()
$env_archivo = Join-Path $raiz '_LOCAL_NO_PUBLICAR\credenciales_locales.env'
if (Test-Path $env_archivo) { $claves = Get-Content $env_archivo -Encoding UTF8 | Where-Object { $_ -match '^[A-Z_]+=(.+)$' } | ForEach-Object { ($_ -split '=', 2)[1] } }
$z = [IO.Compression.ZipFile]::OpenRead($zip)
$hallazgos = 0; $prohibidos = 0; $n = 0
try {
  foreach ($e in $z.Entries) {
    $n++
    if ($e.FullName -match '_LOCAL_NO_PUBLICAR|credenciales|\.env$|wallet|cwallet|ewallet') { $prohibidos++; Write-Host "PROHIBIDO en el ZIP: $($e.FullName)" }
    if ($e.FullName -match '\.(sql|ps1|md|txt|csv|html|cs|csproj|json|m|dax|bim|log|xml|pbir|pbism|pbip|sln)$') {
      $r = New-Object IO.StreamReader($e.Open()); $t = $r.ReadToEnd(); $r.Close()
      foreach ($c in $claves) { if ($c.Length -ge 6 -and $t.Contains($c)) { $hallazgos++; Write-Host "CONTRASEÑA encontrada en: $($e.FullName)" } }
    }
  }
} finally { $z.Dispose() }
if ($hallazgos -or $prohibidos) { Remove-Item $zip; throw "ZIP rechazado: $hallazgos contraseñas y $prohibidos archivos prohibidos." }
"ZIP: $zip"
"Entradas: $n · tamaño: $([math]::Round((Get-Item $zip).Length/1MB,2)) MB · contraseñas encontradas: 0 · archivos prohibidos: 0 (se revisaron $($claves.Count) claves)"
"SHA-256: $((Get-FileHash $zip).Hash)"
