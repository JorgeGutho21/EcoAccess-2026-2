<#
  Cambia el origen de las 5 tablas de datos del modelo Power BI (PBIP) para que lean DIRECTAMENTE
  las vistas VW_BI_* de Oracle con la cuenta de solo lectura ECOACCESS_BD_LECTOR (modo Importación).
  - Agrega dos parámetros:  OrigenDatos ("ORACLE" o "CSV")  y  ServidorOracle ("localhost:1521/FREEPDB1").
  - Con OrigenDatos = "CSV" vuelve a leer los CSV de DATOS_Y_CONSULTAS\datos_csv (respaldo sin Oracle).
  - Solo usa navegación a las vistas autorizadas (no tablas, no SQL nativo).
  - Es idempotente: si ya se aplicó, no cambia nada.
  La contraseña NO está en el modelo: Power BI la pide la primera vez (Base de datos → usuario ECOACCESS_BD_LECTOR)
  y la guarda cifrada en la configuración del usuario de Windows, no en el .pbix ni en el PBIP.
#>
param([string]$Bim = (Join-Path $PSScriptRoot 'PROYECTO_PBIP\EcoAccess_BD\EcoAccess_BD.SemanticModel\model.bim'))
$ErrorActionPreference = 'Stop'
$utf8 = New-Object Text.UTF8Encoding $false
$j = [IO.File]::ReadAllText($Bim, $utf8) | ConvertFrom-Json -Depth 100
$exprs = [Collections.ArrayList]@($j.model.expressions)
if (-not ($exprs | Where-Object name -eq 'OrigenDatos')) {
  [void]$exprs.Add([pscustomobject]@{ name = 'OrigenDatos'; kind = 'm'; expression = '"ORACLE" meta [IsParameterQuery = true, List = {"ORACLE", "CSV"}, DefaultValue = "ORACLE", Type = "Text", IsParameterQueryRequired = true]'; lineageTag = [guid]::NewGuid().ToString() })
  [void]$exprs.Add([pscustomobject]@{ name = 'ServidorOracle'; kind = 'm'; expression = '"localhost:1521/FREEPDB1" meta [IsParameterQuery = true, Type = "Text", IsParameterQueryRequired = true]'; lineageTag = [guid]::NewGuid().ToString() })
  $j.model.expressions = $exprs.ToArray()
}
$cambios = 0
foreach ($t in $j.model.tables) {
  foreach ($p in @($t.partitions)) {
    if ($p.source.type -ne 'm') { continue }
    $lin = @($p.source.expression)
    $i = [Array]::FindIndex($lin, [Predicate[string]]{ param($x) $x -match 'Csv\.Document\(File\.Contents\(CarpetaDatos & "\\(vw_bi_\w+)\.csv"\)' })
    if ($i -lt 0 -or $lin[$i] -match 'Oracle\.Database') { continue }
    $vista = ([regex]::Match($lin[$i], 'vw_bi_\w+')).Value.ToUpper()
    if ($lin[$i + 1] -notmatch 'Encabezados = Table\.PromoteHeaders\(Origen') { throw "Formato inesperado en $($t.name)" }
    $csv = $lin[$i].Trim() -replace '^Origen = ', '' -replace ',$', ''
    $nuevo = @(
      "    // Origen: vista $vista de Oracle con la cuenta ECOACCESS_BD_LECTOR (o los CSV si OrigenDatos = ""CSV"")",
      "    Origen = if OrigenDatos = ""ORACLE"" then Oracle.Database(ServidorOracle, [HierarchicalNavigation = false]){[Schema = ""ECOACCESS_BD"", Item = ""$vista""]}[Data] else $csv,",
      "    Encabezados = if OrigenDatos = ""ORACLE"" then Origen else Table.PromoteHeaders(Origen, [PromoteAllScalars = true]),")
    $p.source.expression = @($lin[0..($i - 1)]) + $nuevo + @($lin[($i + 2)..($lin.Count - 1)])
    $cambios++
  }
}
[IO.File]::WriteAllText($Bim, ($j | ConvertTo-Json -Depth 100), $utf8)
"Tablas cambiadas a Oracle directo: $cambios (0 = ya estaba aplicado)"
