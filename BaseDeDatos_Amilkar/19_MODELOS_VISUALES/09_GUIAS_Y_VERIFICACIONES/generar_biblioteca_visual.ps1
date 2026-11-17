<#
  Genera la biblioteca visual 19_MODELOS_VISUALES a partir de los diagramas ORIGINALES de 02_MODELOS_DE_DATOS.
  No crea modelos nuevos: copia los SVG, PNG y PDF existentes y produce, con Microsoft Edge sin ventana,
  PDF horizontales en tamaños ISO elegidos para que el texto se lea (mínimo ~7,5 pt al imprimir).

  Qué produce:
    - copias de los originales (PNG, SVG y el PDF vectorial que exportó Data Modeler) con su SHA-256;
    - un PDF horizontal por diagrama (A3, A2, A1 o A0 según el contenido real);
    - el modelo relacional completo como póster A1 y como versión de lectura en hojas A3;
    - ATLAS_VISUAL_ECOACCESS.pdf, 00_INDICE_VISUAL.pdf y la galería 00_ABRIR_DIAGRAMAS.html;
    - 09_GUIAS_Y_VERIFICACIONES/ORIGENES_Y_HASHES.csv y VERIFICACION_BIBLIOTECA.md.

  Uso, desde la raíz BaseDeDatos_Amilkar:
    pwsh -File 19_MODELOS_VISUALES\09_GUIAS_Y_VERIFICACIONES\generar_biblioteca_visual.ps1
#>
param([switch]$SoloGaleria)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$vis  = Join-Path $raiz '19_MODELOS_VISUALES'
$mod  = Join-Path $raiz '02_MODELOS_DE_DATOS'
$utf8 = New-Object Text.UTF8Encoding $false
$edge = @('C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe', 'C:\Program Files\Microsoft\Edge\Application\msedge.exe') | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $edge) { throw 'No se encontró Microsoft Edge para imprimir los PDF.' }
$tmp = Join-Path ([IO.Path]::GetTempPath()) 'ecoaccess_visual'
New-Item -ItemType Directory -Force $tmp | Out-Null

# ---------------------------------------------------------------- 1. Datos reales del modelo (Oracle)
$meta = Get-Content (Join-Path $raiz '03_DICCIONARIO_DE_DATOS\metadatos.txt') -Encoding UTF8 | Where-Object { $_ -like 'COL|*' } | ForEach-Object { ($_ -split '\|')[1] }
$colsPorTabla = $meta | Group-Object | ForEach-Object -Begin { $h = @{} } -Process { $h[$_.Name] = $_.Count } -End { $h }
$fks = Get-Content (Join-Path $raiz '03_DICCIONARIO_DE_DATOS\ampliado\restricciones_oracle.txt') -Encoding UTF8 | Where-Object { $_ -like 'FK|*' } | ForEach-Object { $p = $_ -split '\|'; [pscustomobject]@{ Hija = $p[2]; Padre = $p[4] } }
$modulos = [ordered]@{
  M1 = @{ Nombre = 'Catálogos y pesos referenciales'; Tablas = @('MATERIAL', 'PESO_REFERENCIAL') }
  M2 = @{ Nombre = 'Usuarios e identidad'; Tablas = @('ROL', 'USUARIO', 'USUARIO_ROL', 'CREDENCIAL_ACCESO', 'VINCULO_TELEGRAM') }
  M3 = @{ Nombre = 'Infraestructura y códigos QR'; Tablas = @('PUNTO_INSTALACION', 'CONTENEDOR', 'CONTENEDOR_MATERIAL') }
  M4 = @{ Nombre = 'Interacciones, consultas visuales, órdenes, eventos y depósitos'; Tablas = @('INTERACCION', 'CONSULTA_VISION', 'ORDEN_APERTURA', 'EVENTO_DISPOSITIVO', 'DEPOSITO') }
  M5 = @{ Nombre = 'EcoPuntos y auditoría'; Tablas = @('REGLA_ECOPUNTOS', 'MOVIMIENTO_ECOPUNTOS', 'AUDITORIA_DEPOSITO', 'HISTORIAL_ESTADO_CONTENEDOR') }
}
$totTablas = $colsPorTabla.Count; $totCols = ($colsPorTabla.Values | Measure-Object -Sum).Sum; $totFk = $fks.Count
foreach ($k in $modulos.Keys) {
  $t = $modulos[$k].Tablas
  $modulos[$k].Columnas = ($t | ForEach-Object { $colsPorTabla[$_] } | Measure-Object -Sum).Sum
  $modulos[$k].FkInternas = @($fks | Where-Object { $t -contains $_.Hija -and $t -contains $_.Padre }).Count
  $modulos[$k].FkSalientes = @($fks | Where-Object { $t -contains $_.Hija -and $t -notcontains $_.Padre }).Count
}
if ($totTablas -ne 19 -or $totCols -ne 173 -or $totFk -ne 38) { throw "Metadatos inesperados: $totTablas tablas, $totCols columnas, $totFk FK." }

# ---------------------------------------------------------------- 2. Catálogo de diagramas
# Pag: tamaño ISO horizontal. Para los SVG se calcula con escala >= 1 (1 px CSS = 0,2646 mm; la letra más pequeña
# del SVG es de 9,5 px, unos 7 pt). Para las exportaciones de Data Modeler se fijó a partir de la altura de letra
# medida en su PNG de 6250 px de ancho, para que el texto quede cerca de 8 pt.
function M([string]$k) { $m = $modulos[$k]; "$($m.Tablas.Count) tablas ($($m.Tablas -join ', ')), $($m.Columnas) columnas, $($m.FkInternas) FK internas y $($m.FkSalientes) FK hacia otros módulos" }
$items = @(
  [pscustomobject]@{ Id = 'MER'; Grupo = 'Modelos conceptuales'; Carpeta = '01_MER_CONCEPTUAL'; Nombre = 'MER_CONCEPTUAL'; Origen = '01_MER/MER_conceptual'; Tipo = 'svg'; Pag = ''
    Titulo = 'Modelo entidad-relación conceptual (MER)'; Que = 'El modelo conceptual en notación de Chen: 15 entidades y 19 relaciones del negocio. No dibuja tablas puente ni tablas de auditoría, porque describe el negocio y no la implementación.'
    Leer = 'Los rectángulos son entidades, los rombos son relaciones y los números junto a cada línea son la cardinalidad (1, N). Un rectángulo doble no existe en este modelo: todas las entidades son fuertes.' }
  [pscustomobject]@{ Id = 'MEER'; Grupo = 'Modelos conceptuales'; Carpeta = '02_MEER_EXTENDIDO'; Nombre = 'MEER_EXTENDIDO'; Origen = '02_MEER/MEER_extendido'; Tipo = 'svg'; Pag = ''
    Titulo = 'Modelo entidad-relación extendido (MEER)'; Que = 'Las tres especializaciones del modelo: USUARIO en ciudadano, operario y administrador (solapada y total); DEPOSITO con sensor y sin sensor (disjunta y total, discriminante sin_sensor), y MOVIMIENTO_ECOPUNTOS en crédito y reverso (disjunta y total, discriminante tipo).'
    Leer = '«o» indica especialización solapada y «d» disjunta; la doble línea indica participación total. El recuadro inferior explica cómo se implementa cada especialización en las 19 tablas.' }
  [pscustomobject]@{ Id = 'MR'; Grupo = 'Modelo relacional'; Carpeta = '03_MODELO_RELACIONAL_COMPLETO'; Nombre = 'MODELO_RELACIONAL_COMPLETO'; Origen = '03_MODELO_RELACIONAL_GENERAL/MR_00_general_por_modulos'; Tipo = 'svg'; Pag = 'A1'
    Titulo = 'Modelo relacional completo'; Que = "Las $totTablas tablas con sus $totCols columnas y las $totFk claves foráneas, ordenadas por los cinco módulos. Es la implementación física en Oracle (esquema ECOACCESS_BD)."
    Leer = 'Cada caja es una tabla; PK marca la clave primaria y FK las claves foráneas. Las líneas van de la tabla hija (la que guarda la FK) a la tabla padre. Hay una versión póster A1 y una versión de lectura en hojas A3.' }
)
$i = 0
foreach ($k in $modulos.Keys) {
  $i++
  $orig = @('MR_M1_catalogos_base', 'MR_M2_usuarios_identidad', 'MR_M3_infraestructura_qr', 'MR_M4_operacion', 'MR_M5_ecopuntos_auditoria')[$i - 1]
  $carp = @('M1_CATALOGOS', 'M2_USUARIOS', 'M3_INFRAESTRUCTURA', 'M4_OPERACION', 'M5_ECOPUNTOS_AUDITORIA')[$i - 1]
  $items += [pscustomobject]@{ Id = $k; Grupo = 'Módulos relacionales'; Carpeta = "04_MODELO_RELACIONAL_POR_MODULOS/$carp"; Nombre = $carp; Origen = "04_MODELO_RELACIONAL_POR_MODULOS/$orig"; Tipo = 'svg'; Pag = ''
    Titulo = "Módulo $k — $($modulos[$k].Nombre)"; Que = "Diagrama relacional del módulo ${k}: $(M $k). Muestra todas las columnas de cada tabla."
    Leer = 'Las tablas de otros módulos que reciben una FK aparecen como referencia externa. PK y FK se leen igual que en el modelo completo.' }
}
$dmd = New-Object Collections.Generic.List[object]
$dmd.Add(@('DMD_LOGICO', '01_MODELO_LOGICO', 'DMD_MODELO_LOGICO', 'DMD_MER_Logico_por_modulos', 'A0', 'Data Modeler — modelo lógico', 'El modelo lógico del diseño EcoAccess_BD.dmd, exportado desde Oracle SQL Developer Data Modeler, ordenado por módulos.', 'Notación de Data Modeler (Barker): # identificador, * obligatorio, U único; las patas de gallo marcan el lado «muchos».'))
$dmd.Add(@('DMD_REL', '02_MODELO_RELACIONAL_GENERAL', 'DMD_MODELO_RELACIONAL_GENERAL', 'DMD_Relacional_general_por_modulos', 'A0', 'Data Modeler — modelo relacional general (Relational_1)', "El modelo relacional Relational_1 completo: $totTablas tablas y $totFk FK, con PK, UK, FK e índices tal como los muestra Data Modeler.", 'P = clave primaria, F = clave foránea, U = única; la flecha apunta a la tabla padre. Línea continua: FK obligatoria; discontinua: FK opcional.'))
$j = 0
foreach ($k in $modulos.Keys) {
  $j++
  $orig = @('DMD_M1_catalogos_base', 'DMD_M2_usuarios_identidad', 'DMD_M3_infraestructura_qr', 'DMD_M4_operacion', 'DMD_M5_ecopuntos_auditoria')[$j - 1]
  $pag = @('A1', 'A2', 'A1', 'A1', 'A1')[$j - 1]
  $dmd.Add(@("DMD_$k", "03_SUBVISTAS/${k}", "DMD_SUBVISTA_$k", $orig, $pag, "Data Modeler — subvista $k ($($modulos[$k].Nombre))", "Subvista $k del modelo relacional en Data Modeler: $(M $k). Como contexto, la subvista puede mostrar también las tablas de otros módulos que se relacionan con ellas.", 'Misma notación que el modelo relacional general de Data Modeler.'))
}
foreach ($d in $dmd) {
  $items += [pscustomobject]@{ Id = $d[0]; Grupo = 'Exportaciones de Data Modeler'; Carpeta = "05_DATA_MODELER/$($d[1])"; Nombre = $d[2]; Origen = "05_DATA_MODELER_EDITABLE/EXPORTACIONES_DMD/$($d[3])"; Tipo = 'png'; Pag = $d[4]; Titulo = $d[5]; Que = $d[6]; Leer = $d[7] }
}
$items += [pscustomobject]@{ Id = 'FLUJO'; Grupo = 'Flujo de datos'; Carpeta = '06_FLUJO_DE_DATOS'; Nombre = 'FLUJO_DE_DATOS'; Origen = '06_FLUJO_DE_DATOS/FLUJO_datos_ecoaccess'; Tipo = 'svg'; Pag = ''
  Titulo = 'Flujo de datos de EcoAccess'; Que = 'El recorrido del dato en 11 pasos: del código QR y la foto al depósito verificado, los EcoPuntos, la auditoría y Power BI.'
  Leer = 'Cada caja es un paso y la flecha indica el orden; el texto de cada paso nombra la tabla de Oracle donde queda el dato.' }

$iso = [ordered]@{ A3 = @(420, 297); A2 = @(594, 420); A1 = @(841, 594); A0 = @(1189, 841) }
function TamSvg($file) {
  $t = [IO.File]::ReadAllText($file)
  [double]$w = [regex]::Match($t, "<svg[^>]*width='([\d.]+)'").Groups[1].Value
  [double]$h = [regex]::Match($t, "<svg[^>]*height='([\d.]+)'").Groups[1].Value
  @($w, $h)
}
function TamPng($file) { Add-Type -AssemblyName System.Drawing; $b = [Drawing.Image]::FromFile($file); try { @($b.Width, $b.Height) } finally { $b.Dispose() } }

# ---------------------------------------------------------------- 3. Copias, hashes y tamaño de página
$filas = New-Object Collections.Generic.List[object]
foreach ($it in $items) {
  $dest = Join-Path $vis ($it.Carpeta -replace '/', '\')
  New-Item -ItemType Directory -Force $dest | Out-Null
  $base = Join-Path $mod ($it.Origen -replace '/', '\')
  foreach ($ext in 'svg', 'png', 'pdf') {
    $src = "$base.$ext"
    if (-not (Test-Path $src)) { continue }
    $sufijo = if ($ext -eq 'pdf') { if ($it.Tipo -eq 'png') { '_ORIGINAL_VECTORIAL_DATA_MODELER' } else { '_ORIGINAL' } } else { '' }
    $dst = Join-Path $dest "$($it.Nombre)$sufijo.$ext"
    if (-not $SoloGaleria) { Copy-Item $src $dst -Force }
    $filas.Add([pscustomobject]@{ Diagrama = $it.Id; Copia = $dst.Substring($vis.Length + 1).Replace('\', '/'); Original = "02_MODELOS_DE_DATOS/$($it.Origen).$ext"; SHA256 = (Get-FileHash $src).Hash; Igual = ((Get-FileHash $src).Hash -eq (Get-FileHash $dst).Hash) })
  }
  $px = if ($it.Tipo -eq 'svg') { TamSvg "$base.svg" } else { TamPng "$base.png" }
  $it | Add-Member -Force -NotePropertyName AnchoPx -NotePropertyValue $px[0]; $it | Add-Member -Force -NotePropertyName AltoPx -NotePropertyValue $px[1]
  if (-not $it.Pag) {
    $mmW = $px[0] * 0.2646; $mmH = $px[1] * 0.2646
    $it.Pag = ($iso.Keys | Where-Object { [Math]::Min(($iso[$_][0] - 20) / $mmW, ($iso[$_][1] - 34) / $mmH) -ge 1.0 } | Select-Object -First 1)
    if (-not $it.Pag) { $it.Pag = 'A0' }
  }
  $it | Add-Member -Force -NotePropertyName Imagen -NotePropertyValue ("$($it.Nombre).$($it.Tipo)")
  $it | Add-Member -Force -NotePropertyName Dir -NotePropertyValue $dest
}

# ---------------------------------------------------------------- 4. PDF con Edge
function Esc([string]$s) { [Net.WebUtility]::HtmlEncode($s) }
function Imprimir([string]$html, [string]$pdf) {
  $h = Join-Path $tmp ("p_" + [IO.Path]::GetFileNameWithoutExtension($pdf) + '.html')
  [IO.File]::WriteAllText($h, $html, $utf8)
  if (Test-Path $pdf) { Remove-Item $pdf -Force }
  $null = & $edge --headless=new --disable-gpu --no-pdf-header-footer --allow-file-access-from-files --run-all-compositor-stages-before-draw --virtual-time-budget=20000 "--print-to-pdf=$pdf" ([Uri]$h).AbsoluteUri 2>&1
  $t0 = Get-Date; while (-not (Test-Path $pdf) -and ((Get-Date) - $t0).TotalSeconds -lt 60) { Start-Sleep -Milliseconds 300 }
  if (-not (Test-Path $pdf)) { throw "Edge no generó $pdf" }
}
$css = @'
*{box-sizing:border-box} body{margin:0;font-family:'Times New Roman',Times,serif;color:#000;background:#fff}
.hoja{position:relative;overflow:hidden;break-after:page;padding:10mm}
.cab{height:14mm;border-bottom:0.3mm solid #000;margin-bottom:4mm}
.cab h1{font-size:15pt;margin:0 0 1mm 0;font-weight:bold}
.cab p{font-size:9pt;margin:0}
.area{display:flex;align-items:center;justify-content:center}
.area img{width:100%;height:100%;object-fit:contain}
td{overflow-wrap:anywhere}
'@
function PaginaDiagrama($it, [string]$imgUri, [string]$extra = '') {
  $p = $iso[$it.Pag]
  $alto = $p[1] - 20 - 18
  @"
<style>@page p$($it.Id) { size: $($p[0])mm $($p[1])mm; margin:0 } .h$($it.Id){page:p$($it.Id);width:$($p[0])mm;height:$($p[1])mm}</style>
<div class="hoja h$($it.Id)"><div class="cab"><h1>$(Esc $it.Titulo)</h1><p>EcoAccess — Base de Datos · Universidad Popular del Cesar · Profesor Amílkar · Original: 02_MODELOS_DE_DATOS/$(Esc $it.Origen).$($it.Tipo) · Formato $($it.Pag) horizontal$extra</p></div>
<div class="area" style="height:${alto}mm"><img src="$imgUri" alt="$(Esc $it.Titulo)"></div></div>
"@
}
$generados = New-Object Collections.Generic.List[object]
if (-not $SoloGaleria) {
  foreach ($it in $items) {
    $uri = ([Uri](Join-Path $it.Dir $it.Imagen)).AbsoluteUri
    $pdf = Join-Path $it.Dir "$($it.Nombre).pdf"
    Imprimir "<!doctype html><html lang='es'><head><meta charset='utf-8'><style>$css</style></head><body>$(PaginaDiagrama $it $uri)</body></html>" $pdf
    $generados.Add($pdf)
  }
  # Versión de lectura del modelo relacional completo: 4 hojas A3 horizontales, con 6 % de solape y un plano de ubicación.
  $mr = $items | Where-Object Id -eq 'MR'
  $svgUri = ([Uri](Join-Path $mr.Dir $mr.Imagen)).AbsoluteUri
  $W = $mr.AnchoPx; $H = $mr.AltoPx; $ov = 0.06
  $hojas = ''; $n = 0
  foreach ($fila in 0, 1) { foreach ($col in 0, 1) {
    $n++
    $x0 = [Math]::Max(0, $col * $W / 2 - $col * $ov * $W); $y0 = [Math]::Max(0, $fila * $H / 2 - $fila * $ov * $H)
    $cw = $W / 2 + $ov * $W; $ch = $H / 2 + $ov * $H
    $esc = [Math]::Min(330 / ($cw * 0.2646), 255 / ($ch * 0.2646))
    $vw = $cw * 0.2646 * $esc; $vh = $ch * 0.2646 * $esc
    $pos = @('superior izquierda', 'superior derecha', 'inferior izquierda', 'inferior derecha')[$n - 1]
    $hojas += @"
<div class="hoja lect"><div class="cab"><h1>Modelo relacional completo — versión de lectura, hoja $n de 4 (zona $pos)</h1><p>Cada hoja muestra un cuarto del diagrama con 6 % de solape para seguir las líneas entre hojas. El plano de la derecha indica la zona. El diagrama entero está en MODELO_RELACIONAL_COMPLETO.pdf (A1).</p></div>
<div style="display:flex;gap:6mm"><div style="width:${vw}mm;height:${vh}mm;overflow:hidden;position:relative;border:0.2mm solid #000">
<img src="$svgUri" style="position:absolute;left:-$($x0*0.2646*$esc)mm;top:-$($y0*0.2646*$esc)mm;width:$($W*0.2646*$esc)mm;height:$($H*0.2646*$esc)mm"></div>
<div style="width:50mm"><div style="position:relative;width:50mm;height:$(50*$H/$W)mm;border:0.2mm solid #000"><img src="$svgUri" style="width:50mm;height:$(50*$H/$W)mm">
<div style="position:absolute;left:$(50*$x0/$W)mm;top:$(50*$H/$W*$y0/$H)mm;width:$(50*$cw/$W)mm;height:$(50*$H/$W*$ch/$H)mm;border:0.6mm solid #000;background:rgba(0,0,0,0.12)"></div></div>
<p style="font-size:9pt">Zona $pos del modelo.</p></div></div></div>
"@
  } }
  Imprimir "<!doctype html><html lang='es'><head><meta charset='utf-8'><style>$css @page lect { size: 420mm 297mm; margin:0 } .lect{page:lect;width:420mm;height:297mm}</style></head><body>$hojas</body></html>" (Join-Path $mr.Dir 'MODELO_RELACIONAL_COMPLETO_LECTURA_A3.pdf')
}

# ---------------------------------------------------------------- 5. Atlas e índice
$grupos = foreach ($n in ($items.Grupo | Select-Object -Unique)) { [pscustomobject]@{ Name = $n; Group = @($items | Where-Object Grupo -eq $n) } }   # en el orden del catálogo, no alfabético
$portada = @"
<style>@page a4 { size: 297mm 210mm; margin:0 } .a4{page:a4;width:297mm;height:210mm;padding:20mm}</style>
<div class="hoja a4" style="text-align:center;padding-top:35mm"><p style="font-size:14pt;font-weight:bold">UNIVERSIDAD POPULAR DEL CESAR</p><p>Asignatura: Base de Datos · Docente: profesor Amílkar</p>
<p style="font-size:24pt;font-weight:bold;margin-top:14mm">ATLAS VISUAL DE ECOACCESS</p><p style="font-size:13pt">Modelos conceptual, extendido, relacional, Data Modeler y flujo de datos</p>
<p style="margin-top:12mm">EcoAccess — Sistema Inteligente de Gestión y Clasificación de Residuos</p>
<p style="margin-top:10mm;font-weight:bold">Integrantes</p><p>Jorge Alfonso Gutiérrez Thomas<br>Heyder Hedrey Canchila de Ángel<br>Saray Marcela Cadena Palomino</p>
<p style="margin-top:10mm">Valledupar, noviembre de 2026</p></div>
"@
$filasIndice = ''; $num = 0
foreach ($it in $items) { $num++; $filasIndice += "<tr><td>$num</td><td>$(Esc $it.Titulo)</td><td>$($it.Pag) horizontal</td><td>$($it.Carpeta)/$($it.Nombre).pdf</td></tr>" }
$indice = @"
<div class="hoja a4"><h2 style="font-size:16pt;margin:0 0 4mm 0">Índice</h2>
<p style="font-size:10.5pt">Esquema ECOACCESS_BD en Oracle Database 26ai Free: <b>$totTablas tablas, $totCols columnas y $totFk claves foráneas</b> (datos leídos de los metadatos extraídos de Oracle). Cada diagrama tiene una página explicativa seguida del diagrama en su formato horizontal.</p>
<table style="border-collapse:collapse;font-size:8.5pt;width:100%;table-layout:fixed" border="1" cellpadding="2"><colgroup><col style="width:7mm"><col style="width:105mm"><col style="width:24mm"><col></colgroup><tr><th>#</th><th>Diagrama</th><th>Formato</th><th>PDF individual (dentro de 19_MODELOS_VISUALES)</th></tr>$filasIndice
<tr><td>$($num+1)</td><td>Leyenda de símbolos, PK, FK y cardinalidades</td><td>A4 horizontal</td><td>(esta misma obra)</td></tr></table></div>
"@
$cuerpo = ''; $num = 0
foreach ($it in $items) {
  $num++
  $uri = ([Uri](Join-Path $it.Dir $it.Imagen)).AbsoluteUri
  $cuerpo += @"
<div class="hoja a4"><p style="font-size:10pt">Figura $num · $(Esc $it.Grupo)</p><h2 style="font-size:18pt">$(Esc $it.Titulo)</h2>
<p style="font-size:12pt"><b>Qué representa.</b> $(Esc $it.Que)</p><p style="font-size:12pt"><b>Cómo se interpreta.</b> $(Esc $it.Leer)</p>
<p style="font-size:10.5pt"><b>Archivos.</b> PDF horizontal $($it.Pag): 19_MODELOS_VISUALES/$($it.Carpeta)/$($it.Nombre).pdf · Original: 02_MODELOS_DE_DATOS/$($it.Origen).$($it.Tipo)</p></div>
$(PaginaDiagrama $it $uri " · Figura $num")
"@
}
$leyenda = @"
<div class="hoja a4"><h2 style="font-size:18pt">Leyenda de símbolos</h2>
<table style="border-collapse:collapse;font-size:11.5pt;width:100%" border="1" cellpadding="5">
<tr><th>Símbolo</th><th>Dónde aparece</th><th>Significado</th></tr>
<tr><td>Rectángulo</td><td>MER y MEER</td><td>Entidad del negocio.</td></tr>
<tr><td>Rombo</td><td>MER</td><td>Relación entre entidades; los números (1, N) junto a las líneas son la cardinalidad.</td></tr>
<tr><td>«o» / «d» y doble línea</td><td>MEER</td><td>Especialización solapada o disjunta; la doble línea indica participación total.</td></tr>
<tr><td>PK</td><td>Modelos relacionales</td><td>Clave primaria: identifica cada fila. Hay 19, una por tabla.</td></tr>
<tr><td>FK</td><td>Modelos relacionales</td><td>Clave foránea: columna que apunta a la PK de otra tabla. Hay $totFk, de ellas 6 compuestas.</td></tr>
<tr><td>Línea entre tablas</td><td>Modelos relacionales</td><td>Relación implementada por una FK: va de la tabla hija a la tabla padre (1:N; 1:0..1 si la FK también es PK o única).</td></tr>
<tr><td>P · F · U</td><td>Data Modeler</td><td>P clave primaria, F clave foránea, U restricción única. Un asterisco rojo marca columna obligatoria (NOT NULL).</td></tr>
<tr><td># · * · U</td><td>Modelo lógico de Data Modeler</td><td># identificador, * obligatorio, U único.</td></tr>
<tr><td>Pata de gallo</td><td>Data Modeler</td><td>Lado «muchos» de la relación.</td></tr>
<tr><td>Línea continua / discontinua</td><td>Data Modeler</td><td>FK obligatoria / FK opcional (alguna columna admite NULL).</td></tr>
</table></div>
"@
$doc = "<!doctype html><html lang='es'><head><meta charset='utf-8'><title>Atlas visual de EcoAccess</title><style>$css</style></head><body>$portada$indice$cuerpo$leyenda</body></html>"
Imprimir $doc (Join-Path $vis 'ATLAS_VISUAL_ECOACCESS.pdf')

$miniaturas = ''; $num = 0
foreach ($g in $grupos) {
  $miniaturas += "<h2 style='font-size:13pt;margin:4mm 0 2mm 0'>$(Esc $g.Name)</h2><div style='display:flex;flex-wrap:wrap;gap:5mm'>"
  foreach ($it in $g.Group) {
    $num++
    $uri = ([Uri](Join-Path $it.Dir $it.Imagen)).AbsoluteUri
    $miniaturas += "<div style='width:62mm;border:0.2mm solid #000;padding:2mm;break-inside:avoid'><div style='height:34mm;display:flex;align-items:center;justify-content:center'><img src='$uri' style='max-width:58mm;max-height:34mm'></div><p style='font-size:8.5pt;margin:1mm 0'><b>$num. $(Esc $it.Titulo)</b><br>$($it.Pag) horizontal · $($it.Carpeta)/$($it.Nombre).pdf</p></div>"
  }
  $miniaturas += '</div>'
}
$idx = @"
<!doctype html><html lang='es'><head><meta charset='utf-8'><style>$css @page { size: 297mm 210mm; margin:12mm } body{font-size:11pt}</style></head><body>
<h1 style="font-size:17pt;margin:0">Índice visual de los modelos de EcoAccess</h1>
<p>Esquema ECOACCESS_BD: $totTablas tablas, $totCols columnas y $totFk claves foráneas. Abre <b>00_ABRIR_DIAGRAMAS.html</b> para la galería o <b>ATLAS_VISUAL_ECOACCESS.pdf</b> para todos los diagramas en un solo documento. Los originales editables están en 02_MODELOS_DE_DATOS y el diseño de Data Modeler en 05_DATA_MODELER/04_ABRIR_DMD.</p>
$miniaturas</body></html>
"@
Imprimir $idx (Join-Path $vis '00_INDICE_VISUAL.pdf')

# ---------------------------------------------------------------- 6. Copia de consulta del DMD y del DDL
# El diseño se copia COMPLETO (EcoAccess_BD.dmd y su carpeta EcoAccess_BD/), sin los archivos *.local que Data Modeler
# escribe al abrir una sesión. El original editable sigue en 02_MODELOS_DE_DATOS/05_DATA_MODELER_EDITABLE.
$dmdOrig = Join-Path $mod '05_DATA_MODELER_EDITABLE'
$dmdDest = Join-Path $vis '05_DATA_MODELER\04_ABRIR_DMD\COPIA_DE_CONSULTA_DMD'
if (-not $SoloGaleria) {
  if (Test-Path $dmdDest) { Get-ChildItem $dmdDest -Recurse -File | Remove-Item -Force }
  New-Item -ItemType Directory -Force $dmdDest | Out-Null
  Copy-Item (Join-Path $dmdOrig 'EcoAccess_BD.dmd') $dmdDest -Force
  foreach ($f in Get-ChildItem (Join-Path $dmdOrig 'EcoAccess_BD') -Recurse -File | Where-Object { $_.Extension -ne '.local' }) {
    $r = $f.FullName.Substring($dmdOrig.Length + 1); $t = Join-Path $dmdDest $r
    New-Item -ItemType Directory -Force (Split-Path $t) | Out-Null; Copy-Item $f.FullName $t -Force
    $filas.Add([pscustomobject]@{ Diagrama = 'DMD'; Copia = "05_DATA_MODELER/04_ABRIR_DMD/COPIA_DE_CONSULTA_DMD/$($r.Replace('\', '/'))"; Original = "02_MODELOS_DE_DATOS/05_DATA_MODELER_EDITABLE/$($r.Replace('\', '/'))"; SHA256 = (Get-FileHash $f.FullName).Hash; Igual = ((Get-FileHash $f.FullName).Hash -eq (Get-FileHash $t).Hash) })
  }
  $filas.Add([pscustomobject]@{ Diagrama = 'DMD'; Copia = '05_DATA_MODELER/04_ABRIR_DMD/COPIA_DE_CONSULTA_DMD/EcoAccess_BD.dmd'; Original = '02_MODELOS_DE_DATOS/05_DATA_MODELER_EDITABLE/EcoAccess_BD.dmd'; SHA256 = (Get-FileHash (Join-Path $dmdOrig 'EcoAccess_BD.dmd')).Hash; Igual = $true })
  $ddlDest = Join-Path $vis '07_DDL_Y_CODIGO'; New-Item -ItemType Directory -Force $ddlDest | Out-Null
  Copy-Item (Join-Path $raiz '04_SCRIPTS_ORACLE\DDL_FINAL_EXTRAIDO.sql') $ddlDest -Force
  $filas.Add([pscustomobject]@{ Diagrama = 'DDL'; Copia = '07_DDL_Y_CODIGO/DDL_FINAL_EXTRAIDO.sql'; Original = '04_SCRIPTS_ORACLE/DDL_FINAL_EXTRAIDO.sql'; SHA256 = (Get-FileHash (Join-Path $raiz '04_SCRIPTS_ORACLE\DDL_FINAL_EXTRAIDO.sql')).Hash; Igual = ((Get-FileHash (Join-Path $raiz '04_SCRIPTS_ORACLE\DDL_FINAL_EXTRAIDO.sql')).Hash -eq (Get-FileHash (Join-Path $ddlDest 'DDL_FINAL_EXTRAIDO.sql')).Hash) })
}

# ---------------------------------------------------------------- 7. Galería HTML sin Internet (solo rutas relativas)
function Rel([string]$p) { ($p.Substring($vis.Length + 1) -replace '\\', '/') -replace ' ', '%20' }
function Enlace([string]$file, [string]$texto) { if (Test-Path $file) { "<a href='$(Rel $file)'>$texto</a>" } }
$menu = ''; $secciones = ''; $num = 0
foreach ($g in $grupos) {
  $ancla = ($g.Name -replace '[^A-Za-z]', '').ToLower()
  $menu += "<a href='#$ancla'>$(Esc $g.Name)</a>"
  $secciones += "<section id='$ancla'><h2>$(Esc $g.Name)</h2><div class='rejilla'>"
  foreach ($it in $g.Group) {
    $num++
    $png = Join-Path $it.Dir "$($it.Nombre).png"
    $links = @(
      (Enlace (Join-Path $it.Dir "$($it.Nombre).pdf") "PDF $($it.Pag) horizontal"),
      (Enlace $png 'PNG'),
      (Enlace (Join-Path $it.Dir "$($it.Nombre).svg") 'SVG (vectorial)'),
      (Enlace (Join-Path $it.Dir "$($it.Nombre)_ORIGINAL_VECTORIAL_DATA_MODELER.pdf") 'PDF original de Data Modeler'),
      (Enlace (Join-Path $it.Dir "$($it.Nombre)_ORIGINAL.pdf") 'PDF original'),
      (Enlace (Join-Path $it.Dir 'MODELO_RELACIONAL_COMPLETO_LECTURA_A3.pdf') 'Versión de lectura (4 hojas A3)')
    ) | Where-Object { $_ }
    $secciones += @"
<article><a class='vista' href='$(Rel $png)'><img src='$(Rel $png)' alt='$(Esc $it.Titulo)' loading='lazy'></a>
<h3>Figura $num. $(Esc $it.Titulo)</h3><p>$(Esc $it.Que)</p><p class='leer'><b>Cómo leerlo:</b> $(Esc $it.Leer)</p>
<p class='enlaces'>$($links -join ' · ')</p></article>
"@
  }
  $secciones += '</div></section>'
}
$galeria = @"
<!doctype html>
<html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Modelos visuales de EcoAccess</title>
<style>
:root{--texto:#000;--fondo:#fff;--borde:#bbb;--suave:#f4f4f4}
*{box-sizing:border-box} body{margin:0;background:var(--fondo);color:var(--texto);font-family:'Times New Roman',Times,serif;font-size:17px;line-height:1.45}
header{padding:24px 32px 12px;border-bottom:1px solid #000} header h1{margin:0 0 6px;font-size:28px} header p{margin:4px 0}
nav{position:sticky;top:0;background:var(--fondo);border-bottom:1px solid var(--borde);padding:10px 32px;display:flex;flex-wrap:wrap;gap:8px 18px;z-index:2}
nav a,.boton{color:#000;text-decoration:none;border:1px solid #000;padding:5px 12px;border-radius:3px;background:var(--fondo)}
nav a:hover,.boton:hover{background:var(--suave)}
main{padding:12px 32px 40px;max-width:1500px} section{margin-top:28px} h2{font-size:23px;border-bottom:1px solid #000;padding-bottom:4px}
.rejilla{display:grid;grid-template-columns:repeat(auto-fill,minmax(400px,1fr));gap:22px}
article{border:1px solid var(--borde);padding:14px;background:var(--fondo)} article h3{font-size:18px;margin:10px 0 6px}
.vista{display:flex;align-items:center;justify-content:center;height:250px;background:var(--suave);border:1px solid var(--borde)}
.vista img{max-width:100%;max-height:100%;object-fit:contain} .leer{font-size:15px} .enlaces a{color:#000}
.acciones{display:flex;flex-wrap:wrap;gap:10px;margin:14px 0}
table{border-collapse:collapse;width:100%;font-size:16px} td,th{border:1px solid #000;padding:6px 8px;text-align:left;vertical-align:top}
footer{padding:16px 32px;border-top:1px solid #000;font-size:14px}
</style></head><body>
<header><h1>Modelos visuales de EcoAccess</h1>
<p>Base de Datos · Universidad Popular del Cesar · Profesor Amílkar · Jorge Alfonso Gutiérrez Thomas, Heyder Hedrey Canchila de Ángel y Saray Marcela Cadena Palomino</p>
<p>Esquema ECOACCESS_BD en Oracle Database 26ai Free: <b>$totTablas tablas, $totCols columnas y $totFk claves foráneas</b>. Esta galería funciona sin Internet: todos los enlaces son relativos a esta carpeta.</p>
<div class="acciones"><a class="boton" href="$(Rel (Join-Path $vis '03_MODELO_RELACIONAL_COMPLETO\MODELO_RELACIONAL_COMPLETO.pdf'))">Abrir el modelo relacional completo (PDF A1)</a>
<a class="boton" href="ATLAS_VISUAL_ECOACCESS.pdf">Atlas con todos los diagramas (PDF)</a><a class="boton" href="00_INDICE_VISUAL.pdf">Índice visual (PDF)</a>
<a class="boton" href="00_LEEME_PRIMERO.md">Léeme primero</a></div></header>
<nav>$menu<a href='#dmd'>Abrir el DMD y el DDL</a></nav>
<main>$secciones
<section id="dmd"><h2>Abrir el diseño editable y el DDL</h2>
<table><tr><th>Quiero…</th><th>Abro</th></tr>
<tr><td>Abrir el diseño en SQL Developer</td><td><a href="05_DATA_MODELER/04_ABRIR_DMD/GUIA_ABRIR_DMD_EN_SQL_DEVELOPER.pdf">Guía en PDF</a> · <a href="05_DATA_MODELER/04_ABRIR_DMD/GUIA_ABRIR_DMD_EN_SQL_DEVELOPER.md">Guía en texto</a></td></tr>
<tr><td>Abrir el diseño en Data Modeler independiente</td><td><a href="05_DATA_MODELER/04_ABRIR_DMD/GUIA_ABRIR_DMD_EN_DATA_MODELER.pdf">Guía en PDF</a> · <a href="05_DATA_MODELER/04_ABRIR_DMD/GUIA_ABRIR_DMD_EN_DATA_MODELER.md">Guía en texto</a></td></tr>
<tr><td>El archivo de diseño</td><td>05_DATA_MODELER/04_ABRIR_DMD/COPIA_DE_CONSULTA_DMD/EcoAccess_BD.dmd (copia de consulta, con su carpeta EcoAccess_BD). El original editable de la entrega está en 02_MODELOS_DE_DATOS/05_DATA_MODELER_EDITABLE.</td></tr>
<tr><td>Ver el DDL sin ejecutarlo y generar un modelo desde él</td><td><a href="07_DDL_Y_CODIGO/COMO_VER_EL_DDL_Y_GENERAR_UN_MODELO.pdf">Guía en PDF</a> · <a href="07_DDL_Y_CODIGO/DDL_FINAL_EXTRAIDO.sql">DDL_FINAL_EXTRAIDO.sql</a></td></tr>
<tr><td>Ver las capturas reales de SQL Developer y Data Modeler</td><td><a href="08_CAPTURAS_REALES/LEEME.md">Capturas reales</a></td></tr>
</table></section></main>
<footer>Los diagramas se copiaron de 02_MODELOS_DE_DATOS sin modificarlos; su origen y SHA-256 están en 09_GUIAS_Y_VERIFICACIONES/ORIGENES_Y_HASHES.csv. Para regenerar esta carpeta: pwsh -File 19_MODELOS_VISUALES\09_GUIAS_Y_VERIFICACIONES\generar_biblioteca_visual.ps1</footer>
</body></html>
"@
[IO.File]::WriteAllText((Join-Path $vis '00_ABRIR_DIAGRAMAS.html'), $galeria, $utf8)

# ---------------------------------------------------------------- 8. Registro de orígenes y hashes
$filas | Export-Csv (Join-Path $vis '09_GUIAS_Y_VERIFICACIONES\ORIGENES_Y_HASHES.csv') -Encoding UTF8 -Delimiter ';'
$items | Select-Object Id, Grupo, Titulo, Carpeta, Nombre, Pag, AnchoPx, AltoPx, Origen | Export-Csv (Join-Path $vis '09_GUIAS_Y_VERIFICACIONES\CATALOGO_DIAGRAMAS.csv') -Encoding UTF8 -Delimiter ';'
"Diagramas: $($items.Count) · copias: $($filas.Count) (iguales al original: $(@($filas | Where-Object Igual).Count)) · tablas $totTablas · columnas $totCols · FK $totFk"
