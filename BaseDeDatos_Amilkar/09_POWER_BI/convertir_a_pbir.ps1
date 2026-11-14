<#
  Convierte el informe heredado (EcoAccess_BD.Report\report.json, formato antiguo de una sola pieza)
  al formato PBIR (definition\pages\<página>\visuals\<visual>\visual.json), que Power BI Desktop 2.159
  sí carga. Motivo: al abrir el .pbip con el report.json antiguo, Power BI mostraba
  «No se pudo cargar el informe» (comprobado en pantalla el 11/11/2026).
  No toca el modelo semántico ni los datos.
  Uso: pwsh -File convertir_a_pbir.ps1 -Origen <carpeta .Report> -Destino <carpeta .Report nueva>
#>
param([Parameter(Mandatory)][string]$Origen, [Parameter(Mandatory)][string]$Destino)
$ErrorActionPreference = 'Stop'
$utf8 = New-Object Text.UTF8Encoding $false
$URL = 'https://developer.microsoft.com/json-schemas/fabric/item/report'
function Guardar($ruta, $obj) { New-Item -ItemType Directory -Force (Split-Path $ruta) | Out-Null; [IO.File]::WriteAllText($ruta, ($obj | ConvertTo-Json -Depth 80), $utf8) }

# Cambia {"SourceRef":{"Source":"t0"}} por {"SourceRef":{"Entity":"Tabla"}}
function Resolver($nodo, $alias) {
  if ($nodo -is [System.Collections.IList]) { return @($nodo | ForEach-Object { Resolver $_ $alias }) }
  if ($nodo -isnot [pscustomobject]) { return $nodo }
  $nuevo = [ordered]@{}
  foreach ($p in $nodo.PSObject.Properties) {
    if ($p.Name -eq 'SourceRef' -and $p.Value.Source) { $nuevo['SourceRef'] = [ordered]@{ Entity = $alias[$p.Value.Source] } }
    else { $nuevo[$p.Name] = Resolver $p.Value $alias }
  }
  [pscustomobject]$nuevo
}

$legacy = Get-Content (Join-Path $Origen 'report.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if (Test-Path $Destino) { Remove-Item $Destino -Recurse -Force }
$def = Join-Path $Destino 'definition'

Guardar (Join-Path $Destino 'definition.pbir') ([ordered]@{ '$schema' = "$URL/definitionProperties/2.0.0/schema.json"; version = '4.0'; datasetReference = @{ byPath = @{ path = '../EcoAccess_BD.SemanticModel' } } })
Guardar (Join-Path $def 'version.json') ([ordered]@{ '$schema' = "$URL/definition/versionMetadata/1.0.0/schema.json"; version = '2.0.0' })
Guardar (Join-Path $def 'report.json') ([ordered]@{
    '$schema' = "$URL/definition/report/1.0.0/schema.json"
    layoutOptimization = 'None'
    themeCollection = @{ baseTheme = [ordered]@{ name = 'CY24SU10'; reportVersionAtImport = '5.59'; type = 'SharedResources' } }
    settings = [ordered]@{ useStylableVisualContainerHeader = $true; defaultDrillFilterOtherVisuals = $true } })

$orden = @(); $nVis = 0
foreach ($sec in ($legacy.sections | Sort-Object ordinal)) {
  $orden += $sec.name
  $pag = [ordered]@{ '$schema' = "$URL/definition/page/1.0.0/schema.json"; name = $sec.name; displayName = $sec.displayName; displayOption = 'FitToPage'; height = $sec.height; width = $sec.width }
  $cfgSec = if ($sec.config) { $sec.config | ConvertFrom-Json } else { $null }
  if ($cfgSec -and $cfgSec.objects) { $pag.objects = $cfgSec.objects }
  Guardar (Join-Path $def "pages\$($sec.name)\page.json") $pag

  foreach ($vc in $sec.visualContainers) {
    $c = $vc.config | ConvertFrom-Json; $sv = $c.singleVisual; $pos = $c.layouts[0].position
    $v = [ordered]@{ visualType = $sv.visualType }
    if ($sv.prototypeQuery) {
      $alias = @{}; foreach ($f in $sv.prototypeQuery.From) { $alias[$f.Name] = $f.Entity }
      $sel = @{}; foreach ($s in $sv.prototypeQuery.Select) { $sel[$s.Name] = $s }
      $qs = [ordered]@{}
      foreach ($rol in $sv.projections.PSObject.Properties) {
        $proy = foreach ($p in $rol.Value) {
          $s = $sel[$p.queryRef]
          $campo = [ordered]@{}
          foreach ($k in 'Column', 'Measure', 'Aggregation', 'HierarchyLevel') { if ($s.$k) { $campo[$k] = Resolver $s.$k $alias } }
          $o = [ordered]@{ field = [pscustomobject]$campo; queryRef = $p.queryRef; nativeQueryRef = ($p.queryRef -split '\.', 2)[1] }
          if ($p.active) { $o.active = $true }
          [pscustomobject]$o
        }
        $qs[$rol.Name] = @{ projections = @($proy) }
      }
      $q = [ordered]@{ queryState = $qs }
      if ($sv.prototypeQuery.OrderBy) {
        $q.sortDefinition = [ordered]@{ sort = @($sv.prototypeQuery.OrderBy | ForEach-Object { [ordered]@{ field = (Resolver $_.Expression $alias); direction = $(if ($_.Direction -eq 2) { 'Descending' } else { 'Ascending' }) } }); isDefaultSort = $true }
      }
      $v.query = $q
    }
    if ($sv.objects) {
      $obj = $sv.objects
      if ($sv.visualType -eq 'actionButton') {
        # En PBIR el interruptor «show» va en una entrada SIN selector; los valores por estado van con selector «default».
        $nuevo = [ordered]@{}
        foreach ($o in $obj.PSObject.Properties) {
          $lista = @()
          foreach ($e in $o.Value) {
            if ($e.properties.show) { $lista += [pscustomobject]@{ properties = [pscustomobject]@{ show = $e.properties.show } } }
            $resto = [ordered]@{}; foreach ($pp in $e.properties.PSObject.Properties) { if ($pp.Name -ne 'show') { $resto[$pp.Name] = $pp.Value } }
            if ($resto.Count) { $lista += [pscustomobject]@{ properties = [pscustomobject]$resto; selector = [pscustomobject]@{ id = 'default' } } }
          }
          $nuevo[$o.Name] = $lista
        }
        $obj = [pscustomobject]$nuevo
      }
      $v.objects = $obj
    }
    if ($sv.visualType -eq 'card') {
      # El título ya nombra la medida: se oculta la etiqueta inferior, que quedaba cortada.
      $o2 = if ($v.objects) { $v.objects } else { [pscustomobject]@{} }
      $o2 | Add-Member -Force -NotePropertyName categoryLabels -NotePropertyValue @([pscustomobject]@{ properties = [pscustomobject]@{ show = [pscustomobject]@{ expr = [pscustomobject]@{ Literal = [pscustomobject]@{ Value = 'false' } } } } })
      $v.objects = $o2
    }
    if ($sv.vcObjects) { $v.visualContainerObjects = $sv.vcObjects }
    $v.drillFilterOtherVisuals = $true
    $vis = [ordered]@{ '$schema' = "$URL/definition/visualContainer/1.0.0/schema.json"; name = $c.name
      position = [ordered]@{ x = $pos.x; y = $pos.y; z = $pos.z; width = $pos.width; height = $pos.height }; visual = $v }
    Guardar (Join-Path $def "pages\$($sec.name)\visuals\$($c.name)\visual.json") $vis
    $nVis++
  }
}
Guardar (Join-Path $def 'pages\pages.json') ([ordered]@{ '$schema' = "$URL/definition/pagesMetadata/1.0.0/schema.json"; pageOrder = $orden; activePageName = $orden[0] })
"Páginas: $($orden.Count) · visuales: $nVis → $Destino"
