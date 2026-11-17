<#
  Verifica un proyecto de Oracle SQL Developer Data Modeler (.dmd) contra el esquema real ECOACCESS_BD.
  Un .dmd auténtico es un archivo pequeño <nombre>.dmd + una carpeta <nombre>\ con XML por objeto.
  Este script cuenta, dentro de esa carpeta, las tablas (<Table>), columnas (<Column> dentro de cada
  tabla) y claves foráneas (<FKIndexAssociation>) del modelo relacional, y las compara con Oracle:
     19 tablas · 173 columnas · 38 FK  (USER_TABLES / USER_TAB_COLUMNS / USER_CONSTRAINTS tipo R, 11/11/2026)
  Uso: pwsh -File 02_MODELOS_DE_DATOS\08_HERRAMIENTAS_DE_GENERACION\verificar_dmd.ps1 -Dmd 02_MODELOS_DE_DATOS\05_DATA_MODELER_EDITABLE\EcoAccess_BD.dmd
  Nota honesta: el formato XML del .dmd no está documentado por Oracle; si una versión futura cambia los
  nombres de elementos, el script lo dirá (cuenta 0) en lugar de inventar un PASA.
#>
param([Parameter(Mandatory)][string]$Dmd, [int]$Tablas = 19, [int]$Columnas = 173, [int]$Fks = 38)
$ErrorActionPreference = 'Stop'
$archivo = (Resolve-Path $Dmd).Path
$carpeta = Join-Path (Split-Path $archivo) ([IO.Path]::GetFileNameWithoutExtension($archivo))
if (-not (Test-Path $carpeta)) { throw "No existe la carpeta del diseño: $carpeta (un .dmd sin su carpeta está incompleto)." }
$nt = 0; $nc = 0; $nf = 0; $nombres = @()
foreach ($x in Get-ChildItem $carpeta -Recurse -Filter *.xml) {
    try { [xml]$doc = Get-Content $x.FullName -Raw -Encoding UTF8 } catch { continue }
    $raiz = $doc.DocumentElement
    if ($raiz.LocalName -eq 'Table') {
        $nt++; $nombres += $raiz.name
        $nc += @($raiz.SelectNodes("./*[local-name()='columns']/*[local-name()='Column']")).Count
    }
    $nf += @($doc.SelectNodes("//*[local-name()='FKIndexAssociation']")).Count
}
$ok = ($nt -eq $Tablas) -and ($nc -eq $Columnas) -and ($nf -eq $Fks)
"Diseño: $archivo"
"Tablas en el .dmd   : $nt  (Oracle: $Tablas)"
"Columnas en el .dmd : $nc  (Oracle: $Columnas)"
"FK en el .dmd       : $nf  (Oracle: $Fks)"
"Tablas: " + (($nombres | Sort-Object) -join ', ')
if ($ok) { 'RESULTADO: PASA — el .dmd contiene el mismo modelo que Oracle.'; exit 0 } else { 'RESULTADO: FALLA o formato no reconocido — revisar en Data Modeler.'; exit 1 }
