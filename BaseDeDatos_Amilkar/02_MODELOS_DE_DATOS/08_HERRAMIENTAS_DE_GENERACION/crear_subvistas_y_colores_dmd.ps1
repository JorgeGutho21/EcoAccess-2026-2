<#
  1) Colorea las cajas del diagrama relacional y del lógico (MER) según el módulo:
       M1 Catálogos base (ocre) · M2 Usuarios e identidad (azul) · M3 Infraestructura y QR (verde)
       M4 Operación (rojo) · M5 EcoPuntos y auditoría (morado) · vistas BI (gris)
  2) Crea en el modelo relacional 5 SUBVISTAS (una por módulo) con sus tablas y las
     tablas vecinas de otros módulos; las líneas se trazan con el mismo enrutador sin superposiciones.
  Requisito: el diseño debe estar CERRADO en Data Modeler mientras se ejecuta.
  Uso: pwsh -File crear_subvistas_y_colores_dmd.ps1 -Dmd C:\ruta\EcoAccess_BD.dmd
#>
param([Parameter(Mandatory)] [string]$Dmd)
$ErrorActionPreference = 'Stop'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
. "$aqui\enrutador_modelo.ps1"
$carpeta = Join-Path (Split-Path $Dmd) ([IO.Path]::GetFileNameWithoutExtension($Dmd))
$utf8 = New-Object Text.UTF8Encoding $false

function ColorJava([string]$hex) { [BitConverter]::ToInt32([BitConverter]::GetBytes([uint32](0xFF000000 -bor [Convert]::ToUInt32($hex.TrimStart('#'), 16))), 0) }
$MOD = [ordered]@{
  M1 = @{ n = 'M1 Catálogos base';            fondo = '#FCF3CF'; borde = '#9A7D0A'; t = 'MATERIAL','PESO_REFERENCIAL' }
  M2 = @{ n = 'M2 Usuarios e identidad';      fondo = '#D6EAF8'; borde = '#2E6DA4'; t = 'ROL','USUARIO_ROL','USUARIO','CREDENCIAL_ACCESO','VINCULO_TELEGRAM' }
  M3 = @{ n = 'M3 Infraestructura y QR';      fondo = '#D5F5E3'; borde = '#1E8449'; t = 'PUNTO_INSTALACION','CONTENEDOR','CONTENEDOR_MATERIAL' }
  M4 = @{ n = 'M4 Operación del sistema';     fondo = '#FADBD8'; borde = '#B03A2E'; t = 'INTERACCION','CONSULTA_VISION','ORDEN_APERTURA','EVENTO_DISPOSITIVO','DEPOSITO' }
  M5 = @{ n = 'M5 EcoPuntos y auditoría';     fondo = '#E8DAEF'; borde = '#6C3483'; t = 'REGLA_ECOPUNTOS','MOVIMIENTO_ECOPUNTOS','AUDITORIA_DEPOSITO','HISTORIAL_ESTADO_CONTENEDOR' }
}
$modDe = @{}; foreach ($k in $MOD.Keys) { foreach ($t in $MOD[$k].t) { $modDe[$t] = $k } }

function PonerColor($ov, $fondo, $borde) {
  foreach ($tag in 'useDefaultColor', 'backgroundColor', 'foregroundColor') { $n = $ov.SelectSingleNode($tag); if ($n) { [void]$ov.RemoveChild($n) } }
  $d = $ov.OwnerDocument
  $a = $d.CreateElement('useDefaultColor'); $a.InnerText = 'false'; [void]$ov.AppendChild($a)
  $b = $d.CreateElement('backgroundColor'); $b.InnerText = [string](ColorJava $fondo); [void]$ov.AppendChild($b)
  $c = $d.CreateElement('foregroundColor'); $c.InnerText = [string](ColorJava $borde); [void]$ov.AppendChild($c)
}
function Guardar($xml, $ruta) { $w = New-Object Xml.XmlWriterSettings; $w.Encoding = $utf8; $w.Indent = $false; $sw = [Xml.XmlWriter]::Create($ruta, $w); $xml.Save($sw); $sw.Close() }
function Colorear($subview, $nombreDe) {
  $x = [xml](Get-Content $subview -Raw -Encoding UTF8); $n = 0
  foreach ($ov in $x.Diagram.objectViews.OView) {
    $nom = $nombreDe[$ov.oid]; if (-not $nom) { continue }
    if ($modDe.ContainsKey($nom)) { PonerColor $ov $MOD[$modDe[$nom]].fondo $MOD[$modDe[$nom]].borde } else { PonerColor $ov '#E5F5E0' '#7F8C8D' }
    $n++
  }
  Guardar $x $subview; $n
}

# ---------- relacional ----------
$rel = (Get-ChildItem "$carpeta\rel" -Directory | Select-Object -First 1).FullName
$nom = @{}; foreach ($k in 'table', 'view') { Get-ChildItem "$rel\$k\seg_0\*.xml" | ForEach-Object { $d = ([xml](Get-Content $_.FullName -Raw -Encoding UTF8)).DocumentElement; $nom[$d.id] = $d.name } }
$oidDe = @{}; foreach ($k in $nom.Keys) { $oidDe[$nom[$k]] = $k }
$fks = @(); Get-ChildItem "$rel\foreignkey\seg_0\*.xml" | ForEach-Object {
  $d = ([xml](Get-Content $_.FullName -Raw -Encoding UTF8)).DocumentElement
  $fks += @{ oid = $d.id; nombre = $d.name; padre = $nom[$d.referredTableID]; hija = $nom[$d.containerWithKeyObject] }
}
if (@($fks | Where-Object { -not $_.padre -or -not $_.hija }).Count) { throw 'FK sin tabla resuelta' }
$diag = Get-Content "$rel\Diagrams.local" -Raw -Encoding UTF8
$mainId = [regex]::Match($diag, 'objectType="main_view" objectID="([^"]+)"').Groups[1].Value
$main = "$rel\subviews\$mainId.xml"
$tamano = @{}; $mx = [xml](Get-Content $main -Raw -Encoding UTF8)
foreach ($ov in $mx.Diagram.objectViews.OView) { $tamano[$nom[$ov.oid]] = @{ w = [int]$ov.bounds.width; h = [int]$ov.bounds.height } }
$coloreadas = Colorear $main $nom

# borrar subvistas anteriores (p. ej. la de prueba SubView_1)
foreach ($m in [regex]::Matches($diag, '<object objectType="sub_view" objectID="([^"]+)"[^>]*/>')) { Remove-Item "$rel\subviews\$($m.Groups[1].Value).xml" -ErrorAction SilentlyContinue }
$diag = [regex]::Replace($diag, '\s*<object objectType="sub_view"[^>]*/>', '')

$plantilla = @'
<?xml version = '1.0' encoding = 'UTF-8'?>
<Diagram class="oracle.dbtools.crest.swingui.relational.DPVRelationalSubView" name="NOMBRE" id="IDENT">
<createdBy>Usuario</createdBy>
<modifiedBy>Usuario</modifiedBy>
<createdTime>FECHA</createdTime>
<autoRoute>false</autoRoute>
<boxInbox>true</boxInbox>
<showLegend>false</showLegend>
<showLabels>false</showLabels>
<showRelAttributes>false</showRelAttributes>
<gridEnabled>false</gridEnabled>
<showGrid>false</showGrid>
<diagramColor>-1</diagramColor>
<display>false</display>
<notation>1</notation>
<objectViews/>
<connectors/>
</Diagram>
'@
function NuevoId { ([guid]::NewGuid().ToString().ToUpper()) -replace '^(.{8})-(.{4})-(.{4})-(.{4})-(.{12})$', '$1-$2-$3-$4-$5' }

$SUB = [ordered]@{
  M1 = @{ ubic = [ordered]@{ CONTENEDOR_MATERIAL = 0, 0; INTERACCION = 0, 1; CONSULTA_VISION = 0, 2; DEPOSITO = 0, 3; REGLA_ECOPUNTOS = 0, 4; PESO_REFERENCIAL = 1, 1; MATERIAL = 1, 2 } }
  M2 = @{ ubic = [ordered]@{ ROL = 0, 0; USUARIO_ROL = 0, 1; USUARIO = 0, 2; CREDENCIAL_ACCESO = 0, 3; VINCULO_TELEGRAM = 1, 2 } }
  M3 = @{ ubic = [ordered]@{ PUNTO_INSTALACION = 0, 0; CONTENEDOR = 0, 1; CONTENEDOR_MATERIAL = 0, 2; MATERIAL = 0, 3 } }
  M4 = @{ ubic = [ordered]@{ USUARIO = 0, 2; PUNTO_INSTALACION = 1, 0; INTERACCION = 1, 1; CONSULTA_VISION = 1, 2; CONTENEDOR = 2, 0; ORDEN_APERTURA = 2, 1; EVENTO_DISPOSITIVO = 2, 2; PESO_REFERENCIAL = 3, 1; DEPOSITO = 3, 2; MATERIAL = 3, 3 } }
  M5 = @{ ubic = [ordered]@{ USUARIO = 0, 1; HISTORIAL_ESTADO_CONTENEDOR = 1, 0; MOVIMIENTO_ECOPUNTOS = 1, 1; REGLA_ECOPUNTOS = 1, 2; MATERIAL = 1, 3; CONTENEDOR = 2, 0; DEPOSITO = 2, 1; AUDITORIA_DEPOSITO = 2, 2 } }
}
$resumen = @()
foreach ($k in $SUB.Keys) {
  $ubic = $SUB[$k].ubic; $propias = $MOD[$k].t
  $cajas = [ordered]@{}; foreach ($t in $ubic.Keys) { $cajas[$t] = @{ w = $tamano[$t].w; h = $tamano[$t].h; r = $ubic[$t][0]; c = $ubic[$t][1] } }
  $ar = @($fks | Where-Object { $cajas.Contains($_.hija) -and $cajas.Contains($_.padre) -and ($propias -contains $_.hija -or $propias -contains $_.padre) })
  $res = Invoke-Enrutar -Cajas $cajas -Aristas $ar -PasilloMin 50 -Sep 10 -Margen 20
  $chk = Test-Enrutado $res
  $id = NuevoId
  $x = [xml]($plantilla.Replace('NOMBRE', $MOD[$k].n).Replace('IDENT', $id).Replace('FECHA', (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm:ss') + ' UTC'))
  $ovs = $x.Diagram.SelectSingleNode('objectViews'); $cns = $x.Diagram.SelectSingleNode('connectors'); $vid = @{}
  foreach ($t in $cajas.Keys) {
    $vid[$t] = NuevoId
    $ov = $x.CreateElement('OView'); $ov.SetAttribute('class', 'oracle.dbtools.crest.swingui.relational.TVTable'); $ov.SetAttribute('oid', $oidDe[$t]); $ov.SetAttribute('otype', 'Table'); $ov.SetAttribute('vid', $vid[$t])
    $h = $x.CreateElement('hideElementsInCurrentOnly'); $h.InnerText = 'false'; [void]$ov.AppendChild($h)
    $b = $x.CreateElement('bounds'); $p = $res.Pos[$t]
    foreach ($a in @(@('x', $p.x), @('y', $p.y), @('width', $p.w), @('height', $p.h))) { $b.SetAttribute($a[0], [string][int]$a[1]) }
    [void]$ov.AppendChild($b)
    PonerColor $ov $MOD[$modDe[$t]].fondo $MOD[$modDe[$t]].borde   # Data Modeler guarda el color por objeto: igual en todas las vistas
    [void]$ovs.AppendChild($ov)
  }
  foreach ($ru in $res.Rutas) {
    $i = $res.Rutas.IndexOf($ru); $fk = $ru.e
    $cn = $x.CreateElement('Connector'); $cn.SetAttribute('class', 'oracle.dbtools.crest.swingui.relational.TVFKRelation'); $cn.SetAttribute('oid', $fk.oid); $cn.SetAttribute('otype', 'FKIndexAssociation')
    $cn.SetAttribute('vid_source', $vid[$fk.padre]); $cn.SetAttribute('vid_target', $vid[$fk.hija])
    foreach ($par in @(@('hideElementsInCurrentOnly', 'false'), @('backgroundColor', '-16777216'), @('lineWidth', '1'))) { $e = $x.CreateElement($par[0]); $e.InnerText = $par[1]; [void]$cn.AppendChild($e) }
    $pts = $x.CreateElement('points'); $pl = @($res.Polis[$i]); [array]::Reverse($pl)
    foreach ($pt in $pl) { $e = $x.CreateElement('point'); $e.SetAttribute('x', [string][int]$pt[0]); $e.SetAttribute('y', [string][int]$pt[1]); [void]$pts.AppendChild($e) }
    [void]$cn.AppendChild($pts); [void]$cns.AppendChild($cn)
  }
  Guardar $x "$rel\subviews\$id.xml"
  $diag = $diag.Replace('</oracle.dbtools.crest.model.design.relational.RelationalDesign>', "   <object objectType=`"sub_view`" objectID=`"$id`" name=`"$($MOD[$k].n)`" visible=`"true`"/>`r`n</oracle.dbtools.crest.model.design.relational.RelationalDesign>")
  $resumen += [pscustomobject]@{ Subvista = $MOD[$k].n; Tablas = $cajas.Count; FK = $ar.Count; Solapes = $chk.Solapes; Atraviesa = $chk.AtraviesaTablas; Cruces = $chk.Cruces }
}
[IO.File]::WriteAllText("$rel\Diagrams.local", $diag, $utf8)

# ---------- lógico (MER): solo colores ----------
$nomL = @{}; foreach ($k in 'entity', 'view') { Get-ChildItem "$carpeta\logical\$k\seg_0\*.xml" | ForEach-Object { $d = ([xml](Get-Content $_.FullName -Raw -Encoding UTF8)).DocumentElement; $nomL[$d.id] = ($d.name -replace ' ', '_') } }
$colL = Colorear (Get-ChildItem "$carpeta\logical\subviews\*.xml" | Select-Object -First 1).FullName $nomL
"Cajas coloreadas: relacional $coloreadas, lógico $colL"
$resumen | Format-Table -AutoSize | Out-String -Width 200
