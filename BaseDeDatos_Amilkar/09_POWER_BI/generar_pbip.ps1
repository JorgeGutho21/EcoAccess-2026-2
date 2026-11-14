<#
  Genera el proyecto Power BI (formato .pbip, texto) "EcoAccess_BD" a partir de los CSV que exporta
  exportar_csv.sql con la cuenta de SOLO LECTURA ECOACCESS_BD_LECTOR.
   - Modelo semántico (model.bim): 6 tablas importadas con Power Query (M), relaciones y medidas DAX.
   - Informe (report.json): 5 páginas con botones de navegación, tarjetas, gráficos, segmentadores y la galería de roles.
  Todas las páginas llevan el rótulo DATOS SINTÉTICOS. Ninguna clave: los CSV no requieren credenciales.
  Uso: pwsh -File generar_pbip.ps1            (luego abrir EcoAccess_BD\EcoAccess_BD.pbip y pulsar «Actualizar»)
#>
$ErrorActionPreference = 'Stop'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
$utf8 = New-Object Text.UTF8Encoding $false
$destino = Join-Path $aqui 'PROYECTO_PBIP\EcoAccess_BD'
$carpetaCsv = Join-Path $aqui 'DATOS_Y_CONSULTAS\datos_csv'
New-Item -ItemType Directory -Force "$destino\EcoAccess_BD.SemanticModel", "$destino\EcoAccess_BD.Report" | Out-Null
function Guardar($ruta, $obj) { [IO.File]::WriteAllText($ruta, ($obj | ConvertTo-Json -Depth 60), $utf8) }

# ------------------------------------------------------------------ MODELO SEMÁNTICO
$tipos = @{ i = 'int64'; s = 'string'; d = 'dateTime'; n = 'double' }
$tiposM = @{ i = 'Int64.Type'; s = 'type text'; d = 'type datetime'; n = 'type number' }
$fuentes = [ordered]@{
  Depositos       = @{ csv = 'vw_bi_depositos'; cols = 'ID_DEPOSITO:i CIUDADANO:s DIA:d FECHA_HORA:d ESTACION:s CANECA:s MATERIAL:s MATERIAL_NOMBRE:s ESTADO:s EVIDENCIA:s UNIDADES:i PESO_ESTIMADO_KG:n PESO_MEDIDO_KG:n CANAL:s ORIGEN_DATO:s' }
  ConsultasVision = @{ csv = 'vw_bi_consultas_vision'; cols = 'ID_CONSULTA:i CIUDADANO:s DIA:d CANAL:s MODO:s RESULTADO:s MATERIAL:s CANECA_INDICADA:s UNIDADES_ESTIMADAS:i PESO_ESTIMADO_KG:n CONTEXTO_QR:s LATENCIA_MS:i ORIGEN_DATO:s' }
  Ecopuntos       = @{ csv = 'vw_bi_ecopuntos'; cols = 'ID_MOVIMIENTO:i CIUDADANO:s DIA:d TIPO:s PUNTOS:i MATERIAL:s TARIFA:s' }
  Auditoria       = @{ csv = 'vw_bi_auditoria'; cols = 'ID_AUDITORIA:i ID_DEPOSITO:i OPERACION:s ESTADO_ANTERIOR:s ESTADO_NUEVO:s CANTIDAD_ANTERIOR:i CANTIDAD_NUEVA:i PESO_MEDIDO_ANTERIOR_KG:n PESO_MEDIDO_NUEVO_KG:n ACTOR:s MOTIVO:s FECHA_COLOMBIA:d' }
  Ciudadanos      = @{ csv = 'vw_bi_ciudadanos'; cols = 'CIUDADANO:s FECHA_REGISTRO:d ACTIVO:i ES_SINTETICO:i ORIGEN_DATO:s' }
}
$medidas = @{
  Depositos       = [ordered]@{
    'Depósitos registrados'             = 'COUNTROWS ( Depositos )'
    'Depósitos verificados'             = 'CALCULATE ( COUNTROWS ( Depositos ), Depositos[ESTADO] = "VERIFICADO" )'
    'Depósitos pendientes'              = 'CALCULATE ( COUNTROWS ( Depositos ), Depositos[ESTADO] = "PENDIENTE" )'
    'Depósitos rechazados'              = 'CALCULATE ( COUNTROWS ( Depositos ), Depositos[ESTADO] = "RECHAZADO" )'
    'Unidades verificadas'              = 'CALCULATE ( SUM ( Depositos[UNIDADES] ), Depositos[ESTADO] = "VERIFICADO" )'
    'Kg estimados (verificados)'        = 'CALCULATE ( SUM ( Depositos[PESO_ESTIMADO_KG] ), Depositos[ESTADO] = "VERIFICADO" )'
    'Kg medidos (verificados)'          = 'CALCULATE ( SUM ( Depositos[PESO_MEDIDO_KG] ), Depositos[ESTADO] = "VERIFICADO" )'
    'Verificados con peso medido'       = 'CALCULATE ( COUNTROWS ( Depositos ), Depositos[ESTADO] = "VERIFICADO", NOT ISBLANK ( Depositos[PESO_MEDIDO_KG] ) )'
    'Ciudadanos con depósito verificado' = 'CALCULATE ( DISTINCTCOUNT ( Depositos[CIUDADANO] ), Depositos[ESTADO] = "VERIFICADO" )'
    'Rótulo datos'                      = '"DATOS SINTÉTICOS · no representan personas ni pesajes reales"'
  }
  ConsultasVision = [ordered]@{ 'Fotos consultadas' = 'COUNTROWS ( ConsultasVision )' }
  Ecopuntos       = [ordered]@{
    'EcoPuntos netos'       = 'SUM ( Ecopuntos[PUNTOS] )'
    'EcoPuntos acreditados' = 'CALCULATE ( SUM ( Ecopuntos[PUNTOS] ), Ecopuntos[TIPO] = "CRÉDITO" )'
    'EcoPuntos revertidos'  = 'CALCULATE ( SUM ( Ecopuntos[PUNTOS] ), Ecopuntos[TIPO] = "REVERSO" )'
  }
  Ciudadanos      = [ordered]@{ 'Ciudadanos sin depósito verificado' = 'COUNTROWS ( FILTER ( Ciudadanos, ISBLANK ( [Depósitos verificados] ) ) )' }
}
$formatos = @{ 'Kg estimados (verificados)' = '#,0.000'; 'Kg medidos (verificados)' = '#,0.000' }
$tablas = @()
$mTodo = New-Object Text.StringBuilder
foreach ($nombre in $fuentes.Keys) {
  $f = $fuentes[$nombre]
  $cols = foreach ($c in $f.cols -split ' ') { $n, $t = $c -split ':'; [pscustomobject]@{ n = $n; t = $t } }
  $listaTipos = ($cols | ForEach-Object { "{""$($_.n)"", $($tiposM[$_.t])}" }) -join ', '
  $fechasSolo = ($cols | Where-Object { $_.n -in 'DIA', 'FECHA_REGISTRO' } | ForEach-Object { "{""$($_.n)"", type date}" }) -join ', '
  $m = @(
    'let',
    "    Origen = Csv.Document(File.Contents(CarpetaDatos & ""\$($f.csv).csv""), [Delimiter = "","", Encoding = 65001, QuoteStyle = QuoteStyle.Csv]),",
    '    Encabezados = Table.PromoteHeaders(Origen, [PromoteAllScalars = true]),',
    "    Tipos = Table.TransformColumnTypes(Encabezados, {$listaTipos}, ""en-US"")$(if ($fechasSolo) {','} else {''})"
  )
  if ($fechasSolo) { $m += "    SoloFecha = Table.TransformColumnTypes(Tipos, {$fechasSolo})"; $m += 'in'; $m += '    SoloFecha' } else { $m += 'in'; $m += '    Tipos' }
  [void]$mTodo.AppendLine("// ===== $nombre  (origen: ecoaccess_bd.$($f.csv), cuenta de solo lectura)"); [void]$mTodo.AppendLine(($m -join "`n")); [void]$mTodo.AppendLine('')
  $tabla = [ordered]@{
    name       = $nombre
    columns    = @($cols | ForEach-Object {
        $col = [ordered]@{ name = $_.n; dataType = $tipos[$_.t]; sourceColumn = $_.n; summarizeBy = 'none' }
        if ($_.t -eq 'd') { $col.formatString = if ($_.n -in 'DIA', 'FECHA_REGISTRO') { 'yyyy-mm-dd' } else { 'yyyy-mm-dd hh:nn' } }
        if ($_.t -eq 'n') { $col.formatString = '#,0.000' }
        $col })
    partitions = @([ordered]@{ name = $nombre; mode = 'import'; source = [ordered]@{ type = 'm'; expression = $m } })
  }
  if ($medidas.ContainsKey($nombre)) {
    $tabla.measures = @($medidas[$nombre].GetEnumerator() | ForEach-Object {
        $me = [ordered]@{ name = $_.Key; expression = $_.Value }
        if ($formatos.ContainsKey($_.Key)) { $me.formatString = $formatos[$_.Key] } elseif ($_.Key -ne 'Rótulo datos') { $me.formatString = '#,0' }
        $me })
  }
  $tablas += $tabla
}
# Calendario: generado en M. «Ventana» agrupa los días en tramos EXCLUYENTES contados desde el último día con datos,
# para que el segmentador permita «último día», «últimos 7 días» (tramos 1+2) o «últimos 30 días» (1+2+3).
$mCal = @(
  'let',
  '    Ultimo = List.Max(List.Combine({Depositos[DIA], ConsultasVision[DIA], Ecopuntos[DIA]})),',
  '    Fechas = List.Dates(#date(2026, 9, 1), Duration.Days(#date(2026, 12, 31) - #date(2026, 9, 1)) + 1, #duration(1, 0, 0, 0)),',
  '    Tabla = Table.FromList(Fechas, Splitter.SplitByNothing(), {"Fecha"}),',
  '    ConTipo = Table.TransformColumnTypes(Tabla, {{"Fecha", type date}}),',
  '    Semana = Table.AddColumn(ConTipo, "Semana", each Text.From(Date.Year([Fecha])) & "-S" & Text.PadStart(Text.From(Date.WeekOfYear([Fecha], Day.Monday)), 2, "0"), type text),',
  '    Mes = Table.AddColumn(Semana, "Mes", each Date.ToText([Fecha], "yyyy-MM"), type text),',
  '    Ventana = Table.AddColumn(Mes, "Ventana", each let d = Duration.Days(Ultimo - [Fecha]) in',
  '        if d < 0 then "5. Posterior al último dato" else if d = 0 then "1. Último día con datos" else if d <= 6 then "2. Días 2 a 7"',
  '        else if d <= 29 then "3. Días 8 a 30" else "4. Más de 30 días", type text)',
  'in',
  '    Ventana'
)
[void]$mTodo.AppendLine('// ===== Calendario (generado; no viene de Oracle)'); [void]$mTodo.AppendLine(($mCal -join "`n"))
$tablas += [ordered]@{
  name       = 'Calendario'
  columns    = @(
    [ordered]@{ name = 'Fecha'; dataType = 'dateTime'; sourceColumn = 'Fecha'; formatString = 'yyyy-mm-dd'; summarizeBy = 'none' },
    [ordered]@{ name = 'Semana'; dataType = 'string'; sourceColumn = 'Semana'; summarizeBy = 'none' },
    [ordered]@{ name = 'Mes'; dataType = 'string'; sourceColumn = 'Mes'; summarizeBy = 'none' },
    [ordered]@{ name = 'Ventana'; dataType = 'string'; sourceColumn = 'Ventana'; summarizeBy = 'none' })
  partitions = @([ordered]@{ name = 'Calendario'; mode = 'import'; source = [ordered]@{ type = 'm'; expression = $mCal } })
}
$rel = @(
  @('Depositos', 'CIUDADANO', 'Ciudadanos', 'CIUDADANO'), @('ConsultasVision', 'CIUDADANO', 'Ciudadanos', 'CIUDADANO'), @('Ecopuntos', 'CIUDADANO', 'Ciudadanos', 'CIUDADANO'),
  @('Depositos', 'DIA', 'Calendario', 'Fecha'), @('ConsultasVision', 'DIA', 'Calendario', 'Fecha'), @('Ecopuntos', 'DIA', 'Calendario', 'Fecha'))
$i = 0
$modelo = [ordered]@{
  compatibilityLevel = 1567
  model              = [ordered]@{
    culture                       = 'es-CO'
    defaultPowerBIDataSourceVersion = 'powerBI_V3'
    sourceQueryCulture            = 'es-CO'
    tables                        = $tablas
    relationships                 = @($rel | ForEach-Object { $i++; [ordered]@{ name = "rel$i"; fromTable = $_[0]; fromColumn = $_[1]; toTable = $_[2]; toColumn = $_[3] } })
    expressions                   = @([ordered]@{ name = 'CarpetaDatos'; kind = 'm'; expression = """$carpetaCsv"" meta [IsParameterQuery = true, Type = ""Text"", IsParameterQueryRequired = true]" })
    annotations                   = @([ordered]@{ name = 'PBI_QueryOrder'; value = '["CarpetaDatos","Depositos","ConsultasVision","Ecopuntos","Auditoria","Ciudadanos","Calendario"]' })
  }
}
Guardar "$destino\EcoAccess_BD.SemanticModel\model.bim" $modelo
Guardar "$destino\EcoAccess_BD.SemanticModel\definition.pbism" ([ordered]@{ version = '4.2'; settings = @{} })
[IO.File]::WriteAllText("$aqui\DATOS_Y_CONSULTAS\power_query.m", "// Power Query (M) del modelo EcoAccess_BD. GENERADO por generar_pbip.ps1.`n// Parámetro CarpetaDatos = carpeta con los CSV exportados por exportar_csv.sql.`n`n" + $mTodo.ToString(), $utf8)
$dax = New-Object Text.StringBuilder
[void]$dax.AppendLine('// Medidas DAX del modelo EcoAccess_BD. GENERADO por generar_pbip.ps1.')
[void]$dax.AppendLine('// Regla: el peso ESTIMADO (catálogo PESO_REFERENCIAL) y el MEDIDO (instrumento) nunca se suman entre sí.')
foreach ($t in $medidas.Keys) { foreach ($kv in $medidas[$t].GetEnumerator()) { [void]$dax.AppendLine(''); [void]$dax.AppendLine("$t[$($kv.Key)] =`n    $($kv.Value)") } }
[IO.File]::WriteAllText("$aqui\DATOS_Y_CONSULTAS\medidas.dax", $dax.ToString(), $utf8)

# ------------------------------------------------------------------ INFORME
function Lit($v) { @{ expr = @{ Literal = @{ Value = $v } } } }
function Txt($s) { "'" + $s.Replace("'", "''") + "'" }
$script:z = 0
function Contenedor($nombre, $x, $y, $w, $h, $single) {
  $script:z += 100
  $cfg = [ordered]@{ name = $nombre; layouts = @(@{ id = 0; position = [ordered]@{ x = $x; y = $y; z = $script:z; width = $w; height = $h } }); singleVisual = $single }
  [ordered]@{ x = $x; y = $y; z = $script:z; width = $w; height = $h; config = ($cfg | ConvertTo-Json -Depth 60 -Compress); filters = '[]' }
}
# Campos: 'M:Tabla.Medida' o 'C:Tabla.Columna'; roles: hashtable rol -> lista de campos
function Visual($tipo, $roles, $titulo, $ordenarPor) {
  $alias = [ordered]@{}; $select = @(); $proy = [ordered]@{}
  foreach ($rol in $roles.Keys) {
    $proy[$rol] = @()
    foreach ($campo in $roles[$rol]) {
      $k, $resto = $campo -split ':', 2; $t, $p = $resto -split '\.', 2
      if (-not $alias.Contains($t)) { $alias[$t] = "t$($alias.Count)" }
      $clase = if ($k -eq 'M') { 'Measure' } else { 'Column' }
      if (-not ($select | Where-Object { $_.Name -eq "$t.$p" })) {
        $select += [ordered]@{ $clase = [ordered]@{ Expression = @{ SourceRef = @{ Source = $alias[$t] } }; Property = $p }; Name = "$t.$p" }
      }
      $ref = [ordered]@{ queryRef = "$t.$p" }; if ($rol -eq 'Category' -or $rol -eq 'Rows') { $ref.active = $true }
      $proy[$rol] += $ref
    }
  }
  $q = [ordered]@{ Version = 2; From = @($alias.Keys | ForEach-Object { [ordered]@{ Name = $alias[$_]; Entity = $_; Type = 0 } }); Select = $select }
  if ($ordenarPor) {
    $k, $resto = $ordenarPor -split ':', 2; $t, $p = $resto -split '\.', 2; $clase = if ($k -eq 'M') { 'Measure' } else { 'Column' }
    $dir = if ($k -eq 'M') { 2 } else { 1 }
    $q.OrderBy = @([ordered]@{ Direction = $dir; Expression = @{ $clase = [ordered]@{ Expression = @{ SourceRef = @{ Source = $alias[$t] } }; Property = $p } } })
  }
  $v = [ordered]@{ visualType = $tipo; projections = $proy; prototypeQuery = $q; drillFilterOtherVisuals = $true }
  if ($titulo) { $v.vcObjects = @{ title = @(@{ properties = [ordered]@{ show = (Lit 'true'); text = (Lit (Txt $titulo)) } }) } }
  $v
}
function Texto($lineas, $tam = '11pt', $color = '#252423', $negrita = $false) {
  $parr = @($lineas | ForEach-Object {
      $st = [ordered]@{ fontSize = $tam; color = $color }; if ($negrita) { $st.fontWeight = 'bold' }
      @{ textRuns = @(@{ value = $_; textStyle = $st }) } })
  [ordered]@{ visualType = 'textbox'; drillFilterOtherVisuals = $true; objects = @{ general = @(@{ properties = @{ paragraphs = $parr } }) } }
}
function Boton($texto, $seccion) {
  [ordered]@{
    visualType = 'actionButton'; drillFilterOtherVisuals = $true
    objects    = [ordered]@{
      icon = @(@{ properties = @{ shapeType = (Lit "'blank'") }; selector = @{ id = 'default' } })
      text = @(@{ properties = [ordered]@{ show = (Lit 'true'); text = (Lit (Txt $texto)); fontSize = (Lit '12D'); fontColor = @{ solid = @{ color = (Lit "'#FFFFFF'") } } }; selector = @{ id = 'default' } })
      fill = @(@{ properties = [ordered]@{ show = (Lit 'true'); fillColor = @{ solid = @{ color = (Lit "'#1E6B3A'") } }; transparency = (Lit '0D') }; selector = @{ id = 'default' } })
    }
    vcObjects  = @{ visualLink = @(@{ properties = [ordered]@{ show = (Lit 'true'); type = (Lit "'PageNavigation'"); navigationSection = (Lit (Txt $seccion)) } }) }
  }
}
function Segmentador($campo, $modo) {
  $v = Visual 'slicer' ([ordered]@{ Values = @($campo) }) $null $null
  $v.objects = @{ data = @(@{ properties = @{ mode = (Lit (Txt $modo)) } }) }
  $v
}
$paginas = [ordered]@{ ReportSection1 = 'Inicio'; ReportSection2 = 'Operación'; ReportSection3 = 'Ranking y pesos'; ReportSection4 = 'EcoPuntos y auditoría'; ReportSection5 = 'Galería de roles' }
function Comunes($sec) {
  $c = @()
  $c += Contenedor "rotulo_$sec" 880 8 392 40 (Texto @('DATOS SINTÉTICOS · no son datos reales') '12pt' '#B00020' $true)
  if ($sec -ne 'ReportSection1') { $c += Contenedor "volver_$sec" 8 8 130 36 (Boton '← Inicio' 'ReportSection1') }
  $c
}
$s = @{}
# 1. Inicio
$c = @(Comunes 'ReportSection1')
$c += Contenedor 'titulo' 20 50 840 110 (Texto @('EcoAccess · Base de Datos (profesor Amílkar)', 'Universidad Popular del Cesar · Informe de exposición', 'Fuente: vistas VW_BI_* leídas con la cuenta ECOACCESS_BD_LECTOR (solo lectura, seudónimos C-0001…).') '13pt' '#1E6B3A' $true)
$i = 0
$tarjetasInicio = [ordered]@{ 'ConsultasVision.Fotos consultadas' = 'Fotos consultadas (NO son depósitos)'; 'Depositos.Depósitos registrados' = 'Depósitos registrados'; 'Depositos.Depósitos verificados' = 'Depósitos verificados'; 'Ecopuntos.EcoPuntos netos' = 'EcoPuntos netos' }
foreach ($kv in $tarjetasInicio.GetEnumerator()) {
  $c += Contenedor "tarjeta_ini_$i" (20 + 310 * $i) 180 290 130 (Visual 'card' ([ordered]@{ Values = @("M:$($kv.Key)") }) $kv.Value $null); $i++
}
$i = 0
foreach ($sec in 'ReportSection2', 'ReportSection3', 'ReportSection4', 'ReportSection5') {
  $c += Contenedor "nav_$sec" (20 + 310 * $i) 340 290 60 (Boton $paginas[$sec] $sec); $i++
}
$c += Contenedor 'explica' 20 420 1240 280 (Texto @(
    'Cómo leer este tablero:',
    '• Una FOTO consultada a Eco no es un depósito. Un depósito REGISTRADO no es un depósito VERIFICADO.',
    '• Solo los depósitos VERIFICADOS por una persona distinta del titular (operario o administrador) cuentan para rankings y EcoPuntos.',
    '• «Kg estimados» salen del catálogo PESO_REFERENCIAL; «Kg medidos» solo existen cuando hubo instrumento. Nunca se suman entre sí.',
    '• Las fechas están en hora de Colombia (America/Bogota). Los ciudadanos aparecen con seudónimo; no hay correos ni teléfonos.',
    '• Todos los datos son SINTÉTICOS (generados con semilla 20261110) para la sustentación académica.') '12pt')
$s.ReportSection1 = $c
# 2. Operación
$c = @(Comunes 'ReportSection2')
$c += Contenedor 'seg_fecha' 150 8 360 80 (Segmentador 'C:Calendario.Fecha' 'Between')
$c += Contenedor 'seg_ventana' 8 96 230 150 (Segmentador 'C:Calendario.Ventana' 'Basic')
$c += Contenedor 'seg_caneca' 8 254 230 120 (Segmentador 'C:Depositos.CANECA' 'Basic')
$c += Contenedor 'seg_material' 8 382 230 70 (Segmentador 'C:Depositos.MATERIAL_NOMBRE' 'Dropdown')
$c += Contenedor 'seg_estado' 8 460 230 120 (Segmentador 'C:Depositos.ESTADO' 'Basic')
$i = 0
foreach ($m in 'Depósitos verificados', 'Depósitos pendientes', 'Depósitos rechazados', 'Unidades verificadas') {
  $c += Contenedor "tarjeta_op_$i" (250 + 255 * $i) 96 245 100 (Visual 'card' ([ordered]@{ Values = @("M:Depositos.$m") }) $m $null); $i++
}
$c += Contenedor 'g_caneca' 250 206 500 250 (Visual 'clusteredBarChart' ([ordered]@{ Category = @('C:Depositos.CANECA'); Y = @('M:Depositos.Depósitos verificados') }) 'Depósitos verificados por caneca' 'M:Depositos.Depósitos verificados')
$c += Contenedor 'g_semana' 760 206 510 250 (Visual 'clusteredColumnChart' ([ordered]@{ Category = @('C:Calendario.Semana'); Y = @('M:ConsultasVision.Fotos consultadas', 'M:Depositos.Depósitos registrados', 'M:Depositos.Depósitos verificados') }) 'Fotos consultadas vs depósitos, por semana' 'C:Calendario.Semana')
$c += Contenedor 'g_material' 250 466 1020 244 (Visual 'clusteredBarChart' ([ordered]@{ Category = @('C:Depositos.MATERIAL_NOMBRE'); Y = @('M:Depositos.Unidades verificadas') }) 'Unidades verificadas por material' 'M:Depositos.Unidades verificadas')
$s.ReportSection2 = $c
# 3. Ranking y pesos
$c = @(Comunes 'ReportSection3')
$c += Contenedor 'seg_ventana3' 150 8 330 80 (Segmentador 'C:Calendario.Ventana' 'Dropdown')
$c += Contenedor 'nota3' 490 8 380 80 (Texto @('Ventana: elige «1» y «2» para los últimos 7 días.', 'Ranking = SOLO depósitos verificados.') '10pt')
$i = 0
foreach ($m in 'Kg estimados (verificados)', 'Kg medidos (verificados)', 'Verificados con peso medido', 'Ciudadanos con depósito verificado') {
  $c += Contenedor "tarjeta_pe_$i" (8 + 316 * $i) 96 306 100 (Visual 'card' ([ordered]@{ Values = @("M:Depositos.$m") }) $m $null); $i++
}
$c += Contenedor 't_ranking' 8 206 620 504 (Visual 'tableEx' ([ordered]@{ Values = @('C:Depositos.CIUDADANO', 'M:Depositos.Depósitos verificados', 'M:Depositos.Unidades verificadas', 'M:Depositos.Kg estimados (verificados)', 'M:Depositos.Kg medidos (verificados)') }) 'TOP ciudadanos por depósitos verificados' 'M:Depositos.Depósitos verificados')
$c += Contenedor 'g_pesos' 640 206 630 504 (Visual 'clusteredBarChart' ([ordered]@{ Category = @('C:Depositos.MATERIAL_NOMBRE'); Y = @('M:Depositos.Kg estimados (verificados)', 'M:Depositos.Kg medidos (verificados)') }) 'Kg ESTIMADOS vs kg MEDIDOS por material (no se suman)' 'M:Depositos.Kg estimados (verificados)')
$s.ReportSection3 = $c
# 4. EcoPuntos y auditoría
$c = @(Comunes 'ReportSection4')
$i = 0
foreach ($m in 'EcoPuntos acreditados', 'EcoPuntos revertidos', 'EcoPuntos netos') {
  $c += Contenedor "tarjeta_ec_$i" (8 + 300 * $i) 56 290 100 (Visual 'card' ([ordered]@{ Values = @("M:Ecopuntos.$m") }) $m $null); $i++
}
$c += Contenedor 'nota4' 920 56 350 100 (Texto @('Tarifas SINTÉTICAS de prueba.', 'Sin tarifa aprobada el saldo no aumenta.', 'Un reverso nunca borra: resta.') '10pt')
$c += Contenedor 'g_puntos' 8 166 440 544 (Visual 'clusteredBarChart' ([ordered]@{ Category = @('C:Ecopuntos.MATERIAL'); Y = @('M:Ecopuntos.EcoPuntos netos') }) 'EcoPuntos netos por material' 'M:Ecopuntos.EcoPuntos netos')
$c += Contenedor 't_auditoria' 458 166 812 544 (Visual 'tableEx' ([ordered]@{ Values = @('C:Auditoria.ID_DEPOSITO', 'C:Auditoria.OPERACION', 'C:Auditoria.ESTADO_ANTERIOR', 'C:Auditoria.ESTADO_NUEVO', 'C:Auditoria.ACTOR', 'C:Auditoria.MOTIVO', 'C:Auditoria.FECHA_COLOMBIA') }) 'Auditoría de verificaciones (trigger TRG_DEPOSITO_AUD_VERIFICACION)' 'C:Auditoria.ID_DEPOSITO')
$s.ReportSection4 = $c
# 5. Galería de roles (didáctica: NO es un mecanismo de seguridad)
$c = @(Comunes 'ReportSection5')
$c += Contenedor 'gal_tit' 150 8 720 44 (Texto @('Galería didáctica de roles · qué ve y qué hace cada uno') '14pt' '#1E6B3A' $true)
$roles = @(
  @('CIUDADANO (rol 1)', @('Escanea el QR de la estación y habla con Eco por Telegram.', 'Envía fotos: cada una es una CONSULTA_VISION, no un depósito.', 'Deposita en la caneca indicada (VERDE, BLANCA o NEGRA).', 'Ve SUS depósitos y SU saldo de EcoPuntos.', 'NO puede verificar sus propios depósitos (ORA-20101).')),
  @('OPERARIO (rol 2)', @('Verifica o rechaza depósitos de OTRAS personas.', 'Debe dar un motivo si el color no coincide (ORA-20105).', 'Ve estadísticas y rankings de verificados.', 'NO cambia el estado de las canecas (ORA-20120).', 'NO registra tarifas de EcoPuntos.')),
  @('ADMINISTRADOR (rol 3)', @('Todo lo del operario.', 'Pone canecas en servicio o fuera de servicio (queda en el historial).', 'Registra versiones de tarifa (REGLA_ECOPUNTOS).', 'Corrige una verificación cerrada solo con PKG_DEPOSITO.corregir_verificacion y motivo.', 'Lee la auditoría completa.'))
)
$i = 0
foreach ($r in $roles) {
  $c += Contenedor "gal_$i" (8 + 424 * $i) 60 414 420 (Texto (@($r[0]) + ($r[1] | ForEach-Object { "• $_" })) '12pt'); $i++
}
$c += Contenedor 'gal_nota' 8 490 1262 210 (Texto @(
    'Importante: esta galería EXPLICA los roles; no es la seguridad.',
    'La seguridad real está en Oracle: PKG_CONTEXTO_APP.exigir_rol en los procedimientos, los triggers de DEPOSITO y la cuenta ECOACCESS_BD_LECTOR, que solo puede leer vistas (USUARIO da ORA-00942).',
    'Este tablero no aplica RLS porque solo muestra seudónimos y agregados; si se publica en el servicio de Power BI, se recomienda RLS por rol.') '11pt' '#5B3A00')
$s.ReportSection5 = $c

$secciones = @(); $orden = 0
foreach ($sec in $paginas.Keys) {
  $secciones += [ordered]@{ name = $sec; displayName = $paginas[$sec]; displayOption = 1; width = 1280; height = 720; ordinal = $orden; config = '{}'; filters = '[]'; visualContainers = @($s[$sec]) }
  $orden++
}
$informe = [ordered]@{ config = '{"version":"5.43","activeSectionIndex":0}'; layoutOptimization = 0; resourcePackages = @(); sections = $secciones; filters = '[]' }
Guardar "$destino\EcoAccess_BD.Report\report.json" $informe
Guardar "$destino\EcoAccess_BD.Report\definition.pbir" ([ordered]@{ version = '4.0'; datasetReference = @{ byPath = @{ path = '../EcoAccess_BD.SemanticModel' } } })
Guardar "$destino\EcoAccess_BD.pbip" ([ordered]@{ version = '1.0'; artifacts = @(@{ report = @{ path = 'EcoAccess_BD.Report' } }); settings = @{ enableAutoRecovery = $true } })
"Proyecto generado en $destino · páginas: $($paginas.Count) · visuales: $(($s.Values | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum)"
