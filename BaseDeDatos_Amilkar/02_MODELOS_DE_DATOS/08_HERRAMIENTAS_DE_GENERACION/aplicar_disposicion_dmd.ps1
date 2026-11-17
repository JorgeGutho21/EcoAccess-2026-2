<#
  Reordena los diagramas del .dmd (relacional y lógico/MER) con la MISMA disposición por
  módulos del modelo relacional ordenado (generar_relacional_ordenado.ps1):
    fila 0  Módulo 2 Usuarios e identidad
    filas 1-3  Módulo 3 Infraestructura | Módulo 4 Operación | Módulo 5 EcoPuntos y auditoría
    fila 4  Módulo 1 Catálogos base
    debajo  las 9 vistas (sin relaciones)
  Usa los tamaños de caja que guardó Data Modeler y escribe cada conector como polilínea en
  ángulo recto (sin superponerse). Solo cambia coordenadas: no toca tablas, columnas ni claves.
  Uso: pwsh -File aplicar_disposicion_dmd.ps1 -Dmd C:\ruta\EcoAccess_BD.dmd
#>
param([Parameter(Mandatory)] [string]$Dmd)
$ErrorActionPreference = 'Stop'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
. "$aqui\enrutador_modelo.ps1"
$carpeta = Join-Path (Split-Path $Dmd) ([IO.Path]::GetFileNameWithoutExtension($Dmd))

$grid = [ordered]@{
  ROL = 0, 0; USUARIO_ROL = 0, 1; USUARIO = 0, 2; CREDENCIAL_ACCESO = 0, 3; VINCULO_TELEGRAM = 0, 4
  PUNTO_INSTALACION = 1, 0; INTERACCION = 1, 1; CONSULTA_VISION = 1, 2; HISTORIAL_ESTADO_CONTENEDOR = 1, 3
  CONTENEDOR = 2, 0; ORDEN_APERTURA = 2, 1; EVENTO_DISPOSITIVO = 2, 2; MOVIMIENTO_ECOPUNTOS = 2, 3; REGLA_ECOPUNTOS = 2, 4
  CONTENEDOR_MATERIAL = 3, 0; DEPOSITO = 3, 2; AUDITORIA_DEPOSITO = 3, 3
  PESO_REFERENCIAL = 4, 1; MATERIAL = 4, 2
}

function Reordenar($subview, $nombreDe, $conexion) {
  # $nombreDe: oid -> nombre de tabla/entidad/vista ; $conexion: oid del conector -> nombre FK
  $x = [xml](Get-Content $subview -Raw -Encoding UTF8)
  $vidNombre = @{}; $cajas = [ordered]@{}; $vistas = New-Object Collections.Generic.List[object]
  foreach ($ov in $x.Diagram.objectViews.OView) {
    $n = $nombreDe[$ov.oid]; if (-not $n) { continue }
    $vidNombre[$ov.vid] = $n
    if ($grid.Contains($n)) { $cajas[$n] = @{ w = [int]$ov.bounds.width; h = [int]$ov.bounds.height; r = $grid[$n][0]; c = $grid[$n][1]; ov = $ov } }
    else { $vistas.Add($ov) }
  }
  if ($cajas.Count -ne 19) { throw "En $subview se esperaban 19 cajas y hay $($cajas.Count)" }
  $aristas = @(); $conPor = @{}
  foreach ($cn in $x.Diagram.connectors.Connector) {
    $padre = $vidNombre[$cn.vid_source]; $hija = $vidNombre[$cn.vid_target]
    if (-not $padre -or -not $hija) { continue }
    $fk = $conexion[$cn.oid]; if (-not $fk) { $fk = $cn.oid }
    $aristas += @{ hija = $hija; padre = $padre; nombre = $fk }; $conPor[$fk] = $cn
  }
  $res = Invoke-Enrutar -Cajas $cajas -Aristas $aristas -PasilloMin 50 -Sep 10 -Margen 20
  $chk = Test-Enrutado $res
  foreach ($n in $cajas.Keys) { $p = $res.Pos[$n]; $cajas[$n].ov.bounds.x = [string]([int]$p.x); $cajas[$n].ov.bounds.y = [string]([int]$p.y) }
  foreach ($ru in $res.Rutas) {
    $id = $res.Rutas.IndexOf($ru); $cn = $conPor[$ru.e.nombre]
    $pts = @($res.Polis[$id]); [array]::Reverse($pts)     # Data Modeler dibuja del padre (source) a la hija (target)
    $nodo = $cn.SelectSingleNode('points'); $nodo.RemoveAll()
    foreach ($pt in $pts) { $e = $x.CreateElement('point'); $e.SetAttribute('x', [string][int]$pt[0]); $e.SetAttribute('y', [string][int]$pt[1]); [void]$nodo.AppendChild($e) }
  }
  # vistas: una fila ordenada debajo del modelo
  $vx = 40; $vy = $res.Alto + 120
  foreach ($ov in ($vistas | Sort-Object { $nombreDe[$_.oid] })) { $ov.bounds.x = [string]$vx; $ov.bounds.y = [string]$vy; $vx += [int]$ov.bounds.width + 40 }
  $x.Diagram.autoRoute = 'false'
  $w = New-Object Xml.XmlWriterSettings; $w.Encoding = New-Object Text.UTF8Encoding $false; $w.Indent = $false
  $sw = [Xml.XmlWriter]::Create($subview, $w); $x.Save($sw); $sw.Close()
  [pscustomobject]@{ Diagrama = (Split-Path $subview -Leaf); Cajas = $cajas.Count; Conectores = $aristas.Count; Vistas = $vistas.Count; Solapes = $chk.Solapes; Atraviesa = $chk.AtraviesaTablas; Cruces = $chk.Cruces; Ancho = $res.Ancho; Alto = $res.Alto }
}

# --- relacional ---
$rel = Get-ChildItem "$carpeta\rel" -Directory | Select-Object -First 1
$nom = @{}; foreach ($k in 'table', 'view') { Get-ChildItem "$($rel.FullName)\$k\seg_0\*.xml" | ForEach-Object { $d = ([xml](Get-Content $_.FullName -Raw -Encoding UTF8)).DocumentElement; $nom[$d.id] = $d.name } }
$fkn = @{}; Get-ChildItem "$($rel.FullName)\foreignkey\seg_0\*.xml" | ForEach-Object { $d = ([xml](Get-Content $_.FullName -Raw -Encoding UTF8)).DocumentElement; $fkn[$d.id] = $d.name }
$r1 = Reordenar (Get-ChildItem "$($rel.FullName)\subviews\*.xml" | Select-Object -First 1).FullName $nom $fkn
# --- lógico (MER) ---
$nomL = @{}; foreach ($k in 'entity', 'view') { Get-ChildItem "$carpeta\logical\$k\seg_0\*.xml" | ForEach-Object { $d = ([xml](Get-Content $_.FullName -Raw -Encoding UTF8)).DocumentElement; $nomL[$d.id] = ($d.name -replace ' ', '_') } }
$reln = @{}; Get-ChildItem "$carpeta\logical\relation\seg_0\*.xml" | ForEach-Object { $d = ([xml](Get-Content $_.FullName -Raw -Encoding UTF8)).DocumentElement; $reln[$d.id] = $d.name }
$r2 = Reordenar (Get-ChildItem "$carpeta\logical\subviews\*.xml" | Select-Object -First 1).FullName $nomL $reln
$r1, $r2 | Format-Table -AutoSize | Out-String -Width 200
